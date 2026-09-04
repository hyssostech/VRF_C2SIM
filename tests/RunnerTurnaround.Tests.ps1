# tests/RunnerTurnaround.Tests.ps1 - OFFLINE regression check for the runner's
# turnaround logic (docs/RUNNER_TURNAROUND_2026-09-01.md). No simulator, no server.
# Plain pwsh (no Pester dependency): exits 0 when every check passes, 1 otherwise,
# and prints one line per check.
# ONE exception to "no process start": check 8d runs the runner itself, in -DryRun,
# against a throwaway patched copy - the false green it guards is an EXIT CODE, and
# no static assertion can distinguish exit 0 from exit 5.
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

Write-Host '=== 7. the runner and its 5.2 profile scripts parse ==='
foreach ($rel in @('scripts\RunC2SimScenario.ps1', 'scripts\RunnerLib.ps1',
                   'scripts\StartRtiExec52.ps1', 'scripts\LaunchVrf52.ps1', 'scripts\StopVrf52.ps1')) {
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

# 8b. THE 5.2 RTI CONNECTION MODE IS NOT A KNOB (2026-09-04, PREREG_52_RTIEXEC). UG52 5.5.1
# p190: "You cannot use the MAK RTI in lightweight mode with VR-Forces". Under the
# lightweight rid every 5.2 observer reflected 0 entities; under MAK RTI 5.0.1 in rtiexec
# mode the same observer reflected 62. These checks are the tripwire: they fail the moment
# the profile drifts back toward 4.6.1 or the lightweight rid - which no downstream test
# could catch, because the symptom is silence, not an error.
# The VR-Forces-level interface address is DIFFERENT and is NOT part of that repair: the
# discriminator (run 3857) reflected 54-56 entities with the observer's device address blank.
# -DeviceAddress therefore DEFAULTS TO EMPTY and nothing is passed; the checks below pin that
# default and the wiring, so a later edit cannot quietly re-pin an address the evidence does
# not support.
Write-Host '=== 8b. the 5.2 profile: fixed rtiexec mode, interface address off by default ==='
$runnerText = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1') -Raw
Check 'runner: the 5.2 RtiDir is makRti5.0.1 (never 4.6.1)' (
    $runnerText -match "ContainsKey\('RtiDir'\)\)\s*\{\s*\`$RtiDir\s*=\s*'C:\\MAK\\makRti5\.0\.1'")
Check 'runner: the SHARED rid is rid-501-rtiexec-min.mtl' (
    $runnerText -match "\`$RidFile\s*=\s*Join-Path \`$RepoRoot 'config\\rid-501-rtiexec-min\.mtl'")
Check 'runner: -DeviceAddress exists and DEFAULTS TO EMPTY (run 3857 falsified it observer-side)' (
    $params.ContainsKey('DeviceAddress') -and "$($params['DeviceAddress'].DefaultValue)" -match "^''$")
Check 'runner: the 5.2 interface address comes FROM that parameter (tunable, not a literal)' (
    $runnerText -match "\`$DeviceAddress52\s*=\s*\`$DeviceAddress")
Check 'runner: -DeviceAddress is refused on the 5.0.2 profile' (
    $runnerText -match "ContainsKey\('DeviceAddress'\)")
Check 'runner: LaunchVrf52 gets -DeviceAddress ONLY when one was asked for' (
    $runnerText -match "if \(\`$DeviceAddressPassed\) \{ \`$launchArgs \+= @\('-DeviceAddress', \`$DeviceAddress52\) \}")
Check 'runner: Vrf__DeviceAddress is set ONLY when one was asked for' (
    $runnerText -match "if \(\`$DeviceAddressPassed\) \{ \`$AppEnv52\['Vrf__DeviceAddress'\] = \`$DeviceAddress52 \}")
Check 'runner: the manifest records what the BRIDGE federates ended up using either way' (
    $runnerText -match 'bridgeDeviceAddress\s*=' -and $runnerText -match 'deviceAddressStatus\s*=')
# The rtiexec's interface is the RTI LAYER (fixed by the loopback-broadcast rid) and must NOT
# be wired to -DeviceAddress: with the empty default that would hand the RTI a blank address
# and StartRtiExec52 would exit 2 on every run.
Check 'runner: Stage 2r does NOT derive the rtiexec interface from -DeviceAddress' (
    $runnerText -notmatch "'-InterfaceAddress', \`$DeviceAddress")
Check 'runner: Stage 2r invokes StartRtiExec52 BEFORE the Stage 2c RtiProbe gate' (
    $runnerText.IndexOf("Stage 2r") -gt 0 -and
    $runnerText.IndexOf("-Name 'StartRtiExec52'") -gt 0 -and
    $runnerText.IndexOf("-Name 'StartRtiExec52'") -lt $runnerText.IndexOf("-Name 'RtiProbe'"))
foreach ($rel in @('scripts\StartRtiExec52.ps1', 'scripts\LaunchVrf52.ps1')) {
    $t = Get-Content -LiteralPath (Join-Path $RepoRoot $rel) -Raw
    Check ('{0}: defaults to makRti5.0.1' -f $rel) ($t -match "\`$RtiDir\s+=\s+'C:\\MAK\\makRti5\.0\.1'")
}
# StartRtiExec52 owns the RTI infrastructure and stops NOTHING, ever.
Check 'StartRtiExec52: no Stop-Process / taskkill anywhere' (
    (Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\StartRtiExec52.ps1') -Raw) -notmatch 'Stop-Process|taskkill')
# LaunchVrf52 MAY now stop exactly one thing - its own crashed back-end (section 8e). The
# blanket "never Stop-Process" check that used to stand here would have hidden a kill-by-name,
# so it is replaced by the narrower invariants in 8e, not dropped.
$sreAst = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $RepoRoot 'scripts\StartRtiExec52.ps1'), [ref]$null, [ref]$null)
$sreParams = @{}
foreach ($p in $sreAst.ParamBlock.Parameters) { $sreParams[$p.Name.VariablePath.UserPath] = $p }
foreach ($n in @('RtiDir','RidFile','LogDir','TcpPort','UdpPort','DestAddress','InterfaceAddress','ForwarderPort','ReadyTimeoutSec','DryRun')) {
    Check ('StartRtiExec52 declares -{0}' -f $n) ($sreParams.ContainsKey($n))
}
Check 'StartRtiExec52 defaults to the 4001/4001/5000 rendezvous of the golden connection' (
    "$($sreParams['TcpPort'].DefaultValue)" -eq '4001' -and "$($sreParams['UdpPort'].DefaultValue)" -eq '4001' -and
    "$($sreParams['ForwarderPort'].DefaultValue)" -eq '5000')
Check 'StartRtiExec52 defaults to the loopback broadcast + 127.0.0.1 interface' (
    "$($sreParams['DestAddress'].DefaultValue)" -match '127\.255\.255\.255' -and
    "$($sreParams['InterfaceAddress'].DefaultValue)" -match '127\.0\.0\.1')
# The rtiexec OUTLIVES the run that started it, so its own log must NOT be filed under one
# run's evidence directory - it goes to the persistent, gitignored runs\launch52, stamped.
$sreText = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\StartRtiExec52.ps1') -Raw
Check 'StartRtiExec52 logs to runs\launch52 with a UTC-stamped name, not a run directory' (
    $sreText -match "Join-Path \`$repoRoot 'runs\\launch52'" -and $sreText -match "rtiexec_\{0\}\.log")
Check 'runner: Stage 2r passes -LogDir runs\launch52 (never the run directory)' (
    $runnerText -match "\`$RtiExecLogDir = Join-Path \`$RepoRoot 'runs\\launch52'" -and
    $runnerText -match "'-LogDir', \`$RtiExecLogDir" -and
    $runnerText -notmatch "'-RunDir', \`$RunDir")
Check 'runner: the manifest carries the rtiexec logDir and logFile' (
    $runnerText -match 'logDir = \$RtiExecLogDir' -and $runnerText -match 'rtiExec\.logFile\s*=\s*\$rtiLog')
# The pid/log marker the manifest depends on must parse BOTH banner forms, log= last and
# allowed to contain spaces. A silent miss would leave the manifest fields null.
$rx = 'RTIEXEC READY rtiexec=(\d+|none) forwarder=(\d+|none) tcp=\S+ started=(yes|no) log=(.*?)\s*$'
$mYes = [regex]::Match('  [OK]   RTIEXEC READY rtiexec=4242 forwarder=99 tcp=127.0.0.1:4001 started=yes log=C:\r\runs\launch52\rtiexec_20260904T110000Z.log', $rx)
$mNo  = [regex]::Match('  [OK]   RTIEXEC READY rtiexec=15720 forwarder=43728 tcp=127.0.0.1:4001 started=no log=(not started by this run - see C:\r\runs\launch52)', $rx)
Check 'marker parses started=yes with the stamped log path' (
    $mYes.Success -and $mYes.Groups[1].Value -eq '4242' -and $mYes.Groups[3].Value -eq 'yes' -and
    $mYes.Groups[4].Value -match 'rtiexec_20260904T110000Z\.log$') "got '$($mYes.Groups[4].Value)'"
Check 'marker parses started=no, whose log= contains spaces' (
    $mNo.Success -and $mNo.Groups[1].Value -eq '15720' -and $mNo.Groups[2].Value -eq '43728' -and
    $mNo.Groups[3].Value -eq 'no' -and $mNo.Groups[4].Value -match '^\(not started') "got '$($mNo.Groups[4].Value)'"

# 8c. THE 5.2 STARTUP CRASH must FAIL the launch, not be waited out (2026-09-04 cold-start
# review): 0xC0000005 in makVrf::DtVrfSimOptions::parseCmdLine hit 2 of 5 launches, under
# BOTH rids, so the trigger is unknown and the launch stage must DETECT it. Without this the
# runner pushes an init at a back-end that never existed.
Write-Host '=== 8c. LaunchVrf52 detects the startup crash and exits 3 without retrying ==='
$lv52Ast = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $RepoRoot 'scripts\LaunchVrf52.ps1'), [ref]$null, [ref]$null)
$lv52Params = @{}
foreach ($p in $lv52Ast.ParamBlock.Parameters) { $lv52Params[$p.Name.VariablePath.UserPath] = $p }
$lv52Text = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\LaunchVrf52.ps1') -Raw
Check 'LaunchVrf52 declares -MakLogDir defaulting to C:\MAK\logs' (
    $lv52Params.ContainsKey('MakLogDir') -and "$($lv52Params['MakLogDir'].DefaultValue)" -match 'C:\\MAK\\logs')
$crashFn = $lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Get-SimCrashEvidence' }, $true)
Check 'LaunchVrf52 defines Get-SimCrashEvidence(-ProcessId, -LogDir)' (
    @($crashFn).Count -eq 1 -and
    $crashFn[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains 'ProcessId' -and
    $crashFn[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains 'LogDir')
Check 'it looks for the pid-suffixed callstack file MAK writes' ($lv52Text -match "\*-\{0\}\.callstack\.log")
Check 'it recognises the MAK crash-box window titles (5.2 "Error <exe>" and 5.0.2 "<exe>.dmp")' (
    $lv52Text -match "\^Error \.\*vrfSim" -and $lv52Text -match "\^vrfSim\.\*\\\.dmp\`$")
Check 'the readiness poll checks for the crash on EVERY iteration' ($lv52Text -match 'while \(\(Get-Date\) -lt \$deadline\) \{\s*\r?\n\s*\$simCrash = Get-SimCrashEvidence')
Check 'a crash is decided BEFORE the READY verdict (no false green)' (
    $lv52Text.IndexOf("if (`$simCrash.Crashed) {`r`n    # Checked FIRST") -gt 0 -and
    $lv52Text.IndexOf("if (`$simCrash.Crashed) {`r`n    # Checked FIRST") -lt $lv52Text.IndexOf("READY: 5.2d back-end HEALTHY"))
Check 'LaunchVrf52 never uses taskkill, and stops nothing BY NAME (section 8e pins the one pid it may stop)' (
    $lv52Text -notmatch 'taskkill' -and $lv52Text -notmatch 'Stop-Process[^\r\n]*-Name')
# ...and the detector must actually FIRE. The checks above only prove the code is shaped
# right; this one runs the SHIPPED function (lifted out of the script by its own AST, so no
# copy can drift from it) against a synthetic MAK log directory. Real filenames, taken from
# C:\MAK\logs on 2026-09-04: vrfSimHLA1516e5.2d-20260903-215720-Legatus-282607-39028
# .callstack.log, whose last field is the faulting pid.
$csFileFn = $lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Get-CallstackFileForPid' }, $true)
Check 'LaunchVrf52 defines Get-CallstackFileForPid (the ONE place the pid/-Since rule lives)' (@($csFileFn).Count -eq 1)
Invoke-Expression $csFileFn[0].Extent.Text   # Get-SimCrashEvidence delegates the file lookup to it
Invoke-Expression $crashFn[0].Extent.Text
$tmpLogDir = Join-Path ([System.IO.Path]::GetTempPath()) ('lv52crash-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmpLogDir -Force | Out-Null
try {
    $csName = 'vrfSimHLA1516e5.2d-20260903-215720-Legatus-282607-39028.callstack.log'
    Set-Content -LiteralPath (Join-Path $tmpLogDir $csName) -Encoding ascii -Value @(
        'Thread ID - 41792', 'Error Code - 0xC0000005', 'Callstack: ',
        '0x7FF8BCCD5851: makVrf::DtVrfSimOptions::parseCmdLine(768) in vrlinkNetworkInterfaceHLA1516e.dll',
        '0x7FF61370F8C1: DtVrfApp::init(632) in vrfSimHLA1516e.exe')
    $old   = (Get-Date).AddHours(-1)
    $hit   = Get-SimCrashEvidence -ProcessId 39028 -LogDir $tmpLogDir -Since $old
    $miss  = Get-SimCrashEvidence -ProcessId 39029 -LogDir $tmpLogDir -Since $old
    $noDir = Get-SimCrashEvidence -ProcessId 39028 -LogDir (Join-Path $tmpLogDir 'does-not-exist') -Since $old
    # PID RECYCLING: C:\MAK\logs keeps callstacks across boots and Windows reuses pids, so a
    # file older than this back-end's start must NOT fail a healthy launch.
    $stale = Get-SimCrashEvidence -ProcessId 39028 -LogDir $tmpLogDir -Since (Get-Date).AddHours(1)
    Check 'detector FIRES on a callstack file whose last field is the back-end pid' ($hit.Crashed) "reason=$($hit.Reason)"
    Check 'detector reports the file and its first frames' (
        $hit.File -match 'callstack\.log$' -and @($hit.Frames).Count -ge 4 -and
        (@($hit.Frames) -join ' ') -match 'parseCmdLine') ("frames=" + @($hit.Frames).Count)
    Check 'detector does NOT fire for a DIFFERENT pid (39029 vs the 39028 file)' (-not $miss.Crashed) "reason=$($miss.Reason)"
    Check 'detector does NOT fire on a callstack older than this launch (pid recycling)' (-not $stale.Crashed) "reason=$($stale.Reason)"
    Check 'a missing log directory is not a crash and does not throw' (-not $noDir.Crashed)
    # The MAK log dir is shared by the whole toolchain: C:\MAK\logs held
    # rtiAssistant5.0.1-20260903-194550-Legatus-281993-54616.callstack.log on 2026-09-04.
    # A pid-only match would have blamed the back-end for another exe's crash.
    Set-Content -LiteralPath (Join-Path $tmpLogDir 'rtiAssistant5.0.1-20260903-194550-Legatus-281993-54616.callstack.log') `
        -Encoding ascii -Value @('Error Code - 0xC0000005')
    Check "detector does NOT fire on ANOTHER MAK exe's callstack for the polled pid (rtiAssistant, same directory)" (
        -not (Get-SimCrashEvidence -ProcessId 54616 -LogDir $tmpLogDir -Since $old).Crashed)
} finally { Remove-Item -LiteralPath $tmpLogDir -Recurse -Force -ErrorAction SilentlyContinue }
Check 'the callstack match is scoped to the vrfSim exe family, not the pid alone' (
    $lv52Text -match "\`$NamePrefix = 'vrfSim'")
Check 'LaunchVrf52 takes the -Since floor BEFORE starting the back-end' (
    $lv52Text.IndexOf('$simStartFloor = (Get-Date)') -gt 0 -and
    $lv52Text.IndexOf('$simStartFloor = (Get-Date)') -lt $lv52Text.IndexOf('$simProc = Start-Process'))
Check 'both crash checks pass -Since' (
    @([regex]::Matches($lv52Text, 'Get-SimCrashEvidence -ProcessId \$simProc\.Id -LogDir \$MakLogDir -Since \$simStartFloor')).Count -eq 2)

# 8e. THE CRASHED PROCESS LINGERS, AND IT BLOCKED THE RETRY (observed 2026-09-04 11:28-11:30
# UTC; those two console captures were not persisted under runs\launch52, but the crash is on
# disk: C:\MAK\logs\vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.callstack.log +
# .dmp, 07:28:06 LOCAL = 11:28 UTC). 8c's detection worked - exit 3 for pid 59936 - but MAK's
# keeps the faulted process ALIVE (0 threads, title 'Error vrfSimHLA1516e.exe'), so the very
# next launch was refused by the pre-existing-process precondition with exit 2 and an
# unattended runner could not retry. What is pinned here is as much the LIMIT as the fix: the
# leftover classifier must need BOTH conditions (a callstack file for the pid AND <= 4
# threads), and the script must be unable to stop anything except one vrfSimHLA1516e pid it
# has itself identified - never a name, never RTI infrastructure.
Write-Host '=== 8e. the crashed back-end is closed (own pid) / classified (pre-existing leftover) ==='
Check 'LaunchVrf52 declares -LeaveCrashedProcess and -CloseCrashedLeftover as switches' (
    $lv52Params.ContainsKey('LeaveCrashedProcess') -and $lv52Params['LeaveCrashedProcess'].StaticType -eq [switch] -and
    $lv52Params.ContainsKey('CloseCrashedLeftover') -and $lv52Params['CloseCrashedLeftover'].StaticType -eq [switch])
$leftoverFn = $lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Test-CrashedLeftover' }, $true)
$closeFn    = $lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Close-CrashedBackend' }, $true)
Check 'LaunchVrf52 defines Test-CrashedLeftover(-ProcessId, -ThreadCount, -LogDir, -MaxThreads)' (
    @($leftoverFn).Count -eq 1 -and
    (@('ProcessId','ThreadCount','LogDir','MaxThreads') |
        Where-Object { $leftoverFn[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains $_ }).Count -eq 4)
Check 'LaunchVrf52 defines Close-CrashedBackend(-ProcessId, -ExpectedName)' (
    @($closeFn).Count -eq 1 -and
    $closeFn[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains 'ProcessId' -and
    $closeFn[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains 'ExpectedName')

# The classifier, RUN (lifted from the script by its own AST, like 8c's detector). BOTH
# conditions or nothing: this is the predicate that authorises closing a process this script
# did not start, so a one-sided pass here would be a licence to kill a healthy sim.
Invoke-Expression $leftoverFn[0].Extent.Text
$tmpLeft = Join-Path ([System.IO.Path]::GetTempPath()) ('lv52left-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmpLeft -Force | Out-Null
try {
    # Real name shape, real faulting pid position (last field) - C:\MAK\logs, 2026-09-04.
    Set-Content -LiteralPath (Join-Path $tmpLeft 'vrfSimHLA1516e5.2d-20260904-112832-Legatus-282607-59936.callstack.log') `
        -Encoding ascii -Value @('Thread ID - 1', 'Error Code - 0xC0000005', 'Callstack: ')
    $old = (Get-Date).AddHours(-1)
    # 59936 = the pid MAK wrote a callstack for; 59937 = a pid it did not.
    Check 'BOTH conditions (0 threads + callstack for that pid) -> crashed leftover' (
        Test-CrashedLeftover -ProcessId 59936 -ThreadCount 0 -LogDir $tmpLeft -Since $old)
    Check 'boundary: 4 threads (the blocked-back-end ceiling) still counts as a leftover' (
        Test-CrashedLeftover -ProcessId 59936 -ThreadCount 4 -LogDir $tmpLeft -Since $old)
    Check 'boundary: 5 threads does NOT, even with the callstack present' (
        -not (Test-CrashedLeftover -ProcessId 59936 -ThreadCount 5 -LogDir $tmpLeft -Since $old))
    Check 'a HEALTHY sim (36 threads, the observed 5.2d baseline) is never a leftover, callstack or not' (
        -not (Test-CrashedLeftover -ProcessId 59936 -ThreadCount 36 -LogDir $tmpLeft -Since $old))
    Check 'a LOW-thread process with NO callstack for its pid is not a leftover (that is the blocked-on-RTI signature)' (
        -not (Test-CrashedLeftover -ProcessId 59937 -ThreadCount 0 -LogDir $tmpLeft -Since $old))
    Check 'a callstack OLDER than the process (recycled pid) does not make it a leftover' (
        -not (Test-CrashedLeftover -ProcessId 59936 -ThreadCount 0 -LogDir $tmpLeft -Since (Get-Date).AddHours(1)))
    Check 'a missing log directory is not a leftover and does not throw' (
        -not (Test-CrashedLeftover -ProcessId 59936 -ThreadCount 0 -LogDir (Join-Path $tmpLeft 'nope') -Since $old))
    # C:\MAK\logs is shared by the whole toolchain - it really does hold
    # rtiAssistant5.0.1-20260903-194550-Legatus-281993-54616.callstack.log (seen 2026-09-04).
    # ANOTHER exe's callstack for the same pid must not be read as this back-end's crash.
    Set-Content -LiteralPath (Join-Path $tmpLeft 'rtiAssistant5.0.1-20260903-194550-Legatus-281993-54617.callstack.log') `
        -Encoding ascii -Value 'x'
    Check "another MAK exe's callstack for that pid (rtiAssistant) is NOT this back-end's crash" (
        -not (Test-CrashedLeftover -ProcessId 54617 -ThreadCount 0 -LogDir $tmpLeft -Since $old))
    Check 'the -MaxThreads knob is the ceiling it claims to be (8 accepts what 4 rejects)' (
        (Test-CrashedLeftover -ProcessId 59936 -ThreadCount 5 -LogDir $tmpLeft -Since $old -MaxThreads 8) -and
        -not (Test-CrashedLeftover -ProcessId 59936 -ThreadCount 5 -LogDir $tmpLeft -Since $old -MaxThreads 4))
} finally { Remove-Item -LiteralPath $tmpLeft -Recurse -Force -ErrorAction SilentlyContinue }
Check 'the script uses the documented ceiling of 4 threads' ($lv52Text -match '\$CrashedLeftoverMaxThreads = 4')

# What may be stopped, and by what route. ONE Stop-Process in the whole script, by -Id, inside
# Close-CrashedBackend, which re-checks the process NAME first (pid recycling).
$stopCalls = $lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] -and $a.GetCommandName() -eq 'Stop-Process' }, $true)
Check 'exactly ONE Stop-Process call in LaunchVrf52, and it stops a PID, not a name' (
    @($stopCalls).Count -eq 1 -and $stopCalls[0].Extent.Text -match '-Id \$ProcessId' -and $stopCalls[0].Extent.Text -notmatch '-Name') (
    ($stopCalls | ForEach-Object { $_.Extent.Text }) -join ' | ')
Check 'that Stop-Process lives inside Close-CrashedBackend (nowhere else can reach it)' (
    $closeFn[0].Extent.Text.Contains($stopCalls[0].Extent.Text))
Check 'Close-CrashedBackend re-checks the process NAME against -ExpectedName before stopping (pid recycling)' (
    $closeFn[0].Extent.Text -match '\$p\.Name -ne \$ExpectedName' -and
    $closeFn[0].Extent.Text.IndexOf('$p.Name -ne $ExpectedName') -lt $closeFn[0].Extent.Text.IndexOf('Stop-Process'))
Check 'Close-CrashedBackend tries AnswerCrashDumpDialog.ps1 BEFORE Stop-Process' (
    $closeFn[0].Extent.Text -match 'AnswerCrashDumpDialog\.ps1' -and
    $closeFn[0].Extent.Text.IndexOf('AnswerCrashDumpDialog.ps1') -lt $closeFn[0].Extent.Text.IndexOf('Stop-Process'))
Check 'nothing in LaunchVrf52 stops a process by name, and no RTI name appears near the stop' (
    $stopCalls[0].Extent.Text -notmatch 'rti')

# Where the two closes may be called from - exactly two call sites, each behind its own gate.
$closeCalls = @($lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] -and $a.GetCommandName() -eq 'Close-CrashedBackend' }, $true))
Check 'exactly TWO Close-CrashedBackend call sites: our own crashed pid, and the opted-in leftover' (
    $closeCalls.Count -eq 2 -and
    @($closeCalls | Where-Object { $_.Extent.Text -match '\$simProc\.Id' }).Count -eq 1 -and
    @($closeCalls | Where-Object { $_.Extent.Text -match '\$bp\.Id' }).Count -eq 1) (
    ($closeCalls | ForEach-Object { $_.Extent.Text }) -join ' | ')
$ifLeave = @($lv52Ast.FindAll({ param($a)
    $a -is [System.Management.Automation.Language.IfStatementAst] -and
    $a.Clauses[0].Item1.Extent.Text -eq '$LeaveCrashedProcess' -and $a.Extent.Text -match 'Close-CrashedBackend' }, $true))
Check '-LeaveCrashedProcess really opts out: the own-pid close is in the ELSE branch only' (
    $ifLeave.Count -eq 1 -and $null -ne $ifLeave[0].ElseClause -and
    $ifLeave[0].ElseClause.Extent.Text -match 'Close-CrashedBackend -ProcessId \$simProc\.Id' -and
    $ifLeave[0].Clauses[0].Item2.Extent.Text -notmatch 'Close-CrashedBackend')
$ifLeftover = @($lv52Ast.FindAll({ param($a)
    $a -is [System.Management.Automation.Language.IfStatementAst] -and $a.Clauses[0].Item1.Extent.Text -match 'Test-CrashedLeftover' }, $true))
Check 'the leftover close sits behind BOTH gates: the classifier AND -CloseCrashedLeftover' (
    $ifLeftover.Count -eq 1 -and
    $ifLeftover[0].Extent.Text -match 'if \(\$CloseCrashedLeftover\)' -and
    $ifLeftover[0].Extent.Text -match 'Close-CrashedBackend -ProcessId \$bp\.Id')
Check 'the leftover branch never closes anything in -DryRun (it plans the close instead)' (
    $ifLeftover[0].Extent.Text -match 'if \(\$DryRun\) \{ Say-Plan')
Check 'the own-pid close is downstream of the -DryRun exit (a dry run can never reach it)' (
    $lv52Text.IndexOf("Say-Ok 'DRY-RUN complete") -gt 0 -and
    $lv52Text.IndexOf("Say-Ok 'DRY-RUN complete") -lt $lv52Text.IndexOf('Close-CrashedBackend -ProcessId $simProc.Id'))
Check 'an UNREADABLE thread count is not read as 0 (that is the corpse signature)' (
    $lv52Text -match '\$bThrOk = \$false' -and $lv52Text -match 'if \(\$bThrOk -and \(Test-CrashedLeftover')
Check 'the crash path still exits 3 after closing (the close does not turn a crash into a green)' (
    $lv52Text.IndexOf('Close-CrashedBackend -ProcessId $simProc.Id') -lt $lv52Text.IndexOf('NOT READY, NOT retried here') -and
    $lv52Text -match "NOT retried here[^\r\n]*\r?\n[^\r\n]*\r?\n[^\r\n]*\r?\n    exit 3")
# The precondition message is the operator's only instruction when the leftover predates the
# launch: it must SAY it is a corpse, and name the switch that closes it.
Check 'the precondition calls a pre-existing corpse a CRASHED LEFTOVER and names -CloseCrashedLeftover' (
    $lv52Text -match 'is a CRASHED LEFTOVER, not a running sim' -and
    $lv52Text -match 'closes a crashed back-end automatically ONLY on the launch that DETECTED the crash' -and
    $lv52Text -match 'rerun with -CloseCrashedLeftover')
Check 'the precondition re-inventories after a close, so a closed leftover no longer refuses the launch' (
    $lv52Text -match '# Re-inventory: only what is STILL running can refuse this launch\.')

# 8d. THE DRY-RUN FALSE GREEN (found 2026-09-04 while wiring Stage 2r). The -DryRun Result
# branch used to `exit 0` unconditionally, so a dry run that hit the runner's generic catch
# printed "[FAIL] unexpected terminating error ..." AND "DRY-RUN complete" AND exited 0. The
# live path always honoured $RunnerExit; only -DryRun did not. This is the only check here
# that RUNS THE RUNNER (against a throwaway copy beside the real one, so $PSScriptRoot and
# $RepoRoot resolve identically) - a pure AST assertion could not tell 0 from 5.
Write-Host '=== 8d. a terminating error yields a NONZERO exit, in -DryRun as in a live run ==='
$probe = Join-Path $RepoRoot 'scripts\_TerminatingErrorProbe.tmp.ps1'
try {
    $src = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1') -Raw
    $anchor = "        Say-Head 'DRY RUN - the full planned sequence, in order. NOTHING below is executed.'"
    Check 'probe anchor found in the runner (the test is wired to real code)' ($src.Contains($anchor))
    $patched = $src.Replace($anchor, "        throw 'SYNTHETIC TERMINATING ERROR (RunnerTurnaround.Tests.ps1)'" + [Environment]::NewLine + $anchor)
    [System.IO.File]::WriteAllText($probe, $patched, (New-Object System.Text.UTF8Encoding($false)))
    $out  = & pwsh -NoProfile -File $probe -DryRun -SkipServerCheck 2>&1
    $code = $LASTEXITCODE
    $text = ($out | Out-String)
    Check 'a terminating error in -DryRun exits 5 (UNEXPECTED TERMINATING ERROR), not 0' ($code -eq 5) "exit=$code"
    Check 'it says the dry run FAILED and does not also claim completion' (
        $text -match 'DRY-RUN FAILED' -and $text -notmatch 'DRY-RUN complete\.') "exit=$code"
    Check 'the underlying error is still reported' ($text -match 'SYNTHETIC TERMINATING ERROR')
} finally { Remove-Item -LiteralPath $probe -Force -ErrorAction SilentlyContinue }
Check 'the probe copy was removed from scripts\' (-not (Test-Path -LiteralPath $probe))

# 9. Get-VrfUuidByName must parse BOTH app-log route-line forms. The app started
# logging the route's own uuid on 2026-09-02 with the route-uuid fix ("Route '<r>'
# (VRF_UUID:<route>) created; ..."); every run in the record before that logs the
# line without it. Run 20260902T153837Z proved the cost of missing one: the report-
# evidence gate reported "marking -> VRF_UUID unknown (no route line in the app log)"
# for all three taskees against a healthy 3/3 app log and the window ran to its cap.
Write-Host '=== 9. marking -> VRF_UUID mapping parses both app-log route-line forms ==='
$logOld = @"
      Task 'T_R5_CO1': CreateRoute 'T_R5_CO1 ROUTE' (3 pts) for 114.MechCoy; move deferred to route-created.
      Route 'T_R5_CO1 ROUTE' created; MoveAlongRoute issued for VRF_UUID:740c72ac-8f58-7e4c-9720-8791e818910f.
"@
$logNew = @"
      Task 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH': CreateRoute 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE' (3 pts) for 114.MechCoy; move deferred to route-created.
      Route 'T_R5_CO1_NAMELEN_PROBE_PADDING_TO_38CH ROUTE' (VRF_UUID:6ff952a3-1075-e846-8baf-5b722d23daf6) created; MoveAlongRoute issued for VRF_UUID:d4ee70b3-38c2-3a4e-9b79-387f87ad22a0.
"@
$logPatrol = @"
      Task 'T_SCR1': CreateRoute 'T_SCR1 ROUTE' (3 pts) for 1222.MechPlt; patrol deferred to route-created.
      Route 'T_SCR1 ROUTE' (VRF_UUID:598ee64f-b8c5-bc4b-a322-6a528ad5403e) created; PatrolRoute issued for VRF_UUID:7be55c4f-0cf5-e343-8c5a-0bc9cc550d0a (Reconnoiter).
"@
$mapOld    = Get-VrfUuidByName -AppLogText $logOld
$mapNew    = Get-VrfUuidByName -AppLogText $logNew
$mapPatrol = Get-VrfUuidByName -AppLogText $logPatrol
Check 'OLD form (pre-2026-09-02 record) still maps 114.MechCoy' (
    $mapOld.Contains('114.MechCoy') -and $mapOld['114.MechCoy'] -eq 'VRF_UUID:740c72ac-8f58-7e4c-9720-8791e818910f') ("got " + ($mapOld.Keys -join ','))
Check 'NEW form with the route uuid maps 114.MechCoy to the TASKEE uuid, not the route uuid' (
    $mapNew.Contains('114.MechCoy') -and $mapNew['114.MechCoy'] -eq 'VRF_UUID:d4ee70b3-38c2-3a4e-9b79-387f87ad22a0') ("got " + ($mapNew.Values -join ','))
Check 'NEW form, PatrolRoute variant maps too' (
    $mapPatrol.Contains('1222.MechPlt') -and $mapPatrol['1222.MechPlt'] -eq 'VRF_UUID:7be55c4f-0cf5-e343-8c5a-0bc9cc550d0a') ("got " + ($mapPatrol.Values -join ','))
Check 'the app log of run 20260902T153837Z maps all three taskees (the live regression)' (
    $(if (Test-Path (Join-Path $RepoRoot 'runs\20260902T153837Z_run\vrfc2simapp.log')) {
        $m = Get-VrfUuidByName -AppLogText (Get-Content -LiteralPath (Join-Path $RepoRoot 'runs\20260902T153837Z_run\vrfc2simapp.log') -Raw)
        @('114.MechCoy','1222.MechPlt','1.BdeHQ' | Where-Object { $m.Contains($_) }).Count -eq 3
    } else { $true }))

Write-Host ''
Write-Host ('{0} passed, {1} failed' -f $script:Pass, $script:Fail)
if ($script:Fail -gt 0) { exit 1 }
exit 0
