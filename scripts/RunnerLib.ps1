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
# The app logs exactly one line per task-complete report it SENDS
# (src/VrfC2SimApp/VrfC2SimService.cs:1244):
#     SENT TASK STATUS REPORT (TASKCMPLT) taskee=<uuid> task=<uuid|(none)>.
# This is the LIVE completion source. reports-captured.log is NOT usable here:
# tools/ListenReports writes it once, at exit (ListenReports/Program.cs, after the
# listen window), so during the observation window it does not exist yet.
# Returns one (Taskee, Task) record per line, in log order, duplicates kept - the
# count matters (see Test-EarlyExit).
function Get-CompletedTasks {
    param([string]$AppLogText)
    $out = @()
    if ([string]::IsNullOrWhiteSpace($AppLogText)) { return $out }
    $rx = [regex]'SENT TASK STATUS REPORT \(TASKCMPLT\) taskee=(?<taskee>[0-9A-Fa-f-]{36}) task=(?<task>\S+?)\.?\s*$'
    foreach ($line in ($AppLogText -split "`r?`n")) {
        $m = $rx.Match($line)
        if (-not $m.Success) { continue }
        $out += [pscustomobject]@{ Taskee = $m.Groups['taskee'].Value; Task = $m.Groups['task'].Value }
    }
    return $out
}

# ---- early-exit state machine ------------------------------------------------
# State is a hashtable the caller owns across polls:
#   firstSeenUtc    : ordered map taskee -> UTC of the poll that FIRST saw its TASKCMPLT
#   lineCount       : RUNNING TOTAL of TASKCMPLT lines for order taskees across all
#                     polls. It was "lines seen at the latest poll" until 2026-09-14,
#                     when the runner's observation loop stopped re-reading the whole
#                     app log every 5 s and started feeding this function only the text
#                     APPENDED since the previous poll (Read-LiveDelta,
#                     docs/experiments/RUNNER_HARDENING_2026-09-14.md sec 5). Under a
#                     whole-file reader the two definitions coincide, so the change is
#                     invisible to a caller that still passes the whole log; under a
#                     delta reader only the running total is correct.
#   allCompleteUtc  : UTC of the poll that first satisfied the ALL-COMPLETE condition
# The app log carries NO timestamps, so "when did the last completion happen" is
# necessarily "the runner's poll that first saw it" - late by at most one poll
# interval, which only ever LENGTHENS the settle hold. Never shortens it.
function New-CompletionState {
    return @{
        firstSeenUtc   = [ordered]@{}
        lineCount      = 0
        allCompleteUtc = $null
    }
}

# ALL-COMPLETE = every distinct taskee in the order has at least one TASKCMPLT line
#                AND the number of TASKCMPLT lines FOR ORDER TASKEES is >= the number
#                of tasks in the order (so an order with two tasks for one taskee needs
#                two completions, without the runner having to attribute task uuids -
#                the app logs "(none)" for an unattributed completion).
# Lines whose taskee is NOT in the order (a unit tasked by someone else on the same
# server, or a stale report) are ignored entirely: they are neither stamped nor
# counted, so a stray line can never satisfy the count on behalf of an order task
# (review F2, docs/experiments/REVIEW_RUNNER_TURNAROUND_2026-09-01.md).
# NOTE (review F3): two tasks dispatched SIMULTANEOUSLY to one taskee are SUPERSEDED
# by VR-Forces (the old task never completes - VrfC2SimService.cs:954), so the
# count can never reach TaskCount and the early exit never fires; the window then
# runs to its cap, which is the safe direction. Sequenced (gated) tasks do complete.
function Update-CompletionState {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Taskees,
        [Parameter(Mandatory)][int]$TaskCount,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Completions,
        [Parameter(Mandatory)][datetime]$NowUtc
    )
    $inOrder   = @($Completions | Where-Object { $Taskees -contains $_.Taskee })
    $completed = @($inOrder | ForEach-Object { $_.Taskee } | Select-Object -Unique)
    foreach ($u in $completed) {
        if (-not $State.firstSeenUtc.Contains($u)) { $State.firstSeenUtc[$u] = $NowUtc }
    }
    $State.lineCount += $inOrder.Count
    # ALL-COMPLETE is asked of the ACCUMULATED set (firstSeenUtc), never of $completed,
    # which holds only the taskees seen in THIS call's input. With a whole-file reader the
    # two were the same set; with the delta reader added 2026-09-14 they are not, and using
    # $completed would make all-complete demand that every taskee re-report inside a single
    # poll - it would essentially never fire. Equivalent on whole-file input.
    $all = ($Taskees.Count -gt 0)
    foreach ($u in $Taskees) { if (-not $State.firstSeenUtc.Contains($u)) { $all = $false } }
    if ($all -and $State.lineCount -lt $TaskCount) { $all = $false }
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
    return [pscustomobject]@{
        AllComplete     = $all
        Missing         = $missing
        HoldElapsedSecs = [Math]::Round($held, 1)
        HoldElapsed     = ($all -and $held -ge $SettleHoldSecs)
        EvidenceIn      = $ReportEvidence
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
# reports-captured.log is written once at ListenReports exit, so it is NOT readable
# during the window (see Get-CompletedTasks), and the app log has no timestamps, so
# the TASKCMPLT poll stamp (UTC) can not be compared with a trace-clock RPT. The TSK
# record is the completion on the trace clock; it is used instead of the UTC stamp.
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
#                    than that taskee's TASKCMPLT (-CaptureEvidence). The AUTHORITY.
#   'R1-applog'      a complete R1 position-report round (>=1 sent, 0 skipped) logged
#                    AFTER this taskee's TASKCMPLT line (-AppLogPositionEvidence). The
#                    LIVE stand-in, because the capture is written only at exit.
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
        [AllowNull()]$CompletionUtcByTaskee = $null   # taskee uuid -> UTC of the poll that
                                                      # first saw its TASKCMPLT; the anchor
                                                      # of last resort for 'C2SIM-capture'
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
                $anchor = ([datetime]$CompletionUtcByTaskee[$u]); $anchorSrc = 'the runner poll that first saw TASKCMPLT'
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
