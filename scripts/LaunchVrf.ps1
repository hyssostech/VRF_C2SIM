<#
.SYNOPSIS
    Scripted self-launch of VR-Forces (combined front-end + back-end) via
    vrfLauncher.exe, for groundwork-plan item 0.4 (remove the human bring-up
    dependency). DRAFT recipe - live-gated in a supervised session (see
    docs/experiments/PREREG_0_4_SELFLAUNCH.md). ASCII only.

.DESCRIPTION
    The approved bring-up path is vrfLauncher.exe (the GUI launcher / combined
    front-end+back-end orchestrator), NOT a bare vrfSimHLA1516e.exe invocation.
    The raw headless back-end CLI is CONFIRMED UNSAFE on this machine: it crashes
    remote-controller clients (ResetVrf, the app) with 0xC0000005 on tick, root-
    caused to the missing front-end (RUNBOOK sec 0.5; OPUS_EXECUTION_PLAN.md
    Appendix B entries 3399/3412/3413 vs the clean GUI-launched 3414).

    This script reproduces the KNOWN-GOOD human path (double-click of the
    "VR-Forces GUI + Simulation Engine (64-bit)" shortcut = vrfLauncher.exe from
    bin64, which auto-starts the saved HLA connection profile) but does it
    unattended by naming the connection profile on the command line so the
    Simulation Connections Configuration dialog never appears.

    SAFETY: This script only ever invokes vrfLauncher.exe. It NEVER launches a
    bare vrfSim* / rtiexec / ResetVrf, and it NEVER force-kills any process
    (a force-killed joined federate leaves a stale federate - RUNBOOK sec 0).

.SOURCES (every argument / path below is cited to a primary source)
    * Combined-mode-from-CLI recipe (the core technique):
        C:\MAK\vrforces5.0.2\doc\help\Content\Introduction\CLI\
          vrf_startInComBinedModeWithoutLauncherWindow.htm
        "you can run vrfLauncher with the --usePredefinedConnection argument and
         VR-Forces will load the specified connection without displaying the
         Simulation Connections Configuration dialog box. This has the effect of
         running directly in combined mode from the command line."
    * vrfLauncher option table (-B/-F/-C, --simArgs/--guiArgs, --,
      --usePredefinedConnection, and the override example that passes
      --simArgs --appNumber / --guiArgs --appNumber):
        ...\Introduction\CLI\vrf_vrfLauncherCommandLine.htm  (Table 9)
    * Installer combined-mode shortcut = vrfLauncher.exe (no args), cwd bin64:
        "...\Start Menu\Programs\MAK Technologies\VR-Forces 5.0.2\
         VR-Forces GUI + Simulation Engine (64-bit).lnk"  (read 2026-07-16)
    * Saved connection profile (primary source for FED/FOM/execName/appNos):
        C:\MAK\vrforces5.0.2\appData\settings\vrfLauncher\
          HLA 1516 Evolved RPR 2.0 with MAK extensions.xml
        -> applicationNumber 3001 (back-end), applicationNumberFE 3101
           (front-end), fedFileName RPR_FOM_v2.0_1516-2010.xml,
           federationName CWIX-2024, fomModules MAK-VRFExt-6_evolved.xml;
           MAK-DIGuy-7_evolved.xml;MAK-LgrControl-2_evolved.xml, siteId 1,
           sessionId 1, hostAddress 127.0.0.1.
        C:\MAK\vrforces5.0.2\appData\settings\vrfLauncher\autoConnect.xml
        -> myAutoStartDrivers lists exactly this profile (bare vrfLauncher.exe
           auto-connects it).
    * Scenario path form "../userData/scenarios/<name>.scnx" (relative to bin64),
      env (RTI 4.6.1 Machine-scope, MAKLMGRD_LICENSE_FILE from Machine scope),
      cwd = bin64, fresh appNumber per Appendix B, (NOTE: whether rtiexec runs is
      CONNECTION-DEPENDENT - it DOES run under the rtiexec loopback connection.
      Never gate readiness on it. RUNBOOK 0.5.6),
      "judge by process presence not console (block-buffered)":
        docs/RUNBOOK.md sec 0.5, sec 7, sec 8; docs/OPUS_EXECUTION_PLAN.md
        Appendix B - take the value from the single line marked "*** NEXT FREE:"
        (do NOT hard-code it here; an earlier version of this comment went stale).

.PARAMETER Mode
    Combined  (default) - front-end + back-end together. The ONLY approved path;
              matches the known-good human GUI launch.
    BackendOnly  - vrfLauncher -B (back-end only, no front-end). KNOWN CRASH RISK:
              this is the "missing front-end" condition the 0xC0000005 crash was
              root-caused to. Refused unless -AcceptCrashRisk is also passed.
              Included only as the documented future data-point probe
              (RUNBOOK sec 0.5 last paragraph).
    FrontendOnly - vrfLauncher -F. Present for completeness; cannot pass the
              backend-up gate on its own.

.PARAMETER DryRun
    Print every action the script WOULD take (env changes, the exact vrfLauncher
    command line, the readiness poll) and execute NOTHING. Read-only precondition
    checks still run so the printed plan is grounded in real state. -DryRun is the
    only permitted self-test for this drafting task.

.EXAMPLE
    # Self-test (safe): print the plan, launch nothing.
    pwsh -File scripts\LaunchVrf.ps1 -DryRun -BackendAppNumber <fresh1> -FrontendAppNumber <fresh2>

.EXAMPLE
    # Live: combined mode, TropicTortoise.
    pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise -BackendAppNumber <fresh1> -FrontendAppNumber <fresh2>

    *** NEVER PASTE A LITERAL APP NUMBER FROM THIS HELP TEXT. *** These examples used to
    show 3455/3456 - by 2026-07-18 those were BURNED and RESERVED respectively, and this
    help block was the ONLY place an operator saw a concrete pair, so it modelled exactly
    the reuse that causes the stale-federate join hang. Take BOTH numbers from the single
    line marked "*** NEXT FREE: <number> ***" in docs/OPUS_EXECUTION_PLAN.md Appendix B,
    LEDGER THEM BEFORE LAUNCHING, then advance the marker. This script CANNOT enforce
    freshness - it never reads the ledger (RUNBOOK 0.5.1).
#>

[CmdletBinding()]
param(
    [string] $VrfRoot            = 'C:\MAK\vrforces5.0.2',
    [string] $RtiDir             = 'C:\MAK\makRti4.6.1',
    [string] $ConnectionProfile  = 'HLA 1516 Evolved RPR 2.0 with MAK extensions',
    [string] $Scenario           = 'TropicTortoise',
    [switch] $NoScenario,
    # DEFECT-2 FIX (2026-07-18): no baked-in defaults. Both app numbers are
    # MANDATORY; a missing one is a hard exit 2, matching tools/SetSimRate's
    # posture. Baked defaults were a stale-federate trigger against the
    # never-reuse non-negotiable (RUNBOOK sec 0 / Appendix B).
    [int]    $BackendAppNumber   = 0,
    [int]    $FrontendAppNumber  = 0,
    [ValidateSet('Combined','BackendOnly','FrontendOnly')]
    [string] $Mode               = 'Combined',
    [int]    $ReadyTimeoutSec    = 120,
    [int]    $PollIntervalSec    = 3,
    # Back-end health floor. A STALLED back-end sits at 2 threads indefinitely; a
    # healthy one was measured at ~36 (2026-07-18). 8 is a deliberate margin: well
    # above the stall signature, well below the healthy steady state.
    [int]    $BackendMinThreads  = 8,
    # NOTE 2026-07-18: a PRE-EXISTING, ANSWERED rtiAssistant is REQUIRED for
    # unattended launch - it is never a reason to refuse (RUNBOOK 0.5.2). The only
    # blocking assistant state is one sitting on an UNANSWERED "Choose RTI
    # Connection" dialog, which -IgnoreUnansweredRtiAssistant overrides for
    # deliberate probing. -AllowExistingRtiAssistant is retained ONLY so existing
    # command lines and docs keep working; it is accepted and ignored.
    [switch] $IgnoreUnansweredRtiAssistant,
    [switch] $AllowExistingRtiAssistant,
    [switch] $AllowExistingVrf,
    [switch] $AcceptCrashRisk,
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'

# ---- small output helpers (ASCII only) -------------------------------------
function Say      { param([string]$m) Write-Host $m }
function Say-Head { param([string]$m) Write-Host ''; Write-Host ('=== ' + $m + ' ===') }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [DRY-RUN] would ' + $m) }

$modeTag = if ($DryRun) { 'DRY-RUN' } else { 'LIVE' }
$scenarioDisplay = if ($NoScenario) { '(none - --scenarioFileName omitted)' } else { $Scenario }
Say-Head "LaunchVrf.ps1 ($modeTag) - VR-Forces self-launch via vrfLauncher.exe"
Say ("  Mode              : {0}" -f $Mode)
Say ("  VrfRoot           : {0}" -f $VrfRoot)
Say ("  ConnectionProfile : {0}" -f $ConnectionProfile)
Say ("  Scenario          : {0}" -f $scenarioDisplay)
Say ("  Back-end appNumber: {0}" -f $BackendAppNumber)
Say ("  Front-end appNo   : {0}" -f $FrontendAppNumber)
# DEFECT-2 FIX: app numbers are a HARD GATE, not a warning. Checked before any
# other precondition so the failure is unmissable and nothing is launched.
$appNoFail = $false
if ($BackendAppNumber -le 0) {
    Say-Fail 'MISSING -BackendAppNumber. It is MANDATORY (no default). Take the NEXT FREE value from OPUS_EXECUTION_PLAN.md Appendix B and ledger it BEFORE launching.'
    $appNoFail = $true
}
if ($FrontendAppNumber -le 0) {
    Say-Fail 'MISSING -FrontendAppNumber. It is MANDATORY (no default). Take the NEXT FREE value from OPUS_EXECUTION_PLAN.md Appendix B and ledger it BEFORE launching.'
    $appNoFail = $true
}
if (($BackendAppNumber -gt 0) -and ($BackendAppNumber -eq $FrontendAppNumber)) {
    Say-Fail ('-BackendAppNumber and -FrontendAppNumber are IDENTICAL ({0}). Each join consumes its own number; reuse is the stale-federate trigger.' -f $BackendAppNumber)
    $appNoFail = $true
}
if ($appNoFail) {
    Say-Head 'Result'
    Say-Fail 'Aborting: app-number gate failed (never-reuse non-negotiable, RUNBOOK sec 0).'
    exit 2
}

# ---- derived paths ---------------------------------------------------------
$bin64       = Join-Path $VrfRoot 'bin64'
$launcher    = Join-Path $bin64  'vrfLauncher.exe'
$profileXml  = Join-Path $VrfRoot ('appData\settings\vrfLauncher\{0}.xml' -f $ConnectionProfile)
$autoConnect = Join-Path $VrfRoot 'appData\settings\vrfLauncher\autoConnect.xml'
$scenarioRel = "../userData/scenarios/$Scenario.scnx"
$scenarioAbs = Join-Path $VrfRoot ("userData\scenarios\{0}.scnx" -f $Scenario)
$rtiBin      = Join-Path $RtiDir 'bin'
$licMachine  = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')

# Process names to poll (exe base names, no extension)
$procBackend  = 'vrfSimHLA1516e'
$procFrontend = 'vrfGui'
$procRti      = 'rtiexec'
$procLauncher = 'vrfLauncher'

# ---- PRECONDITIONS (read-only; run in both DryRun and live) ----------------
Say-Head 'Preconditions'
$hardFail = $false

# 1. launcher + working directory
if (Test-Path $launcher) { Say-Ok ("vrfLauncher.exe present: {0}" -f $launcher) }
else { Say-Fail ("vrfLauncher.exe NOT found: {0}" -f $launcher); $hardFail = $true }
if (Test-Path $bin64) { Say-Ok ("working directory (cwd) present: {0}" -f $bin64) }
else { Say-Fail ("bin64 working directory NOT found: {0}" -f $bin64); $hardFail = $true }

# 2. RTI 4.6.1 environment (verify, do not mutate - it is Machine-scoped and
#    inherited by the launcher's children, matching the human double-click)
if (Test-Path $rtiBin) { Say-Ok ("RTI 4.6.1 present: {0}" -f $rtiBin) }
else { Say-Fail ("RTI 4.6.1 bin NOT found: {0} (RUNBOOK sec 7: runtime RTI must be 4.6.1)" -f $rtiBin); $hardFail = $true }
$makRtiDir  = [Environment]::GetEnvironmentVariable('MAK_RTIDIR','Machine')
$ridFile    = [Environment]::GetEnvironmentVariable('RTI_RID_FILE','Machine')
if ($makRtiDir) { Say-Ok ("MAK_RTIDIR (Machine) = {0}" -f $makRtiDir) } else { Say-Warn 'MAK_RTIDIR (Machine) is not set' }
if ($ridFile)   { Say-Ok ("RTI_RID_FILE (Machine) = {0}" -f $ridFile) } else { Say-Warn 'RTI_RID_FILE (Machine) is not set' }
if ($makRtiDir -and ($makRtiDir -notmatch '4\.6\.1')) {
    Say-Warn ("MAK_RTIDIR does not look like 4.6.1 ({0}) - the federation RTI must be 4.6.1 (RUNBOOK sec 7)" -f $makRtiDir)
}

# 3. License reachability (passive only - never contact a license server)
if ([string]::IsNullOrWhiteSpace($licMachine)) {
    Say-Warn 'MAKLMGRD_LICENSE_FILE (Machine) is empty - VR-Forces license checkout may hang (RUNBOOK sec 7 item 2)'
} elseif ($licMachine -match '@') {
    Say-Warn ("MAKLMGRD_LICENSE_FILE is a port@host form ({0}) - cannot passively verify without a network probe; not probing." -f $licMachine)
} elseif (Test-Path $licMachine) {
    Say-Ok ("license file present (existence only; expiry/validity NOT checked - no checkout): {0}" -f $licMachine)
} else {
    Say-Warn ("MAKLMGRD_LICENSE_FILE points at a path that does not exist: {0}" -f $licMachine)
}

# 4. Connection profile saved (the doc's 'launch from the Launcher at least once'
#    precondition - the saved profile file IS that persisted network info)
if (Test-Path $profileXml) { Say-Ok ("connection profile saved: {0}" -f $profileXml) }
else {
    Say-Fail ("connection profile NOT saved: {0}" -f $profileXml)
    Say-Fail '  -> vrf_startInComBinedModeWithoutLauncherWindow.htm: the profile must be launched from the Launcher at least once so VR-Forces saves the network address info. Run vrfLauncher -C once, or double-click the shortcut and connect, then retry.'
    $hardFail = $true
}
if (Test-Path $autoConnect) {
    try {
        $ac = Get-Content -LiteralPath $autoConnect -Raw -Encoding UTF8
        if ($ac -match [regex]::Escape($ConnectionProfile)) { Say-Ok 'autoConnect.xml auto-starts this profile (bare vrfLauncher.exe would also connect it)' }
        else { Say-Warn 'autoConnect.xml exists but does not list this profile - a bare launch may show the connection dialog; --usePredefinedConnection still skips it' }
    } catch { Say-Warn ("could not read autoConnect.xml: {0}" -f $_.Exception.Message) }
}

# 5. Scenario file (only if we are going to auto-load one)
if (-not $NoScenario) {
    if (Test-Path $scenarioAbs) { Say-Ok ("scenario file present: {0}" -f $scenarioAbs) }
    else { Say-Fail ("scenario file NOT found: {0}" -f $scenarioAbs); $hardFail = $true }
} else {
    Say-Ok 'scenario auto-load disabled (-NoScenario); backend will start with no scenario'
}

# 6. Stale VR-Forces processes from a prior session (RUNBOOK sec 0 hazard)
# NOTE (2026-07-18): rtiexec is deliberately NOT in this list. It is RTI INFRASTRUCTURE,
# not a VR-Forces federate: under the "Legatus's predefined rtiexec loopback connection"
# (the correct connection on this machine) an rtiexec is started and PERSISTS across
# launches, exactly like rtiForwarder and rtiAssistant. Treating it as a stale federate
# made this script refuse to launch onto a perfectly healthy environment. Killing it would
# repeat this session's central mistake - see docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md.
$existing = @()
foreach ($n in @($procLauncher,$procBackend,$procFrontend)) {
    $p = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($p) { $existing += ($p | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($existing.Count -gt 0) {
    Say-Warn ("VR-Forces/RTI processes ALREADY running: {0}" -f ($existing -join ', '))
    Say-Warn '  A pre-existing federate can cause stale-federate join hangs or a second federation. Do NOT force-kill it (RUNBOOK sec 0); clean-stop or let the operator reload.'
    if (-not $AllowExistingVrf) {
        Say-Fail '  Refusing to launch on top of existing VR-Forces processes. Re-run with -AllowExistingVrf to override deliberately.'
        $hardFail = $true
    } else {
        Say-Warn '  -AllowExistingVrf set: proceeding despite existing processes.'
    }
} else {
    Say-Ok 'no pre-existing vrfLauncher / vrfSimHLA1516e / vrfGui processes'
}

# RTI infrastructure - REPORTED, NEVER REFUSED ON, NEVER KILLED. rtiexec/rtiForwarder
# persist across launches under the rtiexec loopback connection and are required by it.
$infra = @()
foreach ($n in @($procRti,'rtiForwarder')) {
    $ip = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($ip) { $infra += ($ip | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($infra.Count -gt 0) {
    Say-Ok ("RTI infrastructure present (expected; do NOT kill): {0}" -f ($infra -join ', '))
} else {
    Say-Ok 'no rtiexec / rtiForwarder running yet. NOT an error: whether they appear is connection-dependent, and the RTI starts them itself when the chosen connection needs them. Never gate readiness on their presence (RUNBOOK 0.5.6).'
}

# 6b. STALE rtiAssistant HOLDING ITS PORT (added 2026-07-18, found live).
# Every VR-Forces launch starts its own RTI Assistant. A surviving one holds the
# port and the new one dies with "RTI Assistant server creation failed. The port
# [ 6003 ] may be in use". One instance survived 3 days stuck on a modal "Choose
# RTI Connection" dialog. Every federate connects to this port, so a WEDGED
# assistant is an unexcluded suspect for stalled back-ends
# (docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md sec 3/4). Detect it BEFORE
# launching instead of debugging it afterwards.
# NOTE: this checks for the rtiAssistant PROCESS and its window state. It does NOT
# probe the port - $assistPort is used for messages only.
$assistPort = $env:RTI_ASSISTANT_PORT
if ([string]::IsNullOrWhiteSpace($assistPort)) { $assistPort = '6003' }
$assistUnanswered = $false
$assist = Get-Process -Name 'rtiAssistant' -ErrorAction SilentlyContinue
if ($assist) {
    foreach ($a in $assist) {
        $t = if ([string]::IsNullOrWhiteSpace($a.MainWindowTitle)) { '(no window title)' } else { $a.MainWindowTitle }
        Say-Warn ("pre-existing rtiAssistant pid {0} - window: {1}" -f $a.Id, $t)
        if ($a.MainWindowTitle -match 'Choose RTI Connection') {
            # An assistant SHOWING the dialog is UNANSWERED - it cannot serve a
            # connection, and every back-end will block behind it (RUNBOOK 0.5.3).
            # This is the one assistant state that must stop the launch. Do NOT
            # kill it - it must be ANSWERED (or automated per RUNBOOK 0.5.4).
            Say-Fail ("  -> pid {0} is sitting on the UNANSWERED 'Choose RTI Connection' dialog. Back-ends WILL block behind it." -f $a.Id)
            Say-Fail '     ANSWER IT (select the connection, tick "Always try to use this connection", click Connect) - do NOT kill it. Then re-run. Override with -IgnoreUnansweredRtiAssistant only to probe deliberately.'
            if (-not $IgnoreUnansweredRtiAssistant) { $hardFail = $true }
            $assistUnanswered = $true
        }
    }
    Say-Ok ("  This launch's own RTI Assistant will fail to bind port {0} and exit - which is EXPECTED AND BENIGN. The pre-existing assistant serves the federation." -f $assistPort)
    Say-Ok '  PROCEEDING. Do NOT kill the pre-existing assistant to "clean up".'
} else {
    # CORRECTED 2026-07-18 after the root cause was found. The earlier version of
    # this check REFUSED to launch when an rtiAssistant already existed. That was
    # BACKWARDS and would have blocked the only configuration that ever worked.
    #
    # Vendor-documented startup sequence (VR-Forces help, SharedTopics\XMLrti\
    # InstallMAK-RTI.htm): "Start the application. The RTI Assistant will prompt
    # you to choose an RTI configuration. Choose a configuration. If necessary,
    # start the rtiexec. Click Connect. The application should run."
    #
    # So on HLA a federate DOES NOT START until an RTI Assistant has been ANSWERED.
    # An already-answered assistant is the ASSET. When none exists, this launch
    # spawns a fresh one that sits unanswered on "Choose RTI Connection" and the
    # back-end blocks forever (observed repeatedly 2026-07-18: back-end frozen
    # after the VR-Link banner, before the parameter database, threads decaying
    # 7 -> 4 -> 2, UDP 4000 never bound).
    Say-Warn ("NO pre-existing rtiAssistant found. Per the vendor's documented startup sequence, this launch will spawn one that PROMPTS ('Choose RTI Connection') and the BACK-END WILL BLOCK until it is answered.")
    Say-Warn '  Unless the Assistant has been configured to auto-connect, a human (or UI automation) must choose the connection and click Connect.'
    Say-Warn '  Detail: docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md'
}

# 7. Mode guard for the known crash-risk backend-only variant
if ($Mode -eq 'BackendOnly' -and -not $AcceptCrashRisk) {
    Say-Fail 'Mode=BackendOnly (vrfLauncher -B) is the KNOWN 0xC0000005 crash-risk path (missing front-end; RUNBOOK sec 0.5). Refusing. Pass -AcceptCrashRisk to run it deliberately as a documented probe.'
    $hardFail = $true
}

# ---- BUILD THE vrfLauncher COMMAND LINE ------------------------------------
Say-Head 'Resolved vrfLauncher command line'
$q = [char]34   # double quote

$simBlock = @('--appNumber', "$BackendAppNumber")
if (-not $NoScenario) { $simBlock += @('--scenarioFileName', ($q + $scenarioRel + $q)) }
$guiBlock = @('--appNumber', "$FrontendAppNumber")
$profArg  = @('--usePredefinedConnection', ($q + $ConnectionProfile + $q))

switch ($Mode) {
    'Combined'     { $parts = $profArg + @('--simArgs') + $simBlock + @('--guiArgs') + $guiBlock }
    'BackendOnly'  { $parts = @('-B') + $profArg + @('--simArgs') + $simBlock }
    'FrontendOnly' { $parts = @('-F') + $profArg + @('--guiArgs') + $guiBlock }
}
$argString = ($parts -join ' ')
Say ("  cwd : {0}" -f $bin64)
Say ("  cmd : {0} {1}" -f $launcher, $argString)

# ---- ABORT on hard precondition failure ------------------------------------
if ($hardFail) {
    Say-Head 'Result'
    if ($DryRun) { Say-Warn 'DRY-RUN: one or more HARD preconditions FAILED above. A live run would abort here.'; exit 2 }
    else { Say-Fail 'Aborting: hard precondition failure (see above).'; exit 2 }
}

# ---- LAUNCH + READINESS POLL -----------------------------------------------
Say-Head 'Launch'
if ($DryRun) {
    if (-not [string]::IsNullOrWhiteSpace($licMachine)) {
        Say-Plan ("set MAKLMGRD_LICENSE_FILE (process) = {0}  (refresh from Machine scope; RUNBOOK sec 7 item 2)" -f $licMachine)
    } else {
        Say-Plan 'PRESERVE the existing process-scope MAKLMGRD_LICENSE_FILE - the Machine value is EMPTY, and this script deliberately does NOT blank it (DEFECT-3).'
    }
    Say-Plan ("Start-Process -FilePath '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $launcher, $bin64, $argString)
    Say-Plan ("poll every {0}s up to {1}s for readiness signals:" -f $PollIntervalSec, $ReadyTimeoutSec)
    Say ("            - process '{0}' present (NECESSARY, NOT SUFFICIENT - process presence is NOT health)" -f $procBackend)
    Say ("            - back-end thread count > {0} (THE health signal: a blocked back-end sits at 2-4 threads while present; healthy reaches 23-67)" -f $BackendMinThreads)
    Say ("            - back-end UDP endpoints logged INFORMATIONALLY only - UDP 4000 is connection-dependent and is NOT required (the rtiexec loopback connection uses TCP 4001 + forwarder 5000)")
    Say ("            - process '{0}' present     (front-end; combined mode - the crash-avoiding piece)" -f $procFrontend)
    Say  '            - vrfGui MainWindowTitle non-empty (front-end window is up, not stuck in a modal dialog)'
    Say  '            - back-end UDP endpoints logged for the record (passive)'
    Say-Plan 'declare READY only when the back-end is HEALTHY (thread count above the floor - NOT mere process presence, and NOT rtiexec, whose presence is CONNECTION-DEPENDENT; UDP 4000 is informational only and is legitimately absent under the rtiexec loopback connection) AND (in Combined mode) the front-end is up WITH A NON-EMPTY MainWindowTitle. A slow vrfGui start keeps polling rather than misreporting PARTIAL. Distinct outcomes: exit 0 READY; exit 4 BLOCKED (front-end process up but NO window title = modal dialog waiting on a human); exit 1 PARTIAL (back-end healthy but no front-end - the crash-risk condition); exit 3 NOT READY within timeout (on HLA the usual cause is an UNANSWERED RTI Assistant "Choose RTI Connection" prompt - see the precondition warning above).'
    Say-Head 'Result'
    Say-Ok 'DRY-RUN complete: preconditions passed, command line resolved, no process launched.'
    exit 0
}

# --- live path (NOT executed in this drafting task) ---
# DEFECT-3 FIX (2026-07-18): only overwrite from Machine scope when the Machine
# value is actually non-empty. The old unconditional assignment BLANKED a
# working process-scope license value whenever the Machine value was empty or
# null (which was itself only a warning), turning a launchable session into a
# license-hang for no reason.
if (-not [string]::IsNullOrWhiteSpace($licMachine)) {
    $env:MAKLMGRD_LICENSE_FILE = $licMachine
    Say-Ok ("MAKLMGRD_LICENSE_FILE (process) refreshed from Machine scope = {0}" -f $licMachine)
} elseif (-not [string]::IsNullOrWhiteSpace($env:MAKLMGRD_LICENSE_FILE)) {
    Say-Warn ("Machine-scope MAKLMGRD_LICENSE_FILE is EMPTY - PRESERVING the existing process-scope value ({0}) rather than blanking it." -f $env:MAKLMGRD_LICENSE_FILE)
} else {
    Say-Warn 'MAKLMGRD_LICENSE_FILE is empty in BOTH Machine and process scope - license checkout may hang (RUNBOOK sec 7 item 2). Launching anyway; watch for a license dialog.'
}

Say-Ok ("launching: {0} {1}" -f $launcher, $argString)
$launch = Start-Process -FilePath $launcher -WorkingDirectory $bin64 -ArgumentList $argString -PassThru
Say-Ok ("vrfLauncher started (pid {0}); polling for backend readiness..." -f $launch.Id)

# DEFECT-4 FIX (2026-07-18, found live): the poll used to require the rtiexec
# PROCESS. rtiexec NEVER RUNS on this machine - rid.mtl sets RTI_useRtiExec 0 -
# so that condition could never be satisfied and the script reported NOT READY
# against a fully healthy launch. It also treated bare process presence as
# back-end health, which is FALSE: a stalled back-end sits at 2 threads, present
# the whole time. Replaced with the oracle measured against a verified-healthy
# back-end (RUNBOOK sec 0.5 correction):
#   joined  = the back-end has UDP 4000 bound (RTI_udpPort 4000 - the real
#             federation transport; forwarder :5000 is dark even when healthy)
#   healthy = thread count grown well past 2 (~36 observed on a good backend)
$deadline    = (Get-Date).AddSeconds($ReadyTimeoutSec)
$backendUp   = $false
$backendJoin = $false
$backendThr  = 0
$frontUp     = $false
$guiTitle    = ''
while ((Get-Date) -lt $deadline) {
    $b = Get-Process -Name $procBackend  -ErrorAction SilentlyContinue
    $f = Get-Process -Name $procFrontend -ErrorAction SilentlyContinue
    $backendUp = [bool]$b
    $frontUp   = [bool]$f
    if ($b) {
        $bp = ($b | Select-Object -First 1)
        $backendThr = $bp.Threads.Count
        # INFORMATIONAL ONLY - see the DEFECT-5 note below.
        $backendJoin = [bool](Get-NetUDPEndpoint -OwningProcess $bp.Id -LocalPort 4000 -ErrorAction SilentlyContinue)
    } else {
        $backendThr = 0; $backendJoin = $false
    }
    # DEFECT-5 FIX (2026-07-18, found after the root cause): UDP 4000 was REQUIRED for
    # health. That is WRONG - it is CONNECTION-DEPENDENT, not a universal signal. Under
    # the correct "Legatus's predefined rtiexec loopback connection" a fully healthy
    # back-end has NO UDP 4000 binding (that connection uses TCP 4001 + forwarder 5000);
    # UDP 4000 appeared only on runs that had fallen back to a lightweight connection.
    # Requiring it made the script report NOT READY against a verified-healthy federation.
    # Thread count is the connection-independent signal actually measured: a blocked
    # back-end sits at 2-4 threads indefinitely, a healthy one reaches 23-67.
    $backendHealthy = ($backendThr -gt $BackendMinThreads)
    if ($f) { $guiTitle = ($f | Select-Object -First 1 -ExpandProperty MainWindowTitle) }
    # DEFECT-1 FIX (2026-07-18): the -DryRun text always advertised a
    # MainWindowTitle readiness check that the poll never performed, so the
    # script could not detect RISK A (session-startup modal) or RISK B and its
    # own output overstated what it did. Now ACTUALLY tested: a front-end whose
    # MainWindowTitle is empty is up as a PROCESS but has no usable main window
    # - the modal-dialog signature. Keep polling rather than declaring READY.
    $guiTitleOk = -not [string]::IsNullOrWhiteSpace($guiTitle)
    # In Combined mode keep polling until the front-end is ALSO up (a slow vrfGui
    # start must not be misreported as PARTIAL - PARTIAL is the crash-risk signal
    # and should only fire when the front-end genuinely never appeared in time).
    $needFront = ($Mode -eq 'Combined')
    if ($backendHealthy -and (($frontUp -and $guiTitleOk) -or -not $needFront)) { break }
    Start-Sleep -Seconds $PollIntervalSec
}
# Recomputed after the loop for the verdict below. MUST match the in-loop definition
# exactly - an earlier version of this line still required $backendJoin (UDP 4000) after
# the loop condition had been corrected, so a 64-thread healthy back-end was reported as
# "PRESENT BUT NOT HEALTHY". Keep these two in sync.
$backendHealthy = ($backendThr -gt $BackendMinThreads)

Say-Head 'Readiness'
if (-not $backendUp) {
    Say-Fail ("back-end '{0}' process NOT present" -f $procBackend)
} elseif ($backendHealthy) {
    Say-Ok ("back-end '{0}' is HEALTHY ({1} threads; udp4000={2} - informational, connection-dependent)" -f $procBackend, $backendThr, $backendJoin)
} else {
    Say-Fail ("back-end '{0}' process is PRESENT BUT NOT HEALTHY - only {1} threads (a blocked back-end sits at 2-4 indefinitely; healthy is 23-67). PROCESS PRESENCE IS NOT HEALTH. On HLA the usual cause is an UNANSWERED RTI Assistant 'Choose RTI Connection' prompt." -f $procBackend, $backendThr)
}
$guiTitleOk = -not [string]::IsNullOrWhiteSpace($guiTitle)
if ($frontUp -and $guiTitleOk) {
    Say-Ok ("front-end '{0}' is up with a real main window (title: '{1}')" -f $procFrontend, $guiTitle)
} elseif ($frontUp) {
    Say-Fail ("front-end '{0}' process exists but its MainWindowTitle is EMPTY - the signature of a BLOCKING MODAL DIALOG (prereg RISK A session-startup dialog, or a license/LRC error box). The process being up does NOT mean the front-end is usable." -f $procFrontend)
} else {
    Say-Warn ("front-end '{0}' NOT up - combined mode incomplete; this is the crash-risk 'missing front-end' condition" -f $procFrontend)
}

# passive endpoint signal for the record (back-end federation transport)
if ($backendUp) {
    try {
        $bPid  = (Get-Process -Name $procBackend -ErrorAction SilentlyContinue | Select-Object -First 1).Id
        $udp   = Get-NetUDPEndpoint -OwningProcess $bPid -ErrorAction SilentlyContinue | Select-Object -ExpandProperty LocalPort -Unique
        if ($udp) { Say-Ok ("back-end UDP endpoints: {0}" -f ($udp -join ', ')) }
    } catch { Say-Warn ("could not read back-end UDP endpoints: {0}" -f $_.Exception.Message) }
}

Say-Head 'Result'
if ($backendHealthy -and $frontUp -and $guiTitleOk) {
    Say-Ok 'READY: combined-mode VR-Forces is up (back-end HEALTHY by thread count, front-end with a real main window). NOTE: federation JOIN is not tested here - confirm it with a WatchVrf discovery check (RUNBOOK 0.5.7) before trusting any telemetry.'
    exit 0
} elseif ($backendHealthy -and $frontUp) {
    Say-Fail 'BLOCKED: back-end healthy and front-end PROCESS up, but the front-end has NO main window title - a modal dialog is almost certainly waiting for a human (prereg RISK A). NOT ready. Do NOT force-kill; look at the screen and clear the dialog.'
    exit 4
} elseif ($backendHealthy) {
    Say-Warn 'PARTIAL: back-end healthy but front-end not detected. Combined mode may be incomplete (crash risk). Inspect before proceeding.'
    exit 1
} else {
    Say-Fail 'NOT READY within timeout. Likely a blocking dialog (LRC #8 FDD path / license / session-startup) or a failed join. Do NOT force-kill; inspect the launcher/GUI window and clean-stop.'
    exit 3
}
