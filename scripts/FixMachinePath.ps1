# FixMachinePath.ps1 - clean the MACHINE PATH (needs an ELEVATED shell).
#
# WHY (2026-09-04): the machine PATH carries one CORRUPT entry and seven directories that
# do not exist. None of it caused the 5.2 startup crash (that was
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
# THE MAK ENTRIES ARE NOT TOUCHED BY DEFAULT. They were kept because scripts\LaunchVrf.ps1
# (the 5.0.2 path) sets NO per-process PATH and INHERITS them. Now that 5.0.2 is archive,
# -RemoveLegacyMak drops the two 2022-era ones; %MAK_RTIDIR%\bin always stays.
param(
    [switch] $Apply,
    # Remove C:\MAK\vrforces5.0.2\bin64 and C:\MAK\vrlink5.8\bin64. OPT-IN, but RECOMMENDED
    # once 5.0.2 is archive (user, 2026-09-04): they are the only reason an un-prefixed
    # process can silently bind 2022-era MAK DLLs, and our own C# exe (VrfC2SimApp) does NOT
    # live in a MAK bin dir, so it is exactly the process at risk. Any future 5.0.2 run
    # supplies its own per-process PATH anyway (RESEARCH_502_SIDE_BY_SIDE_2026-09-04), which
    # is the correct pattern regardless. %MAK_RTIDIR%\bin is NEVER removed - it is
    # version-agnostic and MAK's own tools (rtiAssistant lives in <rti>\bin\gui and resolves
    # its siblings through PATH) can depend on it.
    [switch] $RemoveLegacyMak,
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
# Entries removed because the DIRECTORY DOES NOT EXIST. Each is RE-CHECKED at run time and
# KEPT if it has since appeared, so this list can never delete a live directory.
# ARE THE PITCH RTI ENTRIES NEEDED? No - verified 2026-09-04: (1) C:\Program Files\prti1516e
# does not exist AT ALL on this machine, so all seven point into a missing tree; (2) three of
# them name .jar FILES, which can never be valid PATH entries under any circumstance (a jar
# belongs on CLASSPATH); (3) NOTHING in the 5.2 stack references Pitch - it was the 5.0.2 C++
# interface's HLA route, and 5.2 uses the MAK RTI; (4) the one thing that does use Pitch,
# runc2simVRFHLApRTI.bat, PREPENDS its own Pitch prefix at run time (line 11), so even after a
# Pitch install these machine entries are redundant. Removing them changes nothing today, and
# a Pitch installer would re-add its own.
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
# Removed ONLY with -RemoveLegacyMak (see the parameter comment above).
$RemoveLegacy = @('C:\MAK\vrforces5.0.2\bin64', 'C:\MAK\vrlink5.8\bin64')

Say ''
Say ('=== FixMachinePath.ps1 (' + $(if ($Apply) { 'APPLY' } else { 'DRY-RUN' }) + ') ===')

# BITNESS GATE (added 2026-09-04, AFTER the applied run; deliberately the FIRST check,
# ahead of elevation, because bitness invalidates the ANALYSIS itself, not just the write).
# This script decides whether to keep an entry with Test-Path. In a 32-BIT process on 64-bit
# Windows, WOW64 FILE REDIRECTION sends C:\Windows\System32\* to SysWOW64, so a System32
# directory that really exists can test FALSE and be classified "(missing)". Observed live:
# C:\Windows\System32\OpenSSH tests False from 32-bit pwsh and True from 64-bit - ssh.exe is
# genuinely there. NOTHING on today's removal lists lives under System32, so the run applied
# on 2026-09-04 was NOT affected (all 7 re-confirmed MISSING from a true 64-bit process via
# C:\Windows\SysNative\cmd.exe) - but the next entry someone adds might be. Note that
# C:\Program Files is NOT file-redirected; only the ProgramFiles ENV VAR differs. So the
# Pitch verdict never depended on bitness, and it was re-confirmed 64-bit regardless.
if ((-not [Environment]::Is64BitProcess) -and [Environment]::Is64BitOperatingSystem) {
    Say-Warn '32-BIT PowerShell on 64-bit Windows: WOW64 redirects System32, so the'
    Say-Warn 'directory-exists checks can misclassify a System32 path as missing.'
    Say-Warn ('this process: ' + (Get-Process -Id $PID).Path)
    Say-Warn 'use 64-bit PowerShell: C:\Program Files\PowerShell\7\pwsh.exe'
    if ($Apply) { Say-Fail 'REFUSING to -Apply from a 32-bit process.'; exit 2 }
    Say-Warn 'continuing DRY-RUN only - treat any "(missing)" line as UNVERIFIED.'
    Say ''
}

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
# WHY THIS REPORTS NO DUPLICATES, though an earlier note claimed five (CORRECTED 2026-09-04):
# the five pairs are %SystemRoot%\system32 etc. against literal C:\Windows\system32 etc.
# They are identical only AFTER EXPANSION. This script compares the RAW registry strings -
# which is the correct comparison, because raw is what it writes back - so it sees them as
# distinct and keeps both. That is deliberate: collapsing them means choosing one form and
# throwing away either the %SystemRoot% indirection or the literal, and five redundant
# directory probes per name lookup is a non-issue. Do not "fix" this by expanding first;
# expanding would bake C:\Windows into a REG_EXPAND_SZ value on write.
$seen = New-Object System.Collections.Generic.HashSet[string]
$keep = @(); $dropped = @()
foreach ($e in $entries) {
    if ([string]::IsNullOrWhiteSpace($e)) { $dropped += '(empty)'; continue }
    $norm = $e.TrimEnd('\').ToLower()
    if (-not $seen.Add($norm)) { $dropped += ('(duplicate) ' + $e); continue }
    if ($RemoveCorrupt -contains $e) { $dropped += ('(corrupt)   ' + $e); continue }
    if ($RemoveLegacyMak -and ($RemoveLegacy -contains $e)) { $dropped += ('(legacy MAK) ' + $e); continue }
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
$allowList = @($RemoveCorrupt + $RemoveMissing)
if ($RemoveLegacyMak) { $allowList += $RemoveLegacy }
$allowed = $allowList | ForEach-Object { $_.TrimEnd('\').ToLower() }
$lost = @()
foreach ($e in $entries) {
    if ([string]::IsNullOrWhiteSpace($e)) { continue }
    $n = $e.TrimEnd('\').ToLower()
    if ($keep.TrimEnd('\').ToLower() -contains $n) { continue }
    if ($allowed -contains $n) { continue }
    $lost += $e
}
# NOTE (2026-09-04): a line used to stand here trying to re-exclude duplicates from $lost.
# It was BOTH dead and BROKEN - inside its inner Where-Object, $_ rebound to the entries
# item, so the test read "x -eq x", was always true, the count was always the full entry
# count, and EVERY candidate was filtered out. That made $lost unconditionally empty and
# this refusal unreachable: the [OK] below was a FALSE GREEN and the script's whole safety
# story rested on it. It was also unnecessary - a duplicate's normalised form is already in
# $keep via its surviving twin, so the -contains test above skips it. Deleted, and a
# negative control (SelfTest-FixMachinePath.ps1) now proves this refusal actually fires.
if ($lost.Count -gt 0) { Say-Fail 'REFUSING: these entries are on no removal list and would disappear:'; $lost | ForEach-Object { Say ('    ! ' + $_) }; exit 1 }
Say-Ok 'no entry outside the removal lists is affected'

# ---- MAK entries: report what survives ----
Say ''
Say '  MAK entries KEPT:'
$keep | Where-Object { $_ -match 'MAK' } | ForEach-Object { Say ('    . ' + $_) }
# Only advise -RemoveLegacyMak if the legacy entries are ACTUALLY still present. Without this
# test the advice kept printing after they had been removed, telling the reader they "are
# still here" when they were not - a small false statement, but this file's whole job is to
# be trusted about what is on the PATH.
$legacyStillThere = @($keep | Where-Object { $RemoveLegacy -contains $_ })
if ((-not $RemoveLegacyMak) -and $legacyStillThere.Count -gt 0) {
    Say '    (the 2022-era vrforces5.0.2 / vrlink5.8 entries are still here. 5.0.2 is ARCHIVE,'
    Say '     so -RemoveLegacyMak drops them - RECOMMENDED: they are how an un-prefixed process,'
    Say '     e.g. our own VrfC2SimApp.exe, can silently bind 2022 MAK DLLs.)'
}

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
