# StartRtiExec52.ps1 - ENSURE a headless MAK RTI 5.0.1 rtiexec is up for the 5.2 profile's rid.
#
# WHY THIS STAGE EXISTS (the ruling, 2026-09-04). UG52 5.5.1 p190 states flatly "You cannot
# use the MAK RTI in lightweight mode with VR-Forces" - the 5.0.2 qualifier "if you are
# running multiple, concurrent federation executions" (UG502 5.5.1 p186) was DROPPED in 5.2.
# Every 5.2d run before 2026-09-04 was lightweight (RTI_useRtiExec 0) and every observer
# reflected ZERO entities. PREREG_52_RTIEXEC_2026-09-04 ran the DOCUMENTED posture instead -
# MAK RTI 5.0.1 in rtiexec mode on the rid-configured connection, interface pinned to
# 127.0.0.1 - and the observer reflected 62 entities (P1 and P3 both HELD). That run changed
# TWO things at once: the RTI connection mode (what this script provides) and the
# VR-Forces-level interface address. The discriminator (sec 4 P4, run 3857) then FALSIFIED
# the second observer-side - an observer with a blank device address still reflected 54-56
# entities - so the connection mode is the repair and --deviceAddress is a tunable defaulting
# to off. Note the rid this script hands the rtiexec DOES pin the RTI's own interface
# (RTI_networkInterfaceAddr 127.0.0.1), which is inherent to a loopback-broadcast rtiexec
# connection and a DIFFERENT layer from --deviceAddress
# (RESEARCH_52_HLA_CONNECTION_CONFIG sec 2 lists all three layers).
# RM 14.3 p14-5 adds the second reason for rtiexec mode: "The
# use of FOM modules also requires the use of the rtiexec and that internal messages be sent
# reliably", and the 5.2 connection config declares 17 FOM modules.
#
# WHAT IT DOES - ENSURE-UP, NEVER RESTART. rtiexec / rtiForwarder / rtiAssistant are RTI
# INFRASTRUCTURE: this script NEVER kills, restarts or reconfigures one (RUNBOOK 0.5.2, the
# project's standing non-negotiable). If an rtiexec.exe from -RtiDir\bin is already running
# AND something is LISTENING on the TCP rendezvous port, the script reports READY and TOUCHES
# NOTHING - it does not check that the running rtiexec was started with this rid, because
# proving that would require killing it. THEY PERSIST ACROSS RUNS BY DESIGN: one rtiexec
# serves every run on this machine until the next reboot, so the common case is the
# already-up path and only the first run after a boot actually starts one.
#
# THE COMMAND LINE (RESEARCH_RTI_CONNECTION_TRANSPORT_2026-09-03 sec D Q4, from RTI UG
# Table 4-1 p4-9/4-10): -M|--manual is MANDATORY - without it -P/-T/-A/-r are IGNORED and the
# assistant's stored connection wins; -R <rid> overrides RTI_RID_FILE; -P udpPort; -T tcpPort;
# -A destAddrString (loopback BROADCAST 127.255.255.255, the 5.0.2 golden-path value);
# -N udpNetworkInterfaceAddr; -i tcpNetworkInterfaceAddr; -D distributedForwarderPort;
# -r useReliable; -l logfile; -n notify level. -K (autoExit) is DELIBERATELY NOT PASSED: the
# rtiexec must outlive the run that started it. The rtiexec starts its OWN rtiForwarder and
# exits if it cannot (RTI UG 4.2.1 p4-11, RM 5.3 p5-12), so the forwarder pid is discovered,
# never started here.
#
# VERIFICATION is a TCP LISTENER on the rendezvous port, not process presence (the health
# lesson: presence is not health). Federates reach the rtiexec through
# RTI_tcpForwarderAddr + RTI_tcpPort - there is no RTI_rtiExecAddr/Port setting at all
# (transport report D13) - so a listening TCP 4001 is the thing a federate can actually use.
#
# Exit codes: 0 READY (already up, or started and listening); 2 precondition/usage failure
# (NOTHING started); 3 NOT LISTENING within -ReadyTimeoutSec (started, but unproven - the
# caller must refuse the launch rather than join a federation with no rendezvous).
# ASCII only.
param(
    # MAK RTI 5.0.1 - the 5.2 profile's RTI. 4.6.1 must NOT appear anywhere in this profile:
    # its assistant version-rejects 5.0.1 federates and vice versa (memory: RTI assistant
    # version gate), and the 5.2 posture is defined on 5.0.1.
    [string] $RtiDir          = 'C:\MAK\makRti5.0.1',
    # The rid EVERY federate in the run must share (sim, tools, observers, app) and the one
    # the rtiexec is configured from. EMPTY = <repo>\config\rid-501-rtiexec-min.mtl - the
    # twelve parameters RTI UG 5.0.1 sec 7.3 p73 says an assistant rtiexec connection sets,
    # and NOTHING else (the superset rid-501-rtiexec.mtl crashed the 5.2 sim in
    # DtVrfSimOptions::parseCmdLine - PREREG_52_RTIEXEC sec 4).
    [string] $RidFile         = '',
    # Roots for the per-process PATH prefix. MAK DLLs bind BY NAME on PATH, so the rtiexec
    # must see the same stack the federates do (DIFF sec H, the 5.2 runtime path trap).
    [string] $VrfRoot         = 'C:\MAK\vrforces5.2d',
    [string] $VrLinkRoot      = 'C:\MAK\vrlink5.10',
    # Where rtiexec_<UTCstamp>.log lands. EMPTY = <repo>\runs\launch52 (gitignored). NEVER
    # under C:\MAK, and deliberately NOT a run directory: the rtiexec OUTLIVES the run that
    # started it and goes on serving later runs, so its log must not be filed under one run's
    # evidence as if it belonged there. The stamp keeps successive rtiexecs' logs apart.
    [string] $LogDir          = '',
    [int]    $UdpPort         = 4001,
    [int]    $TcpPort         = 4001,
    [string] $DestAddress     = '127.255.255.255',
    [string] $InterfaceAddress= '127.0.0.1',
    [int]    $ForwarderPort   = 5000,
    [int]    $NotifyLevel     = 3,
    [int]    $ReadyTimeoutSec = 30,
    [int]    $PollIntervalSec = 1,
    [switch] $DryRun
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Say      { param([string]$m) Write-Host $m }
function Say-Head { param([string]$m) Write-Host ''; Write-Host ('=== ' + $m + ' ===') }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Info { param([string]$m) Write-Host ('  [..]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [DRY-RUN] would ' + $m) }

$modeTag = if ($DryRun) { 'DRY-RUN' } else { 'LIVE' }
Say-Head ("StartRtiExec52.ps1 ({0}) - headless MAK RTI 5.0.1 rtiexec, ENSURE-UP (never restarted)" -f $modeTag)

# ---- ARGUMENTS FIRST, before anything is inspected or started (the LaunchVrf.ps1
# "validated too late" defect). Every failure here is exit 2 = NOTHING was started.
$argFail = @()
foreach ($p in @(@{n='UdpPort';v=$UdpPort}, @{n='TcpPort';v=$TcpPort}, @{n='ForwarderPort';v=$ForwarderPort})) {
    if ($p.v -lt 1 -or $p.v -gt 65535) { $argFail += ('-{0} must be 1..65535 (got {1}).' -f $p.n, $p.v) }
}
if ($NotifyLevel -lt 0 -or $NotifyLevel -gt 4)      { $argFail += ('-NotifyLevel must be 0..4 (got {0}).' -f $NotifyLevel) }
if ($ReadyTimeoutSec -lt 1 -or $ReadyTimeoutSec -gt 600) { $argFail += ('-ReadyTimeoutSec must be 1..600 (got {0}).' -f $ReadyTimeoutSec) }
if ($PollIntervalSec -lt 1 -or $PollIntervalSec -gt $ReadyTimeoutSec) { $argFail += ('-PollIntervalSec must be 1..ReadyTimeoutSec (got {0} with timeout {1}).' -f $PollIntervalSec, $ReadyTimeoutSec) }
foreach ($a in @(@{n='DestAddress';v=$DestAddress}, @{n='InterfaceAddress';v=$InterfaceAddress})) {
    if ($a.v -notmatch '^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$') { $argFail += ('-{0} must be a dotted IPv4 address (got "{1}").' -f $a.n, $a.v) }
}
if ($argFail.Count -gt 0) {
    Say-Head 'Result'
    foreach ($f in $argFail) { Say-Fail $f }
    Say-Fail 'Aborting on arguments. NOTHING was inspected and NOTHING was started.'
    exit 2
}

# ---- derived paths ---------------------------------------------------------
$repoRoot  = Split-Path -Parent $PSScriptRoot
$rtiBin    = Join-Path $RtiDir 'bin'
$rtiExe    = Join-Path $rtiBin 'rtiexec.exe'
$vrfBin64  = Join-Path $VrfRoot 'bin64'
$vrlBin64  = Join-Path $VrLinkRoot 'bin64'
$ridPath   = if ([string]::IsNullOrWhiteSpace($RidFile)) { Join-Path $repoRoot 'config\rid-501-rtiexec-min.mtl' } else { $RidFile }
$logDir    = if ([string]::IsNullOrWhiteSpace($LogDir))  { Join-Path $repoRoot 'runs\launch52' } else { $LogDir }
$logStamp  = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$logFile   = Join-Path $logDir ('rtiexec_{0}.log' -f $logStamp)

Say ("  RtiDir       : {0}" -f $RtiDir)
Say ("  rid (SHARED) : {0}" -f $ridPath)
Say ("  rendezvous   : TCP {0} / UDP {1} on {2}, dest {3}, forwarder port {4}" -f $TcpPort, $UdpPort, $InterfaceAddress, $DestAddress, $ForwarderPort)
Say ("  log          : {0}" -f $logFile)

$hardFail = $false
foreach ($chk in @(
    @{ p=$rtiExe;   what='MAK RTI 5.0.1 rtiexec.exe' },
    @{ p=$ridPath;  what='rid file (-R, and RTI_RID_FILE for the process)' },
    @{ p=$vrfBin64; what='VR-Forces 5.2d bin64 (PATH prefix)' },
    @{ p=$vrlBin64; what='VR-Link 5.10 bin64 (PATH prefix)' })) {
    if (Test-Path -LiteralPath $chk.p) { Say-Ok ("{0}: {1}" -f $chk.what, $chk.p) }
    else { Say-Fail ("{0} MISSING: {1}" -f $chk.what, $chk.p); $hardFail = $true }
}
if ($logDir -like 'C:\MAK*') {
    Say-Fail ("the rtiexec log would land under C:\MAK ({0}) - refused; nothing this project runs writes into the vendor tree." -f $logFile)
    $hardFail = $true
}
if ($RtiDir -match '4\.6\.1') {
    Say-Fail ('-RtiDir names the 4.6.1 tree. The 5.2 profile is defined on MAK RTI 5.0.1 (PREREG_52_RTIEXEC_2026-09-04); a 4.6.1 rtiexec cannot serve 5.0.1 federates.')
    $hardFail = $true
}
if ($hardFail) {
    Say-Head 'Result'
    if ($DryRun) { Say-Warn 'DRY-RUN: one or more HARD preconditions FAILED above. A live run would abort here.' }
    else { Say-Fail 'Aborting: hard precondition failure. NOTHING was started.' }
    exit 2
}

# ---- is one already up? ----------------------------------------------------
# TCP LISTENER, not process presence. Get-NetTCPConnection is the primary; netstat is the
# fallback for a host where the NetTCPIP module is unavailable. A listener on 0.0.0.0 or ::
# serves 127.0.0.1 too, so any listen state on the port counts.
function Get-TcpListener {
    param([int]$Port)
    $o = [ordered]@{ listening = $false; owningPid = $null; source = 'none' }
    try {
        $c = @(Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction Stop)
        if ($c.Count -gt 0) { $o.listening = $true; $o.owningPid = $c[0].OwningProcess; $o.source = 'Get-NetTCPConnection' }
        return $o
    } catch { }
    try {
        $pattern = ('^\s+TCP\s+\S+:{0}\s+\S+\s+LISTENING\s+(\d+)' -f $Port)
        $m = @(& netstat -ano -p tcp 2>$null | Select-String -Pattern $pattern)
        if ($m.Count -gt 0) { $o.listening = $true; $o.owningPid = [int]$m[0].Matches[0].Groups[1].Value; $o.source = 'netstat' }
    } catch { }
    return $o
}
function Get-RtiProcs {
    param([string]$Name, [string]$UnderDir)
    $out = @()
    foreach ($p in @(Get-Process -Name $Name -ErrorAction SilentlyContinue)) {
        $path = ''
        try { $path = $p.Path } catch { }
        # A process whose Path cannot be read (another user / elevation) is COUNTED, not
        # ignored: we must never start a second rtiexec on top of one we simply cannot see.
        if ([string]::IsNullOrWhiteSpace($path) -or $path.StartsWith($UnderDir, [System.StringComparison]::OrdinalIgnoreCase)) {
            $out += [pscustomobject]@{ Id = $p.Id; Path = $(if ($path) { $path } else { '(path not readable)' }) }
        }
    }
    return @($out)
}

# ---- THE PLAN, printed BEFORE the inventory decides whether it is needed -----
# Unconditional so -DryRun shows the exact command line whatever the machine state is:
# on a host that already has an rtiexec the ensure-up path exits before starting anything,
# and a plan printed only on the start path would be invisible exactly when a reviewer
# wants to read it.
$rtiArgs = @('-M', '-R', ('"{0}"' -f $ridPath),
             '-P', [string]$UdpPort, '-T', [string]$TcpPort,
             '-A', $DestAddress, '-N', $InterfaceAddress, '-i', $InterfaceAddress,
             '-D', [string]$ForwarderPort, '-r',
             '-l', ('"{0}"' -f $logFile), '-n', [string]$NotifyLevel)
$rtiArgString = ($rtiArgs -join ' ')
$pathPrefix   = '{0};{1};{2};' -f $vrfBin64, $vrlBin64, $rtiBin

Say-Head 'Plan (used ONLY if the inventory below finds no rtiexec)'
Say ("  process env : PATH={0}<inherited PATH>" -f $pathPrefix)
Say ("                MAK_RTIDIR={0}  RTI_RID_FILE={1}  RTI_ASSISTANT_DISABLE=1" -f $RtiDir, $ridPath)
Say ("  command     : {0} {1}" -f $rtiExe, $rtiArgString)
Say ("  cwd         : {0}" -f $rtiBin)
Say ("  readiness   : poll up to {0}s every {1}s for a TCP {2} LISTEN, then record the rtiexec and rtiForwarder pids" -f $ReadyTimeoutSec, $PollIntervalSec, $TcpPort)
Say  '  -M is MANDATORY: without it -P/-T/-A/-r are IGNORED (RTI UG Table 4-1). -K is NOT passed - the rtiexec must OUTLIVE this run.'

Say-Head 'Inventory (read-only; nothing here is ever killed or restarted - RUNBOOK 0.5.2)'
# PowerShell UNROLLS a single-element array on return, so every call site re-wraps in @()
# (the StopVrf.ps1 lesson - '$a + $b' fails on a bare [PSObject]).
$existingExec = @(Get-RtiProcs -Name 'rtiexec'      -UnderDir $rtiBin)
$existingFwd  = @(Get-RtiProcs -Name 'rtiForwarder' -UnderDir $rtiBin)
foreach ($p in @(@($existingExec) + @($existingFwd))) { Say-Ok ('rtiexec/forwarder pid={0} path={1}' -f $p.Id, $p.Path) }
if (@($existingExec).Count -eq 0) { Say-Info ('no rtiexec.exe from {0} is running' -f $rtiBin) }
$listen = Get-TcpListener -Port $TcpPort
if ($listen.listening) { Say-Ok ('TCP {0} is LISTENING (owner pid {1}, via {2})' -f $TcpPort, $listen.owningPid, $listen.source) }
else { Say-Info ('nothing is listening on TCP {0}' -f $TcpPort) }

$execPidText = if (@($existingExec).Count -gt 0) { [string]@($existingExec)[0].Id } else { 'none' }
$fwdPidText  = if (@($existingFwd).Count  -gt 0) { [string]@($existingFwd)[0].Id }  else { 'none' }

if (@($existingExec).Count -gt 0 -and $listen.listening) {
    Say-Head 'Result'
    Say-Ok ('ALREADY UP - an rtiexec from {0} is running and TCP {1} is listening. NOTHING was started, restarted or reconfigured.' -f $rtiBin, $TcpPort)
    Say-Ok 'RTI infrastructure PERSISTS ACROSS RUNS BY DESIGN: it is not torn down at the end of a run and only the first run after a reboot starts one.'
    # log= names the log of THE RTIEXEC THAT IS ACTUALLY SERVING, which on this path this
    # script did not start and cannot know - saying otherwise would point a reader at an
    # empty file. The directory below holds every rtiexec log this project has written.
    Say-Ok ('RTIEXEC READY rtiexec={0} forwarder={1} tcp={2}:{3} started=no log=(not started by this run - see {4})' -f $execPidText, $fwdPidText, $InterfaceAddress, $TcpPort, $logDir)
    exit 0
}
if (@($existingExec).Count -gt 0 -and (-not $listen.listening)) {
    # Present but not serviceable. It is STILL not ours to kill - report and fail, so a human
    # decides. Starting a second one would fight the first for the port.
    Say-Warn ('an rtiexec is running (pid {0}) but NOTHING is listening on TCP {1}. PRESENCE IS NOT HEALTH.' -f $execPidText, $TcpPort)
    Say-Warn '  It is NOT killed and NO second rtiexec is started (two would fight for the port).'
    Say-Head 'Result'
    Say-Fail ('NOT READY: rtiexec present, TCP {0} not listening. Inspect it (and {1}) before the next launch; never force-kill it.' -f $TcpPort, $logFile)
    exit 3
}

# ---- start one -------------------------------------------------------------
if ($DryRun) {
    Say-Head 'Result'
    Say-Plan ("Start-Process '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}' -WindowStyle Hidden" -f $rtiExe, $rtiBin, $rtiArgString)
    Say-Plan ("poll up to {0}s every {1}s for a TCP {2} LISTEN, then record the rtiexec and rtiForwarder pids" -f $ReadyTimeoutSec, $PollIntervalSec, $TcpPort)
    Say-Ok 'DRY-RUN complete: preconditions passed, nothing was started.'
    exit 0
}

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$env:PATH                 = $pathPrefix + $env:PATH
$env:MAK_RTIDIR           = $RtiDir
$env:RTI_RID_FILE         = $ridPath
$env:RTI_ASSISTANT_DISABLE= '1'

Say-Head 'Start'
$proc = Start-Process -FilePath $rtiExe -WorkingDirectory $rtiBin -ArgumentList $rtiArgString -WindowStyle Hidden -PassThru
Say-Ok ('rtiexec started (pid {0}) - it will OUTLIVE this run and every run after it, by design.' -f $proc.Id)

$deadline = (Get-Date).AddSeconds($ReadyTimeoutSec)
while ((Get-Date) -lt $deadline) {
    $listen = Get-TcpListener -Port $TcpPort
    if ($listen.listening) { break }
    if (-not (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue)) { break }
    Start-Sleep -Seconds $PollIntervalSec
}
$listen  = Get-TcpListener -Port $TcpPort
$alive   = [bool](Get-Process -Id $proc.Id -ErrorAction SilentlyContinue)
$nowFwd  = @(Get-RtiProcs -Name 'rtiForwarder' -UnderDir $rtiBin)
$fwdPidText = if (@($nowFwd).Count -gt 0) { [string]@($nowFwd)[0].Id } else { 'none' }

Say-Head 'Readiness'
if (-not $alive) { Say-Fail ('rtiexec pid {0} EXITED. The rtiexec exits when it cannot start its own rtiForwarder (RTI UG 4.2.1 p4-11) - read {1}.' -f $proc.Id, $logFile) }
elseif ($listen.listening) { Say-Ok ('TCP {0} LISTENING (owner pid {1}, via {2})' -f $TcpPort, $listen.owningPid, $listen.source) }
else { Say-Fail ('rtiexec pid {0} is alive but NOTHING is listening on TCP {1} after {2}s. PRESENCE IS NOT HEALTH.' -f $proc.Id, $TcpPort, $ReadyTimeoutSec) }
if (@($nowFwd).Count -gt 0) { Say-Ok ('rtiForwarder pid {0} (started BY the rtiexec, not by this script)' -f $fwdPidText) }
else { Say-Warn 'no rtiForwarder process visible - the rtiexec starts its own; reliable transport needs it (RM 10.1.2 p10-3).' }
if (Test-Path -LiteralPath $logFile) {
    Say-Ok ('rtiexec log tail ({0}):' -f $logFile)
    Get-Content -LiteralPath $logFile -Tail 10 -ErrorAction SilentlyContinue | ForEach-Object { Say ('         | ' + $_) }
} else { Say-Warn ('rtiexec log NOT created at {0}' -f $logFile) }

Say-Head 'Result'
if ($alive -and $listen.listening) {
    Say-Ok ('RTIEXEC READY rtiexec={0} forwarder={1} tcp={2}:{3} started=yes log={4}' -f $proc.Id, $fwdPidText, $InterfaceAddress, $TcpPort, $logFile)
    Say-Ok 'Federates must now share this rid (RTI_RID_FILE) or they will not share the connection.'
    exit 0
}
Say-Fail ('NOT READY within {0}s. NOTHING was killed. Read {1}, then re-run.' -f $ReadyTimeoutSec, $logFile)
exit 3
