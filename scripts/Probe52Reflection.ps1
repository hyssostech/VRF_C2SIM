# Probe52Reflection.ps1 - the PREREG_52_REFLECTION_2026-09-03 procedure, steps 1-4, as
# ONE tee'd, parameterised run. Exists so the live lane is a single command and every
# tool run leaves a FILE (COLDSTART_REVIEW_2026-09-03 F1: 3803/3804/3807/3809/3815 had
# console output only). Nothing here decides anything: it launches, runs, captures, and
# prints exit codes; the prereg's sec 5 is filled from the captures.
#
# App numbers are MANDATORY and must be ledgered in docs/OPUS_EXECUTION_PLAN.md App. B
# BEFORE this runs (never-reuse rule). Five numbers: backend, gui, CreateOne, WatchVrf
# --diag, WatchVrf --diag --no-wait-ext. -DryRun prints the plan and touches nothing.
#
# Env for EVERY child = the 5.2 assistant-free profile (RunC2SimScenario.ps1 -VrfProfile
# 5.2 / LaunchVrf52.ps1): PATH prefix, MAK_VRFDIR/MAK_VRLDIR/MAK_RTIDIR, RTI_RID_FILE =
# config\rid-461-ridconfigured.mtl, RTI_ASSISTANT_DISABLE, MAKLMGRD_LICENSE_FILE from
# Machine scope, cwd = C:\MAK\vrforces5.2d\bin64. Exit 0 = every step ran and was
# captured (NOT "passed"); 2 = usage/precondition; 1 = a step failed to run.
param(
    [int]    $BackendAppNumber = 0,
    [int]    $FrontendAppNumber = 0,
    [int]    $CreateAppNumber = 0,
    [int]    $WatchAppNumber = 0,
    [int]    $WatchNoWaitAppNumber = 0,
    [string] $Scenario = 'Sample\FirstExperience\firstexperience',
    [double] $LatDeg = 21.283,
    [double] $LonDeg = -157.837,
    [double] $AltMeters = 50,
    [int]    $WatchSecs = 60,
    [int]    $SampleSecs = 5,
    [switch] $NoGui,
    [switch] $SkipLaunch,   # sim already up from a previous step (numbers 1-2 unused)
    [switch] $DryRun
)
$ErrorActionPreference = 'Stop'
function Say { param([string]$m) Write-Host $m }

$repo   = Split-Path -Parent $PSScriptRoot
$bin64  = 'C:\MAK\vrforces5.2d\bin64'
$rid    = Join-Path $repo 'config\rid-461-ridconfigured.mtl'
$outDir = Join-Path $repo 'runs\launch52'
$tools  = @{
    CreateOne = Join-Path $repo 'tools\CreateOne\bin\Release-5.2\net10.0\win-x64\CreateOne.exe'
    WatchVrf  = Join-Path $repo 'tools\WatchVrf\bin\Release-5.2\net10.0\win-x64\WatchVrf.exe'
}
$launch = Join-Path $PSScriptRoot 'LaunchVrf52.ps1'

Say ('=== Probe52Reflection ({0}) - PREREG_52_REFLECTION steps 1-4, tee to {1} ===' -f $(if ($DryRun) { 'DRY-RUN' } else { 'LIVE' }), $outDir)
$fail = $false
foreach ($n in @(@{k='CreateAppNumber';v=$CreateAppNumber},@{k='WatchAppNumber';v=$WatchAppNumber},@{k='WatchNoWaitAppNumber';v=$WatchNoWaitAppNumber})) {
    if ($n.v -le 0) { Say ('  [FAIL] -{0} is MANDATORY (ledger it first)' -f $n.k); $fail = $true }
}
if (-not $SkipLaunch) {
    if ($BackendAppNumber -le 0) { Say '  [FAIL] -BackendAppNumber is MANDATORY (or -SkipLaunch)'; $fail = $true }
    if ((-not $NoGui) -and ($FrontendAppNumber -le 0)) { Say '  [FAIL] -FrontendAppNumber is MANDATORY unless -NoGui'; $fail = $true }
}
$all = @($BackendAppNumber,$FrontendAppNumber,$CreateAppNumber,$WatchAppNumber,$WatchNoWaitAppNumber) | Where-Object { $_ -gt 0 }
if (($all | Select-Object -Unique).Count -ne $all.Count) { Say '  [FAIL] app numbers must be distinct'; $fail = $true }
foreach ($t in $tools.GetEnumerator()) { if (-not (Test-Path -LiteralPath $t.Value)) { Say ('  [FAIL] missing {0}: {1}' -f $t.Key, $t.Value); $fail = $true } }
foreach ($p in @($rid,$launch,$bin64)) { if (-not (Test-Path -LiteralPath $p)) { Say ('  [FAIL] missing {0}' -f $p); $fail = $true } }
if ($fail) { exit 2 }

$env:PATH = 'C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti4.6.1\bin;' + $env:PATH
$env:MAK_VRFDIR = 'C:\MAK\vrforces5.2d'; $env:MAK_VRLDIR = 'C:\MAK\vrlink5.10'; $env:MAK_RTIDIR = 'C:\MAK\makRti4.6.1'
$env:RTI_RID_FILE = $rid
$env:RTI_ASSISTANT_DISABLE = '1'
$lic = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine'); if ($lic) { $env:MAKLMGRD_LICENSE_FILE = $lic }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Each step: print the command, run it from bin64 with output tee'd to a file named by
# tool + app number, print the exit code. The step list is data so the plan is readable.
$steps = @()
if (-not $SkipLaunch) {
    $la = @('-BackendAppNumber', $BackendAppNumber, '-Scenario', $Scenario, '-ReadyTimeoutSec', 240)
    if ($NoGui) { $la += '-NoGui' } else { $la += @('-FrontendAppNumber', $FrontendAppNumber) }
    $steps += @{ name='1-launch'; file=('launch_{0}.txt' -f $BackendAppNumber); exe='pwsh'; args=@('-NoProfile','-File',$launch) + $la; cwd=$repo }
}
$steps += @{ name='2-create';       file=('createone_{0}.txt' -f $CreateAppNumber);      exe=$tools.CreateOne; args=@($CreateAppNumber, $LatDeg, $LonDeg, $AltMeters, 'REFLTEST'); cwd=$bin64 }
$steps += @{ name='3-watch-diag';   file=('watchvrf_{0}_diag.txt' -f $WatchAppNumber);   exe=$tools.WatchVrf;  args=@($WatchAppNumber, $WatchSecs, $SampleSecs, '', '--diag'); cwd=$bin64 }
$steps += @{ name='4-watch-nowait'; file=('watchvrf_{0}_nowait.txt' -f $WatchNoWaitAppNumber); exe=$tools.WatchVrf; args=@($WatchNoWaitAppNumber, $WatchSecs, $SampleSecs, '', '--diag', '--no-wait-ext'); cwd=$bin64 }

$rc = 0
foreach ($s in $steps) {
    $argStr = ($s.args | ForEach-Object { if ("$_" -match '\s|^$') { '"' + $_ + '"' } else { "$_" } }) -join ' '
    $outFile = Join-Path $outDir $s.file
    Say ''
    Say ('--- {0}: {1} {2}' -f $s.name, $s.exe, $argStr)
    Say ('    cwd={0}  capture={1}' -f $s.cwd, $outFile)
    if ($DryRun) { continue }
    Push-Location $s.cwd
    try {
        & $s.exe @($s.args) 2>&1 | Tee-Object -FilePath $outFile | ForEach-Object { '    | ' + $_ }
        $code = $LASTEXITCODE
    } finally { Pop-Location }
    Say ('    exit={0}' -f $code)
    Add-Content -LiteralPath $outFile -Value ('# exit={0} step={1} utc={2}' -f $code, $s.name, (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'))
    if ($s.name -eq '1-launch' -and $code -ne 0) { Say '    launch not READY - stopping the probe here'; $rc = 1; break }
    if ($code -ne 0) { $rc = 1 }
}
Say ''
Say ('=== Probe52Reflection done: {0} (captures in {1}) ===' -f $(if ($rc -eq 0) { 'every step ran' } else { 'a step failed - read the captures' }), $outDir)
exit $rc
