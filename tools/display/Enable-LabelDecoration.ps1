#Requires -Version 7.0
# Enable-LabelDecoration.ps1 - turn ON the LABEL symbol decoration in the VR-Forces GUI, so the map shows each object's
# vendor Label - the full C2SIM designation the interface now creates every object with (LBL, HANDOFF_SEAT_2026-09-28
# sec 3 item 5) - beside its (cut) name.
#
# Run with pwsh 7 (pinned). -WhatIf FIRST - it reads, checks and prints the before/after, and writes nothing:
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\display\Enable-LabelDecoration.ps1 -WhatIf
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\display\Enable-LabelDecoration.ps1
#
# THIS IS NOT A FIXTURE EDIT, AND THAT IS THE VENDOR'S DESIGN, NOT A CHOICE MADE HERE.
#   - The decorations are chosen on Settings > Display > Symbol Decoration Settings, per MODEL SET, On/Off or On Mouse
#     Over (UG52 21.2.3 p473-474); "Label" is one of them (21.2 p470: "Label - If extended labels exist, each label can
#     be displayed separately"); the label is "one of the symbol decorations displayed in Plan View observer mode"
#     (13.2.5 p364).
#   - They are GLOBAL settings: "Global settings include simulation object labels" (UG52 3.7.2 p126); "Other settings are
#     saved to ./appData/settings/vrfGui" (3.7 p125). The scenario's own GUI file (<scenario>.gui_settings, "GUI settings
#     that only apply to this scenario", 12.1 p350) carries none: the Iron Storm fixture's (IronStorm_Centre_52_Aggregate
#     .scnx -> .gui_settings, 1439 B) holds only DtObjectSettings (0), SystemScriptsAvailable (0) and four Overlays.
#   - So the file is the GUI's symbol decoration settings, default_SymbolDecorationSettings.symx (a boost XML archive,
#     <SymbolDecorationSettings version="8">), and the switch is one entry in its myModelSetAndTypeSymbolDecorationMap:
#     model set -> visual type -> the set of ENABLED decoration creators (vrvCore/DtSymbolDecorationSettings.h :44, :189
#     "myModelSetAndTypeSymbolDecorationMap"; enableDecoration(modelSet, type, decorationCreator) :76-77). The model-set
#     keys are DtModelSetId (vrvCore/DtModelSetEnums.h :19-28): 0 Models3D, 1 ModelsColorized, 5 Icons2D - the 2D Plan
#     View is 5. The creator name is DtLabelSymbolDecoration (vrfGuiCore/labelSymbolDecoration.h :22, :52; the file names
#     every other creator by its class, e.g. DtNameSymbolDecoration).
#
# WHERE IT WRITES. Default -SettingsFile is the vendor appData copy, C:\MAK\vrforces5.2d\appData\settings\vrfGui\
# default_SymbolDecorationSettings.symx - the one the runner's GUI reads (LaunchVrf52.ps1 passes no --appDataDir unless
# -AppDataDir is given, and C:\C2SIM\vrf-appdata does not exist on this machine as of 2026-10-04). That is UNDER C:\MAK and
# is NOT the sanctioned fixture deploy (CLAUDE.md sec 5): running this without -WhatIf on that path is the SEAT's / the
# owner's decision. A path outside C:\MAK (a relocated appData, UG52 Table 10 --appDataDir, or vrfGui
# --sharedSettingsDirectory, UG52 Table 10 p173 "default directory is $(APP_DIR)/settings") is taken as given.
#
# SAFETY. The GUI rewrites its settings when it exits ("It remembers the settings in effect when you exit", UG52 3.7
# p124), so a running vrfGui would overwrite this edit: the script REFUSES while a vrfGui process exists (it never
# stops one). It asserts the archive's shape before touching it, edits only the named (model set, type) entries, keeps
# each decoration set sorted (the order the GUI writes), increments that set's <count>, backs the file up beside itself
# (<file>.bak-<UTC stamp>), writes it back as the same UTF-8 bytes otherwise, and re-reads it to verify. Idempotent: a
# set that already holds the decoration is reported and left alone.
#
# Exit codes: 0 done (or would be, with -WhatIf) or already on; 2 bad arguments / file missing; 3 the archive does not
#   read as expected (NOTHING written); 4 vrfGui is running (NOTHING written); 5 unexpected terminating error.
# ASCII only.
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]   $SettingsFile = 'C:\MAK\vrforces5.2d\appData\settings\vrfGui\default_SymbolDecorationSettings.symx',
    # DtModelSetId keys (vrvCore/DtModelSetEnums.h): 5 = Icons2D (Plan View). Add 0 (Models3D) / 1 (ModelsColorized) for
    # the 3D views.
    [int[]]    $ModelSets    = @(5),
    # Visual types as the file names them (Ground, Default, Surface, Subsurface, Fixed Wing, Space, Lifeform, Rotary Wing,
    # Munition). Ground and Default cover every land unit and container on the map.
    [string[]] $Types        = @('Ground', 'Default'),
    [string]   $Decoration   = 'DtLabelSymbolDecoration'
)
Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

function Say([string]$m) { Write-Host $m }

try {
    if (-not [System.IO.Path]::IsPathRooted($SettingsFile) -or $SettingsFile.StartsWith('\\')) {
        Say "[ERR] -SettingsFile must be an absolute local path: '$SettingsFile'"; exit 2
    }
    if (-not (Test-Path -LiteralPath $SettingsFile -PathType Leaf)) {
        Say "[ERR] settings file not found: $SettingsFile"; exit 2
    }
    if ($Decoration -notmatch '^Dt[A-Za-z]+SymbolDecoration$') {
        Say "[ERR] -Decoration '$Decoration' is not a symbol decoration creator name"; exit 2
    }
    $underMak = $SettingsFile.StartsWith('C:\MAK\', [System.StringComparison]::OrdinalIgnoreCase)
    if ($underMak) {
        Say "[NOTE] $SettingsFile is UNDER C:\MAK - not the sanctioned fixture deploy (CLAUDE.md sec 5); writing it is the seat's / owner's call."
    }
    $gui = @(Get-Process -Name 'vrfGui' -ErrorAction SilentlyContinue)
    if ($gui.Count -gt 0) {
        Say ("[STOP] vrfGui is running (pid {0}) - it rewrites its settings on exit and would overwrite this edit. Close it " +
             "normally first; this script never stops a process. NOTHING written." -f (($gui | ForEach-Object Id) -join ', '))
        exit 4
    }

    $bytes = [System.IO.File]::ReadAllBytes($SettingsFile)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $sha0 = (Get-FileHash -LiteralPath $SettingsFile -Algorithm SHA256).Hash.ToLowerInvariant()
    Say "settings file : $SettingsFile ($($bytes.Length) B, sha256 $sha0)"

    if ($text -notmatch '<SymbolDecorationSettings class_id="0" tracking_level="0" version="8">') {
        Say '[ERR] not a version-8 SymbolDecorationSettings archive (the 5.2d shape) - NOTHING written.'; exit 3
    }
    $mapOpen = '<myModelSetAndTypeSymbolDecorationMap'
    $mapClose = '</myModelSetAndTypeSymbolDecorationMap>'
    $a = $text.IndexOf($mapOpen, [System.StringComparison]::Ordinal)
    $b = if ($a -ge 0) { $text.IndexOf($mapClose, $a, [System.StringComparison]::Ordinal) } else { -1 }
    if ($a -lt 0 -or $b -lt 0) { Say '[ERR] myModelSetAndTypeSymbolDecorationMap not found - NOTHING written.'; exit 3 }
    $map = $text.Substring($a, $b - $a)

    # The map's items: <item ...><first>KEY</first><second> ...per-type items... </second></item>, one per model set. The
    # per-type item: <item ...><first>TYPE</first><second><count>N</count><item_version>0</item_version> (<item>DECO</item>)*
    # </second></item>. Located by regex on the map's text only; every edit is re-checked by re-parsing below.
    $typeRx = '(?s)(<item(?: class_id="\d+" tracking_level="\d+" version="\d+")?>\s*<first>{0}</first>\s*<second>\s*<count>)(\d+)(</count>\s*<item_version>\d+</item_version>)((?:\s*<item>[^<]+</item>)*)(\s*</second>)'
    $keyRx = '(?s)<item(?: class_id="\d+" tracking_level="\d+" version="\d+")?>\s*<first>(\d+)</first>\s*<second>\s*<count>\d+</count>\s*<bucket_count>\d+</bucket_count>\s*<item_version>\d+</item_version>'
    $keys = [regex]::Matches($map, $keyRx)
    if ($keys.Count -eq 0) { Say '[ERR] no model-set entries in the map - NOTHING written.'; exit 3 }
    $spans = @{}
    for ($i = 0; $i -lt $keys.Count; $i++) {
        $start = $keys[$i].Index
        $end = if ($i + 1 -lt $keys.Count) { $keys[$i + 1].Index } else { $map.Length }
        $spans[[int]$keys[$i].Groups[1].Value] = @($start, $end)
    }
    Say ("model sets in the file: {0}" -f (($spans.Keys | Sort-Object) -join ', '))

    $newMap = $map
    $changes = 0
    foreach ($ms in ($ModelSets | Sort-Object -Descending)) {     # right to left, so earlier offsets stay valid
        if (-not $spans.ContainsKey($ms)) { Say "[ERR] model set $ms is not in the file - NOTHING written."; exit 3 }
        $s = $spans[$ms][0]; $e = $spans[$ms][1]
        $block = $newMap.Substring($s, $e - $s)
        foreach ($t in $Types) {
            $rx = [regex]::new(($typeRx -f [regex]::Escape($t)))
            $m = $rx.Match($block)
            if (-not $m.Success) { Say "[ERR] model set $ms has no '$t' entry - NOTHING written."; exit 3 }
            $count = [int]$m.Groups[2].Value
            $items = [regex]::Matches($m.Groups[4].Value, '<item>([^<]+)</item>') | ForEach-Object { $_.Groups[1].Value }
            $items = @($items)
            if ($items.Count -ne $count) {
                Say "[ERR] model set $ms '$t': <count>$count</count> but $($items.Count) item(s) - NOTHING written."; exit 3
            }
            if ($items -contains $Decoration) {
                Say "[ok] model set $ms '$t' already shows ${Decoration}: [$($items -join ', ')]"
                continue
            }
            [string[]]$after = @($items) + @($Decoration)
            [Array]::Sort($after, [System.StringComparer]::Ordinal)     # the GUI's own order: a std::set, byte-ordinal
            $indent = if ($m.Groups[4].Value -match '(\r?\n)(\t+)<item>') { $Matches[2] } else { "`t`t`t`t`t`t" }
            $itemsText = ($after | ForEach-Object { "$nl$indent<item>$_</item>" }) -join ''
            $replacement = $m.Groups[1].Value + $after.Count + $m.Groups[3].Value + $itemsText + $m.Groups[5].Value
            Say "[edit] model set $ms '$t':"
            Say "   before: <count>$count</count> [$($items -join ', ')]"
            Say "   after : <count>$($after.Count)</count> [$($after -join ', ')]"
            $block = $block.Substring(0, $m.Index) + $replacement + $block.Substring($m.Index + $m.Length)
            $changes++
        }
        $newMap = $newMap.Substring(0, $s) + $block + $newMap.Substring($e)
    }
    if ($changes -eq 0) { Say "[ok] nothing to change - $Decoration is already on for every requested entry."; exit 0 }
    $newText = $text.Substring(0, $a) + $newMap + $text.Substring($b)

    # Re-parse the result: every requested entry now holds the decoration, every count matches its items, and nothing
    # outside the touched entries moved (the text outside the map is byte-identical).
    if ($newText.Substring(0, $a) -ne $text.Substring(0, $a) -or
        $newText.Substring($a + $newMap.Length) -ne $text.Substring($b)) {
        Say '[ERR] internal: text outside the map changed - NOTHING written.'; exit 3
    }
    $null = [xml]$newText    # well-formed XML or throw (exit 5)
    if ($PSCmdlet.ShouldProcess($SettingsFile, "enable $Decoration for model set(s) $($ModelSets -join ',') type(s) $($Types -join ',') ($changes change(s))")) {
        $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
        $backup = "$SettingsFile.bak-$stamp"
        Copy-Item -LiteralPath $SettingsFile -Destination $backup
        $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        [System.IO.File]::WriteAllText($SettingsFile, $newText, [System.Text.UTF8Encoding]::new($hasBom))
        $check = [System.IO.File]::ReadAllText($SettingsFile, [System.Text.Encoding]::UTF8)
        if ($check -ne $newText) { Say "[ERR] read-back differs - restore from $backup"; exit 3 }
        $sha1 = (Get-FileHash -LiteralPath $SettingsFile -Algorithm SHA256).Hash.ToLowerInvariant()
        Say "[done] $changes entr(y/ies) changed; backup $backup; new sha256 $sha1"
        Say 'Next: start the GUI; Settings > Display > Symbol Decoration Settings should show Label ticked in the On/Off column (UG52 21.2.3).'
    }
    else {
        Say "[WhatIf] $changes entr(y/ies) WOULD change; nothing written (sha256 still $sha0)."
    }
    exit 0
}
catch {
    Say "[ERR] unexpected: $($_.Exception.Message)"
    exit 5
}
