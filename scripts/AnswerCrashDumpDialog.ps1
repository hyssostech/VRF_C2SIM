# AnswerCrashDumpDialog.ps1 - answer the MAK crash-handler prompt of a DEAD vrfSim back-end.
#
# WHAT IT ANSWERS: when vrfSimHLA1516e faults, MAK's handler parks the (already dead)
# process on a Qt message box titled exactly like the dump it offers to write,
#     'vrfSim5.0.2-MSVC++15.0_64-249613-<pid>.dmp'
#     "A fatal error has occurred. Would you like to save a diagnostic file?"  [Yes] [No]
# then, after Yes, a second box with the SAME title:
#     "Saved dump file to '<name>.dmp'"                                        [OK]
# The dump lands in C:\MAK\vrforces5.0.2\bin64\. Yes is the DEFAULT button (screenshot-
# verified 2026-09-02), so Enter answers both boxes. Ruling of record: ALWAYS save the
# dump - it is the vendor's intended action and the evidence a MAK case needs; the
# federate is dead either way (RUNBOOK 0.5.12).
#
# HOW (and why not the obvious ways) - learned the hard way 2026-09-02, run 20260902T011908Z:
#   - The box is Qt: NO Win32 child controls (FindWindowEx finds nothing, BM_CLICK
#     impossible), same family as the RTI dialog (AnswerRtiDialog.ps1).
#   - SetForegroundWindow + SendKeys FAILS from a background shell (foreground lock).
#   - Coordinate clicks (SetCursorPos/mouse_event) are swallowed whenever ANY Windows
#     Security prompt is up (its full-screen 'Shell_SystemDim' layer owns all input) -
#     that day a firewall prompt for dotnet's testhost.exe sat on top of the box.
#   - Posted WM_LBUTTONDOWN/UP do NOTHING on Qt (it validates real input).
#   - Posted WM_KEYDOWN/WM_CHAR/WM_KEYUP VK_RETURN WORKS, needs no focus, no foreground,
#     and gets past the dim layer. That is what this script does.
#
# SAFETY: posts keystrokes ONLY to a top-level window owned by a vrfSim* process whose
# title matches '^vrfSim.*\.dmp$'. Never kills anything. RTI processes untouched.
# Exit codes: 0 dialog(s) answered and the process exited; 1 no such dialog (nothing
# touched); 2 posted but process/dialog still present after -ConfirmSec; 3 error.
param(
    [int] $TargetPid = 0,
    [int] $ConfirmSec = 180,
    [switch] $DryRun
)
$ErrorActionPreference = 'Stop'
try {
    Add-Type -Namespace Native -Name CrashDlg -MemberDefinition @'
[DllImport("user32.dll")]
public static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
[DllImport("user32.dll")]
public static extern bool IsWindow(IntPtr hWnd);
'@ -PassThru | Out-Null

    $dumpDir = 'C:\MAK\vrforces5.0.2\bin64'
    $procs = @(Get-Process -Name 'vrfSim*' -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -match '^vrfSim.*\.dmp$' })
    if ($TargetPid -ne 0) { $procs = @($procs | Where-Object { $_.Id -eq $TargetPid }) }
    if ($procs.Count -eq 0) {
        Write-Host '[NO-DIALOG] no vrfSim* process is showing a crash-dump prompt - nothing touched.'
        exit 1
    }

    function Send-Enter([IntPtr] $h) {
        # WM_SETFOCUS, then VK_RETURN down / char / up (scan code 0x1C). Posted, not sent:
        # no focus or foreground needed, and it passes the Windows Security dim layer.
        [void][Native.CrashDlg]::PostMessageW($h, 0x0007, [IntPtr]::Zero, [IntPtr]::Zero)
        [void][Native.CrashDlg]::PostMessageW($h, 0x0100, [IntPtr]0x0D, [IntPtr]0x001C0001)
        [void][Native.CrashDlg]::PostMessageW($h, 0x0102, [IntPtr]0x0D, [IntPtr]0x001C0001)
        [void][Native.CrashDlg]::PostMessageW($h, 0x0101, [IntPtr]0x0D, [IntPtr]0xC01C0001)
    }

    $rc = 0
    foreach ($p in $procs) {
        $id = $p.Id
        $title = $p.MainWindowTitle
        $dump = Join-Path $dumpDir $title
        Write-Host ("[..] {0} pid={1} prompt '{2}' (dump exists already: {3})" -f $p.Name, $id, $title, (Test-Path $dump))
        if ($DryRun) { Write-Host '  [DRY] would post Enter (Yes, then OK) and wait for exit'; continue }

        $deadline = (Get-Date).AddSeconds($ConfirmSec)
        $lastHandle = [IntPtr]::Zero
        $posts = 0
        $done = $false
        while ((Get-Date) -lt $deadline) {
            $cur = Get-Process -Id $id -ErrorAction SilentlyContinue
            if (-not $cur) { $done = $true; break }
            $cur.Refresh()
            $h = $cur.MainWindowHandle
            # Post once per DISTINCT window (the Yes box and the later OK box have
            # different handles); re-post the same handle only every ~10 s in case the
            # first Enter arrived before Qt finished creating the box.
            if ($h -ne [IntPtr]::Zero -and $cur.MainWindowTitle -match '^vrfSim.*\.dmp$' -and
                ($h -ne $lastHandle -or ($posts % 10) -eq 0)) {
                Send-Enter $h
                if ($h -ne $lastHandle) { Write-Host ("  [..] Enter posted to window 0x{0:X} (default button)" -f [long]$h) }
                $lastHandle = $h
            }
            $posts++
            Start-Sleep -Seconds 1
        }
        $haveDump = Test-Path $dump
        if ($done) {
            Write-Host ("  [OK] pid {0} exited; dump present: {1} ({2})" -f $id, $haveDump, $dump)
            if (-not $haveDump) { Write-Host '  [WARN] process gone but no dump file - it may have been declined by hand.' }
        } else {
            Write-Host ("  [STILL-PRESENT] pid {0} alive after {1}s; dump present: {2}. Inspect by hand; do NOT kill." -f $id, $ConfirmSec, $haveDump)
            $rc = 2
        }
    }
    exit $rc
} catch {
    Write-Host ("[ERROR] {0}" -f $_.Exception.Message)
    exit 3
}
