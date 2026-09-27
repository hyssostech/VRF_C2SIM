#Requires -Version 7.0
# Deploy-C2SimAggregateSms.ps1 - BUILD the C2SIM derived AGGREGATE-LEVEL simulation model set
# C2SIM_AggregateTacticalLevel under C:\C2SIM\vrf-sms from the INSTALLED VR-Forces 5.2d files and
# the committed recipe, at deploy time (package C2; RL-20260927-04 under RL-20260927-02).
#
# Run with pwsh 7 (pinned):
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimAggregateSms.ps1 -WhatIf
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimAggregateSms.ps1
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimAggregateSms.ps1 -SelfTest
#
# WHY A SCRIPT AND NOT COMMITTED FILES (the pattern of Deploy-C2SimSms.ps1). This repo is PUBLIC;
# the .entity, .leaf, .magx, .sms and .opd files are MAK's and are never committed. What is
# committed is the RECIPE, tools/sms/C2SIM_AggregateTacticalLevel.recipe.json, generated from the
# decisions in tools/sms/aggregate_authored_design.json by tools/aggregate/authored_units.py: for
# each output, the vendor file it starts from and literal edits - a single-line edit names the exact
# donor line it replaces; a block edit (delete / replace / graft from another vendor file) names
# the block's first line, its line count and its sha256. This script ASSERTS every one of them on
# the installed files, builds everything in memory, and only then writes OUTSIDE C:\MAK. A failed
# assertion writes nothing. Every output's sha256 is recorded in the recipe too, so a vendor change
# ANYWHERE in a donor (not only on an edited line) stops the build: the authored types inherit
# every unedited donor parameter, so a changed donor is a changed type and must be re-reviewed
# (python tools/aggregate/authored_units.py --check says what moved).
#
# WHAT IT BUILDS. <Dest>\C2SIM_AggregateTacticalLevel.sms beside <Dest>\C2SIM_AggregateTacticalLevel\
# (the vendor layout - MAKTest.sms beside MAKTest\ - and the layout of the entity-level C2SIM sets):
#   .sms          the vendor AggregateTacticalLevel.sms, header comment added, model-set-directory
#                 changed and its include retargeted to AggregateTacticalLevel.sms itself - an
#                 including SMS has the higher priority and needs no copy of files it does not
#                 change (UG52 68.3.1 p1310, 68.3.3 p1312, 68.3.5 p1314); its HLA validator-string
#                 and aggregate connection are the vendor's (asserted), since the aggregate
#                 warfare model needs HLA Evolved (UG52 27.1 p528).
#   vrfSim.opd    byte-identical to AggregateTacticalLevel\vrfSim.opd ("every SMS must have
#                 vrfSim.opd", UG52 68.3.5 p1314).
#   vrfSim\*.entity            one per authored type. An SMS's object types ARE the .entity files
#                 in its vrfSim directory - there is no object list to register them in: "copy the
#                 files into the vrfSim directory in the SMS" (UG52 68.8 p1326); a promoted or new
#                 model "is added to it" as a new .entity file on save (68.3.8 p1315).
#   gui\visuals\Unit\*.leaf + *.magx  the unit symbol of each type (element definition + type map),
#                 the per-SMS visual data layout of AggregateTacticalLevel itself (one .leaf and one
#                 .magx per unit type; "an SMS can have visual data that is specific to the SMS",
#                 UG52 68.5.4 p1320).
# NO smsChecksum*.cksm is written: those files are the SMS UPGRADE tool's reference checksums for
# the STANDARD sets (UG52 68.5.2 p1319, Table 56) - the vendor's own including set MAKTest ships
# none, and neither do the entity-level C2SIM sets that have run (G7b).
#
# Exit codes:
#   0 = the tree is present and verified (or, with -WhatIf, would be written); -SelfTest: PASS
#   2 = bad arguments (-Dest under C:\MAK or -VrfRoot, relative, UNC), recipe or vendor file missing
#   3 = a vendor line or block did not read as recorded, an output hash disagreed (vendor changed -
#       NOTHING is written), a generated file is not ASCII / does not parse, a read-back hash
#       disagreed, or the tree holds a file this script did not write; -SelfTest: FAIL
#   5 = unexpected terminating error
# ASCII only.
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $VrfRoot = 'C:\MAK\vrforces5.2d',
    [string] $Dest    = 'C:\C2SIM\vrf-sms',
    [string] $Recipe  = '',
    [switch] $SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$dry = [bool]$WhatIfPreference
if ([string]::IsNullOrWhiteSpace($Recipe)) { $Recipe = Join-Path $PSScriptRoot 'C2SIM_AggregateTacticalLevel.recipe.json' }

function Say      { param([string]$m) Write-Host $m }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [PLAN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }

$latin1 = [System.Text.Encoding]::Latin1   # byte-preserving; the OUTPUT is gated ASCII
function Get-Sha256Hex([byte[]]$b) { [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($b)).ToLowerInvariant() }

# ---- -SelfTest: CLEAN build + DIRTY controls, each a child run of this script ----------------
if ($SelfTest) {
    $pwsh = (Get-Process -Id $PID).Path
    $root = Join-Path ([System.IO.Path]::GetTempPath()) ('c2sim-aggsms-selftest-' + [Guid]::NewGuid().ToString('N').Substring(0, 12))
    $bad = 0
    function Check([bool]$ok, [string]$what, [string]$detail = '') {
        if ($ok) { Write-Host ('  [PASS] ' + $what) } else { Write-Host ('  [FAIL] ' + $what + $(if ($detail) { ' -- ' + $detail } else { '' })); $script:bad++ }
    }
    function Invoke-Child([string]$vrf, [string]$dst) {
        $out = & $pwsh -NoProfile -File $PSCommandPath -VrfRoot $vrf -Dest $dst -Recipe $Recipe 2>&1 | Out-String
        return [pscustomobject]@{ Code = $LASTEXITCODE; Text = $out }
    }
    try {
        Say '=== Deploy-C2SimAggregateSms.ps1 -SelfTest ==='
        $r = Get-Content -LiteralPath $Recipe -Raw | ConvertFrom-Json
        $name = [string]$r.name
        Say '--- 1. CLEAN: build from the installed vendor tree into a scratch directory ---'
        $cleanDest = Join-Path $root 'clean'
        $c = Invoke-Child $VrfRoot $cleanDest
        Check ($c.Code -eq 0) 'CLEAN build exits 0' ("exit {0}: {1}" -f $c.Code, ($c.Text -split "`n" | Select-String 'FAIL' | Select-Object -First 3))
        $want = @($r.files | ForEach-Object { [string]$_.to }) + @($r.copy | ForEach-Object { [string]$_.to })
        $have = @()
        if (Test-Path -LiteralPath (Join-Path $cleanDest $name)) {
            $base = (Join-Path $cleanDest $name) + '\'
            $have = @(Get-ChildItem -LiteralPath (Join-Path $cleanDest $name) -Recurse -File | ForEach-Object { $_.FullName.Substring($base.Length).Replace('\', '/') })
        }
        Check ((@($want | Sort-Object) -join '|') -eq (@($have | Sort-Object) -join '|')) ('CLEAN tree holds exactly the {0} recipe files' -f $want.Count)
        Check (Test-Path -LiteralPath (Join-Path $cleanDest ($name + '.sms'))) 'CLEAN .sms written beside its directory'
        $ents = @(Get-ChildItem -LiteralPath (Join-Path $cleanDest $name) -Recurse -File -ErrorAction SilentlyContinue)
        $nonAscii = @($ents | Where-Object { ([System.IO.File]::ReadAllBytes($_.FullName) | Where-Object { $_ -gt 126 } | Select-Object -First 1) })
        Check ($ents.Count -gt 0 -and $nonAscii.Count -eq 0) 'CLEAN outputs are ASCII' (($nonAscii | ForEach-Object Name) -join ', ')
        $again = Invoke-Child $VrfRoot $cleanDest
        Check ($again.Code -eq 0 -and $again.Text -match '\(unchanged\)' -and $again.Text -notmatch '\(REPLACED\)|\(new\)') 'CLEAN rerun is idempotent (every file unchanged)'

        Say '--- 2. DIRTY controls: a mutated vendor input must stop the build and write NOTHING ---'
        # the vendor files the deploy reads, copied into a mirror tree that can be mutated
        $inputs = New-Object System.Collections.Generic.List[string]
        $inputs.Add([string]$r.sms.from)
        foreach ($cp in $r.copy) { $inputs.Add([string]$cp.from) }
        foreach ($f in $r.files) {
            $inputs.Add([string]$f.from)
            foreach ($e in $f.edits) { if ($e.PSObject.Properties.Name -contains 'from') { $inputs.Add([string]$e.from) } }
        }
        $inputs = @($inputs | Sort-Object -Unique)
        function New-Mirror([string]$tag) {
            $m = Join-Path $root ('vrf-' + $tag)
            foreach ($rel in $inputs) {
                $src = Join-Path $VrfRoot (Join-Path 'data\simulationModelSets' ($rel -replace '/', '\'))
                $dst = Join-Path $m (Join-Path 'data\simulationModelSets' ($rel -replace '/', '\'))
                [void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($dst))
                [System.IO.File]::Copy($src, $dst, $true)
            }
            return $m
        }
        function Set-MirrorLine([string]$mirror, [string]$rel, [string]$old, [string]$new, [bool]$nextLine) {
            $p = Join-Path $mirror (Join-Path 'data\simulationModelSets' ($rel -replace '/', '\'))
            $t = $latin1.GetString([System.IO.File]::ReadAllBytes($p))
            $i = $t.IndexOf($old, [StringComparison]::Ordinal)
            if ($i -lt 0) { throw ('selftest: line not found in {0}: {1}' -f $rel, $old) }
            if ($nextLine) {
                # the line AFTER $old: a change INSIDE a block, which only the block's sha256 can catch
                $n1 = $t.IndexOf("`n", $i); $n2 = $t.IndexOf("`n", $n1 + 1)
                $cut = if ($t[$n2 - 1] -eq "`r") { $n2 - 1 } else { $n2 }
                $t = $t.Substring(0, $cut) + ' ' + $t.Substring($cut)
            } else {
                $t = $t.Substring(0, $i) + $new + $t.Substring($i + $old.Length)
            }
            [System.IO.File]::WriteAllBytes($p, $latin1.GetBytes($t))
        }
        $first = { param($op) foreach ($f in $r.files) { foreach ($e in $f.edits) { if ($e.op -eq $op) { return [pscustomobject]@{ File = $f; Edit = $e } } } } }
        $mutations = New-Object System.Collections.Generic.List[object]
        $le = & $first 'line'
        $mutations.Add([pscustomobject]@{ Name = 'an edited donor line (single-line edit)'; Rel = [string]$le.File.from
                                          Old = [string]$le.Edit.line; New = ([string]$le.Edit.line) + ' '; Next = $false; Expect = 'VENDOR FILE CHANGED' })
        $be = & $first 'block'
        $mutations.Add([pscustomobject]@{ Name = 'a line INSIDE an edited donor block (its sha256)'; Rel = [string]$be.File.from
                                          Old = [string]$be.Edit.start; New = ''; Next = $true; Expect = 'block (' })
        $de = & $first 'delete'
        $mutations.Add([pscustomobject]@{ Name = 'the first line of a DELETED donor block'; Rel = [string]$de.File.from
                                          Old = [string]$de.Edit.start; New = ([string]$de.Edit.start).Replace('<', '< '); Next = $false; Expect = 'VENDOR FILE CHANGED' })
        $ie = & $first 'insert-from'
        $mutations.Add([pscustomobject]@{ Name = 'a line inside a GRAFTED source block'; Rel = [string]$ie.Edit.from
                                          Old = [string]$ie.Edit.fromStart; New = ''; Next = $true; Expect = 'block (' })
        $ra = [string]($r.sms.assert | Select-Object -First 1)
        $mutations.Add([pscustomobject]@{ Name = 'an asserted .sms template line'; Rel = [string]$r.sms.from
                                          Old = $ra; New = $ra.Replace('hla4', 'hla5'); Next = $false; Expect = 'VENDOR FILE CHANGED' })
        $mutations.Add([pscustomobject]@{ Name = 'a donor line NO edit touches (output hash)'; Rel = [string]$le.File.from
                                          Old = '<real paramName="Combat-Posture-Travel-Modifier">'; New = '<real paramName="Combat-Posture-Travel-Modifier"> '
                                          Next = $false; Expect = 'OUTPUT HASH' })
        $k = 0
        foreach ($mu in $mutations) {
            $k++
            $mirror = New-Mirror ('m' + $k)
            Set-MirrorLine $mirror $mu.Rel $mu.Old $mu.New $mu.Next
            $d = Join-Path $root ('dirty-' + $k)
            $res = Invoke-Child $mirror $d
            $wrote = @(Get-ChildItem -LiteralPath $d -Recurse -File -ErrorAction SilentlyContinue).Count
            Check ($res.Code -eq 3 -and $res.Text -match [regex]::Escape($mu.Expect) -and $wrote -eq 0) ('DIRTY {0} ({1}) -> exit 3, "{2}", 0 files written' -f $mu.Name, $mu.Rel.Split('/')[-1], $mu.Expect) ("exit {0}, files {1}" -f $res.Code, $wrote)
        }
        Say '--- 3. guards ---'
        $g1 = Invoke-Child $VrfRoot 'C:\MAK\c2sim-selftest-must-not-exist'
        Check ($g1.Code -eq 2 -and -not (Test-Path 'C:\MAK\c2sim-selftest-must-not-exist')) '-Dest under C:\MAK is REFUSED (exit 2, nothing created)'
        $g2 = Invoke-Child $VrfRoot 'relative\dir'
        Check ($g2.Code -eq 2) 'a relative -Dest is REFUSED (exit 2)'
    } finally {
        if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
    }
    Say ('DEPLOY SELFTEST {0} ({1} problem(s))' -f $(if ($bad -eq 0) { 'PASS' } else { 'FAIL' }), $bad)
    exit $(if ($bad -eq 0) { 0 } else { 3 })
}

try {
    Say '=== Deploy-C2SimAggregateSms.ps1 - build the C2SIM derived AGGREGATE SMS from the installed vendor files ==='
    Say ('  VrfRoot : {0}' -f $VrfRoot)
    Say ('  Dest    : {0}' -f $Dest)
    Say ('  Recipe  : {0}' -f $Recipe)
    Say ('  WhatIf  : {0}' -f $dry)

    # ---- THE C:\MAK GUARD (the four parts of Deploy-C2SimSms.ps1 / scripts\NewVrfAppData52.ps1) ----
    $forbiddenRoots = @('C:\MAK')
    if (-not [string]::IsNullOrWhiteSpace($VrfRoot)) {
        try { $forbiddenRoots += [System.IO.Path]::GetFullPath($VrfRoot).TrimEnd('\') } catch { }
    }
    if ([string]::IsNullOrWhiteSpace($Dest)) { Say-Fail '-Dest is empty.'; exit 2 }
    if ($Dest -match '^\\\\') { Say-Fail ('-Dest is a UNC or device path ({0}) - REFUSED.' -f $Dest); exit 2 }
    if (-not [System.IO.Path]::IsPathRooted($Dest)) { Say-Fail ('-Dest must be an ABSOLUTE path (got "{0}").' -f $Dest); exit 2 }
    $destFull = [System.IO.Path]::GetFullPath($Dest).TrimEnd('\')
    foreach ($fr in $forbiddenRoots) {
        if (($destFull + '\') -like ($fr + '\*') -or $destFull -eq $fr -or $Dest -like ($fr + '*')) {
            Say-Fail ('-Dest is under {0} ({1}) - REFUSED. This script never writes into the vendor tree.' -f $fr, $destFull)
            exit 2
        }
    }
    $probe = $destFull; $seen = 0
    while (-not [string]::IsNullOrWhiteSpace($probe) -and $seen -lt 64) {
        $seen++
        if (Test-Path -LiteralPath $probe) {
            $item = Get-Item -LiteralPath $probe -Force
            if ($item.LinkType -and $item.Target) {
                foreach ($t in @($item.Target)) {
                    $tFull = [string]$t
                    try { $tFull = [System.IO.Path]::GetFullPath([string]$t) } catch { }
                    foreach ($fr in $forbiddenRoots) {
                        if ($tFull -like ($fr + '*')) {
                            Say-Fail ('-Dest resolves into {0} through a {1} at "{2}" -> "{3}" - REFUSED.' -f $fr, $item.LinkType, $probe, $tFull)
                            exit 2
                        }
                    }
                }
            }
        }
        $parent = [System.IO.Path]::GetDirectoryName($probe)
        if ($parent -eq $probe -or [string]::IsNullOrWhiteSpace($parent)) { break }
        $probe = $parent
    }

    if (-not (Test-Path -LiteralPath $Recipe -PathType Leaf)) { Say-Fail ('recipe missing: {0}' -f $Recipe); exit 2 }
    $r = Get-Content -LiteralPath $Recipe -Raw | ConvertFrom-Json
    if ($r.schemaVersion -ne 1) { Say-Fail ('recipe schemaVersion {0} is not 1' -f $r.schemaVersion); exit 2 }
    $name = [string]$r.name
    if ($name -notmatch '^[A-Za-z0-9_]+$') { Say-Fail ('recipe name {0} is not a plain file name' -f $name); exit 2 }
    $smsRoot = Join-Path $VrfRoot 'data\simulationModelSets'

    # ---- vendor text files as lines (one terminator per file; byte-preserving) ------------------
    $cache = @{}
    function Get-VendorFile([string]$rel) {
        if ($cache.ContainsKey($rel)) { return $cache[$rel] }
        $p = Join-Path $smsRoot ($rel -replace '/', '\')
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw ('VENDOR FILE MISSING - {0} (is -VrfRoot right?)' -f $p) }
        $b = [System.IO.File]::ReadAllBytes($p)
        $t = $latin1.GetString($b)
        $crlf = ([regex]::Matches($t, "`r`n")).Count
        $lf   = ([regex]::Matches($t, "`n")).Count
        if ($lf -eq 0 -or -not $t.EndsWith("`n")) { throw ('VENDOR FILE CHANGED - {0} has no final line terminator' -f $rel) }
        if ($crlf -eq $lf) { $eol = "`r`n" } elseif ($crlf -eq 0) { $eol = "`n" } else { throw ('VENDOR FILE CHANGED - {0} mixes CRLF and LF (CRLF {1}, LF {2})' -f $rel, $crlf, $lf) }
        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.AddRange([string[]]($t.Substring(0, $t.Length - $eol.Length) -split [regex]::Escape($eol)))
        $o = [pscustomobject]@{ Rel = $rel; Lines = $lines; Eol = $eol; Sha256 = (Get-Sha256Hex $b) }
        $cache[$rel] = $o
        return $o
    }
    function Find-One($lines, [string]$exact, [string]$what, [string]$where) {
        $hits = @()
        for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -ceq $exact) { $hits += $i } }
        if ($hits.Count -ne 1) {
            throw ("VENDOR FILE CHANGED - {0}: expected exactly ONE line {1}, found {2}.`n      line: [{3}]`n      Nothing has been written. Re-derive with python tools/aggregate/authored_units.py --check." -f $where, $what, $hits.Count, $exact)
        }
        return $hits[0]
    }
    function Get-BlockSha($lines, [int]$start, [int]$count, [string]$eol) {
        if ($start + $count -gt $lines.Count) { return '(block runs past the end of the file)' }
        $blk = New-Object string[] $count
        $lines.CopyTo($start, $blk, 0, $count)
        return Get-Sha256Hex ($latin1.GetBytes(($blk -join $eol) + $eol))
    }
    function Assert-Block($lines, [int]$start, [int]$count, [string]$sha, [string]$eol, [string]$what, [string]$where) {
        $got = Get-BlockSha $lines $start $count $eol
        if ($got -ne $sha) {
            throw ("VENDOR FILE CHANGED - {0}: the {1} block ({2} lines from [{3}]) hashes {4}, the recipe recorded {5}. Nothing has been written." -f $where, $what, $count, $lines[$start], $got, $sha)
        }
    }

    # ---- build every output IN MEMORY; a failed assertion writes nothing --------------------------
    $plan = [System.Collections.Generic.List[object]]::new()   # Path, Bytes, Rel, Want

    # the .sms
    $sms = Get-VendorFile ([string]$r.sms.from)
    $smsLines = [System.Collections.Generic.List[string]]::new($sms.Lines)
    foreach ($a in $r.sms.assert) { [void](Find-One $smsLines ([string]$a) 'asserted' $sms.Rel); Say-Ok ('{0} asserted [{1}]' -f $sms.Rel, ([string]$a).Trim()) }
    foreach ($e in $r.sms.edits) {
        $i = Find-One $smsLines ([string]$e.line) 'to replace' $sms.Rel
        $smsLines[$i] = [string]$e.with
        Say-Ok ('{0}: [{1}] -> [{2}]' -f $sms.Rel, ([string]$e.line).Trim(), ([string]$e.with).Trim())
    }
    if ($smsLines.Count -gt 0 -and $smsLines[0].StartsWith(';;')) { throw ('VENDOR FILE CHANGED - {0} now starts with a comment; re-derive the recipe' -f $sms.Rel) }
    $smsOut = [System.Collections.Generic.List[string]]::new()
    foreach ($h in $r.sms.header) { $smsOut.Add([string]$h) }
    $smsOut.AddRange($smsLines)
    $plan.Add([pscustomobject]@{ Path = (Join-Path $destFull ([string]$r.sms.to)); Bytes = $latin1.GetBytes(($smsOut -join $sms.Eol) + $sms.Eol)
                                 Rel = [string]$r.sms.to; Want = [string]$r.sms.sha256; Kind = 'sms' })

    # byte-identical copies
    foreach ($cp in $r.copy) {
        $p = Join-Path $smsRoot (([string]$cp.from) -replace '/', '\')
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw ('VENDOR FILE MISSING - {0}' -f $p) }
        $b = [System.IO.File]::ReadAllBytes($p)
        $plan.Add([pscustomobject]@{ Path = (Join-Path $destFull (Join-Path $name (([string]$cp.to) -replace '/', '\'))); Bytes = $b
                                     Rel = ($name + '/' + [string]$cp.to); Want = [string]$cp.sha256; Kind = 'copy' })
    }

    # the authored files
    foreach ($f in $r.files) {
        $src = Get-VendorFile ([string]$f.from)
        $lines = [System.Collections.Generic.List[string]]::new($src.Lines)
        $where = [string]$f.from
        foreach ($e in $f.edits) {
            switch ([string]$e.op) {
                'line' {
                    $i = Find-One $lines ([string]$e.line) 'to replace' $where
                    $lines[$i] = [string]$e.with
                }
                'block' {
                    $i = Find-One $lines ([string]$e.start) 'opening the block' $where
                    Assert-Block $lines $i ([int]$e.count) ([string]$e.sha256) $src.Eol 'replaced' $where
                    $lines.RemoveRange($i, [int]$e.count)
                    $lines.InsertRange($i, [string[]]@($e.with))
                }
                'delete' {
                    $i = Find-One $lines ([string]$e.start) 'opening the block' $where
                    Assert-Block $lines $i ([int]$e.count) ([string]$e.sha256) $src.Eol 'deleted' $where
                    $lines.RemoveRange($i, [int]$e.count)
                }
                'replace-from' {
                    $i = Find-One $lines ([string]$e.start) 'opening the block' $where
                    Assert-Block $lines $i ([int]$e.count) ([string]$e.sha256) $src.Eol 'replaced' $where
                    $g = Get-VendorFile ([string]$e.from)
                    $j = Find-One $g.Lines ([string]$e.fromStart) 'opening the grafted block' $g.Rel
                    Assert-Block $g.Lines $j ([int]$e.fromCount) ([string]$e.fromSha256) $g.Eol 'grafted' $g.Rel
                    $blk = New-Object string[] ([int]$e.fromCount)
                    $g.Lines.CopyTo($j, $blk, 0, [int]$e.fromCount)
                    $lines.RemoveRange($i, [int]$e.count)
                    $lines.InsertRange($i, $blk)
                }
                'insert-from' {
                    $i = Find-One $lines ([string]$e.before) 'to insert before' $where
                    $g = Get-VendorFile ([string]$e.from)
                    $j = Find-One $g.Lines ([string]$e.fromStart) 'opening the grafted block' $g.Rel
                    Assert-Block $g.Lines $j ([int]$e.fromCount) ([string]$e.fromSha256) $g.Eol 'grafted' $g.Rel
                    $blk = New-Object string[] ([int]$e.fromCount)
                    $g.Lines.CopyTo($j, $blk, 0, [int]$e.fromCount)
                    $lines.InsertRange($i, $blk)
                }
                default { throw ('recipe: unknown edit op {0} in {1}' -f $e.op, $f.to) }
            }
        }
        Say-Ok ('{0}: {1} edit(s) asserted and applied -> {2}' -f $where.Split('/')[-1], @($f.edits).Count, ([string]$f.to).Split('/')[-1])
        $plan.Add([pscustomobject]@{ Path = (Join-Path $destFull (Join-Path $name (([string]$f.to) -replace '/', '\')))
                                     Bytes = $latin1.GetBytes(($lines -join $src.Eol) + $src.Eol)
                                     Rel = ($name + '/' + [string]$f.to); Want = [string]$f.sha256; Kind = [System.IO.Path]::GetExtension([string]$f.to) })
    }

    # ---- gate the OUTPUTS: ASCII, parse, and the recorded sha256 of each --------------------------
    $xmlSettings = [System.Xml.XmlReaderSettings]::new()
    $xmlSettings.DtdProcessing = [System.Xml.DtdProcessing]::Ignore
    foreach ($f in $plan) {
        $bi = -1
        for ($i = 0; $i -lt $f.Bytes.Length; $i++) { $x = $f.Bytes[$i]; if ($x -gt 126 -or ($x -lt 32 -and $x -ne 9 -and $x -ne 10 -and $x -ne 13)) { $bi = $i; break } }
        if ($bi -ge 0) { throw ('OUTPUT NOT ASCII - {0} at byte {1} (0x{2:x2}); the recipe must edit it out. Nothing has been written.' -f $f.Rel, $bi, $f.Bytes[$bi]) }
        if ($f.Kind -in @('.entity', '.leaf', '.magx')) {
            try {
                $ms = [System.IO.MemoryStream]::new($f.Bytes)
                $xr = [System.Xml.XmlReader]::Create($ms, $xmlSettings)
                while ($xr.Read()) { }
                $xr.Dispose()
            } catch { throw ('OUTPUT DOES NOT PARSE - {0}: {1}. Nothing has been written.' -f $f.Rel, $_.Exception.Message) }
        } elseif ($f.Kind -eq 'sms') {
            $txt = $latin1.GetString($f.Bytes); $depth = 0; $minDepth = 0
            foreach ($ch in $txt.ToCharArray()) { if ($ch -eq '(') { $depth++ } elseif ($ch -eq ')') { $depth--; if ($depth -lt $minDepth) { $minDepth = $depth } } }
            if ($depth -ne 0 -or $minDepth -lt 0) { throw ('OUTPUT DOES NOT PARSE - {0}: parentheses unbalanced (net {1}). Nothing has been written.' -f $f.Rel, $depth) }
        }
        $h = Get-Sha256Hex $f.Bytes
        if ($h -ne $f.Want) {
            throw ("OUTPUT HASH - {0} hashes {1}, the recipe recorded {2}: a vendor input changed OUTSIDE the asserted lines (every unedited donor parameter is part of the authored type). Nothing has been written. Run python tools/aggregate/authored_units.py --check, review the change, regenerate." -f $f.Rel, $h, $f.Want)
        }
    }
    Say-Ok ('{0} outputs: ASCII, parse, and equal the sha256 the recipe recorded' -f $plan.Count)

    # ---- write (or, -WhatIf, only report) ---------------------------------------------------------
    foreach ($f in $plan) {
        $state = 'new'
        if (Test-Path -LiteralPath $f.Path -PathType Leaf) {
            $state = if ((Get-Sha256Hex ([System.IO.File]::ReadAllBytes($f.Path))) -eq (Get-Sha256Hex $f.Bytes)) { 'unchanged' } else { 'REPLACED' }
        }
        if ($dry) { Say-Plan ('would write ({0}) {1}' -f $state, $f.Path); $f | Add-Member -NotePropertyName State -NotePropertyValue $state; continue }
        if ($state -ne 'unchanged') {
            [void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($f.Path))
            [System.IO.File]::WriteAllBytes($f.Path, [byte[]]$f.Bytes)
        }
        $f | Add-Member -NotePropertyName State -NotePropertyValue $state
    }

    # ---- verify: read-back hashes, no stray files ---------------------------------------------------
    $bad = 0
    if (-not $dry) {
        foreach ($f in $plan) {
            if ((Get-Sha256Hex ([System.IO.File]::ReadAllBytes($f.Path))) -ne $f.Want) { Say-Fail ('read-back hash mismatch: {0}' -f $f.Path); $bad++ }
        }
    }
    $treeDir = Join-Path $destFull $name
    if (Test-Path -LiteralPath $treeDir) {
        $want = @($plan | ForEach-Object { $_.Path })
        foreach ($x in @(Get-ChildItem -LiteralPath $treeDir -Recurse -File -Force)) {
            if ($want -notcontains $x.FullName) {
                Say-Fail ('stray file in the derived SMS tree (it would ALSO be loaded - an extra .entity is a new object type, UG52 68.8): {0} - remove it by hand; this script deletes nothing.' -f $x.FullName)
                $bad++
            }
        }
    }

    Say ''
    Say ('MANIFEST (sha256  bytes  path){0}' -f $(if ($dry) { ' - WHAT WOULD BE WRITTEN' } else { '' }))
    foreach ($f in $plan) {
        Say ('  {0}  {1,6}  {2}  ({3})' -f (Get-Sha256Hex $f.Bytes), $f.Bytes.Length, $f.Path, $f.State)
    }
    $drift = @()
    foreach ($p in $r.vendorInputs.PSObject.Properties) {
        if ($cache.ContainsKey($p.Name) -and $cache[$p.Name].Sha256 -ne [string]$p.Value) { $drift += $p.Name }
    }
    # Every OUTPUT matched its recorded sha256, so a differing input changed only lines no output
    # uses (e.g. a graft source outside the grafted block): a note, not a failure.
    if ($drift) { Say ('  [NOTE] vendor input(s) differ from the recipe record but every output is as recorded: {0}' -f ($drift -join ', ')) }
    if ($bad -gt 0) { Say-Fail ('{0} verification failure(s).' -f $bad); exit 3 }
    if ($dry) { Say-Ok 'WhatIf: nothing written.' } else { Say-Ok ('{0} files present and verified under {1}' -f $plan.Count, $destFull) }
    exit 0
}
catch {
    $msg = $_.Exception.Message
    if ($msg -like 'VENDOR FILE MISSING*') { Say-Fail $msg; exit 2 }
    if ($msg -like 'VENDOR FILE CHANGED*' -or $msg -like 'OUTPUT HASH*' -or $msg -like 'OUTPUT NOT ASCII*' -or $msg -like 'OUTPUT DOES NOT PARSE*') {
        Say-Fail $msg
        exit 3
    }
    Say-Fail ('unexpected error: {0}' -f $_)
    exit 5
}
