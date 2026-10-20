# 1 Hz sampler of the VR-Forces back end (vrfSimHLA1516e) with OPTIONAL ABORT thresholds (0 = off, the default). Read-only: it never stops anything.
# CSV: tUtc,pid,wsMB,privMB,cpuCores,threads. Exits 2 printing "ABORT ..." when the working set exceeds -AbortWsMB, or its
# slope over the last -WindowSec exceeds -AbortMBPerMin; exits 0 at -Seconds. The caller tears down on exit 2.
# CONSOLIDATION 2026-10-20 (the operator, directly): memory-based stops are OFF by default; failure-based stops
# (back end lost, RTI damage, stp-lt failure) stay. Measured, S3 (STP-1296 c14982, no memory limits): the vendor's
# PhaseTwo back end printed a PLATEAU at 18:28:49Z (WS 16,878 / private 38,199 MB) with commit at 95-97 % of the limit
# for 66 min; S1 / S2 had been ended by the 6 GiB stop at T0 + 49 / 43 s, and a 1.0 GiB floor or a 95 % commit guard
# would have ended S3 before its plateau. Re-arm a stop only by passing it explicitly.
param([Parameter(Mandatory)][string]$Out, [int]$Seconds = 3600, [double]$AbortMBPerMin = 0, [int]$WindowSec = 120,
      [double]$AbortWsMB = 0)
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
        if ($AbortWsMB -gt 0 -and $ws -gt $AbortWsMB) { "ABORT $($now.ToUniversalTime().ToString('HH:mm:ss')) pid $($p.Id) ws $ws MB > $AbortWsMB"; exit 2 }
        if ($AbortMBPerMin -gt 0 -and $hist.Count -gt 1 -and ($now - $hist[0].t).TotalSeconds -ge ($WindowSec - 2)) {
            $rate = ($ws - $hist[0].ws) / (($now - $hist[0].t).TotalMinutes)
            if ($rate -gt $AbortMBPerMin) { "ABORT $($now.ToUniversalTime().ToString('HH:mm:ss')) pid $($p.Id) ws $ws MB rising $([math]::Round($rate)) MB/min over $WindowSec s"; exit 2 }
        }
    } else { $hist.Clear() }
    Start-Sleep -Milliseconds 1000
}
exit 0
