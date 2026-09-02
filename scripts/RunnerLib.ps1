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
#   lineCount       : TASKCMPLT lines seen at the latest poll
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
    $State.lineCount = $inOrder.Count
    $all = ($Taskees.Count -gt 0)
    foreach ($u in $Taskees) { if ($completed -notcontains $u) { $all = $false } }
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
# with "Route '<r>' created; MoveAlongRoute|PatrolRoute issued for VRF_UUID:<u>",
# VrfC2SimService.cs:1102/1123). A taskee that can not be mapped (a task type that
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
    $rxB = [regex]"Route '(?<route>[^']*)' created; (?:MoveAlongRoute|PatrolRoute) issued for (?<vrf>VRF_UUID:[0-9a-fA-F-]{36})"
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

# Condition (4). Returns AllSatisfied plus one record per taskee explaining why.
function Test-ReportEvidence {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Taskees,
        [Parameter(Mandatory)]$TaskeeNames,      # taskee uuid -> marking (Get-InitUnitNames)
        [Parameter(Mandatory)]$NameToVrfUuid,    # marking -> VRF_UUID (Get-VrfUuidByName)
        [Parameter(Mandatory)][AllowNull()][AllowEmptyString()][string]$TraceText,
        [Parameter(Mandatory)][double]$ToleranceMeters
    )
    $ev = Get-TraceEvidence -TraceText $TraceText
    $per = [ordered]@{}
    $all = ($Taskees.Count -gt 0)
    foreach ($u in $Taskees) {
        $rec = [ordered]@{ name = $null; vrfUuid = $null; completionT = $null; lastRptT = $null
                           posT = $null; distanceM = $null; satisfied = $false; reason = $null }
        $name = if ($TaskeeNames.Contains($u)) { [string]$TaskeeNames[$u] } else { $null }
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
                if ($rec.distanceM -le $ToleranceMeters) { $rec.satisfied = $true; $rec.reason = 'post-completion RPT agrees with POS' }
                else { $rec.reason = ('post-completion RPT is {0} m from the latest POS (tolerance {1} m)' -f $rec.distanceM, $ToleranceMeters) }
            }
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
