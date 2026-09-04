# FixMachinePath.ps1 - clean the MACHINE PATH (needs an ELEVATED shell).
#
# WHY (2026-09-04): the machine PATH carries one CORRUPT entry, seven directories that do
# not exist, and five exact duplicates. None of it caused the 5.2 startup crash (that was
# --logFileName - PREREG_52_CRASH_BISECT_2026-09-04), but it is a live hazard for OUR C#
# interface exe: VrfC2SimApp.exe does NOT live in a MAK bin64, so when it is started
# WITHOUT the runner's per-process PATH prefix the machine PATH decides which MAK DLLs it
# binds - and the machine PATH currently offers a 2022 VR-Forces 5.0.2 / VR-Link 5.8 stack.
# The USER PATH was already deduplicated on 2026-09-04 (backup in runs\env-backup\).
#
# SAFETY: -DryRun is the DEFAULT. -Apply is required to write. The script backs the PATH up
# first, verifies the backup reads back byte-identical, removes ONLY entries on the explicit
# lists below, refuses if any entry that is not on a list would disappear, and re-reads the
# value afterwards. The registry type (REG_EXPAND_SZ vs REG_SZ) is preserved by writing
# through the registry, NOT through SetEnvironmentVariable, which would silently downgrade
# REG_EXPAND_SZ and break any %VAR% entry.
#
# THE MAK ENTRIES ARE NOT TOUCHED BY DEFAULT, deliberately. scripts\LaunchVrf.ps1 (the
# 5.0.2 golden path) sets NO per-process PATH - it INHERITS these entries, so removing them
# would break 5.0.2, which is a standing non-negotiable. See -MoveRti501Last below and
# docs\experiments\RESEARCH_502_SIDE_BY_SIDE_2026-09-04.md for the staged fix.
param(
    [switch] $Apply,
    [string] $BackupDir = (Join-Path (Split-Path -Parent $PSScriptRoot) 'runs\env-backup')
)
$ErrorActionPreference = 'Stop'
function Say      { param([string]$m) Write-Host $m }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }

# Entries removed because the path is MALFORMED (a stray % makes it unusable and it is not
# a valid REG_EXPAND_SZ reference either).
$RemoveCorrupt = @('C:\Python312%')
# Entries removed because the DIRECTORY DOES NOT EXIST. Each is re-checked at run time and
# skipped if it has since appeared. The Pitch RTI ones are additionally redundant: the C2SIM
# interface .bat files prepend their own Pitch prefix (runc2simVRFHLApRTI.bat), and three of
# them name .jar FILES, which can never be valid PATH directories.
$RemoveMissing = @(
    'C:\Program Files\prti1516e\lib\prti1516e.jar',
    'C:\Program Files\prti1516e\lib\prti1516.jar',
    'C:\Program Files\prti1516e\lib\prti.jar',
    'C:\Program Files\prti1516e\lib\vc141_64',
    'C:\Program Files\prti1516e\lib',
    'C:\Program Files\prti1516e\jre\bin\server',
    'C:\Program Files (x86)\Microsoft SQL Server\160\DTS\Binn\'
)
# CORRECTION recorded 2026-09-04 while dry-running this script: the machine PATH's FIRST
# entry is the literal, unexpanded '%MAK_RTIDIR%\bin' (the value is REG_EXPAND_SZ). An
# earlier report that "the machine PATH now begins with C:\MAK\makRti5.0.1\bin" was reading
# the EXPANDED value - [Environment]::GetEnvironmentVariable(...,'Machine') expands, the
# registry does not. So the PATH entry is VERSION-AGNOSTIC and needs no edit: it follows
# whatever MAK_RTIDIR says. The 5.0.2 drift is entirely in the MAK_RTIDIR VARIABLE (the RTI
# 5.0.1 installer set it machine-wide), and the fix for it is a per-process MAK_RTIDIR, which
# scripts\LaunchVrf52.ps1 already does and scripts\LaunchVrf.ps1 still does not. Reordering
# PATH would fix nothing, so this script has no option to do it.

Say ''
Say ('=== FixMachinePath.ps1 (' + $(if ($Apply) { 'APPLY' } else { 'DRY-RUN' }) + ') ===')

$id = [Security.Principal.WindowsIdentity]::GetCurrent()
$elevated = (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Say ("  elevated: {0}   user: {1}" -f $elevated, $id.Name)
if ($Apply -and -not $elevated) {
    Say-Fail 'The MACHINE PATH needs an ELEVATED shell. Re-run this script as Administrator.'
    exit 2
}

$key = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
$item = Get-Item -LiteralPath $key
$kind = $item.GetValueKind('Path')          # REG_EXPAND_SZ on a stock Windows
$old  = $item.GetValue('Path', $null, 'DoNotExpandEnvironmentNames')
Say ("  registry value kind: {0} (preserved on write)" -f $kind)
$entries = $old -split ';'
Say ("  current: {0} chars, {1} entries" -f $old.Length, ($entries | Where-Object { $_ }).Count)

# ---- build the new list: keep order, first occurrence wins ----
$seen = New-Object System.Collections.Generic.HashSet[string]
$keep = @(); $dropped = @()
foreach ($e in $entries) {
    if ([string]::IsNullOrWhiteSpace($e)) { $dropped += '(empty)'; continue }
    $norm = $e.TrimEnd('\').ToLower()
    if (-not $seen.Add($norm)) { $dropped += ('(duplicate) ' + $e); continue }
    if ($RemoveCorrupt -contains $e) { $dropped += ('(corrupt)   ' + $e); continue }
    if ($RemoveMissing -contains $e) {
        if (Test-Path -LiteralPath $e -ErrorAction SilentlyContinue) {
            Say-Warn ("kept - the directory EXISTS now: " + $e)
        } else { $dropped += ('(missing)   ' + $e); continue }
    }
    $keep += $e
}
$new = $keep -join ';'

Say ''
Say '  would remove:'
if ($dropped.Count -eq 0) { Say '    (nothing - the PATH is already clean)' } else { $dropped | ForEach-Object { Say ('    - ' + $_) } }
Say ("  result: {0} chars, {1} entries (was {2})" -f $new.Length, $keep.Count, ($entries | Where-Object { $_ }).Count)

# ---- refuse if anything NOT on a list would vanish ----
$allowed = @($RemoveCorrupt + $RemoveMissing) | ForEach-Object { $_.TrimEnd('\').ToLower() }
$lost = @()
foreach ($e in $entries) {
    if ([string]::IsNullOrWhiteSpace($e)) { continue }
    $n = $e.TrimEnd('\').ToLower()
    if ($keep.TrimEnd('\').ToLower() -contains $n) { continue }
    if ($allowed -contains $n) { continue }
    $lost += $e                                  # a duplicate is fine: its twin is kept
}
$lost = @($lost | Where-Object { ($entries | Where-Object { $_.TrimEnd('\').ToLower() -eq $_.TrimEnd('\').ToLower() }).Count -le 1 })
if ($lost.Count -gt 0) { Say-Fail 'REFUSING: these entries are on no removal list and would disappear:'; $lost | ForEach-Object { Say ('    ! ' + $_) }; exit 1 }
Say-Ok 'no entry outside the removal lists is affected'

# ---- MAK entries: report, never touch (unless -MoveRti501Last) ----
Say ''
Say '  MAK entries (left in place - scripts\LaunchVrf.ps1 INHERITS them for the 5.0.2 path):'
$keep | Where-Object { $_ -match 'MAK' } | ForEach-Object { Say ('    . ' + $_) }

if (-not $Apply) {
    Say ''
    Say '  [DRY-RUN] nothing was written. Re-run in an ELEVATED shell with -Apply to write.'
    Say '  [DRY-RUN] a backup is taken at write time, verified, and its path printed.'
    exit 0
}

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$bak = Join-Path $BackupDir ("MachinePATH_before_$stamp.txt")
[IO.File]::WriteAllText($bak, $old)
if ([IO.File]::ReadAllText($bak) -cne $old) { Say-Fail 'backup did not verify - NOTHING written'; exit 1 }
Say-Ok ("backup verified: " + $bak)

Set-ItemProperty -LiteralPath $key -Name 'Path' -Value $new -Type $kind
$after = (Get-Item -LiteralPath $key).GetValue('Path', $null, 'DoNotExpandEnvironmentNames')
if ($after -cne $new) { Say-Fail 'readback MISMATCH - restore from the backup above'; exit 1 }
Say-Ok 'machine PATH written and verified'
Say ''
Say '  NOTE: already-running processes keep the old PATH. Sign out / in, or restart the'
Say '  shells and services that need it. To restore:'
Say ('    Set-ItemProperty -LiteralPath "' + $key + '" -Name Path -Value (Get-Content -Raw "' + $bak + '") -Type ' + $kind)
exit 0
