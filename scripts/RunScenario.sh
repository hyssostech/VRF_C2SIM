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
# THE NEXT THREE ARE AO-SPECIFIC DEFAULTS (STP-802), and they are marked as such because the
# demo AO is no longer the Mojave: the scenario names a MOJAVE fixture and the init/order are
# the MOJAVE COA-STP1 pair. Run another AO by passing --scenario/--init/--order, or by exporting
# C2SIM_SCENARIO / C2SIM_INIT / C2SIM_ORDER - the command line still wins over the environment.
# Do NOT mix: a Suwalki scenario with a Mojave order authors legs on another continent (the V6c-
# V6f defect, memory lessons-order-coordinates-vs-init).
# Each of the three also records WHERE IT CAME FROM, and the runner does the same (review F6,
# 2026-09-20): 'argument --x', 'env var C2SIM_X' or 'built-in default'. A value that came from
# the environment is NOT passed on the runner's command line - the runner reads the same
# variable itself and reports it as the environment, so the provenance printed at the end of
# this script and the provenance in the run manifest are the SAME FACT, resolved once. A value
# that was typed here, or that is this script's own default, IS passed, so a default run's
# runner command line stays byte-identical to every run in the record.
# "Set" means the SAME thing here as it does in the runner, which tests
# [string]::IsNullOrWhiteSpace: a variable holding only spaces or tabs counts as UNSET in both.
# Without this the two resolvers disagree on exactly that value - bash would call " " set and
# export it, the runner would call it unset and fall back to ITS OWN default, which is a
# DIFFERENT AO from this script's - the silent AO substitution this whole item exists to close.
nonblank() { [ -n "$(printf '%s' "${1:-}" | tr -d '[:space:]')" ]; }
if nonblank "${C2SIM_SCENARIO:-}"; then SCENARIO="$C2SIM_SCENARIO"; SCENARIO_SRC='env var C2SIM_SCENARIO'
else SCENARIO='R9_Mojave_Empty_52_NavAO';           SCENARIO_SRC='built-in default'; fi
if nonblank "${C2SIM_INIT:-}";     then INIT="$C2SIM_INIT";         INIT_SRC='env var C2SIM_INIT'
else INIT='data/COA-STP1_Initialization.xml';       INIT_SRC='built-in default'; fi
if nonblank "${C2SIM_ORDER:-}";    then ORDER="$C2SIM_ORDER";       ORDER_SRC='env var C2SIM_ORDER'
else ORDER='data/COA-STP1_Order.xml';               ORDER_SRC='built-in default'; fi
CLIENT_ID='C2SIM'
TYPEMAP=''
RUN_SECS=900
WATCH_SECS=0          # 0 = DERIVE (the runner's own formula; see EFF_WATCH below)
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
PRE_ORDER_GATE=''
PRE_ORDER_GATE_TIMEOUT=300
VRF_APPDATA_DIR=''
PAUSE_AT=0
RESUME_AT=0
LOG=''
EXTRA_ENV=()
PASSTHRU=()

usage() {
    cat <<'USAGE'
usage: scripts/RunScenario.sh [options] [-- <extra runner arguments>]

  --profile 5.2|5.0.2       VR-Forces profile                 (default 5.2)
  --gui | --no-gui          front end on/off (5.2 only)       (default --no-gui)
  --scenario NAME           scenario name                     (default R9_Mojave_Empty_52_NavAO,
                            a MOJAVE fixture; or export C2SIM_SCENARIO)
  --init PATH               C2SIM initialization xml          (default data/COA-STP1_Initialization.xml,
                            a MOJAVE init; or export C2SIM_INIT)
  --order PATH              C2SIM order xml                   (default data/COA-STP1_Order.xml,
                            a MOJAVE order; or export C2SIM_ORDER)
  --client-id ID            must equal the init's SystemName  (default C2SIM)
  --type-map PATH           Vrf__TypeMapFile (WINDOWS path)   (default: the repo map)
  --run-secs N              observation window cap            (default 900)
  --watch-secs N            observer duration cap; 0 = DERIVE (default 0)
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
  --pre-order-gate nav-area stage 7d READY GATE: instead of a fixed hold, wait for the
                            SIMULATOR'S OWN "New Primary nav area" row and push the order the
                            moment it appears (default empty = off). Measured 2026-09-14: that
                            row lands 9-12 s after the first placement with a WARM file cache
                            and ~237 s COLD, and the runner logs which of the two this run was.
                            REQUIRES --object-console 3 or 4 (the row prints at level 3); the
                            runner refuses the gate at stage 0 below that.
  --pre-order-gate-timeout N  the gate's timeout, 30..1800 (default 300, which covers the cold
                            ~240 s). On timeout the run STOPS - unless --pre-order-settle N is
                            also given, which then becomes the fallback hold. GATE OR SETTLE.
  --pause-at N              stage 8b Q5 PROBE: at t+Ns of the observation window (measured from
                            PushOrder returning) run tools/PauseSim and PAUSE the scenario
                            (default 0 = off). Costs ONE ledgered appNumber.
  --resume-at M             stage 8b Q5 PROBE: at t+Ms RESUME it (controller->run()). M must be
                            GREATER than N when both are given (default 0 = off). A SEPARATE
                            join and therefore a SEPARATE appNumber - a pause and a resume are
                            never one number reused.
                            The KILL half of the Q5 probe is NOT automated: it is a manual
                            Stop-Process on the vrfSimHLA1516e pid, RUNBOOK 0.5.14 item 14.
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
        --scenario)             SCENARIO="$2"; SCENARIO_SRC='argument --scenario'; shift 2 ;;
        --init)                 INIT="$2";     INIT_SRC='argument --init';         shift 2 ;;
        --order)                ORDER="$2";    ORDER_SRC='argument --order';       shift 2 ;;
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
        --pre-order-gate)       PRE_ORDER_GATE="$2"; shift 2 ;;
        --pre-order-gate-timeout) PRE_ORDER_GATE_TIMEOUT="$2"; shift 2 ;;
        --vrf-appdata-dir)      VRF_APPDATA_DIR="$2"; shift 2 ;;
        --pause-at)             PAUSE_AT="$2"; shift 2 ;;
        --resume-at)            RESUME_AT="$2"; shift 2 ;;
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

# ---- THE MAK LICENCE, resolved from the REGISTRY and not from this shell -----
# The bash half of the rule the three .ps1 entry scripts carry (Resolve-MakLicenseFile;
# CHANGE ONE, CHANGE ALL FOUR). The licence renewed on 2026-09-14 is named in the USER
# scope; the MACHINE scope still names the old 15-sep-2026 file. A shell that was already
# open when that changed still EXPORTS the stale path to everything it starts - this
# wrapper, the runner, the sim, the interface, the observers - and an expired licence
# surfaces as a sim that dies at startup, not as a licence error. So resolve it here,
# through the pinned 64-bit pwsh, and export the answer. RUNBOOK 0.5.15.
# The runner resolves it again for itself: this export is for the sampler subshell and
# for anything else this script starts directly.
# ONLY field 5 of the first INCREMENT line (the expiry) is ever read out of the file.
lic_scope() { "$PWSH64" -NoProfile -Command "[Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','$1')" < /dev/null 2>/dev/null | tr -d '\r\n'; }
LIC="$(lic_scope User)"
[ -z "$LIC" ] && LIC="$(lic_scope Machine)"
LIC_EXPIRY='unknown'
if [ -n "$LIC" ]; then
    LIC_U="$(cygpath -u "$LIC" 2>/dev/null || echo "$LIC")"
    if [ -f "$LIC_U" ]; then
        # Exported ONLY when the file is really there - same rule as the .ps1 resolver, which
        # PRESERVES an inherited value rather than replacing it with a path that resolves to
        # nothing. Overwriting a working inherited value with a broken registry one would be a
        # new failure, invented here.
        export MAKLMGRD_LICENSE_FILE="$LIC"
        LIC_EXPIRY="$(awk '/^[[:space:]]*INCREMENT[[:space:]]/ { print $5; exit }' "$LIC_U")"
        [ -z "$LIC_EXPIRY" ] && LIC_EXPIRY='unknown - no INCREMENT line'
    else
        LIC_EXPIRY='THE FILE DOES NOT EXIST'
        echo "[WARN] *** the licence file does not exist: $LIC ***"
        echo "       That path came from the registry (User scope, else Machine). Nothing is stopped"
        echo "       here, but expect a licence failure in every MAK process. RUNBOOK 0.5.15."
    fi
else
    LIC_EXPIRY='NO PATH IN EITHER REGISTRY SCOPE'
    echo "[WARN] *** MAKLMGRD_LICENSE_FILE is empty in BOTH the User and the Machine scope -"
    echo "       every MAK process may HANG on its licence checkout. RUNBOOK 0.5.15. ***"
fi

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
if [ -z "$LOG" ]; then
    mkdir -p runs/launch52
    LOG="runs/launch52/RunScenario-$STAMP.log"
fi
: > "$LOG" || { echo "[FAIL] cannot write the runner log: $LOG"; exit 2; }

# ---- the stage-7d READY GATE ------------------------------------------------
# One gate exists. The wrapper spells it nav-area (this script's convention); the runner
# parameter is -PreOrderGate NavArea. An unknown name is a TYPO and is refused HERE as well
# as in the runner, so it can never fall through to an ungated push.
# GATE_BUDGET feeds the derived observer cap below for the same reason the settle does: the
# gate's worst case is its whole timeout, spent inside the observers' coverage.
PRE_ORDER_GATE_ARG=''
GATE_BUDGET=0
case "$PRE_ORDER_GATE" in
    ''|off|none)                 PRE_ORDER_GATE_ARG='' ;;
    nav-area|navarea|NavArea)    PRE_ORDER_GATE_ARG='NavArea'; GATE_BUDGET="$PRE_ORDER_GATE_TIMEOUT" ;;
    *)  echo "--pre-order-gate: unknown gate '$PRE_ORDER_GATE' (supported: nav-area)"; exit 2 ;;
esac
# The row the gate waits for is printed at object-console level 3 and nowhere else. The
# runner refuses the combination at stage 0; saying it here as well means the operator is
# told BEFORE a run directory or an appNumber is spent.
if [ -n "$PRE_ORDER_GATE_ARG" ] && [ "$OBJ_CONSOLE" -lt 3 ]; then
    echo "--pre-order-gate $PRE_ORDER_GATE needs --object-console 3 or 4; it is $OBJ_CONSOLE."
    echo "The 'New Primary nav area' row the gate waits for only prints at object-console level 3."
    exit 2
fi

# ---- the stage-8b Q5 PAUSE/RESUME PROBE -------------------------------------
# The runner validates these itself and refuses at stage 0; saying it here as well means the
# operator is told BEFORE a run directory or an appNumber is spent. A resume at or before the
# pause is the one ordering error that would leave the scenario PAUSED for the rest of the
# window, so it is refused rather than warned about.
case "$PAUSE_AT$RESUME_AT" in
    *[!0-9]*) echo "--pause-at / --resume-at take whole seconds (got '$PAUSE_AT' / '$RESUME_AT')."; exit 2 ;;
esac
if [ "$PAUSE_AT" -gt 0 ] && [ "$RESUME_AT" -gt 0 ] && [ "$RESUME_AT" -le "$PAUSE_AT" ]; then
    echo "--resume-at ($RESUME_AT) must be GREATER than --pause-at ($PAUSE_AT): both are offsets"
    echo "from the same instant (PushOrder returning), and resuming first would leave the scenario"
    echo "PAUSED for the rest of the observation window."
    exit 2
fi
if { [ "$PAUSE_AT" -gt 0 ] || [ "$RESUME_AT" -gt 0 ]; } && [ "$RUN_SECS" -gt 0 ]; then
    if [ "$PAUSE_AT" -ge "$RUN_SECS" ] || { [ "$RESUME_AT" -gt 0 ] && [ "$RESUME_AT" -ge "$RUN_SECS" ]; }; then
        echo "[WARN] a Q5 probe offset is at or beyond --run-secs $RUN_SECS, so that half can never"
        echo "       fire - and --stop-when-complete can close the window even earlier. The runner"
        echo "       flags a probe that never fired; its appNumber is burned either way."
    fi
fi

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

# ---- the observer duration, DERIVED HERE TOO --------------------------------
# --watch-secs 0 means "let the runner derive it", which the runner has always done
# (RunC2SimScenario.ps1: $EffWatchSecs = if ($WatchSecs -gt 0) { $WatchSecs } else
# { $DerivedWatchSecs }). It is now this script's DEFAULT: every explicit value passed on
# 2026-09-14 was BELOW the derived cap and earned the runner's truncation WARN
# (run 20260914T170824Z: 900 < 1100), which is exactly the failure the derivation exists
# to prevent. The same sum is computed here for two reasons only - to PRINT it in the
# banner, and to size the thread sampler below. It never reaches the runner: the runner
# derives its own, from its own parameter defaults.
#
# RunnerLib Get-DerivedWatchSecs = preRoll 20 + appJoin 180 + initDispatch 120
#                                + oracleGate 180 + pushOrderListen 30 + run + trail 30
# plus the stage-7d hold AND the stage-7d gate timeout, which the runner adds separately.
# CHANGE ONE, CHANGE BOTH: if a
# runner budget default moves, this sum is stale and only the banner is wrong (the runner
# still uses its own), but the sampler would then be sized off a stale number.
WATCH_FIXED=560
DERIVED_WATCH=$((WATCH_FIXED + RUN_SECS + PRE_ORDER_SETTLE + GATE_BUDGET))
if [ "$WATCH_SECS" -gt 0 ]; then
    EFF_WATCH="$WATCH_SECS"
    WATCH_NOTE="EXPLICIT $WATCH_SECS (derived would be $DERIVED_WATCH)"
    if [ "$WATCH_SECS" -lt "$DERIVED_WATCH" ]; then
        WATCH_NOTE="$WATCH_NOTE  *** BELOW the derived cap - the observers can end BEFORE the window does; pass --watch-secs 0 ***"
    fi
else
    EFF_WATCH="$DERIVED_WATCH"
    WATCH_NOTE="DERIVED $DERIVED_WATCH (20+180+120+180+30+run $RUN_SECS+30+settle $PRE_ORDER_SETTLE+gate $GATE_BUDGET)"
fi

# ---- the runner's command line ----------------------------------------------
ARGS=(-NoProfile -ExecutionPolicy Bypass -File scripts/RunC2SimScenario.ps1)
ARGS+=(-VrfProfile "$PROFILE")
[ "$NOGUI" -eq 1 ] && ARGS+=(-NoGui)
# SCENARIO / INIT / ORDER (review F6). Passed ONLY when this script did NOT take the value from
# the environment; an env-sourced value is EXPORTED instead and the runner resolves it itself,
# with the same precedence (argument > environment > built-in default), so the runner's Stage 0
# banner and its manifest can say "env var C2SIM_X" truthfully rather than calling it an
# argument. The export matters: a bare shell VARIABLE (never exported) is visible to the test
# above but would NOT reach the child, and the runner would then silently use ITS default.
case "$SCENARIO_SRC" in
    'env var'*) export C2SIM_SCENARIO="$SCENARIO" ;;
    *)          ARGS+=(-Scenario "$SCENARIO") ;;
esac
case "$INIT_SRC" in
    'env var'*) export C2SIM_INIT="$INIT" ;;
    *)          ARGS+=(-Init "$INIT") ;;
esac
case "$ORDER_SRC" in
    'env var'*) export C2SIM_ORDER="$ORDER" ;;
    *)          ARGS+=(-Order "$ORDER") ;;
esac
[ -n "$CLIENT_ID" ] && ARGS+=(-ClientId "$CLIENT_ID")
[ -n "$TYPEMAP" ] && ARGS+=(-TypeMapFile "$TYPEMAP")
ARGS+=(-RunSecs "$RUN_SECS" -WatchSecs "$WATCH_SECS" -BackendNotifyLevel "$BACKEND_NOTIFY")
ARGS+=(-RestUrl "$REST_URL" -StompUrl "$STOMP_URL")
# Stage 7d. Always passed, never compared here: the runner validates the range (0..3600) and
# ledgers the value, so a typo is refused with a reason instead of silently dropped by bash.
ARGS+=(-PreOrderSettleSecs "$PRE_ORDER_SETTLE")
# Stage 7d READY GATE. Passed ONLY when a gate was asked for, so a default run's runner
# command line stays byte-identical to every run in the record.
[ -n "$PRE_ORDER_GATE_ARG" ] && ARGS+=(-PreOrderGate "$PRE_ORDER_GATE_ARG" -PreOrderGateTimeoutSec "$PRE_ORDER_GATE_TIMEOUT")
# Relocated appData. Passed ONLY when non-empty, so a default run's runner command line is
# byte-identical to every run in the record; the runner refuses a path that is not a directory
# and refuses the switch outright on the 5.0.2 profile.
[ -n "$VRF_APPDATA_DIR" ] && ARGS+=(-VrfAppDataDir "$VRF_APPDATA_DIR")
# Stage 8b Q5 probe. Passed ONLY when armed, so a default run's runner command line stays
# byte-identical to every run in the record.
[ "$PAUSE_AT" -gt 0 ] && ARGS+=(-PauseAtSec "$PAUSE_AT")
[ "$RESUME_AT" -gt 0 ] && ARGS+=(-ResumeAtSec "$RESUME_AT")
[ "$STOP_WHEN_COMPLETE" -eq 1 ] && ARGS+=(-StopWhenComplete)
[ "$DRYRUN" -eq 1 ] && ARGS+=(-DryRun)
ARGS+=("${PASSTHRU[@]}")

echo "=== RunScenario.sh $STAMP ==="
echo "  repo        : $REPO"
echo "  pwsh        : $PWSH64 (64-bit, pinned - bare 'pwsh' here is 32-bit)"
echo "  licence file: ${LIC:-(none)} (expires $LIC_EXPIRY)"
echo "  profile     : $PROFILE   gui: $([ "$NOGUI" -eq 1 ] && echo off || echo on)   clientId: $CLIENT_ID"
echo "  scenario    : $SCENARIO"
echo "                <- $SCENARIO_SRC"
echo "  init        : $INIT"
echo "                <- $INIT_SRC"
echo "  order       : $ORDER"
echo "                <- $ORDER_SRC"
case "$SCENARIO_SRC$INIT_SRC$ORDER_SRC" in
    *'env var'*)
        echo "  *** ONE OR MORE OF scenario/init/order CAME FROM THE ENVIRONMENT, not from this command line."
        echo "      An exported C2SIM_SCENARIO / C2SIM_INIT / C2SIM_ORDER outlives the shell that set it. Check that"
        echo "      all three belong to the SAME AO before this run is scored: a Suwalki init with a Mojave scenario"
        echo "      authors legs on another continent and the back end's path job never returns (STP-823). Unset"
        echo "      them, or pass --scenario/--init/--order explicitly - an argument always wins over the environment."
        ;;
esac
echo "  type map    : ${TYPEMAP:-(repo default)}"
echo "  windows     : RunSecs=$RUN_SECS backendNotify=$BACKEND_NOTIFY"
echo "  observers   : $WATCH_NOTE"
if [ -n "$PRE_ORDER_GATE_ARG" ]; then
    echo "  pre-order   : READY GATE -PreOrderGate $PRE_ORDER_GATE_ARG  timeout ${PRE_ORDER_GATE_TIMEOUT}s  (stage 7d waits for the simulator's own 'New Primary nav area' row, then pushes at once; needs object console >= 3, it is $OBJ_CONSOLE)"
    if [ "$PRE_ORDER_SETTLE" -gt 0 ]; then
        echo "                FALLBACK on gate timeout: PreOrderSettleSecs=$PRE_ORDER_SETTLE (fixed hold; the gate is what is in force - gate OR settle, never both in sequence)"
    else
        echo "                on gate timeout the run STOPS (exit 3). Pass --pre-order-settle N to fall back to a fixed hold instead."
    fi
else
    echo "  pre-order   : PreOrderSettleSecs=$PRE_ORDER_SETTLE  (stage 7d hold before PushOrder; 0 = off; no READY GATE - see --pre-order-gate)"
fi
[ -n "$VRF_APPDATA_DIR" ] && echo "  appData     : $VRF_APPDATA_DIR  (-VrfAppDataDir -> LaunchVrf52 --appDataDir on sim + gui)"
if [ "$PAUSE_AT" -gt 0 ] || [ "$RESUME_AT" -gt 0 ]; then
    echo "  Q5 probe    : pause at t+${PAUSE_AT}s, resume at t+${RESUME_AT}s of the observation window (0 = that half off)"
    echo "                tools/PauseSim, ONE ledgered appNumber per half; the KILL half is MANUAL (RUNBOOK 0.5.14 item 14)"
fi
echo "  consoles    : object=$OBJ_CONSOLE member=$MEMBER_CONSOLE positionReport=${POS_REPORT}s"
echo "  endpoints   : $REST_URL | $STOMP_URL"
echo "  runner log  : $LOG     (watch it with: tail -f '$LOG')"
echo "  NOTE        : do NOT run Stop-Process / taskkill sweeps while this window is open."
echo "                The run directory's runner.launched file holds the runner's PID;"
echo "                any cleanup MUST exclude it. See docs/RUNBOOK.md sec 0.5.14."
echo

# ---- the run-directory POINTER, cleared BEFORE anything below (including the sampler)
# can read it (review of 374ea49, finding F10; moved earlier 2026-09-15 so the sampler
# subshell cannot race this script's own deletion and read a PREVIOUS run's stale pointer).
# The backstop further down used to find the run directory by MTIME (ls -1dt runs/*_run |
# head -1), which names the NEWEST directory, not the one this script launched - and a write
# into another run directory (the watchdog's own runner.watchdog-ran, for one) flips that
# ordering. The runner writes the path of the run directory it creates into RUNDIR_POINTER;
# this script deletes it FIRST, so what is found afterwards is this run's or nothing at all.
RUNDIR_POINTER='runs/launch52/last-run-dir.txt'
rm -f "$RUNDIR_POINTER"

# ---- optional: the thread sampler, as a DETACHED subshell --------------------
# Note the "< /dev/null": a background subshell that keeps stdin open holds a handle
# this script's children can inherit, which is defect 3 in the header all over again.
#
# THE ARTIFACT LANDED OUTSIDE EVERY RUN DIRECTORY (found 2026-09-15; RUNBOOK 0.5.11 item
# 16). The sampler DID run and DID write real rows - 188/185/219 for the three runs that
# flagged this (20260915T114001Z_run, 20260915T124231Z_run, 20260915T130627Z_run) - but at
# runs/launch52/RunScenario-<this script's own STAMP>.threads.csv, a stamp taken by THIS
# script before the runner is even started, while the runner computes its OWN run-directory
# stamp independently a few seconds later (scripts/RunC2SimScenario.ps1 ~line 1839,
# $stamp = $nowUtc.ToString('yyyyMMddTHHmmssZ')). Every one of the three was off by 1-4 s
# (114001Z_run <-> RunScenario-113733Z/114001Z, 124231Z_run <-> RunScenario-124230Z,
# 130627Z_run <-> RunScenario-130626Z): the file was never IN the run directory and the
# manifest never named it, so nobody who trusted either ever found it. FIXED: wait for the
# runner's OWN run-directory pointer (RUNDIR_POINTER above, written within the first few
# seconds of a live run - long before LaunchVrf, let alone the sim process the old tasklist
# poll waited for) and write straight into that directory as thread-samples.csv.
# SampleThreads.ps1 itself is UNCHANGED and still does its own internal wait for the sim
# process by name (default up to 600 s) before it writes a single data row, so this only
# moves WHERE the file lands, never WHEN sampling starts. Falls back to the historical
# flat-file location, with a printed WARN, if the pointer never appears (a validation abort
# before any run directory exists, or an older runner build) - the flag degrades instead of
# silently doing nothing.
if [ "$SAMPLE_THREADS" -eq 1 ]; then
    SAMPLER_LOG="runs/launch52/RunScenario-$STAMP.sampler.log"
    # -MaxSec IS SIZED FROM THE DERIVED WINDOW, NOT FROM $WATCH_SECS. It used to be
    # $((WATCH_SECS + 100)); with the new default of 0 that is 100 SECONDS and the sampler
    # would die before the order is even pushed - which is what happened to run
    # 20260914T164906Z under an explicit 0 (RUNNER_HARDENING sec 14, defect 2).
    # SampleThreads starts its clock when the SIM APPEARS - before the observers start and
    # long before the order - and it exits on its own when the sim exits, so the budget is
    # the whole run from that moment plus every teardown budget, and being generous costs
    # nothing:
    #   EFF_WATCH                      the observers' own cap (derived above)
    #   + 75   launchSettle 45 + preCheck 30   spent BEFORE the observers start
    #   + 360  traceStopGrace 120 + appExit 120 + stopVrf 120   teardown, after the window
    #   + 100  margin (the historical constant)
    SAMPLER_MAX=$((EFF_WATCH + 75 + 360 + 100))
    if [ "$DRYRUN" -eq 1 ]; then
        # A dry run launches no sim (and no run directory), so the sampler would spend ten
        # minutes looking for one - and, worse, would ATTACH TO A SIM ANOTHER LANE IS
        # RUNNING. Say what it would do.
        echo "  thread sampler: WOULD start SampleThreads.ps1 -ProcessName vrfSimHLA1516e -MaxSec ${SAMPLER_MAX}s -IntervalSec 5, writing <runDir>/thread-samples.csv once the runner creates its run directory (not started: --dry-run)"
    else
        ( SAMPLER_RUN_DIR=''
          for i in $(seq 1 60); do
              if [ -s "$RUNDIR_POINTER" ]; then
                  cand="$(tr -d '\r\n' < "$RUNDIR_POINTER")"
                  cand="$(cygpath -u "$cand" 2>/dev/null || echo "$cand")"
                  if [ -d "$cand" ]; then SAMPLER_RUN_DIR="$cand"; break; fi
              fi
              sleep 2
          done
          if [ -n "$SAMPLER_RUN_DIR" ]; then
              SAMPLER_CSV="$(cygpath -w "$SAMPLER_RUN_DIR/thread-samples.csv" 2>/dev/null || echo "$SAMPLER_RUN_DIR/thread-samples.csv")"
          else
              echo "SampleThreads: $RUNDIR_POINTER never appeared after 120s - falling back to runs/launch52 (this run's own artifact will be hard to find by stamp alone)"
              SAMPLER_CSV="$(cygpath -w "$REPO/runs/launch52/RunScenario-$STAMP.threads.csv" 2>/dev/null || echo "runs/launch52/RunScenario-$STAMP.threads.csv")"
          fi
          "$PWSH64" -NoProfile -ExecutionPolicy Bypass -File scripts/SampleThreads.ps1 \
              -ProcessName vrfSimHLA1516e -MaxSec "$SAMPLER_MAX" -IntervalSec 5 \
              -OutFile "$SAMPLER_CSV" ) > "$SAMPLER_LOG" 2>&1 < /dev/null &
        SAMPLER_BG_PID=$!
        echo "  thread sampler started (pid $SAMPLER_BG_PID), -MaxSec ${SAMPLER_MAX}s (log: $SAMPLER_LOG; csv lands in the run directory as thread-samples.csv once the runner creates it)"
    fi
fi

# ---- THE RUN -----------------------------------------------------------------
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
    6) echo "  6 = BACK-END WS RUNAWAY ABORT (RUNBOOK 0.5.11 item 17 extension). Teardown ran;"
       echo "      see preflight.wsRunaway in the run's manifest for the alerts that triggered it." ;;
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

# ---- thread sampler: wait for it, then record the artifact in the manifest ---------------
# SampleThreads.ps1 notices the sim is gone within one -IntervalSec (5s) of StopVrf tearing
# it down above and writes its own "process gone" row before exiting, so this is normally a
# few-second wait, never the sampler's full -MaxSec budget (which is deliberately generous -
# see the comment above SAMPLER_MAX).
if [ "$SAMPLE_THREADS" -eq 1 ] && [ -n "${SAMPLER_BG_PID:-}" ] && [ -n "$RUNDIR" ]; then
    for i in $(seq 1 12); do
        kill -0 "$SAMPLER_BG_PID" 2>/dev/null || break
        sleep 5
    done
    CSV_U="$RUNDIR/thread-samples.csv"
    if [ -f "$CSV_U" ]; then
        CSV_W="$(cygpath -w "$CSV_U" 2>/dev/null || echo "$CSV_U")"
        if [ -f "$RUNDIR/run-manifest.json" ]; then
            MANIFEST_W="$(cygpath -w "$RUNDIR/run-manifest.json" 2>/dev/null || echo "$RUNDIR/run-manifest.json")"
            "$PWSH64" -NoProfile -Command "\$p='$MANIFEST_W'; \$m = Get-Content -LiteralPath \$p -Raw | ConvertFrom-Json; \$m.artifacts | Add-Member -NotePropertyName threadSamples -NotePropertyValue '$CSV_W' -Force; [System.IO.File]::WriteAllText(\$p, (\$m | ConvertTo-Json -Depth 12), (New-Object System.Text.UTF8Encoding(\$false)))" < /dev/null > /dev/null 2>&1
            echo "  thread sampler: $CSV_W (recorded in the manifest as artifacts.threadSamples)"
        else
            echo "  thread sampler: $CSV_W (manifest not found - path not recorded)"
        fi
        # ---- WS runaway tripwire: SampleThreads.ps1 (RUNBOOK 0.5.11 item 17) writes ONE
        # line per confirmed episode to the sidecar <csv base>.alerts.txt beside thread-
        # samples.csv. Non-empty file = at least one confirmed runaway; surface it loudly
        # and record it in the manifest the same way artifacts.threadSamples is recorded
        # above - nothing here kills or touches the back end, this only reports.
        ALERTS_U="${CSV_U%.csv}.alerts.txt"
        if [ -s "$ALERTS_U" ]; then
            ALERTS_W="$(cygpath -w "$ALERTS_U" 2>/dev/null || echo "$ALERTS_U")"
            ALERTS_FIRST="$(head -n 1 "$ALERTS_U")"
            echo
            echo "  [WARN] BACK-END WS RUNAWAY detected: $ALERTS_FIRST"
            echo "         full alert log: $ALERTS_W"
            if [ -f "$RUNDIR/run-manifest.json" ]; then
                MANIFEST_W="$(cygpath -w "$RUNDIR/run-manifest.json" 2>/dev/null || echo "$RUNDIR/run-manifest.json")"
                "$PWSH64" -NoProfile -Command "\$p='$MANIFEST_W'; \$m = Get-Content -LiteralPath \$p -Raw | ConvertFrom-Json; \$m.artifacts | Add-Member -NotePropertyName threadSampleAlerts -NotePropertyValue '$ALERTS_W' -Force; \$m | Add-Member -NotePropertyName backendWsRunaway -NotePropertyValue \$true -Force; [System.IO.File]::WriteAllText(\$p, (\$m | ConvertTo-Json -Depth 12), (New-Object System.Text.UTF8Encoding(\$false)))" < /dev/null > /dev/null 2>&1
                echo "  thread sampler: $ALERTS_W (recorded in the manifest as artifacts.threadSampleAlerts, backendWsRunaway=true)"
            fi
        fi
    else
        echo "  [WARN] thread sampler: no thread-samples.csv in $RUNDIR - check $SAMPLER_LOG"
    fi
fi

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
