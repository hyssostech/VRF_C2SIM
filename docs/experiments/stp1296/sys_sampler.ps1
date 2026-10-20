# STP-1296 amendment 2: the SYSTEM-side 1 Hz sampler, run beside ws_sampler2.ps1 (the back-end sampler) on the same
# host clock (both stamp Get-Date UTC). Read-only: it never stops anything; the caller tears down on exit 3.
# CSV: tUtc,availGiB,vmmemWslWsMB,vmmemWslPrivMB,ifcWsMB,ifcPrivMB,guiWsMB,guiPrivMB,backendWsMB,backendPrivMB
# (an absent or unreadable process = empty cells). tUtc is HH:mm:ss.fff.
# ALERTS, each a one-time snapshot (top-10 processes by working set + docker stats --no-stream) into -SnapDir:
#   availGiB < -WarnGiB  -> "WARN ..." line, snapshot warn-<hhmmss>.txt, keep sampling;
#   availGiB < -StopGiB  -> "STOP ..." line, snapshot stop-<hhmmss>.txt, exit 3 (the MEMORY STOP, c14919).
param([Parameter(Mandatory)][string]$Out, [Parameter(Mandatory)][string]$SnapDir, [int]$Seconds = 3600,
      [double]$WarnGiB = 7.0, [double]$StopGiB = 6.0)
"tUtc,availGiB,vmmemWslWsMB,vmmemWslPrivMB,ifcWsMB,ifcPrivMB,guiWsMB,guiPrivMB,backendWsMB,backendPrivMB" | Set-Content $Out -Encoding ascii
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
    ('{0},{1},{2},{3},{4},{5}' -f $now.ToUniversalTime().ToString('HH:mm:ss.fff'), $avail, (Mem 'vmmemWSL'), (Mem 'VrfC2SimApp'),
        (Mem 'vrfGui'), (Mem 'vrfSimHLA1516e')) | Add-Content $Out -Encoding ascii
    if ($avail -lt $StopGiB) { Snap 'stop' $now; "STOP $($now.ToUniversalTime().ToString('HH:mm:ss.fff')) free $avail GiB < $StopGiB"; exit 3 }
    if (-not $warned -and $avail -lt $WarnGiB) { $warned = $true; Snap 'warn' $now; "WARN $($now.ToUniversalTime().ToString('HH:mm:ss.fff')) free $avail GiB < $WarnGiB" }
    Start-Sleep -Milliseconds 1000
}
exit 0
