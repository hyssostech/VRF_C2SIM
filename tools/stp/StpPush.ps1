# StpPush.ps1 - make an STP server push C2SIM content, over STP's own WebSocket JSON API (no Pro UI).
# FIRST USED LIVE 2026-10-11, REHEARSAL_WAYB-2026-10-11-6 (docs/experiments/REHEARSAL_WAYB_STPPUSH_2026-10-11.md): an STP
# container (image stp-server:coa-l13) pushed the Iron Storm init and order to c2sim-server-vrf, success=true both.
# The STP side (read from STP main, cited in that record's plan): the C2SIM bridge's push_c2sim_content pushes the
# content it is GIVEN (it builds nothing) with its configured ResetBeforeInitialize / RunAfterInitialize - the same
# STOP/RESET/INITIALIZE, push, SHARE/START sequence as tools/PushInit; an order push = SwitchToRunning + push.
#
# Wire format:
#   outer: {"method":"Request","params":{"jsonRequest":"<inner JSON as a string>","cookie":N,"timeout":S}}
#   inner: {"method":"Register"|"PushC2SIMContent"|"LoadNewScenario"|"GetC2SIMContent","params":{...}}
#   reply: {"method":"RequestResponse","params":{"cookie":N,"success":true|false,"result":...}}
# dataType must be spelled exactly "initialization" or "order" (anything else parses as All, which STP refuses).
#
# Actions:
#   initialization | order : push -File as is
#   regen                  : OFFLINE for the C2SIM server - load -File (.op object_set) into STP, then
#                            GetC2SIMContent for initialization and order, written to -OutDir. Pushes nothing.
# PORTS: the STP container's 9599 must be published on a NON-default host port (the owner, 2026-10-11: defaults are taken
# by live testing - stp-lt holds 9500-9599); the default -Url below is the rehearsal's 39599.
# Exit: 0 success; 1 refused / failed / no reply. ASCII only.
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('initialization', 'order', 'regen')]
    [string]$Action,
    [Parameter(Mandatory = $true)]
    [string]$File,
    [string]$Url = 'ws://127.0.0.1:39599',
    [string]$ScenarioName = 'STP-IRON-STORM-SYNTHETIC',
    [string]$OutDir = '.',
    [int]$TimeoutSec = 180,
    # regen only: '' = all affiliations (what CAPSTONE-STP883 had to use - 'friend' loses every task, STP-1105).
    [string]$Affiliation = ''
)
$ErrorActionPreference = 'Stop'

$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(20)
$script:cookie = 1

function Send-Text([string]$s) {
    $b = [System.Text.Encoding]::UTF8.GetBytes($s)
    $null = $ws.SendAsync([ArraySegment[byte]]::new($b), [System.Net.WebSockets.WebSocketMessageType]::Text, $true,
        [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
}

function Receive-Text([int]$sec) {
    $cts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($sec))
    $buf = New-Object byte[] 65536
    $ms = [System.IO.MemoryStream]::new()
    do {
        $r = $ws.ReceiveAsync([ArraySegment[byte]]::new($buf), $cts.Token).GetAwaiter().GetResult()
        if ($r.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
            throw "STP closed the socket: $($r.CloseStatus) $($r.CloseStatusDescription)"
        }
        $ms.Write($buf, 0, $r.Count)
    } until ($r.EndOfMessage)
    return [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
}

function Invoke-Stp([string]$method, [hashtable]$params, [int]$sec) {
    $c = $script:cookie++
    $inner = @{ method = $method; params = $params } | ConvertTo-Json -Depth 10 -Compress
    $outer = @{ method = 'Request'; params = @{ jsonRequest = $inner; cookie = $c; timeout = $sec } } | ConvertTo-Json -Depth 10 -Compress
    Write-Host ("[{0:HH:mm:ss.fff}Z] -> {1} (cookie {2}, {3} chars)" -f (Get-Date).ToUniversalTime(), $method, $c, $outer.Length)
    Send-Text $outer
    $deadline = (Get-Date).AddSeconds($sec + 15)
    while ((Get-Date) -lt $deadline) {
        $m = (Receive-Text ($sec + 15)) | ConvertFrom-Json
        if ($m.method -eq 'RequestResponse' -and [int]$m.params.cookie -eq $c) {
            Write-Host ("[{0:HH:mm:ss.fff}Z] <- RequestResponse cookie {1} success={2}" -f (Get-Date).ToUniversalTime(), $c, $m.params.success)
            return $m.params
        }
        if ($m.method -eq 'StpMessage') { Write-Host "   [StpMessage] $($m.params.level): $($m.params.message)" }
    }
    throw "no RequestResponse for cookie $c within $sec s"
}

$exit = 1
try {
    $null = $ws.ConnectAsync([Uri]$Url, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
    $reg = Invoke-Stp 'Register' @{ serviceName = 'VrfC2SimStpPush'; language = 'powershell'; solvables = @(); machineId = $null; sessionId = $null } 30
    if (-not $reg.success) { throw "Register refused: $($reg.result)" }
    Write-Host "registered, session = $($reg.result)"

    $text = [System.IO.File]::ReadAllText((Resolve-Path $File).Path)   # strips a BOM if present
    if ($Action -eq 'regen') {
        $ld = Invoke-Stp 'LoadNewScenario' @{ content = $text } $TimeoutSec
        if (-not $ld.success) { throw "LoadNewScenario failed: $($ld.result)" }
        foreach ($t in 'initialization', 'order') {
            $g = Invoke-Stp 'GetC2SIMContent' @{ name = $ScenarioName; dataType = $t; affiliation = $Affiliation; coaPoids = @() } $TimeoutSec
            if (-not $g.success) { throw "GetC2SIMContent $t failed: $($g.result)" }
            $out = Join-Path $OutDir ("REGEN-{0}_{1}.xml" -f $ScenarioName, $t)
            [System.IO.File]::WriteAllText($out, [string]$g.result, [System.Text.UTF8Encoding]::new($false))
            Write-Host "wrote $out ($(([string]$g.result).Length) chars)"
        }
        $exit = 0
    }
    else {
        # options omitted on purpose: null -> no per-call override; the container env and
        # c2simbridge.json decide (ResetBeforeInitialize/RunAfterInitialize True, SystemName "Not Set").
        $p = Invoke-Stp 'PushC2SIMContent' @{ content = $text; dataType = $Action } $TimeoutSec
        if ($p.success) { Write-Host "PUSH OK ($Action)"; $exit = 0 }
        else { Write-Host "PUSH FAILED ($Action): $($p.result)" }
    }
}
catch { Write-Host "ERROR: $($_.Exception.Message)" }
finally {
    try { $null = $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'done', [System.Threading.CancellationToken]::None).GetAwaiter().GetResult() } catch { }
}
exit $exit
