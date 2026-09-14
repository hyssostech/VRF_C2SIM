# scripts\RunnerWatchdog.ps1 - the DETACHED teardown watchdog (design A2:
# docs\experiments\RUNNER_EXIT127_2026-09-14.md sec 3.1). It closes item 1 of
# docs\experiments\RUNNER_HARDENING_2026-09-14.md sec 8 - "NOT covered: the wrapper dying too".
#
# WHY THIS EXISTS. On 2026-09-14 the runner pwsh was TERMINATED from outside
# (TerminateProcess). No finally / trap / Register-EngineEvent PowerShell.Exiting /
# AppDomain.ProcessExit handler survives that, so the runner's teardown never ran and
# VR-Forces plus the interface stayed joined for nine hours. The first backstop
# (scripts\RunScenario.sh) repairs that AS LONG AS THE WRAPPER'S BASH SURVIVES THE RUNNER.
# This process covers the case it does not: a closed terminal, a kill that takes the whole
# shell, a launch path with no wrapper at all. It is a separate process with its own console
# and its own redirected handles, started by the runner at Stage 6b and outliving it by
# design.
#
# WHAT IT DOES. Polls the runner's PID every -PollSec. A death must be observed TWICE, 2 s
# apart, before anything is touched - one transient failed read is not a death (review of
# 374ea49, finding F2). When the runner is gone:
#   runner.launched      ABSENT  -> REFUSE and touch nothing. The run launched nothing, so
#                                   whatever is up may be a FOREIGN live session (RUNBOOK
#                                   sec 0). This is the same rule the wrapper follows.
#   runner.teardown-ran  PRESENT -> the runner completed its own teardown. Exit, touch nothing.
#   runner.watchdog-ran  PRESENT -> another backstop claimed the teardown first. Exit.
#   otherwise                    -> CLAIM runner.watchdog-ran (atomically), then StopIface
#                                   (clean resign), then StopVrf52 / StopVrf, then touch
#                                   observers.stop. The runner's own teardown order.
#
# NOTHING IS EVER FORCE-KILLED HERE, on any path - not the interface, not the back-end, not
# a teardown tool that overran its budget. A force-killed joined federate is a stale federate
# and the next start hangs at RTI join (RUNBOOK sec 0). rtiexec / rtiForwarder / rtiAssistant
# are RTI infrastructure: this script never stops them and never hands them to anything that
# could (RUNBOOK 0.5.2); it only REPORTS them in the closing inventory.
#
# IDEMPOTENCE, STATED HONESTLY. runner.watchdog-ran is claimed with CreateNew, so two
# watchdogs cannot both tear down. scripts\RunScenario.sh's backstop does NOT yet read or
# write that marker (it predates this script), so a runner killed while BOTH the wrapper and
# this watchdog are alive can be torn down twice - the wrapper first (it fires the instant the
# runner returns) and this watchdog ~PollSec later. That duplication is benign: every step is
# a graceful, idempotent request (StopIface against an already-UNINITIALIZED server, StopVrf
# against nothing to stop, an observers.stop that already exists). Closing it completely is a
# one-line change in the wrapper - see docs\experiments\RUNNER_HARDENING_2026-09-14.md sec 10.
#
# EXIT CODES
#   0 = nothing was owed (teardown marker present, or another backstop claimed it), OR this
#       watchdog's teardown ran and every step reported success.
#   2 = REFUSED at validation: bad arguments, missing run directory, no runner.launched, or a
#       runner.launched whose PID is not the one we were told to watch. NOTHING was touched.
#   3 = the teardown RAN and at least one step did not report success. Recorded, never fatal:
#       the remaining steps still run. Inspect before the next launch.
#   4 = -MaxSec expired with the runner STILL ALIVE. Nothing was torn down - a live run must
#       never be torn down by a timer.
#   5 = unexpected terminating error.
#
# ASCII only (Windows tooling decodes cp1252, not UTF-8). CRLF, like every other file here.
[CmdletBinding()]
param(
    # The PID to watch. The runner passes its own $PID; it is also the CONTENT of
    # <RunDir>\runner.launched, and a mismatch between the two is a refusal (below).
    # NOT [Parameter(Mandatory)]: a mandatory parameter PROMPTS, and this process is detached
    # with its stdin redirected from an empty file, so a prompt would be answered by EOF and
    # the refusal would arrive as a parameter-binding error instead of as an exit code with a
    # reason. Every argument is validated explicitly below instead.
    [int]    $RunnerPid  = 0,
    [string] $RunDir     = '',
    [string] $VrfProfile = '5.0.2',
    # StopIface has NO defaults by design (tools/StopIface/Program.cs) and neither do these:
    # a watchdog that guessed an endpoint could drive the OPERATOR'S server (8080/61613)
    # instead of the private test server. Both are required.
    [string] $RestUrl    = '',
    [string] $StompUrl   = '',
    # How long this watchdog lives at most. It must outlast the whole observation window plus
    # teardown; the runner computes it from the same budgets it uses for the observers.
    [int]    $MaxSec     = 7200,
    [int]    $PollSec    = 5,
    # TEST-ONLY. Both default OFF and both log LOUDLY when on, and RunC2SimScenario.ps1
    # passes NEITHER on any path. They exist because the two things finding F2 fixed are
    # otherwise unreachable offline: nothing can make Windows fail an OpenProcess on demand.
    #   -NoHandleCache      skip the handle cache, so liveness runs on the Get-Process +
    #                       StartTime FALLBACK - the path whose behaviour F2 was about.
    #   -TestFakeDeadReads  force the first N liveness observations to report GONE; that is
    #                       exactly the transient the two-observation rule must absorb.
    [switch] $NoHandleCache,
    [int]    $TestFakeDeadReads = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Logging: ONE LINE PER DECISION, UTC-stamped (our logs stamp UTC; the vendor's stamp LOCAL).
# Written to stdout (the launcher redirects it to <RunDir>\watchdog.stdout.log) AND appended
# to <RunDir>\runner-watchdog.log. The second copy is deliberate: this process outlives every
# other process of the run, and its evidence must be findable under a fixed name whoever
# started it and however they redirected it.
# ---------------------------------------------------------------------------
$script:LogFile = $null
function Log {
    param([string]$Level, [string]$Message)
    $line = ('{0} [{1}] {2}' -f (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ'), $Level, $Message)
    Write-Host $line
    if ($script:LogFile) { try { [System.IO.File]::AppendAllText($script:LogFile, $line + "`r`n") } catch { } }
}

try {

# ---------------------------------------------------------------------------
# ARGUMENTS FIRST, before anything is touched or opened (the LaunchVrf.ps1 "validated too
# late" defect, and the reason StopVrf52.ps1 validates in its first lines).
# ---------------------------------------------------------------------------
$bad = @()
if ($RunnerPid -le 0)  { $bad += ('-RunnerPid must be a positive process id (got {0}).' -f $RunnerPid) }
if (-not $RunDir)      { $bad += '-RunDir is required.' }
elseif (-not (Test-Path -LiteralPath $RunDir -PathType Container)) {
    $bad += ('-RunDir is not an existing directory: {0}' -f $RunDir)
}
if ($VrfProfile -ne '5.0.2' -and $VrfProfile -ne '5.2') {
    $bad += ("-VrfProfile must be '5.0.2' or '5.2' (got '{0}'). It selects StopVrf.ps1 vs StopVrf52.ps1." -f $VrfProfile)
}
if (-not $RestUrl)  { $bad += '-RestUrl is required (StopIface has no defaults, and a guessed endpoint could drive the operator''s own server).' }
if (-not $StompUrl) { $bad += '-StompUrl is required (same reason as -RestUrl).' }
if ($PollSec -lt 1 -or $PollSec -gt 60)     { $bad += ('-PollSec must be 1..60 (got {0}).' -f $PollSec) }
if ($MaxSec  -lt 30 -or $MaxSec  -gt 86400) { $bad += ('-MaxSec must be 30..86400 (got {0}).' -f $MaxSec) }
if ($TestFakeDeadReads -lt 0 -or $TestFakeDeadReads -gt 10) { $bad += ('-TestFakeDeadReads must be 0..10 (got {0}). It is a TEST switch and must be 0 on a real run.' -f $TestFakeDeadReads) }

if ($RunDir -and (Test-Path -LiteralPath $RunDir -PathType Container)) {
    $script:LogFile = Join-Path $RunDir 'runner-watchdog.log'
}
if ($bad.Count -gt 0) {
    foreach ($b in $bad) { Log 'FAIL' $b }
    Log 'FAIL' 'REFUSED at validation. NOTHING was touched.'
    exit 2
}

$PathLaunched    = Join-Path $RunDir 'runner.launched'
$PathTeardownRan = Join-Path $RunDir 'runner.teardown-ran'
$PathWatchdogRan = Join-Path $RunDir 'runner.watchdog-ran'
$PathStopFile    = Join-Path $RunDir 'observers.stop'

$RepoRoot     = Split-Path -Parent $PSScriptRoot
$ExeStopIface = Join-Path $RepoRoot 'tools\StopIface\bin\Release\net10.0\StopIface.exe'
$StopVrf      = Join-Path $PSScriptRoot $(if ($VrfProfile -eq '5.2') { 'StopVrf52.ps1' } else { 'StopVrf.ps1' })
# The host that is running THIS script - never a bare 'pwsh'. Bare pwsh on this machine
# resolves to the 32-BIT build (RUNBOOK 0.5.14 item 1); $PSHOME is whatever started us, and
# the runner starts us with its own 64-bit host.
$PwshExe      = Join-Path $PSHOME 'pwsh.exe'

Log 'INFO' '=== RunnerWatchdog.ps1 - detached teardown backstop (A2) ==='
Log 'INFO' ('  runner pid : {0}' -f $RunnerPid)
Log 'INFO' ('  run dir    : {0}' -f $RunDir)
Log 'INFO' ('  profile    : {0}  -> {1}' -f $VrfProfile, $StopVrf)
Log 'INFO' ('  endpoints  : rest={0} stomp={1}' -f $RestUrl, $StompUrl)
Log 'INFO' ('  budget     : poll {0}s, max {1}s ({2:N1} min)' -f $PollSec, $MaxSec, ($MaxSec / 60.0))
Log 'INFO' ('  this pid   : {0}   host: {1} (64-bit: {2})' -f $PID, $PSHOME, [Environment]::Is64BitProcess)
Log 'INFO' '  NOTHING is force-killed on any path; rtiexec / rtiForwarder / rtiAssistant are never touched.'
Log 'INFO' '  a death is acted on only after TWO observations 2 s apart (review of 374ea49, finding F2).'
if ($NoHandleCache -or $TestFakeDeadReads -gt 0) {
    Log 'WARN' ('*** TEST SWITCHES ARE ON: -NoHandleCache={0} -TestFakeDeadReads={1}. This is NOT a production posture; the runner never passes them. ***' -f [bool]$NoHandleCache, $TestFakeDeadReads)
}

# ---------------------------------------------------------------------------
# THE FOREIGN-SESSION GUARD. Absence of runner.launched means the run launched nothing (the
# runner has validation aborts AFTER the run directory exists and BEFORE anything is launched,
# one of which fires precisely when VR-Forces is ALREADY UP and belongs to someone else).
# Acting on that absence would invert the project's single most important rule. The watchdog
# is started at Stage 6b-w, which is AFTER the marker is written at Stage 3, so at this point
# the marker MUST already be there.
# ---------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $PathLaunched -PathType Leaf)) {
    Log 'FAIL' ('REFUSED: no runner.launched in {0}. This run launched nothing, so what is up may be a FOREIGN live session (RUNBOOK sec 0). NOTHING was touched.' -f $RunDir)
    exit 2
}
$markerPid = $null
try {
    $raw = Get-Content -LiteralPath $PathLaunched -Raw -ErrorAction Stop
    if ($null -ne $raw) { $markerPid = $raw.Trim() }
} catch { }
if ($markerPid -and ($markerPid -match '^\d+$')) {
    if ([int]$markerPid -ne $RunnerPid) {
        Log 'FAIL' ('REFUSED: runner.launched holds pid {0} but -RunnerPid is {1}. This run directory belongs to a DIFFERENT runner; tearing down on it could stop a live session. NOTHING was touched.' -f $markerPid, $RunnerPid)
        exit 2
    }
    Log 'OK' ('runner.launched present and its pid matches (-RunnerPid {0}). This run owns what it launched.' -f $RunnerPid)
} else {
    Log 'WARN' ('runner.launched is present but holds no readable pid (got "{0}"). Proceeding on the marker''s PRESENCE, which is the contract; the pid is a convenience for operators.' -f $markerPid)
}

# ---------------------------------------------------------------------------
# Hold a HANDLE on the runner. Windows does not recycle a PID while a handle to it is open,
# so this removes PID reuse from the liveness test entirely. If the handle cannot be taken
# (already exited, or an access failure) the loop falls back to Get-Process + StartTime,
# which is a weaker but explicit reuse guard.
#
# FINDING F2 (review of 374ea49). That fallback is only REAL if the cached-process object is
# DROPPED. .NET's Process.HasExited re-opens the process on every call when no handle is
# cached and treats ANY failure to open it as 'exited' - and the flag is STICKY. Keeping the
# object after a failed cache would therefore convert one transient OpenProcess failure into
# a permanent 'the runner is dead', i.e. the teardown of a HEALTHY run - the one thing this
# design promises never to do.
# ---------------------------------------------------------------------------
$runnerProc  = $null
$runnerStart = $null
try {
    $runnerProc = Get-Process -Id $RunnerPid -ErrorAction Stop
    try { $runnerStart = $runnerProc.StartTime } catch { }
    if ($NoHandleCache) {
        $runnerProc = $null
        Log 'WARN' ('-NoHandleCache (TEST): the handle cache is SKIPPED, so liveness runs on the Get-Process + StartTime FALLBACK for pid {0}.' -f $RunnerPid)
    } else {
        try { $null = $runnerProc.Handle }
        catch {
            $runnerProc = $null
            Log 'WARN' ('could not cache a handle on pid {0} ({1}). The cached-process object is DROPPED (finding F2) and liveness falls back to Get-Process + StartTime - a weaker PID-reuse guard, but one that cannot latch a transient failure into a permanent death.' -f $RunnerPid, $_.Exception.Message)
        }
    }
    Log 'OK' ('watching runner pid {0} (started {1}; liveness via {2})' -f $RunnerPid, $(if ($runnerStart) { $runnerStart.ToString('yyyy-MM-dd HH:mm:ss') } else { 'unknown' }), $(if ($null -ne $runnerProc) { 'a cached handle' } else { 'Get-Process + StartTime' }))
} catch {
    # Get-Process ITSELF failed. That is usually 'the process is gone' - but it is not proof,
    # and logging it as 'ALREADY GONE' when the runner is alive is a false statement in the
    # one log that outlives the run (finding F2). Ask a SECOND, independent source, and take
    # the StartTime from it when it answers so the PID-reuse guard survives.
    $getProcErr  = $_.Exception.Message
    $runnerProc  = $null
    $cimProc     = $null
    try { $cimProc = Get-CimInstance -ClassName Win32_Process -Filter ('ProcessId = {0}' -f $RunnerPid) -ErrorAction Stop } catch { $cimProc = $null }
    if ($null -ne $cimProc) {
        try { $runnerStart = [datetime]$cimProc.CreationDate } catch { $runnerStart = $null }
        Log 'WARN' ('Get-Process FAILED for pid {0} ({1}) but Win32_Process says the process EXISTS (started {2}). The runner is ALIVE - NOT gone. Polling continues on the Get-Process + StartTime fallback.' -f $RunnerPid, $getProcErr, $(if ($runnerStart) { $runnerStart.ToString('yyyy-MM-dd HH:mm:ss') } else { 'unknown' }))
    } else {
        Log 'WARN' ('runner pid {0} is ALREADY GONE at watchdog start - Get-Process ({1}) and Win32_Process BOTH say so. Going straight to the teardown decision.' -f $RunnerPid, $getProcErr)
    }
}

# How many liveness observations are still to be FORCED to 'gone' (TEST ONLY, 0 in a run).
$script:FakeDeadLeft = $TestFakeDeadReads

function Test-RunnerAlive {
    param($Proc, [int]$ProcessId, $StartTime)
    if ($script:FakeDeadLeft -gt 0) {
        $script:FakeDeadLeft = $script:FakeDeadLeft - 1
        Log 'WARN' ('-TestFakeDeadReads (TEST): this observation of pid {0} is FORCED to report GONE ({1} forced read(s) left).' -f $ProcessId, $script:FakeDeadLeft)
        return $false
    }
    if ($null -ne $Proc) {
        try { return (-not $Proc.HasExited) } catch { }
    }
    $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($null -eq $p) { return $false }
    if ($null -ne $StartTime) {
        # A different start time means the PID was RECYCLED: our runner is gone and this is
        # someone else's process wearing its number.
        try { if ($p.StartTime -ne $StartTime) { return $false } } catch { }
    }
    return $true
}

# ---------------------------------------------------------------------------
# THE POLL LOOP. It does exactly one thing - wait for the runner to stop existing. While the
# runner is alive the watchdog reads no markers and touches nothing, so a long, healthy run is
# never at risk from it.
# ---------------------------------------------------------------------------
# TWO CONSECUTIVE OBSERVATIONS, 2 s apart, are required before the loop is left (finding F2).
# The death decision used to be made on ONE reading, and every way a LIVE runner can read as
# dead - a transient Get-Process failure, a momentarily unopenable process - then cost a
# healthy run its teardown. A confirmation costs a dead runner 2 s and costs a live one
# nothing at all.
$deadline = (Get-Date).AddSeconds($MaxSec)
$alive    = $true
while ((Get-Date) -lt $deadline) {
    $alive = Test-RunnerAlive -Proc $runnerProc -ProcessId $RunnerPid -StartTime $runnerStart
    if (-not $alive) {
        Log 'INFO' ('pid {0} read as GONE. CONFIRMING in 2 s - one observation is never enough to tear down a run.' -f $RunnerPid)
        Start-Sleep -Seconds 2
        $alive = Test-RunnerAlive -Proc $runnerProc -ProcessId $RunnerPid -StartTime $runnerStart
        if (-not $alive) {
            Log 'INFO' ('pid {0} read as GONE TWICE, 2 s apart. Treating the runner as dead.' -f $RunnerPid)
            break
        }
        Log 'WARN' ('pid {0} read as GONE once and ALIVE 2 s later: the first read was TRANSIENT and NOTHING was touched. Continuing to poll.' -f $RunnerPid)
    }
    Start-Sleep -Seconds $PollSec
}
if ($alive) {
    Log 'FAIL' ('-MaxSec ({0}s) EXPIRED and runner pid {1} is STILL ALIVE. NOTHING was torn down - a live run must never be torn down by a timer. The wrapper backstop (scripts\RunScenario.sh) still covers this runner if its shell survives.' -f $MaxSec, $RunnerPid)
    exit 4
}
$exitCodeText = 'unknown'
if ($null -ne $runnerProc) { try { $exitCodeText = [string]$runnerProc.ExitCode } catch { } }
Log 'INFO' ('runner pid {0} IS GONE (exit code as seen from here: {1}). A raw -1 here, or 127 from bash, means TerminateProcess - RUNBOOK 0.5.14 item 4.' -f $RunnerPid, $exitCodeText)

# The runner writes runner.teardown-ran as the LAST statement of its finally, immediately
# before `exit`, so if the process is gone the write has already happened. This short settle
# is insurance against filesystem visibility lag only, and it costs a normal run nothing.
Start-Sleep -Seconds 2

# ---------------------------------------------------------------------------
# THE DECISION. One line per branch.
# ---------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $PathLaunched -PathType Leaf)) {
    Log 'FAIL' 'REFUSED: runner.launched has DISAPPEARED since startup. Refusing to tear down on an ambiguous run directory. NOTHING was touched.'
    exit 2
}
if (Test-Path -LiteralPath $PathTeardownRan -PathType Leaf) {
    Log 'OK' 'runner.teardown-ran is present: the runner completed its own teardown. Standing down without touching anything.'
    exit 0
}
if (Test-Path -LiteralPath $PathWatchdogRan -PathType Leaf) {
    Log 'OK' 'runner.watchdog-ran is already present: another backstop claimed the teardown. Standing down without touching anything.'
    exit 0
}

# CLAIM the teardown ATOMICALLY. CreateNew fails if the file exists, so two watchdogs racing
# on the same run directory can never both act - the loser stands down here.
try {
    $fs = [System.IO.File]::Open($PathWatchdogRan, [System.IO.FileMode]::CreateNew,
                                 [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try {
        $bytes = [System.Text.Encoding]::ASCII.GetBytes(
            ('claimed by RunnerWatchdog.ps1 pid {0} at {1} for runner pid {2}{3}' -f `
                $PID, (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ'), $RunnerPid, "`r`n"))
        $fs.Write($bytes, 0, $bytes.Length)
    } finally { $fs.Dispose() }
} catch [System.IO.IOException] {
    Log 'OK' ('runner.watchdog-ran was claimed by someone else in the same instant ({0}). Standing down.' -f $_.Exception.Message)
    exit 0
}
Log 'WARN' '*** THE RUNNER DIED WITHOUT A COMPLETED TEARDOWN. Tearing down from the watchdog. ***'
Log 'INFO' ('claimed {0}' -f $PathWatchdogRan)

function Invoke-Step {
    param(
        [string]$Name, [string]$File, [string[]]$Arguments, [string]$Cwd,
        [string]$OutFile, [string]$ErrFile, [int]$TimeoutSec
    )
    # Bounded, and NEVER kills the child on expiry: a teardown tool still working is not
    # something to terminate. An overrun returns $null, which the caller treats as a failed
    # step, and the remaining steps still run.
    Log 'INFO' ('{0}: {1} {2}' -f $Name, $File, ($Arguments -join ' '))
    if (-not (Test-Path -LiteralPath $File -PathType Leaf)) {
        Log 'FAIL' ('{0}: NOT FOUND at {1}. This step did not run.' -f $Name, $File)
        return $null
    }
    try {
        $sp = @{ FilePath = $File; WorkingDirectory = $Cwd; PassThru = $true; NoNewWindow = $true;
                 RedirectStandardOutput = $OutFile; RedirectStandardError = $ErrFile }
        if ($Arguments.Count -gt 0) { $sp.ArgumentList = $Arguments }
        $p = Start-Process @sp
        try { $null = $p.Handle } catch { }
        if (-not $p.WaitForExit($TimeoutSec * 1000)) {
            Log 'FAIL' ('{0}: did NOT exit within {1}s and was LEFT RUNNING (never killed). Its output is in {2}.' -f $Name, $TimeoutSec, $OutFile)
            return $null
        }
        return $p.ExitCode
    } catch {
        Log 'FAIL' ('{0}: could not be started ({1}).' -f $Name, $_.Exception.Message)
        return $null
    }
}

$stepsOk = $true

# 1. StopIface - drive the C2SIM server to UNINITIALIZED so the interface RESIGNS from the
#    RTI. This is the ONLY correct way to stop the interface (RUNBOOK sec 4).
$code = Invoke-Step -Name 'StopIface' -File $ExeStopIface -Arguments @($RestUrl, $StompUrl, '--yes') `
            -Cwd $RepoRoot -OutFile (Join-Path $RunDir 'watchdog-stopiface.stdout.log') `
            -ErrFile (Join-Path $RunDir 'watchdog-stopiface.stderr.log') -TimeoutSec 180
if ($code -eq 0) {
    Log 'OK' 'StopIface exit 0: server driven to UNINITIALIZED; the interface should resign.'
} else {
    $stepsOk = $false
    Log 'FAIL' ('StopIface exit {0} (0 ok; 1 the server did NOT reach UNINITIALIZED - the interface MAY STILL BE JOINED; 2 usage; blank = it never produced a code). Nothing was force-killed. INSPECT BEFORE THE NEXT RUN.' -f $code)
}

# 2. StopVrf - graceful, nothing killed, RTI infrastructure preserved.
$code = Invoke-Step -Name 'StopVrf' -File $PwshExe `
            -Arguments @('-NoProfile','-ExecutionPolicy','Bypass','-File', $StopVrf, '-TimeoutSec','120') `
            -Cwd $RepoRoot -OutFile (Join-Path $RunDir 'watchdog-stopvrf.stdout.log') `
            -ErrFile (Join-Path $RunDir 'watchdog-stopvrf.stderr.log') -TimeoutSec 300
if ($code -eq 0) {
    Log 'OK' 'StopVrf exit 0: VR-Forces is down or was already down (graceful; RTI infrastructure preserved).'
} else {
    $stepsOk = $false
    Log 'FAIL' ('StopVrf exit {0} (0 down/already down; 2 bad args; 3 STILL RUNNING - NOTHING killed; 5 unexpected; blank = it never produced a code). A leftover instance HARD-BLOCKS the next launch.' -f $code)
}

# 3. Tell the observers to stop. They poll for this file once a second and take their normal
#    clean resign / disconnect path - the same one their duration expiry takes. Nothing is
#    signalled, closed or killed; a file appears and the tool reads it. Without it they run to
#    their own duration cap, which is the designed fallback and costs only trace.
try {
    [System.IO.File]::WriteAllText($PathStopFile,
        ('stop requested by RunnerWatchdog.ps1 pid {0} at {1} (runner pid {2} died without a completed teardown){3}' -f `
            $PID, (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ'), $RunnerPid, "`r`n"))
    Log 'OK' ('observers.stop touched ({0}) - WatchVrf and ListenReports resign within ~1 s.' -f $PathStopFile)
} catch {
    $stepsOk = $false
    Log 'FAIL' ('could not create {0}: {1}. The observers will run to their own duration cap instead.' -f $PathStopFile, $_.Exception.Message)
}

# 4. Closing inventory. REPORT ONLY - this script stops nothing here.
$ours = @('vrfSimHLA1516e','vrfGui','vrfLauncher','VrfC2SimApp','WatchVrf','ListenReports')
$left = @()
foreach ($n in $ours) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) { $left += ('{0}(pid {1})' -f $p.ProcessName, $p.Id) }
}
if ($left.Count -gt 0) {
    Log 'WARN' ('STILL RUNNING after the watchdog teardown: {0}. NOT killed. Observers end on their own cap; anything else needs inspection before the next launch.' -f ($left -join ', '))
} else {
    Log 'OK' 'nothing of ours is left running.'
}
$rti = @()
foreach ($n in @('rtiexec','rtiForwarder','rtiAssistant')) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) { $rti += ('{0}(pid {1})' -f $p.ProcessName, $p.Id) }
}
if ($rti.Count -gt 0) { Log 'OK' ('RTI infrastructure PRESERVED (correct): {0}' -f ($rti -join ', ')) }
else { Log 'INFO' 'no RTI infrastructure process is running (none was touched by this script).' }

if ($stepsOk) {
    Log 'OK' 'watchdog teardown COMPLETE - every step reported success.'
    exit 0
}
Log 'FAIL' 'watchdog teardown RAN but at least one step did not report success (see above). Exit 3.'
exit 3

} catch {
    Log 'FAIL' ('unexpected terminating error: {0}' -f $_.Exception.Message)
    Log 'FAIL' ('at: {0}' -f $_.InvocationInfo.PositionMessage)
    exit 5
}
