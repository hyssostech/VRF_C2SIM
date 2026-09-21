# scripts/RunnerLib.ps1 - PURE helpers for scripts/RunC2SimScenario.ps1.
#
# Dot-sourced by the runner AND by tests/RunnerTurnaround.Tests.ps1. Everything in
# here is side-effect free (no process start, no file write, no sleep) so the
# runner's turnaround logic - the observer duration cap, the -StopWhenComplete
# early-exit criterion (including the report-evidence condition), the trace
# stop-file timing, the tool capability probe parse and the CRLF ledger rewrite -
# can be exercised WITHOUT a simulator (docs/RUNNER_TURNAROUND_2026-09-01.md).
#
# ASCII only. Set-StrictMode -Version Latest compatible: every variable read here
# is assigned first.

Set-StrictMode -Version Latest

# ---- observer duration CAP --------------------------------------------------
# The WatchVrf / ListenReports duration argument. It is the UPPER BOUND the tools
# would run to if the runner never told them to stop: the SUM of every stage's
# worst-case budget between observer start and the end of the trail. With the
# stop-file mechanism (Get-TraceStopWaitSecs below) the observers normally end at
# StopIface + TrailSecs and this sum is only the safety net for a runner that dies
# mid-run. Unchanged formula from the pre-turnaround runner, by design.
function Get-DerivedWatchSecs {
    param(
        [Parameter(Mandatory)][int]$PreRollSecs,
        [Parameter(Mandatory)][int]$AppJoinTimeoutSec,
        [Parameter(Mandatory)][int]$InitDispatchWaitSec,
        [Parameter(Mandatory)][int]$OracleGateTimeoutSec,
        [Parameter(Mandatory)][int]$PushOrderListenSec,
        [Parameter(Mandatory)][int]$RunSecs,
        [Parameter(Mandatory)][int]$TrailSecs
    )
    return $PreRollSecs + $AppJoinTimeoutSec + $InitDispatchWaitSec +
           $OracleGateTimeoutSec + $PushOrderListenSec + $RunSecs + $TrailSecs
}

# ---- what the ORDER asks for ------------------------------------------------
# Every <Task> in a C2SIM order, as (TaskUuid, Taskee) pairs - one record per
# PerformingEntity of each Task. Namespace-agnostic (local-name()) because the
# order carries the SISO default namespace and the runner must not care which
# version. In SISO-STD-C2SIM the UUID and PerformingEntity sit INSIDE the task-type
# wrapper (<Task><ManeuverWarfareTask><UUID/>...<PerformingEntity/>), so both the
# Task's direct element children AND each child's element children are scanned -
# never deeper, so a UUID inside <Location> or a route can not be mistaken for the
# task's. Returns an EMPTY array on unparseable XML - the caller decides what that
# means (the runner WARNs and disables early exit; PushOrder is the authority on
# whether the order is acceptable, not this helper).
function Get-OrderTasks {
    param([string]$OrderText)
    $tasks = @()
    if ([string]::IsNullOrWhiteSpace($OrderText)) { return $tasks }
    $doc = New-Object System.Xml.XmlDocument
    try { $doc.LoadXml($OrderText) } catch { return $tasks }
    foreach ($t in @($doc.SelectNodes("//*[local-name()='Task']"))) {
        $uuid    = $null
        $taskees = @()
        $scan = @()
        foreach ($c in @($t.ChildNodes)) {
            if ($c.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
            $scan += $c
            foreach ($g in @($c.ChildNodes)) {
                if ($g.NodeType -eq [System.Xml.XmlNodeType]::Element) { $scan += $g }
            }
        }
        foreach ($c in $scan) {
            if ($c.LocalName -eq 'UUID' -and -not $uuid) { $uuid = $c.InnerText.Trim() }
            if ($c.LocalName -eq 'PerformingEntity') {
                $v = $c.InnerText.Trim()
                if (-not [string]::IsNullOrWhiteSpace($v)) { $taskees += $v }
            }
        }
        foreach ($taskee in $taskees) {
            $tasks += [pscustomobject]@{ TaskUuid = $uuid; Taskee = $taskee }
        }
    }
    return $tasks
}

function Get-OrderTaskees {
    param([string]$OrderText)
    return @(@(Get-OrderTasks -OrderText $OrderText) | ForEach-Object { $_.Taskee } | Select-Object -Unique)
}

# ---- what the INTERFACE reported --------------------------------------------
# The app logs exactly one line per task-status report it SENDS
# (src/VrfC2SimApp/VrfC2SimService.cs:4958):
#     SENT TASK STATUS REPORT (<CODE>) taskee=<uuid> task=<uuid|(none)> - <why>.
#
# THE " - <why>." SUFFIX IS THE D1 DEFECT (run 20260914T230706Z). Until 2026-09-14 the
# app logged the task uuid LAST ("... task=<uuid>.") and the regex here anchored that
# token to the END OF THE LINE. Commit 81d108c (B1: TASKSTRT at dispatch, TASKABRT for
# tasks that will never run) appended " - {Why}." to every one of those lines, and this
# parser then matched NOTHING: the V2 chain run pushed 41 TASKCMPLT and 2 TASKABRT lines
# for its 11 taskees and the runner still reported ALL ELEVEN as having no TASKCMPLT,
# with completionLinesSeen = 0 in its manifest. The task token is now delimited by a
# LOOKAHEAD (an optional period, then whitespace or end of text), so whatever the app
# appends to the line after it can never break the parse again.
#
# TERMINAL CODES, not TASKCMPLT alone. A task VR-Forces FAILS is reported TASKABRT, and
# so is a successor the interface skips because its predecessor was abandoned: both are
# legitimate END STATES of an order task. Counting only TASKCMPLT made the task-coverage
# test unmeetable for any order containing one (the V2 run: 42 tasks, at most 40 of which
# could ever be TASKCMPLT). TASKSTRT is NOT terminal and is deliberately not matched.
#
# Returns one (Taskee, Task, Code) record per report line, in log order, duplicates kept.
# task=(none) - the app's form for a report it could not attribute to a task uuid - is
# returned as-is; Update-CompletionState counts it as UNATTRIBUTED and it closes no task
# (the safe direction: the window then runs to its cap).
function Get-TerminalTaskReports {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    $out = @()
    if ([string]::IsNullOrWhiteSpace($AppLogText)) { return $out }
    # No line split and no '$' line anchor: [regex]::Matches walks the text once (the app
    # log reached 40 MB in 127 s in the G6 run) and the task token ends where the LOOKAHEAD
    # says it does, not where the line happens to end.
    $rx = [regex]'SENT TASK STATUS REPORT \((?<code>TASKCMPLT|TASKABRT)\) taskee=(?<taskee>[0-9A-Fa-f-]{36}) task=(?<task>\S+?)(?=\.?(?:\s|$))'
    foreach ($m in $rx.Matches($AppLogText)) {
        $out += [pscustomobject]@{
            Taskee = $m.Groups['taskee'].Value
            Task   = $m.Groups['task'].Value
            Code   = $m.Groups['code'].Value
        }
    }
    return $out
}

# The pre-2026-09-14 name. Kept because tests\RunnerTurnaround.Tests.ps1 section 3 calls
# it with the log fixtures of the record, and because "the tasks that completed" is what
# a reader of the early exit expects to find. Same records, TASKABRT included.
function Get-CompletedTasks {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    return @(Get-TerminalTaskReports -AppLogText $AppLogText)
}

# ---- early-exit state machine ------------------------------------------------
# State is a hashtable the caller owns across polls:
#   firstSeenUtc    : ordered map taskee -> UTC of the poll that FIRST saw a terminal
#                     report for it
#   firstSeenCode   : ordered map taskee -> the Code (TASKCMPLT/TASKABRT) of that SAME
#                     first terminal report. Added 2026-09-20 (V6i cosmetic defect):
#                     Test-ReportEvidence used to print "TASKCMPLT" unconditionally for
#                     this anchor even when the terminal report was TASKABRT - the
#                     tracking here was always code-agnostic, only the printed label
#                     assumed TASKCMPLT. Defaults to 'TASKCMPLT' when the record has no
#                     Code (pre-2026-09-14 callers), same convention as codeCounts below.
#   lineCount       : RUNNING TOTAL of terminal report lines for order taskees across all
#                     polls. It was "lines seen at the latest poll" until 2026-09-14,
#                     when the runner's observation loop stopped re-reading the whole
#                     app log every 5 s and started feeding this function only the text
#                     APPENDED since the previous poll (Read-LiveDelta,
#                     docs/experiments/RUNNER_HARDENING_2026-09-14.md sec 5). Under a
#                     whole-file reader the two definitions coincide, so the change is
#                     invisible to a caller that still passes the whole log; under a
#                     delta reader only the running total is correct. It is a DIAGNOSTIC
#                     now, not the criterion - see terminalByTask.
#   terminalByTask  : ordered map '<taskee>|<task>' -> the FIRST terminal code seen for
#                     that pair. THE criterion for task coverage, and idempotent: a
#                     duplicate line, or a poll that re-reads the same text, can not
#                     inflate it the way lineCount could.
#   codeCounts      : ordered map code -> terminal LINES seen for order taskees
#   unattributed    : terminal lines for order taskees with task=(none)
#   taskCount       : the order's (Task, PerformingEntity) record count, as last given
#   orderTaskKeys   : the order's own '<taskee>|<task>' keys when the caller supplies
#                     -OrderTasks; empty otherwise (the count-only fallback)
#   allCompleteUtc  : UTC of the poll that first satisfied the ALL-COMPLETE condition
# The app log carries NO timestamps, so "when did the last completion happen" is
# necessarily "the runner's poll that first saw it" - late by at most one poll
# interval, which only ever LENGTHENS the settle hold. Never shortens it.
function New-CompletionState {
    return @{
        firstSeenUtc   = [ordered]@{}
        firstSeenCode  = [ordered]@{}
        lineCount      = 0
        terminalByTask = [ordered]@{}
        codeCounts     = [ordered]@{}
        unattributed   = 0
        taskCount      = 0
        orderTaskKeys  = @()
        allCompleteUtc = $null
    }
}

# The UNIT of the task-coverage test is the (taskee, task) PAIR, which is exactly what
# Get-OrderTasks returns - one record per PerformingEntity of each <Task>. A task with two
# performers is two records and the interface reports it once per taskee, so the pair is
# the only key on which "one terminal report per order task" is true on both sides.
function Get-TaskKey {
    param([AllowNull()][AllowEmptyString()][string]$Taskee, [AllowNull()][AllowEmptyString()][string]$Task)
    return ('{0}|{1}' -f $Taskee, $Task)
}

# TASK COVERAGE, read off a completion state. Closed = order tasks with a terminal report.
# With -OrderTasks supplied (the runner always does) the closed set is the INTERSECTION
# with the order's own keys, so a terminal report for a task nobody ordered can not close
# anything; without it (the unit fixtures) the fallback is the number of DISTINCT pairs
# seen, which is what the old lineCount was reaching for.
# Text is the line the runner prints: "40 TASKCMPLT + 2 TASKABRT = 42 terminal of 42 tasks".
function Get-TaskCoverage {
    param([Parameter(Mandatory)][hashtable]$State)
    $taskCount = 0; if ($State.Contains('taskCount'))    { $taskCount = [int]$State['taskCount'] }
    $unatt     = 0; if ($State.Contains('unattributed')) { $unatt     = [int]$State['unattributed'] }
    $keys      = @(); if ($State.Contains('orderTaskKeys')) { $keys = @($State['orderTaskKeys']) }
    $term = [ordered]@{}
    if ($State.Contains('terminalByTask') -and $null -ne $State['terminalByTask']) { $term = $State['terminalByTask'] }
    $byCode     = [ordered]@{}
    $open       = @()
    $closedKeys = @()
    if ($keys.Count -gt 0) {
        foreach ($k in $keys) { if ($term.Contains($k)) { $closedKeys += $k } else { $open += $k } }
    } else {
        $closedKeys = @($term.Keys)
    }
    foreach ($k in $closedKeys) {
        $c = [string]$term[$k]
        if ($byCode.Contains($c)) { $byCode[$c] = [int]$byCode[$c] + 1 } else { $byCode[$c] = 1 }
    }
    # TASKCMPLT first, TASKABRT second, anything the app grows later after them.
    $known = @('TASKCMPLT', 'TASKABRT')
    $parts = @()
    foreach ($c in $known)          { if ($byCode.Contains($c))    { $parts += ('{0} {1}' -f $byCode[$c], $c) } }
    foreach ($c in @($byCode.Keys)) { if ($known -notcontains $c) { $parts += ('{0} {1}' -f $byCode[$c], $c) } }
    $text = ('0 terminal of {0} tasks' -f $taskCount)
    if ($parts.Count -gt 0) { $text = ('{0} = {1} terminal of {2} tasks' -f ($parts -join ' + '), $closedKeys.Count, $taskCount) }
    if ($unatt -gt 0) { $text += ('; {0} unattributed terminal line(s) (task=(none)) closing no task' -f $unatt) }
    return [pscustomobject]@{
        Closed       = $closedKeys.Count
        TaskCount    = $taskCount
        Open         = $open
        ByCode       = $byCode
        Unattributed = $unatt
        Text         = $text
    }
}

# ALL-COMPLETE = (1) every distinct taskee in the order has at least one TERMINAL report
#                (TASKCMPLT or TASKABRT) AND (2) every order task has one, counted per
#                (taskee, task) PAIR.
# (2) REPLACED "terminal lines >= order task count" on 2026-09-14 (D2, run
# 20260914T230706Z). The line count could never reach the task count once ANY task ended
# in TASKABRT under the old TASKCMPLT-only parse, and even with TASKABRT counted a line
# total is the wrong instrument: a duplicate line, or one taskee reporting twice, satisfies
# it on behalf of a task that never ended. Per-pair coverage says exactly what the
# criterion means and is idempotent under the delta reader.
# Lines whose taskee is NOT in the order (a unit tasked by someone else on the same
# server, or a stale report) are ignored entirely: they are neither stamped nor
# counted, so a stray line can never satisfy the count on behalf of an order task
# (review F2, docs/experiments/REVIEW_RUNNER_TURNAROUND_2026-09-01.md).
# NOTE (review F3): two tasks dispatched SIMULTANEOUSLY to one taskee are SUPERSEDED
# by VR-Forces (VrfC2SimService.cs:954). The superseded one now reports TASKABRT
# (Vrf__SupersededTaskCode), which is terminal, so such an order CAN close - it could
# not before. A task that ends with NO report of either kind still holds the window to
# its cap, which is the safe direction.
function Update-CompletionState {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Taskees,
        [Parameter(Mandatory)][int]$TaskCount,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Completions,
        [Parameter(Mandatory)][datetime]$NowUtc,
        [AllowEmptyCollection()][object[]]$OrderTasks = @()   # Get-OrderTasks records
    )
    $State.taskCount = $TaskCount
    if ($OrderTasks.Count -gt 0) {
        $State.orderTaskKeys = @($OrderTasks | ForEach-Object { Get-TaskKey -Taskee ([string]$_.Taskee) -Task ([string]$_.TaskUuid) })
    }
    $inOrder   = @($Completions | Where-Object { $Taskees -contains $_.Taskee })
    $completed = @($inOrder | ForEach-Object { $_.Taskee } | Select-Object -Unique)
    # First record IN LOG ORDER for each taskee this poll, so a newly-first-seen taskee's
    # firstSeenCode names the SAME report that set its firstSeenUtc (not some later one,
    # if this poll's delta happens to carry more than one terminal line for it).
    $firstRecByTaskee = @{}
    foreach ($c in $inOrder) {
        if (-not $firstRecByTaskee.ContainsKey($c.Taskee)) { $firstRecByTaskee[$c.Taskee] = $c }
    }
    foreach ($u in $completed) {
        if (-not $State.firstSeenUtc.Contains($u)) {
            $State.firstSeenUtc[$u] = $NowUtc
            $code0 = 'TASKCMPLT'
            $c0 = $firstRecByTaskee[$u]
            if ($null -ne $c0 -and $null -ne $c0.PSObject.Properties['Code'] -and -not [string]::IsNullOrWhiteSpace([string]$c0.Code)) { $code0 = [string]$c0.Code }
            $State.firstSeenCode[$u] = $code0
        }
    }
    $State.lineCount += $inOrder.Count
    foreach ($c in $inOrder) {
        # A record without a Code is a TASKCMPLT: that is what every caller before
        # 2026-09-14 produced, and it keeps the unit fixtures of the record meaningful.
        $code = 'TASKCMPLT'
        if ($null -ne $c.PSObject.Properties['Code'] -and -not [string]::IsNullOrWhiteSpace([string]$c.Code)) { $code = [string]$c.Code }
        if ($State.codeCounts.Contains($code)) { $State.codeCounts[$code] = [int]$State.codeCounts[$code] + 1 } else { $State.codeCounts[$code] = 1 }
        $task = ''
        if ($null -ne $c.PSObject.Properties['Task']) { $task = [string]$c.Task }
        if ([string]::IsNullOrWhiteSpace($task) -or $task -eq '(none)') { $State.unattributed = [int]$State.unattributed + 1; continue }
        $k = Get-TaskKey -Taskee ([string]$c.Taskee) -Task $task
        if (-not $State.terminalByTask.Contains($k)) { $State.terminalByTask[$k] = $code }
    }
    # ALL-COMPLETE is asked of the ACCUMULATED state (firstSeenUtc, terminalByTask), never
    # of this call's input. With a whole-file reader the two were the same; with the delta
    # reader added 2026-09-14 they are not, and using the input would make all-complete
    # demand that every taskee re-report inside a single poll - it would essentially never
    # fire. Equivalent on whole-file input.
    $all = ($Taskees.Count -gt 0)
    foreach ($u in $Taskees) { if (-not $State.firstSeenUtc.Contains($u)) { $all = $false } }
    if ($all) {
        $cov = Get-TaskCoverage -State $State
        if ($cov.Closed -lt $cov.TaskCount) { $all = $false }
    }
    if ($all -and $null -eq $State.allCompleteUtc) { $State.allCompleteUtc = $NowUtc }
    return $State
}

# The DECISION. ShouldClose is true only when (1-3) ALL-COMPLETE has held for at
# least SettleHoldSecs AND (4) the report EVIDENCE is in (Test-ReportEvidence
# below - a post-completion text report for every taskee that agrees with its
# POS). The hold is the FLOOR so the movement gate (static -> moving -> settled,
# HEADLESS_RUN_PLAN 4a.1 "settled" = <10 m over 3 samples) still gets a
# post-completion plateau; the evidence is what guarantees the POS/RPT half.
# TrailSecs is added on top by the teardown. Zero taskees => never closes (the
# window then runs to its RunSecs cap). ReportEvidence is MANDATORY so a caller
# can not forget condition (4).
#
# FailedCondition NAMES THE FIRST UNMET CONDITION, WITH ITS NUMBERS, and is $null when
# ShouldClose. Before 2026-09-14 the runner's did-not-fire line could only offer "the
# hold had not elapsed, or the line count was below the task count" - a guess printed as
# a fact, at the one moment a reader needs to know which.
function Test-EarlyExit {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Taskees,
        [Parameter(Mandatory)][int]$SettleHoldSecs,
        [Parameter(Mandatory)][datetime]$NowUtc,
        [Parameter(Mandatory)][bool]$ReportEvidence
    )
    $missing = @($Taskees | Where-Object { -not $State.firstSeenUtc.Contains($_) })
    $all  = ($Taskees.Count -gt 0 -and $null -ne $State.allCompleteUtc)
    $held = 0.0
    if ($all) { $held = ($NowUtc - [datetime]$State.allCompleteUtc).TotalSeconds }
    $cov  = Get-TaskCoverage -State $State
    $openTxt = '(none)'
    if ($cov.Open.Count -gt 0) {
        $shown = @($cov.Open | Select-Object -First 5 | ForEach-Object { ($_ -split '\|')[-1] })
        $openTxt = ($shown -join ', ')
        if ($cov.Open.Count -gt $shown.Count) { $openTxt += (' (+{0} more)' -f ($cov.Open.Count - $shown.Count)) }
    }
    $failed = $null
    if ($Taskees.Count -eq 0) {
        $failed = 'condition (1) taskee coverage: the order yields ZERO taskees, so the early exit can never fire'
    } elseif ($missing.Count -gt 0) {
        $failed = ('condition (1) taskee coverage: {0} of {1} order taskee(s) have NO terminal report (TASKCMPLT/TASKABRT): {2}' -f `
                   $missing.Count, $Taskees.Count, ($missing -join ', '))
    } elseif ($cov.Closed -lt $cov.TaskCount) {
        $failed = ('condition (2) task coverage: {0} of {1} order task(s) have a terminal report [{2}]; still open: {3}' -f `
                   $cov.Closed, $cov.TaskCount, $cov.Text, $openTxt)
    } elseif (-not $all) {
        $failed = 'conditions (1-2): ALL-COMPLETE has not been recorded by any poll yet'
    } elseif ($held -lt $SettleHoldSecs) {
        $failed = ('condition (3) settle hold: {0}s of {1}s elapsed since ALL-COMPLETE' -f [Math]::Round($held, 1), $SettleHoldSecs)
    } elseif (-not $ReportEvidence) {
        $failed = ('condition (4) position evidence: no post-completion position report yet for every one of the {0} taskee(s)' -f $Taskees.Count)
    }
    return [pscustomobject]@{
        AllComplete     = $all
        Missing         = $missing
        MissingTasks    = $cov.Open
        TasksClosed     = $cov.Closed
        TaskCount       = $cov.TaskCount
        TerminalByCode  = $cov.ByCode
        TerminalSummary = $cov.Text
        Unattributed    = $cov.Unattributed
        HoldElapsedSecs = [Math]::Round($held, 1)
        HoldElapsed     = ($all -and $held -ge $SettleHoldSecs)
        EvidenceIn      = $ReportEvidence
        FailedCondition = $failed
        ShouldClose     = ($all -and $held -ge $SettleHoldSecs -and $ReportEvidence)
    }
}

# ---- report EVIDENCE for the settle hold (ruling 2026-09-02) -------------------
# Why: run 20260901T235823Z closed the window SettleHoldSecs (60 s) after the last
# TASKCMPLT and StopIface landed in the MIDDLE of a VR-Forces text-report round
# (the rounds are ~60 s apart and take ~10 s to emit ~44 POSITION lines; the round
# in that run began at trace t=267.4 and had 20 lines out when StopIface fired at
# t=278.0). The company's LAST report therefore predated its own completion and the
# movement gate's POS==RPT check failed by 11.8 m while POS itself sat on the P2c
# endpoint (docs/experiments/PREREG_RUNNER_CONFIRM_2026-09-01.md sec 6).
#
# The hold is now EVIDENCE-BASED. Condition (4) of the early exit: for EVERY order
# taskee there is a captured text report that (a) is LATER than the taskee's task
# completion and (b) AGREES with the taskee's latest sampled position within
# ReportToleranceMeters. (b) is what makes the evidence real: in that run a report
# WAS emitted 1.5 s after the company's completion (t=213.3 vs TSK 211.8), but the
# aggregate centre was still converging and that report is the one that missed by
# 11.8 m - "later than completion" alone would have closed the window on it. (b) is
# exactly the predicate the movement gate adjudicates (HEADLESS_RUN_PLAN 4a, POS/RPT
# agreement), so the window can only close once the evidence the gate needs exists.
# SettleHoldSecs stays as a FLOOR: both must hold. Worst case close is therefore
# ~completion + one report round (60 s) + the round's emission spread (~10 s).
#
# SOURCE: watchvrf-trace.csv, which the runner already reads live (Read-LiveText) and
# which the adjudication reads for RPT. It carries all three records on ONE clock:
#   TSK,<t>,"<marking>","<taskType>"          VrfBridge task-complete event - the SAME
#                                             event that makes the app log TASKCMPLT
#                                             (VrfC2SimService.cs:1151 then :1244)
#   RPT,<t>,"POSITION ""<marking>"" <lat> <lon>"   the Lua tracker's text report
#   POS,<t>,<VRF_UUID>,<lat>,<lon>,<alt>        the sampled HLA position
# reports-captured.log stamps report ARRIVAL in UTC, not trace time, and the app log has
# no timestamps at all, so neither can be compared with a trace-clock RPT. The TSK record
# is the completion on the trace clock; it is used instead of the UTC stamp.
#
# KEYS: TSK and RPT are keyed by VR-Forces MARKING (the init's <Name>), POS by
# VRF_UUID. The taskee (C2SIM UUID) maps to its marking through the init
# (Get-InitUnitNames) and the marking to its VRF_UUID through the app log's route
# lines (Get-VrfUuidByName: "Task '<t>': CreateRoute '<r>' ... for <name>" joined
# with "Route '<r>' [(VRF_UUID:<route>)] created; MoveAlongRoute|PatrolRoute issued
# for VRF_UUID:<u>", VrfC2SimService.cs:1102/1123). The route-uuid parenthetical is
# OPTIONAL in the regex: the app started logging it on 2026-09-02 with the route-uuid
# fix, and every run in the record before that logs the line without it, so both
# forms must parse (run 20260902T153837Z hit this - the gate reported "marking ->
# VRF_UUID unknown (no route line in the app log)" for all three taskees against a
# perfectly healthy 3/3 app log, and the window ran to its cap). A taskee that can
# not be mapped (a task type that
# logs no route line, a missing TSK, a unit without a Name) is NOT satisfied, so
# the window runs to its RunSecs cap - the safe direction, and the reason is
# recorded per taskee.

function Get-InitUnitNames {
    param([string]$InitText)
    $map = [ordered]@{}
    if ([string]::IsNullOrWhiteSpace($InitText)) { return $map }
    $doc = New-Object System.Xml.XmlDocument
    try { $doc.LoadXml($InitText) } catch { return $map }
    foreach ($u in @($doc.SelectNodes("//*[local-name()='Unit']"))) {
        $uuid = $null; $name = $null
        foreach ($c in @($u.ChildNodes)) {
            if ($c.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
            if ($c.LocalName -eq 'UUID' -and -not $uuid) { $uuid = $c.InnerText.Trim() }
            if ($c.LocalName -eq 'Name' -and -not $name) { $name = $c.InnerText.Trim() }
        }
        if ($uuid -and $name -and -not $map.Contains($uuid)) { $map[$uuid] = $name }
    }
    return $map
}

function Get-VrfUuidByName {
    param([string]$AppLogText)
    $map = [ordered]@{}
    if ([string]::IsNullOrWhiteSpace($AppLogText)) { return $map }
    $routeToName = @{}
    $rxA = [regex]"Task '(?<task>[^']*)': CreateRoute '(?<route>[^']*)' \(\d+ pts\) for (?<name>.+?); "
    # The " (VRF_UUID:<route>)" parenthetical is OPTIONAL - new app builds log it, every
    # run in the record before 2026-09-02 does not. Both must parse.
    $rxB = [regex]"Route '(?<route>[^']*)'(?: \(VRF_UUID:[0-9a-fA-F-]{36}\))? created; (?:MoveAlongRoute|PatrolRoute) issued for (?<vrf>VRF_UUID:[0-9a-fA-F-]{36})"
    foreach ($line in ($AppLogText -split "`r?`n")) {
        $a = $rxA.Match($line)
        if ($a.Success) { $routeToName[$a.Groups['route'].Value] = $a.Groups['name'].Value; continue }
        $b = $rxB.Match($line)
        if ($b.Success) {
            $r = $b.Groups['route'].Value
            if ($routeToName.ContainsKey($r)) {
                $n = $routeToName[$r]
                if (-not $map.Contains($n)) { $map[$n] = $b.Groups['vrf'].Value }
            }
        }
    }
    return $map
}

# One pass over the trace: first TSK per marking, LAST RPT POSITION per marking,
# LAST real POS per VRF_UUID. Lines that do not parse are skipped, never fatal.
function Get-TraceEvidence {
    param([AllowNull()][AllowEmptyString()][string]$TraceText)
    $ev = @{ tsk = @{}; rpt = @{}; pos = @{} }
    if ([string]::IsNullOrWhiteSpace($TraceText)) { return $ev }
    $rxTsk = [regex]'^TSK,(?<t>[0-9.]+),"(?<name>(?:[^"]|"")*)",'
    $rxRpt = [regex]'^RPT,(?<t>[0-9.]+),"POSITION ""(?<name>(?:[^"]|"")*?)"" (?<lat>-?[0-9.]+) (?<lon>-?[0-9.]+)"'
    $rxPos = [regex]'^POS,(?<t>[0-9.]+),(?<uuid>VRF_UUID:[0-9a-fA-F-]{36}),(?<lat>-?[0-9.]+),(?<lon>-?[0-9.]+),(?<alt>-?[0-9.eE+]+)'
    $inv = [System.Globalization.CultureInfo]::InvariantCulture
    foreach ($line in ($TraceText -split "`r?`n")) {
        if ($line.Length -lt 5) { continue }
        switch ($line.Substring(0, 4)) {
            'TSK,' {
                $m = $rxTsk.Match($line)
                if ($m.Success) {
                    $n = $m.Groups['name'].Value -replace '""', '"'
                    if (-not $ev.tsk.ContainsKey($n)) { $ev.tsk[$n] = [double]::Parse($m.Groups['t'].Value, $inv) }
                }
            }
            'RPT,' {
                $m = $rxRpt.Match($line)
                if ($m.Success) {
                    $n = $m.Groups['name'].Value -replace '""', '"'
                    $ev.rpt[$n] = [pscustomobject]@{
                        T = [double]::Parse($m.Groups['t'].Value, $inv)
                        Lat = [double]::Parse($m.Groups['lat'].Value, $inv)
                        Lon = [double]::Parse($m.Groups['lon'].Value, $inv) }
                }
            }
            'POS,' {
                $m = $rxPos.Match($line)
                if ($m.Success) {
                    # Same degeneracy filter as the runner's Get-RealPositions: the pole
                    # placeholder, out-of-range longitude and the "altitude is memory"
                    # sample (alt 1e68 seen at t=228.9 in run 20260901T235823Z) are NOT
                    # positions and must not become the "latest POS".
                    $lat = 0.0; $lon = 0.0; $alt = 0.0
                    $fl = [System.Globalization.NumberStyles]::Float
                    if (-not [double]::TryParse($m.Groups['lat'].Value, $fl, $inv, [ref]$lat)) { continue }
                    if (-not [double]::TryParse($m.Groups['lon'].Value, $fl, $inv, [ref]$lon)) { continue }
                    if (-not [double]::TryParse($m.Groups['alt'].Value, $fl, $inv, [ref]$alt)) { continue }
                    if ([double]::IsNaN($lat) -or [double]::IsNaN($lon) -or [double]::IsNaN($alt)) { continue }
                    if ([Math]::Abs($lat) -ge 89.999999 -or [Math]::Abs($lon) -gt 180.0 -or [Math]::Abs($alt) -gt 100000.0) { continue }
                    $ev.pos[$m.Groups['uuid'].Value] = [pscustomobject]@{
                        T = [double]::Parse($m.Groups['t'].Value, $inv); Lat = $lat; Lon = $lon }
                }
            }
        }
    }
    return $ev
}

# Great-circle distance, metres (haversine, R = 6371 km). Good to <0.1 m at the
# scales that matter here (0-250 m).
function Get-DistanceMeters {
    param([double]$Lat1, [double]$Lon1, [double]$Lat2, [double]$Lon2)
    $r = 6371000.0
    $p1 = $Lat1 * [Math]::PI / 180.0; $p2 = $Lat2 * [Math]::PI / 180.0
    $dp = $p2 - $p1; $dl = ($Lon2 - $Lon1) * [Math]::PI / 180.0
    $a = [Math]::Sin($dp / 2) * [Math]::Sin($dp / 2) + [Math]::Cos($p1) * [Math]::Cos($p2) * [Math]::Sin($dl / 2) * [Math]::Sin($dl / 2)
    return 2.0 * $r * [Math]::Asin([Math]::Sqrt([Math]::Min(1.0, $a)))
}

# Init <Name> -> the marking the trace/app log actually carry. Exact match wins; else the
# UNIQUE key of the form '<name>~<tag>' (the app's proxy tag); else the name unchanged, so
# the caller's 'not found' reasons still fire. Ambiguity (two tagged variants) is left
# unresolved on purpose - the safe direction is the window running to its cap.
function Resolve-MarkingKey {
    param([Parameter(Mandatory)][string]$Name, [AllowEmptyCollection()][string[]]$Keys)
    if ($Keys -contains $Name) { return $Name }
    $tagged = @($Keys | Where-Object { $_ -like ($Name + '~*') } | Select-Object -Unique)
    if ($tagged.Count -eq 1) { return $tagged[0] }
    return $Name
}

# ---- condition (4) SOURCES, 2026-09-14 ---------------------------------------
# THE DEFECT THIS FIXES. Until today condition (4) accepted ONE kind of evidence: an
# RPT record. An RPT row is a VR-FORCES RADIO TEXT REPORT (tools/WatchVrf/ConFormat.cs
# :83-96) - the Lua tracker's "POSITION <marking> <lat> <lon>" broadcast. Nothing in
# this interface asks for one and the scenarios in the record do not run that tracker,
# so RPT=0 in EVERY trace of 2026-09-14 (8 runs) and -StopWhenComplete has NEVER fired:
# every run burned its whole -RunSecs cap while its taskees sat still. The report
# channel the interface DOES drive is the C2SIM PositionReport (R1,
# Vrf:PositionReportSeconds; VrfC2SimService.MaybeSendPositionReports) - 147 position
# ticks for the single taskee of run 20260914T154243Z against 0 RPT rows - and it is
# also the channel the adjudication reads. Condition (4) now accepts it.
# docs/experiments/RUNNER_HARDENING_2026-09-14.md sec 14.

# One pass over ListenReports' reports-captured.log.
#   pos   : C2SIM uuid -> the LAST PositionReport capture time (UTC [datetime])
#   posN  : C2SIM uuid -> how many PositionReports the capture holds for it
#   cmplt : C2SIM uuid -> the FIRST TASKCMPLT TaskStatus capture time (UTC [datetime])
#
# FORMAT (tools/ListenReports/Program.cs; runs/20260914T154243Z_run/reports-captured.log):
# records separated by a blank line, each headed
#     [HH:mm:ss.fff] REPORT #<n> (<len> chars)
# and followed by the wire-format <ReportBody>. The header stamp is DateTime.UtcNow at
# the moment the report arrived - UTC, and WITH NO DATE, which is the only reason
# -RunStartUtc is needed. A position record carries <PositionReportContent> with the
# subject in <SubjectEntity>; a completion record carries
# <TaskStatusCode>TASKCMPLT</TaskStatusCode> with the taskee in <ReportingEntity>. Both
# therefore sit on ONE wall clock, so "a position fix later than this taskee's own
# completion" is answerable without leaving the file. Unparseable lines are skipped.
#
# READ LIVE. ListenReports appends each record and FLUSHES it as it arrives (2026-09-14;
# it advertises 'incremental-capture'), so this parses a file another process still holds
# open - which is why the caller opens it with FileShare.ReadWrite (RunC2SimScenario
# Read-LiveText) and why a poll can catch the LAST record half-written. That is safe in
# ONE DIRECTION ONLY, and it is the right one: a record's HEADER is written before its
# body, so a torn tail can only DROP evidence, never invent it. A truncated header matches
# nothing and the lines that would have followed it do not exist yet; a complete header
# with a half-written body yields no <SubjectEntity> / <ReportingEntity> match and so
# contributes nothing. The cost is one poll - the whole file is re-read on the next one.
function Get-ReportCaptureEvidence {
    param(
        [AllowNull()][AllowEmptyString()][string]$CaptureText,
        [Parameter(Mandatory)][datetime]$RunStartUtc
    )
    $ev = @{ pos = @{}; posN = @{}; cmplt = @{} }
    if ([string]::IsNullOrWhiteSpace($CaptureText)) { return $ev }
    $rxHead = [regex]'^\[(?<h>\d{2}):(?<mi>\d{2}):(?<s>\d{2})\.(?<f>\d{3})\] REPORT #\d+'
    $rxSubj = [regex]'<SubjectEntity>\s*(?<u>[0-9a-fA-F-]{36})\s*</SubjectEntity>'
    $rxFrom = [regex]'<ReportingEntity>\s*(?<u>[0-9a-fA-F-]{36})\s*</ReportingEntity>'
    $start  = $RunStartUtc.ToUniversalTime()
    $day    = $start.Date
    $t = $null; $prev = $null; $isPos = $false; $isCmplt = $false
    foreach ($line in ($CaptureText -split "`r?`n")) {
        $h = $rxHead.Match($line)
        if ($h.Success) {
            $isPos = $false; $isCmplt = $false
            $cand = $day.AddHours([int]$h.Groups['h'].Value).AddMinutes([int]$h.Groups['mi'].Value).AddSeconds([int]$h.Groups['s'].Value).AddMilliseconds([int]$h.Groups['f'].Value)
            # The stamp has no date. Roll the day forward ONLY when the clock went
            # backwards by more than half a day - i.e. a run that crossed midnight UTC.
            # A plain "earlier than the previous record" test would roll on any
            # out-of-order millisecond and put the rest of the capture a day ahead.
            $ref = if ($null -eq $prev) { $start } else { $prev }
            if ($cand -lt $ref.AddHours(-12)) { $cand = $cand.AddDays(1); $day = $day.AddDays(1) }
            $t = $cand; $prev = $cand
            continue
        }
        if ($null -eq $t) { continue }
        if ($line.Contains('<PositionReportContent>')) { $isPos = $true; continue }
        if ($line.Contains('TASKCMPLT')) { $isCmplt = $true; continue }
        if ($isPos) {
            $m = $rxSubj.Match($line)
            if ($m.Success) {
                $u = $m.Groups['u'].Value
                if ((-not $ev.pos.ContainsKey($u)) -or ($t -gt [datetime]$ev.pos[$u])) { $ev.pos[$u] = $t }
                if ($ev.posN.ContainsKey($u)) { $ev.posN[$u] = [int]$ev.posN[$u] + 1 } else { $ev.posN[$u] = 1 }
            }
            continue
        }
        if ($isCmplt) {
            $m = $rxFrom.Match($line)
            if ($m.Success) {
                $u = $m.Groups['u'].Value
                if ((-not $ev.cmplt.ContainsKey($u)) -or ($t -lt [datetime]$ev.cmplt[$u])) { $ev.cmplt[$u] = $t }
            }
        }
    }
    return $ev
}

# The LIVE stand-in for the capture above, and the only per-taskee post-completion
# position evidence a RUNNING runner can see.
#
# WHY IT HAS TO EXIST: ListenReports writes reports-captured.log ONCE, AT ITS EXIT
# (tools/ListenReports/Program.cs, the closing File.WriteAllTextAsync). During the
# observation window that file does not exist yet, so Get-ReportCaptureEvidence can not
# answer live - it is the AUTHORITY (the offline harness, the adjudication, and any run
# whose capture is already on disk), not the live source.
#
# WHAT THIS READS: the interface logs ONE summary line per R1 round -
#     R1 position reports: <sent> sent, <skipped> skipped (no reflected object yet), ...
# and the app log is a SINGLE TOTALLY ORDERED FILE that also carries
#     SENT TASK STATUS REPORT (TASKCMPLT) taskee=<uuid> task=<uuid>
# so "a round appears BELOW this taskee's LAST TASKCMPLT line" is a post-completion
# position report for that taskee, decided on line order alone - which is what makes it
# usable at all, because the app log carries NO timestamps.
#
# "0 skipped" IS LOAD-BEARING: a round WITH skips does not say WHICH units were sent, so
# it can not be attributed to this taskee. An unsatisfied taskee runs the window to its
# -RunSecs cap - the safe direction - so the conservative test is the right one.
#
# AND SO IS "sides=both". MaybeSendPositionReports applies the side filter BEFORE the skip
# counters (VrfC2SimService.cs: `if (hostile ? !red : !blue) continue;` precedes both
# skipped++ branches), so under Vrf:PositionReportSides = blue a HOSTILE taskee is neither
# sent nor skipped and the round still reads "N sent, 0 skipped". That would satisfy this
# test on behalf of a unit that got nothing. Rounds are therefore counted ONLY when the
# interface says it is reporting BOTH sides - which is its default, and what every run in
# the record has logged. Under a one-sided filter this satisfier simply never fires and the
# window runs to its cap, exactly as it did before 2026-09-14.
# ORDER IS MEASURED IN CHARACTER OFFSETS, not line numbers, and the text is NEVER SPLIT.
# This runs on the WHOLE app log, which reached 40 MB in 127 s in the G6 run (sec 5): a
# -split would allocate a String[] of every line on top of the string itself, every time.
# [regex]::Matches walks the string once and yields .Index, which orders the records just as
# well. Same reason Get-VrfUuidByName is called on the same single read.
function Get-AppLogPositionEvidence {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    $out = @{}
    if ([string]::IsNullOrWhiteSpace($AppLogText)) { return $out }
    $rxTsk = [regex]'SENT TASK STATUS REPORT \(TASKCMPLT\) taskee=(?<taskee>[0-9A-Fa-f-]{36})'
    # The parenthetical between "skipped" and "sides=" differs between app builds, so it is
    # matched loosely; "sides=" itself is required, and a line without it is not a round.
    $rxR1  = [regex]'R1 position reports: (?<sent>\d+) sent, (?<skipped>\d+) skipped[^\r\n]*?sides=(?<sides>[A-Za-z]+)'
    $lastTsk = @{}
    foreach ($m in $rxTsk.Matches($AppLogText)) { $lastTsk[$m.Groups['taskee'].Value] = $m.Index }
    if ($lastTsk.Count -eq 0) { return $out }
    $lastRound = -1
    foreach ($m in $rxR1.Matches($AppLogText)) {
        if (([int]$m.Groups['sent'].Value -gt 0) -and ([int]$m.Groups['skipped'].Value -eq 0) -and
            ($m.Groups['sides'].Value -eq 'both')) { $lastRound = $m.Index }
    }
    foreach ($k in @($lastTsk.Keys)) { $out[$k] = ($lastRound -gt [int]$lastTsk[$k]) }
    return $out
}

# The moment the ORDER really reached the C2SIM bus, out of PushOrder's own capture
# (c2sim-bus.log, first line: "[HH:mm:ss.fff] ORDER (<n> chars)"). UTC, and with no date
# in the stamp, so the run's start supplies one. $null when there is no such header.
# Used ONLY to LABEL the runner's t+Ns messages: the observation-window clock starts when
# PushOrder RETURNS, which is up to -PushOrderListenSec LATER than this moment, and the
# two were being printed as if they were the same thing.
function Get-BusOrderUtc {
    param(
        [AllowNull()][AllowEmptyString()][string]$BusLogText,
        [Parameter(Mandatory)][datetime]$RunStartUtc
    )
    if ([string]::IsNullOrWhiteSpace($BusLogText)) { return $null }
    $m = [regex]::Match($BusLogText, '(?m)^\[(?<h>\d{2}):(?<mi>\d{2}):(?<s>\d{2})\.(?<f>\d{3})\]\s+ORDER\b')
    if (-not $m.Success) { return $null }
    $start = $RunStartUtc.ToUniversalTime()
    $t = $start.Date.AddHours([int]$m.Groups['h'].Value).AddMinutes([int]$m.Groups['mi'].Value).AddSeconds([int]$m.Groups['s'].Value).AddMilliseconds([int]$m.Groups['f'].Value)
    if ($t -lt $start.AddHours(-12)) { $t = $t.AddDays(1) }
    return $t
}

# Stage 8b WS RUNAWAY ABORT (RUNBOOK 0.5.11 item 17 extension, 2026-09-15 - V6f harvest defects 1+2).
# scripts\SampleThreads.ps1's tripwire only WARNS: it appends one line per confirmed episode to
# <csv base>.alerts.txt and nothing reads that file while a run is live. This is the runner-side half:
# given the WHOLE alerts file text and the instant the order reached the bus, return only the lines
# timestamped AT OR AFTER that instant, in file order - an alert from before the order even existed
# cannot be evidence THIS order caused a runaway. (Defect 2 - the object-creation-burst alert firing
# shortly after a mid-run dispatch - is a SEPARATE fix, SampleThreads.ps1's own re-armable warm-up
# -WarmupResetAtUtc/-WarmupResetFile; this filter does not touch that.)
# A line whose leading timestamp fails to parse is CONSERVATIVELY KEPT, not dropped: the producer
# (SampleThreads.ps1's Get-WsSlopeAlertLine) always writes a round-trip ISO-8601 UTC stamp first, so a
# parse failure means something is already wrong, and silently discarding possible runaway evidence
# would be the worse failure mode.
function Get-WsRunawayAlertsSinceDispatch {
    param(
        [AllowNull()][AllowEmptyString()][string] $AlertsText,
        [Parameter(Mandatory)][datetime] $DispatchUtc
    )
    $out = @()
    if ([string]::IsNullOrWhiteSpace($AlertsText)) { return $out }
    $dispatch = $DispatchUtc.ToUniversalTime()
    foreach ($line in ($AlertsText -split "`r?`n")) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $ts = ($line -split '\s+', 2)[0]
        $parsed = $null
        try {
            $parsed = ([datetime]::Parse($ts, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)).ToUniversalTime()
        } catch {
            $parsed = $null
        }
        if ($null -eq $parsed -or $parsed -ge $dispatch) { $out += $line }
    }
    return $out
}

# Condition (4). Returns AllSatisfied plus one record per taskee explaining why.
#
# THREE INDEPENDENT SATISFIERS, tried in this order; the FIRST that holds wins and names
# itself in the record's 'via'. Each one on its own is the post-completion position
# evidence the movement gate needs (HEADLESS_RUN_PLAN 4a) - demanding more than one is
# how this condition became unfireable (see the block above).
#   'RPT'            a post-completion VR-Forces text report agreeing with the sampled
#                    POS within -ToleranceMeters. The 2026-09-02 rule, UNCHANGED and
#                    still the strongest of the three - when it exists at all.
#   'C2SIM-capture'  a C2SIM PositionReport for the taskee's OWN uuid, captured later
#                    than that taskee's TASKCMPLT (-CaptureEvidence). The AUTHORITY, and
#                    LIVE since 2026-09-14: ListenReports appends and flushes per report.
#   'R1-applog'      a complete R1 position-report round (>=1 sent, 0 skipped) logged
#                    AFTER this taskee's TASKCMPLT line (-AppLogPositionEvidence). The
#                    fallback for a run with no usable capture - no observer, wrong
#                    endpoints, or an observer that died.
# All three new arguments are OPTIONAL: omitted, this function behaves exactly as it did
# before 2026-09-14, which is what tests\RunnerTurnaround.Tests.ps1 check 4b pins down.
function Test-ReportEvidence {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Taskees,
        [Parameter(Mandatory)]$TaskeeNames,      # taskee uuid -> marking (Get-InitUnitNames)
        [Parameter(Mandatory)]$NameToVrfUuid,    # marking -> VRF_UUID (Get-VrfUuidByName)
        [Parameter(Mandatory)][AllowNull()][AllowEmptyString()][string]$TraceText,
        [Parameter(Mandatory)][double]$ToleranceMeters,
        [AllowNull()]$CaptureEvidence = $null,        # Get-ReportCaptureEvidence output
        [AllowNull()]$AppLogPositionEvidence = $null, # Get-AppLogPositionEvidence output
        [AllowNull()]$CompletionUtcByTaskee = $null,  # taskee uuid -> UTC of the poll that
                                                      # first saw its TERMINAL report; the
                                                      # anchor of last resort for 'C2SIM-capture'
        [AllowNull()]$CodeByTaskee = $null            # taskee uuid -> the Code
                                                      # (TASKCMPLT/TASKABRT) of that SAME
                                                      # first terminal report
                                                      # (Update-CompletionState.firstSeenCode).
                                                      # Optional and printing-only: unset or
                                                      # missing prints 'TERMINAL' rather than
                                                      # guessing a code (2026-09-20, V6i
                                                      # cosmetic defect - this label used to
                                                      # hardcode 'TASKCMPLT' regardless of the
                                                      # actual terminal code).
    )
    $ev = Get-TraceEvidence -TraceText $TraceText
    $per = [ordered]@{}
    $all = ($Taskees.Count -gt 0)
    foreach ($u in $Taskees) {
        $rec = [ordered]@{ name = $null; vrfUuid = $null; completionT = $null; lastRptT = $null
                           posT = $null; distanceM = $null; capCompletionUtc = $null
                           capPosUtc = $null; capPosCount = 0; via = $null
                           satisfied = $false; reason = $null }
        $name = if ($TaskeeNames.Contains($u)) { [string]$TaskeeNames[$u] } else { $null }
        # The trace and the app log are keyed by VR-Forces MARKING: the init <Name> PLUS an
        # optional '~<tag>' the app appends to a proxy-substituted unit (VrfSettings
        # ProxyMarkingTag, default '~PXY', FidelityTable mode). Resolve the init name to that
        # marking, else '114.MechCoy' never matches '114.MechCoy~PXY' and the window runs
        # to its cap against a healthy run (run 20260902T193508Z).
        if ($name) { $name = Resolve-MarkingKey -Name $name -Keys (@($ev.tsk.Keys) + @($ev.rpt.Keys) + @($NameToVrfUuid.Keys)) }
        $rec.name = $name
        if (-not $name) { $rec.reason = 'taskee has no <Name> in the init - can not key TSK/RPT' }
        else {
            $vrf = if ($NameToVrfUuid.Contains($name)) { [string]$NameToVrfUuid[$name] } else { $null }
            $rec.vrfUuid = $vrf
            if ($ev.tsk.ContainsKey($name)) { $rec.completionT = $ev.tsk[$name] }
            if ($ev.rpt.ContainsKey($name)) { $rec.lastRptT = $ev.rpt[$name].T }
            if ($vrf -and $ev.pos.ContainsKey($vrf)) { $rec.posT = $ev.pos[$vrf].T }
            if ($null -eq $rec.completionT)  { $rec.reason = 'no TSK (task-complete) record in the trace yet' }
            elseif ($null -eq $rec.lastRptT) { $rec.reason = 'no RPT POSITION line for this marking yet' }
            elseif ($rec.lastRptT -le $rec.completionT) { $rec.reason = ('last RPT t={0} is not later than completion t={1}' -f $rec.lastRptT, $rec.completionT) }
            elseif (-not $vrf) { $rec.reason = 'marking -> VRF_UUID unknown (no route line in the app log)' }
            elseif ($null -eq $rec.posT) { $rec.reason = 'no real POS sample for the VRF_UUID yet' }
            else {
                $r = $ev.rpt[$name]; $p = $ev.pos[$vrf]
                $rec.distanceM = [Math]::Round((Get-DistanceMeters -Lat1 $r.Lat -Lon1 $r.Lon -Lat2 $p.Lat -Lon2 $p.Lon), 2)
                if ($rec.distanceM -le $ToleranceMeters) { $rec.satisfied = $true; $rec.via = 'RPT'; $rec.reason = 'post-completion RPT agrees with POS' }
                else { $rec.reason = ('post-completion RPT is {0} m from the latest POS (tolerance {1} m)' -f $rec.distanceM, $ToleranceMeters) }
            }
        }
        # 'C2SIM-capture'. The anchor is the capture's OWN TASKCMPLT TaskStatus record when
        # it holds one - same file, same clock, exact - and otherwise the runner's poll
        # stamp, which is late by at most one poll and therefore only ever DELAYS the close.
        if ((-not $rec.satisfied) -and $null -ne $CaptureEvidence) {
            $anchor = $null; $anchorSrc = ''
            if ($CaptureEvidence.cmplt.ContainsKey($u)) {
                $anchor = [datetime]$CaptureEvidence.cmplt[$u]; $anchorSrc = 'the capture own TASKCMPLT'
            } elseif ($null -ne $CompletionUtcByTaskee -and $CompletionUtcByTaskee.Contains($u)) {
                $anchorCode = 'TERMINAL'
                if ($null -ne $CodeByTaskee -and $CodeByTaskee.Contains($u) -and -not [string]::IsNullOrWhiteSpace([string]$CodeByTaskee[$u])) { $anchorCode = [string]$CodeByTaskee[$u] }
                $anchor = ([datetime]$CompletionUtcByTaskee[$u]); $anchorSrc = ('the runner poll that first saw {0}' -f $anchorCode)
            }
            if ($null -ne $anchor) {
                $rec.capCompletionUtc = $anchor.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                if ($CaptureEvidence.pos.ContainsKey($u)) {
                    $lastPos = [datetime]$CaptureEvidence.pos[$u]
                    $rec.capPosUtc = $lastPos.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                    if ($CaptureEvidence.posN.ContainsKey($u)) { $rec.capPosCount = [int]$CaptureEvidence.posN[$u] }
                    if ($lastPos -gt $anchor) {
                        $rec.satisfied = $true; $rec.via = 'C2SIM-capture'
                        $rec.reason = ('C2SIM PositionReport at {0} is later than {1} at {2} ({3} fixes captured)' -f $rec.capPosUtc, $anchorSrc, $rec.capCompletionUtc, $rec.capPosCount)
                    }
                }
            }
        }
        # 'R1-applog'. Line order inside the app log, nothing else - that log has no clock.
        if ((-not $rec.satisfied) -and $null -ne $AppLogPositionEvidence -and $AppLogPositionEvidence.ContainsKey($u) -and [bool]$AppLogPositionEvidence[$u]) {
            $rec.satisfied = $true; $rec.via = 'R1-applog'
            $rec.reason = 'the interface logged a COMPLETE R1 position-report round (0 skipped) after this taskee TASKCMPLT line'
        }
        if (-not $rec.satisfied) { $all = $false }
        $per[$u] = [pscustomobject]$rec
    }
    return [pscustomobject]@{ AllSatisfied = $all; PerTaskee = $per }
}

# ---- THE APP'S OWN ROUTE-SHIFT ANNOUNCEMENT (review F5) -----------------------
# The runner PREDICTS the route-shift state from its own environment plus the deployed
# appsettings.json, in the app's precedence order. A prediction is not an observation:
# scripts\StartInterface52.ps1 -RouteShift on|off sets Vrf__PreflightRouteShift in the
# INTERFACE's process, so a hand-started interface can run with a value the runner's own
# shell never saw, and a deployed appsettings.json can change between the Stage 0 read and
# the app's own read. The app states what it really resolved, in its own log, in two places:
#
#   start-up, ON only : "LATERAL ROUTE SHIFT ON (Vrf:PreflightRouteShift, the shipped
#                        default since the user ruling of 2026-09-20; STP-804/806): ..."
#   first ground move : "ROUTE PRE-FLIGHT enabled: ... LATERAL ROUTE SHIFT ON|off (the shift
#                        CHANGES the line a unit drives; ...)"
#
# ABSENCE IS NOT EVIDENCE OF 'off'. The app prints NOTHING at start-up when the shift is off,
# and the pre-flight line appears only once a ground move is dispatched, so a log carrying
# neither line means NOT OBSERVED. That is why Announced is a THREE-valued $true/$false/$null
# and never defaults to the prediction.
#
# Returns an ordered hashtable:
#   Announced = $true | $false | $null   what the app said (null = it has not said yet)
#   Line      = the matched log line, trimmed ('' when there is none)
#   Source    = which of the two lines supplied the answer
#   Skipped   = $true when the app ALSO said the shift is SKIPPED FOR THIS RUN (offline with
#               an empty tile cache): configured ON, but no leg can ever be shifted.
function Get-RouteShiftAnnouncement {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    $out = [ordered]@{ Announced = $null; Line = ''; Source = ''; Skipped = $false }
    if ([string]::IsNullOrEmpty($AppLogText)) { return $out }
    if ([regex]::IsMatch($AppLogText, 'LATERAL ROUTE SHIFT SKIPPED FOR THIS RUN')) { $out['Skipped'] = $true }
    # The pre-flight line is read FIRST because it is the only one that can say "off". When
    # both lines are present they agree by construction - one setting, read once at start-up.
    # ON / off are matched case-SENSITIVELY, which is how the app writes them.
    $m = [regex]::Match($AppLogText, '(?m)^.*LATERAL ROUTE SHIFT (?<v>ON|off) \(the shift CHANGES.*$')
    if ($m.Success) {
        $out['Announced'] = ($m.Groups['v'].Value -ceq 'ON')
        $out['Line']      = $m.Value.Trim()
        $out['Source']    = "the app's ROUTE PRE-FLIGHT enabled line"
        return $out
    }
    $m = [regex]::Match($AppLogText, '(?m)^.*LATERAL ROUTE SHIFT ON \(Vrf:PreflightRouteShift.*$')
    if ($m.Success) {
        $out['Announced'] = $true
        $out['Line']      = $m.Value.Trim()
        $out['Source']    = "the app's start-up banner"
    }
    return $out
}

# THE VERDICT on the runner's prediction against the app's announcement (review F5). Pure, so
# the rule itself can be tested offline rather than asserted in prose.
#   $Predicted  - whatever the manifest holds: a [bool], or the 'UNKNOWN - unparseable ...'
#                 string the Stage 0 resolver writes when Vrf__PreflightRouteShift is garbage.
#   $Announced  - $true / $false / $null, straight from Get-RouteShiftAnnouncement.
# Returns @{ Observed; Mismatch; Agreement }.
#
# THE TWO RULES IT ENCODES:
#   1. NO ANNOUNCEMENT MEANS NOT OBSERVED. The prediction is never promoted to an observation,
#      and no MISMATCH is ever claimed on silence - the app prints nothing at start-up when the
#      shift is off, so silence is not evidence of anything.
#   2. A non-boolean prediction (the unparseable-env case) can never AGREE with an announcement;
#      if the app somehow announced anything at all, that is a mismatch worth seeing.
function Get-RouteShiftVerdict {
    param($Predicted, [AllowNull()][AllowEmptyString()][string]$PredictedSource,
          $Announced, [AllowNull()][AllowEmptyString()][string]$AnnouncedSource,
          [AllowNull()][AllowEmptyString()][string]$Stage)
    $where = $(if ([string]::IsNullOrWhiteSpace($Stage)) { 'this point' } else { $Stage })
    if ($null -eq $Announced) {
        return [ordered]@{
            Observed  = $false
            Mismatch  = $false
            Agreement = ('NOT OBSERVED at {0} - the app log carries no "LATERAL ROUTE SHIFT" line yet. The prediction stands UNCONFIRMED, and an unconfirmed prediction is not an observation. The app prints NOTHING at start-up when the shift is OFF, so silence here is not evidence of OFF.' -f $where)
        }
    }
    $annText = $(if ($Announced) { 'ON' } else { 'off' })
    if (($Predicted -is [bool]) -and ([bool]$Predicted -eq [bool]$Announced)) {
        return [ordered]@{
            Observed  = $true
            Mismatch  = $false
            Agreement = ('CONFIRMED at {0} - the app announced {1}, which is what this runner predicted from {2}. Evidence: {3}' -f $where, $annText, $PredictedSource, $AnnouncedSource)
        }
    }
    return [ordered]@{
        Observed  = $true
        Mismatch  = $true
        Agreement = ('MISMATCH at {0} - this runner PREDICTED [{1}] from {2}, and the app ANNOUNCED [{3}] ({4}). THE APP IS THE AUTHORITY ON THE APP: every route in this run was driven under the ANNOUNCED value. Usual cause: the interface was started by hand (scripts\StartInterface52.ps1 -RouteShift on|off sets Vrf__PreflightRouteShift in ITS OWN process, which this runner''s shell cannot see), or the deployed appsettings.json differs from the one Stage 0 read.' -f $where, $Predicted, $PredictedSource, $annText, $AnnouncedSource)
    }
}

# =============================================================================
# THE SAME AGREEMENT MACHINERY FOR THE OTHER TWO SETTINGS THAT CHANGE A RUN'S MEANING
# (adca180 build report FINDING 2, D7 lane)
# =============================================================================
# The route shift got a prediction, an announcement and an agreement verdict because it CHANGES
# WHERE UNITS DRIVE. Two more settings change what a run means just as much and had nothing:
#   Vrf:DeStackComposedSiblings  - changes WHERE UNITS START (prereg D7 P1)
#   Vrf:ArrivalApproachFraction  - changes WHEN A TASK IS REPORTED COMPLETE (prereg D7 P5)
# Both are scored off the app's log; until the app announced them unconditionally there was
# nothing to read at Stage 6c. These are the pure halves - a parser and a rule - in the same
# shape as Get-RouteShiftAnnouncement / Get-RouteShiftVerdict, which are left exactly as they
# were so their own tests keep their meaning.

# THE APP'S OWN START-UP ANNOUNCEMENT of the composed-sibling de-stack, written unconditionally
# (VrfC2SimService 0c-iv). ON / off are matched case-SENSITIVELY, as the app writes them.
# Returns @{ Announced = $true/$false/$null; Line; Source }.
function Get-DeStackSiblingAnnouncement {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    $out = [ordered]@{ Announced = $null; Line = ''; Source = '' }
    if ([string]::IsNullOrEmpty($AppLogText)) { return $out }
    $m = [regex]::Match($AppLogText, '(?m)^.*COMPOSED-SIBLING DE-STACK (?<v>ON|off) \(Vrf:DeStackComposedSiblings.*$')
    if ($m.Success) {
        $out['Announced'] = ($m.Groups['v'].Value -ceq 'ON')
        $out['Line']      = $m.Value.Trim()
        $out['Source']    = "the app's COMPOSED-SIBLING DE-STACK start-up line"
    }
    return $out
}

# ...and of the arrival approach fraction. The announced value is a NUMBER, not a flag, so it is
# returned as a [double] and compared with a tolerance by the verdict below - 0.50 and 0.5 are
# the same setting and must not read as a mismatch.
function Get-ArrivalApproachAnnouncement {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    $out = [ordered]@{ Announced = $null; Line = ''; Source = '' }
    if ([string]::IsNullOrEmpty($AppLogText)) { return $out }
    $m = [regex]::Match($AppLogText, '(?m)^.*ARRIVAL APPROACH FRACTION (?<v>[0-9]+(?:\.[0-9]+)?) \(Vrf:ArrivalApproachFraction.*$')
    if ($m.Success) {
        $out['Announced'] = [double]$m.Groups['v'].Value
        $out['Line']      = $m.Value.Trim()
        $out['Source']    = "the app's ARRIVAL APPROACH FRACTION start-up line"
    }
    return $out
}

# THE VERDICT RULE, generalised from Get-RouteShiftVerdict so the three settings cannot drift
# apart. Same two rules it encodes:
#   1. NO ANNOUNCEMENT MEANS NOT OBSERVED. A prediction is never promoted to an observation.
#   2. A prediction that is not a value at all (the unparseable-env case) can never AGREE.
# Numbers are compared with a tolerance; anything else ordinally after trimming.
function Get-SettingVerdict {
    param([Parameter(Mandatory)][string]$Name,
          $Predicted, [AllowNull()][AllowEmptyString()][string]$PredictedSource,
          $Announced, [AllowNull()][AllowEmptyString()][string]$AnnouncedSource,
          [AllowNull()][AllowEmptyString()][string]$Stage,
          [double]$Tolerance = 1e-9)
    $where = $(if ([string]::IsNullOrWhiteSpace($Stage)) { 'this point' } else { $Stage })
    if ($null -eq $Announced) {
        return [ordered]@{
            Observed  = $false
            Mismatch  = $false
            Agreement = ('NOT OBSERVED at {0} - the app log carries no {1} announcement yet. The prediction stands UNCONFIRMED, and an unconfirmed prediction is not an observation.' -f $where, $Name)
        }
    }
    $agree = $false
    if (($Predicted -is [bool]) -and ($Announced -is [bool])) {
        $agree = ([bool]$Predicted -eq [bool]$Announced)
    } elseif (($Predicted -is [double] -or $Predicted -is [int] -or $Predicted -is [decimal]) -and
              ($Announced -is [double] -or $Announced -is [int] -or $Announced -is [decimal])) {
        $agree = ([math]::Abs([double]$Predicted - [double]$Announced) -le $Tolerance)
    }
    if ($agree) {
        return [ordered]@{
            Observed  = $true
            Mismatch  = $false
            Agreement = ('CONFIRMED at {0} - the app announced {1} = [{2}], which is what this runner predicted from {3}. Evidence: {4}' -f $where, $Name, $Announced, $PredictedSource, $AnnouncedSource)
        }
    }
    return [ordered]@{
        Observed  = $true
        Mismatch  = $true
        Agreement = ('MISMATCH at {0} - this runner PREDICTED {1} = [{2}] from {3}, and the app ANNOUNCED [{4}] ({5}). THE APP IS THE AUTHORITY ON THE APP: score this run on the ANNOUNCED value. Usual cause: the interface was started by hand with its own environment, or the deployed appsettings.json differs from the one Stage 0 read.' -f $where, $Name, $Predicted, $PredictedSource, $Announced, $AnnouncedSource)
    }
}

# ---- THE VENDOR PER-PROCESS LOG FOR ONE PID (D1b harvest, finding A1) ---------
# VR-Forces 5.2 writes
#     <prefix><version>-<date>-<time>-<host>-<build>-<pid>.log
# into the SHARED C:\MAK\logs. It NEVER writes the flat 5.0.2 names (bin64\vrfSim.log,
# C:\MAK\logs\vrfGui.log), which is why the runner's end-of-run capture WARNed twice per
# 5.2 run, since forever, about two files that cannot exist - while the real logs sat
# un-captured beside them. The pid is the ONLY field that ties a file in that shared
# directory to one run, so the capture is BY PID.
#
# Three filters, the same three (and for the same reasons) as LaunchVrf52.ps1's
# Get-VendorSimLogForPid, which already does exactly this for the back-end log at READY:
#   - the pid is the LAST name field, hence the '<prefix>*-<pid>.log' filter;
#   - C:\MAK\logs is shared by the whole MAK toolchain and keeps files across boots, and
#     Windows recycles pids, hence the -Since mtime floor;
#   - the .callstack.log for the same pid does NOT match that filter (its last field is
#     'callstack') and is excluded explicitly anyway - it is separate evidence with a
#     separate life: IT is the file that may be shared, and this one is not.
#
# SECRETS - A HARD CONSTRAINT, not a style note. These files carry the FULL PROCESS
# ENVIRONMENT IN CLEARTEXT (DtPrintEnvironmentVariables at notifyLevel 3;
# FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10). This function COPIES a file and NEVER
# OPENS ONE: nothing here reads, greps, parses, hashes or prints a byte of its content,
# and the caller repeats the warning LaunchVrf52 prints for the copy it takes. Length is
# read from the directory entry, not from the file. Read-only, and it never throws: a
# capture failure is a WARN for the caller and nothing else.
#
# Returns @{ Source = <the file copied, '' when none>; Error = <message, '' when none>;
#            SizeBytes = <directory-entry length, $null when none> }
function Copy-VendorLogByPid {
    param([int]$ProcessId, [string]$LogDir, [string]$NamePrefix,
          [datetime]$Since, [string]$Destination)
    $out = [ordered]@{ Source = ''; Error = ''; SizeBytes = $null }
    try {
        $ls = @(Get-ChildItem -LiteralPath $LogDir -Filter ($NamePrefix + ('*-{0}.log' -f $ProcessId)) -File -ErrorAction SilentlyContinue |
                Where-Object { ($_.Name -notmatch '\.callstack\.log$') -and ($_.LastWriteTime -ge $Since) } |
                Sort-Object LastWriteTime -Descending)
        if ($ls.Count -eq 0) { return $out }
        $out['SizeBytes'] = $ls[0].Length
        Copy-Item -LiteralPath $ls[0].FullName -Destination $Destination -Force -ErrorAction Stop
        $out['Source'] = $ls[0].FullName
    } catch {
        $out['Error'] = $_.Exception.Message
    }
    return $out
}

# ---- line endings for files the runner rewrites -------------------------------
# The repo checks out CRLF (core.autocrlf=true); a file the runner rewrites must
# come back CRLF regardless of what it found (an LF working copy left by another
# tool would otherwise be perpetuated - docs/OPUS_EXECUTION_PLAN.md was found LF
# after run 20260901T235823Z). Idempotent: CRLF in -> CRLF out, no doubled CR.
function ConvertTo-CrlfText {
    param([AllowNull()][AllowEmptyString()][string]$Text)
    if ($null -eq $Text) { return '' }
    return ($Text -replace "`r`n", "`n") -replace "`n", "`r`n"
}

# ---- trace stop timing -------------------------------------------------------
# The observers are told to stop (stop-file touched) no earlier than
# ReferenceUtc + TrailSecs, where ReferenceUtc is the StopIface moment (or, when
# StopIface never ran, the moment teardown reached the observers). Returns the
# whole seconds still to wait, never negative.
function Get-TraceStopWaitSecs {
    param(
        [Parameter(Mandatory)][datetime]$ReferenceUtc,
        [Parameter(Mandatory)][int]$TrailSecs,
        [Parameter(Mandatory)][datetime]$NowUtc
    )
    $remaining = ($ReferenceUtc.AddSeconds($TrailSecs) - $NowUtc).TotalSeconds
    if ($remaining -le 0) { return 0 }
    return [int][Math]::Ceiling($remaining)
}

# ---- observer duration-cap fallback (review F1) -------------------------------
# When an observer started with --stop-file has NOT exited within the grace after
# the stop file was touched, teardown must not proceed to StopVrf under a possibly
# still-joined federate. It keeps waiting - never kills - up to the moment the
# observer's OWN duration cap ends it: StartedUtc (the stage's launch stamp) +
# DurationSecs (the cap argument it was given) + MarginSecs (process start-up and
# resign latency; P2c's WatchVrf-trace exited 5.5 s after start + cap). Returns the
# whole seconds still to wait, never negative, rounded UP.
function Get-ObserverCapRemainingSecs {
    param(
        [Parameter(Mandatory)][datetime]$StartedUtc,
        [Parameter(Mandatory)][int]$DurationSecs,
        [Parameter(Mandatory)][int]$MarginSecs,
        [Parameter(Mandatory)][datetime]$NowUtc
    )
    $remaining = ($StartedUtc.AddSeconds($DurationSecs + $MarginSecs) - $NowUtc).TotalSeconds
    if ($remaining -le 0) { return 0 }
    return [int][Math]::Ceiling($remaining)
}

# The manifest stamps every stage start as 'yyyy-MM-ddTHH:mm:ss.fffZ' (UTC). Parse
# that back to a UTC datetime; $null when the text is missing or malformed so the
# caller can fall back (to the Process object's own StartTime) instead of throwing
# inside teardown.
function ConvertFrom-ManifestUtc {
    param([AllowNull()][AllowEmptyString()][string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
    $out = [datetime]::MinValue
    $styles = [System.Globalization.DateTimeStyles]::AssumeUniversal -bor [System.Globalization.DateTimeStyles]::AdjustToUniversal
    if ([datetime]::TryParseExact($Text, 'yyyy-MM-ddTHH:mm:ss.fffZ', [System.Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$out)) {
        return $out
    }
    return $null
}

# ---- tool capability probe parse ---------------------------------------------
# `<tool>.exe --capabilities` prints one capability token per stdout line and exits
# 0. A DEPLOYED binary that predates the flag rejects it as an unknown option and
# exits 2 (ToolArgs.UnknownFlags) - or, for WatchVrf with no MAK PATH, fails to load
# its bridge and exits non-zero. Either way: not exit 0 => NOT supported. This is
# what keeps the runner from repeating the -ConsoleLogDir landmine (passing a flag
# the deployed oracle does not have kills the oracle stage with exit 2 after a full
# launch cycle).
function Test-ToolCapability {
    param(
        [AllowNull()][AllowEmptyCollection()][string[]]$ProbeLines,
        $ExitCode,
        [Parameter(Mandatory)][string]$Capability
    )
    if ($null -eq $ExitCode -or [int]$ExitCode -ne 0) { return $false }
    foreach ($l in @($ProbeLines)) {
        if ($null -ne $l -and $l.Trim() -eq $Capability) { return $true }
    }
    return $false
}

# ---- stage 7d READY GATE: the simulator's own navigation-area acquisition -----
# The sectorised navigation area is NOT usable when the entities are placed. It
# becomes usable at the first
#     VRF console [3] <object> (VRF_UUID:...): New Primary nav area: | <area>
# row printed by a placed PLATFORM - 9.1-12.1 s after the first placement with a
# warm file cache and 236.9 s cold (four runs, 2026-09-14;
# docs/experiments/G7B_G8_RESULTS_2026-09-14.md sec 1.5 and 3). Before that row
# every ground-vehicle-move-to fails the "Is current point in nav area?" gate and
# is planned by the FEATURE planner on one straight part, silently, at console
# level 3. These two parsers are what the runner's stage-7d gate polls the
# interface log with; they are pure so the same code can be replayed offline over
# a finished run's vrfc2simapp.log.
#
# ANY object's row counts. The row is the SIMULATOR saying the area is now its
# primary one; the marking that prints it first is whichever platform the engine
# reached first (1.BdeHQ in all four runs) and is not something to depend on.
# The rows only exist at object-console level >= 3, which is why the runner
# refuses -PreOrderGate with the console below that.
function Get-NavAreaRows {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    $rows = @()
    if ([string]::IsNullOrEmpty($AppLogText)) { return $rows }
    foreach ($line in ($AppLogText -split "`r?`n")) {
        # Cheap literal reject first: this runs over every appended line of a log
        # that reaches gigabytes.
        if ($line.IndexOf('New Primary nav area', [System.StringComparison]::Ordinal) -lt 0) { continue }
        $m = [regex]::Match($line, 'VRF console \[(?<lvl>\d+)\]\s+(?<obj>.*?)\s*(?:\(VRF_UUID:(?<uuid>[^)]*)\))?\s*:\s*New Primary nav area\s*:\s*\|?\s*(?<area>.*?)\s*$')
        if (-not $m.Success) { continue }
        $rows += [ordered]@{
            level  = [int]$m.Groups['lvl'].Value
            object = $m.Groups['obj'].Value
            uuid   = $(if ($m.Groups['uuid'].Success) { $m.Groups['uuid'].Value } else { '' })
            area   = $m.Groups['area'].Value
            line   = $line.Trim()
        }
    }
    return $rows
}

# The interface's own placement line, the OTHER end of the measured interval:
#     PLACEMENT: UNIT <marking> domain=1 created at authored lat/lon; ...
# PLATFORM is matched as well as UNIT because the first entity placed is what
# starts the terrain-tile stream, and the init shells and proxy platforms are
# placed before any member (G7B_G8_RESULTS sec 3). The delta between the first of
# these and the first nav-area row is the CACHE-STATE indicator: ~10 s warm,
# ~240 s cold.
function Get-PlacementRows {
    param([AllowNull()][AllowEmptyString()][string]$AppLogText)
    $rows = @()
    if ([string]::IsNullOrEmpty($AppLogText)) { return $rows }
    foreach ($line in ($AppLogText -split "`r?`n")) {
        if ($line.IndexOf('PLACEMENT:', [System.StringComparison]::Ordinal) -lt 0) { continue }
        $m = [regex]::Match($line, 'PLACEMENT:\s+(?<kind>UNIT|PLATFORM)\s+(?<name>\S+)\s.*?\bcreated\b')
        if (-not $m.Success) { continue }
        $rows += [ordered]@{
            kind = $m.Groups['kind'].Value
            name = $m.Groups['name'].Value
            line = $line.Trim()
        }
    }
    return $rows
}

# ---- STAGE 1a: is another runner already live? (RUNBOOK 0.5.14 item 15) -----
# WHY THIS EXISTS. Run V8z (2026-09-15 03:51Z, voided): two RunC2SimScenario.ps1
# instances were started 2 s apart. BOTH passed the OLD Stage 1 (RUNBOOK 0.5.0
# checks vrfLauncher/vrfSimHLA1516e/vrfGui/WatchVrf/ListenReports, never ANOTHER
# RUNNER), both allocated appNos, both launched; the second saw the first's READY
# back end, its PushInit failed, and ITS teardown stopped the sim under the FIRST
# runner's live order.
#
# PURE ON PURPOSE, like every other helper in this file. These take an
# ALREADY-FETCHED process snapshot (one Get-CimInstance Win32_Process call, made
# by the runner) instead of querying WMI themselves, so the detection and
# ancestor-exclusion logic can be exercised offline with a synthetic array - no
# real second pwsh needed to prove the match/exclude rules.
function Get-ProcessAncestorIds {
    param(
        [Parameter(Mandatory)][int]$StartPid,
        [Parameter(Mandatory)][AllowEmptyCollection()]$Processes
    )
    $byPid = @{}
    foreach ($p in $Processes) { $byPid[[int]$p.ProcessId] = $p }
    $ids = New-Object 'System.Collections.Generic.HashSet[int]'
    $cur = $StartPid
    for ($i = 0; $i -lt 64; $i++) {
        if (-not $ids.Add($cur)) { break }           # cycle guard
        if (-not $byPid.ContainsKey($cur)) { break }  # chain ends where the snapshot does
        $parent = $byPid[$cur]
        if (-not $parent.ParentProcessId -or [int]$parent.ParentProcessId -eq 0) { break }
        $cur = [int]$parent.ParentProcessId
    }
    return $ids
}

# Every OTHER pwsh.exe in $Processes whose CommandLine names $ScriptPattern,
# excluding $SelfPid and its ancestor chain (a debugger or a nested pwsh
# invocation must not refuse against itself). Get-Process exposes no
# CommandLine property, which is why the runner fetches Win32_Process instead.
function Get-OtherRunnerProcessInfo {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()]$Processes,
        [Parameter(Mandatory)][int]$SelfPid,
        [string]$ScriptPattern = 'RunC2SimScenario\.ps1'
    )
    # @(...): PowerShell enumerates an IEnumerable return value onto the pipeline
    # and unwraps a single emitted item back to a scalar (Hashtable/PSCustomObject
    # are the documented exceptions - a HashSet[int] is not), so a chain of exactly
    # one ancestor would otherwise hand back a bare int with no .Contains(). Wrapping
    # the call forces an array regardless of count, matching this file's existing
    # convention at every Get-OrderTasks/Get-NavAreaRows/... call site.
    $exclude = @(Get-ProcessAncestorIds -StartPid $SelfPid -Processes $Processes)
    $found = @()
    foreach ($p in $Processes) {
        $ppid = [int]$p.ProcessId
        if ($exclude.Contains($ppid)) { continue }
        if ([string]::IsNullOrEmpty($p.CommandLine)) { continue }
        if ($p.CommandLine -notmatch $ScriptPattern) { continue }
        $startedUtc = ''
        if ($p.PSObject.Properties['StartedUtc'] -and $p.StartedUtc) {
            $startedUtc = $p.StartedUtc
        } elseif ($p.PSObject.Properties['CreationDate'] -and $p.CreationDate) {
            try { $startedUtc = ([Management.ManagementDateTimeConverter]::ToDateTime($p.CreationDate)).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ') }
            catch { try { $startedUtc = ([datetime]$p.CreationDate).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ') } catch { $startedUtc = '' } }
        }
        $found += [ordered]@{ pid = $ppid; startedUtc = $startedUtc; commandLine = $p.CommandLine }
    }
    return $found
}

# The lock file's content is three "key=value" lines (pid/utc/runDir) - simple
# enough to hand-read in an incident, structured enough to parse back reliably.
function ConvertFrom-RunnerLockText {
    param([AllowNull()][AllowEmptyString()][string]$Text)
    $info = [ordered]@{ pid = 0; utc = ''; runDir = '' }
    if ([string]::IsNullOrEmpty($Text)) { return $info }
    if ($Text -match '(?m)^pid=(\d+)\s*$')    { $info.pid    = [int]$Matches[1] }
    if ($Text -match '(?m)^utc=(\S+)\s*$')    { $info.utc    = $Matches[1] }
    if ($Text -match '(?m)^runDir=(.*?)\s*$')  { $info.runDir = $Matches[1] }
    return $info
}

function Format-RunnerLockContent {
    param(
        [Parameter(Mandatory)][int]$RunnerPid,
        [Parameter(Mandatory)][string]$Utc,
        [Parameter(Mandatory)][string]$RunDir
    )
    return ('pid={0}{3}utc={1}{3}runDir={2}{3}' -f $RunnerPid, $Utc, $RunDir, "`r`n")
}

# The exact wording RUNBOOK 0.5.14 item 15 documents for a refusal, factored out
# so both the process-scan path and the lock-file path in the runner produce the
# same line and a test can pin it down once.
function Format-OtherRunnerRefusal {
    param(
        [Parameter(Mandatory)][int]$OtherPid,
        [Parameter(Mandatory)][string]$Utc,
        [Parameter(Mandatory)][string]$RunDir
    )
    return ('another runner is live: pid {0} started {1}, run dir {2}' -f $OtherPid, $Utc, $RunDir)
}

# ---- THE TWO 5.2 GUI TEARDOWN MODALS, AS PERSISTED SETTINGS (STP-844) -------
# D1 (run 20260920T172141Z) was the FIRST 5.2 GUI-ON teardown ever. StopVrf52 sent
# WM_CLOSE, the GUI raised its exit prompt, nothing answered it, the whole 121 s budget
# was spent waiting and vrfGui was left running (StopVrf52 exit 3, runner exit 4). A
# READ-ONLY window enumeration of that pid found TWO STACKED modals, both of class
# makVrf::DtNeverAskAgainMessageBox, both owned by the GUI main window:
#   1. "Are You Sure?"   / "Quit VR-Forces GUI"
#         [Yes] [No] + checkbox "Quit All Sim Engines"          (underneath, disabled)
#   2. "Session Status"  / "The current session has ended. Close current terrain?"
#         [Yes] [No] + checkbox "Execute session changes without prompting."  (on top)
#
# MODAL 1 IS DOCUMENTED. UG52 4.6 "Exiting VR-Forces" states that exiting the GUI opens
# an exit prompt, explicitly including independent mode - which is how LaunchVrf52 starts
# 5.2 (UG52 4.1.2). UG52 4.6.1 "Disabling the Quit Prompt" is the vendor's own off
# switch: Settings > Application > General Application Settings > clear "Show Quit Dialog
# On Close" ("exiting acts as if you clicked Yes on the exit prompt"). It is a PERSISTED
# SETTING, not a command-line option - all 85 vrfGui options in UG52 Table 10 were read
# and none suppresses it; the only relevant one is --appDataDir, which decides WHICH
# settings tree is read.
#
# MODAL 2 IS OUR OWN ORDERING, not a save-scenario prompt (there is none: the objects our
# run creates and deletes raise nothing). StopVrf52 closes the front end first and asks
# the back end to close -GraceSec later, so the session ends while modal 1 is still open
# and the GUI stacks the session-ended prompt on top of it. Its documented switch is
# UG52 4.3.1 "Configuring Session Messages and Join at Startup": Settings > Application >
# Session Settings page > "Show Session Terrain Change Prompts" - "Enable if you want to
# be prompted when the terrain in a session changes". Clearing it is exactly what the
# dialog's own never-ask-again checkbox does from inside the dialog. With modal 1
# suppressed the GUI should exit on WM_CLOSE BEFORE the back end is asked to close, so
# modal 2 should never arise; clearing it too is belt-and-braces for the case where the
# GUI outlives the grace for some other reason.
#
# WHERE THE TWO VALUES LIVE - appData\settings\vrfGui\, under whatever --appDataDir the
# GUI was given (vendor default C:\MAK\vrforces5.2d\appData, which is exactly the tree
# D1 ran against, launchvrf.stdout.log:44):
#   default_Application.apsx      <myShowQuitDialogOnClose>1</...>
#       the boost NVP of makArchives::DtApplicationSettingsRecord::myShowQuitDialogOnClose
#       (include\makArchives\DtApplicationSettingsRecord.h:107 serialize, :124 member;
#       setter/getter at :61/:64). 0 = no exit prompt.
#   default_SessionSettings.srsx  <mySessionOptions>112885</...>
#       the boost NVP of makVrf::DtVrfSessionSettingsRecord::mySessionOptions
#       (include\vrfGuiCore\vrfSessionSettingsRecord.h:85 serialize, :103 member), and it
#       is a FLAG WORD, not a boolean. The enum at :29-38 names SEVEN bits:
#         0x00001 DtAutoJoinSession                        SET in the shipped value
#         0x00002 DtAskToJoin                              clear
#         0x00004 DtAlwaysJoinWithSessionDatabase          SET
#         0x00008 DtAlwaysJoinWithOpenDatabase             clear
#         0x00010 DtShowSessionDialogs                     SET  <- the only bit we clear
#         0x00040 DtAllowScenarioChanges                   SET
#         0x40000 DtAutomaticallyOpenSessionDatabaseWithoutJoiningSession   clear
#       The shipped value is 112885 = 0x1B8F5, and CLEARING 0x10 GIVES 112869.
#       *** THE NAMED BITS ACCOUNT FOR ONLY 85 OF 112885. The other 112800 is SEVEN SET
#       BITS WITH NO NAME IN THE 5.2 HEADER: 0x20, 0x80, 0x800, 0x1000, 0x2000, 0x8000,
#       0x10000. Do not present this word as if it decomposed into the enum alone - it
#       does not, which means this build's VrfSessionSettingsOptions is WIDER than the
#       shipped vrfGuiCore header, and one of those seven unnamed bits could be the real
#       never-ask-again store. That weakens the DERIVED claim below and it is the first
#       alternative to check if the remedy does not take. ***
#       THE EDIT IS THEREFORE A MASK (-band -bnot 0x10), never a rewritten literal: it
#       preserves 0x1 and 0x4 (set) and 0x2 (clear) - which is why D1's GUI auto-joined its
#       session at startup with no join prompt, and that must not change - AND it preserves
#       all seven unnamed bits, whose meaning we do not know and must not silently drop.
#
# DERIVED, NOT VERIFIED: that modal 2's checkbox "Execute session changes without
# prompting." is the same setting as the Session Settings page's "Show Session Terrain
# Change Prompts" / DtShowSessionDialogs. The vendor documents the page option, and the
# SDK header names the flag and its accessor (setDisplaySessionDialogs, :69), but no MAK
# document ties that checkbox STRING to that flag, and the string is not in any bin64 DLL
# as plain ASCII or UTF-16. If "Session Status" still appears with the bit cleared, the
# mapping is wrong - but note that "put the bit back" is only ONE of the responses, and
# probably not the first: the same observation is also explained by (i) the checkbox
# living in one of the seven UNNAMED bits above, or in one of the two undocumented flag
# words in settings\vrfGui\applicationSettings.xml
# (DtVrfExtendedApplicationSettingsDataFlags, DtVrfExtendedEntitySettingsDataFlags), in
# which case 0x10 is the right FILE and the wrong BIT; or (ii) session settings arriving
# from the SESSION at join time rather than from the local file - the GUI joins with
# DtAlwaysJoinWithSessionDatabase (0x4) SET - in which case no local edit can suppress
# that modal at all. Distinguish them by diffing the run-owned settings directory against
# a copy taken right after seeding, rather than by guessing.
# myShowQuitDialogOnClose, by contrast, is named by the vendor's own help page
# (doc\help\Content\Introduction\Starting\vrf_disableQuitDialog.htm) and by UG52 4.6.1.
#
# These two helpers are PURE (string in, string out) so the whole remedy is exercised
# offline with no VR-Forces on the machine; the file I/O and the tree seeding live in
# scripts\NewVrfAppData52.ps1, which never writes under C:\MAK.

# makVrf::DtVrfSessionSettingsRecord::DtShowSessionDialogs (vrfSessionSettingsRecord.h:35)
$script:VrfShowSessionDialogsFlag = 16

# Read the two prompt settings out of the TEXT of the two vrfGui settings files. Either
# may be empty or missing the key (a tree seeded from a different VR-Forces version, a
# truncated copy): that is reported as ABSENT, never guessed at as a default, because
# "the key is not there" and "the key is 0" have opposite consequences for a teardown.
function Get-VrfGuiPromptSettings {
    param(
        [AllowEmptyString()][string]$ApplicationXml = '',
        [AllowEmptyString()][string]$SessionSettingsXml = ''
    )
    $quit = $null
    $mq = [regex]::Match($ApplicationXml, '<myShowQuitDialogOnClose>\s*([0-9]+)\s*</myShowQuitDialogOnClose>')
    if ($mq.Success) { $quit = [int]$mq.Groups[1].Value }

    $opts = $null
    $mo = [regex]::Match($SessionSettingsXml, '<mySessionOptions>\s*(-?[0-9]+)\s*</mySessionOptions>')
    if ($mo.Success) { $opts = [int]$mo.Groups[1].Value }

    $sessionDialogs = $null
    if ($null -ne $opts) { $sessionDialogs = ((($opts -band $script:VrfShowSessionDialogsFlag)) -ne 0) }

    $quitOff    = (($null -ne $quit) -and ($quit -eq 0))
    $sessionOff = (($null -ne $sessionDialogs) -and (-not $sessionDialogs))

    $quitText = 'ABSENT'
    if ($null -ne $quit) { $quitText = [string]$quit }
    $optText = 'ABSENT'
    if ($null -ne $opts) { $optText = [string]$opts }
    $sessText = 'ABSENT'
    if ($null -ne $sessionDialogs) {
        if ($sessionDialogs) { $sessText = 'SET - session prompts ON' } else { $sessText = 'CLEAR - session prompts OFF' }
    }

    return [pscustomobject]@{
        ShowQuitDialogOnClose = $quit
        SessionOptions        = $opts
        ShowSessionDialogs    = $sessionDialogs
        QuitPromptOff         = $quitOff
        SessionPromptOff      = $sessionOff
        Unattended            = ($quitOff -and $sessionOff)
        Summary               = ('myShowQuitDialogOnClose={0}; mySessionOptions={1}, DtShowSessionDialogs 0x10 {2}' -f $quitText, $optText, $sessText)
    }
}

# Return the SETTINGS TEXT with one prompt turned off, plus what changed. Nothing is
# written here. The Application edit pins the value to 0; the SessionSettings edit is a
# MASK (-band -bnot 0x10) so every other flag in the word survives untouched. Both
# replace AT MOST ONE occurrence: these files carry the key once, and a second occurrence
# would mean a file shape nobody has seen - rewriting it blind is how a settings tree
# gets silently corrupted.
function Set-VrfGuiPromptSettingsText {
    param(
        [Parameter(Mandatory)][ValidateSet('Application', 'SessionSettings')][string]$Kind,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Text
    )
    $out     = $Text
    $changed = $false
    $found   = $false
    $before  = ''
    $after   = ''
    if ($Kind -eq 'Application') {
        $rx = [regex]'(?<open><myShowQuitDialogOnClose>\s*)(?<val>[0-9]+)(?<close>\s*</myShowQuitDialogOnClose>)'
        $m  = $rx.Match($Text)
        if ($m.Success) {
            $found  = $true
            $before = $m.Groups['val'].Value
            $after  = '0'
            if ($before -ne '0') { $out = $rx.Replace($Text, '${open}0${close}', 1); $changed = $true }
        }
    } else {
        $rx = [regex]'(?<open><mySessionOptions>\s*)(?<val>-?[0-9]+)(?<close>\s*</mySessionOptions>)'
        $m  = $rx.Match($Text)
        if ($m.Success) {
            $found = $true
            $v     = [int]$m.Groups['val'].Value
            $nv    = ($v -band (-bnot $script:VrfShowSessionDialogsFlag))
            $before = [string]$v
            $after  = [string]$nv
            if ($nv -ne $v) { $out = $rx.Replace($Text, ('${open}' + $nv + '${close}'), 1); $changed = $true }
        }
    }
    return [pscustomobject]@{
        Text     = $out
        Changed  = $changed
        KeyFound = $found
        Before   = $before
        After    = $after
    }
}

# ---- STP-825 holder join detection: robust to the garbled rtiexec log sink -------------
# Some MAK rtiexec 5.0.1 instances write this log through an UNSERIALISED sink that DOUBLES
# and INTERLEAVES text token-by-token WITHIN A LINE (the garbling never crosses a line).
# Observed verbatim (runs\launch52\rtiexec_20260921T032248Z5.0.1-...-47636.log line 10685,
# holder pid 87404, federation MAK-ONE-2025):
#   Federate Federate remoteControl 87404 ("remoteControl" 3)remoteControl 87404
#   ("remoteControl" 3) has joined federation " has joined federation "MAK-ONE-2025MAK-ONE-2025".
# so the clean line the vendor documents (RunC2SimScenario.ps1's Stage 2h header)
#   Federate remoteControl <pid> ("remoteControl" 2) has joined federation "<name>".
# never appears contiguously, and a detector that requires it verbatim reports a real join
# as a timeout (STP-825 D8, 2026-09-21 - all four holders joined and were refused anyway).
# The three conditions below are checked IN ORDER on the SAME line, each anchored past the
# previous one's match, so a doubled/truncated repeat of the federation name still satisfies
# the third without requiring a closing quote, while a line for a DIFFERENT pid or a
# DIFFERENT federation still fails:
#   1) "remoteControl" followed by ProcessId as a whole number (\b keeps 8740 from matching
#      inside 87404 - the boundary before the number is automatic after \s+).
#   2) the phrase "has joined federation" somewhere after that.
#   3) FederationName somewhere after THAT (a repeated or truncated copy, e.g.
#      "MAK-ONE-2025MAK-ONE-2025" or "MAK-ONE-2025MAK-", still contains it once).
function Test-HolderJoinedInLog {
    param(
        [AllowNull()][AllowEmptyString()][string]$LogDelta,
        [Parameter(Mandatory)][int]$ProcessId,
        [Parameter(Mandatory)][string]$FederationName
    )
    if ([string]::IsNullOrEmpty($LogDelta)) { return $false }
    $lineRe = ('remoteControl\s+{0}\b.*has joined federation.*{1}' -f `
                [regex]::Escape([string]$ProcessId), [regex]::Escape($FederationName))
    foreach ($line in ($LogDelta -split "`r?`n")) {
        if ([string]::IsNullOrEmpty($line)) { continue }
        if ($line -match $lineRe) { return $true }
    }
    return $false
}

# The last -MaxLines lines (file order) that mention ProcessId at all, whether or not they
# parse as a join - the operator-visible half of the fallback below: when the strict matcher
# above finds nothing but the holder is still alive, showing these at once beats a bare
# "did not join within 45s" (the pid may well be joined, just too garbled to confirm
# strictly). Always returns an array (never $null), per this file's Set-StrictMode -Version
# Latest and the @(...) call-site convention used throughout (see Get-OtherRunnerProcessInfo).
function Get-HolderPidLogLines {
    param(
        [AllowNull()][AllowEmptyString()][string]$LogDelta,
        [Parameter(Mandatory)][int]$ProcessId,
        [int]$MaxLines = 5
    )
    $out = @()
    if ([string]::IsNullOrEmpty($LogDelta)) { return $out }
    $pidRe = ('remoteControl\s+{0}\b' -f [regex]::Escape([string]$ProcessId))
    foreach ($line in ($LogDelta -split "`r?`n")) {
        if ([string]::IsNullOrEmpty($line)) { continue }
        if ($line -match $pidRe) { $out += $line }
    }
    if ($out.Count -gt $MaxLines) { $out = @($out[($out.Count - $MaxLines)..($out.Count - 1)]) }
    return $out
}

