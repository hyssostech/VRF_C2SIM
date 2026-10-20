# Self-test for ff_check.ps1 (S1-1). Red first: the OLD rule (a raw-text match, rtk_run.ps1 first text) must FAIL the
# shipped-file shape, and the NEW rule must pass every case. With -File, also prints both rules' verdicts on a real
# file (run it on a COPY of the box's fastForwardSettings.mtl). Exit 0 = all pass, 1 = a failure.
param([string]$File = '')
. (Join-Path $PSScriptRoot 'ff_check.ps1')
$old = { param($t) $t -match 'fast-forward-entry' }
$shipped = @"
;; fastForwardSettings.mtl
;; Example:
;; (fast-forward-entry
;;    (play-speed 3)
;;    (frame-mode fixed-frame)
;; )

(fast-forward-settings
)
"@
$live = $shipped + "`n(fast-forward-settings`n   (fast-forward-entry (play-speed 3) (frame-mode fixed-frame))`n)`n"
$trailing = "(fast-forward-settings (fast-forward-entry (play-speed 6)) ) ;; a trailing comment`n"
$commentAfterCode = "(fast-forward-settings ) ;; (fast-forward-entry (play-speed 6))`n"
$cases = @(
    @{ n = 'shipped shape (comments only, empty block)'; t = $shipped; want = $false },
    @{ n = 'one live entry';                              t = $live;    want = $true },
    @{ n = 'live entry before a trailing comment';        t = $trailing; want = $true },
    @{ n = 'entry only inside a trailing comment';        t = $commentAfterCode; want = $false },
    @{ n = 'empty text';                                  t = '';       want = $false },
    @{ n = 'CRLF shipped shape';                          t = ($shipped -replace "`n", "`r`n"); want = $false }
)
$fail = 0
foreach ($c in $cases) {
    $got = Test-FastForwardEntry $c.t
    $ok = ($got -eq $c.want)
    if (-not $ok) { $fail++ }
    "{0} NEW {1} (want {2}): {3}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $got, $c.want, $c.n
}
# red first: the old rule must get the shipped shape WRONG, or this test does not show the defect
$redOld = & $old $shipped
if ($redOld) { "PASS RED-FIRST: the OLD raw-text rule ABORTs on the shipped shape (the S1-1 defect reproduced)" }
else { "FAIL RED-FIRST: the OLD rule did not reproduce the defect - the fixture does not match the finding"; $fail++ }
if ($File) {
    if (-not (Test-Path -LiteralPath $File)) { "FAIL -File not found: $File"; exit 1 }
    $t = Get-Content -LiteralPath $File -Raw
    "FILE {0}: OLD rule entry={1}; NEW rule entry={2}" -f $File, (& $old $t), (Test-FastForwardEntry $t)
}
if ($fail) { "SELFTEST FAILED ($fail)"; exit 1 } else { "SELFTEST PASSED"; exit 0 }
