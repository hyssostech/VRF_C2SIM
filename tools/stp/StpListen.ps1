# StpListen.ps1 - register on an STP server's WebSocket JSON API and RECORD what it pushes for -Seconds.
# Purpose: the STP-976 check that STP re-emits the interface's position reports to clients - per the STP-1097 session
# (STP main 3c614980, StpJsonClient.cs:169) as an event named "SymbolReport" carrying {poid, symbol}; it is not in
# Schema.json, so this records EVERY message and counts them by name rather than filtering on a schema.
# Same wire format as tools\stp\StpPush.ps1 (Register is a Request envelope; events arrive unsolicited).
# Output: -Out (JSON lines, one message per line, with a receive timestamp); a summary of message counts by name; for
# SymbolReport, the number of distinct poids and, per poid, whether its reported location changed between events.
# Sends nothing but Register. Exit: 0 connected and listened; 1 connect/register failed. ASCII only.
param(
    [string]$Url = 'ws://127.0.0.1:39599',
    [int]$Seconds = 120,
    [Parameter(Mandatory = $true)]
    [string]$Out
)
$ErrorActionPreference = 'Stop'
$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(20)

function Send-Text([string]$s) {
    $b = [System.Text.Encoding]::UTF8.GetBytes($s)
    $null = $ws.SendAsync([ArraySegment[byte]]::new($b), [System.Net.WebSockets.WebSocketMessageType]::Text, $true,
        [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
}

function Receive-Text([System.Threading.CancellationToken]$ct) {
    $buf = New-Object byte[] 1048576
    $ms = [System.IO.MemoryStream]::new()
    do {
        $r = $ws.ReceiveAsync([ArraySegment[byte]]::new($buf), $ct).GetAwaiter().GetResult()
        if ($r.MessageType -eq 'Close') { return $null }
        $ms.Write($buf, 0, $r.Count)
    } until ($r.EndOfMessage)
    [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
}

function Get-MessageName($m) {
    foreach ($n in @($m.method, $m.params.method, $m.params.name, $m.name, $m.event)) { if ($n) { return [string]$n } }
    return '(unnamed)'
}

$counts = @{}
$lastLoc = @{}
$moved = @{}
$exit = 1
try {
    $null = $ws.ConnectAsync([Uri]$Url, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
    $inner = @{ method = 'Register'; params = @{ serviceName = 'VrfC2SimStpListen'; language = 'powershell'; solvables = @(); machineId = $null; sessionId = $null } } | ConvertTo-Json -Depth 10 -Compress
    Send-Text (@{ method = 'Request'; params = @{ jsonRequest = $inner; cookie = 1; timeout = 30 } } | ConvertTo-Json -Depth 10 -Compress)
    $writer = [System.IO.StreamWriter]::new($Out, $false, [System.Text.UTF8Encoding]::new($false))
    $cts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($Seconds))
    try {
        while (-not $cts.IsCancellationRequested) {
            try { $text = Receive-Text $cts.Token } catch { break }
            if ($null -eq $text) { break }
            $stamp = (Get-Date).ToUniversalTime().ToString('o')
            $writer.WriteLine('{"rx":"' + $stamp + '","msg":' + $text + '}')
            try { $m = $text | ConvertFrom-Json } catch { $m = $null }
            $name = if ($m) { Get-MessageName $m } else { '(not json)' }
            $counts[$name] = 1 + [int]$counts[$name]
            if ($name -eq 'SymbolReport' -and $m) {
                $p = if ($m.params.params) { $m.params.params } else { $m.params }
                $poid = [string]$p.poid
                $loc = ($p.symbol | ConvertTo-Json -Depth 8 -Compress)
                if ($poid) {
                    if ($lastLoc.ContainsKey($poid) -and $lastLoc[$poid] -ne $loc) { $moved[$poid] = $true }
                    $lastLoc[$poid] = $loc
                }
            }
        }
    } finally { $writer.Dispose() }
    $exit = 0
}
catch { Write-Host "ERROR: $($_.Exception.Message)" }
finally {
    try { $null = $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'done', [System.Threading.CancellationToken]::None).GetAwaiter().GetResult() } catch { }
}
Write-Host "=== StpListen $Url for $Seconds s -> $Out ==="
$counts.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object { Write-Host ("  {0,6}x {1}" -f $_.Value, $_.Key) }
Write-Host ("  SymbolReport: {0} distinct poid(s); {1} with a symbol that CHANGED between events" -f $lastLoc.Count, $moved.Count)
exit $exit
