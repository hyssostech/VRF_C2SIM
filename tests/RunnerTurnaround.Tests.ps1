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
#   4b. condition (4), report evidence (run 20260901T235823Z fixture): completion
#      with no later RPT -> not satisfied; later RPT that DISAGREES with POS (the
#      company at t=213.3, 11.8 m) -> not satisfied; later + agreeing -> satisfied;
#      hold < 60 blocks even with evidence; evidence missing blocks even past the
#      hold; name/uuid mapping parsers; degenerate POS ignored
#   4c. ConvertTo-CrlfText (ledger rewrite ending)
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
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(3600) -ReportEvidence $true
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
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(160) -ReportEvidence $true
Check '2 of 3 seen: not all complete, no close' (-not $v.AllComplete -and -not $v.ShouldClose)
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP2c -NowUtc $t0.AddSeconds(215)
Check 'all 3 seen: allCompleteUtc stamped at the poll that first saw it (t+215)' ($s.allCompleteUtc -eq $t0.AddSeconds(215))
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(215) -ReportEvidence $true
Check 'all complete at t+215: AllComplete true, ShouldClose false (hold 0 of 60)' ($v.AllComplete -and -not $v.ShouldClose -and $v.HoldElapsedSecs -eq 0)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(215 + 59) -ReportEvidence $true
Check 'hold 59 s of 60: still open' (-not $v.ShouldClose)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs $hold -NowUtc $t0.AddSeconds(215 + 60) -ReportEvidence $true
Check 'hold 60 s of 60: closes' ($v.ShouldClose)
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP2c -NowUtc $t0.AddSeconds(300)
Check 'a later poll does NOT move allCompleteUtc (hold is measured from the first sighting)' ($s.allCompleteUtc -eq $t0.AddSeconds(215))
Check 'first-seen stamps are never overwritten' ($s.firstSeenUtc['670cfdb2-6c43-f267-ad7f-bd6e739def24'] -eq $t0.AddSeconds(145))

# Zero taskees (unparseable / empty order): never closes, even with completions present.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees @() -TaskCount 0 -Completions $cP2c -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees @() -SettleHoldSecs 0 -NowUtc $t0.AddSeconds(3600) -ReportEvidence $true
Check 'zero taskees: never all-complete, never closes' (-not $v.AllComplete -and -not $v.ShouldClose)

# Line count below task count: two tasks for one performer, one completion line so far.
$one = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees @($one) -TaskCount 2 -Completions @([pscustomobject]@{ Taskee = $one; Task = 'x' }) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees @($one) -SettleHoldSecs 0 -NowUtc $t0 -ReportEvidence $true
Check 'one performer, two tasks, one TASKCMPLT: NOT all complete' (-not $v.AllComplete)
$s = Update-CompletionState -State $s -Taskees @($one) -TaskCount 2 -Completions @([pscustomobject]@{ Taskee = $one; Task = 'x' }, [pscustomobject]@{ Taskee = $one; Task = 'y' }) -NowUtc $t0.AddSeconds(10)
$v = Test-EarlyExit -State $s -Taskees @($one) -SettleHoldSecs 0 -NowUtc $t0.AddSeconds(10) -ReportEvidence $true
Check 'one performer, two tasks, two TASKCMPLT lines: all complete, closes with hold 0' ($v.AllComplete -and $v.ShouldClose)

# Hold of 0 closes on the same poll that completes.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP2c -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 0 -NowUtc $t0 -ReportEvidence $true
Check 'settle hold 0: closes on the completing poll' ($v.ShouldClose)

# Review F2: a TASKCMPLT line for a taskee NOT in the order must not count. One order
# taskee with two tasks, ONE line of its own plus ONE stray line for a foreign unit:
# before the fix the stray satisfied the count rule and the window closed.
$stray = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees @($one) -TaskCount 2 -Completions @([pscustomobject]@{ Taskee = $one; Task = 'x' }, [pscustomobject]@{ Taskee = $stray; Task = 'z' }) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees @($one) -SettleHoldSecs 0 -NowUtc $t0 -ReportEvidence $true
Check 'stray-taskee line: NOT counted toward the task count (1 of 2), no close' (-not $v.AllComplete -and -not $v.ShouldClose -and $s.lineCount -eq 1) "lineCount=$($s.lineCount)"
Check 'stray-taskee line: the stray is not stamped in firstSeenUtc' (-not $s.firstSeenUtc.Contains($stray) -and $s.firstSeenUtc.Contains($one))
# Only stray lines, none for the order taskee: nothing seen at all.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions @([pscustomobject]@{ Taskee = $stray; Task = 'z' }, [pscustomobject]@{ Taskee = $stray; Task = 'z' }, [pscustomobject]@{ Taskee = $stray; Task = 'z' }) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 0 -NowUtc $t0 -ReportEvidence $true
Check 'three stray lines, zero order lines: nothing seen, Missing names all 3, no close' ($s.firstSeenUtc.Count -eq 0 -and $s.lineCount -eq 0 -and @($v.Missing).Count -eq 3 -and -not $v.ShouldClose)
# P2c's real 3 lines plus a stray still close - the stray is ignored, not fatal.
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions ($cP2c + @([pscustomobject]@{ Taskee = $stray; Task = 'z' })) -NowUtc $t0
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 0 -NowUtc $t0 -ReportEvidence $true
Check 'P2c lines + a stray: still all complete (lineCount 3, stray ignored)' ($v.AllComplete -and $s.lineCount -eq 3)

Write-Host '=== 4b. report evidence, condition (4) (Test-ReportEvidence / Test-EarlyExit) ==='
# Fixture from run 20260901T235823Z (runs/20260901T235823Z_run, untracked): TSK for
# the three taskees, the company's last three RPT lines, the company POS samples
# around completion, one entity's RPT/POS pair, and the degenerate POS at t=228.9.
$initFx = @'
<?xml version="1.0"?>
<MessageBody xmlns="http://www.sisostds.org/schemas/C2SIM/1.1"><C2SIMInitializationBody><ObjectInitialization><ObjectDefinitions>
<Entity><ActorEntity><CollectiveEntity><Unit><Name>1.BdeHQ</Name><UUID>670cfdb2-6c43-f267-ad7f-bd6e739def24</UUID></Unit></CollectiveEntity></ActorEntity></Entity>
<Entity><ActorEntity><CollectiveEntity><Unit><Name>1222.MechPlt</Name><UUID>001aa71b-4c26-a1ea-28b2-f7dfe8e76342</UUID></Unit></CollectiveEntity></ActorEntity></Entity>
<Entity><ActorEntity><CollectiveEntity><Unit><EntityDescriptor><Superior>670cfdb3-6c43-f267-ad7f-bd6e739def24</Superior><Nested><UUID>ffffffff-0000-0000-0000-000000000000</UUID><Name>NOT-A-UNIT</Name></Nested></EntityDescriptor><Subordinate>7dbaa71b-667d-2059-bb1c-882fdfed6242</Subordinate><Name>114.MechCoy</Name><UUID>139aa71b-75df-4888-4a5a-6056bae66242</UUID></Unit></CollectiveEntity></ActorEntity></Entity>
</ObjectDefinitions></ObjectInitialization></C2SIMInitializationBody></MessageBody>
'@
$appLogFx = @'
      Task 'T_R5_PL1': CreateRoute 'T_R5_PL1 ROUTE' (3 pts) for 1222.MechPlt; move deferred to route-created.
      Task 'T_R5_CO1': CreateRoute 'T_R5_CO1 ROUTE' (3 pts) for 114.MechCoy; move deferred to route-created.
      Task 'T_R5_TK1': CreateRoute 'T_R5_TK1 ROUTE' (3 pts) for 1.BdeHQ; move deferred to route-created.
      Route 'T_R5_PL1 ROUTE' created; MoveAlongRoute issued for VRF_UUID:17cae39c-a7f4-234b-b532-7cb5e31f224e.
      Route 'T_R5_CO1 ROUTE' created; MoveAlongRoute issued for VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52.
      Route 'T_R5_TK1 ROUTE' created; MoveAlongRoute issued for VRF_UUID:2a83e509-54d5-6c49-8f6f-e4935d1772ff.
'@
$fxTaskees = @('670cfdb2-6c43-f267-ad7f-bd6e739def24', '001aa71b-4c26-a1ea-28b2-f7dfe8e76342', '139aa71b-75df-4888-4a5a-6056bae66242')
$names = Get-InitUnitNames -InitText $initFx
Check 'init parse: 3 taskee uuids -> markings' ($names.Count -eq 3 -and $names['139aa71b-75df-4888-4a5a-6056bae66242'] -eq '114.MechCoy' -and $names['670cfdb2-6c43-f267-ad7f-bd6e739def24'] -eq '1.BdeHQ') ("got " + ($names.Values -join ','))
Check 'init parse: a nested (grandchild) UUID/Name pair is not mistaken for the unit' (-not $names.Contains('ffffffff-0000-0000-0000-000000000000') -and $names.Values -notcontains 'NOT-A-UNIT')
$realNames = Get-InitUnitNames -InitText (Get-Content -LiteralPath (Join-Path $RepoRoot 'data\R9_Mojave_Lean_Initialization_NoComments.xml') -Raw -Encoding UTF8)
Check 'REAL R9 init: 6 units; the 3 order taskees map to 1.BdeHQ / 1222.MechPlt / 114.MechCoy' ($realNames.Count -eq 6 -and $realNames['670cfdb2-6c43-f267-ad7f-bd6e739def24'] -eq '1.BdeHQ' -and $realNames['001aa71b-4c26-a1ea-28b2-f7dfe8e76342'] -eq '1222.MechPlt' -and $realNames['139aa71b-75df-4888-4a5a-6056bae66242'] -eq '114.MechCoy') ("count=$($realNames.Count)")
Check 'init parse: garbage -> empty map, no throw' ((Get-InitUnitNames -InitText '<nope').Count -eq 0)
$vrfMap = Get-VrfUuidByName -AppLogText $appLogFx
Check 'app-log parse: 3 markings -> VRF_UUIDs via the route join' ($vrfMap.Count -eq 3 -and $vrfMap['114.MechCoy'] -eq 'VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52' -and $vrfMap['1.BdeHQ'] -eq 'VRF_UUID:2a83e509-54d5-6c49-8f6f-e4935d1772ff') ("got " + ($vrfMap.Keys -join ','))
Check 'app-log parse: a created-route line with no CreateRoute partner maps nothing' ((Get-VrfUuidByName -AppLogText "Route 'X ROUTE' created; MoveAlongRoute issued for VRF_UUID:2a83e509-54d5-6c49-8f6f-e4935d1772ff.").Count -eq 0)

# Trace text as the runner would see it at successive polls (trace clock seconds).
$traceHead = @(
    'POS,145.0,VRF_UUID:2a83e509-54d5-6c49-8f6f-e4935d1772ff,34.651068,-116.693660,1116.5'
    'TSK,145.3,"1.BdeHQ","move-along"'
    'RPT,153,"POSITION ""1.BdeHQ"" 34.651068 -116.693660"'
    'RPT,153,"POSITION ""114.MechCoy"" 34.648331 -116.693388"'
    'POS,157.0,VRF_UUID:17cae39c-a7f4-234b-b532-7cb5e31f224e,34.647854,-116.693115,1116.5'
    'TSK,157.3,"1222.MechPlt","move-along"'
    'RPT,160,"POSITION ""1222.MechPlt"" 34.647854 -116.693115"'
    'POS,210.6,VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52,34.630541,-116.693377,1116.7'
    'TSK,211.8,"114.MechCoy","move-along"'
)
$at212 = ($traceHead -join "`n") + "`n"
$at214 = $at212 + "POS,212.7,VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52,34.640109,-116.693391,1116.6`nRPT,213.3,`"POSITION `"`"114.MechCoy`"`" 34.653809 -116.693388`"`n"
$at219 = $at214 + "POS,216.8,VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52,34.653440,-116.693388,1116.8`nPOS,218.8,VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52,34.653915,-116.693388,1116.8`n"
$at230 = $at219 + "POS,228.9,VRF_UUID:f864e51f-e571-704f-92e4-108201ec1049,0.000000,90.000000,101112964038886526957791966946910942263970025203852196198915728474112.0`nPOS,228.9,VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52,0.000000,90.000000,101112964038886526957791966946910942263970025203852196198915728474112.0`n"
$at275 = $at230 + "POS,273.8,VRF_UUID:cbea8b1b-05fa-fa4f-88fe-bc1ec8d05d52,34.653915,-116.693388,1116.8`nRPT,273.9,`"POSITION `"`"114.MechCoy`"`" 34.653915 -116.693388`"`nRPT,274.3,`"POSITION `"`"1.BdeHQ`"`" 34.651068 -116.693660`"`nRPT,274.4,`"POSITION `"`"1222.MechPlt`"`" 34.647854 -116.693115`"`n"

$e = Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText $at212 -ToleranceMeters 2.0
Check 't=212: company completed (TSK 211.8), no later RPT -> NOT satisfied' (-not $e.AllSatisfied -and -not $e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].satisfied -and $e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].reason -match 'not later than completion') ($e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].reason)
Check 't=212: the two entities (RPT after their TSK, 0.0 m) ARE satisfied' ($e.PerTaskee['670cfdb2-6c43-f267-ad7f-bd6e739def24'].satisfied -and $e.PerTaskee['001aa71b-4c26-a1ea-28b2-f7dfe8e76342'].satisfied)
$e = Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText $at214 -ToleranceMeters 2.0
Check 't=214: RPT 213.3 IS later than TSK 211.8 but is 1.5 km from POS 212.7 -> NOT satisfied (the literal "later" rule alone would pass here)' (-not $e.AllSatisfied -and $e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].reason -match 'm from the latest POS') ($e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].reason)
$e = Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText $at219 -ToleranceMeters 2.0
$dCo = $e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].distanceM
Check 't=219: centre settled at 34.653915; the 213.3 RPT is 11.8 m off -> NOT satisfied (the confirm-run miss)' (-not $e.AllSatisfied -and $dCo -gt 11 -and $dCo -lt 13) "distance=$dCo"
$e = Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText $at230 -ToleranceMeters 2.0
Check 't=230: the degenerate POS (lat 0 / lon 90 / alt 1e68) does NOT replace the latest real POS' ($e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].posT -eq 218.8) "posT=$($e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].posT)"
$e = Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText $at275 -ToleranceMeters 2.0
Check 't=275: next report round (RPT 273.9 == POS 273.8) -> ALL satisfied' ($e.AllSatisfied -and $e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].distanceM -eq 0 -and $e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].lastRptT -eq 273.9) ($e.PerTaskee['139aa71b-75df-4888-4a5a-6056bae66242'].reason)
Check 'tolerance is a real knob: 20 m would have accepted the 11.8 m miss at t=219' ((Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText $at219 -ToleranceMeters 20.0).AllSatisfied)
$e = Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid @{} -TraceText $at275 -ToleranceMeters 2.0
Check 'no marking -> VRF_UUID mapping (no route lines yet) -> NOT satisfied, reason says so' (-not $e.AllSatisfied -and $e.PerTaskee['670cfdb2-6c43-f267-ad7f-bd6e739def24'].reason -match 'VRF_UUID unknown')
$e = Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames @{} -NameToVrfUuid $vrfMap -TraceText $at275 -ToleranceMeters 2.0
Check 'taskee without a Name in the init -> NOT satisfied' (-not $e.AllSatisfied -and $e.PerTaskee['670cfdb2-6c43-f267-ad7f-bd6e739def24'].reason -match 'no <Name>')
Check 'empty trace -> NOT satisfied (no TSK yet)' (-not (Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText '' -ToleranceMeters 2.0).AllSatisfied)
Check 'zero taskees -> NOT satisfied (never closes on evidence alone)' (-not (Test-ReportEvidence -Taskees @() -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText $at275 -ToleranceMeters 2.0).AllSatisfied)
Check 'CRLF trace parses the same' ((Test-ReportEvidence -Taskees $fxTaskees -TaskeeNames $names -NameToVrfUuid $vrfMap -TraceText ($at275 -replace "`n", "`r`n") -ToleranceMeters 2.0).AllSatisfied)
Check 'haversine: 1 deg lat at the equator ~ 111.2 km' ([Math]::Abs((Get-DistanceMeters -Lat1 0 -Lon1 0 -Lat2 1 -Lon2 0) - 111195) -lt 50)

# The decision: both halves must hold (SettleHoldSecs is a FLOOR, evidence is the gate).
$s = New-CompletionState
$s = Update-CompletionState -State $s -Taskees $taskees -TaskCount 3 -Completions $cP2c -NowUtc $t0.AddSeconds(215)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 60 -NowUtc $t0.AddSeconds(215 + 30) -ReportEvidence $true
Check 'evidence in, hold 30 of 60 -> NOT closed (floor holds)' (-not $v.ShouldClose -and $v.EvidenceIn -and -not $v.HoldElapsed)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 60 -NowUtc $t0.AddSeconds(215 + 60) -ReportEvidence $false
Check 'hold 60 of 60, evidence pending -> NOT closed (evidence gates)' (-not $v.ShouldClose -and $v.HoldElapsed -and -not $v.EvidenceIn)
$v = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 60 -NowUtc $t0.AddSeconds(215 + 60) -ReportEvidence $true
Check 'hold 60 of 60 AND evidence in -> closes' ($v.ShouldClose)
$threw = $false
try { $null = Test-EarlyExit -State $s -Taskees $taskees -SettleHoldSecs 60 -NowUtc $t0 } catch { $threw = $true }
Check '-ReportEvidence is mandatory (a caller can not forget condition 4)' $threw

Write-Host '=== 4c. ledger line endings (ConvertTo-CrlfText) ==='
Check 'LF -> CRLF' ((ConvertTo-CrlfText -Text "a`nb`n") -eq "a`r`nb`r`n")
Check 'CRLF stays CRLF (idempotent, no doubled CR)' ((ConvertTo-CrlfText -Text "a`r`nb`r`n") -eq "a`r`nb`r`n")
Check 'mixed -> all CRLF' ((ConvertTo-CrlfText -Text "a`r`nb`nc") -eq "a`r`nb`r`nc")
Check 'null -> empty string' ((ConvertTo-CrlfText -Text $null) -eq '')

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
# Condition (4) wiring: every Test-EarlyExit call in the runner passes -ReportEvidence,
# and the in-loop one passes the Test-ReportEvidence verdict (not a literal).
$eeCalls = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] -and $a.GetCommandName() -eq 'Test-EarlyExit' }, $true)
Check 'runner: every Test-EarlyExit call passes -ReportEvidence' (@($eeCalls).Count -ge 2 -and @($eeCalls | Where-Object { $_.Extent.Text -match '-ReportEvidence' }).Count -eq @($eeCalls).Count) "calls=$(@($eeCalls).Count)"
Check 'runner: the poll-loop Test-EarlyExit takes the Test-ReportEvidence verdict' (@($eeCalls | Where-Object { $_.Extent.Text -match '-ReportEvidence \$evidenceOk' }).Count -eq 1)
$reCalls = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] -and $a.GetCommandName() -eq 'Test-ReportEvidence' }, $true)
Check 'runner: Test-ReportEvidence is called on the live trace with the tolerance knob' (@($reCalls).Count -eq 1 -and $reCalls[0].Extent.Text -match '\$PathTrace' -and $reCalls[0].Extent.Text -match '-ToleranceMeters \$ReportToleranceMeters')
$ul = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Update-Ledger' }, $true)
Check 'runner: Update-Ledger writes through ConvertTo-CrlfText' (@($ul).Count -eq 1 -and $ul[0].Extent.Text -match 'WriteAllText\(\$LedgerDoc, \(ConvertTo-CrlfText \$updated\)')

# 8. The .Missing member-enumeration defect that made run 20260902T143638Z exit 5
# ("The property 'Count' cannot be found on this object" at RunC2SimScenario.ps1:2171).
# @() around the CALL member-enumerates .Missing and PowerShell unwraps a ONE-element
# result to a bare [string]; under Set-StrictMode -Version Latest the later $missing.Count
# then throws. The branch is reached only when -StopWhenComplete fails to fire with
# EXACTLY ONE taskee missing, so no prior run had executed it. @() must wrap the PROPERTY.
Write-Host '=== 8. $missing must be an array for 0, 1 and 2 missing taskees (run 20260902T143638Z, EXIT=5) ==='
$nowUtc8 = (Get-Date).ToUniversalTime()
$state8  = New-CompletionState
$state8.firstSeenUtc['A'] = $nowUtc8
$state8.firstSeenUtc['B'] = $nowUtc8
$state8.lineCount = 2
$one8  = @( (Test-EarlyExit -State $state8 -Taskees @('A','B','C')     -SettleHoldSecs 60 -NowUtc $nowUtc8 -ReportEvidence $false).Missing )
$zero8 = @( (Test-EarlyExit -State $state8 -Taskees @('A','B')         -SettleHoldSecs 60 -NowUtc $nowUtc8 -ReportEvidence $false).Missing )
$two8  = @( (Test-EarlyExit -State $state8 -Taskees @('A','B','C','D') -SettleHoldSecs 60 -NowUtc $nowUtc8 -ReportEvidence $false).Missing )
$bad8  = @(Test-EarlyExit -State $state8 -Taskees @('A','B','C') -SettleHoldSecs 60 -NowUtc $nowUtc8 -ReportEvidence $false).Missing
Check 'ONE missing: @( (call).Missing ) is a 1-element array and .Count is usable' ($one8.Count -eq 1 -and $one8[0] -eq 'C') ("got " + ($one8 -join ','))
Check 'ZERO missing: @( (call).Missing ) is an EMPTY array, not @($null)'          ($zero8.Count -eq 0) "got $($zero8.Count)"
Check 'TWO missing: @( (call).Missing ) keeps both'                                ($two8.Count -eq 2) ("got " + ($two8 -join ','))
Check 'the DEFECTIVE form @(call).Missing really does unwrap to a bare string'     ($bad8 -is [string] -and $bad8 -eq 'C') "got $($bad8.GetType().Name)"
$missAssign = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.AssignmentStatementAst] -and $a.Left.Extent.Text -eq '$missing' }, $true)
Check 'runner: $missing wraps the PROPERTY, not the call - @( (Test-EarlyExit ...).Missing )' (
    @($missAssign).Count -eq 1 -and $missAssign[0].Right.Extent.Text -match '^@\(\s*\(Test-EarlyExit.*\)\.Missing\s*\)$') (
    ($missAssign | ForEach-Object { $_.Right.Extent.Text }) -join ' | ')

Write-Host ''
Write-Host ('{0} passed, {1} failed' -f $script:Pass, $script:Fail)
if ($script:Fail -gt 0) { exit 1 }
exit 0
