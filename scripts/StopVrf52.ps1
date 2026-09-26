# StopVrf52.ps1 - bring VR-Forces 5.2d down GRACEFULLY, unattended; force ONLY the run's own
# back end, identified by pid + start time, and only after its graceful close was refused.
#
# A3 (lane A2, 2026-09-26) - WHAT CHANGED AND ON WHOSE AUTHORITY. Both live runs of 2026-09-26
# (20260926T115957Z, 20260926T181639Z) ended at exit 3: `taskkill /PID` (no /F) said "SUCCESS"
# and the back end ran on for the whole budget; the SEAT then force-stopped the run's own back end
# by pid, twice, on the owner's standing "initiative" direction (feedback memory 2026-09-26).
# This script now does exactly that, and nothing wider:
#   (a) TEARDOWN DIAGNOSTICS before any close request: MainWindowTitle, MainWindowHandle, the
#       Win32_Process parent and any conhost / OpenConsole / WindowsTerminal parent or child.
#       Every run that closed on record shows window="...vrfSimHLA1516e.exe" (a console); both
#       refusals show window="" (lane A report, A3) - these lines are the discriminator.
#   (b) The vendor's own exit - "To exit a simulation engine from the console window, press Q
#       and then Enter" (UG52 4.6 p146) - is NOT DELIVERABLE from here: LaunchVrf52.ps1 starts
#       the back end with a plain Start-Process (no -RedirectStandardInput), so its console input
#       is not ours to write. The close request stays taskkill WITHOUT /F.
#   (c) If the back end is still up at the end of the budget AND the caller passed its recorded
#       -ForceOwnBackendPid + -ForceOwnBackendStartUtc AND the live process matches BOTH
#       (RunnerLib Test-OwnBackendIdentity), it is Stop-Process -Id -Force'd and the log says
#       "FORCED - graceful close refused (see diagnostics)"; exit 6. rtiexec, rtiForwarder,
#       rtiAssistant, RtiProbe (the holder) and vrfGui are NEVER forced. Without the pair (the
#       RunnerWatchdog and manual path) nothing is forced and a refusal is still exit 3.
# RESIDUAL RISK, stated: a force-stopped back end is a JOINED federate that did not resign, which
# is what RUNBOOK sec 0 warns leaves a STALE FEDERATE in the long-lived rtiexec. The seat's two
# force-stops are the only evidence so far; the next launch's join is the check.
#
# WHY A SEPARATE SCRIPT (2026-09-03): StopVrf.ps1 is written around the 5.0.2 COMBINED-MODE
# shutdown - it closes vrfGui and then drives, through UI Automation, the two modals that
# path raises ("Are You Sure?" with its "Quit All Back-Ends" checkbox, and the nested
# "Session Status" box). On 5.2 the two executables are launched INDEPENDENTLY
# (LaunchVrf52.ps1, UG52 4.1.2), so there is no "quit all back-ends" relationship to tick:
# the front-end's quit does not own the back-end, and the back-end has to be asked
# separately.
#
# WHICH MODALS A 5.2 vrfGui RAISES ON CLOSE - ANSWERED 2026-09-20 (STP-844). Demo
# rehearsal D1 (run 20260920T172141Z) was the first GUI-ON 5.2 teardown ever run. It timed
# out at exit 3 and a READ-ONLY window enumeration of the surviving pid found TWO stacked
# never-ask-again message boxes, both class makVrf::DtNeverAskAgainMessageBox, both owned
# by the GUI main window:
#   1. "Are You Sure?"  / "Quit VR-Forces GUI"   [Yes][No] + "Quit All Sim Engines"
#      = the UG52 4.6 exit prompt, raised by the WM_CLOSE this script sends.
#   2. "Session Status" / "The current session has ended. Close current terrain?"
#      [Yes][No] + "Execute session changes without prompting."
#      = raised by the SESSION ending while modal 1 is still open, i.e. by THIS SCRIPT's
#        own ordering (front end first, back end -GraceSec later). We manufacture it.
# THE FIX IS CONFIGURATION, NOT AUTOMATION, and it is not in this script: both boxes have
# a persisted setting (UG52 4.6.1 and UG52 4.3.1), both are turned off in a run-owned
# appData tree by scripts\NewVrfAppData52.ps1, and LaunchVrf52.ps1 -AppDataDir points the
# GUI at it. With the exit prompt gone the GUI should close on WM_CLOSE inside the grace,
# before the back end is asked - so modal 2 cannot arise either.
# STILL NO CLICKING HERE, DELIBERATELY. This script LOGS every window it can see and
# answers nothing (RUNBOOK 0.5.9 "enumerate, never predict"). The project goal is headless
# operation; a UI Automation answerer is not built, and an unrecognised modal must never
# be guessed at. What D1 exposed is that the LOGGING half had been lost in the 5.0.2 ->
# 5.2 split: StopVrf.ps1 prints every visible window a stuck process owns, StopVrf52.ps1
# printed only MainWindowTitle - which on a modal keeps reporting the MAIN window and so
# named nothing. That read-only half is restored below (Get-VrfWindows /
# Get-VrfNestedWindows / Get-DialogButtonNames, ported from StopVrf.ps1 with every
# Invoke/click path left behind).
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
# Exit codes (StopVrf.ps1's contract, plus 6):
#   0 = down, or already down, or a dry run completed
#   2 = bad arguments
#   3 = still running after the budget - NOTHING was killed; inspect before the next launch
#   4 = (reserved, as in StopVrf.ps1: a confirm dialog that could not be driven via UIA.
#        This script drives no dialog, so it never returns 4 - the code is kept unused so
#        the two scripts' contracts stay comparable.)
#   5 = unexpected terminating error - VR-Forces MAY STILL BE RUNNING
#   6 = FORCED: the graceful close was refused and the run's OWN back end (pid + start time
#       matched) was force-stopped; nothing else of VR-Forces is left. A refused close, scoreable.
# ASCII only.
[CmdletBinding()]
param(
    # Total budget for the whole shutdown, including the grace below.
    [int]    $TimeoutSec             = 60,
    # How long the front-end's own close is given BEFORE the back-end is asked to close.
    # 20 s per the 5.2 teardown procedure; the back-end is never asked earlier.
    [int]    $GraceSec               = 20,
    # A3: the run's OWN back end, as the runner recorded it at launch. BOTH or neither: the pair is
    # the identity (a pid alone can be reused). Given, a back end still up at the end of the budget
    # is force-stopped if - and only if - it matches both (exit 6). Not given: nothing is forced.
    [int]    $ForceOwnBackendPid     = 0,
    [string] $ForceOwnBackendStartUtc = '',
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

$ownStartUtc = $null
if (($ForceOwnBackendPid -gt 0) -xor (-not [string]::IsNullOrWhiteSpace($ForceOwnBackendStartUtc))) {
    Say-Fail '-ForceOwnBackendPid and -ForceOwnBackendStartUtc go TOGETHER (the pair is the identity; a pid alone can be reused).'
    exit 2
}
if ($ForceOwnBackendPid -gt 0) {
    try {
        $ownStartUtc = [datetime]::Parse($ForceOwnBackendStartUtc, [System.Globalization.CultureInfo]::InvariantCulture,
                                         [System.Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    } catch {
        Say-Fail ("-ForceOwnBackendStartUtc '{0}' is not an ISO-8601 instant." -f $ForceOwnBackendStartUtc)
        exit 2
    }
}

$procFrontend = 'vrfGui'
$procBackend  = 'vrfSimHLA1516e'
$procLauncher = 'vrfLauncher'
$rtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')

Say '=== StopVrf52.ps1 - unattended VR-Forces 5.2d shutdown (graceful; only the run''s own back end may be forced) ==='
Say ('  TimeoutSec : {0} (total budget: front-end close + grace + back-end close)' -f $TimeoutSec)
Say ('  GraceSec   : {0} (front-end only, before the back-end is asked)' -f $GraceSec)
Say ('  DryRun     : {0}' -f [bool]$DryRun)
if ($ForceOwnBackendPid -gt 0) {
    Say ('  OwnBackend : pid {0} started {1:o} - force-stopped ONLY if the graceful close is refused and BOTH match' -f $ForceOwnBackendPid, $ownStartUtc)
} else {
    Say '  OwnBackend : (not given) - NO FORCE on any path'
}

# The identity match lives in RunnerLib (pure, unit-tested by the suite, 10f). A load failure must
# never cost the graceful close: it only disables the force.
$script:OwnIdOk = $false
try {
    . (Join-Path $PSScriptRoot 'RunnerLib.ps1')
    $script:OwnIdOk = [bool](Get-Command Test-OwnBackendIdentity -ErrorAction SilentlyContinue)
} catch {
    Say-Warn ('RunnerLib.ps1 could not be loaded ({0}) - the identity match is unavailable, so NOTHING will be forced.' -f $_.Exception.Message)
}

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

# ---- READ-ONLY window diagnostic (STP-844) ------------------------------------
# The half of StopVrf.ps1 that D1 needed and 5.2 did not have. EVERYTHING here READS:
# EnumWindows + GetWindowText + IsWindowVisible + IsWindowEnabled, and UIA PROPERTY reads.
# There is no InvokePattern, no click and no SetForegroundWindow anywhere in this file - and
# there must not be. (The one Stop-Process is section 3b's identity-gated force of the run's own
# back end, A3; nothing in this diagnostic touches a process.) Why the bare MainWindowTitle was not enough: when a
# modal is up, .NET's MainWindowTitle keeps reporting the MAIN window's title, so D1's
# teardown logged the scenario name and named neither dialog (harvest sec 8, A-i).
# The UIA half is optional: if UIAutomationClient cannot be loaded (no desktop, a stripped
# host), the top-level EnumWindows half still runs. A diagnostic that refuses to run at
# all when one of its two halves is unavailable is worse than a partial one.
#
# NOTHING HERE MAY COST THE BACK END ITS CLOSE REQUEST (STP-844 review items 4+5). The
# whole script body runs inside ONE try whose catch exits 5, and Write-WindowDiagnostic is
# called BETWEEN the front-end grace and the back-end taskkill. So a throw anywhere in the
# diagnostic - a failed Add-Type on a host without a C# compiler, a UIA type that will not
# load, a PropertyCondition constructor that fails - would skip the back-end close
# entirely and leave a JOINED vrfSimHLA1516e behind. That is strictly worse than having no
# diagnostic at all. Therefore: the type loads are LAZY (nothing is compiled on a headless
# run, a -DryRun, or a run with no VR-Forces process), they are guarded, and every call
# site is wrapped so ANY failure degrades to one Say-Warn line and the teardown carries on
# unchanged.
$script:UiaOk    = $false
$script:WinApiOk = $false
$script:DiagInit = $false

# Load the two type surfaces on FIRST USE, once. Either half may fail on its own: without
# the P/Invoke class there is no window list at all, without UIA there are no nested
# windows or button names but the top-level list still works.
function Initialize-WindowDiagnostic {
    if ($script:DiagInit) { return }
    $script:DiagInit = $true
    try {
        Add-Type @'
using System; using System.Runtime.InteropServices; using System.Text;
public class StopVrf52Win {
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
  public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int m);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
}
'@
        $script:WinApiOk = $true
    } catch {
        Say-Warn ('window-enumeration types could not be compiled ({0}) - the teardown continues; only the process-level lines are available.' -f $_.Exception.Message)
    }
    try {
        Add-Type -AssemblyName UIAutomationClient
        Add-Type -AssemblyName UIAutomationTypes
        $script:UiaOk = $true
    } catch {
        Say-Warn ('UI Automation types unavailable ({0}) - the diagnostic will list TOP-LEVEL windows only, not nested dialogs or their buttons.' -f $_.Exception.Message)
    }
}

# Every TOP-LEVEL window owned by the VR-Forces processes still running. The callback runs
# in its own scope and can only WRITE to a script-scoped variable - a function-local would
# come back permanently empty, which is the exact bug StopVrf.ps1 records having shipped.
function Get-VrfWindows {
    $vrfPids = @()
    foreach ($n in @($procFrontend, $procBackend, $procLauncher)) {
        $vrfPids += @(Get-Process -Name $n -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
    }
    $script:vrfWindows = @()
    if (-not $script:WinApiOk) { return @() }
    if ($vrfPids.Count -eq 0) { return @() }
    $cb = [StopVrf52Win+EnumWindowsProc]{
        param($h, $l)
        $procId = 0
        [void][StopVrf52Win]::GetWindowThreadProcessId($h, [ref]$procId)
        if ($vrfPids -contains [int]$procId) {
            $sb = New-Object System.Text.StringBuilder 512
            [void][StopVrf52Win]::GetWindowText($h, $sb, 512)
            $t = $sb.ToString()
            if ($t) {
                $script:vrfWindows += [pscustomobject]@{
                    Handle  = $h
                    Pid     = [int]$procId
                    Title   = $t
                    Visible = [StopVrf52Win]::IsWindowVisible($h)
                    Enabled = [StopVrf52Win]::IsWindowEnabled($h)
                }
            }
        }
        return $true
    }
    [void][StopVrf52Win]::EnumWindows($cb, [IntPtr]::Zero)
    return @($script:vrfWindows)
}

# Windows NESTED inside those top-level windows. Companion to Get-VrfWindows, not a
# replacement: D1's two message boxes were BOTH reachable as top-level windows owned by
# the main window, but the 5.0.2 record has a "Session Status" box that was a DESCENDANT
# and invisible to EnumWindows. Deliberately NOT filtered to a known class or name
# (RUNBOOK 0.5.9): everything of ControlType Window is returned and the caller logs all of
# it, QDockWidgets included - their presence is what proves the scan really ran.
function Get-VrfNestedWindows {
    $found = @()
    if (-not $script:UiaOk) { return @($found) }
    # The CONSTRUCTOR and the two STATIC TYPE READS are guarded too, not just the per-element
    # access below: on review, these were the one unprotected throw left on the teardown path
    # (STP-844 review item 5). $script:UiaOk only says the assemblies loaded.
    $winCond = $null
    try {
        $winCond = New-Object System.Windows.Automation.PropertyCondition(
                       [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                       [System.Windows.Automation.ControlType]::Window)
    } catch { return @($found) }
    foreach ($w in @(Get-VrfWindows)) {
        # Any of these can throw if the window dies mid-scan. This runs DURING a shutdown,
        # so that is the normal case, not an exception: skip and move on.
        try { $root = [System.Windows.Automation.AutomationElement]::FromHandle($w.Handle) } catch { continue }
        if ($null -eq $root) { continue }
        try { $kids = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $winCond) } catch { continue }
        foreach ($k in $kids) {
            try {
                $found += [pscustomobject]@{
                    Element  = $k
                    Name     = $k.Current.Name
                    Class    = $k.Current.ClassName
                    Handle   = $k.Current.NativeWindowHandle
                    OwnerPid = $w.Pid
                }
            } catch { continue }
        }
    }
    return @($found)
}

# The buttons a dialog ACTUALLY exposes, as text. Two jobs: it is the evidence the next
# unknown modal gets diagnosed from, and its emptiness separates a real prompt from a
# QDockWidget - observed, not predicted. READ-ONLY: names and enabled state, no Invoke.
function Get-DialogButtonNames {
    param($element)
    $names = @()
    $btnCond = $null
    try {
        $btnCond = New-Object System.Windows.Automation.PropertyCondition(
                       [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                       [System.Windows.Automation.ControlType]::Button)
    } catch { return @() }
    try { $btns = $element.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond) } catch { return @() }
    foreach ($b in $btns) {
        try {
            if ($b.Current.IsEnabled) { $names += $b.Current.Name }
            else                      { $names += ('{0} (disabled)' -f $b.Current.Name) }
        } catch { continue }
    }
    return @($names)
}

# One report. $Why is printed first so the log says WHICH moment produced it (after the
# grace, or at the final timeout). Nothing is clicked; every line is something read.
function Write-WindowDiagnostic {
    param([string]$Why, [scriptblock]$Emit)
    Initialize-WindowDiagnostic
    if (-not $script:WinApiOk) {
        & $Emit ('WINDOW DIAGNOSTIC ({0}) SKIPPED - the window-enumeration types are not available in this session. The teardown is unaffected.' -f $Why)
        return
    }
    & $Emit ('WINDOW DIAGNOSTIC ({0}) - read-only: EnumWindows + UIA property reads. NOTHING IS CLICKED.' -f $Why)
    $tops = @(Get-VrfWindows)
    if ($tops.Count -eq 0) {
        & $Emit '  no titled top-level window is owned by any VR-Forces process.'
    } else {
        foreach ($w in $tops) {
            & $Emit ('  top-level: "{0}" (pid {1}, hwnd {2}, visible={3}, enabled={4})' -f $w.Title, $w.Pid, $w.Handle, $w.Visible, $w.Enabled)
        }
        # ENABLED=True on exactly one window while the main window is False is the modal
        # signature: that one is the box on top, and it is the one blocking the close.
        $onTop = @($tops | Where-Object { $_.Visible -and $_.Enabled })
        if ($onTop.Count -gt 0) {
            & $Emit ('  ON TOP (visible AND enabled - this is what is waiting for an answer): {0}' -f (($onTop | ForEach-Object { '"' + $_.Title + '"' }) -join ', '))
        }
    }
    if (-not $script:UiaOk) {
        & $Emit '  nested-window scan SKIPPED: UI Automation types could not be loaded in this session.'
        return
    }
    $nested = @(Get-VrfNestedWindows)
    if ($nested.Count -eq 0) { & $Emit '  no nested ControlType=Window elements found.' }
    foreach ($d in $nested) {
        $btn = ((Get-DialogButtonNames -element $d.Element) -join ', ')
        & $Emit ('  nested: class="{0}" name="{1}" hwnd={2} pid={3} buttons=[{4}]' -f $d.Class, $d.Name, $d.Handle, $d.OwnerPid, $btn)
    }
    & $Emit '  KNOWN (STP-844, D1): class makVrf::DtNeverAskAgainMessageBox named "Are You Sure?" is the UG52 4.6 exit prompt and "Session Status" is the session-ended / close-terrain prompt. Both are suppressed by CONFIGURATION (scripts\NewVrfAppData52.ps1 + LaunchVrf52 -AppDataDir), never by clicking them from here.'
}

# ---- A3 TEARDOWN DIAGNOSTICS - read-only, BEFORE the close request ---------------
# What the 2026-09-26 refusals lacked in their own log: does the back end own a console window,
# and who hosts its console? Every close on record had window="...vrfSimHLA1516e.exe"; both
# refusals had window="" (lane A report, A3). Candidates for the change: the Windows Terminal
# default-terminal handoff (the console hosted by OpenConsole under WindowsTerminal) or a harness
# whose children get no classic conhost. Guarded end to end: a failure here costs one WARN line,
# never the close request.
function Write-TeardownDiagnostic {
    param([int[]]$Pids)
    foreach ($id in @($Pids)) {
        try {
            $p = Get-Process -Id $id -ErrorAction Stop
            $st = try { $p.StartTime.ToUniversalTime().ToString('o') } catch { '(unreadable)' }
            Say-Info ('TEARDOWN DIAGNOSTIC pid={0}: MainWindowTitle="{1}" MainWindowHandle={2} StartTime(UTC)={3}' -f $id, $p.MainWindowTitle, $p.MainWindowHandle, $st)
        } catch {
            Say-Warn ('TEARDOWN DIAGNOSTIC pid={0}: process not readable ({1})' -f $id, $_.Exception.Message)
            continue
        }
        try {
            $me = Get-CimInstance -ClassName Win32_Process -Filter ('ProcessId={0}' -f $id) -ErrorAction Stop
            $parent = if ($me) { Get-CimInstance -ClassName Win32_Process -Filter ('ProcessId={0}' -f $me.ParentProcessId) -ErrorAction SilentlyContinue } else { $null }
            $kids = @(Get-CimInstance -ClassName Win32_Process -Filter ('ParentProcessId={0}' -f $id) -ErrorAction SilentlyContinue)
            $hostRx = '^(conhost|OpenConsole|WindowsTerminal)\.exe$'
            Say-Info ('  parent: ParentProcessId={0} name={1}{2}' -f $(if ($me) { $me.ParentProcessId } else { '?' }),
                      $(if ($parent) { $parent.Name } else { '(gone)' }),
                      $(if ($parent -and $parent.Name -match $hostRx) { '  <- A CONSOLE HOST' } else { '' }))
            $kidText = if ($kids.Count -eq 0) { '(none)' } else { ($kids | ForEach-Object { '{0}({1})' -f $_.Name, $_.ProcessId }) -join ', ' }
            $kidHosts = @($kids | Where-Object { $_.Name -match $hostRx })
            Say-Info ('  children: {0}  -> console host child: {1}' -f $kidText, $(if ($kidHosts.Count -gt 0) { 'YES' } else { 'NO' }))
            $wt = @(Get-CimInstance -ClassName Win32_Process -Filter "Name='WindowsTerminal.exe' OR Name='OpenConsole.exe'" -ErrorAction SilentlyContinue)
            Say-Info ('  WindowsTerminal/OpenConsole on this machine: {0}' -f $(if ($wt.Count -eq 0) { '(none)' } else { ($wt | ForEach-Object { '{0}({1}, parent {2})' -f $_.Name, $_.ProcessId, $_.ParentProcessId }) -join ', ' }))
            if ([string]::IsNullOrEmpty($p.MainWindowTitle) -and $kidHosts.Count -eq 0) {
                Say-Warn '  NO console window and NO console-host child: the 2026-09-26 refusal precondition (A3) is PRESENT - taskkill without /F may have nothing that ends this process.'
            }
        } catch {
            Say-Warn ('  console-host parentage not readable ({0}) - the teardown continues.' -f $_.Exception.Message)
        }
    }
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
    # A dry run still prints its PLAN (lane A2): otherwise the plan can only be checked on a
    # machine with a simulator up, and the suite's 10d could only ever SKIP.
    if (-not $DryRun) { exit 0 }
}

if ($DryRun) {
    Say ''
    Say '=== Dry run - what WOULD happen for the state above ==='
    foreach ($p in @($fe + $la)) { Say-Ok ('would call CloseMainWindow on {0} pid={1}' -f $p.ProcessName, $p.Id) }
    if ($fe.Count -eq 0 -and $la.Count -eq 0) { Say-Ok 'no front-end / launcher present, so no window would be closed' }
    foreach ($p in $be) { Say-Ok ('would wait {0}s, then run: taskkill /PID {1}   (NO /F - a graceful close request to a JOINED FEDERATE)' -f $GraceSec, $p.Id) }
    if ($be.Count -eq 0) { Say-Ok ('no {0} present, so no back-end close would be requested' -f $procBackend) }
    Say-Ok 'would REPORT CloseMainWindow()''s return value (STP-844: D1 discarded it, so "the prompt opened" could not be told from "the window was already disabled").'
    Say-Ok 'would run the READ-ONLY WINDOW DIAGNOSTIC after the grace and again at a timeout: every titled top-level window (title, visible, enabled) plus every nested ControlType=Window with its class, name and BUTTON NAMES.'
    Say-Ok 'would record TEARDOWN DIAGNOSTICS before any close request: each back end''s MainWindowTitle and MainWindowHandle, its Win32_Process parent (ParentProcessId) and any conhost.exe / OpenConsole.exe / WindowsTerminal.exe parent or child - the A3 discriminator (window="" on both 2026-09-26 refusals).'
    Say-Ok 'console exit (UG52 4.6 p146 "press Q and then Enter") is NOT DELIVERABLE headless: LaunchVrf52.ps1 starts the back end with a plain Start-Process (no -RedirectStandardInput), so its console input is not this script''s to write. The close request stays taskkill WITHOUT /F.'
    if ($ForceOwnBackendPid -gt 0) {
        Say-Ok ('would FORCE-STOP pid {0} ONLY if it is still up after the {1}s budget AND it is vrfSimHLA1516e started at {2:o} (Test-OwnBackendIdentity: pid + start time); logged "FORCED - graceful close refused (see diagnostics)", exit 6. rtiexec / rtiForwarder / rtiAssistant / RtiProbe / vrfGui: never.' -f $ForceOwnBackendPid, $TimeoutSec, $ownStartUtc)
    } else {
        Say-Ok 'NO FORCE: -ForceOwnBackendPid / -ForceOwnBackendStartUtc not given, so a refused close ends at exit 3 with nothing killed (the watchdog and manual path).'
    }
    Say-Ok 'any window this script does not recognise would be LOGGED, never clicked. RTI processes untouched.'
    exit 0
}

# ---- 1. front-end (and a stray launcher): its own main window, WM_CLOSE --------
Say ''
Say '=== Close the front-end ==='
foreach ($p in @($fe + $la)) {
    try {
        # KEEP THE BOOLEAN (STP-844). D1 threw it away (`$null = ...`) and that one bit is
        # the difference between "the close request landed and opened a prompt" and "the
        # main window was already DISABLED by a modal, so WM_CLOSE was never posted" -
        # .NET returns false in the second case and the log looked identical either way.
        # On D1 it turned out to be TRUE (the enumeration found the exit prompt the close
        # had opened), but that was established two hours later by hand, not from the log.
        $sent = $p.CloseMainWindow()
        if ($sent) {
            Say-Ok ('CloseMainWindow sent to {0} pid={1} - returned TRUE (the close request was posted to its main window)' -f $p.ProcessName, $p.Id)
        } else {
            Say-Warn ('CloseMainWindow on {0} pid={1} returned FALSE - the main window did not accept it. The usual cause is that a MODAL IS ALREADY UP and has disabled the main window, so the close was never posted; the window diagnostic below names what is on screen.' -f $p.ProcessName, $p.Id)
        }
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
$stillUpAfterGrace = @(Get-Procs $procFrontend)
foreach ($p in $stillUpAfterGrace) {
    # ENUMERATE, NEVER PREDICT (RUNBOOK 0.5.9): a front-end still up after the grace is
    # most likely sitting on a modal. Record what can be seen and click NOTHING. Note
    # MainWindowTitle is NOT sufficient on its own - with a modal up it keeps reporting
    # the MAIN window's title, which on D1 was the scenario name and named no dialog.
    $t = try { $p.MainWindowTitle } catch { '(inaccessible)' }
    Say-Warn ('{0} pid={1} still up after the {2}s grace - MainWindowTitle: "{3}". NOT clicked, NOT killed.' -f $p.ProcessName, $p.Id, $GraceSec, $t)
}
if ($stillUpAfterGrace.Count -gt 0) {
    # Run the diagnostic HERE as well as at the timeout: at this moment the back end has
    # not been asked to close yet, so whatever is on screen was raised by the GUI's own
    # quit path alone. That distinction is exactly what D1 could not make afterwards -
    # its second modal ("Session Status") was raised by the back end going away 20 s
    # later, and only a report taken BEFORE that can separate the two.
    # THE BACK-END CLOSE MUST NOT DEPEND ON THIS (STP-844 review item 4). The next section
    # is the only thing that asks a JOINED vrfSimHLA1516e to close; an exception escaping
    # here would reach the outer catch, exit 5, and leave that federate running. A missing
    # diagnostic costs a future investigation; a skipped back-end close costs the next run.
    try {
        Write-WindowDiagnostic -Why ('after the ' + $GraceSec + 's grace, BEFORE the back end is asked to close') -Emit ${function:Say-Warn}
    } catch {
        Say-Warn ('the window diagnostic failed ({0}) - IGNORED, the teardown continues to the back-end close.' -f $_.Exception.Message)
    }
}

# ---- 2. back-end: taskkill WITHOUT /F = a close REQUEST ------------------------
Say ''
Say '=== Teardown diagnostics (A3), read-only, BEFORE the close request ==='
try {
    Write-TeardownDiagnostic -Pids @(@(Get-Procs $procBackend) | ForEach-Object { $_.Id })
} catch {
    Say-Warn ('teardown diagnostics failed ({0}) - IGNORED, the close request follows.' -f $_.Exception.Message)
}
Say-Info 'console exit (UG52 4.6 p146 "press Q and then Enter") NOT DELIVERABLE: the runner does not own the back end''s console input (LaunchVrf52.ps1 plain Start-Process). Using taskkill without /F.'

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

# ---- 3b. A3: the graceful close was REFUSED - force the run's OWN back end, and nothing else ----
$forced = @()
if ($ForceOwnBackendPid -gt 0) {
    try {
        Write-WindowDiagnostic -Why ('TIMEOUT after ' + $TimeoutSec + 's, BEFORE any force') -Emit ${function:Say-Fail}
    } catch {
        Say-Fail ('the window diagnostic failed ({0}) - IGNORED.' -f $_.Exception.Message)
    }
    if (-not $script:OwnIdOk) {
        Say-Fail 'the identity match is unavailable (RunnerLib.ps1 not loaded) - NOTHING is forced.'
    } else {
        foreach ($p in @(Get-Procs $procBackend)) {
            $st = try { $p.StartTime.ToUniversalTime() } catch { $null }
            # A failure of the MATCH is a refusal, never a force - and never a terminating error
            # that would turn "refused, nothing killed" (3) into "unexpected error" (5).
            $v = try {
                Test-OwnBackendIdentity -ExpectedPid $ForceOwnBackendPid -ExpectedStartUtc $ownStartUtc `
                    -ActualPid $p.Id -ActualStartUtc $st -ActualName $p.ProcessName
            } catch {
                [pscustomobject]@{ Match = $false; Why = ('the identity check failed ({0}) - refused' -f $_.Exception.Message) }
            }
            if (-not $v.Match) {
                Say-Fail ('NOT forced: {0} pid={1} - {2}' -f $p.ProcessName, $p.Id, $v.Why)
                continue
            }
            Say-Fail ('FORCED - graceful close refused (see diagnostics): Stop-Process -Id {0} -Force on {1} ({2}). A force-stopped JOINED federate may leave a STALE FEDERATE in rtiexec (RUNBOOK sec 0) - the next launch''s join is the check.' -f $p.Id, $p.ProcessName, $v.Why)
            Stop-Process -Id $p.Id -Force -ErrorAction Continue
            $forced += $p.Id
        }
    }
    if ($forced.Count -gt 0) {
        $forceDeadline = (Get-Date).AddSeconds(15)
        while ((Get-Date) -lt $forceDeadline -and @($forced | Where-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue }).Count -gt 0) {
            Start-Sleep -Seconds 1
        }
        $left = @()
        foreach ($n in @($procFrontend, $procBackend, $procLauncher)) {
            foreach ($p in @(Get-Procs $n)) { $left += ('{0}(pid {1})' -f $p.ProcessName, $p.Id) }
        }
        $rtiLeft = @()
        foreach ($n in $rtiNames) { foreach ($p in @(Get-Procs $n)) { $rtiLeft += ('{0}(pid {1})' -f $p.ProcessName, $p.Id) } }
        if ($rtiLeft.Count -gt 0) { Say-Ok ('RTI infrastructure preserved (correct): {0}' -f ($rtiLeft -join ', ')) }
        if ($left.Count -eq 0) {
            Say-Fail ('VR-Forces 5.2d is down ONLY BECAUSE the run''s own back end was FORCED (pid {0}) after its graceful close was refused. Exit 6.' -f ($forced -join ', '))
            exit 6
        }
    }
}
Say-Fail ('still running after {0}s: {1}. {2}' -f $TimeoutSec, ($left -join ', '), $(if ($forced.Count -gt 0) { 'pid ' + ($forced -join ', ') + ' was FORCED; the processes listed are what is left.' } else { 'NOTHING WAS FORCE-KILLED - a force-killed joined federate leaves a stale federate and the next join hangs (RUNBOOK sec 0).' }))
# THE DIAGNOSTIC D1 OWED AND DID NOT HAVE (STP-844). "Inspect the screen for a modal"
# is not an artifact: on D1 it cost a separate, hand-run enumeration hours later to learn
# which two dialogs were up. This produces that evidence in the run's own stopvrf log,
# at the moment of failure, without touching anything.
# Guarded for the same reason as the post-grace call, and for one more: a throw HERE would
# turn this script's documented exit 3 ("still running, nothing was killed") into exit 5
# ("unexpected terminating error"), and the runner's teardown branch reads that code.
try {
    Write-WindowDiagnostic -Why ('TIMEOUT after ' + $TimeoutSec + 's') -Emit ${function:Say-Fail}
} catch {
    Say-Fail ('the window diagnostic failed ({0}) - IGNORED; the verdict below is unaffected.' -f $_.Exception.Message)
}
Say-Fail 'If one of the windows above is a modal, THAT is what is blocking the shutdown. This script answers nothing by design. The supported remedy is configuration: seed a run-owned appData with scripts\NewVrfAppData52.ps1 and launch with LaunchVrf52.ps1 -AppDataDir <that tree> so the GUI raises no prompt at all (UG52 4.6.1, 4.3.1).'
exit 3

}
catch {
    # Never let a terminating error surface as the bare exit 1, which is indistinguishable
    # from a generic failure at the worst moment - mid-shutdown, possibly with a modal up.
    Say-Fail ('unexpected terminating error: {0}' -f $_.Exception.Message)
    Say-Fail 'VR-FORCES MAY STILL BE RUNNING. Nothing was force-killed. Inspect before the next launch.'
    exit 5
}
