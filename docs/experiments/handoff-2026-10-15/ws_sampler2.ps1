# 1 Hz sampler of the VR-Forces back end (vrfSimHLA1516e) with an ABORT threshold. Read-only: it never stops anything.
# CSV: tUtc,pid,wsMB,privMB,cpuCores,threads. Exits 2 printing "ABORT ..." when the working set exceeds -AbortWsMB, or its
# slope over the last -WindowSec exceeds -AbortMBPerMin; exits 0 at -Seconds. The caller tears down on exit 2.
param([Parameter(Mandatory)][string]$Out, [int]$Seconds = 3600, [double]$AbortMBPerMin = 1000, [int]$WindowSec = 120,
      [double]$AbortWsMB = 16000)
"tUtc,pid,wsMB,privMB,cpuCores,threads" | Set-Content $Out -Encoding ascii
$prev = @{}; $hist = New-Object System.Collections.Generic.List[object]
$end = (Get-Date).AddSeconds($Seconds)
while ((Get-Date) -lt $end) {
    $p = Get-Process vrfSimHLA1516e -ErrorAction SilentlyContinue | Select-Object -First 1
    $now = Get-Date
    if ($p) {
        $cpu = $p.TotalProcessorTime.TotalSeconds; $cores = ''
        if ($prev.pid -eq $p.Id) { $cores = '{0:F2}' -f (($cpu - $prev.cpu) / [math]::Max(0.001, ($now - $prev.t).TotalSeconds)) }
        else { $hist.Clear() }
        $prev = @{ pid = $p.Id; cpu = $cpu; t = $now }
        $ws = [math]::Round($p.WorkingSet64 / 1MB, 1)
        ('{0},{1},{2},{3},{4},{5}' -f $now.ToUniversalTime().ToString('HH:mm:ss'), $p.Id, $ws,
            [math]::Round($p.PrivateMemorySize64 / 1MB, 1), $cores, $p.Threads.Count) | Add-Content $Out -Encoding ascii
        $hist.Add([pscustomobject]@{ t = $now; ws = $ws })
        while ($hist.Count -gt 0 -and ($now - $hist[0].t).TotalSeconds -gt $WindowSec) { $hist.RemoveAt(0) }
        if ($ws -gt $AbortWsMB) { "ABORT $($now.ToUniversalTime().ToString('HH:mm:ss')) pid $($p.Id) ws $ws MB > $AbortWsMB"; exit 2 }
        if ($hist.Count -gt 1 -and ($now - $hist[0].t).TotalSeconds -ge ($WindowSec - 2)) {
            $rate = ($ws - $hist[0].ws) / (($now - $hist[0].t).TotalMinutes)
            if ($rate -gt $AbortMBPerMin) { "ABORT $($now.ToUniversalTime().ToString('HH:mm:ss')) pid $($p.Id) ws $ws MB rising $([math]::Round($rate)) MB/min over $WindowSec s"; exit 2 }
        }
    } else { $hist.Clear() }
    Start-Sleep -Milliseconds 1000
}
exit 0
