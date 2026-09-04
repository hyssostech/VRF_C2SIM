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
# ./bin), -n|--notifyLevel 0-4 (default 2), --logFileName (5.2 default lands in
# C:\MAK\logs - we always redirect it), -q|--doNotUseConsole.
#
# ENVIRONMENT (the two 5.2 runtime traps, DIFF sec H): MAK DLLs bind BY NAME on
# PATH and the Machine PATH lists vrforces5.0.2 / vrlink5.8 first, so this script
# PREFIXES the process PATH with the 5.2d stack. The RTI 5.0.1 installer also set
# MAK_RTIDIR / RTI_RID_FILE to makRti5.0.1 at Machine scope while the 1516e gates
# stay on makRti4.6.1, so BOTH are overridden per process. Nothing is written to
# Machine/User scope; the 5.0.2 script and its environment are untouched.
#
# NO RTI ASSISTANT (2026-09-03, PREREG_52_LAUNCH result): the 5.0.1 installer left
# an ELEVATED 5.0.1 rtiAssistant on port 6003 that version-rejects every 4.6.1
# federate ("RTI component was using a different RTI version than the RTI
# Assistant" - the assistant's own toast). Fix, both documented: set
# RTI_ASSISTANT_DISABLE (existence alone disables assistant use, MAK RTI 4.6.1
# Reference Manual 5.2.10) and take the connection from the rid instead
# (RTI_configureConnectionWithRid 1 - the ONE edit in the repo-owned rid copy
# config/rid-461-ridconfigured.mtl; lightweight, no rtiexec, UDP/TCP 4000,
# multicast 229.7.7.7). Verified 2026-09-03: rtiSimple1516e_64 (4.6.1) joins and
# exchanges interactions under this env with no assistant contact and no dialog.
# This removes the once-per-boot "Choose RTI Connection" dependency entirely for
# processes launched here. Every federate that must interoperate with this sim
# (RtiProbe/WatchVrf/CreateOne/app) needs the SAME env: assistant disabled + the
# SAME rid file, or it will not share a connection.
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
# Exit codes (same contract as LaunchVrf.ps1): 0 READY; 1 PARTIAL (back-end healthy,
# no front-end); 2 precondition/usage failure (nothing launched); 3 NOT READY within
# timeout; 4 BLOCKED (front-end process up, no window title).
# Non-negotiables: NEVER kill rtiAssistant / rtiexec / rtiForwarder; fresh ledgered
# app numbers per join (OPUS_EXECUTION_PLAN.md App. B, NEXT FREE marker). ASCII-only.
param(
    [string] $VrfRoot            = 'C:\MAK\vrforces5.2d',
    [string] $VrLinkRoot         = 'C:\MAK\vrlink5.10',
    [string] $RtiDir             = 'C:\MAK\makRti4.6.1',
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
    # Back-end log. EMPTY = <repo>\runs\launch52\vrfSim_<appNo>_<UTCstamp>.log
    # (never C:\MAK\logs, the 5.2 default, and never under C:\MAK at all).
    [string] $LogFile            = '',
    [string] $ExConnConfigFile   = '',
    # rid with RTI_configureConnectionWithRid 1; EMPTY = the repo-owned
    # config\rid-461-ridconfigured.mtl. Assistant use is DISABLED (see header)
    # unless -UseRtiAssistant, which reverts to $RtiDir\rid.mtl + assistant flow.
    [string] $RidFile            = '',
    [switch] $UseRtiAssistant,
    [int]    $ReadyTimeoutSec    = 120,
    [int]    $PollIntervalSec    = 3,
    [int]    $BackendMinThreads  = 8,
    [switch] $IgnoreUnansweredRtiAssistant,
    [switch] $AllowExistingVrf,
    [switch] $QuietBackend,
    # The 5.0.2 Launcher's "Network Interface Address" (its profile <hostAddress>): the card
    # for UDP/best-effort traffic. UG52 Table 10 p177 / Table 11 p180-181: --deviceAddress and
    # --hostAddressString|-H on BOTH vrfGui and vrfSim. EMPTY = not passed (VR-Forces picks
    # "the first device listed", IOG 5.2.1 p81). RESEARCH_52_HLA_CONNECTION_CONFIG P3.
    [string] $DeviceAddress      = '',
    [switch] $DryRun
)
$ErrorActionPreference = 'Stop'

function Say      { param([string]$m) Write-Host $m }
function Say-Head { param([string]$m) Write-Host ''; Write-Host ('=== ' + $m + ' ===') }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [DRY-RUN] would ' + $m) }

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

# ---- app-number gate (hard, checked first, nothing launched on failure) ----
$appNoFail = $false
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
if ($appNoFail) { Say-Head 'Result'; Say-Fail 'Aborting: app-number gate failed.'; exit 2 }

# ---- derived paths ---------------------------------------------------------
$bin64      = Join-Path $VrfRoot 'bin64'
$simExe     = Join-Path $bin64 'vrfSimHLA1516e.exe'
$guiExe     = Join-Path $bin64 'vrfGui.exe'
$vrlBin     = Join-Path $VrLinkRoot 'bin64'
$rtiBin     = Join-Path $RtiDir 'bin'
$repoRootEarly = Split-Path -Parent $PSScriptRoot
$ridFile    = if ($UseRtiAssistant) { Join-Path $RtiDir 'rid.mtl' }
              elseif ([string]::IsNullOrWhiteSpace($RidFile)) { Join-Path $repoRootEarly 'config\rid-461-ridconfigured.mtl' }
              else { $RidFile }
$connDir    = Join-Path $VrfRoot 'appData\settings\connections'
$connFile   = if ([string]::IsNullOrWhiteSpace($ExConnConfigFile)) { Join-Path $connDir 'MAK-ONE-2025-Config.xml' } else { $ExConnConfigFile }
$scenarioRel = ''
$scenarioAbs = ''
if (-not [string]::IsNullOrWhiteSpace($Scenario)) {
    $scenarioRel = '../userData/scenarios/' + ($Scenario -replace '\\', '/') + '.scnx'
    $scenarioAbs = Join-Path $VrfRoot ('userData\scenarios\{0}.scnx' -f $Scenario)
}
$repoRoot   = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $stamp   = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $LogFile = Join-Path $repoRoot ('runs\launch52\vrfSim_{0}_{1}.log' -f $BackendAppNumber, $stamp)
}
$licMachine = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')

$procBackend  = 'vrfSimHLA1516e'
$procFrontend = 'vrfGui'

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
$logDir = Split-Path -Parent $LogFile
if ($logDir -like 'C:\MAK*') { Say-Fail ("log file would land under C:\MAK ({0}) - refused; pass -LogFile outside the vendor tree." -f $LogFile); $hardFail = $true }
else { Say-Ok ("back-end log: {0}" -f $LogFile) }

# Mixed-RTI environment report (Machine scope, informational - overridden per process)
$mRti = [Environment]::GetEnvironmentVariable('MAK_RTIDIR','Machine')
$mRid = [Environment]::GetEnvironmentVariable('RTI_RID_FILE','Machine')
if ($mRti -and ($mRti -ne $RtiDir)) { Say-Warn ("Machine MAK_RTIDIR={0} differs from -RtiDir {1}; overriding MAK_RTIDIR and RTI_RID_FILE for the launched processes only." -f $mRti, $RtiDir) }
else { Say-Ok ("Machine MAK_RTIDIR={0}" -f $(if ($mRti) { $mRti } else { '(unset)' })) }
if ($mRid) { Say ("         Machine RTI_RID_FILE={0}" -f $mRid) }

# Stale VR-Forces processes (federates). rtiexec/rtiForwarder/rtiAssistant are RTI
# infrastructure: reported, never refused on, NEVER killed.
$existing = @()
foreach ($n in @('vrfLauncher',$procBackend,$procFrontend)) {
    $p = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($p) { $existing += ($p | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($existing.Count -gt 0) {
    Say-Warn ("VR-Forces processes ALREADY running: {0}" -f ($existing -join ', '))
    if (-not $AllowExistingVrf) { Say-Fail '  Refusing to launch on top of existing VR-Forces processes (-AllowExistingVrf overrides deliberately).'; $hardFail = $true }
    else { Say-Warn '  -AllowExistingVrf set: proceeding despite existing processes.' }
} else { Say-Ok 'no pre-existing vrfLauncher / vrfSimHLA1516e / vrfGui processes' }
$infra = @()
foreach ($n in @('rtiexec','rtiForwarder')) {
    $ip = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($ip) { $infra += ($ip | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($infra.Count -gt 0) { Say-Ok ("RTI infrastructure present (expected; do NOT kill): {0}" -f ($infra -join ', ')) }
else { Say-Ok 'no rtiexec / rtiForwarder running yet (connection-dependent; never a readiness gate).' }
$assist = Get-Process -Name 'rtiAssistant' -ErrorAction SilentlyContinue
if (-not $UseRtiAssistant) {
    if ($assist) {
        $ids = ($assist | ForEach-Object { $_.Id }) -join ', '
        Say-Ok ("assistant-free mode (RTI_ASSISTANT_DISABLE + rid-configured connection): existing rtiAssistant pid(s) {0} are IGNORED by the launched processes (a 5.0.1 assistant version-rejects 4.6.1 federates - the reason this mode is the default). Never kill them." -f $ids)
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
if ([string]::IsNullOrWhiteSpace($licMachine) -and [string]::IsNullOrWhiteSpace($env:MAKLMGRD_LICENSE_FILE)) {
    Say-Warn 'MAKLMGRD_LICENSE_FILE empty in Machine AND process scope - license checkout may hang.'
} else { Say-Ok ("MAKLMGRD_LICENSE_FILE = {0}" -f $(if ($licMachine) { $licMachine } else { $env:MAKLMGRD_LICENSE_FILE })) }

# ---- argument strings (UG52 4.1.2 / 4.1.3 / Table 11 / Table 12) ------------
$simArgs = @('--siteId', $SiteId, '--appNumber', $BackendAppNumber, '--sessionId', $SessionId,
             '--notifyLevel', $NotifyLevel, '--logFileName', ('"{0}"' -f $LogFile))
if ($scenarioRel) { $simArgs += @('--scenarioFileName', ('"{0}"' -f $scenarioRel)) }
if (-not [string]::IsNullOrWhiteSpace($ExConnConfigFile)) { $simArgs += @('--exConnConfigFile', ('"{0}"' -f $ExConnConfigFile)) }
if ($QuietBackend) { $simArgs += '--doNotUseConsole' }
if (-not [string]::IsNullOrWhiteSpace($DeviceAddress)) { $simArgs += @('--deviceAddress', $DeviceAddress, '--hostAddressString', $DeviceAddress) }
$guiArgs = @('--siteId', $SiteId, '--appNumber', $FrontendAppNumber, '--sessionId', $SessionId, '--hla1516e')
if (-not [string]::IsNullOrWhiteSpace($ExConnConfigFile)) { $guiArgs += @('--exConnConfigFile', ('"{0}"' -f $ExConnConfigFile)) }
if (-not [string]::IsNullOrWhiteSpace($DeviceAddress)) { $guiArgs += @('--deviceAddress', $DeviceAddress, '--hostAddressString', $DeviceAddress) }
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

if ($hardFail) {
    Say-Head 'Result'
    if ($DryRun) { Say-Warn 'DRY-RUN: one or more HARD preconditions FAILED above. A live run would abort here.' }
    else { Say-Fail 'Aborting: hard precondition failure (see above).' }
    exit 2
}
if ($DryRun) {
    Say-Head 'Result'
    Say-Plan ("Start-Process '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $simExe, $bin64, $simArgString)
    if (-not $NoGui) { Say-Plan ("Start-Process '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $guiExe, $bin64, $guiArgString) }
    Say-Plan ("poll up to {0}s every {1}s: back-end threads > {2}{3}" -f $ReadyTimeoutSec, $PollIntervalSec, $BackendMinThreads, $(if ($NoGui) { '' } else { ' AND vrfGui MainWindowTitle non-empty' }))
    Say-Ok 'DRY-RUN complete: preconditions passed, nothing launched.'
    exit 0
}

# ---- LIVE ------------------------------------------------------------------
if (-not [string]::IsNullOrWhiteSpace($licMachine)) { $env:MAKLMGRD_LICENSE_FILE = $licMachine }
$env:PATH         = $pathPrefix + $env:PATH
$env:MAK_VRFDIR   = $VrfRoot
$env:MAK_VRLDIR   = $VrLinkRoot
$env:MAK_RTIDIR   = $RtiDir
$env:RTI_RID_FILE = $ridFile
if (-not $UseRtiAssistant) { $env:RTI_ASSISTANT_DISABLE = '1' }
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

Say-Head 'Launch'
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
while ((Get-Date) -lt $deadline) {
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

Say-Head 'Readiness'
if (-not $backendUp) {
    Say-Fail ("back-end pid {0} NOT present (exit code {1}) - check the log: {2}" -f $simProc.Id, $backendExit, $LogFile)
} elseif ($backendHealthy) {
    Say-Ok ("back-end pid {0} is HEALTHY by thread count ({1} threads; floor {2})" -f $simProc.Id, $backendThr, $BackendMinThreads)
} else {
    Say-Fail ("back-end pid {0} PRESENT BUT NOT HEALTHY - {1} threads (floor {2}). PROCESS PRESENCE IS NOT HEALTH. Suspects: unanswered RTI Assistant prompt, RTI version mismatch (assistant vs makRti4.6.1 federate), license, connection config." -f $simProc.Id, $backendThr, $BackendMinThreads)
}
if ($needFront) {
    if ($frontUp -and $guiTitleOk) { Say-Ok ("front-end pid {0} up with a real main window (title: '{1}')" -f $guiProc.Id, $guiTitle) }
    elseif ($frontUp) { Say-Fail ("front-end pid {0} exists but MainWindowTitle is EMPTY - blocking modal dialog signature (Scenario Startup dialog, license/LRC box). Look at the screen; do not kill." -f $guiProc.Id) }
    else { Say-Warn ("front-end pid {0} NOT up (exit code {1})" -f $guiProc.Id, $frontExit) }
}
if (Test-Path -LiteralPath $LogFile) {
    Say-Ok ("back-end log tail ({0}):" -f $LogFile)
    Get-Content -LiteralPath $LogFile -Tail 12 -ErrorAction SilentlyContinue | ForEach-Object { Say ('         | ' + $_) }
} else { Say-Warn ("back-end log NOT created at {0} (--logFileName ignored, or the process died before logging)" -f $LogFile) }

Say-Head 'Result'
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
