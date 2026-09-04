# LaunchVrf52.ps1 - bring up VR-Forces 5.2d in INDEPENDENT mode (direct bin64 launch).
#
# WHY A SEPARATE SCRIPT (2026-09-03): every LaunchVrf.ps1 invocation is INVALID on 5.2d.
# The 5.0.2 vrfLauncher option set (--usePredefinedConnection, --connectionProfile ...)
# is gone; 5.2 combined mode exists only as `vrfLauncher --connection "<name>" --run`
# and REQUIRES a connection saved from the Launcher GUI at least once (UG52 5.3.1).
# The documented alternative that needs no GUI-saved state is the independent launch
# (UG52 4.1.2 "Starting Independent VR-Forces Executables", p.133; 4.1.3 session ID;
# 4.1.4 HLA Configuration example, p.135):
#     vrfGui --siteId 1 --appNumber 3000 --hla1516e
#     vrfSimHLA1516e --siteId 1 --appNumber 3002
# Both default to ./appData/settings/connections/MAK-ONE-YYYY-Config.xml
# (--exConnConfigFile, Table 11/12, p.167/181) and to session 1 (4.1.3).
# vrfSim options used (UG52 Table 11, p.181): -L|--scenarioFileName (relative to
# ./bin), -n|--notifyLevel 0-4 (default 2), -q|--doNotUseConsole. --logFileName is
# NOT among them any more - see the VENDOR LOG block below.
#
# VENDOR LOG: --logFileName IS NOT PASSED; THE VENDOR'S OWN LOG IS HARVESTED
# (2026-09-04, docs/experiments/PREREG_52_CRASH_BISECT_2026-09-04.md sec 5). Passing
# --logFileName crashed the sim at startup in 6 of 18 launches (33%); omitting it crashed
# 0 of 12 (Fisher's exact, one-sided, p = 0.031). A 22-character path inside the vendor's
# own C:\MAK\logs crashed too, so it is the OPTION, not the path length or location. The
# sim ALWAYS writes its own log to C:\MAK\logs\vrfSimHLA1516e5.2d-<date>-<time>-<host>-
# <build>-<pid>.log regardless, so nothing is lost: at READY (and on the crash path) this
# script COPIES that file for THIS pid to -LogFile. The copy is a SNAPSHOT taken at that
# moment - the vendor keeps writing - and a later snapshot is just another run of the
# harvest. -LogFileName <path> still exists to exercise the option deliberately (a
# repeat of the bisect, a vendor bug report); DO NOT re-enable it casually - it is a
# 1-in-3 startup crash.
# *** SECRETS: the harvested vendor log contains the FULL PROCESS ENVIRONMENT IN
# CLEARTEXT (DtPrintEnvironmentVariables at --notifyLevel 3; FORENSICS_52_STARTUP_CRASH_
# 2026-09-04 sec 10) - never attach it to a ticket, mail or issue; send the
# .callstack.log / .dmp instead. It is NOT scrubbed here on purpose: a scrubber that
# silently misses one variable is worse than a warning that is always true. ***
#
# ENVIRONMENT (the two 5.2 runtime traps, DIFF sec H): MAK DLLs bind BY NAME on
# PATH and the Machine PATH lists vrforces5.0.2 / vrlink5.8 first, so this script
# PREFIXES the process PATH with the 5.2d stack. The RTI 5.0.1 installer also set
# MAK_RTIDIR / RTI_RID_FILE to makRti5.0.1 at Machine scope while the Machine PATH
# still lists makRti4.6.1\bin, so BOTH are set per process (to 5.0.1, the RTI this
# profile is defined on) rather than trusted. Nothing is written to Machine/User
# scope; the 5.0.2 script and its environment are untouched.
#
# NO RTI ASSISTANT (2026-09-03, PREREG_52_LAUNCH result): the 5.0.1 installer left
# an ELEVATED 5.0.1 rtiAssistant on port 6003 that version-rejects every 4.6.1
# federate ("RTI component was using a different RTI version than the RTI
# Assistant" - the assistant's own toast). Fix, both documented: set
# RTI_ASSISTANT_DISABLE (existence alone disables assistant use, MAK RTI 4.6.1
# Reference Manual 5.2.10) and take the connection from the rid instead
# (RTI_configureConnectionWithRid 1). Every federate that must interoperate with
# this sim (RtiProbe/WatchVrf/CreateOne/app) needs the SAME env: assistant
# disabled + the SAME rid file, or it will not share a connection.
#
# ...BUT THE RID MUST BE THE RTIEXEC POSTURE, NOT A LIGHTWEIGHT ONE (2026-09-04,
# PREREG_52_RTIEXEC result - THIS SUPERSEDES THE PARAGRAPH ABOVE'S OLD DEFAULT).
# The first assistant-free rid, config/rid-461-ridconfigured.mtl, was a WRONG FIX:
# it bypassed the version-locked assistant but put the federation in LIGHTWEIGHT
# mode, and UG52 5.5.1 p190 says flatly "You cannot use the MAK RTI in lightweight
# mode with VR-Forces" (the 5.0.2 qualifier "if you are running multiple ...
# federation executions" was dropped in 5.2). Under it every observer reflected 0
# entities. The defaults here are now MAK RTI 5.0.1 with
# config/rid-501-rtiexec-min.mtl (rtiexec at 127.0.0.1:4001, loopback broadcast
# 127.255.255.255 on interface 127.0.0.1, forwarder 5000, internal messages
# reliable - the twelve parameters RTI UG 5.0.1 sec 7.3 p73 lists) and an rtiexec
# must ALREADY BE RUNNING for that rid: scripts/StartRtiExec52.ps1 ensures one,
# and the runner does it in Stage 2r. Under that posture the observer reflected 62
# entities. Assistant-free STAYS; it is orthogonal to lightweight-vs-rtiexec.
# NOT part of the repair: -DeviceAddress. The repairing run set it too, but the
# discriminator (run 3857) then reflected 54-56 entities with the observer's device
# address blank, so it is a tunable that defaults to OFF - see the parameter.
#
# READINESS is the LaunchVrf.ps1 oracle, unchanged: back-end thread count above
# -BackendMinThreads (a blocked back-end sits at 2-4 threads while present; healthy
# 5.0.2 reached 23-67, and the 5.2d BASELINE is now observed at 34-62 - 36 at the
# first healthy launch, PREREG_52_LAUNCH_2026-09-03 attempt 2 - so the floor of 8
# separates the two states with room to spare on both stacks) and, unless
# -NoGui, a vrfGui with a NON-EMPTY MainWindowTitle (empty = modal dialog; on 5.2
# the documented candidate is the Scenario Startup dialog, UG52 4.1.1 Figure 17).
# Federation JOIN is NOT tested here - confirm with the 5.2 RtiProbe/WatchVrf build.
#
# STARTUP CRASH DETECTION (2026-09-04, cold-start review of PREREG_52_RTIEXEC). The 5.2
# sim sometimes dies at startup with 0xC0000005 inside
# makVrf::DtVrfSimOptions::parseCmdLine - 2 of 5 launches on 2026-09-04, under BOTH the
# lightweight and the rtiexec rid, so it is NOT a rid property. THE TRIGGER IS NOW KNOWN and
# is removed by default: --logFileName (PREREG_52_CRASH_BISECT_2026-09-04 sec 5; VENDOR LOG
# block above). Detection STAYS - it is the guard for the residual and for -LogFileName runs.
# It must never be mistaken for "not ready yet": the readiness poll therefore watches for
# three signatures and fails the launch the moment any appears -
#   (a) the back-end process is GONE;
#   (b) its main window title is MAK's crash box ("Error vrfSimHLA1516e.exe", or the 5.0.2
#       'vrfSim*.dmp' form AnswerCrashDumpDialog.ps1 answers);
#   (c) a NEW <MakLogDir>\vrfSimHLA1516e*-<pid>.callstack.log exists (the handler writes it
#       with the faulting pid as the last field, verified against the 38180 / 39028 files).
# The first frames of the callstack go into this script's own output so the runner log
# carries the evidence, and the exit is 3. NO RETRY: whether a crashed launch should be
# retried is a separate decision, and a silent retry would hide how often this fires.
#
# THE CRASHED PROCESS LINGERS, AND IT BLOCKED THE NEXT LAUNCH (observed 2026-09-04 11:28-11:30
# UTC; the two console captures named in that report, runs\launch52\launch_3860_appsmoke.txt and
# launch_3862_appsmoke_retry.txt, were NOT persisted, but the crash itself is on disk:
# C:\MAK\logs\vrfSimHLA1516e5.2d-20260904-072806-Legatus-282607-59936.callstack.log and its
# .dmp, stamped 07:28:06 LOCAL = 11:28 UTC). Detection worked - "CRASHED AT STARTUP ... exit 3"
# for that pid 59936 - but MAK's crash handler KEEPS THE
# FAULTED PROCESS ALIVE (title becomes 'Error vrfSimHLA1516e.exe', 0 threads), so the very next
# launch was refused by the pre-existing-process precondition (exit 2) and an unattended runner
# could not even retry. Two narrow, asymmetric remedies, both bounded by the project rule that
# a process which FAILED ITS OWN start/join may be closed without asking while a healthy one
# may not:
#   - OUR OWN pid, this launch: when the poll declares CRASHED AT STARTUP for the pid THIS
#     script started, it closes that pid before exiting 3 - first scripts\AnswerCrashDumpDialog
#     .ps1 (it answers the 5.0.2-form '<exe>...dmp' prompt; the 5.2 box titled 'Error <exe>' is
#     NOT one it matches, so it simply reports no dialog), then Stop-Process on that single pid.
#     -LeaveCrashedProcess opts out when the live process is wanted for forensics.
#   - A PRE-EXISTING pid, some earlier launch's: NOT closed by default - this script cannot know
#     it failed its own start, only that it looks dead. -CloseCrashedLeftover closes it, and
#     ONLY when BOTH hold: MAK wrote a <pid>.callstack.log for it AND it has <= 4 threads. Both
#     are required because either alone is ambiguous - a callstack file can be a recycled pid's
#     (Windows reuses pids and C:\MAK\logs keeps files across boots), and a low thread count
#     alone is also the signature of a back-end merely BLOCKED on the RTI (2-4 threads while
#     present). Together they cannot describe a healthy sim, which runs at 34-62 threads here.
# NOTHING ELSE IS EVER CLOSED: not vrfGui, not another vrfSim, and never rtiexec / rtiForwarder
# / rtiAssistant. If a front-end this script started is still up after a crash it is reported,
# not killed - it will block the next launch until a human deals with it.
# On the CAUSE of the crash itself see docs/experiments/FORENSICS_52_STARTUP_CRASH_2026-09-04.md
# (a vendor-side fault in vl.dll, with rid / launcher / cwd / timing falsified as triggers),
# RE-SCOPED by PREREG_52_CRASH_BISECT_2026-09-04 sec 5: the fault lives in the --logFileName
# path, which we simply stop taking. It does not change this script's job: detect it, print the
# evidence, exit 3, and do not leave the corpse blocking the next launch.
#
# Exit codes (same contract as LaunchVrf.ps1): 0 READY; 1 PARTIAL (back-end healthy,
# no front-end); 2 precondition/usage failure (nothing launched); 3 NOT READY within
# timeout, or the back-end CRASHED at startup; 4 BLOCKED (front-end process up, no
# window title).
# Non-negotiables: NEVER kill rtiAssistant / rtiexec / rtiForwarder; fresh ledgered
# app numbers per join (OPUS_EXECUTION_PLAN.md App. B, NEXT FREE marker). ASCII-only.
param(
    [string] $VrfRoot            = 'C:\MAK\vrforces5.2d',
    [string] $VrLinkRoot         = 'C:\MAK\vrlink5.10',
    # MAK RTI 5.0.1 - the 5.2 profile's RTI, in RTIEXEC mode (see the header). 4.6.1 is the
    # 5.0.2 stack's RTI and must not appear here: the two version-reject each other.
    [string] $RtiDir             = 'C:\MAK\makRti5.0.1',
    # Scenario name under $VrfRoot\userData\scenarios (subdirs allowed, e.g.
    # 'Sample\Raid'); EMPTY = no -L (sim engine starts with no scenario loaded).
    [string] $Scenario           = '',
    # MANDATORY, no defaults - the never-reuse rule (RUNBOOK sec 0).
    [int]    $BackendAppNumber   = 0,
    [int]    $FrontendAppNumber  = 0,
    [int]    $SiteId             = 1,
    [int]    $SessionId          = 1,
    [switch] $NoGui,
    [int]    $NotifyLevel        = 3,
    # Back-end log - now the HARVEST DESTINATION, not a --logFileName argument (header,
    # VENDOR LOG). EMPTY = <repo>\runs\launch52\vrfSim_<appNo>_<UTCstamp>.log (never
    # C:\MAK\logs, the 5.2 default, and never under C:\MAK at all). The vendor's own log
    # for this pid is COPIED here at READY / on a startup crash. SECRETS: that copy holds
    # the whole process environment in cleartext - never attach it anywhere.
    [string] $LogFile            = '',
    # DELIBERATE re-enable of --logFileName, and NOTHING ELSE uses it. EMPTY (the default)
    # = the option is NOT passed, because passing it crashes the sim at startup ~1 launch
    # in 3: 6 crashes / 18 launches with it, 0 / 12 without, p = 0.031
    # (docs/experiments/PREREG_52_CRASH_BISECT_2026-09-04.md sec 5; a short vendor-default
    # path crashed too, so it is the option, not the path). Pass a path here only to
    # reproduce that bisect or to give MAK a repro - never for routine logging: the
    # vendor's own log is harvested to -LogFile instead.
    [string] $LogFileName        = '',
    [string] $ExConnConfigFile   = '',
    # rid with RTI_configureConnectionWithRid 1; EMPTY = the repo-owned
    # config\rid-501-rtiexec-min.mtl (the RTIEXEC posture - see the header; an rtiexec must
    # already be up for it, scripts\StartRtiExec52.ps1). Assistant use is DISABLED unless
    # -UseRtiAssistant, which reverts to $RtiDir\rid.mtl + assistant flow.
    [string] $RidFile            = '',
    [switch] $UseRtiAssistant,
    [int]    $ReadyTimeoutSec    = 120,
    [int]    $PollIntervalSec    = 3,
    [int]    $BackendMinThreads  = 8,
    [switch] $IgnoreUnansweredRtiAssistant,
    [switch] $AllowExistingVrf,
    [switch] $QuietBackend,
    # The 5.0.2 Launcher's "Network Interface Address" (its profile <hostAddress>): the card
    # for UDP/best-effort traffic. UG52 Table 10 p177 / Table 11 p180-181: --deviceAddress and
    # --hostAddressString|-H on BOTH vrfGui and vrfSim. EMPTY = not passed (VR-Forces picks
    # "the first device listed", IOG 5.2.1 p81). RESEARCH_52_HLA_CONNECTION_CONFIG P3.
    # NOT part of the observation-channel repair: run 3857 (PREREG_52_RTIEXEC sec 4 P4) had an
    # observer with a BLANK device address reflect 54-56 entities off the rtiexec sim. The
    # runner therefore passes nothing by default and the sim launches unpinned - which is also
    # the open sim-side arm of that same test. 127.0.0.1 is the 5.0.2 configuration's value,
    # available here to pin deliberately, never because a 5.2 experiment demanded it.
    [string] $DeviceAddress      = '',
    # Where MAK's crash handler writes <exe><ver>-<date>-<time>-<host>-<build>-<pid>.callstack
    # .log (and the matching .dmp) - see the STARTUP CRASH block in the header.
    [string] $MakLogDir          = 'C:\MAK\logs',
    # Do NOT close the back-end THIS launch started when it crashes at startup (default is to
    # close it, so an unattended retry is not blocked by the corpse). For forensics: the live
    # process, its crash box and its handles stay put - and WILL refuse the next launch.
    [switch] $LeaveCrashedProcess,
    # Close a PRE-EXISTING vrfSimHLA1516e that this script did not start, and only one that
    # BOTH has a <pid>.callstack.log in -MakLogDir AND runs <= 4 threads (header: either
    # condition alone is ambiguous). Off by default: a process this script did not start is
    # not one it can prove failed its own startup.
    [switch] $CloseCrashedLeftover,
    [switch] $DryRun
)
$ErrorActionPreference = 'Stop'

function Say      { param([string]$m) Write-Host $m }
function Say-Head { param([string]$m) Write-Host ''; Write-Host ('=== ' + $m + ' ===') }
function Say-Ok   { param([string]$m) Write-Host ('  [OK]   ' + $m) }
function Say-Warn { param([string]$m) Write-Host ('  [WARN] ' + $m) }
function Say-Fail { param([string]$m) Write-Host ('  [FAIL] ' + $m) }
function Say-Plan { param([string]$m) Write-Host ('  [DRY-RUN] would ' + $m) }

# ---- crashed-process helpers (used by the preconditions AND by the readiness verdict) ------
# Newest MAK callstack file whose LAST NAME FIELD is $ProcessId, written at or after $Since,
# or '' when there is none. Two filters, both load-bearing:
#   $Since  - C:\MAK\logs keeps callstacks across boots and Windows RECYCLES pids, so a file
#             older than the process being judged says nothing about it.
#   $NamePrefix - the directory is shared by the whole MAK toolchain, not just the sim: it
#             holds e.g. rtiAssistant5.0.1-20260903-194550-Legatus-281993-54616.callstack.log
#             (observed 2026-09-04). A pid-only match could therefore pin ANOTHER exe's crash
#             on this back-end, so the exe family is part of the match.
# Read-only, never throws.
function Get-CallstackFileForPid {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since = [datetime]::MinValue,
          [string]$NamePrefix = 'vrfSim')
    try {
        $cs = @(Get-ChildItem -LiteralPath $LogDir -Filter ($NamePrefix + ('*-{0}.callstack.log' -f $ProcessId)) -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $Since } |
                Sort-Object LastWriteTime -Descending)
        if ($cs.Count -gt 0) { return $cs[0].FullName }
    } catch { }
    return ''
}

# TRUE only for a process that is a CRASHED LEFTOVER: MAK wrote a callstack for its pid AND it
# is down to $MaxThreads or fewer threads. BOTH conditions, deliberately (header): a callstack
# alone can belong to a recycled pid, and a low thread count alone is also what a back-end
# merely BLOCKED on the RTI looks like (2-4 threads while present). A healthy 5.2d sim runs at
# 34-62 threads, so it can never satisfy the thread half. This is the ONLY predicate that may
# authorise closing a process this script did not start.
function Test-CrashedLeftover {
    param([int]$ProcessId, [int]$ThreadCount, [string]$LogDir,
          [datetime]$Since = [datetime]::MinValue, [int]$MaxThreads = 4)
    if ($ThreadCount -gt $MaxThreads) { return $false }
    return ((Get-CallstackFileForPid -ProcessId $ProcessId -LogDir $LogDir -Since $Since) -ne '')
}

# Close ONE named-checked back-end pid: the vendor's crash prompt first, Stop-Process only if
# the process survives it. The name re-check is the pid-recycling guard - between the verdict
# and this call the pid could belong to something else entirely, and this function must never
# be able to stop anything but a vrfSimHLA1516e. It takes a pid, never a name, so it CANNOT
# reach rtiexec / rtiForwarder / rtiAssistant even by mistake.
function Close-CrashedBackend {
    param([int]$ProcessId, [string]$ExpectedName = 'vrfSimHLA1516e',
          [int]$DialogConfirmSec = 20, [int]$ExitWaitSec = 15)
    $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $p) { Say-Ok ('  pid {0} is already gone - nothing to close.' -f $ProcessId); return }
    if ($p.Name -ne $ExpectedName) {
        Say-Warn ("  pid {0} is now '{1}', not '{2}' - the pid was recycled. NOT touched." -f $ProcessId, $p.Name, $ExpectedName)
        return
    }
    $answer = Join-Path $PSScriptRoot 'AnswerCrashDumpDialog.ps1'
    if (Test-Path -LiteralPath $answer) {
        Say ('  answering MAK''s crash-dump prompt first: AnswerCrashDumpDialog.ps1 -TargetPid {0} -ConfirmSec {1}' -f $ProcessId, $DialogConfirmSec)
        try {
            # 6>&1 as well as 2>&1: AnswerCrashDumpDialog.ps1 reports with Write-Host, whose
            # information stream a bare pipeline does not carry - without it its lines land
            # unprefixed in the middle of this script's output.
            & $answer -TargetPid $ProcessId -ConfirmSec $DialogConfirmSec 6>&1 2>&1 | ForEach-Object { Say ('         | ' + $_) }
            Say ('  AnswerCrashDumpDialog.ps1 exit {0} (1 = no prompt of the 5.0.2 form it matches; the 5.2 box is titled "Error <exe>" and is NOT answered by it - Stop-Process below is then the only exit)' -f $LASTEXITCODE)
        } catch { Say-Warn ('  AnswerCrashDumpDialog.ps1 failed: {0}' -f $_.Exception.Message) }
    } else {
        Say-Warn ('  {0} not found - skipping the crash-dump prompt step.' -f $answer)
    }
    if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
        Say-Ok ('  pid {0} exited after the crash-dump prompt was answered; no Stop-Process needed.' -f $ProcessId)
        return
    }
    Say-Warn ('  Stop-Process -Id {0} -Force: closing OUR OWN failed vrfSimHLA1516e (a process that failed its own startup, kept alive only by MAK''s crash handler). No other pid is touched.' -f $ProcessId)
    try { Stop-Process -Id $ProcessId -Force -ErrorAction Stop }
    catch { Say-Warn ('  Stop-Process failed: {0}. The pid remains and WILL block the next launch.' -f $_.Exception.Message); return }
    $wait = (Get-Date).AddSeconds($ExitWaitSec)
    while ((Get-Date) -lt $wait) {
        if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) { break }
        Start-Sleep -Seconds 1
    }
    if (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue) {
        Say-Fail ('  pid {0} is STILL present {1}s after Stop-Process - the next launch will be refused until it is gone.' -f $ProcessId, $ExitWaitSec)
    } else {
        Say-Ok ('  pid {0} closed; the pre-existing-process precondition will not block the next launch on its account.' -f $ProcessId)
    }
}

# ---- vendor-log HARVEST (replaces --logFileName; header, VENDOR LOG) -----------------------
# Newest VENDOR log written for $ProcessId at or after $Since, or '' when there is none. Same
# three filters as Get-CallstackFileForPid, and for the same reasons: the pid is the LAST name
# field (vrfSimHLA1516e5.2d-<date>-<time>-<host>-<build>-<pid>.log), C:\MAK\logs is shared by
# the whole MAK toolchain and keeps files across boots, and Windows recycles pids. The
# .callstack.log for the same pid does NOT match this filter (its last field is 'callstack',
# not the pid) and is excluded explicitly anyway - it is separate evidence with a separate
# life: it is the file that may be shared, and this one is not. Read-only, never throws.
function Get-VendorSimLogForPid {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since = [datetime]::MinValue,
          [string]$NamePrefix = 'vrfSim')
    try {
        $ls = @(Get-ChildItem -LiteralPath $LogDir -Filter ($NamePrefix + ('*-{0}.log' -f $ProcessId)) -File -ErrorAction SilentlyContinue |
                Where-Object { ($_.Name -notmatch '\.callstack\.log$') -and ($_.LastWriteTime -ge $Since) } |
                Sort-Object LastWriteTime -Descending)
        if ($ls.Count -gt 0) { return $ls[0].FullName }
    } catch { }
    return ''
}

# COPY (never move - the vendor may still be writing to it) that log to $Destination and say
# where it came from. Returns the destination on success, '' otherwise. A missing vendor log
# is a LOUD WARNING and nothing more: this function must never change the readiness verdict,
# which is decided by the thread-count oracle and the crash detector alone.
function Copy-VendorSimLog {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since, [string]$Destination,
          [string]$Occasion = 'READY')
    $src = Get-VendorSimLogForPid -ProcessId $ProcessId -LogDir $LogDir -Since $Since
    if (-not $src) {
        Say-Warn ('VENDOR LOG NOT FOUND for pid {0} in {1} (no vrfSim*-{0}.log written at or after {2:yyyy-MM-dd HH:mm:ss}). Nothing was copied to {3}. This does NOT change the verdict - but the run has no back-end log, so look in {1} by hand (the vendor stamps LOCAL time). --logFileName is deliberately NOT passed (PREREG_52_CRASH_BISECT_2026-09-04 sec 5: 6 crashes / 18 launches with it, 0 / 12 without).' -f `
            $ProcessId, $LogDir, $Since, $Destination)
        return ''
    }
    try {
        $dstDir = Split-Path -Parent $Destination
        if ($dstDir) { New-Item -ItemType Directory -Force -Path $dstDir | Out-Null }
        Copy-Item -LiteralPath $src -Destination $Destination -Force -ErrorAction Stop
    } catch {
        Say-Warn ('VENDOR LOG COPY FAILED ({0} -> {1}): {2}. The original is untouched; the verdict is unchanged.' -f $src, $Destination, $_.Exception.Message)
        return ''
    }
    $hasEnv = $false
    try { $hasEnv = [bool](Select-String -LiteralPath $Destination -SimpleMatch 'DtPrintEnvironmentVariables' -List -ErrorAction SilentlyContinue) } catch { }
    # ONE marker line, parsed by the runner (RunC2SimScenario Stage 3) into the manifest:
    # occasion is a single token, src runs to ' dst=', dst runs to end of line (paths may
    # contain spaces).
    Say-Ok ('VENDOR LOG HARVESTED occasion={0} src={1} dst={2}' -f $Occasion, $src, $Destination)
    Say ('         SNAPSHOT ONLY: taken at {0}; the sim keeps writing to {1}. Re-run the harvest (or copy that file again) for a later view.' -f $Occasion, $src)
    Say-Warn ('         SECRETS: this copy carries the FULL PROCESS ENVIRONMENT IN CLEARTEXT{0} (DtPrintEnvironmentVariables at --notifyLevel 3; FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10). NEVER attach it to a ticket, mail or issue - send the .callstack.log / .dmp instead. It is not scrubbed, by decision.' -f `
        $(if ($hasEnv) { ' - DtPrintEnvironmentVariables IS PRESENT in this copy' } else { ' (DtPrintEnvironmentVariables not found in this copy - assume it is there anyway)' }))
    return $Destination
}

$modeTag = if ($DryRun) { 'DRY-RUN' } else { 'LIVE' }
$scenarioDisplay = if ([string]::IsNullOrWhiteSpace($Scenario)) { '(none - -L omitted)' } else { $Scenario }
Say-Head "LaunchVrf52.ps1 ($modeTag) - VR-Forces 5.2d INDEPENDENT launch (UG52 4.1.2)"
Say ("  VrfRoot           : {0}" -f $VrfRoot)
Say ("  VrLinkRoot        : {0}" -f $VrLinkRoot)
Say ("  RtiDir            : {0}" -f $RtiDir)
Say ("  Scenario          : {0}" -f $scenarioDisplay)
Say ("  Site / Session    : {0} / {1}" -f $SiteId, $SessionId)
Say ("  Back-end appNumber: {0}" -f $BackendAppNumber)
Say ("  Front-end appNo   : {0}{1}" -f $FrontendAppNumber, $(if ($NoGui) { ' (-NoGui: not launched)' } else { '' }))
Say ("  DeviceAddress     : {0}" -f $(if ([string]::IsNullOrWhiteSpace($DeviceAddress)) { '(empty - --deviceAddress/--hostAddressString NOT passed; VR-Forces picks the first device listed)' } else { $DeviceAddress }))
Say ("  MakLogDir         : {0} (startup-crash callstacks AND the vendor's own sim log)" -f $MakLogDir)
Say ("  --logFileName     : {0}" -f $(if ([string]::IsNullOrWhiteSpace($LogFileName)) {
        'NOT PASSED (the default). PREREG_52_CRASH_BISECT_2026-09-04 sec 5: passing it crashed the sim at startup 6 times in 18 launches (~1 in 3), omitting it 0 in 12, p = 0.031 - and a short vendor-default path crashed too, so it is the OPTION, not the path. Do not re-enable it casually.'
    } else { ('PASSED DELIBERATELY -> {0}. THAT IS A ~1-IN-3 STARTUP CRASH (PREREG_52_CRASH_BISECT_2026-09-04 sec 5); only a bisect repeat or a vendor bug report should be doing this.' -f $LogFileName) }))
Say ("  vendor log harvest: {0}\vrfSim*-<pid>.log for THIS pid is COPIED (snapshot at READY / at a startup crash) to the -LogFile path reported below" -f $MakLogDir)
Say-Warn ("  SECRETS: that harvested copy holds the FULL PROCESS ENVIRONMENT IN CLEARTEXT (DtPrintEnvironmentVariables at --notifyLevel 3; FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10). NEVER attach it to a ticket, mail or issue - send the .callstack.log / .dmp instead.")
Say ("  Crashed processes : own pid on a startup crash -> {0}; pre-existing crashed leftover -> {1}" -f `
    $(if ($LeaveCrashedProcess) { 'LEFT RUNNING (-LeaveCrashedProcess; it will block the next launch)' } else { 'CLOSED before exit 3' }), `
    $(if ($CloseCrashedLeftover) { 'CLOSED if it has a callstack AND <= 4 threads (-CloseCrashedLeftover)' } else { 'left alone (refuses the launch; -CloseCrashedLeftover closes it)' }))

# ---- argument gate (hard, checked first, nothing launched on failure) ------
$appNoFail = $false
# A malformed -DeviceAddress would go straight onto the sim's command line, and an empty
# -MakLogDir would silently disarm the startup-crash detector. Both are exit 2.
if ((-not [string]::IsNullOrWhiteSpace($DeviceAddress)) -and ($DeviceAddress -notmatch '^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$')) {
    Say-Fail ("-DeviceAddress must be a dotted IPv4 address or EMPTY (empty = do not pass --deviceAddress/--hostAddressString); got '{0}'." -f $DeviceAddress)
    $appNoFail = $true
}
if ([string]::IsNullOrWhiteSpace($MakLogDir)) {
    Say-Fail '-MakLogDir must not be empty: it is where MAK writes the startup-crash callstack this script watches for. Pass the real directory (default C:\MAK\logs).'
    $appNoFail = $true
}
if ($BackendAppNumber -le 0) {
    Say-Fail 'MISSING -BackendAppNumber. MANDATORY (no default). Take the NEXT FREE value from OPUS_EXECUTION_PLAN.md Appendix B and ledger it BEFORE launching.'
    $appNoFail = $true
}
if ((-not $NoGui) -and ($FrontendAppNumber -le 0)) {
    Say-Fail 'MISSING -FrontendAppNumber. MANDATORY unless -NoGui. Ledger it BEFORE launching.'
    $appNoFail = $true
}
if ((-not $NoGui) -and ($BackendAppNumber -gt 0) -and ($BackendAppNumber -eq $FrontendAppNumber)) {
    Say-Fail ('-BackendAppNumber and -FrontendAppNumber are IDENTICAL ({0}). Each join consumes its own number.' -f $BackendAppNumber)
    $appNoFail = $true
}
if ($appNoFail) { Say-Head 'Result'; Say-Fail 'Aborting: argument gate failed. NOTHING was launched.'; exit 2 }

# ---- derived paths ---------------------------------------------------------
$bin64      = Join-Path $VrfRoot 'bin64'
$simExe     = Join-Path $bin64 'vrfSimHLA1516e.exe'
$guiExe     = Join-Path $bin64 'vrfGui.exe'
$vrlBin     = Join-Path $VrLinkRoot 'bin64'
$rtiBin     = Join-Path $RtiDir 'bin'
$repoRootEarly = Split-Path -Parent $PSScriptRoot
$ridFile    = if ($UseRtiAssistant) { Join-Path $RtiDir 'rid.mtl' }
              elseif ([string]::IsNullOrWhiteSpace($RidFile)) { Join-Path $repoRootEarly 'config\rid-501-rtiexec-min.mtl' }
              else { $RidFile }
$connDir    = Join-Path $VrfRoot 'appData\settings\connections'
$connFile   = if ([string]::IsNullOrWhiteSpace($ExConnConfigFile)) { Join-Path $connDir 'MAK-ONE-2025-Config.xml' } else { $ExConnConfigFile }
$scenarioRel = ''
$scenarioAbs = ''
if (-not [string]::IsNullOrWhiteSpace($Scenario)) {
    $scenarioRel = '../userData/scenarios/' + ($Scenario -replace '\\', '/') + '.scnx'
    $scenarioAbs = Join-Path $VrfRoot ('userData\scenarios\{0}.scnx' -f $Scenario)
}
$repoRoot   = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $stamp   = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $LogFile = Join-Path $repoRoot ('runs\launch52\vrfSim_{0}_{1}.log' -f $BackendAppNumber, $stamp)
}
$licMachine = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')

$procBackend  = 'vrfSimHLA1516e'
$procFrontend = 'vrfGui'
# Thread ceiling for the crashed-leftover classifier (Test-CrashedLeftover). 4, not
# $BackendMinThreads (8): the two answer different questions. 8 is the HEALTH floor - above it
# the sim is serviceable. 4 is the CORPSE ceiling - the crashed process observed on 2026-09-04
# sat at 0 threads, and a back-end merely blocked on the RTI sits at 2-4. The gap 5..8 is
# deliberately claimed by NEITHER: a process in it is neither healthy nor provably dead, so it
# is reported and left alone.
$CrashedLeftoverMaxThreads = 4

# ---- PRECONDITIONS (read-only; run in both DryRun and live) ----------------
Say-Head 'Preconditions'
$hardFail = $false
foreach ($chk in @(
    @{ p=$simExe;   what='5.2d back-end vrfSimHLA1516e.exe' },
    @{ p=$guiExe;   what='5.2d front-end vrfGui.exe' },
    @{ p=(Join-Path $vrlBin 'vlHLA1516e.dll'); what='VR-Link 5.10 bin64 (vlHLA1516e.dll)' },
    @{ p=$rtiBin;   what='RTI bin dir' },
    @{ p=$ridFile;  what='RTI rid.mtl (per-process RTI_RID_FILE target)' },
    @{ p=$connFile; what='exercise connection config (MAK-ONE-YYYY-Config.xml)' }
)) {
    if (Test-Path -LiteralPath $chk.p) { Say-Ok ("{0}: {1}" -f $chk.what, $chk.p) }
    else { Say-Fail ("{0} MISSING: {1}" -f $chk.what, $chk.p); $hardFail = $true }
}
if ($scenarioAbs) {
    if (Test-Path -LiteralPath $scenarioAbs) { Say-Ok ("scenario file: {0}" -f $scenarioAbs) }
    else { Say-Fail ("scenario file MISSING: {0}" -f $scenarioAbs); $hardFail = $true }
}
$logDir = Split-Path -Parent $LogFile
if ($logDir -like 'C:\MAK*') { Say-Fail ("log file would land under C:\MAK ({0}) - refused; pass -LogFile outside the vendor tree." -f $LogFile); $hardFail = $true }
else { Say-Ok ("back-end log (HARVEST DESTINATION - the vendor's own log for this pid is copied here; --logFileName is not passed): {0}" -f $LogFile) }

# Mixed-RTI environment report (Machine scope, informational - overridden per process)
$mRti = [Environment]::GetEnvironmentVariable('MAK_RTIDIR','Machine')
$mRid = [Environment]::GetEnvironmentVariable('RTI_RID_FILE','Machine')
if ($mRti -and ($mRti -ne $RtiDir)) { Say-Warn ("Machine MAK_RTIDIR={0} differs from -RtiDir {1}; overriding MAK_RTIDIR and RTI_RID_FILE for the launched processes only." -f $mRti, $RtiDir) }
else { Say-Ok ("Machine MAK_RTIDIR={0}" -f $(if ($mRti) { $mRti } else { '(unset)' })) }
if ($mRid) { Say ("         Machine RTI_RID_FILE={0}" -f $mRid) }

# Stale VR-Forces processes (federates). rtiexec/rtiForwarder/rtiAssistant are RTI
# infrastructure: reported, never refused on, NEVER killed.
$vrfProcNames = @('vrfLauncher',$procBackend,$procFrontend)
$existing = @()
foreach ($n in $vrfProcNames) {
    $p = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($p) { $existing += ($p | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($existing.Count -gt 0) {
    Say-Warn ("VR-Forces processes ALREADY running: {0}" -f ($existing -join ', '))
    # A pre-existing back-end is not necessarily a RUNNING sim. On 2026-09-04 (launch_3860 ->
    # launch_3862) the previous launch's back-end had crashed at startup and MAK's handler kept
    # the corpse alive at 0 threads, titled 'Error vrfSimHLA1516e.exe'; this precondition then
    # refused the retry with exit 2. Say what it is, and close it ONLY under
    # -CloseCrashedLeftover and ONLY when both leftover conditions hold.
    foreach ($bp in @(Get-Process -Name $procBackend -ErrorAction SilentlyContinue)) {
        # An UNREADABLE thread count must never be read as 0: that is the corpse signature, and
        # inventing it would let this script close a process it knows nothing about.
        $bThr = 0; $bThrOk = $false
        try { $bThr = $bp.Threads.Count; $bThrOk = $true } catch { }
        $bStart = [datetime]::MinValue; try { $bStart = $bp.StartTime }    catch { }
        $bTitle = '';                   try { $bTitle = $bp.MainWindowTitle } catch { }
        $bCs = Get-CallstackFileForPid -ProcessId $bp.Id -LogDir $MakLogDir -Since $bStart
        if ($bThrOk -and (Test-CrashedLeftover -ProcessId $bp.Id -ThreadCount $bThr -LogDir $MakLogDir -Since $bStart -MaxThreads $CrashedLeftoverMaxThreads)) {
            Say-Fail ("  pid {0} is a CRASHED LEFTOVER, not a running sim: {1} thread(s) (<= {2}) AND MAK wrote a callstack for that pid ({3}){4}. MAK's crash handler keeps a faulted process alive, so it goes on blocking launches until something closes it." -f `
                $bp.Id, $bThr, $CrashedLeftoverMaxThreads, $bCs, $(if ($bTitle) { ", window title '$bTitle'" } else { '' }))
            Say-Fail ('  LaunchVrf52 closes a crashed back-end automatically ONLY on the launch that DETECTED the crash - the pid it started itself. This pid predates this launch, so it is left alone by default. Close it by hand (Stop-Process -Id {0} -Force) or rerun with -CloseCrashedLeftover, which closes ONLY a pre-existing vrfSimHLA1516e whose pid HAS a callstack file AND has <= {1} threads (both, so a healthy 34-62 thread sim can never match, and neither can an RTI process - only vrfSimHLA1516e pids are ever considered).' -f $bp.Id, $CrashedLeftoverMaxThreads)
            if ($CloseCrashedLeftover) {
                if ($DryRun) { Say-Plan ('close crashed leftover pid {0} (-CloseCrashedLeftover): AnswerCrashDumpDialog.ps1 then Stop-Process on that pid only.' -f $bp.Id) }
                else {
                    Say-Warn ('  -CloseCrashedLeftover: closing pid {0} now.' -f $bp.Id)
                    Close-CrashedBackend -ProcessId $bp.Id -ExpectedName $procBackend
                }
            }
        } else {
            Say-Warn ('  pid {0} is NOT classified as a crashed leftover (threads: {1}; callstack for this pid since its start: {2}) - it is left alone even with -CloseCrashedLeftover. Only a process failing BOTH tests may be closed.' -f `
                $bp.Id, $(if ($bThrOk) { $bThr } else { 'UNREADABLE - treated as not-a-corpse' }), $(if ($bCs) { $bCs } else { 'none' }))
        }
    }
    # Re-inventory: only what is STILL running can refuse this launch.
    $existing = @()
    foreach ($n in $vrfProcNames) {
        $p = Get-Process -Name $n -ErrorAction SilentlyContinue
        if ($p) { $existing += ($p | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
    }
}
if ($existing.Count -gt 0) {
    if (-not $AllowExistingVrf) { Say-Fail ('  Refusing to launch on top of existing VR-Forces processes ({0}) (-AllowExistingVrf overrides deliberately).' -f ($existing -join ', ')); $hardFail = $true }
    else { Say-Warn '  -AllowExistingVrf set: proceeding despite existing processes.' }
} else { Say-Ok 'no pre-existing vrfLauncher / vrfSimHLA1516e / vrfGui processes' }
$infra = @()
foreach ($n in @('rtiexec','rtiForwarder')) {
    $ip = Get-Process -Name $n -ErrorAction SilentlyContinue
    if ($ip) { $infra += ($ip | ForEach-Object { '{0}(pid {1})' -f $_.Name, $_.Id }) }
}
if ($infra.Count -gt 0) { Say-Ok ("RTI infrastructure present (expected; do NOT kill): {0}" -f ($infra -join ', ')) }
else {
    # NOT a hard failure here (this script does not own the rendezvous), but under the
    # rtiexec posture an absent rtiexec means the sim has nothing to rendezvous with and
    # the federation degrades to the lightweight mode UG52 5.5.1 p190 prohibits.
    Say-Warn 'no rtiexec / rtiForwarder is running. The 5.2 posture is RTIEXEC mode (UG52 5.5.1 p190): run scripts\StartRtiExec52.ps1 first, or use the runner, whose Stage 2r does it. Never kill one that IS running.'
}
$assist = Get-Process -Name 'rtiAssistant' -ErrorAction SilentlyContinue
if (-not $UseRtiAssistant) {
    if ($assist) {
        $ids = ($assist | ForEach-Object { $_.Id }) -join ', '
        Say-Ok ("assistant-free mode (RTI_ASSISTANT_DISABLE + rid-configured rtiexec connection): existing rtiAssistant pid(s) {0} are IGNORED by the launched processes (an assistant of the wrong version rejects the federate outright, and an ELEVATED one cannot be clicked from here - the reasons this mode is the default). Never kill them." -f $ids)
    } else {
        Say-Ok 'assistant-free mode: no rtiAssistant running and none will be spawned or consulted (RTI_ASSISTANT_DISABLE, RTI Ref Manual 5.2.10). No Choose RTI Connection dialog can occur.'
    }
} elseif ($assist) {
    foreach ($a in $assist) {
        $t = if ([string]::IsNullOrWhiteSpace($a.MainWindowTitle)) { '(no window title - ALSO the signature of an ELEVATED assistant, whose windows/dialogs this script cannot see or click)' } else { $a.MainWindowTitle }
        $started = try { $a.StartTime.ToString('yyyy-MM-dd HH:mm:ss') } catch { '(start time inaccessible)' }
        Say-Warn ("-UseRtiAssistant: pre-existing rtiAssistant pid {0} started {1} - window: {2}. A 5.0.1 assistant version-rejects 4.6.1 federates ('RTI component was using a different RTI version', observed 2026-09-03); observe, never kill." -f $a.Id, $started, $t)
        if ($a.MainWindowTitle -match 'Choose RTI Connection') {
            if ($IgnoreUnansweredRtiAssistant) { Say-Warn '  -IgnoreUnansweredRtiAssistant set: proceeding.' }
            else { Say-Fail ("  pid {0} sits on the UNANSWERED 'Choose RTI Connection' dialog. Back-ends WILL block behind it. Run scripts/AnswerRtiDialog.ps1 first." -f $a.Id); $hardFail = $true }
        }
    }
} else {
    Say-Warn '-UseRtiAssistant with NO pre-existing rtiAssistant: the first federate spawns one that PROMPTS (Choose RTI Connection); the back-end blocks until it is answered (AnswerRtiDialog.ps1 - cannot click an ELEVATED assistant).'
}
if ([string]::IsNullOrWhiteSpace($licMachine) -and [string]::IsNullOrWhiteSpace($env:MAKLMGRD_LICENSE_FILE)) {
    Say-Warn 'MAKLMGRD_LICENSE_FILE empty in Machine AND process scope - license checkout may hang.'
} else { Say-Ok ("MAKLMGRD_LICENSE_FILE = {0}" -f $(if ($licMachine) { $licMachine } else { $env:MAKLMGRD_LICENSE_FILE })) }

# ---- argument strings (UG52 4.1.2 / 4.1.3 / Table 11 / Table 12) ------------
$simArgs = @('--siteId', $SiteId, '--appNumber', $BackendAppNumber, '--sessionId', $SessionId,
             '--notifyLevel', $NotifyLevel)
# --logFileName ONLY when -LogFileName was passed on purpose. Default EMPTY = absent from the
# command line: with it the sim crashed at startup 6 times in 18 launches, without it 0 in 12
# (PREREG_52_CRASH_BISECT_2026-09-04 sec 5, p = 0.031; a short vendor-default path crashed too,
# so the path is not the trigger). The vendor writes its own log to $MakLogDir either way and
# this script harvests it - see Copy-VendorSimLog.
if (-not [string]::IsNullOrWhiteSpace($LogFileName)) { $simArgs += @('--logFileName', ('"{0}"' -f $LogFileName)) }
if ($scenarioRel) { $simArgs += @('--scenarioFileName', ('"{0}"' -f $scenarioRel)) }
if (-not [string]::IsNullOrWhiteSpace($ExConnConfigFile)) { $simArgs += @('--exConnConfigFile', ('"{0}"' -f $ExConnConfigFile)) }
if ($QuietBackend) { $simArgs += '--doNotUseConsole' }
if (-not [string]::IsNullOrWhiteSpace($DeviceAddress)) { $simArgs += @('--deviceAddress', $DeviceAddress, '--hostAddressString', $DeviceAddress) }
$guiArgs = @('--siteId', $SiteId, '--appNumber', $FrontendAppNumber, '--sessionId', $SessionId, '--hla1516e')
if (-not [string]::IsNullOrWhiteSpace($ExConnConfigFile)) { $guiArgs += @('--exConnConfigFile', ('"{0}"' -f $ExConnConfigFile)) }
if (-not [string]::IsNullOrWhiteSpace($DeviceAddress)) { $guiArgs += @('--deviceAddress', $DeviceAddress, '--hostAddressString', $DeviceAddress) }
$simArgString = ($simArgs -join ' ')
$guiArgString = ($guiArgs -join ' ')
$pathPrefix = '{0};{1};{2};' -f $bin64, $vrlBin, $rtiBin

Say-Head 'Plan'
Say ("  process env : PATH={0}<Machine PATH>" -f $pathPrefix)
Say ("                MAK_VRFDIR={0}  MAK_VRLDIR={1}  MAK_RTIDIR={2}" -f $VrfRoot, $VrLinkRoot, $RtiDir)
Say ("                RTI_RID_FILE={0}" -f $ridFile)
if (-not $UseRtiAssistant) { Say '                RTI_ASSISTANT_DISABLE=1 (assistant-free; connection from the rid)' }
Say ("  back-end    : {0} {1}" -f $simExe, $simArgString)
if ($NoGui) { Say '  front-end   : (not launched: -NoGui)' } else { Say ("  front-end   : {0} {1}" -f $guiExe, $guiArgString) }
Say ("  cwd         : {0}" -f $bin64)

if ($hardFail) {
    Say-Head 'Result'
    if ($DryRun) { Say-Warn 'DRY-RUN: one or more HARD preconditions FAILED above. A live run would abort here.' }
    else { Say-Fail 'Aborting: hard precondition failure (see above).' }
    exit 2
}
if ($DryRun) {
    Say-Head 'Result'
    Say-Plan ("Start-Process '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $simExe, $bin64, $simArgString)
    if (-not $NoGui) { Say-Plan ("Start-Process '{0}' -WorkingDirectory '{1}' -ArgumentList '{2}'" -f $guiExe, $bin64, $guiArgString) }
    Say-Plan ("poll up to {0}s every {1}s: back-end threads > {2}{3}" -f $ReadyTimeoutSec, $PollIntervalSec, $BackendMinThreads, $(if ($NoGui) { '' } else { ' AND vrfGui MainWindowTitle non-empty' }))
    Say-Plan ("HARVEST the vendor's own back-end log at READY: newest {0}\vrfSim*-<pid>.log for THIS pid, written at or after the launch floor, COPIED (never moved - the sim keeps writing) to {1}. A snapshot; missing = loud WARN, verdict unchanged. --logFileName is NOT passed (PREREG_52_CRASH_BISECT_2026-09-04 sec 5: 6 crashes / 18 launches with it, 0 / 12 without, p = 0.031){2}." -f `
        $MakLogDir, $LogFile, $(if ([string]::IsNullOrWhiteSpace($LogFileName)) { '' } else { (' - EXCEPT that -LogFileName was given, so this run DOES pass it, at a ~1-in-3 crash risk: ' + $LogFileName) }))
    Say-Plan 'WARN, on that harvested copy, that it holds the full process environment in cleartext and must never be attached to a ticket or mail (send the .callstack.log / .dmp instead).'
    Say-Plan ("watch the SAME poll for a STARTUP CRASH (0xC0000005 in DtVrfSimOptions::parseCmdLine; its trigger, --logFileName, is NOT passed - PREREG_52_CRASH_BISECT_2026-09-04 sec 5 - but the detector stays): back-end gone, a MAK crash-box window title, or a new {0}\vrfSimHLA1516e*-<pid>.callstack.log. On any of them: print the first frames and exit 3, WITHOUT retrying." -f $MakLogDir)
    Say-Plan 'HARVEST that crashed pid''s vendor log too, on the crash path and BEFORE the corpse is closed - a crashed run''s short log is the forensic value, and the copy lands beside the callstack path printed with the frames (same secrets warning: never attach it anywhere).'
    if ($LeaveCrashedProcess) {
        Say-Plan 'LEAVE the crashed back-end running on that crash (-LeaveCrashedProcess) - and it would then refuse the NEXT launch until closed by hand or with -CloseCrashedLeftover.'
    } else {
        Say-Plan 'close OUR OWN failed back-end pid on that crash, before exiting 3 (AnswerCrashDumpDialog.ps1, then Stop-Process on that pid only - never the GUI, never rtiexec / rtiForwarder / rtiAssistant), so the next launch is not refused by the corpse.'
    }
    Say-Ok 'DRY-RUN complete: preconditions passed, nothing launched.'
    exit 0
}

# ---- STARTUP-CRASH DETECTOR (see the header block) --------------------------
# Returns a record for the back-end pid: crashed yes/no, which signature fired, the
# callstack file if MAK wrote one, and its first frames. Read-only: it decides, it does not
# act. Closing the crashed pid is the caller's job (Close-CrashedBackend, after the evidence
# has been printed), and only ever for the pid this script started.
function Get-SimCrashEvidence {
    param([int]$ProcessId, [string]$LogDir, [datetime]$Since)
    $o = [ordered]@{ Crashed = $false; Reason = ''; File = ''; Frames = @() }
    # (c) the callstack file - the strongest signature, and the only one that survives the
    # process exiting before we look. Get-CallstackFileForPid owns the pid-recycling guard
    # ($Since); see its comment.
    try {
        $csFile = Get-CallstackFileForPid -ProcessId $ProcessId -LogDir $LogDir -Since $Since
        if ($csFile) {
            $o.Crashed = $true
            $o.Reason  = 'MAK crash handler wrote a callstack for this pid'
            $o.File    = $csFile
            $o.Frames  = @(Get-Content -LiteralPath $csFile -TotalCount 12 -ErrorAction SilentlyContinue)
            return $o
        }
    } catch { }
    # (b) the crash box: MAK titles it 'Error <exe>' on 5.2 and '<exe>...dmp' on 5.0.2.
    try {
        $p = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($p) {
            $t = ''
            try { $t = $p.MainWindowTitle } catch { }
            if ($t -match '^Error .*vrfSim' -or $t -match '^vrfSim.*\.dmp$') {
                $o.Crashed = $true
                $o.Reason  = ("MAK crash dialog is up on the back-end: window title '{0}'" -f $t)
                return $o
            }
        }
    } catch { }
    return $o
}

# ---- LIVE ------------------------------------------------------------------
if (-not [string]::IsNullOrWhiteSpace($licMachine)) { $env:MAKLMGRD_LICENSE_FILE = $licMachine }
$env:PATH         = $pathPrefix + $env:PATH
$env:MAK_VRFDIR   = $VrfRoot
$env:MAK_VRLDIR   = $VrLinkRoot
$env:MAK_RTIDIR   = $RtiDir
$env:RTI_RID_FILE = $ridFile
if (-not $UseRtiAssistant) { $env:RTI_ASSISTANT_DISABLE = '1' }
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

Say-Head 'Launch'
# Taken BEFORE the start so it can never be later than the process itself; it is the floor
# for "this back-end's callstack file" (see Get-SimCrashEvidence -Since). A couple of seconds
# of slack absorbs filesystem timestamp granularity without letting in yesterday's crash.
$simStartFloor = (Get-Date).AddSeconds(-5)
$simProc = Start-Process -FilePath $simExe -WorkingDirectory $bin64 -ArgumentList $simArgString -PassThru
Say-Ok ("back-end started (pid {0})" -f $simProc.Id)
$guiProc = $null
if (-not $NoGui) {
    $guiProc = Start-Process -FilePath $guiExe -WorkingDirectory $bin64 -ArgumentList $guiArgString -PassThru
    Say-Ok ("front-end started (pid {0})" -f $guiProc.Id)
}
Say-Ok 'polling for readiness...'

$deadline   = (Get-Date).AddSeconds($ReadyTimeoutSec)
$backendUp  = $false; $backendThr = 0; $backendExit = $null
$frontUp    = $false; $guiTitle = ''; $frontExit = $null
$needFront  = -not $NoGui
# The startup crash is NOT "not ready yet" - it is terminal, and the poll must not sit out
# its whole timeout on a dead process. Checked EVERY iteration, and once more after the
# loop (the callstack file can land a moment after the process disappears).
$simCrash   = [ordered]@{ Crashed = $false; Reason = ''; File = ''; Frames = @() }
while ((Get-Date) -lt $deadline) {
    $simCrash = Get-SimCrashEvidence -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor
    if ($simCrash.Crashed) { $backendThr = 0; break }
    $b = Get-Process -Id $simProc.Id -ErrorAction SilentlyContinue
    $backendUp = [bool]$b
    if ($b) { $backendThr = $b.Threads.Count } else { $backendThr = 0; $backendExit = $simProc.ExitCode; break }
    if ($needFront) {
        $f = Get-Process -Id $guiProc.Id -ErrorAction SilentlyContinue
        $frontUp = [bool]$f
        if ($f) { $guiTitle = $f.MainWindowTitle } else { $frontExit = $guiProc.ExitCode }
    }
    $backendHealthy = ($backendThr -gt $BackendMinThreads)
    $guiTitleOk = -not [string]::IsNullOrWhiteSpace($guiTitle)
    if ($backendHealthy -and ((-not $needFront) -or ($frontUp -and $guiTitleOk))) { break }
    Start-Sleep -Seconds $PollIntervalSec
}
# Keep the verdict definition IDENTICAL to the in-loop one (LaunchVrf.ps1 lesson).
$backendHealthy = ($backendThr -gt $BackendMinThreads)
$guiTitleOk = -not [string]::IsNullOrWhiteSpace($guiTitle)
# One more look: MAK writes the callstack around the moment the process disappears, so a
# poll that ended on "process gone" can still gain the evidence a second later.
if (-not $simCrash.Crashed) {
    $late = Get-SimCrashEvidence -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor
    if ($late.Crashed) { $simCrash = $late }
}

Say-Head 'Readiness'
if ($simCrash.Crashed) {
    # THE STARTUP CRASH (header block). Loud, with the frames, and terminal - the run that
    # follows would otherwise push an init at a back-end that never existed.
    Say-Fail ('back-end pid {0} CRASHED AT STARTUP - {1}' -f $simProc.Id, $simCrash.Reason)
    if ($simCrash.File) { Say-Fail ('  callstack: {0}' -f $simCrash.File) }
    foreach ($ln in @($simCrash.Frames)) { Say ('         | ' + $ln) }
    Say-Fail '  0xC0000005 in makVrf::DtVrfSimOptions::parseCmdLine. Its KNOWN trigger is --logFileName (6 crashes / 18 launches with it, 0 / 12 without, p = 0.031 - PREREG_52_CRASH_BISECT_2026-09-04 sec 5) and this script does not pass it by default. A crash WITHOUT -LogFileName is therefore NEW: the option is exonerated for this one, so record it and do not reuse the old explanation. NOT retried here.'
    if (-not [string]::IsNullOrWhiteSpace($LogFileName)) {
        Say-Fail ('  -LogFileName WAS PASSED on this launch ({0}). That is the KNOWN trigger: PREREG_52_CRASH_BISECT_2026-09-04 sec 5 measured 6 crashes / 18 launches with the option and 0 / 12 without (p = 0.031). Drop it before reading anything else into this crash.' -f $LogFileName)
    }
    # HARVEST BEFORE CLOSING: the corpse's own vendor log is short and is exactly the forensic
    # value of a crashed run, and the crash handler may still be holding the file. The copy
    # lands beside the callstack path printed above; it is a copy, so the original stays for
    # MAK. A missing one is a warning, never a change to this crash verdict.
    $null = Copy-VendorSimLog -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor -Destination $LogFile -Occasion 'STARTUP-CRASH'
    Say '  (A crashed pid USUALLY HAS NO vendor log at all: of the 10 pids with a .callstack.log in C:\MAK\logs on 2026-09-04, 9 had a .dmp and a .callstack.log but no .log - consistent with the fault being IN the log-stream installer itself. A "VENDOR LOG NOT FOUND" warning here is EXPECTED, not a second defect; the callstack and the dump above are the evidence.)'
    # The process is dead as a simulator but NOT gone: MAK's crash handler parks it (0 threads,
    # title 'Error vrfSimHLA1516e.exe'), and on 2026-09-04 that corpse made the next launch
    # exit 2 on the pre-existing-process precondition - an unattended runner could not retry.
    # This is OUR OWN pid, and it failed ITS OWN startup, so closing it needs no permission
    # (project rule); a healthy instance would still need one, and gets none here.
    if ($LeaveCrashedProcess) {
        Say-Warn ('  -LeaveCrashedProcess: pid {0} is LEFT AS IT IS for forensics (crash box, handles, dump prompt intact). It WILL refuse the next launch until it is closed by hand or with -CloseCrashedLeftover.' -f $simProc.Id)
    } else {
        Say-Warn ('  closing pid {0}: it is the back-end THIS script started and it failed its own startup. RTI infrastructure (rtiexec / rtiForwarder / rtiAssistant) and every other pid are untouched. -LeaveCrashedProcess keeps it instead.' -f $simProc.Id)
        Close-CrashedBackend -ProcessId $simProc.Id -ExpectedName $procBackend
    }
    if ($needFront -and $guiProc -and (Get-Process -Id $guiProc.Id -ErrorAction SilentlyContinue)) {
        Say-Warn ('  the front-end this script started (pid {0}) is still up. It did NOT fail its own startup, so it is NOT closed here - but it will refuse the next launch unless it is stopped (scripts\StopVrf52.ps1) or -AllowExistingVrf is passed.' -f $guiProc.Id)
    }
}
if (-not $backendUp) {
    Say-Fail ("back-end pid {0} NOT present (exit code {1}) - check the log: {2}" -f $simProc.Id, $backendExit, $LogFile)
} elseif ($backendHealthy) {
    Say-Ok ("back-end pid {0} is HEALTHY by thread count ({1} threads; floor {2})" -f $simProc.Id, $backendThr, $BackendMinThreads)
} else {
    Say-Fail ("back-end pid {0} PRESENT BUT NOT HEALTHY - {1} threads (floor {2}). PROCESS PRESENCE IS NOT HEALTH. Suspects: no rtiexec listening for this rid (scripts\StartRtiExec52.ps1), RTI version mismatch between the federate and an assistant, license, connection config." -f $simProc.Id, $backendThr, $BackendMinThreads)
}
if ($needFront) {
    if ($frontUp -and $guiTitleOk) { Say-Ok ("front-end pid {0} up with a real main window (title: '{1}')" -f $guiProc.Id, $guiTitle) }
    elseif ($frontUp) { Say-Fail ("front-end pid {0} exists but MainWindowTitle is EMPTY - blocking modal dialog signature (Scenario Startup dialog, license/LRC box). Look at the screen; do not kill." -f $guiProc.Id) }
    else { Say-Warn ("front-end pid {0} NOT up (exit code {1})" -f $guiProc.Id, $frontExit) }
}
# THE HARVEST (header, VENDOR LOG): --logFileName is not passed, so the only back-end log is
# the vendor's own in $MakLogDir. Copy it for THIS pid, on every non-crash outcome - a NOT
# READY back-end is exactly when its log matters most. The crash path harvested already,
# before closing the corpse, so it is not repeated here.
if (-not $simCrash.Crashed) {
    $null = Copy-VendorSimLog -ProcessId $simProc.Id -LogDir $MakLogDir -Since $simStartFloor -Destination $LogFile `
                -Occasion $(if ($backendHealthy) { 'READY' } else { 'NOT-READY' })
}
if (Test-Path -LiteralPath $LogFile) {
    Say-Ok ("back-end log tail - HARVESTED COPY, secrets warning above, do not attach it anywhere ({0}):" -f $LogFile)
    Get-Content -LiteralPath $LogFile -Tail 12 -ErrorAction SilentlyContinue | ForEach-Object { Say ('         | ' + $_) }
} else { Say-Warn ("no back-end log at {0}: the vendor log for this pid was not found or could not be copied (see the harvest warning above). --logFileName is deliberately NOT passed - PREREG_52_CRASH_BISECT_2026-09-04 sec 5." -f $LogFile) }

Say-Head 'Result'
if ($simCrash.Crashed) {
    # Checked FIRST: a crashed back-end can momentarily still satisfy the thread-count
    # oracle, and "READY" on a process with a callstack file would be the worst false green
    # this script could produce.
    Say-Fail ('CRASHED: the 5.2d back-end died at startup ({0}). NOT READY, NOT retried here - {1}. Exit 3.' -f `
        $simCrash.Reason,
        $(if ($LeaveCrashedProcess) { 'the crashed pid was LEFT RUNNING (-LeaveCrashedProcess) and will block the next launch' } else { 'the crashed pid was closed above, so a retry is not blocked by it' }))
    exit 3
}
if ($backendHealthy -and ((-not $needFront) -or ($frontUp -and $guiTitleOk))) {
    $frontNote = if ($needFront) { ', front-end with a real main window' } else { ' (no GUI requested)' }
    Say-Ok ('READY: 5.2d back-end HEALTHY by thread count{0}. Federation JOIN is NOT tested here - confirm with the 5.2 build of RtiProbe/WatchVrf before trusting anything.' -f $frontNote)
    exit 0
} elseif ($backendHealthy -and $frontUp) {
    Say-Fail 'BLOCKED: back-end healthy, front-end PROCESS up with NO main window title - a modal dialog is waiting for a human. Do NOT force-kill.'
    exit 4
} elseif ($backendHealthy) {
    Say-Warn 'PARTIAL: back-end healthy but the front-end never appeared in time.'
    exit 1
} else {
    Say-Fail 'NOT READY within timeout. Do NOT force-kill; inspect the back-end console/log and the RTI Assistant.'
    exit 3
}
