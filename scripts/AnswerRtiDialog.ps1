# AnswerRtiDialog.ps1 - answer the once-per-boot "Choose RTI Connection" dialog.
#
# WHY THIS EXISTS: on a fresh boot the FIRST federate to contact the MAK RTI Assistant
# raises a Qt "Choose RTI Connection" dialog with NO UIA tree. Every federate blocks
# behind it (RUNBOOK 0.5.3; LaunchVrf warns but cannot answer it). The 2026-07-22
# session established the recovery: a DPI-AWARE coordinate click on the Connect button
# at window-relative ratio (0.668, 0.949) of the 573x583 dialog, with the starred
# predefined rtiexec loopback connection as the default selection
# (memory [[vrf-teardown-relaunch-wedges-rti]], "Boot RTI dialog"). Verified working
# 2026-07-22; first scripted 2026-09-01 after it blocked run 20260901T183422Z.
#
# SAFETY: this script clicks ONLY inside a window titled exactly 'Choose RTI Connection'.
# If no such window exists it exits 1 having touched nothing. It never kills anything.
# Exit codes: 0 clicked (and the window went away within -ConfirmSec); 1 no dialog found;
# 2 clicked but the window is STILL PRESENT after -ConfirmSec (inspect by hand);
# 3 unexpected error.
param(
    [double] $RatioX = 0.668,
    [double] $RatioY = 0.949,
    [int] $ConfirmSec = 15
)
$ErrorActionPreference = 'Stop'
try {
    Add-Type -Namespace Native -Name Win32 -MemberDefinition @'
[DllImport("user32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
public static extern IntPtr FindWindowW(string lpClassName, string lpWindowName);
[DllImport("user32.dll")]
public static extern bool IsWindow(IntPtr hWnd);
[DllImport("user32.dll")]
public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
[DllImport("user32.dll")]
public static extern bool SetForegroundWindow(IntPtr hWnd);
[DllImport("user32.dll")]
public static extern bool SetCursorPos(int X, int Y);
[DllImport("user32.dll")]
public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
[DllImport("user32.dll")]
public static extern IntPtr SetProcessDpiAwarenessContext(IntPtr value);
public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
'@ -PassThru | Out-Null

    # Per-monitor-v2 DPI awareness (-4) so GetWindowRect returns PHYSICAL pixels -
    # the exact condition under which the 0.668/0.949 ratio was measured (2026-07-22).
    [void][Native.Win32]::SetProcessDpiAwarenessContext([IntPtr]::new(-4))

    $title = 'Choose RTI Connection'
    # Prefer the assistant process's own main-window handle (robust against title
    # marshaling / matching quirks); fall back to a Unicode FindWindow by exact title.
    $h = [IntPtr]::Zero
    foreach ($p in @(Get-Process -Name rtiAssistant -ErrorAction SilentlyContinue)) {
        if ($p.MainWindowTitle -eq $title -and $p.MainWindowHandle -ne 0) { $h = $p.MainWindowHandle; break }
    }
    if ($h -eq [IntPtr]::Zero) { $h = [Native.Win32]::FindWindowW($null, $title) }
    if ($h -eq [IntPtr]::Zero -or -not [Native.Win32]::IsWindow($h)) {
        Write-Host "[NO-DIALOG] no window titled '$title' - nothing to answer, nothing touched."
        exit 1
    }
    $r = New-Object Native.Win32+RECT
    [void][Native.Win32]::GetWindowRect($h, [ref]$r)
    $w = $r.Right - $r.Left; $ht = $r.Bottom - $r.Top
    $x = [int]($r.Left + $w * $RatioX)
    $y = [int]($r.Top + $ht * $RatioY)
    Write-Host ("[..] dialog at ({0},{1}) size {2}x{3}; clicking Connect at ({4},{5}) (ratio {6},{7})" -f `
        $r.Left, $r.Top, $w, $ht, $x, $y, $RatioX, $RatioY)
    [void][Native.Win32]::SetForegroundWindow($h)
    Start-Sleep -Milliseconds 400
    [void][Native.Win32]::SetCursorPos($x, $y)
    Start-Sleep -Milliseconds 200
    [Native.Win32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)  # LEFTDOWN
    [Native.Win32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)  # LEFTUP

    # Confirm the dialog actually went away - a click that landed nowhere useful
    # leaves it up, and that must be exit 2, not a false success.
    $deadline = (Get-Date).AddSeconds($ConfirmSec)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        if ([Native.Win32]::FindWindowW($null, $title) -eq [IntPtr]::Zero) {
            Write-Host '[OK] dialog answered (window gone).'
            exit 0
        }
    }
    Write-Host ("[STILL-PRESENT] clicked, but the '{0}' window is still up after {1}s. Inspect by hand; do NOT kill anything." -f $title, $ConfirmSec)
    exit 2
} catch {
    Write-Host ("[ERROR] {0}" -f $_.Exception.Message)
    exit 3
}
