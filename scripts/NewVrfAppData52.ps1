# NewVrfAppData52.ps1 - seed a RUN-OWNED VR-Forces 5.2d appData tree OUTSIDE C:\MAK and
# turn off the two GUI modals that hang an unattended teardown (STP-844).
#
# WHY THIS EXISTS. Demo rehearsal D1 (run 20260920T172141Z) was the first 5.2 GUI-ON
# teardown ever attempted. StopVrf52.ps1 sent the GUI a WM_CLOSE, the GUI raised its
# documented exit prompt (UG52 4.6), nothing answered it, the teardown spent its whole
# 121 s budget waiting for a human and vrfGui was left running and still JOINED. A
# read-only enumeration of that pid afterwards found TWO stacked never-ask-again message
# boxes - "Are You Sure?" (the exit prompt) with "Session Status" on top of it. The full
# mechanism, the vendor citations and the two persisted keys are documented at the
# "THE TWO 5.2 GUI TEARDOWN MODALS" block in scripts\RunnerLib.ps1; read that first.
#
# THE REMEDY IS THE VENDOR'S OWN CONFIGURATION, NOT GUI AUTOMATION. UG52 4.6.1 disables
# the exit prompt (myShowQuitDialogOnClose) and UG52 4.3.1's "Show Session Terrain Change
# Prompts" is the session-dialog flag (DtShowSessionDialogs 0x10 inside mySessionOptions).
# Nothing here clicks anything, and nothing here is a UI Automation answerer: this project
# is headed for headless operation and GUI automation is deliberately not built.
#
# WHY A COPY AND NOT AN IN-PLACE EDIT. Those two settings live in
#   C:\MAK\vrforces5.2d\appData\settings\vrfGui\{default_Application.apsx,
#                                                default_SessionSettings.srsx}
# which is UNDER C:\MAK. Writing under C:\MAK is forbidden here, and a vendor reinstall
# reverts that tree anyway (UG52 "Uninstalling", p.100-101). There is no user-profile
# copy of these files to edit instead: the only other trees carrying the key are the
# vendor's own settings\exampleCustom\ and the project's existing relocated tree. So the
# change goes in a COPY, and the copy is selected at launch time with the documented
# --appDataDir option (UG52 Table 10 p164 for vrfGui, Table 11 p178 for vrfSim, same
# text: "Specifies the location of application data. If not specified, VR-Forces uses the
# default: ./appData"). 5.2 is the first release where that option can be trusted -
# Release Notes VRF-9265 fixed "--appDataDir is not processed until after attempting to
# load config files and many instances of hard-coded relative paths" and VRF-9255 taught
# the Launcher about it.
#
# THE RESTORE PATH IS "DELETE THE COPY". The vendor tree is never opened for writing by
# this script (it is the robocopy SOURCE and the junction TARGET, both read-only uses), so
# reverting to D1 behaviour is: stop passing -AppDataDir, or Remove-Item the -Dest tree.
# Nothing in C:\MAK has to be put back, because nothing in C:\MAK was changed.
#
# THE LAYOUT (identical to C:\C2SIM\vrf-appdata, whose README-C2SIM.txt carries the
# provenance of the first relocation and the reasoning behind the nested shape):
#   <Dest>\
#       appData\        <-- THIS is the --appDataDir value
#           cache\      JUNCTION -> <VrfRoot>\appData\cache   (1.4 M files; never copied)
#           settings\   real copy; the two edits live here
#           cdb\ drivers\ drivers-legacy\ gdal_data\ importConfig\ logs\ osgEarth\
#           plugins\ proj_lib\ qt\ schema\ videoStreams\      real copies
#       data            JUNCTION -> <VrfRoot>\data
#       userData        JUNCTION -> <VrfRoot>\userData
# The nested appData\ plus the two sibling junctions exist because vendor settings files
# carry live "../data/..." and "../appData/..." references; both readings of the base path
# then resolve. See C:\C2SIM\vrf-appdata\README-C2SIM.txt for the full argument.
#
# THIS TREE IS NOT C:\C2SIM\vrf-appdata. That one carries a DIFFERENT single edit
# ((setqb loadAllNavigationDataOnTerrainLoad 1)) and pointing a demo at it would change
# nav-data loading at the same time as the quit prompt - two variables in one run. A tree
# seeded here differs from the vendor's by the two GUI prompt keys and by NOTHING else.
#
# Exit codes:
#   0 = the tree exists and both prompts are OFF (seeded now, or already in that state)
#   2 = bad arguments (no -Dest, -Dest under C:\MAK, missing source), or a dry run that
#       would have been refused
#   3 = the seed or the edit failed, or the verification read-back disagrees
#   5 = unexpected terminating error
# ASCII only.
[CmdletBinding()]
param(
    # The PARENT directory that will hold appData\ (plus the data\userData junctions).
    # MANDATORY and must not be under C:\MAK. Example: C:\C2SIM\vrf-appdata-unattended
    [string] $Dest    = '',
    [string] $VrfRoot = 'C:\MAK\vrforces5.2d',
    # Re-seed over an existing -Dest. Without it an existing tree is only RE-CHECKED and
    # re-edited, never re-copied - so running this twice is cheap and idempotent.
    [switch] $Force,
    [switch] $DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Say      { param([string]$m) Write-Host $m }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Info { param([string]$m) Write-Host ('  [..]   ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [PLAN] ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }

. (Join-Path $PSScriptRoot 'RunnerLib.ps1')

try {

Say '=== NewVrfAppData52.ps1 - run-owned appData with the GUI quit/session prompts OFF (STP-844) ==='
Say ('  Dest    : {0}' -f $(if ($Dest) { $Dest } else { '(MISSING)' }))
Say ('  VrfRoot : {0}' -f $VrfRoot)
Say ('  Force   : {0}   DryRun : {1}' -f [bool]$Force, [bool]$DryRun)

# ---- arguments FIRST, before anything is touched ------------------------------
if ([string]::IsNullOrWhiteSpace($Dest)) {
    Say-Fail '-Dest is MANDATORY: the parent directory that will hold appData\ (e.g. C:\C2SIM\vrf-appdata-unattended).'
    exit 2
}
# The C:\MAK guard is deliberately a PREFIX test on the raw string as well as on the
# resolved path: a junction or a substituted drive could resolve into C:\MAK from a path
# that does not look like it, and a path that LOOKS like it must be refused even if it
# does not exist yet (Resolve-Path cannot be used on a directory we are about to create).
$destFull = $Dest
try { $destFull = [System.IO.Path]::GetFullPath($Dest) } catch { }
if ($destFull -like 'C:\MAK*' -or $Dest -like 'C:\MAK*') {
    Say-Fail ('-Dest is under C:\MAK ({0}) - REFUSED. The whole point of this script is that the vendor tree is never written to; a reinstall reverts it and this project does not modify C:\MAK.' -f $destFull)
    exit 2
}
$srcAppData = Join-Path $VrfRoot 'appData'
$srcCache   = Join-Path $srcAppData 'cache'
if (-not (Test-Path -LiteralPath $srcAppData -PathType Container)) {
    Say-Fail ('source appData not found: {0} (is -VrfRoot right?)' -f $srcAppData)
    exit 2
}
$srcGui = Join-Path $srcAppData 'settings\vrfGui'
if (-not (Test-Path -LiteralPath $srcGui -PathType Container)) {
    Say-Fail ('source has no settings\vrfGui: {0} - this is not a VR-Forces appData tree.' -f $srcGui)
    exit 2
}
Say-Ok ('source appData: {0} (read-only use: robocopy source + junction target)' -f $srcAppData)

$dstAppData = Join-Path $destFull 'appData'
$dstGui     = Join-Path $dstAppData 'settings\vrfGui'
$alreadyThere = (Test-Path -LiteralPath $dstGui -PathType Container)

# The four files that can carry the two keys. The two default_* files are the ones the
# GUI reads (they are the only Application/SessionSettings records in the tree); the two
# backups\ files are rewritten by the GUI at startup and are patched purely so that no
# copy of the tree disagrees with itself. Missing backups are NOT an error.
$targets = @(
    [pscustomobject]@{ Rel = 'settings\vrfGui\default_Application.apsx';       Kind = 'Application';     Required = $true  },
    [pscustomobject]@{ Rel = 'settings\vrfGui\default_SessionSettings.srsx';   Kind = 'SessionSettings'; Required = $true  },
    [pscustomobject]@{ Rel = 'settings\vrfGui\backups\Application.backup';     Kind = 'Application';     Required = $false },
    [pscustomobject]@{ Rel = 'settings\vrfGui\backups\SessionSettings.backup'; Kind = 'SessionSettings'; Required = $false }
)

# ---- what WOULD happen --------------------------------------------------------
if ($DryRun) {
    Say ''
    Say '=== Dry run - nothing is copied, linked or written ==='
    if ($alreadyThere -and -not $Force) {
        Say-Plan ('{0} already exists - would SKIP the copy and only re-apply the two settings edits (idempotent).' -f $dstAppData)
    } else {
        Say-Plan ('robocopy "{0}" "{1}" /E /XD "{2}"   (the vendor cache subtree is never copied)' -f $srcAppData, $dstAppData, $srcCache)
        Say-Plan ('junction {0} -> {1}' -f (Join-Path $dstAppData 'cache'), $srcCache)
        Say-Plan ('junction {0} -> {1}' -f (Join-Path $destFull 'data'),     (Join-Path $VrfRoot 'data'))
        Say-Plan ('junction {0} -> {1}' -f (Join-Path $destFull 'userData'), (Join-Path $VrfRoot 'userData'))
    }
    foreach ($t in $targets) {
        Say-Plan ('edit {0}: {1}' -f $t.Rel, $(if ($t.Kind -eq 'Application') { 'myShowQuitDialogOnClose -> 0 (UG52 4.6.1)' } else { 'mySessionOptions -band -bnot 0x10, i.e. clear DtShowSessionDialogs (UG52 4.3.1)' }))
    }
    Say-Plan 'write README-C2SIM-UNATTENDED.txt beside appData\ with the provenance and the restore path'
    Say-Plan ('then pass  -AppDataDir "{0}"  to scripts\LaunchVrf52.ps1 (it forwards --appDataDir to BOTH vrfSim and vrfGui)' -f $dstAppData)
    Say-Ok  'nothing under C:\MAK would be written on any path above.'
    exit 0
}

# ---- 1. seed ------------------------------------------------------------------
Say ''
Say '=== Seed ==='
if ($alreadyThere -and -not $Force) {
    Say-Ok ('{0} already exists - skipping the copy (pass -Force to re-seed from the vendor tree).' -f $dstAppData)
} else {
    if (-not (Test-Path -LiteralPath $destFull -PathType Container)) { $null = New-Item -ItemType Directory -Path $destFull -Force }
    # /E all subdirs including empty, /XD excludes the 1.4 M-file cache, /NFL /NDL /NJH
    # /NJS keep the console readable, /R:1 /W:1 so a locked file fails fast instead of
    # retrying a million times. robocopy's exit code is a BIT FIELD: < 8 is success
    # (0 = nothing to do, 1 = files copied, 2 = extras, 4 = mismatches), >= 8 is a real
    # failure. Treating a non-zero exit as failure here would fail every successful copy.
    $rcArgs = @($srcAppData, $dstAppData, '/E', '/XD', $srcCache, '/NFL', '/NDL', '/NJH', '/NJS', '/NP', '/R:1', '/W:1')
    Say-Info ('robocopy "{0}" "{1}" /E /XD "{2}" ...' -f $srcAppData, $dstAppData, $srcCache)
    & robocopy @rcArgs | Out-Null
    $rc = $LASTEXITCODE
    if ($rc -ge 8) {
        Say-Fail ('robocopy FAILED with exit {0} (>= 8 is a real failure; 0-7 is success). Nothing further was done.' -f $rc)
        exit 3
    }
    Say-Ok ('robocopy exit {0} (0-7 = success)' -f $rc)

    # Junctions. New-Item -ItemType Junction fails if the link path already exists, so
    # each one is created only when absent - that is what makes -Force re-seeding safe.
    $links = @(
        [pscustomobject]@{ Link = (Join-Path $dstAppData 'cache');    Target = $srcCache },
        [pscustomobject]@{ Link = (Join-Path $destFull  'data');      Target = (Join-Path $VrfRoot 'data') },
        [pscustomobject]@{ Link = (Join-Path $destFull  'userData');  Target = (Join-Path $VrfRoot 'userData') }
    )
    foreach ($l in $links) {
        if (Test-Path -LiteralPath $l.Link) { Say-Ok ('junction already present: {0}' -f $l.Link); continue }
        if (-not (Test-Path -LiteralPath $l.Target)) { Say-Warn ('junction target missing, skipped: {0}' -f $l.Target); continue }
        $null = New-Item -ItemType Junction -Path $l.Link -Target $l.Target
        Say-Ok ('junction {0} -> {1}' -f $l.Link, $l.Target)
    }
}
if (-not (Test-Path -LiteralPath $dstGui -PathType Container)) {
    Say-Fail ('after seeding there is still no {0} - the copy did not produce a settings tree.' -f $dstGui)
    exit 3
}

# ---- 2. the two edits ---------------------------------------------------------
Say ''
Say '=== Turn the two prompts off (UG52 4.6.1 and 4.3.1) ==='
$editFail = $false
foreach ($t in $targets) {
    $path = Join-Path $dstAppData $t.Rel
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        if ($t.Required) { Say-Fail ('MISSING and required: {0}' -f $path); $editFail = $true }
        else             { Say-Ok   ('not present (fine, the GUI rewrites it at startup): {0}' -f $t.Rel) }
        continue
    }
    # Byte-level read/write with an explicit no-BOM UTF-8 encoding. The vendor files are
    # CRLF, ASCII, no BOM, and the edit only rewrites digits INSIDE an existing line, so
    # the line endings and everything else survive byte-for-byte. Set-Content would add
    # a BOM or re-encode; Get-Content -Raw + Set-Content would also append a newline.
    $enc  = New-Object System.Text.UTF8Encoding($false)
    $text = $enc.GetString([System.IO.File]::ReadAllBytes($path))
    $res  = Set-VrfGuiPromptSettingsText -Kind $t.Kind -Text $text
    if (-not $res.KeyFound) {
        Say-Fail ('{0}: the key is NOT in this file - refusing to invent it (a settings file that does not carry the key is a file shape nobody has seen).' -f $t.Rel)
        if ($t.Required) { $editFail = $true }
        continue
    }
    if ($res.Changed) {
        [System.IO.File]::WriteAllBytes($path, $enc.GetBytes($res.Text))
        Say-Ok ('{0}: {1} -> {2}' -f $t.Rel, $res.Before, $res.After)
    } else {
        Say-Ok ('{0}: already {1} - unchanged' -f $t.Rel, $res.After)
    }
}
if ($editFail) { Say-Fail 'at least one required settings file could not be edited.'; exit 3 }

# ---- 3. read back, from disk, and say what the GUI will see -------------------
Say ''
Say '=== Verify (read back from disk, not from memory) ==='
$encR = New-Object System.Text.UTF8Encoding($false)
$appPath  = Join-Path $dstAppData 'settings\vrfGui\default_Application.apsx'
$sessPath = Join-Path $dstAppData 'settings\vrfGui\default_SessionSettings.srsx'
$appText  = $encR.GetString([System.IO.File]::ReadAllBytes($appPath))
$sessText = $encR.GetString([System.IO.File]::ReadAllBytes($sessPath))
$state = Get-VrfGuiPromptSettings -ApplicationXml $appText -SessionSettingsXml $sessText
Say ('         ' + $state.Summary)
if (-not $state.Unattended) {
    Say-Fail 'the read-back does NOT show both prompts off. Do not use this tree for an unattended run.'
    exit 3
}
Say-Ok 'both prompts are OFF in this tree: the exit prompt (UG52 4.6) and the session-dialog flag (UG52 4.3.1).'

# ---- 4. provenance beside the tree, never inside it ---------------------------
$readme = Join-Path $destFull 'README-C2SIM-UNATTENDED.txt'
$nowUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$lines = @(
    'C2SIM RUN-OWNED VR-FORCES 5.2 appData - UNATTENDED TEARDOWN (STP-844)',
    '=====================================================================',
    '',
    'WHAT THIS IS',
    'A copy of ' + $srcAppData + ' with exactly TWO vendor settings changed, so that a',
    'GUI launched against it exits on WM_CLOSE without waiting for a human:',
    '  settings\vrfGui\default_Application.apsx      myShowQuitDialogOnClose  -> 0',
    '      (UG52 4.6.1 "Disabling the Quit Prompt"; the "Are You Sure?" / "Quit VR-Forces',
    '       GUI" exit prompt of UG52 4.6)',
    '  settings\vrfGui\default_SessionSettings.srsx  mySessionOptions bit 0x10 cleared',
    '      (DtShowSessionDialogs, include\vrfGuiCore\vrfSessionSettingsRecord.h:35; the',
    '       Session Settings page option "Show Session Terrain Change Prompts", UG52',
    '       4.3.1 - the "Session Status" / "The current session has ended. Close current',
    '       terrain?" prompt)',
    'The two settings\vrfGui\backups\*.backup copies are set to match. Every other byte of',
    'the tree is the vendor copy.',
    '',
    'CREATED',
    $nowUtc + ' by scripts\NewVrfAppData52.ps1 (C2SIM VRF interface project).',
    '',
    'HOW IT IS USED',
    '  scripts\LaunchVrf52.ps1 -AppDataDir "' + $dstAppData + '" ...',
    'which forwards --appDataDir to BOTH vrfSimHLA1516e and vrfGui (UG52 Table 11 p178 /',
    'Table 10 p164). Nothing under C:\MAK is written by that path or by this script.',
    '',
    'RESTORE PATH',
    'Stop passing -AppDataDir, or delete this directory. The vendor tree was never',
    'modified, so there is nothing to put back.',
    '',
    'LAYOUT',
    '  appData\        <-- the --appDataDir value',
    '  appData\cache   JUNCTION -> ' + $srcCache,
    '  data            JUNCTION -> ' + (Join-Path $VrfRoot 'data'),
    '  userData        JUNCTION -> ' + (Join-Path $VrfRoot 'userData'),
    'The nested appData\ and the two sibling junctions exist because vendor settings files',
    'carry live "../data/..." and "../appData/..." references - see',
    'C:\C2SIM\vrf-appdata\README-C2SIM.txt for the full argument.',
    '',
    'NOT THE SAME TREE AS C:\C2SIM\vrf-appdata, whose one edit is',
    '(setqb loadAllNavigationDataOnTerrainLoad 1). This tree does NOT carry that edit.',
    ''
)
[System.IO.File]::WriteAllBytes($readme, (New-Object System.Text.UTF8Encoding($false)).GetBytes(($lines -join "`r`n")))
Say-Ok ('provenance written: {0}' -f $readme)

Say ''
Say '=== Result ==='
Say-Ok ('pass this to LaunchVrf52.ps1:   -AppDataDir "{0}"' -f $dstAppData)
Say-Ok 'nothing under C:\MAK was written.'
exit 0

}
catch {
    Say-Fail ('unexpected terminating error: {0}' -f $_.Exception.Message)
    Say-Fail ('at: {0}' -f $_.InvocationInfo.PositionMessage)
    Say-Fail 'the tree may be half-seeded - inspect it, or delete it and re-run.'
    exit 5
}
