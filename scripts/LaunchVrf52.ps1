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
# lightweight and the rtiexec rid, so THE TRIGGER IS UNKNOWN and it is NOT a rid property.
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
# Exit codes (same contract as LaunchVrf.ps1): 0 READY; 1 PARTIAL (back-end healthy,
# no front-end); 2 precondition/usage failure (nothing launched); 3 NOT READY within
# timeout, or the back-end CRASHED at startup; 4 BLOCKED (front-end process up, no
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
    # Back-end log. EMPTY = <repo>\runs\launch52\vrfSim_<appNo>_<UTCstamp>.log
    # (never C:\MAK\logs, the 5.2 default, and never under C:\MAK at all).
    [string] $LogFile            = '',
    [string] $ExConnConfigFile   = '',
    # rid with RTI_configureConnectionWithRid 1; EMPTY = the repo-owned
    # config\rid-501-rtiexec-min.mtl (the RTIEXEC posture - see the header; an rtiexec must
    # already be up for it, scripts\StartRtiExec52.ps1). Assistant use is DISABLED unless
    # -UseRtiAssistant, which reverts to $RtiDir\rid.mtl + assistant flow.
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
    # NOT part of the observation-channel repair: run 3857 (PREREG_52_RTIEXEC sec 4 P4) had an
    # observer with a BLANK device address reflect 54-56 entities off the rtiexec sim. The
    # runner therefore passes nothing by default and the sim launches unpinned - which is also
    # the open sim-side arm of that same test. 127.0.0.1 is the 5.0.2 configuration's value,
    # available here to pin deliberately, never because a 5.2 experiment demanded it.
    [string] $DeviceAddress      = '',
    # Where MAK's crash handler writes <exe><ver>-<date>-<time>-<host>-<build>-<pid>.callstack
    # .log (and the matching .dmp) - see the STARTUP CRASH block in the header.
    [string] $MakLogDir          = 'C:\MAK\logs',
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
Say ("  DeviceAddress     : {0}" -f $(if ([string]::IsNullOrWhiteSpace($DeviceAddress)) { '(empty - --deviceAddress/--hostAddressString NOT passed; VR-Forces picks the first device listed)' } else { $DeviceAddress }))
Say ("  MakLogDir         : {0} (startup-crash callstacks)" -f $MakLogDir)

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
if ($appNoFail) { Say-Head 'Result'; Say-Fail 'Aborting: argument gate failed. NOTHING was launched.'; exit 2 }

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
    Say-Plan ("watch the SAME poll for a STARTUP CRASH (0xC0000005 in DtVrfSimOptions::parseCmdLine, 2 of 5 launches on 2026-09-04, trigger UNKNOWN): back-end gone, a MAK crash-box window title, or a new {0}\vrfSimHLA1516e*-<pid>.callstack.log. On any of them: print the first frames and exit 3, WITHOUT retrying." -f $MakLogDir)
    Say-Ok 'DRY-RUN complete: preconditions passed, nothing launched.'
    exit 0
}

# ---- STARTUP-CRASH DETECTOR (see the header block) --------------------------
# Returns a record for the back-end pid: crashed yes/no, which signature fired, the
# callstack file if MAK wrote one, and its first frames. Read-only and NEVER kills: a
# crashed federate is already dead, and its crash box is answered by
# scripts\AnswerCrashDumpDialog.ps1, not here.
function Get-SimCrashEvidence {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since)
    $o = [ordered]@{ Crashed = $false; Reason = ''; File = ''; Frames = @() }
    # (c) the callstack file - the strongest signature, and the only one that survives the
    # process exiting before we look. The faulting pid is the LAST field of the name.
    # -Since IS LOAD-BEARING, not tidiness: C:\MAK\logs accumulates callstacks across boots
    # and Windows RECYCLES pids, so a file from a long-dead process that happened to hold
    # this pid would fail a perfectly healthy launch. Only a file written at or after this
    # back-end started can be this back-end's.
    try {
        $cs = @(Get-ChildItem -LiteralPath $LogDir -Filter ('*-{0}.callstack.log' -f $ProcessId) -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $Since } |
                Sort-Object LastWriteTime -Descending)
        if ($cs.Count -gt 0) {
            $o.Crashed = $true
            $o.Reason  = 'MAK crash handler wrote a callstack for this pid'
            $o.File    = $cs[0].FullName
            $o.Frames  = @(Get-Content -LiteralPath $cs[0].FullName -TotalCount 12 -ErrorAction SilentlyContinue)
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
if (-not [string]::IsNullOrWhiteSpace($licMachine)) { $env:MAKLMGRD_LICENSE_FILE = $licMachine }
$env:PATH         = $pathPrefix + $env:PATH
$env:MAK_VRFDIR   = $VrfRoot
$env:MAK_VRLDIR   = $VrLinkRoot
$env:MAK_RTIDIR   = $RtiDir
$env:RTI_RID_FILE = $ridFile
if (-not $UseRtiAssistant) { $env:RTI_ASSISTANT_DISABLE = '1' }
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

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
    Say-Fail '  KNOWN AND UNEXPLAINED: 0xC0000005 in makVrf::DtVrfSimOptions::parseCmdLine hit 2 of 5 launches on 2026-09-04, under BOTH the lightweight and the rtiexec rid - the trigger is NOT the rid and is NOT known. NOT retried here.'
    Say-Fail '  The process is already dead; MAK''s crash box (if any) is answered by scripts\AnswerCrashDumpDialog.ps1. Nothing is killed.'
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
if (Test-Path -LiteralPath $LogFile) {
    Say-Ok ("back-end log tail ({0}):" -f $LogFile)
    Get-Content -LiteralPath $LogFile -Tail 12 -ErrorAction SilentlyContinue | ForEach-Object { Say ('         | ' + $_) }
} else { Say-Warn ("back-end log NOT created at {0} (--logFileName ignored, or the process died before logging)" -f $LogFile) }

Say-Head 'Result'
if ($simCrash.Crashed) {
    # Checked FIRST: a crashed back-end can momentarily still satisfy the thread-count
    # oracle, and "READY" on a process with a callstack file would be the worst false green
    # this script could produce.
    Say-Fail ('CRASHED: the 5.2d back-end died at startup ({0}). NOT READY, NOT retried. Exit 3.' -f $simCrash.Reason)
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
