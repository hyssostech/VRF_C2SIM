<#
.SYNOPSIS
  START THE PERSISTENT FEDERATION HOLDER for a demo day (STP-825). Operator-facing.

.DESCRIPTION
  Promoted 2026-09-21 from the seat scratchpad script `validation\p7_holder_retry.ps1`, which
  DEMO_RUNBOOK section 0 used to name - a session-specific path under %TEMP% that no operator
  could follow (defect DR-6). The behaviour is the scratchpad script's, unchanged.

  WHY IT EXISTS. rtiexec 5.0.1 rejects a federation CREATOR's FOM-module distribution
  intermittently - measured at roughly 42% of creates on the long-lived rtiexec of this machine
  (RUNBOOK sec 9c, D3 2026-09-20). A JOIN never exercises that receive path, so if ONE federate
  holds the federation open, every later launch only ever JOINS and the defect is AVOIDED rather
  than retried. LaunchVrf52.ps1 starts its own 900-second holder per launch, which is not enough
  for a demo day; this script starts one holder for as long as you ask and retries over the
  application numbers YOU supply until one create succeeds.

  It starts exactly one extra federate (tools\RtiProbe.exe) in the RTIEXEC posture the sim uses
  (assistant disabled, repo rid file, cwd = VR-Forces bin64) and nothing else. It never kills
  anything, never touches rtiexec / rtiForwarder / rtiAssistant, and writes only under
  -OutDir (runs\holder52 by default). The resulting live `RtiProbe.exe` is EXPECTED for the
  rest of the demo day - it is the holder, not a leftover.

  PRECONDITIONS (all refused with exit 2, having started nothing):
    - a headless rtiexec must already be up (scripts\StartRtiExec52.ps1);
    - the machine must be idle of our simulation processes - vrfSim*, WatchVrf*, VrfC2SimApp*
      and any RtiProbe* (an RtiProbe already running is very likely a holder you already have);
    - no runner lock (runs\runner.lock) may be held;
    - the rtiexec log this script reads the join out of must exist in -RtiLogDir.

.PARAMETER AppNumbers
  MANDATORY, no default: the HLA application numbers to try, in order, one per create attempt.
  THEY COME FROM THE LEDGER OR FROM THE OPERATOR - this script invents none, because a reused
  application number is a stale-federate hang (RUNBOOK sec 0). Numbers not reached on a given
  try are burned, never reused. Test-federate numbers come from the Appendix B marker in
  docs\OPUS_EXECUTION_PLAN.md ("*** NEXT FREE: ... ***"); a demo-day holder may instead use
  free numbers of the demo block 9101-9199 - which are NOT ledgered, but 9101 (the interface),
  9102/9103 (DEMO_RUNBOOK's Way B back end / front end) and 9190/9191 (LaunchVrf52's own
  per-launch holder) are already spoken for.

.PARAMETER SettleSecs
  How long the holder stays joined, in seconds. 900 (the RtiProbe default posture) is enough
  for one launch; a demo DAY wants 28800 (8 hours). The holder resigns by itself afterwards.

.PARAMETER Federation
  Federation execution name to create-or-join. Default MAK-ONE-2025, the name the 5.2
  connection config carries and the one every other 5.2 federate here uses.

.PARAMETER JoinTimeoutSec
  How long to watch the rtiexec log for this attempt's "has joined federation" line before
  calling the attempt failed and moving to the next application number. Default 45.

.PARAMETER OutDir
  Where each attempt's stdout/stderr is written. Default runs\holder52 under the repository
  (runs\ is gitignored). Created if missing. Nothing is ever written under C:\MAK.

.PARAMETER RtiLogDir
  Where scripts\StartRtiExec52.ps1 puts the rtiexec log this script reads the join line out of.
  Default runs\launch52 under the repository.

.PARAMETER WhatIf
  Print the plan - every command line, the environment, the application numbers in order - and
  start nothing. Preconditions are still evaluated and reported; the exit code is the one a
  real run would have used.

.EXAMPLE
  pwsh -NoProfile -File scripts\StartFederationHolder52.ps1 -AppNumbers 4651,4652,4653,4654 -SettleSecs 28800
  Once per demo day, before the first LaunchVrf52. EXPECT "HOLDER JOINED: pid ... appNo ...".

.NOTES
  Exit 0 = a holder is joined and holding. 1 = every application number was tried and none
  joined (do NOT keep launching blindly; RUNBOOK sec 9c). 2 = a precondition refused the run.

  KNOWN LIMIT, carried over from the scratchpad script this replaces: the holder is started
  with -NoNewWindow, so it SHARES this console. It outlives this script (it is its own
  process), but closing the window it was started from may take it with it. Leave that window
  open for the demo day, or check with `Get-Process RtiProbe` before each launch. Changing it
  to a detached console is a behaviour change this promotion deliberately did not make.
#>
[CmdletBinding()]
param(
    # [string[]], NOT [int[]], AND PARSED BELOW. `pwsh -File ... -AppNumbers 4651,4652` hands
    # PowerShell the single STRING "4651,4652"; bound to an [int[]] parameter that silently
    # becomes the one number 46514652 - a bogus application number, accepted without a word
    # (measured 2026-09-21 while writing this script). As strings the value is split on commas
    # and whitespace and each piece is parsed strictly, so both `-File` and a real array work
    # and anything else is refused by name.
    [Parameter(Mandatory)][string[]] $AppNumbers,
    [int]    $SettleSecs     = 900,
    [string] $Federation     = 'MAK-ONE-2025',
    [int]    $JoinTimeoutSec = 45,
    [string] $OutDir         = '',
    [string] $RtiLogDir      = '',
    [string] $VrfRoot        = 'C:\MAK\vrforces5.2d',
    [string] $VrLinkRoot     = 'C:\MAK\vrlink5.10',
    [string] $RtiDir         = 'C:\MAK\makRti5.0.1',
    [string] $RidFile        = '',
    [switch] $WhatIf
)
# Continue, not Stop: an attempt that fails is DATA here - the script reports it and tries the
# next application number. This is the scratchpad script's own setting, kept deliberately.
$ErrorActionPreference = 'Continue'

$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutDir)    { $OutDir    = Join-Path $RepoRoot 'runs\holder52' }
if (-not $RtiLogDir) { $RtiLogDir = Join-Path $RepoRoot 'runs\launch52' }
if (-not $RidFile)   { $RidFile   = Join-Path $RepoRoot 'config\rid-501-rtiexec-min.mtl' }
$Bin64 = Join-Path $VrfRoot 'bin64'
$Exe   = Join-Path $RepoRoot 'tools\RtiProbe\bin\Release-5.2\net10.0\win-x64\RtiProbe.exe'

function Say      { param([string]$m) Write-Host ('  ' + $m) }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) -ForegroundColor Red }
function Say-Plan { param([string]$m) Write-Host ('  [WhatIf] ' + $m) }
function Stamp    { (Get-Date).ToUniversalTime().ToString('HH:mm:ss') + 'Z' }

# Split on commas and whitespace, then parse each piece strictly. See the -AppNumbers comment.
$AppNos      = @()
$badAppNos   = @()
foreach ($piece in @($AppNumbers | ForEach-Object { [regex]::Split([string]$_, '[,\s]+') })) {
    if ([string]::IsNullOrWhiteSpace($piece)) { continue }
    $v = 0
    if ([int]::TryParse($piece.Trim(), [ref]$v)) { $AppNos += $v } else { $badAppNos += $piece.Trim() }
}

Write-Host ('=== StartFederationHolder52 ({0}) - STP-825 persistent federation holder ===' -f `
    $(if ($WhatIf) { 'WhatIf' } else { 'LIVE' }))
Say ('federation   : {0}' -f $Federation)
Say ('appNumbers   : {0}   (in order, one create attempt each - FROM THE LEDGER, never invented)' -f ($AppNos -join ', '))
Say ('settle       : {0}s (the holder resigns by itself after that)' -f $SettleSecs)
Say ('holder tool  : {0}' -f $Exe)
Say ('out dir      : {0}' -f $OutDir)

# ---- preconditions: every one of these refuses with exit 2, having started nothing ----
$problems = @()
foreach ($p in @(@{p=$Exe;   w='RtiProbe.exe (build: dotnet build tools\RtiProbe\RtiProbe.csproj -c Release -p:BridgeConfig=Release-5.2 -t:Rebuild)'},
                 @{p=$Bin64; w='VR-Forces 5.2d bin64 (the holder''s working directory)'},
                 @{p=(Join-Path $VrLinkRoot 'bin64'); w='VR-Link 5.10 bin64'},
                 @{p=(Join-Path $RtiDir 'bin'); w='MAK RTI 5.0.1 bin'},
                 @{p=$RidFile; w='rid file (the RTIEXEC posture)'})) {
    if (-not (Test-Path -LiteralPath $p.p)) { $problems += ('missing {0}: {1}' -f $p.w, $p.p) }
}
foreach ($b in $badAppNos) { $problems += ('-AppNumbers value is not a whole number: {0}' -f $b) }
if ($AppNos.Count -lt 1) { $problems += 'at least one -AppNumbers value is required' }
foreach ($n in $AppNos) { if ($n -lt 1 -or $n -gt 65535) { $problems += ('application number out of range 1..65535: {0}' -f $n) } }
if (@($AppNos | Sort-Object -Unique).Count -ne $AppNos.Count) {
    $problems += ('-AppNumbers contains a DUPLICATE: {0}. A reused application number is a stale-federate hang (RUNBOOK sec 0).' -f ($AppNos -join ', '))
}
if ($SettleSecs -lt 1 -or $SettleSecs -gt 86400) { $problems += ('-SettleSecs must be 1..86400 (got {0})' -f $SettleSecs) }

# The busy check is by process NAME only and NEVER kills: a live RtiProbe is very likely a
# holder that is already doing this job, and a live vrfSim/VrfC2SimApp means a demo or a run is
# in progress - either way this is not the moment to start another federate.
$busy = @(Get-Process vrfSim*, vrfGui*, WatchVrf*, VrfC2SimApp*, RtiProbe* -ErrorAction SilentlyContinue)
if ($busy.Count -gt 0) {
    $problems += ('not idle - ' + (($busy | ForEach-Object { $_.Name + ':' + $_.Id }) -join ' ') +
                  '. Nothing is killed here. A live RtiProbe is probably the holder you already have.')
}
if (Test-Path -LiteralPath (Join-Path $RepoRoot 'runs\runner.lock')) {
    $problems += 'runs\runner.lock is present - a harness run holds the launch lock. Wait for it.'
}
$rtiexec = @(Get-Process rtiexec -ErrorAction SilentlyContinue)
$RtiLog  = $null
if ($rtiexec.Count -lt 1) {
    $problems += 'rtiexec is not running - start it first: pwsh -NoProfile -File scripts\StartRtiExec52.ps1'
} else {
    Say-Ok ('rtiexec pid {0} (NEVER restarted or killed by this script)' -f $rtiexec[0].Id)
    if (Test-Path -LiteralPath $RtiLogDir) {
        $RtiLog = Get-ChildItem -LiteralPath $RtiLogDir -Filter ('rtiexec_*-' + $rtiexec[0].Id + '.log') -ErrorAction SilentlyContinue |
                  Select-Object -First 1
    }
    if ($null -eq $RtiLog) {
        $problems += ('no rtiexec_*-{0}.log under {1} - the join line is read out of that log, so this script cannot tell a join from a refusal without it.' -f $rtiexec[0].Id, $RtiLogDir)
    } else {
        Say-Ok ('rtiexec log  : {0}' -f $RtiLog.FullName)
    }
}
if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Say-Fail $_ }
    Say-Fail 'REFUSED at preconditions. NOTHING was started.'
    # -WhatIf is a PLANNING mode: the plan below is printed anyway, so it can be read from a
    # machine (or a worktree) that is not set up to run it. The exit code is unchanged.
    if (-not $WhatIf) { exit 2 }
}

# ---- the environment the holder needs: identical to the sim's (RUNBOOK 0.5.13 / 0.5.15) ----
# MINIMAL licence resolution, User scope then Machine (RUNBOOK 0.5.15): a process started before
# the 2026-09-14 renewal hands its children the STALE path, and an expired licence surfaces as a
# federate that dies at startup rather than as a licence error. This is the two-line form, NOT a
# fifth copy of Resolve-MakLicenseFile (RunC2SimScenario / LaunchVrf52 / StartInterface52 /
# RunScenario.sh hold the four byte-identical copies; nothing here changes that rule).
$lic = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE', 'User')
if (-not $lic) { $lic = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE', 'Machine') }
$envVars = [ordered]@{
    MAKLMGRD_LICENSE_FILE = $lic
    MAK_VRFDIR            = $VrfRoot
    MAK_VRLDIR            = $VrLinkRoot
    MAK_RTIDIR            = $RtiDir
    RTI_RID_FILE          = $RidFile
    RTI_ASSISTANT_DISABLE = '1'
}
$pathPrefix = ('{0};{1};{2};' -f $Bin64, (Join-Path $VrLinkRoot 'bin64'), (Join-Path $RtiDir 'bin'))
Say ('PATH prefix  : {0}' -f $pathPrefix)
foreach ($k in $envVars.Keys) { Say ('{0,-23}= {1}' -f $k, $envVars[$k]) }
Say ('cwd          : {0}' -f $Bin64)

if ($WhatIf) {
    $rtiLogSay = $(if ($null -ne $RtiLog) { $RtiLog.FullName } else { ('<the rtiexec_*-<pid>.log under {0}>' -f $RtiLogDir) })
    foreach ($n in $AppNos) {
        Say-Plan ('would start "{0}" {1} {2} 1 {3} 3   (cwd {4}; stdout {5})' -f `
            $Exe, $n, $Federation, $SettleSecs, $Bin64, (Join-Path $OutDir ('holder_{0}.out' -f $n)))
        Say-Plan ('would then watch {0} for up to {1}s for /remoteControl <pid> .* has joined federation/, and STOP AT THE FIRST JOIN' -f $rtiLogSay, $JoinTimeoutSec)
    }
    Say-Plan 'nothing was started, no application number was spent, no file was written.'
    exit $(if ($problems.Count -gt 0) { 2 } else { 0 })
}

if (-not (Test-Path -LiteralPath $OutDir)) { $null = New-Item -ItemType Directory -Path $OutDir -Force }
$env:PATH = $pathPrefix + $env:PATH
foreach ($k in $envVars.Keys) { Set-Item -Path ('Env:' + $k) -Value ([string]$envVars[$k]) }

foreach ($n in $AppNos) {
    $before = (Get-Content -LiteralPath $RtiLog.FullName | Measure-Object -Line).Lines
    $out = Join-Path $OutDir ('holder_{0}.out' -f $n)
    Say ('{0} attempt appNo {1}: starting the holder' -f (Stamp), $n)
    $p = Start-Process -FilePath $Exe -ArgumentList @([string]$n, $Federation, '1', [string]$SettleSecs, '3') `
            -WorkingDirectory $Bin64 -NoNewWindow -PassThru `
            -RedirectStandardOutput $out -RedirectStandardError ($out + '.err')
    $joined = $false
    for ($i = 0; $i -lt $JoinTimeoutSec; $i++) {
        Start-Sleep -Seconds 1
        if ($p.HasExited) { break }
        $tail = Get-Content -LiteralPath $RtiLog.FullName | Select-Object -Skip $before
        if ($tail | Where-Object { $_ -match ('remoteControl ' + $p.Id + ' .*has joined federation') }) { $joined = $true; break }
    }
    if ($joined) {
        Say-Ok ('{0} HOLDER JOINED: pid {1} appNo {2}, holding {3} s from now. NEVER kill it - every launch in that window only JOINS.' -f (Stamp), $p.Id, $n, $SettleSecs)
        exit 0
    }
    $tail = Get-Content -LiteralPath $RtiLog.FullName | Select-Object -Skip $before
    $mod  = @($tail | Where-Object { $_ -match 'Failed to process FOM file' }) | Select-Object -Last 1
    Say-Fail ('{0} attempt appNo {1}: create FAILED (exit {2}; {3}) - STP-825, this number is now burned.' -f `
        (Stamp), $n, $p.ExitCode, $(if ($mod) { ($mod -replace '.*FOM file ', '') -replace ' when.*', '' } else { 'no module named' }))
    Start-Sleep -Seconds 3
}
Say-Fail ('no holder joined after {0} attempt(s). Do NOT keep launching blindly (RUNBOOK sec 9c); supply more ledgered numbers, or ask the user about restarting rtiexec - that call is theirs, never the operator''s.' -f $AppNos.Count)
exit 1
