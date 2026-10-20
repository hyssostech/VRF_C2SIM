# STP-1296 amendment 2: the SYSTEM-side sampler, run beside ws_sampler2.ps1 (the back-end sampler) on the same host
# clock (both stamp Get-Date UTC). One sample + a 1 s sleep per loop: about 1 Hz, slower under memory pressure.
# Read-only: it never stops anything; the caller tears down on its STOP marker.
# CSV: tUtc,availGiB,vmmemWslWsMB,vmmemWslPrivMB,ifcWsMB,ifcPrivMB,guiWsMB,guiPrivMB,backendWsMB,backendPrivMB,
#      commitGiB,commitLimitGiB,pageFaultsPerSec,pagefileUsedMB,backendPageFaultsPerSec   (the last five: amendment C5)
# commit charge / limit and page faults/s: Win32_PerfFormattedData_PerfOS_Memory (CommittedBytes, CommitLimit,
# PageFaultsPersec); pagefile use: Win32_PageFileUsage CurrentUsage (MB, summed); the back end's own page faults/s:
# Win32_PerfFormattedData_PerfProc_Process (Name vrfSimHLA1516e, PageFaultsPersec). An unreadable value is empty.
# (an absent or unreadable process = empty cells). tUtc is HH:mm:ss.fff. availGiB is Win32_OperatingSystem
# FreePhysicalMemory, which Windows reports as AVAILABLE memory (free + standby + zeroed; Task Manager "Available"),
# the same quantity as every earlier memory gate. vmmem* matches vmmemWSL (and vmmem on older WSL builds).
# ALERTS, each a one-time snapshot (top-10 processes by working set + docker stats --no-stream) into -SnapDir:
#   availGiB < -WarnGiB  -> "WARN ..." line, snapshot warn-<hhmmss>.txt, keep sampling;
#   availGiB < -StopGiB  -> "STOP ..." line, snapshot stop-<hhmmss>.txt, the marker <SnapDir>\..\c12-STOP.txt, exit 3
#   (the MEMORY STOP, c14919). One sample that crosses both thresholds snapshots once, as STOP.
# The caller watches the MARKER, not the exit code.
param([Parameter(Mandatory)][string]$Out, [Parameter(Mandatory)][string]$SnapDir, [int]$Seconds = 3600,
      [double]$WarnGiB = 7.0, [double]$StopGiB = 6.0,
      # Amendment C5: a COMMIT guard - STOP when the system commit charge is >= this percent of the commit limit (0 = off).
      [double]$StopCommitPct = 0)
"tUtc,availGiB,vmmemWslWsMB,vmmemWslPrivMB,ifcWsMB,ifcPrivMB,guiWsMB,guiPrivMB,backendWsMB,backendPrivMB,commitGiB,commitLimitGiB,pageFaultsPerSec,pagefileUsedMB,backendPageFaultsPerSec" | Set-Content $Out -Encoding ascii
$script:commitPct = -1
function Vm {
    $m = try { Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory -ErrorAction Stop } catch { $null }
    $script:commitPct = if ($m -and $m.CommitLimit -gt 0) { 100.0 * $m.CommittedBytes / $m.CommitLimit } else { -1 }
    $pf = try { (Get-CimInstance Win32_PageFileUsage -ErrorAction Stop | Measure-Object CurrentUsage -Sum).Sum } catch { $null }
    $bp = try { (Get-CimInstance Win32_PerfFormattedData_PerfProc_Process -Filter "Name='vrfSimHLA1516e'" -ErrorAction Stop | Select-Object -First 1).PageFaultsPersec } catch { $null }
    '{0},{1},{2},{3},{4}' -f $(if ($m) { [math]::Round($m.CommittedBytes / 1GB, 2) } else { '' }), $(if ($m) { [math]::Round($m.CommitLimit / 1GB, 2) } else { '' }),
        $(if ($m) { $m.PageFaultsPersec } else { '' }), $(if ($null -ne $pf) { $pf } else { '' }), $(if ($null -ne $bp) { $bp } else { '' })
}
New-Item -ItemType Directory -Force $SnapDir | Out-Null
function Mem([string]$name) {
    $p = Get-Process $name -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { return ',' }
    try { '{0},{1}' -f [math]::Round($p.WorkingSet64 / 1MB, 1), [math]::Round($p.PrivateMemorySize64 / 1MB, 1) } catch { ',' }
}
function Snap([string]$kind, [datetime]$now) {
    $f = Join-Path $SnapDir ('{0}-{1}.txt' -f $kind, $now.ToUniversalTime().ToString('HHmmss'))
    ('{0} snapshot at {1}' -f $kind, $now.ToUniversalTime().ToString('HH:mm:ss.fff')) | Set-Content $f -Encoding ascii
    Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First 10 |
        ForEach-Object { '{0} {1} wsMB {2} privMB {3}' -f $_.ProcessName, $_.Id, [math]::Round($_.WorkingSet64 / 1MB), [math]::Round($_.PrivateMemorySize64 / 1MB) } |
        Add-Content $f -Encoding ascii
    (docker stats --no-stream --format '{{.Name}} {{.MemUsage}} {{.CPUPerc}}' 2>&1) | Add-Content $f -Encoding ascii
}
$warned = $false
$end = (Get-Date).AddSeconds($Seconds)
while ((Get-Date) -lt $end) {
    $now = Get-Date
    $avail = [math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB, 2)
    ('{0},{1},{2},{3},{4},{5},{6}' -f $now.ToUniversalTime().ToString('HH:mm:ss.fff'), $avail, (Mem 'vmmem*'), (Mem 'VrfC2SimApp'),
        (Mem 'vrfGui'), (Mem 'vrfSimHLA1516e'), (Vm)) | Add-Content $Out -Encoding ascii
    if ($StopCommitPct -gt 0 -and $script:commitPct -ge $StopCommitPct) { Snap 'stop' $now; ('{0} commit {1:F1} % of the limit >= {2} %' -f $now.ToUniversalTime().ToString('HH:mm:ss.fff'), $script:commitPct, $StopCommitPct) | Set-Content (Join-Path (Split-Path $SnapDir -Parent) 'c12-STOP.txt') -Encoding ascii; "STOP $($now.ToUniversalTime().ToString('HH:mm:ss.fff')) commit $([math]::Round($script:commitPct, 1)) % >= $StopCommitPct %"; exit 3 }
    if ($avail -lt $StopGiB) { Snap 'stop' $now; ('{0} free {1} GiB < {2}' -f $now.ToUniversalTime().ToString('HH:mm:ss.fff'), $avail, $StopGiB) | Set-Content (Join-Path (Split-Path $SnapDir -Parent) 'c12-STOP.txt') -Encoding ascii; "STOP $($now.ToUniversalTime().ToString('HH:mm:ss.fff')) free $avail GiB < $StopGiB"; exit 3 }
    if (-not $warned -and $avail -lt $WarnGiB) { $warned = $true; Snap 'warn' $now; "WARN $($now.ToUniversalTime().ToString('HH:mm:ss.fff')) free $avail GiB < $WarnGiB" }
    Start-Sleep -Milliseconds 1000
}
exit 0
