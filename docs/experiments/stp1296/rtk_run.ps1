# STP-1296 D231 STEP 1 - MAK's Road to Kaunas sample AS SHIPPED, back end only (-NoGui), NO interface, NO order.
# PREREG: docs/experiments/PREREG_STP1296_STEP1_RTK_2026-10-20.md (L1 of PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md,
# revised). Five app numbers: holder AppBase, its retry +1, back end +2, RunSim +3, WatchVrf +4. Read before running.
# Memory instrumentation and the STOP order are amendment 2's (stp1296_arm.ps1 header): an optional gate (-GateGiB; S1 / S2
# used 11.5 GiB free, Available; OFF by default since the consolidation note below) with vmmem* and docker stats
# recorded; sys_sampler.ps1 from the gate; ws_sampler2.ps1 from READY; every STOP (sys marker, sys sampler gone,
# ws_sampler2 ABORT) stops the BACK END FIRST.
# What this script does NOT do: save the scenario (no GUI; no save call exists here), write under C:\MAK itself, touch
# rtiexec / rtiForwarder / rtiAssistant, the C2SIM server, stp-lt or 19575. The SIM writes its own log under
# C:\MAK\logs and its terrain cache through the appData cache junction, as on every run (declared in the PREREG).
# LaunchVrf52 itself COPIES the vendor log at load (its READY snapshot, -LogFile default runs\launch52\vrfSim_*;
# LaunchVrf52.ps1:825-827, :1449). That copy stays on the box: never attached, never committed. This script copies
# nothing; it reads the vendor log in place for counts.
# CONSOLIDATION 2026-10-20 (the operator, directly): memory-based stops are OFF by default; failure-based stops
# (back end lost, RTI damage, stp-lt failure) stay. Measured, S3 (STP-1296 c14982, no memory limits): the vendor's
# PhaseTwo back end printed a PLATEAU at 18:28:49Z (WS 16,878 / private 38,199 MB) with commit at 95-97 % of the limit
# for 66 min; S1 / S2 had been ended by the 6 GiB stop at T0 + 49 / 43 s, and a 1.0 GiB floor or a 95 % commit guard
# would have ended S3 before its plateau. Re-arm a stop only by passing it explicitly.
param([Parameter(Mandatory)][int]$AppBase,
      [string]$Root = 'F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM',
      [string]$Scenario = 'Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunasPhaseTwo\RoadToKaunasPhaseTwo',   # amendment C (D236); Counterattack via -Scenario
      [string]$Tag = 'rtk',
      # AMENDMENT C (D234 / D236): NO fixed window and NO ceiling. The run ends ONLY on IDLE (no reflected object moved
      # more than -IdleMeters over -IdleEndSec WALL s, from WatchVrf's POS lines), on a SAFETY STOP, or when the
      # OPERATOR creates <run dir>\OPERATOR-END.txt. Every -CheckpointSec the script reports its state and keeps running.
      # -InstrumentSec is how long the holder, samplers and observer are started for (the holder's maximum is
      # 86,400 s); a checkpoint warns when less than 2 h of it remains.
      [int]$CheckpointSec = 14400, [int]$InstrumentSec = 86400, [int]$IdleEndSec = 600, [double]$IdleMeters = 100,
      [double]$AirFloorM = 1500,
      # Amendment C1: a file of NON-GROUND object uuids (one per line; every kind-11 object whose DIS domain is not 1,
      # from box read B5). When given it REPLACES the altitude cut for the idle test (PhaseTwo has air routes down to
      # 118.5 m, B5 sec 3a). Matching is on the uuid's hex digits, case-insensitive.
      [string]$NonGroundUuidFile = '',
      # 0 = RunSim sends run() only, so the scenario's own saved time multiplier stands (RunSim sets one only if > 1).
      [int]$Multiplier = 0,
      # Amendment C2 (D238): the pre-launch free-memory gate, GiB. 11.5 was a comparability number; the safety line is
      # the MEMORY STOP (-StopGiB, sys_sampler.ps1; see C5 below - S3 disabled both, c14981).
      [double]$GateGiB = 0,
      # Amendment C5 (D240): the memory STOPs as parameters. -StopGiB is the system sampler's MEMORY STOP (S1 / S2: 6.0);
      # -WsAbortMBPerMin / -WsAbortWsMB are ws_sampler2's slope / size aborts (S1 / S2: 700 MB/min over 300 s, 16,000 MB). 0
      # disables that stop, and 0 is now the DEFAULT for all five (the consolidation note at the top). S3 ran with NO memory limits by the operator's direct
      # instruction (STP-1296 c14981): -GateGiB 0 -StopGiB 0 -StopCommitPct 0, the ws_sampler2 aborts out of reach. (C5's
      # first draft proposed a 1.0 GiB emergency floor, c14978; c14980 found it unsafe; both superseded by c14981.)
      [double]$StopGiB = 0, [double]$WsAbortMBPerMin = 0, [double]$WsAbortWsMB = 0,
      # C5 commit guard: STOP at this percent of the commit limit (0 = off); a MEMORY STOP like the floor (back end first).
      [double]$StopCommitPct = 0,
      # Amendment C3 (D239): the back end's --notifyLevel (4 = our default; 2 = the vendor default), and -NoWatch (no
      # WatchVrf observer). Under -NoWatch there is no IDLE end (no POS); the run ends only on a STOP or the operator,
      # and a WS that stays flat for -FlatSec (max - min < -FlatMB) is reported once as ANSWERED and the run continues.
      [int]$NotifyLevel = 4, [switch]$NoWatch, [int]$FlatSec = 600, [double]$FlatMB = 100)
$ErrorActionPreference = 'Continue'
$R = $Root
Set-Location $R
$d = "runs\launch52\stp1296\$Tag"; New-Item -ItemType Directory -Force $d | Out-Null
$ts = { (Get-Date).ToUniversalTime().ToString('HH:mm:ss') }
$bin64 = 'C:\MAK\vrforces5.2d\bin64'
$appData = 'C:\C2SIM\vrf-appdata-unattended\appData'
$watchExe = Join-Path $R 'tools\WatchVrf\bin\Release-5.2\net10.0\win-x64\WatchVrf.exe'
$runSimExe = Join-Path $R 'tools\RunSim\bin\Release-5.2\net10.0\win-x64\RunSim.exe'
$stopFile = Join-Path $R "$d\c13-watch.stop"
foreach ($p in $watchExe, $runSimExe, "C:\MAK\vrforces5.2d\userData\scenarios\$Scenario.scnx") {
    if (-not (Test-Path -LiteralPath $p)) { "ABORT: missing $p"; exit 2 } }
if (-not (Select-String -Path (Join-Path $R 'tools\WatchVrf\UsageText.cs') -SimpleMatch 'report-simtime' -Quiet)) { "ABORT: WatchVrf source lacks --report-simtime"; exit 2 }
if (-not ((& $watchExe --capabilities) -contains 'report-simtime')) { "ABORT: the built WatchVrf.exe lacks report-simtime - rebuild it"; exit 2 }
function CacheSize {
    # appData\cache is a JUNCTION (NewVrfAppData52.ps1:43); the S1 run read 0 files through it, so count its target
    $c = Get-Item "$appData\cache" -ErrorAction SilentlyContinue
    $cacheRoot = if ($c -and $c.LinkType -eq 'Junction' -and $c.Target) { @($c.Target)[0] } else { "$appData\cache" }
    $f = Get-ChildItem (Join-Path $cacheRoot 'vrfsim') -Recurse -File -ErrorAction SilentlyContinue
    "{0} files / {1} MB" -f @($f).Count, [math]::Round((($f | Measure-Object Length -Sum).Sum) / 1MB, 1) }

# ---- gate (amendment 2) ----
$free = [math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB, 2)
"[$(& $ts)] free memory (Available): $free GiB"
$gate = "$d\c0-gate.txt"
"gate $(& $ts) free $free GiB" | Set-Content $gate -Encoding ascii
"vmmem*: " + ((Get-Process vmmem* -ErrorAction SilentlyContinue | ForEach-Object { "pid $($_.Id) wsMB $([math]::Round($_.WorkingSet64/1MB)) privMB $([math]::Round($_.PrivateMemorySize64/1MB))" }) -join '; ') | Add-Content $gate -Encoding ascii
(docker stats --no-stream --format '{{.Name}} {{.MemUsage}} {{.CPUPerc}}' 2>&1) | Add-Content $gate -Encoding ascii
"cache before: " + (CacheSize) | Add-Content $gate -Encoding ascii
$ffPath = "$appData\settings\vrfSim\fastForwardSettings.mtl"
$ff = if (Test-Path -LiteralPath $ffPath) { Get-Content $ffPath -Raw } else { $null }
"fastForwardSettings: " + $(if ($null -eq $ff) { 'ABSENT (no file, so no entry)' } else { $ff -replace '\s+', ' ' }) | Add-Content $gate -Encoding ascii
Get-Content $gate
. (Join-Path $PSScriptRoot 'ff_check.ps1')   # S1-1: ignore ';' comment lines (the shipped file documents the syntax in them)
if (Test-FastForwardEntry $ff) { "ABORT: fastForwardSettings.mtl holds a LIVE entry (it can switch the frame mode)"; exit 4 }
if ($free -lt $GateGiB) { "ABORT: free memory $free GiB < $GateGiB (launch gate; amendment 2 / C2)"; exit 4 }
if (Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue) { "ABORT: VR-Forces or an interface is already running"; exit 2 }
$sysStart = Get-Date
$sys = Start-Process pwsh -ArgumentList @('-NoProfile','-File',(Join-Path $PSScriptRoot 'sys_sampler.ps1'),
    '-Out',"$R\$d\c12-sys.csv",'-SnapDir',"$R\$d\c12-snap",'-Seconds',"$InstrumentSec",'-StopGiB',"$StopGiB",'-StopCommitPct',"$StopCommitPct",
    '-WarnGiB',"$($StopGiB + 1.0)") -RedirectStandardOutput "$d\c12-sys.out" -PassThru -WindowStyle Hidden
$null = $sys.Handle
$smp = $null; $watch = $null; $bePid = 0; $beStart = $null; $launchUtc = (Get-Date).ToUniversalTime()
function Get-VendorLog {
    # the back end's own log: newest *<pid>.log written since this launch (an older same-pid log can exist)
    if ($script:bePid -le 0) { return $null }
    Get-ChildItem C:\MAK\logs -Filter "*$($script:bePid).log" -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge $script:launchUtc } | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
}
function Get-LiveLogLength {
    # The vendor log's LIVE length, by opening the file (shared read). An open file's directory-entry LastWriteTime and
    # Length can lag on NTFS (S2's c16: a constant 16:16:05.603 during the run, 16:16:54.547 after the STOP), so the
    # directory entry is no freshness measure (amendment C4). -1 when the file cannot be opened.
    $w = Get-VendorLog
    if (-not $w) { return -1 }
    try { $fs = [System.IO.File]::Open($w.FullName, 'Open', 'Read', 'ReadWrite'); try { $fs.Length } finally { $fs.Dispose() } } catch { -1 }
}
function Show-Threads {
    if ($script:bePid -le 0) { return }   # pid 0 is the System Idle process, never ours
    $b = Get-Process -Id $script:bePid -ErrorAction SilentlyContinue
    if (-not $b) { return }
    # C3 fix (S1 console, rtk_run.ps1:89 then): the old pipeline form produced concatenated values and InvalidArgument;
    # an explicit loop over a typed dictionary, one double per thread id.
    $before = New-Object 'System.Collections.Generic.Dictionary[int,double]'
    foreach ($th in @($b.Threads)) { try { $before[[int]$th.Id] = [double]$th.TotalProcessorTime.TotalSeconds } catch {} }
    Start-Sleep 5; $b = Get-Process -Id $b.Id -ErrorAction SilentlyContinue
    if (-not $b) { return }
    $deltas = New-Object 'System.Collections.Generic.List[double]'
    foreach ($th in @($b.Threads)) { try { $id = [int]$th.Id; if ($before.ContainsKey($id)) { $deltas.Add([double]$th.TotalProcessorTime.TotalSeconds - $before[$id]) } } catch {} }
    $top = @($deltas | Sort-Object -Descending | Select-Object -First 3 | ForEach-Object { '{0:F2}' -f $_ })
    "   top threads (cpu s / 5 s): " + ($top -join ', ') + "; ws MB " + [math]::Round($b.WorkingSet64 / 1MB)
}
function Show-LogCounts([string]$label) {
    # Vendor log: COUNTS ONLY, read in place, never copied (it holds the environment in clear text).
    $w = Get-VendorLog
    if (-not $w) { "   vendor log ($label): not found"; return }
    $q = @(Select-String -Path $w.FullName -Pattern 'queued for ([0-9.]+) seconds' | ForEach-Object { [double]$_.Matches[0].Groups[1].Value } | Sort-Object)
    $pct = { param($p) if ($q.Count) { $q[[math]::Min($q.Count - 1, [math]::Floor($p * $q.Count))] } else { 'n/a' } }
    $n = 0; foreach ($l in [System.IO.File]::ReadLines($w.FullName)) { $n++ }
    "   vendor log ($label): last write " + $w.LastWriteTimeUtc.ToString('HH:mm:ss.fff') + "; lines $n" +
        "; feature_source " + (Select-String -Path $w.FullName -SimpleMatch 'feature_source').Count +
        "; queued-for n " + $q.Count + " median " + (& $pct 0.5) + " p95 " + (& $pct 0.95)
}
function Get-WsSince([datetime]$fromUtc) {
    # (time, wsMB) rows of ws_sampler2's CSV at or after $fromUtc (HH:mm:ss, same UTC day as T0)
    # (midnight wrap NOT handled: a run that crosses 00:00Z loses rows after it here - declared, PREREG sec 0e)
    $from = $fromUtc.ToString('HH:mm:ss')
    @(Import-Csv (Join-Path $R "$d\c11-sampler.csv") -ErrorAction SilentlyContinue | Where-Object { $_.tUtc -ge $from } | ForEach-Object { [pscustomobject]@{ t = $_.tUtc; ws = [double]$_.wsMB } })
}
function Get-Slope($rows) {
    # MB/min from the first and last rows, by their own HH:mm:ss stamps
    if ($rows.Count -lt 2) { return 'n/a' }
    $dt = ([datetime]::ParseExact($rows[-1].t, 'HH:mm:ss', $null) - [datetime]::ParseExact($rows[0].t, 'HH:mm:ss', $null)).TotalMinutes
    if ($dt -le 0) { return 'n/a' }; [math]::Round(($rows[-1].ws - $rows[0].ws) / $dt)
}
$slopeDone = $false; $readyUtc = $null; $t0Utc = $null
function Report-Slopes {
    # C3 READING: printed once - at T0 + 50 s from the loop, or from Stop-Run if a STOP comes first (S1 stopped at
    # T0 + 49 s). The pre-stated window is T0 -> T0 + 45 s (inside S1's 49 s), beside the 60 s before T0 and READY -> T0.
    if ($script:slopeDone -or $null -eq $script:t0Utc) { return }
    $script:slopeDone = $true
    $t0s = $script:t0Utc.ToString('HH:mm:ss')
    $a45 = @(Get-WsSince $script:t0Utc | Where-Object { $_.t -le $script:t0Utc.AddSeconds(45).ToString('HH:mm:ss') })
    $pre = @(Get-WsSince $script:t0Utc.AddSeconds(-60) | Where-Object { $_.t -lt $t0s })
    $rdy = if ($script:readyUtc) { @(Get-WsSince $script:readyUtc | Where-Object { $_.t -le $t0s }) } else { @() }
    "[$(& $ts)] READING (C3): WS slope T0 -> T0+45 s = $(Get-Slope $a45) MB/min ($($a45.Count) samples); 60 s before T0 = $(Get-Slope $pre) MB/min; READY -> T0 = $(Get-Slope $rdy) MB/min ($($rdy.Count) samples)" |
        Tee-Object -FilePath (Join-Path $R "$d\c15-checkpoints.txt") -Append
}
function Stop-Run([string]$why, [switch]$Memory) {
    "[$(& $ts)] STOP: $why"
    $b = Get-Process vrfSimHLA1516e -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($Memory -and $b -and $script:bePid -gt 0) {
        # Amendment C3 (D239): a MEMORY STOP forces our OWN back end AT ONCE, with no graceful attempt (S1 lived 35 s
        # past the STOP). Identity = pid + start time via RunnerLib's Test-OwnBackendIdentity, the same check
        # StopVrf52 makes before its own force; protected processes (rti*, RtiProbe, vrfGui) are refused by it.
        # RunnerLib is dot-sourced in a CHILD scope: it sets Set-StrictMode -Version Latest (RunnerLib.ps1:13), which
        # must not leak into the rest of Stop-Run (Fable C3 item 3).
        $id = & { . (Join-Path $R 'scripts\RunnerLib.ps1'); Test-OwnBackendIdentity -ExpectedPid $script:bePid -ExpectedStartUtc $script:beStart -ActualPid $b.Id -ActualStartUtc $b.StartTime.ToUniversalTime() -ActualName $b.ProcessName }
        if ($id.Match) { Stop-Process -Id $b.Id -Force; "[$(& $ts)] FORCED own back end pid $($b.Id) at once (memory STOP, D239)" }
        else { "[$(& $ts)] NOT forced at once: $($id.Why) - falling back to StopVrf52" }
        Start-Sleep 2
        $b = Get-Process -Id $b.Id -ErrorAction SilentlyContinue
    }
    Report-Slopes
    $w = Get-VendorLog; if ($w) { "   vendor log last write BEFORE the stop: " + $w.LastWriteTimeUtc.ToString('HH:mm:ss.fff') }
    Show-Threads
    # the force pair is the RECORDED identity ($script:bePid / $script:beStart), not the live process's own (Fable C3 item 9)
    if ($b -and -not ($script:bePid -gt 0 -and $script:beStart)) { pwsh -NoProfile -File scripts\StopVrf52.ps1 -TimeoutSec 15 -GraceSec 1 *> "$d\c10-force.log"; "[$(& $ts)] back end stop (no recorded identity, no force) exit $LASTEXITCODE" }
    elseif ($b) { pwsh -NoProfile -File scripts\StopVrf52.ps1 -TimeoutSec 15 -GraceSec 1 -ForceOwnBackendPid $script:bePid -ForceOwnBackendStartUtc $script:beStart.ToString('o') *> "$d\c10-force.log"; "[$(& $ts)] back end stop exit $LASTEXITCODE" }
    else { "[$(& $ts)] no back end running" }
    New-Item -ItemType File -Force $stopFile | Out-Null
    Start-Sleep 5
    foreach ($p in $script:smp, $script:watch, $sys) { if ($p -and -not $p.HasExited) { try { Stop-Process -Id $p.Id -ErrorAction SilentlyContinue } catch {} } }
    "   left running: " + ((Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue | ForEach-Object { "$($_.ProcessName) $($_.Id)" }) -join ', ')
    "   cache after: " + (CacheSize)
    Show-LogCounts 'after the stop'
    exit 7
}
function Stop-Check {
    if (Test-Path "$d\c12-STOP.txt") { Stop-Run ("MEMORY STOP - " + ((Get-Content "$d\c12-STOP.txt") -join ' ')) -Memory }
    if ($sys.HasExited) { Stop-Run 'the system sampler ended without a STOP marker - memory is unwatched' }
    if ($script:smp -and $script:smp.HasExited -and (Select-String -Path "$d\c11-sampler.out" -Pattern '^ABORT' -Quiet -ErrorAction SilentlyContinue)) {
        Stop-Run ("back-end sampler " + ((Select-String -Path "$d\c11-sampler.out" -Pattern '^ABORT' | Select-Object -First 1).Line)) -Memory }
}

# ---- launch: the vendor scenario, its own .scn (terrain, SMS, variable-frame 0.1 s, its saved time multiplier) ----
$launch = Start-Process pwsh -ArgumentList @('-NoProfile','-File','scripts\LaunchVrf52.ps1','-Scenario',$Scenario,
    '-FederationHoldAppNumber',"$AppBase",'-FederationHoldSecs',"$InstrumentSec",'-BackendAppNumber',"$($AppBase+2)",'-NoGui',
    '-NotifyLevel',"$NotifyLevel",'-ReadyTimeoutSec','240','-ScenarioLoadTimeoutSec','420','-AppDataDir',$appData) -RedirectStandardOutput "$d\c2-launchvrf52.log" -RedirectStandardError "$d\c2-launchvrf52.err" -PassThru -WindowStyle Hidden
$deadline = (Get-Date).AddSeconds(900)   # ReadyTimeoutSec 240 + ScenarioLoadTimeoutSec 420 + the holder join
while ((Get-Date) -lt $deadline) { Stop-Check; if (Select-String -Path "$d\c2-launchvrf52.log" -Pattern 'READY: 5.2d|\[FAIL\]' -Quiet -ErrorAction SilentlyContinue) { break }; Start-Sleep 1 }
"[$(& $ts)] launch: " + ((Select-String -Path "$d\c2-launchvrf52.log" -Pattern 'READY: 5.2d|\[FAIL\]|loaded scenario' | Select-Object -First 3 | ForEach-Object { $_.Line.Trim().Substring(0,[Math]::Min(90,$_.Line.Trim().Length)) }) -join ' | ')
if (-not (Select-String -Path "$d\c2-launchvrf52.log" -Pattern 'READY: 5.2d' -Quiet)) { Stop-Run 'ABORT: VR-Forces not READY' }
# READY alone is not a load: LaunchVrf52 prints READY after a failed load or a load timeout too (:1440-1446, :1469).
if (-not (Select-String -Path "$d\c2-launchvrf52.log" -SimpleMatch 'scenario LOAD CONFIRMED' -Quiet)) { Stop-Run 'ABORT: the scenario load was not confirmed (LaunchVrf52.ps1:1445)' }
$be = Get-Process vrfSimHLA1516e -ErrorAction SilentlyContinue | Select-Object -First 1
if ($be) { $bePid = $be.Id; $beStart = $be.StartTime.ToUniversalTime(); "[$(& $ts)] back end pid $bePid" }
$script:readyUtc = (Get-Date).ToUniversalTime()   # C3: READY -> T0 is kept as in S1; its WS slope is reported
$smp = Start-Process pwsh -ArgumentList @('-NoProfile','-File',(Join-Path $PSScriptRoot '..\handoff-2026-10-15\ws_sampler2.ps1'),
    '-Out',"$R\$d\c11-sampler.csv",'-Seconds',"$InstrumentSec",'-AbortMBPerMin',"$WsAbortMBPerMin",'-WindowSec','300','-AbortWsMB',"$WsAbortWsMB") -RedirectStandardOutput "$d\c11-sampler.out" -PassThru -WindowStyle Hidden

# ---- tool env (5.2: VR-Forces 5.2d, VR-Link 5.10, MAK RTI 5.0.1 rtiexec posture; StartInterface52.ps1 :8-12) ----
$env:PATH = "$bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin;" + $env:PATH
$env:MAK_VRFDIR = 'C:\MAK\vrforces5.2d'; $env:MAK_VRLDIR = 'C:\MAK\vrlink5.10'; $env:MAK_RTIDIR = 'C:\MAK\makRti5.0.1'
$env:RTI_RID_FILE = Join-Path $R 'config\rid-501-rtiexec-min.mtl'; $env:RTI_ASSISTANT_DISABLE = '1'
$lic = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','User')
if (-not $lic) { $lic = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine') }
if ($lic -and (Test-Path -LiteralPath $lic -PathType Leaf)) { $env:MAKLMGRD_LICENSE_FILE = $lic }

if (-not $NoWatch) {
# ---- observer first (sim clock + back-end count + positions + task completions), then the clock ----
Remove-Item $stopFile -ErrorAction SilentlyContinue
$watch = Start-Process $watchExe -ArgumentList @("$($AppBase+4)", "$InstrumentSec", '10', '""', '--stop-file', $stopFile,
    '--report-backends', '--report-simtime') -WorkingDirectory $bin64 -RedirectStandardOutput "$d\c13-watch.txt" -RedirectStandardError "$d\c13-watch.err" -PassThru -WindowStyle Hidden
$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) { Stop-Check; if (Select-String -Path "$d\c13-watch.txt" -Pattern '^# t=' -Quiet -ErrorAction SilentlyContinue) { break }; Start-Sleep 3 }
"[$(& $ts)] observer: " + ((Select-String -Path "$d\c13-watch.txt" -Pattern '^# t=' | Select-Object -First 1).Line)
} else { "[$(& $ts)] -NoWatch: no observer (amendment C3)" }
$rsArgs = @($AppBase + 3); if ($Multiplier -gt 1) { $rsArgs += $Multiplier }
Push-Location $bin64; & $runSimExe @rsArgs *> (Join-Path $R "$d\c14-runsim.txt"); $rse = $LASTEXITCODE; Pop-Location
$t0 = Get-Date
if ($rse -ne 0) { Stop-Run "RunSim exit $rse - run() was not sent, the clock did not start (tools/RunSim/Program.cs:143-152)" }
"[$(& $ts)] T0 (RunSim exit $rse): " + ((Get-Content "$d\c14-runsim.txt" | Select-Object -Last 2) -join ' | ')

# ---- no fixed window, no ceiling: run until IDLE, a safety STOP, or the operator's end file; checkpoints report ----
# Idle test (amendment C, Fable fixes): every -IdleEndSec, the latest POS of every GROUND object is compared with the
# previous snapshot. GROUND = NOT in -NonGroundUuidFile (the B5 DIS domains; amendment C1). The altitude cut
# (-AirFloorM) is only the fallback without that file - REFUTED for PhaseTwo by B5 (air routes down to 118.5 m; sec 0d).
# The scoring classification is the .oob DIS domain (B3c), not this filter. IDLE is declared ONLY if the instruments
# are alive over the interval: the observer process runs, the back end process runs, new '# t=' samples arrived,
# backends= reads 1, sim= advanced, AND the back end's own log was written within the interval. A back end that
# freezes in place keeps backends=1 (BackendCount keeps a timed-out back end; VrfFacade.h:392-399) and its sim=
# still advances at the controller's 1x extrapolation (D36 (a)), so the vendor log's freshness is the liveness
# test, and a clock that falls from >= 13.5x to about 1x is a STOP too. Otherwise it is a STOP, never IDLE.
$watchPath = Join-Path $R "$d\c13-watch.txt"
$posOffset = 0L; $latest = @{}; $prevSnap = $null; $endWhy = ''; $exitCode = 0
$lastT = -1.0; $lastSim = -1.0; $lastBe = -1; $firstSim = $null; $firstT = $null; $tsk = 0
$opEnd = Join-Path $R "$d\OPERATOR-END.txt"; Remove-Item $opEnd -ErrorAction SilentlyContinue
if ($watch) { $null = $watch.Handle }
function Read-NewPos {
    try {
        $fs = [System.IO.File]::Open($watchPath, 'Open', 'Read', 'ReadWrite')
        try { $null = $fs.Seek($script:posOffset, 'Begin'); $sr = New-Object System.IO.StreamReader($fs)
            while ($null -ne ($line = $sr.ReadLine())) {
                if ($line.StartsWith('POS,')) { $f = $line.Split(','); if ($f.Count -ge 6) { $script:latest[$f[2]] = @([double]$f[3], [double]$f[4], [double]$f[5]) } }
                elseif ($line.StartsWith('# t=')) {
                    if ($line -match '# t=([0-9.]+)s') { $script:lastT = [double]$Matches[1] }
                    if ($line -match ' backends=(-?\d+)') { $script:lastBe = [int]$Matches[1] }
                    if ($line -match ' sim=(-?[0-9.]+)') { $script:lastSim = [double]$Matches[1]; if ($null -eq $script:firstSim -and $script:lastSim -ge 0) { $script:firstSim = $script:lastSim; $script:firstT = $script:lastT } } }
                elseif ($line.StartsWith('TSK,')) { $script:tsk++ } }
            $script:posOffset = $fs.Position } finally { $fs.Dispose() }
    } catch {}
}
$nonGround = @{}
if ($NonGroundUuidFile) {
    if (-not (Test-Path -LiteralPath $NonGroundUuidFile)) { Stop-Run "ABORT: -NonGroundUuidFile not found: $NonGroundUuidFile" }
    $ln = 0
    foreach ($l in Get-Content -LiteralPath $NonGroundUuidFile) { $ln++
        if ($l.Trim() -eq '') { continue }
        $h = (($l.Trim() -replace '^VRF_UUID:', '') -replace '[^0-9A-Fa-f]', '').ToLowerInvariant()
        if ($h.Length -ne 32) { Stop-Run "ABORT: -NonGroundUuidFile line $ln is not one uuid (32 hex digits): '$l'" }
        $nonGround[$h] = $true }
    "[$(& $ts)] idle test: ground = NOT in the $($nonGround.Count) non-ground uuids of $NonGroundUuidFile (B5 domains)"
} else { "[$(& $ts)] idle test: ground = POS altitude <= $AirFloorM m (no -NonGroundUuidFile)" }
function Test-Ground($k, $alt) {
    if ($script:nonGround.Count -gt 0) { $h = ($k -replace '[^0-9A-Fa-f]', '').ToLowerInvariant()
        foreach ($g in $script:nonGround.Keys) { if ($h.Contains($g)) { return $false } }; return $true }
    return ($alt -le $AirFloorM)
}
function Get-MaxMove($a, $b) {
    $m = 0.0
    foreach ($k in $b.Keys) { if ($a.ContainsKey($k) -and (Test-Ground $k $b[$k][2]) -and (Test-Ground $k $a[$k][2])) {
        $dLat = ($b[$k][0] - $a[$k][0]) * 110574.0; $dLon = ($b[$k][1] - $a[$k][1]) * 111320.0 * [math]::Cos($b[$k][0] * [math]::PI / 180)
        $dd = [math]::Sqrt($dLat * $dLat + $dLon * $dLon); if ($dd -gt $m) { $m = $dd } } }
    $m
}
$nextIdle = $t0.AddSeconds($IdleEndSec); $nextCp = $t0.AddSeconds($CheckpointSec); $warned = $false
$instEnd = $sysStart.AddSeconds($InstrumentSec)   # the earliest-started instrument with the shortest life ends first
$pT = -1.0; $pSim = -1.0; $sawFull = $false; $pLen = -1
$flatDone = $false; $script:t0Utc = $t0.ToUniversalTime(); $nextVlog = Get-Date; $nextFlat = $t0.AddSeconds($FlatSec)
"nowUtc,vendorLogLastWriteUtc_dirEntry,vendorLogBytes_dirEntry,vendorLogBytes_live" | Set-Content (Join-Path $R "$d\c16-vlog.csv") -Encoding ascii
while ($true) {
    Stop-Check; Start-Sleep 1   # C5 (Fable): the STOP marker is polled every 1 s, not 5 s
    if ((Get-Date) -ge $t0.AddSeconds(50)) { Report-Slopes }
    if ((Get-Date) -ge $nextVlog) { $nextVlog = (Get-Date).AddSeconds(5)
        # C3/C4: a vendor-log freshness sample every 5 s; the LIVE length (file opened) is the measure, the directory
        # entry's stamp and length are kept beside it to show the lag (S1's "24 s before T0" is a suspected artifact)
        $vl = Get-VendorLog
        "$((Get-Date).ToUniversalTime().ToString('HH:mm:ss')),$(if ($vl) { $vl.LastWriteTimeUtc.ToString('HH:mm:ss.fff') } else { '' }),$(if ($vl) { $vl.Length } else { '' }),$(Get-LiveLogLength)" |
            Add-Content (Join-Path $R "$d\c16-vlog.csv") -Encoding ascii }
    if ($NoWatch) {
        if (-not (Get-Process -Id $bePid -ErrorAction SilentlyContinue)) { Stop-Run 'the back end process is gone' }
        if (-not $flatDone -and (Get-Date) -ge $nextFlat) { $nextFlat = (Get-Date).AddSeconds(60)   # rate-limited (Fable C3 item 7)
            $from = (Get-Date).ToUniversalTime().AddSeconds(-$FlatSec).ToString('HH:mm:ss')
            $rows = @(Import-Csv (Join-Path $R "$d\c11-sampler.csv") -ErrorAction SilentlyContinue | Where-Object { $_.tUtc -ge $from })
            if ($rows.Count -ge [math]::Floor($FlatSec * 0.8)) {
                # C5 (Fable): PLATEAU needs the WS AND the private bytes flat - under pressure the OS trims the WS while
                # private bytes keep climbing, so a flat WS alone could be an artefact
                $span = ($rows | ForEach-Object { [double]$_.wsMB } | Measure-Object -Maximum -Minimum)
                $pspan = ($rows | ForEach-Object { [double]$_.privMB } | Measure-Object -Maximum -Minimum)
                $dd = [math]::Max($span.Maximum - $span.Minimum, $pspan.Maximum - $pspan.Minimum)
                $cpu = ($rows | Where-Object { $_.cpuCores -ne '' } | ForEach-Object { [double]$_.cpuCores } | Measure-Object -Average).Average
                if ($dd -lt $FlatMB) {
                    # liveness (Fable C3 item 4): a frozen back end is also flat; the back end's own CPU is the test. (The
                    # first text said the vendor log "stopped 24 s before T0" in S1 - a SUSPECTED INSTRUMENT ARTIFACT of the
                    # directory entry, amendment C4.)
                    if ($cpu -ge 0.2) { $flatDone = $true
                        "[$(& $ts)] ANSWERED / PLATEAU (C3/C5): back-end WS level $([math]::Round($span.Maximum)) MB, private $([math]::Round($pspan.Maximum)) MB; WS and private bytes flat over the last $FlatSec s (max - min $([math]::Round($dd)) MB < $FlatMB) with the back end busy (mean $([math]::Round($cpu, 2)) cores); the run continues as observation until the operator ends it" |
                            Tee-Object -FilePath (Join-Path $R "$d\c15-checkpoints.txt") -Append }
                    else { "[$(& $ts)] FLAT but the back end is near idle (mean $([math]::Round($cpu, 2)) cores) - NOT answered (could be hung)" |
                            Tee-Object -FilePath (Join-Path $R "$d\c15-checkpoints.txt") -Append } } } }
        if (Test-Path $opEnd) { $endWhy = 'OPERATOR END (' + ((Get-Content $opEnd -ErrorAction SilentlyContinue) -join ' ') + ')'; $exitCode = 8; break }
        continue
    }
    if (Test-Path $opEnd) { $endWhy = 'OPERATOR END (' + ((Get-Content $opEnd -ErrorAction SilentlyContinue) -join ' ') + ')'; $exitCode = 8; break }
    if (-not $warned -and (Get-Date) -ge $instEnd.AddSeconds(-7200)) { $warned = $true
        "[$(& $ts)] WARNING: the instruments end at $($instEnd.ToUniversalTime().ToString('HH:mm:ss'))Z (2 h from now); the system sampler ending is a STOP" | Tee-Object -FilePath (Join-Path $R "$d\c15-checkpoints.txt") -Append }
    if ((Get-Date) -ge $nextCp) {
        $cw = Get-VendorLog; $cb = Get-Process -Id $bePid -ErrorAction SilentlyContinue
        "[$(& $ts)] CHECKPOINT (running; only the operator ends it here, via $opEnd): T0 + $([math]::Round(((Get-Date) - $t0).TotalHours, 2)) h; " +
            "objects $($latest.Count); sim $lastSim; backends $lastBe; TSK $tsk; back end ws MB $(if ($cb) { [math]::Round($cb.WorkingSet64/1MB) } else { 'gone' }); vendor log last write $(if ($cw) { $cw.LastWriteTimeUtc.ToString('HH:mm:ss') } else { 'n/a' })" |
            Tee-Object -FilePath (Join-Path $R "$d\c15-checkpoints.txt") -Append
        $nextCp = (Get-Date).AddSeconds($CheckpointSec)
    }
    if ((Get-Date) -ge $nextIdle) {
        Read-NewPos
        if ($watch.HasExited) { Stop-Run 'the observer (WatchVrf) exited - movement is unwatched' }
        if (-not (Get-Process -Id $bePid -ErrorAction SilentlyContinue)) { Stop-Run 'the back end process is gone' }
        if ($lastBe -eq 0) { Stop-Run 'BACK END LOST: WatchVrf backends=0 (Q1)' }
        if ($lastT -le $pT) { Stop-Run "no new observer sample since t=$pT s - movement is unwatched" }
        if ($lastSim -le $pSim) { Stop-Run "the sim clock did not advance (sim=$lastSim, previous $pSim)" }
        $lw = Get-VendorLog; $liveLen = Get-LiveLogLength
        # C4: freshness = the LIVE length grew since the previous check (the directory entry can lag on an open file)
        if ($liveLen -lt 0 -or ($pLen -ge 0 -and $liveLen -le $pLen)) {
            Stop-Run "back end silent: its vendor log did not grow in the last $IdleEndSec s (live length $liveLen, previous $pLen)" }
        $pLen = $liveLen
        $iRatio = if ($pT -ge 0 -and $lastT -gt $pT) { [math]::Round(($lastSim - $pSim) / ($lastT - $pT), 2) } else { -1 }
        if ($iRatio -ge 0 -and $iRatio -ge 13.5) { $sawFull = $true }
        if ($sawFull -and $iRatio -ge 0 -and $iRatio -lt 1.5) { Stop-Run "the clock fell from >= 13.5x to $iRatio x - the frozen-back-end signature (D36 (a))" }
        $snap = @{}; foreach ($k in $latest.Keys) { $snap[$k] = $latest[$k] }
        if ($prevSnap) { $mm = Get-MaxMove $prevSnap $snap
            $gnd = @($snap.Keys | Where-Object { Test-Ground $_ $snap[$_][2] }).Count
            "[$(& $ts)] idle check: objects $($snap.Count) (ground $gnd, non-ground $($snap.Count - $gnd)), max ground move over the last $IdleEndSec s = $([math]::Round($mm)) m; t=$lastT sim=$lastSim; interval SIM/WALL $iRatio; vendor log $($lw.LastWriteTimeUtc.ToString('HH:mm:ss'))Z"
            $ngSeen = $snap.Count - $gnd
            # Amendment C1 guard: IDLE needs at least one ground object, and (with the file) the file's uuids must be seen
            # among the reflected objects - otherwise "nothing moved" could mean "nothing was counted".
            $classOk = ($gnd -ge 1) -and ((-not $NonGroundUuidFile) -or ($ngSeen -ge 1))
            if (-not $classOk) { "[$(& $ts)]   idle not judged: ground $gnd, non-ground seen $ngSeen of $($nonGround.Count) in the file - the classification is not usable" }
            elseif ($mm -lt $IdleMeters) { $endWhy = "IDLE (no ground object moved > $IdleMeters m in $IdleEndSec s, instruments alive; ground $gnd, non-ground $ngSeen)"; $exitCode = 0; break } }
        $prevSnap = $snap; $pT = $lastT; $pSim = $lastSim; $nextIdle = (Get-Date).AddSeconds($IdleEndSec)
    }
}
$ratio = if ($null -ne $firstSim -and $lastT -gt $firstT) { [math]::Round(($lastSim - $firstSim) / ($lastT - $firstT), 2) } else { 'n/a' }
$mult = if ($ratio -eq 'n/a') { 'n/a' } elseif ($ratio -lt 1.5) { 'NOT APPLIED (about 1x: the saved multiplier did not take effect)' } elseif ($ratio -lt 13.5) { 'LOAD SHORTFALL (below 13.5)' } else { 'at the saved multiplier' }
"[$(& $ts)] END $endWhy; exit $exitCode; mean SIM/WALL $ratio ($mult); last sim $lastSim; TSK lines $tsk" | Tee-Object -FilePath (Join-Path $R "$d\c15-checkpoints.txt") -Append
$w = Get-VendorLog
"[$(& $ts)] run end ($endWhy); vendor log last write AT THE END (Q1): " + $(if ($w) { $w.LastWriteTimeUtc.ToString('HH:mm:ss.fff') } else { 'not found' })
Show-Threads
$vb = if ($bePid -gt 0) { Get-Process -Id $bePid -ErrorAction SilentlyContinue } else { $null }

# ---- teardown: observer resigns, then the back end ----
New-Item -ItemType File -Force $stopFile | Out-Null
if ($watch) { $watch.WaitForExit(30000) | Out-Null }
if ($vb) { pwsh -NoProfile -File scripts\StopVrf52.ps1 -ForceOwnBackendPid $vb.Id -ForceOwnBackendStartUtc $beStart.ToString('o') *> "$d\c9-stopvrf52.log"; "[$(& $ts)] StopVrf: $LASTEXITCODE" }
foreach ($p in $smp, $watch, $sys) { if ($p -and -not $p.HasExited) { try { Stop-Process -Id $p.Id -ErrorAction SilentlyContinue } catch {} } }
"   cache after: " + (CacheSize)
Show-LogCounts 'after the close'
"   left running: " + ((Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue | ForEach-Object { "$($_.ProcessName) $($_.Id)" }) -join ', ')
"   RTI: " + ((Get-Process rtiexec, rtiForwarder -ErrorAction SilentlyContinue | ForEach-Object { "$($_.ProcessName) $($_.Id)" }) -join ', ') + "; holder app $AppBase"
exit $exitCode
