# StopVrf52.ps1 - bring VR-Forces 5.2d down GRACEFULLY, unattended, killing nothing.
#
# WHY A SEPARATE SCRIPT (2026-09-03): StopVrf.ps1 is written around the 5.0.2 COMBINED-MODE
# shutdown - it closes vrfGui and then drives, through UI Automation, the two modals that
# path raises ("Are You Sure?" with its "Quit All Back-Ends" checkbox, and the nested
# "Session Status" box). On 5.2 the two executables are launched INDEPENDENTLY
# (LaunchVrf52.ps1, UG52 4.1.2), so there is no "quit all back-ends" relationship to tick:
# the front-end's quit does not own the back-end, and the back-end has to be asked
# separately. Which modals a 5.2 vrfGui raises on close is NOT yet observed - the first
# live 5.2 teardown records it (this script LOGS every window title it can see rather than
# clicking anything it does not recognise, the RUNBOOK 0.5.9 "enumerate, never predict"
# rule). Nothing here is copied from StopVrf.ps1's UIA machinery until that evidence
# exists; guessing a button on an unknown modal can do something destructive.
#
# THE MECHANISM (both halves are GRACEFUL REQUESTS, never a kill):
#   1. CloseMainWindow() on vrfGui   - WM_CLOSE to its own main window.
#   2. after -GraceSec, taskkill /PID <backend> WITHOUT /F - documented as a request to
#      close (the process runs its own shutdown and can refuse); /F would TERMINATE a
#      JOINED FEDERATE and leave a stale federate behind, which is the project's single
#      most important rule (RUNBOOK sec 0). /F IS NEVER USED HERE, on any path.
#   3. wait out the remaining budget; report what is still up. Still-running is exit 3.
# rtiAssistant / rtiexec / rtiForwarder are RTI infrastructure: inventoried, never touched
# (RUNBOOK 0.5.2). Note the 5.2 profile launches assistant-free anyway (RTI_ASSISTANT_DISABLE
# + config/rid-501-rtiexec-min.mtl), so a running assistant there belongs to someone else -
# and its rtiexec/rtiForwarder are the federation's RENDEZVOUS, deliberately left up for the
# next run (StartRtiExec52.ps1 finds them and starts nothing).
#
# Exit codes (same contract as StopVrf.ps1, so the runner's teardown branch is unchanged):
#   0 = down, or already down, or a dry run completed
#   2 = bad arguments
#   3 = still running after the budget - NOTHING was killed; inspect before the next launch
#   4 = (reserved, as in StopVrf.ps1: a confirm dialog that could not be driven via UIA.
#        This script drives no dialog, so it never returns 4 - the code is kept unused so
#        the two scripts' contracts stay comparable.)
#   5 = unexpected terminating error - VR-Forces MAY STILL BE RUNNING
# ASCII only.
[CmdletBinding()]
param(
    # Total budget for the whole shutdown, including the grace below.
    [int]    $TimeoutSec             = 60,
    # How long the front-end's own close is given BEFORE the back-end is asked to close.
    # 20 s per the 5.2 teardown procedure; the back-end is never asked earlier.
    [int]    $GraceSec               = 20,
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Say      { param([string]$m) Write-Host $m }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Info { param([string]$m) Write-Host ('  [..]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }

# Arguments FIRST, before anything is touched (the LaunchVrf.ps1 "validated too late"
# defect). The runner pre-checks the same 5..600 range, so an exit 2 here is a caller bug.
if ($TimeoutSec -lt 5 -or $TimeoutSec -gt 600) { Say-Fail ("TimeoutSec must be between 5 and 600 (got {0})." -f $TimeoutSec); exit 2 }
if ($GraceSec -lt 1 -or $GraceSec -ge $TimeoutSec) {
    Say-Fail ("GraceSec must be 1..TimeoutSec-1 (got {0} with TimeoutSec {1}); it is spent INSIDE the total budget." -f $GraceSec, $TimeoutSec)
    exit 2
}

$procFrontend = 'vrfGui'
$procBackend  = 'vrfSimHLA1516e'
$procLauncher = 'vrfLauncher'
$rtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')

Say '=== StopVrf52.ps1 - unattended VR-Forces 5.2d shutdown (graceful, nothing killed) ==='
Say ('  TimeoutSec : {0} (total budget: front-end close + grace + back-end close)' -f $TimeoutSec)
Say ('  GraceSec   : {0} (front-end only, before the back-end is asked)' -f $GraceSec)
Say ('  DryRun     : {0}' -f [bool]$DryRun)

try {

# PowerShell UNROLLS a single-element array on return, so every call site re-wraps in @()
# (the StopVrf.ps1 lesson - '$fe + $be' fails on a bare [Process]).
function Get-Procs { param([string]$Name) return @(Get-Process -Name $Name -ErrorAction SilentlyContinue) }
function Describe-Proc {
    param($p)
    # Threads/MainWindowTitle throw on a process that exited between snapshot and access.
    try   { return ('{0} pid={1} threads={2} window="{3}"' -f $p.ProcessName, $p.Id, $p.Threads.Count, $p.MainWindowTitle) }
    catch { return ('{0} pid={1} (exited during inspection)' -f $p.ProcessName, $p.Id) }
}

Say ''
Say '=== Inventory ==='
$fe = @(Get-Procs $procFrontend)
$be = @(Get-Procs $procBackend)
$la = @(Get-Procs $procLauncher)
foreach ($p in @($fe + $be + $la)) { Say-Ok (Describe-Proc $p) }
foreach ($n in $rtiNames) {
    foreach ($p in @(Get-Procs $n)) { Say-Ok ('{0} pid={1} - RTI infrastructure, WILL BE PRESERVED' -f $p.ProcessName, $p.Id) }
}
if ($fe.Count -eq 0 -and $be.Count -eq 0 -and $la.Count -eq 0) {
    Say-Ok 'no VR-Forces processes running - nothing to do.'
    exit 0
}

if ($DryRun) {
    Say ''
    Say '=== Dry run - what WOULD happen for the state above ==='
    foreach ($p in @($fe + $la)) { Say-Ok ('would call CloseMainWindow on {0} pid={1}' -f $p.ProcessName, $p.Id) }
    if ($fe.Count -eq 0 -and $la.Count -eq 0) { Say-Ok 'no front-end / launcher present, so no window would be closed' }
    foreach ($p in $be) { Say-Ok ('would wait {0}s, then run: taskkill /PID {1}   (NO /F - a graceful close request to a JOINED FEDERATE)' -f $GraceSec, $p.Id) }
    if ($be.Count -eq 0) { Say-Ok ('no {0} present, so no back-end close would be requested' -f $procBackend) }
    Say-Ok 'any window this script does not recognise would be LOGGED, never clicked. RTI processes untouched.'
    exit 0
}

# ---- 1. front-end (and a stray launcher): its own main window, WM_CLOSE --------
Say ''
Say '=== Close the front-end ==='
foreach ($p in @($fe + $la)) {
    try {
        $null = $p.CloseMainWindow()
        Say-Ok ('CloseMainWindow sent to {0} pid={1}' -f $p.ProcessName, $p.Id)
    } catch {
        Say-Warn ('CloseMainWindow on {0} pid={1} failed: {2} (not fatal; the back-end is still asked below)' -f $p.ProcessName, $p.Id, $_.Exception.Message)
    }
}

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$graceEnd = (Get-Date).AddSeconds($GraceSec)
while ((Get-Date) -lt $graceEnd) {
    if (@(Get-Procs $procFrontend).Count -eq 0 -and @(Get-Procs $procLauncher).Count -eq 0) { break }
    Start-Sleep -Seconds 2
}
foreach ($p in @(Get-Procs $procFrontend)) {
    # ENUMERATE, NEVER PREDICT (RUNBOOK 0.5.9): a front-end still up after the grace is
    # most likely sitting on a modal. Record what can be seen - an EMPTY title is itself a
    # signature (a modal, or an elevated window this session cannot see) - and click NOTHING.
    $t = try { $p.MainWindowTitle } catch { '(inaccessible)' }
    Say-Warn ('{0} pid={1} still up after the {2}s grace - window title: "{3}". NOT clicked, NOT killed; record it (this is the evidence the first live 5.2 teardown owes).' -f $p.ProcessName, $p.Id, $GraceSec, $t)
}

# ---- 2. back-end: taskkill WITHOUT /F = a close REQUEST ------------------------
Say ''
Say '=== Ask the back-end to close (taskkill, NO /F) ==='
foreach ($p in @(Get-Procs $procBackend)) {
    try {
        $out = & taskkill /PID $p.Id 2>&1
        Say-Ok ('taskkill /PID {0} (no /F): {1}' -f $p.Id, (($out | Out-String).Trim()))
    } catch {
        Say-Warn ('taskkill /PID {0} failed: {1}' -f $p.Id, $_.Exception.Message)
    }
}
while ((Get-Date) -lt $deadline) {
    if (@(Get-Procs $procBackend).Count -eq 0 -and @(Get-Procs $procFrontend).Count -eq 0 -and
        @(Get-Procs $procLauncher).Count -eq 0) { break }
    Start-Sleep -Seconds 2
}

# ---- 3. verdict ---------------------------------------------------------------
Say ''
Say '=== Result ==='
$left = @()
foreach ($n in @($procFrontend, $procBackend, $procLauncher)) {
    foreach ($p in @(Get-Procs $n)) { $left += ('{0}(pid {1})' -f $p.ProcessName, $p.Id) }
}
$rtiLeft = @()
foreach ($n in $rtiNames) { foreach ($p in @(Get-Procs $n)) { $rtiLeft += ('{0}(pid {1})' -f $p.ProcessName, $p.Id) } }
if ($rtiLeft.Count -gt 0) { Say-Ok ('RTI infrastructure preserved (correct): {0}' -f ($rtiLeft -join ', ')) }
if ($left.Count -eq 0) {
    Say-Ok 'VR-Forces 5.2d is down (graceful; nothing was killed).'
    exit 0
}
Say-Fail ('still running after {0}s: {1}. NOTHING WAS FORCE-KILLED - a force-killed joined federate leaves a stale federate and the next join hangs (RUNBOOK sec 0). Inspect the screen for a modal before the next launch.' -f $TimeoutSec, ($left -join ', '))
exit 3

}
catch {
    # Never let a terminating error surface as the bare exit 1, which is indistinguishable
    # from a generic failure at the worst moment - mid-shutdown, possibly with a modal up.
    Say-Fail ('unexpected terminating error: {0}' -f $_.Exception.Message)
    Say-Fail 'VR-FORCES MAY STILL BE RUNNING. Nothing was force-killed. Inspect before the next launch.'
    exit 5
}
