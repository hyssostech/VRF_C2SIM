# tests/RecordChecks.ps1 - PURE helpers for the U2 RECORD CHECKS (section 13 of
# tests\RunnerTurnaround.Tests.ps1). Dot-sourced by that suite; it starts nothing,
# reads no vendor log, and writes only when a generator switch is passed explicitly.
#
# WHY A SEPARATE FILE: every record check ships with a DIRTY control and a CLEAN
# control, and both must go through the SAME code as the real tree. Text-in /
# objects-out functions are the only way to make that literally true: the controls
# pass a string, the tree pass passes the file's text, one function decides.
#
# WHY tests\ AND NOT scripts\ (a deliberate deviation from the lane brief, which
# offered scripts\RecordChecks.ps1 or "inside the test file"): scripts\ is the LIVE
# runner surface - it is deployed, it is enumerated by checks 7 / 12 / 12b, and a
# record-linting helper there would be scanned as if it were part of the launch
# path. The data these functions read (tests\tripwire_allowlist.txt,
# tests\ruling_claims_baseline.txt) already lives in tests\. Nothing here is
# reachable from a run.
# pwsh 7.6.1 NOTE, measured on this box (scratchpad laneC_probe4.ps1): the array
# subexpression @(...) on a System.Collections.Generic.List[object] throws
# "Argument types do not match" - for ANY element type, including int. It works on
# List[psobject], List[string] and ArrayList. Every accumulator below is therefore a
# List[psobject]; do not "tidy" it back to List[object].
Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------
# Rule data (single source for the checks AND for the generators)
# ---------------------------------------------------------------------------

# Handoff sec 4 rule 7 / AUDIT S3: "tripwire phrases are scanned, not remembered".
# Both rules are proximity rules on ONE physical line: the record has 1,800-character
# lines, so "near" has to mean character distance, not line distance.
$script:TripwireRules = @(
    [pscustomobject]@{
        Id     = 'T1'
        Name   = 'buried/underground within 80 chars of never-moves/stationary/freeze'
        First  = 'buried|underground'
        Second = 'never mov|stationary|did not move|sitting still|freeze|froze'
        Window = 80
    },
    [pscustomobject]@{
        Id     = 'T2'
        Name   = '"R4" within 80 chars of move-along/MOVE task/a move/mover'
        First  = 'R4'
        Second = 'move-along|MOVE task|a move|mover'
        Window = 80
    }
)

# Handoff sec 4 rule 1 [CHECK]. Lane B may hand the seat a wider pattern; it is one
# variable on purpose so widening it is a one-line change.
$script:RulingClaimPattern = 'USER RULING|RULED[^|]{0,40}\(user|owner ruling|user ruling'
$script:RulingIdPattern    = 'RL-\d{8}-\d{2}'

# AUDIT sec 3 DOC CHANGES item 7: the 200-line cap was met with 1,868-character
# lines, so a cap is a PAIR (lines, max line length).
$script:LiveDocRelPaths = @(
    'docs\HANDOFF_2026-09-14_PARALLEL_LANES.md',
    'docs\DEMO_READINESS_2026-09-06.md',
    'docs\RUNBOOK.md',
    'docs\DEMO_RUNBOOK.md'
)
$script:RulingLedgerRelPaths = @('docs\RULINGS.md', 'docs\RULINGS_ARCHIVE.md')

# C-4: only preregs written AFTER the cut are subject to the lint.
$script:PreregLintCutoff = [datetime]::new(2026, 9, 21)

# ---------------------------------------------------------------------------
# Text primitives
# ---------------------------------------------------------------------------

function Get-RecordTextLines {
    param([string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return @() }
    $t = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    $parts = $t.Split("`n")
    if ($parts.Length -eq 1) {
        if ($parts[0] -eq '') { return @() }
        return @($parts)
    }
    if ($parts[$parts.Length - 1] -eq '') { $parts = $parts[0..($parts.Length - 2)] }
    return @($parts)
}

function Get-RecordLineFingerprint {
    param([string]$Line)
    $norm = ([regex]::Replace([string]$Line, '\s+', ' ')).Trim()
    $sha = [System.Security.Cryptography.SHA1]::Create()
    try {
        $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($norm))
    } finally {
        $sha.Dispose()
    }
    return (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-RecordRelativePath {
    param([string]$RepoRoot, [string]$FullName)
    $root = [System.IO.Path]::GetFullPath($RepoRoot)
    if (-not $root.EndsWith('\')) { $root = $root + '\' }
    $full = [System.IO.Path]::GetFullPath($FullName)
    if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($root.Length)
    }
    return $full
}

function Test-RecordPathExcluded {
    param([string]$RelPath)
    $p = ('\' + ([string]$RelPath).Replace('/', '\') + '\').ToLowerInvariant()
    foreach ($seg in @('\.claude\', '\bin\', '\obj\', '\runs\')) {
        if ($p.Contains($seg)) { return $true }
    }
    return $false
}

# docs\**\*.md + src\**\*.cs + src\**\appsettings*.json, minus .claude\ bin\ obj\ runs\.
function Get-RecordScanFiles {
    param([string]$RepoRoot)
    $out = New-Object System.Collections.Generic.List[psobject]
    $docs = Join-Path $RepoRoot 'docs'
    if (Test-Path -LiteralPath $docs -PathType Container) {
        foreach ($f in @(Get-ChildItem -LiteralPath $docs -Recurse -File -Filter '*.md')) {
            $rel = Get-RecordRelativePath -RepoRoot $RepoRoot -FullName $f.FullName
            if (-not (Test-RecordPathExcluded -RelPath $rel)) {
                $out.Add([pscustomobject]@{ FullName = $f.FullName; RelPath = $rel })
            }
        }
    }
    $src = Join-Path $RepoRoot 'src'
    if (Test-Path -LiteralPath $src -PathType Container) {
        foreach ($f in @(Get-ChildItem -LiteralPath $src -Recurse -File)) {
            if ($f.Extension -ne '.cs' -and -not ($f.Name -like 'appsettings*.json')) { continue }
            $rel = Get-RecordRelativePath -RepoRoot $RepoRoot -FullName $f.FullName
            if (-not (Test-RecordPathExcluded -RelPath $rel)) {
                $out.Add([pscustomobject]@{ FullName = $f.FullName; RelPath = $rel })
            }
        }
    }
    return @($out | Sort-Object RelPath)
}

# ---------------------------------------------------------------------------
# C-1  DOC CAPS - line count AND max line length
# ---------------------------------------------------------------------------

function Get-DocCapFromText {
    param([string]$Text, [int]$MaxLineLength = 160)
    $lines = @(Get-RecordTextLines -Text $Text)
    $max = 0
    $maxNo = 0
    $over = New-Object System.Collections.Generic.List[psobject]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $len = ([string]$lines[$i]).Length
        if ($len -gt $max) { $max = $len; $maxNo = $i + 1 }
        if ($len -gt $MaxLineLength) { $over.Add([pscustomobject]@{ LineNumber = $i + 1; Length = $len }) }
    }
    return [pscustomobject]@{
        Exists         = $true
        LineCount      = $lines.Count
        MaxLineLength  = $max
        MaxLineNumber  = $maxNo
        OverLines      = @($over)
    }
}

function Get-DocCapMeasurement {
    param([string]$Path, [int]$MaxLineLength = 160)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [pscustomobject]@{
            Exists = $false; LineCount = 0; MaxLineLength = 0; MaxLineNumber = 0; OverLines = @()
        }
    }
    return (Get-DocCapFromText -Text ([System.IO.File]::ReadAllText($Path)) -MaxLineLength $MaxLineLength)
}

function Format-DocCapDetail {
    param([object]$Measurement, [int]$MaxLines, [int]$MaxLineLength)
    if (-not $Measurement.Exists) { return 'ABSENT' }
    return ('lines={0}/{1}, maxlinelen={2}/{3} (at line {4}), lines over cap={5}' -f `
            $Measurement.LineCount, $MaxLines, $Measurement.MaxLineLength, $MaxLineLength,
            $Measurement.MaxLineNumber, @($Measurement.OverLines).Count)
}

# ---------------------------------------------------------------------------
# C-2  TRIPWIRE PHRASE SCAN
# ---------------------------------------------------------------------------

function Get-RecordOffsetLine {
    param([int[]]$LineStarts, [int]$Offset)
    $lo = 0
    $hi = @($LineStarts).Count - 1
    $ans = 0
    while ($lo -le $hi) {
        $mid = [int][math]::Floor(($lo + $hi) / 2)
        if ($LineStarts[$mid] -le $Offset) { $ans = $mid; $lo = $mid + 1 } else { $hi = $mid - 1 }
    }
    return ($ans + 1)
}

# The scan is TEXT-scoped, not line-scoped: the record wraps its prose, so a phrase
# pair can straddle a newline and a per-line scan would never see it. MEASURED on
# main ac1ec58: text-scoped finds 40 hits, per-line finds 29. A newline counts as
# one character.
#
# KNOWN MISSES, measured on main ac1ec58 and REPORTED to the seat rather than fixed
# here - the phrases and the 80-character window are the fresh-start handoff sec 4
# rule 7 contract, and a check does not widen its own contract:
#   - RUNBOOK.md:3553 "... meeting R4 ... NARROWING that rule for MOVE tasks only":
#     the gap is 98 characters, outside T2's 80. A window of 120 reaches it; the
#     next T2 pair anywhere in the tree sits at 183, so 120 adds no other hit.
#   - appsettings.Demo.json:40 "sitting still under the ground": "under the ground"
#     is not "underground", so T1's FIRST phrase never matches.
#   - VrfC2SimService.cs:3328 "The two buried platforms happened not to move":
#     "happened not to move" is not "did not move", so T1's SECOND phrase never
#     matches.
# All three are audit-named sites. Window and phrases are data on
# $script:TripwireRules above; each change is one line.
#
# The fingerprint is taken over the MATCHED SPAN (first phrase to second phrase), not
# over the physical line: re-wrapping a paragraph then does NOT invalidate a reviewed
# allowlist entry, while changing the words between the two phrases does.
#
# At most one hit per occurrence of the FIRST phrase (its nearest partner), so a dense
# falsification section does not produce a combinatorial pile of entries.
function Find-TripwireHits {
    param([string]$Text, [string]$RelPath = '(control)')
    $norm = ([string]$Text).Replace("`r`n", "`n").Replace("`r", "`n")
    $lineStarts = New-Object System.Collections.Generic.List[int]
    $lineStarts.Add(0)
    for ($i = 0; $i -lt $norm.Length; $i++) {
        if ($norm[$i] -eq "`n") { $lineStarts.Add($i + 1) }
    }
    $starts = $lineStarts.ToArray()
    $lines = @(Get-RecordTextLines -Text $norm)
    $hits = New-Object System.Collections.Generic.List[psobject]
    foreach ($rule in $script:TripwireRules) {
        $aM = [regex]::Matches($norm, $rule.First, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($aM.Count -eq 0) { continue }
        $bM = [regex]::Matches($norm, $rule.Second, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($bM.Count -eq 0) { continue }
        foreach ($ma in $aM) {
            $bestGap = -1
            $bestStart = 0
            $bestEnd = 0
            foreach ($mb in $bM) {
                if ($ma.Index -le $mb.Index) {
                    $g = $mb.Index - ($ma.Index + $ma.Length)
                    $s = $ma.Index
                    $e = $mb.Index + $mb.Length
                } else {
                    $g = $ma.Index - ($mb.Index + $mb.Length)
                    $s = $mb.Index
                    $e = $ma.Index + $ma.Length
                }
                if ($g -lt 0) { $g = 0 }
                if ($g -gt $rule.Window) { continue }
                if ($bestGap -lt 0 -or $g -lt $bestGap) { $bestGap = $g; $bestStart = $s; $bestEnd = $e }
            }
            if ($bestGap -lt 0) { continue }
            $lineNo = Get-RecordOffsetLine -LineStarts $starts -Offset $bestStart
            $lineText = ''
            if ($lineNo -ge 1 -and $lineNo -le $lines.Count) { $lineText = [string]$lines[$lineNo - 1] }
            $hits.Add([pscustomobject]@{
                RelPath     = $RelPath
                LineNumber  = $lineNo
                RuleId      = $rule.Id
                RuleName    = $rule.Name
                Gap         = $bestGap
                Line        = $lineText
                Span        = $norm.Substring($bestStart, $bestEnd - $bestStart)
                Fingerprint = (Get-RecordLineFingerprint -Line $norm.Substring($bestStart, $bestEnd - $bestStart))
            })
        }
    }
    return @($hits | Sort-Object LineNumber, RuleId)
}

function Get-TripwireTreeHits {
    param([string]$RepoRoot)
    $hits = New-Object System.Collections.Generic.List[psobject]
    foreach ($f in @(Get-RecordScanFiles -RepoRoot $RepoRoot)) {
        $text = [System.IO.File]::ReadAllText($f.FullName)
        foreach ($h in @(Find-TripwireHits -Text $text -RelPath $f.RelPath)) { $hits.Add($h) }
    }
    return @($hits | Sort-Object RelPath, LineNumber, RuleId)
}

# One entry per allowed hit: relative path + rule id + fingerprint of the
# whitespace-normalised line + a reason. The fingerprint (not the line number) is
# what makes an allowlist survive a reflow - and what makes an EDIT of the line
# show up as a new, unreviewed hit.
function Read-TripwireAllowlist {
    param([string]$Path)
    $entries = New-Object System.Collections.Generic.List[psobject]
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return @($entries) }
    $lineNo = 0
    foreach ($raw in @(Get-RecordTextLines -Text ([System.IO.File]::ReadAllText($Path)))) {
        $lineNo++
        $t = ([string]$raw).Trim()
        if ($t.Length -eq 0 -or $t.StartsWith('#')) { continue }
        $parts = $t.Split('|')
        if ($parts.Count -lt 4) {
            $entries.Add([pscustomobject]@{
                SourceLine = $lineNo; Malformed = $true; RelPath = ''; RuleId = ''; Fingerprint = ''; Reason = $t
            })
            continue
        }
        $entries.Add([pscustomobject]@{
            SourceLine  = $lineNo
            Malformed   = $false
            RelPath     = $parts[0].Trim()
            RuleId      = $parts[1].Trim()
            Fingerprint = $parts[2].Trim()
            Reason      = (($parts[3..($parts.Count - 1)] -join '|')).Trim()
        })
    }
    return @($entries)
}

function Get-TripwireEntryKey {
    param([string]$RelPath, [string]$RuleId, [string]$Fingerprint)
    return (('{0}|{1}|{2}' -f $RelPath, $RuleId, $Fingerprint)).ToLowerInvariant()
}

# Splits current hits against the allowlist. NOTHING here writes.
function Compare-TripwireHits {
    param([object[]]$Hits, [object[]]$Entries)
    $allowed = @{}
    foreach ($e in @($Entries)) {
        if ($e.Malformed) { continue }
        $allowed[(Get-TripwireEntryKey -RelPath $e.RelPath -RuleId $e.RuleId -Fingerprint $e.Fingerprint)] = $e
    }
    $seen = @{}
    $unallowed = New-Object System.Collections.Generic.List[psobject]
    foreach ($h in @($Hits)) {
        $k = Get-TripwireEntryKey -RelPath $h.RelPath -RuleId $h.RuleId -Fingerprint $h.Fingerprint
        if ($allowed.ContainsKey($k)) { $seen[$k] = $true; continue }
        $unallowed.Add($h)
    }
    $stale = New-Object System.Collections.Generic.List[psobject]
    foreach ($e in @($Entries)) {
        if ($e.Malformed) { continue }
        $k = Get-TripwireEntryKey -RelPath $e.RelPath -RuleId $e.RuleId -Fingerprint $e.Fingerprint
        if (-not $seen.ContainsKey($k)) { $stale.Add($e) }
    }
    return [pscustomobject]@{
        Unallowed = @($unallowed)
        Stale     = @($stale)
        Malformed = @(@($Entries) | Where-Object { $_.Malformed })
    }
}

function Format-TripwireHit {
    param([object]$Hit)
    $snippet = ([regex]::Replace([string]$Hit.Line, '\s+', ' ')).Trim()
    if ($snippet.Length -gt 120) { $snippet = $snippet.Substring(0, 117) + '...' }
    return ('{0}:{1} [{2} {3}, gap {4}] {5}' -f $Hit.RelPath, $Hit.LineNumber, $Hit.RuleId, $Hit.RuleName, $Hit.Gap, $snippet)
}

# ---------------------------------------------------------------------------
# C-3  RULING IDS
# ---------------------------------------------------------------------------

# "Same paragraph / table row", defined exactly:
#  - a claim on a line whose first non-space character is '|' is a TABLE ROW, and
#    its unit is that one physical line (an id in the row above does NOT count);
#  - otherwise the unit is the PARAGRAPH: the run of consecutive lines around it,
#    stopping at a blank line or at a table row (so a table cannot leak an id into
#    the prose next to it).
function Get-RulingClaimUnit {
    param([string[]]$Lines, [int]$Index)
    $cur = [string]$Lines[$Index]
    if ($cur.TrimStart().StartsWith('|')) {
        return [pscustomobject]@{ Kind = 'table-row'; Start = $Index; End = $Index; Text = $cur }
    }
    $s = $Index
    while ($s -gt 0) {
        $prev = [string]$Lines[$s - 1]
        if ($prev.Trim().Length -eq 0) { break }
        if ($prev.TrimStart().StartsWith('|')) { break }
        $s--
    }
    $e = $Index
    while ($e -lt ($Lines.Count - 1)) {
        $next = [string]$Lines[$e + 1]
        if ($next.Trim().Length -eq 0) { break }
        if ($next.TrimStart().StartsWith('|')) { break }
        $e++
    }
    return [pscustomobject]@{ Kind = 'paragraph'; Start = $s; End = $e; Text = (@($Lines[$s..$e]) -join "`n") }
}

function Find-RulingClaims {
    param(
        [string]$Text,
        [string]$RelPath = '(control)',
        [string]$ClaimPattern = $null
    )
    if ([string]::IsNullOrEmpty($ClaimPattern)) { $ClaimPattern = $script:RulingClaimPattern }
    $lines = @(Get-RecordTextLines -Text $Text)
    $out = New-Object System.Collections.Generic.List[psobject]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if (-not [regex]::IsMatch([string]$lines[$i], $ClaimPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) { continue }
        $unit = Get-RulingClaimUnit -Lines $lines -Index $i
        $ids = @([regex]::Matches($unit.Text, $script:RulingIdPattern) | ForEach-Object { $_.Value } | Sort-Object -Unique)
        $out.Add([pscustomobject]@{
            RelPath    = $RelPath
            LineNumber = $i + 1
            UnitKind   = $unit.Kind
            UnitStart  = $unit.Start + 1
            UnitEnd    = $unit.End + 1
            Ids        = $ids
            Line       = [string]$lines[$i]
        })
    }
    return @($out)
}

function Get-RulingLedgerIds {
    param([string]$RepoRoot, [string[]]$LedgerRelPaths = $null)
    if ($null -eq $LedgerRelPaths) { $LedgerRelPaths = $script:RulingLedgerRelPaths }
    $ids = New-Object System.Collections.Generic.List[string]
    foreach ($rel in @($LedgerRelPaths)) {
        $p = Join-Path $RepoRoot $rel
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { continue }
        foreach ($m in [regex]::Matches([System.IO.File]::ReadAllText($p), $script:RulingIdPattern)) {
            if (-not $ids.Contains($m.Value)) { $ids.Add($m.Value) }
        }
    }
    return @($ids)
}

# A claim is SOUND when its unit carries at least one id AND at least one of those
# ids exists in the ledger. Pass an empty $LedgerIds to test the id-placement rule
# alone (that is what the controls do).
function Test-RulingClaimSound {
    param([object]$Claim, [string[]]$LedgerIds)
    if (@($Claim.Ids).Count -eq 0) { return [pscustomobject]@{ Sound = $false; Why = 'no RL-id in the same ' + $Claim.UnitKind } }
    foreach ($id in @($Claim.Ids)) {
        if (@($LedgerIds) -contains $id) { return [pscustomobject]@{ Sound = $true; Why = '' } }
    }
    return [pscustomobject]@{ Sound = $false; Why = ('id(s) ' + (@($Claim.Ids) -join ',') + ' not found in the ruling ledger') }
}

# The ratchet population: every docs\**\*.md that is NOT a live doc and NOT the
# ledger itself. (The ledger is excluded on purpose: it does not exist yet, and the
# moment lane B creates it every row would count as a "new file with un-id'd
# claims" - a false red produced by another lane doing its job correctly.)
function Get-RulingClaimCounts {
    param([string]$RepoRoot, [string[]]$ExcludeRelPaths = $null)
    if ($null -eq $ExcludeRelPaths) { $ExcludeRelPaths = @($script:LiveDocRelPaths + $script:RulingLedgerRelPaths) }
    $excl = @{}
    foreach ($x in @($ExcludeRelPaths)) { $excl[$x.ToLowerInvariant()] = $true }
    $rows = New-Object System.Collections.Generic.List[psobject]
    foreach ($f in @(Get-RecordScanFiles -RepoRoot $RepoRoot)) {
        if ($f.RelPath -notlike '*.md') { continue }
        if ($excl.ContainsKey($f.RelPath.ToLowerInvariant())) { continue }
        $claims = @(Find-RulingClaims -Text ([System.IO.File]::ReadAllText($f.FullName)) -RelPath $f.RelPath)
        $un = @($claims | Where-Object { @($_.Ids).Count -eq 0 })
        $rows.Add([pscustomobject]@{ RelPath = $f.RelPath; Total = $claims.Count; Unidentified = $un.Count })
    }
    return @($rows | Sort-Object RelPath)
}

function Read-RulingClaimsBaseline {
    param([string]$Path)
    $map = @{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $map }
    foreach ($raw in @(Get-RecordTextLines -Text ([System.IO.File]::ReadAllText($Path)))) {
        $t = ([string]$raw).Trim()
        if ($t.Length -eq 0 -or $t.StartsWith('#')) { continue }
        $parts = $t.Split('|')
        if ($parts.Count -lt 2) { continue }
        $n = 0
        if ([int]::TryParse($parts[1].Trim(), [ref]$n)) { $map[$parts[0].Trim().ToLowerInvariant()] = $n }
    }
    return $map
}

# RISEN = above the baseline (or absent from the baseline with a non-zero count).
# A DROP is progress: reported, never failed, never silently re-baselined.
function Compare-RulingClaimCounts {
    param([object[]]$Counts, [hashtable]$Baseline)
    $risen = New-Object System.Collections.Generic.List[psobject]
    $improved = New-Object System.Collections.Generic.List[psobject]
    foreach ($row in @($Counts)) {
        $k = $row.RelPath.ToLowerInvariant()
        $base = 0
        $known = $Baseline.ContainsKey($k)
        if ($known) { $base = [int]$Baseline[$k] }
        if ($row.Unidentified -gt $base) {
            $risen.Add([pscustomobject]@{ RelPath = $row.RelPath; Baseline = $base; Now = $row.Unidentified; New = (-not $known) })
        } elseif ($row.Unidentified -lt $base) {
            $improved.Add([pscustomobject]@{ RelPath = $row.RelPath; Baseline = $base; Now = $row.Unidentified; New = $false })
        }
    }
    return [pscustomobject]@{ Risen = @($risen); Improved = @($improved) }
}

# ---------------------------------------------------------------------------
# C-4  PREREG LINT
# ---------------------------------------------------------------------------

function Get-PreregNameDate {
    param([string]$Name)
    $m = [regex]::Match([string]$Name, '(\d{4})-(\d{2})-(\d{2})')
    if (-not $m.Success) { return $null }
    try {
        return [datetime]::new([int]$m.Groups[1].Value, [int]$m.Groups[2].Value, [int]$m.Groups[3].Value)
    } catch {
        return $null
    }
}

function Test-PreregSubject {
    param([string]$Name, [datetime]$Cutoff = [datetime]::MinValue)
    if ($Cutoff -eq [datetime]::MinValue) { $Cutoff = $script:PreregLintCutoff }
    if ($Name -notlike 'PREREG_*.md') { return $false }
    $d = Get-PreregNameDate -Name $Name
    if ($null -eq $d) { return $false }
    return ($d -gt $Cutoff)
}

# Handoff sec 4 rule 5 + AUDIT S3/S8. Returns the list of problems; empty = clean.
function Test-PreregText {
    param([string]$Text, [string]$Name = '(control)')
    $problems = New-Object System.Collections.Generic.List[string]
    $lines = @(Get-RecordTextLines -Text $Text)

    if ($Text -notmatch 'VENDOR CITATION:')     { $problems.Add('no "VENDOR CITATION:"') }
    if ($Text -notmatch 'OWN-RECORD CITATION:') { $problems.Add('no "OWN-RECORD CITATION:"') }

    $kind = $null
    foreach ($l in $lines) {
        $m = [regex]::Match([string]$l, '^\s*RUN KIND:\s*([A-Za-z-]+)\s*$')
        if ($m.Success) { $kind = $m.Groups[1].Value.ToLowerInvariant(); break }
    }
    if ($null -eq $kind) {
        $problems.Add('no "RUN KIND:" line carrying one of movement|non-movement|offline')
    } elseif (@('movement', 'non-movement', 'offline') -notcontains $kind) {
        $problems.Add(('RUN KIND: "' + $kind + '" is not one of movement|non-movement|offline'))
    }

    # A DEVIATION line only counts when it actually QUOTES the rule it departs from.
    $deviation = $false
    foreach ($l in $lines) {
        if ([string]$l -match 'DEVIATION FROM RECORD:' -and [string]$l -match '"[^"]+"') { $deviation = $true; break }
    }

    if ($kind -eq 'movement' -and -not $deviation) {
        if ($Text -notmatch '--pre-order-gate') {
            $problems.Add('movement run: no "--pre-order-gate" and no quoting "DEVIATION FROM RECORD:" line')
        }
        $console = $false
        foreach ($l in $lines) { if ([string]$l -match '^\s*CONSOLE LEVEL:\s*4\s*$') { $console = $true; break } }
        if (-not $console) {
            $problems.Add('movement run: no "CONSOLE LEVEL: 4" line and no quoting "DEVIATION FROM RECORD:" line')
        }
    }

    # AUDIT S8: DurationScale 0.25 put a 300 s armed end inside the 360 s stall
    # window and manufactured a symptom. Any value but 1.0 owes the sentence.
    $compressed = New-Object System.Collections.Generic.List[string]
    foreach ($m in [regex]::Matches($Text, 'DurationScale\s*[:=]?\s*(-?\d+(?:\.\d+)?)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $v = 0.0
        if ([double]::TryParse($m.Groups[1].Value, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$v)) {
            if ($v -ne 1.0 -and -not $compressed.Contains($m.Groups[1].Value)) { $compressed.Add($m.Groups[1].Value) }
        }
    }
    if ($compressed.Count -gt 0 -and $Text -notmatch 'ARMED ENDS VS STALL WINDOW:') {
        $problems.Add(('DurationScale ' + ($compressed -join ',') + ' != 1.0 with no "ARMED ENDS VS STALL WINDOW:" line'))
    }

    return @($problems)
}

# ---------------------------------------------------------------------------
# C-5  ASCII + CRLF (REPO CLAUDE.md sec 5)
# ---------------------------------------------------------------------------

function Test-AsciiCrlfBytes {
    param([byte[]]$Bytes, [int]$MaxProblems = 8)
    $problems = New-Object System.Collections.Generic.List[string]
    $line = 1
    $n = @($Bytes).Count
    for ($i = 0; $i -lt $n; $i++) {
        if ($problems.Count -ge $MaxProblems) { $problems.Add('... more problems suppressed'); break }
        $b = $Bytes[$i]
        if ($b -eq 13) {
            if (($i + 1) -ge $n -or $Bytes[$i + 1] -ne 10) { $problems.Add(('stray CR at byte {0} (line {1})' -f $i, $line)) }
            continue
        }
        if ($b -eq 10) {
            if ($i -eq 0 -or $Bytes[$i - 1] -ne 13) { $problems.Add(('bare LF at byte {0} (line {1})' -f $i, $line)) }
            $line++
            continue
        }
        if ($b -eq 9) { continue }
        if ($b -lt 32 -or $b -gt 126) { $problems.Add(('byte 0x{0:x2} at byte {1} (line {2}) is not printable ASCII' -f $b, $i, $line)) }
    }
    return @($problems)
}

function Test-AsciiCrlfFile {
    param([string]$Path, [int]$MaxProblems = 8)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return @('ABSENT') }
    return (Test-AsciiCrlfBytes -Bytes ([System.IO.File]::ReadAllBytes($Path)) -MaxProblems $MaxProblems)
}

# ---------------------------------------------------------------------------
# GENERATORS - reached ONLY from an explicit switch. A check that re-baselines
# itself during a normal run is a false green (AUDIT: "six tools reported success
# while doing nothing"), so nothing above ever calls these.
# ---------------------------------------------------------------------------

function New-TripwireAllowlistContent {
    param([object[]]$Hits, [object[]]$ExistingEntries, [string]$Stamp)
    $prev = @{}
    foreach ($e in @($ExistingEntries)) {
        if ($e.Malformed) { continue }
        $prev[(Get-TripwireEntryKey -RelPath $e.RelPath -RuleId $e.RuleId -Fingerprint $e.Fingerprint)] = $e.Reason
    }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("# tests/tripwire_allowlist.txt - the reviewed exceptions to the tripwire scan`r`n")
    [void]$sb.Append("# (check 13b of tests\RunnerTurnaround.Tests.ps1; handoff sec 4 rule 7).`r`n")
    [void]$sb.Append("#`r`n")
    [void]$sb.Append("# FORMAT: <relative path> | <rule id> | <sha1 of the whitespace-normalised line> | <reason>`r`n")
    [void]$sb.Append("# A hit that is not listed here FAILS the suite. An entry that matches nothing`r`n")
    [void]$sb.Append("# is reported as a WARN, not a failure. Editing an allowed line changes its`r`n")
    [void]$sb.Append("# fingerprint, so the edit comes back for review - that is the point.`r`n")
    [void]$sb.Append("#`r`n")
    [void]$sb.Append("# REGENERATE (never automatic):`r`n")
    [void]$sb.Append("#   pwsh -NoProfile -File tests\RunnerTurnaround.Tests.ps1 -UpdateTripwireAllowlist`r`n")
    [void]$sb.Append("# then READ the diff: a reason that still says REVIEW has not been reviewed.`r`n")
    [void]$sb.Append(("# Generated {0}.`r`n" -f $Stamp))
    foreach ($h in @($Hits | Sort-Object RelPath, RuleId, LineNumber)) {
        $k = Get-TripwireEntryKey -RelPath $h.RelPath -RuleId $h.RuleId -Fingerprint $h.Fingerprint
        $reason = ('REVIEW (auto-generated {0}) - classify: falsification/correction/historical site, or LIVE guidance that must be fixed instead' -f $Stamp)
        if ($prev.ContainsKey($k)) { $reason = $prev[$k] }
        [void]$sb.Append(('{0} | {1} | {2} | {3}' -f $h.RelPath, $h.RuleId, $h.Fingerprint, $reason))
        [void]$sb.Append("`r`n")
    }
    return $sb.ToString()
}

function New-RulingClaimsBaselineContent {
    param([object[]]$Counts, [string]$Stamp)
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("# tests/ruling_claims_baseline.txt - the RATCHET for un-id'd ruling claims`r`n")
    [void]$sb.Append("# in docs\ files that are not one of the four LIVE docs (check 13c).`r`n")
    [void]$sb.Append("#`r`n")
    [void]$sb.Append("# FORMAT: <relative path> | <number of ruling claims with no RL-id in their unit>`r`n")
    [void]$sb.Append("# The suite FAILS when a count rises above its baseline, or when a file that`r`n")
    [void]$sb.Append("# is not listed here acquires one. A count that FALLS is reported, never failed.`r`n")
    [void]$sb.Append("#`r`n")
    [void]$sb.Append("# REGENERATE (never automatic):`r`n")
    [void]$sb.Append("#   pwsh -NoProfile -File tests\RunnerTurnaround.Tests.ps1 -UpdateRulingClaimsBaseline`r`n")
    [void]$sb.Append(("# Generated {0}.`r`n" -f $Stamp))
    foreach ($row in @($Counts | Where-Object { $_.Unidentified -gt 0 } | Sort-Object RelPath)) {
        [void]$sb.Append(('{0} | {1}' -f $row.RelPath, $row.Unidentified))
        [void]$sb.Append("`r`n")
    }
    return $sb.ToString()
}

function Write-RecordCheckFile {
    param([string]$Path, [string]$Content)
    $bytes = [System.Text.Encoding]::ASCII.GetBytes($Content)
    [System.IO.File]::WriteAllBytes($Path, $bytes)
}
