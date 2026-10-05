#Requires -Version 7.0
# Enable-LabelDecoration.ps1 - THE LABEL DECORATION DEPLOY STEP (LBL; RL-20261004-05: "I need this to work on every
# installed instance of this component. Ok to change C:\MAK, but needs to be automated/reproducible").
#
# Turns ON the LABEL symbol decoration in the VR-Forces GUI of EVERY VR-Forces 5.x install on the machine, so the map shows
# each object's vendor Label - the clean designator the interface creates every object with (LBL, LBL2) - beside its cut
# name. The logic is tools\display\LabelDecorationLib.ps1; scripts\LaunchVrf52.ps1 runs its READ-ONLY check before every
# GUI launch and WARNS when the decoration is off (it never edits).
#
# WHEN TO RUN IT: after ANY VR-Forces install or reinstall, and with every deploy (RUNBOOK sec 9). -WhatIf first:
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\display\Enable-LabelDecoration.ps1 -Verify
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\display\Enable-LabelDecoration.ps1 -WhatIf
#   & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\display\Enable-LabelDecoration.ps1
#
# DISCOVERY - every install, each reported with where it was found: -VrfRoot (comma/semicolon lists allowed); MAK_VRFDIR
# and VRF_HOME; the installer's registry keys HKLM/HKCU [WOW6432Node\]SOFTWARE\MAK Technologies\VR-Forces 5* (value
# Location); the Uninstall entries "VR-Forces 5*" (InstallLocation); C:\MAK\vrforces5*. A path that is not an install
# (no bin64\vrfGui.exe and no appData\settings\vrfGui) is SKIPPED and said. -NoAutoDiscover = -VrfRoot only (the tests).
#
# WHAT IS EDITED, PER INSTALL: <root>\appData\settings\vrfGui\default_SymbolDecorationSettings.symx - the GUI's settings in
# the APPLICATION settings directory, which vrfGui uses unless started with --useUserSettingsDirectory (UG52 Table 10
# p173-174: "store settings in the user login settings directory, rather than the application settings directory"; 3.7
# p125 "Other settings are saved to ./appData/settings/vrfGui"; the decorations are GLOBAL settings, 3.7.2 p126, so no
# scenario fixture carries them - the Iron Storm .scnx's .gui_settings holds only overlays). Also edited: a relocated
# appData given by -AppDataDir (LaunchVrf52 -AppDataDir / UG52 --appDataDir), and any PER-USER copy found under
# -UserSettingsRoot (default %APPDATA%\MAK, %LOCALAPPDATA%\MAK, Documents\MAK, searched 6 deep for
# vrfGui\default_SymbolDecorationSettings.symx; the user directory's location is not documented, and none exists on the
# 2026-10-04 machine; LaunchVrf52 never passes --useUserSettingsDirectory). The edit: DtLabelSymbolDecoration added to the
# ON set of model set 5 (Icons2D, the 2D Plan View; vrvCore/DtModelSetEnums.h) for types Ground and Default - one <item>
# and that set's <count> + 1 each; nothing else in the file moves.
#
# SAFETY: a target whose vrfGui is RUNNING is REFUSED (the GUI rewrites its settings when it exits, UG52 3.7 p124) - an
# install's own vrfGui blocks that install, any vrfGui blocks a relocated or per-user copy, an unreadable process path
# blocks all; this script never stops a process. Each write is preceded by a timestamped backup beside the file
# (<file>.bak-<UTC>), keeps the file's encoding, and is RE-READ and verified ON. A missing file is never invented.
# Idempotent: an install already ON is left byte-identical and said so.
#
# Exit codes, -Verify (check only, writes nothing): 0 every target ON; 1 some target OFF; 2 a target's file missing or
#   unparseable, or no install found.
# Exit codes, deploy: 0 every target ON (changed, would change under -WhatIf, or already); 2 a file missing / bad
#   arguments / no install found; 3 a file unparseable or a write that did not re-read ON; 4 a target REFUSED (its vrfGui
#   is running); 5 unexpected. The worst over all targets is returned; every target is still processed and reported.
# -RunningGuiPath replaces the live process list (tests: '' = none, '?' = an unreadable path).
# ASCII only.
[CmdletBinding(SupportsShouldProcess)]
param(
    [string[]] $VrfRoot          = @(),
    [string[]] $AppDataDir       = @(),
    [switch]   $NoAutoDiscover,
    [switch]   $Verify,
    [string[]] $UserSettingsRoot,
    [string[]] $RunningGuiPath,
    [int[]]    $ModelSets        = @(5),
    [string[]] $Types            = @('Ground', 'Default'),
    [string]   $Decoration       = 'DtLabelSymbolDecoration'
)
Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

function Say      { param([string]$m) Write-Host $m }

try {
    . (Join-Path $PSScriptRoot 'LabelDecorationLib.ps1')
    if ($Decoration -notmatch '^Dt[A-Za-z]+SymbolDecoration$') { Say "[FAIL] -Decoration '$Decoration' is not a symbol decoration creator name"; exit 2 }
    $mode = if ($Verify) { 'VERIFY (read-only)' } elseif ($WhatIfPreference) { 'WHATIF (writes nothing)' } else { 'DEPLOY' }
    Say ("=== Enable-LabelDecoration.ps1 - {0}: {1} for model set(s) {2}, type(s) {3} (RL-20261004-05) ===" -f $mode, $Decoration, ($ModelSets -join ','), ($Types -join ','))

    # ---- the targets ---------------------------------------------------------------------------------------------
    $targets = [System.Collections.Generic.List[object]]::new()
    foreach ($i in @(Find-VrfInstalls -VrfRoot $VrfRoot -NoAutoDiscover:$NoAutoDiscover)) {
        if (-not $i.IsInstall) {
            Say ("SKIPPED {0} - not a VR-Forces install (no bin64\vrfGui.exe, no appData\settings\vrfGui); found by: {1}" -f $i.Root, ($i.Sources -join '; '))
            continue
        }
        $targets.Add([pscustomobject]@{ Kind = 'install'; Root = $i.Root; File = $i.SettingsFile; Header = ("INSTALL {0}  (found by: {1})" -f $i.Root, ($i.Sources -join '; ')) })
    }
    foreach ($ad in @(Split-LblList $AppDataDir)) {
        $f = Join-Path $ad ('settings\vrfGui\' + $LblSettingsLeaf)
        $targets.Add([pscustomobject]@{ Kind = 'appData'; Root = ''; File = $f; Header = ("SETTINGS (relocated appData, -AppDataDir) {0}" -f $f) })
    }
    $userRoots = if ($PSBoundParameters.ContainsKey('UserSettingsRoot')) { $UserSettingsRoot } elseif ($NoAutoDiscover) { @() } else { Get-LblDefaultUserRoots }
    foreach ($uf in @(Find-UserLabelSettings -Roots $userRoots)) {
        $targets.Add([pscustomobject]@{ Kind = 'per-user'; Root = ''; File = $uf; Header = ("SETTINGS (per-user copy) {0}" -f $uf) })
    }
    if ($targets.Count -eq 0) {
        Say '[FAIL] no VR-Forces 5.x install found (pass -VrfRoot, or see the discovery sources in this script''s header).'
        exit 2
    }
    $guiPaths = if ($PSBoundParameters.ContainsKey('RunningGuiPath')) { @(Split-LblList $RunningGuiPath) } else { Get-RunningVrfGuiPaths }
    $guiPaths = @($guiPaths | Select-Object -Unique)

    # ---- each target ---------------------------------------------------------------------------------------------
    $worst = 0
    foreach ($t in $targets) {
        Say $t.Header
        $st = Test-LabelDecorationFile -Path $t.File -ModelSets $ModelSets -Types $Types -Decoration $Decoration
        if ($Verify) {
            switch ($st.Status) {
                'on'          { Say ("  [ON]          {0} - {1}" -f $t.File, $st.Detail) }
                'off'         { Say ("  [OFF]         {0} - {1}" -f $t.File, $st.Detail); $worst = [Math]::Max($worst, 1) }
                'missing'     { Say ("  [MISSING]     {0}" -f $t.File); $worst = 2 }
                default       { Say ("  [UNPARSEABLE] {0} - {1}" -f $t.File, $st.Detail); $worst = 2 }
            }
            continue
        }
        # (if/elseif, not switch: a `continue` inside a PowerShell switch continues the SWITCH, not this loop)
        if ($st.Status -eq 'missing') {
            Say ("  [MISSING]     {0} - nothing written (a settings file is never invented; start this install's GUI once so it writes its defaults, then re-run)." -f $t.File)
            $worst = [Math]::Max($worst, 2); continue
        } elseif ($st.Status -eq 'unparseable') {
            Say ("  [UNPARSEABLE] {0} - {1}; NOTHING written." -f $t.File, $st.Detail)
            $worst = [Math]::Max($worst, 3); continue
        } elseif ($st.Status -eq 'on') {
            Say ("  [ok] already ON - nothing to change ({0}); file untouched." -f $st.Detail)
            continue
        }
        if (Test-LblGuiBlocks -InstallRoot $t.Root -GuiPaths $guiPaths) {
            Say ("  [REFUSED] a vrfGui is running ({0}) - it rewrites its settings when it exits and would undo this. Close it normally and re-run; this script never stops a process. NOTHING written." -f ($guiPaths -join ', '))
            $worst = [Math]::Max($worst, 4)
            continue
        }
        $r = Read-LabelSettingsText -Path $t.File
        $edit = Set-LabelDecorationText -Text $r.Text -ModelSets $ModelSets -Types $Types -Decoration $Decoration
        foreach ($l in $edit.Lines) { Say $l }
        if ($PSCmdlet.ShouldProcess($t.File, "enable $Decoration ($($edit.Changes) change(s))")) {
            $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ')
            $backup = "$($t.File).bak-$stamp"
            Copy-Item -LiteralPath $t.File -Destination $backup
            # Atomic replace: write a temp file beside the target, then move it over (a crash mid-write
            # leaves the original intact; the .bak above is the second line of defence).
            $tmp = "$($t.File).tmp-$stamp"
            [System.IO.File]::WriteAllText($tmp, $edit.Text, [System.Text.UTF8Encoding]::new($r.Bom))
            [System.IO.File]::Move($tmp, $t.File, $true)
            $re = Test-LabelDecorationFile -Path $t.File -ModelSets $ModelSets -Types $Types -Decoration $Decoration
            $back = Read-LabelSettingsText -Path $t.File
            if ($re.Status -ne 'on' -or $back.Text -cne $edit.Text) {
                Say ("  [FAIL] the re-read is not ON ({0}) - restore from {1}" -f $re.Detail, $backup)
                $worst = [Math]::Max($worst, 3)
                continue
            }
            Say ("  [done] {0} change(s); re-read and verified ON; backup {1}; sha256 {2}" -f $edit.Changes, $backup,
                 (Get-FileHash -LiteralPath $t.File -Algorithm SHA256).Hash.ToLowerInvariant())
        } else {
            Say ("  [WhatIf] {0} change(s) WOULD be made; nothing written." -f $edit.Changes)
        }
    }
    Say ("=== exit {0} ===" -f $worst)
    exit $worst
}
catch {
    Say "[FAIL] unexpected: $($_.Exception.Message)"
    exit 5
}
