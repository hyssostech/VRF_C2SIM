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
      cwd = bin64, fresh appNumber per Appendix B, rtiexec auto-spawned on join,
      "judge by process presence not console (block-buffered)":
        docs/RUNBOOK.md sec 0.5, sec 7, sec 8; docs/OPUS_EXECUTION_PLAN.md
        Appendix B (NEXT FREE app number = 3455 at time of writing).

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
    pwsh -File scripts\LaunchVrf.ps1 -DryRun -BackendAppNumber 3455 -FrontendAppNumber 3456

.EXAMPLE
    # Live (supervised session only, per the prereg): combined mode, TropicTortoise.
    pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise -BackendAppNumber 3455 -FrontendAppNumber 3456
#>

[CmdletBinding()]
param(
    [string] $VrfRoot            = 'C:\MAK\vrforces5.0.2',
    [string] $RtiDir             = 'C:\MAK\makRti4.6.1',
    [string] $ConnectionProfile  = 'HLA 1516 Evolved RPR 2.0 with MAK extensions',
    [string] $Scenario           = 'TropicTortoise',
    [switch] $NoScenario,
    [int]    $BackendAppNumber   = 3455,
    [int]    $FrontendAppNumber  = 3456,
    [ValidateSet('Combined','BackendOnly','FrontendOnly')]
    [string] $Mode               = 'Combined',
    [int]    $ReadyTimeoutSec    = 120,
    [int]    $PollIntervalSec    = 3,
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
if (-not $PSBoundParameters.ContainsKey('BackendAppNumber') -or -not $PSBoundParameters.ContainsKey('FrontendAppNumber')) {
    Say-Warn 'Using DEFAULT app number(s). Both MUST be fresh per OPUS_EXECUTION_PLAN.md Appendix B (never reuse) and recorded there. Pass -BackendAppNumber/-FrontendAppNumber with the current NEXT-FREE values.'
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
$existing = @()
foreach ($n in @($procLauncher,$procBackend,$procFrontend,$procRti)) {
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
    Say-Ok 'no pre-existing vrfLauncher / vrfSimHLA1516e / vrfGui / rtiexec processes'
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
    Say-Plan ("set MAKLMGRD_LICENSE_FILE (process) = {0}  (refresh from Machine scope; RUNBOOK sec 7 item 2)" -f $licMachine)
    Say-Plan ("Start-Process -FilePath '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $launcher, $bin64, $argString)
    Say-Plan ("poll every {0}s up to {1}s for readiness signals:" -f $PollIntervalSec, $ReadyTimeoutSec)
    Say ("            - process '{0}' present  (back-end; RUNBOOK sec 0.5 authoritative signal)" -f $procBackend)
    Say ("            - process '{0}' present   (federate joined the RTI; RUNBOOK sec 0.5)" -f $procRti)
    Say ("            - process '{0}' present     (front-end; combined mode - the crash-avoiding piece)" -f $procFrontend)
    Say  '            - vrfGui MainWindowTitle non-empty (front-end window is up, not stuck in a modal dialog)'
    Say  '            - rtiexec TCP listening ports (Get-NetTCPConnection on rtiexec PID; passive)'
    Say-Plan 'declare READY only when back-end AND rtiexec are up (AND the front-end too in Combined mode - a slow vrfGui start keeps polling rather than misreporting PARTIAL); WARN (not ready) if back-end is up but rtiexec never appears (possible blocking dialog: LRC #8 / license / session-startup).'
    Say-Head 'Result'
    Say-Ok 'DRY-RUN complete: preconditions passed, command line resolved, no process launched.'
    exit 0
}

# --- live path (NOT executed in this drafting task) ---
$env:MAKLMGRD_LICENSE_FILE = $licMachine
Say-Ok ("MAKLMGRD_LICENSE_FILE (process) refreshed from Machine scope = {0}" -f $licMachine)

Say-Ok ("launching: {0} {1}" -f $launcher, $argString)
$launch = Start-Process -FilePath $launcher -WorkingDirectory $bin64 -ArgumentList $argString -PassThru
Say-Ok ("vrfLauncher started (pid {0}); polling for backend readiness..." -f $launch.Id)

$deadline   = (Get-Date).AddSeconds($ReadyTimeoutSec)
$backendUp  = $false
$rtiUp      = $false
$frontUp    = $false
$guiTitle   = ''
while ((Get-Date) -lt $deadline) {
    $b = Get-Process -Name $procBackend  -ErrorAction SilentlyContinue
    $r = Get-Process -Name $procRti      -ErrorAction SilentlyContinue
    $f = Get-Process -Name $procFrontend -ErrorAction SilentlyContinue
    $backendUp = [bool]$b
    $rtiUp     = [bool]$r
    $frontUp   = [bool]$f
    if ($f) { $guiTitle = ($f | Select-Object -First 1 -ExpandProperty MainWindowTitle) }
    # In Combined mode keep polling until the front-end is ALSO up (a slow vrfGui
    # start must not be misreported as PARTIAL - PARTIAL is the crash-risk signal
    # and should only fire when the front-end genuinely never appeared in time).
    $needFront = ($Mode -eq 'Combined')
    if ($backendUp -and $rtiUp -and ($frontUp -or -not $needFront)) { break }
    Start-Sleep -Seconds $PollIntervalSec
}

Say-Head 'Readiness'
if ($backendUp) { Say-Ok  ("back-end '{0}' is up" -f $procBackend) } else { Say-Fail ("back-end '{0}' NOT up" -f $procBackend) }
if ($rtiUp)     { Say-Ok  ("'{0}' is up (federate joined the RTI)" -f $procRti) } else { Say-Fail ("'{0}' NOT up (no federate join observed)" -f $procRti) }
if ($frontUp)   { Say-Ok  ("front-end '{0}' is up (window title: '{1}')" -f $procFrontend, $guiTitle) } else { Say-Warn ("front-end '{0}' NOT up - combined mode incomplete; this is the crash-risk 'missing front-end' condition" -f $procFrontend) }

# passive port signal for the record
if ($rtiUp) {
    try {
        $rtiPid = (Get-Process -Name $procRti -ErrorAction SilentlyContinue | Select-Object -First 1).Id
        $ports  = Get-NetTCPConnection -OwningProcess $rtiPid -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty LocalPort -Unique
        if ($ports) { Say-Ok ("rtiexec listening ports: {0}" -f ($ports -join ', ')) }
    } catch { Say-Warn ("could not read rtiexec listening ports: {0}" -f $_.Exception.Message) }
}

Say-Head 'Result'
if ($backendUp -and $rtiUp -and $frontUp) {
    Say-Ok 'READY: combined-mode VR-Forces is up (back-end + rtiexec + front-end). Proceed to the ResetVrf --dry-run gate (prereg).'
    exit 0
} elseif ($backendUp -and $rtiUp) {
    Say-Warn 'PARTIAL: back-end + rtiexec up but front-end not detected. Combined mode may be incomplete (crash risk). Inspect before proceeding.'
    exit 1
} else {
    Say-Fail 'NOT READY within timeout. Likely a blocking dialog (LRC #8 FDD path / license / session-startup) or a failed join. Do NOT force-kill; inspect the launcher/GUI window and clean-stop.'
    exit 3
}
