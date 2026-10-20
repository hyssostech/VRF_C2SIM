# STP-1296 (D114(a)) - ONE ARM of the registered session (PREREG_STP1296_GROUP_TASK_2026-10-19.md, amendment 1 sec 1c).
# A COPY of handoff-2026-10-15/repro_run.ps1 (the R-a / L2 runner) with ONLY these declared changes (PREREG G0f, sec 2):
#   -NoGui (no vrfGui, no frontend number; 4 numbers per launch: holder, +1 retry, back end +2, interface +3);
#   the interface process env: Vrf__AggregateMovePlanner=$Planner, Vrf__ContainerTypeOverride=F-UCI-H=8:4:1:111 (BOTH
#   arms), Logging__LogLevel__VrfC2SimApp=Debug (BOTH arms; the Group member lines log at Debug);
#   two pre-launch gates: free memory >= 7.0 GiB (HANDOFF_PARK sec 5 step 3) and the private C2SIM server answering
#   the reset (G0e) - either missing = ABORT before any launch;
#   no GUI capture at the end (there is no GUI). Read before running.
# AMENDMENT 2 (after c14963's MEMORY STOP in arm P; STP-1296 comment, 2026-10-20), on top of the above:
#   the memory gate is >= 11.5 GiB free (the value at c14963's gate), recorded with the vmmem* working set and
#   docker stats for every container (c0-gate.txt);
#   a second sampler, about 1 Hz (sys_sampler.ps1: free memory, vmmemWSL, VrfC2SimApp, vrfGui, back end; WS + private MB) runs
#   from the gate to the end on the same host clock as ws_sampler2.ps1, snapshotting the top-10 processes + docker stats
#   on alert (WARN < 7.0 GiB, STOP < 6.0 GiB; a STOP writes the marker c12-STOP.txt);
#   every STOP (that marker, the system sampler gone, ws_sampler2's ABORT) tears down with the BACK END FIRST
#   (StopVrf52 -TimeoutSec 15 -GraceSec 1 with our own pid + start), then the reset and the interface; exit 7;
#   ws_sampler2.ps1 is run from its committed place (handoff-2026-10-15), not from a copy next to this script.
# HANDOFF COPY (2026-10-15) of the scratch tool that ran REPRO_BACKEND_STOP R-a/R-b; -Root/-Planner added for L2.
# Expects runs\launch52\repro\repro_init.xml + repro_order_<a|b>.xml (tools/stp/make_backend_stop_repro.py) and the
# disposable STP container stp-r on ws://127.0.0.1:39599 (see HANDOFF_2026-10-15.md). Read before running.
# One sanitized repro run end to end (scratch tool). Launch VR-Forces (own holder number), sampler, interface (MAIN),
# push repro init + order through the stp-r container, watch the second dispatch + 6 min, sample threads, tear down.
param([Parameter(Mandatory)][string]$Tag, [Parameter(Mandatory)][int]$AppBase, [string]$Order = 'repro_order_a.xml',
      [string]$Root = 'F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM',
      [Parameter(Mandatory)][ValidateSet('PlanPath','Group')][string]$Planner)
$ErrorActionPreference = 'Continue'
$R = $Root   # L2: pass the feat/aggregate-documented-move worktree so ITS build and scripts run
$env:Vrf__AggregateMovePlanner = $Planner
$env:Vrf__ContainerTypeOverride = 'F-UCI-H=8:4:1:111'
$env:Logging__LogLevel__VrfC2SimApp = 'Debug'
Set-Location $R
$d = "runs\launch52\stp1296\$Tag"; New-Item -ItemType Directory -Force $d | Out-Null
$ts = { (Get-Date).ToUniversalTime().ToString('HH:mm:ss') }
Remove-Item Env:Vrf__DemoExtent -ErrorAction SilentlyContinue
$free = [math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB, 2)
"[$(& $ts)] free memory: $free GiB"
$gate = "$d\c0-gate.txt"
"gate $(& $ts) free $free GiB" | Set-Content $gate -Encoding ascii
"vmmem*: " + ((Get-Process vmmem* -ErrorAction SilentlyContinue | ForEach-Object { "pid $($_.Id) wsMB $([math]::Round($_.WorkingSet64/1MB)) privMB $([math]::Round($_.PrivateMemorySize64/1MB))" }) -join '; ') | Add-Content $gate -Encoding ascii
(docker stats --no-stream --format '{{.Name}} {{.MemUsage}} {{.CPUPerc}}' 2>&1) | Add-Content $gate -Encoding ascii
if ($free -lt 11.5) { "ABORT: free memory $free GiB < 11.5 (STP-1296 amendment 2)"; exit 4 }
$sys = Start-Process pwsh -ArgumentList @('-NoProfile','-File',(Join-Path $PSScriptRoot 'sys_sampler.ps1'),
    '-Out',"$R\$d\c12-sys.csv",'-SnapDir',"$R\$d\c12-snap",'-Seconds','3600') -RedirectStandardOutput "$d\c12-sys.out" -PassThru -WindowStyle Hidden
$null = $sys.Handle   # Fable review: a redirected -PassThru process keeps no handle, so its ExitCode would read null
$vb = $null; $smp = $null; $ifc = $null
function Stop-Run([string]$why) {
    # amendment 2: every STOP tears down with the BACK END FIRST (our own pid + start), on a short budget
    # (-TimeoutSec 15 -GraceSec 1: about 16 s from the trigger to the force, against the 60 s default).
    "[$(& $ts)] STOP: $why"
    $b = Get-Process vrfSimHLA1516e -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($b) { pwsh -NoProfile -File scripts\StopVrf52.ps1 -TimeoutSec 15 -GraceSec 1 -ForceOwnBackendPid $b.Id -ForceOwnBackendStartUtc $b.StartTime.ToUniversalTime().ToString('o') *> "$d\c10-force.log"; "[$(& $ts)] back end stop exit $LASTEXITCODE" }
    else { "[$(& $ts)] no back end running" }
    tools\StopIface\bin\Release\net10.0\StopIface.exe http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM --yes *> "$d\c8-stopiface.log"
    foreach ($p in $script:smp, $script:ifc, $sys) { if ($p -and -not $p.HasExited) { try { Stop-Process -Id $p.Id -ErrorAction SilentlyContinue } catch {} } }
    "   left running: " + ((Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue | ForEach-Object { "$($_.ProcessName) $($_.Id)" }) -join ', ')
    exit 7
}
function Stop-Check {
    # called in every wait loop. Three triggers, none read from an exit code:
    #   the system sampler's MEMORY STOP marker; the system sampler gone without one (memory unwatched);
    #   the back-end sampler's ABORT line (c14919 / PREREG sec 2 working-set rule).
    if (Test-Path "$d\c12-STOP.txt") { Stop-Run ("MEMORY STOP - " + ((Get-Content "$d\c12-STOP.txt") -join ' ')) }
    if ($sys.HasExited) { Stop-Run 'the system sampler ended without a STOP marker - memory is unwatched' }
    if ($script:smp -and $script:smp.HasExited -and (Select-String -Path "$d\c11-sampler.out" -Pattern '^ABORT' -Quiet -ErrorAction SilentlyContinue)) {
        Stop-Run ("back-end sampler " + ((Select-String -Path "$d\c11-sampler.out" -Pattern '^ABORT' | Select-Object -First 1).Line)) }
}
$reset = (tools\StopIface\bin\Release\net10.0\StopIface.exe http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM --yes 2>&1 | Select-String 'after RESET') -join ''
"[$(& $ts)] reset: " + $reset
if (-not $reset) { Stop-Process -Id $sys.Id -ErrorAction SilentlyContinue; "ABORT: the private C2SIM server did not answer the reset - D106 is OPERATOR ONLY, so STOP"; exit 5 }
if (Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue) { Stop-Process -Id $sys.Id -ErrorAction SilentlyContinue; "ABORT: VR-Forces or an interface is already running"; exit 2 }
$launch = Start-Process pwsh -ArgumentList @('-NoProfile','-File','scripts\LaunchVrf52.ps1','-Scenario','IronStorm_Centre_52_Aggregate',
    '-FederationHoldAppNumber',"$AppBase",'-BackendAppNumber',"$($AppBase+2)",'-NoGui',
    '-AppDataDir','C:\C2SIM\vrf-appdata-unattended\appData') -RedirectStandardOutput "$d\c2-launchvrf52.log" -RedirectStandardError "$d\c2-launchvrf52.err" -PassThru -WindowStyle Hidden
$deadline = (Get-Date).AddSeconds(420)
while ((Get-Date) -lt $deadline) { Stop-Check; if (Select-String -Path "$d\c2-launchvrf52.log" -Pattern 'READY: 5.2d|\[FAIL\]' -Quiet -ErrorAction SilentlyContinue) { break }; Start-Sleep 5 }
"[$(& $ts)] launch: " + ((Select-String -Path "$d\c2-launchvrf52.log" -Pattern 'READY: 5.2d|\[FAIL\]' | Select-Object -First 2 | ForEach-Object { $_.Line.Trim().Substring(0,[Math]::Min(80,$_.Line.Trim().Length)) }) -join ' | ')
if (-not (Select-String -Path "$d\c2-launchvrf52.log" -Pattern 'READY: 5.2d' -Quiet)) { Stop-Run 'ABORT: VR-Forces not READY' }
$smp = Start-Process pwsh -ArgumentList @('-NoProfile','-File',(Join-Path $PSScriptRoot '..\handoff-2026-10-15\ws_sampler2.ps1'),
    '-Out',"$R\$d\c11-sampler.csv",'-Seconds','1800','-AbortMBPerMin','700','-WindowSec','300','-AbortWsMB','16000') -RedirectStandardOutput "$d\c11-sampler.out" -PassThru -WindowStyle Hidden
$ifc = Start-Process pwsh -ArgumentList @('-NoProfile','-File','scripts\StartInterface52.ps1','-ClientId','STP','-Server','private','-AppNumber',"$($AppBase+3)") `
    -RedirectStandardOutput "$d\c3-interface.log" -RedirectStandardError "$d\c3-interface.err" -PassThru -WindowStyle Hidden
$f = "$d\c3-interface.log"
$deadline = (Get-Date).AddSeconds(150)
while ((Get-Date) -lt $deadline) { Stop-Check; if ((Test-Path $f) -and (Select-String -Path $f -Pattern 'Connected to C2SIM' -Quiet)) { break }; Start-Sleep 3 }
"[$(& $ts)] interface: " + ((Select-String -Path $f -Pattern 'Backend discovered|NAVIGATE GATE' | ForEach-Object { $_.Line.Trim().Substring(0,[Math]::Min(70,$_.Line.Trim().Length)) }) -join ' | ')
pwsh -NoProfile -File tools\stp\StpPush.ps1 -Action initialization -File "runs\launch52\repro\repro_init.xml" -Url ws://127.0.0.1:39599 *> "$d\c5-init.log"
"[$(& $ts)] init push exit $LASTEXITCODE"
$deadline = (Get-Date).AddSeconds(120)
while ((Get-Date) -lt $deadline) { Stop-Check; if ((Select-String -Path $f -Pattern 'created as uuid').Count -ge 1 -and (Select-String -Path $f -Pattern 'R1 position reports: 1 sent' -Quiet)) { break }; Start-Sleep 5 }
"[$(& $ts)] bound: " + (Select-String -Path $f -Pattern 'created as uuid').Count
pwsh -NoProfile -File tools\stp\StpPush.ps1 -Action order -File "runs\launch52\repro\$Order" -Url ws://127.0.0.1:39599 *> "$d\c7-order.log"
"[$(& $ts)] order push exit $LASTEXITCODE"
$deadline = (Get-Date).AddSeconds(1200)
while ((Get-Date) -lt $deadline) { Stop-Check; if (Select-String -Path $f -Pattern "DISPATCHED \S+ task 'TEST_MOVE_2'|BACK END LOST:" -Quiet) { break }; Start-Sleep 3 }
Select-String -Path $f -Pattern "DISPATCHED \S+ task 'TEST_MOVE_\d'|member\(s\) ended|POPULATE .* member\(s\) created|WATER ON THE LINE -" | ForEach-Object { "   " + $_.Line.Trim().Substring(0,[Math]::Min(170,$_.Line.Trim().Length)) } | Select-Object -Last 8
$deadline = (Get-Date).AddSeconds(360)
while ((Get-Date) -lt $deadline) { Stop-Check; if (Select-String -Path $f -Pattern 'BACK END LOST:' -Quiet) { break }; if ($smp.HasExited) { "[$(& $ts)] BACK-END SAMPLER EXITED without an ABORT line"; break }; Start-Sleep 3 }
"[$(& $ts)] window end. BACK END LOST lines: " + (Select-String -Path $f -Pattern 'BACK END LOST:').Count
Select-String -Path $f -Pattern 'SIM/WALL RATIO' | Select-Object -Last 3 | ForEach-Object { "   " + $_.Line.Trim().Substring(0,[Math]::Min(70,$_.Line.Trim().Length)) }
$vb = Get-Process vrfSimHLA1516e -ErrorAction SilentlyContinue | Select-Object -First 1
if ($vb) {
    $a = @{}; foreach ($t in $vb.Threads) { try { $a[$t.Id] = $t.TotalProcessorTime.TotalSeconds } catch {} }
    Start-Sleep 5; $vb = Get-Process -Id $vb.Id
    "   top threads (cpu s / 5 s): " + (($vb.Threads | ForEach-Object { try { [math]::Round($_.TotalProcessorTime.TotalSeconds - $a[$_.Id], 2) } catch { 0 } } | Sort-Object -Descending | Select-Object -First 3) -join ', ') + "; ws MB " + [math]::Round($vb.WorkingSet64/1MB)
}
tools\StopIface\bin\Release\net10.0\StopIface.exe http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM --yes *> "$d\c8-stopiface.log"
pwsh -NoProfile -File scripts\StopVrf52.ps1 *> "$d\c9-stopvrf52.log"; $e = $LASTEXITCODE
if ($e -eq 3 -and $vb) { pwsh -NoProfile -File scripts\StopVrf52.ps1 -ForceOwnBackendPid $vb.Id -ForceOwnBackendStartUtc $vb.StartTime.ToUniversalTime().ToString('o') *> "$d\c10-force.log"; $e = "3 -> $LASTEXITCODE" }
"[$(& $ts)] StopVrf: $e"
if ($vb) { $w = Get-ChildItem C:\MAK\logs -Filter "*$($vb.Id).log" | Select-Object -First 1; "   vendor log last write (after close): " + $w.LastWriteTimeUtc.ToString('HH:mm:ss.fff') + "; in-obstacle lines: " + (Select-String -Path $w.FullName -SimpleMatch 'Start point is within obstacle').Count }
foreach ($p in $smp, $ifc, $sys) { if ($p -and -not $p.HasExited) { try { Stop-Process -Id $p.Id -ErrorAction SilentlyContinue } catch {} } }
"   left running: " + ((Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue | ForEach-Object { "$($_.ProcessName) $($_.Id)" }) -join ', ')
"   RTI: " + ((Get-Process rtiexec, rtiForwarder -ErrorAction SilentlyContinue | ForEach-Object { "$($_.ProcessName) $($_.Id)" }) -join ', ') + "; launch holder app $AppBase (see c2-launchvrf52.log)"
