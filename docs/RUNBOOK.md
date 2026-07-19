# RUNBOOK - operating the c2simVRFinterface against VR-Forces

Hard-won runtime procedure. Read this BEFORE any run; do NOT re-derive it (a whole
session was burned rediscovering the pieces below). Companion to START_HERE.md
(build) and PORT.md sec 4 (environment). ASCII-only.

## 0. The single most important rule

NEVER force-kill a JOINED interface (`Stop-Process -Force`, `taskkill /F`). It does
not resign from the RTI, so it leaves a STALE FEDERATE; the next interface start then
HANGS at RTI join (1 thread, ~0 CPU, log frozen at the config banner). Recovery is
AUTOMATED - use `tools/ResetVrf` (sec 8), or bring VR-Forces down and up again with
scripts/StopVrf.ps1 + scripts/LaunchVrf.ps1 (sec 0.5), both unattended.
*** CORRECTED 2026-07-18: this line used to read "The only recovery for a stale
federate is a manual VR-Forces scenario reload in the GUI." THAT IS FALSE and it is
the FIRST substantive claim in this runbook, so it mislead every reader who started
here. NOTHING in the recovery path requires a human or a GUI. ***
Stop the interface CLEANLY instead (sec 4) - it resigns, leaves no stale federate,
and needs no reload. Force-kill + reload was the old clunky path (pre- and
post-compaction); the clean stop replaces it.

## 0.5 BRINGING VR-FORCES UP FOR LIVE WORK - THE WORKING PROCEDURE

Status: SOLVED 2026-07-18. VR-Forces launches UNATTENDED, zero human interaction,
and the WatchVrf movement oracle is verified end to end. This section is the
operational procedure. Narrative and the five wrong diagnoses that preceded it:
docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md. Gate result:
docs/experiments/PREREG_0_4_SELFLAUNCH.md sec 12.

READ THIS WHOLE SECTION BEFORE LAUNCHING. Two rules below (the never-kill rule and
the do-not-set rule) each cost a full live session when violated.

### 0.5.0 PRE-FLIGHT: INVENTORY WHAT IS ALREADY RUNNING (added 2026-07-18 evening)

A VR-Forces instance launched by an EARLIER SESSION KEEPS RUNNING across a context
clear or session boundary. Observed 2026-07-18: a healthy back-end (vrfSimHLA1516e,
63 threads) plus vrfGui were already up at session start, left by the post-sweep
LaunchVrf regression run (3491/3492). Inventory BEFORE launching:

    Get-Process | Where-Object { $_.ProcessName -match 'vrf|rti' } |
      Select-Object ProcessName,Id,@{n='Threads';e={$_.Threads.Count}},StartTime
    Get-CimInstance Win32_Process -Filter "Name='vrfSimHLA1516e.exe' OR Name='vrfGui.exe'" |
      ForEach-Object { $_.ProcessId; $_.CommandLine }

The command line carries `--appNumber` and `--scenarioFileName`, which identify the
run that left it behind by cross-reference to the Appendix B ledger.

WHY THIS MATTERS: a stale-but-healthy back-end interacts with a known LaunchVrf.ps1
defect - the script picks the back-end with `Select-Object -First 1` and never
correlates to the process it launched, so with `-AllowExistingVrf` it can measure the
OLD instance and report a FALSE READY. The leftover's scenario contents are also
unknown (a throwaway entity from a tools/CreateOne oracle check may still be in it),
which would contaminate a scored baseline trace.

THE SCRIPT ALREADY GUARDS THIS, so teardown is REQUIRED and not merely preferred:
LaunchVrf.ps1 lines 242-258 REFUSE to launch when vrfLauncher / vrfSimHLA1516e /
vrfGui are already running ("Refusing to launch on top of existing VR-Forces
processes", hardFail). The only override is `-AllowExistingVrf` - which is precisely
the false-READY trap above. DO NOT reach for that switch to get past a leftover;
shut the leftover down instead. (The guard deliberately does NOT look at rtiAssistant
/ rtiexec / rtiForwarder, so leaving RTI infrastructure up does not trip it.)

DEFAULT: prefer a FRESH launch on fresh ledgered appNos over reusing an instance of
unknown scenario state. Tear the leftover down with scripts/StopVrf.ps1 (sec 0.5.9 -
do NOT merely "close the front-end"; that raises a modal confirm and blocks) and leave
rtiAssistant / rtiexec / rtiForwarder RUNNING (sec 0.5.2).

KNOWN COSMETIC DEFECT (not fixed - the launch script is not being edited immediately
before a live session): the warning at line 248 says "VR-Forces/RTI processes ALREADY
running" but the check covers only the three VR-Forces process names, never any rti*
process. The text overstates what the code inspects.

### 0.5.1 THE COMMAND

    $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
    pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise `
         -BackendAppNumber <fresh> -FrontendAppNumber <fresh>

Expect `EXIT=0` and `[OK] READY`. Takes ~35 s to scenario-loaded. Both app numbers
are MANDATORY, must be FRESH, must differ, and must be LEDGERED IN
OPUS_EXECUTION_PLAN.md Appendix B BEFORE the launch.
WHAT THE SCRIPT ACTUALLY ENFORCES: presence, positivity, and that the two differ
(hard exit 2). WHAT IT CANNOT ENFORCE: FRESHNESS and LEDGERING - it never reads
the ledger. Reuse is YOUR error to avoid; its symptom is the stale-federate join
hang (sec 0). Take the value from the single "*** NEXT FREE:" marker in Appendix B
and advance it. Verified on 2026-07-18: TWO clean end-to-end script runs (3484/3485 and
3486/3487), both EXIT=0 with no human interaction. A THIRD script run (3482/3483)
FAILED and is what exposed two of the script's defects - do not count it as a pass.

What the script actually runs (if you ever need it by hand, cwd = bin64):

    vrfLauncher.exe --usePredefinedConnection "HLA 1516 Evolved RPR 2.0 with MAK extensions" ^
      --simArgs --appNumber <backendAppNo> --scenarioFileName "../userData/scenarios/TropicTortoise.scnx" ^
      --guiArgs --appNumber <frontendAppNo>

The `--simArgs` / `--guiArgs` overrides are SAFE and documented (vrfLauncher Table 9
worked example). They were wrongly blamed twice on 2026-07-18 and are EXONERATED -
do not re-accuse them.

### 0.5.2 *** NEVER KILL rtiAssistant / rtiexec / rtiForwarder ***

These are RTI INFRASTRUCTURE. They persist across launches BY DESIGN and are not
stale federates. An ALREADY-ANSWERED rtiAssistant is precisely what makes
unattended launch work.

Killing a long-lived rtiAssistant as "cleanup" is what broke the 2026-07-18
session and cost the entire live window. Every VR-Forces launch starts its OWN
assistant; when one is already running, the new one fails to bind port 6003 and
exits immediately. THAT FAILURE IS EXPECTED AND BENIGN: the already-answered
assistant keeps serving the connection, so no UNANSWERED dialog ever blocks
startup. The error dialog "RTI Assistant server creation failed. The port [ 6003 ]
may be in use" IS NORMAL and is not a fault to fix - BUT IT DOES APPEAR ON SCREEN,
one per launch, and needs dismissing or ignoring. On 2026-07-18 a stack of these
is what prompted the user to stop VR-Forces mid-session. They are cosmetic; do NOT
"fix" them by killing the assistant.

Corollary: do not "tidy" processes you did not start. Check what a process IS
before deciding it is stale.

### 0.5.3 ONE-TIME SETUP PER MACHINE (already done on this machine)

On HLA the vendor's documented startup sequence REQUIRES a human once. VR-Forces
help, SharedTopics\XMLrti\InstallMAK-RTI.htm, verbatim:

    "Start the application. The RTI Assistant will prompt you to choose an RTI
     configuration. Choose a configuration. If necessary, start the rtiexec.
     Click Connect. The application should run."

MAK RTI Users Guide p. 4-2 names the symptom when it is not answered, verbatim:
"The federate startup process may appear to hang while the Choose RTI Connection
dialog box is waiting for input." THIS IS DOCUMENTED BEHAVIOUR, NOT A BUG.

Procedure, once per machine (and possibly once per reboot - UNTESTED):
1. Launch (0.5.1). The "Choose RTI Connection" dialog appears.
2. Select the connection. On this machine: "Legatus's predefined rtiexec loopback
   connection". Local TCP Interface 127.0.0.1, Local UDP Interface 127.0.0.1.
3. ENSURE "Always try to use this connection" IS CHECKED.
4. Click Connect.
5. LEAVE THAT ASSISTANT RUNNING (see 0.5.2).

Thereafter launches are silent - verified: no dialog appeared at any point across
a 35 s poll on the next launch.

NOTE: the checkbox alone is not sufficient if no answered assistant exists. It was
already checked on 2026-07-18 and the dialog still prompted, because the answered
assistant had been killed. The checkbox persists the CHOICE; the running answered
assistant is what serves it.

### 0.5.4 AUTOMATING THE DIALOG (if it ever returns, e.g. after a reboot)

The dialog is a Qt window (class `Qt5QWindowIcon`) and exposes NO UI Automation
child tree - `AutomationElement.FindFirst` returns the window and nothing inside
it, so element-based automation FAILS. What works, used successfully 2026-07-18:

1. Find the window: process `rtiAssistant` with MainWindowTitle
   "Choose RTI Connection".
2. `GetWindowRect` it. SCREENSHOT it (`Graphics.CopyFromScreen`) and LOOK at the
   image before clicking - do not click coordinates blind.
3. Click Connect: on the observed 573x583 dialog the Connect button centre is at
   window-relative (383, 553); convert to screen coords and use `SetCursorPos` +
   `mouse_event` (down 0x0002, up 0x0004).
4. ALLOW MORE THAN 3 SECONDS for it to dismiss. A 3 s check reported "still open"
   on a click that had in fact succeeded - do not conclude failure and re-click.

### 0.5.5 *** DO NOT SET RTI_ASSISTANT_DISABLE ***

RTI Reference Manual sec 5.2.10 documents it and it does exactly what it says -
processes start in 8 seconds with no prompt. DO NOT USE IT ANYWAY.

With the Assistant disabled, nothing supplies the connection configuration:
federates join the federation but NEVER DISCOVER EACH OTHER. WatchVrf reported
`reflected=0 readable=0` for a full 40 s against a back-end whose own log proved
the scenario was loaded. THE MOVEMENT ORACLE GOES SILENTLY BLIND - a full Phase-1
session would produce empty telemetry with no error anywhere.

Mechanism (RefMan Appendix A, verbatim): `RTI_mcastDiscoveryEnabled` "is set to 0
unless RTI_configureConnectionWithRid is set to 1", and our rid.mtl sets
`RTI_configureConnectionWithRid 0`, so the Assistant's stored connection is the
only source of connection values. The Assistant does TWO jobs - prompting
(unwanted) and supplying the connection (REQUIRED). Disabling it drops both.

DO NOT edit the shared C:\MAK\makRti4.6.1\rid.mtl to work around this. If
rid.mtl values are ever genuinely needed, do it PROCESS-SCOPED: copy rid.mtl to a
private directory, set `RTI_configureConnectionWithRid 1` in THE COPY, and point
only our processes at it via `RTI_CONFIG` (documented in InstallMAK-RTI.htm:
"set the environment variable RTI_CONFIG to the directory that contains them").
Note RefMan requires all federates in a federation to agree on `RTI_useRtiExec`.

### 0.5.6 IS THE BACK-END ACTUALLY UP? - HEALTH ORACLE

PROCESS PRESENCE IS NOT HEALTH. A blocked back-end sits at 2-4 threads,
fully present, indefinitely.

USE: back-end THREAD COUNT. Blocked 2-4; healthy 23-67 (observed). This is the
connection-independent signal and is what LaunchVrf.ps1 gates on.
CORROBORATE WITH: vrfSim.log progressing past the VR-Link/MSVC banner to
"Loading parameter database file ...", sensor propagators, and finally
"Successfully loaded scenario." (log is BLOCK-BUFFERED per sec 3 - it corroborates,
it never proves).

DO NOT USE, all three shown wrong on 2026-07-18:
- process presence (see above);
- `rtiexec` presence - CONNECTION-DEPENDENT. Under the correct rtiexec loopback
  connection an rtiexec IS running; under the disabled/unanswered fallback none
  appears. Never gate readiness on it.
- `UDP 4000 bound` - CONNECTION-DEPENDENT and FALSE on a healthy back-end under
  the correct connection (which uses TCP 4001 + forwarder 5000). UDP 4000 appears
  only on the lightweight fallback.

### 0.5.7 IS THE ORACLE ACTUALLY SEEING ANYTHING? - MANDATORY PRE-CHECK

RUN THIS BEFORE ANY SCORED LIVE WORK. A launch can be perfectly healthy while the
oracle is blind (0.5.5), and nothing warns you.

    WatchVrf.exe <freshAppNo> 30 2      # cwd bin64, RTI env per sec 7

*** THE OLD PASS/FAIL CRITERION WAS WRONG IN BOTH DIRECTIONS. CORRECTED 2026-07-18
(evening), from live evidence on appNos 3455 / 3496 / 3499 / 3500. *** It used to
read: "REQUIRE reflected>0 ... if it is still 0 after 20 s, STOP; do not run the
session." Both halves failed live, in OPPOSITE directions:

FALSE GREEN - `reflected>0` PASSES ON PURE GARBAGE. On appNo 3455 the pre-check
reported `reflected=3 readable=2` and PASSED, while BOTH readable objects were
degenerate for all 14 samples:

    POS,...,cde66adc-...,90.000000,-90.000000,0.0     <- pole, i.e. no position
    POS,...,f864e51f-...,NaN,-90.000000,NaN           <- NaN lat and alt

*** CORRECTION 2026-07-19: "the baseline objects are POSITIONLESS" WAS AN ARTEFACT
OF A BAD CAST IN OUR OWN CODE, NOT A FACT ABOUT VR-FORCES. *** Those degenerate
readings were undefined behaviour: `resolveStateRep` ended in a blind
`static_cast<DtReflectedEntity*>(obj)->entityStateRep()`, and the TropicTortoise
baseline objects are CONTROL OBJECTS (`DtReflectedControlObject`), whose state
repository is a `DtEnvironmentProcessRepository`, not a
`DtBaseEntityStateRepository`. The cast dispatched to the same vtable slot on a
disjoint hierarchy, so `location()` returned garbage (90/-90, NaN, and in one run
an altitude of 1.02e15 m) and `lastSetLocation()` faulted outright (0xC0000005 -
this is what crashed `TryGetEntityMotion`). Header evidence and the fix are in the
long comment above `resolveStateRep` in `src/VrfFacade/VrfFacade.cpp`. The scenario
.oob does give the Page-In Area a real authored position (34.615N, -116.55W).

*** RETRACTED 2026-07-19 LATE - THIS "FIXED" CLAIM IS FALSE. THE FIX WENT OUT WITH A
REVERT AND WAS NEVER RE-APPLIED. *** The paragraph below was written at 15:32 describing
code that was REVERTED at 16:40 (commit 5d14eda) because it broke object creation. It then
survived a later RUNBOOK edit untouched. VERIFY BEFORE BELIEVING ANY OF IT:
  - src/VrfFacade/VrfFacade.cpp:735 STILL CONTAINS the blind
    `static_cast<DtReflectedEntity*>(obj)->entityStateRep()`. The undefined behaviour is
    STILL IN THE SHIPPING BRIDGE.
  - `resolveStateRep` DOES NOT EXIST in any tracked source (0 hits). Every reference to it
    in this RUNBOOK points at a function that went out with the revert. It survives only in
    stale untracked build artifacts.
  - Control objects STILL EMIT DEGENERATE POS LINES. The most recent pre-check
    (runs/20260719T222134Z_run/watchvrf-precheck.csv) has 28 of them,
    `POS,3,VRF_UUID:cde66adc-...,NaN,-90.000000,NaN`.
WHAT SURVIVES, because it is implementation-independent: the DIAGNOSIS above (control
objects sit on a disjoint repository hierarchy; the blind cast is UB on them; that is why
location() returned garbage while lastSetLocation() faulted) and the fact that "the
baseline objects are positionless" is IMPRECISE, not simply an artefact (re-verified from the .oob 2026-07-19): 2 of the 3 baseline objects (GlblTerrDmg, GlobalEnv) genuinely sit at ECEF (6378137,1,1) = null island and ARE positionless as claimed. Only the Page-In Area has a real authored position (34.615N/-116.55W) and reflects as 90/-90 because of the bad cast. Do not flatten this in either direction. The wording below called it simply an artefact rather than VR-Forces
behaviour. What is FALSE is only the claim that it was fixed.
IF YOU ARE PLANNING THE NATIVE RE-ATTEMPT: the crash source is NOT already removed. Scope
accordingly. Read HANDOFF_2026-07-19.md sec 5 for the rules first.

SUPERSEDED TEXT FOLLOWS - the claim in this paragraph is the false one just retracted:
FIXED: those objects are now identified positively and return FALSE (no reading) -
they no longer emit POS lines at all. The pass/fail criterion below is UNAFFECTED
in substance (a degenerate row and a missing row are both "not a real coordinate"),
but the FALSE-GREEN shape has changed: you will now see a LOWER `readable` count on
a stock load rather than degenerate POS rows. Judge on real coordinates, as below.
NOTE these objects are not truly positionless - `DtEnvironmentProcessRepository`
does expose `location()`; reading it would need a control-object-aware accessor that
the bridge does not yet have. Nobody has ever seen their real position.

A count of discovered objects is NOT evidence the oracle can read a position.
Trusting `reflected>0` here would have green-lit a whole session.

FALSE ABORT - `reflected=0 at 20 s` DOES NOT MEAN THE FEDERATION IS BLIND. It also
means "measured too early". LaunchVrf's `READY` is thread-count + main-window only;
it does NOT imply scenario loaded or federation joined (the script says so itself).
Observed settle times after launch, same script, same scenario, nothing else varied:

    appNo 3455  ~40 s after launch   -> reflected=3   (visible)
    appNo 3499  ~20-50 s after launch -> reflected=0  (blind - would have STOPPED)
    appNo 3500  ~104 s after launch  -> reflected=3   (visible; SAME federation)

The 20 s abort rule would have killed a perfectly healthy session. Settle time is
VARIABLE and exceeded 50 s on a normal launch.

THE CORRECTED CRITERION - require a REAL COORDINATE, and be patient about it:

  PASS  = at least one POS line whose lat/lon are real numbers, not NaN, and not the
          90.000000,-90.000000 pole placeholder.
  RETRY = `reflected=0`, or every readable object degenerate. Wait and re-run with a
          FRESH ledgered appNo. Allow up to ~3 MINUTES from launch before concluding
          anything. Do not judge a launch blind at 20 s.
  STOP  = still no real-coordinate POS after ~3 min AND a CreateOne entity also fails
          to appear with real coordinates (below). That is a genuinely blind oracle.

BECAUSE THE BASELINE OBJECTS ARE POSITIONLESS, THE "STRONGER CHECK" BELOW IS IN
PRACTICE THE ONLY CHECK THAT CAN PASS ON A STOCK TropicTortoise LOAD. Treat it as
mandatory, not optional.

STRONGER CHECK (settles position fidelity, not just discovery):

    CreateOne.exe <freshAppNo>          # creates one M1A2 at a COA-STP1 AO coord

then confirm WatchVrf emits a POS line for that uuid with REAL coordinates, not
NaN. Expected shape (verified 2026-07-18):

    POS,23.5,VRF_UUID:adfaadb3-...,34.517156,-116.973525,1060.7

Requested altitude was 10000 m MSL; 1060.7 is the GROUND CLAMP working. Relaunch
afterwards so the throwaway never enters a scored trace (a relaunch reloads the
scenario from file and removes it).

CAUTION: a freshly loaded TropicTortoise contains only NON-ENTITY CONTROL OBJECTS
(GlblTerrDmg, GlobalEnv, Blocking Terrain Page-In Area). Since 2026-07-19 they emit
NO POS line at all (see the correction above; before that they emitted garbage rows
that looked like data). Do NOT conclude the oracle is broken from their absence -
create a real entity and check that.

### 0.5.7a WATCHVRF TRACE LINE TYPES

One process, one stream, ONE CLOCK BASE (elapsed seconds from the same UTC start),
so every line type is directly comparable on a single timeline. Records are
interleaved and read LINE BY LINE; each record is exactly one physical line.
Field 0 is the type tag. This list is APPEND-ONLY - POS and CON have never changed
shape and consumers may rely on that.

    POS,<t>,<uuid>,<latDeg>,<lonDeg>,<altM>          per object, per sample tick
    RAW,<t>,<uuid>,<rawLat>,<rawLon>,<rawAlt>,<velX>,<velY>,<velZ>
                                                     un-extrapolated state, paired
                                                     with each POS  (added 2026-07-19)
    CON,<t>,<uuid>,<notifyLevel>,<message>           Object Console warning
    BCON,<t>,<simAddress>,<notifyLevel>,<message>    backend console  (added 2026-07-19)
    TSK,<t>,<unitMarking>,<taskType>                 task completion  (added 2026-07-19)
    RPT,<t>,<text>                                   radio text-report (added 2026-07-19)
    # ...                                            human-readable comment/summary

Worked examples:

    POS,23.5,VRF_UUID:adfaadb3-...,34.517156,-116.973525,1060.7
    RAW,23.5,VRF_UUID:adfaadb3-...,34.517091,-116.973498,1060.7,1.412,-0.233,0.008
    CON,12.5,1:1:0:2001,1,"route failed, ""no path"""
    BCON,8,1:3201,1,"backend warn, ""x"""
    TSK,31,"A Co, 1st","move-along"
    RPT,7.25,"POSITION ""tank1"" 39.0 -76.0"

POS vs RAW - WHY BOTH. POS reports `location()`, the position computed THROUGH
VR-Link's dead-reckoning approximator. RAW reports `lastSetLocation()` and
`lastSetVelocity()`, the values VR-Forces last actually SENT, with no extrapolation
(baseEntityStateRepository.h:118 and :133 - both declared on the BASE state
repository, so they resolve for entities and aggregates alike). The two lines carry
the same `t` and the same `uuid` and use identical lat/lon/alt formatting (F6/F6/F1),
so they are comparable digit-for-digit. Sustained divergence indicts the
approximator; agreement exonerates it and points upstream. RAW is emitted only
after a successful POS line for that object, never instead of one - POS remains the
primary movement oracle and its shape is unchanged.

RAW VELOCITY IS GEOCENTRIC (ECEF) metres/second, NOT local ENU: velY is not
"north". It originates as DtVector32 (float), so only float precision is meaningful
regardless of the F3 formatting.

CON vs BCON - WHY BOTH. With only CON, an empty console trace is ambiguous: "VR-Forces
raised no warnings" and "warnings were raised but never delivered to us" look
identical. BCON is an independent delivery path (per sim engine, not per object), so
traffic on BCON while CON stays silent localises the fault to the object-console
delivery path rather than to genuine silence. For a POSITIVE answer that bypasses the
network entirely, the bridge also exposes `LogObjectConsoleToFile(uuid, path)`, which
makes the BACKEND write that object's console to a file on its own filesystem, and
`SetObjectNotifyLevel(uuid, level)` to rule out threshold suppression. Neither has a
WatchVrf caller yet - they would need new arguments, and WatchVrf's argument surface
is deliberately frozen.

ESCAPING. Trailing free-text fields are RFC-4180 quoted, with backslash/CR/LF
C-escaped first (\\, \r, \n) so a multi-line message can never split a record.
Decode = strip outer quotes, undouble "", then reverse the C-escapes. Quoted
fields: CON's message, and BOTH TSK fields and RPT's text. POS is never quoted
(its fields are structurally safe). See tools/WatchVrf/ConFormat.cs, which is the
single source of truth; `WatchVrf.exe --con-selftest` asserts all of the above
OFFLINE (no VrfBridge.dll / MAK PATH needed) and round-trips the decoder.

WHY TSK/RPT EXIST. A position-only trace CANNOT distinguish "VR-Forces rejected
the task", "accepted it but the unit could not move", and "silently dropped it":
all three produce an identical static POS series. That is exactly what run
20260719T144109Z hit - two units bit-exactly static across 76 samples, one
snapping 63 m the wrong way - with no way to tell which. TSK is the acceptance /
completion signal; RPT is the radio narrative.

TSK AND RPT CARRY NO UUID, and this is not an oversight. The underlying managed
payloads (VrfBridge.cpp:125-134) carry only what is listed above -
TaskCompleted = UnitMarking + TaskType, TextReport = Text. Correlate TSK to POS by
resolving markingText -> uuid OUT OF BAND; do NOT assume TSK field 2 is a uuid.
Absence of a TSK line for a tasked unit is itself evidence (no completion was
ever reported) - but it is NOT proof of rejection, since a task that is accepted
and never finishes also produces no TSK.

### 0.5.8 VENDOR DIAGNOSTICS - READ THESE BEFORE PROBING

The 2026-07-18 session ran seven probes against documented behaviour before
reading the vendor's own procedure. Order of resort:

- VR-Forces help (HTML, offline): C:\MAK\vrforces5.0.2\doc\help\Content
  Startup/connections/sessions: Introduction\Starting\ and Introduction\Concepts\
  CLI options: Introduction\CLI\vrf_vrfSimCommandLine.htm and
  vrf_vrfLauncherCommandLine.htm. Troubleshooting: Appendixes\Troubleshooting\
  (thin - 3 pages; includes the Nahimic BlackApps.dat second-display start
  failure, which IS present on this machine as a warning in vrfGui.log).
  Back-end config-file equivalents of the CLI options:
  Appendixes\vrfSim\vrf_vrfSimMTLParams.htm (./appData/settings/vrfSim/vrfSim.mtl).
- MAK RTI books (PDF, offline): C:\MAK\makRti4.6.1\doc\RTIUsersGuide.pdf and
  RTIReferenceManual.pdf. THE VR-FORCES HELP DEFERS TO THESE for anything RTI -
  including the Assistant. They were never opened by this effort until 2026-07-18.
- Application logs: vrfSim.log and vrfGui.log, written to THE DIRECTORY VR-FORCES
  WAS RUN FROM (bin64 here). Block-buffered.
- Raise back-end verbosity: `--simArgs --notifyLevel 4 --fileNotifyLevel 4`
  (0=fatal .. 4=debug, default 2). At 4 it dumps the full environment it saw,
  which is how the PATH/RTI question was settled.
- RTI Assistant per-PID logs: %LOCALAPPDATA%\Temp\makRtiAssistant<pid>.txt. A
  HEALTHY assistant's log is ~615 bytes and ends "RTI connections cache file: ...
  connections.xml loaded."; one that DIED on the port collision is ~56 bytes (the
  command line only). Byte size alone tells you which happened.
- License: `lmutil.exe lmstat -a -c <licfile>`. NOTE this license is NODE-LOCKED
  and UNCOUNTED with NO SERVER lines, so no lmgrd/maklmgrd daemon is needed and
  its absence is NORMAL - lmstat correctly reports "No SERVER lines in license
  file. (-13,66)". Do not chase a missing license daemon.
- The Assistant's persisted connection choice lives in
  %APPDATA%\MAK\RTI\4.6\Legatus\connections.xml (`chosen="1"`). UNDOCUMENTED by
  MAK - do not hand-edit it; use the dialog.

### 0.5.9 CLEAN SHUTDOWN - UNATTENDED (scripts/StopVrf.ps1)

USE THE SCRIPT:

    pwsh -File scripts\StopVrf.ps1            # add -DryRun to see what it would do

Exit codes: 0 = down, already down, or dry run; 2 = bad args; 3 = timed out (NOT
force-killed - it prints the titles of every visible window still owned, so a blocking
second modal names itself); 4 = confirm dialog present but not drivable via UIA;
5 = unexpected terminating error (VR-Forces MAY STILL BE RUNNING, possibly with an
unanswered modal; nothing was force-killed - re-run or inspect). An unattended runner
must branch on 5 as well as 3. VERIFIED LIVE 2026-07-18: teardown
in 8 s, EXIT=0, zero human interaction, all three RTI processes preserved.

WHAT THIS SECTION USED TO SAY, AND WHY IT WAS NOT ENOUGH: it said only that in COMBINED
mode "when you shut down the front-end, the back-end automatically shuts down also"
(vendor, Introduction\Starting\vrf_startVRF.htm) - so close vrfGui, not the back-end.
That is TRUE BUT NOT UNATTENDED. Closing vrfGui raises a MODAL CONFIRM that blocks
shutdown until a human answers it:

    title = "Are You Sure?"   class = makVrf::DtNeverAskAgainMessageBox
    Text "Quit VR-Forces" | Button "Yes" | Button "No" | CheckBox "Quit All Back-Ends"

Teardown was never gated unattended, and this section read as if it were a one-liner.
An unattended session that cannot shut itself down cannot loop.

HOW StopVrf.ps1 ANSWERS IT - UI AUTOMATION, BY CONTROL NAME, NOT COORDINATES. This
dialog DOES expose a full UIA tree. NOTE THE CONTRAST AND DO NOT GENERALISE: the RTI
"Choose RTI Connection" dialog does NOT (sec 0.5.4, line "The dialog is a Qt window
(class Qt5QWindowIcon) and exposes NO UI Automation..."), which is why that one needs
screenshot + coordinate clicking and this one must not. The two dialogs belong to
DIFFERENT PROCESSES (rtiAssistant vs vrfGui) and behave differently; neither result
predicts the other.

*** FOURTH DATA POINT, 2026-07-19 - A MODAL THAT IS NOT A TOP-LEVEL WINDOW AT ALL, AND IT
HUNG A LIVE TEARDOWN. Run 20260719T193252Z: StopVrf returned EXIT=3 leaving vrfGui alive,
blocked by a dialog it could not see:
    class    makVrf::DtNeverAskAgainMessageBox   (the SAME class as "Are You Sure?")
    name     "Session Status"
    text     "The current session has ended. Close current terrain?"
    buttons  Close / Yes / No           checkbox "Execute session changes without prompting."
WHY IT WAS INVISIBLE: it is a NESTED DESCENDANT of the vrfGui main window, not a top-level
window. Enumerating top-level windows for the vrfGui process returns ONLY
makVrf::DtVrfQtDeMainWindow; a TreeScope::Descendants search for ControlType Window finds
"Session Status" plus three QDockWidgets. StopVrf's original search - the one that
correctly finds "Are You Sure?" - is top-level and structurally cannot see this.
WHEN IT FIRES: after StopIface drives the C2SIM server to UNINITIALIZED, VR-Forces treats
the session as ended and raises it. It was ORIGINALLY believed to fire on EVERY cleanly-torn-down
unattended run. This is not an edge case.
ANSWERED "No" - the application is being closed anyway, so leaving the terrain loaded is
the smaller state change, and answering No unblocked the teardown by hand.
DO NOT TICK "Execute session changes without prompting." It persistently mutates the
user's VR-Forces configuration and would silently change future INTERACTIVE sessions too.
The fix belongs in our script, not in their config.
StopVrf now scans DESCENDANTS as well as top-level windows, logs EVERY nested window it
finds with class/name/buttons even when it knows the answer, and REFUSES TO PRESS ANYTHING
on a dialog it does not recognise - guessing a button on an unknown modal could do
something destructive. Its timeout report no longer claims "the cause is NOT a modal
dialog", which was exactly the false conclusion this defect produced. ***

*** THIRD DATA POINT, 2026-07-19 - AND IT REFINES THE CONTRAST ABOVE. The rtiAssistant
"MAK RTI Error Notification" dialog IS FULLY UIA-DRIVABLE, even though the rtiAssistant
"Choose RTI Connection" dialog is NOT. So the split is NOT per-process as the paragraph
above implies - the SAME process owns one dialog that exposes a UIA tree and another that
does not. Do not predict either way; ENUMERATE.
Observed: window class Qt5QWindowIcon, name "MAK RTI Error Notification", three buttons -
"Close" (enabled), "More   >>> Enter" (disabled), "Dismiss" (enabled). Dismissed by
FindAll on ProcessIdProperty -> ControlType Button -> InvokePattern.Invoke(). Content was
LRC #45, "Failed to open FDD file: RPR_FOM_v2.0_1516-2010.xml".
WHY THIS MATTERED: a modal was sitting on the LONG-LIVED assistant that services every
join, and killing an assistant previously cost an entire live window (sec 0.5.2). The
dialog was DISMISSED, NOT killed, and the assistant was then confirmed healthy by a
ResetVrf --dry-run on ledgered appNo 3522 - "Connected to RTI Assistant", joined,
resigned cleanly, EXIT=0 in 21 s against a 20 s baseline. THE NON-NEGOTIABLE HELD: no RTI
process was killed.
ON THE FDD ERROR ITSELF: the file exists and is readable
(C:\MAK\vrforces5.0.2\bin64\RPR_FOM_v2.0_1516-2010.xml, 1058783 bytes, unchanged since
2022). FedFileName is configured as a BARE RELATIVE FILENAME, so any federate joining with
a cwd that does not contain it fails exactly this way - which is why every joiner must use
cwd = VR-Forces bin64 (sec 7 item 3). NOT ESTABLISHED: which process raised it or when.
"LRC #45" is a lifetime counter on an assistant up since the previous day, the dialog is
not timestamped, and no assistant-side log was found. Do not guess a culprit.
IT DID NOT CONTAMINATE THE 2026-07-19 RUNS: both scored runs produced 4512 POS lines each
and no run artifact contains "FDD" or "Failed to open". A federate that could not open the
FDD would not have joined at all. ***

Coordinate approaches were
tried here first and ALL THREE FAILED: CopyFromScreen captured the occluding window,
SetForegroundWindow was refused by the Windows foreground lock, and PrintWindow with
PW_RENDERFULLCONTENT returned an all-black bitmap (Qt/OpenGL surface).

THE DIALOG'S COMPOSITION VARIES BETWEEN LAUNCHES - VERIFIED, CAUSE UNKNOWN. On one
teardown it exposed the "Quit All Back-Ends" checkbox (StopVrf ticked it, ToggleState
On); on the very next launch/teardown cycle the same title+class exposed NO checkbox and
StopVrf logged "checkbox not found". BOTH succeeded, EXIT=0, both processes down -
because plain "Yes" in COMBINED mode already closes the GUI and the engine it started.
Do NOT assume the checkbox is present, and do not treat its absence as a fault.

VENDOR-DOCUMENTED ALTERNATIVE (not currently used; recorded because it is the vendor's
own answer): Settings > Application > General Application Settings > clear "Show Quit
Dialog On Close" (doc\help\Content\Introduction\Starting\vrf_disableQuitDialog.htm,
"Disabling the Quit Prompt"), persisted as `myShowQuitDialogOnClose` under
appData\settings\vrfGui. The docs state that with the prompt disabled, closing "acts as
if you clicked Yes" - which in combined mode closes the GUI and the engine it started,
but is NOT the same as the vendor's "Yes, and Quit All Back-Ends" option
(ExitingVR-Forces.htm), which the shipped 5.0.2 dialog implements as a CHECKBOX beside
"Yes" rather than as a second button. NOT ADOPTED because which settings file the GUI
actually READS is unverified: `default_Application.apsx` (serializer version 9) and
`backups\Application.backup` (version 14) both carry the key, and write path does not
prove read path. A third copy exists at appData\settings\exampleCustom\, and
vrfGui\applicationSettings.xml is version 14 but holds a DIFFERENT class
(DtVrfGuiApplicationSettings) that does NOT contain the quit key - a plausible
explanation for the 9-vs-14 split that has not been run down. Mutating vendor settings
is also a wider blast radius than answering one dialog. There is also a remote
`DtVrfRemoteController::exit()` (include\vrfcontrol\vrfRemoteController.h:825;
DtExitMessageType = 45 at include\vrfmsgs\messageTypes.h:125), but every vendor
statement scopes the Remote Control API to the BACK-END ("control a VR-Forces
simulation engine from a remote application"); treating it as a GUI shutdown is
undocumented. Good fallback if a GUI ever dies without taking its engine.

KNOWN GAP: the vendor documents the exit flow as EXACTLY ONE prompt
(ExitingVR-Forces.htm) and never states whether disabling or answering it suppresses
other modals. A "standard 'scenario modified' prompt" and a save-terrain prompt ARE
documented in OTHER contexts (VRFUsersGuide; Terrain\NavData\vrf_cancellingNavDataGen.htm),
so the mechanism exists in the product - what is undocumented is whether either can fire
on close. StopVrf.ps1 answers only "Are You Sure?". On timeout it exits 3 and PRINTS THE
ACTUAL TITLES of every visible window still owned by those processes, rather than
guessing at a cause - if a second modal is blocking, its title will be in that list.

UNCHANGED NON-NEGOTIABLES: never force-kill a JOINED federate (sec 0) - that leaves a
stale federate and the next start hangs at RTI join; StopVrf.ps1 never kills anything.
Leave rtiAssistant / rtiexec / rtiForwarder RUNNING (0.5.2).

### 0.5.10 HISTORICAL - the three wrong "corrections" written into this section

All three were written confidently on 2026-07-18 and acted on. Kept so they are
not re-derived:
1. "rtiexec is spawned automatically by the RTI on first federate join" - false
   under the fallback connection; connection-dependent either way (0.5.6).
2. "rtiexec NEVER runs here BECAUSE (setqb RTI_useRtiExec 0)" - wrong twice: the
   observation was connection-specific, AND that parameter is INERT. RTI Users
   Guide p. 7-8 / RefMan Appendix A, verbatim under RTI_useRtiExec, RTI_udpPort,
   RTI_tcpPort, RTI_destAddrString and RTI_tcpForwarderAddr: "This parameter is
   ignored unless RTI_configureConnectionWithRid is set to 1." Ours is 0, so all
   rid.mtl connection values here are DISCARDED in favour of the Assistant's
   stored connection.
3. "UDP 4000 bound means the back-end joined" - connection-dependent (0.5.6).

Also superseded: an earlier version of this section presented
`RTI_ASSISTANT_DISABLE=1` as "THE FIX ... VERIFIED". It is NOT the fix - see
0.5.5. And the "ResetVrf discovers 0 objects / BackendCount=0" residual recorded
against it is RESOLVED: under the correct connection the 0.4 gate passed twice
(3489/3490), discovering the 2 TropicTortoise baseline objects.

---

## 0.5-ARCHIVE - the raw vrfSimHLA1516e headless recipe (CONFIRMED UNSAFE, 2026-07-15)

RETAINED FOR THE HISTORICAL RECORD ONLY. DO NOT USE THIS RECIPE. The supported
bring-up is 0.5.1 above. Everything below predates the 2026-07-18 root cause and
its "root cause found" claims should be read in that light - the crash it
describes is real, but the launch-hang symptoms discussed alongside it were the
RTI Assistant prompt (0.5.3), which nothing in this archive knew about.

GOTCHA: `vrfSimHLA1516e.exe --help` does NOT print usage and exit - it silently starts a
real (unconfigured) sim instance instead. Do not probe with `--help`; the option reference
is `C:\MAK\vrforces5.0.2\doc\help\Content\Introduction\CLI\vrf_vrfSimCommandLine.htm`
(official MAK docs, on disk, offline).

Launch (same env as sec 1: RTI 4.6.1 on PATH, `MAKLMGRD_LICENSE_FILE` from Machine scope,
cwd = `C:\MAK\vrforces5.0.2\bin64`, fresh appNumber per the Appendix B ledger in
OPUS_EXECUTION_PLAN.md - the backend consumes one too, same ledger, do not reuse the
interface app's range implicitly):
```
vrfSimHLA1516e.exe --execName CWIX-2024 --siteId 1 --sessionId 1 --appNumber <freshAppNo> ^
  --fedFileName RPR_FOM_v2.0_1516-2010.xml ^
  --fomModules MAK-VRFExt-6_evolved.xml --fomModules MAK-DIGuy-7_evolved.xml --fomModules MAK-LgrControl-2_evolved.xml ^
  --scenarioFileName "../userData/scenarios/<Bogaland2|TropicTortoise>.scnx"
```
FED file + FOM modules are the SAME three that PORT.md sec 4 / RUNBOOK sec 7 already
reverse-engineered to match VR-Forces (they must match whatever VR-Forces itself loads, and
these are it - confirmed via a MAK ground-vehicle-test `.bat` under
`vrforces5.0.2\autotests\scenarioPerformanceTests\`, which uses the same fedFileName/
fomModules set but a different execName - do not copy that file's execName/appNumber).
`--scenarioFileName` (`-L`) path is relative to `bin64` per the official docs; scenario
files live in `C:\MAK\vrforces5.0.2\userData\scenarios\`. Run this in the background (it is
a persistent process, like the interface) and verify success by process presence
(`vrfSimHLA1516e` + `rtiexec` both up) - not by console output (block-buffered, sec 3 below).

Clean shutdown: same as any other federate - drive the C2SIM server to UNINITIALIZED and let
it resign, or if nothing has joined it yet, a plain close is fine (it never joined a
federate). Do NOT force-kill it once anything (the interface, ResetVrf, WatchVrf) has joined
the federation it hosts - sec 0 applies to it too. EXCEPTION (2026-07-15): a process stuck
behind a blocking startup error dialog (e.g. the LRC #8 case below) never completed a real
join - closing it directly is fine; only a process that has genuinely joined needs the clean
stop.

KNOWN ISSUE - CONFIRMED UNSAFE, 2026-07-15: this recipe gets vrfSimHLA1516e running and
loading a scenario, but produces a backend that CRASHES remote-controller clients (ResetVrf,
the app) on tick - root-caused (see the last bullet below) to the headless launch itself, not
to any particular scenario. DO NOT use this recipe for live work; have a human launch
VR-Forces via the GUI (combined front-end+back-end mode) instead. Left here for the historical
record and because the launch args/env themselves are still correct and useful if the
underlying gap is ever found and fixed:
- First attempt: a bare `--fedFileName RPR_FOM_v2.0_1516-2010.xml` (relative filename)
  produced an RTI popup "LRC #8: Failed to open FDD file" - `rtiexec` (auto-spawned by the
  RTI, likely a different cwd than vrfSim's) could not resolve the bare filename even though
  it exists in vrfSim's own bin64. FIX: pass an ABSOLUTE path for `--fedFileName`.
- After that fix, vrfSim loaded the TropicTortoise scenario cleanly ("Successfully loaded
  scenario", objects registering) - but a `ResetVrf --dry-run` against it then crashed
  (`0xC0000005` access violation inside `VrfFacade::Tick()` -> MAK's own
  `controller->tick()`) DURING discovery, before any of our own init/units were involved -
  the crash happened while reflecting the scenario's OWN native "Locally Simulated" objects
  (GlblTerrDmg, EnvironmentProcess, VrfExtendedAttributes, the page-in area). Shortly after
  (~2 min), vrfSim itself crashed too (dump `vrfSim5.0.2-MSVC++15.0_64-249613-<pid>.dmp` in
  `C:\MAK\vrforces5.0.2\bin64\`), timing-adjacent to the ResetVrf crash - plausibly a
  cascade (ResetVrf's crash destabilizing shared HLA-level state) rather than two
  independent bugs. A retried ResetVrf dry-run in between succeeded cleanly, so it is not
  100% reproducible on demand.
- A prior, DIFFERENT vrfSim crash exists from 2026-07-14 evening (same bin64 dir), already
  documented: docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md "Live A/B -
  ATTEMPT 1 ABORTED" (creating a full amphib-laden unit transplant on top of an
  already-loaded scenario). That one IS explained (backend overload); this session's is
  NOT yet explained and may be a different mechanism (no units were even created this
  time).
- REPRODUCED again (2026-07-15, later same day): a ResetVrf dry-run against a freshly-loaded
  TropicTortoise instance crashed identically (0xC0000005 in VrfFacade::Tick()), then the
  SAME crash killed the live app itself mid-tick (3rd reproduction total that session) - all
  against backends launched headless via this doc's sec 0.5 CLI recipe. ZERO reproductions at
  Sweden/Bogaland2 that same session - but EVERY Sweden run that session used the user's
  GUI-launched (combined front-end+back-end mode) backend, while EVERY TropicTortoise attempt
  used the headless CLI recipe - region and launch method were fully confounded, never
  isolated.
- ROOT CAUSE FOUND (2026-07-15, same day, later): NOT a TropicTortoise/Mojave content issue.
  The user launched TropicTortoise via the GUI (combined mode, matching how Sweden was always
  launched) and a ResetVrf dry-run against THAT backend succeeded cleanly (0 crashes) -
  discovering the identical 2 baseline objects the headless-launched instance also had, just
  without the crash. Confirms: the crash is specific to the sec 0.5 HEADLESS CLI launch recipe
  (`vrfSimHLA1516e.exe` alone, no front-end) missing something the GUI's combined
  front-end+back-end mode provides - NOT anything about Mojave's terrain/scenario content
  (which was independently ruled out anyway: byte-identical .scnx to the repo snapshot,
  byte-identical page-in-area object to Bogaland2's own, identical FOM/connection config used
  for both scenarios per the GUI's own saved connection profile). CONCLUSION: sec 0.5's
  headless launch recipe is NOT SAFE TO USE - it produces a backend that crashes remote-
  controller clients (ResetVrf, the app) on tick. Until the actual missing piece is found
  (leading candidate: the combined-mode front-end/back-end pairing itself, e.g.
  `--frontEndPID` or an equivalent front-end presence the backend's reflectAttributeValues
  path may depend on) always have a human launch VR-Forces via the GUI; do not use the sec
  0.5 headless CLI recipe for live work.

THE USER'S ACTUAL LAUNCH TOOL is `vrfLauncher.exe`, not a bare `vrfSimHLA1516e.exe` invocation -
this is the likely missing piece and the concrete next thing to try for a reliable headless
recipe. `vrfLauncher.exe --help` (captured 2026-07-15, run from `C:\MAK\vrforces5.0.2\bin64`):
```
USAGE:

   vrfLauncher.exe  [-G <string>] [--usePredefinedConnection <string>]
                    [--useUserSettingsDirectory] [-R] [--guiArgs] [--simArgs]
                    [--] [-C] [-B] [-F] [-v] [-h]

Where:
   -G <string>,  --locale <string>
            Language to use for application
   --usePredefinedConnection <string>
            Use a predefined connection name (e.g. "DIS localhost")
   --useUserSettingsDirectory
            Whether or not to use the shared application settings (default) or
            user login settings directory.
   -R,  --makRadio
            Launch the application in MAK Radio launch mode
   --guiArgs
            Pass all arguments after this only to the front-end (F-- can also be
            used)
   --simArgs
            Pass all arguments after this only to the back-end (B-- can also be
            used)
   --
            Pass all arguments after this to launched applications
   -C,  --config
            Do not launch application - just the configuration tool
   -B,  --backend
            Launch the Back-End Application
   -F,  --frontend
            Launch the Front-End Application
   -v,  --version
            Displays version information and exits.
   -h,  --help
            Displays usage information and exits.

   VR-Forces Launcher
```
The user's saved connection profile (confirmed on-screen, Simulation Connections Configuration
dialog, starred/default) is named **"HLA 1516 Evolved RPR 2.0 with MAK extensions"** - FED file
`RPR_FOM_v2.0_1516-2010.xml`, the same 3 FOM modules already used everywhere in this doc,
Federation `CWIX-2024`, back-end Application Number `3001`, front-end Application Number `3101`.
UNTESTED CANDIDATE headless recipe (profile name verified, this exact invocation NOT yet tried
live): `vrfLauncher.exe -B --usePredefinedConnection "HLA 1516 Evolved RPR 2.0 with MAK
extensions" --simArgs --appNumber <freshAppNo> --scenarioFileName
"../userData/scenarios/<Bogaland2|TropicTortoise>.scnx"` (the `--simArgs`-prefixed args are
guesses at what a bare `-B` backend-only launch needs beyond the connection profile - siteId/
sessionId/execName/FED/FOM may already be implied by the profile and not need repeating; verify
against actual behavior, do not assume this is complete). If `-B` alone (backend-only, no
front-end) reproduces the crash, that would show the crash is about "missing front-end" rather
than "not using vrfLauncher specifically" - a further useful data point either way.

## 0.6 GOTCHA - never put a comment in the XML prolog of a pushed init/order file
(found + root-caused 2026-07-15, cost a long bisection): a large explanatory `<!-- ... -->`
comment BEFORE the root `<MessageBody>` element (i.e. in the XML prolog, after `<?xml?>` but
before the root tag) is standard-legal XML - `xmllint` confirms well-formed, and the port's
own `InitParser`/`OrderParser` accept it fine - but the REAL C2SIM server's parser does NOT
tolerate it and silently rejects the WHOLE message with a generic, unhelpful error
(`ERROR Error processing message`, or via `PushInit --verbose`:
`Only INITIALIZATION messages are permitted in server state INITIALIZING`, itself a red
herring - it is not actually a performative/state problem). Proven by bisection (stripping
the prolog comment alone fixed an otherwise-identical push; reverting other suspects -
coordinates, ForceSide trimming - did NOT fix it in isolation). FIX for INIT files: put documentation comments INSIDE the root element (right after the
opening `<MessageBody ...>` tag), never before it - confirmed working via PushInit.
CORRECTION for ORDER files (found immediately after, same session): the inside-root fix is
NOT enough for orders - a large multi-line comment there crashed the receiving app's STOMP
client (`System.Xml.XmlException: Unexpected end of file while parsing Comment`, app
"Restart recommended") even though the pushed file itself was well-formed XML. Orders are
live-broadcast over STOMP (a different delivery path than init's REST/QUERYINIT poll), and
something in that path - likely the server's or SDK's own re-slicing of the live event body
- cuts the message at a point that lands INSIDE a large comment, truncating it mid-comment
on the receiving end. FIX for orders: no large block comment anywhere in the file; small
single-line inline comments (e.g. `<!--UnitName-->` on an ActorReference/PerformingEntity
line, matching the established pattern in R9_Mojave_UnitMove_Order.xml) are fine and did not
reproduce this. `data/COA-STP1_Sweden_Initialization.xml` (comment inside root, works) and
`data/COA-STP1_Sweden_MinimalOrder.xml` (no block comment, works) are the worked examples.
Diagnostic tool improvement made alongside this: `tools/PushInit` gained a `--verbose` flag
(prints the SDK's own trace-level raw server response, normally discarded by
`NullLoggerFactory` - this is what surfaced the real `<error>` detail above the generic
`resp.Message`); use it whenever a push fails with only a generic error.

## 1. Environment (verify - do not assume; see PORT.md sec 4)

- VR-Forces running HLA1516e, execName CWIX-2024, siteId 1, sessionId 1
  (`vrfSimHLA1516e` + `rtiexec` processes up).
- C2SIM server container `c2sim-server` up: REST 8080, STOMP 61613 (`docker ps`).
  `docker restart c2sim-server` (~30 s to ActiveMQ-ready) if STOMP has degraded
  across many runs. After a Docker restart the container may rebind 0.0.0.0:8080 itself.
- IPv4 8080 free (stop the COA-GPT `tileserver.py` if it reclaimed it).
- exe env: `QT_QPA_PLATFORM_PLUGIN_PATH=C:\MAK\vrforces5.0.2\bin64\platforms`; PATH must
  include `C:\MAK\makRti4.6.1\bin` (MAK RTI - NOT Pitch/prti1516e) and
  `C:\MAK\vrforces5.0.2\bin64`; launch with cwd = `C:\MAK\vrforces5.0.2\bin64`.
  The repo `runc2simVRFHLApRTI.bat` prepends PITCH RTI - do NOT use it as-is; this
  interface links MAK RTI 4.6.1 (confirmed by its startup log "Using MAK ... RTI 4.6.1").

## 2. Launch command (arg map from main.cxx argv[1..18])

`bin64\c2simVRFHLA1516e.exe <srvIP> <restPort> <stompPort> <clientId> <skipInit> <ibml> <tracking> <vrfAddr> <reportInterval> <blueForce> <debug> <sessionId> <appNumber> <siteId> <obs> <timeMult> <bundle> <federation>`

- Golden STP:  `127.0.0.1 8080 61613 STP   0 0 3 127.0.0.1 0 0 0 1 3201 1 3 0 0 CWIX-2024 0`
- COA-STP1:    `127.0.0.1 8080 61613 C2SIM 0 0 0 127.0.0.1 0 0 0 1 <freshAppNo> 1 0 0 0 CWIX-2024`
- clientId (argv4) MUST equal the init's SystemName (STP init -> STP; COA-STP1 -> C2SIM),
  or the interface creates 0 units.
- appNumber (argv13) must be FRESH each run (a prior run's federate lingers). Increment it.
- debug (argv11) MUST be 0 (debug=1 is broken - PORT.md sec 6).

## 3. Run cycle (ORDER MATTERS)

1. Push the init FIRST, THEN start the interface (documented: PHASE1_REWIRE.md
   Verification step 3; START_HERE Run/verify). The interface late-joins via QUERYINIT
   at startup and creates the units.
   `tools\PushInit\bin\Release\net10.0\PushInit.exe <init.xml>`  -> expect "QUERYINIT: N Units".
2. Start the interface (sec 2). JUDGE CONNECT BY THREAD COUNT, NOT THE LOG: stdout
   redirected to a file is BLOCK-BUFFERED (~4 KB), so the log sits at ~1133 B showing
   only the config banner even after a successful connect. Connected = ~9-10 threads;
   hang-at-RTI = 1 thread / ~0 CPU. Unit creation flushes the buffer (log jumps past ~6 KB).
3. Push the order:
   `tools\PushOrder\bin\Release\net10.0\PushOrder.exe <order.xml> <listen-secs>`
4. Observe HEADLESSLY - the GUI is NOT the instrument:
   - movement: the WatchVrf POS trace (displacement between samples). THIS is the
     movement oracle and the only admissible evidence of motion.
   - whether VR-Forces ever ACKNOWLEDGED the tasking: the TSK / RPT lines in that
     same trace (sec 0.5.7a). A static POS series alone cannot tell a rejected task
     from an accepted-but-immobile unit; TSK/RPT are what separate the two.
   - what the interface told C2SIM: tools/ListenReports.
   - task start/complete lines in the interface log are CORROBORATING ONLY -
     completions lie in BOTH directions and may never stand alone.
   *** CORRECTED 2026-07-18: this step used to say "entity movement in the VR-Forces
   GUI", i.e. verification by human eyeball. That contradicts the headless mandate
   (VRF_GROUNDWORK_PLAN.md sec 1a rule 2) and is not how any run is scored. Watching
   the GUI is fine as a diagnostic; it is not evidence. ***

## 4. CLEAN STOP (do this instead of force-kill)

The interface exits and resigns from the RTI when the C2SIM server broadcasts
`systemState == UNINITIALIZED` (C2SIMinterface.cpp:1828 -> `setTimeToQuit(true)` ->
main.cxx:424 loop exit -> `delete facade` -> RTI resign). Drive the server there with
STOP then RESET (NOT INITIALIZE, which would move on to INITIALIZING):
via the SDK, `await sdk.PushCommand(C2SIMCommands.STOP); await sdk.PushCommand(C2SIMCommands.RESET);`
(`PushCommand` is public - C2SIMSSDK.cs:537/556; states enum C2SIMSSDK.cs:35).
DONE: `tools/StopIface` does exactly this in one command (drives server STOP -> RESET ->
UNINITIALIZED); it is the standard clean stop - see sec 7. The manual SDK two-command path above
is the fallback / what StopIface does under the hood.

Corollary: NEVER push a fresh init to a RUNNING interface. `PushInit` calls
`ResetToInitializing` = STOP/RESET/INITIALIZE, and the RESET step's UNINITIALIZED
transient triggers the interface's clean shutdown. That is why a mid-run PushInit drops
the interface to 1 thread - it is RESIGNING, not hanging. (Push init only while NO
interface is running - sec 3.)

## 5. If a federate got stale anyway (after an accidental force-kill)

Symptom: next interface start hangs at RTI join (1 thread, ~0 CPU, log frozen at config).
RECOVERY IS AUTOMATED AND NEEDS NO HUMAN:
  1. `tools/ResetVrf` (sec 8) - re-creates the federation state remotely, OR
  2. `pwsh -File scripts\StopVrf.ps1` then `scripts\LaunchVrf.ps1` (sec 0.5) - both
     unattended; a relaunch reloads the scenario from file.
Avoid the situation entirely by clean-stopping (sec 4).
*** CORRECTED 2026-07-18: this section used to say "reload the VR-Forces scenario in
the GUI ... This is the ONLY step that needs the human." FALSE. It is also exactly the
kind of statement that led a supervisor to conclude the whole effort needed a human at
the GUI (VRF_GROUNDWORK_PLAN.md sec 1a). No step in this recovery needs a person. ***

## 6. Known runtime blocker (2026-07-09): the C++ STOMP client hangs at connect

Symptom: a FRESH interface run (correct sec-3 push-init-first sequence, healthy broker)
connects to the RTI (~9-10 threads) but NEVER establishes a STOMP connection to 61613
(confirmed: `Get-NetTCPConnection -OwningProcess <pid>` shows no 61613 connection), so it
never late-joins and creates 0 units. Its log is frozen at the config banner (block-buffered,
sec 3) and stderr is empty. This is the "connecting STOMP stream" hang flagged in PORT.md sec 8.

Diagnosed - what it is NOT:
- NOT the push order (sec 3 is correct; coa3 proved it - its log shows RTI -> "connecting STOMP
  stream" -> "SERVER ALREADY RUNNING - REQUESTING LATE JOIN" -> received INIT -> 128 units).
- NOT broker readiness (PushInit's .NET STOMP client works against the same broker seconds before).
- NOT a port shadow: the two 61613 listeners are just Docker Desktop dual-stack forwarding
  (`com.docker.backend` on 0.0.0.0 + [::], `wslrelay` on [::1]); both reach the container.

What it IS (CORRECTED - an earlier NordVPN guess was WRONG and is retracted; loopback 127.0.0.1
never touches a VPN tunnel, and the user's STP connector + the .NET PushInit both connect to
http://127.0.0.1:8080/C2SIMServer and 127.0.0.1:61613/topic/C2SIM fine): the Docker Desktop / WSL2
loopback PORT-PROXY went slow. A raw TCP connect to the loopback ports measured 5-9 SECONDS (should
be <1 ms) - `com.docker.backend` + `wslrelay` had degraded, almost certainly from THIS session
OVER-CHURNING Docker (an unnecessary `docker restart c2sim-server` on top of an earlier full
Docker-recovery). The interface's C++/boost STOMP client cannot ride out that latency, so it stalls
before it even opens the socket (`Get-NetTCPConnection` shows 0 connections for the PID); the .NET
SDK clients tolerate it. The METHOD IS SOUND - this SAME session ran it successfully many times
(transcript: golden trace "initialized 49 units", then "connecting STOMP stream"/"created units"
repeatedly, and after a Docker recovery, coa3 "initialized 128 units").

FIX for a fresh session: do NOT restart the broker as a habit - it was never the problem, and the
restarts are what degraded the proxy. If a raw TCP connect to 127.0.0.1:61613 is not near-instant,
reset the Docker port proxy (restart Docker Desktop, or reboot), confirm loopback is fast, THEN run
sec 3 unchanged. The session transcript (~/.claude/projects/.../a1852c45-...jsonl, around lines
1540-1605) shows the working launch + push + late-join sequence and a prior Docker recovery.

Impact: blocks the LIVE proof ONLY. The aggregate-movement fix (PORT.md sec 10) is validated
independently (MAK `setAggregateFormation` API + valid formation names + clean build) and does
not depend on this run. Its real home is the .NET port (VRF_C2SIM), whose STOMP client is the
.NET SDK - which demonstrably works (PushInit). Decision: do NOT sink more time into the
deprecated-C++ live proof. If a visual is later deemed essential, the next lever is a full
Docker Desktop / container RECREATE (not just restart), which is disruptive.

## 7. Running the .NET PORT (VrfC2SimApp) live - hard-won 2026-07-10

First live bring-up of the .NET port. The C2SIM server had been removed; redeployed from
`Downloads/Docker.zip` (c2sim-docker-4.8.4.9-rev1 + c2simFiles-v3) per its `.docx`:
`docker image load -i c2sim-docker-4.8.4.9-rev1.tar.gz`, untar c2simFiles, then
`docker run -d --name c2sim-server -v "<host>\c2simFiles\c2simFiles":/opt/c2simFiles -p 8080:8080 -p 61613:61613 <imageId>`.
Verify: REST `http://127.0.0.1:8080/C2SIMServer` -> HTTP 200; 8080/61613 open + fast.

LAUNCH ENV that actually works (four things the offline docs got wrong or omitted):
1. **Runtime RTI must be 4.6.1, NOT 4.6b.** VR-Forces' rtiexec is `C:\MAK\makRti4.6.1`
   (`MAK_RTIDIR`/`RTI_RID_FILE` both 4.6.1). The bridge is *built* against 4.6b libs but
   runs fine on 4.6.1 (proven: the app logged "Using MAK ... RTI version 4.6.1" and joined).
   So PATH = `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;...`.
   (The START_HERE/APP.md offline PATH lists 4.6b - fine for `--parse-*` which only LOAD the
   DLLs, WRONG for a live join, which must match the federation's RTI = 4.6.1.)
2. **`MAKLMGRD_LICENSE_FILE` must point at the RENEWED license.** A shell may inherit a STALE
   session value pointing at a now-deleted expired `.lic` -> the RTI/VR-Link license checkout
   HANGS in `bridge.Start()` before any socket (low CPU, threads decreasing, 0 connections).
   Fix: `$env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')`.
3. **cwd must be `C:\MAK\vrforces5.0.2\bin64`** (as for the C++ interface) so Legion finds
   `vrfLegion.lua` + terrain data. Wrong cwd -> `FATAL[Legion] ... vrfLegion.lua ... No such file`
   then an SEHException. Since the .NET host loads appsettings from cwd, pass
   `--contentRoot="<exe dir>"` so config still loads while cwd = VRF bin64.
4. **FED file + FOM modules MUST match VR-Forces**, else `bridge.Start()` crashes `0xC0000005`
   after "addInteractionCallback - bad class name: Data/RadioSignal.*/Comment" (missing FOM
   class handles). Set in appsettings `Vrf`: `FedFileName=RPR_FOM_v2.0_1516-2010.xml`,
   `FomModules=[MAK-VRFExt-6_evolved.xml, MAK-DIGuy-7_evolved.xml, MAK-LgrControl-2_evolved.xml]`
   (all resolve from VRF bin64). Read VR-Forces' own `--fedFileName/--fomModules` off its
   command line if they differ. Use a FRESH `Vrf__ApplicationNumber` each run (stale-federate).

With all four, the app JOINS HLA (RTI ports established, no crash) and logs "Connected to
C2SIM". Clean stop: `tools/StopIface` drives the server STOP->RESET->UNINITIALIZED (the
RUNBOOK sec-4 tool, now built) - the interface is meant to catch UNINITIALIZED and resign.

PORT GAPS found + FIXED this session (the app now runs live end-to-end):
- **STOMP receive works** - the earlier "receives nothing" was a MISDIAGNOSIS. `tools/StompProbe`
  (subscribe + hook every event) proved the SDK receives the init + status broadcasts fine, with
  BOTH the app's `1.0.2` and the tools' `CWIX2024v1.0.2` settings. The app only *looked* dead
  because of the three real gaps below (it doesn't log raw/received messages).
- **No late-join (FIXED).** The app only subscribed to FUTURE broadcasts; with push-init-first it
  created 0 units. FIX: after `_sdk.Connect()`, call `_sdk.JoinSession()` (REST QUERYINIT) and feed
  the result through `ProcessInitialization`. Verified live: "late-join QUERYINIT ... 49 units".
- **Parsers assumed `<MessageBody>` root (FIXED).** The SDK's live events deliver the BARE inner
  body (`<C2SIMInitializationBody>`, `<OrderBody>`), but InitParser/OrderParser (tested on FILES)
  expected the full envelope -> 0 units / no task on live events. FIX: try the envelope, then the
  bare body directly (both body types carry `[XmlRoot]`). Verified: init + order both parse live.
- **Empty status body (FIXED).** The STOMP status broadcast body is empty `<SystemMessageBody/>`
  and the header has no state, so `OnStatusChanged`'s `e.Body.Contains("UNINITIALIZED")` NEVER
  matched -> no clean stop. FIX: treat the event as a trigger and read the real state via REST
  `GetStatus()` (== `C2SIMServerStatus.UNINITIALIZED`). Verified: StopIface -> app resigns clean,
  rtiexec back to 2 (no stale federate).
- Also aligned appsettings `C2SIM` to the proven tool values: `ProtocolVersion=CWIX2024v1.0.2`,
  `RestPassword=v0lgenau` (for the REST GetStatus/QUERYINIT/report-push calls).

FULL PIPELINE LIVE-VERIFIED (2026-07-10): deploy -> HLA join -> late-join (49 units + 4 areas)
-> order received/parsed over STOMP -> taskee resolved -> CreateRoute + MoveAlongRoute (ENTITY
1.BdeHQ AND disaggregated AGGREGATE 14.MechBn) -> sim runs -> unit MOVES -> task COMPLETES ->
`OnVrfTaskCompleted` -> "SENT TASK STATUS REPORT (TASKCMPLT)" pushed to C2SIM; position reports
also flow (`OnVrfTextReport` -> 4140 pushed) -> clean stop (no stale federate). Every stage works.

RUN() GAP (found + fixed): the app never called `_bridge.Run()`, so the VR-Forces sim clock never
started and tasked units never moved/completed (no TASKCMPLT). The C++ interface calls
`facade()->Run()` on the server RUNNING state (C2SIMinterface.cpp:1819/1917). FIX: the app now
queues `_bridge.Run()` after late-join and on each RUNNING status, plus an optional
`Vrf:TimeMultiplier` (default 1 = real-time; set higher e.g. 20 to run the clock fast - a 20x run
completed 1.BdeHQ's route in ~30 s and fired the TASKCMPLT report).
NOTE the position-report volume is high (no aggregate-component dedup / bundling yet - deferred,
docs/APP.md); functional but chatty, especially at high TimeMultiplier.

AGGREGATE geodetic (14.MechBn) - isolated + FIXED + LIVE-VERIFIED (2026-07-10, after a
VR-Forces scenario reload): with the static_cast fallback the golden order tasks 14.MechBn
end to end - "CreateRoute 'T1_1_4_A ROUTE' (3 pts) for 14.MechBn" -> route created ->
"MoveAlongRoute issued". So BOTH entity and disaggregated-aggregate tasking now work live.
History (pre-fix): with entities
well-settled the entity tasks fine but 14.MechBn still ABANDONED at point 0, so it is
aggregate-specific, NOT timing. Cause: the port's dynamic_cast<DtReflectedAggregate*> misses
the disaggregated aggregate, where the C++ oracle's blind static_cast read the base myStateRep
and worked. FIX applied in VrfFacade::TryGetEntityGeodetic: after the typed entity/aggregate
casts, fall back to the C++ static_cast base-state read. Builds 0/0.

*** CORRECTION 2026-07-19: THE "RTTI ACROSS THE MAK DLL BOUNDARY" DIAGNOSIS ABOVE IS WRONG.
*** BUT NOTE, RETRACTED 2026-07-19 LATE: this correction ALSO claimed the static_cast
fallback "has been REMOVED" and that aggregates "are now resolved" through the typed list.
BOTH OF THOSE ARE FALSE. That code was REVERTED (commit 5d14eda) because it broke object
creation, and VrfFacade.cpp:735 STILL CONTAINS the blind static_cast today.
`resolveStateRep` does not exist in tracked source. THE HEADER ANALYSIS BELOW IS SOUND AND
STANDS; only the "and it was fixed" half is false. Do not scope the native re-attempt as
though the UB were already gone. ***
The dynamic_cast does not "miss": in an HLA build
DtReflectedExtAggregate does not derive DtReflectedAggregate AT ALL. MAK says so in the header
(`reflectedExtAggregate.h:15-19`: "In HLA we need to derive from DtReflectedObject instead of
DtReflectedAggregate so that we can make use of the constructor that takes a state repository").
There is no such base subobject, so returning null is the dynamic_cast behaving CORRECTLY. RTTI
is not involved and was never the problem. Aggregates are now resolved positively through the
UUID manager's own typed aggregate list (`DtReflectedExtAggregateList::lookupEA`, plus a
pointer-identity scan as a second path), which yields a correctly-typed DtReflectedExtAggregate*
and needs no cast. Aggregate position VALUES are unchanged - the blind cast had been landing, by
exact vtable-slot alignment, on the very extAggregateStateRep() the new code calls directly.
Full derivation with header citations: the comment above `resolveStateRep` in
src/VrfFacade/VrfFacade.cpp. *** NOT yet live-verified: the very next run's creates
fired ZERO ObjectCreated callbacks - the federation had DEGRADED after ~5 runs (accumulated
VR-Forces entities + the early force-killed 3210 federate). Recover per sec 5 (reload the
VR-Forces scenario in the GUI to clear accumulated entities / stale federates), then re-run
the golden move order and confirm 14.MechBn tasks (point 0 -> route -> move -> TASKCMPLT).
OPERATIONAL NOTE for repeated live runs: entities VR-Forces creates on the interface's behalf
PERSIST across a clean interface resign; several back-to-back runs accumulate them and can stop
new creates from reflecting - reload the scenario between heavy runs.

## 8. Self-service VR-Forces reset (avoid the manual GUI reload) - API found 2026-07-11

The manual GUI scenario reload is needed ONLY to (a) clear accumulated entities (sec 7 note)
and (b) recover a stale federate after a force-kill (sec 5). Both are automatable via the
remote controller (`DtVrfRemoteController`, vrfcontrol/vrfRemoteController.h) - so a fresh
session need NOT wait on a human to reload:

- **`deleteObject(const DtUUID& uuid, addr = DtSimSendToAll)`** (:1283) - the direct counterpart
  to `createEntity`; "Delete VR-Force's object by name". SURGICAL FIX for accumulation: the app
  already tracks every created uuid in `_vrfUuidByName` (entities, aggregates, routes, areas), so
  on clean-stop it can `deleteObject` each one and leave the federation as it found it - no
  reload. (Delete BEFORE resign, and tick a few times to flush the messages.)
- **`loadScenario(const DtFilename& scnx, ...)`** (:528) / **`newScenario(dbname, guidbname, ...)`**
  (:451) - HARD reset: reload the scenario (or start a fresh one), a full clean slate that also
  clears orphans from crashes/force-kills that per-object delete cannot reach. An ALTERNATIVE hard
  reset (Option 2 below), but needs the scenario file / terrain-db names the GUI uses (bogoland has
  none on disk) - so `tools/ResetVrf` was built on Option 1 (delete-all-reflected) instead.
- `vrlinkNetworkInterface::removeAndDeleteAll()` / `resetSimulation()` exist too, but are
  network-interface-level (may only clear the LOCAL reflected view, not command the backend);
  `deleteObject` / `loadScenario` are the backend-commanding calls - prefer those.

Solution A IMPLEMENTED + LIVE-VERIFIED (2026-07-11): `VrfFacade::DeleteObject(uuid)` -> bridge ->
`VrfC2SimService` deletes every created uuid (tracked in `_vrfUuidByName`) on clean-stop, before
resign (opt-out: `Vrf:CleanupCreatedOnStop=false`). The tick loop now runs on `_stopTick` (not the
host token) so cleanup can enqueue + flush deletes while it is still ticking. Live: a COA-STP1 run
logged "Cleanup: deleting 164 created VR-Forces objects ... 164 deletes dispatched (1566 ms)" then
resigned clean. (Whether VRF fully REMOVES all 164 - incl. disaggregated aggregates/routes - is a
GUI/next-run confirmation.)

SOLUTION A IS NOT COMPLETE CLEANUP - it can MISS objects (2026-07-11, live). After a COA-STP1 run
where Solution A dispatched 168 deletes and resigned clean, a ResetVrf pass STILL found 2 leftover
tactical graphics (one a route "T23_AOA...", user-spotted on the GUI) and deleted them; a confirming
dry-run then found 0. Cause: a race - an object CREATED shortly before clean-stop (e.g. a route from
a task dispatched late, or - here - from a second order push) may not be in `_vrfUuidByName` when the
cleanup enumerates it, or its create/delete does not drain in the bounded window. So Solution A is
best-effort; ResetVrf (this section) is the authoritative sweep that catches what it misses. Run
ResetVrf after a heavy/re-pushed run to guarantee a clean slate. (Possible Solution-A hardening: a
short settle before the cleanup snapshot, or delete-by-reflected like ResetVrf - but ResetVrf already
covers it, so lower priority.)

ResetVrf (hard reset) - DONE + LIVE-VERIFIED (2026-07-11), Option 1 "delete-all-reflected"
(file-free, clears ANY orphan). With Solution A working this is a RECOVERY lever (clears ORPHANS
from crashes/force-kills that Solution A can't reach). It is `tools/ResetVrf`, a pure-VR-Forces
mini-host (SmokeTest-shaped: bare VrfBridge reference + Ijwhost copy, NO C2SIM/STOMP):

1. Facade (implemented): `VrfFacade::BeginTrackingReflectedObjects()` registers the UUID network
   manager's per-type change callbacks (addEntity/Aggregate/EnvironmentalUUIDChangedCallback), each
   accumulating the resolved uuid into a `std::set<std::string>`; `GetAllReflectedUuids()` snapshots
   it. This is HOW you enumerate reflected objects: the base reflected lists (DtReflectedEntityList /
   DtReflectedAggregateList / DtReflectedControlObjectList) expose only first()/last() - NO iterator -
   so callback-collection is the way. The change callback (matches makVrf::DtUUIDChangedCallback:
   void(DtReflectedObject*, const DtUUID&, void*)) is a STATIC member of VrfFacade::Impl so it can
   legally name the private Impl AND keep all Dt* out of VrfFacade.h. Register BEFORE the first Tick().
2. Bridge (implemented): `BeginTrackingReflectedObjects()` + `IEnumerable<String^>^ GetAllReflectedUuids()`.
   Deletes reuse the existing `DeleteObject(uuid)` (the BACKEND-commanding call - NOT the local-only
   `removeAndDeleteAll`/`resetSimulation`, which would only clear THIS federate's reflected view).
3. Tool `tools/ResetVrf/Program.cs`: StartupConfig (HLA, CWIX-2024, FED/FOM matching appsettings,
   fresh appNumber) -> `bridge.Start()` to JOIN -> `BeginTrackingReflectedObjects()` -> tick until
   the discovered count stops growing (settle, cap 20 s) -> `GetAllReflectedUuids()` -> DeleteObject
   each (skipping NIL uuids, below) -> tick ~3 s to flush -> `bridge.Stop()` to RESIGN cleanly.

RUN IT (same LAUNCH ENV as the app - RTI 4.6.1 on PATH, MAKLMGRD_LICENSE_FILE from Machine, cwd =
VRF bin64, FRESH appNumber; sec 7). PowerShell:
```
$env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
$env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
Push-Location C:\MAK\vrforces5.0.2\bin64
& <repo>\tools\ResetVrf\bin\Release\net10.0\win-x64\ResetVrf.exe <freshAppNo> [--dry-run]
Pop-Location
```
Args: `[applicationNumber] [federation] [--dry-run]` (defaults 3299 / CWIX-2024). `--dry-run` (alias
`--list`) DISCOVERS + reports only, issues NO deletes - read-only, safe to see what is present first.

NIL-UUID FILTER: discovery can surface `VRF_UUID:0:0:0` (the entity-identifier nil) - a transient
pre-resolution form / backend artifact, NOT a created object (the change callback can fire once with
the nil id then again with the resolved GUID, so both land in the set). ResetVrf SKIPS nil uuids
(`:0:0:0` suffix or an all-zero GUID); they vanish on their own once the real objects are deleted.

LIVE-VERIFIED (2026-07-11), rigorous discover->delete->re-discover protocol (GUI-independent):
- dry-run (appNo 3271): 2 deletable objects present -> join+resign -> objects INTACT (the CONTROL: a
  join+resign WITHOUT a delete leaves them, so resign is not what removes them).
- real reset (appNo 3272): discovered 3 (2 deletable + 1 nil skipped) -> 2 deleteObject issued -> resign.
- fresh dry-run (appNo 3273): a brand-new federate discovered 0 objects. So deleteObject REMOVED them
  from the BACKEND (not just this federate's view). The controlled comparison (dry-run left them,
  real-run removed them) isolates deleteObject as the cause. Every run joined RTI 4.6.1 and resigned
  clean (no stale federate).

Option 2 (NOT built) - `loadScenario(scnx)` / `newScenario(dbname,guidbname)`: simpler facade (one
call) but the GUI scenario "bogoland" (a built-in MAK terrain) has NO loadable .scnx on disk (search
of C:\MAK, ~/Documents, the profile found only map images), so loadScenario has no file to point at
without the user exporting one. Signatures: vrfcontrol/vrfRemoteController.h :528 (loadScenario) /
:451 (newScenario). Option 1 above is file-free and clears ANY orphan, so it is preferred.
