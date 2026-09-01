# tests/RunnerTurnaround.Tests.ps1 - OFFLINE regression check for the runner's
# turnaround logic (docs/RUNNER_TURNAROUND_2026-09-01.md). No simulator, no server,
# no process start. Plain pwsh (no Pester dependency): exits 0 when every check
# passes, 1 otherwise, and prints one line per check.
#
#   pwsh -NoProfile -File tests\RunnerTurnaround.Tests.ps1
#
# What it pins down:
#   1. the observer duration CAP formula (unchanged from the pre-turnaround runner)
#   2. order parsing: (Task, PerformingEntity) pairs out of the R9 order file
#   3. TASKCMPLT parsing out of real interface-log lines (runs 20260901T211310Z,
#      20260901T221227Z)
#   4. the early-exit decision: 2/3 taskees never closes; 3/3 closes only after the
#      settle hold; zero taskees never closes; line count below task count blocks
#   5. the trace stop-file timing (StopIface + trail, never negative)
#   6. the --capabilities probe parse (exit 0 AND token present)
#   7. both PowerShell files parse with zero errors
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $RepoRoot 'scripts\RunnerLib.ps1')

$script:Pass = 0
$script:Fail = 0
function Check {
    param([string]$Name, [bool]$Condition, [string]$Detail = '')
    if ($Condition) { $script:Pass++; Write-Host ('  [PASS] ' + $Name) }
    else            { $script:Fail++; Write-Host ('  [FAIL] ' + $Name + $(if ($Detail) { ' -- ' + $Detail } else { '' })) }
}

Write-Host '=== 1. observer duration cap (Get-DerivedWatchSecs) ==='
$d900 = Get-DerivedWatchSecs -PreRollSecs 20 -AppJoinTimeoutSec 180 -InitDispatchWaitSec 120 -OracleGateTimeoutSec 180 -PushOrderListenSec 30 -RunSecs 900 -TrailSecs 30
$d420 = Get-DerivedWatchSecs -PreRollSecs 20 -AppJoinTimeoutSec 180 -InitDispatchWaitSec 120 -OracleGateTimeoutSec 180 -PushOrderListenSec 30 -RunSecs 420 -TrailSecs 30
$d600 = Get-DerivedWatchSecs -PreRollSecs 20 -AppJoinTimeoutSec 180 -InitDispatchWaitSec 120 -OracleGateTimeoutSec 180 -PushOrderListenSec 30 -RunSecs 600 -TrailSecs 30
Check 'defaults + RunSecs 900 -> 1460 (manifest 20260901T211310Z watchSecs)' ($d900 -eq 1460) "got $d900"
Check 'defaults + RunSecs 420 -> 980 (manifest 20260901T221227Z watchSecs)'  ($d420 -eq 980)  "got $d420"
Check 'defaults + RunSecs 600 -> 1160 (runner default)'                      ($d600 -eq 1160) "got $d600"

Write-Host '=== 2. order parsing (Get-OrderTasks / Get-OrderTaskees) ==='
$orderPath = Join-Path $RepoRoot 'data\R9_Mojave_UnitMove_Order.xml'
$orderText = Get-Content -LiteralPath $orderPath -Raw
$tasks   = @(Get-OrderTasks   -OrderText $orderText)
$taskees = @(Get-OrderTaskees -OrderText $orderText)
$expectTaskees = @('001aa71b-4c26-a1ea-28b2-f7dfe8e76342','139aa71b-75df-4888-4a5a-6056bae66242','670cfdb2-6c43-f267-ad7f-bd6e739def24')
Check 'R9 order yields 3 (Task, PerformingEntity) pairs' ($tasks.Count -eq 3) "got $($tasks.Count)"
Check 'R9 order yields the 3 known taskee UUIDs' (@(Compare-Object $taskees $expectTaskees).Count -eq 0) ("got " + ($taskees -join ','))
Check 'R9 task UUIDs are the a5000000-...-0001/2/3 task ids, not location/route ids' (
    @($tasks | Where-Object { $_.TaskUuid -match '^a5000000-0000-0000-0000-00000000000[123]$' }).Count -eq 3) ("got " + (($tasks | ForEach-Object { $_.TaskUuid }) -join ','))
Check 'unparseable XML yields an empty array, not a throw' (@(Get-OrderTasks -OrderText '<not xml').Count -eq 0)
Check 'empty text yields an empty array' (@(Get-OrderTaskees -OrderText '').Count -eq 0)
# Unwrapped form (UUID/PerformingEntity as direct children of Task) must parse too,
# and a UUID buried deeper than the wrapper must NOT be picked up as the task's.
$synthetic = @'
<Order xmlns="http://www.sisostds.org/schemas/C2SIM/1.1">
  <Task><UUID>11111111-1111-1111-1111-111111111111</UUID><PerformingEntity>aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa</PerformingEntity></Task>
  <Task><ManeuverWarfareTask><Location><Deep><UUID>99999999-9999-9999-9999-999999999999</UUID></Deep></Location>
    <UUID>22222222-2222-2222-2222-222222222222</UUID><PerformingEntity>aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa</PerformingEntity></ManeuverWarfareTask></Task>
</Order>
'@
$syn = @(Get-OrderTasks -OrderText $synthetic)
Check 'synthetic: 2 tasks, 1 distinct taskee (two tasks for one performer)' ($syn.Count -eq 2 -and @(Get-OrderTaskees -OrderText $synthetic).Count -eq 1) "got $($syn.Count) tasks"
Check 'synthetic: deeper UUID not mistaken for the task UUID' (($syn | ForEach-Object { $_.TaskUuid }) -notcontains '99999999-9999-9999-9999-999999999999')

Write-Host '=== 3. TASKCMPLT parsing (Get-CompletedTasks) - real interface-log lines ==='
# Verbatim from runs/20260901T211310Z_run/vrfc2simapp.log (all three completed).
$logP2c = @'
info: VrfC2Sim[0]
      VRF task complete: 1.BdeHQ / move-along
info: VrfC2Sim[0]
      SENT TASK STATUS REPORT (TASKCMPLT) taskee=670cfdb2-6c43-f267-ad7f-bd6e739def24 task=a5000000-0000-0000-0000-000000000003.
info: VrfC2Sim[0]
      VRF task complete: 1222.MechPlt / move-along
info: VrfC2Sim[0]
      SENT TASK STATUS REPORT (TASKCMPLT) taskee=001aa71b-4c26-a1ea-28b2-f7dfe8e76342 task=a5000000-0000-0000-0000-000000000001.
info: VrfC2Sim[0]
      VRF task complete: 114.MechCoy / move-along
info: VrfC2Sim[0]
      SENT TASK STATUS REPORT (TASKCMPLT) taskee=139aa71b-75df-4888-4a5a-6056bae66242 task=a5000000-0000-0000-0000-000000000002.
info: VrfC2Sim[0]
      C2SIM server state -> INITIALIZED.
'@
# Verbatim from runs/20260901T221227Z_run/vrfc2simapp.log (company never completed).
$logP3 = @'
info: VrfC2Sim[0]
      SENT TASK STATUS REPORT (TASKCMPLT) taskee=670cfdb2-6c43-f267-ad7f-bd6e739def24 task=a5000000-0000-0000-0000-000000000003.
info: VrfC2Sim[0]
      VRF task complete: 1222.MechPlt / move-along
info: VrfC2Sim[0]
      SENT TASK STATUS REPORT (TASKCMPLT) taskee=001aa71b-4c26-a1ea-28b2-f7dfe8e76342 task=a5000000-0000-0000-0000-000000000001.
'@
$cP2c = @(Get-CompletedTasks -AppLogText $logP2c)
$cP3  = @(Get-CompletedTasks -AppLogText $logP3)
Check 'P2c log -> 3 completions' ($cP2c.Count -eq 3) "got $($cP2c.Count)"
Check 'P3 log -> 2 completions'  ($cP3.Count -eq 2)  "got $($cP3.Count)"
Check 'P2c completions carry the task uuid without the trailing period' (($cP2c | ForEach-Object { $_.Task }) -contains 'a5000000-0000-0000-0000-000000000003')
Check 'CRLF log text parses the same' (@(Get-CompletedTasks -AppLogText ($logP2c -replace "`n", "`r`n")).Count -eq 3)
Check 'task=(none) form is accepted' (@(Get-CompletedTasks -AppLogText 'SENT TASK STATUS REPORT (TASKCMPLT) taskee=670cfdb2-6c43-f267-ad7f-bd6e739def24 task=(none).').Count -eq 1)
Check 'a TASKCMPLT-looking line for a non-uuid taskee is ignored' (@(Get-CompletedTasks -AppLogText 'SENT TASK STATUS REPORT (TASKCMPLT) taskee=bogus task=x.').Count -eq 0)
Check 'empty log -> no completions' (@(Get-CompletedTasks -AppLogText '').Count -eq 0)

Write-Host '=== 4. early-exit decision (Update-CompletionState / Test-EarlyExit) ==='
$t0 = [datetime]::new(2026, 9, 1, 21, 16, 0, [System.DateTimeKind]::Utc)
$hold = 60

# P3 shape: 2 of 3 taskees complete - must NEVER close, however long it holds.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP3 -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(3600)
Check 'P3 (2/3 taskees): AllComplete is false' (-not $v.AllComplete)
Check 'P3 (2/3 taskees): ShouldClose is false even after an hour' (-not $v.ShouldClose)
Check 'P3 (2/3 taskees): Missing names the company' (@($v.Missing).Count -eq 1 -and $v.Missing[0] -eq '139aa71b-75df-4888-4a5a-6056bae66242') ("got " + ($v.Missing -join ','))

# P2c shape, arriving over successive polls: partial -> all-complete -> hold -> close.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions @() -NowUtc $t0
Check 'no completions yet: nothing seen, allCompleteUtc null' ($s.firstSeenUtc.Count -eq 0 -and $null -eq $s.allCompleteUtc)
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions @($cP2c[0]) -NowUtc $t0.AddSeconds(145)
Check 'first completion stamps firstSeenUtc for that taskee only' ($s.firstSeenUtc.Count -eq 1 -and $s.firstSeenUtc['670cfdb2-6c43-f267-ad7f-bd6e739def24'] -eq $t0.AddSeconds(145))
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions @($cP2c[0], $cP2c[1]) -NowUtc $t0.AddSeconds(160)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(160)
Check '2 of 3 seen: not all complete, no close' (-not $v.AllComplete -and -not $v.ShouldClose)
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP2c -NowUtc $t0.AddSeconds(215)
Check 'all 3 seen: allCompleteUtc stamped at the poll that first saw it (t+215)' ($s.allCompleteUtc -eq $t0.AddSeconds(215))
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(215)
Check 'all complete at t+215: AllComplete true, ShouldClose false (hold 0 of 60)' ($v.AllComplete -and -not $v.ShouldClose -and $v.HoldElapsedSecs -eq 0)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(215 + 59)
Check 'hold 59 s of 60: still open' (-not $v.ShouldClose)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(215 + 60)
Check 'hold 60 s of 60: closes' ($v.ShouldClose)
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP2c -NowUtc $t0.AddSeconds(300)
Check 'a later poll does NOT move allCompleteUtc (hold is measured from the first sighting)' ($s.allCompleteUtc -eq $t0.AddSeconds(215))
Check 'first-seen stamps are never overwritten' ($s.firstSeenUtc['670cfdb2-6c43-f267-ad7f-bd6e739def24'] -eq $t0.AddSeconds(145))

# Zero taskees (unparseable / empty order): never closes, even with completions present.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees @() -TaskCount 0 -Completions $cP2c -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees @() -SettleHoldSecs 0 -NowUtc $t0.AddSeconds(3600)
Check 'zero taskees: never all-complete, never closes' (-not $v.AllComplete -and -not $v.ShouldClose)

# Line count below task count: two tasks for one performer, one completion line so far.
$one = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees @($one) -TaskCount 2 -Completions @([pscustomobject]@{ Taskee = $one; Task = 'x' }) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees @($one) -SettleHoldSecs 0 -NowUtc $t0
Check 'one performer, two tasks, one TASKCMPLT: NOT all complete' (-not $v.AllComplete)
$s = Update-CompletionState -State $s -Taskees @($one) -TaskCount 2 -Completions @([pscustomobject]@{ Taskee = $one; Task = 'x' }, [pscustomobject]@{ Taskee = $one; Task = 'y' }) -NowUtc $t0.AddSeconds(10)
$v = Test-EarlyExit -State $s -Taskees @($one) -SettleHoldSecs 0 -NowUtc $t0.AddSeconds(10)
Check 'one performer, two tasks, two TASKCMPLT lines: all complete, closes with hold 0' ($v.AllComplete -and $v.ShouldClose)

# Hold of 0 closes on the same poll that completes.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP2c -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 0 -NowUtc $t0
Check 'settle hold 0: closes on the completing poll' ($v.ShouldClose)

# Review F2: a TASKCMPLT line for a taskee NOT in the order must not count. One order
# taskee with two tasks, ONE line of its own plus ONE stray line for a foreign unit:
# before the fix the stray satisfied the count rule and the window closed.
$stray = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees @($one) -TaskCount 2 -Completions @([pscustomobject]@{ Taskee = $one; Task = 'x' }, [pscustomobject]@{ Taskee = $stray; Task = 'z' }) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees @($one) -SettleHoldSecs 0 -NowUtc $t0
Check 'stray-taskee line: NOT counted toward the task count (1 of 2), no close' (-not $v.AllComplete -and -not $v.ShouldClose -and $s.lineCount -eq 1) "lineCount=$($s.lineCount)"
Check 'stray-taskee line: the stray is not stamped in firstSeenUtc' (-not $s.firstSeenUtc.Contains($stray) -and $s.firstSeenUtc.Contains($one))
# Only stray lines, none for the order taskee: nothing seen at all.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions @([pscustomobject]@{ Taskee = $stray; Task = 'z' }, [pscustomobject]@{ Taskee = $stray; Task = 'z' }, [pscustomobject]@{ Taskee = $stray; Task = 'z' }) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 0 -NowUtc $t0
Check 'three stray lines, zero order lines: nothing seen, Missing names all 3, no close' ($s.firstSeenUtc.Count -eq 0 -and $s.lineCount -eq 0 -and @($v.Missing).Count -eq 3 -and -not $v.ShouldClose)
# P2c's real 3 lines plus a stray still close - the stray is ignored, not fatal.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions ($cP2c + @([pscustomobject]@{ Taskee = $stray; Task = 'z' })) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 0 -NowUtc $t0
Check 'P2c lines + a stray: still all complete (lineCount 3, stray ignored)' ($v.AllComplete -and $s.lineCount -eq 3)

Write-Host '=== 5. trace stop timing (Get-TraceStopWaitSecs) ==='
$stopIface = [datetime]::new(2026, 9, 1, 21, 31, 6, 169, [System.DateTimeKind]::Utc)  # manifest 20260901T211310Z StopIface start
Check 'app exited 4 s after StopIface, trail 30 -> wait 26' ((Get-TraceStopWaitSecs -ReferenceUtc $stopIface -TrailSecs 30 -NowUtc $stopIface.AddSeconds(4)) -eq 26)
Check 'fractional remainder rounds UP (3.2 s -> 4)' ((Get-TraceStopWaitSecs -ReferenceUtc $stopIface -TrailSecs 30 -NowUtc $stopIface.AddSeconds(26.8)) -eq 4)
Check 'already past the trail -> 0, never negative' ((Get-TraceStopWaitSecs -ReferenceUtc $stopIface -TrailSecs 30 -NowUtc $stopIface.AddSeconds(200)) -eq 0)
Check 'trail 0 -> 0' ((Get-TraceStopWaitSecs -ReferenceUtc $stopIface -TrailSecs 0 -NowUtc $stopIface) -eq 0)

Write-Host '=== 5b. observer cap fallback after grace expiry (Get-ObserverCapRemainingSecs, review F1) ==='
# manifest 20260901T211310Z: WatchVrf-trace startedUtc 21:15:01.511, cap 1460 s, actually
# exited 21:39:27.003 = start + 1465.5 s. Grace would have expired at StopIface + trail
# 30 + grace 120 = 21:33:36.169.
$watchStart = [datetime]::new(2026, 9, 1, 21, 15, 1, 511, [System.DateTimeKind]::Utc)
$graceEnd   = $stopIface.AddSeconds(30 + 120)
$capRem = Get-ObserverCapRemainingSecs -StartedUtc $watchStart -DurationSecs 1460 -MarginSecs 30 -NowUtc $graceEnd
# start + 1460 + 30 = 21:39:51.511; minus 21:33:36.169 = 375.342 s -> 376
Check 'P2c geometry: grace end -> 376 s more to the cap + 30 margin' ($capRem -eq 376) "got $capRem"
Check 'the margin covers the observed overshoot (P2c exit at start + 1465.5 s is before start + cap + margin)' ($watchStart.AddSeconds(1465.5) -lt $watchStart.AddSeconds(1460 + 30))
Check 'margin 0 -> exactly cap - elapsed, rounded up (346)' ((Get-ObserverCapRemainingSecs -StartedUtc $watchStart -DurationSecs 1460 -MarginSecs 0 -NowUtc $graceEnd) -eq 346)
Check 'already past cap + margin -> 0, never negative' ((Get-ObserverCapRemainingSecs -StartedUtc $watchStart -DurationSecs 1460 -MarginSecs 30 -NowUtc $watchStart.AddSeconds(2000)) -eq 0)
Check 'exactly at cap + margin -> 0' ((Get-ObserverCapRemainingSecs -StartedUtc $watchStart -DurationSecs 1460 -MarginSecs 30 -NowUtc $watchStart.AddSeconds(1490)) -eq 0)
Check 'fractional remainder rounds UP (0.4 s -> 1)' ((Get-ObserverCapRemainingSecs -StartedUtc $watchStart -DurationSecs 1460 -MarginSecs 30 -NowUtc $watchStart.AddSeconds(1489.6)) -eq 1)
# P3 geometry (RunSecs 420 -> cap 980): start 22:14:18, StopIface 22:22:23.149.
$p3Start = [datetime]::new(2026, 9, 1, 22, 14, 18, 0, [System.DateTimeKind]::Utc)
$p3Stop  = [datetime]::new(2026, 9, 1, 22, 22, 23, 149, [System.DateTimeKind]::Utc)
# start + 980 + 30 = 22:31:08.000; grace end 22:24:53.149 -> 374.851 -> 375
Check 'P3 geometry (cap 980): grace end -> 375 s more' ((Get-ObserverCapRemainingSecs -StartedUtc $p3Start -DurationSecs 980 -MarginSecs 30 -NowUtc $p3Stop.AddSeconds(150)) -eq 375)
# Manifest stamp round-trip (the runner reads the stage's startedUtc back from the manifest).
$parsed = ConvertFrom-ManifestUtc -Text '2026-09-01T21:15:01.511Z'
Check 'manifest stamp parses to the UTC instant' ($null -ne $parsed -and $parsed -eq $watchStart -and $parsed.Kind -eq [System.DateTimeKind]::Utc)
Check 'manifest stamp: null text -> null (caller falls back)' ($null -eq (ConvertFrom-ManifestUtc -Text $null))
Check 'manifest stamp: malformed text -> null, no throw' ($null -eq (ConvertFrom-ManifestUtc -Text 'yesterday'))

Write-Host '=== 6. capability probe parse (Test-ToolCapability) ==='
Check 'exit 0 + token present -> supported' (Test-ToolCapability -ProbeLines @('capabilities','con-selftest','stop-file') -ExitCode 0 -Capability 'stop-file')
Check 'exit 0 + token absent -> unsupported' (-not (Test-ToolCapability -ProbeLines @('capabilities','con-selftest') -ExitCode 0 -Capability 'stop-file'))
Check 'exit 2 (old binary: unknown option) -> unsupported even if stdout had the token' (-not (Test-ToolCapability -ProbeLines @('stop-file') -ExitCode 2 -Capability 'stop-file'))
Check 'null exit (timed out / could not start) -> unsupported' (-not (Test-ToolCapability -ProbeLines @('stop-file') -ExitCode $null -Capability 'stop-file'))
Check 'empty stdout -> unsupported' (-not (Test-ToolCapability -ProbeLines @() -ExitCode 0 -Capability 'stop-file'))
Check 'token match is exact after trim (" stop-file " ok, "stop-files" not)' ((Test-ToolCapability -ProbeLines @(' stop-file ') -ExitCode 0 -Capability 'stop-file') -and -not (Test-ToolCapability -ProbeLines @('stop-files') -ExitCode 0 -Capability 'stop-file'))

Write-Host '=== 7. both PowerShell files parse ==='
foreach ($rel in @('scripts\RunC2SimScenario.ps1', 'scripts\RunnerLib.ps1')) {
    $tokens = $null; $errors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile((Join-Path $RepoRoot $rel), [ref]$tokens, [ref]$errors)
    Check ('{0} parses with 0 errors' -f $rel) ($errors.Count -eq 0) (($errors | ForEach-Object { $_.Message }) -join '; ')
}
# The runner must expose the new switches with the documented defaults.
$runnerAst = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1'), [ref]$null, [ref]$null)
$params = @{}
foreach ($p in $runnerAst.ParamBlock.Parameters) { $params[$p.Name.VariablePath.UserPath] = $p }
Check 'runner declares -StopWhenComplete as a switch' ($params.ContainsKey('StopWhenComplete') -and $params['StopWhenComplete'].StaticType -eq [switch])
Check 'runner declares -SettleHoldSecs default 60' ($params.ContainsKey('SettleHoldSecs') -and "$($params['SettleHoldSecs'].DefaultValue)" -eq '60')
Check 'runner declares -TraceStopGraceSec default 120' ($params.ContainsKey('TraceStopGraceSec') -and "$($params['TraceStopGraceSec'].DefaultValue)" -eq '120')
# Review F1: the Stage 1 inventory must know the observers by process name, and the
# teardown's cap fallback must be wired (Complete-Background takes -CapSecs).
$assign = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.AssignmentStatementAst] -and $a.Left.Extent.Text -eq '$ProcObservers' }, $true)
Check 'runner declares $ProcObservers = WatchVrf, ListenReports' (@($assign).Count -eq 1 -and $assign[0].Right.Extent.Text -match "'WatchVrf'" -and $assign[0].Right.Extent.Text -match "'ListenReports'") ("got " + ($assign | ForEach-Object { $_.Right.Extent.Text }))
$cb = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Complete-Background' }, $true)
Check 'Complete-Background declares -CapSecs and -CapMarginSecs' (@($cb).Count -eq 1 -and $cb[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains 'CapSecs' -and $cb[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains 'CapMarginSecs')
$cbCalls = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] -and $a.GetCommandName() -eq 'Complete-Background' }, $true)
Check 'both observer Complete-Background calls pass -CapSecs' (@($cbCalls).Count -eq 2 -and @($cbCalls | Where-Object { $_.Extent.Text -match '-CapSecs' }).Count -eq 2) "calls=$(@($cbCalls).Count)"

Write-Host ''
Write-Host ('{0} passed, {1} failed' -f $script:Pass, $script:Fail)
if ($script:Fail -gt 0) { exit 1 }
exit 0
