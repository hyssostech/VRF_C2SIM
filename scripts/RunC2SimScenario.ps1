<#
.SYNOPSIS
    THE ONE-BUTTON UNATTENDED C2SIM -> VR-Forces RUNNER. Sequences stages 1-8 of
    docs/HEADLESS_RUN_PLAN.md sec 1 with zero humans in the UI, and leaves a
    timestamped run directory full of EVIDENCE.

.DESCRIPTION
    Contract (HEADLESS_RUN_PLAN.md sec 2):

        pwsh -File scripts\RunC2SimScenario.ps1 -Init <init.xml> -Order <order.xml> -RunSecs 600

    THIS SCRIPT DOES NOT SCORE THE RUN. It collects: the WatchVrf POS/CON trace
    (the movement oracle), the ListenReports capture (what the interface told
    C2SIM), the PushOrder bus log, the VrfC2SimApp log, and a run manifest
    recording every appNumber consumed, both clocks, tool identities, exact input
    paths and the exit code of every stage. HEADLESS_RUN_PLAN.md sec 4a is marked
    "AWAITING USER RULING" and sec 4a.6 says run 1 is a MEASUREMENT, not an
    acceptance test - so NO threshold from sec 4a (50 m / 250 m / 25 m / 5x /
    200 km/h) appears anywhere in this file, by design. A separate scorer consumes
    the manifest + trace once the criterion is ratified.

    STAGE ORDER (each constraint below cost a live session; do not "improve" them):
      0  validate every input, up front, BEFORE VR-Forces is launched
      1  pre-flight process inventory (RUNBOOK 0.5.0) - REFUSE on a pre-existing
         vrfSimHLA1516e / vrfGui / vrfLauncher. -AllowExistingVrf is NOT used and
         is NOT offered: it is the false-READY trap.
      2  allocate EVERY appNumber from the single marker in
         OPUS_EXECUTION_PLAN.md Appendix B and ADVANCE the marker, BEFORE any join
      3  LaunchVrf.ps1 (combined mode)
      4  oracle pre-check, passive (RUNBOOK 0.5.7) - ADVISORY by default, see below
      5  WatchVrf + ListenReports START HERE, BEFORE the init, so unit births are
         in the trace
      6  PushInit, then start VrfC2SimApp (RUNBOOK sec 3: init first, app late-joins)
      7  post-init ORACLE GATE - the RUNBOOK 0.5.7 CORRECTED criterion, applied to
         the live trace: a POS line with REAL lat/lon (not NaN, not the 90/-90
         pole), retried up to ~3 minutes
      8  PushOrder, then observe for -RunSecs
      9  teardown in a finally: StopIface (clean resign) THEN StopVrf.ps1

    *** DEVIATION FROM THE BRIEF, DELIBERATE AND FLAGGED (stage 4 vs stage 7) ***
    The brief asks for a hard oracle pre-check before anything is scored. RUNBOOK
    0.5.7 states that on a stock TropicTortoise load the baseline objects are
    POSITIONLESS ("90.000000,-90.000000" and NaN), so a passive real-coordinate
    pre-check run BEFORE any C2SIM unit exists is EXPECTED TO FAIL - "the STRONGER
    CHECK below is IN PRACTICE THE ONLY CHECK THAT CAN PASS ON A STOCK
    TropicTortoise LOAD". Making that fatal would abort every healthy run.
    Therefore:
      - stage 4 (pre-init, passive) is ADVISORY: it proves the oracle can JOIN and
        DISCOVER, and its degenerate-coordinate result is RECORDED, not fatal.
        Pass -StrictPreInitOracle to make it fatal if that is ruled the criterion.
      - stage 7 (post-init) applies the 0.5.7 CORRECTED criterion for real, against
        the scoring trace itself, once C2SIM units exist. It is FATAL.
    HEADLESS_RUN_PLAN sec 2 says the pre-check runs "before anything is SCORED",
    not "before the init is pushed", and sec 4a.6 lists it as a run-VALIDITY gate -
    both of which stage 7 satisfies. The alternative (RUNBOOK 0.5.7 "STRONGER
    CHECK": tools/CreateOne a throwaway entity, verify its POS, then RELAUNCH
    VR-Forces so the throwaway never enters a scored trace) is NOT implemented
    here; it needs a ruling because it changes appNumber accounting and adds a
    launch cycle. See the report accompanying this draft.

    NON-NEGOTIABLES HONOURED HERE:
      - NOTHING is ever force-killed. Not the app, not a federate, not VR-Forces.
        A federate that will not exit is REPORTED, loudly, and left alone
        (RUNBOOK sec 0).
      - rtiAssistant / rtiexec / rtiForwarder are NEVER touched, never even
        refused on (RUNBOOK 0.5.2). They are inventoried for the manifest only.
      - Teardown runs on every path, success or failure, via finally. CAVEAT,
        stated rather than glossed: PowerShell does NOT reliably run a top-level
        finally on Ctrl-C. A Ctrl-C'd run can therefore leave VR-Forces up and the
        interface JOINED. Recovery is scripts/StopVrf.ps1 (and tools/StopIface if
        the interface is still up) - never a force-kill.
      - Every external invocation's exit code is CAPTURED and RECORDED. None is
        assumed.

.PARAMETER Init
    C2SIM initialization XML. Default data/R9_Mojave_Lean_Initialization.xml.
    HEADLESS_RUN_PLAN 4a.0: the LEAN file (6 units) supersedes sec 3's full file
    (158 unit/actor references); both contain all three taskee UUIDs, the lean one
    keeps 152 irrelevant units out of the trace.

.PARAMETER Order
    C2SIM order XML. Default data/R9_Mojave_UnitMove_Order.xml. Three MOVE tasks
    against three taskees (4a.0), legs ~556-578 m.

.PARAMETER RunSecs
    Observation window AFTER the order is pushed. Default 600.

.PARAMETER DryRun
    Print the ENTIRE planned sequence - every command line, every appNumber that
    WOULD be allocated, every output path - and do nothing else. -DryRun:
      * launches nothing, starts no process except read-only inventory
      * contacts no server (no REST, no STOMP, no RTI)
      * does NOT advance the Appendix B marker and does NOT write to it
      * does NOT create the run directory
    This is how the script is reviewed. It is the only self-test permitted before
    a live gate.

.OUTPUTS
    Exit codes (the RUNNER's own; per-stage codes are in the manifest):
      0  the run completed and the evidence was collected (this is NOT a verdict
         on the scenario - see 4a.6), or a dry run completed
      2  usage / validation error, or a called tool exited 2. NOTHING was launched
         where the check could be made before launch.
      3  a stage failed after VR-Forces was up; teardown ran. Evidence is partial
         and the manifest says which stage failed.
      4  teardown itself did not fully complete - VR-Forces and/or the interface
         MAY STILL BE RUNNING and MAY STILL BE JOINED. Nothing was force-killed.
         MANUAL INSPECTION REQUIRED before the next run.
      5  unexpected terminating error. Same warning as 4.

.EXAMPLE
    pwsh -File scripts\RunC2SimScenario.ps1 -DryRun

.EXAMPLE
    pwsh -File scripts\RunC2SimScenario.ps1 -RunSecs 600
#>
[CmdletBinding()]
param(
    [string] $Init,
    [string] $Order,
    [int]    $RunSecs = 600,

    # Where the evidence lands. A timestamped subdirectory is created under this.
    [string] $RunRoot,

    # VR-Forces bring-up (passed straight through to LaunchVrf.ps1).
    [string] $Scenario   = 'TropicTortoise',
    [string] $VrfRoot    = 'C:\MAK\vrforces5.0.2',
    [string] $VrLinkRoot = 'C:\MAK\vrlink5.8',
    [string] $RtiDir     = 'C:\MAK\makRti4.6.1',
    [string] $Federation = 'CWIX-2024',

    # C2SIM endpoints. NOTE: these reach PushInit / PushOrder / StopIface ONLY.
    # tools/ListenReports HARDCODES localhost (ListenReports/Program.cs:85-87) and
    # takes no endpoint argument - see the KNOWN TOOL LIMITATIONS block below.
    [string] $RestUrl  = 'http://127.0.0.1:8080/C2SIMServer',
    [string] $StompUrl = 'http://127.0.0.1:61613/topic/C2SIM',

    # Oracle cadence. 2 s per HEADLESS_RUN_PLAN 4a.2 ("Sample interval 2 s").
    # This is a MEASUREMENT PARAMETER, not a threshold - it says how often we look,
    # never what counts as movement.
    [int] $SampleSecs = 2,

    # Timing budgets. All are upper bounds; the run does not wait them out when the
    # signal arrives earlier.
    [int] $PreRollSecs         = 20,   # trace time before the init is pushed
    [int] $AppJoinTimeoutSec   = 180,  # app: launch -> "Connected to C2SIM"
    [int] $OracleGateTimeoutSec= 180,  # RUNBOOK 0.5.7 "allow up to ~3 MINUTES"
    [int] $InitDispatchWaitSec = 120,  # app: -> "Init dispatched: N units"
    [int] $PushOrderListenSec  = 30,   # PushOrder's own blocking listen window
    [int] $TrailSecs           = 30,   # trace time after the observation window
    [int] $LaunchSettleSec     = 45,   # after LaunchVrf READY, before the oracle
                                       # pre-check. READY is thread-count + window
                                       # only; it does NOT imply scenario loaded or
                                       # federation joined (RUNBOOK 0.5.7).
    [int] $AppExitTimeoutSec   = 120,  # app: StopIface -> process gone (NEVER killed)
    [int] $StopVrfTimeoutSec   = 120,

    # Explicit override for the WatchVrf / ListenReports duration. 0 = derive it.
    [int] $WatchSecs = 0,

    [switch] $StrictPreInitOracle,
    [switch] $SkipServerCheck,
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptVersion = '1.0.0-draft'

# =============================================================================
# KNOWN TOOL LIMITATIONS THIS SCRIPT WORKS AROUND (read before editing)
# =============================================================================
# 1. tools/ListenReports HARDCODES RestUrl/StompUrl to 127.0.0.1
#    (ListenReports/Program.cs:85-87). -RestUrl / -StompUrl therefore do NOT reach
#    it. Against a non-localhost server it would capture NOTHING, silently. This
#    script REFUSES to start if -RestUrl or -StompUrl is non-localhost, rather
#    than produce a capture file that looks valid and is empty.
# 2. tools/WatchVrf OVERLOADS EXIT CODE 2: usage error (WatchRunner.cs:60) AND
#    operational exception (WatchRunner.cs:183). Its arguments here are generated,
#    never typed, so a 2 from WatchVrf is an operational failure - but the code
#    alone cannot prove that. Recorded as ambiguous wherever it is captured.
# 3. tools/PushOrder writes its bus log to c2sim-bus.log BESIDE ITS OWN BINARY
#    (PushOrder/Program.cs:112) with no override. This script COPIES it into the
#    run directory afterwards; the copy can be stale if PushOrder failed before
#    writing, so the copy is timestamp-checked and the result recorded.
# 4. tools/StopIface requires <restUrl> <stompUrl> AND --yes, with NO defaults, and
#    exits 1 if the server does not reach UNINITIALIZED (StopIface/Program.cs:138).
#    Exit 1 there means the interface MAY STILL BE JOINED - it is escalated, not
#    swallowed.
# 5. src/VrfC2SimApp reads appsettings.json from its content root, so it is
#    launched with --contentRoot=<exe dir> while cwd is VR-Forces bin64
#    (RUNBOOK sec 7 item 3). Its ApplicationNumber is overridden through the
#    environment variable Vrf__ApplicationNumber - historically hand-edited in
#    appsettings.json, which is exactly how stale-federate hangs were created.
# 6. appsettings.json pins Vrf:ClientId = "STP". RUNBOOK sec 2: clientId MUST equal
#    the init's SystemName or the interface creates 0 units. This script READS the
#    SystemName out of the init file and REFUSES on a mismatch, up front.
# =============================================================================

# ---- output helpers (ASCII only) -------------------------------------------
function Say      { param([string]$m) Write-Host $m }
function Say-Head { param([string]$m) Write-Host ''; Write-Host ('=== ' + $m + ' ===') }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Info { param([string]$m) Write-Host ('  [..]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [DRY-RUN] ' + $m) }

# ---- paths ------------------------------------------------------------------
$RepoRoot  = Split-Path -Parent $PSScriptRoot
$DocsDir   = Join-Path $RepoRoot 'docs'
$DataDir   = Join-Path $RepoRoot 'data'
$ToolsDir  = Join-Path $RepoRoot 'tools'
$LedgerDoc = Join-Path $DocsDir 'OPUS_EXECUTION_PLAN.md'

$LaunchVrf = Join-Path $PSScriptRoot 'LaunchVrf.ps1'
$StopVrf   = Join-Path $PSScriptRoot 'StopVrf.ps1'

$ExeWatchVrf      = Join-Path $ToolsDir 'WatchVrf\bin\Release\net10.0\win-x64\WatchVrf.exe'
$ExePushInit      = Join-Path $ToolsDir 'PushInit\bin\Release\net10.0\PushInit.exe'
$ExePushOrder     = Join-Path $ToolsDir 'PushOrder\bin\Release\net10.0\PushOrder.exe'
$ExeListenReports = Join-Path $ToolsDir 'ListenReports\bin\Release\net10.0\ListenReports.exe'
$ExeStopIface     = Join-Path $ToolsDir 'StopIface\bin\Release\net10.0\StopIface.exe'
$ExeApp           = Join-Path $RepoRoot 'src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.exe'

$Bin64 = Join-Path $VrfRoot 'bin64'

if ([string]::IsNullOrWhiteSpace($Init))    { $Init    = Join-Path $DataDir 'R9_Mojave_Lean_Initialization.xml' }
if ([string]::IsNullOrWhiteSpace($Order))   { $Order   = Join-Path $DataDir 'R9_Mojave_UnitMove_Order.xml' }
if ([string]::IsNullOrWhiteSpace($RunRoot)) { $RunRoot = Join-Path $RepoRoot 'runs' }

$ProcBackend  = 'vrfSimHLA1516e'
$ProcFrontend = 'vrfGui'
$ProcLauncher = 'vrfLauncher'
$RtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')

# ---- manifest ---------------------------------------------------------------
# Written to disk after EVERY stage, so an aborted or crashed run still leaves a
# manifest describing exactly how far it got.
$Manifest = [ordered]@{
    schema          = 'vrf-c2sim-run-manifest/1'
    scriptVersion   = $ScriptVersion
    scoring         = 'NONE. This runner collects evidence only. HEADLESS_RUN_PLAN.md sec 4a is AWAITING USER RULING and sec 4a.6 declares run 1 a measurement, not an acceptance test. No threshold from 4a is embedded in this script.'
    dryRun          = [bool]$DryRun
    clocks          = [ordered]@{}
    host            = [ordered]@{}
    inputs          = [ordered]@{}
    tools           = [ordered]@{}
    appNumbers      = @()
    ledger          = [ordered]@{}
    preflight       = [ordered]@{}
    stages          = @()
    oracle          = [ordered]@{}
    artifacts       = [ordered]@{}
    validityFlags   = @()
    runnerExitCode  = $null
}
$RunDir       = $null
$ManifestPath = $null

function Save-Manifest {
    if ($DryRun -or -not $ManifestPath) { return }
    try {
        $Manifest.clocks.savedLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
        $Manifest.clocks.savedUtc   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        $json = $Manifest | ConvertTo-Json -Depth 12
        [System.IO.File]::WriteAllText($ManifestPath, $json, (New-Object System.Text.UTF8Encoding($false)))
    } catch {
        Say-Warn ('could not write the manifest: {0}' -f $_.Exception.Message)
    }
}

function Add-Flag {
    param([string]$Severity, [string]$Text)
    $Manifest.validityFlags += [ordered]@{
        severity  = $Severity
        text      = $Text
        atUtc     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    }
    if ($Severity -eq 'FAIL') { Say-Fail $Text } elseif ($Severity -eq 'WARN') { Say-Warn $Text } else { Say-Info $Text }
}

function Add-Stage {
    param(
        [string]$Name, [string]$File, [string[]]$Arguments, [string]$Cwd,
        $ExitCode, [string]$StdOut, [string]$StdErr, [string]$Note, [string]$Outcome
    )
    $Manifest.stages += [ordered]@{
        name        = $Name
        commandLine = (Format-CommandLine -File $File -Arguments $Arguments)
        cwd         = $Cwd
        startedUtc  = $script:LastStageStartUtc
        endedUtc    = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        exitCode    = $ExitCode
        stdoutFile  = $StdOut
        stderrFile  = $StdErr
        outcome     = $Outcome
        note        = $Note
    }
    Save-Manifest
}

function Format-CommandLine {
    param([string]$File, [string[]]$Arguments)
    $parts = @()
    foreach ($a in @($Arguments)) {
        if ($null -eq $a) { continue }
        if ($a -match '[\s"]') { $parts += ('"' + ($a -replace '"','\"') + '"') } else { $parts += $a }
    }
    if ($parts.Count -eq 0) { return $File }
    return ($File + ' ' + ($parts -join ' '))
}

$script:LastStageStartUtc = $null

# ---- external invocation ----------------------------------------------------
# EVERY external process in this script goes through Invoke-External or
# Start-External. Neither ever assumes an exit code; both record what they got.
function Invoke-External {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$File,
        [string[]]$Arguments = @(),
        [string]$Cwd,
        [string]$StdOutFile,
        [string]$StdErrFile,
        [string]$Note
    )
    $cmd = Format-CommandLine -File $File -Arguments $Arguments
    if ($DryRun) {
        Say-Plan ('STAGE {0}' -f $Name)
        Say      ('            cwd    : {0}' -f $Cwd)
        Say      ('            run    : {0}' -f $cmd)
        if ($StdOutFile) { Say ('            stdout : {0}' -f $StdOutFile) }
        if ($StdErrFile) { Say ('            stderr : {0}' -f $StdErrFile) }
        if ($Note)       { Say ('            note   : {0}' -f $Note) }
        return [pscustomobject]@{ ExitCode = 0; DryRun = $true }
    }

    $script:LastStageStartUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    Say-Info ('{0}: {1}' -f $Name, $cmd)

    $sp = @{ FilePath = $File; WorkingDirectory = $Cwd; PassThru = $true; Wait = $true; NoNewWindow = $true }
    if ($Arguments.Count -gt 0) { $sp.ArgumentList = $Arguments }
    if ($StdOutFile) { $sp.RedirectStandardOutput = $StdOutFile }
    if ($StdErrFile) { $sp.RedirectStandardError  = $StdErrFile }

    $code = $null
    $outcome = 'ran'
    try {
        $p = Start-Process @sp
        $code = $p.ExitCode
    } catch {
        $outcome = 'could-not-start'
        $code = $null
        Say-Fail ('{0}: could not start: {1}' -f $Name, $_.Exception.Message)
    }
    Add-Stage -Name $Name -File $File -Arguments $Arguments -Cwd $Cwd -ExitCode $code `
              -StdOut $StdOutFile -StdErr $StdErrFile -Note $Note -Outcome $outcome
    if ($null -ne $code) { Say-Info ('{0}: EXIT={1}' -f $Name, $code) }
    return [pscustomobject]@{ ExitCode = $code; DryRun = $false }
}

function Start-External {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$File,
        [string[]]$Arguments = @(),
        [string]$Cwd,
        [string]$StdOutFile,
        [string]$StdErrFile,
        [string]$Note
    )
    $cmd = Format-CommandLine -File $File -Arguments $Arguments
    if ($DryRun) {
        Say-Plan ('STAGE {0}  (background)' -f $Name)
        Say      ('            cwd    : {0}' -f $Cwd)
        Say      ('            run    : {0}' -f $cmd)
        if ($StdOutFile) { Say ('            stdout : {0}' -f $StdOutFile) }
        if ($StdErrFile) { Say ('            stderr : {0}' -f $StdErrFile) }
        if ($Note)       { Say ('            note   : {0}' -f $Note) }
        return $null
    }
    Say-Info ('{0} (background): {1}' -f $Name, $cmd)
    $sp = @{ FilePath = $File; WorkingDirectory = $Cwd; PassThru = $true; NoNewWindow = $true }
    if ($Arguments.Count -gt 0) { $sp.ArgumentList = $Arguments }
    if ($StdOutFile) { $sp.RedirectStandardOutput = $StdOutFile }
    if ($StdErrFile) { $sp.RedirectStandardError  = $StdErrFile }
    $p = Start-Process @sp
    $Manifest.stages += [ordered]@{
        name        = $Name
        commandLine = $cmd
        cwd         = $Cwd
        startedUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        endedUtc    = $null
        exitCode    = $null
        stdoutFile  = $StdOutFile
        stderrFile  = $StdErrFile
        outcome     = 'started-background'
        note        = $Note
        processId   = $p.Id
    }
    Save-Manifest
    return $p
}

function Complete-Background {
    param([string]$Name, $Process, [int]$TimeoutSec, [string]$Note)
    if ($DryRun -or $null -eq $Process) { return }
    Say-Info ('waiting for {0} (pid {1}) to finish on its own - it is NEVER killed' -f $Name, $Process.Id)
    $null = $Process.WaitForExit($TimeoutSec * 1000)
    $code = $null
    $outcome = 'still-running'
    if ($Process.HasExited) { $code = $Process.ExitCode; $outcome = 'exited' }
    foreach ($s in $Manifest.stages) {
        if ($s.name -eq $Name -and $null -eq $s.exitCode -and $s.outcome -eq 'started-background') {
            $s.endedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            $s.exitCode = $code
            $s.outcome  = $outcome
            if ($Note) { $s.note = $Note }
            break
        }
    }
    Save-Manifest
    if ($outcome -eq 'exited') { Say-Info ('{0}: EXIT={1}' -f $Name, $code) }
    else { Add-Flag 'WARN' ("{0} (pid {1}) had not exited after {2}s. NOT killed - it is a joined federate. It will resign on its own timer." -f $Name, $Process.Id, $TimeoutSec) }
}

# ---- live-file reading (the trace is being written while we read it) --------
function Read-LiveText {
    param([string]$Path)
    if (-not $Path -or -not (Test-Path -LiteralPath $Path)) { return '' }
    $fs = $null; $sr = $null
    try {
        $share = [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete
        $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, $share)
        $sr = New-Object System.IO.StreamReader($fs)
        return $sr.ReadToEnd()
    } catch {
        return ''
    } finally {
        if ($sr) { $sr.Dispose() } elseif ($fs) { $fs.Dispose() }
    }
}

# ---- the RUNBOOK 0.5.7 CORRECTED coordinate criterion -----------------------
# PASS = at least one POS line whose lat/lon are real numbers, NOT NaN, and NOT
#        the 90.000000,-90.000000 pole placeholder.
# This function answers ONLY "is this coordinate real". It says nothing about
# distance, arrival or movement - those are the scorer's job (4a), not this
# script's.
function Get-RealPositions {
    param([string]$TraceText)
    $real = @()
    $degenerate = 0
    $posLines = 0
    foreach ($line in ($TraceText -split "`r?`n")) {
        if (-not $line.StartsWith('POS,')) { continue }
        $posLines++
        $f = $line.Split(',')
        if ($f.Length -lt 6) { $degenerate++; continue }
        $lat = 0.0; $lon = 0.0
        $styles = [System.Globalization.NumberStyles]::Float
        $inv    = [System.Globalization.CultureInfo]::InvariantCulture
        if (-not [double]::TryParse($f[3], $styles, $inv, [ref]$lat)) { $degenerate++; continue }
        if (-not [double]::TryParse($f[4], $styles, $inv, [ref]$lon)) { $degenerate++; continue }
        if ([double]::IsNaN($lat) -or [double]::IsNaN($lon) -or
            [double]::IsInfinity($lat) -or [double]::IsInfinity($lon)) { $degenerate++; continue }
        # The pole placeholder. Compared with a tolerance rather than for equality
        # because the trace prints F6 and an exact string match is brittle.
        if ([Math]::Abs($lat) -ge 89.999999) { $degenerate++; continue }
        if ([Math]::Abs($lon) -gt 180.0)     { $degenerate++; continue }
        $real += [ordered]@{ line = $line; uuid = $f[2]; lat = $lat; lon = $lon }
    }
    return [pscustomobject]@{
        PosLineCount   = $posLines
        RealCount      = $real.Count
        DegenerateCount= $degenerate
        First          = $(if ($real.Count -gt 0) { $real[0] } else { $null })
        Uuids          = @($real | ForEach-Object { $_.uuid } | Select-Object -Unique)
    }
}

function Get-TraceSummaryLine {
    # The "# t=..s reflected=N readable=M" line WatchVrf emits after each sample.
    param([string]$TraceText)
    $last = $null
    foreach ($line in ($TraceText -split "`r?`n")) {
        if ($line.StartsWith('# t=')) { $last = $line }
    }
    return $last
}

# =============================================================================
# STAGE 0 - VALIDATE EVERYTHING, BEFORE ANYTHING IS LAUNCHED
# =============================================================================
$nowLocal = Get-Date
$nowUtc   = $nowLocal.ToUniversalTime()
$stamp    = $nowUtc.ToString('yyyyMMddTHHmmssZ')

Say-Head ('RunC2SimScenario.ps1 v{0} ({1})' -f $ScriptVersion, $(if ($DryRun) { 'DRY-RUN' } else { 'LIVE' }))
Say ('  local clock : {0}' -f $nowLocal.ToString('yyyy-MM-dd HH:mm:ss zzz'))
Say ('  UTC clock   : {0}   <- this machine stamps logs UTC' -f $nowUtc.ToString('yyyy-MM-dd HH:mm:ss'))
Say ('  repo root   : {0}' -f $RepoRoot)
Say ('  init        : {0}' -f $Init)
Say ('  order       : {0}' -f $Order)
Say ('  RunSecs     : {0}' -f $RunSecs)
Say ''
Say '  THIS SCRIPT DOES NOT SCORE. It collects evidence. HEADLESS_RUN_PLAN sec 4a'
Say '  is AWAITING USER RULING; sec 4a.6 makes run 1 a measurement, not a test.'

$Manifest.clocks.startLocal    = $nowLocal.ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
$Manifest.clocks.startUtc      = $nowUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
$Manifest.clocks.timeZoneId    = [System.TimeZoneInfo]::Local.Id
$Manifest.clocks.utcOffsetHours= [System.TimeZoneInfo]::Local.GetUtcOffset($nowLocal).TotalHours
$Manifest.host.machine         = $env:COMPUTERNAME
$Manifest.host.user            = $env:USERNAME
$Manifest.host.psVersion       = $PSVersionTable.PSVersion.ToString()
$Manifest.host.os              = [System.Environment]::OSVersion.VersionString

Say-Head 'Stage 0 - validation (nothing is launched or contacted until this passes)'
$bad = @()

if ($RunSecs -lt 30 -or $RunSecs -gt 86400) { $bad += ('-RunSecs must be 30..86400 (got {0})' -f $RunSecs) }
if ($SampleSecs -le 0 -or $SampleSecs -gt 3600) { $bad += ('-SampleSecs must be 1..3600 (got {0})' -f $SampleSecs) }
foreach ($pair in @(
    @{n='-PreRollSecs';v=$PreRollSecs}, @{n='-AppJoinTimeoutSec';v=$AppJoinTimeoutSec},
    @{n='-OracleGateTimeoutSec';v=$OracleGateTimeoutSec}, @{n='-InitDispatchWaitSec';v=$InitDispatchWaitSec},
    @{n='-PushOrderListenSec';v=$PushOrderListenSec}, @{n='-TrailSecs';v=$TrailSecs},
    @{n='-LaunchSettleSec';v=$LaunchSettleSec}, @{n='-AppExitTimeoutSec';v=$AppExitTimeoutSec},
    @{n='-StopVrfTimeoutSec';v=$StopVrfTimeoutSec})) {
    if ($pair.v -lt 0 -or $pair.v -gt 86400) { $bad += ('{0} must be 0..86400 (got {1})' -f $pair.n, $pair.v) }
}
# StopVrf.ps1 validates TimeoutSec 5..600 itself and exits 2 - catch it here so the
# failure lands BEFORE VR-Forces is launched instead of during teardown.
if ($StopVrfTimeoutSec -lt 5 -or $StopVrfTimeoutSec -gt 600) {
    $bad += ('-StopVrfTimeoutSec must be 5..600 - StopVrf.ps1 exits 2 outside that range (StopVrf.ps1:83-86). Got {0}.' -f $StopVrfTimeoutSec)
}
if ($PushOrderListenSec -gt 86400) { $bad += 'PushOrder accepts seconds-to-listen 0..86400 only.' }

foreach ($f in @(
    @{n='-Init';  p=$Init},
    @{n='-Order'; p=$Order})) {
    if (-not (Test-Path -LiteralPath $f.p -PathType Leaf)) { $bad += ('{0} file not found: {1}' -f $f.n, $f.p) }
}
foreach ($f in @(
    @{n='LaunchVrf.ps1';   p=$LaunchVrf},
    @{n='StopVrf.ps1';     p=$StopVrf},
    @{n='WatchVrf.exe';    p=$ExeWatchVrf},
    @{n='PushInit.exe';    p=$ExePushInit},
    @{n='PushOrder.exe';   p=$ExePushOrder},
    @{n='ListenReports.exe';p=$ExeListenReports},
    @{n='StopIface.exe';   p=$ExeStopIface},
    @{n='VrfC2SimApp.exe'; p=$ExeApp},
    @{n='Appendix B ledger'; p=$LedgerDoc})) {
    if (-not (Test-Path -LiteralPath $f.p -PathType Leaf)) { $bad += ('{0} not found: {1} (build Release, or fix the path)' -f $f.n, $f.p) }
}
if (-not (Test-Path -LiteralPath $Bin64 -PathType Container)) {
    $bad += ('VR-Forces bin64 not found: {0} - it is the mandatory cwd for every HLA process (RUNBOOK sec 7 item 3)' -f $Bin64)
}

# LIMITATION 1: ListenReports cannot be pointed anywhere. Refuse rather than
# capture nothing against a remote server.
foreach ($u in @(@{n='-RestUrl';v=$RestUrl}, @{n='-StompUrl';v=$StompUrl})) {
    if ($u.v -notmatch '(?i)://(127\.0\.0\.1|localhost|\[::1\])[:/]') {
        $bad += ("{0}='{1}' is not localhost. tools/ListenReports HARDCODES 127.0.0.1 (ListenReports/Program.cs:85-87) and takes no endpoint argument, so the report capture would be SILENTLY EMPTY. Refusing." -f $u.n, $u.v)
    }
}

# LIMITATION 6: clientId must equal the init's SystemName (RUNBOOK sec 2).
$initSystemNames = @()
$appClientId = $null
if (Test-Path -LiteralPath $Init -PathType Leaf) {
    try {
        $initText = Get-Content -LiteralPath $Init -Raw -Encoding UTF8
        $initSystemNames = @([regex]::Matches($initText, '<SystemName>([^<]*)</SystemName>') |
                             ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    } catch { $bad += ('could not read -Init to check SystemName: {0}' -f $_.Exception.Message) }
}
$appSettings = Join-Path (Split-Path -Parent $ExeApp) 'appsettings.json'
if (Test-Path -LiteralPath $appSettings -PathType Leaf) {
    try {
        $cfg = Get-Content -LiteralPath $appSettings -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($cfg.PSObject.Properties.Name -contains 'Vrf' -and
            $cfg.Vrf.PSObject.Properties.Name -contains 'ClientId') { $appClientId = [string]$cfg.Vrf.ClientId }
    } catch { Say-Warn ('could not parse {0}: {1}' -f $appSettings, $_.Exception.Message) }
}
if ($appClientId -and $initSystemNames.Count -gt 0 -and ($initSystemNames -notcontains $appClientId)) {
    $bad += ("clientId MISMATCH: appsettings Vrf:ClientId='{0}' but the init declares SystemName [{1}]. RUNBOOK sec 2: they MUST match or the interface creates 0 UNITS. Fix appsettings.json (or the init) before running." -f $appClientId, ($initSystemNames -join ','))
}
if (-not $appClientId) { Say-Warn 'could not read Vrf:ClientId from the app appsettings.json - the SystemName match is UNVERIFIED.' }

if ($bad.Count -gt 0) {
    Say-Head 'Result'
    foreach ($b in $bad) { Say-Fail $b }
    Say-Fail 'Aborting at validation. NOTHING was launched and NO server was contacted.'
    exit 2
}
Say-Ok 'inputs, tools, timing budgets and the clientId/SystemName match all validate'
Say-Ok ('init SystemName [{0}] matches app clientId [{1}]' -f ($initSystemNames -join ','), $appClientId)

$Manifest.inputs.init          = (Resolve-Path -LiteralPath $Init).Path
$Manifest.inputs.order         = (Resolve-Path -LiteralPath $Order).Path
$Manifest.inputs.runSecs       = $RunSecs
$Manifest.inputs.sampleSecs    = $SampleSecs
$Manifest.inputs.scenario      = $Scenario
$Manifest.inputs.federation    = $Federation
$Manifest.inputs.restUrl       = $RestUrl
$Manifest.inputs.stompUrl      = $StompUrl
$Manifest.inputs.clientId      = $appClientId
$Manifest.inputs.initSystemName= ($initSystemNames -join ',')

# ---- tool identities --------------------------------------------------------
function Get-ToolIdentity {
    param([string]$Path)
    $o = [ordered]@{ path = $Path; exists = $false }
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        $fi = Get-Item -LiteralPath $Path
        $o.exists         = $true
        $o.sizeBytes      = $fi.Length
        $o.lastWriteUtc   = $fi.LastWriteTimeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
        try {
            $vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($Path)
            if ($vi.FileVersion)    { $o.fileVersion    = $vi.FileVersion }
            if ($vi.ProductVersion) { $o.productVersion = $vi.ProductVersion }
        } catch { }
        try { $o.sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash } catch { }
    }
    return $o
}
foreach ($t in @(
    @{k='LaunchVrf';     p=$LaunchVrf},     @{k='StopVrf';   p=$StopVrf},
    @{k='WatchVrf';      p=$ExeWatchVrf},   @{k='PushInit';  p=$ExePushInit},
    @{k='PushOrder';     p=$ExePushOrder},  @{k='StopIface'; p=$ExeStopIface},
    @{k='ListenReports'; p=$ExeListenReports}, @{k='VrfC2SimApp'; p=$ExeApp},
    @{k='RunC2SimScenario'; p=$PSCommandPath})) {
    $Manifest.tools[$t.k] = Get-ToolIdentity -Path $t.p
}
try {
    $gitHead = & git -C $RepoRoot rev-parse HEAD 2>$null
    $gitBranch = & git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null
    $Manifest.host.gitCommit = ("$gitHead").Trim()
    $Manifest.host.gitBranch = ("$gitBranch").Trim()
} catch { $Manifest.host.gitCommit = '(unavailable)' }

# =============================================================================
# STAGE 1 - PRE-FLIGHT PROCESS INVENTORY (RUNBOOK 0.5.0)
# =============================================================================
Say-Head 'Stage 1 - pre-flight process inventory (RUNBOOK 0.5.0)'
$existing = @()
foreach ($n in @($ProcLauncher, $ProcBackend, $ProcFrontend)) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
        $threads = '?'
        try { $threads = $p.Threads.Count } catch { }
        $existing += [ordered]@{ name = $p.Name; processId = $p.Id; threads = $threads }
        Say-Warn ('{0} pid={1} threads={2} ALREADY RUNNING' -f $p.Name, $p.Id, $threads)
    }
}
$infra = @()
foreach ($n in $RtiNames) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
        $title = ''
        try { $title = $p.MainWindowTitle } catch { }
        $infra += [ordered]@{ name = $p.Name; processId = $p.Id; windowTitle = $title }
        Say-Ok ('{0} pid={1} - RTI INFRASTRUCTURE. Never touched, never refused on (RUNBOOK 0.5.2). Window: "{2}"' -f $p.Name, $p.Id, $title)
    }
}
$Manifest.preflight.existingVrf = $existing
$Manifest.preflight.rtiInfra    = $infra

if ($existing.Count -gt 0) {
    Say-Head 'Result'
    Say-Fail 'A VR-Forces instance is ALREADY RUNNING. Refusing to start.'
    Say-Fail '  RUNBOOK 0.5.0: an instance from an earlier session survives a context clear. Its'
    Say-Fail '  scenario contents are UNKNOWN and could contaminate a scored trace, and LaunchVrf.ps1'
    Say-Fail '  hard-fails on it anyway (LaunchVrf.ps1:255-260).'
    Say-Fail '  DO NOT reach for -AllowExistingVrf. It is the FALSE-READY trap: LaunchVrf picks the'
    Say-Fail '  back-end with Select-Object -First 1 and would measure the OLD instance. This runner'
    Say-Fail '  does not offer that switch at all.'
    Say-Fail '  FIX: pwsh -File scripts\StopVrf.ps1   (leaves rtiAssistant/rtiexec/rtiForwarder up), then re-run.'
    exit 2
}
Say-Ok 'no pre-existing vrfLauncher / vrfSimHLA1516e / vrfGui - clear to launch'
if ($infra.Count -eq 0) {
    Say-Warn 'no rtiAssistant is running. RUNBOOK 0.5.3: on HLA a federate does not start until an'
    Say-Warn '  RTI Assistant has been ANSWERED. LaunchVrf.ps1 warns about this too and will proceed;'
    Say-Warn '  if the launch stalls at 2-4 back-end threads, that is the cause. Do NOT kill anything.'
    Add-Flag 'WARN' 'No pre-existing rtiAssistant at pre-flight (RUNBOOK 0.5.3 - unattended launch depends on an already-answered one).'
}

# Optional read-only reachability probe of the C2SIM server, BEFORE VR-Forces goes
# up, so a dead broker costs a launch cycle instead of a whole run. Read-only GET.
if (-not $SkipServerCheck) {
    if ($DryRun) {
        Say-Plan ('would GET {0} (read-only reachability probe; -SkipServerCheck disables it). NOT PERFORMED IN A DRY RUN.' -f $RestUrl)
    } else {
        try {
            $r = Invoke-WebRequest -Uri $RestUrl -UseBasicParsing -TimeoutSec 15
            Say-Ok ('C2SIM REST reachable: HTTP {0}' -f $r.StatusCode)
            $Manifest.preflight.c2simRestStatus = $r.StatusCode
        } catch {
            $Manifest.preflight.c2simRestStatus = ('unreachable: ' + $_.Exception.Message)
            Say-Head 'Result'
            Say-Fail ('C2SIM REST at {0} is not reachable: {1}' -f $RestUrl, $_.Exception.Message)
            Say-Fail '  RUNBOOK sec 1: the c2sim-server container must be up (REST 8080, STOMP 61613).'
            Say-Fail '  Aborting BEFORE VR-Forces is launched. Pass -SkipServerCheck to bypass.'
            exit 2
        }
    }
}

# =============================================================================
# STAGE 2 - APPLICATION NUMBERS: ALLOCATE AND LEDGER BEFORE ANY JOIN
# =============================================================================
# HEADLESS_RUN_PLAN sec 2 / RUNBOOK 0.5.1 / Appendix B: EVERY join takes a fresh
# number from the single marker, ledgered BEFORE the join. That includes the app's
# own Vrf__ApplicationNumber, historically hand-set and exactly as capable of
# causing a stale-federate hang as any other join.
#
# The marker is searched BY ITS FORM. The bare string "*** NEXT FREE:" also
# matches the instructions and several pointers elsewhere in that file; only ONE
# line carries a number, and Appendix B says to STOP and reconcile if two are ever
# found. This regex enforces exactly that.
Say-Head 'Stage 2 - appNumber allocation (Appendix B marker), BEFORE any join'

$MarkerPattern = '\*\*\*\s*NEXT\s+FREE:\s*(\d+)\s*\*\*\*'

$ledgerRaw = Get-Content -LiteralPath $LedgerDoc -Raw -Encoding UTF8
$markerMatches = [regex]::Matches($ledgerRaw, $MarkerPattern)
if ($markerMatches.Count -ne 1) {
    Say-Head 'Result'
    Say-Fail ('found {0} value-bearing "NEXT FREE" markers in {1}; expected EXACTLY 1.' -f $markerMatches.Count, $LedgerDoc)
    Say-Fail '  Appendix B: "There is exactly ONE such line; if you ever find two, STOP and reconcile."'
    Say-Fail '  Aborting. Nothing was launched, nothing was ledgered.'
    exit 2
}
$FirstFree = [int]$markerMatches[0].Groups[1].Value
if ($FirstFree -le 0 -or $FirstFree -gt 65000) {
    Say-Fail ('the marker value {0} is not a usable appNumber (WatchVrf accepts 1..65535).' -f $FirstFree)
    exit 2
}

# The allocation. Purposes are the ledger text; keep them specific enough that a
# reader six months later can tell which join each number was.
$Alloc = @(
    [ordered]@{ key='vrfBackend';  purpose='LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode' }
    [ordered]@{ key='vrfFrontend'; purpose='LaunchVrf.ps1 front-end (vrfGui), combined mode' }
    [ordered]@{ key='oraclePre';   purpose='WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)' }
    [ordered]@{ key='oracleTrace'; purpose='WatchVrf MAIN run trace - the movement oracle / scoring input' }
    [ordered]@{ key='app';         purpose='VrfC2SimApp Vrf__ApplicationNumber (the interface federate)' }
)
for ($i = 0; $i -lt $Alloc.Count; $i++) { $Alloc[$i].appNumber = $FirstFree + $i }
$AppNo = @{}
foreach ($a in $Alloc) { $AppNo[$a.key] = $a.appNumber }
$NextFree = $FirstFree + $Alloc.Count

Say ('  marker currently reads : {0}' -f $FirstFree)
foreach ($a in $Alloc) { Say ('    {0,-6}  {1,-12} {2}' -f $a.appNumber, $a.key, $a.purpose) }
Say ('  marker would advance to: {0}' -f $NextFree)

$Manifest.appNumbers      = $Alloc
$Manifest.ledger.file     = $LedgerDoc
$Manifest.ledger.wasValue = $FirstFree
$Manifest.ledger.newValue = $NextFree

function Update-Ledger {
    param([int]$From, [int]$To, [string]$RunId, $Allocation)
    $nl = if ($ledgerRaw -match "`r`n") { "`r`n" } else { "`n" }
    $lines = @()
    $lines += ''
    $lines += ('CLAIMED {0} by scripts/RunC2SimScenario.ps1 (run {1}). Ledgered BEFORE any join,' -f (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm'), $RunId)
    $lines += 'per the never-reuse non-negotiable. Annotate with results from the run manifest.'
    foreach ($a in $Allocation) { $lines += ('- {0}: CLAIMED - {1}' -f $a.appNumber, $a.purpose) }
    $lines += 'NOTE: numbers this runner allocates but does not consume (e.g. an abort before the'
    $lines += 'join) are BURNED, not recycled. The run manifest records which were actually used.'
    $lines += ''
    $block = ($lines -join $nl) + $nl

    $m = [regex]::Match($ledgerRaw, $MarkerPattern)
    if (-not $m.Success) { throw 'the marker vanished between the read and the write - refusing to guess.' }
    $newMarker = $m.Value -replace [regex]::Escape($From.ToString()), $To.ToString()
    $updated = $ledgerRaw.Substring(0, $m.Index) + $block + $newMarker + $ledgerRaw.Substring($m.Index + $m.Length)

    # Re-verify BEFORE writing: exactly one marker, and it carries the new value.
    $check = [regex]::Matches($updated, $MarkerPattern)
    if ($check.Count -ne 1 -or [int]$check[0].Groups[1].Value -ne $To) {
        throw ('ledger rewrite self-check FAILED (markers={0}). Not written.' -f $check.Count)
    }
    [System.IO.File]::WriteAllText($LedgerDoc, $updated, (New-Object System.Text.UTF8Encoding($false)))
}

# =============================================================================
# RUN DIRECTORY + DERIVED PATHS
# =============================================================================
$RunId  = ('{0}_run' -f $stamp)
$RunDir = Join-Path $RunRoot $RunId

$PathTrace        = Join-Path $RunDir 'watchvrf-trace.csv'
$PathTraceErr     = Join-Path $RunDir 'watchvrf-trace.stderr.log'
$PathPreTrace     = Join-Path $RunDir 'watchvrf-precheck.csv'
$PathPreTraceErr  = Join-Path $RunDir 'watchvrf-precheck.stderr.log'
$PathReports      = Join-Path $RunDir 'reports-captured.log'
$PathReportsOut   = Join-Path $RunDir 'listenreports.stdout.log'
$PathReportsErr   = Join-Path $RunDir 'listenreports.stderr.log'
$PathAppLog       = Join-Path $RunDir 'vrfc2simapp.log'
$PathAppErr       = Join-Path $RunDir 'vrfc2simapp.stderr.log'
$PathLaunchOut    = Join-Path $RunDir 'launchvrf.stdout.log'
$PathLaunchErr    = Join-Path $RunDir 'launchvrf.stderr.log'
$PathPushInitOut  = Join-Path $RunDir 'pushinit.stdout.log'
$PathPushInitErr  = Join-Path $RunDir 'pushinit.stderr.log'
$PathPushOrderOut = Join-Path $RunDir 'pushorder.stdout.log'
$PathPushOrderErr = Join-Path $RunDir 'pushorder.stderr.log'
$PathBusLog       = Join-Path $RunDir 'c2sim-bus.log'
$PathStopIfaceOut = Join-Path $RunDir 'stopiface.stdout.log'
$PathStopIfaceErr = Join-Path $RunDir 'stopiface.stderr.log'
$PathStopVrfOut   = Join-Path $RunDir 'stopvrf.stdout.log'
$PathStopVrfErr   = Join-Path $RunDir 'stopvrf.stderr.log'
$ManifestPath     = Join-Path $RunDir 'run-manifest.json'

$Manifest.artifacts.runDir        = $RunDir
$Manifest.artifacts.trace         = $PathTrace
$Manifest.artifacts.preCheckTrace = $PathPreTrace
$Manifest.artifacts.reports       = $PathReports
$Manifest.artifacts.appLog        = $PathAppLog
$Manifest.artifacts.busLog        = $PathBusLog
$Manifest.artifacts.manifest      = $ManifestPath

# WatchVrf / ListenReports must cover the WHOLE run (a 4a.6 run-validity item), so
# the duration is an UPPER BOUND over every intermediate wait. If the joins are
# fast the observers simply keep sampling past the end of the window; that costs
# idle time and is preferable to a trace that stops mid-run.
$DerivedWatchSecs = $PreRollSecs + $AppJoinTimeoutSec + $InitDispatchWaitSec +
                    $OracleGateTimeoutSec + $PushOrderListenSec + $RunSecs + $TrailSecs
$EffWatchSecs = if ($WatchSecs -gt 0) { $WatchSecs } else { $DerivedWatchSecs }
if ($EffWatchSecs -le 0) { Say-Fail 'computed observer duration is not positive.'; exit 2 }
$Manifest.inputs.watchSecs        = $EffWatchSecs
$Manifest.inputs.watchSecsDerived = $DerivedWatchSecs

# HLA environment, identical for WatchVrf and the app (RUNBOOK sec 7 items 1-3).
$PathPrefix = ('{0};{1};{2}' -f $Bin64, (Join-Path $VrLinkRoot 'bin64'), (Join-Path $RtiDir 'bin'))
$LicMachine = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')

Say-Head 'Planned run'
Say ('  run id      : {0}' -f $RunId)
Say ('  run dir     : {0}' -f $RunDir)
Say ('  observers   : {0}s (derived: preRoll {1} + appJoin {2} + initDispatch {3} + oracleGate {4} + pushOrderListen {5} + run {6} + trail {7})' -f `
        $EffWatchSecs, $PreRollSecs, $AppJoinTimeoutSec, $InitDispatchWaitSec, $OracleGateTimeoutSec, $PushOrderListenSec, $RunSecs, $TrailSecs)
Say ('  HLA PATH    : {0};<inherited>' -f $PathPrefix)
Say ('  license     : MAKLMGRD_LICENSE_FILE (Machine) = {0}' -f $(if ($LicMachine) { $LicMachine } else { '(EMPTY - checkout may hang, RUNBOOK sec 7 item 2)' }))
Say ('  HLA cwd     : {0}' -f $Bin64)

# =============================================================================
# LIVE RUN
# =============================================================================
$RunnerExit          = 0
$WatchProc           = $null
$ListenProc          = $null
$AppProc             = $null
$VrfLaunched         = $false
$AppStarted          = $false
$SavedPath           = $env:PATH
$SavedLicense        = $env:MAKLMGRD_LICENSE_FILE
$SavedVrfAppNumber   = $env:Vrf__ApplicationNumber
$LedgerAdvanced      = $false

function Stop-Runner {
    param([int]$Code, [string]$Reason)
    Add-Flag 'FAIL' $Reason
    $script:RunnerExit = $Code
    throw [System.OperationCanceledException]::new($Reason)
}

try {
    if ($DryRun) {
        Say-Head 'DRY RUN - the full planned sequence, in order. NOTHING below is executed.'
        Say-Plan ('would create the run directory {0}' -f $RunDir)
        Say-Plan ('would REWRITE the Appendix B marker in {0}: {1} -> {2}, and append a CLAIMED block for {3} numbers.' -f $LedgerDoc, $FirstFree, $NextFree, $Alloc.Count)
        Say-Plan ('would set, for HLA child processes only: PATH="{0};<inherited>", MAKLMGRD_LICENSE_FILE from Machine scope, Vrf__ApplicationNumber={1}' -f $PathPrefix, $AppNo['app'])
        Say ''
    } else {
        New-Item -ItemType Directory -Path $RunDir -Force | Out-Null
        Say-Ok ('run directory created: {0}' -f $RunDir)
        Save-Manifest

        Update-Ledger -From $FirstFree -To $NextFree -RunId $RunId -Allocation $Alloc
        $LedgerAdvanced = $true
        $Manifest.ledger.advanced = $true
        Say-Ok ('Appendix B marker advanced {0} -> {1} and {2} numbers CLAIMED, BEFORE any join' -f $FirstFree, $NextFree, $Alloc.Count)
        Save-Manifest

        $env:PATH = ('{0};{1}' -f $PathPrefix, $SavedPath)
        if (-not [string]::IsNullOrWhiteSpace($LicMachine)) {
            $env:MAKLMGRD_LICENSE_FILE = $LicMachine
            Say-Ok 'MAKLMGRD_LICENSE_FILE refreshed from Machine scope'
        } elseif (-not [string]::IsNullOrWhiteSpace($SavedLicense)) {
            Say-Warn 'Machine-scope MAKLMGRD_LICENSE_FILE is EMPTY - PRESERVING the process value rather than blanking it.'
        } else {
            Add-Flag 'WARN' 'MAKLMGRD_LICENSE_FILE empty in both Machine and process scope - license checkout may hang (RUNBOOK sec 7 item 2).'
        }
    }

    # ---------------------------------------------------------------------
    # STAGE 3 - bring VR-Forces up
    # ---------------------------------------------------------------------
    Say-Head 'Stage 3 - LaunchVrf.ps1 (combined mode)'
    $launchArgs = @(
        '-NoProfile','-File', $LaunchVrf,
        '-Scenario', $Scenario,
        '-VrfRoot', $VrfRoot,
        '-RtiDir', $RtiDir,
        '-BackendAppNumber',  [string]$AppNo['vrfBackend'],
        '-FrontendAppNumber', [string]$AppNo['vrfFrontend']
    )
    $r = Invoke-External -Name 'LaunchVrf' -File 'pwsh' -Arguments $launchArgs -Cwd $RepoRoot `
            -StdOutFile $PathLaunchOut -StdErrFile $PathLaunchErr `
            -Note 'exit 0 READY; 1 PARTIAL (no front-end - crash risk); 2 precondition/args; 3 NOT READY within timeout; 4 BLOCKED (modal dialog). -AllowExistingVrf is deliberately NOT passed.'
    if (-not $DryRun) {
        $VrfLaunched = $true
        switch ($r.ExitCode) {
            0 { Say-Ok 'VR-Forces READY' }
            2 { Stop-Runner 2 'LaunchVrf exited 2 (precondition/argument failure). Nothing joined.' }
            1 { Stop-Runner 3 'LaunchVrf exited 1 PARTIAL - back-end healthy but no front-end. That is the known 0xC0000005 crash-risk condition; refusing to run a scored trace on it.' }
            3 { Stop-Runner 3 'LaunchVrf exited 3 NOT READY within timeout. Usual cause is an UNANSWERED RTI Assistant prompt (RUNBOOK 0.5.3). Nothing force-killed.' }
            4 { Stop-Runner 3 'LaunchVrf exited 4 BLOCKED - the front-end has no main window, i.e. a modal dialog is waiting. Nothing force-killed.' }
            default { Stop-Runner 3 ('LaunchVrf exited {0} - undocumented code.' -f $r.ExitCode) }
        }
    }

    # LaunchVrf's READY is thread-count + main-window only. It does NOT imply
    # scenario loaded or federation joined - the script says so itself, and
    # RUNBOOK 0.5.7 measured settle times past 50 s.
    Say-Head ('Stage 3b - settle {0}s before touching the oracle (RUNBOOK 0.5.7: READY does NOT mean joined)' -f $LaunchSettleSec)
    if ($DryRun) { Say-Plan ('would sleep {0}s' -f $LaunchSettleSec) } else { Start-Sleep -Seconds $LaunchSettleSec }

    # ---------------------------------------------------------------------
    # STAGE 4 - ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
    # ---------------------------------------------------------------------
    Say-Head 'Stage 4 - oracle pre-check, pre-init (ADVISORY - see the header block)'
    Say '  RUNBOOK 0.5.7: a stock TropicTortoise contains only POSITIONLESS control objects,'
    Say '  so a DEGENERATE result here is EXPECTED and is NOT a fault. What this stage proves'
    Say '  is that the oracle can JOIN and DISCOVER. The real coordinate criterion is applied'
    Say '  post-init at stage 7, against the scoring trace.'
    $preSecs = 30
    $r = Invoke-External -Name 'WatchVrf-precheck' -File $ExeWatchVrf `
            -Arguments @([string]$AppNo['oraclePre'], [string]$preSecs, [string]$SampleSecs, $Federation) `
            -Cwd $Bin64 -StdOutFile $PathPreTrace -StdErrFile $PathPreTraceErr `
            -Note 'ADVISORY. WatchVrf OVERLOADS exit 2 (usage AND operational exception) - a 2 here is operational, since these arguments are generated.'
    if (-not $DryRun) {
        $preText = Read-LiveText -Path $PathPreTrace
        $pre     = Get-RealPositions -TraceText $preText
        $Manifest.oracle.preInit = [ordered]@{
            advisory        = $true
            appNumber       = $AppNo['oraclePre']
            exitCode        = $r.ExitCode
            posLines        = $pre.PosLineCount
            realCoordLines  = $pre.RealCount
            degenerateLines = $pre.DegenerateCount
            lastSummaryLine = (Get-TraceSummaryLine -TraceText $preText)
            firstRealLine   = $(if ($pre.First) { $pre.First.line } else { $null })
        }
        Say ('  POS lines={0} real={1} degenerate={2}  last: {3}' -f $pre.PosLineCount, $pre.RealCount, $pre.DegenerateCount, (Get-TraceSummaryLine -TraceText $preText))
        if ($pre.RealCount -gt 0) {
            Say-Ok ('oracle already reads a REAL coordinate pre-init: {0}' -f $pre.First.line)
        } else {
            Add-Flag 'INFO' 'Pre-init oracle pre-check saw NO real-coordinate POS line. EXPECTED on a stock TropicTortoise (RUNBOOK 0.5.7); advisory only.'
            if ($StrictPreInitOracle) {
                Stop-Runner 3 '-StrictPreInitOracle was set and the pre-init pre-check found no real coordinate.'
            }
        }
        if ($r.ExitCode -ne 0) {
            Add-Flag 'WARN' ('WatchVrf pre-check exited {0} (advisory stage). Exit 2 from WatchVrf is AMBIGUOUS - usage or operational.' -f $r.ExitCode)
        }
        Save-Manifest
    }

    # ---------------------------------------------------------------------
    # STAGE 5 - OBSERVERS START FIRST, BEFORE THE INIT (unit births in trace)
    # ---------------------------------------------------------------------
    Say-Head 'Stage 5 - start the observers BEFORE the init, so unit births are in the trace'
    $WatchProc = Start-External -Name 'WatchVrf-trace' -File $ExeWatchVrf `
            -Arguments @([string]$AppNo['oracleTrace'], [string]$EffWatchSecs, [string]$SampleSecs, $Federation) `
            -Cwd $Bin64 -StdOutFile $PathTrace -StdErrFile $PathTraceErr `
            -Note 'THE MOVEMENT ORACLE and the scoring input. Started before PushInit (HEADLESS_RUN_PLAN sec 2). Resigns on its own timer; never killed.'
    $ListenProc = Start-External -Name 'ListenReports' -File $ExeListenReports `
            -Arguments @([string]$EffWatchSecs, $PathReports) `
            -Cwd $RepoRoot -StdOutFile $PathReportsOut -StdErrFile $PathReportsErr `
            -Note 'Endpoints are HARDCODED to localhost inside the tool; -RestUrl/-StompUrl do not reach it.'

    Say-Head ('Stage 5b - pre-roll {0}s of trace before the init is pushed' -f $PreRollSecs)
    if ($DryRun) { Say-Plan ('would sleep {0}s' -f $PreRollSecs) } else { Start-Sleep -Seconds $PreRollSecs }

    # ---------------------------------------------------------------------
    # STAGE 6 - PushInit, THEN the app (RUNBOOK sec 3: order matters)
    # ---------------------------------------------------------------------
    Say-Head 'Stage 6 - PushInit, then start the interface (RUNBOOK sec 3: init FIRST, the app late-joins)'
    $r = Invoke-External -Name 'PushInit' -File $ExePushInit `
            -Arguments @($Init, $RestUrl, $StompUrl) -Cwd $RepoRoot `
            -StdOutFile $PathPushInitOut -StdErrFile $PathPushInitErr `
            -Note 'exit 0 ok; 1 push rejected; 2 usage. Drives the server RESET -> INITIALIZING -> share -> RUNNING. Never run against a RUNNING interface (RUNBOOK sec 4 corollary) - none is running yet.'
    if (-not $DryRun) {
        switch ($r.ExitCode) {
            0 { Say-Ok 'init accepted and server switched to RUNNING' }
            2 { Stop-Runner 2 'PushInit exited 2 (usage error). The server was NOT touched.' }
            default { Stop-Runner 3 ('PushInit exited {0} - the init was rejected or the push failed. See {1}.' -f $r.ExitCode, $PathPushInitOut) }
        }
        $pushInitText = Read-LiveText -Path $PathPushInitOut
        $qm = [regex]::Match($pushInitText, 'QUERYINIT\s*:\s*(\d+)\s+Units')
        if ($qm.Success) {
            $Manifest.oracle.queryInitUnits = [int]$qm.Groups[1].Value
            Say-Ok ('QUERYINIT reports {0} units will be handed to a late joiner' -f $qm.Groups[1].Value)
            if ([int]$qm.Groups[1].Value -eq 0) {
                Add-Flag 'FAIL' 'QUERYINIT reports 0 Units - the interface will create nothing. Continuing to collect evidence, but this run is NOT valid (4a.6).'
            }
        } else {
            Add-Flag 'WARN' 'could not read the QUERYINIT unit count out of PushInit stdout.'
        }
    }

    Say-Head 'Stage 6b - start VrfC2SimApp with a LEDGERED ApplicationNumber'
    Say ('  Vrf__ApplicationNumber={0} comes from the Appendix B marker, NOT from appsettings.json' -f $AppNo['app'])
    Say  '  (appsettings.json carries a baked-in ApplicationNumber; hand-editing it is exactly how stale-federate hangs were created - the env override wins and is ledgered)'
    if (-not $DryRun) { $env:Vrf__ApplicationNumber = [string]$AppNo['app'] }
    else { Say-Plan ('would set env Vrf__ApplicationNumber={0} for the child, then clear it' -f $AppNo['app']) }
    $AppProc = Start-External -Name 'VrfC2SimApp' -File $ExeApp `
            -Arguments @(('--contentRoot=' + (Split-Path -Parent $ExeApp))) -Cwd $Bin64 `
            -StdOutFile $PathAppLog -StdErrFile $PathAppErr `
            -Note 'cwd MUST be VR-Forces bin64 so Legion finds vrfLegion.lua (RUNBOOK sec 7 item 3); --contentRoot keeps appsettings.json loading. ApplicationNumber overridden via env.'
    if (-not $DryRun) {
        $AppStarted = $true
        $env:Vrf__ApplicationNumber = $SavedVrfAppNumber
    }

    Say-Head ('Stage 6c - wait up to {0}s for the interface to connect to C2SIM' -f $AppJoinTimeoutSec)
    if ($DryRun) {
        Say-Plan ('would poll {0} for "Connected to C2SIM", with the app thread count as a backstop' -f $PathAppLog)
    } else {
        $deadline = (Get-Date).AddSeconds($AppJoinTimeoutSec)
        $connected = $false
        $threads = 0
        while ((Get-Date) -lt $deadline) {
            if ($AppProc.HasExited) {
                Stop-Runner 3 ('VrfC2SimApp exited early with code {0} - see {1} and {2}.' -f $AppProc.ExitCode, $PathAppLog, $PathAppErr)
            }
            $appText = Read-LiveText -Path $PathAppLog
            if ($appText -match 'Connected to C2SIM') { $connected = $true; break }
            try { $threads = (Get-Process -Id $AppProc.Id -ErrorAction Stop).Threads.Count } catch { $threads = 0 }
            Start-Sleep -Seconds 3
        }
        $Manifest.oracle.appConnected = $connected
        $Manifest.oracle.appThreads   = $threads
        if ($connected) { Say-Ok 'interface logged "Connected to C2SIM"' }
        else {
            # RUNBOOK sec 3: a redirected stdout can be block-buffered, so absence of
            # the line is not proof of absence of the connect. Thread count is the
            # independent signal (connected ~9-10, hang-at-RTI 1).
            Add-Flag 'WARN' ('did not see "Connected to C2SIM" within {0}s (thread count {1}). Redirected stdout can be block-buffered (RUNBOOK sec 3), so this is NOT proof it failed. Continuing.' -f $AppJoinTimeoutSec, $threads)
        }
    }

    Say-Head ('Stage 6d - wait up to {0}s for the interface to dispatch the init' -f $InitDispatchWaitSec)
    if ($DryRun) {
        Say-Plan ('would poll {0} for "Init dispatched: N units + M areas queued for creation"' -f $PathAppLog)
    } else {
        $deadline = (Get-Date).AddSeconds($InitDispatchWaitSec)
        $dm = $null
        while ((Get-Date) -lt $deadline) {
            if ($AppProc.HasExited) {
                Stop-Runner 3 ('VrfC2SimApp exited before dispatching the init (code {0}).' -f $AppProc.ExitCode)
            }
            $dm = [regex]::Match((Read-LiveText -Path $PathAppLog), 'Init dispatched:\s*(\d+)\s+units\s*\+\s*(\d+)\s+areas')
            if ($dm.Success) { break }
            Start-Sleep -Seconds 3
        }
        if ($dm -and $dm.Success) {
            $Manifest.oracle.initDispatchedUnits = [int]$dm.Groups[1].Value
            $Manifest.oracle.initDispatchedAreas = [int]$dm.Groups[2].Value
            Say-Ok ('interface dispatched {0} units + {1} areas for creation' -f $dm.Groups[1].Value, $dm.Groups[2].Value)
        } else {
            $Manifest.oracle.initDispatchedUnits = $null
            Add-Flag 'WARN' ('no "Init dispatched" line within {0}s. Continuing deliberately: a wasted observation window is cheaper than a lost diagnosis, and the trace + app log are the evidence either way.' -f $InitDispatchWaitSec)
        }
        Save-Manifest
    }

    # ---------------------------------------------------------------------
    # STAGE 7 - THE ORACLE GATE (RUNBOOK 0.5.7 CORRECTED criterion, for real)
    # ---------------------------------------------------------------------
    Say-Head ('Stage 7 - ORACLE GATE: a POS line with REAL lat/lon, retried up to {0}s' -f $OracleGateTimeoutSec)
    Say '  RUNBOOK 0.5.7 CORRECTED: reflected>0 is NOT sufficient (it passes on pole/NaN'
    Say '  garbage) and reflected=0 at 20 s is NOT a stop (settle time exceeded 50 s live).'
    Say '  PASS = lat/lon real, not NaN, not the 90/-90 pole. Applied to the live trace.'
    if ($DryRun) {
        Say-Plan ('would poll {0} every 5s for up to {1}s for a real-coordinate POS line; STOP the run if none appears' -f $PathTrace, $OracleGateTimeoutSec)
    } else {
        $deadline = (Get-Date).AddSeconds($OracleGateTimeoutSec)
        $gate = $null
        while ($true) {
            $traceText = Read-LiveText -Path $PathTrace
            $gate = Get-RealPositions -TraceText $traceText
            if ($gate.RealCount -gt 0) { break }
            if ((Get-Date) -ge $deadline) { break }
            Say-Info ('  no real coordinate yet - {0}' -f $(if (Get-TraceSummaryLine -TraceText $traceText) { Get-TraceSummaryLine -TraceText $traceText } else { 'no samples yet' }))
            Start-Sleep -Seconds 5
        }
        $Manifest.oracle.gate = [ordered]@{
            criterion       = 'RUNBOOK 0.5.7 CORRECTED: at least one POS line with real (non-NaN, non-pole) lat/lon. This is a RUN-VALIDITY gate, NOT a score.'
            timeoutSec      = $OracleGateTimeoutSec
            posLines        = $gate.PosLineCount
            realCoordLines  = $gate.RealCount
            degenerateLines = $gate.DegenerateCount
            distinctRealUuids = @($gate.Uuids)
            firstRealLine   = $(if ($gate.First) { $gate.First.line } else { $null })
            passed          = ($gate.RealCount -gt 0)
        }
        Save-Manifest
        if ($gate.RealCount -gt 0) {
            Say-Ok ('ORACLE GATE PASSED: {0} real-coordinate POS lines across {1} distinct uuids' -f $gate.RealCount, $gate.Uuids.Count)
            Say-Ok ('  first: {0}' -f $gate.First.line)
        } else {
            Stop-Runner 3 ("ORACLE GATE FAILED: no POS line with a real coordinate within {0}s (POS lines seen: {1}, all degenerate). RUNBOOK 0.5.7 STOP condition. Run the STRONGER CHECK by hand (tools/CreateOne with a fresh ledgered appNo, then WatchVrf on that uuid) before blaming the interface." -f $OracleGateTimeoutSec, $gate.PosLineCount)
        }
    }

    # ---------------------------------------------------------------------
    # STAGE 8 - push the order and observe
    # ---------------------------------------------------------------------
    Say-Head 'Stage 8 - PushOrder, then observe'
    $r = Invoke-External -Name 'PushOrder' -File $ExePushOrder `
            -Arguments @($Order, [string]$PushOrderListenSec, $RestUrl, $StompUrl) -Cwd $RepoRoot `
            -StdOutFile $PathPushOrderOut -StdErrFile $PathPushOrderErr `
            -Note 'BLOCKS for seconds-to-listen. exit 0 ok; 1 order rejected; 2 usage. Writes c2sim-bus.log beside its own binary; copied into the run dir afterwards.'
    $orderPushedUtc = (Get-Date).ToUniversalTime()
    if (-not $DryRun) {
        $Manifest.clocks.orderPushedUtc   = $orderPushedUtc.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        $Manifest.clocks.orderPushedLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
        switch ($r.ExitCode) {
            0 { Say-Ok 'order accepted by the server' }
            2 { Stop-Runner 2 'PushOrder exited 2 (usage error). Nothing was pushed.' }
            default { Stop-Runner 3 ('PushOrder exited {0} - the server REJECTED the order. See {1}.' -f $r.ExitCode, $PathPushOrderOut) }
        }
        # LIMITATION 3: copy the bus log, and record whether it is actually ours.
        $srcBus = Join-Path (Split-Path -Parent $ExePushOrder) 'c2sim-bus.log'
        if (Test-Path -LiteralPath $srcBus -PathType Leaf) {
            $busWrite = (Get-Item -LiteralPath $srcBus).LastWriteTimeUtc
            Copy-Item -LiteralPath $srcBus -Destination $PathBusLog -Force
            $fresh = ($busWrite -ge $orderPushedUtc.AddSeconds(-($PushOrderListenSec + 60)))
            $Manifest.artifacts.busLogSourceWriteUtc = $busWrite.ToString('yyyy-MM-ddTHH:mm:ssZ')
            $Manifest.artifacts.busLogIsFromThisRun  = $fresh
            if ($fresh) { Say-Ok ('bus log copied: {0}' -f $PathBusLog) }
            else { Add-Flag 'WARN' ('the copied c2sim-bus.log was last written {0} - it may be from an EARLIER run (PushOrder gives no output-path override).' -f $busWrite) }
        } else {
            Add-Flag 'WARN' 'PushOrder produced no c2sim-bus.log beside its binary.'
        }
    }

    Say-Head ('Stage 8b - observation window: {0}s' -f $RunSecs)
    Say '  Nothing is judged here. The trace and the report capture are the evidence;'
    Say '  scoring happens later, against a ratified 4a.'
    if ($DryRun) {
        Say-Plan ('would sleep {0}s while WatchVrf and ListenReports keep sampling' -f $RunSecs)
    } else {
        $obsEnd = (Get-Date).AddSeconds($RunSecs)
        $appDeathRecorded = $false
        while ((Get-Date) -lt $obsEnd) {
            Start-Sleep -Seconds 30
            $remaining = [int]([Math]::Max(0, ($obsEnd - (Get-Date)).TotalSeconds))
            $sum = Get-TraceSummaryLine -TraceText (Read-LiveText -Path $PathTrace)
            Say-Info ('  {0}s remaining   trace: {1}' -f $remaining, $(if ($sum) { $sum } else { '(no samples)' }))
            # An interface that dies mid-window is RECORDED, ONCE, and the window is
            # then RUN OUT rather than cut short: 4a.6 makes "the trace covers the
            # whole run" a validity item, and a truncated trace destroys the evidence
            # that would explain the death. Do not turn this into a break.
            if ($AppProc.HasExited -and -not $appDeathRecorded) {
                $appDeathRecorded = $true
                Add-Flag 'FAIL' ('VrfC2SimApp exited DURING the observation window with code {0}. The window is being RUN OUT anyway so the trace still covers it; the run is NOT valid (4a.6) but the evidence is preserved.' -f $AppProc.ExitCode)
            }
        }
        $Manifest.clocks.observationEndUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        Say-Ok 'observation window complete'
    }

    if ($DryRun) {
        Say-Head 'DRY RUN - teardown that WOULD follow (it also runs on every failure path)'
    }
}
catch [System.OperationCanceledException] {
    # Stop-Runner already recorded the reason and set $RunnerExit.
    if ($RunnerExit -eq 0) { $RunnerExit = 3 }
}
catch {
    $RunnerExit = 5
    Say-Fail ('unexpected terminating error: {0}' -f $_.Exception.Message)
    Say-Fail ('at: {0}' -f $_.InvocationInfo.PositionMessage)
    Add-Flag 'FAIL' ('unexpected terminating error: {0}' -f $_.Exception.Message)
}
finally {
    # =====================================================================
    # TEARDOWN - runs on EVERY path. StopIface (clean resign) THEN StopVrf.
    # Nothing here force-kills anything, ever.
    # =====================================================================
    Say-Head 'Teardown (runs on every path, success or failure)'

    $teardownOk = $true

    # 1. StopIface: drive the C2SIM server to UNINITIALIZED so the interface
    #    RESIGNS from the RTI (RUNBOOK sec 4). This is the ONLY correct way to
    #    stop the interface. Skipped when the app was never started, so a
    #    validation abort does not tear down someone else's live session.
    if ($AppStarted -or $DryRun) {
        $r = Invoke-External -Name 'StopIface' -File $ExeStopIface `
                -Arguments @($RestUrl, $StompUrl, '--yes') -Cwd $RepoRoot `
                -StdOutFile $PathStopIfaceOut -StdErrFile $PathStopIfaceErr `
                -Note 'REQUIRES <restUrl> <stompUrl> --yes; NO defaults. exit 0 ok; 1 the server did NOT reach UNINITIALIZED (the interface MAY STILL BE JOINED); 2 usage.'
        if (-not $DryRun) {
            switch ($r.ExitCode) {
                0 { Say-Ok 'server driven to UNINITIALIZED; the interface should resign' }
                default {
                    $teardownOk = $false
                    Add-Flag 'FAIL' ('StopIface exited {0}. The server may NOT be UNINITIALIZED and the interface may STILL BE JOINED. Nothing was force-killed. INSPECT BEFORE THE NEXT RUN.' -f $r.ExitCode)
                }
            }
        }
    } else {
        Say-Info 'interface was never started - StopIface skipped (it is destructive and would hit a server this run never used)'
    }

    # 2. Wait for the interface to exit ON ITS OWN. NEVER force-killed: a
    #    force-killed joined federate is a stale federate (RUNBOOK sec 0), and
    #    the next start hangs at RTI join.
    if ($AppProc -and -not $DryRun) {
        Say-Info ('waiting up to {0}s for VrfC2SimApp (pid {1}) to resign and exit' -f $AppExitTimeoutSec, $AppProc.Id)
        $null = $AppProc.WaitForExit($AppExitTimeoutSec * 1000)
        if ($AppProc.HasExited) {
            Say-Ok ('VrfC2SimApp exited with code {0} (clean resign)' -f $AppProc.ExitCode)
            foreach ($s in $Manifest.stages) {
                if ($s.name -eq 'VrfC2SimApp' -and $s.outcome -eq 'started-background') {
                    $s.exitCode = $AppProc.ExitCode
                    $s.endedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                    $s.outcome  = 'exited'
                    break
                }
            }
        } else {
            $teardownOk = $false
            Add-Flag 'FAIL' ("VrfC2SimApp (pid {0}) did NOT exit within {1}s. IT IS NOT BEING KILLED - a force-killed joined federate leaves a STALE FEDERATE and the next start hangs at RTI join (RUNBOOK sec 0). MANUAL INSPECTION REQUIRED: check whether the server actually reached UNINITIALIZED, or use tools/ResetVrf (RUNBOOK sec 8)." -f $AppProc.Id, $AppExitTimeoutSec)
        }
    } elseif ($DryRun) {
        Say-Plan 'would wait for VrfC2SimApp to exit on its own; would NEVER kill it'
    }

    # 3. Let the observers finish their own timers. They resign cleanly.
    Complete-Background -Name 'WatchVrf-trace' -Process $WatchProc -TimeoutSec ($EffWatchSecs + 120) `
        -Note 'THE MOVEMENT ORACLE trace. Allowed to run its full duration and resign itself.'
    Complete-Background -Name 'ListenReports' -Process $ListenProc -TimeoutSec ($EffWatchSecs + 120)
    if ($DryRun) {
        Say-Plan 'would wait for WatchVrf and ListenReports to finish their own timers (never killed)'
    }

    # 4. Bring VR-Forces down. RTI infrastructure is preserved by StopVrf itself.
    if ($VrfLaunched -or $DryRun) {
        $r = Invoke-External -Name 'StopVrf' -File 'pwsh' `
                -Arguments @('-NoProfile','-File', $StopVrf, '-TimeoutSec', [string]$StopVrfTimeoutSec) `
                -Cwd $RepoRoot -StdOutFile $PathStopVrfOut -StdErrFile $PathStopVrfErr `
                -Note 'exit 0 down/already down; 2 bad args; 3 timed out (NOT killed); 4 confirm dialog not drivable via UIA; 5 unexpected error - VR-FORCES MAY STILL BE RUNNING. An unattended runner must branch on 5 as well as 3 (RUNBOOK 0.5.9).'
        if (-not $DryRun) {
            switch ($r.ExitCode) {
                0 { Say-Ok 'VR-Forces is down (graceful; RTI infrastructure preserved)' }
                default {
                    $teardownOk = $false
                    Add-Flag 'FAIL' ('StopVrf exited {0}. VR-Forces MAY STILL BE RUNNING, possibly behind an unanswered modal. NOTHING was force-killed. Inspect before the next run - a leftover instance HARD-BLOCKS the next launch.' -f $r.ExitCode)
                }
            }
        }
    } else {
        Say-Info 'VR-Forces was never launched by this run - StopVrf skipped'
    }

    # 5. Post-teardown inventory: what is left, and confirm RTI survived.
    if (-not $DryRun) {
        $left = @()
        foreach ($n in @($ProcLauncher, $ProcBackend, $ProcFrontend)) {
            foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
                $left += ('{0}(pid {1})' -f $p.Name, $p.Id)
            }
        }
        $rtiLeft = @()
        foreach ($n in $RtiNames) {
            foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) { $rtiLeft += ('{0}(pid {1})' -f $p.Name, $p.Id) }
        }
        $Manifest.preflight.postRunVrf = $left
        $Manifest.preflight.postRunRti = $rtiLeft
        if ($left.Count -gt 0) {
            $teardownOk = $false
            Add-Flag 'FAIL' ('VR-Forces processes still present after teardown: {0}. Not killed.' -f ($left -join ', '))
        } else { Say-Ok 'no VR-Forces processes remain' }
        if ($rtiLeft.Count -gt 0) { Say-Ok ('RTI infrastructure preserved (correct): {0}' -f ($rtiLeft -join ', ')) }

        # Restore this process's environment.
        $env:PATH                  = $SavedPath
        $env:MAKLMGRD_LICENSE_FILE = $SavedLicense
        $env:Vrf__ApplicationNumber= $SavedVrfAppNumber

        if (-not $teardownOk -and $RunnerExit -lt 4) { $RunnerExit = 4 }
        $Manifest.runnerExitCode = $RunnerExit
        Save-Manifest
    }

    Say-Head 'Result'
    if ($DryRun) {
        Say-Ok 'DRY-RUN complete.'
        Say-Ok ('  NOTHING was launched, NO server was contacted, the Appendix B marker was NOT advanced (it still reads {0}).' -f $FirstFree)
        Say-Ok ('  No run directory was created ({0} does not exist because of this invocation).' -f $RunDir)
        Say-Ok  '  This script COLLECTS EVIDENCE and does NOT score. HEADLESS_RUN_PLAN sec 4a is unratified.'
        exit 0
    }

    Say ('  run directory : {0}' -f $RunDir)
    Say ('  manifest      : {0}' -f $ManifestPath)
    Say ('  appNumbers    : {0} (marker advanced {1} -> {2}, ledgered={3})' -f `
            (($Alloc | ForEach-Object { $_.appNumber }) -join ','), $FirstFree, $NextFree, $LedgerAdvanced)
    Say ('  local / UTC   : {0} / {1}' -f (Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'), (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss'))
    foreach ($f in $Manifest.validityFlags) { Say ('  [{0}] {1}' -f $f.severity, $f.text) }
    switch ($RunnerExit) {
        0 { Say-Ok 'RUN COMPLETE - evidence collected. THIS IS NOT A VERDICT: sec 4a.6 makes run 1 a measurement. Score the trace separately.' }
        2 { Say-Fail 'ABORTED at validation / usage. Nothing was launched where it could be checked first.' }
        3 { Say-Fail 'RUN FAILED after VR-Forces was up. Teardown ran. Evidence is PARTIAL - the manifest names the stage.' }
        4 { Say-Fail 'TEARDOWN INCOMPLETE. VR-Forces and/or the interface MAY STILL BE RUNNING and MAY STILL BE JOINED. Nothing was force-killed. INSPECT BEFORE THE NEXT RUN.' }
        5 { Say-Fail 'UNEXPECTED TERMINATING ERROR. Same warning as exit 4 - inspect before the next run.' }
    }
    exit $RunnerExit
}
