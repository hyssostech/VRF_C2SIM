# LabelDecorationLib.ps1 - the LOGIC of the Label symbol decoration deploy (LBL; RL-20261004-05), dot-sourced by
# tools\display\Enable-LabelDecoration.ps1 (the deploy) and, READ-ONLY, by scripts\LaunchVrf52.ps1 (the pre-flight WARN).
# Defines functions only: dot-sourcing it reads nothing, writes nothing and sets no StrictMode.
#
# THE VENDOR MECHANISM (cited in full in Enable-LabelDecoration.ps1's header):
#   - the decorations are GUI display settings, per model set, On/Off or On Mouse Over (UG52 21.2.3 p473-474), GLOBAL
#     (3.7.2 p126), saved to ./appData/settings/vrfGui (3.7 p125) - the "application settings directory"; vrfGui keeps them
#     in a per-user "user login settings directory" instead only with --useUserSettingsDirectory (UG52 Table 10 p173-174);
#   - the file is default_SymbolDecorationSettings.symx, a boost XML archive <SymbolDecorationSettings version="8">; its
#     myModelSetAndTypeSymbolDecorationMap maps model set -> visual type -> the set of ENABLED decoration creators
#     (vrvCore/DtSymbolDecorationSettings.h :44, :189; enableDecoration :76-77); model set keys are DtModelSetId
#     (vrvCore/DtModelSetEnums.h :19-28: 0 Models3D, 1 ModelsColorized, 5 Icons2D); the Label's creator is
#     DtLabelSymbolDecoration (vrfGuiCore/labelSymbolDecoration.h :22, :52).
# ASCII only.

$LblSettingsLeaf = 'default_SymbolDecorationSettings.symx'
$LblDefaultModelSets = @(5)
$LblDefaultTypes = @('Ground', 'Default')
$LblDefaultDecoration = 'DtLabelSymbolDecoration'

# One model set's entry inside the map: <item ...><first>KEY</first><second><count>N</count><bucket_count>..
$LblKeyRx = '(?s)<item(?: class_id="\d+" tracking_level="\d+" version="\d+")?>\s*<first>(\d+)</first>\s*<second>\s*<count>\d+</count>\s*<bucket_count>\d+</bucket_count>\s*<item_version>\d+</item_version>'
# One visual type's ON set inside a model set's entry (groups: 1 head, 2 count, 3 item_version, 4 items, 5 close).
$LblTypeRx = '(?s)(<item(?: class_id="\d+" tracking_level="\d+" version="\d+")?>\s*<first>{0}</first>\s*<second>\s*<count>)(\d+)(</count>\s*<item_version>\d+</item_version>)((?:\s*<item>[^<]+</item>)*)(\s*</second>)'

function Get-LabelDecorationState {
    # Parse the archive text; never throws. Parsed=false + Error when the shape is not the 5.2d one.
    param([string]$Text, [int[]]$ModelSets = $LblDefaultModelSets, [string[]]$Types = $LblDefaultTypes,
          [string]$Decoration = $LblDefaultDecoration)
    $fail = { param($why) [pscustomobject]@{ Parsed = $false; Error = $why; Entries = @(); AllOn = $false } }
    if ([string]::IsNullOrEmpty($Text)) { return (& $fail 'empty file') }
    if ($Text -notmatch '<SymbolDecorationSettings class_id="0" tracking_level="0" version="8">') {
        return (& $fail 'not a version-8 SymbolDecorationSettings archive (the 5.2d shape)')
    }
    $open = '<myModelSetAndTypeSymbolDecorationMap'
    $a = $Text.IndexOf($open, [System.StringComparison]::Ordinal)
    $b = if ($a -ge 0) { $Text.IndexOf('</myModelSetAndTypeSymbolDecorationMap>', $a, [System.StringComparison]::Ordinal) } else { -1 }
    if ($a -lt 0 -or $b -lt 0) { return (& $fail 'myModelSetAndTypeSymbolDecorationMap not found') }
    $map = $Text.Substring($a, $b - $a)
    $keys = [regex]::Matches($map, $LblKeyRx)
    if ($keys.Count -eq 0) { return (& $fail 'no model-set entries in the map') }
    $spans = @{}
    for ($i = 0; $i -lt $keys.Count; $i++) {
        $end = if ($i + 1 -lt $keys.Count) { $keys[$i + 1].Index } else { $map.Length }
        $spans[[int]$keys[$i].Groups[1].Value] = @($keys[$i].Index, $end)
    }
    $entries = @()
    foreach ($ms in $ModelSets) {
        if (-not $spans.ContainsKey($ms)) { return (& $fail "model set $ms is not in the map") }
        $s = $spans[$ms][0]; $e = $spans[$ms][1]
        $block = $map.Substring($s, $e - $s)
        foreach ($t in $Types) {
            $m = [regex]::new(($LblTypeRx -f [regex]::Escape($t))).Match($block)
            if (-not $m.Success) { return (& $fail "model set $ms has no '$t' entry") }
            $count = [int]$m.Groups[2].Value
            $items = @([regex]::Matches($m.Groups[4].Value, '<item>([^<]+)</item>') | ForEach-Object { $_.Groups[1].Value })
            if ($items.Count -ne $count) { return (& $fail "model set $ms '$t': <count>$count</count> but $($items.Count) item(s)") }
            $indent = if ($m.Groups[4].Value -match '(\r?\n)(\t+)<item>') { $Matches[2] } else { "`t`t`t`t`t`t" }
            $entries += [pscustomobject]@{
                ModelSet = $ms; Type = $t; Count = $count; Items = $items; On = ($items -contains $Decoration)
                Start = $a + $s + $m.Index; Length = $m.Length; Head = $m.Groups[1].Value; Mid = $m.Groups[3].Value
                Tail = $m.Groups[5].Value; Indent = $indent
            }
        }
    }
    $allOn = @($entries | Where-Object { -not $_.On }).Count -eq 0
    return [pscustomobject]@{ Parsed = $true; Error = $null; Entries = $entries; AllOn = $allOn }
}

function Set-LabelDecorationText {
    # The archive with $Decoration added to every requested ON set that lacks it: that set's <count> + 1 and one
    # <item>, kept in byte-ordinal order (a std::set - the order the GUI writes). Throws when the shape is wrong or the
    # result does not re-parse as ON. Changes = 0 means nothing to do (Text is returned unchanged).
    param([string]$Text, [int[]]$ModelSets = $LblDefaultModelSets, [string[]]$Types = $LblDefaultTypes,
          [string]$Decoration = $LblDefaultDecoration)
    $st = Get-LabelDecorationState -Text $Text -ModelSets $ModelSets -Types $Types -Decoration $Decoration
    if (-not $st.Parsed) { throw "the archive does not read as expected: $($st.Error)" }
    $nl = if ($Text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $new = $Text
    $lines = @()
    $changes = 0
    foreach ($en in @($st.Entries | Sort-Object Start -Descending)) {
        if ($en.On) { continue }
        [string[]]$after = @($en.Items) + @($Decoration)
        [Array]::Sort($after, [System.StringComparer]::Ordinal)
        $itemsText = ($after | ForEach-Object { "$nl$($en.Indent)<item>$_</item>" }) -join ''
        $replacement = $en.Head + $after.Count + $en.Mid + $itemsText + $en.Tail
        $new = $new.Substring(0, $en.Start) + $replacement + $new.Substring($en.Start + $en.Length)
        $lines = @(("    model set {0} '{1}': <count>{2}</count> [{3}] -> <count>{4}</count> [{5}]" -f $en.ModelSet, $en.Type,
                    $en.Count, ($en.Items -join ', '), $after.Count, ($after -join ', '))) + $lines
        $changes++
    }
    if ($changes -gt 0) {
        $re = Get-LabelDecorationState -Text $new -ModelSets $ModelSets -Types $Types -Decoration $Decoration
        if (-not $re.Parsed -or -not $re.AllOn) { throw "internal: the edited archive does not re-read as ON ($($re.Error))" }
        $null = [xml]$new    # well-formed XML or throw
    }
    return [pscustomobject]@{ Text = $new; Changes = $changes; Lines = $lines }
}

function Read-LabelSettingsText {
    # The file as text plus what is needed to write it back as the same bytes otherwise (UTF-8, BOM or not).
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = [System.Text.UTF8Encoding]::new($false).GetString($bytes, $(if ($bom) { 3 } else { 0 }), $bytes.Length - $(if ($bom) { 3 } else { 0 }))
    return [pscustomobject]@{ Text = $text; Bom = $bom; Length = $bytes.Length }
}

function Test-LabelDecorationFile {
    # READ-ONLY. Status: on | off | missing | unparseable. Never throws.
    param([string]$Path, [int[]]$ModelSets = $LblDefaultModelSets, [string[]]$Types = $LblDefaultTypes,
          [string]$Decoration = $LblDefaultDecoration)
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [pscustomobject]@{ Status = 'missing'; Detail = 'no such file'; State = $null }
    }
    try { $r = Read-LabelSettingsText -Path $Path }
    catch { return [pscustomobject]@{ Status = 'unparseable'; Detail = $_.Exception.Message; State = $null } }
    $st = Get-LabelDecorationState -Text $r.Text -ModelSets $ModelSets -Types $Types -Decoration $Decoration
    if (-not $st.Parsed) { return [pscustomobject]@{ Status = 'unparseable'; Detail = $st.Error; State = $st } }
    $off = @($st.Entries | Where-Object { -not $_.On } | ForEach-Object { "$($_.ModelSet)/$($_.Type)" })
    if ($off.Count -eq 0) { return [pscustomobject]@{ Status = 'on'; Detail = 'ON for ' + (($st.Entries | ForEach-Object { "$($_.ModelSet)/$($_.Type)" }) -join ', '); State = $st } }
    return [pscustomobject]@{ Status = 'off'; Detail = 'OFF for ' + ($off -join ', '); State = $st }
}

function Split-LblList {
    # A [string[]] whose elements may themselves be "a,b" or "a;b" (pwsh -File passes one string).
    param([string[]]$Values)
    $out = @()
    foreach ($v in @($Values)) { if ($null -ne $v) { $out += @($v -split '[,;]' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }) } }
    return $out
}

function Find-VrfInstalls {
    # Every VR-Forces 5.x install root this machine names, with where each was found. Sources: -VrfRoot; MAK_VRFDIR and
    # VRF_HOME; the installer's registry keys (HKLM/HKCU [WOW6432Node\]SOFTWARE\MAK Technologies\VR-Forces 5*, value
    # Location - written by the 5.2d installer, verified 2026-10-04); the Uninstall entries "VR-Forces 5*"
    # (InstallLocation); C:\MAK\vrforces5*. -NoAutoDiscover = -VrfRoot only. IsInstall: bin64\vrfGui.exe or
    # appData\settings\vrfGui exists.
    param([string[]]$VrfRoot = @(), [switch]$NoAutoDiscover)
    $cands = [System.Collections.Generic.List[object]]::new()
    foreach ($r in @(Split-LblList $VrfRoot)) { $cands.Add(@($r, '-VrfRoot')) }
    if (-not $NoAutoDiscover) {
        foreach ($ev in 'MAK_VRFDIR', 'VRF_HOME') {
            $v = [Environment]::GetEnvironmentVariable($ev)
            if (-not [string]::IsNullOrWhiteSpace($v)) { $cands.Add(@($v, "env $ev")) }
        }
        foreach ($k in 'HKLM:\SOFTWARE\WOW6432Node\MAK Technologies', 'HKLM:\SOFTWARE\MAK Technologies',
                       'HKCU:\SOFTWARE\WOW6432Node\MAK Technologies', 'HKCU:\SOFTWARE\MAK Technologies') {
            if (-not (Test-Path -LiteralPath $k)) { continue }
            foreach ($sub in @(Get-ChildItem -LiteralPath $k -ErrorAction SilentlyContinue | Where-Object { $_.PSChildName -like 'VR-Forces 5*' })) {
                $loc = $null
                try { $loc = (Get-ItemProperty -LiteralPath $sub.PSPath -Name Location -ErrorAction Stop).Location } catch { }
                if (-not [string]::IsNullOrWhiteSpace($loc)) { $cands.Add(@($loc, ('registry ' + ($sub.Name -replace '^HKEY_LOCAL_MACHINE', 'HKLM' -replace '^HKEY_CURRENT_USER', 'HKCU')))) }
            }
        }
        foreach ($u in 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall', 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall') {
            if (-not (Test-Path -LiteralPath $u)) { continue }
            foreach ($sub in @(Get-ChildItem -LiteralPath $u -ErrorAction SilentlyContinue)) {
                try {
                    $p = Get-ItemProperty -LiteralPath $sub.PSPath -ErrorAction Stop
                    if ($p.PSObject.Properties['DisplayName'] -and $p.DisplayName -like 'VR-Forces 5*' -and
                        $p.PSObject.Properties['InstallLocation'] -and -not [string]::IsNullOrWhiteSpace($p.InstallLocation)) {
                        $cands.Add(@($p.InstallLocation, ('uninstall entry "' + $p.DisplayName + '"')))
                    }
                } catch { }
            }
        }
        if (Test-Path -LiteralPath 'C:\MAK') {
            foreach ($d in @(Get-ChildItem -LiteralPath 'C:\MAK' -Directory -Filter 'vrforces5*' -ErrorAction SilentlyContinue)) { $cands.Add(@($d.FullName, 'C:\MAK\vrforces5*')) }
        }
    }
    $byKey = [ordered]@{}
    foreach ($c in $cands) {
        $full = $c[0]
        try { $full = [System.IO.Path]::GetFullPath($c[0]) } catch { }
        $full = $full.TrimEnd('\')
        $key = $full.ToLowerInvariant()
        if (-not $byKey.Contains($key)) { $byKey[$key] = [pscustomobject]@{ Root = $full; Sources = [System.Collections.Generic.List[string]]::new() } }
        if (-not $byKey[$key].Sources.Contains($c[1])) { $byKey[$key].Sources.Add($c[1]) }
    }
    foreach ($v in $byKey.Values) {
        $isInstall = (Test-Path -LiteralPath (Join-Path $v.Root 'bin64\vrfGui.exe') -PathType Leaf) -or
                     (Test-Path -LiteralPath (Join-Path $v.Root 'appData\settings\vrfGui') -PathType Container)
        [pscustomobject]@{ Root = $v.Root; Sources = @($v.Sources); IsInstall = $isInstall
                           SettingsFile = (Join-Path $v.Root ('appData\settings\vrfGui\' + $LblSettingsLeaf)) }
    }
}

function Find-UserLabelSettings {
    # Per-user copies of the file (vrfGui --useUserSettingsDirectory keeps settings in the "user login settings
    # directory", UG52 Table 10 p173-174; its location is not documented): every vrfGui\default_SymbolDecorationSettings.symx
    # up to 6 levels under each root.
    param([string[]]$Roots = @())
    foreach ($r in @(Split-LblList $Roots)) {
        if (-not (Test-Path -LiteralPath $r -PathType Container)) { continue }
        @(Get-ChildItem -LiteralPath $r -Recurse -Depth 6 -File -Filter $LblSettingsLeaf -ErrorAction SilentlyContinue |
          Where-Object { $_.Directory.Name -eq 'vrfGui' } | ForEach-Object { $_.FullName })
    }
}

function Get-LblDefaultUserRoots {
    $docs = [Environment]::GetFolderPath('MyDocuments')
    @(@($env:APPDATA, $env:LOCALAPPDATA, $docs) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
      ForEach-Object { Join-Path $_ 'MAK' })
}

function Get-RunningVrfGuiPaths {
    # The executable of every running vrfGui; '?' where it cannot be read (treated as blocking everything).
    @(Get-Process -Name 'vrfGui' -ErrorAction SilentlyContinue | ForEach-Object {
        $p = $null
        try { $p = $_.Path } catch { }
        if ([string]::IsNullOrEmpty($p)) { '?' } else { $p }
    })
}

function Test-LblGuiBlocks {
    # Does a running vrfGui block writing this target? An install's own vrfGui blocks that install; any vrfGui blocks a
    # target that is not an install's own (a relocated appData, a per-user copy); an unreadable path ('?') blocks all.
    param([string]$InstallRoot, [string[]]$GuiPaths)
    foreach ($g in @($GuiPaths)) {
        if ([string]::IsNullOrWhiteSpace($g)) { continue }
        if ($g -eq '?' -or [string]::IsNullOrWhiteSpace($InstallRoot)) { return $true }
        $gf = $g
        try { $gf = [System.IO.Path]::GetFullPath($g) } catch { }
        if ($gf.StartsWith($InstallRoot.TrimEnd('\') + '\', [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}
