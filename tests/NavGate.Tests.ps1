# tests/NavGate.Tests.ps1 - OFFLINE check for tools\navdata\nav_gate.py (the abstract-graph
# connectivity gate, ratio = Average Neighbor Node Count / (Average Node Count - 1), gate 0.9
# on ANY sector). No simulator, no MAK install, no network. Plain pwsh (no Pester): exits 0
# when every check passes, 1 otherwise, one line per check.
#
#   pwsh -NoProfile -File tests\NavGate.Tests.ps1 [-Python <python.exe>]
#
# The controls are SYNTHETIC logs written by nav_gate.py --write-control, in the log format
# the record quotes in fragments only (format status [A] - see nav_gate.py's docstring).
# Passing them is SELF-CONSISTENCY of parser and generator, not validation against a real
# vrfNavGenerator log. DIRTY CONTROLS RUN FIRST: a gate never seen to fail proves nothing.
#
#   A. empty log            -> exit 2 (instrument failure; a log that yields nothing never passes)
#   B. dirty (one sector 0.40 at (7,3)) -> exit 1, names (7,3), 1 sector < 0.9, 1 < 0.5
#   C. WEST20-shaped (published 234 / 1,598 < 0.5, 409 < 0.9; WEST20 :300) -> exit 1, numbers reproduced
#   D. clean (1,600 sectors at 1.00) -> exit 0

param(
    [string]$Python = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Gate = Join-Path $RepoRoot 'tools\navdata\nav_gate.py'

if (-not $Python) {
    $cand = Join-Path $env:LOCALAPPDATA 'Programs\Python\Python312\python.exe'
    if (Test-Path $cand) { $Python = $cand } else { $Python = 'python' }
}

$script:Pass = 0
$script:Fail = 0
function Check([string]$Name, [bool]$Ok, [string]$Detail = '') {
    if ($Ok) { $script:Pass++; Write-Output "PASS  $Name" }
    else { $script:Fail++; Write-Output "FAIL  $Name  $Detail" }
}

$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("navgate-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null

function Run-Gate([string]$Log) {
    $out = & $Python $Gate $Log --json
    $code = $LASTEXITCODE
    $obj = $null
    if ($out) { $obj = ($out -join "`n") | ConvertFrom-Json }
    return @{ Code = $code; Json = $obj }
}

try {
    # A. empty log - instrument failure
    $empty = Join-Path $tmp 'empty.log'
    Set-Content -Path $empty -Value 'no report blocks here' -Encoding ascii
    $r = Run-Gate $empty
    Check 'A empty log -> exit 2' ($r.Code -eq 2) "exit=$($r.Code)"
    Check 'A empty log -> verdict INSTRUMENT FAILURE' ($r.Json.verdict -eq 'INSTRUMENT FAILURE') "$($r.Json.verdict)"

    foreach ($k in 'dirty', 'west20', 'clean') {
        & $Python $Gate --write-control $k (Join-Path $tmp "$k.log") | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "could not write the $k control" }
    }

    # B. dirty control
    $r = Run-Gate (Join-Path $tmp 'dirty.log')
    Check 'B dirty -> exit 1' ($r.Code -eq 1) "exit=$($r.Code)"
    Check 'B dirty -> 1 sector < 0.9' ($r.Json.sectors_lt_0_9 -eq 1) "$($r.Json.sectors_lt_0_9)"
    Check 'B dirty -> 1 sector < 0.5' ($r.Json.sectors_lt_0_5_fragmented -eq 1) "$($r.Json.sectors_lt_0_5_fragmented)"
    $f = @($r.Json.failing_sectors)
    Check 'B dirty -> names sector (7,3) at 0.4' (($f.Count -eq 1) -and ($f[0].i -eq 7) -and ($f[0].j -eq 3) -and ([math]::Abs($f[0].ratio - 0.4) -lt 1e-6)) ($f | ConvertTo-Json -Compress)
    $txt = & $Python $Gate (Join-Path $tmp 'dirty.log')
    Check 'B dirty -> text output lists FAILING sector (7,3)' ([bool](($txt -join "`n") -match 'FAILING sector \(7,3\) ratio 0\.4000')) ''

    # C. WEST20-shaped control (self-consistency with the published numbers)
    $r = Run-Gate (Join-Path $tmp 'west20.log')
    Check 'C west20 -> exit 1' ($r.Code -eq 1) "exit=$($r.Code)"
    Check 'C west20 -> 1,600 sectors, 1,598 measured, 2 without a report' (($r.Json.sectors_total -eq 1600) -and ($r.Json.sectors_measured -eq 1598) -and ($r.Json.sectors_no_report -eq 2)) "$($r.Json.sectors_total)/$($r.Json.sectors_measured)/$($r.Json.sectors_no_report)"
    Check 'C west20 -> 234 fragmented (< 0.5)' ($r.Json.sectors_lt_0_5_fragmented -eq 234) "$($r.Json.sectors_lt_0_5_fragmented)"
    Check 'C west20 -> 409 below 0.9' ($r.Json.sectors_lt_0_9 -eq 409) "$($r.Json.sectors_lt_0_9)"
    Check 'C west20 -> 1,118 exactly 1.00, median 1.0' (($r.Json.sectors_eq_1 -eq 1118) -and ($r.Json.ratio_median -eq 1.0)) "$($r.Json.sectors_eq_1) / $($r.Json.ratio_median)"
    Check 'C west20 -> 1,600 Generated rows, Generation time 1429.81' (($r.Json.generated_rows -eq 1600) -and ($r.Json.generation_time_s -eq 1429.81)) "$($r.Json.generated_rows) / $($r.Json.generation_time_s)"

    # D. clean control
    $r = Run-Gate (Join-Path $tmp 'clean.log')
    Check 'D clean -> exit 0' ($r.Code -eq 0) "exit=$($r.Code)"
    Check 'D clean -> 1,600 measured, 0 < 0.9, min 1.0' (($r.Json.sectors_measured -eq 1600) -and ($r.Json.sectors_lt_0_9 -eq 0) -and ($r.Json.ratio_min -eq 1.0)) "$($r.Json.sectors_measured)/$($r.Json.sectors_lt_0_9)/$($r.Json.ratio_min)"
    Check 'D clean -> tag histogram {1: 1600}' ($r.Json.tag_count_histogram.'1' -eq 1600) ''

    # E. area-dir tripwire (byte size + manifest hash) on a tiny folder
    $area = Join-Path $tmp 'area'
    New-Item -ItemType Directory -Path $area | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $area 'a.bin'), [byte[]](1, 2, 3))
    [System.IO.File]::WriteAllBytes((Join-Path $area 'b.bin'), [byte[]](4, 5))
    $out = & $Python $Gate (Join-Path $tmp 'clean.log') --area-dir $area --area-hash --json
    $j = ($out -join "`n") | ConvertFrom-Json
    Check 'E area-dir -> 2 files, 5 bytes, manifest hash present' (($j.area_files -eq 2) -and ($j.area_bytes -eq 5) -and ($j.area_manifest_sha256 -match '^[0-9a-f]{64}$')) "$($j.area_files)/$($j.area_bytes)"
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}

Write-Output ("NavGate.Tests: {0} passed, {1} failed" -f $script:Pass, $script:Fail)
if ($script:Fail -gt 0) { exit 1 } else { exit 0 }
