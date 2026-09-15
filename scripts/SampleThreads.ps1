<#
.SYNOPSIS
  Per-thread CPU sampler for ONE process (2026-09-13, PREREG_NAVDATA_G3: "which thread is hot when the
  sim engine's sim/wall ratio collapses?"). Also carries a WORKING-SET RUNAWAY TRIPWIRE (RUNBOOK 0.5.11
  item 17, 2026-09-15): back-end runs (thread-samples.csv) showed wsMB growing 1-2.5 GB/min at LOW cpu
  after a particular order, versus ~4000 MB flat on healthy runs.

.DESCRIPTION
  Every -IntervalSec seconds writes ONE CSV row for the first process named -ProcessName:
    tUtc, tSec, pid, procCpuCores, wsMB, threads, then top-N threads as tid:cores pairs
      procCpuCores = delta(TotalProcessorTime) / interval  (1.0 = one core flat out; the process may
                     exceed 1.0 when several threads are busy)
      tid:cores    = per-thread delta(TotalProcessorTime) / interval from Get-Process().Threads,
                     sorted descending, top -TopThreads (default 8); a thread id is stable for the
                     thread's life, so the same tid recurring at the top across rows is the "hot thread".
  Reads only; never touches the process. Stops when -StopFile exists, when -MaxSec elapses, or when the
  process exits (writes a final "process gone" row). Companion of SampleCpu.ps1 (machine + per-process).

  WORKING-SET RUNAWAY TRIPWIRE: after each row, the ws slope over the trailing -WsSlopeWindowSamples
  samples (default 6, ~30 s at the default 5 s interval) is compared against -WsSlopeMBPerMinAlert
  (default 500 MB/min). A slope-only rule false-alarms on the ordinary startup ramp every run has (the
  process image loading, plus - on some runs - a CPU-busy scenario/terrain load spike to ~4 GB):
  measured on runs/launch52/RunScenario-20260915T133258Z.threads.csv, that legitimate ramp hits
  1000-2000 MB/min for a moment while procCpuCores is 1.7-4.1 (busy). The confirmed runaway
  (RunScenario-20260915T130626Z.threads.csv) grows at 500-900+ MB/min while procCpuCores stays under 1
  (idle) - the CONTEXT note's own "at low CPU" observation. So the tripwire also requires the average
  procCpuCores over the window to be at or below -WsSlopeMaxAvgCpuCores (default 1.0), and ignores the
  first -WsSlopeWarmupSec seconds (default 60) of any run (the universal process-image-load ramp, which
  is over well before 60 s on every run measured so far). When slope-over-threshold AND low-cpu holds
  for 3 consecutive evaluations, ONE line is appended to the sidecar "<OutFile base>.alerts.txt" (e.g.
  thread-samples.csv -> thread-samples.alerts.txt) and printed to stdout; sampling continues - nothing
  is ever killed from here.

  RE-ARMABLE WARM-UP (2026-09-15, V6f harvest defect 2): -WsSlopeWarmupSec above is relative to THIS
  PROCESS'S OWN START, which protects the universal load ramp but nothing else - the V6f run
  (runs/20260915T160411Z_run) put a legitimate object-creation-and-compose burst at t+175s, long past
  the default 60s warmup, and the tripwire alerted on it (16:07:20Z) nine seconds after the order
  reached the bus (16:07:10.545Z). -WarmupResetAtUtc <UTC datetime> tells the tripwire to treat THAT
  instant as a second t=0 for the warmup test: a sample timestamped at or after the reset restarts the
  -WsSlopeWarmupSec countdown from the reset instant instead of from process start; a sample before the
  reset (or when no reset is given) is judged exactly as before. This is armed FOR REAL from the first
  poll where it can be parsed - a value already known at start-up (typically -ReplayCsv, where the
  dispatch instant is already on record) applies immediately, and via -WarmupResetFile <path> the LIVE
  loop is a running process that cannot be handed a new command-line argument once started: it re-checks
  that file (like -StopFile) once per sample until it exists and parses, then latches the value for the
  rest of the run - the runner writes ONE line (the UTC dispatch instant, any format
  [datetime]::Parse(..., RoundtripKind) accepts) to that path the moment the order reaches the bus. Only
  the FIRST successful read is used; a later rewrite of the file is ignored (armed once, at dispatch, by
  design). Neither option changes -WsSlopeWarmupSec's own default or its process-start behaviour when
  no reset is ever supplied - a run that never sets one is byte-for-byte the pre-2026-09-15 tripwire.

  -ReplayCsv <path> replays a previously captured thread-samples.csv through the SAME tripwire function
  (Add-WsSlopeSample) offline, for proof/regression use - no process, no OutFile, no live sampling.
  -WarmupResetAtUtc and -WarmupResetFile both work under -ReplayCsv too (the file is read ONCE, before
  the first row, since there is no live wait to poll across).

.EXAMPLE
  scripts\SampleThreads.ps1 -ProcessName vrfSimHLA1516e -MaxSec 1900 -IntervalSec 5 -OutFile x\threads.csv

.EXAMPLE
  scripts\SampleThreads.ps1 -ReplayCsv runs\launch52\RunScenario-20260915T130626Z.threads.csv

.EXAMPLE
  scripts\SampleThreads.ps1 -ReplayCsv runs\20260915T160411Z_run\thread-samples.csv -WarmupResetAtUtc '2026-09-15T16:07:10.545Z'
#>
param(
    [string] $ProcessName,
    [string] $OutFile,
    [int]    $IntervalSec = 5,
    [int]    $MaxSec = 3600,
    [int]    $TopThreads = 8,
    [int]    $WaitForProcessSec = 600,
    [string] $StopFile = '',
    [double] $WsSlopeMBPerMinAlert = 500,
    [int]    $WsSlopeWindowSamples = 6,
    [double] $WsSlopeWarmupSec = 60,
    [double] $WsSlopeMaxAvgCpuCores = 1.0,
    [string] $WarmupResetAtUtc = '',
    [string] $WarmupResetFile = '',
    [string] $ReplayCsv = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---- WS runaway tripwire: one set of functions, shared by the live loop and -ReplayCsv ----
function Get-WsSlopeAlertLine {
    param(
        [Parameter(Mandatory = $true)] [string] $TUtcText,
        [Parameter(Mandatory = $true)] [double] $SlopeMBPerMin,
        [Parameter(Mandatory = $true)] [double] $WindowSec,
        [Parameter(Mandatory = $true)] [double] $WsNowMB,
        [Parameter(Mandatory = $true)] $ProcId
    )
    return ('{0} BACK-END WS RUNAWAY: {1:F1} MB/min over {2:F1} s, ws now {3:F0} MB, pid {4}' -f
        $TUtcText, $SlopeMBPerMin, $WindowSec, $WsNowMB, $ProcId)
}

# Parses a UTC datetime out of -WarmupResetAtUtc / a -WarmupResetFile's content. Returns $null on
# anything unparseable or blank - a bad or not-yet-written reset value must never throw and must never
# be mistaken for "warm-up re-armed"; it is simply treated as "no reset yet".
function ConvertTo-WsResetUtc {
    param([AllowNull()][AllowEmptyString()][string] $Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
    try {
        return ([datetime]::Parse($Text.Trim(), [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)).ToUniversalTime()
    } catch {
        return $null
    }
}

function New-WsSlopeTracker {
    param(
        [int]    $WindowSamples,
        [double] $SlopeMBPerMinAlert,
        [double] $WarmupSec,
        [double] $MaxAvgCpuCores,
        # RE-ARMABLE WARM-UP (see the .DESCRIPTION block above). $null = the historical
        # process-start-relative warm-up, unchanged. Set directly at construction (a value already
        # known, e.g. -ReplayCsv) or later by mutating the returned object's .ResetAtUtc property (the
        # live loop's -WarmupResetFile poll) - a pscustomobject property is a plain mutable NoteProperty.
        $ResetAtUtc = $null
    )
    [pscustomobject]@{
        WindowSamples      = $WindowSamples
        SlopeMBPerMinAlert = $SlopeMBPerMinAlert
        WarmupSec          = $WarmupSec
        MaxAvgCpuCores     = $MaxAvgCpuCores
        ResetAtUtc         = $ResetAtUtc
        Buffer             = New-Object System.Collections.Generic.List[object]
        Consecutive        = 0
        Alerted            = $false
    }
}

# Feeds ONE sample into $Tracker. Returns the alert line the instant a sustained runaway is
# confirmed (slope over threshold AND avg cpu at or under the idle gate, both true for 3
# consecutive evaluations, past the warmup), else $null. Never mutates anything outside
# $Tracker - safe to call once per live sample or once per replayed CSV row (this IS the
# code the offline -ReplayCsv proof exercises, not a re-implementation of it).
function Add-WsSlopeSample {
    param(
        [Parameter(Mandatory = $true)] $Tracker,
        [Parameter(Mandatory = $true)] [string]   $TUtcText,
        [Parameter(Mandatory = $true)] [datetime] $TUtc,
        [Parameter(Mandatory = $true)] [double]   $TSec,
        [Parameter(Mandatory = $true)] [double]   $WsMB,
        [Parameter(Mandatory = $true)] [double]   $CpuCores,
        [Parameter(Mandatory = $true)] $ProcId
    )
    $Tracker.Buffer.Add([pscustomobject]@{
        TUtcText = $TUtcText; TUtc = $TUtc; TSec = $TSec; WsMB = $WsMB; CpuCores = $CpuCores
    })
    $capacity = $Tracker.WindowSamples + 1
    while ($Tracker.Buffer.Count -gt $capacity) { $Tracker.Buffer.RemoveAt(0) }
    if ($Tracker.Buffer.Count -lt $capacity) {
        $Tracker.Consecutive = 0
        $Tracker.Alerted = $false
        return $null
    }
    $oldest = $Tracker.Buffer[0]
    $newest = $Tracker.Buffer[$Tracker.Buffer.Count - 1]
    $spanSec = ($newest.TUtc - $oldest.TUtc).TotalSeconds
    $slope = 0.0
    if ($spanSec -gt 0) { $slope = (($newest.WsMB - $oldest.WsMB) / $spanSec) * 60.0 }
    $avgCpu = ($Tracker.Buffer | Measure-Object -Property CpuCores -Average).Average
    # Re-armable warm-up: once a reset instant is set AND this sample is at or after it, the warm-up
    # clock is elapsed-since-reset, not elapsed-since-process-start - see New-WsSlopeTracker/.DESCRIPTION.
    # A sample still BEFORE the reset (or a run with no reset at all) uses the original TSec test,
    # unchanged.
    $warmupElapsedSec = $newest.TSec
    if ($null -ne $Tracker.ResetAtUtc -and $newest.TUtc -ge $Tracker.ResetAtUtc) {
        $warmupElapsedSec = ($newest.TUtc - $Tracker.ResetAtUtc).TotalSeconds
    }
    $isOver = ($warmupElapsedSec -ge $Tracker.WarmupSec) -and
              ($slope -ge $Tracker.SlopeMBPerMinAlert) -and
              ($avgCpu -le $Tracker.MaxAvgCpuCores)
    if ($isOver) {
        $Tracker.Consecutive++
    } else {
        $Tracker.Consecutive = 0
        $Tracker.Alerted = $false
    }
    if ($Tracker.Consecutive -ge 3 -and -not $Tracker.Alerted) {
        $Tracker.Alerted = $true
        return Get-WsSlopeAlertLine -TUtcText $newest.TUtcText -SlopeMBPerMin $slope -WindowSec $spanSec -WsNowMB $newest.WsMB -ProcId $ProcId
    }
    return $null
}

function Get-WsSlopeAlertsPath {
    param([Parameter(Mandatory = $true)] [string] $CsvPath)
    if ($CsvPath -match '\.csv$') { return ($CsvPath -replace '\.csv$', '.alerts.txt') }
    return "$CsvPath.alerts.txt"
}

# ---- -ReplayCsv: replay a captured CSV through the SAME tripwire, offline, no process -----
if ($ReplayCsv) {
    if (-not (Test-Path -LiteralPath $ReplayCsv)) { throw "SampleThreads -ReplayCsv: file not found: $ReplayCsv" }
    # RE-ARMABLE WARM-UP, resolved ONCE before the first row: -WarmupResetAtUtc wins if given; else
    # -WarmupResetFile is read a single time (no live wait to poll across in a replay). Either way this
    # is the SAME ConvertTo-WsResetUtc/-ResetAtUtc plumbing the live loop uses below.
    $replayResetAtUtc = ConvertTo-WsResetUtc -Text $WarmupResetAtUtc
    if ($null -eq $replayResetAtUtc -and $WarmupResetFile -and (Test-Path -LiteralPath $WarmupResetFile)) {
        $replayResetAtUtc = ConvertTo-WsResetUtc -Text (Get-Content -LiteralPath $WarmupResetFile -Raw -ErrorAction SilentlyContinue)
    }
    $tracker = New-WsSlopeTracker -WindowSamples $WsSlopeWindowSamples -SlopeMBPerMinAlert $WsSlopeMBPerMinAlert -WarmupSec $WsSlopeWarmupSec -MaxAvgCpuCores $WsSlopeMaxAvgCpuCores -ResetAtUtc $replayResetAtUtc
    if ($replayResetAtUtc) { "ReplayCsv: warm-up RE-ARMED at $($replayResetAtUtc.ToString('o'))" }
    $rows = @(Import-Csv -LiteralPath $ReplayCsv)
    $rowsUsed = 0
    $alerts = @()
    foreach ($row in $rows) {
        if (-not $row.tUtc -or $row.tUtc -eq '') { continue }
        if ($null -eq $row.wsMB -or $row.wsMB -eq '' -or $null -eq $row.procCpuCores -or $row.procCpuCores -eq '') { continue }
        $tUtcParsed = [datetime]::Parse($row.tUtc, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
        $rowsUsed++
        $alertLine = Add-WsSlopeSample -Tracker $tracker -TUtcText $row.tUtc -TUtc $tUtcParsed -TSec ([double]$row.tSec) -WsMB ([double]$row.wsMB) -CpuCores ([double]$row.procCpuCores) -ProcId $row.pid
        if ($alertLine) { $alerts += $alertLine; $alertLine }
    }
    if ($alerts.Count -eq 0) {
        "ReplayCsv: 0 alerts over $rowsUsed data rows ($ReplayCsv)"
    } else {
        "ReplayCsv: $($alerts.Count) alert(s) over $rowsUsed data rows ($ReplayCsv)"
    }
    exit 0
}

# ---- live sampling (unchanged apart from the tripwire hook) --------------------------------
if (-not $ProcessName) { throw 'SampleThreads: -ProcessName is required unless -ReplayCsv is given.' }
if (-not $OutFile)     { throw 'SampleThreads: -OutFile is required unless -ReplayCsv is given.' }

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
$alertsFile = Get-WsSlopeAlertsPath -CsvPath $OutFile
$cols = @('tUtc', 'tSec', 'pid', 'procCpuCores', 'wsMB', 'threads')
for ($i = 1; $i -le $TopThreads; $i++) { $cols += "top$i" }
Set-Content -Path $OutFile -Value ($cols -join ',') -Encoding ascii

# Wait for the process to appear (the sampler is started before or right after the sim launch).
$t0 = [DateTime]::UtcNow
$proc = $null
while (-not $proc) {
    $proc = @(Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Sort-Object StartTime | Select-Object -First 1)
    if ($proc.Count -gt 0) { $proc = $proc[0]; break } else { $proc = $null }
    if (([DateTime]::UtcNow - $t0).TotalSeconds -gt $WaitForProcessSec) {
        Add-Content -Path $OutFile -Value ('{0},{1:F1},,,,,process never appeared' -f ([DateTime]::UtcNow.ToString('o')), ([DateTime]::UtcNow - $t0).TotalSeconds) -Encoding ascii
        exit 2
    }
    Start-Sleep -Seconds 2
}
"SampleThreads: pid $($proc.Id) ($ProcessName) interval=${IntervalSec}s max=${MaxSec}s top=$TopThreads out=$OutFile"
"SampleThreads: WS runaway tripwire armed - slope>=${WsSlopeMBPerMinAlert} MB/min AND avgCpu<=${WsSlopeMaxAvgCpuCores} cores over ${WsSlopeWindowSamples} samples, 3x consecutive, after ${WsSlopeWarmupSec}s warmup; alerts -> $alertsFile"

$prevProc = $proc.TotalProcessorTime
$prevThreads = @{}
foreach ($th in $proc.Threads) { $prevThreads[$th.Id] = $th.TotalProcessorTime }
$prevWall = [DateTime]::UtcNow
$tStart = $prevWall
# RE-ARMABLE WARM-UP: -WarmupResetAtUtc, if already known at start-up, applies from sample 1. Otherwise
# -WarmupResetFile is polled once per sample below (like -StopFile) until it exists and parses, because
# a live process cannot be handed a new command-line argument after it has started - see .DESCRIPTION.
$wsResetAtUtc = ConvertTo-WsResetUtc -Text $WarmupResetAtUtc
$wsTracker = New-WsSlopeTracker -WindowSamples $WsSlopeWindowSamples -SlopeMBPerMinAlert $WsSlopeMBPerMinAlert -WarmupSec $WsSlopeWarmupSec -MaxAvgCpuCores $WsSlopeMaxAvgCpuCores -ResetAtUtc $wsResetAtUtc
if ($wsResetAtUtc) { "SampleThreads: WS warm-up RE-ARMED at $($wsResetAtUtc.ToString('o')) (from -WarmupResetAtUtc)" }
elseif ($WarmupResetFile) { "SampleThreads: WS warm-up will RE-ARM from $WarmupResetFile once it exists and parses" }

while ($true) {
    Start-Sleep -Seconds $IntervalSec
    if ($StopFile -and (Test-Path $StopFile)) { break }
    if ($WarmupResetFile -and -not $wsTracker.ResetAtUtc -and (Test-Path -LiteralPath $WarmupResetFile)) {
        $wsFileReset = ConvertTo-WsResetUtc -Text (Get-Content -LiteralPath $WarmupResetFile -Raw -ErrorAction SilentlyContinue)
        if ($wsFileReset) {
            $wsTracker.ResetAtUtc = $wsFileReset
            "SampleThreads: WS warm-up RE-ARMED at $($wsFileReset.ToString('o')) (from $WarmupResetFile)"
        }
    }
    $now = [DateTime]::UtcNow
    $tSec = ($now - $tStart).TotalSeconds
    if ($tSec -gt $MaxSec) { break }
    $p = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
    if (-not $p -or $p.HasExited) {
        Add-Content -Path $OutFile -Value ('{0},{1:F1},{2},,,,process gone' -f $now.ToString('o'), $tSec, $proc.Id) -Encoding ascii
        break
    }
    $dt = ($now - $prevWall).TotalSeconds
    if ($dt -le 0) { continue }
    $cores = ($p.TotalProcessorTime - $prevProc).TotalSeconds / $dt
    $prevProc = $p.TotalProcessorTime
    $rows = @()
    $cur = @{}
    foreach ($th in $p.Threads) {
        $cur[$th.Id] = $th.TotalProcessorTime
        if ($prevThreads.ContainsKey($th.Id)) {
            $c = ($th.TotalProcessorTime - $prevThreads[$th.Id]).TotalSeconds / $dt
            if ($c -gt 0.005) { $rows += [pscustomobject]@{ tid = $th.Id; cores = $c } }
        }
    }
    $prevThreads = $cur
    $prevWall = $now
    $top = @($rows | Sort-Object cores -Descending | Select-Object -First $TopThreads | ForEach-Object { '{0}:{1:F2}' -f $_.tid, $_.cores })
    while ($top.Count -lt $TopThreads) { $top += '' }
    $wsMB = $p.WorkingSet64 / 1MB
    $line = @($now.ToString('o'), ('{0:F1}' -f $tSec), $p.Id, ('{0:F2}' -f $cores), ('{0:F0}' -f $wsMB), $p.Threads.Count) + $top
    Add-Content -Path $OutFile -Value ($line -join ',') -Encoding ascii
    $alertLine = Add-WsSlopeSample -Tracker $wsTracker -TUtcText $now.ToString('o') -TUtc $now -TSec $tSec -WsMB $wsMB -CpuCores $cores -ProcId $p.Id
    if ($alertLine) {
        Add-Content -Path $alertsFile -Value $alertLine -Encoding ascii
        $alertLine
    }
}
"SampleThreads: done ($OutFile)"

