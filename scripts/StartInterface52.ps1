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
.PARAMETER RouteShift
  THE LATERAL ROUTE SHIFT (Vrf:PreflightRouteShift; STP-804/806, RUNBOOK sec 12), which is ON by
  default since the user ruling of 2026-09-20 ("Route shift: ON. Use as default for any run.").
  'config' (the default) sets nothing and lets appsettings decide; 'off' and 'on' set
  Vrf__PreflightRouteShift for this process only. This is the off-switch, and it is the SAME
  variable the runner reads for its manifest - there is one switch, not two.
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
    [ValidateSet('config','on','off')]
    [string] $RouteShift = 'config',
    [switch] $WhatIf
)
$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not $RidFile) { $RidFile = Join-Path $RepoRoot 'config\rid-501-rtiexec-min.mtl' }
$Exe = Join-Path $RepoRoot 'src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe'

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
# The route shift is ON by default (user ruling 2026-09-20) and that default lives in
# appsettings.json / appsettings.Demo.json, so 'config' sets NOTHING here - adding a third place
# that states the default is how the three drift apart. 'on'/'off' override it for this process
# only, and the value is printed with the rest of the environment below.
if ($RouteShift -ne 'config') { $envVars['Vrf__PreflightRouteShift'] = $(if ($RouteShift -eq 'on') { 'true' } else { 'false' }) }
Write-Host ('StartInterface52: {0}' -f $Exe)
Write-Host ('  PATH prefix : {0}' -f $pathPrefix)
foreach ($k in $envVars.Keys) { Write-Host ('  {0,-27}= {1}' -f $k, $envVars[$k]) }
# The licence, from the registry rather than from whatever this console inherited, pinned onto
# this process so VrfC2SimApp.exe (started below, in THIS console) gets it. RUNBOOK 0.5.15.
$LicInfo = Resolve-MakLicenseFile
Write-Host ('  config      : appsettings.json + appsettings.{0}.json + the env above' -f $Environment)
if ($WhatIf) { Write-Host '(WhatIf: nothing started)'; exit 0 }

$env:PATH = $pathPrefix + $env:PATH
foreach ($k in $envVars.Keys) { Set-Item -Path ('Env:' + $k) -Value ([string]$envVars[$k]) }
Set-Location (Split-Path -Parent $Exe)
& $Exe
exit $LASTEXITCODE
