<#
.SYNOPSIS
  Start the C2SIM <-> VR-Forces 5.2d interface STANDALONE (demo / operator use). NOT the test harness.

.DESCRIPTION
  DEMO_READINESS_2026-09-06 rows 5/6/8. Sets ONLY the interface's own runtime environment and
  starts VrfC2SimApp.exe in this console:
    - the RUNTIME PATH TRAP: VR-Forces 5.2d bin64 + VR-Link 5.10 bin64 + MAK RTI 5.0.1 bin
      prefixed on PATH (VrfBridge.dll loads the MAK DLLs by PATH; without the prefix the
      first bridge call fails with "A procedure imported by VrfBridge.dll could not be loaded")
    - MAK_VRFDIR / MAK_VRLDIR / MAK_RTIDIR
    - the assistant-free RTI posture the sim uses: RTI_ASSISTANT_DISABLE=1 + RTI_RID_FILE =
      config\rid-501-rtiexec-min.mtl (RUNBOOK 0.5.13). A headless rtiexec must already be
      listening on TCP 4001 (scripts\StartRtiExec52.ps1); this script checks and refuses.
    - DOTNET_ENVIRONMENT=Demo so appsettings.Demo.json overlays appsettings.json
    - Vrf__ClientId / Vrf__ApplicationNumber / Vrf__PositionReportSeconds from the parameters
  Nothing is written under C:\MAK. No observers, no ledger, no scoring - the operator watches
  the VR-Forces GUI and STP.

  ORDER OF START (DEMO_RUNBOOK): 1. StartRtiExec52.ps1  2. LaunchVrf52.ps1 (GUI on)  3. this
  script  4. STP pushes the initialization, then orders.

.PARAMETER ClientId
  Must equal the SystemName of the init STP pushes (RUNBOOK sec 2). Default STP.
.PARAMETER AppNumber
  HLA application number of the interface federate. Demo block 9101-9199 (outside the test
  ledger); two interfaces on one network must differ. Default 9101.
.PARAMETER PositionReportSeconds
  Period of the C2SIM position reports (0 = off). Default 10.
.PARAMETER WhatIf
  Print the environment and command line, start nothing.
#>
[CmdletBinding()]
param(
    [string] $ClientId = 'STP',
    [int]    $AppNumber = 9101,
    [int]    $PositionReportSeconds = 10,
    [string] $VrfRoot    = 'C:\MAK\vrforces5.2d',
    [string] $VrLinkRoot = 'C:\MAK\vrlink5.10',
    [string] $RtiDir     = 'C:\MAK\makRti5.0.1',
    [string] $RidFile    = '',
    [string] $Environment = 'Demo',
    [switch] $WhatIf
)
$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not $RidFile) { $RidFile = Join-Path $RepoRoot 'config\rid-501-rtiexec-min.mtl' }
$Exe = Join-Path $RepoRoot 'src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe'

$problems = @()
foreach ($p in @(@{p=$VrfRoot; w='VR-Forces 5.2d root'}, @{p=(Join-Path $VrLinkRoot 'bin64'); w='VR-Link 5.10 bin64'},
                 @{p=(Join-Path $RtiDir 'bin'); w='MAK RTI 5.0.1 bin'}, @{p=$RidFile; w='rid file'}, @{p=$Exe; w='interface exe (build Release-5.2 first)'})) {
    if (-not (Test-Path -LiteralPath $p.p)) { $problems += ('missing {0}: {1}' -f $p.w, $p.p) }
}
if ($AppNumber -lt 1 -or $AppNumber -gt 65535) { $problems += "AppNumber must be 1..65535 (got $AppNumber)" }
if ($problems.Count -gt 0) { $problems | ForEach-Object { Write-Host "[FAIL] $_" -ForegroundColor Red }; exit 2 }

$rti = Test-NetConnection -ComputerName 127.0.0.1 -Port 4001 -WarningAction SilentlyContinue -InformationLevel Quiet
if (-not $rti) {
    Write-Host '[FAIL] no rtiexec listening on TCP 4001 - run scripts\StartRtiExec52.ps1 first (RUNBOOK 0.5.13).' -ForegroundColor Red
    if (-not $WhatIf) { exit 3 }
}

$pathPrefix = ('{0};{1};{2};' -f (Join-Path $VrfRoot 'bin64'), (Join-Path $VrLinkRoot 'bin64'), (Join-Path $RtiDir 'bin'))
$envVars = [ordered]@{
    MAK_VRFDIR                 = $VrfRoot
    MAK_VRLDIR                 = $VrLinkRoot
    MAK_RTIDIR                 = $RtiDir
    RTI_RID_FILE               = $RidFile
    RTI_ASSISTANT_DISABLE      = '1'
    DOTNET_ENVIRONMENT         = $Environment
    Vrf__ClientId              = $ClientId
    Vrf__ApplicationNumber     = [string]$AppNumber
    Vrf__PositionReportSeconds = [string]$PositionReportSeconds
}
Write-Host ('StartInterface52: {0}' -f $Exe)
Write-Host ('  PATH prefix : {0}' -f $pathPrefix)
foreach ($k in $envVars.Keys) { Write-Host ('  {0,-27}= {1}' -f $k, $envVars[$k]) }
Write-Host ('  config      : appsettings.json + appsettings.{0}.json + the env above' -f $Environment)
if ($WhatIf) { Write-Host '(WhatIf: nothing started)'; exit 0 }

$env:PATH = $pathPrefix + $env:PATH
foreach ($k in $envVars.Keys) { Set-Item -Path ('Env:' + $k) -Value ([string]$envVars[$k]) }
Set-Location (Split-Path -Parent $Exe)
& $Exe
exit $LASTEXITCODE
