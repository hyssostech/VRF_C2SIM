<#
.SYNOPSIS
  Machine + per-process CPU/memory sampler for a VR-Forces run (2026-09-06, user request:
  "Observe the cpu load so we can understand if this particular machine is being overwhelmed").

.DESCRIPTION
  Every -IntervalSec seconds writes ONE CSV row:
    tUtc, tSec, machineCpuPct, availMemMB, then per watched process name:
      <name>_cpuPct   = 100 * delta(TotalProcessorTime) / (interval * logicalCpus)
                        (machine-normalised: 100 = every logical CPU busy; a process using
                        4 threads flat out on 32 logical CPUs reads ~12.5)
      <name>_cores    = the same as "cores' worth" (cpuPct * logicalCpus / 100)
      <name>_threads, <name>_wsMB
  A process name that is not running yields empty cells (never an error). Multiple processes
  with the same name (e.g. two WatchVrf) are summed.
  Stops when -StopFile exists, when -MaxSec elapses, or on Ctrl+C. Never touches any process.

  Vendor context: UG52 6.1.1 - the sim engine's default configuration "limits the overall CPU
  usage" (numCallbackThreads 4, nav threads 2, network threads forced 1 unless thread-safe RTI);
  the video card is a GUI factor only (6.1.2). This sampler answers "is the MACHINE saturated"
  vs "is the sim's configured thread budget saturated" - two different readings of the same
  crawl. Tracy (6.2.1) is the vendor's per-frame breakdown; this is the OS-level view.

.EXAMPLE
  scripts\SampleCpu.ps1 -OutFile runs\x\cpu-samples.csv -StopFile runs\x\observers.stop -MaxSec 3600
#>
param(
    [Parameter(Mandatory = $true)] [string] $OutFile,
    [int]    $IntervalSec = 5,
    [int]    $MaxSec = 7200,
    [string] $StopFile = '',
    [string[]] $Processes = @('vrfSimHLA1516e5.2d', 'vrfSimHLA1516e', 'vrfSim', 'VrfC2SimApp', 'rtiexec', 'rtiForwarder', 'WatchVrf', 'ListenReports', 'vrfGui')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logical = [Environment]::ProcessorCount
$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

# Header. One column set per watched name; names are sanitised to [A-Za-z0-9_].
$names = @($Processes | Select-Object -Unique)
$cols = @('tUtc', 'tSec', 'machineCpuPct', 'availMemMB')
foreach ($n in $names) {
    $s = ($n -replace '[^A-Za-z0-9_]', '_')
    $cols += @("${s}_cpuPct", "${s}_cores", "${s}_threads", "${s}_wsMB")
}
Set-Content -Path $OutFile -Value ($cols -join ',') -Encoding ascii
"SampleCpu: logicalCpus=$logical interval=${IntervalSec}s max=${MaxSec}s out=$OutFile stopFile='$StopFile'"

# Per-process CPU needs a delta: remember (pid -> TotalProcessorTime) between samples.
$prevCpu = @{}
$prevWall = [DateTime]::UtcNow
$t0 = $prevWall
$machineCounter = '\Processor(_Total)\% Processor Time'
$memCounter = '\Memory\Available MBytes'

while ($true) {
    Start-Sleep -Seconds $IntervalSec
    $now = [DateTime]::UtcNow
    $wallSec = ($now - $prevWall).TotalSeconds
    $prevWall = $now

    $machine = ''; $avail = ''
    try {
        $c = Get-Counter -Counter @($machineCounter, $memCounter) -ErrorAction Stop
        $machine = [math]::Round(($c.CounterSamples | Where-Object { $_.Path -like '*processor*' } | Select-Object -First 1).CookedValue, 1)
        $avail   = [math]::Round(($c.CounterSamples | Where-Object { $_.Path -like '*memory*' } | Select-Object -First 1).CookedValue, 0)
    } catch { }

    $row = @($now.ToString('yyyy-MM-ddTHH:mm:ssZ'), [math]::Round(($now - $t0).TotalSeconds, 0), $machine, $avail)
    $procs = Get-Process -ErrorAction SilentlyContinue
    # WORKING SET VIA CIM, NOT Process.WorkingSet64: a 32-bit PowerShell host (the Claude Code tool
    # host is one - [Environment]::Is64BitProcess False, found 2026-09-06) reads a 64-bit process's
    # WorkingSet64 CLAMPED at 4096 MB (the sim showed 4096 flat while CIM said 5183). CPU times and
    # thread counts are not affected. One CIM query per sample, keyed by pid.
    $wsByPid = @{}
    try {
        foreach ($w in (Get-CimInstance Win32_Process -ErrorAction Stop | Select-Object ProcessId, WorkingSetSize)) {
            $wsByPid[[int]$w.ProcessId] = [double]$w.WorkingSetSize
        }
    } catch { }
    $seen = @{}
    foreach ($n in $names) {
        $ps = @($procs | Where-Object { $_.ProcessName -eq $n })
        if ($ps.Count -eq 0) { $row += @('', '', '', ''); continue }
        $cpuSec = 0.0; $threads = 0; $ws = 0.0
        foreach ($p in $ps) {
            $seen[$p.Id] = $true
            try {
                $tp = $p.TotalProcessorTime.TotalSeconds
                if ($prevCpu.ContainsKey($p.Id)) { $cpuSec += ($tp - $prevCpu[$p.Id]) }
                $prevCpu[$p.Id] = $tp
                $threads += $p.Threads.Count
                if ($wsByPid.ContainsKey([int]$p.Id)) { $ws += $wsByPid[[int]$p.Id] / 1MB } else { $ws += $p.WorkingSet64 / 1MB }
            } catch { }
        }
        $pct = if ($wallSec -gt 0) { [math]::Round(100.0 * $cpuSec / ($wallSec * $logical), 1) } else { '' }
        $cores = if ($wallSec -gt 0) { [math]::Round($cpuSec / $wallSec, 2) } else { '' }
        $row += @($pct, $cores, $threads, [math]::Round($ws, 0))
    }
    # forget exited pids
    foreach ($k in @($prevCpu.Keys)) { if (-not $seen.ContainsKey($k)) { $prevCpu.Remove($k) } }

    Add-Content -Path $OutFile -Value ($row -join ',') -Encoding ascii

    if ($StopFile -and (Test-Path $StopFile)) { "SampleCpu: stop file seen, exiting."; break }
    if (($now - $t0).TotalSeconds -ge $MaxSec) { "SampleCpu: MaxSec reached, exiting."; break }
}
