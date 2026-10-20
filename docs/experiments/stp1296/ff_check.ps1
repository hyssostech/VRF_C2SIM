# STP-1296 step 1 (S1-1, dispatcher finding 2026-10-20): does an appData fastForwardSettings.mtl hold a LIVE entry?
# An .mtl comment runs from ';' to the end of the line; the shipped file documents its syntax in ';;' lines that
# contain the words "fast-forward-entry", so a raw-text match is a false ABORT (rtk_run.ps1 first text, :46). A live
# entry is "(fast-forward-entry" outside a comment (UG 7.6.1 p261; addendum (5)). Dot-sourced by rtk_run.ps1 and
# ff_check_selftest.ps1.
function Remove-MtlComments([string]$text) {
    if ($null -eq $text) { return '' }
    (($text -split "`r?`n") | ForEach-Object { $i = $_.IndexOf(';'); if ($i -ge 0) { $_.Substring(0, $i) } else { $_ } }) -join "`n"
}
function Test-FastForwardEntry([string]$text) {
    (Remove-MtlComments $text) -match '\(\s*fast-forward-entry\b'
}
