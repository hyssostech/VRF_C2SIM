#!/usr/bin/env bash
# RunScenario.sh - the ONLY supported way to launch scripts/RunC2SimScenario.ps1 for a
# live run. It is a TEMPLATE: the defaults below are the G6 configuration, every one of
# them is overridable on the command line, and anything after "--" is passed straight
# through to the runner.
#
# WHY THIS FILE EXISTS (2026-09-14; docs/experiments/RUNNER_HARDENING_2026-09-14.md).
# The G6 run of 2026-09-14 was launched by an ad-hoc two-line wrapper and three separate
# defects met in it:
#
#   1. The runner pwsh was TERMINATED from outside at t+127 s of its observation window
#      (a high-bit Windows exit code, reported by this bash as 127). TerminateProcess
#      cannot be intercepted, so the runner's own try/finally teardown - which is correct
#      and covers every in-process path - never ran. VR-Forces and the interface stayed
#      joined for NINE HOURS and the app log grew to 5.89 GB. A teardown backstop must
#      therefore live OUTSIDE the runner process: that is this script, sec [BACKSTOP].
#   2. Bare "pwsh" on this machine resolves to C:\Program Files (x86)\PowerShell\7 - the
#      32-BIT build. A 32-bit host died of address-space exhaustion in this same loop on
#      2026-09-07. This script pins the 64-bit build BY FULL PATH and verifies it; the
#      runner refuses a 32-bit host outright.
#   3. The runner's stdout was a PIPE. .NET's Process.Start with any redirection calls
#      CreateProcess with bInheritHandles=TRUE, which duplicates every inheritable handle
#      into the child - so VrfC2SimApp inherited the harness pipe and held it open for
#      nine hours even though it never wrote to it. The runner's stdout is therefore a
#      FILE here, stdin is /dev/null, and the runner is NEVER piped and NEVER teed.
#      NEVER add "| tee": it recreates the inherited pipe AND makes $? the status of tee.
#
# Watch the run from ANOTHER shell with:  tail -f <the log path this script prints>
# ASCII only.

set -u

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO" || { echo "cannot cd to repo root"; exit 1; }

# ---- the 64-bit host, pinned by full path -----------------------------------
PWSH64='/c/Program Files/PowerShell/7/pwsh.exe'

# ---- defaults (the G6 configuration) ----------------------------------------
PROFILE='5.2'
NOGUI=1
SCENARIO='R9_Mojave_Empty_52_NavAO'
INIT='data/COA-STP1_Initialization.xml'
ORDER='data/COA-STP1_Order.xml'
CLIENT_ID='C2SIM'
TYPEMAP=''
RUN_SECS=900
WATCH_SECS=1200
BACKEND_NOTIFY=3
OBJ_CONSOLE=4
MEMBER_CONSOLE=4
POS_REPORT=10
REST_URL='http://127.0.0.1:18080/C2SIMServer'
STOMP_URL='http://127.0.0.1:61614/topic/C2SIM'
STOP_WHEN_COMPLETE=1
DRYRUN=0
SAMPLE_THREADS=0
PRE_ORDER_SETTLE=0
VRF_APPDATA_DIR=''
LOG=''
EXTRA_ENV=()
PASSTHRU=()

usage() {
    cat <<'USAGE'
usage: scripts/RunScenario.sh [options] [-- <extra runner arguments>]

  --profile 5.2|5.0.2       VR-Forces profile                 (default 5.2)
  --gui | --no-gui          front end on/off (5.2 only)       (default --no-gui)
  --scenario NAME           scenario name                     (default R9_Mojave_Empty_52_NavAO)
  --init PATH               C2SIM initialization xml          (default data/COA-STP1_Initialization.xml)
  --order PATH              C2SIM order xml                   (default data/COA-STP1_Order.xml)
  --client-id ID            must equal the init's SystemName  (default C2SIM)
  --type-map PATH           Vrf__TypeMapFile (WINDOWS path)   (default: the repo map)
  --run-secs N              observation window cap            (default 900)
  --watch-secs N            observer duration cap             (default 1200)
  --backend-notify N        sim-wide --notifyLevel 0..4       (default 3)
  --object-console N        Vrf__ObjectConsoleNotifyLevel     (default 4)
  --member-console N        Vrf__ObjectConsoleMemberNotifyLevel (default 4)
  --position-report N       Vrf__PositionReportSeconds        (default 10)
  --rest-url URL            C2SIM REST endpoint               (default the private test server)
  --stomp-url URL           C2SIM STOMP endpoint              (default the private test server)
  --stop-when-complete | --no-stop-when-complete              (default on)
  --env K=V                 extra environment for the app (repeatable)
  --pre-order-settle N      stage 7d: hold N s after the oracle gate and BEFORE PushOrder, so
                            the LAZILY loaded sectorised nav area can arrive (default 0 = off)
  --vrf-appdata-dir DIR     5.2 only: --appDataDir for the sim and the gui (default empty =
                            not passed, VR-Forces uses its own appData). The prepared copy is
                            C:\C2SIM\vrf-appdata\appData, whose one delta from the vendor tree
                            is loadAllNavigationDataOnTerrainLoad 1 (nav data loaded WITH the
                            scenario instead of lazily at first entity placement)
  --sample-threads          also run scripts/SampleThreads.ps1 against the sim
  --log PATH                runner stdout+stderr file         (default runs/launch52/RunScenario-<stamp>.log)
  --dry-run                 pass -DryRun to the runner (launches nothing)
  -h | --help               this text

Everything after "--" is appended to the runner's command line unchanged.
USAGE
}

while [ $# -gt 0 ]; do
    case "$1" in
        --profile)              PROFILE="$2"; shift 2 ;;
        --gui)                  NOGUI=0; shift ;;
        --no-gui)               NOGUI=1; shift ;;
        --scenario)             SCENARIO="$2"; shift 2 ;;
        --init)                 INIT="$2"; shift 2 ;;
        --order)                ORDER="$2"; shift 2 ;;
        --client-id)            CLIENT_ID="$2"; shift 2 ;;
        --type-map)             TYPEMAP="$2"; shift 2 ;;
        --run-secs)             RUN_SECS="$2"; shift 2 ;;
        --watch-secs)           WATCH_SECS="$2"; shift 2 ;;
        --backend-notify)       BACKEND_NOTIFY="$2"; shift 2 ;;
        --object-console)       OBJ_CONSOLE="$2"; shift 2 ;;
        --member-console)       MEMBER_CONSOLE="$2"; shift 2 ;;
        --position-report)      POS_REPORT="$2"; shift 2 ;;
        --rest-url)             REST_URL="$2"; shift 2 ;;
        --stomp-url)            STOMP_URL="$2"; shift 2 ;;
        --stop-when-complete)   STOP_WHEN_COMPLETE=1; shift ;;
        --no-stop-when-complete) STOP_WHEN_COMPLETE=0; shift ;;
        --env)                  EXTRA_ENV+=("$2"); shift 2 ;;
        --pre-order-settle)     PRE_ORDER_SETTLE="$2"; shift 2 ;;
        --vrf-appdata-dir)      VRF_APPDATA_DIR="$2"; shift 2 ;;
        --sample-threads)       SAMPLE_THREADS=1; shift ;;
        --log)                  LOG="$2"; shift 2 ;;
        --dry-run)              DRYRUN=1; shift ;;
        -h|--help)              usage; exit 0 ;;
        --)                     shift; while [ $# -gt 0 ]; do PASSTHRU+=("$1"); shift; done ;;
        *)                      echo "unknown option: $1"; echo; usage; exit 2 ;;
    esac
done

# ---- GATE: the 64-bit host, checked before anything is launched --------------
# The runner refuses a 32-bit host itself (exit 2); this is the same gate one layer
# out, so the failure is reported before a run directory or an appNumber is spent.
if [ ! -x "$PWSH64" ]; then
    echo "[FAIL] 64-bit pwsh not found at: $PWSH64"
    echo "       Bare 'pwsh' on this machine is the 32-BIT build and MUST NOT run the runner."
    exit 2
fi
if ! "$PWSH64" -NoProfile -Command '[Environment]::Is64BitProcess' < /dev/null 2>/dev/null | grep -qi '^true'; then
    echo "[FAIL] $PWSH64 is not a 64-bit PowerShell."
    exit 2
fi

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
if [ -z "$LOG" ]; then
    mkdir -p runs/launch52
    LOG="runs/launch52/RunScenario-$STAMP.log"
fi
: > "$LOG" || { echo "[FAIL] cannot write the runner log: $LOG"; exit 2; }

# ---- the app's environment ---------------------------------------------------
# One place, printed below, and recorded in the run manifest by the runner.
export Vrf__TypeMappingMode=FidelityTable
export Vrf__CreationPolicy=AtOrder
export Vrf__DeStackCreates=true
export Vrf__DeStackSpacingMeters=700
export Vrf__DeStackRotationDeg=0
export Vrf__DropOriginVertexMeters=100
export Vrf__TaskPredecessorTimeoutSeconds=7200
export Vrf__ObjectConsoleNotifyLevel="$OBJ_CONSOLE"
export Vrf__ObjectConsoleMemberNotifyLevel="$MEMBER_CONSOLE"
export Vrf__PositionReportSeconds="$POS_REPORT"
for kv in "${EXTRA_ENV[@]}"; do export "$kv"; done

# ---- the runner's command line ----------------------------------------------
ARGS=(-NoProfile -ExecutionPolicy Bypass -File scripts/RunC2SimScenario.ps1)
ARGS+=(-VrfProfile "$PROFILE")
[ "$NOGUI" -eq 1 ] && ARGS+=(-NoGui)
ARGS+=(-Scenario "$SCENARIO" -Init "$INIT" -Order "$ORDER")
[ -n "$CLIENT_ID" ] && ARGS+=(-ClientId "$CLIENT_ID")
[ -n "$TYPEMAP" ] && ARGS+=(-TypeMapFile "$TYPEMAP")
ARGS+=(-RunSecs "$RUN_SECS" -WatchSecs "$WATCH_SECS" -BackendNotifyLevel "$BACKEND_NOTIFY")
ARGS+=(-RestUrl "$REST_URL" -StompUrl "$STOMP_URL")
# Stage 7d. Always passed, never compared here: the runner validates the range (0..3600) and
# ledgers the value, so a typo is refused with a reason instead of silently dropped by bash.
ARGS+=(-PreOrderSettleSecs "$PRE_ORDER_SETTLE")
# Relocated appData. Passed ONLY when non-empty, so a default run's runner command line is
# byte-identical to every run in the record; the runner refuses a path that is not a directory
# and refuses the switch outright on the 5.0.2 profile.
[ -n "$VRF_APPDATA_DIR" ] && ARGS+=(-VrfAppDataDir "$VRF_APPDATA_DIR")
[ "$STOP_WHEN_COMPLETE" -eq 1 ] && ARGS+=(-StopWhenComplete)
[ "$DRYRUN" -eq 1 ] && ARGS+=(-DryRun)
ARGS+=("${PASSTHRU[@]}")

echo "=== RunScenario.sh $STAMP ==="
echo "  repo        : $REPO"
echo "  pwsh        : $PWSH64 (64-bit, pinned - bare 'pwsh' here is 32-bit)"
echo "  profile     : $PROFILE   scenario: $SCENARIO   gui: $([ "$NOGUI" -eq 1 ] && echo off || echo on)"
echo "  init/order  : $INIT | $ORDER   clientId: $CLIENT_ID"
echo "  type map    : ${TYPEMAP:-(repo default)}"
echo "  windows     : RunSecs=$RUN_SECS WatchSecs=$WATCH_SECS backendNotify=$BACKEND_NOTIFY"
echo "  pre-order   : PreOrderSettleSecs=$PRE_ORDER_SETTLE  (stage 7d hold before PushOrder; 0 = off)"
[ -n "$VRF_APPDATA_DIR" ] && echo "  appData     : $VRF_APPDATA_DIR  (-VrfAppDataDir -> LaunchVrf52 --appDataDir on sim + gui)"
echo "  consoles    : object=$OBJ_CONSOLE member=$MEMBER_CONSOLE positionReport=${POS_REPORT}s"
echo "  endpoints   : $REST_URL | $STOMP_URL"
echo "  runner log  : $LOG     (watch it with: tail -f '$LOG')"
echo "  NOTE        : do NOT run Stop-Process / taskkill sweeps while this window is open."
echo "                The run directory's runner.launched file holds the runner's PID;"
echo "                any cleanup MUST exclude it. See docs/RUNBOOK.md sec 0.5.14."
echo

# ---- optional: the thread sampler, as a DETACHED subshell --------------------
# Note the "< /dev/null": a background subshell that keeps stdin open holds a handle
# this script's children can inherit, which is defect 3 in the header all over again.
if [ "$SAMPLE_THREADS" -eq 1 ]; then
    SAMPLER_LOG="runs/launch52/RunScenario-$STAMP.sampler.log"
    SAMPLER_CSV="$(cygpath -w "$REPO/runs/launch52/RunScenario-$STAMP.threads.csv" 2>/dev/null || echo "runs/launch52/RunScenario-$STAMP.threads.csv")"
    ( for i in $(seq 1 120); do tasklist | grep -qi vrfSimHLA1516e && break; sleep 5; done
      "$PWSH64" -NoProfile -ExecutionPolicy Bypass -File scripts/SampleThreads.ps1 \
          -ProcessName vrfSimHLA1516e -MaxSec $((WATCH_SECS + 100)) -IntervalSec 5 \
          -OutFile "$SAMPLER_CSV" ) > "$SAMPLER_LOG" 2>&1 < /dev/null &
    echo "  thread sampler started (log: $SAMPLER_LOG)"
fi

# ---- THE RUN -----------------------------------------------------------------
# The run-directory POINTER (review of 374ea49, finding F10). The backstop below used to find
# the run directory by MTIME (ls -1dt runs/*_run | head -1), which names the NEWEST directory,
# not the one this script launched - and a write into another run directory (the watchdog's own
# runner.watchdog-ran, for one) flips that ordering. The runner writes the path of the run
# directory it creates into RUNDIR_POINTER; this script deletes it FIRST, so what is found
# afterwards is this run's or nothing at all.
RUNDIR_POINTER='runs/launch52/last-run-dir.txt'
rm -f "$RUNDIR_POINTER"

# stdout AND stderr to a FILE, stdin from /dev/null. No pipe. No tee. No job control.
"$PWSH64" "${ARGS[@]}" > "$LOG" 2>&1 < /dev/null
rc=$?

echo
echo "runner exit: $rc"
case "$rc" in
    0) echo "  0 = run complete, evidence collected (NOT a verdict - score the trace separately)" ;;
    2) echo "  2 = aborted at validation. Nothing was launched." ;;
    3) echo "  3 = failed after VR-Forces was up. Teardown ran; evidence is partial." ;;
    4) echo "  4 = TEARDOWN INCOMPLETE. VR-Forces and/or the interface may still be joined." ;;
    5) echo "  5 = unexpected terminating error. Same warning as 4." ;;
    127) echo "  127 = NOT 'command not found'. On this MSYS bash it means the child exited with a"
         echo "        HIGH-BIT (NTSTATUS-shaped) Windows exit code that MSYS does not map to a signal."
         echo "        The only SILENT one is 0xFFFFFFFF (-1), which is what TerminateProcess(handle,-1)"
         echo "        writes - i.e. .NET Process.Kill() / PowerShell Stop-Process. The other three"
         echo "        (0xC0000409, 0xC00000FD, 0xE0434352) are CLR fatal errors that print to stderr"
         echo "        AND raise a WER / Application Error event. So: 127 with a silent log and no WER"
         echo "        event means the runner was KILLED FROM OUTSIDE, not that it crashed."
         echo "        (0xC0000017 -> 138 'Bus error'; 0xC0000005 -> 139 'Segmentation fault'.)"
         echo "        Measured 2026-09-14: docs/experiments/RUNNER_HARDENING_2026-09-14.md sec 2." ;;
    *) echo "  (undocumented exit code - read $LOG)" ;;
esac

# ---- [BACKSTOP] the teardown the runner could not run ------------------------
# CONTRACT (both markers are written by scripts/RunC2SimScenario.ps1):
#   runner.launched      exists  => VR-Forces became this run's to stop. Its ABSENCE means
#                                   the run aborted at validation and touched nothing, so
#                                   this script must NOT tear anything down - a foreign
#                                   live session could be what is up (RUNBOOK sec 0).
#   runner.teardown-ran  exists  => the runner's finally completed. Nothing to do.
# launched AND NOT teardown-ran => the runner died where no finally could run. Tear down
# here, in the order the runner uses: StopIface (clean resign) THEN StopVrf, then signal
# the observers. Nothing is force-killed here either, ever.
RUNDIR=''
RUNDIR_SRC=''
if [ "$DRYRUN" -eq 1 ]; then
    # A dry run creates NO run directory, so there is nothing here that this script could
    # legitimately tear down - and the mtime fallback below would happily name a PREVIOUS,
    # killed run's directory and tear down on it (finding F10, second order).
    echo "  dry run: no run directory was created, so the wrapper backstop is not armed."
elif [ -f "$RUNDIR_POINTER" ]; then
    RUNDIR="$(tr -d '\r\n' < "$RUNDIR_POINTER")"
    RUNDIR="$(cygpath -u "$RUNDIR" 2>/dev/null || echo "$RUNDIR")"
    RUNDIR_SRC="pointer $RUNDIR_POINTER"
    if [ ! -d "$RUNDIR" ]; then
        echo "  [WARN] $RUNDIR_POINTER names $RUNDIR, which is not a directory. Falling back to the mtime scan."
        RUNDIR=''
        RUNDIR_SRC=''
    fi
fi
if [ "$DRYRUN" -eq 0 ] && [ -z "$RUNDIR" ]; then
    # Fallback only: no pointer (an older runner, a validation abort before the run directory
    # existed, or a failed write). It can name the WRONG directory - see finding F10 - so it
    # says so out loud.
    RUNDIR="$(ls -1dt runs/*_run 2>/dev/null | head -1)"
    RUNDIR_SRC='NEWEST runs/*_run by mtime (no pointer file - this can be the WRONG directory)'
fi
[ -n "$RUNDIR" ] && echo "  run directory : $RUNDIR   [$RUNDIR_SRC]"
if [ -n "$RUNDIR" ] && [ -f "$RUNDIR/runner.launched" ] && [ ! -f "$RUNDIR/runner.teardown-ran" ]; then
    # CLAIM THE TEARDOWN FIRST (review of 374ea49, finding F3). scripts/RunnerWatchdog.ps1 is a
    # second backstop on the same run directory and claims runner.watchdog-ran with CreateNew.
    # This script predated it and claimed nothing, so a kill that left this bash alive produced
    # TWO OVERLAPPING teardowns - this one's StopVrf52 still inside its 20 s front-end grace
    # when the watchdog's StopIface started. Every step is a graceful, idempotent request, so
    # that was benign, but it was never measured. noclobber (set -C) makes the create-or-fail
    # atomic, in a SUBSHELL so noclobber does not leak into the rest of this script.
    if ( set -C; : > "$RUNDIR/runner.watchdog-ran" ) 2>/dev/null; then
        printf 'claimed by RunScenario.sh pid %s at %s (runner pid %s exited %s without a completed teardown)\n' \
            "$$" "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$(cat "$RUNDIR/runner.launched" 2>/dev/null)" "$rc" \
            >> "$RUNDIR/runner.watchdog-ran"
    else
        echo
        echo "  runner.watchdog-ran is ALREADY CLAIMED in $RUNDIR - the detached watchdog"
        echo "  (scripts/RunnerWatchdog.ps1) got there first. STANDING DOWN: this script tears"
        echo "  NOTHING down. Read $RUNDIR/runner-watchdog.log for what it did."
        echo "  runner log: $LOG"
        exit $rc
    fi
    echo
    echo "*** THE RUNNER DID NOT RECORD A COMPLETED TEARDOWN (exit $rc). Tearing down from the wrapper. ***"
    echo "    run directory : $RUNDIR   [$RUNDIR_SRC]"
    echo "    runner pid was: $(cat "$RUNDIR/runner.launched" 2>/dev/null)"
    echo "    claimed       : $RUNDIR/runner.watchdog-ran (the detached watchdog stands down on it)"
    STOPIFACE='tools/StopIface/bin/Release/net10.0/StopIface.exe'
    if [ -x "$STOPIFACE" ]; then
        echo "    StopIface ..."
        "$STOPIFACE" "$REST_URL" "$STOMP_URL" --yes >> "$LOG" 2>&1 < /dev/null
        echo "      StopIface exit: $?   (0 ok; 1 the server did NOT reach UNINITIALIZED - the"
        echo "      interface MAY STILL BE JOINED; 2 usage)"
    else
        echo "    [FAIL] $STOPIFACE not found - the interface was NOT asked to resign."
        echo "           Build it, or run it by hand: StopIface.exe $REST_URL $STOMP_URL --yes"
    fi
    if [ "$PROFILE" = '5.2' ]; then STOPVRF='scripts/StopVrf52.ps1'; else STOPVRF='scripts/StopVrf.ps1'; fi
    echo "    $STOPVRF ..."
    "$PWSH64" -NoProfile -ExecutionPolicy Bypass -File "$STOPVRF" -TimeoutSec 120 >> "$LOG" 2>&1 < /dev/null
    echo "      StopVrf exit: $?   (0 down/already down; 3 still running - NOTHING killed; 5 unexpected)"
    touch "$RUNDIR/observers.stop"
    echo "    observers.stop touched - WatchVrf and ListenReports resign within ~1 s."
    echo "    Teardown output was appended to $LOG."
    echo "    rtiexec / rtiForwarder / rtiAssistant were NOT touched (RUNBOOK 0.5.2)."
    echo "    STILL RUNNING NOW:"
    tasklist 2>/dev/null | grep -Ei 'vrfSimHLA1516e|vrfGui|VrfC2SimApp|WatchVrf|ListenReports' || echo "      (none of ours)"
else
    if [ -n "$RUNDIR" ] && [ -f "$RUNDIR/runner.teardown-ran" ]; then
        echo "  teardown marker present in $RUNDIR - the runner tore down its own run."
    else
        echo "  no runner.launched marker in ${RUNDIR:-(no run directory found)} - nothing was"
        echo "  launched, so the wrapper tears NOTHING down (a foreign live session must never be touched)."
    fi
fi

echo
echo "  runner log: $LOG"
exit $rc
