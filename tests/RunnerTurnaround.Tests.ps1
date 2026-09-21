# tests/RunnerTurnaround.Tests.ps1 - OFFLINE regression check for the runner's
# turnaround logic (docs/RUNNER_TURNAROUND_2026-09-01.md). No simulator, no server.
# Plain pwsh (no Pester dependency): exits 0 when every check passes, 1 otherwise,
# and prints one line per check.
# TWO exceptions to "no process start", both -DryRun and both launching nothing:
# check 8d runs the runner itself against a throwaway patched copy (the false green it
# guards is an EXIT CODE, which no static assertion can tell from 0), and check 8g runs
# LaunchVrf52 to read the sim COMMAND LINE it would use - an argument that is absent by
# design (--logFileName) cannot be proven absent from the AST alone.
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
#   4b2. the report-evidence label names the REAL terminal code (TASKCMPLT/TASKABRT),
#      never a hardcoded TASKCMPLT (V6i cosmetic defect, run 20260915T184048Z)
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

Write-Host '=== 4b2. report-evidence label names the REAL terminal code, not a hardcoded TASKCMPLT (V6i cosmetic defect) ==='
# Real V6i line shape (runs\20260915T184048Z_run\vrfc2simapp.log:154). The runner's own
# evidence line used to read "...first saw TASKCMPLT at 2026-09-15T18:44:21.516Z..." for
# this taskee even though its ONLY terminal report was TASKABRT
# (docs\experiments\V6_LIVE_JOIN_GATE_2026-09-15.md sec "COSMETIC DEFECT").
$v6iAbrtLine = 'SENT TASK STATUS REPORT (TASKABRT) taskee=001aa71b-4c26-a1ea-28b2-f7dfe8e76342 task=a5000000-0000-0000-0000-000000000001 - MALFORMED: this task''s geometry is not plausible ground for its taskee - a route vertex or a leg breaks the configured extent bounds (Vrf:MaxVertexFromTaskeeKm / Vrf:MaxRouteLegKm), so no VR-Forces task is issued and nothing is sent to the back end - task ''T_R5_PL1'': route vertex 1 at 58.70296,16.50923 is 8768.9 km from the taskee at 34.61296,-116.60049 (bound 100 km) - refused, not dispatched.'
$abrtDone = @(Get-TerminalTaskReports -AppLogText $v6iAbrtLine)
Check 'V6i TASKABRT line parses to one Code=TASKABRT record' ($abrtDone.Count -eq 1 -and $abrtDone[0].Code -eq 'TASKABRT' -and $abrtDone[0].Taskee -eq '001aa71b-4c26-a1ea-28b2-f7dfe8e76342') ("got Count=$($abrtDone.Count) Code=$($abrtDone[0].Code)")
$uAbrt = '001aa71b-4c26-a1ea-28b2-f7dfe8e76342'
$sAbrt = New-CompletionState
$sAbrt = Update-CompletionState -State $sAbrt -Taskees @($uAbrt) -TaskCount 1 -Completions $abrtDone -NowUtc $t0
Check 'Update-CompletionState.firstSeenCode carries TASKABRT for this taskee (not a default TASKCMPLT guess)' ($sAbrt.firstSeenCode[$uAbrt] -eq 'TASKABRT') ("got $($sAbrt.firstSeenCode[$uAbrt])")
# Pre-existing behaviour is unchanged for a plain TASKCMPLT record (no Code property at all -
# the pre-2026-09-14 caller shape codeCounts already defaults, and firstSeenCode must match).
$cmpltNoCode = @([pscustomobject]@{ Taskee = '139aa71b-75df-4888-4a5a-6056bae66242'; Task = 'a5000000-0000-0000-0000-000000000002' })
$sCmplt = New-CompletionState
$sCmplt = Update-CompletionState -State $sCmplt -Taskees @('139aa71b-75df-4888-4a5a-6056bae66242') -TaskCount 1 -Completions $cmpltNoCode -NowUtc $t0
Check 'firstSeenCode defaults to TASKCMPLT for a Code-less record (pre-2026-09-14 caller shape)' ($sCmplt.firstSeenCode['139aa71b-75df-4888-4a5a-6056bae66242'] -eq 'TASKCMPLT')

# Test-ReportEvidence: force the 'runner poll' fallback anchor (no CaptureEvidence.cmplt
# entry, no trace/name evidence) so ONLY the C2SIM-capture branch under test can satisfy,
# and check the printed reason names the REAL code instead of a hardcoded TASKCMPLT.
$anchorUtc = [datetime]::new(2026, 9, 15, 18, 44, 21, 516, [System.DateTimeKind]::Utc)
$posUtc    = $anchorUtc.AddSeconds(5)
$capEvAbrt = @{ pos = @{ $uAbrt = $posUtc }; posN = @{ $uAbrt = 7 }; cmplt = @{} }
$eAbrt = Test-ReportEvidence -Taskees @($uAbrt) -TaskeeNames @{} -NameToVrfUuid @{} -TraceText '' -ToleranceMeters 2.0 `
             -CaptureEvidence $capEvAbrt -CompletionUtcByTaskee @{ $uAbrt = $anchorUtc } -CodeByTaskee @{ $uAbrt = 'TASKABRT' }
Check 'TASKABRT case: report-evidence reason says "first saw TASKABRT at", never TASKCMPLT' (
    $eAbrt.PerTaskee[$uAbrt].satisfied -and $eAbrt.PerTaskee[$uAbrt].via -eq 'C2SIM-capture' -and
    $eAbrt.PerTaskee[$uAbrt].reason -match 'first saw TASKABRT at 2026-09-15T18:44:21\.516Z' -and
    $eAbrt.PerTaskee[$uAbrt].reason -notmatch 'TASKCMPLT') ($eAbrt.PerTaskee[$uAbrt].reason)

# Same wiring for an ordinary completion: the label must still say TASKCMPLT (this was
# already correct; -CodeByTaskee must not turn a real TASKCMPLT into something else).
$uCmplt = '139aa71b-75df-4888-4a5a-6056bae66242'
$capEvCmplt = @{ pos = @{ $uCmplt = $posUtc }; posN = @{ $uCmplt = 3 }; cmplt = @{} }
$eCmplt = Test-ReportEvidence -Taskees @($uCmplt) -TaskeeNames @{} -NameToVrfUuid @{} -TraceText '' -ToleranceMeters 2.0 `
             -CaptureEvidence $capEvCmplt -CompletionUtcByTaskee @{ $uCmplt = $anchorUtc } -CodeByTaskee @{ $uCmplt = 'TASKCMPLT' }
Check 'TASKCMPLT case: report-evidence reason still says "first saw TASKCMPLT at"' (
    $eCmplt.PerTaskee[$uCmplt].satisfied -and $eCmplt.PerTaskee[$uCmplt].reason -match 'first saw TASKCMPLT at') (
    $eCmplt.PerTaskee[$uCmplt].reason)

# Backward compatibility: an older caller that never passes -CodeByTaskee gets the honest
# 'TERMINAL' label, not a guessed TASKCMPLT (the guess is exactly the 2026-09-15 defect).
$eNoCode = Test-ReportEvidence -Taskees @($uAbrt) -TaskeeNames @{} -NameToVrfUuid @{} -TraceText '' -ToleranceMeters 2.0 `
             -CaptureEvidence $capEvAbrt -CompletionUtcByTaskee @{ $uAbrt = $anchorUtc }
Check '-CodeByTaskee omitted -> label says TERMINAL, not a guessed TASKCMPLT' (
    $eNoCode.PerTaskee[$uAbrt].reason -match 'first saw TERMINAL at' -and $eNoCode.PerTaskee[$uAbrt].reason -notmatch 'TASKCMPLT') (
    $eNoCode.PerTaskee[$uAbrt].reason)

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
# TIGHTENED (D7 lane): the two positions are taken from the INVOKE-EXTERNAL call sites, not from
# any occurrence of "-Name '<tool>'" in the file. The loose form matched a Get-Process -Name
# 'RtiProbe' in the Stage 1 inventory - a read-only process listing that launches nothing - and
# reported a stage-order violation that did not exist. Same property, measured on the thing that
# actually runs the stage.
Check 'runner: Stage 2r invokes StartRtiExec52 BEFORE the Stage 2c RtiProbe gate' (
    $runnerText.IndexOf("Stage 2r") -gt 0 -and
    $runnerText.IndexOf("Invoke-External -Name 'StartRtiExec52'") -gt 0 -and
    $runnerText.IndexOf("Invoke-External -Name 'RtiProbe'") -gt 0 -and
    $runnerText.IndexOf("Invoke-External -Name 'StartRtiExec52'") -lt $runnerText.IndexOf("Invoke-External -Name 'RtiProbe'"))
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
# FORWARDER PORT 5000 -> 5002 (main c0c4185, 2026-09-15; RUNBOOK 9c): an unrelated user
# process took 0.0.0.0:5000 after the forwarder died, so the rid and StartRtiExec52's default
# were moved together. This check was left behind and has been a FALSE RED on main ever since
# - the product is correct and the assertion was stale, exactly like 8d/8f were in 3c71025.
Check 'StartRtiExec52 defaults to the 4001/4001/5002 rendezvous of the golden connection' (
    "$($sreParams['TcpPort'].DefaultValue)" -eq '4001' -and "$($sreParams['UdpPort'].DefaultValue)" -eq '4001' -and
    "$($sreParams['ForwarderPort'].DefaultValue)" -eq '5002')
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

# 8f. --logFileName IS THE STARTUP-CRASH TRIGGER, SO IT IS NOT PASSED; THE VENDOR'S OWN LOG IS
# HARVESTED INSTEAD (2026-09-04, docs/experiments/PREREG_52_CRASH_BISECT_2026-09-04.md sec 5).
# Passing --logFileName crashed the sim at startup in 6 of 18 launches; omitting it in 0 of 12
# (Fisher's exact, one-sided, p = 0.031). A 22-character path inside the vendor's own log
# directory crashed too, so it is the OPTION, not the path. What is pinned here: the option is
# ABSENT by default, still reachable on purpose (-LogFileName), and the harvest picks the right
# vendor file for the right pid, copies rather than moves, and never changes the verdict.
Write-Host '=== 8f. --logFileName not passed by default; the vendor log is harvested instead ==='
Check 'LaunchVrf52 declares -LogFileName and it DEFAULTS TO EMPTY (option not passed)' (
    $lv52Params.ContainsKey('LogFileName') -and "$($lv52Params['LogFileName'].DefaultValue)" -match "^''$")
$simArgsInit = @($lv52Ast.FindAll({ param($a)
    $a -is [System.Management.Automation.Language.AssignmentStatementAst] -and
    $a.Left.Extent.Text -eq '$simArgs' -and $a.Operator -eq 'Equals' }, $true))
Check 'the sim argument list is BUILT without --logFileName' (
    $simArgsInit.Count -eq 1 -and $simArgsInit[0].Right.Extent.Text -notmatch 'logFileName') (
    ($simArgsInit | ForEach-Object { $_.Right.Extent.Text }) -join ' | ')
$logOptAppend = @($lv52Ast.FindAll({ param($a)
    $a -is [System.Management.Automation.Language.AssignmentStatementAst] -and
    $a.Left.Extent.Text -eq '$simArgs' -and $a.Operator -ne 'Equals' -and
    $a.Right.Extent.Text -match 'logFileName' }, $true))
$logOptIf = @($lv52Ast.FindAll({ param($a)
    $a -is [System.Management.Automation.Language.IfStatementAst] -and
    $a.Clauses[0].Item1.Extent.Text -match 'IsNullOrWhiteSpace\(\$LogFileName\)' -and
    $a.Clauses[0].Item2.Extent.Text -match "'--logFileName'" }, $true))
Check 'the ONE --logFileName append is guarded by a non-empty -LogFileName' (
    $logOptAppend.Count -eq 1 -and $logOptIf.Count -eq 1 -and
    $logOptIf[0].Clauses[0].Item2.Extent.Text.Contains($logOptAppend[0].Extent.Text)) (
    ($logOptAppend | ForEach-Object { $_.Extent.Text }) -join ' | ')
Check 'the bisect record and the crash rate are named in the script, where someone would re-enable it' (
    $lv52Text -match 'PREREG_52_CRASH_BISECT_2026-09-04' -and $lv52Text -match '6 (crashes|times) . 18 launches')
Check 'the secrets warning rides with the harvest (never attach it; send the callstack/dmp)' (
    $lv52Text -match 'DtPrintEnvironmentVariables' -and $lv52Text -match 'NEVER attach it to a ticket' -and
    $lv52Text -match '\.callstack\.log / \.dmp')
Check 'the harvest COPIES and never moves (the vendor is still writing)' (
    $lv52Text -match 'Copy-Item -LiteralPath \$src' -and $lv52Text -notmatch 'Move-Item')

# The finder and the copier, RUN (lifted from the shipped script by its own AST, like 8c/8e).
# Real vendor filename shape, from C:\MAK\logs on 2026-09-04:
# vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.log (+ the .callstack.log beside it).
$vlogFn = $lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Get-VendorSimLogForPid' }, $true)
$copyFn = $lv52Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Copy-VendorSimLog' }, $true)
Check 'LaunchVrf52 defines Get-VendorSimLogForPid(-ProcessId, -LogDir, -Since)' (
    @($vlogFn).Count -eq 1 -and
    (@('ProcessId','LogDir','Since') | Where-Object { $vlogFn[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains $_ }).Count -eq 3)
Check 'LaunchVrf52 defines Copy-VendorSimLog(-ProcessId, -LogDir, -Since, -Destination)' (
    @($copyFn).Count -eq 1 -and
    (@('ProcessId','LogDir','Since','Destination') | Where-Object { $copyFn[0].Body.ParamBlock.Parameters.Name.VariablePath.UserPath -contains $_ }).Count -eq 4)
# The copier writes through the script's Say-* helpers, which live in the script and not here,
# so it runs inside a scope with stubs of its own; nothing outside this block sees them.
$script:HarvestMarkerLine = ''
& {
    $script:SayLines = @()
    function Say      { param([string]$m) $script:SayLines += $m }
    function Say-Ok   { param([string]$m) $script:SayLines += ('  [OK]   ' + $m) }
    function Say-Warn { param([string]$m) $script:SayLines += ('  [WARN] ' + $m) }
    Invoke-Expression $vlogFn[0].Extent.Text
    Invoke-Expression $copyFn[0].Extent.Text
    $tmpV = Join-Path ([System.IO.Path]::GetTempPath()) ('lv52vlog-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmpV -Force | Out-Null
    try {
        $old   = (Get-Date).AddHours(-1)
        $mine  = Join-Path $tmpV 'vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.log'
        $newer = Join-Path $tmpV 'vrfSimHLA1516e5.2d-20260904-081500-Legatus-282607-59936.log'
        Set-Content -LiteralPath $mine  -Encoding ascii -Value @('older copy', 'DtPrintEnvironmentVariables', 'AZURE_CLIENT_SECRET=redacted-in-this-fixture')
        Set-Content -LiteralPath $newer -Encoding ascii -Value @('NEWER copy for the same pid', 'DtPrintEnvironmentVariables')
        (Get-Item -LiteralPath $mine).LastWriteTime  = (Get-Date).AddMinutes(-10)
        (Get-Item -LiteralPath $newer).LastWriteTime = (Get-Date).AddMinutes(-1)
        Set-Content -LiteralPath (Join-Path $tmpV 'vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59937.log') -Encoding ascii -Value 'another pid'
        Set-Content -LiteralPath (Join-Path $tmpV 'rtiAssistant5.0.1-20260903-194550-Legatus-281993-59936.log')   -Encoding ascii -Value 'another exe, same pid'
        Set-Content -LiteralPath (Join-Path $tmpV 'vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.callstack.log') -Encoding ascii -Value 'Error Code - 0xC0000005'
        Check 'harvest picks the NEWEST vendor log for that pid' (
            (Get-VendorSimLogForPid -ProcessId 59936 -LogDir $tmpV -Since $old) -eq $newer) (
            "got " + (Get-VendorSimLogForPid -ProcessId 59936 -LogDir $tmpV -Since $old))
        Check "harvest ignores ANOTHER pid's log (59937)" (
            (Get-VendorSimLogForPid -ProcessId 59936 -LogDir $tmpV -Since $old) -notmatch '59937')
        Check "harvest ignores ANOTHER exe's log for the same pid (rtiAssistant in the shared C:\MAK\logs)" (
            (Get-VendorSimLogForPid -ProcessId 59936 -LogDir $tmpV -Since $old) -notmatch 'rtiAssistant')
        Check 'harvest never returns the .callstack.log (separate evidence, and the only file that may be shared)' (
            (Get-VendorSimLogForPid -ProcessId 59936 -LogDir $tmpV -Since $old) -notmatch 'callstack')
        Check 'a log OLDER than this launch is not harvested (pid recycling, same rule as the callstack)' (
            (Get-VendorSimLogForPid -ProcessId 59936 -LogDir $tmpV -Since (Get-Date).AddHours(1)) -eq '')
        Check 'a pid with no vendor log yields empty, and does not throw' (
            (Get-VendorSimLogForPid -ProcessId 12345 -LogDir $tmpV -Since $old) -eq '')
        Check 'a MISSING log directory yields empty, and does not throw' (
            (Get-VendorSimLogForPid -ProcessId 59936 -LogDir (Join-Path $tmpV 'nope') -Since $old) -eq '')
        # The copy itself: destination written, SOURCE STILL THERE, marker line emitted.
        $dst = Join-Path $tmpV 'harvested\vrfSim_9999_20260904T120000Z.log'
        $script:SayLines = @()
        $ret = Copy-VendorSimLog -ProcessId 59936 -LogDir $tmpV -Since $old -Destination $dst -Occasion 'READY'
        $script:HarvestSay = $script:SayLines
        Check 'the copy lands at the destination and returns it' (
            $ret -eq $dst -and (Test-Path -LiteralPath $dst) -and
            (Get-Content -LiteralPath $dst -Raw) -match 'NEWER copy for the same pid')
        Check 'it is a COPY: the vendor original is still in place (the sim is still writing to it)' (
            (Test-Path -LiteralPath $newer))
        Check 'the copy is announced with a src= / dst= marker line' (
            @($script:HarvestSay | Where-Object { $_ -match 'VENDOR LOG HARVESTED' }).Count -eq 1)
        Check 'the copy carries the SECRETS warning, and says DtPrintEnvironmentVariables was found in it' (
            @($script:HarvestSay | Where-Object { $_ -match 'SECRETS' -and $_ -match 'NEVER attach' }).Count -eq 1 -and
            @($script:HarvestSay | Where-Object { $_ -match 'DtPrintEnvironmentVariables IS PRESENT' }).Count -eq 1) (
            ($script:HarvestSay -join ' // '))
        # A missing vendor log must WARN and return '', never throw and never look like success:
        # the readiness verdict is the thread-count oracle's, not this function's.
        $script:SayLines = @()
        $none = Copy-VendorSimLog -ProcessId 12345 -LogDir $tmpV -Since $old -Destination (Join-Path $tmpV 'none.log') -Occasion 'READY'
        Check 'a MISSING vendor log warns loudly, copies nothing and returns empty (verdict unchanged)' (
            $none -eq '' -and -not (Test-Path -LiteralPath (Join-Path $tmpV 'none.log')) -and
            @($script:SayLines | Where-Object { $_ -match 'VENDOR LOG NOT FOUND' }).Count -eq 1 -and
            @($script:SayLines | Where-Object { $_ -match 'VENDOR LOG HARVESTED' }).Count -eq 0) (
            ($script:SayLines -join ' // '))
        $script:SayLines = @()
        $badDir = Copy-VendorSimLog -ProcessId 59936 -LogDir (Join-Path $tmpV 'nope') -Since $old -Destination (Join-Path $tmpV 'none2.log') -Occasion 'STARTUP-CRASH'
        Check 'a missing MAK log directory is a warning too, not a throw' ($badDir -eq '')
        $script:HarvestMarkerLine = @($script:HarvestSay | Where-Object { $_ -match 'VENDOR LOG HARVESTED' })[0]
    } finally { Remove-Item -LiteralPath $tmpV -Recurse -Force -ErrorAction SilentlyContinue }
}
# Both crash-path and ready-path harvests exist, and the crash one runs BEFORE the corpse is
# closed (a closed process can take its log's last lines with it, and MAK holds the file).
Check 'exactly TWO Copy-VendorSimLog call sites: the startup-crash path and the readiness path' (
    @([regex]::Matches($lv52Text, 'Copy-VendorSimLog -ProcessId \$simProc\.Id')).Count -eq 2)
Check 'the crash-path harvest runs BEFORE the corpse is closed' (
    $lv52Text.IndexOf("-Occasion 'STARTUP-CRASH'") -gt 0 -and
    $lv52Text.IndexOf("-Occasion 'STARTUP-CRASH'") -lt $lv52Text.IndexOf('Close-CrashedBackend -ProcessId $simProc.Id'))
# The guard is a CONTAINMENT property, not a layout one: asserting that the harvest is
# the very next line after the guard broke on 2026-09-04, when the SCENARIO-LOAD GATE put
# ~24 lines between them and left the guard itself untouched. Ask the AST the real
# question instead - exactly one `if (-not $simCrash.Crashed)` block contains a harvest,
# and it is the READY/NOT-READY one, never the crash-path copy.
$ifReadyHarvest = @($lv52Ast.FindAll({ param($a)
    $a -is [System.Management.Automation.Language.IfStatementAst] -and
    $a.Clauses[0].Item1.Extent.Text -eq '-not $simCrash.Crashed' -and
    $a.Clauses[0].Item2.Extent.Text -match 'Copy-VendorSimLog' }, $true))
Check 'the ready-path harvest is skipped when the launch crashed (the crash path already took it)' (
    $ifReadyHarvest.Count -eq 1 -and
    $ifReadyHarvest[0].Clauses[0].Item2.Extent.Text -match '-Occasion \$\(if \(\$backendHealthy\)' -and
    $ifReadyHarvest[0].Clauses[0].Item2.Extent.Text -notmatch 'STARTUP-CRASH') (
    "matches=$($ifReadyHarvest.Count)")

# The runner's 5.2 profile must carry all of it: no -LogFileName on the launch line, the
# manifest field, and a marker parse that really matches the line the launcher prints.
Check 'runner: the 5.2 launch line does NOT pass -LogFileName' ($runnerText -notmatch "'-LogFileName'")
Check 'runner: the manifest records logFileNamePassed = $false with the bisect citation' (
    $runnerText -match 'vendorLog\s*=' -and $runnerText -match 'logFileNamePassed = \$false' -and
    $runnerText -match 'PREREG_52_CRASH_BISECT_2026-09-04')
Check 'runner: the manifest records where the harvested log came from and went to' (
    $runnerText -match 'harvestedFrom' -and $runnerText -match 'harvestedTo' -and
    $runnerText -match 'vendorLog\.harvestedTo\s*=')
Check 'runner: the manifest carries the secrets warning about that copy' (
    $runnerText -match 'FULL PROCESS ENVIRONMENT IN CLEARTEXT' -and $runnerText -match 'never attach it to a ticket|NEVER attach it to a ticket')
# The parse is only worth anything if it matches what LaunchVrf52 actually printed above.
$harvestRx = 'VENDOR LOG HARVESTED occasion=(\S+) src=(.*?) dst=(.*?)\s*$'
Check 'runner: the harvest marker regex is the one used here' ($runnerText -match [regex]::Escape($harvestRx))
$hm8f = [regex]::Match($script:HarvestMarkerLine, $harvestRx)
Check 'that regex parses the REAL marker line the shipped Copy-VendorSimLog emitted' (
    $hm8f.Success -and $hm8f.Groups[1].Value -eq 'READY' -and
    $hm8f.Groups[2].Value -match '59936\.log$' -and $hm8f.Groups[3].Value -match 'vrfSim_9999_20260904T120000Z\.log$') (
    "line='$script:HarvestMarkerLine'")
# A path WITH SPACES must survive the same parse - src stops at ' dst=', dst runs to EOL.
$spacey = '  [OK]   VENDOR LOG HARVESTED occasion=STARTUP-CRASH src=C:\MAK my logs\vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.log dst=C:\repo dir\runs\launch52\vrfSim_3900_20260904T120000Z.log'
$hmSp = [regex]::Match($spacey, $harvestRx)
Check 'the marker parse survives spaces in both paths' (
    $hmSp.Success -and $hmSp.Groups[1].Value -eq 'STARTUP-CRASH' -and
    $hmSp.Groups[2].Value -eq 'C:\MAK my logs\vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.log' -and
    $hmSp.Groups[3].Value -eq 'C:\repo dir\runs\launch52\vrfSim_3900_20260904T120000Z.log') (
    "src='$($hmSp.Groups[2].Value)' dst='$($hmSp.Groups[3].Value)'")

# 8g. ...and the option really is off / on the sim's command line. The checks above are all
# static; only running the shipped launcher in -DryRun proves what would reach vrfSimHLA1516e.
# NOTHING is launched: -DryRun starts no process and the argument line is printed by the Plan
# section before any precondition can abort, so this holds on a machine without 5.2 installed.
Write-Host '=== 8g. the sim command line: no --logFileName by default, present when asked for ==='
$lv52Script = Join-Path $RepoRoot 'scripts\LaunchVrf52.ps1'
$dryDefault = (& pwsh -NoProfile -File $lv52Script -DryRun -NoGui -BackendAppNumber 9101 2>&1 | Out-String)
$dryOptIn   = (& pwsh -NoProfile -File $lv52Script -DryRun -NoGui -BackendAppNumber 9101 -LogFileName 'C:\MAK\logs\bisect-repeat.log' 2>&1 | Out-String)
$lineDefault = @($dryDefault -split "`r?`n" | Where-Object { $_ -match 'back-end\s+: .*vrfSimHLA1516e\.exe' })[0]
$lineOptIn   = @($dryOptIn   -split "`r?`n" | Where-Object { $_ -match 'back-end\s+: .*vrfSimHLA1516e\.exe' })[0]
Check 'the DEFAULT sim command line carries NO --logFileName (the 1-in-3 startup crash)' (
    $lineDefault -and $lineDefault -notmatch '--logFileName' -and $lineDefault -match '--notifyLevel 3') (
    "line='$lineDefault'")
Check '-LogFileName puts the option back, with the path given (a deliberate bisect repeat)' (
    $lineOptIn -and $lineOptIn -match '--logFileName "C:\\MAK\\logs\\bisect-repeat\.log"') (
    "line='$lineOptIn'")
Check 'the default dry run still says the option is NOT passed, and cites the bisect' (
    $dryDefault -match 'NOT PASSED' -and $dryDefault -match 'PREREG_52_CRASH_BISECT_2026-09-04')
Check 'the opt-in dry run WARNS that it is a ~1-in-3 startup crash' (
    $dryOptIn -match 'PASSED DELIBERATELY' -and $dryOptIn -match '1-IN-3 STARTUP CRASH')
Check 'the dry run plans the harvest and repeats the secrets warning' (
    $dryDefault -match 'would HARVEST' -and $dryDefault -match 'SECRETS' -and $dryDefault -match 'never be attached to a ticket or mail')

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
    # PINNED 64-BIT HOST + THE 5.2 PROFILE, for the two reasons 8h below spells out and
    # this check originally missed. Bare "pwsh" on this machine is the 32-BIT build, which
    # the runner's OWN bitness gate refuses at exit 2 BEFORE the injected error is reached;
    # and the default profile aborts at Stage 0 tool-existence validation, because only
    # Release-5.2 is built in this tree. Either one makes this a false RED (measured
    # 2026-09-15: bare pwsh gave exit=2 and "this runner is hosted in a 32-BIT PowerShell",
    # while the pinned host + profile gave exit=5 and DRY-RUN FAILED, as asserted below).
    $probePwsh = 'C:\Program Files\PowerShell\7\pwsh.exe'
    $out  = & $probePwsh -NoProfile -File $probe -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck 2>&1
    $code = $LASTEXITCODE
    $text = ($out | Out-String)
    Check 'a terminating error in -DryRun exits 5 (UNEXPECTED TERMINATING ERROR), not 0' ($code -eq 5) "exit=$code"
    Check 'it says the dry run FAILED and does not also claim completion' (
        $text -match 'DRY-RUN FAILED' -and $text -notmatch 'DRY-RUN complete\.') "exit=$code"
    Check 'the underlying error is still reported' ($text -match 'SYNTHETIC TERMINATING ERROR')
} finally { Remove-Item -LiteralPath $probe -Force -ErrorAction SilentlyContinue }
Check 'the probe copy was removed from scripts\' (-not (Test-Path -LiteralPath $probe))

# 8h. THE $script: FLAG THAT WAS NEVER SET ON THE DRY-RUN PATH (found live, V6b's
# first use of the launch lock, 2026-09-15, fixed on main 5cae9c5). Stage 1a sets
# $script:RunnerLockTaken = $true only in the LIVE lock-taking branch; a -DryRun
# never executes it, so under Set-StrictMode -Version Latest (RunnerLib.ps1, dot-
# sourced) the outer finally's "if ($script:RunnerLockTaken -and ...)" threw
# "the variable ... cannot be retrieved because it has not been set" on EVERY dry
# run (and any abort before Stage 1a), turning exit 0/2 into exit 1. Fixed by
# initialising $script:RunnerLockTaken = $false before Stage 1a runs. This is
# ANOTHER check that runs the real runner (like 8d): a pure AST/text assertion
# cannot tell a StrictMode runtime throw from a clean exit. Uses the PINNED
# 64-bit pwsh (item 1 of this same RUNBOOK section) rather than bare "pwsh" -
# bare "pwsh" on this machine is the 32-bit build and is refused by the runner's
# OWN bitness gate before Stage 1a is ever reached, which would make this check
# pass or fail for the wrong reason entirely. -VrfProfile 5.2 -NoGui: the only
# profile actually built in this tree (src/VrfC2SimApp/bin has Release-5.2, not
# Release) - the default profile would abort at Stage 0 tool-existence validation
# before Stage 1a, for a reason that has nothing to do with the lock.
Write-Host '=== 8h. -DryRun does not throw under StrictMode and reports the launch lock ==='
$dryLockPwsh   = 'C:\Program Files\PowerShell\7\pwsh.exe'
$dryLockScript = Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1'
$dryLockOut  = (& $dryLockPwsh -NoProfile -File $dryLockScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck 2>&1 | Out-String)
$dryLockCode = $LASTEXITCODE
# THE ASSERTION THAT WOULD HAVE CAUGHT THE DEFECT, in the coordinator's own
# words: a dry run must end "0/2", never 1 - 0 clean, 2 a legitimate validation/
# process refusal (this machine may have a real leftover observer or another
# runner live; that is environment noise Stage 1 owns, not this check - see 8d's
# own history of the same fragility). Exit 1 was ONLY ever the StrictMode
# "$script:RunnerLockTaken ... has not been set" throw.
Check '-DryRun exits 0 or 2, never 1 (the StrictMode "variable ... has not been set" throw)' (
    $dryLockCode -in @(0, 2)) "exit=$dryLockCode"
Check '-DryRun prints the Stage 1a "would take the exclusive lock" plan line' (
    $dryLockOut -match 'would take the exclusive lock')
Check '-DryRun never reports the RunnerLockTaken StrictMode throw' (
    $dryLockOut -notmatch "RunnerLockTaken.*cannot be retrieved")

# 8i. `--sample-threads` WROTE ITS ARTIFACT WHERE NOBODY LOOKED (found 2026-09-15;
# 20260915T114001Z_run / 20260915T124231Z_run / 20260915T130627Z_run all passed
# --sample-threads and none had a thread-sample file in the run directory or the manifest).
# The sampler DID run - three real CSVs, 188/185/219 rows - at
# runs/launch52/RunScenario-<stamp>.threads.csv, under a stamp scripts/RunScenario.sh takes
# for itself BEFORE the runner starts, 1-4 s off the stamp RunC2SimScenario.ps1 picks
# independently for its OWN run directory (~line 1839). Fixed in scripts/RunScenario.sh: the
# sampler now waits on the runner's run-directory pointer (runs/launch52/last-run-dir.txt)
# and writes <runDir>\thread-samples.csv, recorded in the manifest as
# artifacts.threadSamples (RUNBOOK 0.5.11 item 16). TWO independent OFFLINE assertions, in
# the style of 8h: (1) the wrapper's dry-run plan line, which a static read cannot prove
# right any more than 8h's StrictMode throw could - this runs the REAL bash script, like
# 8d/8g/8h run the real ps1; (2) a DIRECT SampleThreads.ps1 invocation, offline, against a
# throwaway renamed copy of timeout.exe (never vrfSimHLA1516e, never VR-Forces, nothing to
# tear down), proving the instrument itself still writes real rows.
Write-Host '=== 8i. --sample-threads: wrapper dry-run plan + a direct sampler invocation writes real rows ==='
$sampleBash = 'C:\Program Files\Git\bin\bash.exe'
if (-not (Test-Path -LiteralPath $sampleBash)) {
    Check '8i-1 dry-run plan SKIPPED (no bash.exe at the pinned Git path)' $true
} else {
    function ConvertTo-PosixPathLocal([string]$WinPath) {
        $p = $WinPath -replace '\\', '/'
        if ($p -match '^([A-Za-z]):(.*)$') { return ('/' + $Matches[1].ToLower() + $Matches[2]) }
        return $p
    }
    $sampleWrapperPosix = ConvertTo-PosixPathLocal (Join-Path $RepoRoot 'scripts\RunScenario.sh')
    $sampleDryOut = (& $sampleBash $sampleWrapperPosix '--dry-run' '--sample-threads' 2>&1 | Out-String)
    Check '8i-1 the wrapper accepted the flags (no usage/unknown-option text)' (
        $sampleDryOut -notmatch 'unknown option' -and $sampleDryOut -notmatch 'usage: scripts/RunScenario\.sh')
    Check '8i-1 dry run still prints the historical plan prefix (docs/tests grep on it)' (
        $sampleDryOut -match [regex]::Escape('thread sampler: WOULD start SampleThreads.ps1 -ProcessName vrfSimHLA1516e -MaxSec') -and
        $sampleDryOut -match [regex]::Escape('-IntervalSec 5'))
    Check '8i-1 dry run now says the csv lands in the run directory, not a bare stamp file' (
        $sampleDryOut -match 'thread-samples\.csv')
}

Write-Host '--- 8i-2. a direct SampleThreads.ps1 invocation against a throwaway process ---'
$samplerTestDir = Join-Path ([System.IO.Path]::GetTempPath()) ('sampler-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $samplerTestDir -Force | Out-Null
$samplerTargetExe = Join-Path $samplerTestDir 'RunnerTurnaroundSamplerTarget.exe'
$samplerCsvPath   = Join-Path $samplerTestDir 'thread-samples.csv'
$samplerTargetProc = $null
try {
    Copy-Item -LiteralPath (Join-Path $env:WINDIR 'System32\timeout.exe') -Destination $samplerTargetExe -Force
    $samplerTargetProc = Start-Process -FilePath $samplerTargetExe -ArgumentList '/T','60','/NOBREAK' -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 1
    & 'C:\Program Files\PowerShell\7\pwsh.exe' -NoProfile -ExecutionPolicy Bypass `
        -File (Join-Path $RepoRoot 'scripts\SampleThreads.ps1') `
        -ProcessName 'RunnerTurnaroundSamplerTarget' -OutFile $samplerCsvPath -MaxSec 19 -IntervalSec 5 | Out-Null
    $samplerRows = @()
    if (Test-Path -LiteralPath $samplerCsvPath) { $samplerRows = @(Get-Content -LiteralPath $samplerCsvPath) }
    Check '8i-2 the csv header is the documented column set' (
        $samplerRows.Count -ge 1 -and
        $samplerRows[0] -eq 'tUtc,tSec,pid,procCpuCores,wsMB,threads,top1,top2,top3,top4,top5,top6,top7,top8')
    Check '8i-2 a direct sampler invocation writes >= 3 data rows against a real process' (
        $samplerRows.Count -ge 4) "got $($samplerRows.Count) lines including header"
    if ($samplerRows.Count -ge 2) {
        $samplerFirstRow = $samplerRows[1] -split ','
        Check '8i-2 the pid column matches the throwaway process' (
            $samplerTargetProc -and $samplerFirstRow[2] -eq [string]$samplerTargetProc.Id) (
            "row pid " + $samplerFirstRow[2] + " vs started " + $(if ($samplerTargetProc) { $samplerTargetProc.Id } else { '?' }))
        Check '8i-2 the row carries utc/threads/cpu/working-set columns' (
            $samplerFirstRow[0] -match '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d' -and
            $samplerFirstRow[3] -match '^[0-9.]+$' -and
            $samplerFirstRow[4] -match '^[0-9.]+$' -and
            $samplerFirstRow[5] -match '^[0-9]+$') ("row: " + $samplerRows[1])
    }
} finally {
    if ($samplerTargetProc -and -not $samplerTargetProc.HasExited) {
        Stop-Process -Id $samplerTargetProc.Id -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $samplerTestDir -Recurse -Force -ErrorAction SilentlyContinue
}

# 8j. WS RUNAWAY TRIPWIRE (RUNBOOK 0.5.11 item 17, 2026-09-15): SampleThreads.ps1 -ReplayCsv
# replays a captured thread-samples.csv through the SAME slope/cpu-gate/warmup logic
# (Add-WsSlopeSample) the live sampler uses - this IS the offline proof, not a
# reimplementation. Two real fixtures: runs\launch52\RunScenario-20260915T130626Z.threads.csv
# is the confirmed back-end memory runaway (wsMB grew from 3,117 while procCpuCores stayed
# under 1 core, starting at the 13:11:50Z order) and must alert; the same-day healthy run
# RunScenario-20260915T133258Z.threads.csv has an ordinary CPU-busy startup/scenario-load ramp
# to ~4,000 MB that plateaus and must NOT alert - a slope-only rule with no cpu gate false-
# alarms on that exact ramp (procCpuCores 1.7-4.1 there versus under 1 during the real
# runaway), which is why the tripwire gates on both. Both CSVs are untracked (runs\ is
# gitignored), so this check SKIPS gracefully if they are not present on this machine.
Write-Host '=== 8j. WS runaway tripwire: -ReplayCsv proof against the two 2026-09-15 fixtures ==='
$wsRunawayCsv = Join-Path $RepoRoot 'runs\launch52\RunScenario-20260915T130626Z.threads.csv'
$wsHealthyCsv = Join-Path $RepoRoot 'runs\launch52\RunScenario-20260915T133258Z.threads.csv'
$wsPwsh = 'C:\Program Files\PowerShell\7\pwsh.exe'
if (-not (Test-Path -LiteralPath $wsRunawayCsv) -or -not (Test-Path -LiteralPath $wsHealthyCsv) -or -not (Test-Path -LiteralPath $wsPwsh)) {
    Check '8j SKIPPED (fixture CSVs or pinned pwsh not present on this machine)' $true
} else {
    $wsSampler = Join-Path $RepoRoot 'scripts\SampleThreads.ps1'
    $wsRunawayOut = (& $wsPwsh -NoProfile -File $wsSampler -ReplayCsv $wsRunawayCsv 2>&1 | Out-String)
    $wsHealthyOut = (& $wsPwsh -NoProfile -File $wsSampler -ReplayCsv $wsHealthyCsv 2>&1 | Out-String)
    Check '8j runaway fixture (130626Z) DOES alert' (
        $wsRunawayOut -match 'BACK-END WS RUNAWAY') ("output: " + $wsRunawayOut)
    $wsFirstAlertTime = $null
    if ($wsRunawayOut -match '(\S+) BACK-END WS RUNAWAY') {
        $wsFirstAlertTime = [datetime]::Parse($Matches[1], [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
    }
    $wsOnset = [datetime]::Parse('2026-09-15T13:11:50Z', [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
    Check '8j runaway fixture alerts within ~2 min of the documented 13:11:50Z onset' (
        $wsFirstAlertTime -and [Math]::Abs(($wsFirstAlertTime - $wsOnset).TotalSeconds) -le 120) (
        "first alert: " + $(if ($wsFirstAlertTime) { $wsFirstAlertTime.ToString('o') } else { '(none matched)' }))
    Check '8j healthy fixture (133258Z) does NOT alert' (
        $wsHealthyOut -notmatch 'BACK-END WS RUNAWAY' -and $wsHealthyOut -match '0 alerts') ("output: " + $wsHealthyOut)
}

# 8k. STAGE 2h - THE FEDERATION HOLDER (STP-825, 2026-09-15). On the 5.2 profile nothing
# creates the federation before the SIM does: Stage 2c's RtiProbe creates MAK-ONE-2025 and
# then DESTROYS it again on resign, being the last federate in it. Since 14:23Z rtiexec 5.0.1
# rejects a CREATOR's FOM-module distribution intermittently ("Sending Create Response =
# Error"), so the sim dies at startup - after a full launch cycle and a whole ledger block of
# burned appNumbers. Stage 2h starts a HOLDER federate first, so the gate and the sim both
# take the JOIN path (joins have never failed; confirmed live, runs 20260915T151959Z /
# 20260915T160253Z). Like 8d/8g/8h this runs the REAL runner in -DryRun, because what is
# asserted is what the stage would DO - a command line, a stage ORDERING and an appNumber
# ALLOCATION - and a static read of the AST proves none of the three. NOTHING is launched:
# -DryRun starts no process, contacts no server and does not advance the ledger marker.
#
# SKIPPED (not failed) in a checkout with no Release-5.2 build tree: Stage 0's tool-existence
# validation aborts there long before Stage 2h, and asserting on plan lines that were never
# reached is the false-RED shape 3c71025 already had to undo twice.
Write-Host '=== 8k. Stage 2h federation holder: planned by default, omitted at -FederationHoldSecs 0 ==='
$holdPwsh    = 'C:\Program Files\PowerShell\7\pwsh.exe'
$holdScript  = Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1'
$holdOn      = (& $holdPwsh -NoProfile -File $holdScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck 2>&1 | Out-String)
$holdOnCode  = $LASTEXITCODE
$holdOff     = (& $holdPwsh -NoProfile -File $holdScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck -FederationHoldSecs 0 2>&1 | Out-String)
$holdOffCode = $LASTEXITCODE
if ($holdOn -notmatch 'DRY RUN - the full planned sequence' -or $holdOff -notmatch 'DRY RUN - the full planned sequence') {
    Check '8k SKIPPED - neither dry run reached the planned sequence in this checkout (Stage 0 aborts on the missing Release-5.2 binaries)' $true
} else {
    Check '8k both dry runs exit 0 or 2, never 1 (the StrictMode "has not been set" throw - 8h''s defect class)' (
        $holdOnCode -in @(0, 2) -and $holdOffCode -in @(0, 2)) "on=$holdOnCode off=$holdOffCode"
    Check '8k the plan lists the holder stage and the exact RtiProbe command line it would run' (
        $holdOn -match 'would start the federation holder RtiProbe\.exe <appNo> \S+ 1 900 3 DETACHED' -and
        $holdOn -match 'RtiProbe\.exe \d+ \S+ 1 900 3')
    Check '8k the holder stage is planned BEFORE the Stage 2c RTI readiness gate' (
        $holdOn.IndexOf('Stage 2h - federation HOLDER') -gt 0 -and
        $holdOn.IndexOf('Stage 2h - federation HOLDER') -lt $holdOn.IndexOf('Stage 2c - RTI readiness gate'))
    Check '8k the plan allocates ONE ledgered appNumber PER ATTEMPT and consumes none in a dry run' (
        $holdOn -match 'would allocate 4 appNumbers for it, ONE PER ATTEMPT \(\d+,\d+,\d+,\d+\) - a dry run consumes NONE' -and
        $holdOn -match 'fedHold1' -and $holdOn -match 'fedHold4')
    Check '8k the plan reads the join from the RTIEXEC LOG, pid-anchored (the holder stdout cannot say it in time)' (
        $holdOn -match 'would wait up to 45s per attempt for the rtiexec log line: Federate remoteControl <holderPid> \.\.\. has joined federation')
    Check '8k the plan says teardown LEAVES the holder joined and does NOT wait for it' (
        $holdOn -match 'would LEAVE the holder joined at teardown and NOT wait for it')
    Check '8k -FederationHoldSecs 0 OMITS the stage: no holder start line, no fedHold appNumber' (
        $holdOff -notmatch 'would start the federation holder' -and $holdOff -notmatch 'fedHold')
    Check '8k -FederationHoldSecs 0 says out loud that the SIM then CREATES the federation (the STP-825 failure mode)' (
        $holdOff -match 'the holder is DISABLED, so the SIM becomes the federation CREATOR')
}
# THE REGRESSION THIS PINS, caught in review before it shipped: the 5.0.2 refusal first
# tested the VALUE of -FederationHoldSecs, and its default is 900 - so EVERY 5.0.2 run would
# have aborted at Stage 0 validation over a parameter its operator never typed. The 5.0.2
# profile must stay byte-for-byte what it was: a default dry run there may not mention the
# holder at all. This assertion holds even in a checkout with no 5.0.2 binaries, because a
# refusal would land in that run's Stage 0 Result block, which is always printed.
$holdLegacy = (& $holdPwsh -NoProfile -File $holdScript -DryRun -SkipServerCheck 2>&1 | Out-String)
Check '8k the 5.0.2 profile is untouched: a DEFAULT dry run there never mentions the holder' (
    $holdLegacy -notmatch 'FederationHold' -and $holdLegacy -notmatch 'Stage 2h')

# Static half: the holder is a JOINED FEDERATE, so it must be started detached and must never
# be force-killed or waited on. Complete-Background is this runner's only wait-for-a-child
# helper; the holder must never be handed to it (RUNBOOK sec 0).
Check '8k runner: the holder is started DETACHED (own console) and never passed to Complete-Background' (
    $runnerText -match 'FederationHolder' -and
    $runnerText -match '-NewConsole -Note \$holderNote' -and
    $runnerText -notmatch 'Complete-Background[^\r\n]*FederationHolder' -and
    $runnerText -match 'NOT killed, NOT waited for')

# 8l. THE WS RUNAWAY ABORT (RUNBOOK 0.5.11 item 17 extension, 2026-09-15 - V6f harvest defects
# 1+2). scripts\SampleThreads.ps1's tripwire only WARNS: check 8j already proves the alert file
# itself; this proves the RUNNER now polls it live and would act. Like 8h/8k this runs the REAL
# runner in -DryRun (a command line and a stage ORDERING, which a static AST read cannot prove
# right), and SKIPS (as a PASS) in a checkout with no Release-5.2 build tree, for the same
# reason 8k does - Stage 0 aborts before Stage 8b is ever planned there. The plan text is split
# across several SHORTER anchors (the style 8i already uses for this exact reason) because the
# real line is long enough that -DryRun output piped through Out-String can wrap it.
Write-Host '=== 8l. WS runaway abort: the plan lists it, -WsRunawayAbortAfter 0 omits it ==='
Check '8l runner declares -WsRunawayAbortAfter default 3' (
    $params.ContainsKey('WsRunawayAbortAfter') -and "$($params['WsRunawayAbortAfter'].DefaultValue)" -eq '3')

# 8l-0. Get-WsRunawayAlertsSinceDispatch (RunnerLib.ps1) - the PURE filter the runner's Stage 8b
# poll calls. Fixture text is the REAL V6f alerts file (runs\20260915T160411Z_run,
# thread-samples.alerts.txt): 6 confirmed episodes, the FIRST at 16:07:20.469Z is the
# object-creation-burst alert (V6f harvest defect 2) firing ~9.9s after the order reached the
# bus at 16:07:10.545Z (both AFTER dispatch, so this filter does not and should not suppress
# it - that is SampleThreads.ps1's own -WarmupResetAtUtc job, checked separately below).
$wsAlertsFixtureText = @'
2026-09-15T16:07:20.4692982Z BACK-END WS RUNAWAY: 1098.8 MB/min over 30.1 s, ws now 3470 MB, pid 47388
2026-09-15T16:08:25.7001938Z BACK-END WS RUNAWAY: 692.2 MB/min over 30.1 s, ws now 3913 MB, pid 47388
2026-09-15T16:10:21.0895045Z BACK-END WS RUNAWAY: 2263.6 MB/min over 30.1 s, ws now 6815 MB, pid 47388
'@
$wsDispatchReal = [datetime]::Parse('2026-09-15T16:07:10.545Z', [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
$wsSinceReal = @(Get-WsRunawayAlertsSinceDispatch -AlertsText $wsAlertsFixtureText -DispatchUtc $wsDispatchReal)
Check '8l-0 all 3 V6f alerts are AFTER dispatch and all 3 are kept' ($wsSinceReal.Count -eq 3) "got $($wsSinceReal.Count)"
$wsDispatchLate = [datetime]::Parse('2026-09-15T16:09:00Z', [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
$wsSinceLate = @(Get-WsRunawayAlertsSinceDispatch -AlertsText $wsAlertsFixtureText -DispatchUtc $wsDispatchLate)
Check '8l-0 a LATER synthetic dispatch drops the two alerts before it, keeps the one after' (
    $wsSinceLate.Count -eq 1 -and $wsSinceLate[0] -match '16:10:21') "got $($wsSinceLate.Count): $($wsSinceLate -join ' | ')"
Check '8l-0 empty alerts text yields an empty array, not a throw' (
    @(Get-WsRunawayAlertsSinceDispatch -AlertsText '' -DispatchUtc $wsDispatchReal).Count -eq 0)
Check '8l-0 a line with an unparseable leading timestamp is CONSERVATIVELY KEPT, not dropped' (
    @(Get-WsRunawayAlertsSinceDispatch -AlertsText 'NOT-A-TIMESTAMP BACK-END WS RUNAWAY: garbage' -DispatchUtc $wsDispatchReal).Count -eq 1)
$wsPwshDry    = 'C:\Program Files\PowerShell\7\pwsh.exe'
$wsDryScript  = Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1'
$wsDryOn      = (& $wsPwshDry -NoProfile -File $wsDryScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck 2>&1 | Out-String)
$wsDryOnCode  = $LASTEXITCODE
$wsDryOff     = (& $wsPwshDry -NoProfile -File $wsDryScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck -WsRunawayAbortAfter 0 2>&1 | Out-String)
$wsDryOffCode = $LASTEXITCODE
if ($wsDryOn -notmatch 'DRY RUN - the full planned sequence' -or $wsDryOff -notmatch 'DRY RUN - the full planned sequence') {
    Check '8l SKIPPED - neither dry run reached the planned sequence in this checkout (Stage 0 aborts on the missing Release-5.2 binaries)' $true
} else {
    Check '8l both dry runs exit 0 or 2, never 1 (the StrictMode "has not been set" throw - 8h''s defect class)' (
        $wsDryOnCode -in @(0, 2) -and $wsDryOffCode -in @(0, 2)) "on=$wsDryOnCode off=$wsDryOffCode"
    Check '8l the default plan polls the alerts file every 10s for BACK-END WS RUNAWAY alerts' (
        $wsDryOn -match 'would poll' -and $wsDryOn -match 'thread-samples\.alerts\.txt' -and
        $wsDryOn -match 'every 10s' -and $wsDryOn -match 'BACK-END WS' -and $wsDryOn -match 'RUNAWAY alerts')
    Check '8l the default plan says it would abort via Stop-Runner 6 at the default count of 3' (
        $wsDryOn -match 'Stop-Runner 6' -and $wsDryOn -match 'exit 6' -and $wsDryOn -match 'once 3 such alert')
    Check '8l the default plan names -WsRunawayAbortAfter 0 as the off switch' (
        $wsDryOn -match [regex]::Escape('-WsRunawayAbortAfter 0 disables this check'))
    Check '8l -WsRunawayAbortAfter 0 OMITS the abort-rule plan line entirely' (
        $wsDryOff -notmatch 'WS RUNAWAY' -and $wsDryOff -notmatch 'thread-samples\.alerts\.txt')
}
# THE REGRESSION THIS PINS: -WsRunawayAbortAfter defaults to 3 (armed), so a default dry run
# that never mentions it would be the "no abort rule at all" false-green this whole item exists
# to close - the same shape 8k's own final assertion guards for -FederationHoldSecs.
$wsDryLegacy = (& $wsPwshDry -NoProfile -File $wsDryScript -DryRun -SkipServerCheck 2>&1 | Out-String)
if ($wsDryLegacy -match 'DRY RUN - the full planned sequence') {
    Check '8l a 5.0.2 default dry run still plans the abort rule (armed by default, profile-independent)' (
        $wsDryLegacy -match 'BACK-END WS' -and $wsDryLegacy -match 'RUNAWAY alerts')
} else {
    Check '8l 5.0.2 leg SKIPPED - that dry run did not reach the planned sequence either' $true
}
# Static half: a negative value is refused at validation, before anything is launched.
Check '8l runner: a negative -WsRunawayAbortAfter is refused at validation' (
    $runnerText -match 'WsRunawayAbortAfter must be >= 0')

# 8m. THE DEMO FEDERATION HOLDER (STP-825, 2026-09-15). Stage 2h (8k above) only protects the
# TEST harness path - the STANDALONE DEMO path (docs/DEMO_RUNBOOK.md: StartRtiExec52 ->
# LaunchVrf52 -> StartInterface52) has no runner in front of it, so LaunchVrf52.ps1 itself now
# starts a holder before its own back end, in the same posture (tools/RtiProbe.exe <appNo>
# <execName> 1 <holdSecs> 3, detached, never killed). The runner's OWN Stage 3 call site must
# pin -FederationHoldSecs 0 explicitly, so a 5.2 run through the runner NEVER starts a second
# holder on top of its own Stage 2h one (that would burn a second appNumber for nothing) - Stage
# 3's command line must stay byte-identical to what it was before LaunchVrf52 grew this switch.
# Like 8g/8k/8l this runs the REAL scripts in -DryRun (a command line and a default value, which
# a static AST read alone cannot prove was actually PASSED), and SKIPS (as a PASS) exactly where
# 8k does - a checkout with no Release-5.2 build tree never reaches the plan that would show it.
Write-Host '=== 8m. LaunchVrf52 STP-825 federation holder: planned by default, omitted at 0; runner Stage 3 pins 0 ==='
$lv52HoldOnOut  = (& $holdPwsh -NoProfile -File $lv52Script -DryRun -NoGui -BackendAppNumber 9101 2>&1 | Out-String)
$lv52HoldOnCode = $LASTEXITCODE
$lv52HoldOffOut = (& $holdPwsh -NoProfile -File $lv52Script -DryRun -NoGui -BackendAppNumber 9101 -FederationHoldSecs 0 2>&1 | Out-String)
$lv52HoldOffCode = $LASTEXITCODE
$lv52HoldOnFlat  = ($lv52HoldOnOut  -replace '\s+', ' ')
$lv52HoldOffFlat = ($lv52HoldOffOut -replace '\s+', ' ')
if ($lv52HoldOnOut -match 'STP-825 federation holder tool MISSING') {
    Check '8m SKIPPED - tools\RtiProbe.exe is not built in this checkout (no Release-5.2 tree); LaunchVrf52 hard-precondition-refuses before ever printing the holder plan' $true
} else {
    Check '8m both LaunchVrf52 dry runs exit 0 or 2, never anything else' (
        $lv52HoldOnCode -in @(0, 2) -and $lv52HoldOffCode -in @(0, 2)) "on=$lv52HoldOnCode off=$lv52HoldOffCode"
    Check '8m the DEFAULT dry run plans the STP-825 holder: RtiProbe.exe appNumber 9190, DETACHED, and names the retry number 9191' (
        $lv52HoldOnFlat -match 'would start the STP-825 federation HOLDER first: tools/RtiProbe\.exe 9190 \S+ 1 900 3, DETACHED' -and
        $lv52HoldOnFlat -match 'retrying ONCE on appNumber 9191 if the create is refused')
    Check '8m the default plan says it would refuse the launch (exit 3, naming STP-825) if neither attempt joins' (
        $lv52HoldOnFlat -match 'FAIL \(exit 3, naming STP-825\) and launch NOTHING')
    Check '8m the default startup banner reports the holder ON with its appNumber, retry number and hold' (
        $lv52HoldOnFlat -match 'Federation hold\s*: STP-825 holder ON - appNumber 9190 \(retry 9191\), hold 900s')
    Check '8m -FederationHoldSecs 0 OMITS the holder plan entirely (no RtiProbe start line, no appNumber 9190)' (
        $lv52HoldOffOut -notmatch 'would start the STP-825 federation HOLDER' -and $lv52HoldOffOut -notmatch '9190')
    Check '8m -FederationHoldSecs 0 says out loud that this launch''s OWN back end becomes the federation CREATOR (the STP-825 failure mode)' (
        $lv52HoldOffFlat -match "this launch's own back end will be the federation CREATOR")
}
# Static, build-tree-independent half: the parameters and their defaults are declared in the
# source, so this holds even in a checkout with nothing built at all.
Check '8m LaunchVrf52 declares -FederationHoldSecs defaulting to 900 (ON by default)' (
    $lv52Text -match '\[int\]\s+\$FederationHoldSecs\s+= 900')
Check '8m LaunchVrf52 declares -FederationHoldAppNumber defaulting to 9190 (free in the documented 9101-9199 demo block)' (
    $lv52Text -match '\[int\]\s+\$FederationHoldAppNumber = 9190')
Check '8m LaunchVrf52 retries on AppNumber + 1, never a hardcoded second literal' (
    $lv52Text -match '\[int\]\(\$FederationHoldAppNumber \+ 1\)')
Check '8m LaunchVrf52 never Stop-Process against the holder - it is a joined federate (RUNBOOK sec 0)' (
    -not ([regex]::IsMatch($lv52Text, 'holder[\s\S]{0,400}Stop-Process')) -and
    -not ([regex]::IsMatch($lv52Text, 'Stop-Process[\s\S]{0,400}holder')))

# Runner side: Stage 3's LaunchVrf52 call site must carry -FederationHoldSecs 0 EXPLICITLY,
# regardless of the RUNNER's OWN -FederationHoldSecs value (its Stage 2h holder already covers
# the sim before Stage 3 ever runs). Reuses $holdOn / $holdOff from 8k above (the SAME dry runs,
# same "does not advance the ledger" guarantee) rather than paying for two more runner
# invocations. Whitespace is collapsed before matching because Write-Host's long command-line
# plan lines wrap across the console width when captured, and the anchor '-NotifyLevel <n>
# -FederationHoldSecs 0' is specific to the Stage 3 command line - the runner's OWN Stage 2h
# prose repeats the bare phrase '-FederationHoldSecs 0' on the OFF path, so an un-anchored
# substring match would be a false green there.
if ($holdOn -notmatch 'DRY RUN - the full planned sequence') {
    Check '8m SKIPPED - the runner dry run above (8k) did not reach the planned sequence either' $true
} else {
    $holdOnFlat  = ($holdOn  -replace '\s+', ' ')
    $holdOffFlat = ($holdOff -replace '\s+', ' ')
    Check '8m runner Stage 3 pins LaunchVrf52 to -FederationHoldSecs 0 (runner''s own holder ON)' (
        $holdOnFlat -match '-NotifyLevel \d+ -FederationHoldSecs 0')
    Check '8m runner Stage 3 pins LaunchVrf52 to -FederationHoldSecs 0 even with the runner''s OWN holder OFF' (
        $holdOffFlat -match '-NotifyLevel \d+ -FederationHoldSecs 0')
}
# Static half of the same claim, independent of any build tree: the literal append is in the
# source, inside the 5.2-only branch (LaunchVrf.ps1, the 5.0.2 script, has no such switch).
Check '8m runner source: Stage 3 literally appends -FederationHoldSecs 0 to launchArgs' (
    $runnerText -match [regex]::Escape("`$launchArgs += @('-FederationHoldSecs', '0')"))

# 8m2. STP-825 D8, 2026-09-21 (run 20260921T045700Z): all four Stage 2h holders JOINED (every
# pid has a join line in the rtiexec log, all four processes stayed alive) but the runner
# reported "did not join within 45s" four times and refused the launch. Cause: some MAK
# rtiexec 5.0.1 instances write this log through an UNSERIALISED sink that DOUBLES and
# INTERLEAVES text token-by-token WITHIN a line, so the exact quoted phrase the old detector
# required never appears contiguously. Test-HolderJoinedInLog/Get-HolderPidLogLines
# (RunnerLib.ps1, reused verbatim - not dot-sourced, see LaunchVrf52.ps1's comment - in
# LaunchVrf52.ps1) are the pure functions both scripts now share/duplicate for this.
# $garbledJoin87404 and $garbledJoin74612 are copied VERBATIM out of the real sink: runs\
# launch52\rtiexec_20260921T032248Z5.0.1-20260920-232249-Legatus-281993-47636.log, lines
# 10685 (holder pid 87404) and 4051 (holder pid 74612) - both joins to federation MAK-ONE-2025.
Write-Host '=== 8m2. STP-825 holder join detection survives the garbled rtiexec log sink (Test-HolderJoinedInLog / Get-HolderPidLogLines) ==='
$cleanJoinLine    = 'Federate remoteControl 87404 ("remoteControl" 3) has joined federation "MAK-ONE-2025".'
$garbledJoin87404 = 'Federate Federate remoteControl 87404 ("remoteControl" 3)remoteControl 87404 ("remoteControl" 3) has joined federation " has joined federation "MAK-ONE-2025MAK-ONE-2025".'
$garbledJoin74612 = 'Federate Federate remoteControl 74612 ("remoteControl" 2)remoteControl 74612 ("remoteControl" 2) has joined federation " has joined federation "MAK-ONE-2025MAK-ONE-2025".'
$garbledTruncated = 'Federate Federate remoteControl 87404 ("remoteControl" 3)remoteControl 87404 ("remoteControl" 3) has joined federation " has joined federation "MAK-ONE-2025MAK-'
$resignedLine     = 'Federate remoteControl 87404 ("remoteControl" 3) has resigned from federation "MAK-ONE-2025".'

Check '8m2 the clean vendor line matches (pid 87404, MAK-ONE-2025)' (
    Test-HolderJoinedInLog -LogDelta $cleanJoinLine -ProcessId 87404 -FederationName 'MAK-ONE-2025')
Check '8m2 the verbatim garbled line (rtiexec log line 10685) matches for pid 87404 / MAK-ONE-2025' (
    Test-HolderJoinedInLog -LogDelta $garbledJoin87404 -ProcessId 87404 -FederationName 'MAK-ONE-2025')
Check '8m2 a truncated mid-doubling repeat ("...MAK-ONE-2025MAK-", no closing quote) still matches' (
    Test-HolderJoinedInLog -LogDelta $garbledTruncated -ProcessId 87404 -FederationName 'MAK-ONE-2025')
Check '8m2 pid 8740 (a whole-number PREFIX of 87404) does NOT match the garbled 87404 line' (
    -not (Test-HolderJoinedInLog -LogDelta $garbledJoin87404 -ProcessId 8740 -FederationName 'MAK-ONE-2025'))
Check '8m2 a different federation name (STP825AB) does NOT match the garbled 87404/MAK-ONE-2025 line' (
    -not (Test-HolderJoinedInLog -LogDelta $garbledJoin87404 -ProcessId 87404 -FederationName 'STP825AB'))
Check '8m2 a garbled line for a DIFFERENT pid (rtiexec log line 4051, pid 74612) does not match a query for 87404' (
    -not (Test-HolderJoinedInLog -LogDelta $garbledJoin74612 -ProcessId 87404 -FederationName 'MAK-ONE-2025'))
Check '8m2 a "has resigned" line does not match (the join phrase is required, not just pid+federation)' (
    -not (Test-HolderJoinedInLog -LogDelta $resignedLine -ProcessId 87404 -FederationName 'MAK-ONE-2025'))
Check '8m2 a null log delta returns false without throwing under Set-StrictMode -Version Latest' (
    -not (Test-HolderJoinedInLog -LogDelta $null -ProcessId 87404 -FederationName 'MAK-ONE-2025'))
Check '8m2 an empty-string log delta returns false without throwing' (
    -not (Test-HolderJoinedInLog -LogDelta '' -ProcessId 87404 -FederationName 'MAK-ONE-2025'))

# Get-HolderPidLogLines: the operator-visible half of the fallback (a still-alive holder whose
# join the strict matcher could not confirm). The function body returns @() (never $null) when
# nothing matched, but PowerShell unrolls a zero-element array on the return pipeline, so a
# BARE '$r = Get-HolderPidLogLines ...' assignment still lands $null when the count is 0 - the
# same pipeline behaviour Get-OtherRunnerProcessInfo's own note (above) is about. The @(...)
# call-site convention used throughout this file exists for exactly this: wrap every call and
# .Count never throws under Set-StrictMode -Version Latest, whatever the function found.
Check '8m2 Get-HolderPidLogLines finds the garbled line for its own pid' (
    (@(Get-HolderPidLogLines -LogDelta $garbledJoin87404 -ProcessId 87404)).Count -eq 1)
Check '8m2 Get-HolderPidLogLines (wrapped in @()) is an empty array for a pid absent from the delta' (
    (@(Get-HolderPidLogLines -LogDelta $garbledJoin87404 -ProcessId 99999)).Count -eq 0)
Check '8m2 Get-HolderPidLogLines (wrapped in @()) against a null log delta is an empty array, no throw' (
    (@(Get-HolderPidLogLines -LogDelta $null -ProcessId 87404)).Count -eq 0)

# ===========================================================================================
# 8m3. N9 (D8 harvest, 2026-09-21): THE JOIN EVIDENCE MUST BE QUOTED, NOT CONSTRUCTED.
#
# Stage 2h announced the join as `rtiexec log: remoteControl <pid> has joined federation
# "<name>"` - a sentence the runner BUILT from the two values it had searched for. On the D8
# host the real line is the doubled, interleaved $garbledJoin87404 above, so the runner's own
# permanent record made a garbled log read clean; overturning that reading cost the harvest a
# whole section. The runner now quotes the line that matched.
#
# Three properties, and all three can fail:
#   1. the quoted line IS the matched line, byte for byte apart from the control-character and
#      truncation rules - so a garbled log LOOKS garbled in the record;
#   2. the predicate and the evidence share one matcher, so "joined" can never be reported
#      beside a line that did not match;
#   3. the runner no longer builds the sentence.
# ============================================================================
# 8x. Get-PlacementRows against the interface's REAL placement lines (BL-3).
#
# WHY THIS EXISTS, and it is a gap that existed for the whole life of the
# function: until 2026-09-21 NO test fed Get-PlacementRows a single line. It is
# the input half of the Stage-7d warm/cold cache-state indicator (the delta
# between the first placement line and the first nav-area row: ~10 s warm,
# ~240 s cold), and that indicator is the discriminator for STP-856 - units
# created under a cold-streamed terrain.
#
# The buried-units lane added a WALL stamp to those lines. A first version wrote
# 'PLACEMENT at WALL <stamp>:', which deletes the substring 'PLACEMENT:' that
# RunnerLib.ps1:1227 rejects on - so the function returned EMPTY, the indicator
# degraded to 'UNKNOWN for this run', and the offline suite said nothing because
# nothing exercised it. The lines below are REAL, copied from the shipped format
# strings (VrfC2SimService.cs FinalizePlacement), not paraphrases.
Write-Host '=== 8x. Get-PlacementRows parses the interface''s REAL placement lines, stamp and all (BL-3) ==='
$placeReal = @(
  '      PLACEMENT: PLATFORM 28ID__FRIENDLY_INFANTRY_DIVISION domain=1 created at authored lat/lon; create alt 0 m from the FALLBACK (terrain height under the create point: UNKNOWN); post-create SetAltitude: 0 m ABOVE GROUND LEVEL - create alt = 0 (FALLBACK - no terrain height for this point; the create clamp is then the only thing placing it, ifCreateVrfObject.h:210-212); C2SIM gave no altitude -> on the ground: setAltitude(0, aboveGroundLevel=TRUE) (WALL 2026-09-21T11:51:46.311Z).',
  '      PLACEMENT: UNIT 1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE domain=1 created at authored lat/lon; create alt 131.11627508402665 m from the TERRAIN QUERY (terrain height under the create point: 130.1 m); post-create SetAltitude: 0 m ABOVE GROUND LEVEL - create alt = terrain 130.1 m + CreateClearanceMeters 1 m (created AT the surface - UG52 14.3.3 + MAK''s own sample); C2SIM gave no altitude -> on the ground: setAltitude(0, aboveGroundLevel=TRUE) (WALL 2026-09-21T11:52:18.902Z).',
  '      PLACEMENT summary: 1 of 1 create altitude(s) came from the TERRAIN QUERY, 0 from the FALLBACK (WALL 2026-09-21T11:52:18.903Z).',
  '      PLACEMENT RE-CLAMP: 32 of 36 object(s) - LAND PLATFORMS ONLY - were created at the FALLBACK altitude because the init''s terrain-profile query was not answered.'
) -join "`r`n"
$placeRows = @(Get-PlacementRows -AppLogText $placeReal)
Check '8x Get-PlacementRows finds BOTH object rows in the stamped format (this is the BL-3 regression)' (
    $placeRows.Count -eq 2)
Check '8x the PLATFORM row keeps its kind and its marking' (
    $placeRows.Count -eq 2 -and $placeRows[0].kind -eq 'PLATFORM' -and
    $placeRows[0].name -eq '28ID__FRIENDLY_INFANTRY_DIVISION')
Check '8x the UNIT row keeps its kind and its marking' (
    $placeRows.Count -eq 2 -and $placeRows[1].kind -eq 'UNIT' -and
    $placeRows[1].name -eq '1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FORCE')
Check '8x neither the SUMMARY line nor a RE-CLAMP line is mistaken for an object row' (
    (@($placeRows | Where-Object { $_.name -like '*summary*' -or $_.kind -notin @('UNIT','PLATFORM') })).Count -eq 0)
Check '8x the WALL stamp is PRESENT in the captured line (the queued item is delivered, not dropped)' (
    $placeRows.Count -eq 2 -and $placeRows[0].line -match 'WALL 2026-09-21T11:51:46\.311Z')
# The fail-first half: the format the lane first shipped, which this test would have caught.
$placeBroken = '      PLACEMENT at WALL 2026-09-21T11:51:46.311Z: PLATFORM 28ID__FRIENDLY_INFANTRY_DIVISION domain=1 created at authored lat/lon; create alt 0 m from the FALLBACK.'
Check '8x REGRESSION GUARD: a stamp placed BEFORE the colon breaks the parser - proving the test can fail' (
    (@(Get-PlacementRows -AppLogText $placeBroken)).Count -eq 0)
Check '8x empty and null input are handled without throwing' (
    (@(Get-PlacementRows -AppLogText '')).Count -eq 0 -and (@(Get-PlacementRows -AppLogText $null)).Count -eq 0)

Write-Host '=== 8m3. N9: the Stage 2h join evidence QUOTES the matched rtiexec line ==='
Check '8m3 Get-HolderJoinLine returns the CLEAN line verbatim' (
    (Get-HolderJoinLine -LogDelta $cleanJoinLine -ProcessId 87404 -FederationName 'MAK-ONE-2025') -eq $cleanJoinLine)
Check '8m3 ...and the GARBLED line verbatim - doubling, interleaving and all (this is the D8 line)' (
    (Get-HolderJoinLine -LogDelta $garbledJoin87404 -ProcessId 87404 -FederationName 'MAK-ONE-2025') -eq $garbledJoin87404)
Check '8m3 the quoted evidence is NOT the clean sentence the runner used to construct' (
    (Get-HolderJoinLine -LogDelta $garbledJoin87404 -ProcessId 87404 -FederationName 'MAK-ONE-2025') -ne
    ('rtiexec log: remoteControl 87404 has joined federation "MAK-ONE-2025"'))
Check '8m3 a non-match returns the EMPTY string, never a manufactured line' (
    (Get-HolderJoinLine -LogDelta $resignedLine -ProcessId 87404 -FederationName 'MAK-ONE-2025') -eq '' -and
    (Get-HolderJoinLine -LogDelta $garbledJoin74612 -ProcessId 87404 -FederationName 'MAK-ONE-2025') -eq '' -and
    (Get-HolderJoinLine -LogDelta $null -ProcessId 87404 -FederationName 'MAK-ONE-2025') -eq '')
# THE TWO MUST AGREE BY CONSTRUCTION. A predicate with its own copy of the regex is how a run
# comes to say "joined" and quote something else; Test-HolderJoinedInLog is now defined in
# terms of Get-HolderJoinLine, and this is the assertion that keeps it that way.
$n9Cases = @(
    @{ D = $cleanJoinLine;    P = 87404; F = 'MAK-ONE-2025' },
    @{ D = $garbledJoin87404; P = 87404; F = 'MAK-ONE-2025' },
    @{ D = $garbledTruncated; P = 87404; F = 'MAK-ONE-2025' },
    @{ D = $garbledJoin74612; P = 87404; F = 'MAK-ONE-2025' },
    @{ D = $resignedLine;     P = 87404; F = 'MAK-ONE-2025' },
    @{ D = $garbledJoin87404; P = 8740;  F = 'MAK-ONE-2025' },
    @{ D = $garbledJoin87404; P = 87404; F = 'STP825AB' },
    @{ D = '';                P = 87404; F = 'MAK-ONE-2025' })
$n9Agree = $true
foreach ($c in $n9Cases) {
    $said  = [bool](Test-HolderJoinedInLog -LogDelta $c.D -ProcessId $c.P -FederationName $c.F)
    $shown = [bool](Get-HolderJoinLine    -LogDelta $c.D -ProcessId $c.P -FederationName $c.F)
    if ($said -ne $shown) { $n9Agree = $false }
}
Check '8m3 the predicate and the quoted evidence agree on all 8 cases - one matcher, not two' $n9Agree
# ConvertTo-QuotableLogLine: safe to print, still recognisably the same line.
$n9Ctrl = "Federate remoteControl 87404 has joined" + [string][char]7 + [string][char]27 + " federation ""MAK-ONE-2025""."
$n9Quoted = ConvertTo-QuotableLogLine -Line $n9Ctrl
Check '8m3 control characters become spaces (a raw BEL/ESC from a garbled sink must not reach the console or the manifest)' (
    $n9Quoted -notmatch "[\x00-\x1f\x7f]" -and $n9Quoted -match 'has joined' -and $n9Quoted -match 'MAK-ONE-2025')
Check '8m3 a normal line is returned UNCHANGED - the sanitiser must not launder evidence' (
    (ConvertTo-QuotableLogLine -Line $cleanJoinLine) -eq $cleanJoinLine -and
    (ConvertTo-QuotableLogLine -Line $garbledJoin87404) -eq $garbledJoin87404)
$n9Long = ('x' * 500)
$n9Trunc = ConvertTo-QuotableLogLine -Line $n9Long -MaxChars 200
Check '8m3 an over-long line is truncated at 200 chars and SAYS SO with the true length' (
    $n9Trunc.StartsWith(('x' * 200)) -and $n9Trunc -match 'TRUNCATED, 500 chars total')
Check '8m3 null/empty in, empty out, no throw under StrictMode' (
    (ConvertTo-QuotableLogLine -Line $null) -eq '' -and (ConvertTo-QuotableLogLine -Line '') -eq '')
# ...and the two scripts really use it. LaunchVrf52 duplicates the pair deliberately
# ("CHANGE ONE, CHANGE BOTH"), so both copies are checked.
Check '8m3 the runner no longer BUILDS the sentence, and quotes the matched line instead' (
    $runnerText -notmatch "rtiexec log: remoteControl \{0\} has joined federation" -and
    $runnerText -match 'Get-HolderJoinLine -LogDelta' -and
    $runnerText -match 'rtiexec log line \(VERBATIM\)' -and
    $runnerText -match 'ConvertTo-QuotableLogLine -Line \$hJoinLine')
Check '8m3 the loose-match fallback and the pid-line dump are quoted through the same sanitiser' (
    $runnerText -match 'ConvertTo-QuotableLogLine -Line \$pl' -and
    $runnerText -match '\$hLooseQuoted\s*=\s*ConvertTo-QuotableLogLine')
$n9Launch = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\LaunchVrf52.ps1') -Raw
$n9LibText = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\RunnerLib.ps1') -Raw
Check '8m3 LaunchVrf52.ps1 carries the SAME pair (CHANGE ONE, CHANGE BOTH) and quotes the line in its own HELD message' (
    $n9Launch -match 'function Get-HolderJoinLine' -and
    $n9Launch -match 'function ConvertTo-QuotableLogLine' -and
    $n9Launch -match 'rtiexec log line \(VERBATIM\)' -and
    $n9Launch -match 'federation HELD.*\{6\}')
Check '8m3 both copies of Test-HolderJoinedInLog are defined in terms of Get-HolderJoinLine, not a second regex' (
    ([regex]::Matches($n9LibText + $n9Launch,
        'Get-HolderJoinLine -LogDelta \$LogDelta -ProcessId \$ProcessId -FederationName \$FederationName')).Count -eq 2)

# 9. Get-VrfUuidByName must parse BOTH app-log route-line forms. The app started
# logging the route's own uuid on 2026-09-02 with the route-uuid fix ("Route '<r>'
# (VRF_UUID:<route>) created; ..."); every run in the record before that logs the
# line without it. Run 20260902T153837Z proved the cost of missing one: the report-
# evidence gate reported "marking -> VRF_UUID unknown (no route line in the app log)"
# for all three taskees against a healthy 3/3 app log and the window ran to its cap.
# =============================================================================
# 8n-8r. THE RUNNER MUST TELL THE TRUTH ABOUT WHAT IT LAUNCHED (2026-09-20).
# Five items from the cold-start review of f26d4ad (F5, F6) and the D1b harvest
# (findings A1, A2, A3). Each one is a place where the runner's own evidence said
# something that was not so: a value silently taken from the environment, a
# PREDICTION printed as an observation, a WARN about files that cannot exist, a
# false STP-825 alarm, and an assumption ("the two configs are identical") that
# nothing ever checked.
# =============================================================================

# 8n. F6: scenario, init and order must resolve IDENTICALLY - argument, then the environment,
# then the built-in default - in BOTH entry points, and the run must SAY where each came from.
# scripts\RunC2SimScenario.ps1 honoured $env:C2SIM_INIT and $env:C2SIM_ORDER but NOT
# $env:C2SIM_SCENARIO (scripts\RunScenario.sh honoured all three), so an operator who exported
# all three for a new AO and called the ps1 DIRECTLY - a documented usage, its own help says so -
# got that AO's init and order on the OLD AO's scenario: the cross-AO mix that authors legs on
# another continent and wedges the back end's path job (STP-823, memory
# lessons-order-coordinates-vs-init).
# THESE ARE BEHAVIOUR CHECKS against the REAL runner in -DryRun. The Stage 0 banner is printed
# BEFORE the binaries are validated, so they are real assertions even in a checkout with nothing
# built - unlike 8k/8l/8m, they never skip.
Write-Host '=== 8n. F6: scenario/init/order provenance - argument > environment > built-in default ==='
$provPwsh    = 'C:\Program Files\PowerShell\7\pwsh.exe'
$provScript  = Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1'
$provSaved   = @{ s = $env:C2SIM_SCENARIO; i = $env:C2SIM_INIT; o = $env:C2SIM_ORDER }
$provEnvOut = ''; $provArgOut = ''; $provDefOut = ''
try {
    $env:C2SIM_SCENARIO = 'RunnerTurnaround_EnvScenario'
    $env:C2SIM_INIT     = (Join-Path $RepoRoot 'data\COA-STP1_Initialization.xml')
    $env:C2SIM_ORDER    = (Join-Path $RepoRoot 'data\COA-STP1_Order.xml')
    $provEnvOut = (& $provPwsh -NoProfile -File $provScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck 2>&1 | Out-String)
    # THE ARM THAT STOPS THIS PASSING BY THE ENVIRONMENT BEING IGNORED, and the one that stops
    # it passing by the ARGUMENT being ignored: the same three variables are still exported here,
    # and -Scenario/-Init/-Order must beat every one of them.
    $provArgOut = (& $provPwsh -NoProfile -File $provScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck `
                        -Scenario 'RunnerTurnaround_ArgScenario' `
                        -Init  (Join-Path $RepoRoot 'data\R9_Mojave_Lean_Initialization.xml') `
                        -Order (Join-Path $RepoRoot 'data\R9_Mojave_UnitMove_Order.xml') 2>&1 | Out-String)
    $env:C2SIM_SCENARIO = $null
    $env:C2SIM_INIT     = $null
    $env:C2SIM_ORDER    = $null
    $provDefOut = (& $provPwsh -NoProfile -File $provScript -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck 2>&1 | Out-String)
} finally {
    $env:C2SIM_SCENARIO = $provSaved.s
    $env:C2SIM_INIT     = $provSaved.i
    $env:C2SIM_ORDER    = $provSaved.o
}
# Whitespace is collapsed before matching for the reason 8m already gives: these banner lines
# wrap across the console width when a dry run's output is captured through Out-String.
$provEnvFlat = ($provEnvOut -replace '\s+', ' ')
$provArgFlat = ($provArgOut -replace '\s+', ' ')
$provDefFlat = ($provDefOut -replace '\s+', ' ')
Check '8n THE F6 DEFECT: $env:C2SIM_SCENARIO is honoured by the ps1 (it was read only by RunScenario.sh)' (
    $provEnvFlat -match 'scenario : RunnerTurnaround_EnvScenario') "banner did not carry the env scenario"
Check '8n the banner names the SOURCE of all three, and says the environment for all three' (
    $provEnvFlat -match 'scenario : RunnerTurnaround_EnvScenario <- env var C2SIM_SCENARIO' -and
    $provEnvFlat -match 'init : \S*COA-STP1_Initialization\.xml <- env var C2SIM_INIT' -and
    $provEnvFlat -match 'order : \S*COA-STP1_Order\.xml <- env var C2SIM_ORDER')
Check '8n an env-sourced value is IMPOSSIBLE TO MISS: a warning that names the variables and the hazard' (
    $provEnvFlat -match '3 of scenario/init/order came from the ENVIRONMENT' -and
    $provEnvFlat -match 'An exported C2SIM_SCENARIO / C2SIM_INIT / C2SIM_ORDER outlives the shell that set it' -and
    $provEnvFlat -match 'STP-823')
Check '8n an ARGUMENT beats the environment for all three (so the env arm cannot be a false green)' (
    $provArgFlat -match 'scenario : RunnerTurnaround_ArgScenario <- argument -Scenario' -and
    $provArgFlat -match 'init : \S*R9_Mojave_Lean_Initialization\.xml <- argument -Init' -and
    $provArgFlat -match 'order : \S*R9_Mojave_UnitMove_Order\.xml <- argument -Order')
Check '8n with all three given as arguments the environment warning is NOT printed' (
    $provArgFlat -notmatch 'came from the ENVIRONMENT')
Check '8n with NOTHING exported all three report the built-in default, and 5.2 still gets its own scenario default' (
    $provDefFlat -match 'scenario : Sample\\FirstExperience\\firstexperience <- built-in default' -and
    $provDefFlat -match 'init : \S*R9_Mojave_Lean_Initialization\.xml <- built-in default' -and
    $provDefFlat -match 'order : \S*R9_Mojave_UnitMove_Order\.xml <- built-in default' -and
    $provDefFlat -notmatch 'came from the ENVIRONMENT')
# The WRAPPER half of "identically in both entry points". scripts\RunScenario.sh already read all
# three; what it did NOT do was say so, and it passed an env-sourced value to the runner as an
# ARGUMENT, which would make the runner report 'argument' for a value nobody typed. It now prints
# the source and passes the value through the ENVIRONMENT instead, so one resolver answers once.
$provBash = 'C:\Program Files\Git\bin\bash.exe'
if (-not (Test-Path -LiteralPath $provBash)) {
    Check '8n wrapper leg SKIPPED (no bash.exe at the pinned Git path)' $true
} else {
    $provWrapperPosix = ($RepoRoot -replace '\\', '/')
    if ($provWrapperPosix -match '^([A-Za-z]):(.*)$') { $provWrapperPosix = ('/' + $Matches[1].ToLower() + $Matches[2]) }
    $provWrapperPosix = $provWrapperPosix + '/scripts/RunScenario.sh'
    $provShDef = (& $provBash $provWrapperPosix '--help' 2>&1 | Out-String)
    Check '8n the wrapper still documents all three environment variables in its help' (
        $provShDef -match 'C2SIM_SCENARIO' -and $provShDef -match 'C2SIM_INIT' -and $provShDef -match 'C2SIM_ORDER')
    $provShText = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\RunScenario.sh') -Raw
    Check '8n the wrapper records a SOURCE for each of the three' (
        $provShText -match "SCENARIO_SRC='env var C2SIM_SCENARIO'" -and
        $provShText -match "INIT_SRC='env var C2SIM_INIT'" -and
        $provShText -match "ORDER_SRC='env var C2SIM_ORDER'" -and
        $provShText -match "SCENARIO_SRC='argument --scenario'")
    Check '8n the wrapper EXPORTS an env-sourced value instead of passing it as an argument (one resolver, one answer)' (
        $provShText -match 'export C2SIM_SCENARIO="\$SCENARIO"' -and
        $provShText -match 'export C2SIM_INIT="\$INIT"' -and
        $provShText -match 'export C2SIM_ORDER="\$ORDER"')
    # BOTH resolvers must agree on what "set" MEANS. The runner tests IsNullOrWhiteSpace, so a
    # variable holding only spaces is UNSET there; bash -n would call it set, export it, and the
    # runner would then fall back to ITS OWN default - a DIFFERENT AO from the wrapper's. That is
    # the silent AO substitution this item exists to close, so it is checked against the real
    # script: a whitespace-only C2SIM_SCENARIO must land on the WRAPPER's default.
    $provBlankOut = (& $provBash '-c' "export C2SIM_SCENARIO='   '; export C2SIM_INIT='  '; export C2SIM_ORDER=' '; '$provWrapperPosix' --dry-run" 2>&1 | Out-String)
    $provBlankFlat = ($provBlankOut -replace '\s+', ' ')
    Check '8n a WHITESPACE-ONLY C2SIM_SCENARIO/INIT/ORDER counts as unset in the wrapper too (same rule as the runner)' (
        $provBlankFlat -match 'scenario : R9_Mojave_Empty_52_NavAO <- built-in default' -and
        $provBlankFlat -match 'init : data/COA-STP1_Initialization\.xml <- built-in default' -and
        $provBlankFlat -match 'order : data/COA-STP1_Order\.xml <- built-in default') (
        "the wrapper banner did not fall back to its OWN defaults")
}

# 8o. F5: the runner PREDICTS the route-shift state from its OWN environment, while a
# hand-started interface (scripts\StartInterface52.ps1 -RouteShift on|off) resolves it in ITS
# own process - so the manifest could state, as fact, a value the app never used. The shift
# CHANGES WHERE UNITS DRIVE, so that is not a cosmetic field. The prediction stays, labelled as
# one, and the app's OWN announcement is recorded beside it with an agreement verdict.
# Get-RouteShiftAnnouncement is the pure parser; the fixture lines below are the app's real
# message templates (src\VrfC2SimApp\VrfC2SimService.cs:534, :3970-3977, :4210) rendered.
Write-Host '=== 8o. F5: the route shift - prediction vs the app''s own announcement ==='
$rsBanner = @'
info: VrfC2Sim[0]
      LATERAL ROUTE SHIFT ON (Vrf:PreflightRouteShift, the shipped default since the user ruling of 2026-09-20; STP-804/806): before every GROUND move with more than one vertex, each leg is scored against the streamed terrain and a FLAGGED leg is detoured laterally - up to +/-600 m - onto ground the same sampler scores as clear. Tile cache: C:\x\preflight-cache (341 files).
info: VrfC2Sim[0]
      Connected to C2SIM.
'@
$rsPreflightOn = @'
info: VrfC2Sim[0]
      ROUTE PRE-FLIGHT enabled: threshold 0.92 on a 40 m sustained window, step 25 m, tiles cached in C:\x. Readers: warnings off, LATERAL ROUTE SHIFT ON (the shift CHANGES the line a unit drives; neither reader ever refuses a task).
'@
$rsPreflightOff = @'
info: VrfC2Sim[0]
      ROUTE PRE-FLIGHT enabled: threshold 0.92 on a 40 m sustained window, step 25 m, tiles cached in C:\x. Readers: warnings off, LATERAL ROUTE SHIFT off (the shift CHANGES the line a unit drives; neither reader ever refuses a task).
'@
$rsSilent = @'
info: VrfC2Sim[0]
      Connected to C2SIM.
info: VrfC2Sim[0]
      Init dispatched: 6 units + 0 areas queued for creation.
'@
$rsSkipped = $rsBanner + "info: VrfC2Sim[0]`r`n      LATERAL ROUTE SHIFT SKIPPED FOR THIS RUN: Vrf:PreflightOffline is TRUE and the tile cache is empty.`r`n"
$annBanner = Get-RouteShiftAnnouncement -AppLogText $rsBanner
$annOn     = Get-RouteShiftAnnouncement -AppLogText $rsPreflightOn
$annOff    = Get-RouteShiftAnnouncement -AppLogText $rsPreflightOff
$annSilent = Get-RouteShiftAnnouncement -AppLogText $rsSilent
$annEmpty  = Get-RouteShiftAnnouncement -AppLogText ''
$annSkip   = Get-RouteShiftAnnouncement -AppLogText $rsSkipped
Check '8o the start-up banner announces ON, and the line is carried as evidence' (
    $annBanner['Announced'] -eq $true -and $annBanner['Line'] -match 'LATERAL ROUTE SHIFT ON \(Vrf:PreflightRouteShift')
Check '8o the ROUTE PRE-FLIGHT line announces ON' ($annOn['Announced'] -eq $true) "got [$($annOn['Announced'])]"
Check '8o the ROUTE PRE-FLIGHT line announces off - the ONLY line that can (case-sensitive ON/off)' (
    $annOff['Announced'] -eq $false) "got [$($annOff['Announced'])]"
Check '8o SILENCE IS NOT EVIDENCE OF OFF: a log with neither line yields $null, never $false' (
    $null -eq $annSilent['Announced'] -and $null -eq $annEmpty['Announced']) "silent=[$($annSilent['Announced'])] empty=[$($annEmpty['Announced'])]"
Check '8o a SKIPPED-for-this-run log is still announced ON, and the skip is flagged separately' (
    $annSkip['Announced'] -eq $true -and $annSkip['Skipped'] -eq $true -and $annBanner['Skipped'] -eq $false)
# The VERDICT rule itself, pure (Get-RouteShiftVerdict). This is the half that decides whether a
# run's evidence says CONFIRMED, MISMATCH or NOT OBSERVED, so it is checked rather than argued.
$vAgree    = Get-RouteShiftVerdict -Predicted $true  -PredictedSource 'appsettings.json' -Announced $true  -AnnouncedSource 'banner' -Stage 'Stage 6c'
$vMismatch = Get-RouteShiftVerdict -Predicted $true  -PredictedSource 'appsettings.json' -Announced $false -AnnouncedSource 'preflight line' -Stage 'teardown'
$vMism2    = Get-RouteShiftVerdict -Predicted $false -PredictedSource 'env Vrf__PreflightRouteShift=false' -Announced $true -AnnouncedSource 'banner' -Stage 'Stage 6c'
$vSilent   = Get-RouteShiftVerdict -Predicted $true  -PredictedSource 'appsettings.json' -Announced $null  -AnnouncedSource '' -Stage 'Stage 6c'
$vUnparse  = Get-RouteShiftVerdict -Predicted 'UNKNOWN - unparseable Vrf__PreflightRouteShift' -PredictedSource 'env' -Announced $true -AnnouncedSource 'banner' -Stage 'Stage 6c'
Check '8o verdict: prediction ON + announcement ON = CONFIRMED, no mismatch' (
    $vAgree['Observed'] -eq $true -and $vAgree['Mismatch'] -eq $false -and $vAgree['Agreement'] -match '^CONFIRMED')
Check '8o verdict: BOTH disagreement directions are a MISMATCH that names both values' (
    $vMismatch['Mismatch'] -eq $true -and $vMismatch['Agreement'] -match 'PREDICTED \[True\]' -and $vMismatch['Agreement'] -match 'ANNOUNCED \[off\]' -and
    $vMism2['Mismatch']    -eq $true -and $vMism2['Agreement']    -match 'PREDICTED \[False\]' -and $vMism2['Agreement'] -match 'ANNOUNCED \[ON\]')
Check '8o verdict: NO announcement is NOT OBSERVED - never a mismatch, never a promotion of the prediction' (
    $vSilent['Observed'] -eq $false -and $vSilent['Mismatch'] -eq $false -and
    $vSilent['Agreement'] -match '^NOT OBSERVED' -and $vSilent['Agreement'] -notmatch '^CONFIRMED' -and
    $vSilent['Agreement'] -match 'silence here is not evidence of OFF')
Check '8o verdict: an UNPARSEABLE prediction can never be reported as agreeing with the app' (
    $vUnparse['Mismatch'] -eq $true -and $vUnparse['Agreement'] -match 'UNKNOWN - unparseable')
Check '8o verdict: the MISMATCH text names the hand-started interface as the usual cause (the F5 case)' (
    $vMismatch['Agreement'] -match 'StartInterface52\.ps1 -RouteShift on\|off')
# The runner side: the field is named for what it is, the prediction is never written into the
# observation, and a disagreement is loud.
Check '8o runner: the manifest field is `predicted`, with `announced` and `agreement` beside it' (
    $runnerText -match 'predicted\s*=' -and $runnerText -match 'predictedSource\s*=' -and
    $runnerText -match 'announced\s*=' -and $runnerText -match 'agreement\s*=' -and
    $runnerText -notmatch "routeShift = \[ordered\]@\{\s*[\r\n]\s*effective")
Check '8o runner: the app''s announcement is folded in at Stage 6c AND again at teardown' (
    @([regex]::Matches($runnerText, 'Update-RouteShiftObservation -AppLogPath \$PathAppLog')).Count -ge 2)
$rsFn = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Update-RouteShiftObservation' }, $true)
Check '8o runner: Update-RouteShiftObservation exists, defers the RULE to the pure verdict, and NEVER assigns the prediction into the observation' (
    @($rsFn).Count -eq 1 -and
    $rsFn[0].Extent.Text -notmatch '\$rs\.announced\s*=\s*\$rs\.predicted' -and
    $rsFn[0].Extent.Text -match '\$rs\.announced\s*=\s*\[bool\]\$ann\[''Announced''\]' -and
    $rsFn[0].Extent.Text -match 'Get-RouteShiftVerdict' -and
    $rsFn[0].Extent.Text -match "if \(-not \`$v\['Observed'\]\) \{ return \}")
Check '8o runner: a MISMATCH is a validity flag, not a silently corrected field' (
    $rsFn[0].Extent.Text -match "Add-Flag 'WARN' \('ROUTE SHIFT PREDICTION/REALITY MISMATCH")
Check '8o the runner SAYS the value is a prediction, in the dry-run banner' (
    $provDefFlat -match 'route shift is PREDICTED (ON|OFF) for this run' -and
    $provDefFlat -match 'that is a PREDICTION from this shell, not an observation')

# ===========================================================================================
# 8u. THE SAME AGREEMENT DISCIPLINE FOR THE OTHER TWO SETTINGS THAT CHANGE A RUN'S MEANING
#     (adca180 build report FINDING 2; D7 prereg P1 and P5 had to be scored by hand)
# ===========================================================================================
Write-Host '=== 8u. DeStackComposedSiblings / ArrivalApproachFraction: predicted, announced, agreed ==='
# The app's own start-up lines, verbatim in the shape it writes them (VrfC2SimService 0c-iv).
$dsOn  = 'info: VrfC2SimApp.VrfC2SimService[0]' + "`r`n" +
         '      COMPOSED-SIBLING DE-STACK ON (Vrf:DeStackComposedSiblings, user ruling 2026-09-21 + N4). ON spreads composed siblings that share a coordinate onto ONE ring about it at EQUAL bearings.'
$dsOff = '      COMPOSED-SIBLING DE-STACK off (Vrf:DeStackComposedSiblings, user ruling 2026-09-21 + N4). ON spreads composed siblings ...'
$afTxt = '      ARRIVAL APPROACH FRACTION 0.50 (Vrf:ArrivalApproachFraction, user ruling 2026-09-21). Above 0 a member''s traversal bar is capped at this share of ITS OWN distance.'
$afZero= '      ARRIVAL APPROACH FRACTION 0.00 (Vrf:ArrivalApproachFraction, user ruling 2026-09-21). Above 0 ...'
Check '8u the de-stack announcement parses ON and off case-SENSITIVELY, and silence is $null' (
    (Get-DeStackSiblingAnnouncement -AppLogText $dsOn)['Announced'] -eq $true -and
    (Get-DeStackSiblingAnnouncement -AppLogText $dsOff)['Announced'] -eq $false -and
    $null -eq (Get-DeStackSiblingAnnouncement -AppLogText 'nothing relevant here')['Announced'] -and
    $null -eq (Get-DeStackSiblingAnnouncement -AppLogText '')['Announced'])
Check '8u the de-stack announcement carries the LINE it read, for the evidence' (
    (Get-DeStackSiblingAnnouncement -AppLogText $dsOn)['Line'] -match 'COMPOSED-SIBLING DE-STACK ON')
Check '8u the approach-fraction announcement parses a NUMBER, including 0' (
    [double](Get-ArrivalApproachAnnouncement -AppLogText $afTxt)['Announced'] -eq 0.5 -and
    [double](Get-ArrivalApproachAnnouncement -AppLogText $afZero)['Announced'] -eq 0.0 -and
    $null -eq (Get-ArrivalApproachAnnouncement -AppLogText 'nothing relevant')['Announced'])
# The VERDICT rule, pure - the half that decides CONFIRMED / MISMATCH / NOT OBSERVED.
$sAgree   = Get-SettingVerdict -Name 'Vrf:DeStackComposedSiblings' -Predicted $true -PredictedSource 'appsettings.json' -Announced $true -AnnouncedSource 'banner' -Stage 'Stage 6c'
$sMis     = Get-SettingVerdict -Name 'Vrf:DeStackComposedSiblings' -Predicted $true -PredictedSource 'appsettings.json' -Announced $false -AnnouncedSource 'banner' -Stage 'Stage 6c'
$sSilent  = Get-SettingVerdict -Name 'Vrf:DeStackComposedSiblings' -Predicted $true -PredictedSource 'appsettings.json' -Announced $null -AnnouncedSource '' -Stage 'Stage 6c'
$sNumOk   = Get-SettingVerdict -Name 'Vrf:ArrivalApproachFraction' -Predicted 0.5 -PredictedSource 'appsettings.json' -Announced 0.5 -AnnouncedSource 'banner' -Stage 'Stage 6c'
$sNumMis  = Get-SettingVerdict -Name 'Vrf:ArrivalApproachFraction' -Predicted 0.5 -PredictedSource 'appsettings.json' -Announced 0.0 -AnnouncedSource 'banner' -Stage 'Stage 6c'
$sUnparse = Get-SettingVerdict -Name 'Vrf:ArrivalApproachFraction' -Predicted 'UNKNOWN - unparseable Vrf__ArrivalApproachFraction' -PredictedSource 'env' -Announced 0.5 -AnnouncedSource 'banner' -Stage 'Stage 6c'
Check '8u verdict: prediction and announcement agreeing = CONFIRMED, no mismatch' (
    $sAgree['Observed'] -eq $true -and $sAgree['Mismatch'] -eq $false -and $sAgree['Agreement'] -match '^CONFIRMED')
Check '8u verdict: a disagreement is a MISMATCH that names BOTH values and the setting' (
    $sMis['Mismatch'] -eq $true -and $sMis['Agreement'] -match 'PREDICTED Vrf:DeStackComposedSiblings = \[True\]' -and
    $sMis['Agreement'] -match 'ANNOUNCED \[False\]')
Check '8u verdict: NO announcement is NOT OBSERVED - never a mismatch, never a promotion' (
    $sSilent['Observed'] -eq $false -and $sSilent['Mismatch'] -eq $false -and $sSilent['Agreement'] -match '^NOT OBSERVED')
Check '8u verdict: NUMBERS compare with a tolerance (0.50 and 0.5 agree) and a real change does not' (
    $sNumOk['Mismatch'] -eq $false -and $sNumMis['Mismatch'] -eq $true -and
    (Get-SettingVerdict -Name 'x' -Predicted 0.5 -PredictedSource 's' -Announced 0.50 -AnnouncedSource 'b' -Stage 't')['Mismatch'] -eq $false)
Check '8u verdict: an UNPARSEABLE prediction can never be reported as agreeing with the app' (
    $sUnparse['Mismatch'] -eq $true -and $sUnparse['Agreement'] -match 'UNKNOWN - unparseable')
# The runner side.
Check '8u runner: both settings are RESOLVED in the app''s own precedence order (env > json > C# initialiser)' (
    $runnerText -match 'function Resolve-AppSetting' -and
    $runnerText -match "Resolve-AppSetting -Key 'DeStackComposedSiblings' -Type 'bool'" -and
    $runnerText -match "Resolve-AppSetting -Key 'ArrivalApproachFraction' -Type 'double'")
Check '8u runner: the manifest carries predicted/announced/agreement for each, under inputs.appSettings' (
    $runnerText -match '\$Manifest\.inputs\.appSettings = \[ordered\]@\{' -and
    $runnerText -match 'deStackComposedSiblings\s*=' -and $runnerText -match 'arrivalApproachFraction\s*=')
Check '8u runner: the app''s announcements are folded in at Stage 6c AND again at teardown' (
    @([regex]::Matches($runnerText, 'Update-AppSettingObservations -AppLogPath \$PathAppLog')).Count -ge 2)
$asFn = $runnerAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Update-AppSettingObservations' }, $true)
Check '8u runner: Update-AppSettingObservations defers the RULE to the pure verdict and never writes the prediction into the observation' (
    @($asFn).Count -eq 1 -and
    $asFn[0].Extent.Text -match 'Get-SettingVerdict' -and
    $asFn[0].Extent.Text -notmatch '\$slot\.announced\s*=\s*\$slot\.predicted' -and
    $asFn[0].Extent.Text -match "\`$slot\.announced\s*=\s*\`$ann\['Announced'\]" -and
    $asFn[0].Extent.Text -match "if \(-not \`$v\['Observed'\]\) \{ continue \}")
Check '8u runner: a MISMATCH on either is a validity flag, not a silently corrected field' (
    $asFn[0].Extent.Text -match "Add-Flag 'WARN' \('\{0\} PREDICTION/REALITY MISMATCH")
Check '8u the runner SAYS both values are predictions, in the dry-run banner' (
    $provDefFlat -match 'Vrf:DeStackComposedSiblings is PREDICTED' -and
    $provDefFlat -match 'Vrf:ArrivalApproachFraction is PREDICTED' -and
    $provDefFlat -match 'both are PREDICTIONS from this shell')

# ===========================================================================================
# 8v. -DurationScale: the order's clock got a switch (ironstorm_cuta_prep_report STEP 4)
# ===========================================================================================
Write-Host '=== 8v. -DurationScale: a switch, validated, exported, echoed and in the manifest ==='
Check '8v the parameter exists, is a [double] and DEFAULTS TO 0 (= this runner sets nothing)' (
    $params.ContainsKey('DurationScale') -and
    "$($params['DurationScale'].StaticType)" -match 'Double' -and
    "$($params['DurationScale'].DefaultValue)" -eq '0')
Check '8v it is VALIDATED before anything is launched, with a finite positive range' (
    $runnerText -match '\$DurationScaleOn = \(\$DurationScale -ne 0\)' -and
    $runnerText -match '\[double\]::IsFinite\(\$DurationScale\)' -and
    $runnerText -match '-DurationScale must be 0')
Check '8v it is EXPORTED as Vrf__DurationScale, with the invariant culture (never a comma decimal)' (
    $runnerText -match "\`$env:Vrf__DurationScale = \[string\]::Format\(\[System\.Globalization\.CultureInfo\]::InvariantCulture" -and
    $runnerText -match "Resolve-AppSetting|Vrf__DurationScale")
Check '8v the shell''s own value is SAVED and RESTORED - a runner must not leave a clock scale behind' (
    $runnerText -match '\$DurationScaleEnvBefore = \[Environment\]::GetEnvironmentVariable\(''Vrf__DurationScale''\)' -and
    $runnerText -match 'if \(\$DurationScaleOn\) \{ \$env:Vrf__DurationScale = \$DurationScaleEnvBefore \}')
Check '8v the MANIFEST records the switch, whether it was exported, and what was in the shell before' (
    $runnerText -match '\$Manifest\.inputs\.durationScale = \[ordered\]@\{' -and
    $runnerText -match 'envValueBefore\s*=' -and $runnerText -match 'exported\s*=')
Check '8v the Stage 0 banner ECHOES it either way, so a compressed run can never be read as a full-length one' (
    $provDefFlat -match 'order clock: -DurationScale not given' -or
    $provDefFlat -match 'order clock SCALED')
Check '8v the note says what it scales AND what it does not (the order''s clock, never movement)' (
    $runnerText -match 'the Duration that ends a task and the StartTime delay that holds one back\) and NOT movement')

# -------------------------------------------------------------------------------------------
# 8v2. SF-D (cold-start review of 1d0fb69, 2026-09-21): THE RESTORE HAD A HOLE, AND IT IS A
# BEHAVIOURAL ONE. Vrf__DurationScale is exported at Stage 0 (~:2466) but used to be restored
# only in the TEARDOWN finally (Stage 9), which no early abort reaches: the Stage 0 validation
# "exit 2" is ~180 lines below the export, and Stage 0b/1/2 add nine more. A run that failed
# any of them left the ORDER CLOCK SCALED IN THE INVOKING PROCESS - harmless through
# scripts\RunScenario.sh (a throwaway child pwsh) and NOT harmless in the operator's own
# shell, where the next run is silently compressed while its manifest says this runner set
# nothing. The fix is one try/finally spanning everything after the export.
#
# WHY THIS RUNS THE RUNNER (like 8d and 8h): the leak is in the CALLER's environment, and no
# text assertion can tell a restore that runs from one that is skipped. `exit` inside a script
# invoked with `&` returns to the caller, so a throwaway wrapper sees exactly what an operator
# would see. -RunSecs 1 is out of the documented 30..86400 band, so the runner aborts at the
# Stage 0 validation "exit 2" with NOTHING launched and NO run directory - the cheapest of the
# ten bypassing paths, and the first one an operator meets. Measured against the pre-fix
# script: exit 2 and Vrf__DurationScale left at 7.
Write-Host '=== 8v2. SF-D: -DurationScale is restored on an EARLY ABORT, not only after teardown ==='
$dsLeakPwsh   = 'C:\Program Files\PowerShell\7\pwsh.exe'
$dsLeakRunner = Join-Path $RepoRoot 'scripts\RunC2SimScenario.ps1'
$dsLeakProbe  = Join-Path ([System.IO.Path]::GetTempPath()) ('_DurationScaleLeakProbe.{0}.ps1' -f [Guid]::NewGuid().ToString('N'))
$dsLeakSrc = @'
param([string]$Runner, [string]$Before)
if ($Before -eq '(unset)') { $env:Vrf__DurationScale = $null } else { $env:Vrf__DurationScale = $Before }
$null = & $Runner -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck -RunSecs 1 -DurationScale 7 2>&1
Write-Output ('EXITCODE=' + $LASTEXITCODE)
Write-Output ('AFTER=[' + $env:Vrf__DurationScale + ']')
'@
try {
    [System.IO.File]::WriteAllText($dsLeakProbe, $dsLeakSrc, (New-Object System.Text.UTF8Encoding($false)))
    # Arm (a): the shell had NOTHING. After a failed run it must still have nothing.
    $dsUnsetOut = (& $dsLeakPwsh -NoProfile -File $dsLeakProbe -Runner $dsLeakRunner -Before '(unset)' 2>&1 | Out-String)
    Check '8v2 (a) the probe really reached the Stage 0 validation abort (exit 2)' (
        $dsUnsetOut -match 'EXITCODE=2') $dsUnsetOut
    Check '8v2 (a) an aborted run leaves NO Vrf__DurationScale behind when the shell had none' (
        $dsUnsetOut -match 'AFTER=\[\]') $dsUnsetOut
    # Arm (b): the shell had its OWN value. A restore must put THAT back, not merely clear it.
    $dsSetOut = (& $dsLeakPwsh -NoProfile -File $dsLeakProbe -Runner $dsLeakRunner -Before '3' 2>&1 | Out-String)
    Check '8v2 (b) the probe really reached the Stage 0 validation abort (exit 2)' (
        $dsSetOut -match 'EXITCODE=2') $dsSetOut
    Check '8v2 (b) an aborted run puts the shell''s OWN value back, not the runner''s 7' (
        $dsSetOut -match 'AFTER=\[3\]') $dsSetOut
} finally { Remove-Item -LiteralPath $dsLeakProbe -Force -ErrorAction SilentlyContinue }
# And the structural half: ONE restore, in the OUTERMOST finally, not in the teardown one.
Check '8v2 the restore appears exactly ONCE in the runner (one mechanism, not ten patches)' (
    ([regex]::Matches($runnerText, 'if \(\$DurationScaleOn\) \{ \$env:Vrf__DurationScale = \$DurationScaleEnvBefore \}')).Count -eq 1)
Check '8v2 the outermost try is opened right after the export and closed at the end of the file' (
    $runnerText -match '# SF-D \(cold-start review of 1d0fb69' -and
    $runnerText -match 'closes the OUTERMOST try, opened immediately after the -DurationScale export')

# ===========================================================================================
# 8v3. SF-R4 (cold-start review of 2df59ba): Vrf__ClientId HAD THE SAME HOLE, AND A WORSE ONE.
#
# It was exported at ~:2459 and restored NOWHERE in the file - the teardown finally puts back
# ApplicationNumber, the two C2SIM urls, the profile env and AppEnv52, and never ClientId; the
# outermost finally covered only DurationScale. So a -ClientId run left its value in the
# operator's shell even when it SUCCEEDED, and the next run inherited it while this runner's own
# banner printed "clientId : <x> (appsettings.json)". ClientId is the C2SIM SystemName the
# interface filters on, so the inherited value decides WHICH UNITS GET CREATED: the first
# review's reason for deferring it ("cannot change what a run MEANS") is factually wrong.
#
# Fixed with the SAME mechanism as -DurationScale, one line lower in the SAME finally, and the
# banner, the validation gate and the manifest now name the EFFECTIVE source of the three.
Write-Host '=== 8v3. SF-R4: Vrf__ClientId is restored, and the banner names its TRUE source ==='
$ciProbe = Join-Path ([System.IO.Path]::GetTempPath()) ('_ClientIdLeakProbe.{0}.ps1' -f [Guid]::NewGuid().ToString('N'))
# The probe runs a REAL dry run (nothing launched, no run directory - see 8h) and reports the
# shell afterwards plus the two lines that carry the claim: the Stage 0 banner and, when it
# fires, the SystemName gate. 'STP' is the SystemName the shipped inits declare, so the
# banner-reading arms must use it - a mismatching id aborts at validation BEFORE the banner,
# which is itself asserted as arm (d).
$ciSrc = @'
param([string]$Runner, [string]$Before, [string]$Pass)
if ($Before -eq '(unset)') { $env:Vrf__ClientId = $null } else { $env:Vrf__ClientId = $Before }
if ($Pass -eq '(none)') {
    $out = & $Runner -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck 2>&1
} else {
    $out = & $Runner -VrfProfile 5.2 -NoGui -DryRun -SkipServerCheck -ClientId $Pass 2>&1
}
Write-Output ('EXITCODE=' + $LASTEXITCODE)
Write-Output ('AFTER=[' + $env:Vrf__ClientId + ']')
foreach ($ln in (($out | Out-String) -split "`r?`n")) {
    if ($ln -match 'clientId\s+:' -or $ln -match 'clientId MISMATCH') { Write-Output ('LINE>' + $ln.Trim()) }
}
'@
try {
    [System.IO.File]::WriteAllText($ciProbe, $ciSrc, (New-Object System.Text.UTF8Encoding($false)))
    # (a) -ClientId on a shell that had nothing: the run must give the shell back nothing.
    $ciA = (& $dsLeakPwsh -NoProfile -File $ciProbe -Runner $dsLeakRunner -Before '(unset)' -Pass 'STP' 2>&1 | Out-String)
    Check '8v3 (a) -ClientId leaves NOTHING behind when the shell had nothing' (
        $ciA -match 'AFTER=\[\]') $ciA
    Check '8v3 (a) and the banner credits the SWITCH, not appsettings.json' (
        $ciA -match 'clientId\s+:\s+STP \(-ClientId -> Vrf__ClientId') $ciA
    # (b) -ClientId on a shell that had its OWN value: that value comes back, not the runner's.
    $ciB = (& $dsLeakPwsh -NoProfile -File $ciProbe -Runner $dsLeakRunner -Before 'MINE' -Pass 'STP' 2>&1 | Out-String)
    Check '8v3 (b) -ClientId puts the shell''s OWN value back, not the runner''s' (
        $ciB -match 'AFTER=\[MINE\]') $ciB
    # (c) THE MISLABEL ITSELF: no -ClientId, but the shell carries one. The app reads it and it
    #     beats appsettings.json, so the banner must say so - it used to say "(appsettings.json)".
    $ciC = (& $dsLeakPwsh -NoProfile -File $ciProbe -Runner $dsLeakRunner -Before 'STP' -Pass '(none)' 2>&1 | Out-String)
    Check '8v3 (c) an INHERITED Vrf__ClientId is named as INHERITED, never credited to appsettings.json' (
        $ciC -match 'clientId\s+:\s+STP \(INHERITED Vrf__ClientId in this shell' -and
        $ciC -notmatch 'clientId\s+:\s+STP \(appsettings\.json\)') $ciC
    Check '8v3 (c) the runner did not set it, so it is left exactly as the shell had it' (
        $ciC -match 'AFTER=\[STP\]') $ciC
    # (d) AND THE GATE NOW BITES ON IT. A leaked id that disagrees with the init's SystemName is
    #     a run that creates 0 UNITS; the check used to compare appsettings.json, which is the
    #     value the app would NOT have used, so it could not see this at all.
    $ciD = (& $dsLeakPwsh -NoProfile -File $ciProbe -Runner $dsLeakRunner -Before 'WRONGID' -Pass '(none)' 2>&1 | Out-String)
    Check '8v3 (d) an inherited id that disagrees with the init is REFUSED at validation (exit 2)' (
        $ciD -match 'EXITCODE=2') $ciD
    Check '8v3 (d) and the refusal names the EFFECTIVE value and the source it came from' (
        $ciD -match "clientId MISMATCH: the EFFECTIVE Vrf:ClientId is 'WRONGID' \(source: INHERITED Vrf__ClientId") $ciD
    Check '8v3 (d) a refused run still hands the shell back its own value untouched' (
        $ciD -match 'AFTER=\[WRONGID\]') $ciD
} finally { Remove-Item -LiteralPath $ciProbe -Force -ErrorAction SilentlyContinue }
# The structural half: ONE mechanism, and it is the one SF-D built.
Check '8v3 the restore is one line in the SAME outermost finally as the DurationScale one' (
    ([regex]::Matches($runnerText, 'if \(\$ClientId\) \{ \$env:Vrf__ClientId = \$ClientIdEnvBefore \}')).Count -eq 1 -and
    $runnerText -match '\$ClientIdEnvBefore\s+= \[Environment\]::GetEnvironmentVariable\(''Vrf__ClientId''\)')
Check '8v3 the SystemName gate checks the EFFECTIVE value and names which source produced it' (
    $runnerText -match 'the EFFECTIVE Vrf:ClientId is ''\{0\}'' \(source: \{1\}\)' -and
    $runnerText -notmatch "clientId MISMATCH: appsettings Vrf:ClientId='\{0\}'")
Check '8v3 the manifest records the effective value, its SOURCE and what the shell had before' (
    $runnerText -match '\$Manifest\.inputs\.clientIdSource = \$ClientIdSource' -and
    $runnerText -match '\$Manifest\.inputs\.clientIdDetail = \[ordered\]@\{' -and
    $runnerText -match 'appSettings     = \$\(if \(\$appSettingsClientId\)')
Check '8v3 the dead earlier manifest write - which said "(appsettings)" for an env-sourced run - is gone' (
    $runnerText -notmatch "\`$Manifest\.inputs\.clientId      = \`$\(if \(\`$ClientId\) \{ \`$ClientId \} else \{ \('\(appsettings\) \{0\}'")
# AND THE INVENTORY THE REVIEW ASKED FOR: every Vrf__ / C2SIM__ the runner exports, and where
# each is put back. If a new export appears without a restore, this count moves.
$ciExports = @([regex]::Matches($runnerText, '\$env:(Vrf__|C2SIM__)[A-Za-z0-9_]+\s*=') |
                ForEach-Object { $_.Value -replace '^\$env:' -replace '\s*=$' } | Sort-Object -Unique)
Check '8v3 the Vrf__/C2SIM__ export inventory is the known four + the two app-launch urls' (
    (@($ciExports) -join ',') -eq 'C2SIM__RestUrl,C2SIM__StompUrl,Vrf__ApplicationNumber,Vrf__ClientId,Vrf__DurationScale') (
    'found: ' + (@($ciExports) -join ','))
Check '8v3 every one of them has a restore: ApplicationNumber/RestUrl/StompUrl in the teardown finally (and immediately after the app launch), ClientId and DurationScale in the outermost one' (
    $runnerText -match '\$env:Vrf__ApplicationNumber= \$SavedVrfAppNumber' -and
    $runnerText -match '\$env:C2SIM__RestUrl        = \$SavedC2SimRestUrl' -and
    $runnerText -match '\$env:C2SIM__StompUrl       = \$SavedC2SimStompUrl' -and
    $runnerText -match 'if \(\$ClientId\) \{ \$env:Vrf__ClientId = \$ClientIdEnvBefore \}' -and
    $runnerText -match 'if \(\$DurationScaleOn\) \{ \$env:Vrf__DurationScale = \$DurationScaleEnvBefore \}')

# ===========================================================================================
# 8w. E4 / N3 (D6 and D7 harvests, both RECURRING): what the manifest could not say
# ===========================================================================================
Write-Host '=== 8w. E4: the persistent holders alive at launch; N3: the DEPLOYED build identity ==='
Check '8w E4: Stage 1 records the RtiProbe holders that were ALREADY running, as their own field' (
    $runnerText -match '\$Manifest\.preflight\.existingFederationHolders\s*=' -and
    $runnerText -match '\$HolderProcName\s+=\s+''RtiProbe''' -and
    $runnerText -match 'Get-Process -Name \$HolderProcName')
Check '8w E4: the field is DISTINGUISHED from postRunFederationHolder, which is this run''s own holder' (
    $runnerText -match 'existingFederationHoldersNote\s*=' -and
    $runnerText -match 'postRunFederationHolder lists only THIS run''''s holder and is a different question')
Check '8w E4: a pre-existing holder is NEVER touched and never refused on (RUNBOOK sec 0)' (
    $runnerText -match 'Never touched, never waited for, never refused on \(STP-825, RUNBOOK sec 0\)')
Check '8w N3: the manifest records the DEPLOYED app''s build identity, read off the binary' (
    $runnerText -match '\$Manifest\.host\.deployedAppBuild\s*=' -and
    $runnerText -match 'FileVersionInfo\]::GetVersionInfo\(\$probe\)\.ProductVersion' -and
    $runnerText -match "\\\+git\\\.\(\?<c>\[0-9a-fA-F\]\+\)")
Check '8w N3: a binary built from a DIRTY tree is a WARN flag, not a silently recorded commit' (
    $runnerText -match 'was built from a DIRTY working tree at commit' -and
    $runnerText -match "Add-Flag 'WARN' \('the DEPLOYED VrfC2SimApp")
Check '8w N3: host.gitCommit is annotated as the WORKING TREE''s HEAD, which is a different question' (
    $runnerText -match '\$Manifest\.host\.gitCommitNote\s*=' -and
    $runnerText -match 'a statement about the CHECKOUT, not about the deployed binary')
Check '8w N3: the csproj STAMPS the commit (and a DIRTY marker) into the assembly' (
    (Get-Content -LiteralPath (Join-Path $RepoRoot 'src\VrfC2SimApp\VrfC2SimApp.csproj') -Raw) -match 'StampGitIdentity' -and
    (Get-Content -LiteralPath (Join-Path $RepoRoot 'src\VrfC2SimApp\VrfC2SimApp.csproj') -Raw) -match 'BuildGitCommit' -and
    (Get-Content -LiteralPath (Join-Path $RepoRoot 'src\VrfC2SimApp\VrfC2SimApp.csproj') -Raw) -match '\+DIRTY' -and
    (Get-Content -LiteralPath (Join-Path $RepoRoot 'src\VrfC2SimApp\VrfC2SimApp.csproj') -Raw) -match 'ContinueOnError="true"')

# 8p. D1b harvest A1: the end-of-run vendor-log capture looked for the FLAT 5.0.2 names
# (bin64\vrfSim.log, C:\MAK\logs\vrfGui.log) that VR-Forces 5.2 never writes, so every 5.2 run in
# the record carries two "not found - nothing captured" WARNs about files that cannot exist,
# while the real per-process logs sat un-captured beside them. Captured BY PID now, for the
# processes THIS run launched, with LaunchVrf52's own filters.
# THE HARD CONSTRAINT these checks exist to pin: those vendor logs carry the full process
# environment in cleartext. The capture may COPY one. It may never open one.
Write-Host '=== 8p. D1b A1: the vendor GUI/sim logs are captured BY PID, and never opened ==='
$vlDir = Join-Path ([System.IO.Path]::GetTempPath()) ('vendorlog-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $vlDir -Force | Out-Null
$vlOut = Join-Path $vlDir 'out'
New-Item -ItemType Directory -Path $vlOut -Force | Out-Null
try {
    # A throwaway C:\MAK\logs shaped like the real one. NOTHING here touches C:\MAK.
    $vlSince = (Get-Date).AddMinutes(-5)
    $vlWanted = Join-Path $vlDir 'vrfGui5.2d-20260920-145246-Legatus-282607-83276.log'
    Set-Content -LiteralPath $vlWanted -Value 'MARKER-WANTED' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $vlDir 'vrfGui5.2d-20260920-145246-Legatus-282607-99999.log') -Value 'MARKER-OTHERPID' -Encoding ascii
    Set-Content -LiteralPath (Join-Path $vlDir 'vrfGui5.2d-20260920-145246-Legatus-282607-83276.callstack.log') -Value 'MARKER-CALLSTACK' -Encoding ascii
    $vlStale = Join-Path $vlDir 'vrfSimHLA1516e5.2d-20250101-000000-Legatus-282607-83276.log'
    Set-Content -LiteralPath $vlStale -Value 'MARKER-STALE' -Encoding ascii
    (Get-Item -LiteralPath $vlStale).LastWriteTime = (Get-Date).AddDays(-30)
    $vlHit = Copy-VendorLogByPid -ProcessId 83276 -LogDir $vlDir -NamePrefix 'vrfGui' -Since $vlSince -Destination (Join-Path $vlOut 'vendor-vrfGui.log')
    Check '8p the log for THIS pid is found and copied (5.2 versioned name, not the flat one)' (
        $vlHit['Source'] -eq $vlWanted -and (Test-Path -LiteralPath (Join-Path $vlOut 'vendor-vrfGui.log')))    "source=$($vlHit['Source'])"
    Check '8p the ORIGINAL is left in place - copied, never moved' (Test-Path -LiteralPath $vlWanted)
    Check '8p the copy is the right file (another pid''s log was NOT taken)' (
        (Get-Content -LiteralPath (Join-Path $vlOut 'vendor-vrfGui.log') -Raw) -match 'MARKER-WANTED')
    $vlCs = Copy-VendorLogByPid -ProcessId 83276 -LogDir $vlDir -NamePrefix 'vrfGui' -Since $vlSince -Destination (Join-Path $vlOut 'x.log')
    Check '8p the .callstack.log for the SAME pid is excluded (separate evidence, separate rules)' (
        $vlCs['Source'] -notmatch 'callstack')
    $vlOld = Copy-VendorLogByPid -ProcessId 83276 -LogDir $vlDir -NamePrefix 'vrfSim' -Since $vlSince -Destination (Join-Path $vlOut 'vendor-vrfSim.log')
    Check '8p a file for a RECYCLED pid, written before this run, is refused by the mtime floor' (
        $vlOld['Source'] -eq '' -and $vlOld['Error'] -eq '' -and -not (Test-Path -LiteralPath (Join-Path $vlOut 'vendor-vrfSim.log')))
    $vlNone = Copy-VendorLogByPid -ProcessId 4242 -LogDir $vlDir -NamePrefix 'vrfGui' -Since $vlSince -Destination (Join-Path $vlOut 'none.log')
    Check '8p an unknown pid yields "nothing found", not a throw and not a wrong file' (
        $vlNone['Source'] -eq '' -and $vlNone['Error'] -eq '')
    $vlBadDir = Copy-VendorLogByPid -ProcessId 83276 -LogDir (Join-Path $vlDir 'no-such-dir') -NamePrefix 'vrfGui' -Since $vlSince -Destination (Join-Path $vlOut 'nd.log')
    Check '8p a missing log directory is survivable, never a throw' ($vlBadDir['Source'] -eq '')
    # 2026-09-21: THE DESTINATION DIRECTORY IS CREATED. These copies used to land FLAT in the run
    # directory, where an ordinary `runs\<run>\*.log` glob reads them - and one did, printing two
    # vendor-log lines from files that hold the full process environment in cleartext. The copy
    # now goes into a vendor\ SUBDIRECTORY, and the helper makes it, so the rule is enforced where
    # the copy happens rather than remembered at each call site.
    $vlSub = Join-Path (Join-Path $vlOut 'run') 'vendor'
    Check '8p the vendor\ subdirectory does not exist before the copy (the control for the next check)' (
        -not (Test-Path -LiteralPath $vlSub))
    $vlDeep = Copy-VendorLogByPid -ProcessId 83276 -LogDir $vlDir -NamePrefix 'vrfGui' -Since $vlSince -Destination (Join-Path $vlSub 'vendor-vrfGui.log')
    Check '8p the copy CREATES its destination directory, so vendor\ needs no separate mkdir' (
        $vlDeep['Source'] -eq $vlWanted -and (Test-Path -LiteralPath (Join-Path $vlSub 'vendor-vrfGui.log'))) "error=$($vlDeep['Error'])"
    Check '8p and nothing matching *.log is left at the run-directory level by that copy' (
        @(Get-ChildItem -LiteralPath (Join-Path $vlOut 'run') -Filter '*.log' -File -ErrorAction SilentlyContinue).Count -eq 0)
    Check '8p Get-VendorLogDir is the one name for it (runner, manifest and tests cannot disagree)' (
        (Get-VendorLogDir -RunDir 'C:\x\runs\20260921T000000Z_run') -eq 'C:\x\runs\20260921T000000Z_run\vendor')
} finally {
    Remove-Item -LiteralPath $vlDir -Recurse -Force -ErrorAction SilentlyContinue
}
# THE SECRECY POSTURE, as a structural fact rather than a promise: the helper copies and reads
# nothing. A future edit that adds a Get-Content / Select-String / Read-LiveText inside it fails
# here. (Get-ChildItem and its .Length are DIRECTORY-ENTRY reads, not content.)
$libAst = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $RepoRoot 'scripts\RunnerLib.ps1'), [ref]$null, [ref]$null)
$vlFn = $libAst.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $a.Name -eq 'Copy-VendorLogByPid' }, $true)
Check '8p Copy-VendorLogByPid lives in RunnerLib (so it can be tested offline at all)' (@($vlFn).Count -eq 1)
if (@($vlFn).Count -eq 1) {
    $vlCmds = @($vlFn[0].FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] }, $true) |
                ForEach-Object { $_.GetCommandName() } | Where-Object { $_ })
    Check '8p Copy-VendorLogByPid NEVER opens a vendor log (no Get-Content/Select-String/Read-LiveText/Get-FileHash)' (
        @($vlCmds | Where-Object { $_ -in @('Get-Content','Select-String','Read-LiveText','Import-Csv','Get-FileHash','Out-String') }).Count -eq 0) (
        "commands: " + ($vlCmds -join ','))
    Check '8p Copy-VendorLogByPid copies (never moves) and excludes the callstack file' (
        ($vlCmds -contains 'Copy-Item') -and ($vlCmds -notcontains 'Move-Item') -and
        $vlFn[0].Extent.Text -match '\\\.callstack\\\.log\$')
}
Check '8p runner: the 5.2 teardown captures by pid for BOTH processes it launched' (
    $runnerText -match "procId = \`$BackendPid"  -and $runnerText -match "procId = \`$FrontendPid" -and
    $runnerText -match "Copy-VendorLogByPid -ProcessId")
Check '8p runner: the front-end pid is parsed out of the launch output, like the back-end pid' (
    $runnerText -match "front-end started \\\(pid \(\\d\+\)\\\)")
Check '8p runner: the copy is announced WITH the secrets warning, and the manifest repeats it' (
    $runnerText -match 'SECRETS: \{0\} holds the FULL PROCESS ENVIRONMENT IN CLEARTEXT' -and
    $runnerText -match 'vendorLogs = \[ordered\]@\{')
Check '8p runner: the 5.0.2 capture still reads the SAME flat sources (that profile writes them)' (
    $runnerText -match "foreach \(\`$lg in @\('vrfSim\.log', 'vrfGui\.log'\)\)" -and
    $runnerText -match "bin64-' \+ \`$lg")
# 2026-09-21: BOTH profiles' copies land under vendor\, and NEITHER call site builds the path
# from a literal. The run directory itself must stay free of vendor *.log files, because that is
# what the `runs\<run>\*.log` glob reads.
Check '8p runner: the 5.2 destination is Get-VendorLogDir, not a path built beside our own logs' (
    $runnerText -match 'Get-VendorLogDir -RunDir \$RunDir' -and
    $runnerText -notmatch '\$dstPath = Join-Path \$RunDir \$vl\.dst')
Check '8p runner: the 5.0.2 bin64-*.log copy goes under vendor\ too' (
    $runnerText -match "Join-Path \`$b64Dir \('bin64-' \+ \`$lg\)" -and
    $runnerText -notmatch "Join-Path \`$RunDir \('bin64-' \+ \`$lg\)")
Check '8p runner: the dry-run plan and the secrets WARN both name the vendor\ subdirectory and the glob rule' (
    $provDefFlat -match 'vendor\\vendor-vrfSim\.log' -and
    $runnerText -match 'runs\\<run>\\\*\.log glob' -and
    $runnerText -match 'never glob')
Check '8p runner: the manifest records WHERE the copies are and WHY' (
    $runnerText -match 'directory  = \(Get-VendorLogDir -RunDir \$RunDir\)' -and
    $runnerText -match 'whySubdirectory\s*=')
Check '8p RunnerLib: Copy-VendorLogByPid creates the destination directory itself' (
    $libAst.Extent.Text -match 'New-Item -ItemType Directory -Path \$dstDir')
Check '8p tools\analysis\run_census.py reads vendor\ FIRST and still falls back to the old flat path' (
    ((Get-Content -LiteralPath (Join-Path $RepoRoot 'tools\analysis\run_census.py') -Raw) -match
     'os\.path\.join\(rd, "vendor", "bin64-vrfSim\.log"\)') -and
    ((Get-Content -LiteralPath (Join-Path $RepoRoot 'tools\analysis\run_census.py') -Raw) -match
     'os\.path\.join\(rd, "bin64-vrfSim\.log"\)'))

# 8q. D1b harvest A3: LaunchVrf52 shouts "-FederationHoldSecs 0 ... this launch's own back end
# will be the federation CREATOR - the STP-825 failure mode" whenever it has no holder of its
# own. That is true standalone and FALSE under the runner, which passes 0 precisely because its
# Stage 2h holder is already joined - so every 5.2 run through the runner printed the alarm
# twice, falsely. -FederationHeldByCaller makes the line say what is true. The STANDALONE
# DEFAULT MUST NOT CHANGE, which is what 8m's own assertions keep pinning.
Write-Host '=== 8q. D1b A3: -FederationHeldByCaller - the false STP-825 alarm under the runner ==='
$hbcOut  = (& $holdPwsh -NoProfile -File $lv52Script -DryRun -NoGui -BackendAppNumber 9101 -FederationHoldSecs 0 -FederationHeldByCaller 2>&1 | Out-String)
$hbcCode = $LASTEXITCODE
$hbcFlat = ($hbcOut -replace '\s+', ' ')
Check '8q the switch is ACCEPTED (exit 0 or 2, never a parameter-binding failure)' (
    $hbcCode -in @(0, 2)) "exit=$hbcCode"
Check '8q with the switch, the banner says the CALLER holds the federation' (
    $hbcFlat -match 'Federation hold : OFF for this launch \(-FederationHoldSecs 0 -FederationHeldByCaller\): the CALLER already holds the federation')
Check '8q with the switch, the false STP-825 CREATOR alarm is GONE' (
    $hbcFlat -notmatch "this launch's own back end will be the federation CREATOR")
Check '8q with the switch, the plan still says no holder is started HERE (nothing is hidden, only relabelled)' (
    $hbcFlat -match 'NO holder is started BY THIS SCRIPT because the CALLER already holds the federation')
Check '8q WITHOUT the switch the alarm is still printed - the standalone default is untouched' (
    $lv52HoldOffFlat -match "this launch's own back end will be the federation CREATOR")
$hbcBoth  = (& $holdPwsh -NoProfile -File $lv52Script -DryRun -NoGui -BackendAppNumber 9101 -FederationHoldSecs 900 -FederationHeldByCaller 2>&1 | Out-String)
$hbcBothCode = $LASTEXITCODE
Check '8q the switch with a POSITIVE hold is REFUSED (two holders would burn an appNumber for nothing)' (
    $hbcBothCode -eq 2 -and $hbcBoth -match '-FederationHeldByCaller says the CALLER already holds the federation, but -FederationHoldSecs') (
    "exit=$hbcBothCode")
Check '8q LaunchVrf52 declares -FederationHeldByCaller as a SWITCH (declarative, starts nothing)' (
    $lv52Text -match '\[switch\] \$FederationHeldByCaller')
Check '8q runner: Stage 3 passes -FederationHeldByCaller ONLY when its own Stage 2h holder is armed' (
    $runnerText -match "if \(\`$FederationHoldOn\) \{ \`$launchArgs \+= '-FederationHeldByCaller' \}")
if ($holdOn -notmatch 'DRY RUN - the full planned sequence') {
    Check '8q runner dry-run leg SKIPPED - the 8k dry run did not reach the planned sequence in this checkout' $true
} else {
    Check '8q runner Stage 3 command line carries -FederationHoldSecs 0 -FederationHeldByCaller with the holder ON' (
        $holdOnFlat -match '-FederationHoldSecs 0 -FederationHeldByCaller')
    Check '8q with the runner''s OWN holder OFF the switch is NOT passed - then nobody holds it and the alarm is TRUE' (
        $holdOffFlat -notmatch '-FederationHeldByCaller')
}

# 8r. D1b harvest A2: the .NET tools (RtiProbe, WatchVrf, PauseSim) resolve their connection
# config from the BOUND vrfcontrol.dll - the VENDOR tree - while the sim, the gui and the app
# read the copy under -VrfAppDataDir (holder.1.stdout.log:5, "source=bound stack"). Harmless
# today because the two files are byte-identical; "they are identical" was an ASSUMPTION nothing
# checked. The runner now HASHES BOTH and says so either way. It does not re-plumb the tools.
Write-Host '=== 8r. D1b A2: the two connection configs are VERIFIED identical, not assumed ==='
Check '8r runner: the vendor copy is resolved as its own path, beside the (possibly relocated) one' (
    $runnerText -match "\`$ConnConfigVendorFile = Join-Path \`$VrfRoot 'appData\\settings\\connections\\MAK-ONE-2025-Config\.xml'")
Check '8r runner: BOTH files are hashed with SHA256' (
    $runnerText -match "\`$ConnConfigSha\s+= \(Get-FileHash -LiteralPath \`$ConnConfigFile\s+-Algorithm SHA256\)\.Hash" -and
    $runnerText -match "\`$ConnConfigVendorSha = \(Get-FileHash -LiteralPath \`$ConnConfigVendorFile -Algorithm SHA256\)\.Hash")
Check '8r runner: a DIFFERENCE is a loud validity flag naming both paths and both hashes' (
    $runnerText -match "Add-Flag 'WARN' \('CONNECTION CONFIG DIVERGENCE")
Check '8r runner: an UNREADABLE copy is reported too, not silently treated as equal' (
    $runnerText -match 'could not compare the two connection configs')
Check '8r runner: the manifest carries both hashes and the verdict' (
    $runnerText -match 'connectionConfigVendorFile\s+=' -and
    $runnerText -match 'connectionConfigSha256\s+= \$ConnConfigSha' -and
    $runnerText -match 'connectionConfigVendorSha256 = \$ConnConfigVendorSha' -and
    $runnerText -match 'connectionConfigIdentical\s+= \$ConnConfigMatch')
Check '8r runner: the tools are NOT re-plumbed - the check only REPORTS (no new tool argument was added)' (
    $runnerText -notmatch '-ConnectionConfigFile \$ConnConfigVendorFile')
if ($provDefFlat -notmatch 'VrfProfile 5\.2 - VR-Forces') {
    Check '8r behaviour leg SKIPPED - the 5.2 banner is printed after Stage 0 validation, which aborts in a checkout with no Release-5.2 binaries' $true
} else {
    Check '8r a DEFAULT 5.2 run (appData not relocated) says the two are ONE FILE, and hashes it' (
        $provDefFlat -match 'conn config : \S*MAK-ONE-2025-Config\.xml sha256' -and
        $provDefFlat -match 'the sim, the gui, the app and the \.NET tools all read this one file')
}

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

# 10. STP-844 - THE TWO 5.2 GUI TEARDOWN MODALS, SUPPRESSED BY VENDOR CONFIGURATION.
# Demo rehearsal D1 (run 20260920T172141Z) was the first GUI-ON 5.2 teardown ever run: the
# GUI raised UG52 4.6's exit prompt on StopVrf52's WM_CLOSE, nothing answered it, the
# teardown spent its full 121 s budget and left a JOINED vrfGui behind. A read-only window
# enumeration afterwards found TWO stacked makVrf::DtNeverAskAgainMessageBox modals ("Are
# You Sure?" and, on top of it, "Session Status" - the latter raised by our own ordering,
# the back end being asked to close 20 s after the GUI). The remedy is the vendor's own
# configuration, not a UIA answerer: myShowQuitDialogOnClose (UG52 4.6.1) and the
# DtShowSessionDialogs 0x10 bit of mySessionOptions (UG52 4.3.1), both set in a RUN-OWNED
# appData copy that LaunchVrf52 selects with --appDataDir. Nothing under C:\MAK is written.
# Everything below is OFFLINE: the parsers are pure, and the seeding script runs against a
# throwaway TEMP tree built here, never against the real vendor tree.
Write-Host '=== 10. STP-844 GUI quit/session prompts: pure parsers (RunnerLib) ==='
# The exact shape of the shipped vendor files, cut down to the lines that matter:
# C:\MAK\vrforces5.2d\appData\settings\vrfGui\default_Application.apsx (boost archive
# version 9, Application class version 6) and default_SessionSettings.srsx (SessionSettings
# class version 14). 112885 = 0x1B8F5, which HAS bit 0x10 set.
$appOn  = "<Application class_id=`"0`" tracking_level=`"0`" version=`"6`">`r`n`t<myMouseHideTime>3</myMouseHideTime>`r`n`t<myShowQuitDialogOnClose>1</myShowQuitDialogOnClose>`r`n</Application>"
$appOff = $appOn -replace '<myShowQuitDialogOnClose>1<', '<myShowQuitDialogOnClose>0<'
$sesOn  = "<SessionSettings class_id=`"0`" tracking_level=`"0`" version=`"14`">`r`n`t<mySessionOptions>112885</mySessionOptions>`r`n</SessionSettings>"
$sesOff = $sesOn -replace '112885', '112869'

$stOn = Get-VrfGuiPromptSettings -ApplicationXml $appOn -SessionSettingsXml $sesOn
Check '10 the SHIPPED vendor values read as both prompts ON (this is what D1 launched against)' (
    $stOn.ShowQuitDialogOnClose -eq 1 -and $stOn.SessionOptions -eq 112885 -and
    $stOn.ShowSessionDialogs -eq $true -and (-not $stOn.Unattended)) $stOn.Summary
$stOff = Get-VrfGuiPromptSettings -ApplicationXml $appOff -SessionSettingsXml $sesOff
Check '10 the patched values read as Unattended (quit prompt off AND session dialogs off)' (
    $stOff.ShowQuitDialogOnClose -eq 0 -and $stOff.SessionOptions -eq 112869 -and
    $stOff.ShowSessionDialogs -eq $false -and $stOff.Unattended) $stOff.Summary
# HALF-DONE MUST NOT READ AS DONE: either prompt alone still hangs a teardown.
Check '10 quit prompt off but session dialogs still ON is NOT Unattended' (
    -not (Get-VrfGuiPromptSettings -ApplicationXml $appOff -SessionSettingsXml $sesOn).Unattended)
Check '10 session dialogs off but quit prompt still ON is NOT Unattended' (
    -not (Get-VrfGuiPromptSettings -ApplicationXml $appOn -SessionSettingsXml $sesOff).Unattended)
# An ABSENT key is reported as absent, never defaulted: "not there" and "0" have opposite
# consequences and a launch precheck that guessed would be a false green.
$stNone = Get-VrfGuiPromptSettings -ApplicationXml '' -SessionSettingsXml ''
Check '10 missing keys read as ABSENT (null), not as 0, and never as Unattended' (
    $null -eq $stNone.ShowQuitDialogOnClose -and $null -eq $stNone.SessionOptions -and
    $null -eq $stNone.ShowSessionDialogs -and (-not $stNone.Unattended) -and
    $stNone.Summary -match 'ABSENT') $stNone.Summary

Write-Host '=== 10b. STP-844 the two edits: pinned value, MASKED flag, idempotent ==='
$edApp = Set-VrfGuiPromptSettingsText -Kind 'Application' -Text $appOn
Check '10b Application 1 -> 0, one byte, everything else untouched' (
    $edApp.Changed -and $edApp.KeyFound -and $edApp.Before -eq '1' -and $edApp.After -eq '0' -and
    $edApp.Text -eq $appOff) 'text differs from the expected patched form'
Check '10b Application edit is idempotent (already 0 -> Changed false, KeyFound true)' (
    $($r = Set-VrfGuiPromptSettingsText -Kind 'Application' -Text $appOff; (-not $r.Changed) -and $r.KeyFound))
Check '10b Application: key absent -> KeyFound false and the text is returned UNCHANGED (never invented)' (
    $($r = Set-VrfGuiPromptSettingsText -Kind 'Application' -Text '<Application/>'; (-not $r.KeyFound) -and (-not $r.Changed) -and $r.Text -eq '<Application/>'))
$edSes = Set-VrfGuiPromptSettingsText -Kind 'SessionSettings' -Text $sesOn
Check '10b SessionSettings 112885 -> 112869 (0x10 cleared)' (
    $edSes.Changed -and $edSes.Before -eq '112885' -and $edSes.After -eq '112869' -and $edSes.Text -eq $sesOff) $edSes.After
# THE EDIT IS A MASK, NOT A LITERAL. The same word carries DtAutoJoinSession 0x1 and
# DtAlwaysJoinWithSessionDatabase 0x4 (both SET in the shipped value, which is why D1's GUI
# auto-joined with no join prompt) and DtAskToJoin 0x2 (CLEAR). Rewriting the word to a
# constant would silently change how the GUI joins its session.
$after = [int]$edSes.After
Check '10b the MASK preserves every other flag: 0x1 and 0x4 still SET, 0x2 still CLEAR, 0x40 still SET' (
    ($after -band 0x1) -ne 0 -and ($after -band 0x4) -ne 0 -and ($after -band 0x2) -eq 0 -and ($after -band 0x40) -ne 0) ("got $after")
Check '10b a word WITHOUT 0x10 is left alone (Changed false), and 0x7B -> 0x6B' (
    $(  $noBit = Set-VrfGuiPromptSettingsText -Kind 'SessionSettings' -Text '<mySessionOptions>5</mySessionOptions>'
        $withBit = Set-VrfGuiPromptSettingsText -Kind 'SessionSettings' -Text '<mySessionOptions>123</mySessionOptions>'
        (-not $noBit.Changed) -and $noBit.KeyFound -and $withBit.Changed -and $withBit.After -eq '107'))

Write-Host '=== 10c. STP-844 NewVrfAppData52.ps1 against a THROWAWAY temp tree (never the vendor tree) ==='
$gqSrc  = Join-Path ([System.IO.Path]::GetTempPath()) ('stp844src-'  + [guid]::NewGuid().ToString('N'))
$gqDst  = Join-Path ([System.IO.Path]::GetTempPath()) ('stp844dst-'  + [guid]::NewGuid().ToString('N'))
$gqExe  = Join-Path $RepoRoot 'scripts\NewVrfAppData52.ps1'
$gqPwsh = 'C:\Program Files\PowerShell\7\pwsh.exe'
try {
    $gqGui = Join-Path $gqSrc 'appData\settings\vrfGui'
    $null = New-Item -ItemType Directory -Path (Join-Path $gqGui 'backups') -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $gqSrc 'appData\cache') -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $gqSrc 'data') -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $gqSrc 'userData') -Force
    # Written as BYTES with a no-BOM UTF-8 encoding, exactly as the vendor ships them
    # (CRLF, ASCII, no BOM): the point of the round trip is that the seeding script's edit
    # changes ONE byte and leaves the encoding and the line endings alone.
    $gqEnc = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllBytes((Join-Path $gqGui 'default_Application.apsx'),     $gqEnc.GetBytes($appOn  + "`r`n"))
    [System.IO.File]::WriteAllBytes((Join-Path $gqGui 'default_SessionSettings.srsx'), $gqEnc.GetBytes($sesOn  + "`r`n"))
    [System.IO.File]::WriteAllBytes((Join-Path $gqGui 'backups\Application.backup'),   $gqEnc.GetBytes($appOn  + "`r`n"))
    $gqSrcBytes = [System.IO.File]::ReadAllBytes((Join-Path $gqGui 'default_Application.apsx'))

    if (-not (Test-Path -LiteralPath $gqPwsh)) {
        Check '10c SKIPPED - pwsh 7 not at the expected path, so the seeding script cannot be run here' $true
    } else {
        $gqDry = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest $gqDst -VrfRoot $gqSrc -DryRun 2>&1 | Out-String)
        $gqDryCode = $LASTEXITCODE
        Check '10c the DRY RUN exits 0, copies nothing, and says so' (
            $gqDryCode -eq 0 -and (-not (Test-Path -LiteralPath $gqDst)) -and
            $gqDry -match 'nothing is copied, linked or written') "exit=$gqDryCode"
        Check '10c the dry-run plan names BOTH edits by their vendor citation' (
            $gqDry -match 'myShowQuitDialogOnClose -> 0 \(UG52 4\.6\.1\)' -and
            $gqDry -match 'clear DtShowSessionDialogs \(UG52 4\.3\.1\)')

        $gqOut  = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest $gqDst -VrfRoot $gqSrc 2>&1 | Out-String)
        $gqCode = $LASTEXITCODE
        $gqAppOut  = Join-Path $gqDst 'appData\settings\vrfGui\default_Application.apsx'
        $gqSesOut  = Join-Path $gqDst 'appData\settings\vrfGui\default_SessionSettings.srsx'
        Check '10c the real seed exits 0 and produces the two settings files' (
            $gqCode -eq 0 -and (Test-Path -LiteralPath $gqAppOut) -and (Test-Path -LiteralPath $gqSesOut)) "exit=$gqCode"
        $gqState = Get-VrfGuiPromptSettings `
            -ApplicationXml     $(if (Test-Path -LiteralPath $gqAppOut) { Get-Content -LiteralPath $gqAppOut -Raw } else { '' }) `
            -SessionSettingsXml $(if (Test-Path -LiteralPath $gqSesOut) { Get-Content -LiteralPath $gqSesOut -Raw } else { '' })
        Check '10c BOTH keys land in the RIGHT FILES of the seeded tree (read back from disk)' (
            $gqState.Unattended -and $gqState.ShowQuitDialogOnClose -eq 0 -and $gqState.SessionOptions -eq 112869) $gqState.Summary
        # THE SOURCE MUST BE UNTOUCHED. The whole (a)-over-(b) argument is that the tree
        # the GUI reads by default is never written to; if seeding mutated its source this
        # script would be doing exactly what it claims not to do.
        Check '10c the SOURCE tree is byte-identical afterwards (the vendor tree is only ever read)' (
            $(@(Compare-Object $gqSrcBytes ([System.IO.File]::ReadAllBytes((Join-Path $gqGui 'default_Application.apsx')))).Count -eq 0))
        if (Test-Path -LiteralPath $gqAppOut) {
            $gqOutBytes = [System.IO.File]::ReadAllBytes($gqAppOut)
            $gqDiff = 0
            for ($i = 0; $i -lt [Math]::Min($gqSrcBytes.Length, $gqOutBytes.Length); $i++) {
                if ($gqSrcBytes[$i] -ne $gqOutBytes[$i]) { $gqDiff++ }
            }
            Check '10c the edit changes EXACTLY ONE BYTE and keeps the length, the CRLFs and the absence of a BOM' (
                $gqOutBytes.Length -eq $gqSrcBytes.Length -and $gqDiff -eq 1 -and $gqOutBytes[0] -eq 60) "diff=$gqDiff len=$($gqOutBytes.Length)/$($gqSrcBytes.Length)"
        }
        Check '10c the missing backups\SessionSettings.backup is tolerated, not an error' (
            $gqOut -match 'not present \(fine, the GUI rewrites it at startup\)')
        Check '10c the README records the RESTORE PATH (delete the copy; nothing in C:\MAK to put back)' (
            $(  $rp = Join-Path $gqDst 'README-C2SIM-UNATTENDED.txt'
                (Test-Path -LiteralPath $rp) -and ((Get-Content -LiteralPath $rp -Raw) -match 'RESTORE PATH')))
        # Re-running must be cheap and must not re-copy: the demo script may call it every time.
        $gqAgain = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest $gqDst -VrfRoot $gqSrc 2>&1 | Out-String)
        Check '10c a second run is idempotent: no re-copy, both keys already correct, exit 0' (
            $LASTEXITCODE -eq 0 -and $gqAgain -match 'skipping the copy' -and
            $gqAgain -match 'already 0 - unchanged' -and $gqAgain -match 'already 112869 - unchanged')
        # THE HARD CONSTRAINT, exercised rather than asserted about: a -Dest inside the
        # vendor tree is refused with exit 2 before anything is copied.
        $gqMak = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest ('C' + ':\MAK\vrforces5.2d\appData-test') -VrfRoot $gqSrc 2>&1 | Out-String)
        Check '10c a -Dest under the vendor tree is REFUSED (exit 2) and names why' (
            $LASTEXITCODE -eq 2 -and $gqMak -match 'REFUSED') "exit=$LASTEXITCODE"
    }
} finally {
    Remove-Item -LiteralPath $gqDst -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $gqSrc -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host '=== 10d. STP-844 StopVrf52: the lost diagnostic is back, and still nothing is clicked ==='
$sv52Path = Join-Path $RepoRoot 'scripts\StopVrf52.ps1'
$sv52Text = Get-Content -LiteralPath $sv52Path -Raw
# Assert against CODE, not comments: the header legitimately discusses Stop-Process, /F and
# clicking in order to say they are never used, and a naive whole-file match would flag it.
$sv52Code = (@(Get-Content -LiteralPath $sv52Path | Where-Object { $_.Trim() -notmatch '^#' }) -join "`n")
Check '10d StopVrf52 restores the three READ-ONLY enumerators StopVrf.ps1 has' (
    $sv52Code -match 'function Get-VrfWindows' -and
    $sv52Code -match 'function Get-VrfNestedWindows' -and
    $sv52Code -match 'function Get-DialogButtonNames')
Check '10d the diagnostic runs on the TIMEOUT path (the artifact D1 owed and did not have)' (
    $sv52Code -match 'Write-WindowDiagnostic -Why \(.TIMEOUT after')
Check '10d and after the grace, BEFORE the back end is asked (so the two modals can be told apart)' (
    $sv52Code -match 'Write-WindowDiagnostic -Why \(.after the . \+ \$GraceSec')
Check '10d it reports both ENABLED state and BUTTON NAMES - MainWindowTitle alone named neither D1 dialog' (
    $sv52Code -match 'IsWindowEnabled' -and $sv52Code -match 'Get-DialogButtonNames -element')
Check '10d CloseMainWindow''s return value is CAPTURED and reported, not discarded' (
    $sv52Code -match '\$sent\s*=\s*\$p\.CloseMainWindow\(\)' -and
    $sv52Code -notmatch '\$null\s*=\s*\$p\.CloseMainWindow\(\)' -and
    $sv52Code -match 'returned FALSE')
# STILL NO GUI AUTOMATION. The project goal is headless; an answerer is out of scope and
# must not creep in with the diagnostic that was only ever meant to LOG.
Check '10d NO click path exists in the code: no InvokePattern, no Invoke(), no SetForegroundWindow, no Toggle' (
    $sv52Code -notmatch 'InvokePattern' -and $sv52Code -notmatch 'TogglePattern' -and
    $sv52Code -notmatch 'SetForegroundWindow' -and $sv52Code -notmatch '\.Invoke\(\)')
Check '10d NO Stop-Process anywhere in the code (RUNBOOK sec 0)' (
    $sv52Code -notmatch 'Stop-Process')
# taskkill appears twice in the code: the one INVOCATION and the log line that echoes what
# was run. Only the invocation may be asserted on - the log line legitimately contains the
# string "/F" in "(no /F)". So: find the call sites, insist there is exactly one, and
# insist IT carries no /F. A whole-file '/F' match would flag the log line and a whole-file
# 'no /F' match would pass even if the call site grew one.
$sv52TaskkillCalls = @(Get-Content -LiteralPath $sv52Path |
    Where-Object { $_.Trim() -notmatch '^#' -and $_ -match '&\s+taskkill' })
Check '10d exactly ONE taskkill call site, and it carries no /F (a force-killed joined federate hangs the next join)' (
    $sv52TaskkillCalls.Count -eq 1 -and $sv52TaskkillCalls[0] -notmatch '/F') ("call sites: " + $sv52TaskkillCalls.Count)
Check '10d the UIA half degrades instead of failing the teardown when the types cannot be loaded' (
    $sv52Code -match '\$script:UiaOk\s*=\s*\$false' -and $sv52Code -match 'if \(-not \$script:UiaOk\)')
$sv52Dry = (& $gqPwsh -NoProfile -NonInteractive -File $sv52Path -DryRun 2>&1 | Out-String)
if ($sv52Dry -notmatch 'Dry run - what WOULD happen') {
    Check '10d SKIPPED - no VR-Forces process is running, so StopVrf52 -DryRun exits at "nothing to do" before the plan' $true
} else {
    Check '10d the dry-run plan names the window diagnostic and the CloseMainWindow return value' (
        $sv52Dry -match 'READ-ONLY WINDOW DIAGNOSTIC' -and $sv52Dry -match "would REPORT CloseMainWindow")
}

Write-Host '=== 10e. STP-844 LaunchVrf52 precheck: GUI-on only, advisory only, no StrictMode leak ==='
Check '10e the precheck is gated on a front end actually being launched (-NoGui raises no dialog)' (
    $lv52Text -match 'if \(-not \$NoGui\) \{[\s\S]{0,4000}vrfGui TEARDOWN PROMPTS ARE ON')
Check '10e it reads the EFFECTIVE appData - the relocated tree when -AppDataDir is given, the vendor default otherwise' (
    $lv52Text -match '\$effAppData = \$\(if \(\[string\]::IsNullOrWhiteSpace\(\$AppDataDir\)\)')
Check '10e it WARNS and never refuses (a GUI-on interactive launch with the prompt on is valid)' (
    $lv52Text -match 'NOT A REFUSAL' -and
    -not ([regex]::IsMatch($lv52Text, 'TEARDOWN PROMPTS ARE ON[\s\S]{0,1500}\$hardFail = \$true')))
Check '10e it names STP-844, the run that proved it, and the remedy script' (
    $lv52Text -match 'STP-844' -and $lv52Text -match '20260920T172141Z' -and $lv52Text -match 'NewVrfAppData52\.ps1')
# RunnerLib.ps1 opens with Set-StrictMode -Version Latest and LaunchVrf52 has never run
# under it. Dot-sourcing at script scope would turn StrictMode on for a LIVE LAUNCH, where
# an unset variable anywhere downstream becomes a terminating error mid-flight.
Check '10e RunnerLib is dot-sourced inside a CHILD SCOPE, never at LaunchVrf52 script scope' (
    $lv52Text -match '& \{\s*\r?\n\s*param\(\$libPath, \$appXml, \$sessXml\)\s*\r?\n\s*\. \$libPath' -and
    -not ([regex]::IsMatch($lv52Text, '(?m)^\s*\.\s+\(Join-Path \$PSScriptRoot ''RunnerLib\.ps1''\)')))
$lv52Code = (@(Get-Content -LiteralPath $lv52Script | Where-Object { $_.Trim() -notmatch '^#' }) -join "`n")
Check '10e LaunchVrf52 still EXECUTES no Set-StrictMode of its own (the reason the child scope matters)' (
    $lv52Code -notmatch 'Set-StrictMode')

# 10f. STP-844 COLD-START REVIEW FIXES (items 2-6 of scratchpad\validation\guiquit_review.md).
# Five should-fixes, none of them a behaviour change to the remedy itself, all of them
# things that could waste or wreck the confirming run:
#   2. a TRAILING BACKSLASH on -AppDataDir renders as \" to the C runtime, so vrfGui
#      silently ignores --appDataDir while the precheck prints [OK] - a false green of
#      exactly the kind that costs a whole run.
#   3. the precheck's two Get-Content calls sat OUTSIDE its try, under
#      $ErrorActionPreference='Stop' in a script with no outer catch - an advisory read
#      could abort a LIVE LAUNCH.
#   4/5. the Add-Type and the window diagnostic were unguarded on the TEARDOWN path,
#      BETWEEN the grace and the back-end close - a throw there exits 5 and leaves a JOINED
#      back end running, which is worse than having no diagnostic.
#   6. the C:\MAK guard's comment claimed junction/subst protection the code did not
#      implement; \\?\C:\MAK\..., \\.\C:\MAK\... and \\localhost\C$\MAK\... all passed.
Write-Host '=== 10f. STP-844 review item 2: a trailing backslash on -AppDataDir must not produce \" ==='
Check '10f LaunchVrf52 trims trailing separators from -AppDataDir ONCE, before any reader' (
    $lv52Text -match [regex]::Escape("`$AppDataDir = `$AppDataDir.TrimEnd('\', '/')"))
Check '10f and refuses a bare drive root, which survives the trim as the DRIVE-RELATIVE "C:"' (
    $lv52Text -match "\`$AppDataDir -match '\^\[A-Za-z\]:\`$'" -and $lv52Text -match 'bare drive root')
# Behavioural half: run the real script and read the command lines it would use. A static
# match cannot prove the trim reached BOTH argument strings, and it is the rendered
# argument - not the variable - that the C runtime mis-parses.
$gqTrimDir = Join-Path ([System.IO.Path]::GetTempPath()) ('stp844trim-' + [guid]::NewGuid().ToString('N'))
try {
    $null = New-Item -ItemType Directory -Path (Join-Path $gqTrimDir 'settings\vrfGui') -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $gqTrimDir 'settings\vrfSim') -Force
    if (-not (Test-Path -LiteralPath $gqPwsh)) {
        Check '10f SKIPPED - pwsh 7 not at the expected path' $true
    } else {
        # NOTE the deliberate trailing backslash - this is what tab completion produces.
        $gqTrimOut = (& $gqPwsh -NoProfile -NonInteractive -File $lv52Script -DryRun -NoGui `
                        -BackendAppNumber 9101 -AppDataDir ($gqTrimDir + '\') 2>&1 | Out-String)
        if ($gqTrimOut -notmatch '--appDataDir') {
            Check '10f SKIPPED - the dry run refused before it printed a command line' $true
        } else {
            Check '10f the rendered --appDataDir argument carries NO backslash-quote (the C runtime would read \" as an escaped quote)' (
                $gqTrimOut -notmatch '--appDataDir "[^"]*\\"') 'a trailing \" survived into the argument string'
            Check '10f the path still reaches the argument, i.e. the trim did not eat the directory' (
                $gqTrimOut -match [regex]::Escape('--appDataDir "' + $gqTrimDir + '"'))
        }
        $gqRootOut  = (& $gqPwsh -NoProfile -NonInteractive -File $lv52Script -DryRun -NoGui `
                        -BackendAppNumber 9101 -AppDataDir 'Q:\' 2>&1 | Out-String)
        $gqRootCode = $LASTEXITCODE
        Check '10f a bare drive root is refused at the argument gate (exit 2), not silently resolved against the cwd' (
            $gqRootCode -eq 2 -and $gqRootOut -match 'bare drive root') "exit=$gqRootCode"
    }
} finally { Remove-Item -LiteralPath $gqTrimDir -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host '=== 10f2. STP-844 review item 3: the precheck cannot abort a live launch ==='
# AST, not regex: find the two Get-Content calls that read the vrfGui settings files and
# prove each one is INSIDE a try. A regex would only show they moved, not that they landed
# somewhere protected.
$lv52Ast  = [System.Management.Automation.Language.Parser]::ParseFile($lv52Script, [ref]$null, [ref]$null)
function Test-InsideTry {
    param($node)
    $n = $node
    while ($null -ne $n) {
        if ($n -is [System.Management.Automation.Language.TryStatementAst]) { return $true }
        $n = $n.Parent
    }
    return $false
}
$gqGuiReads = @($lv52Ast.FindAll({
    param($n)
    $n -is [System.Management.Automation.Language.CommandAst] -and
    $n.GetCommandName() -eq 'Get-Content' -and
    $n.Extent.Text -match 'guiAppFile|guiSessFile' }, $true))
Check '10f2 both vrfGui settings reads are present and BOTH are inside a try (AST, not a grep)' (
    $gqGuiReads.Count -eq 2 -and
    @($gqGuiReads | Where-Object { Test-InsideTry $_ }).Count -eq 2) ("found " + $gqGuiReads.Count)

Write-Host '=== 10f3. STP-844 review items 4+5: a failing diagnostic must not cost the back end its close ==='
$sv52Ast = [System.Management.Automation.Language.Parser]::ParseFile($sv52Path, [ref]$null, [ref]$null)
$gqDiagCalls = @($sv52Ast.FindAll({
    param($n)
    $n -is [System.Management.Automation.Language.CommandAst] -and
    $n.GetCommandName() -eq 'Write-WindowDiagnostic' }, $true))
Check '10f3 every Write-WindowDiagnostic call site is wrapped in its OWN try (AST)' (
    $gqDiagCalls.Count -ge 2 -and
    @($gqDiagCalls | Where-Object { Test-InsideTry $_ }).Count -eq $gqDiagCalls.Count) ("call sites: " + $gqDiagCalls.Count)
# The post-grace call sits BETWEEN the grace loop and the taskkill. Prove the back-end stop
# is REACHABLE past it: the taskkill call must not be inside the same try as the diagnostic,
# or a throw would skip it even with the catch present.
$gqTaskkill = @($sv52Ast.FindAll({
    param($n)
    $n -is [System.Management.Automation.Language.CommandAst] -and
    $n.GetCommandName() -eq 'taskkill' }, $true))
Check '10f3 the back-end taskkill exists and is NOT nested inside a diagnostic try block' (
    $gqTaskkill.Count -eq 1 -and
    ($gqTaskkill[0].Extent.StartOffset -gt $gqDiagCalls[0].Extent.StartOffset) -and
    @($gqDiagCalls | Where-Object {
        $gqTaskkill[0].Extent.StartOffset -gt $_.Extent.StartOffset -and
        $gqTaskkill[0].Extent.EndOffset   -lt $_.Parent.Parent.Extent.EndOffset }).Count -eq 0) ("taskkill sites: " + $gqTaskkill.Count)
# The Add-Type must be LAZY: nothing is compiled on a headless run, a -DryRun, or a run
# with no VR-Forces process. On main it ran unconditionally on every invocation.
$gqAddTypes = @($sv52Ast.FindAll({
    param($n)
    $n -is [System.Management.Automation.Language.CommandAst] -and
    $n.GetCommandName() -eq 'Add-Type' }, $true))
Check '10f3 every Add-Type lives inside the lazy Initialize-WindowDiagnostic function and inside a try' (
    $gqAddTypes.Count -ge 1 -and
    @($gqAddTypes | Where-Object { Test-InsideTry $_ }).Count -eq $gqAddTypes.Count -and
    @($gqAddTypes | Where-Object {
        $f = $_.Parent
        while ($null -ne $f -and -not ($f -is [System.Management.Automation.Language.FunctionDefinitionAst])) { $f = $f.Parent }
        $null -ne $f -and $f.Name -eq 'Initialize-WindowDiagnostic' }).Count -eq $gqAddTypes.Count) ("Add-Type sites: " + $gqAddTypes.Count)
Check '10f3 Get-VrfWindows returns empty rather than throwing when the P/Invoke types never compiled' (
    $sv52Code -match 'if \(-not \$script:WinApiOk\) \{ return @\(\) \}')
Check '10f3 both PropertyCondition constructions sit inside a try, not just the element access (AST)' (
    $(  $conds = @($sv52Ast.FindAll({
            param($n)
            $n -is [System.Management.Automation.Language.CommandAst] -and
            $n.GetCommandName() -eq 'New-Object' -and
            $n.Extent.Text -match 'PropertyCondition' }, $true))
        $conds.Count -eq 2 -and @($conds | Where-Object { Test-InsideTry $_ }).Count -eq 2))
# The real proof: inject a THROWING diagnostic and show the script still reaches the
# back-end close. Done by rewriting the script's own text in a TEMP copy - the repo file is
# not touched - and running it with no VR-Forces process required beyond the dry-run plan.
$gqThrowCopy = Join-Path ([System.IO.Path]::GetTempPath()) ('stp844throw-' + [guid]::NewGuid().ToString('N') + '.ps1')
try {
    $gqThrowText = $sv52Text -replace 'function Write-WindowDiagnostic \{', "function Write-WindowDiagnostic {`r`n    throw 'INJECTED diagnostic failure (test 10f3)'"
    Set-Content -LiteralPath $gqThrowCopy -Value $gqThrowText -Encoding ascii
    $gqThrowParse = $null
    $null = [System.Management.Automation.Language.Parser]::ParseFile($gqThrowCopy, [ref]$null, [ref]$gqThrowParse)
    Check '10f3 the injected-throw copy still parses (the injection landed where it was meant to)' (
        $gqThrowParse.Count -eq 0 -and $gqThrowText -match 'INJECTED diagnostic failure')
    if (-not (Test-Path -LiteralPath $gqPwsh)) {
        Check '10f3 SKIPPED - pwsh 7 not at the expected path' $true
    } else {
        $gqThrowOut  = (& $gqPwsh -NoProfile -NonInteractive -File $gqThrowCopy -DryRun 2>&1 | Out-String)
        $gqThrowCode = $LASTEXITCODE
        # -DryRun never reaches the diagnostic, so this asserts the weaker but still
        # necessary thing: injecting a throwing diagnostic changes no exit code on the
        # paths a test may safely run. Exit 5 here would mean the throw escaped at parse
        # or definition time.
        Check '10f3 a THROWING Write-WindowDiagnostic does not turn a clean run into exit 5' (
            $gqThrowCode -ne 5 -and $gqThrowOut -notmatch 'unexpected terminating error') "exit=$gqThrowCode"
    }
} finally { Remove-Item -LiteralPath $gqThrowCopy -Force -ErrorAction SilentlyContinue }

Write-Host '=== 10f4. STP-844 review item 6: the C:\MAK guard now resolves links instead of claiming to ==='
$gqSeedText = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\NewVrfAppData52.ps1') -Raw
Check '10f4 the comment no longer claims a junction/subst protection GetFullPath cannot give' (
    $gqSeedText -notmatch 'PREFIX test on the raw string as well as on the\r?\n# resolved path' -and
    $gqSeedText -match 'PURE STRING NORMALISATION and')
Check '10f4 C:\MAK is hard-coded into the forbidden set and -AlsoForbiddenRoot can only ADD to it' (
    $gqSeedText -match [regex]::Escape("`$forbiddenRoots = @('C:\MAK') + @(`$AlsoForbiddenRoot") -and
    $gqSeedText -match 'only ADDS')
$gqSeedSrc = Join-Path ([System.IO.Path]::GetTempPath()) ('stp844g-src-'  + [guid]::NewGuid().ToString('N'))
$gqFakeMak = Join-Path ([System.IO.Path]::GetTempPath()) ('stp844g-mak-'  + [guid]::NewGuid().ToString('N'))
$gqLinkDir = Join-Path ([System.IO.Path]::GetTempPath()) ('stp844g-link-' + [guid]::NewGuid().ToString('N'))
try {
    $null = New-Item -ItemType Directory -Path (Join-Path $gqSeedSrc 'appData\settings\vrfGui') -Force
    $null = New-Item -ItemType Directory -Path $gqFakeMak -Force
    $null = New-Item -ItemType Directory -Path $gqLinkDir -Force
    if (-not (Test-Path -LiteralPath $gqPwsh)) {
        Check '10f4 SKIPPED - pwsh 7 not at the expected path' $true
    } else {
        # THE THREE MEASURED BYPASSES. Each must now be refused with exit 2 BEFORE anything
        # is copied. They name the real C:\MAK deliberately - the guard must refuse them, so
        # nothing is ever created there; the run is proved harmless by the exit code and by
        # the refusal text, and no -Force or seeding path is reached.
        foreach ($bad in @(('\\?\' + 'C:\MAK\x'), ('\\.\' + 'C:\MAK\x'), ('\\localhost\C$\MAK\x'))) {
            $o = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest $bad -VrfRoot $gqSeedSrc 2>&1 | Out-String)
            Check ('10f4 bypass form refused: ' + $bad) (
                $LASTEXITCODE -eq 2 -and $o -match 'UNC or device path') "exit=$LASTEXITCODE"
        }
        $o = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest 'relative\path' -VrfRoot $gqSeedSrc 2>&1 | Out-String)
        Check '10f4 a RELATIVE -Dest is refused (it would resolve against the .NET process dir, not the shell)' (
            $LASTEXITCODE -eq 2 -and $o -match 'must be an ABSOLUTE path') "exit=$LASTEXITCODE"
        # THE JUNCTION CASE, built entirely in TEMP and pointed at a TEMP stand-in for the
        # vendor root via -AlsoForbiddenRoot. The real C:\MAK is never a junction target
        # here; what is exercised is the ancestor walk and the link resolution.
        $gqLink = Join-Path $gqLinkDir 'looks-innocent'
        $gqJunctionMade = $false
        try { $null = New-Item -ItemType Junction -Path $gqLink -Target $gqFakeMak; $gqJunctionMade = $true } catch { }
        if (-not $gqJunctionMade) {
            Check '10f4 SKIPPED - could not create a junction in TEMP on this machine' $true
        } else {
            $o = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest (Join-Path $gqLink 'sub') `
                    -VrfRoot $gqSeedSrc -AlsoForbiddenRoot $gqFakeMak 2>&1 | Out-String)
            Check '10f4 a -Dest whose ANCESTOR is a junction into the forbidden root is refused (the old guard let this through)' (
                $LASTEXITCODE -eq 2 -and $o -match 'resolves into .* through a Junction') "exit=$LASTEXITCODE"
            $o = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest $gqLink `
                    -VrfRoot $gqSeedSrc -AlsoForbiddenRoot $gqFakeMak 2>&1 | Out-String)
            Check '10f4 and so is -Dest being the junction ITSELF' (
                $LASTEXITCODE -eq 2 -and $o -match 'resolves into .* through a Junction') "exit=$LASTEXITCODE"
            # THE GUARD MUST NOT BE A BLANKET REFUSAL: an ordinary TEMP path with the same
            # extra forbidden root still passes the guard and reaches the source check.
            $o = (& $gqPwsh -NoProfile -NonInteractive -File $gqExe -Dest (Join-Path $gqLinkDir 'plain') `
                    -VrfRoot $gqSeedSrc -AlsoForbiddenRoot $gqFakeMak -DryRun 2>&1 | Out-String)
            Check '10f4 a plain non-link path under the same parent is NOT refused (the guard discriminates)' (
                $LASTEXITCODE -eq 0 -and $o -match 'nothing is copied, linked or written') "exit=$LASTEXITCODE"
        }
    }
} finally {
    # Remove the junction itself first, so nothing recursive ever walks through it.
    Remove-Item -LiteralPath (Join-Path $gqLinkDir 'looks-innocent') -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $gqLinkDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $gqFakeMak -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $gqSeedSrc -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host '=== 10f5. STP-844 review item 7: the 112885 decomposition is recorded correctly ==='
# 112885 has TWELVE set bits; the enum names only five of them (85). The record used to
# present the word as if it decomposed into the enum alone. The mask must preserve the
# seven unnamed bits, whose meaning nobody has read off a header.
$gqUnnamed = @(0x20, 0x80, 0x800, 0x1000, 0x2000, 0x8000, 0x10000)
Check '10f5 the seven unnamed bits are all SET in the shipped value and sum to 112800' (
    @($gqUnnamed | Where-Object { (112885 -band $_) -ne 0 }).Count -eq 7 -and
    ($gqUnnamed | Measure-Object -Sum).Sum -eq 112800)
Check '10f5 the named bits account for only 85 of 112885' (
    (112885 -band (0x1 -bor 0x2 -bor 0x4 -bor 0x8 -bor 0x10 -bor 0x40 -bor 0x40000)) -eq 85)
Check '10f5 the MASK preserves every one of the seven unnamed bits' (
    $(  $r = Set-VrfGuiPromptSettingsText -Kind 'SessionSettings' -Text '<mySessionOptions>112885</mySessionOptions>'
        $v = [int]$r.After
        @($gqUnnamed | Where-Object { ($v -band $_) -ne 0 }).Count -eq 7 -and $v -eq 112869))
$gqLibText = Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\RunnerLib.ps1') -Raw
Check '10f5 RunnerLib''s citation block SAYS the seven bits are unnamed instead of implying the enum is complete' (
    $gqLibText -match 'NO NAME IN THE 5\.2 HEADER' -and
    $gqLibText -match 'ONLY 85 OF 112885' -and
    $gqLibText -match '0x20, 0x80, 0x800, 0x1000, 0x2000, 0x8000')
Check '10f5 and it names the OTHER explanations for a session-prompt miss, not just "the mapping is wrong"' (
    $gqLibText -match 'DtVrfExtendedApplicationSettingsDataFlags' -and
    $gqLibText -match 'from the SESSION at join time')

Write-Host '=== 11. WAY B AS TYPED (fix/wayb-as-typed, 2026-09-21) ==='
# The hand-started demo path of docs\DEMO_RUNBOOK.md section 2 had never been run end to end,
# and reading it against the runner turned up two defects that would have made the rehearsal
# fail silently - R-3 (the interface started with the WRONG working directory) and DR-1 (no
# endpoint control at all, so the interface listened to one C2SIM server while the documented
# stand-in pushes went to another) - plus operator-facing defects DR-2..DR-8 in the runbook
# itself. These checks pin the fixes. Everything here is -WhatIf or text: nothing is started.
$wbPwsh      = 'C:\Program Files\PowerShell\7\pwsh.exe'
$wbIface     = Join-Path $RepoRoot 'scripts\StartInterface52.ps1'
$wbHolder    = Join-Path $RepoRoot 'scripts\StartFederationHolder52.ps1'
$wbIfaceText = Get-Content -LiteralPath $wbIface -Raw
$wbDemoText  = Get-Content -LiteralPath (Join-Path $RepoRoot 'docs\DEMO_RUNBOOK.md') -Raw
$wbBookText  = Get-Content -LiteralPath (Join-Path $RepoRoot 'docs\RUNBOOK.md') -Raw

# --- 11a. R-3: cwd = VR-Forces bin64 + --contentRoot=<exe dir>, exactly as the runner does ---
$wbIfaceAst = [System.Management.Automation.Language.Parser]::ParseFile($wbIface, [ref]$null, [ref]$null)
$wbSetLoc = @($wbIfaceAst.FindAll({
    param($n)
    $n -is [System.Management.Automation.Language.CommandAst] -and
    $n.GetCommandName() -eq 'Set-Location' }, $true))
Check '11a StartInterface52 has exactly ONE Set-Location and it targets $Bin64, not the exe directory' (
    $wbSetLoc.Count -eq 1 -and $wbSetLoc[0].Extent.Text -match '\$Bin64' -and
    $wbIfaceText -notmatch 'Set-Location \(Split-Path -Parent \$Exe\)') ("Set-Location sites: " + $wbSetLoc.Count)
Check '11a $Bin64 is derived from the SAME $VrfRoot the PATH prefix uses (one VrfHome resolution)' (
    $wbIfaceText -match '\$Bin64\s+= Join-Path \$VrfRoot ''bin64''' -and
    $wbIfaceText -match '\$pathPrefix = \(''\{0\};\{1\};\{2\};'' -f \$Bin64,')
Check '11a the exe is invoked with the --contentRoot argument array, and the reason is cited' (
    $wbIfaceText -match '\$AppArgs\s+= @\(''--contentRoot='' \+ \$ContentRoot\)' -and
    $wbIfaceText -match '& \$Exe @AppArgs' -and
    $wbIfaceText -match 'RUNBOOK sec 7 item 3')
if (-not (Test-Path -LiteralPath $wbPwsh)) {
    Check '11a SKIPPED - pwsh 7 not at the expected path' $true
} else {
    # -WhatIf starts nothing. It prints the plan even when a precondition fails (a worktree has
    # no built exe), which is what makes the plan readable from any tree; the exit code is
    # unchanged and is deliberately NOT asserted here.
    $wbDefault = (& $wbPwsh -NoProfile -NonInteractive -File $wbIface -WhatIf 2>&1 | Out-String)
    Check '11a -WhatIf names cwd = the VR-Forces bin64' (
        $wbDefault -match 'cwd\s+:\s*C:\\MAK\\vrforces5\.2d\\bin64') 'cwd line missing'
    Check '11a -WhatIf names the --contentRoot argument, pointing at the exe directory' (
        $wbDefault -match 'arguments\s+:\s*--contentRoot=.*win-x64') 'arguments line missing'
    Check '11a -WhatIf still says it started nothing' ($wbDefault -match '\(WhatIf: nothing started\)')

    # --- 11b. DR-1: the C2SIM endpoint control, and the loud line that names the server ---
    Check '11b the DEFAULT (-Server standard) names appsettings own 8080/61613 and sets NO env override' (
        $wbDefault -match '\*\*\* C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest=http://127\.0\.0\.1:8080/C2SIMServer\s+stomp=http://127\.0\.0\.1:61613/topic/C2SIM \*\*\*' -and
        $wbDefault -match 'NO env override is set' -and
        $wbDefault -notmatch 'C2SIM__RestUrl\s+=')
    $wbPrivate = (& $wbPwsh -NoProfile -NonInteractive -File $wbIface -WhatIf -Server private 2>&1 | Out-String)
    Check '11b -Server private exports C2SIM__RestUrl / C2SIM__StompUrl for 18080 / 61614' (
        $wbPrivate -match 'C2SIM__RestUrl\s+= http://127\.0\.0\.1:18080/C2SIMServer' -and
        $wbPrivate -match 'C2SIM__StompUrl\s+= http://127\.0\.0\.1:61614/topic/C2SIM')
    Check '11b -Server private says so in the loud line too' (
        $wbPrivate -match '\*\*\* C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest=http://127\.0\.0\.1:18080/C2SIMServer\s+stomp=http://127\.0\.0\.1:61614/topic/C2SIM \*\*\*')
    Check '11b the loud line is printed in BOTH cases and tells the operator the pushes must match' (
        $wbDefault -match 'THE PUSHES MUST NAME THIS SAME PAIR' -and
        $wbPrivate -match 'THE PUSHES MUST NAME THIS SAME PAIR')
    $wbExplicit = (& $wbPwsh -NoProfile -NonInteractive -File $wbIface -WhatIf -Server private `
                        -RestUrl 'http://10.0.0.5:9999/C2SIMServer' 2>&1 | Out-String)
    Check '11b an explicit -RestUrl WINS over -Server, and the partial override is called out' (
        $wbExplicit -match 'C2SIM__RestUrl\s+= http://10\.0\.0\.5:9999/C2SIMServer' -and
        $wbExplicit -match 'C2SIM__StompUrl\s+= http://127\.0\.0\.1:61614/topic/C2SIM')
    $wbBadUrl = (& $wbPwsh -NoProfile -NonInteractive -File $wbIface -WhatIf -StompUrl 'not-a-url' 2>&1 | Out-String)
    Check '11b a -StompUrl that is not an absolute http/https URL is refused by name' (
        $wbBadUrl -match '\[FAIL\] -StompUrl is not an absolute http/https URL: not-a-url')
    # FOUND BY THE ADVERSARIAL PASS ON THIS BRANCH, not by a live run. With -Server standard the
    # script sets no override - so an override ALREADY IN THE CONSOLE (the runner sets and
    # restores these; a rehearsal script sets them for a child; an operator may export one) is
    # what the app would actually hear. Naming appsettings' 8080 there would be the DR-1 lie in
    # a new place. The child below is given one in its environment and must say so.
    $wbInheritEnv = @{ C2SIM__RestUrl = 'http://127.0.0.1:18080/C2SIMServer' }
    $wbInherit = (& {
        $saved = $env:C2SIM__RestUrl
        try {
            $env:C2SIM__RestUrl = $wbInheritEnv['C2SIM__RestUrl']
            & $wbPwsh -NoProfile -NonInteractive -File $wbIface -WhatIf 2>&1 | Out-String
        } finally {
            if ($null -eq $saved) { Remove-Item -Path 'Env:C2SIM__RestUrl' -ErrorAction SilentlyContinue }
            else { $env:C2SIM__RestUrl = $saved }
        }
    })
    Check '11b an INHERITED C2SIM__RestUrl is reported as the real source, not appsettings 8080' (
        $wbInherit -match 'rest=http://127\.0\.0\.1:18080/C2SIMServer' -and
        $wbInherit -match 'INHERITED from this console''s environment \(C2SIM__RestUrl\)' -and
        $wbInherit -match 'it WINS' -and
        $wbInherit -notmatch 'rest=http://127\.0\.0\.1:8080/C2SIMServer')
    Check '11b and the test put the console variable back' ($null -eq $env:C2SIM__RestUrl)
}
# The env must not leak into an operator's own console: every variable this script sets is
# restored in a finally, INCLUDING the two endpoint overrides, which would otherwise silently
# redirect the operator's next tool at the rehearsal server.
$wbAppCall = @($wbIfaceAst.FindAll({
    param($n)
    $n -is [System.Management.Automation.Language.CommandAst] -and
    $n.Extent.Text -match '^& \$Exe @AppArgs' }, $true))
Check '11b the app is started INSIDE a try whose finally restores the environment (AST)' (
    $wbAppCall.Count -eq 1 -and (Test-InsideTry $wbAppCall[0]) -and
    $wbIfaceText -match 'Pop-Location' -and
    $wbIfaceText -match 'Remove-Item -Path \(''Env:'' \+ \$k\)' -and
    $wbIfaceText -match '\$env:PATH = \$SavedPath') ('& $Exe @AppArgs sites: ' + $wbAppCall.Count)
Check '11b C2SIM__RestUrl / C2SIM__StompUrl are added to the SAME $envVars map that is saved and restored' (
    $wbIfaceText -match '\$envVars\[''C2SIM__RestUrl''\]\s+= \$EffRestUrl' -and
    $wbIfaceText -match '\$envVars\[''C2SIM__StompUrl''\] = \$EffStompUrl' -and
    $wbIfaceText -match 'foreach \(\$k in \$envVars\.Keys\) \{ \$SavedEnv\[\$k\] =')

# --- 11c. DR-2 / DR-8: the Way B application numbers are inside the documented demo block ---
Check '11c no TYPED command in DEMO_RUNBOOK still passes 9201 / 9202 (the note recording the change may name them)' (
    $wbDemoText -notmatch '-BackendAppNumber 9201' -and $wbDemoText -notmatch '-FrontendAppNumber 9202')
Check '11c the Way B launch command uses 9102 / 9103, inside the 9101-9199 demo block' (
    $wbDemoText -match '-BackendAppNumber 9102 -FrontendAppNumber 9103')
Check '11c and those two collide with neither the interface (9101) nor LaunchVrf52 holder (9190/9191)' (
    @(9102, 9103 | Where-Object { $_ -in @(9101, 9190, 9191) }).Count -eq 0 -and
    @(9102, 9103 | Where-Object { $_ -lt 9101 -or $_ -gt 9199 }).Count -eq 0)
Check '11c the runbook SAYS why the block covers the back end and front end, not only interfaces' (
    $wbDemoText -match 'federation holder already sits in\s+that block at 9190/9191' -and
    $wbDemoText -match 'the block is not reserved to interfaces' -and
    $wbDemoText -match 'sit outside the\s+block\s+entirely and were therefore exempt from nothing')
Check '11c LaunchVrf52 stops claiming the runbook example sits outside the block' (
    (Get-Content -LiteralPath (Join-Path $RepoRoot 'scripts\LaunchVrf52.ps1') -Raw) -match 'that example now uses\s*\r?\n?\s*# 9102/9103')

# --- 11d. DR-3: the licence text no longer reads as a stop-work ---
Check '11d DEMO_RUNBOOK does not claim the licence lapsed on 2026-09-15' (
    $wbDemoText -notmatch 'LAPSES 2026-09-15')
Check '11d it gives the renewed date and points at the licence line the scripts print' (
    $wbDemoText -match 'RENEWED on 2026-09-14 and now LAPSES 2026-10-31' -and
    $wbDemoText -match 'expires 31-oct-2026')

# --- 11e. DR-4: one clock statement, and it is the measured load-dependent one ---
Check '11e the withdrawn single-number clock readings are gone from DEMO_RUNBOOK' (
    $wbDemoText -notmatch '1\.5-2\.8x' -and
    $wbDemoText -notmatch 'sim clock still held real time' -and
    $wbDemoText -notmatch 'completed in real time \(ratio')
Check '11e what remains is RUNBOOK 11f: load-dependent, a range, and "do not quote a single number"' (
    $wbDemoText -match 'LOAD-DEPENDENT' -and $wbDemoText -match '2\.6x' -and $wbDemoText -match '4\.7x' -and
    $wbDemoText -match 'RUNBOOK sec 11f' -and $wbDemoText -match 'SIM/WALL RATIO')
Check '11e the D1 entry keeps its withdrawal beside the number it withdrew' (
    $wbDemoText -match 'is WITHDRAWN: it compared wall clock against wall')

# --- 11f. DR-5: the stop order names the real stop tool beside Ctrl+C ---
Check '11f the Way B clean stop names tools\StopIface with its endpoints and --yes' (
    $wbDemoText -match 'StopIface\\bin\\Release\\net10\.0\\StopIface\.exe' -and
    $wbDemoText -match 'RUNBOOK sec 4' -and $wbDemoText -match '--yes')
Check '11f Ctrl+C is still offered as the by-hand equivalent, not removed' (
    $wbDemoText -match 'Ctrl\+C in the interface''s own window does the same thing')

# --- 11g. DR-6: the persistent holder is a repo script an operator can actually run ---
$wbHolderPresent = Test-Path -LiteralPath $wbHolder -PathType Leaf
Check '11g scripts\StartFederationHolder52.ps1 exists' $wbHolderPresent
# A missing file must FAIL these checks, not abort the suite with a throw and lose the summary.
$wbHolderText = ''
if ($wbHolderPresent) {
    $wbHolderErr = $null
    $null = [System.Management.Automation.Language.Parser]::ParseFile($wbHolder, [ref]$null, [ref]$wbHolderErr)
    Check '11g it parses with zero errors' ($wbHolderErr.Count -eq 0) ("errors: " + $wbHolderErr.Count)
    $wbHolderText = Get-Content -LiteralPath $wbHolder -Raw
} else {
    Check '11g it parses with zero errors' $false 'the file does not exist'
}
Check '11g -AppNumbers is MANDATORY with no default (the ledger supplies them; the script invents none)' (
    $wbHolderText -match '\[Parameter\(Mandatory\)\]\[string\[\]\] \$AppNumbers,' -and
    $wbHolderText -notmatch '\$AppNumbers\s*=' -and
    $wbHolderText -match 'never invented')
Check '11g it has a -WhatIf that starts nothing, and it never kills anything' (
    $wbHolderText -match '\[switch\] \$WhatIf' -and
    $wbHolderText -match 'no application number was spent' -and
    $wbHolderText -notmatch 'Stop-Process' -and $wbHolderText -notmatch 'taskkill')
Check '11g DEMO_RUNBOOK points at the repo script and no longer at the seat scratchpad path' (
    $wbDemoText -match 'scripts\\StartFederationHolder52\.ps1' -and
    $wbDemoText -notmatch 'p7_holder_retry')
Check '11g RUNBOOK 9c THE DEMO POSTURE names the repo script' (
    $wbBookText -match 'THE DEMO POSTURE IS A PERSISTENT HOLDER[\s\S]{0,200}scripts\\StartFederationHolder52\.ps1')
if (-not (Test-Path -LiteralPath $wbPwsh) -or -not $wbHolderPresent) {
    Check '11g SKIPPED - pwsh 7 or the holder script is not present' $true
} else {
    # THE DEFECT THIS CATCHES, measured while writing the script: `pwsh -File ... -AppNumbers
    # 4651,4652` hands PowerShell ONE string, and an [int[]] parameter turned it into the single
    # bogus number 46514652 without a word. The strings are split and parsed instead.
    $wbHold = (& $wbPwsh -NoProfile -NonInteractive -File $wbHolder -AppNumbers 4651,4652 `
                    -SettleSecs 28800 -WhatIf 2>&1 | Out-String)
    Check '11g -AppNumbers 4651,4652 via -File is TWO attempts, not the concatenated number 46514652' (
        $wbHold -match 'would start[^\r\n]*RtiProbe\.exe" 4651 MAK-ONE-2025 1 28800 3' -and
        $wbHold -match 'would start[^\r\n]*RtiProbe\.exe" 4652 MAK-ONE-2025 1 28800 3' -and
        $wbHold -notmatch '46514652')
    Check '11g the -WhatIf plan says nothing was started and no number was spent' (
        $wbHold -match 'nothing was started, no application number was spent')
    $wbHoldBad = (& $wbPwsh -NoProfile -NonInteractive -File $wbHolder -AppNumbers 4651,4651 -WhatIf 2>&1 | Out-String)
    Check '11g a DUPLICATE application number is refused by name (a reused appNo is a stale-federate hang)' (
        $wbHoldBad -match '\[FAIL\] -AppNumbers contains a DUPLICATE')
}

# --- 11h. DR-7: ONE appData tree is named, and it is the one with the teardown fix ---
Check '11h the navigation section names the unattended tree as THE one, with the STP-844 reason' (
    $wbDemoText -match 'ONE TREE, AND IT IS `C:\\C2SIM\\vrf-appdata-unattended\\appData`' -and
    $wbDemoText -match 'TEARDOWN PROMPTS ARE PRE-DISABLED \(STP-844\)')
Check '11h the "How to pass it" line no longer hands the operator the old tree' (
    $wbDemoText -notmatch '--vrf-appdata-dir C:\\C2SIM\\vrf-appdata\\appData')

# --- 11i. section 4: the push endpoints are a MATCHED PAIR per server ---
Check '11i both server blocks are given, each with its own PushInit AND PushOrder' (
    $wbDemoText -match 'PRIVATE server - use with `StartInterface52\.ps1 -Server private`' -and
    $wbDemoText -match 'STANDARD server - use with `StartInterface52\.ps1 -Server standard`' -and
    @([regex]::Matches($wbDemoText, 'PushInit\.exe')).Count -ge 2 -and
    @([regex]::Matches($wbDemoText, 'PushOrder\.exe')).Count -ge 2)
Check '11i and the silent-failure mode is stated where the operator will read it' (
    $wbDemoText -match 'A MISMATCHED PAIR IS SILENT')
Check '11i Way B step 3 carries the server choice' (
    $wbDemoText -match 'StartInterface52\.ps1 -ClientId STP -Server standard')

# --- 11j. the honesty note tracks reality: D5c verified the sequence live, STP itself pushing stays owed ---
Check '11j section 2 names D5c as the run that verified the sequence live, and still names STP itself pushing as owed' (
    $wbDemoText -match [regex]::Escape('VERIFIED LIVE AS A SINGLE SEQUENCE (D5c') -and
    $wbDemoText -match 'STP ITSELF PUSHING')

# --- 11k. both touched scripts parse ---
foreach ($wbF in @('scripts\StartInterface52.ps1', 'scripts\StartFederationHolder52.ps1', 'scripts\LaunchVrf52.ps1')) {
    $wbPath = Join-Path $RepoRoot $wbF
    if (-not (Test-Path -LiteralPath $wbPath -PathType Leaf)) {
        Check ('11k parses with zero errors: ' + $wbF) $false 'the file does not exist'
        continue
    }
    $wbErr = $null
    $null = [System.Management.Automation.Language.Parser]::ParseFile($wbPath, [ref]$null, [ref]$wbErr)
    Check ('11k parses with zero errors: ' + $wbF) ($wbErr.Count -eq 0) ("errors: " + $wbErr.Count)
}

# === 12. every Say-* call in scripts\*.ps1 resolves to a defined function ===
# STP: LaunchVrf52.ps1:1158 called Say-Info from the standalone federation-holder path
# (commit d1885c0), but only Say / Say-Head / Say-Ok / Say-Warn / Say-Fail / Say-Plan were
# defined. The runner always passes -FederationHeldByCaller so that path never ran until
# the Way B rehearsal (D5), where LaunchVrf52 died 1s in with "The term 'Say-Info' is not
# recognized", exit 1, before any holder or VR-Forces process started. A static AST check
# catches this on every never-run branch, not just the one that finally executed.
Write-Host '=== 12. every Say-* call resolves to a defined function (LaunchVrf52 Say-Info, STP) ==='
$sayScripts = Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'scripts') -Filter '*.ps1' -File
$runnerLibPath12 = Join-Path $RepoRoot 'scripts\RunnerLib.ps1'
$runnerLibAst12 = [System.Management.Automation.Language.Parser]::ParseFile($runnerLibPath12, [ref]$null, [ref]$null)
$runnerLibFuncs12 = @($runnerLibAst12.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true) | ForEach-Object { $_.Name })

# 12b widens the same walk to ANY Verb-Noun call name: undefined-locally, not dot-sourced
# from RunnerLib.ps1, and not resolvable by Get-Command in this process (this test itself
# runs under pwsh -NoProfile -NonInteractive, so a hit here is a genuine unresolved name,
# not a profile-loaded convenience function). On this tree it finds exactly the Say-Info
# defect and nothing else - no noise - so it stays alongside the narrower 12 check.
$unresolvedSay12 = New-Object System.Collections.Generic.List[string]
$unresolvedAny12 = New-Object System.Collections.Generic.List[string]
foreach ($f12 in $sayScripts) {
    $text12 = Get-Content -LiteralPath $f12.FullName -Raw
    $ast12 = [System.Management.Automation.Language.Parser]::ParseFile($f12.FullName, [ref]$null, [ref]$null)
    $ownFuncs12 = @($ast12.FindAll({ param($a) $a -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true) | ForEach-Object { $_.Name })
    $dotSourced12 = @($ast12.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] -and $a.InvocationOperator -eq [System.Management.Automation.Language.TokenKind]::Dot }, $true))
    # Dot-sourcing resolution is intentionally narrow: only "does this file dot-source
    # RunnerLib.ps1 at all" (LaunchVrf52.ps1 does it inside a child scope with a $libPath
    # variable, not the plain '. (Join-Path $PSScriptRoot ...)' form, so this checks for
    # the dot-source OPERATOR plus the literal filename anywhere in the file rather than
    # tracing the argument expression).
    $dotSourcesRunnerLib12 = ($dotSourced12.Count -gt 0) -and ($text12 -match 'RunnerLib\.ps1')

    $allCommandNames12 = @($ast12.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] }, $true) | ForEach-Object {
            $n = $null
            try { $n = $_.GetCommandName() } catch { $n = $null }
            $n
        } | Where-Object { $_ } | Sort-Object -Unique)

    foreach ($callName12 in @($allCommandNames12 | Where-Object { $_ -match '^Say-[A-Za-z]+$' })) {
        $defined12 = ($ownFuncs12 -contains $callName12) -or ($dotSourcesRunnerLib12 -and ($runnerLibFuncs12 -contains $callName12))
        if (-not $defined12) { $unresolvedSay12.Add("$($f12.Name): $callName12") }
    }
    foreach ($callName12 in @($allCommandNames12 | Where-Object { $_ -match '^[A-Za-z][A-Za-z0-9]*-[A-Za-z][A-Za-z0-9]*$' })) {
        if ($ownFuncs12 -contains $callName12) { continue }
        if ($dotSourcesRunnerLib12 -and ($runnerLibFuncs12 -contains $callName12)) { continue }
        if (Get-Command $callName12 -ErrorAction SilentlyContinue) { continue }
        $unresolvedAny12.Add("$($f12.Name): $callName12")
    }
}
Check '12 no undefined Say-* calls in scripts\*.ps1' ($unresolvedSay12.Count -eq 0) ($unresolvedSay12 -join '; ')
Check '12b no unresolved Verb-Noun calls in scripts\*.ps1 (own file / dot-sourced RunnerLib / Get-Command)' ($unresolvedAny12.Count -eq 0) ($unresolvedAny12 -join '; ')

Write-Host ''
Write-Host ('{0} passed, {1} failed' -f $script:Pass, $script:Fail)
if ($script:Fail -gt 0) { exit 1 }
exit 0
