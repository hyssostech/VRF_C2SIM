#Requires -Version 7.0
# Deploy-C2SimSms.ps1 - BUILD the four C2SIM derived simulation model sets (SMS) under
# C:\C2SIM\vrf-sms from the INSTALLED VR-Forces 5.2d files, at deploy time.
#
# Run with pwsh 7 (pinned):
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimSms.ps1 -WhatIf
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimSms.ps1
#
# WHY A SCRIPT AND NOT COMMITTED FILES. This repo is PUBLIC. The overriding files
# (vrfSim.opd, ground-vehicle-move-to.lua/.xml, Ground_Vehicle.ope, ground-tracked.sysdef) are
# MAK's, so they are never committed - not even as test fixtures. What is committed is this
# recipe: it reads the vendor originals, ASSERTS each line it changes still reads exactly as
# the record says, applies the recorded one-line changes, and writes the result OUTSIDE
# C:\MAK, so the customisation survives a vendor reinstall (DEMO_READINESS row 21).
#
# WHAT IT BUILDS (record: G7B_G8_RESULTS_2026-09-14.md :28-35, :241-242, :509-518, :590-595;
# PREREG_N1_N2_CORRIDOR_SLOPE_2026-09-14.md :195-218; tools/FixtureGen/README.md). For each
# NAME below: <Dest>\NAME.sms plus the model-set directory <Dest>\NAME\ - the vendor's own
# layout (MAKTest.sms beside MAKTest\), and the layout the fixtures name by absolute path
# (e.g. "C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms"; build_fixture.py
# SMS_52_CUSTOM).
#   C2SIM_EntityLevel_AbstractGraphs          scripts\ground-vehicle-move-to.lua (+ .xml)
#   C2SIM_EntityLevel_AbstractGraphs_Slope2   the same script + vrfSim\systems\movement\ground-tracked.sysdef
#   C2SIM_EntityLevel_Corridor2000            vrfSim\platforms\Ground_Vehicle.ope
#   C2SIM_EntityLevel_Corridor2000_Slope2     the same .ope + the same .sysdef
# Every tree also carries vrfSim.opd byte-identical to EntityLevel's ("every SMS must have
# vrfSim.opd", UG52 68.3.5 p1314).
#
# VENDOR MECHANISM (VR-Forces 5.2 Users Guide): an SMS that includes another has the higher
# priority and "any parameters or files in the higher priority SMSs that are also in the lower
# priority SMSs override those" (68.3.1 p1310); priority by inclusion (68.3.3 p1312); "the
# scripts in the highest priority SMS supersede those in the lower priority SMSs" (68.3.4
# p1313); filename parameters may be absolute (Table 15 p271).
#
# THE .sms TEXT is not invented here. It is the vendor's MAKTest.sms (an SMS that includes
# EntityLevel.sms: `(include "..\data\simulationModelSets\EntityLevel.sms")` at :87, which is
# also the form UG52 68.3.3 p1313 prints) with its header comment replaced by ours and its ONE
# model-set-directory line changed - the record's shape (G7B_G8:509-518: enumerations-file "",
# read-only False; PREREG_N1_N2:203-206: the four .sms differ only in model-set-directory).
#
# RECORD GAPS (laneR_restore_plan sec 4): G1 - the original .sms text, the exact printInfo
# line and the .xml sidecar were never saved. The .sms is rebuilt from MAKTest.sms as above;
# the proof line is inserted right after the patched :488 so it prints once per mesh query
# (the recorded behaviour: 32 lines for 32 queries in G7b run C) and its text is the recorded
# proof string verbatim; the .xml is the vendor's, unchanged (the record names no change to
# it). G2 - no SMS hash was ever recorded, so a rebuilt tree cannot be proved byte-identical
# to the lost one; this script is deterministic (no timestamps), so its manifest IS the
# hash record from now on.
#
# Exit codes:
#   0 = all four trees present and verified (or, with -WhatIf, would be written)
#   2 = bad arguments (-Dest under C:\MAK or -VrfRoot, relative, UNC) or vendor file missing
#   3 = a vendor line did not read as recorded (vendor changed - NOTHING is written), a
#       read-back hash disagreed, or a tree holds a file this script did not write
#   5 = unexpected terminating error
# ASCII only.
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $VrfRoot = 'C:\MAK\vrforces5.2d',
    [string] $Dest    = 'C:\C2SIM\vrf-sms'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$dry = [bool]$WhatIfPreference

function Say      { param([string]$m) Write-Host $m }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [PLAN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }

# The recorded proof string (G7B_G8_RESULTS_2026-09-14.md :241-242; tools/FixtureGen/README.md).
$ProofText = 'C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true'

# Relative paths inside a model-set directory.
$RelLua    = 'scripts\ground-vehicle-move-to.lua'
$RelXml    = 'scripts\ground-vehicle-move-to.xml'
$RelOpe    = 'vrfSim\platforms\Ground_Vehicle.ope'
$RelSysdef = 'vrfSim\systems\movement\ground-tracked.sysdef'
$RelOpd    = 'vrfSim.opd'

$Trees = [ordered]@{
    'C2SIM_EntityLevel_AbstractGraphs'        = @($RelLua, $RelXml)
    'C2SIM_EntityLevel_AbstractGraphs_Slope2' = @($RelLua, $RelXml, $RelSysdef)
    'C2SIM_EntityLevel_Corridor2000'          = @($RelOpe)
    'C2SIM_EntityLevel_Corridor2000_Slope2'   = @($RelOpe, $RelSysdef)
}

try {
    Say '=== Deploy-C2SimSms.ps1 - build the C2SIM derived SMS trees from the installed vendor files ==='
    Say ('  VrfRoot : {0}' -f $VrfRoot)
    Say ('  Dest    : {0}' -f $Dest)
    Say ('  WhatIf  : {0}' -f $dry)

    # ---- THE C:\MAK GUARD (same four parts as scripts\NewVrfAppData52.ps1) --------------
    $forbiddenRoots = @('C:\MAK')
    if (-not [string]::IsNullOrWhiteSpace($VrfRoot)) {
        try { $forbiddenRoots += [System.IO.Path]::GetFullPath($VrfRoot).TrimEnd('\') } catch { }
    }
    if ([string]::IsNullOrWhiteSpace($Dest)) { Say-Fail '-Dest is empty.'; exit 2 }
    if ($Dest -match '^\\\\') { Say-Fail ('-Dest is a UNC or device path ({0}) - REFUSED.' -f $Dest); exit 2 }
    if (-not [System.IO.Path]::IsPathRooted($Dest)) { Say-Fail ('-Dest must be an ABSOLUTE path (got "{0}").' -f $Dest); exit 2 }
    $destFull = [System.IO.Path]::GetFullPath($Dest).TrimEnd('\')
    foreach ($fr in $forbiddenRoots) {
        if ($destFull -like ($fr + '*') -or $Dest -like ($fr + '*')) {
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

    # ---- vendor inputs (read-only) ------------------------------------------------------
    $smsRoot   = Join-Path $VrfRoot 'data\simulationModelSets'
    $entityDir = Join-Path $smsRoot 'EntityLevel'
    $makTest   = Join-Path $smsRoot 'MAKTest.sms'
    foreach ($p in @($makTest, (Join-Path $smsRoot 'EntityLevel.sms'), (Join-Path $entityDir $RelOpd),
                     (Join-Path $entityDir $RelLua), (Join-Path $entityDir $RelXml),
                     (Join-Path $entityDir $RelOpe), (Join-Path $entityDir $RelSysdef))) {
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { Say-Fail ('vendor file missing: {0} (is -VrfRoot right?)' -f $p); exit 2 }
    }

    $latin1 = [System.Text.Encoding]::Latin1   # byte-preserving; every input is asserted ASCII
    function Get-Sha256Hex([byte[]]$b) { [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($b)).ToLowerInvariant() }

    # Read a vendor text file as CRLF lines. Refuses non-ASCII and mixed line endings, so a
    # line NUMBER below means exactly what it meant when the record was written.
    function Read-VendorLines([string]$path) {
        $b = [System.IO.File]::ReadAllBytes($path)
        foreach ($x in $b) { if ($x -gt 127) { throw ('vendor file is not ASCII: {0}' -f $path) } }
        $t = $latin1.GetString($b)
        $crlf = ([regex]::Matches($t, "`r`n")).Count
        $lf   = ([regex]::Matches($t, "`n")).Count
        if ($crlf -ne $lf -or -not $t.EndsWith("`r`n")) { throw ('vendor file is not uniformly CRLF-terminated: {0} (CRLF {1}, LF {2})' -f $path, $crlf, $lf) }
        $lines = [System.Collections.Generic.List[string]]::new()
        $lines.AddRange([string[]]($t.Substring(0, $t.Length - 2) -split "`r`n"))
        return , $lines
    }
    function Join-Lines([System.Collections.Generic.List[string]]$lines) {
        return $latin1.GetBytes(($lines -join "`r`n") + "`r`n")
    }
    # Replace line $n (1-based) - ONLY if it reads exactly $expect (the recorded original).
    function Set-VendorLine($lines, [int]$n, [string]$expect, [string[]]$with, [string]$what) {
        $got = if ($lines.Count -ge $n) { $lines[$n - 1] } else { '(file has only ' + $lines.Count + ' lines)' }
        if ($got -cne $expect) {
            throw ("VENDOR FILE CHANGED - {0} line {1} does not read as recorded.`n      expected: [{2}]`n      found   : [{3}]`n      Nothing has been written. Re-derive the patch from the record before changing this script." -f $what, $n, $expect, $got)
        }
        $lines.RemoveAt($n - 1)
        $lines.InsertRange($n - 1, [string[]]$with)
        Say-Ok ('{0}:{1} asserted "{2}" -> {3} line(s)' -f $what, $n, $expect.Trim(), $with.Count)
    }

    # ---- build every output IN MEMORY first; a failed assertion writes nothing ---------
    $opdBytes = [System.IO.File]::ReadAllBytes((Join-Path $entityDir $RelOpd))
    $xmlBytes = [System.IO.File]::ReadAllBytes((Join-Path $entityDir $RelXml))

    # scripts\ground-vehicle-move-to.lua :488 (record: G7B_G8_RESULTS :28-35, :590-595)
    $lua = Read-VendorLines (Join-Path $entityDir $RelLua)
    $ind = ' ' * 27
    Set-VendorLine $lua 488 ($ind + 'local params = {useAbstractGraphs = false, useChannels = true, channelRadius = 4.0}') @(
        ($ind + 'local params = {useAbstractGraphs = true, useChannels = true, channelRadius = 4.0}'),
        ($ind + 'printInfo("' + $ProofText + '")')
    ) $RelLua
    $luaBytes = Join-Lines $lua

    # vrfSim\platforms\Ground_Vehicle.ope :22 (record: PREREG_N1_N2 :209-213)
    $ope = Read-VendorLines (Join-Path $entityDir $RelOpe)
    Set-VendorLine $ope 22 '      (DtRwReal propagation-box-extent 200.000000)' @(
        '      (DtRwReal propagation-box-extent 2000.000000)',
        '      (DtRwBoolean enable-path-plan-timing True)'
    ) $RelOpe
    $opeBytes = Join-Lines $ope

    # vrfSim\systems\movement\ground-tracked.sysdef :133 (record: PREREG_N1_N2 :215-218)
    $sys = Read-VendorLines (Join-Path $entityDir $RelSysdef)
    Set-VendorLine $sys 133 '         (slope-avoidance-factor 1.000000)' @(
        '         (slope-avoidance-factor 2.000000)'
    ) $RelSysdef
    $sysBytes = Join-Lines $sys

    # The .sms body: MAKTest.sms below its header comment. Assert the lines the record
    # relies on before using it (G7B_G8 :509-518).
    $mt = Read-VendorLines $makTest
    $bodyStart = -1
    for ($i = 0; $i -lt $mt.Count; $i++) {
        if ($mt[$i].StartsWith('(simulation-model-set')) { $bodyStart = $i; break }
        if (-not $mt[$i].StartsWith(';;')) { throw ('MAKTest.sms line {0} is neither a ;; comment nor the body start: [{1}]' -f ($i + 1), $mt[$i]) }
    }
    if ($bodyStart -lt 0) { throw 'MAKTest.sms has no (simulation-model-set line' }
    $body = [System.Collections.Generic.List[string]]::new()
    for ($i = $bodyStart; $i -lt $mt.Count; $i++) { $body.Add($mt[$i]) }
    foreach ($must in @('         (model-set-directory "MAKTest")',
                        '   (include "..\data\simulationModelSets\EntityLevel.sms")',
                        '   (enumerations-file "")',
                        '   (read-only False)')) {
        $hits = @($body | Where-Object { $_ -ceq $must }).Count
        if ($hits -ne 1) { throw ('VENDOR FILE CHANGED - MAKTest.sms should carry exactly one line [{0}], found {1}. Nothing has been written.' -f $must, $hits) }
    }
    Say-Ok ('MAKTest.sms asserted: include of EntityLevel.sms, enumerations-file "", read-only False, model-set-directory "MAKTest" (body = its lines {0}-{1})' -f ($bodyStart + 1), $mt.Count)
    $mtdIndex = $body.IndexOf('         (model-set-directory "MAKTest")')

    $plan = [System.Collections.Generic.List[object]]::new()   # Path, Bytes
    foreach ($name in $Trees.Keys) {
        $rels = $Trees[$name]
        $hdr = @(
            (';; {0}.sms - C2SIM derived simulation model set (VRF_C2SIM repo).' -f $name),
            ';; GENERATED by tools/sms/Deploy-C2SimSms.ps1 from the installed VR-Forces files. Do not edit;',
            ';; rerun the script. Body = the vendor MAKTest.sms below its header comment, with only',
            ';; model-set-directory changed. Includes the shipped EntityLevel.sms unchanged (UG52 68.3.1 p1310).',
            (';; Overrides in {0}\: {1}' -f $name, (($rels + $RelOpd) -join ', '))
        )
        $sms = [System.Collections.Generic.List[string]]::new()
        $sms.AddRange([string[]]$hdr)
        $b2 = [System.Collections.Generic.List[string]]::new($body)
        $b2[$mtdIndex] = ('         (model-set-directory "{0}")' -f $name)
        $sms.AddRange($b2)
        $plan.Add([pscustomobject]@{ Path = (Join-Path $destFull ($name + '.sms')); Bytes = (Join-Lines $sms) })
        $plan.Add([pscustomobject]@{ Path = (Join-Path $destFull (Join-Path $name $RelOpd)); Bytes = $opdBytes })
        foreach ($r in $rels) {
            $bytes = switch ($r) { $RelLua { $luaBytes } $RelXml { $xmlBytes } $RelOpe { $opeBytes } $RelSysdef { $sysBytes } }
            $plan.Add([pscustomobject]@{ Path = (Join-Path $destFull (Join-Path $name $r)); Bytes = $bytes })
        }
    }

    # ---- write (or, -WhatIf, only report) ----------------------------------------------
    foreach ($f in $plan) {
        $state = 'new'
        if (Test-Path -LiteralPath $f.Path -PathType Leaf) {
            $state = if ((Get-Sha256Hex ([System.IO.File]::ReadAllBytes($f.Path))) -eq (Get-Sha256Hex $f.Bytes)) { 'unchanged' } else { 'REPLACED' }
        }
        if ($dry) { Say-Plan ('would write ({0}) {1}' -f $state, $f.Path); continue }
        if ($state -ne 'unchanged') {
            [void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($f.Path))
            [System.IO.File]::WriteAllBytes($f.Path, [byte[]]$f.Bytes)
        }
        $f | Add-Member -NotePropertyName State -NotePropertyValue $state
    }

    # ---- verify: read-back hashes, opd identity, no stray files ------------------------
    $bad = 0
    $vendorOpd = Get-Sha256Hex $opdBytes
    if (-not $dry) {
        foreach ($f in $plan) {
            $h = Get-Sha256Hex ([System.IO.File]::ReadAllBytes($f.Path))
            if ($h -ne (Get-Sha256Hex $f.Bytes)) { Say-Fail ('read-back hash mismatch: {0}' -f $f.Path); $bad++ }
            if ($f.Path.EndsWith('\' + $RelOpd) -and $h -ne $vendorOpd) { Say-Fail ('vrfSim.opd not byte-identical to EntityLevel''s: {0}' -f $f.Path); $bad++ }
        }
    }
    foreach ($name in $Trees.Keys) {
        $treeDir = Join-Path $destFull $name
        if (-not (Test-Path -LiteralPath $treeDir)) { continue }
        $want = @($plan | ForEach-Object { $_.Path })
        foreach ($x in @(Get-ChildItem -LiteralPath $treeDir -Recurse -File -Force)) {
            if ($want -notcontains $x.FullName) {
                Say-Fail ('stray file in a derived SMS tree (it would ALSO override the vendor): {0} - remove it by hand; this script deletes nothing.' -f $x.FullName)
                $bad++
            }
        }
    }

    Say ''
    Say ('MANIFEST (sha256  bytes  path){0}' -f $(if ($dry) { ' - WHAT WOULD BE WRITTEN' } else { '' }))
    foreach ($f in $plan) {
        Say ('  {0}  {1,6}  {2}{3}' -f (Get-Sha256Hex $f.Bytes), $f.Bytes.Length, $f.Path,
            $(if (-not $dry) { '  (' + $f.State + ')' } else { '' }))
    }
    Say ('  vendor EntityLevel\vrfSim.opd sha256 {0}' -f $vendorOpd)
    if ($bad -gt 0) { Say-Fail ('{0} verification failure(s).' -f $bad); exit 3 }
    if ($dry) { Say-Ok 'WhatIf: nothing written.' } else { Say-Ok ('{0} files present and verified under {1}' -f $plan.Count, $destFull) }
    exit 0
}
catch {
    $msg = $_.Exception.Message
    if ($msg -like 'VENDOR FILE CHANGED*' -or $msg -like 'vendor file is not*' -or $msg -like 'MAKTest.sms*') {
        Say-Fail $msg
        exit 3
    }
    Say-Fail ('unexpected error: {0}' -f $_)
    exit 5
}
