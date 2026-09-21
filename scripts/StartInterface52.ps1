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
.PARAMETER Server
  WHICH C2SIM SERVER THE INTERFACE LISTENS TO. 'standard' (the default) sets NOTHING and lets
  appsettings.json decide - today that is the operator's standard server, REST 8080 / STOMP
  61613, which is where STP lives and is the right choice for the real demo. 'private' selects
  the private test server the harness uses (REST http://127.0.0.1:18080/C2SIMServer, STOMP
  http://127.0.0.1:61614/topic/C2SIM, docker container c2sim-server-vrf) for a rehearsal that
  must not touch the operator's server. The choice is exported as C2SIM__RestUrl /
  C2SIM__StompUrl - the SAME env override the runner uses (RunC2SimScenario.ps1 Stage 6b),
  because the Host builder maps '__' to ':' onto the C2SIM section of appsettings.json.
  WHICHEVER is chosen, this script prints one loud line naming the pair, and the stand-in push
  commands MUST name the SAME pair (DEMO_RUNBOOK sec 4) - listening on one server while pushing
  to the other creates nothing and looks healthy doing it.
.PARAMETER RestUrl
  Explicit C2SIM REST endpoint; overrides -Server. EMPTY = leave it to -Server.
.PARAMETER StompUrl
  Explicit C2SIM STOMP endpoint; overrides -Server. EMPTY = leave it to -Server.
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
    [ValidateSet('standard','private')]
    [string] $Server     = 'standard',
    [string] $RestUrl    = '',
    [string] $StompUrl   = '',
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
# THE WORKING DIRECTORY (R-3, fixed 2026-09-21). RUNBOOK sec 7 item 3: cwd MUST be the
# VR-Forces bin64 so Legion finds vrfLegion.lua + terrain data - a wrong cwd gives
# "FATAL[Legion] ... vrfLegion.lua ... No such file" and then an SEHException. Since the .NET
# host loads appsettings from the CONTENT ROOT, and the content root defaults to cwd, the app
# is passed --contentRoot=<exe dir> so its configuration still loads from beside the exe.
# This is exactly how the runner starts the same exe (RunC2SimScenario.ps1 Stage 6b, cwd
# $Bin64 + --contentRoot), and that is the ONLY start path with live evidence behind it -
# every "standalone start" on the record went through the runner wrapper, so this script's
# old cwd (the exe directory) had never been exercised live.
# ONE VrfHome resolution: $Bin64 is also what the PATH prefix below is built from.
$Bin64       = Join-Path $VrfRoot 'bin64'
$ContentRoot = Split-Path -Parent $Exe
$AppArgs     = @('--contentRoot=' + $ContentRoot)

# THE C2SIM SERVER (DR-1, fixed 2026-09-21). Until today this script had no endpoint control
# at all, so it always listened to whatever appsettings.json names, while DEMO_RUNBOOK sec 4's
# stand-in push commands name the PRIVATE server explicitly - listen on one, push to the
# other, and the interface creates nothing while looking healthy. The private pair is spelled
# here in the EXACT form appsettings.json uses for the standard pair (scheme, host, path).
$PrivateRestUrl  = 'http://127.0.0.1:18080/C2SIMServer'
$PrivateStompUrl = 'http://127.0.0.1:61614/topic/C2SIM'

# What appsettings would say if nothing is overridden - read so the loud line below can NAME
# the server even in the 'standard' case, where this script deliberately sets no override.
# The DEPLOYED file beside the exe is the one the app actually reads; the repo copy under
# src\ is a fallback so this reads correctly from a tree that has not been built yet.
function Get-C2SimEndpointsFromConfig {
    param([string]$ContentRootDir, [string]$EnvName, [string]$RepoRootDir)
    $out = [pscustomobject]@{ Rest = ''; Stomp = ''; Source = 'NOT FOUND - no appsettings.json could be read' }
    $roots = @($ContentRootDir, (Join-Path $RepoRootDir 'src\VrfC2SimApp'))
    foreach ($root in $roots) {
        $baseFile = Join-Path $root 'appsettings.json'
        if (-not (Test-Path -LiteralPath $baseFile -PathType Leaf)) { continue }
        $read = @()
        foreach ($f in @($baseFile, (Join-Path $root ('appsettings.{0}.json' -f $EnvName)))) {
            if (-not (Test-Path -LiteralPath $f -PathType Leaf)) { continue }
            try {
                $j = Get-Content -LiteralPath $f -Raw -Encoding UTF8 | ConvertFrom-Json
            } catch { continue }
            $read += (Split-Path -Leaf $f)
            if ($null -eq $j) { continue }
            $sec = $j.PSObject.Properties | Where-Object { $_.Name -eq 'C2SIM' } | Select-Object -First 1
            if ($null -eq $sec -or $null -eq $sec.Value) { continue }
            foreach ($p in $sec.Value.PSObject.Properties) {
                if ($p.Name -eq 'RestUrl'  -and -not [string]::IsNullOrWhiteSpace([string]$p.Value)) { $out.Rest  = [string]$p.Value }
                if ($p.Name -eq 'StompUrl' -and -not [string]::IsNullOrWhiteSpace([string]$p.Value)) { $out.Stomp = [string]$p.Value }
            }
        }
        if ($read.Count -gt 0) {
            $out.Source = ('{0} ({1})' -f $root, ($read -join ' + '))
            if ($root -ne $ContentRootDir) { $out.Source = 'REPO SOURCE COPY, not the deployed tree - ' + $out.Source }
            break
        }
    }
    return $out
}

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

# WHICH SERVER, decided before the preconditions so a bad URL is reported with them.
# EMPTY = "set no override, let appsettings decide" - that is what -Server standard means.
$EffRestUrl = ''
$EffStompUrl = ''
if ($Server -eq 'private') { $EffRestUrl = $PrivateRestUrl; $EffStompUrl = $PrivateStompUrl }
if (-not [string]::IsNullOrWhiteSpace($RestUrl))  { $EffRestUrl  = $RestUrl.Trim() }
if (-not [string]::IsNullOrWhiteSpace($StompUrl)) { $EffStompUrl = $StompUrl.Trim() }

$problems = @()
foreach ($p in @(@{p=$VrfRoot; w='VR-Forces 5.2d root'}, @{p=$Bin64; w='VR-Forces 5.2d bin64 (the working directory - RUNBOOK sec 7 item 3)'},
                 @{p=(Join-Path $VrLinkRoot 'bin64'); w='VR-Link 5.10 bin64'},
                 @{p=(Join-Path $RtiDir 'bin'); w='MAK RTI 5.0.1 bin'}, @{p=$RidFile; w='rid file'}, @{p=$Exe; w='interface exe (build Release-5.2 first)'})) {
    if (-not (Test-Path -LiteralPath $p.p)) { $problems += ('missing {0}: {1}' -f $p.w, $p.p) }
}
if ($AppNumber -lt 1 -or $AppNumber -gt 65535) { $problems += "AppNumber must be 1..65535 (got $AppNumber)" }
foreach ($u in @(@{v=$EffRestUrl; n='-RestUrl'}, @{v=$EffStompUrl; n='-StompUrl'})) {
    if ([string]::IsNullOrWhiteSpace($u.v)) { continue }
    $parsed = $null
    if (-not [uri]::TryCreate($u.v, [System.UriKind]::Absolute, [ref]$parsed) -or
        ($parsed.Scheme -ne 'http' -and $parsed.Scheme -ne 'https')) {
        $problems += ('{0} is not an absolute http/https URL: {1}' -f $u.n, $u.v)
    }
}
if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "[FAIL] $_" -ForegroundColor Red }
    # -WhatIf is a PLANNING mode. The plan below (cwd, arguments, environment, and above all
    # the loud C2SIM-server line) is printed even when a precondition fails, so it can be read
    # from a tree that has not been built yet. THE EXIT CODE IS UNCHANGED - still 2 - only its
    # position moved; see the WhatIf exit below.
    if (-not $WhatIf) { exit 2 }
}

$rti = Test-NetConnection -ComputerName 127.0.0.1 -Port 4001 -WarningAction SilentlyContinue -InformationLevel Quiet
if (-not $rti) {
    Write-Host '[FAIL] no rtiexec listening on TCP 4001 - run scripts\StartRtiExec52.ps1 first (RUNBOOK 0.5.13).' -ForegroundColor Red
    if (-not $WhatIf) { exit 3 }
}

$pathPrefix = ('{0};{1};{2};' -f $Bin64, (Join-Path $VrLinkRoot 'bin64'), (Join-Path $RtiDir 'bin'))
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
# C2SIM__RestUrl / C2SIM__StompUrl are set ONLY when a server was actually chosen. With
# -Server standard and no explicit URL, nothing is added here and appsettings.json wins -
# which is the documented default and what the real demo wants.
if ($EffRestUrl)  { $envVars['C2SIM__RestUrl']  = $EffRestUrl }
if ($EffStompUrl) { $envVars['C2SIM__StompUrl'] = $EffStompUrl }

$ConfigEndpoints = Get-C2SimEndpointsFromConfig -ContentRootDir $ContentRoot -EnvName $Environment -RepoRootDir $RepoRoot

# AN INHERITED OVERRIDE BEATS appsettings, so the loud line must not report appsettings' value
# when one is present. This console may already carry C2SIM__RestUrl / C2SIM__StompUrl - the
# runner sets and restores them, a rehearsal script sets them for a child, an operator may have
# exported one by hand - and with -Server standard this script sets nothing, so that inherited
# value is what the app would actually hear. Naming appsettings' value in that case would be
# the DR-1 lie in a new place, so the inherited values are read here and named as the source.
$InheritedRest  = [Environment]::GetEnvironmentVariable('C2SIM__RestUrl',  'Process')
$InheritedStomp = [Environment]::GetEnvironmentVariable('C2SIM__StompUrl', 'Process')
$InheritedNote  = ''
if (-not $EffRestUrl -and -not [string]::IsNullOrWhiteSpace($InheritedRest)) {
    $ConfigEndpoints.Rest = $InheritedRest
    $InheritedNote = 'C2SIM__RestUrl'
}
if (-not $EffStompUrl -and -not [string]::IsNullOrWhiteSpace($InheritedStomp)) {
    $ConfigEndpoints.Stomp = $InheritedStomp
    $InheritedNote = $(if ($InheritedNote) { $InheritedNote + ' + C2SIM__StompUrl' } else { 'C2SIM__StompUrl' })
}
if ($InheritedNote) {
    $ConfigEndpoints.Source = ('INHERITED from this console''s environment ({0}), which OVERRIDES appsettings - this script set no override' -f $InheritedNote)
}

$SayRest  = $(if ($EffRestUrl)  { $EffRestUrl }  elseif ($ConfigEndpoints.Rest)  { $ConfigEndpoints.Rest }  else { 'UNKNOWN' })
$SayStomp = $(if ($EffStompUrl) { $EffStompUrl } elseif ($ConfigEndpoints.Stomp) { $ConfigEndpoints.Stomp } else { 'UNKNOWN' })
$SayHow = if ($EffRestUrl -and $EffStompUrl) {
    ('-Server {0} / -RestUrl / -StompUrl, exported as C2SIM__RestUrl + C2SIM__StompUrl (env overrides appsettings)' -f $Server)
} elseif ($EffRestUrl -or $EffStompUrl) {
    ('PART override: only {0} is exported; the other endpoint still comes from appsettings - {1}' -f `
        $(if ($EffRestUrl) { 'C2SIM__RestUrl' } else { 'C2SIM__StompUrl' }), $ConfigEndpoints.Source)
} elseif ($InheritedNote) {
    ('-Server standard: this script set NO override, but one is ALREADY in this console''s environment and it WINS - {0}' -f $ConfigEndpoints.Source)
} else {
    ('-Server standard: NO env override is set; these values are appsettings'' own - {0}' -f $ConfigEndpoints.Source)
}

Write-Host ('StartInterface52: {0}' -f $Exe)
Write-Host ('  cwd         : {0}' -f $Bin64)
Write-Host ('  arguments   : {0}' -f ($AppArgs -join ' '))
Write-Host ('  PATH prefix : {0}' -f $pathPrefix)
foreach ($k in $envVars.Keys) { Write-Host ('  {0,-27}= {1}' -f $k, $envVars[$k]) }
# The licence, from the registry rather than from whatever this console inherited, pinned onto
# this process so VrfC2SimApp.exe (started below, in THIS console) gets it. RUNBOOK 0.5.15.
$LicInfo = Resolve-MakLicenseFile
Write-Host ('  config      : appsettings.json + appsettings.{0}.json + the env above' -f $Environment)
Write-Host ('  content root: {0}' -f $ContentRoot)
# ONE LOUD LINE, ALWAYS - in -WhatIf too. The DR-1 failure this exists to stop is silent:
# listening on one server while STP (or the stand-in push) talks to another creates nothing
# and looks healthy doing it.
Write-Host ''
Write-Host ('*** C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest={0}  stomp={1} ***' -f $SayRest, $SayStomp)
Write-Host ('***   how: {0} ***' -f $SayHow)
Write-Host '***   THE PUSHES MUST NAME THIS SAME PAIR (DEMO_RUNBOOK sec 4) - a mismatched pair creates nothing and looks healthy. ***'
Write-Host ''
if ($WhatIf) {
    Write-Host '(WhatIf: nothing started)'
    # Same exit code a real start would have refused with - the preconditions above only
    # deferred it so this plan could be printed.
    exit $(if ($problems.Count -gt 0) { 2 } else { 0 })
}

# THE ENVIRONMENT IS SCOPED TO THIS RUN. When the script is launched as its own process
# (pwsh -File ..., the documented way) nothing could leak anyway; when an operator DOT-SOURCES
# it or runs it in their own console, the try/finally below puts PATH, every variable set here
# and the working directory back the way they were - including C2SIM__RestUrl / C2SIM__StompUrl,
# which would otherwise silently redirect the operator's next tool to the rehearsal server.
$AppExit   = 1
$SavedPath = $env:PATH
$SavedEnv  = [ordered]@{}
foreach ($k in $envVars.Keys) { $SavedEnv[$k] = [Environment]::GetEnvironmentVariable($k, 'Process') }
Push-Location
try {
    $env:PATH = $pathPrefix + $env:PATH
    foreach ($k in $envVars.Keys) { Set-Item -Path ('Env:' + $k) -Value ([string]$envVars[$k]) }
    Set-Location -LiteralPath $Bin64
    & $Exe @AppArgs
    $AppExit = $LASTEXITCODE
} finally {
    Pop-Location
    $env:PATH = $SavedPath
    foreach ($k in $SavedEnv.Keys) {
        if ($null -eq $SavedEnv[$k]) { Remove-Item -Path ('Env:' + $k) -ErrorAction SilentlyContinue }
        else { Set-Item -Path ('Env:' + $k) -Value ([string]$SavedEnv[$k]) }
    }
}
exit $AppExit
