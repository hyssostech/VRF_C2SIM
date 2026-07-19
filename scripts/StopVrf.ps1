<#
.SYNOPSIS
    Shut VR-Forces down UNATTENDED - no clicks, no force-kill, RTI infrastructure preserved.

.DESCRIPTION
    Closing vrfGui raises a modal Qt confirm titled "Are You Sure?"
    (class makVrf::DtNeverAskAgainMessageBox) which blocks shutdown until answered.
    RUNBOOK sec 0.5.9 previously said only "close the front-end and the back-end
    follows", which is NOT achievable unattended by CloseMainWindow alone.

    This script answers that dialog through UI AUTOMATION - by control name, not by
    screen coordinates. Verified 2026-07-18: the dialog DOES expose a full UIA tree
    (unlike the RTI "Choose RTI Connection" dialog, which does not - do not
    generalise between them):

        root  class = makVrf::DtNeverAskAgainMessageBox
        Text     "Quit VR-Forces"
        Button   "Yes"
        Button   "No"
        CheckBox "Quit All Back-Ends"

    Ticking "Quit All Back-Ends" is what makes the back-end follow the front-end.
    Vendor naming note: the User's Guide calls this the "Yes, and Quit All Back-Ends"
    option (Introduction\Starting\ExitingVR-Forces.htm); the shipped 5.0.2 dialog
    implements it as a CHECKBOX beside "Yes" rather than a second button.

    WHY NOT SCREENSHOT + COORDINATE CLICK: attempted first and all three variants
    failed on this dialog - CopyFromScreen captured the occluding window,
    SetForegroundWindow was refused by the Windows foreground lock, and PrintWindow
    with PW_RENDERFULLCONTENT returned an all-black bitmap (Qt/OpenGL surface).
    UIA is the reliable channel here.

    BACK-END GRACEFUL CLOSE FALLBACK (added after the 2026-07-19 defect, run
    20260719T144109Z): answering the dialog with "Quit All Back-Ends" = On does NOT
    always carry the back-end. On that run vrfGui exited correctly and vrfSimHLA1516e
    did not even BEGIN to shut down - it burned ~one full core across an 8 s sample,
    still held ESTABLISHED TCP to 127.0.0.1:6003 (rtiAssistant) and 127.0.0.1:4001 plus
    a bound UDP endpoint on 4001 (i.e. still JOINED to the federation), and owned no
    modal window at all, only its own console. StopVrf waited out the full budget and
    exited 3.

    ROOT CAUSE UNKNOWN - do not pretend otherwise. The SAME script tore down an IDLE
    instance in 6 s earlier the SAME day and failed on an instance that had just run a
    full scenario, so it correlates with scenario activity, but nothing here has been
    established as causal. Treat that correlation as a lead, not a diagnosis.

    The fallback is the mechanism proven by hand on that instance: CloseMainWindow()
    (WM_CLOSE) sent to the back-end's OWN window brought it down in five seconds,
    cleanly, with all three RTI processes preserved. That is exactly the graceful
    channel this script already uses on vrfGui, just aimed at the back-end. It is a
    GRACEFUL REQUEST, not a kill: the back-end runs its own shutdown, leaves the
    federation, and may still refuse - in which case the run still fails (exit 3).

    WHAT THIS SCRIPT WILL NEVER DO:
      - force-kill a JOINED federate (a hard non-negotiable; it is how stale-federate
        join hangs are created). Everything here is a graceful quit.
      - touch rtiAssistant / rtiexec / rtiForwarder. They are RTI infrastructure,
        they persist across launches, and an already-answered rtiAssistant is what
        makes unattended LAUNCH work (RUNBOOK sec 0.5.2).

.PARAMETER TimeoutSec
    Total budget for the WHOLE shutdown - detecting the dialog, answering it, AND
    waiting for the processes to exit. It is NOT a post-dialog-only budget.

.PARAMETER QuitBackEnds
    Controls the "Quit All Back-Ends" checkbox. $true (default) ticks it so back-ends
    exit with the front-end; $false explicitly UNTICKS it. The checkbox is driven to
    the requested state in BOTH cases - the dialog class is DtNeverAskAgainMessageBox
    and its state can persist from a previous session, so "do nothing" would NOT
    reliably mean "unticked".

.PARAMETER BackEndCloseTimeoutSec
    Budget for the BACK-END GRACEFUL CLOSE FALLBACK only (see below), spent after
    TimeoutSec is already exhausted. It is deliberately separate so the fallback can
    never eat into the main budget, and so a caller can size the two independently.

.PARAMETER DryRun
    Report what is running and what WOULD be done for that exact state; change nothing.

.OUTPUTS
    Exit codes:
      0 = VR-Forces is down, or was already down, or a dry run completed.
          NOTE: 0 deliberately covers all three. Use -DryRun's own output to tell
          them apart; a dry run never changes state, so a caller that did not pass
          -DryRun cannot receive the dry-run flavour of 0.
      2 = bad arguments
      3 = timed out waiting for processes to exit - INCLUDING after the back-end
          graceful close fallback was tried and also timed out. Nothing was killed.
      4 = the confirm dialog appeared but could not be driven via UIA
      5 = an unexpected terminating error (assembly load failure, or a process that
          exited underneath us). Explicitly trapped so it can never surface as the
          bare PowerShell exit 1, which would be indistinguishable from a generic
          failure at exactly the worst moment - mid-shutdown with a modal up.
#>
[CmdletBinding()]
param(
    [int]    $TimeoutSec              = 60,
    [int]    $BackEndCloseTimeoutSec  = 30,
    [bool]   $QuitBackEnds            = $true,
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Say-Ok   { param($m) Write-Host "  [OK]   $m" }
function Say-Warn { param($m) Write-Host "  [WARN] $m" }
function Say-Fail { param($m) Write-Host "  [FAIL] $m" }

# --- argument validation BEFORE touching anything (LaunchVrf validates too late; recorded defect) ---
if ($TimeoutSec -lt 5 -or $TimeoutSec -gt 600) {
    Say-Fail "TimeoutSec must be between 5 and 600 (got $TimeoutSec)."
    exit 2
}
if ($BackEndCloseTimeoutSec -lt 5 -or $BackEndCloseTimeoutSec -gt 600) {
    Say-Fail "BackEndCloseTimeoutSec must be between 5 and 600 (got $BackEndCloseTimeoutSec)."
    exit 2
}

$procFrontend = 'vrfGui'
$procBackend  = 'vrfSimHLA1516e'
$procLauncher = 'vrfLauncher'
$rtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')
$confirmTitle = 'Are You Sure?'

Write-Host "=== StopVrf.ps1 - unattended VR-Forces shutdown ==="
Write-Host ("  TimeoutSec             : {0} (covers dialog detection + answer + process exit)" -f $TimeoutSec)
Write-Host ("  BackEndCloseTimeoutSec : {0} (back-end graceful close fallback only)" -f $BackEndCloseTimeoutSec)
Write-Host ("  QuitBackEnds           : {0}" -f $QuitBackEnds)
Write-Host ("  DryRun                 : {0}" -f [bool]$DryRun)
Write-Host ""

# Everything from here can touch live processes / assemblies. Any terminating error
# becomes exit 5 rather than the bare, undocumented exit 1.
try {

# --- inventory ---
Write-Host "=== Inventory ==="

# NOTE: PowerShell UNROLLS a single-element array on return, so `return @(...)` hands
# back a bare [Process] when exactly one matches - and `$fe + $be` then fails with
# "does not contain a method named 'op_Addition'". Every CALL SITE therefore wraps this
# in @(...) again. Do not "simplify" those wrappers away.
function Get-Procs { param($name) return @(Get-Process -Name $name -ErrorAction SilentlyContinue) }

# Threads/CloseMainWindow throw InvalidOperationException on a process that exited
# between the snapshot and the access. Report defensively rather than dying.
function Describe-Proc {
    param($p)
    try   { return ("{0} pid={1} threads={2}" -f $p.ProcessName, $p.Id, $p.Threads.Count) }
    catch { return ("{0} pid={1} threads=(exited during inspection)" -f $p.ProcessName, $p.Id) }
}

$fe = @(Get-Procs $procFrontend)
$be = @(Get-Procs $procBackend)
$la = @(Get-Procs $procLauncher)
foreach ($p in @($fe + $be + $la)) { Say-Ok (Describe-Proc $p) }
foreach ($n in $rtiNames) {
    foreach ($p in @(Get-Procs $n)) {
        Say-Ok ("{0} pid={1} - RTI infrastructure, WILL BE PRESERVED" -f $p.ProcessName, $p.Id)
    }
}
if ($fe.Count -eq 0 -and $be.Count -eq 0 -and $la.Count -eq 0) {
    Say-Ok 'no VR-Forces processes running - nothing to do.'
    exit 0
}

# Which processes will actually be ASKED to close, for this exact state. vrfLauncher
# is deliberately included: it was previously part of the success criterion but was
# never asked to close, so a launcher-only state burned the whole timeout and then
# reported a fabricated diagnosis.
$closeTargets = @()
if ($fe.Count -gt 0) { $closeTargets += $fe }
if ($fe.Count -eq 0 -and $be.Count -gt 0) { $closeTargets += $be }
if ($fe.Count -eq 0 -and $be.Count -eq 0 -and $la.Count -gt 0) { $closeTargets += $la }

if ($DryRun) {
    Write-Host ""
    Write-Host "=== Dry run - what WOULD happen for the state above ==="
    if ($closeTargets.Count -eq 0) {
        Say-Warn 'nothing would be asked to close.'
    } else {
        foreach ($p in $closeTargets) {
            Say-Ok ("would call CloseMainWindow on {0} pid={1}" -f $p.ProcessName, $p.Id)
        }
    }
    if ($fe.Count -gt 0) {
        Say-Ok ('would answer the "{0}" dialog via UIA: set "Quit All Back-Ends" = {1}, then Yes' -f $confirmTitle, $QuitBackEnds)
    } else {
        Say-Ok ('no front-end present, so the "{0}" confirm is not expected' -f $confirmTitle)
    }
    if ($be.Count -gt 0) {
        Say-Ok ('if any {0} were still running after {1}s, would then send it a GRACEFUL CloseMainWindow and wait up to {2}s' -f $procBackend, $TimeoutSec, $BackEndCloseTimeoutSec)
    } else {
        Say-Ok ('no {0} present, so the back-end graceful close fallback is not expected' -f $procBackend)
    }
    Say-Ok 'RTI processes would NOT be touched. Nothing would be force-killed.'
    exit 0
}

# --- ask the targets to close ---
Write-Host ""
Write-Host "=== Shutdown ==="
if ($closeTargets.Count -eq 0) {
    Say-Warn 'nothing to ask to close, yet VR-Forces processes are present - not waiting.'
} else {
    foreach ($p in $closeTargets) {
        try {
            $sent = $p.CloseMainWindow()
            if ($sent) { Say-Ok    ("close request accepted by {0} pid={1}" -f $p.ProcessName, $p.Id) }
            else       { Say-Warn ("{0} pid={1} has no main window to close - it may need a different exit path" -f $p.ProcessName, $p.Id) }
        } catch {
            Say-Warn ("{0} pid={1} exited before the close request landed" -f $p.ProcessName, $p.Id)
        }
    }
}

# --- UIA machinery ---
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System; using System.Runtime.InteropServices; using System.Text;
public class StopVrfWin {
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
  public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int m);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
}
'@

# Enumerate EVERY top-level window owned by the VR-Forces processes. Used both to find
# the confirm dialog AND to report what is actually on screen when we time out - the
# previous version asserted "a second modal dialog is the likeliest cause" without ever
# having looked, despite already running this exact enumeration.
function Get-VrfWindows {
    $vrfPids = @()
    foreach ($n in @($procFrontend, $procBackend, $procLauncher)) {
        $vrfPids += @(Get-Process -Name $n -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
    }
    $script:vrfWindows = @()
    if ($vrfPids.Count -eq 0) { return @() }
    # NOTE: the callback runs in its own scope. It can READ these enclosing locals but
    # can only WRITE to a script-scoped variable, so results accumulate in
    # $script:vrfWindows and the caller reads back that same one. An earlier draft
    # returned a function-local instead - permanently empty, so the dialog would never
    # have been found and every run would have timed out at exit 3.
    $cb = [StopVrfWin+EnumWindowsProc]{
        param($h, $l)
        $procId = 0
        [void][StopVrfWin]::GetWindowThreadProcessId($h, [ref]$procId)
        if ($vrfPids -contains [int]$procId) {
            $sb = New-Object System.Text.StringBuilder 512
            [void][StopVrfWin]::GetWindowText($h, $sb, 512)
            $t = $sb.ToString()
            if ($t) {
                $script:vrfWindows += [pscustomobject]@{
                    Handle  = $h
                    Pid     = [int]$procId
                    Title   = $t
                    Visible = [StopVrfWin]::IsWindowVisible($h)
                }
            }
        }
        return $true
    }
    [void][StopVrfWin]::EnumWindows($cb, [IntPtr]::Zero)
    return $script:vrfWindows
}

function Answer-ConfirmDialog {
    param($handle)
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($handle)

    # Drive the checkbox to the REQUESTED state in both directions. The dialog class is
    # DtNeverAskAgainMessageBox and its state can persist from a previous session, so
    # skipping the block when $QuitBackEnds is $false would leave a previously-ticked
    # box ticked and kill back-ends the caller asked to keep.
    $cond = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, 'Quit All Back-Ends')
    $chk = $el.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($null -ne $chk) {
        $tog = $chk.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
        $want = if ($QuitBackEnds) { [System.Windows.Automation.ToggleState]::On }
                else               { [System.Windows.Automation.ToggleState]::Off }
        if ($tog.Current.ToggleState -ne $want) {
            $tog.Toggle()
            Start-Sleep -Milliseconds 400
        }
        if ($tog.Current.ToggleState -ne $want) {
            Say-Warn ("'Quit All Back-Ends' is {0} but {1} was requested" -f $tog.Current.ToggleState, $want)
        } else {
            Say-Ok ("'Quit All Back-Ends' = {0} (requested)" -f $tog.Current.ToggleState)
        }
    } else {
        Say-Warn "'Quit All Back-Ends' checkbox not found in this dialog."
    }

    $cond = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, 'Yes')
    $yes = $el.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($null -eq $yes) {
        Say-Fail 'the "Yes" button was not found in the dialog UIA tree.'
        exit 4
    }
    $yes.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Say-Ok 'answered Yes'
}

# --- wait loop ---
# Dialogs are tracked BY WINDOW HANDLE, not by a single boolean latch. A permanent latch
# meant that a second vrfGui's dialog, or an Invoke() that did not actually dismiss the
# window, was ignored forever and guaranteed a timeout.
$deadline = (Get-Date).AddSeconds($TimeoutSec)
$answered = @{}

while ((Get-Date) -lt $deadline) {
    $stillUp = @(@(Get-Procs $procFrontend) + @(Get-Procs $procBackend) + @(Get-Procs $procLauncher))
    if ($stillUp.Count -eq 0) { break }

    foreach ($w in (Get-VrfWindows)) {
        if ($w.Title -eq $confirmTitle) {
            $key = $w.Handle.ToString()
            if (-not $answered.ContainsKey($key)) {
                Say-Ok ('confirm dialog "{0}" detected (hwnd {1}, pid {2}) - answering via UI Automation' -f $w.Title, $w.Handle, $w.Pid)
                try {
                    Answer-ConfirmDialog -handle $w.Handle
                    $answered[$key] = $true
                } catch {
                    Say-Fail ("could not drive the confirm dialog via UIA: {0}" -f $_.Exception.Message)
                    exit 4
                }
            }
        }
        # Other visible windows are NOT reported here. During a healthy shutdown the
        # main window and the back-end console are both still up and still visible for
        # a moment, and warning about them every run was pure noise that read like a
        # fault. They ARE reported - with full titles and handles - on the timeout
        # path below, which is the only place the information is actionable.
    }
    Start-Sleep -Milliseconds 500
}

# --- back-end graceful close fallback ---
# Reached only when the main budget is spent and a back-end is STILL up, i.e. the GUI
# quit did not carry it (see the FALLBACK section of the header comment; root cause
# unknown). This is a GRACEFUL WM_CLOSE to the back-end's own window - the same channel
# already used on vrfGui - and it is the LAST escalation this script has.
#
# THERE IS DELIBERATELY NO FORCE-KILL PATH HERE, AND NONE MAY EVER BE ADDED - not
# Stop-Process -Force, not taskkill /F, not "just as a last resort", not behind a flag.
# The back-end is a JOINED federate; force-killing it strands the federation and creates
# exactly the stale-federate join hangs this whole toolchain exists to avoid (RUNBOOK
# sec 0). If the graceful close does not work, the CORRECT outcome is a non-zero exit
# and a clear operator message, not a dead federate.
$fallbackUsed = $false
$beLeft = @(Get-Procs $procBackend)
if ($beLeft.Count -gt 0) {
    $fallbackUsed = $true
    Write-Host ""
    Write-Host "=== Back-end graceful close fallback ==="
    $feLeft = @(Get-Procs $procFrontend)
    if ($feLeft.Count -eq 0) {
        Say-Warn ('the GUI quit did NOT carry the back-end: {0} is gone but {1} is still running.' -f $procFrontend, $procBackend)
    } else {
        Say-Warn ('{0} is still running AND {1} is still up - the GUI quit has not completed either.' -f $procBackend, $procFrontend)
    }
    Say-Warn 'escalating to a GRACEFUL CloseMainWindow on the back-end itself. Nothing is being killed.'

    foreach ($p in $beLeft) {
        try {
            $sent = $p.CloseMainWindow()
            if ($sent) { Say-Ok    ("fallback close request accepted by {0} pid={1}" -f $p.ProcessName, $p.Id) }
            else       { Say-Warn ("fallback close request NOT accepted by {0} pid={1} - it reports no main window" -f $p.ProcessName, $p.Id) }
        } catch {
            Say-Warn ("{0} pid={1} exited before the fallback close request landed" -f $p.ProcessName, $p.Id)
        }
    }

    # Its own bounded budget, spent only here, so it can never extend TimeoutSec silently.
    $beDeadline = (Get-Date).AddSeconds($BackEndCloseTimeoutSec)
    while ((Get-Date) -lt $beDeadline) {
        if (@(Get-Procs $procBackend).Count -eq 0) { break }
        Start-Sleep -Milliseconds 500
    }

    if (@(Get-Procs $procBackend).Count -eq 0) {
        Say-Ok ('back-end exited after the graceful fallback (within {0}s). No force-kill was used.' -f $BackEndCloseTimeoutSec)
    } else {
        Say-Fail ('back-end STILL running {0}s after the graceful fallback close.' -f $BackEndCloseTimeoutSec)
    }
}

# --- verify ---
Write-Host ""
Write-Host "=== Result ==="
foreach ($n in $rtiNames) {
    foreach ($p in @(Get-Procs $n)) {
        Say-Ok ("{0} pid={1} still running - CORRECT, never kill RTI infrastructure" -f $p.ProcessName, $p.Id)
    }
}

$left = @(@(Get-Procs $procFrontend) + @(Get-Procs $procBackend) + @(Get-Procs $procLauncher))
if ($left.Count -eq 0) {
    if ($fallbackUsed) {
        Say-Ok 'VR-Forces is DOWN, but ONLY after the back-end graceful close fallback was needed.'
        Say-Ok 'The GUI quit alone did not carry the back-end on this run - see the fallback section above.'
    }
    Say-Ok 'VR-Forces is DOWN (graceful quit; no process was force-killed).'
    exit 0
}

foreach ($p in $left) { Say-Fail ("still running after {0}s: {1} pid={2}" -f $TimeoutSec, $p.ProcessName, $p.Id) }
if ($fallbackUsed) {
    Say-Fail ('the back-end graceful close fallback WAS used (extra {0}s) and did not clear it.' -f $BackEndCloseTimeoutSec)
}

# Report what is ACTUALLY on screen instead of guessing at a cause.
$windows = Get-VrfWindows
$visible = @($windows | Where-Object { $_.Visible })
if ($visible.Count -gt 0) {
    Say-Fail 'visible windows still owned by these processes:'
    foreach ($w in $visible) { Say-Fail ('    "{0}" (pid {1}, hwnd {2})' -f $w.Title, $w.Pid, $w.Handle) }
    Say-Fail 'If one of the above is a modal prompt this script does not answer (only'
    Say-Fail ('"{0}" is handled), that is what is blocking the shutdown.' -f $confirmTitle)
} else {
    Say-Fail 'no visible windows are owned by these processes - the cause is NOT a modal dialog.'
}
Say-Fail 'NOT force-killing - a joined federate must never be force-killed (RUNBOOK sec 0).'
exit 3

} catch {
    Write-Host ""
    Say-Fail ("unexpected terminating error: {0}" -f $_.Exception.Message)
    Say-Fail ("at: {0}" -f $_.InvocationInfo.PositionMessage)
    Say-Fail 'VR-Forces may still be running, possibly with an unanswered modal dialog.'
    Say-Fail 'Nothing was force-killed. Re-run this script, or inspect by hand.'
    exit 5
}
