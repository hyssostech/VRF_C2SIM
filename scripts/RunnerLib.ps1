# scripts/RunnerLib.ps1 - PURE helpers for scripts/RunC2SimScenario.ps1.
#
# Dot-sourced by the runner AND by tests/RunnerTurnaround.Tests.ps1. Everything in
# here is side-effect free (no process start, no file write, no sleep) so the
# runner's turnaround logic - the observer duration cap, the -StopWhenComplete
# early-exit criterion, the trace stop-file timing and the tool capability probe
# parse - can be exercised WITHOUT a simulator (docs/RUNNER_TURNAROUND_2026-09-01.md).
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

# The DECISION. ShouldClose is true only when ALL-COMPLETE has held for at least
# SettleHoldSecs. The hold exists so the movement gate (static -> moving -> settled,
# POS/RPT agreement, HEADLESS_RUN_PLAN 4a.1 "settled" = <10 m over 3 samples) still
# gets a post-completion plateau in the trace; TrailSecs is added on top by the
# teardown. Zero taskees => never closes (the window then runs to its RunSecs cap).
function Test-EarlyExit {
    param(
        [Parameter(Mandatory)][hashtable]$State,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Taskees,
        [Parameter(Mandatory)][int]$SettleHoldSecs,
        [Parameter(Mandatory)][datetime]$NowUtc
    )
    $missing = @($Taskees | Where-Object { -not $State.firstSeenUtc.Contains($_) })
    $all  = ($Taskees.Count -gt 0 -and $null -ne $State.allCompleteUtc)
    $held = 0.0
    if ($all) { $held = ($NowUtc - [datetime]$State.allCompleteUtc).TotalSeconds }
    return [pscustomobject]@{
        AllComplete     = $all
        Missing         = $missing
        HoldElapsedSecs = [Math]::Round($held, 1)
        ShouldClose     = ($all -and $held -ge $SettleHoldSecs)
    }
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
