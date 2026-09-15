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

    # licence: User scope FIRST, Machine only as a fallback - they disagree (0.5.15)
    $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','User')
    if (-not $env:MAKLMGRD_LICENSE_FILE) { $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine') }
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

*** ANSWERED 2026-09-01: it is ONCE PER REBOOT, not once per machine. After a reboot
(no rtiAssistant running), the FIRST federate to contact the RTI (P1 RUN 1: RtiProbe,
appNo 3611) raised the dialog again despite the persisted checkbox, and blocked behind
it for 625 s. The scripted answer is scripts/AnswerRtiDialog.ps1 (see 0.5.4); the
runner now arms a watcher for it around Stage 2c. ***

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

### 0.5.4 AUTOMATING THE DIALOG (it RETURNS after every reboot - see the 0.5.3 note)

*** SCRIPTED 2026-09-01: scripts/AnswerRtiDialog.ps1 implements this recipe (handle
from the assistant process's MainWindowHandle; DPI-aware Connect click at
window-relative ratio 0.668,0.949 = the (383,553)-on-573x583 below; exit 0 only when
the window is confirmed gone; touches nothing if no dialog exists). Verified live
2026-09-01: dialog answered, rtiexec+rtiForwarder spawned, the blocked RtiProbe
unblocked. P/Invoke trap recorded there: FindWindowW without CharSet=Unicode marshals
ANSI and never matches - the process-handle path avoids it. ***

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

### 0.5.5 RTI_ASSISTANT_DISABLE - ONLY WITH A rid THAT CONFIGURES THE CONNECTION

Never set it ALONE. The Assistant does TWO jobs - prompting (unwanted) and supplying the
connection (REQUIRED). Disable it with a stock `rid.mtl` (`RTI_configureConnectionWithRid 0`)
and nothing supplies connection values: federates join but NEVER DISCOVER EACH OTHER and the
movement oracle goes SILENTLY BLIND (`reflected=0 readable=0` for 40 s against a back-end
whose log proved the scenario was loaded). RefMan App. A, verbatim: `RTI_mcastDiscoveryEnabled`
"is set to 0 unless RTI_configureConnectionWithRid is set to 1". CORRECTED 2026-09-03
(PREREG_52_LAUNCH; RefMan 5.2.10): with a repo-owned rid copy that DOES configure the
connection the pair is safe, and is the assistant-free launch this project uses. SUPERSEDED IN
PART 2026-09-04 (PREREG_52_RTIEXEC): the first such rid, `rid-461-ridconfigured.mtl`, was
LIGHTWEIGHT, which UG52 5.5.1 p190 forbids with VR-Forces - every observer under it saw 0
entities; the 5.2 rid is now `config\rid-501-rtiexec-min.mtl` (0.5.13). NEVER edit the shared
rid.mtl, and every federate in a run must point at the SAME rid file or they do not share a
connection (RefMan: they must also agree on `RTI_useRtiExec`).

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

*** SUPERSEDED CLAIM (the "it was fixed" half is FALSE - see the retraction below): "the baseline objects are POSITIONLESS" WAS AN ARTEFACT
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
baseline objects are positionless" is *** THIRD ATTEMPT AT THIS CORRECTION; THE FIRST TWO WERE BOTH WRONG. Re-derived from the
.oob AND from every trace, 2026-07-20. AUTHORED positions: GlblTerrDmg (d39a55ad) and
GlobalEnv (f864e51f) at ECEF (6378137,1,1) = null island; Page-In Area (cde66adc) at
34.615N/-116.55W, a REAL position. REFLECTED values, which is what matters:
  d39a55ad  NEVER APPEARS IN ANY TRACE AT ALL - 0 samples. That is why readable=2 of
            reflected=3. Its position has never been read because it never reflects.
  f864e51f  1388 samples, TWO distinct values: "NaN,-90.000000,NaN" and
            "0.000000,-90.000000,6.4e72". NEVER 9e-6. Its authored null-island position
            has NEVER been read either - the readings are garbage, not a faithful null.
  cde66adc  1390 samples, FOUR distinct values for a STATIC object: 90/-90/0.0,
            NaN/-90/NaN, 0.000001/-90/1.02e15, 0.000001/-90/6.4e72.
SO: the bad cast corrupts BOTH readable objects, not one. An earlier version of this
correction said "2 of 3 are genuinely positionless as claimed, only the Page-In Area is
cast-corrupted" - THAT IS FALSE. Neither readable object's true position has ever been
seen, and "reflects as 90/-90" holds in only 2 of the 5 observed value-forms.
CONSEQUENCE FOR THE NATIVE RE-ATTEMPT: a control-object-aware accessor is needed for BOTH
readable objects, and a NaN row from GlobalEnv must NOT be read as correct-and-expected. *** The wording below called it simply an artefact rather than VR-Forces
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

BECAUSE THE READABLE BASELINE OBJECTS REFLECT CAST-CORRUPTED GARBAGE (NOT
positionless - see the census in CORRECTIONS_LOG.md), THE "STRONGER CHECK" BELOW IS IN
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
*** FALSE - RETRACTED 2026-07-19 LATE. They STILL emit degenerate POS lines: 28 of them
in the most recent pre-check (runs/20260719T222134Z_run/watchvrf-precheck.csv). The fix
that would have suppressed them WENT OUT WITH THE NATIVE REVERT (5d14eda). Do NOT calibrate
against an absence that does not occur. Superseded text follows: ***
NO POS line at all (see the correction above; before that they emitted garbage rows
that looked like data). Do NOT conclude the oracle is broken from their absence -
create a real entity and check that.

### 0.5.7a WATCHVRF TRACE LINE TYPES

One process, one stream, ONE CLOCK BASE (elapsed seconds from the same UTC start),
so every line type is directly comparable on a single timeline. Records are
interleaved and read LINE BY LINE; each record is exactly one physical line.
Field 0 is the type tag. This list is APPEND-ONLY - POS and CON have never changed
shape and consumers may rely on that.

    POS,<t>,<uuid>,<latDeg>,<lonDeg>,<altM>   per object, per sample tick - the movement oracle
    CON,<t>,<uuid>,<notifyLevel>,<message>    Object Console warning for that object
    TSK,<t>,<unitMarking>,<taskType>          task-completion report
    RPT,<t>,<text>                            radio text-report (carries VR-Forces marking text)
    # ...                                     human-readable comment/summary line

These FOUR record types (POS/CON/TSK/RPT) plus # comment lines are everything WatchVrf
emits. Worked examples:

    POS,23.5,VRF_UUID:adfaadb3-...,34.517156,-116.973525,1060.7
    CON,12.5,1:1:0:2001,1,"route failed, ""no path"""
    TSK,31,"A Co, 1st","move-along"
    RPT,7.25,"POSITION ""tank1"" 39.0 -76.0"

*** A RAW record type (raw lastSetLocation vs dead-reckoned location) and a BCON backend-
console type, plus LogObjectConsoleToFile / SetObjectNotifyLevel bridge functions, were
DESIGNED but WENT OUT WITH THE NATIVE REVERT (commit 5d14eda) and DO NOT EXIST. WatchVrf
emits only the four types above. The raw-vs-DR oracle test that RAW was for is UN-BUILT -
see HANDOFF sec 2 and sec 5 before scoping it; do not build a trace consumer that expects
RAW or BCON lines. ***

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

- FIRST, FOR ANY "WHY DID THE UNIT DO THAT" QUESTION (added 2026-09-06, PREREG_CONSOLE_
  CHANNEL): THE OBJECT'S OWN CONSOLE. Every VR-Forces object narrates what its controllers
  do on a per-object console (UG52 21.9.1 p483, level 0 fatal .. 4 debug; the installed
  vrfSim.mtl leaves objects at 1 = warnings only). Open it from the app - env
  `Vrf__ObjectConsoleNotifyLevel=4` (setting Vrf:ObjectConsoleNotifyLevel; default -1 leaves
  the vendor default) - which calls DtVrfRemoteController::setObjectNotifyLevel for every
  object we create and for the tasked aggregate's members. The COMPLETE capture is the
  WatchVrf trace's `CON,` rows (the app's own callback receives only a subset, by a rule not
  established - own units in one run, member warnings in another; the app log shows what it
  gets decoded as "VRF console [level] name (uuid): text"). Read it with
  `python tools/analysis/console_narrative.py runs/<run> [<unit name>|<uuid tail>] [--all]`.
  One run at level 4 closed a week-old movement question (DESIGN_ORBAT C1b/C11). Volume:
  ~170k rows per 7-minute run at level 4 - fine for a diagnostic run, not a default.
  `-BackendNotifyLevel 4` (runner) raises the SIM-WIDE log instead; it did not help there.
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
unattended run. (The "not an edge case" claim that stood here is RETRACTED - see the correction above: the modal is INTERMITTENT. NOTE the evidence: it is named in ZERO of six logs, including the run where it DID appear - so log absence proves nothing here. The valid evidence is that FOUR teardowns completed cleanly..)
ANSWERED "No" - the application is being closed anyway, so leaving the terrain loaded is
the smaller state change, and answering No unblocked the teardown by hand.
DO NOT TICK "Execute session changes without prompting." It persistently mutates the
user's VR-Forces configuration and would silently change future INTERACTIVE sessions too.
The fix belongs in our script, not in their config.
*** UNVERIFIED - THE DESCENDANT SCAN HAS NEVER BEEN EXERCISED BY A REAL OCCURRENCE. On the
only run since it shipped, the modal did not appear AND no nested-window lines were logged
at all. Do not treat the rest of this paragraph as tested behaviour. ***
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
IT DID NOT CONTAMINATE THE 2026-07-19 RUNS: the three scored runs produced 4512 / 4464 /
4556 POS lines (161438Z / 202349Z / 222134Z) - do NOT validate a bridge rebuild against
"4512 each", that was true only when the set was 144109Z + 161438Z - and no run artifact
contains "FDD" or "Failed to open". A federate that could not open the
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

### 0.5.10 HISTORICAL - claims written into this section that were WRONG

Kept as tripwires only, so they are not re-derived (all 2026-07-18):
1. "rtiexec is spawned automatically on first federate join" and 3. "UDP 4000
   bound means the back-end joined" - both connection-dependent, never oracles
   (0.5.6). 2. "rtiexec NEVER runs BECAUSE (setqb RTI_useRtiExec 0)" - that
   parameter, and every rid connection value, is INERT unless
   `RTI_configureConnectionWithRid` is 1 (RefMan App. A). It is 1 in the repo rid
   (0.5.5), 0 in the stock one. 4. `RTI_ASSISTANT_DISABLE=1` presented as "THE
   FIX ... VERIFIED" - it is only safe WITH such a rid; see the corrected 0.5.5.
The "ResetVrf discovers 0 objects" residual is RESOLVED (0.4 gate passed twice).

### 0.5.11 THE RUNNER - turnaround switches (added 2026-09-01; CONFIRMED 2026-09-02 by runs 20260901T235823Z and 20260902T003710Z)

MEASURED (PREREG_RUNNER_CONFIRM_2026-09-01.md sec 6): R9 at 1x with `-RunSecs 420
-StopWhenComplete` ran start -> manifest in 7 min 9 s (P2c 26 min 23 s); 3/3 TASKCMPLT at
the P2c offsets; both observers took the stop-file path and exited ~2 s after the touch;
RTI trio untouched. CAVEAT: with SettleHoldSecs 60 (== the report cadence) StopIface can
cut a report round mid-emission, so a taskee's LAST RPT can predate its own completion
(11.8 m off while POS matched P2c to 0.00 m). RULED 2026-09-02: the hold is now
EVIDENCE-BASED (item 2 rule 4 below), RE-CONFIRMED by run 20260902T003710Z
(PREREG_RUNNER_CONFIRM2 sec 6): POS==RPT 0.0 m for all three taskees, window closed
64.6 s after the last completion. -StopWhenComplete runs are valid for POS==RPT
adjudication (n=1 under rule 4).

scripts/RunC2SimScenario.ps1 is the one-button run (HEADLESS_RUN_PLAN sec 2). Two
turnaround changes landed on branch runner-turnaround; full design, alternatives
and the before/after wall-time budget are in docs/RUNNER_TURNAROUND_2026-09-01.md.

1. THE TRACE ENDS WITH THE WINDOW (default, no switch). The observers (WatchVrf-trace,
   ListenReports) are still given the worst-case duration SUM as an argument, but it is
   now only the CAP. At teardown the runner touches `<runDir>\observers.stop` at
   StopIface + TrailSecs; both tools poll for it once a second and then take their
   NORMAL exit path - WatchVrf resigns via bridge.Stop(), ListenReports disconnects
   and writes reports-captured.log. Nothing is killed (sec 0 still applies). Before
   launch (Stage 0b) the runner probes each tool with `--capabilities` and passes
   `--stop-file` ONLY if the deployed binary advertises `stop-file`; an old binary
   makes the runner WARN and fall back to the duration-only behaviour of the record
   (manifest `inputs.traceStop.mode` = stop-file | partial | duration-only). Measured
   dead time removed: 8 min 21 s per run (both 2026-09-01 runs).
2. `-StopWhenComplete` (OFF by default - keep it off for the one canonical
   fixed-window run per milestone). Closes the observation window early once EVERY
   PerformingEntity in the pushed order has a `SENT TASK STATUS REPORT (TASKCMPLT)`
   line in vrfc2simapp.log, the TASKCMPLT line count is >= the order's task count,
   `-SettleHoldSecs` (default 60) have elapsed since the poll that first saw that
   (a FLOOR), AND (rule 4, 2026-09-02) every taskee has an `RPT POSITION` line in
   the live watchvrf-trace.csv that is LATER than its `TSK` record and within 2 m of
   its latest real `POS`. VR-Forces text reports come in ~60 s rounds (~44 lines over
   ~10 s), so expect the close at last completion + 60..70 s, one round later if the
   first round caught a still-converging aggregate centre. Only lines for taskees IN
   THE ORDER count (a stray line for a foreign unit is ignored). RunSecs stays the
   cap. Manifest `oracle.earlyExit` records whether it fired, when, and the per-taskee
   report evidence (with the reason while pending). A 2/3 outcome (like run
   20260901T221227Z) never fires - the window runs to the cap, as it must; so does a
   taskee whose marking can not be mapped to a VRF_UUID (no route line).
   MULTI-TASK ORDERS: two tasks dispatched SIMULTANEOUSLY to one taskee are SUPERSEDED
   by VR-Forces ("the old task will not complete", VrfC2SimService.cs:954) and yield
   ONE TASKCMPLT line, so the count never reaches the task count and the switch is
   INERT (the window runs to -RunSecs; safe, no truncation). SEQUENCED (gated) tasks
   complete one after another and DO fire it. A fan-out task counts ONCE (one
   synthesized unit-level line). Design note sec 3.
3. `-TraceStopGraceSec` (default 120): how long teardown waits for the observers
   after the stop-file touch before deciding one did not see it. If one is still
   running then, teardown WARNs and KEEPS WAITING up to that observer's own duration
   cap + 30 s (stage start + cap argument + margin) - never kills - so StopVrf never
   runs under a possibly still-joined observer. Stage 1 (pre-flight) REFUSES to launch
   while any WatchVrf / ListenReports process exists (report, never kill; it ends on
   its own cap - wait it out). A pre-existing `<runDir>\observers.stop` is refused
   with exit 2 before anything is created (run-directory collision; never deleted).

Offline gate: `pwsh -NoProfile -File tests\RunnerTurnaround.Tests.ps1` (96 checks,
no sim). What the confirming live run must show is listed in the design note sec 4;
`# STOP requested via stop-file` in the WatchVrf trace followed by a clean resign is
the load-bearing line.

---

### 0.5.12 A CRASHED BACK-END PARKED ON THE MAK DUMP PROMPT (scripts/AnswerCrashDumpDialog.ps1)

SYMPTOM: `Get-Process vrfSim*` shows a vrfSimHLA1516e whose MainWindowTitle is
`vrfSim5.0.2-MSVC++15.0_64-249613-<pid>.dmp`. That is MAK's crash handler: the federate
is ALREADY DEAD (no HLA traffic, StopVrf exits 3 and correctly refuses to kill it) and is
waiting on a Qt message box "A fatal error has occurred. Would you like to save a
diagnostic file?" [Yes] [No]; after Yes a second box "Saved dump file to '...'" [OK].
The .dmp lands in `C:\MAK\vrforces5.0.2\bin64\` (older ones from 2023-12, 2026-07-14/15/22
are there too - do not confuse them with yours; match the pid).

RULING (2026-09-02, user-confirmed): ALWAYS ANSWER YES. The dump is the evidence a MAK
case needs and the vendor's own intended action; declining buys nothing - the process is
dead either way. Then quit vrfGui with StopVrf.ps1 (its normal path; the back-end is
already gone so only the GUI confirm runs). Never force-kill the parked federate.

USE THE SCRIPT:

    pwsh -File scripts\AnswerCrashDumpDialog.ps1            # add -DryRun to inspect first
    pwsh -File scripts\StopVrf.ps1

Exit codes: 0 answered + process exited (dump named in the output); 1 no such prompt
(nothing touched); 2 posted but still present after -ConfirmSec (inspect by hand);
3 error. It only ever posts to a top-level window owned by a vrfSim* process whose
title matches `^vrfSim.*\.dmp$`.

HOW IT WORKS AND WHAT DOES NOT - so nobody relearns this (cost: most of a session on
2026-09-02 after run 20260902T011908Z):
- The box is Qt: no Win32 child controls, so FindWindowEx/BM_CLICK find nothing (same
  family as the RTI dialog in 0.5.4; unlike the vrfGui quit confirm in 0.5.9, which has a
  UIA tree), and posted WM_LBUTTONDOWN/UP do nothing.
- SetForegroundWindow + SendKeys from a background shell FAILS (foreground lock).
- Coordinate clicks (the 0.5.4 recipe) are swallowed while ANY Windows Security prompt is
  on screen - its full-screen `Shell_SystemDim` (owner PickerHost) takes every click (a
  testhost firewall prompt sat on the box for hours on 2026-09-02). WindowFromPoint on
  the target before clicking is the tell.
- Posted WM_SETFOCUS + WM_KEYDOWN/WM_CHAR/WM_KEYUP VK_RETURN WORKS without focus or
  foreground and passes the dim layer. Enter = the default button = Yes on the first box
  and OK on the second (screenshot-verified). That is the script's mechanism.
- To SEE a hidden dialog: move it with SetWindowPos(SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE)
  and screenshot its rect with System.Drawing CopyFromScreen (scratchpad only).

THE testhost FIREWALL PROMPT (`dotnet test` copies testhost.exe into every test bin, and
each NEW PATH prompts once): a NUISANCE, not a blocker - vstest talks over loopback,
which the firewall does not filter, so tests pass answered or not. RULING (user,
2026-09-02): CANCEL the prompt; do NOT `Set-NetFirewallProfile -NotifyOnListen False`
(machine-wide, too broad). Least-privilege silence if it keeps costing attention: a
per-path inbound BLOCK rule per testhost.exe copy (grants nothing, loopback unaffected).

---

### 0.5.13 THE 5.2 PROFILE (VR-Forces 5.2d; added 2026-09-03, RTI posture 2026-09-04)

ONE SWITCH runs the whole pipeline on the 5.2d stack (5.0.2 unchanged, still the default):
`pwsh -File scripts\RunC2SimScenario.ps1 -VrfProfile 5.2 [-NoGui]`. It DERIVES the roots
(`vrforces5.2d`+`vrlink5.10`+`makRti5.0.1`; 4.6.1 appears NOWHERE here), LaunchVrf52/StopVrf52,
the `Release-5.2` binaries, the config-file identity (`MAK-ONE-2025-Config.xml`, FOM list
CLEARED - config modules are ADDITIVE), `data/unit-type-map-52.json` and the per-child
environment; `-VrfProfile 5.2 -DryRun` prints every value and a hand-passed -VrfRoot/
-VrLinkRoot/-RtiDir/-Federation is REFUSED. GUI ON by default; no dialog watcher is armed
(0.5.5); FixtureGen's `--out-dir` stays the only sanctioned write under `C:\MAK`. RTI POSTURE
- NOT A KNOB (UG52 5.5.1 p190 "You cannot use the MAK RTI in lightweight mode with
VR-Forces"; PREREG_52_RTIEXEC: rtiexec mode reflected 62 entities, lightweight 0):
RTIEXEC MODE on `config\rid-501-rtiexec-min.mtl`, which EVERY child must share as
`RTI_RID_FILE` (federates that do not share it do not share a connection), plus
`RTI_ASSISTANT_DISABLE=1`. `scripts\StartRtiExec52.ps1` (runner Stage 2r, before the RtiProbe
gate) ensures a headless rtiexec LISTENS on TCP 4001; it is NEVER killed or restarted and
PERSISTS ACROSS RUNS. `-DeviceAddress` is NOT part of the repair - run 3857 reflected 54-56
entities with the observer's device address blank - so it defaults to EMPTY and nothing is
passed (the sim-side arm is still open); the rid's `RTI_networkInterfaceAddr` is another
layer. The lightweight `rid-461-ridconfigured.mtl` is unreachable from here.
STARTUP CRASH - CAUSE FOUND, AND IT WAS OURS (PREREG_52_CRASH_BISECT_2026-09-04 sec 5): passing `--logFileName` crashed the sim in `DtVrfSimOptions::parseCmdLine` (0xC0000005) in 6 of 18 launches, omitting it 0 of 12 (Fisher one-sided p=0.031); a 22-character vendor-default path crashed too, so it is the OPTION, not the path.
LaunchVrf52 therefore does NOT pass it (`-LogFileName <path>` re-enables it deliberately, at ~1 crash in 3 - bisect repeats and MAK bug reports only) and HARVESTS the vendor's own `C:\MAK\logs\vrfSim*-<pid>.log` for that pid into `runs\launch52` at READY and on the crash path, before the corpse is closed (a COPY, a snapshot; one marker line -> manifest `inputs.vrfProfile.vendorLog`; missing = loud WARN, verdict unchanged). Its poll still FAILS a launch (exit 3, no retry) on a vanished back-end, a MAK crash-box title, or a new `C:\MAK\logs\*-<pid>.callstack.log`, whose first frames it prints.
SECRETS: that harvested copy carries the FULL PROCESS ENVIRONMENT IN CLEARTEXT (`DtPrintEnvironmentVariables` at notifyLevel 3; FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 10) - NEVER attach it to a ticket, mail or issue; send the `.callstack.log` / `.dmp` instead. It is not scrubbed, by decision.
LICENCE CAP UNTESTED: the rtiexec log shows at most TWO federates joined, so the unlicensed-
for-two cap (RTI UG 8.2) is unexercised on 5.2; the first FOUR-federate run must confirm
`HaveRtiLicense()=1` on EVERY federate (`WatchVrf --diag` prints it). MANIFEST: profile, rid +
sha256, connection mode, the device address used, rtiexec/forwarder pids and the app's
`VrfBridge native stack` line - the RUNTIME stack, which is what a trace compares against.

### 0.5.14 LAUNCHING A RUN - the wrapper, the markers, exit 127, the 64-bit rule (2026-09-14)

LAUNCH EVERY LIVE RUN THROUGH `scripts\RunScenario.sh`. It is a parameterised template
(`--help` lists every option; anything after `--` goes to the runner unchanged) and it is
the only supported launch path. Three defects met in the ad-hoc wrapper that launched G6
on 2026-09-14; each is now closed by something this section names
(docs/experiments/RUNNER_HARDENING_2026-09-14.md).

1. 64-BIT ONLY. Bare `pwsh` on this machine resolves to `C:\Program Files (x86)\PowerShell\7`
   - the 32-BIT build, ~2 GB of address space - because that PATH entry precedes the 64-bit
   one. A 32-bit host died of address-space exhaustion in the observation loop on
   2026-09-07, the mitigation recorded then was a PROCEDURE, and the procedure lapsed
   silently. It is now a GATE at both layers: `RunScenario.sh` pins
   `C:\Program Files\PowerShell\7\pwsh.exe` and verifies `Is64BitProcess`, and
   `RunC2SimScenario.ps1` refuses a 32-bit host with exit 2 before anything is allocated.

2. STDOUT TO A FILE. NEVER PIPE THE RUNNER. NEVER `| tee` IT. `Start-External`
   (RunC2SimScenario.ps1) starts every child through `Start-Process` with redirection, and
   .NET's `Process.Start` then calls `CreateProcess` with `bInheritHandles=TRUE`, which
   duplicates EVERY inheritable handle into the child - including whatever the runner holds
   as its own stdout. In G6 that was the harness's pipe: `VrfC2SimApp` never wrote to it but
   held it open, and the harness did not see EOF for nine hours. No `Start-Process` switch
   suppresses this, so the fix is upstream - give the runner a FILE for stdout and stderr and
   `/dev/null` for stdin, which `RunScenario.sh` does. `| tee` additionally makes `$?` the
   exit status of `tee`. Watch a run from ANOTHER shell with `tail -f <the log>`. The same
   rule applies to any background subshell in a wrapper: give it `< /dev/null` too.

3. THE TEARDOWN BACKSTOP AND ITS TWO MARKERS. The runner's teardown is a `finally` and it
   covers every in-process path - but on 2026-09-14 the runner was TERMINATED from outside,
   and no `finally`, `trap` or `PowerShell.Exiting` handler survives `TerminateProcess`.
   VR-Forces and the interface stayed joined for nine hours. The backstop is therefore
   OUT-OF-PROCESS, in the wrapper, and reads two marker files in the run directory:

       runner.launched       written the instant VR-Forces became this run's to stop.
                             Contains the runner's PID. ABSENT => the run launched nothing,
                             and the wrapper tears down NOTHING (a foreign live session
                             must never be touched - sec 0).
       runner.teardown-ran   written as the last statement of the runner's finally.

   launched AND NOT teardown-ran => the wrapper runs StopIface (clean resign), then
   StopVrf52/StopVrf, then touches `observers.stop`, and prints what is still up. Nothing is
   force-killed there either. The wrapper dying TOO is covered by a second, independent
   backstop - item 6.

4. `runner exit: 127` DOES NOT MEAN "COMMAND NOT FOUND" and does not mean PowerShell failed.
   On this MSYS bash it means the child exited with a HIGH-BIT (NTSTATUS-shaped) Windows exit
   code that MSYS does not map to a signal. Of the codes that produce it, exactly one is
   SILENT: `0xFFFFFFFF` (-1), which is what `TerminateProcess(handle, -1)` writes - i.e. .NET
   `Process.Kill()` / PowerShell `Stop-Process`. The other three (`0xC0000409`, `0xC00000FD`,
   `0xE0434352`) are CLR fatal errors that print to stderr AND raise a WER / Application Error
   event. So 127 + a silent log + no WER event = THE RUNNER WAS KILLED FROM OUTSIDE. For
   contrast: `0xC0000017` (OS out of memory) is bash 138 "Bus error" and `0xC0000005` is bash
   139 "Segmentation fault" - neither is 127. The runner's own codes are 0/2/3/4/5 only.
   `RunScenario.sh` prints this legend after every run.

5. NO PROCESS SWEEPS DURING A RUN WINDOW. `Stop-Process` / `taskkill` sweeps are BANNED while
   a run is open - a sweep filtered on a CommandLine substring is the shape that fits every
   observation of the G6 kill. If a cleanup is unavoidable it MUST exclude TWO pids - the
   runner's, which is the content of `<RunDir>\runner.launched`, AND the detached watchdog's,
   which is the content of `<RunDir>\watchdog.pid` (item 6; it is a direct CHILD of the
   runner, so a tree kill or a CommandLine-pattern sweep takes it along with the runner and
   removes the very backstop the sweep makes necessary) - and MUST exclude the killer's own
   shell (filter on a token the killer cannot contain; list before killing). State the quiet
   period in the run's prereg.

6. THE DETACHED WATCHDOG - the backstop for the backstop (`scripts\RunnerWatchdog.ps1`,
   2026-09-14). Item 3's wrapper backstop only runs if the wrapper's bash SURVIVES the runner.
   The watchdog covers the case it does not - a closed terminal, a kill that takes the whole
   shell, a launch path with no wrapper. The runner starts it at STAGE 3w, the instant
   `runner.launched` is written and VR-Forces becomes this run's to stop (it was stage 6b-w
   until the review of 374ea49 - finding F6 - and the window between the marker and stage 6b is
   tens of seconds to a couple of minutes, not "sub-second"). It is the one process of a run
   that OUTLIVES the runner by design.

       what it watches   the runner's PID (passed as -RunnerPid; the same number is in
                         <RunDir>\runner.launched), polled every 5 s until -MaxSec. A death
                         must be observed TWICE, 2 s apart, before anything is touched: one
                         transient failed read is not a death (finding F2).
       what it reads     nothing at all while the runner is alive.
       when it acts      runner gone AND runner.launched present AND runner.teardown-ran
                         absent AND runner.watchdog-ran absent. Then: claim
                         runner.watchdog-ran, StopIface, StopVrf52/StopVrf, touch
                         observers.stop, print the inventory.
       when it refuses   no runner.launched, or a runner.launched holding a DIFFERENT pid
                         (exit 2, touching nothing - same foreign-session rule as item 3).
       its own timer     -MaxSec expiring with the runner STILL ALIVE is exit 4 and NOTHING
                         torn down. A live run is never torn down by a timer.
       evidence          <RunDir>\runner-watchdog.log (fixed name) and watchdog.stdout.log /
                         watchdog.stderr.log; its pid is in <RunDir>\watchdog.pid and in the
                         manifest (artifacts.watchdog).
       exit codes        0 nothing owed / teardown done; 2 REFUSED; 3 teardown ran with a
                         failed step; 4 -MaxSec expired while the runner lived; 5 unexpected.

   IT IS DETACHED, AND THAT IS THE POINT. It runs in its OWN HIDDEN CONSOLE (`Start-Process`
   WITHOUT `-NoNewWindow`, which makes PowerShell pass CREATE_NEW_CONSOLE): a child that
   SHARES the runner's console receives that console's Ctrl+C / Ctrl+Break and dies when the
   terminal closes - i.e. it would die in exactly the scenario it exists for. Its stdout,
   stderr and stdin are all files in the run directory (stdin is `watchdog.stdin.empty`, the
   Windows `< /dev/null`), so it holds no handle belonging to the runner, the wrapper or the
   terminal. It force-kills NOTHING on any path and never touches rtiexec / rtiForwarder /
   rtiAssistant. `-NoWatchdog` on the runner turns it off - then item 3 is the only backstop.

   THE OVERLAP IS CLOSED (finding F3, 2026-09-14). `RunScenario.sh` now CLAIMS
   `runner.watchdog-ran` atomically (`set -C; : > ...`) before its own teardown and STANDS DOWN
   if the claim fails, so the wrapper and the watchdog can no longer tear down the same run at
   once - whichever gets the marker acts, the other says so and exits. Historic runs (and any
   launch path that is not this wrapper) can still show the old pair, an immediate wrapper
   teardown and a watchdog one ~5 s later; every step is a graceful, idempotent request, so
   that is a no-op that logs, not two failures.

   ITS BUDGET. `-MaxSec` is derived by the runner as
   `max(EffWatchSecs, DerivedWatchSecs) + LaunchSettleSec + (preCheck 30 + StageTimeoutSec) +
   StageTimeoutSec + (TraceStopGraceSec + AppExitTimeoutSec + StopVrfTimeoutSec) + 600`, capped
   at 86400 - the observation window PLUS the stages between stage 3w and the observers PLUS
   every teardown budget PLUS ten minutes of slack. It is printed in the stage banner with its
   terms. Expiry with the runner still alive is exit 4 and NOTHING torn down.

   THE RUN-DIRECTORY POINTER (finding F10). The runner writes the run directory it created into
   `runs\launch52\last-run-dir.txt`; `RunScenario.sh` deletes that file before launching and
   reads it afterwards, instead of taking the NEWEST `runs\*_run` by mtime - which is not
   necessarily the one it launched, and which any later write into another run directory can
   flip. If the pointer is missing the wrapper falls back to the mtime scan and SAYS SO.

7. `--pre-order-settle N` (wrapper) / `-PreOrderSettleSecs N` (runner) - HOLD THE ORDER BACK.
   STAGE 7d, added 2026-09-14, OFF by default (0) so a default run stays comparable with the
   record. The sectorised navigation area loads LAZILY, AFTER the entities are placed: run
   `20260914T130439Z` logged the area's "New Primary nav area" rows 175 s after the members
   were created, so a task issued before that is PLANNED WITHOUT THE MESH. With N > 0 the
   runner waits N seconds between the oracle gate and PushOrder, printing one status line
   every 30 s, and stamps `clocks.preOrderSettleStartUtc` / `EndUtc` in the manifest. The hold
   is ADDED TO THE OBSERVERS' DURATION CAP, so the trace still covers the whole run.
   IT IS A MEASUREMENT PARAMETER, NOT A FIX: it says how long we wait, never whether the mesh
   arrived - only the object consoles say that (0.5 lessons; notify level 4). The vendor-side
   alternative is UG52 Appendix C `loadAllNavigationDataOnTerrainLoad`, which loads every
   sector at terrain load instead of on demand; that is a configuration change, not a runner
   one, and it is the better answer if the hold turns out to matter.

8. `--vrf-appdata-dir DIR` (wrapper) / `-VrfAppDataDir DIR` (runner) - THE RELOCATED appData.
   5.2 ONLY, added 2026-09-14, EMPTY by default so a default run's command line is unchanged
   and VR-Forces reads `C:\MAK\vrforces5.2d\appData` exactly as before. With a directory it
   becomes `LaunchVrf52.ps1 -AppDataDir`, i.e. `--appDataDir` on BOTH the sim and the gui
   (UG52 Table 11 p178 / Table 10 p164; trustworthy only since 5.2 - VRF-9255 / VRF-9265).
   THE PREPARED TREE IS `C:\C2SIM\vrf-appdata\appData`: a full copy of the vendor appData
   (`cache\` junctioned back, so the warm terrain cache is NOT re-tiled), with exactly ONE
   line different - `(setqb loadAllNavigationDataOnTerrainLoad 1)`, which loads navigation
   data WITH the scenario instead of lazily at first entity placement (UG52 Appendix C
   p1671, the vendor-side alternative item 7 points at). Provenance and the reinstall
   procedure are in that tree's `README-C2SIM.txt`; the design record is
   `docs/experiments/APPDATA_RELOCATION_2026-09-14.md`. NOTHING under `C:\MAK` was modified,
   so ROLLBACK IS DROPPING THE OPTION.
   Pass the directory that CONTAINS `settings\` (the nested `...\vrf-appdata\appData`, not
   its parent): the runner refuses a path that is not an existing directory, and refuses the
   switch outright on the 5.0.2 profile (`LaunchVrf.ps1` has no such option); LaunchVrf52
   re-checks it and echoes the `loadAllNavigationDataOnTerrainLoad` line it actually read, so
   the run log records which way the setting was set. Ledgered as `inputs.vrfAppDataDir`.
   WATCH ITEM: `--appDataDir` is parsed by `makVrf::DtVrfSimOptions::parseCmdLine`, the same
   path in which `--logFileName` crashes ~1 launch in 3. There is no evidence it shares that
   defect, but if the sim dies at STARTUP the first thing to drop is this option.


9. `--stop-when-complete` FINALLY CLOSES A WINDOW (2026-09-14). It is ON by default in the
   wrapper and had NEVER ONCE FIRED. Condition (4) of the early exit - "post-completion
   report evidence" - demanded an **RPT** row in the WatchVrf trace, and an RPT row is a
   VR-FORCES RADIO TEXT REPORT (`tools/WatchVrf/ConFormat.cs:83-96`), emitted by a Lua
   tracker this interface never asks for and these scenarios never run. RPT = 0 in all 8
   traces of 2026-09-14, so every run burned its whole `-RunSecs` cap with its units
   already stopped: 1200.7 s instead of ~154 s in run `20260914T154243Z`, 1208.1 s instead
   of ~314 s in `20260914T130439Z`.
   Condition (4) now accepts ANY ONE of three sources, per taskee (RunnerLib
   `Test-ReportEvidence`; the manifest's `oracle.earlyExit.reportEvidence[<taskee>].via`
   records which one closed it):

       RPT            the old rule, kept unchanged - a post-completion text report within
                      `-ReportToleranceMeters` of the sampled POS. Strongest when it exists.
       C2SIM-capture  a C2SIM PositionReport for the taskee's OWN uuid, captured after that
                      taskee's TASKCMPLT, out of `reports-captured.log`. THE AUTHORITY: that
                      file dates the TASKCMPLT (a TaskStatus report) and the position fixes
                      on ONE wall clock. LIVE since 2026-09-14 pm: ListenReports appends
                      and flushes every report as it arrives (it advertises
                      `incremental-capture`), so the capture is readable DURING the window.
       R1-applog      THE LIVE STAND-IN, and the only per-taskee post-completion position
                      evidence a running runner can see: an `R1 position reports: N sent,
                      0 skipped` line appearing BELOW that taskee's TASKCMPLT line in
                      `vrfc2simapp.log`. Line order is the clock - that log has no
                      timestamps. `0 skipped` AND `sides=both` are both load-bearing: a
                      round with skips does not say which units were sent, and the side
                      filter is applied BEFORE the skip counters, so under
                      `Vrf__PositionReportSides=blue` a hostile taskee is neither sent nor
                      skipped and the round would read `0 skipped` on its behalf. Under a
                      one-sided filter this satisfier never fires.

   `-SettleHoldSecs` (60) remains the FLOOR, so the window closes 60 s after the last
   TASKCMPLT plus however long the next position report takes - about 65 s in practice at
   `Vrf__PositionReportSeconds=10`. Unsatisfied evidence still runs the window to its cap:
   the safe direction, and the per-taskee reason is printed every 30 s and ledgered.

9b. **IT STILL DID NOT FIRE - TWO MORE DEFECTS, BOTH CLOSED (2026-09-14, run
   `20260914T230706Z`).** That run pushed **41 TASKSTRT, 41 TASKCMPLT and 2 TASKABRT**
   lines for its 42-task / 11-taskee order and the runner reported *every one of the
   eleven taskees* as having no TASKCMPLT, `completionLinesSeen: 0` in the manifest, and
   ran the full 1500 s cap.

   **D1 - the parse, not the run.** `RunnerLib.ps1` `Get-CompletedTasks` matched
   `... task=(?<task>\S+?)\.?\s*$` - the task uuid had to be the LAST thing on the line.
   Commit `81d108c` ("B1: TASKSTRT at dispatch, TASKABRT for tasks that will never run")
   changed the app's own log line (`src/VrfC2SimApp/VrfC2SimService.cs:4958`) to

       SENT TASK STATUS REPORT ({Code}) taskee={Uuid} task={Task} - {Why}.

   so every line now ends with a reason, the `$` anchor never matched again, and the
   parser returned ZERO records against a perfectly healthy log. The task token is now
   delimited by a LOOKAHEAD (optional period, then whitespace or end of text): whatever
   the interface appends after it cannot break the parse again. **When the runner says a
   taskee never reported, grep the app log yourself before believing it** - this is the
   second time a healthy run was read as a failed one by a log regex (the first was the
   route-uuid parenthetical, 0.5.11).

   **D2 - TASKCMPLT is not the only end state.** The criterion was "TASKCMPLT lines >=
   order task count". A task VR-Forces FAILS is reported `TASKABRT`, and so is a successor
   the interface skips because its predecessor was abandoned - in that run one of each. A
   TASKCMPLT-only count therefore had a ceiling of 40 against a task count of 42 and could
   never be met. The rule now counts **TERMINAL** reports - `TASKCMPLT` **or** `TASKABRT` -
   and counts them **per (taskee, task) PAIR**, not by line total, so a duplicate line or
   one taskee reporting twice can no longer close a task that never ended. `TASKSTRT` is
   not terminal. A report the app cannot attribute (`task=(none)`) is counted and printed
   but closes no task. The close line now names the arithmetic:

       closed: 40 TASKCMPLT + 2 TASKABRT = 42 terminal of 42 tasks

   and the did-not-fire line names **which condition blocked it, with its numbers**
   ("BLOCKED BY condition (2) task coverage: 39 of 42 order task(s) have a terminal report
   ...") instead of the old guess "(none - the hold had not elapsed, or the line count was
   below the task count)". The manifest gains `oracle.earlyExit.tasksClosed`,
   `.terminalByCode`, `.unattributedTerminalLines` and `.failedCondition`.

   OFFLINE REPLAY of the fixed rule (RunnerLib's own functions over the finished run
   directories): `20260914T230706Z` - conditions (1)+(2) met at **23:20:12Z** (last closing
   terminal report 23:20:08.8Z), evidence in, **WOULD HAVE CLOSED at 23:21:12Z, t+655 s of
   the 1500 s cap - 845 s saved**; `20260914T224505Z` (1 task, TASKSTRT only, no terminal
   report) - **would NOT close**, blocked by condition (1), which is the correct answer.

10. `--sample-threads` IS SIZED FROM THE DERIVED WINDOW, and `--watch-secs` DEFAULTS TO 0.
   The sampler used to get `-MaxSec $((WATCH_SECS + 100))`. `--watch-secs 0` means "let the
   runner derive the cap", so that arithmetic handed `SampleThreads.ps1` **100 seconds** and
   the sampler was dead long before the order was pushed (run `20260914T164906Z`). It now
   gets `EFF_WATCH + 75 + 360 + 100`: the derived observer cap, plus the launch settle and
   pre-check spent BEFORE the observers start (the sampler's clock starts when the SIM
   appears), plus every teardown budget, plus the historical margin. The sampler exits on
   its own when the sim exits, so being generous costs nothing; being short loses the
   measurement silently.
   `--watch-secs` now DEFAULTS TO 0 = derive, and the wrapper prints the derived number
   (`observers : DERIVED 1460 (20+180+120+180+30+run 900+30+settle 0+gate 0)`). Every explicit
   value passed on 2026-09-14 was BELOW the derived cap and earned the runner's truncation
   WARN (`20260914T170824Z`: 900 < 1100) - which is the very failure the derivation exists
   to prevent. Pass a number only to deliberately shorten the observers; the wrapper then
   says `EXPLICIT n (derived would be m)` and shouts if n < m.

11. THE `t+Ns` IN THE STAGE-8b MESSAGES IS THE OBSERVATION-WINDOW CLOCK, which starts when
   **PushOrder RETURNS** - up to `-PushOrderListenSec` (30 s) after the order actually
   reached the bus. Every such message now says so, and appends the seconds since the
   ORDER record in `c2sim-bus.log` (`[HH:mm:ss.fff] ORDER (<n> chars)`, PushOrder's own
   capture) when that file has one; the moment is ledgered as `clocks.orderOnBusUtc`. Read
   `TASKCMPLT ... (t+77s)` in an older log as "77 s after PushOrder returned", never as
   "77 s after the order".

12. **HOLD THE ORDER UNTIL THE NAVIGATION AREA IS READY - `--pre-order-gate nav-area`, not a
   guessed settle.** The sectorised nav area is NOT usable when the entities are placed. It
   becomes usable at the first `New Primary nav area: | <area>` row printed by a placed
   platform, and that row partitions the move-to nav gate perfectly: every goal before it
   fails `Is current point in nav area?` and is planned by the FEATURE planner on one
   straight part - silently, at console level 3 - and every goal after it passes the nav gate
   (four single-variable runs, 2026-09-14; docs/experiments/G7B_G8_RESULTS_2026-09-14.md
   sec 1.5, resolved to 0.3 s in run D). PASSING THE GATE IS NOT THE SAME AS BEING PLANNED:
   in N2b (205046Z) every goal issued 4.7-9.7 s after the row passed both gates and the mesh
   query still returned 0 points (PREREG_N1_N2_CORRIDOR_SLOPE sec 10.1); the row is necessary,
   not sufficient, and N2c (sec 5.5) tests whether a delay after it suffices. Under `CreationPolicy=AtOrder` the order reached
   the bus 4.7-7.7 s BEFORE that row in every run, so the first legs of every demo run so
   far were feature-planned.

   The wait is **9.1-12.1 s warm and 236.9 s cold** (same tiles, same 2.33 GB streamed; the
   difference is the file cache - sec 3b). A fixed `--pre-order-settle N` cannot be right
   across a 20x spread, and `loadAllNavigationDataOnTerrainLoad = 1` does not move the row
   at all (sec 3a) - do not let it stand in for the wait.

       bash scripts/RunScenario.sh --pre-order-gate nav-area --object-console 4 ...

   The runner then polls `vrfc2simapp.log` for that row and pushes the order the MOMENT it
   appears, logging the object, the area and the delta from the first `PLACEMENT:` line -
   which it calls **WARM** (<= 60 s) or **COLD**. That delta is the cache-state indicator
   and is the number to quote when a demo is slow to start; the prepare step should load the
   scenario once beforehand so the second load is the cheap one.

   - `--pre-order-gate-timeout N` (default 300, range 30..1800) bounds the wait. On timeout
     the run STOPS (exit 3) with a NOT-READY message - UNLESS `--pre-order-settle N` is also
     given, which then becomes the fallback hold. **GATE OR SETTLE, never one after the
     other**: with both, the gate is in force and the settle is the timeout fallback only
     (`oracle.preOrderGate.fellBackToSettle` in the manifest, plus a WARN flag).
   - **REQUIRES `--object-console 3` or `4`.** The row prints at object-console level 3 and
     nowhere else, and `appsettings.json` ships `Vrf:ObjectConsoleNotifyLevel = -1`
     (consoles OFF). Both the wrapper and the runner's stage 0 REFUSE the gate below 3, with
     exit 2, before anything is launched.
   - The gate's timeout is inside the observers' derived cap and the stage-3w watchdog
     budget, exactly as the settle is; the banner prints `+ preOrderGate 300`.
   - Manifest: `clocks.preOrderGate{Start,Fired,TimedOut}Utc`, `inputs.preOrderGate`,
     `inputs.preOrderGateTimeoutSec`, and `oracle.preOrderGate` (object, uuid, area, the row
     verbatim, `firstPlacement`, `placementToAreaSec`, `cacheState`, `waitedSec`).

   Off by default, so a run without the flag behaves exactly as every run in the record.
   Mechanism and the offline replay that validated the detector:
   docs/experiments/RUNNER_HARDENING_2026-09-14.md sec 15. NOT YET PROVEN LIVE - the first
   live gated run must show the first leg mesh-planned.

---

### 0.5.15 THE LICENCE FILE - two registry scopes that disagree (added 2026-09-14)

WHERE IT LIVES: `C:\MAK\MAKLicenseManager\*.lic`. The live one, renewed 2026-09-14, is
`SALES-TEMP-10-31-26-MAK-node-locked-DEMO_1-dec-2025.lic` - node-locked DEMO, **expires
31-oct-2026**, verified with `lmutil lmdiag` for `vrfengine`, `vrfgui`,
`vrf_remote_controller` and `vrl_run`. The superseded 15-sep-2026 file is still in that
directory: do not point anything at it, and do not delete it either.

TWO SCOPES, AND THEY DISAGREE. `MAKLMGRD_LICENSE_FILE` exists in the **User** scope (the
renewed file) and in the **Machine** scope (still the old one - the elevation needed to
change it was refused). Windows composes a NEW process's environment as Machine-then-User,
so a freshly started tree gets the User value; a process that was ALREADY RUNNING when the
value changed keeps the old one and hands it to every child it starts. That is the trap:
the shell looks right, the run is wrong, and the symptom is a back-end that dies at startup
- indistinguishable at a glance from the `--logFileName` startup crash (0.5.13) - rather
than a licence error.

WHAT THE SCRIPTS DO ABOUT IT. `scripts\RunC2SimScenario.ps1` (at the Stage 0 banner, so the
runner AND every child it starts get it), `scripts\LaunchVrf52.ps1`,
`scripts\StartInterface52.ps1`, `scripts\RunScenario.sh`, `scripts\LaunchVrf.ps1` (the
5.0.2 launcher) and `scripts\Probe52Reflection.ps1` each read the registry themselves -
User scope, else Machine - set `MAKLMGRD_LICENSE_FILE` on their OWN process so children
inherit it, and print one line:

    [OK]   licence file: C:\MAK\MAKLicenseManager\SALES-TEMP-10-31-26-...lic (expires 31-oct-2026)

The expiry is field 5 of the file's first `INCREMENT` line; NOTHING else is ever read out of
the file. A path that resolves to a file that does not exist WARNS loudly and stops nothing.
`LaunchVrf52.ps1` additionally REFUSES to launch when that date is in the past (exit 2,
before any process, log or app number is spent) and takes `-LicenseFile <path>` to override
the resolution - for an install whose licence is not in the registry, and to exercise that
gate without touching the machine's environment. The SIX copies of the resolver are
deliberate (no module is shared by all six scripts): CHANGE ONE, CHANGE ALL SIX.

THE RESIDUAL IS CLOSED (2026-09-14). Two scripts still read the MACHINE scope alone, and
`LaunchVrf.ps1` did worse than read it - on its live path it ASSIGNED
`$env:MAKLMGRD_LICENSE_FILE` from Machine, OVERWRITING the per-process pin the runner had
just set, so a 5.0.2 run resolved the renewed licence and then had the lapsed one put back
underneath it. `scripts\LaunchVrf.ps1` (`$licResolved`/`$licScope`, used by the
precondition report, the dry-run plan and the live assignment) and
`scripts\Probe52Reflection.ps1` now resolve User-then-Machine like the rest, PRESERVE an
inherited value rather than replacing it with a path that resolves to nothing, and say
which scope they used. `LaunchVrf.ps1` additionally WARNS when the two scopes disagree.

ALIGN THE MACHINE SCOPE ONCE - the real fix, one elevated command:

    setx /M MAKLMGRD_LICENSE_FILE "C:\MAK\MAKLicenseManager\SALES-TEMP-10-31-26-MAK-node-locked-DEMO_1-dec-2025.lic"

After that every process agrees however it was started. `setx` writes the value for FUTURE
processes only - this session, and anything it already launched, keeps what it inherited.
Until it is run, the per-process resolution above is the only thing keeping runs on the
renewed licence.

BY HAND, in a shell that already has the stale value:

    $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','User')
    if (-not $env:MAKLMGRD_LICENSE_FILE) { $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine') }

---

## 0.5-ARCHIVE - the raw vrfSimHLA1516e headless recipe (CONFIRMED UNSAFE, 2026-07-15)

RETAINED FOR THE HISTORICAL RECORD ONLY. DO NOT USE THIS RECIPE; the supported bring-up is
0.5.1 above. Everything below predates the 2026-07-18 root cause, so read its "root cause
found" claims in that light: the crash it describes is real, but the launch-hang symptoms
discussed alongside it were the RTI Assistant prompt (0.5.3), unknown to this archive.

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
- C2SIM server: *** ALL AUTOMATED RUNS USE THE PRIVATE TEST SERVER (2026-09-02) ***
  container `c2sim-server-vrf`, REST **18080**, STOMP **61614**, bind mount
  `C:\C2SIM\docker\c2simFiles-vrf` (a copy of the operator's `c2simFiles`; the server
  persists its last init there as `c2simServerInit.xml`). It is the runner's default
  (`-RestUrl`/`-StompUrl`), reaches every stage (PushInit/PushOrder/StopIface args,
  ListenReports `--rest-url`/`--stomp-url`, the app via `C2SIM__RestUrl`/`C2SIM__StompUrl`),
  and the app log's first lines say `C2SIM endpoints: rest=... stomp=...`. WHY: the operator
  works the C2SIM GUI against THEIR server (`c2sim_server4.8.4.9`, 8080/61613) and an
  initialization pushed there mid-run reset the interface (run 20260902T193508Z, formally
  INVALID for that reason). The two servers must never be shared again. Recreate it if
  missing (same image, second instance):
  `docker run -d --name c2sim-server-vrf -p 18080:8080 -p 61614:61613 -v C:\C2SIM\docker\c2simFiles-vrf:/opt/c2simFiles -e TZ=America/Los_Angeles -e LANG=en_US.UTF-8 -e LC_ALL=en_US.UTF-8 -e LANGUAGE=en_US.UTF-8 c2sim-server:4.8.4.9-rev1`
  (~30 s to Apollo+Tomcat ready; verify REST `http://127.0.0.1:18080/C2SIMServer` -> HTTP 200).
  `docker restart c2sim-server-vrf` if STOMP has degraded across many runs.
- The operator's server `c2sim_server4.8.4.9` (REST 8080, STOMP 61613) is NOT ours to
  reset, push to, or restart. The historical notes below that say 8080/61613 describe it.
- IPv4 8080/18080 free of squatters (stop the COA-GPT `tileserver.py` if it reclaimed 8080).
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
   Fix: resolve the USER scope first, the Machine scope only as a fallback - since 2026-09-14
   the two disagree and the Machine one still names the EXPIRED file. See sec 0.5.15; the
   entry scripts now do this themselves, per process, and print the path and the expiry.
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

*** READ FIRST - the narrative immediately below is SUPERSEDED. The RTTI / dynamic_cast
cause is WRONG, and the "FIXED + LIVE-VERIFIED" / "Builds 0/0" claim is FALSE: that code
was REVERTED (commit 5d14eda - it broke object creation) and VrfFacade.cpp:735 STILL
contains the blind static_cast today. Do NOT scope the native re-attempt as though the UB
were gone. The CORRECTION block further down carries the current picture; this banner just
ensures you hit the truth first. Superseded narrative follows. ***

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
is not involved and was never the problem. *** FALSE - RETRACTED 2026-07-20. resolveStateRep has ZERO hits in src/ and
VrfFacade.cpp:735 still carries the blind static_cast. This never shipped. (lookupEA DOES
exist at VrfFacade.cpp:544 but on the SUB-AGGREGATE path, which is not this.) The
"HEADER ANALYSIS BELOW IS SOUND" vouching above covers the HEADER ANALYSIS ONLY and ENDS
HERE - it does not vouch for the two sentences that follow. *** Superseded: aggregates are now resolved positively through the
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
new creates from reflecting - recover headlessly per sec 8 (tools/ResetVrf) between heavy runs - NO GUI reload; sec 5 and sec 0 both record the GUI-reload claim as false.

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
$env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','User')  # 0.5.15: User first
if (-not $env:MAKLMGRD_LICENSE_FILE) { $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine') }
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

---

## 9. REBUILDING AND DEPLOYING THE NATIVE BRIDGE - THERE ARE **TEN** CONSUMERS, NOT SEVEN

Added 2026-09-14 (cold-start review of feat/integration 02b51de, NOTE in sec 2.10). The "all 7
copies" figure that appears in docs/HANDOFF_2026-07-19.md sec 5, docs/RESUME_PROMPT.md and the
memory entry is STALE and has been corrected in place.

WHAT IS ACTUALLY BUILT. There is no `VrfFacade.dll` and no `.def`: `VrfFacade` is compiled INTO
`VrfBridge.dll` (the build directory holds only `VrfBridge.{dll,lib,exp,pdb}` plus `Ijwhost.dll`),
and the managed side binds by C++/CLI assembly reference, not P/Invoke. A managed build that
SUCCEEDS against `src/VrfBridge/build/<config>/VrfBridge.dll` is therefore proof that every member
it calls exists on the referenced assembly.

THE TEN CONSUMERS - AND ONLY SIX OF THEM CAN BUILD A 5.2 TREE (corrected 2026-09-14 by gate G-A,
which measured it). Ten csproj files reference the bridge by `<Reference Include="VrfBridge">` and
each keeps its OWN copy in its `bin`. They are NOT alike, and the claim that stood here until now -
that all ten point at `src/VrfBridge/build/$(BridgeConfig)/VrfBridge.dll` - was FALSE for four:

  SIX carry the BridgeConfig axis (a `BridgeConfig` property plus `OutputPath` /
  `IntermediateOutputPath` overrides, and a HintPath onto `build/$(BridgeConfig)/`), so
  `-p:BridgeConfig=Release-5.2` gives them their own `bin\Release-5.2\` tree. THIS IS THE 5.2
  DEPLOY SET:

      src/VrfC2SimApp    tools/CreateOne    tools/RtiProbe
      tools/RunSim       tools/SetAlt       tools/WatchVrf

  FOUR hard-code `build/Release/VrfBridge.dll` and have NO BridgeConfig property at all, so
  `-p:BridgeConfig=Release-5.2` is SILENTLY IGNORED: they build into `bin\Release\` against the
  5.0.2 bridge, report Build succeeded / exit 0, and never produce a `bin\Release-5.2\` at all.
  THEY ARE 5.0.2-ONLY TODAY:

      src/SmokeTest      tools/CreateTaskAgg    tools/ResetVrf    tools/SetSimRate

  WHY, AND WHY IT IS NOT A ONE-LINE FIX: commit `529fe5c` ("5.2 tool join gate PASSED") converted
  five tools and VrfC2SimApp already had the axis; these four were never converted. They also do
  not compile `tools/Shared/StackIdentity.cs`, which is what makes a tool read the bound stack from
  `VrfBridge.NativeStackInfo()` and join the 5.2 way. Adding the csproj axis ALONE would therefore
  emit a "5.2" build that joins with the hard-coded 5.0.2 CWIX-2024 federation identity - worse
  than having no 5.2 build. Converting them is a code change plus a live join gate. OPEN.

  (`bridge-spikes/VrfBridgeSpike/SpikeRunner` is NOT one of them - it references
  `VrfBridge.Spike.dll`, a different artefact, and is not part of a deploy.)

THE PROCEDURE (native changes are pre-authorized; see the memory entry):
  1. BACK UP the existing `src/VrfBridge/build/<config>/VrfBridge.dll` first - none are committed.
  2. `/t:Rebuild` ALWAYS (never an incremental build of the C++/CLI project).
  3. Rebuild every consumer that HAS the BridgeConfig axis so every `bin` copy of that bridge is
     ONE hash (six for `Release-5.2`; all ten for `Release`). A PARTIAL redeploy is the trap: the
     tools and the app then disagree about what the bridge can do. Building the other four with
     `-p:BridgeConfig=Release-5.2` is a NO-OP that still exits 0 - check for the output tree, not
     the exit code.
  4. Confirm one hash, then RE-PIN the deployed build and record the pin.

WHY A PARTIAL DEPLOY IS DANGEROUS, CONCRETELY (M1 of the same review): a managed-only refresh of a
deployed folder - a new `VrfC2SimApp.dll/exe` beside an OLD `VrfBridge.dll` - does NOT fail at
start-up. It fails ten seconds in, when the R1 position poll first JITs `TryGetEntityKinematics` and
throws `MissingMethodException` on the vrf-tick thread. As of 2026-09-14 that no longer kills the
process silently: every tick-loop phase is wrapped (`VrfC2SimService.TickPhase`) and logs
`Tick phase '<name>' FAILED`, and `Program.cs` installs an `AppDomain.UnhandledException` handler
that writes the exception to stderr before the CLR terminates. Those are DIAGNOSTICS, not a fix -
a repeating `Tick phase 'MaybeSendPositionReports' FAILED (MissingMethodException)` means exactly
this, and the answer is steps 1-4 above.

DEPLOYED BUILD PIN (gate G-A, 2026-09-14). main `165e04c` (merge `0f4d09e` = feat/integration,
which brought the SimTimeSeconds/BackendCount readers and `TryGetEntityKinematics`).
`VrfBridge.dll` SHA256 `99B7B2355B7C78AFBCA2170DFA088EE4E73F28BB4DB8647F8DA93358D3706C04`
(996352 bytes), native `/t:Rebuild` of `Release-5.2|x64` at 2026-09-14T22:28:04Z, 0 errors.
All TEN consumers rebuilt with `-t:Rebuild`, 0 errors; the SIX 5.2 consumers are at THAT ONE
hash (the four 5.0.2-only ones are above, and are the reason this line does not say "ten").
Offline suites 18/18 exit 0, `--rulings-selftest` 136 PASS / 0 FAIL; `--parse-order`
COA-STP1 42 tasks and PROBE_RIDGE_1-35_DELAYED 1 task / simStartMs=300000; `--runtime-check`
exit 0 reporting `native stack = 5.2|C:\MAK\vrforces5.2d\bin64\vrfcontrol.dll`. The PRE state
it replaced was itself a partial deploy: four different bridge hashes across the six 5.2 bin
trees (VrfC2SimApp on 2FF06047 of 2026-09-06, the other five on three 2026-09-04 builds).
THE PIN NAMES 165e04c, NOT the tip: two commits from a parallel lane (`fe17e7b`, `0432b98`)
landed on main WHILE this gate ran. Both are docs + one new `data/*.xml` only - `git diff
--name-only 165e04c 0432b98` touches nothing under `src/` or `tools/` and no csproj/vcxproj -
so the binaries above are still the binaries of the current tip. One checkout, several lanes:
check this before trusting any pin whose sha is not the tip.

---

## 10. THE C16 PROGRESS WATCHDOG IS OFF BY DEFAULT - HOW TO TURN IT ON FOR THE VALIDATION RUN

Added 2026-09-14 (cold-start review sec 2.8). `Vrf:StallDetection` defaults FALSE, is absent from
BOTH `appsettings.json` and `appsettings.Demo.json`, and `scripts/StartInterface52.ps1` has NO
parameter for it. There is therefore NO config key and NO script switch to flip: the C16 validation
run is enabled by ENVIRONMENT OVERRIDE, in the interface's own shell, before it starts:

```powershell
$env:Vrf__StallDetection = "true"     # double underscore = the ':' of Vrf:StallDetection
$env:Vrf__StallClock     = "sim"      # optional; default is "wall"
```

`Vrf:StallClock` governs THIS WATCHDOG ONLY (2026-09-14). The clock C2SIM task times run on is
`Vrf:TaskClock` - sec 11 above.

The gate is at the CALL SITE (`TickLoop`: `if (_vrf.StallDetection) MaybeCheckStalls();`), so with
the default nothing inside the watchdog executes and no TASKABRT can be emitted by it - which is why
the merged build is safe to run without this.

*** `Vrf:StallWindowSeconds` 0 NOW MEANS "CALIBRATED", NOT "1 SECOND" ***. On main, an explicit 0
meant a one-second window; on this build 0 (and any negative) means "use the clock's calibrated
window" - 240 s wall / 360 s sim. Nothing SHIPS a value, so the reinterpretation has nil blast
radius on the shipped configs; it matters to anyone HAND-WRITING a config for the validation run. If
you want a short window, write the number you want - do not write 0 and expect one second.

---

## 11. THE CLOCK C2SIM TASK TIMES RUN ON, AND THE R4 TASKING KEYS

Added 2026-09-14 (the two cold-start reviews of `5c67d41` and `0c96f50`, items M1/M2/A1 and
the user's rulings Q1-Q7). **All seven are now IN `appsettings.json` at their defaults, and
the six that change what a demo does are in `appsettings.Demo.json` with a `_Key` line each
saying why** - a standalone deployment's behaviour has to be readable from its settings, not
from C# source. Every one can still be overridden per process (double underscore = the `:`),
which is how an experiment differs from the shipped profile:

```powershell
$env:Vrf__TimedCompletion                 = "true"      # DEFAULT. false = evidence-only completion
$env:Vrf__TaskClock                       = "sim"       # DEFAULT. "wall" to measure in real seconds
$env:Vrf__DurationScale                   = "1.0"       # DEFAULT. 0.05 compresses a demo into minutes
$env:Vrf__TaskPredecessorTimeoutSeconds   = "600"       # DEFAULT (7200 in the Demo overlay)
$env:Vrf__TaskPredecessorEndMarginSeconds = "60"        # DEFAULT
$env:Vrf__TaskChainBackstopSeconds        = "86400"     # DEFAULT. A1: the DISPATCH wait's backstop
$env:Vrf__SupersededTaskCode              = "TASKABRT"  # DEFAULT. "TASKCMPLT" = the literal R4 reading
```

`Vrf:DefaultHoldSeconds` existed between `06f8cf0` and the Q4 ruling and is GONE: a task with
no Duration and no geometry is malformed and is refused, not held (below).

- **`Vrf:TaskClock` is NOT `Vrf:StallClock`.** TaskClock carries ALL THREE C2SIM task times -
  the Duration that ends a task (R4), the StartTime/DelayTimeAmount delay that holds one back,
  and the STREND predecessor gate. StallClock carries the progress watchdog's no-progress
  window and NOTHING else. They were the same knob before this change, so turning the watchdog
  onto the sim clock silently changed when every task in the order completed. Both read the
  SAME sim-clock sample through the same hysteresis, so they can never disagree about whether
  the scenario is running - only about which clock they prefer.
- **A PAUSED SCENARIO DOES NOT AGE A TASK: task time HOLDS** (Q5, USER RULING 2026-09-14,
  built in `f2794d7`). When `DtVrfRemoteController::simTime()` is READABLE but has not advanced
  for 60 wall seconds and a VR-Forces back end is still listed, the task-clock axis adds
  NOTHING. All three task times ride that one axis, so all three freeze TOGETHER: the armed
  Duration that ends a task, both phases of the STREND gate, and the StartTime/DelayTimeAmount
  start delay. Pause the scenario for a ten-minute coffee break and it costs the order nothing,
  and nothing completes early when it runs again. (Before Q5 the interface served WALL seconds
  after 60 s of flatness - which burned those ten minutes off every armed Duration.)
- **It falls back to WALL seconds only when the BACK END IS GONE**, not when the clock is merely
  flat: either three consecutive UNREADABLE samples (`StallPolicy.ModeSwitchConfirmations`) take
  the hysteresis path, or - since STP-809, see below - the vendor reports that not one known
  back end is still operating. The axis then serves wall seconds. Nothing is lost either way - the axis
  adds FORWARD movement only, so a fall back keeps the time already served, serves the rest on
  the new base, and restarts no wait. The `TASK CLOCK:` transition lines name which happened:
  losing the reader says the sim reader is GONE and task times are now on the wall clock, and
  "readable and advancing again" is said ONLY on a genuine recovery.
- **The HOLD line REPEATS, once a wall minute** (`StallPolicy.LogRateLimitSeconds` = 60), and
  that is deliberate rather than a rate-limit bug: while the hold rests on the BackendCount
  fallback it is the only symptom of the case below, so the one thing it must not be is quiet.
  It then carries its own caveat sentence; a hold on a CONFIRMED pause does not need one and
  says so (STP-809, below).
- **CLOSED BY STP-809: the interface now asks the back end WHAT IT IS DOING, not just whether
  it was ever discovered.** Until STP-809 the hold rested on one signal, `BackendCount` =
  `backends().count()`, and the vendor's back-end list DEACTIVATES an entry that has missed its
  status timeout rather than removing it (`vrfBackendListener.h:161-163`, `doTimeouts()`
  "deactivates any status objects which have not responded within the timeout interval";
  `remove()` at `:153-155` "normally, this should not need to get called!"; `backends()` is the
  list of "all KNOWN back ends"). A back end that died IN PLACE therefore looked exactly like a
  paused one and froze task time for the rest of the run (pass-3 review E2). The facade now also
  reads:
  - `VrfFacade::BackendControlState()` -> `DtVrfRemoteController::backendsControlState()`
    (`vrfRemoteController.h:320-323`): **PAUSED** (`DtPauseControlType`) vs **RUNNING**
    (`DtRunControlType`) vs no back end at all. A pause is now a POSITIVE reading.
  - `VrfFacade::ActiveBackendCount()` -> how many KNOWN back ends the vendor still calls
    simulatable or in transition (`DtBackend::isInSimulatableState` / `isInTransitionStatus`,
    `vrfutil/backend.h:109`/`:115`, over `backendListener()->backendList()`).

  **The rule (`StallPolicy.TaskClockAction`), in the order it is applied:** not one active back
  end -> **WALL** (it has DIED, whatever its last status said); PAUSED -> **HOLD**; no back end
  at all -> **WALL**; RUNNING with a flat clock -> **HOLD** (the status message and the clock
  sample refresh on different cadences, so this is a blind period, not a death - only the
  three-unreadable-sample hysteresis may switch clocks); anything unreadable -> the old
  `BackendCount` rule, unchanged. **A signal that says nothing can never change an outcome**, so
  a deployment carrying an older `VrfBridge.dll` behaves exactly as it did before (it also gets
  one WARNING a minute naming the partial deploy - RUNBOOK sec 9).
- **WHAT THE OPERATOR READS.** The repeating `TASK CLOCK:` line now names the state it decided
  on: "the back end REPORTS PAUSED (DtPauseControlType; BackendCount=1, active=1)" is a
  confirmed pause and carries NO caveat; "the control state is UNKNOWN - falling back to the
  back-end COUNT" still carries the old caveat sentence, because on that fallback a dead back
  end still looks paused. If the back end dies, the line that fires is the WALL one: "the back
  end's last status said RUNNING but NOT ONE known back end is still simulatable or in
  transition ... it has DIED, it was not paused", and task times move to the wall clock keeping
  every second already served. **UNCONFIRMED LIVE** (assessment live gate 11): nobody has yet
  killed a back end and watched what the vendor reports for the deactivated entry. If the active
  count never drops, the behaviour is exactly the pre-STP-809 one - the repeating WARNING with
  its caveat - so that is still the line to recognise at a demo.
- **The predecessor gate is a FLOOR, not the whole window - and it asks TWO questions.** A gated
  task waits for its predecessor to COMPLETE for at least
  `(that predecessor's Duration x Vrf:DurationScale) + Vrf:TaskPredecessorEndMarginSeconds` (M1),
  and it waits for that predecessor to DISPATCH AT ALL for `Vrf:TaskChainBackstopSeconds`
  (86,400 s by default) whenever the predecessor is a task in the SAME order (A1). The second
  window is generous on purpose: a predecessor that really dies is ABANDONED - every dispatch
  dead end in the interface says so - which fails the gate at once, so a timeout there only ever
  punished a HEALTHY deep chain. A DANGLING `startAfterTaskUuid`, one no task in the order
  carries, is the single case nothing will ever speak for, and it still expires at
  `Vrf:TaskPredecessorTimeoutSeconds`.
- **Do you need to raise `TaskPredecessorTimeoutSeconds` for a long order? NO - but only since
  A1 (`b6471a3`).** Before that commit COA-STP1 needed **>= 20,000 s**: measured on the real
  `TaskSequencer` + `TimedCompletionPolicy` + `TaskDispatchPolicy`, the shipped 600 s AND the
  Demo overlay's 7,200 both dispatched 21 of the 42 tasks and TASKABRT'd the other 21, because
  the gate covered the predecessor's Duration but not its LEAD TIME (its start delay plus its own
  gate wait); at a compressed `Vrf:DurationScale` the outcome was not even the same twice.
  Raising it is still harmless - it is a floor - and it is no longer necessary.
- **`Vrf:DurationScale` is validated at start-up.** A zero, negative, NaN or infinite value is
  REJECTED with an ERROR line and the run proceeds at 1.0 (the order as written). It used to
  mean "no end time armed" on one half of the order's clock and "dispatch now" on the other.
- **`Vrf:TaskPredecessorEndMarginSeconds` is validated too, and ZERO IS REFUSED** (E5). A value
  that is not greater than zero gets an ERROR line and the run proceeds at the shipped **60 s**.
  Zero is not a harmless setting: at 0 the completion window equals the predecessor's scaled
  Duration EXACTLY, while that completion is OBSERVED up to about `3 x (sim ratio)` seconds late
  (the 1 s clock sample staircase plus the 1 s timed walk), so the gate and the completion race
  and a successor is skipped on timing rather than on fact - the non-determinism A1 was fixed to
  remove, arriving by configuration instead. A NEGATIVE margin reaching the derivation is still
  clamped to zero there, so a window can never come out SHORTER than the end time it waits for;
  that clamp is a floor against nonsense, not a blessing of zero. Raising the margin is free.
- **Every order says how DEEP it is, against the backstop** (E4). One `CHAIN DEPTH:` INFO line
  per order names how many task-clock seconds after receipt the deepest chain reaches its last
  dispatch, when that last task is armed to end, and what `Vrf:TaskChainBackstopSeconds` is.
  COA-STP1 measures **16,800 s** to the last dispatch and **21,600 s** to the last end, against
  the 86,400 s backstop - 5.1x of headroom. If the lead ever meets or exceeds the backstop a
  WARNING says so at receipt, naming what to raise, instead of the operator learning it hours
  later as a burst of `never dispatched within 86400s` lines that look like a wedge.
- **A task with NO Duration AND NO geometry is REFUSED, not held** (Q4, USER RULING 2026-09-14).
  There is no knob: `Vrf:DefaultHoldSeconds` is DELETED. Such a task gets an ERROR naming both
  missing elements, a TASKABRT, and an abandon so its STREND successors fail fast. If you see
  that line, the ORDER is at fault - give the task a Duration, a geometry, or both. None of
  COA-STP1's 42 tasks is affected: all 42 carry a Duration.

START-UP PROOF: one `TASK CLOCK (R4):` line names the clock in force, the scale, and the gate
formula. If that line is missing, the build predates this change.

---

## 12. THE ROUTE PRE-FLIGHT AND ITS LATERAL SHIFT - BOTH OFF BY DEFAULT (STP-804/806)

Design: `docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md`. Evidence: FINDING_EARLY_STOPS
sec 7/7e, PREREG_RIDGE_AG sec 3.2-3.3, PREREG_N1_N2 sec 10.3 (N2d),
READ_4-27_G3_AND_OFFSET_SCORING sec 2.3/2.5. NOTHING BELOW HAS BEEN RUN LIVE.

Two separate features, two separate keys:

| key | default | what it does |
|---|---|---|
| `Vrf:PreflightWarnings` | false | AFTER dispatch, score the route that was driven and push one ObservationReport per flagged leg. Never alters a task. |
| `Vrf:PreflightRouteShift` | false | BEFORE dispatch, score the route and detour each FLAGGED leg laterally onto ground the same sampler scores as clear. CHANGES WHERE UNITS DRIVE. |

Turn them on with environment overrides in the interface's own shell (double underscore = the
`:` of the key), as with the C16 watchdog in sec 10:

```powershell
$env:Vrf__PreflightRouteShift  = "true"
$env:Vrf__PreflightWarnings    = "true"     # so the route that IS driven is reported too
$env:Vrf__PreflightOffline     = "true"     # the AO tile cache must be pre-warmed
$env:Vrf__PreflightCacheDir    = "C:\C2SIM\preflight-cache"
```

WHAT THE SHIFT DOES, EXACTLY. For each leg whose sustained-40 m ratio reaches
`Vrf:PreflightThreshold` (0.92), it searches offsets outward from `...ShiftStepMeters` (25 m) to
`...ShiftMaxMeters` (600 m), both sides, and takes the SMALLEST that clears. "Clears" means the
whole detoured polyline re-scores at or below `threshold - ...ShiftMarginRatio` (0.82) AND -
while `...ShiftClearFormationBand` is true - every formation slot line (-50/-25/0/+25/+50 m)
scores below the threshold. It then inserts FOUR points between the leg's two authored
vertices: two ON the authored line where the path leaves and rejoins it, and two on the offset
line spanning the flagged window plus `...ShiftPadMeters` (50) plus `...ShiftLeadMeters` (110)
each side. `...ShiftMaxTurnDegrees` (30) sets the corner and hence the transit length.

WHAT IT NEVER DOES: move, drop or reorder one of STP's own vertices; refuse a task; or act
silently. A flagged leg no offset clears is dispatched AS AUTHORED with an ObservationReport
saying so.

THE THREE THINGS TO LOOK FOR IN THE LOG:

    ROUTE SHIFT check queued for <unit> (<n> vertices); dispatch deferred ...   (it is running)
    ROUTE SHIFTED <d> m <side> - ratio <before> -> <after> ...                  (it acted)
    NO ROUTE SHIFT - NO CLEARED LINE within +/-<band> m ...                     (it declined)

and one failure mode that must never be silent:

    the ROUTE SHIFT check did not finish within <t> s - dispatching on the line as authored

DISPATCH IS DEFERRED WHILE IT RUNS. The check runs OFF the tick thread (a cold leg fetches
terrain tiles over HTTP) and the task is dispatched from the re-entry, exactly as the
TerrainProfile continuation works. `Vrf:PreflightRouteShiftTimeoutSeconds` (30) bounds it; on
expiry the AUTHORED line is dispatched. PRE-WARM THE CACHE for the AO and set
`Vrf:PreflightOffline=true` (the STP-802 scenario-prep posture) so a demo never waits on the
network at dispatch.

OFFLINE PROOF, no bridge and no network: `VrfC2SimApp --routeshift-selftest` (it asserts that
zero tiles were fetched). On the 1-35 ridge leg it reproduces the record: the authored line
1.098, the chosen shift +75 m NORTH at 0.803 with a formation band max of 0.878, 1-1's T23 leg
(0.870) and 1-35's authored V0->V1 line (0.524 - the line N2d drove) left untouched, and the
42.9 km PL BLUE leg flagged with no cleared line in the band.
