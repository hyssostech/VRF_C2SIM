<#
.SYNOPSIS
  Per-thread CPU sampler for ONE process (2026-09-13, PREREG_NAVDATA_G3: "which thread is hot when the
  sim engine's sim/wall ratio collapses?").

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

.EXAMPLE
  scripts\SampleThreads.ps1 -ProcessName vrfSimHLA1516e -MaxSec 1900 -IntervalSec 5 -OutFile x\threads.csv
#>
param(
    [Parameter(Mandatory = $true)] [string] $ProcessName,
    [Parameter(Mandatory = $true)] [string] $OutFile,
    [int]    $IntervalSec = 5,
    [int]    $MaxSec = 3600,
    [int]    $TopThreads = 8,
    [int]    $WaitForProcessSec = 600,
    [string] $StopFile = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
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

$prevProc = $proc.TotalProcessorTime
$prevThreads = @{}
foreach ($th in $proc.Threads) { $prevThreads[$th.Id] = $th.TotalProcessorTime }
$prevWall = [DateTime]::UtcNow
$tStart = $prevWall

while ($true) {
    Start-Sleep -Seconds $IntervalSec
    if ($StopFile -and (Test-Path $StopFile)) { break }
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
    $line = @($now.ToString('o'), ('{0:F1}' -f $tSec), $p.Id, ('{0:F2}' -f $cores), ('{0:F0}' -f ($p.WorkingSet64 / 1MB)), $p.Threads.Count) + $top
    Add-Content -Path $OutFile -Value ($line -join ',') -Encoding ascii
}
"SampleThreads: done ($OutFile)"
