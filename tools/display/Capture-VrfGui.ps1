# Capture-VrfGui.ps1 - screenshots of the vrfGui main window for a rehearsal's evidence. READ-ONLY toward VR-Forces: no
# keyboard or mouse input is sent. Used by REHEARSAL_WAYB-2026-10-11-4/-5/-6 (docs/experiments/REHEARSAL_WAYB_HAND2/HAND3/
# STPPUSH_2026-10-11.md).
#   <Out>-pw.png   PrintWindow(PW_RENDERFULLCONTENT=2) on the window handle
#   <Out>-scr.png  with -Screen: CopyFromScreen of the window rectangle after ShowWindow(SW_RESTORE) + SetForegroundWindow
#                  (brings the GUI to the front; the desktop compositor's pixels)
# A BLANK WHITE MAP in BOTH files is the GUI's own state, not the capture: in 4 of 4 runs it matched the display being OFF
# when the GUI started (REHEARSAL_WAYB_STPPUSH_2026-10-11 finding - a correlation, no cause claimed).
# Exit: 0 saved; 2 no vrfGui window. ASCII only.
param([Parameter(Mandatory = $true)][string]$Out, [switch]$Screen)
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class W2 {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public struct RECT { public int L, T, R, B; }
}
"@
[void][W2]::SetProcessDPIAware()
$p = Get-Process vrfGui -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { "no vrfGui window"; exit 2 }
$h = $p.MainWindowHandle
$r = New-Object W2+RECT; [void][W2]::GetWindowRect($h, [ref]$r)
$w = $r.R - $r.L; $ht = $r.B - $r.T
$bmp = New-Object System.Drawing.Bitmap $w, $ht
$g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
$ok = [W2]::PrintWindow($h, $hdc, 2); $g.ReleaseHdc($hdc); $g.Dispose()
$bmp.Save("$Out-pw.png", [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
"pw: $Out-pw.png ${w}x${ht} printwindow=$ok"
if ($Screen) {
  [void][W2]::ShowWindow($h, 9); [void][W2]::SetForegroundWindow($h); Start-Sleep -Milliseconds 1500
  [void][W2]::GetWindowRect($h, [ref]$r); $w = $r.R - $r.L; $ht = $r.B - $r.T
  $bmp = New-Object System.Drawing.Bitmap $w, $ht
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size $w, $ht)); $g.Dispose()
  $bmp.Save("$Out-scr.png", [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
  "scr: $Out-scr.png ${w}x${ht} at ($($r.L),$($r.T))"
}
