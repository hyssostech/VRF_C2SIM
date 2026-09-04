# SelfTest-FixMachinePath.ps1 - prove FixMachinePath.ps1's refusal guard ACTUALLY FIRES.
#
# WHY (2026-09-04): the guard was dead for its whole life. The line that filtered $lost
# used $_ inside a nested Where-Object, where it rebinds to the INNER pipeline item, so the
# test read "x -eq x", was always true, and every candidate was filtered away. $lost was
# unconditionally empty, the refusal was unreachable, and "[OK] no entry outside the removal
# lists is affected" printed on every run without checking anything. A safety check nobody
# has ever seen FAIL is not a safety check (memory: lessons-false-greens).
#
# This test runs the guard in both directions:
#   NEGATIVE CONTROL - a mutant copy is made that drops one arbitrary entry which is on NO
#                      removal list. The guard MUST refuse (exit 1, "REFUSING").
#   POSITIVE CONTROL - the REAL script, unmodified. The guard MUST pass (exit 0, "[OK]").
# Both run in DRY-RUN. Neither can write: -Apply is never passed, and the mutant lives in a
# temp directory. Needs no elevation.
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'FixMachinePath.ps1'
if (-not (Test-Path -LiteralPath $script)) { Write-Host "[FAIL] not found: $script"; exit 1 }

$fails = 0
function Check { param([string]$what, [bool]$ok)
    if ($ok) { Write-Host ('  [OK]   ' + $what) } else { Write-Host ('  [FAIL] ' + $what); $script:fails++ }
}

Write-Host ''
Write-Host '=== SelfTest-FixMachinePath ==='

# ---- pick a victim: an entry that EXISTS in the machine PATH and is on no removal list ----
$raw = (Get-Item -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment').GetValue('Path', $null, 'DoNotExpandEnvironmentNames')
$txt = Get-Content -LiteralPath $script -Raw
# the removal lists, read from the script itself so the test cannot drift from it
$listed = @()
foreach ($m in [regex]::Matches($txt, "(?m)^\s*'([^']+)',?\s*$")) { $listed += $m.Groups[1].Value.TrimEnd('\').ToLower() }
$victim = ($raw -split ';' | Where-Object { $_ -and ($listed -notcontains $_.TrimEnd('\').ToLower()) } | Select-Object -Last 1)
if (-not $victim) { Write-Host '  [FAIL] could not pick a victim entry'; exit 1 }
Write-Host ("  victim (on no removal list): " + $victim)

# ---- build the mutant ----
$anchor = '    $keep += $e'
if ($txt -notmatch [regex]::Escape($anchor)) { Write-Host '  [FAIL] anchor line not found - the script changed shape; update this test'; exit 1 }
$inject = '    if ($env:FIXPATH_SELFTEST_DROP -and ($norm -eq $env:FIXPATH_SELFTEST_DROP)) { $dropped += (''(injected) '' + $e); continue }' + "`r`n" + $anchor
# literal String.Replace, NOT -replace: the text is full of backslashes and $ signs, which
# the regex operator would eat as escapes and substitution groups
$mutTxt = $txt.Replace($anchor, $inject)
Check 'mutant differs from the original (the injection actually landed)' ($mutTxt -cne $txt)

$tmp = Join-Path ([IO.Path]::GetTempPath()) ('fixpath_selftest_' + [Guid]::NewGuid().ToString('N') + '.ps1')
[IO.File]::WriteAllText($tmp, $mutTxt)
try {
    # ---- NEGATIVE CONTROL: the guard must REFUSE ----
    $env:FIXPATH_SELFTEST_DROP = $victim.TrimEnd('\').ToLower()
    $out = & pwsh -NoProfile -File $tmp 2>&1 | Out-String
    $code = $LASTEXITCODE
    Write-Host ''
    Write-Host '  -- negative control (one un-listed entry dropped) --'
    Check 'guard REFUSED (exit 1)'                  ($code -eq 1)
    Check 'printed REFUSING'                        ($out -match 'REFUSING')
    Check 'named the victim entry'                  ($out -match [regex]::Escape($victim))
    Check 'did NOT print the all-clear'             ($out -notmatch 'no entry outside the removal lists is affected')
    if ($code -ne 1) { Write-Host '  ---- mutant output ----'; Write-Host $out }
} finally {
    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    Remove-Item Env:\FIXPATH_SELFTEST_DROP -ErrorAction SilentlyContinue
}

# ---- POSITIVE CONTROL: the real script, unmodified, must PASS ----
Write-Host ''
Write-Host '  -- positive control (real script, dry-run, both switch settings) --'
foreach ($legacy in @($false, $true)) {
    $args2 = @('-NoProfile', '-File', $script)
    if ($legacy) { $args2 += '-RemoveLegacyMak' }
    $out2 = & pwsh @args2 2>&1 | Out-String
    $c2 = $LASTEXITCODE
    $tag = $(if ($legacy) { '-RemoveLegacyMak' } else { '(default)' })
    Check ("real script $tag exits 0"               ) ($c2 -eq 0)
    Check ("real script $tag prints the all-clear"  ) ($out2 -match 'no entry outside the removal lists is affected')
    Check ("real script $tag wrote nothing"         ) ($out2 -match 'DRY-RUN\] nothing was written')
}

# ---- BITNESS GATE (added 2026-09-04) ----
# The script classifies entries with Test-Path, and a 32-bit process on 64-bit Windows has
# System32 redirected to SysWOW64 - so it must refuse to -Apply from 32-bit. Exercised
# against a copy whose ONLY write is replaced by a marker, so -Apply is risk-free here AND
# "it never reached the write" is provable instead of assumed.
Write-Host ''
Write-Host '  -- bitness gate --'
$pw32 = 'C:\Program Files (x86)\PowerShell\7\pwsh.exe'
$pw64 = 'C:\Program Files\PowerShell\7\pwsh.exe'
$w = "Set-ItemProperty -LiteralPath `$key -Name 'Path' -Value `$new -Type `$kind"
if ($txt -notmatch [regex]::Escape($w)) {
    Check 'write line found for neutering (script changed shape; update this test)' $false
} else {
    $safe = $txt.Replace($w, "Write-Host '  !!! REACHED THE WRITE (neutered) !!!'")
    Check 'neutered copy differs from the original' ($safe -cne $txt)
    $tb = Join-Path ([IO.Path]::GetTempPath()) ('fixpath_bitness_' + [Guid]::NewGuid().ToString('N') + '.ps1')
    [IO.File]::WriteAllText($tb, $safe)
    try {
        if (Test-Path -LiteralPath $pw32) {
            $o = & $pw32 -NoProfile -File $tb -Apply 2>&1 | Out-String
            $c = $LASTEXITCODE
            Check '32-bit -Apply refuses (exit 2)'        ($c -eq 2)
            Check '32-bit -Apply says REFUSING'           ($o -match 'REFUSING to -Apply from a 32-bit process')
            Check '32-bit -Apply never reaches the write' ($o -notmatch 'REACHED THE WRITE')
            Check 'bitness gates BEFORE the elevation test' ($o -notmatch 'needs an ELEVATED shell')
            $o = & $pw32 -NoProfile -File $tb 2>&1 | Out-String
            Check '32-bit dry-run still analyses (exit 0)' ($LASTEXITCODE -eq 0)
            Check '32-bit dry-run warns about WOW64'       ($o -match '32-BIT PowerShell on 64-bit Windows')
        } else { Write-Host '  [skip] no 32-bit pwsh on this machine' }
        if (Test-Path -LiteralPath $pw64) {
            $o = & $pw64 -NoProfile -File $tb 2>&1 | Out-String
            Check '64-bit dry-run exits 0'        ($LASTEXITCODE -eq 0)
            Check '64-bit dry-run does NOT warn'  ($o -notmatch '32-BIT PowerShell')
        } else { Write-Host '  [skip] no 64-bit pwsh at the expected path' }
    } finally { Remove-Item -LiteralPath $tb -Force -ErrorAction SilentlyContinue }
}

Write-Host ''
if ($fails -eq 0) { Write-Host '  SELFTEST PASS - the refusal guard fires when it should and passes when it should'; exit 0 }
Write-Host ("  SELFTEST FAIL - " + $fails + ' check(s) failed'); exit 1
