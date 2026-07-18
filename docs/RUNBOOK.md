# RUNBOOK - operating the c2simVRFinterface against VR-Forces

Hard-won runtime procedure. Read this BEFORE any run; do NOT re-derive it (a whole
session was burned rediscovering the pieces below). Companion to START_HERE.md
(build) and PORT.md sec 4 (environment). ASCII-only.

## 0. The single most important rule

NEVER force-kill a JOINED interface (`Stop-Process -Force`, `taskkill /F`). It does
not resign from the RTI, so it leaves a STALE FEDERATE; the next interface start then
HANGS at RTI join (1 thread, ~0 CPU, log frozen at the config banner). The only
recovery for a stale federate is a manual VR-Forces scenario reload in the GUI.
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

REQUIRE `reflected>0`. DISCOVERY TAKES ~13 SECONDS TO POPULATE - it reported
`reflected=0` until t=13.3s on a healthy federation, so DO NOT JUDGE BEFORE ~15 s.
If it is still 0 after 20 s, STOP; do not run the session.

STRONGER CHECK (settles position fidelity, not just discovery):

    CreateOne.exe <freshAppNo>          # creates one M1A2 at a COA-STP1 AO coord

then confirm WatchVrf emits a POS line for that uuid with REAL coordinates, not
NaN. Expected shape (verified 2026-07-18):

    POS,23.5,VRF_UUID:adfaadb3-...,34.517156,-116.973525,1060.7

Requested altitude was 10000 m MSL; 1060.7 is the GROUND CLAMP working. Relaunch
afterwards so the throwaway never enters a scored trace (a relaunch reloads the
scenario from file and removes it).

CAUTION: a freshly loaded TropicTortoise contains only NON-ENTITY CONTROL OBJECTS
(GlblTerrDmg, Blocking Terrain Page-In Area). Their POS lines legitimately carry
NaN lat/alt. Do NOT conclude the oracle is broken from those - create a real
entity and check that.

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

### 0.5.9 CLEAN SHUTDOWN

Per the vendor (Introduction\Starting\vrf_startVRF.htm): in COMBINED mode
"when you shut down the front-end, the back-end automatically shuts down also".
So close vrfGui, not the back-end. Never force-kill a JOINED federate (sec 0);
that leaves a stale federate and the next start hangs at RTI join.
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
4. Observe: task start/complete lines in the interface log; entity movement in the VR-Forces GUI.

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
Recovery: reload the VR-Forces scenario in the GUI (re-creates the federation). This is
the ONLY step that needs the human, and it is only needed because of a prior force-kill.
Avoid it entirely by clean-stopping (sec 4).

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
the disaggregated aggregate (concrete reflected type / RTTI across the MAK DLL boundary), where
the C++ oracle's blind static_cast read the base myStateRep and worked. FIX applied in
VrfFacade::TryGetEntityGeodetic: after the typed entity/aggregate casts, fall back to the C++
static_cast base-state read. Builds 0/0. NOT yet live-verified: the very next run's creates
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
