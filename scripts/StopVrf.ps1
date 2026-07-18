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

    WHY NOT SCREENSHOT + COORDINATE CLICK: attempted first and all three variants
    failed on this dialog - CopyFromScreen captured the occluding window,
    SetForegroundWindow was refused by the Windows foreground lock, and PrintWindow
    with PW_RENDERFULLCONTENT returned an all-black bitmap (Qt/OpenGL surface).
    UIA is the reliable channel here.

    WHAT THIS SCRIPT WILL NEVER DO:
      - force-kill a JOINED federate (a hard non-negotiable; it is how stale-federate
        join hangs are created). Everything here is a graceful quit.
      - touch rtiAssistant / rtiexec / rtiForwarder. They are RTI infrastructure,
        they persist across launches, and an already-answered rtiAssistant is what
        makes unattended LAUNCH work (RUNBOOK sec 0.5.2).

.PARAMETER TimeoutSec
    How long to wait for the processes to exit after answering the dialog.

.PARAMETER QuitBackEnds
    Tick "Quit All Back-Ends" so the back-end exits with the front-end. Default true.
    Pass -QuitBackEnds:$false to leave back-ends running deliberately.

.PARAMETER DryRun
    Report what is running and what would be done; change nothing.

.OUTPUTS
    Exit codes (each distinct - LaunchVrf.ps1 overloads 0 and 2 across two paths and
    that ambiguity is a recorded defect; do not repeat it here):
      0 = VR-Forces is down (or was already down), RTI infrastructure intact
      2 = bad arguments
      3 = timed out waiting for processes to exit
      4 = the confirm dialog appeared but could not be driven via UIA
#>
[CmdletBinding()]
param(
    [int]    $TimeoutSec   = 60,
    [switch] $QuitBackEnds = $true,
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

$procFrontend = 'vrfGui'
$procBackend  = 'vrfSimHLA1516e'
$procLauncher = 'vrfLauncher'
$rtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')

Write-Host "=== StopVrf.ps1 - unattended VR-Forces shutdown ==="
Write-Host ("  TimeoutSec   : {0}" -f $TimeoutSec)
Write-Host ("  QuitBackEnds : {0}" -f [bool]$QuitBackEnds)
Write-Host ("  DryRun       : {0}" -f [bool]$DryRun)
Write-Host ""

# --- inventory ---
Write-Host "=== Inventory ==="
$fe = @(Get-Process -Name $procFrontend -ErrorAction SilentlyContinue)
$be = @(Get-Process -Name $procBackend  -ErrorAction SilentlyContinue)
$la = @(Get-Process -Name $procLauncher -ErrorAction SilentlyContinue)
foreach ($p in ($fe + $be + $la)) {
    Say-Ok ("{0} pid={1} threads={2}" -f $p.ProcessName, $p.Id, $p.Threads.Count)
}
foreach ($n in $rtiNames) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
        Say-Ok ("{0} pid={1} - RTI infrastructure, WILL BE PRESERVED" -f $p.ProcessName, $p.Id)
    }
}
if ($fe.Count -eq 0 -and $be.Count -eq 0 -and $la.Count -eq 0) {
    Say-Ok 'no VR-Forces processes running - nothing to do.'
    exit 0
}

if ($DryRun) {
    Write-Host ""
    Say-Ok 'DRY RUN: would CloseMainWindow on the front-end, answer the "Are You Sure?" dialog'
    Say-Ok ('           via UIA (Quit All Back-Ends = {0}, then Yes), and wait for exit.' -f [bool]$QuitBackEnds)
    Say-Ok '           RTI processes would NOT be touched.'
    exit 0
}

# --- ask the front-end to close ---
Write-Host ""
Write-Host "=== Shutdown ==="
foreach ($p in $fe) {
    Say-Ok ("requesting close of {0} pid={1}" -f $p.ProcessName, $p.Id)
    $null = $p.CloseMainWindow()
}
if ($fe.Count -eq 0) {
    # No front-end (e.g. BackendOnly launch): ask the back-end directly.
    foreach ($p in $be) {
        Say-Ok ("no front-end; requesting close of {0} pid={1}" -f $p.ProcessName, $p.Id)
        $null = $p.CloseMainWindow()
    }
}

# --- answer the confirm dialog via UIA ---
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System; using System.Runtime.InteropServices; using System.Text;
public class StopVrfWin {
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
  public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int m);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
}
'@

function Find-ConfirmDialog {
    # Match on the OWNING PROCESS as well as the title, so a same-titled dialog from
    # some unrelated application can never be driven by this script.
    $vrfPids = @()
    foreach ($n in @($procFrontend, $procBackend)) {
        $vrfPids += @(Get-Process -Name $n -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
    }
    if ($vrfPids.Count -eq 0) { return [IntPtr]::Zero }
    # NOTE: the callback runs in its own scope, so it must assign to a SCRIPT-scoped
    # variable and this function must read back that same one. An earlier draft
    # assigned $script:found but returned a function-local $found - which is always
    # IntPtr.Zero, so the dialog would never have been detected and every run would
    # have timed out at exit 3.
    $script:found = [IntPtr]::Zero
    $cb = [StopVrfWin+EnumWindowsProc]{
        param($h, $l)
        $procId = 0
        [void][StopVrfWin]::GetWindowThreadProcessId($h, [ref]$procId)
        if ($vrfPids -contains [int]$procId) {
            $sb = New-Object System.Text.StringBuilder 512
            [void][StopVrfWin]::GetWindowText($h, $sb, 512)
            if ($sb.ToString() -eq 'Are You Sure?') { $script:found = $h }
        }
        return $true
    }
    [void][StopVrfWin]::EnumWindows($cb, [IntPtr]::Zero)
    return $script:found
}

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$dialogHandled = $false

while ((Get-Date) -lt $deadline) {
    $stillUp = @(Get-Process -Name $procFrontend -ErrorAction SilentlyContinue) +
               @(Get-Process -Name $procBackend  -ErrorAction SilentlyContinue) +
               @(Get-Process -Name $procLauncher -ErrorAction SilentlyContinue)
    if ($stillUp.Count -eq 0) { break }

    $dlg = Find-ConfirmDialog
    if ($dlg -ne [IntPtr]::Zero -and -not $dialogHandled) {
        Say-Ok 'confirm dialog "Are You Sure?" detected - answering via UI Automation'
        try {
            $el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)

            if ($QuitBackEnds) {
                $cond = New-Object System.Windows.Automation.PropertyCondition(
                            [System.Windows.Automation.AutomationElement]::NameProperty, 'Quit All Back-Ends')
                $chk = $el.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
                if ($null -ne $chk) {
                    $tog = $chk.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
                    if ($tog.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) {
                        $tog.Toggle()
                        Start-Sleep -Milliseconds 400
                    }
                    Say-Ok ("'Quit All Back-Ends' = {0}" -f $tog.Current.ToggleState)
                } else {
                    Say-Warn "'Quit All Back-Ends' checkbox not found - the back-end may survive the front-end."
                }
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
            $dialogHandled = $true
        } catch {
            Say-Fail ("could not drive the confirm dialog via UIA: {0}" -f $_.Exception.Message)
            exit 4
        }
    }
    Start-Sleep -Milliseconds 500
}

# --- verify ---
Write-Host ""
Write-Host "=== Result ==="
$leftFe = @(Get-Process -Name $procFrontend -ErrorAction SilentlyContinue)
$leftBe = @(Get-Process -Name $procBackend  -ErrorAction SilentlyContinue)
$leftLa = @(Get-Process -Name $procLauncher -ErrorAction SilentlyContinue)

foreach ($n in $rtiNames) {
    foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
        Say-Ok ("{0} pid={1} still running - CORRECT, never kill RTI infrastructure" -f $p.ProcessName, $p.Id)
    }
}

if (($leftFe.Count + $leftBe.Count + $leftLa.Count) -eq 0) {
    Say-Ok 'VR-Forces is DOWN (graceful quit; no process was force-killed).'
    exit 0
}

foreach ($p in ($leftFe + $leftBe + $leftLa)) {
    Say-Fail ("still running after {0}s: {1} pid={2}" -f $TimeoutSec, $p.ProcessName, $p.Id)
}
Say-Fail 'TIMED OUT. NOT force-killing - a joined federate must never be force-killed (RUNBOOK sec 0).'
Say-Fail 'Investigate by hand: a second modal dialog (e.g. "save scenario?") is the likeliest cause.'
exit 3
