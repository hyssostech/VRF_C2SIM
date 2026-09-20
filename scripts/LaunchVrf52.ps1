# LaunchVrf52.ps1 - bring up VR-Forces 5.2d in INDEPENDENT mode (direct bin64 launch).
#
# WHY A SEPARATE SCRIPT (2026-09-03): every LaunchVrf.ps1 invocation is INVALID on 5.2d.
# The 5.0.2 vrfLauncher option set (--usePredefinedConnection, --connectionProfile ...)
# is gone; 5.2 combined mode exists only as `vrfLauncher --connection "<name>" --run`
# and REQUIRES a connection saved from the Launcher GUI at least once (UG52 5.3.1).
# The documented alternative that needs no GUI-saved state is the independent launch
# (UG52 4.1.2 "Starting Independent VR-Forces Executables", p.133; 4.1.3 session ID;
# 4.1.4 HLA Configuration example, p.135):
#     vrfGui --siteId 1 --appNumber 3000 --hla1516e
#     vrfSimHLA1516e --siteId 1 --appNumber 3002
# Both default to ./appData/settings/connections/MAK-ONE-YYYY-Config.xml
# (--exConnConfigFile, Table 11/12, p.167/181) and to session 1 (4.1.3).
# vrfSim options used (UG52 Table 11, p.181): -L|--scenarioFileName (relative to
# ./bin), -n|--notifyLevel 0-4 (default 2), -q|--doNotUseConsole. --logFileName is
# NOT among them any more - see the VENDOR LOG block below.
#
# VENDOR LOG: --logFileName IS NOT PASSED; THE VENDOR'S OWN LOG IS HARVESTED
# (2026-09-04, docs/experiments/PREREG_52_CRASH_BISECT_2026-09-04.md sec 5). Passing
# --logFileName crashed the sim at startup in 6 of 18 launches (33%); omitting it crashed
# 0 of 12 (Fisher's exact, one-sided, p = 0.031). A 22-character path inside the vendor's
# own C:\MAK\logs crashed too, so it is the OPTION, not the path length or location. The
# sim ALWAYS writes its own log to C:\MAK\logs\vrfSimHLA1516e5.2d-<date>-<time>-<host>-
# <build>-<pid>.log regardless, so nothing is lost: at READY (and on the crash path) this
# script COPIES that file for THIS pid to -LogFile. The copy is a SNAPSHOT taken at that
# moment - the vendor keeps writing - and a later snapshot is just another run of the
# harvest. -LogFileName <path> still exists to exercise the option deliberately (a
# repeat of the bisect, a vendor bug report); DO NOT re-enable it casually - it is a
# 1-in-3 startup crash.
# *** SECRETS: the harvested vendor log contains the FULL PROCESS ENVIRONMENT IN
# CLEARTEXT (DtPrintEnvironmentVariables at --notifyLevel 3; FORENSICS_52_STARTUP_CRASH_
# 2026-09-04 sec 10) - never attach it to a ticket, mail or issue; send the
# .callstack.log / .dmp instead. It is NOT scrubbed here on purpose: a scrubber that
# silently misses one variable is worse than a warning that is always true. ***
#
# ENVIRONMENT (the two 5.2 runtime traps, DIFF sec H): MAK DLLs bind BY NAME on
# PATH and the Machine PATH lists vrforces5.0.2 / vrlink5.8 first, so this script
# PREFIXES the process PATH with the 5.2d stack. The RTI 5.0.1 installer also set
# MAK_RTIDIR / RTI_RID_FILE to makRti5.0.1 at Machine scope while the Machine PATH
# still lists makRti4.6.1\bin, so BOTH are set per process (to 5.0.1, the RTI this
# profile is defined on) rather than trusted. Nothing is written to Machine/User
# scope; the 5.0.2 script and its environment are untouched.
#
# NO RTI ASSISTANT (2026-09-03, PREREG_52_LAUNCH result): the 5.0.1 installer left
# an ELEVATED 5.0.1 rtiAssistant on port 6003 that version-rejects every 4.6.1
# federate ("RTI component was using a different RTI version than the RTI
# Assistant" - the assistant's own toast). Fix, both documented: set
# RTI_ASSISTANT_DISABLE (existence alone disables assistant use, MAK RTI 4.6.1
# Reference Manual 5.2.10) and take the connection from the rid instead
# (RTI_configureConnectionWithRid 1). Every federate that must interoperate with
# this sim (RtiProbe/WatchVrf/CreateOne/app) needs the SAME env: assistant
# disabled + the SAME rid file, or it will not share a connection.
#
# ...BUT THE RID MUST BE THE RTIEXEC POSTURE, NOT A LIGHTWEIGHT ONE (2026-09-04,
# PREREG_52_RTIEXEC result - THIS SUPERSEDES THE PARAGRAPH ABOVE'S OLD DEFAULT).
# The first assistant-free rid, config/rid-461-ridconfigured.mtl, was a WRONG FIX:
# it bypassed the version-locked assistant but put the federation in LIGHTWEIGHT
# mode, and UG52 5.5.1 p190 says flatly "You cannot use the MAK RTI in lightweight
# mode with VR-Forces" (the 5.0.2 qualifier "if you are running multiple ...
# federation executions" was dropped in 5.2). Under it every observer reflected 0
# entities. The defaults here are now MAK RTI 5.0.1 with
# config/rid-501-rtiexec-min.mtl (rtiexec at 127.0.0.1:4001, loopback broadcast
# 127.255.255.255 on interface 127.0.0.1, forwarder 5000, internal messages
# reliable - the twelve parameters RTI UG 5.0.1 sec 7.3 p73 lists) and an rtiexec
# must ALREADY BE RUNNING for that rid: scripts/StartRtiExec52.ps1 ensures one,
# and the runner does it in Stage 2r. Under that posture the observer reflected 62
# entities. Assistant-free STAYS; it is orthogonal to lightweight-vs-rtiexec.
# NOT part of the repair: -DeviceAddress. The repairing run set it too, but the
# discriminator (run 3857) then reflected 54-56 entities with the observer's device
# address blank, so it is a tunable that defaults to OFF - see the parameter.
#
# READINESS is the LaunchVrf.ps1 oracle, unchanged: back-end thread count above
# -BackendMinThreads (a blocked back-end sits at 2-4 threads while present; healthy
# 5.0.2 reached 23-67, and the 5.2d BASELINE is now observed at 34-62 - 36 at the
# first healthy launch, PREREG_52_LAUNCH_2026-09-03 attempt 2 - so the floor of 8
# separates the two states with room to spare on both stacks) and, unless
# -NoGui, a vrfGui with a NON-EMPTY MainWindowTitle (empty = modal dialog; on 5.2
# the documented candidate is the Scenario Startup dialog, UG52 4.1.1 Figure 17).
# Federation JOIN is NOT tested here - confirm with the 5.2 RtiProbe/WatchVrf build.
#
# STARTUP CRASH DETECTION (2026-09-04, cold-start review of PREREG_52_RTIEXEC). The 5.2
# sim sometimes dies at startup with 0xC0000005 inside
# makVrf::DtVrfSimOptions::parseCmdLine - 2 of 5 launches on 2026-09-04, under BOTH the
# lightweight and the rtiexec rid, so it is NOT a rid property. THE TRIGGER IS NOW KNOWN and
# is removed by default: --logFileName (PREREG_52_CRASH_BISECT_2026-09-04 sec 5; VENDOR LOG
# block above). Detection STAYS - it is the guard for the residual and for -LogFileName runs.
# It must never be mistaken for "not ready yet": the readiness poll therefore watches for
# three signatures and fails the launch the moment any appears -
#   (a) the back-end process is GONE;
#   (b) its main window title is MAK's crash box ("Error vrfSimHLA1516e.exe", or the 5.0.2
#       'vrfSim*.dmp' form AnswerCrashDumpDialog.ps1 answers);
#   (c) a NEW <MakLogDir>\vrfSimHLA1516e*-<pid>.callstack.log exists (the handler writes it
#       with the faulting pid as the last field, verified against the 38180 / 39028 files).
# The first frames of the callstack go into this script's own output so the runner log
# carries the evidence, and the exit is 3. NO RETRY: whether a crashed launch should be
# retried is a separate decision, and a silent retry would hide how often this fires.
#
# THE CRASHED PROCESS LINGERS, AND IT BLOCKED THE NEXT LAUNCH (observed 2026-09-04 11:28-11:30
# UTC; the two console captures named in that report, runs\launch52\launch_3860_appsmoke.txt and
# launch_3862_appsmoke_retry.txt, were NOT persisted, but the crash itself is on disk:
# C:\MAK\logs\vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.callstack.log and its
# .dmp, stamped 07:28:06 LOCAL = 11:28 UTC). Detection worked - "CRASHED AT STARTUP ... exit 3"
# for that pid 59936 - but MAK's crash handler KEEPS THE
# FAULTED PROCESS ALIVE (title becomes 'Error vrfSimHLA1516e.exe', 0 threads), so the very next
# launch was refused by the pre-existing-process precondition (exit 2) and an unattended runner
# could not even retry. Two narrow, asymmetric remedies, both bounded by the project rule that
# a process which FAILED ITS OWN start/join may be closed without asking while a healthy one
# may not:
#   - OUR OWN pid, this launch: when the poll declares CRASHED AT STARTUP for the pid THIS
#     script started, it closes that pid before exiting 3 - first scripts\AnswerCrashDumpDialog
#     .ps1 (it answers the 5.0.2-form '<exe>...dmp' prompt; the 5.2 box titled 'Error <exe>' is
#     NOT one it matches, so it simply reports no dialog), then Stop-Process on that single pid.
#     -LeaveCrashedProcess opts out when the live process is wanted for forensics.
#   - A PRE-EXISTING pid, some earlier launch's: NOT closed by default - this script cannot know
#     it failed its own start, only that it looks dead. -CloseCrashedLeftover closes it, and
#     ONLY when BOTH hold: MAK wrote a <pid>.callstack.log for it AND it has <= 4 threads. Both
#     are required because either alone is ambiguous - a callstack file can be a recycled pid's
#     (Windows reuses pids and C:\MAK\logs keeps files across boots), and a low thread count
#     alone is also the signature of a back-end merely BLOCKED on the RTI (2-4 threads while
#     present). Together they cannot describe a healthy sim, which runs at 34-62 threads here.
# NOTHING ELSE IS EVER CLOSED: not vrfGui, not another vrfSim, and never rtiexec / rtiForwarder
# / rtiAssistant. If a front-end this script started is still up after a crash it is reported,
# not killed - it will block the next launch until a human deals with it.
# On the CAUSE of the crash itself see docs/experiments/FORENSICS_52_STARTUP_CRASH_2026-09-04.md
# (a vendor-side fault in vl.dll, with rid / launcher / cwd / timing falsified as triggers),
# RE-SCOPED by PREREG_52_CRASH_BISECT_2026-09-04 sec 5: the fault lives in the --logFileName
# path, which we simply stop taking. It does not change this script's job: detect it, print the
# evidence, exit 3, and do not leave the corpse blocking the next launch.
#
# THE STP-825 FEDERATION HOLDER (2026-09-15, -FederationHoldSecs). rtiexec 5.0.1 rejects a
# CREATOR's FOM-module distribution intermittently (~1 create in 4-7; "Failed to process FOM
# file <module> ... Sending Create Response = Error") - JOINS have never failed. The TEST
# harness routes around this at RunC2SimScenario.ps1 Stage 2h: a holder federate creates-or-
# joins the federation before anything else, so the sim only ever JOINS (RUNBOOK 0.5.14 item
# 18). This script has NO runner in front of it on the STANDALONE DEMO path
# (docs/DEMO_RUNBOOK.md: StartRtiExec52 -> LaunchVrf52 -> StartInterface52): without a holder
# of its own, THIS SCRIPT'S OWN back end would be the federation's CREATOR, and a rejected
# create kills it at startup exactly as it did on the runner before Stage 2h existed. So this
# script now starts the SAME KIND of holder in the SAME posture (tools/RtiProbe.exe <appNo>
# <execName> 1 <holdSecs> 3, detached, own hidden console, never killed - RUNBOOK sec 0)
# before the back end. Default ON (900 s, appNumber 9190 - the demo block's free slot, see
# the parameter below); -FederationHoldSecs 0 restores the pre-STP-825 behaviour and is what
# the RUNNER passes explicitly at its own Stage 3 call site, because its OWN Stage 2h holder
# already covers the sim - two holders would burn a second appNumber for nothing.
#
# Exit codes (same contract as LaunchVrf.ps1): 0 READY; 1 PARTIAL (back-end healthy,
# no front-end); 2 precondition/usage failure (nothing launched); 3 NOT READY within
# timeout, the back-end CRASHED at startup, or the STP-825 federation holder could not
# join (nothing launched); 4 BLOCKED (front-end process up, no
# window title).
# Non-negotiables: NEVER kill rtiAssistant / rtiexec / rtiForwarder; fresh ledgered
# app numbers per join (OPUS_EXECUTION_PLAN.md App. B, NEXT FREE marker). ASCII-only.
param(
    [string] $VrfRoot            = 'C:\MAK\vrforces5.2d',
    [string] $VrLinkRoot         = 'C:\MAK\vrlink5.10',
    # MAK RTI 5.0.1 - the 5.2 profile's RTI, in RTIEXEC mode (see the header). 4.6.1 is the
    # 5.0.2 stack's RTI and must not appear here: the two version-reject each other.
    [string] $RtiDir             = 'C:\MAK\makRti5.0.1',
    # Scenario name under $VrfRoot\userData\scenarios (subdirs allowed, e.g.
    # 'Sample\Raid'); EMPTY = no -L (sim engine starts with no scenario loaded).
    [string] $Scenario           = '',
    # MANDATORY, no defaults - the never-reuse rule (RUNBOOK sec 0).
    [int]    $BackendAppNumber   = 0,
    [int]    $FrontendAppNumber  = 0,
    [int]    $SiteId             = 1,
    [int]    $SessionId          = 1,
    [switch] $NoGui,
    [int]    $NotifyLevel        = 3,
    # Back-end log - now the HARVEST DESTINATION, not a --logFileName argument (header,
    # VENDOR LOG). EMPTY = <repo>\runs\launch52\vrfSim_<appNo>_<UTCstamp>.log (never
    # C:\MAK\logs, the 5.2 default, and never under C:\MAK at all). The vendor's own log
    # for this pid is COPIED here at READY / on a startup crash. SECRETS: that copy holds
    # the whole process environment in cleartext - never attach it anywhere.
    [string] $LogFile            = '',
    # DELIBERATE re-enable of --logFileName, and NOTHING ELSE uses it. EMPTY (the default)
    # = the option is NOT passed, because passing it crashes the sim at startup ~1 launch
    # in 3: 6 crashes / 18 launches with it, 0 / 12 without, p = 0.031
    # (docs/experiments/PREREG_52_CRASH_BISECT_2026-09-04.md sec 5; a short vendor-default
    # path crashed too, so it is the option, not the path). Pass a path here only to
    # reproduce that bisect or to give MAK a repro - never for routine logging: the
    # vendor's own log is harvested to -LogFile instead.
    [string] $LogFileName        = '',
    [string] $ExConnConfigFile   = '',
    # RELOCATED appData. UG52 Table 11 p178 (vrfSim) and Table 10 p164 (vrfGui), same text:
    # '--appDataDir directory - Specifies the location of application data. If not specified,
    # VR-Forces uses the default: ./appData'. EMPTY (the default) = the option is NOT passed
    # and both executables use $VrfRoot\appData exactly as before - no behaviour change.
    # Point it at C:\C2SIM\vrf-appdata\appData (that tree's README-C2SIM.txt carries the
    # provenance and the reinstall procedure) to run on the reinstall-safe copy whose ONLY
    # delta from the vendor tree is (setqb loadAllNavigationDataOnTerrainLoad 1): navigation
    # data loaded WITH the scenario instead of lazily when an entity is placed (UG52 App. C
    # Table 76 p1671). On run 20260914T130439Z the lazy path put 'New Primary nav area' at
    # wall 201 s against member creation at wall 25 s, so units tasked at creation planned
    # with no mesh; 5.2 Release Notes VRF-9225 confirms lazy loading is the designed
    # behaviour for local entities, not a misconfiguration.
    # 5.2 is the first release where this option can be trusted: Release Notes VRF-9265 fixed
    # '--appDataDir is not processed until after attempting to load config files and many
    # instances of hard-coded relative paths', and VRF-9255 taught the Launcher about it.
    # The directory must EXIST (hard precondition below). It is ALSO where the sim's terrain
    # cache goes - terrainInterfaceConfig.mtl:244 documents the default sim-cache-path as
    # $(APP_DIR)/cache/vrfsim - which is why the supplied tree junctions its cache\ back to
    # the vendor's warm one instead of starting cold.
    [string] $AppDataDir         = '',
    # rid with RTI_configureConnectionWithRid 1; EMPTY = the repo-owned
    # config\rid-501-rtiexec-min.mtl (the RTIEXEC posture - see the header; an rtiexec must
    # already be up for it, scripts\StartRtiExec52.ps1). Assistant use is DISABLED unless
    # -UseRtiAssistant, which reverts to $RtiDir\rid.mtl + assistant flow.
    [string] $RidFile            = '',
    [switch] $UseRtiAssistant,
    # STP-825 (2026-09-15): see the header. 0 = OFF, the pre-STP-825 behaviour - this is what
    # the runner (RunC2SimScenario.ps1) passes explicitly at its own Stage 3 call site, since
    # its OWN Stage 2h holder already covers the sim before Stage 3 ever runs. Positive =
    # start a holder federate BEFORE the back end so this script's own back end only ever
    # JOINS the federation (joins have never failed - only the CREATE is rejected
    # intermittently). Bounds match the runner's own Stage 2h validation (0..86400).
    [int]    $FederationHoldSecs      = 900,
    # DEMO application-number block is 9101-9199 (DEMO_READINESS_2026-09-06 row 8;
    # appsettings.Demo.json _ApplicationNumber comment): the interface owns 9101, and the
    # DEMO_RUNBOOK.md Way B example back end/front end (9201/9202) sit OUTSIDE that block
    # entirely. 9190 is FREE in the documented range - nothing in the repo's docs or
    # appsettings names 9102-9199 except 9101 itself - and is placed near the top of the
    # block, away from 9101, so an operator's own back end/front end numbers chosen inside
    # 9101-9199 are unlikely to collide with it by coincidence. On a refused create the
    # holder retries ONCE on -FederationHoldAppNumber + 1 (9191 by default) - a NEW
    # appNumber, never a reused one (RUNBOOK sec 0).
    [int]    $FederationHoldAppNumber = 9190,
    [int]    $ReadyTimeoutSec    = 120,
    # How long to wait, AFTER thread-count READY and only when -Scenario was given, for the
    # vendor's own "Successfully loaded scenario" line before harvesting its log. READY fires
    # before load completes, so harvesting at READY truncates the copy mid-load - which once cost
    # a false finding (see the SCENARIO-LOAD GATE comment at the harvest call). 0 disables the
    # wait and restores the old harvest-at-READY behaviour.
    [int]    $ScenarioLoadTimeoutSec = 180,
    [int]    $PollIntervalSec    = 3,
    [int]    $BackendMinThreads  = 8,
    [switch] $IgnoreUnansweredRtiAssistant,
    [switch] $AllowExistingVrf,
    [switch] $QuietBackend,
    # The 5.0.2 Launcher's "Network Interface Address" (its profile <hostAddress>): the card
    # for UDP/best-effort traffic. UG52 Table 10 p177 / Table 11 p180-181: --deviceAddress and
    # --hostAddressString|-H on BOTH vrfGui and vrfSim. EMPTY = not passed (VR-Forces picks
    # "the first device listed", IOG 5.2.1 p81). RESEARCH_52_HLA_CONNECTION_CONFIG P3.
    # NOT part of the observation-channel repair: run 3857 (PREREG_52_RTIEXEC sec 4 P4) had an
    # observer with a BLANK device address reflect 54-56 entities off the rtiexec sim. The
    # runner therefore passes nothing by default and the sim launches unpinned - which is also
    # the open sim-side arm of that same test. 127.0.0.1 is the 5.0.2 configuration's value,
    # available here to pin deliberately, never because a 5.2 experiment demanded it.
    [string] $DeviceAddress      = '',
    # Where MAK's crash handler writes <exe><ver>-<date>-<time>-<host>-<build>-<pid>.callstack
    # .log (and the matching .dmp) - see the STARTUP CRASH block in the header.
    [string] $MakLogDir          = 'C:\MAK\logs',
    # Do NOT close the back-end THIS launch started when it crashes at startup (default is to
    # close it, so an unattended retry is not blocked by the corpse). For forensics: the live
    # process, its crash box and its handles stay put - and WILL refuse the next launch.
    [switch] $LeaveCrashedProcess,
    # Close a PRE-EXISTING vrfSimHLA1516e that this script did not start, and only one that
    # BOTH has a <pid>.callstack.log in -MakLogDir AND runs <= 4 threads (header: either
    # condition alone is ambiguous). Off by default: a process this script did not start is
    # not one it can prove failed its own startup.
    [switch] $CloseCrashedLeftover,
    # An EXPLICIT .lic path, used INSTEAD of the registry resolution (User scope, else
    # Machine). Empty = resolve, which is the normal path. It exists for an install whose
    # licence is not in the registry at all, and so the expired-licence gate below can be
    # exercised against a scratch file without touching the machine's environment.
    [string] $LicenseFile        = '',
    [switch] $DryRun
)
$ErrorActionPreference = 'Stop'
# Computed HERE (before the startup banner reads it), not inside the argument gate below -
# a -FederationHoldSecs default of 900 must show as ON in the banner even before the gate runs.
$FederationHoldOn = ($FederationHoldSecs -gt 0)

# ---- -AppDataDir NORMALISED HERE, BEFORE ANY READER (STP-844 review item 2) ----
# A TRAILING BACKSLASH SILENTLY DISABLES THE RELOCATION. Both argument strings below are
# built as ('"{0}"' -f $AppDataDir) and handed to Start-Process -ArgumentList, so
# "...\appData\" renders as  --appDataDir "C:\...\appData\"  and the Microsoft C runtime
# reads that final \" as an ESCAPED QUOTE: vrfGui receives a mangled, unterminated argument
# and quietly falls back to the vendor ./appData. Tab completion in BOTH PowerShell and Git
# Bash appends that backslash. What makes it a false green rather than a visible failure is
# that every other consumer in this script - Test-Path, Join-Path, the STP-844 precheck -
# handles a trailing separator perfectly, so the launch log prints "[OK] vrfGui teardown
# prompts are OFF in <relocated path>" while the GUI is reading the vendor tree, the
# prompts are on, and the teardown hangs exactly as it did on D1. Trimmed ONCE, here,
# before $connDir, the -AppDataDir precondition, the precheck or the argument strings can
# read it, so all five agree on one value. A bare drive root survives the trim as "C:" and
# is refused in the argument gate below (it is drive-RELATIVE, not the root).
if (-not [string]::IsNullOrWhiteSpace($AppDataDir)) { $AppDataDir = $AppDataDir.TrimEnd('\', '/') }

function Say      { param([string]$m) Write-Host $m }
function Say-Head { param([string]$m) Write-Host ''; Write-Host ('=== ' + $m + ' ===') }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [DRY-RUN] would ' + $m) }

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

# ---- crashed-process helpers (used by the preconditions AND by the readiness verdict) ------
# Newest MAK callstack file whose LAST NAME FIELD is $ProcessId, written at or after $Since,
# or '' when there is none. Two filters, both load-bearing:
#   $Since  - C:\MAK\logs keeps callstacks across boots and Windows RECYCLES pids, so a file
#             older than the process being judged says nothing about it.
#   $NamePrefix - the directory is shared by the whole MAK toolchain, not just the sim: it
#             holds e.g. rtiAssistant5.0.1-20260903-194550-Legatus-281993-54616.callstack.log
#             (observed 2026-09-04). A pid-only match could therefore pin ANOTHER exe's crash
#             on this back-end, so the exe family is part of the match.
# Read-only, never throws.
function Get-CallstackFileForPid {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since = [datetime]::MinValue,
          [string]$NamePrefix = 'vrfSim')
    try {
        $cs = @(Get-ChildItem -LiteralPath $LogDir -Filter ($NamePrefix + ('*-{0}.callstack.log' -f $ProcessId)) -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $Since } |
                Sort-Object LastWriteTime -Descending)
        if ($cs.Count -gt 0) { return $cs[0].FullName }
    } catch { }
    return ''
}

# TRUE only for a process that is a CRASHED LEFTOVER: MAK wrote a callstack for its pid AND it
# is down to $MaxThreads or fewer threads. BOTH conditions, deliberately (header): a callstack
# alone can belong to a recycled pid, and a low thread count alone is also what a back-end
# merely BLOCKED on the RTI looks like (2-4 threads while present). A healthy 5.2d sim runs at
# 34-62 threads, so it can never satisfy the thread half. This is the ONLY predicate that may
# authorise closing a process this script did not start.
function Test-CrashedLeftover {
    param([int]$ProcessId, [int]$ThreadCount, [string]$LogDir,
          [datetime]$Since = [datetime]::MinValue, [int]$MaxThreads = 4)
    if ($ThreadCount -gt $MaxThreads) { return $false }
    return ((Get-CallstackFileForPid -ProcessId $ProcessId -LogDir $LogDir -Since $Since) -ne '')
}

# Close ONE named-checked back-end pid: the vendor's crash prompt first, Stop-Process only if
# the process survives it. The name re-check is the pid-recycling guard - between the verdict
# and this call the pid could belong to something else entirely, and this function must never
# be able to stop anything but a vrfSimHLA1516e. It takes a pid, never a name, so it CANNOT
# reach rtiexec / rtiForwarder / rtiAssistant even by mistake.
function Close-CrashedBackend {
    param([int]$ProcessId, [string]$ExpectedName = 'vrfSimHLA1516e',
          [int]$DialogConfirmSec = 20, [int]$ExitWaitSec = 15)
    $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $p) { Say-Ok ('  pid {0} is already gone - nothing to close.' -f $ProcessId); return }
    if ($p.Name -ne $ExpectedName) {
        Say-Warn ("  pid {0} is now '{1}', not '{2}' - the pid was recycled. NOT touched." -f $ProcessId, $p.Name, $ExpectedName)
        return
    }
    $answer = Join-Path $PSScriptRoot 'AnswerCrashDumpDialog.ps1'
    if (Test-Path -LiteralPath $answer) {
        Say ('  answering MAK''s crash-dump prompt first: AnswerCrashDumpDialog.ps1 -TargetPid {0} -ConfirmSec {1}' -f $ProcessId, $DialogConfirmSec)
        try {
            # 6>&1 as well as 2>&1: AnswerCrashDumpDialog.ps1 reports with Write-Host, whose
            # information stream a bare pipeline does not carry - without it its lines land
            # unprefixed in the middle of this script's output.
            & $answer -TargetPid $ProcessId -ConfirmSec $DialogConfirmSec 6>&1 2>&1 | ForEach-Object { Say ('         | ' + $_) }
            Say ('  AnswerCrashDumpDialog.ps1 exit {0} (1 = no prompt of the 5.0.2 form it matches; the 5.2 box is titled "Error <exe>" and is NOT answered by it - Stop-Process below is then the only exit)' -f $LASTEXITCODE)
        } catch { Say-Warn ('  AnswerCrashDumpDialog.ps1 failed: {0}' -f $_.Exception.Message) }
    } else {
        Say-Warn ('  {0} not found - skipping the crash-dump prompt step.' -f $answer)
    }
    if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
        Say-Ok ('  pid {0} exited after the crash-dump prompt was answered; no Stop-Process needed.' -f $ProcessId)
        return
    }
    Say-Warn ('  Stop-Process -Id {0} -Force: closing OUR OWN failed vrfSimHLA1516e (a process that failed its own startup, kept alive only by MAK''s crash handler). No other pid is touched.' -f $ProcessId)
    try { Stop-Process -Id $ProcessId -Force -ErrorAction Stop }
    catch { Say-Warn ('  Stop-Process failed: {0}. The pid remains and WILL block the next launch.' -f $_.Exception.Message); return }
    $wait = (Get-Date).AddSeconds($ExitWaitSec)
    while ((Get-Date) -lt $wait) {
        if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) { break }
        Start-Sleep -Seconds 1
    }
    if (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue) {
        Say-Fail ('  pid {0} is STILL present {1}s after Stop-Process - the next launch will be refused until it is gone.' -f $ProcessId, $ExitWaitSec)
    } else {
        Say-Ok ('  pid {0} closed; the pre-existing-process precondition will not block the next launch on its account.' -f $ProcessId)
    }
}

# ---- vendor-log HARVEST (replaces --logFileName; header, VENDOR LOG) -----------------------
# Newest VENDOR log written for $ProcessId at or after $Since, or '' when there is none. Same
# three filters as Get-CallstackFileForPid, and for the same reasons: the pid is the LAST name
# field (vrfSimHLA1516e5.2d-<date>-<time>-<host>-<build>-<pid>.log), C:\MAK\logs is shared by
# the whole MAK toolchain and keeps files across boots, and Windows recycles pids. The
# .callstack.log for the same pid does NOT match this filter (its last field is 'callstack',
# not the pid) and is excluded explicitly anyway - it is separate evidence with a separate
# life: it is the file that may be shared, and this one is not. Read-only, never throws.
function Get-VendorSimLogForPid {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since = [datetime]::MinValue,
          [string]$NamePrefix = 'vrfSim')
    try {
        $ls = @(Get-ChildItem -LiteralPath $LogDir -Filter ($NamePrefix + ('*-{0}.log' -f $ProcessId)) -File -ErrorAction SilentlyContinue |
                Where-Object { ($_.Name -notmatch '\.callstack\.log$') -and ($_.LastWriteTime -ge $Since) } |
                Sort-Object LastWriteTime -Descending)
        if ($ls.Count -gt 0) { return $ls[0].FullName }
    } catch { }
    return ''
}

# COPY (never move - the vendor may still be writing to it) that log to $Destination and say
# where it came from. Returns the destination on success, '' otherwise. A missing vendor log
# is a LOUD WARNING and nothing more: this function must never change the readiness verdict,
# which is decided by the thread-count oracle and the crash detector alone.
function Copy-VendorSimLog {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since, [string]$Destination,
          [string]$Occasion = 'READY')
    $src = Get-VendorSimLogForPid -ProcessId $ProcessId -LogDir $LogDir -Since $Since
    if (-not $src) {
        Say-Warn ('VENDOR LOG NOT FOUND for pid {0} in {1} (no vrfSim*-{0}.log written at or after {2:yyyy-MM-dd HH:mm:ss}). Nothing was copied to {3}. This does NOT change the verdict - but the run has no back-end log, so look in {1} by hand (the vendor stamps LOCAL time). --logFileName is deliberately NOT passed (PREREG_52_CRASH_BISECT_2026-09-04 sec 5: 6 crashes / 18 launches with it, 0 / 12 without).' -f `
            $ProcessId, $LogDir, $Since, $Destination)
        return ''
    }
    try {
        $dstDir = Split-Path -Parent $Destination
        if ($dstDir) { New-Item -ItemType Directory -Force -Path $dstDir | Out-Null }
        Copy-Item -LiteralPath $src -Destination $Destination -Force -ErrorAction Stop
    } catch {
        Say-Warn ('VENDOR LOG COPY FAILED ({0} -> {1}): {2}. The original is untouched; the verdict is unchanged.' -f $src, $Destination, $_.Exception.Message)
        return ''
    }
    $hasEnv = $false
    try { $hasEnv = [bool](Select-String -LiteralPath $Destination -SimpleMatch 'DtPrintEnvironmentVariables' -List -ErrorAction SilentlyContinue) } catch { }
    # ONE marker line, parsed by the runner (RunC2SimScenario Stage 3) into the manifest:
    # occasion is a single token, src runs to ' dst=', dst runs to end of line (paths may
    # contain spaces).
    Say-Ok ('VENDOR LOG HARVESTED occasion={0} src={1} dst={2}' -f $Occasion, $src, $Destination)
    Say ('         SNAPSHOT ONLY: taken at {0}; the sim keeps writing to {1}. Re-run the harvest (or copy that file again) for a later view.' -f $Occasion, $src)
    Say-Warn ('         SECRETS: this copy carries the FULL PROCESS ENVIRONMENT IN CLEARTEXT{0} (DtPrintEnvironmentVariables at --notifyLevel 3; FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10). NEVER attach it to a ticket, mail or issue - send the .callstack.log / .dmp instead. It is not scrubbed, by decision.' -f `
        $(if ($hasEnv) { ' - DtPrintEnvironmentVariables IS PRESENT in this copy' } else { ' (DtPrintEnvironmentVariables not found in this copy - assume it is there anyway)' }))
    return $Destination
}

# ---- STP-825 federation holder helpers (same posture as the runner's Stage 2h) --------
# The rtiexec this holder must join may already be running from an EARLIER script or an
# earlier demo (StartRtiExec52.ps1's own header: it persists across runs by design) - this
# script did not necessarily start it, so it does not know its pid or log path the way the
# runner does right after running StartRtiExec52 itself. Discover it the same way
# StartRtiExec52.ps1's own inventory does: an rtiexec.exe process rooted under -RtiDir\bin.
function Get-ServingRtiExecPid {
    param([string]$RtiBinDir)
    foreach ($p in @(Get-Process -Name 'rtiexec' -ErrorAction SilentlyContinue)) {
        $path = ''
        try { $path = $p.Path } catch { }
        if ([string]::IsNullOrWhiteSpace($path) -or $path.StartsWith($RtiBinDir, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $p.Id
        }
    }
    return $null
}

# Newest rtiexec_*-<pid>.log in $LogDir for the serving pid. The vendor's own rtiexec.exe
# APPENDS "-<pid>.log" to whatever base name -l was given, regardless of that base name -
# verified against runs\launch52\rtiexec_20260915T155028Z5.0.1-...-75168.log. '' when none
# is found - the caller then falls back to the holder's own stdout.
function Get-RtiExecLogForPid {
    param([string]$LogDir, $RtiExecPid)
    if (-not $RtiExecPid -or -not $LogDir -or -not (Test-Path -LiteralPath $LogDir -PathType Container)) { return '' }
    $f = @(Get-ChildItem -LiteralPath $LogDir -Filter ('rtiexec_*-{0}.log' -f $RtiExecPid) -ErrorAction SilentlyContinue |
           Sort-Object LastWriteTimeUtc -Descending) | Select-Object -First 1
    if ($f) { return $f.FullName }
    return ''
}

# Read a live, growing log from a byte OFFSET (RunC2SimScenario.ps1's Read-TextFromOffset,
# reused verbatim): the rtiexec log runs to tens of thousands of lines and this is polled
# once a second. FileShare ReadWrite because the rtiexec holds it open for writing.
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

# The holder's own stdout, read live (RunC2SimScenario.ps1's Read-LiveText, reused
# verbatim): RtiProbe prints "created/joined" only AFTER the whole hold, so this is the
# documented fallback when the rtiexec log path is unknown - it can only confirm a hold
# SHORTER than the 45s wait.
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

# Federation identity the holder must create-or-join: the connection config's own execName
# (tools/Shared/StackIdentity.cs reads it from there when the tools are given no federation
# argument - the same reasoning as the runner's Stage 2h). Falls back to the literal
# 'MAK-ONE-2025' when the config cannot be read or parsed.
function Get-FederationHoldName {
    param([string]$ConnConfigFile)
    if (Test-Path -LiteralPath $ConnConfigFile -PathType Leaf) {
        try {
            $m = [regex]::Match((Get-Content -LiteralPath $ConnConfigFile -Raw -Encoding UTF8), '<execName\s+value\s*=\s*"([^"]+)"')
            if ($m.Success -and -not [string]::IsNullOrWhiteSpace($m.Groups[1].Value)) {
                return [pscustomobject]@{ Name = $m.Groups[1].Value; Source = ('execName from {0}' -f $ConnConfigFile) }
            }
        } catch {
            return [pscustomobject]@{ Name = 'MAK-ONE-2025'; Source = ('FALLBACK literal - {0} could not be read: {1}' -f $ConnConfigFile, $_.Exception.Message) }
        }
    }
    return [pscustomobject]@{ Name = 'MAK-ONE-2025'; Source = ('FALLBACK literal - execName could not be read from {0}' -f $ConnConfigFile) }
}

$modeTag = if ($DryRun) { 'DRY-RUN' } else { 'LIVE' }
$scenarioDisplay = if ([string]::IsNullOrWhiteSpace($Scenario)) { '(none - -L omitted)' } else { $Scenario }
Say-Head "LaunchVrf52.ps1 ($modeTag) - VR-Forces 5.2d INDEPENDENT launch (UG52 4.1.2)"
Say ("  VrfRoot           : {0}" -f $VrfRoot)
Say ("  VrLinkRoot        : {0}" -f $VrLinkRoot)
Say ("  RtiDir            : {0}" -f $RtiDir)
Say ("  Scenario          : {0}" -f $scenarioDisplay)
Say ("  Site / Session    : {0} / {1}" -f $SiteId, $SessionId)
Say ("  Back-end appNumber: {0}" -f $BackendAppNumber)
Say ("  Front-end appNo   : {0}{1}" -f $FrontendAppNumber, $(if ($NoGui) { ' (-NoGui: not launched)' } else { '' }))
Say ("  DeviceAddress     : {0}" -f $(if ([string]::IsNullOrWhiteSpace($DeviceAddress)) { '(empty - --deviceAddress/--hostAddressString NOT passed; VR-Forces picks the first device listed)' } else { $DeviceAddress }))
Say ("  MakLogDir         : {0} (startup-crash callstacks AND the vendor's own sim log)" -f $MakLogDir)
Say ("  Federation hold   : {0}" -f $(if ($FederationHoldOn) { ("STP-825 holder ON - appNumber {0} (retry {1}), hold {2}s" -f $FederationHoldAppNumber, ($FederationHoldAppNumber + 1), $FederationHoldSecs) } else { "OFF (-FederationHoldSecs 0) - this launch's own back end will be the federation CREATOR, the STP-825 failure mode" }))
Say ("  --logFileName     : {0}" -f $(if ([string]::IsNullOrWhiteSpace($LogFileName)) {
        'NOT PASSED (the default). PREREG_52_CRASH_BISECT_2026-09-04 sec 5: passing it crashed the sim at startup 6 times in 18 launches (~1 in 3), omitting it 0 in 12, p = 0.031 - and a short vendor-default path crashed too, so it is the OPTION, not the path. Do not re-enable it casually.'
    } else { ('PASSED DELIBERATELY -> {0}. THAT IS A ~1-IN-3 STARTUP CRASH (PREREG_52_CRASH_BISECT_2026-09-04 sec 5); only a bisect repeat or a vendor bug report should be doing this.' -f $LogFileName) }))
Say ("  vendor log harvest: {0}\vrfSim*-<pid>.log for THIS pid is COPIED (snapshot at READY / at a startup crash) to the -LogFile path reported below" -f $MakLogDir)
Say-Warn ("  SECRETS: that harvested copy holds the FULL PROCESS ENVIRONMENT IN CLEARTEXT (DtPrintEnvironmentVariables at --notifyLevel 3; FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10). NEVER attach it to a ticket, mail or issue - send the .callstack.log / .dmp instead.")
Say ("  Crashed processes : own pid on a startup crash -> {0}; pre-existing crashed leftover -> {1}" -f `
    $(if ($LeaveCrashedProcess) { 'LEFT RUNNING (-LeaveCrashedProcess; it will block the next launch)' } else { 'CLOSED before exit 3' }), `
    $(if ($CloseCrashedLeftover) { 'CLOSED if it has a callstack AND <= 4 threads (-CloseCrashedLeftover)' } else { 'left alone (refuses the launch; -CloseCrashedLeftover closes it)' }))

# ---- argument gate (hard, checked first, nothing launched on failure) ------
$appNoFail = $false
# A malformed -DeviceAddress would go straight onto the sim's command line, and an empty
# -MakLogDir would silently disarm the startup-crash detector. Both are exit 2.
if ((-not [string]::IsNullOrWhiteSpace($DeviceAddress)) -and ($DeviceAddress -notmatch '^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$')) {
    Say-Fail ("-DeviceAddress must be a dotted IPv4 address or EMPTY (empty = do not pass --deviceAddress/--hostAddressString); got '{0}'." -f $DeviceAddress)
    $appNoFail = $true
}
if ([string]::IsNullOrWhiteSpace($MakLogDir)) {
    Say-Fail '-MakLogDir must not be empty: it is where MAK writes the startup-crash callstack this script watches for. Pass the real directory (default C:\MAK\logs).'
    $appNoFail = $true
}
if ($BackendAppNumber -le 0) {
    Say-Fail 'MISSING -BackendAppNumber. MANDATORY (no default). Take the NEXT FREE value from OPUS_EXECUTION_PLAN.md Appendix B and ledger it BEFORE launching.'
    $appNoFail = $true
}
if ((-not $NoGui) -and ($FrontendAppNumber -le 0)) {
    Say-Fail 'MISSING -FrontendAppNumber. MANDATORY unless -NoGui. Ledger it BEFORE launching.'
    $appNoFail = $true
}
if ((-not $NoGui) -and ($BackendAppNumber -gt 0) -and ($BackendAppNumber -eq $FrontendAppNumber)) {
    Say-Fail ('-BackendAppNumber and -FrontendAppNumber are IDENTICAL ({0}). Each join consumes its own number.' -f $BackendAppNumber)
    $appNoFail = $true
}
# STP-825 federation holder arguments (same bounds as the runner's own Stage 2h gate).
# $FederationHoldOn itself is computed earlier, right after $ErrorActionPreference, so the
# startup banner above can already report it correctly.
if ($FederationHoldSecs -lt 0 -or $FederationHoldSecs -gt 86400) {
    Say-Fail ("-FederationHoldSecs must be 0..86400 (got {0}). 0 = no holder (pre-STP-825 behaviour; what the runner passes at its Stage 3 call site since its own Stage 2h already holds the federation)." -f $FederationHoldSecs)
    $appNoFail = $true
}
if ($FederationHoldOn) {
    if ($FederationHoldAppNumber -le 0) {
        Say-Fail 'MISSING/invalid -FederationHoldAppNumber. MANDATORY (> 0) whenever -FederationHoldSecs is positive - the holder is a federate and needs its own appNumber, ledgered like any other join.'
        $appNoFail = $true
    } elseif (($FederationHoldAppNumber -eq $BackendAppNumber) -or ((-not $NoGui) -and ($FederationHoldAppNumber -eq $FrontendAppNumber)) -or (($FederationHoldAppNumber + 1) -eq $BackendAppNumber) -or ((-not $NoGui) -and (($FederationHoldAppNumber + 1) -eq $FrontendAppNumber))) {
        Say-Fail ("-FederationHoldAppNumber {0} (or its retry number {1}) COLLIDES with -BackendAppNumber/-FrontendAppNumber. Each join consumes its own number." -f $FederationHoldAppNumber, ($FederationHoldAppNumber + 1))
        $appNoFail = $true
    }
}
# A BARE DRIVE ROOT survives the trim at the top of this script as "C:", which is a
# DRIVE-RELATIVE path meaning "the current directory on C:", not "C:\". Refuse it rather
# than let Join-Path and --appDataDir resolve it against whatever the cwd happens to be.
# Judged HERE, with the other argument failures, and before the first reader ($connDir).
if ((-not [string]::IsNullOrWhiteSpace($AppDataDir)) -and ($AppDataDir -match '^[A-Za-z]:$')) {
    Say-Fail ("-AppDataDir is a bare drive root ('{0}:\'), which after normalisation is the DRIVE-RELATIVE path '{0}:'. Pass the appData DIRECTORY itself, e.g. C:\C2SIM\vrf-appdata-unattended\appData." -f $AppDataDir.Substring(0, 1))
    $appNoFail = $true
}
if ($appNoFail) { Say-Head 'Result'; Say-Fail 'Aborting: argument gate failed. NOTHING was launched.'; exit 2 }

# ---- licence gate (resolve from the registry, then REFUSE an expired one) ---
# The licence is pinned onto this process here, so the sim and the gui inherit the file the
# REGISTRY names rather than whatever this process tree started with (RUNBOOK 0.5.15).
# An expired licence does not announce itself: the back-end starts, fails its checkout and
# dies in a way that reads exactly like the ~1-in-3 startup crash. So it is refused HERE,
# in the same place and with the same exit code as the argument gate - before any path,
# process, log or app number is spent, in -DryRun as in a live launch.
$LicInfo = Resolve-MakLicenseFile -PathOverride $LicenseFile
if ($LicInfo.Expiry -and ($LicInfo.Expiry.Date -lt (Get-Date).Date)) {
    Say-Head 'Result'
    Say-Fail ('THE MAK LICENCE EXPIRED ON {0}: {1}' -f $LicInfo.ExpiryText, $LicInfo.Path)
    Say-Fail ('  Resolved from {0}. Renew it, point MAKLMGRD_LICENSE_FILE at the new .lic in BOTH the User and the Machine scope (RUNBOOK 0.5.15), then retry.' -f $LicInfo.Source)
    Say-Fail 'Aborting: licence gate failed. NOTHING was launched.'
    exit 2
}

# ---- derived paths ---------------------------------------------------------
$bin64      = Join-Path $VrfRoot 'bin64'
$simExe     = Join-Path $bin64 'vrfSimHLA1516e.exe'
$guiExe     = Join-Path $bin64 'vrfGui.exe'
$vrlBin     = Join-Path $VrLinkRoot 'bin64'
$rtiBin     = Join-Path $RtiDir 'bin'
$repoRootEarly = Split-Path -Parent $PSScriptRoot
$ridFile    = if ($UseRtiAssistant) { Join-Path $RtiDir 'rid.mtl' }
              elseif ([string]::IsNullOrWhiteSpace($RidFile)) { Join-Path $repoRootEarly 'config\rid-501-rtiexec-min.mtl' }
              else { $RidFile }
# With -AppDataDir the sim's DEFAULT exercise-connection config comes from THERE
# (./appData/settings/connections relative to the appData dir, UG52 4.1.2), so the
# precondition must check the file the sim will actually open, not the vendor copy.
$connDir    = if ([string]::IsNullOrWhiteSpace($AppDataDir)) { Join-Path $VrfRoot 'appData\settings\connections' }
              else { Join-Path $AppDataDir 'settings\connections' }
$connFile   = if ([string]::IsNullOrWhiteSpace($ExConnConfigFile)) { Join-Path $connDir 'MAK-ONE-2025-Config.xml' } else { $ExConnConfigFile }
$scenarioRel = ''
$scenarioAbs = ''
if (-not [string]::IsNullOrWhiteSpace($Scenario)) {
    $scenarioRel = '../userData/scenarios/' + ($Scenario -replace '\\', '/') + '.scnx'
    $scenarioAbs = Join-Path $VrfRoot ('userData\scenarios\{0}.scnx' -f $Scenario)
}
$repoRoot   = Split-Path -Parent $PSScriptRoot
# STP-825 federation holder (see -FederationHoldSecs above): the SAME RtiProbe.exe the
# runner's own Stage 2h uses, from this script's own repo root. LaunchVrf52 is 5.2-only, so
# the bridge output tree is always Release-5.2 (RUNBOOK sec 9 - eleven 5.2 consumers, one
# hash).
$ExeRtiProbe   = Join-Path $repoRoot 'tools\RtiProbe\bin\Release-5.2\net10.0\win-x64\RtiProbe.exe'
# Where the rtiexec this script's holder must join writes its log - the SAME directory
# StartRtiExec52.ps1 and the runner use (runs\launch52, gitignored, never under C:\MAK): the
# rtiexec OUTLIVES every script that touches it, so its log is not filed under any one
# script's own output.
$RtiExecLogDir = Join-Path $repoRoot 'runs\launch52'
if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $stamp   = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $LogFile = Join-Path $repoRoot ('runs\launch52\vrfSim_{0}_{1}.log' -f $BackendAppNumber, $stamp)
}

$procBackend  = 'vrfSimHLA1516e'
$procFrontend = 'vrfGui'
# Thread ceiling for the crashed-leftover classifier (Test-CrashedLeftover). 4, not
# $BackendMinThreads (8): the two answer different questions. 8 is the HEALTH floor - above it
# the sim is serviceable. 4 is the CORPSE ceiling - the crashed process observed on 2026-09-04
# sat at 0 threads, and a back-end merely blocked on the RTI sits at 2-4. The gap 5..8 is
# deliberately claimed by NEITHER: a process in it is neither healthy nor provably dead, so it
# is reported and left alone.
$CrashedLeftoverMaxThreads = 4

# ---- PRECONDITIONS (read-only; run in both DryRun and live) ----------------
Say-Head 'Preconditions'
$hardFail = $false
foreach ($chk in @(
    @{ p=$simExe;   what='5.2d back-end vrfSimHLA1516e.exe' },
    @{ p=$guiExe;   what='5.2d front-end vrfGui.exe' },
    @{ p=(Join-Path $vrlBin 'vlHLA1516e.dll'); what='VR-Link 5.10 bin64 (vlHLA1516e.dll)' },
    @{ p=$rtiBin;   what='RTI bin dir' },
    @{ p=$ridFile;  what='RTI rid.mtl (per-process RTI_RID_FILE target)' },
    @{ p=$connFile; what='exercise connection config (MAK-ONE-YYYY-Config.xml)' }
)) {
    if (Test-Path -LiteralPath $chk.p) { Say-Ok ("{0}: {1}" -f $chk.what, $chk.p) }
    else { Say-Fail ("{0} MISSING: {1}" -f $chk.what, $chk.p); $hardFail = $true }
}
if ($scenarioAbs) {
    if (Test-Path -LiteralPath $scenarioAbs) { Say-Ok ("scenario file: {0}" -f $scenarioAbs) }
    else { Say-Fail ("scenario file MISSING: {0}" -f $scenarioAbs); $hardFail = $true }
}
# STP-825 federation holder tool - reported here, SOFT (like CreateOne elsewhere in this
# repo): a missing build must not block the REST of this script's dry-run plan or its other,
# unrelated preconditions. A LIVE launch still refuses cleanly on this: the holder-start block
# below (LIVE section) tries to start $ExeRtiProbe, catches the failure, retries once, and - if
# it never joins - exits 3 naming STP-825, exactly as a rejected create would. Dry run always
# prints the holder plan on its own merits; it does not depend on the file existing.
if ($FederationHoldOn) {
    if (Test-Path -LiteralPath $ExeRtiProbe -PathType Leaf) { Say-Ok ("STP-825 federation holder tool: {0}" -f $ExeRtiProbe) }
    else {
        Say-Warn ("STP-825 federation holder tool MISSING: {0} (build it: dotnet build tools\RtiProbe\RtiProbe.csproj -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild). NOT a hard precondition here - a LIVE launch will still refuse cleanly (exit 3, naming STP-825) when the holder cannot be started, rather than blocking this script's other preconditions on it. Pass -FederationHoldSecs 0 to run without the holder (pre-STP-825 behaviour, which is the failure mode this holder exists to avoid)." -f $ExeRtiProbe)
    }
}
$logDir = Split-Path -Parent $LogFile
if ($logDir -like 'C:\MAK*') { Say-Fail ("log file would land under C:\MAK ({0}) - refused; pass -LogFile outside the vendor tree." -f $LogFile); $hardFail = $true }
else { Say-Ok ("back-end log (HARVEST DESTINATION - the vendor's own log for this pid is copied here; --logFileName is not passed): {0}" -f $LogFile) }

# Relocated appData (--appDataDir). Not given = vendor default, nothing to check.
if (-not [string]::IsNullOrWhiteSpace($AppDataDir)) {
    if (Test-Path -LiteralPath $AppDataDir -PathType Container) {
        Say-Ok ("relocated appData (--appDataDir): {0}" -f $AppDataDir)
        $appDataMtl = Join-Path $AppDataDir 'settings\vrfSim\vrfSim.mtl'
        if (Test-Path -LiteralPath $appDataMtl) {
            # Echo the setting this relocation exists for, so the run log records which way it was set.
            $navLine = @(Get-Content -LiteralPath $appDataMtl | Where-Object { $_ -match '^\(setqb\s+loadAllNavigationDataOnTerrainLoad\s' })
            if ($navLine.Count -eq 1) { Say ('         ' + $navLine[0].Trim() + '   (1 = nav data loads WITH the scenario, UG52 App. C p1671)') }
            else { Say-Warn ('  settings\vrfSim\vrfSim.mtl has {0} loadAllNavigationDataOnTerrainLoad lines - expected 1.' -f $navLine.Count) }
        } else {
            Say-Warn ('  {0} has no settings\vrfSim\vrfSim.mtl - pass the directory that CONTAINS settings\, e.g. C:\C2SIM\vrf-appdata\appData, not its parent.' -f $AppDataDir)
        }
    } else {
        Say-Fail ('-AppDataDir is MISSING or is not a directory: {0}' -f $AppDataDir)
        $hardFail = $true
    }
} else { Say-Ok ('appData: vendor default (-AppDataDir not given; the sim uses ./appData = ' + (Join-Path $VrfRoot 'appData') + ')') }

# THE GUI'S TWO TEARDOWN MODALS, CHECKED BEFORE THE LAUNCH, NOT AFTER IT (STP-844).
# Only matters when a front end is actually started: a headless run raises no dialog and
# 79/79 headless 5.2 teardowns were clean. GUI-on is the new variable - demo rehearsal D1
# (run 20260920T172141Z) was the first ever, and its teardown timed out on two stacked
# never-ask-again message boxes that nothing was there to answer. Both have a persisted
# setting in the appData tree THIS launch is about to hand the GUI, so the one moment
# where saying so is cheap is here, before anything starts. REPORT, NEVER REFUSE: a
# GUI-on launch with the prompts ON is perfectly valid when a human is at the keyboard,
# and this script cannot know which kind of run this is. The keys, the vendor citations
# and the remedy live in RunnerLib.ps1's "THE TWO 5.2 GUI TEARDOWN MODALS" block.
if (-not $NoGui) {
    $effAppData = $(if ([string]::IsNullOrWhiteSpace($AppDataDir)) { Join-Path $VrfRoot 'appData' } else { $AppDataDir })
    $guiAppFile  = Join-Path $effAppData 'settings\vrfGui\default_Application.apsx'
    $guiSessFile = Join-Path $effAppData 'settings\vrfGui\default_SessionSettings.srsx'
    $guiAppText  = ''
    $guiSessText = ''
    # THE READS ARE INSIDE THE try, NOT BEFORE IT (STP-844 review item 3). This script sets
    # $ErrorActionPreference = 'Stop' and has NO outer try/catch, so a Get-Content failure
    # at script scope - an ACL, a sharing violation, a delete between Test-Path and the
    # read, a path that trips the provider - is a TERMINATING error that would abort a LIVE
    # LAUNCH. That directly contradicts this block's own contract: an advisory line must
    # never be able to fail a launch. Everything the precheck touches now lives in here.
    # RunnerLib.ps1 is dot-sourced INSIDE A CHILD SCOPE (& { ... }) on purpose. It opens
    # with Set-StrictMode -Version Latest, and this script - unlike the runner - has never
    # run under StrictMode; dot-sourcing it at script scope would turn StrictMode on for
    # the whole of a LIVE LAUNCH, where an unset variable anywhere downstream becomes a
    # terminating error mid-flight. Set-StrictMode applies to the scope it is called in and
    # its children, so inside & { } it dies with the block. The whole read is also wrapped
    # in try/catch: an advisory line must never be able to fail a launch.
    $guiPrompts = $null
    try {
        if (Test-Path -LiteralPath $guiAppFile  -PathType Leaf) { $guiAppText  = (Get-Content -LiteralPath $guiAppFile  -Raw) }
        if (Test-Path -LiteralPath $guiSessFile -PathType Leaf) { $guiSessText = (Get-Content -LiteralPath $guiSessFile -Raw) }
        $guiPrompts = & {
            param($libPath, $appXml, $sessXml)
            . $libPath
            Get-VrfGuiPromptSettings -ApplicationXml $appXml -SessionSettingsXml $sessXml
        } (Join-Path $PSScriptRoot 'RunnerLib.ps1') $guiAppText $guiSessText
    } catch {
        Say-Warn ('could not read the vrfGui teardown-prompt settings under {0}: {1} (advisory only - the launch is not affected).' -f $effAppData, $_.Exception.Message)
    }
    if ($null -eq $guiPrompts) {
        # nothing more to say; the catch above already reported it
    } elseif ($guiPrompts.Unattended) {
        Say-Ok ('vrfGui teardown prompts are OFF in {0} ({1}) - an unattended StopVrf52 can close this front end.' -f $effAppData, $guiPrompts.Summary)
    } else {
        Say-Warn ('vrfGui TEARDOWN PROMPTS ARE ON in {0}: {1}' -f $effAppData, $guiPrompts.Summary)
        Say-Warn ('  STP-844: with myShowQuitDialogOnClose=1 the GUI raises the UG52 4.6 exit prompt ("Are You Sure?" / "Quit VR-Forces GUI") on the WM_CLOSE that StopVrf52.ps1 sends, and nothing answers it - the teardown then burns its whole budget and leaves a JOINED vrfGui behind (exit 3, runner exit 4). This is exactly what run 20260920T172141Z did.')
        Say-Warn ('  FIX (vendor configuration, UG52 4.6.1 + 4.3.1, nothing under C:\MAK is touched):  pwsh -File scripts\NewVrfAppData52.ps1 -Dest C:\C2SIM\vrf-appdata-unattended   then relaunch with  -AppDataDir C:\C2SIM\vrf-appdata-unattended\appData')
        Say-Warn '  NOT A REFUSAL: launching anyway is correct for an INTERACTIVE session, where a human answers the prompt. It is wrong for an unattended run.'
    }
}

# Mixed-RTI environment report (Machine scope, informational - overridden per process)
$mRti = [Environment]::GetEnvironmentVariable('MAK_RTIDIR','Machine')
$mRid = [Environment]::GetEnvironmentVariable('RTI_RID_FILE','Machine')
if ($mRti -and ($mRti -ne $RtiDir)) { Say-Warn ("Machine MAK_RTIDIR={0} differs from -RtiDir {1}; overriding MAK_RTIDIR and RTI_RID_FILE for the launched processes only." -f $mRti, $RtiDir) }
else { Say-Ok ("Machine MAK_RTIDIR={0}" -f $(if ($mRti) { $mRti } else { '(unset)' })) }
if ($mRid) { Say ("         Machine RTI_RID_FILE={0}" -f $mRid) }

# Stale VR-Forces processes (federates). rtiexec/rtiForwarder/rtiAssistant are RTI
# infrastructure: reported, never refused on, NEVER killed.
$vrfProcNames = @('vrfLauncher',$procBackend,$procFrontend)
$existing = @()
foreach ($n in $vrfProcNames) {
    $p = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($p) { $existing += ($p | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($existing.Count -gt 0) {
    Say-Warn ("VR-Forces processes ALREADY running: {0}" -f ($existing -join ', '))
    # A pre-existing back-end is not necessarily a RUNNING sim. On 2026-09-04 (launch_3860 ->
    # launch_3862) the previous launch's back-end had crashed at startup and MAK's handler kept
    # the corpse alive at 0 threads, titled 'Error vrfSimHLA1516e.exe'; this precondition then
    # refused the retry with exit 2. Say what it is, and close it ONLY under
    # -CloseCrashedLeftover and ONLY when both leftover conditions hold.
    foreach ($bp in @(Get-Process -Name $procBackend -ErrorAction SilentlyContinue)) {
        # An UNREADABLE thread count must never be read as 0: that is the corpse signature, and
        # inventing it would let this script close a process it knows nothing about.
        $bThr = 0; $bThrOk = $false
        try { $bThr = $bp.Threads.Count; $bThrOk = $true } catch { }
        $bStart = [datetime]::MinValue; try { $bStart = $bp.StartTime }    catch { }
        $bTitle = '';                   try { $bTitle = $bp.MainWindowTitle } catch { }
        $bCs = Get-CallstackFileForPid -ProcessId $bp.Id -LogDir $MakLogDir -Since $bStart
        if ($bThrOk -and (Test-CrashedLeftover -ProcessId $bp.Id -ThreadCount $bThr -LogDir $MakLogDir -Since $bStart -MaxThreads $CrashedLeftoverMaxThreads)) {
            Say-Fail ("  pid {0} is a CRASHED LEFTOVER, not a running sim: {1} thread(s) (<= {2}) AND MAK wrote a callstack for that pid ({3}){4}. MAK's crash handler keeps a faulted process alive, so it goes on blocking launches until something closes it." -f `
                $bp.Id, $bThr, $CrashedLeftoverMaxThreads, $bCs, $(if ($bTitle) { ", window title '$bTitle'" } else { '' }))
            Say-Fail ('  LaunchVrf52 closes a crashed back-end automatically ONLY on the launch that DETECTED the crash - the pid it started itself. This pid predates this launch, so it is left alone by default. Close it by hand (Stop-Process -Id {0} -Force) or rerun with -CloseCrashedLeftover, which closes ONLY a pre-existing vrfSimHLA1516e whose pid HAS a callstack file AND has <= {1} threads (both, so a healthy 34-62 thread sim can never match, and neither can an RTI process - only vrfSimHLA1516e pids are ever considered).' -f $bp.Id, $CrashedLeftoverMaxThreads)
            if ($CloseCrashedLeftover) {
                if ($DryRun) { Say-Plan ('close crashed leftover pid {0} (-CloseCrashedLeftover): AnswerCrashDumpDialog.ps1 then Stop-Process on that pid only.' -f $bp.Id) }
                else {
                    Say-Warn ('  -CloseCrashedLeftover: closing pid {0} now.' -f $bp.Id)
                    Close-CrashedBackend -ProcessId $bp.Id -ExpectedName $procBackend
                }
            }
        } else {
            Say-Warn ('  pid {0} is NOT classified as a crashed leftover (threads: {1}; callstack for this pid since its start: {2}) - it is left alone even with -CloseCrashedLeftover. Only a process failing BOTH tests may be closed.' -f `
                $bp.Id, $(if ($bThrOk) { $bThr } else { 'UNREADABLE - treated as not-a-corpse' }), $(if ($bCs) { $bCs } else { 'none' }))
        }
    }
    # Re-inventory: only what is STILL running can refuse this launch.
    $existing = @()
    foreach ($n in $vrfProcNames) {
        $p = Get-Process -Name $n -ErrorAction SilentlyContinue
        if ($p) { $existing += ($p | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
    }
}
if ($existing.Count -gt 0) {
    if (-not $AllowExistingVrf) { Say-Fail ('  Refusing to launch on top of existing VR-Forces processes ({0}) (-AllowExistingVrf overrides deliberately).' -f ($existing -join ', ')); $hardFail = $true }
    else { Say-Warn '  -AllowExistingVrf set: proceeding despite existing processes.' }
} else { Say-Ok 'no pre-existing vrfLauncher / vrfSimHLA1516e / vrfGui processes' }
$infra = @()
foreach ($n in @('rtiexec','rtiForwarder')) {
    $ip = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($ip) { $infra += ($ip | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($infra.Count -gt 0) { Say-Ok ("RTI infrastructure present (expected; do NOT kill): {0}" -f ($infra -join ', ')) }
else {
    # NOT a hard failure here (this script does not own the rendezvous), but under the
    # rtiexec posture an absent rtiexec means the sim has nothing to rendezvous with and
    # the federation degrades to the lightweight mode UG52 5.5.1 p190 prohibits.
    Say-Warn 'no rtiexec / rtiForwarder is running. The 5.2 posture is RTIEXEC mode (UG52 5.5.1 p190): run scripts\StartRtiExec52.ps1 first, or use the runner, whose Stage 2r does it. Never kill one that IS running.'
}
$assist = Get-Process -Name 'rtiAssistant' -ErrorAction SilentlyContinue
if (-not $UseRtiAssistant) {
    if ($assist) {
        $ids = ($assist | ForEach-Object { $_.Id }) -join ', '
        Say-Ok ("assistant-free mode (RTI_ASSISTANT_DISABLE + rid-configured rtiexec connection): existing rtiAssistant pid(s) {0} are IGNORED by the launched processes (an assistant of the wrong version rejects the federate outright, and an ELEVATED one cannot be clicked from here - the reasons this mode is the default). Never kill them." -f $ids)
    } else {
        Say-Ok 'assistant-free mode: no rtiAssistant running and none will be spawned or consulted (RTI_ASSISTANT_DISABLE, RTI Ref Manual 5.2.10). No Choose RTI Connection dialog can occur.'
    }
} elseif ($assist) {
    foreach ($a in $assist) {
        $t = if ([string]::IsNullOrWhiteSpace($a.MainWindowTitle)) { '(no window title - ALSO the signature of an ELEVATED assistant, whose windows/dialogs this script cannot see or click)' } else { $a.MainWindowTitle }
        $started = try { $a.StartTime.ToString('yyyy-MM-dd HH:mm:ss') } catch { '(start time inaccessible)' }
        Say-Warn ("-UseRtiAssistant: pre-existing rtiAssistant pid {0} started {1} - window: {2}. A 5.0.1 assistant version-rejects 4.6.1 federates ('RTI component was using a different RTI version', observed 2026-09-03); observe, never kill." -f $a.Id, $started, $t)
        if ($a.MainWindowTitle -match 'Choose RTI Connection') {
            if ($IgnoreUnansweredRtiAssistant) { Say-Warn '  -IgnoreUnansweredRtiAssistant set: proceeding.' }
            else { Say-Fail ("  pid {0} sits on the UNANSWERED 'Choose RTI Connection' dialog. Back-ends WILL block behind it. Run scripts/AnswerRtiDialog.ps1 first." -f $a.Id); $hardFail = $true }
        }
    }
} else {
    Say-Warn '-UseRtiAssistant with NO pre-existing rtiAssistant: the first federate spawns one that PROMPTS (Choose RTI Connection); the back-end blocks until it is answered (AnswerRtiDialog.ps1 - cannot click an ELEVATED assistant).'
}
if ($LicInfo.Exists) {
    Say-Ok ("MAKLMGRD_LICENSE_FILE = {0} (expires {1}; resolved at the licence gate above)" -f $LicInfo.Path, $LicInfo.ExpiryText)
} elseif (-not [string]::IsNullOrWhiteSpace($env:MAKLMGRD_LICENSE_FILE)) {
    Say-Warn ("the licence gate resolved NO usable file - the INHERITED process value ({0}) is what the sim will use. RUNBOOK 0.5.15." -f $env:MAKLMGRD_LICENSE_FILE)
} else {
    Say-Warn 'MAKLMGRD_LICENSE_FILE empty in the registry AND in this process - licence checkout may hang. RUNBOOK 0.5.15.'
}

# ---- argument strings (UG52 4.1.2 / 4.1.3 / Table 11 / Table 12) ------------
$simArgs = @('--siteId', $SiteId, '--appNumber', $BackendAppNumber, '--sessionId', $SessionId,
             '--notifyLevel', $NotifyLevel)
# --logFileName ONLY when -LogFileName was passed on purpose. Default EMPTY = absent from the
# command line: with it the sim crashed at startup 6 times in 18 launches, without it 0 in 12
# (PREREG_52_CRASH_BISECT_2026-09-04 sec 5, p = 0.031; a short vendor-default path crashed too,
# so the path is not the trigger). The vendor writes its own log to $MakLogDir either way and
# this script harvests it - see Copy-VendorSimLog.
if (-not [string]::IsNullOrWhiteSpace($LogFileName)) { $simArgs += @('--logFileName', ('"{0}"' -f $LogFileName)) }
if ($scenarioRel) { $simArgs += @('--scenarioFileName', ('"{0}"' -f $scenarioRel)) }
if (-not [string]::IsNullOrWhiteSpace($ExConnConfigFile)) { $simArgs += @('--exConnConfigFile', ('"{0}"' -f $ExConnConfigFile)) }
if ($QuietBackend) { $simArgs += '--doNotUseConsole' }
if (-not [string]::IsNullOrWhiteSpace($DeviceAddress)) { $simArgs += @('--deviceAddress', $DeviceAddress, '--hostAddressString', $DeviceAddress) }
# --appDataDir on BOTH executables (UG52 Table 11 p178 / Table 10 p164) so the front end
# reads the same settings tree as the back end. Absent unless -AppDataDir was given.
if (-not [string]::IsNullOrWhiteSpace($AppDataDir)) { $simArgs += @('--appDataDir', ('"{0}"' -f $AppDataDir)) }
$guiArgs = @('--siteId', $SiteId, '--appNumber', $FrontendAppNumber, '--sessionId', $SessionId, '--hla1516e')
if (-not [string]::IsNullOrWhiteSpace($ExConnConfigFile)) { $guiArgs += @('--exConnConfigFile', ('"{0}"' -f $ExConnConfigFile)) }
if (-not [string]::IsNullOrWhiteSpace($DeviceAddress)) { $guiArgs += @('--deviceAddress', $DeviceAddress, '--hostAddressString', $DeviceAddress) }
if (-not [string]::IsNullOrWhiteSpace($AppDataDir)) { $guiArgs += @('--appDataDir', ('"{0}"' -f $AppDataDir)) }
$simArgString = ($simArgs -join ' ')
$guiArgString = ($guiArgs -join ' ')
$pathPrefix = '{0};{1};{2};' -f $bin64, $vrlBin, $rtiBin

Say-Head 'Plan'
Say ("  process env : PATH={0}<Machine PATH>" -f $pathPrefix)
Say ("                MAK_VRFDIR={0}  MAK_VRLDIR={1}  MAK_RTIDIR={2}" -f $VrfRoot, $VrLinkRoot, $RtiDir)
Say ("                RTI_RID_FILE={0}" -f $ridFile)
if (-not $UseRtiAssistant) { Say '                RTI_ASSISTANT_DISABLE=1 (assistant-free; connection from the rid)' }
Say ("  back-end    : {0} {1}" -f $simExe, $simArgString)
if ($NoGui) { Say '  front-end   : (not launched: -NoGui)' } else { Say ("  front-end   : {0} {1}" -f $guiExe, $guiArgString) }
Say ("  cwd         : {0}" -f $bin64)
if (-not [string]::IsNullOrWhiteSpace($AppDataDir)) { Say ("  appData     : {0}  (--appDataDir; the vendor default {1}\appData is NOT used)" -f $AppDataDir, $VrfRoot) }
else { Say ("  appData     : {0}\appData  (vendor default; -AppDataDir not given)" -f $VrfRoot) }

if ($hardFail) {
    Say-Head 'Result'
    if ($DryRun) { Say-Warn 'DRY-RUN: one or more HARD preconditions FAILED above. A live run would abort here.' }
    else { Say-Fail 'Aborting: hard precondition failure (see above).' }
    exit 2
}
if ($DryRun) {
    Say-Head 'Result'
    if ($FederationHoldOn) {
        $fedNameDry = Get-FederationHoldName -ConnConfigFile $connFile
        Say-Plan ("start the STP-825 federation HOLDER first: tools/RtiProbe.exe {0} {1} 1 {2} 3, DETACHED (own hidden console), cwd {3}. Federation identity: {4} ({5})." -f $FederationHoldAppNumber, $fedNameDry.Name, $FederationHoldSecs, $bin64, $fedNameDry.Name, $fedNameDry.Source)
        Say-Plan ("wait up to 45s for its join in the serving rtiexec's log (rtiexec_*-<pid>.log under {0}), retrying ONCE on appNumber {1} if the create is refused." -f $RtiExecLogDir, ($FederationHoldAppNumber + 1))
        Say-Plan 'FAIL (exit 3, naming STP-825) and launch NOTHING if neither attempt joins. The holder OUTLIVES this script and is NEVER killed.'
    } else {
        Say-Plan "-FederationHoldSecs 0: NO federation holder. This launch's own back end will be the federation CREATOR at RTI join time - the STP-825 failure mode."
    }
    Say-Plan ("Start-Process '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $simExe, $bin64, $simArgString)
    if (-not $NoGui) { Say-Plan ("Start-Process '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $guiExe, $bin64, $guiArgString) }
    Say-Plan ("poll up to {0}s every {1}s: back-end threads > {2}{3}" -f $ReadyTimeoutSec, $PollIntervalSec, $BackendMinThreads, $(if ($NoGui) { '' } else { ' AND vrfGui MainWindowTitle non-empty' }))
    Say-Plan ("HARVEST the vendor's own back-end log at READY: newest {0}\vrfSim*-<pid>.log for THIS pid, written at or after the launch floor, COPIED (never moved - the sim keeps writing) to {1}. A snapshot; missing = loud WARN, verdict unchanged. --logFileName is NOT passed (PREREG_52_CRASH_BISECT_2026-09-04 sec 5: 6 crashes / 18 launches with it, 0 / 12 without, p = 0.031){2}." -f `
        $MakLogDir, $LogFile, $(if ([string]::IsNullOrWhiteSpace($LogFileName)) { '' } else { (' - EXCEPT that -LogFileName was given, so this run DOES pass it, at a ~1-in-3 crash risk: ' + $LogFileName) }))
    Say-Plan 'WARN, on that harvested copy, that it holds the full process environment in cleartext and must never be attached to a ticket or mail (send the .callstack.log / .dmp instead).'
    Say-Plan ("watch the SAME poll for a STARTUP CRASH (0xC0000005 in DtVrfSimOptions::parseCmdLine; its trigger, --logFileName, is NOT passed - PREREG_52_CRASH_BISECT_2026-09-04 sec 5 - but the detector stays): back-end gone, a MAK crash-box window title, or a new {0}\vrfSimHLA1516e*-<pid>.callstack.log. On any of them: print the first frames and exit 3, WITHOUT retrying." -f $MakLogDir)
    Say-Plan 'HARVEST that crashed pid''s vendor log too, on the crash path and BEFORE the corpse is closed - a crashed run''s short log is the forensic value, and the copy lands beside the callstack path printed with the frames (same secrets warning: never attach it anywhere).'
    if ($LeaveCrashedProcess) {
        Say-Plan 'LEAVE the crashed back-end running on that crash (-LeaveCrashedProcess) - and it would then refuse the NEXT launch until closed by hand or with -CloseCrashedLeftover.'
    } else {
        Say-Plan 'close OUR OWN failed back-end pid on that crash, before exiting 3 (AnswerCrashDumpDialog.ps1, then Stop-Process on that pid only - never the GUI, never rtiexec / rtiForwarder / rtiAssistant), so the next launch is not refused by the corpse.'
    }
    Say-Ok 'DRY-RUN complete: preconditions passed, nothing launched.'
    exit 0
}

# ---- STARTUP-CRASH DETECTOR (see the header block) --------------------------
# Returns a record for the back-end pid: crashed yes/no, which signature fired, the
# callstack file if MAK wrote one, and its first frames. Read-only: it decides, it does not
# act. Closing the crashed pid is the caller's job (Close-CrashedBackend, after the evidence
# has been printed), and only ever for the pid this script started.
function Get-SimCrashEvidence {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since)
    $o = [ordered]@{ Crashed = $false; Reason = ''; File = ''; Frames = @() }
    # (c) the callstack file - the strongest signature, and the only one that survives the
    # process exiting before we look. Get-CallstackFileForPid owns the pid-recycling guard
    # ($Since); see its comment.
    try {
        $csFile = Get-CallstackFileForPid -ProcessId $ProcessId -LogDir $LogDir -Since $Since
        if ($csFile) {
            $o.Crashed = $true
            $o.Reason  = 'MAK crash handler wrote a callstack for this pid'
            $o.File    = $csFile
            $o.Frames  = @(Get-Content -LiteralPath $csFile -TotalCount 12 -ErrorAction SilentlyContinue)
            return $o
        }
    } catch { }
    # (b) the crash box: MAK titles it 'Error <exe>' on 5.2 and '<exe>...dmp' on 5.0.2.
    try {
        $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($p) {
            $t = ''
            try { $t = $p.MainWindowTitle } catch { }
            if ($t -match '^Error .*vrfSim' -or $t -match '^vrfSim.*\.dmp$') {
                $o.Crashed = $true
                $o.Reason  = ("MAK crash dialog is up on the back-end: window title '{0}'" -f $t)
                return $o
            }
        }
    } catch { }
    return $o
}

# ---- LIVE ------------------------------------------------------------------
# MAKLMGRD_LICENSE_FILE was pinned onto this process by the licence gate above and the sim,
# the gui and anything else started here inherit it. The Machine scope is NOT re-read: it
# still names the pre-2026-09-14 file (RUNBOOK 0.5.15).
$env:PATH         = $pathPrefix + $env:PATH
$env:MAK_VRFDIR   = $VrfRoot
$env:MAK_VRLDIR   = $VrLinkRoot
$env:MAK_RTIDIR   = $RtiDir
$env:RTI_RID_FILE = $ridFile
if (-not $UseRtiAssistant) { $env:RTI_ASSISTANT_DISABLE = '1' }
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

# ---- STP-825 FEDERATION HOLDER (see -FederationHoldSecs above) -------------------------
# Started BEFORE the back end, AFTER the process env above is set, so the holder inherits
# the exact same PATH/MAK_*DIR/RTI_RID_FILE/RTI_ASSISTANT_DISABLE posture the back end and
# front end get - the runner's own Stage 2h posture, reused here.
if ($FederationHoldOn) {
    Say-Head 'Federation HOLDER (STP-825) - before the back end'
    $fedName = Get-FederationHoldName -ConnConfigFile $connFile
    Say ("  federation  : {0} ({1})" -f $fedName.Name, $fedName.Source)
    $holderStdIn = Join-Path $logDir ('holder_{0}.stdin.empty' -f $BackendAppNumber)
    try { [System.IO.File]::WriteAllText($holderStdIn, '') }
    catch { Say-Warn ("could not create {0}: {1}. The holder will inherit this script's own stdin instead." -f $holderStdIn, $_.Exception.Message) }

    $servingPid = Get-ServingRtiExecPid -RtiBinDir $rtiBin
    $holderLog  = Get-RtiExecLogForPid -LogDir $RtiExecLogDir -RtiExecPid $servingPid
    if ($holderLog) { Say-Ok ('join will be read from the rtiexec log: {0}' -f $holderLog) }
    else { Say-Warn ("no serving rtiexec log found under {0} (pid {1}) - the join can then only be confirmed from the holder's OWN stdout, written AFTER its {2}s hold, so with the default hold a short wait will report UNCONFIRMED even on a successful join." -f $RtiExecLogDir, $(if ($servingPid) { $servingPid } else { 'none found' }), $FederationHoldSecs) }

    $holderJoined   = $false
    $holderPidOut   = $null
    $holderAppUsed  = $null
    $holderAttempts = @([int]$FederationHoldAppNumber, [int]($FederationHoldAppNumber + 1))
    for ($hi = 0; $hi -lt $holderAttempts.Count -and -not $holderJoined; $hi++) {
        $hAppNo = $holderAttempts[$hi]
        $hOut   = Join-Path $logDir ('holder_{0}_attempt{1}.stdout.log' -f $BackendAppNumber, ($hi + 1))
        $hErr   = Join-Path $logDir ('holder_{0}_attempt{1}.stderr.log' -f $BackendAppNumber, ($hi + 1))
        [long]$hOffset = 0
        if ($holderLog) { try { $hOffset = (Get-Item -LiteralPath $holderLog).Length } catch { $hOffset = 0 } }

        Say-Info ('attempt {0}/{1}: starting holder RtiProbe.exe {2} {3} 1 {4} 3 (detached, own hidden console)' -f ($hi + 1), $holderAttempts.Count, $hAppNo, $fedName.Name, $FederationHoldSecs)
        $hProc = $null
        try {
            $hsp = @{ FilePath = $ExeRtiProbe; WorkingDirectory = $bin64; PassThru = $true; WindowStyle = 'Hidden'
                      ArgumentList = @([string]$hAppNo, $fedName.Name, '1', [string]$FederationHoldSecs, '3')
                      RedirectStandardOutput = $hOut; RedirectStandardError = $hErr }
            if (Test-Path -LiteralPath $holderStdIn) { $hsp.RedirectStandardInput = $holderStdIn }
            $hProc = Start-Process @hsp
            try { $null = $hProc.Handle } catch { }
        } catch {
            Say-Warn ('attempt {0}: the holder could not be started: {1}' -f ($hi + 1), $_.Exception.Message)
            continue
        }
        $hJoinRe = ('remoteControl\s+{0}\b.*has joined federation "{1}"' -f $hProc.Id, [regex]::Escape($fedName.Name))
        $hStart  = Get-Date
        while (((Get-Date) - $hStart).TotalSeconds -lt 45) {
            Start-Sleep -Seconds 1
            if ($holderLog) {
                if ((Read-TextFromOffset -Path $holderLog -Offset $hOffset) -match $hJoinRe) { $holderJoined = $true; break }
            } elseif ((Read-LiveText -Path $hOut) -match 'created/joined') {
                $holderJoined = $true; break
            }
            if ($hProc.HasExited) { break }
        }
        if ($holderJoined) {
            $holderPidOut  = $hProc.Id
            $holderAppUsed = $hAppNo
            Say-Ok ('federation HELD: holder pid {0} (appNumber {1}) joined {2} within 45s on attempt {3}/{4}. It stays joined for {5}s and is NEVER killed.' -f $holderPidOut, $holderAppUsed, $fedName.Name, ($hi + 1), $holderAttempts.Count, $FederationHoldSecs)
        } else {
            $hExited = $hProc.HasExited
            Say-Warn ('attempt {0}/{1} FAILED: holder pid {2} on appNumber {3} did not join within 45s (exited={4}).' -f ($hi + 1), $holderAttempts.Count, $hProc.Id, $hAppNo, $hExited)
            if (-not $hExited) { Say-Warn ('  pid {0} is NOT killed (RUNBOOK sec 0) - it may be a joined federate this script simply could not see.' -f $hProc.Id) }
        }
    }
    if (-not $holderJoined) {
        Say-Head 'Result'
        Say-Fail ('STP-825: the federation HOLDER could not join {0} after {1} attempt(s) (appNumbers {2}). Without a holder THIS SCRIPT''S OWN back end becomes the federation CREATOR, and rtiexec 5.0.1 is currently rejecting creator FOM distribution intermittently - the back end would die at startup. REFUSING TO LAUNCH before any back end is started. Read the holder logs under {3} and the rtiexec log ({4}). Nothing was killed. To run WITHOUT the holder (the pre-STP-825 behaviour, which is the failure mode): -FederationHoldSecs 0.' -f $fedName.Name, $holderAttempts.Count, ($holderAttempts -join ','), $logDir, $(if ($holderLog) { $holderLog } else { $RtiExecLogDir }))
        exit 3
    }
} else {
    Say-Head 'Federation HOLDER (STP-825)'
    Say-Warn "-FederationHoldSecs 0: no holder. This launch's own back end will be the federation CREATOR at RTI join time - the STP-825 failure mode (rtiexec 5.0.1 rejects a creator's FOM-module distribution intermittently)."
}

Say-Head 'Launch'
# Taken BEFORE the start so it can never be later than the process itself; it is the floor
# for "this back-end's callstack file" (see Get-SimCrashEvidence -Since). A couple of seconds
# of slack absorbs filesystem timestamp granularity without letting in yesterday's crash.
$simStartFloor = (Get-Date).AddSeconds(-5)
$simProc = Start-Process -FilePath $simExe -WorkingDirectory $bin64 -ArgumentList $simArgString -PassThru
Say-Ok ("back-end started (pid {0})" -f $simProc.Id)
$guiProc = $null
if (-not $NoGui) {
    $guiProc = Start-Process -FilePath $guiExe -WorkingDirectory $bin64 -ArgumentList $guiArgString -PassThru
    Say-Ok ("front-end started (pid {0})" -f $guiProc.Id)
}
Say-Ok 'polling for readiness...'

$deadline   = (Get-Date).AddSeconds($ReadyTimeoutSec)
$backendUp  = $false; $backendThr = 0; $backendExit = $null
$frontUp    = $false; $guiTitle = ''; $frontExit = $null
$needFront  = -not $NoGui
# The startup crash is NOT "not ready yet" - it is terminal, and the poll must not sit out
# its whole timeout on a dead process. Checked EVERY iteration, and once more after the
# loop (the callstack file can land a moment after the process disappears).
$simCrash   = [ordered]@{ Crashed = $false; Reason = ''; File = ''; Frames = @() }
while ((Get-Date) -lt $deadline) {
    $simCrash = Get-SimCrashEvidence -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor
    if ($simCrash.Crashed) { $backendThr = 0; break }
    $b = Get-Process -Id $simProc.Id -ErrorAction SilentlyContinue
    $backendUp = [bool]$b
    if ($b) { $backendThr = $b.Threads.Count } else { $backendThr = 0; $backendExit = $simProc.ExitCode; break }
    if ($needFront) {
        $f = Get-Process -Id $guiProc.Id -ErrorAction SilentlyContinue
        $frontUp = [bool]$f
        if ($f) { $guiTitle = $f.MainWindowTitle } else { $frontExit = $guiProc.ExitCode }
    }
    $backendHealthy = ($backendThr -gt $BackendMinThreads)
    $guiTitleOk = -not [string]::IsNullOrWhiteSpace($guiTitle)
    if ($backendHealthy -and ((-not $needFront) -or ($frontUp -and $guiTitleOk))) { break }
    Start-Sleep -Seconds $PollIntervalSec
}
# Keep the verdict definition IDENTICAL to the in-loop one (LaunchVrf.ps1 lesson).
$backendHealthy = ($backendThr -gt $BackendMinThreads)
$guiTitleOk = -not [string]::IsNullOrWhiteSpace($guiTitle)
# One more look: MAK writes the callstack around the moment the process disappears, so a
# poll that ended on "process gone" can still gain the evidence a second later.
if (-not $simCrash.Crashed) {
    $late = Get-SimCrashEvidence -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor
    if ($late.Crashed) { $simCrash = $late }
}

Say-Head 'Readiness'
if ($simCrash.Crashed) {
    # THE STARTUP CRASH (header block). Loud, with the frames, and terminal - the run that
    # follows would otherwise push an init at a back-end that never existed.
    Say-Fail ('back-end pid {0} CRASHED AT STARTUP - {1}' -f $simProc.Id, $simCrash.Reason)
    if ($simCrash.File) { Say-Fail ('  callstack: {0}' -f $simCrash.File) }
    foreach ($ln in @($simCrash.Frames)) { Say ('         | ' + $ln) }
    Say-Fail '  0xC0000005 in makVrf::DtVrfSimOptions::parseCmdLine. Its KNOWN trigger is --logFileName (6 crashes / 18 launches with it, 0 / 12 without, p = 0.031 - PREREG_52_CRASH_BISECT_2026-09-04 sec 5) and this script does not pass it by default. A crash WITHOUT -LogFileName is therefore NEW: the option is exonerated for this one, so record it and do not reuse the old explanation. NOT retried here.'
    if (-not [string]::IsNullOrWhiteSpace($LogFileName)) {
        Say-Fail ('  -LogFileName WAS PASSED on this launch ({0}). That is the KNOWN trigger: PREREG_52_CRASH_BISECT_2026-09-04 sec 5 measured 6 crashes / 18 launches with the option and 0 / 12 without (p = 0.031). Drop it before reading anything else into this crash.' -f $LogFileName)
    }
    # HARVEST BEFORE CLOSING: the corpse's own vendor log is short and is exactly the forensic
    # value of a crashed run, and the crash handler may still be holding the file. The copy
    # lands beside the callstack path printed above; it is a copy, so the original stays for
    # MAK. A missing one is a warning, never a change to this crash verdict.
    $null = Copy-VendorSimLog -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor -Destination $LogFile -Occasion 'STARTUP-CRASH'
    Say '  (A crashed pid USUALLY HAS NO vendor log at all: of the 10 pids with a .callstack.log in C:\MAK\logs on 2026-09-04, 9 had a .dmp and a .callstack.log but no .log - consistent with the fault being IN the log-stream installer itself. A "VENDOR LOG NOT FOUND" warning here is EXPECTED, not a second defect; the callstack and the dump above are the evidence.)'
    # The process is dead as a simulator but NOT gone: MAK's crash handler parks it (0 threads,
    # title 'Error vrfSimHLA1516e.exe'), and on 2026-09-04 that corpse made the next launch
    # exit 2 on the pre-existing-process precondition - an unattended runner could not retry.
    # This is OUR OWN pid, and it failed ITS OWN startup, so closing it needs no permission
    # (project rule); a healthy instance would still need one, and gets none here.
    if ($LeaveCrashedProcess) {
        Say-Warn ('  -LeaveCrashedProcess: pid {0} is LEFT AS IT IS for forensics (crash box, handles, dump prompt intact). It WILL refuse the next launch until it is closed by hand or with -CloseCrashedLeftover.' -f $simProc.Id)
    } else {
        Say-Warn ('  closing pid {0}: it is the back-end THIS script started and it failed its own startup. RTI infrastructure (rtiexec / rtiForwarder / rtiAssistant) and every other pid are untouched. -LeaveCrashedProcess keeps it instead.' -f $simProc.Id)
        Close-CrashedBackend -ProcessId $simProc.Id -ExpectedName $procBackend
    }
    if ($needFront -and $guiProc -and (Get-Process -Id $guiProc.Id -ErrorAction SilentlyContinue)) {
        Say-Warn ('  the front-end this script started (pid {0}) is still up. It did NOT fail its own startup, so it is NOT closed here - but it will refuse the next launch unless it is stopped (scripts\StopVrf52.ps1) or -AllowExistingVrf is passed.' -f $guiProc.Id)
    }
}
if (-not $backendUp) {
    Say-Fail ("back-end pid {0} NOT present (exit code {1}) - check the log: {2}" -f $simProc.Id, $backendExit, $LogFile)
} elseif ($backendHealthy) {
    Say-Ok ("back-end pid {0} is HEALTHY by thread count ({1} threads; floor {2})" -f $simProc.Id, $backendThr, $BackendMinThreads)
} else {
    Say-Fail ("back-end pid {0} PRESENT BUT NOT HEALTHY - {1} threads (floor {2}). PROCESS PRESENCE IS NOT HEALTH. Suspects: no rtiexec listening for this rid (scripts\StartRtiExec52.ps1), RTI version mismatch between the federate and an assistant, license, connection config." -f $simProc.Id, $backendThr, $BackendMinThreads)
}
if ($needFront) {
    if ($frontUp -and $guiTitleOk) { Say-Ok ("front-end pid {0} up with a real main window (title: '{1}')" -f $guiProc.Id, $guiTitle) }
    elseif ($frontUp) { Say-Fail ("front-end pid {0} exists but MainWindowTitle is EMPTY - blocking modal dialog signature (Scenario Startup dialog, license/LRC box). Look at the screen; do not kill." -f $guiProc.Id) }
    else { Say-Warn ("front-end pid {0} NOT up (exit code {1})" -f $guiProc.Id, $frontExit) }
}
# THE HARVEST (header, VENDOR LOG): --logFileName is not passed, so the only back-end log is
# the vendor's own in $MakLogDir. Copy it for THIS pid, on every non-crash outcome - a NOT
# READY back-end is exactly when its log matters most. The crash path harvested already,
# before closing the corpse, so it is not repeated here.
if (-not $simCrash.Crashed) {
    # SCENARIO-LOAD GATE, added 2026-09-04 after an adversarial audit. READY is THREAD-COUNT
    # readiness and fires BEFORE the scenario finishes loading, so harvesting there truncates the
    # log mid-load. That is not hypothetical: the 3908 fixture harvest stopped at 5,294 lines
    # while the live file reached 6,837, and the missing tail contained the very line the run was
    # trying to observe - "Successfully loaded scenario." at :6414. A session then concluded from
    # the truncated copy that 5.2 "never prints a load line" and built a discriminator around the
    # absence (PREREG_52_FIXTURE_LOAD sec 4, corrected). When a scenario was requested, wait for
    # the vendor's own load line before harvesting, so the copy contains the outcome.
    if ($backendHealthy -and $ScenarioLoadTimeoutSec -gt 0 -and -not [string]::IsNullOrWhiteSpace($Scenario)) {
        $liveLog = Get-VendorSimLogForPid -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor
        if ($liveLog) {
            $loadDeadline = (Get-Date).AddSeconds($ScenarioLoadTimeoutSec)
            $loaded = $false
            while ((Get-Date) -lt $loadDeadline) {
                try {
                    if (Select-String -LiteralPath $liveLog -SimpleMatch 'Successfully loaded scenario' -List -ErrorAction SilentlyContinue) { $loaded = $true; break }
                    if (Select-String -LiteralPath $liveLog -SimpleMatch 'Failed to load scenario' -List -ErrorAction SilentlyContinue) { break }
                } catch { }
                Start-Sleep -Seconds 2
            }
            if ($loaded) { Say-Ok ('scenario LOAD CONFIRMED in the vendor log ("Successfully loaded scenario") - harvesting after it, not at READY') }
            else { Say-Warn ('no "Successfully loaded scenario" line within {0}s; harvesting anyway. The copy may be TRUNCATED MID-LOAD - do not read absence of a line in it as evidence.' -f $ScenarioLoadTimeoutSec) }
        }
    }
    $null = Copy-VendorSimLog -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor -Destination $LogFile `
                -Occasion $(if ($backendHealthy) { 'READY' } else { 'NOT-READY' })
}
if (Test-Path -LiteralPath $LogFile) {
    Say-Ok ("back-end log tail - HARVESTED COPY, secrets warning above, do not attach it anywhere ({0}):" -f $LogFile)
    Get-Content -LiteralPath $LogFile -Tail 12 -ErrorAction SilentlyContinue | ForEach-Object { Say ('         | ' + $_) }
} else { Say-Warn ("no back-end log at {0}: the vendor log for this pid was not found or could not be copied (see the harvest warning above). --logFileName is deliberately NOT passed - PREREG_52_CRASH_BISECT_2026-09-04 sec 5." -f $LogFile) }

Say-Head 'Result'
if ($simCrash.Crashed) {
    # Checked FIRST: a crashed back-end can momentarily still satisfy the thread-count
    # oracle, and "READY" on a process with a callstack file would be the worst false green
    # this script could produce.
    Say-Fail ('CRASHED: the 5.2d back-end died at startup ({0}). NOT READY, NOT retried here - {1}. Exit 3.' -f `
        $simCrash.Reason,
        $(if ($LeaveCrashedProcess) { 'the crashed pid was LEFT RUNNING (-LeaveCrashedProcess) and will block the next launch' } else { 'the crashed pid was closed above, so a retry is not blocked by it' }))
    exit 3
}
if ($backendHealthy -and ((-not $needFront) -or ($frontUp -and $guiTitleOk))) {
    $frontNote = if ($needFront) { ', front-end with a real main window' } else { ' (no GUI requested)' }
    Say-Ok ('READY: 5.2d back-end HEALTHY by thread count{0}. Federation JOIN is NOT tested here - confirm with the 5.2 build of RtiProbe/WatchVrf before trusting anything.' -f $frontNote)
    exit 0
} elseif ($backendHealthy -and $frontUp) {
    Say-Fail 'BLOCKED: back-end healthy, front-end PROCESS up with NO main window title - a modal dialog is waiting for a human. Do NOT force-kill.'
    exit 4
} elseif ($backendHealthy) {
    Say-Warn 'PARTIAL: back-end healthy but the front-end never appeared in time.'
    exit 1
} else {
    Say-Fail 'NOT READY within timeout. Do NOT force-kill; inspect the back-end console/log and the RTI Assistant.'
    exit 3
}
