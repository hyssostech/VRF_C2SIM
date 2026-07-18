# PREREG 0.4 - Scripted VR-Forces self-launch via vrfLauncher (live gate)

Pre-registration for groundwork-plan Phase 0 item 0.4 (docs/VRF_GROUNDWORK_PLAN.md
lines 83-86). Written BEFORE the live run so the result cannot be rationalized after
the fact. Single variable, single prediction, explicit falsifier. ASCII only.

> *** GATE RUN AND PASSED 2026-07-18. SEE SECTION 12 (the result) FIRST. ***
> Section 11's "0.4 is demoted / three defects block it" status is HISTORICAL and
> must not be acted on. Sections 1-10 are the intact pre-registration as written
> before the run.

Status: RUN 2026-07-18 - PREDICTION CONFIRMED. Result in section 12. The scripted
launch works unattended and the ResetVrf join gate passed twice in a row.
Sections 1-10 below are unmodified from before the run.

Artifact under test: scripts/LaunchVrf.ps1 (companion; parses clean, -DryRun verified).

---

## 1. Single variable under test

HOW VR-Forces is brought up, and NOTHING ELSE:

- CONTROL (baseline, known-good): a human launches VR-Forces via the GUI - the
  "VR-Forces GUI + Simulation Engine (64-bit)" shortcut, which is
  `vrfLauncher.exe` (no args) run from `C:\MAK\vrforces5.0.2\bin64`, then the
  human confirms/loads the connection + a scenario. This is the path that has
  produced every clean ResetVrf/app join on this machine (OPUS_EXECUTION_PLAN.md
  Appendix B: 3414 clean at TropicTortoise via GUI, 3401/3415/3418 clean).
- TREATMENT (the thing being validated): scripts/LaunchVrf.ps1 runs the SAME
  `vrfLauncher.exe` in the SAME cwd (bin64) in COMBINED mode, naming the saved
  connection profile on the command line so the Simulation Connections
  Configuration dialog never appears, and passing a scenario to the back-end. No
  human clicks are needed to bring the federation up.

Everything else is held constant and is verified to already match the control:
- Same launcher binary, same cwd (`bin64`) - installer shortcut confirmed
  identical (`VR-Forces GUI + Simulation Engine (64-bit).lnk` -> TargetPath
  `...\bin64\vrfLauncher.exe`, Arguments empty, WorkingDir `...\bin64`).
- Same connection profile "HLA 1516 Evolved RPR 2.0 with MAK extensions"
  (the user's starred/default; primary source
  `C:\MAK\vrforces5.0.2\appData\settings\vrfLauncher\HLA 1516 Evolved RPR 2.0 with MAK extensions.xml`:
  back-end app 3001, front-end app 3101, FED `RPR_FOM_v2.0_1516-2010.xml`,
  federation `CWIX-2024`, FOM modules MAK-VRFExt-6_evolved / MAK-DIGuy-7_evolved /
  MAK-LgrControl-2_evolved, siteId 1, sessionId 1, host 127.0.0.1). The scripted
  run overrides ONLY the two application numbers (to fresh values, stale-federate
  avoidance) via `--simArgs --appNumber` / `--guiArgs --appNumber` exactly as the
  official launcher doc example prescribes.
- Same RTI (4.6.1) - Machine-scope `MAK_RTIDIR=C:\MAK\makRti4.6.1`,
  `RTI_RID_FILE=C:\MAK\makRti4.6.1\rid.mtl`, inherited by the launcher's children
  just as for the human double-click (the script does NOT rewrite PATH).
- Same license - `MAKLMGRD_LICENSE_FILE` refreshed from Machine scope (RUNBOOK
  sec 7 item 2), the same value the human's fresh process inherits.
- Same scenario (default TropicTortoise; confirmed the active #1 recent scenario
  in `vrfGui\applicationSettings.xml`).

The deliberate confound the last week never isolated (Appendix B 3414 note:
"region and launch method were fully confounded") is thereby broken: same region,
same profile, same everything - only human-vs-script bring-up differs.

## 2. Why combined mode, not backend-only

The raw headless back-end (`vrfSimHLA1516e.exe` alone) is CONFIRMED UNSAFE: it
produces a backend that crashes remote-controller clients (ResetVrf, the app) with
`0xC0000005` inside `VrfFacade::Tick()` on tick (RUNBOOK sec 0.5; reproduced
Appendix B 3399, 3412, 3413). Root cause: the crash is specific to the headless
CLI launch missing the front-end that the GUI's combined mode provides
(Appendix B 3414: the identical scenario launched combined via the GUI joined
CLEANLY). Therefore the accepted recipe MUST include the front-end. Combined mode
(`vrfLauncher --usePredefinedConnection "<profile>"`, NO `-B`) is exactly the
front-end+back-end pairing the human uses, and the documented way to get it from
the command line:

> "you can run vrfLauncher with the --usePredefinedConnection argument and
> VR-Forces will load the specified connection without displaying the Simulation
> Connections Configuration dialog box. This has the effect of running directly in
> combined mode from the command line."
> -- C:\MAK\vrforces5.0.2\doc\help\Content\Introduction\CLI\vrf_startInComBinedModeWithoutLauncherWindow.htm

Backend-only (`-B`) is the KNOWN crash-risk condition and is NOT the recipe. The
script refuses `-Mode BackendOnly` unless `-AcceptCrashRisk` is passed, and only
because the RUNBOOK flags it as a useful FUTURE data point ("if -B alone reproduces
the crash, that shows the crash is about missing front-end"), not as an accepted
bring-up path.

## 3. The exact command the treatment issues

cwd = `C:\MAK\vrforces5.0.2\bin64`, `MAKLMGRD_LICENSE_FILE` refreshed from Machine:

```
vrfLauncher.exe --usePredefinedConnection "HLA 1516 Evolved RPR 2.0 with MAK extensions" ^
  --simArgs --appNumber <freshBackendAppNo> --scenarioFileName "../userData/scenarios/TropicTortoise.scnx" ^
  --guiArgs --appNumber <freshFrontendAppNo>
```

Argument-by-argument provenance:
- `--usePredefinedConnection "<profile>"` - vrf_vrfLauncherCommandLine.htm Table 9
  ("launch without needing to display the Simulation Connections dialog box") and
  vrf_startInComBinedModeWithoutLauncherWindow.htm (combined mode).
- `--simArgs ... --guiArgs ...` split, and overriding `--appNumber` on each side -
  vrf_vrfLauncherCommandLine.htm Table 9 worked example (verbatim there for a DIS
  profile: `--simArgs --deviceAddress 127.0.0.2 --appNumber 3002 --guiArgs
  --deviceAddress 127.0.0.2 --appNumber 3102`).
- `--scenarioFileName "../userData/scenarios/<name>.scnx"` (a vrfSim back-end
  option, routed via --simArgs) - RUNBOOK sec 0.5 (path relative to bin64;
  scenarios live in userData/scenarios). Both TropicTortoise.scnx and
  Bogaland2.scnx confirmed present.
- Fresh application numbers - RUNBOOK sec 2/7 (stale-federate avoidance) +
  OPUS_EXECUTION_PLAN.md Appendix B ledger (NEXT FREE = 3455 at time of writing).

## 4. PREDICTION (registered before the run)

After the scripted launch brings the combined federation up (LaunchVrf.ps1 reports
READY: back-end `vrfSimHLA1516e` + `rtiexec` + front-end `vrfGui` all up), a
`ResetVrf --dry-run` join against that backend will JOIN AND READ CLEANLY - it
discovers the scenario's baseline objects (2 for TropicTortoise per Appendix B
3414/3435 history), issues no deletes, and RESIGNS cleanly with NO crash - and it
will do this TWICE IN A ROW (two independent ResetVrf dry-runs, fresh app numbers).

This is the exact behavior the GUI-launched backend showed (Appendix B 3414). The
prediction is that scripted combined-mode bring-up is INDISTINGUISHABLE from the
human GUI bring-up at the ResetVrf level.

## 5. FALSIFIER (any one of these = prediction falsified = recipe rejected)

- ANY `0xC0000005` access violation, in ResetVrf, in the app, or in vrfSim itself
  (the signature is inside `VrfFacade::Tick()` -> `controller->tick()`), on either
  ResetVrf dry-run.
- ResetVrf failing to join, or hanging at RTI join (1 thread / ~0 CPU / log frozen
  at the config banner - the stale-federate signature).
- Any client crash / "Restart recommended" / vrfSim self-crash (a new
  `vrfSim5.0.2-*.dmp` in bin64).
- The second ResetVrf dry-run failing even if the first passed (must be clean
  TWICE - one clean pass is within the known "not 100% reproducible on demand"
  noise, Appendix B 3400).

One clean pass is NOT acceptance; a single failure of either pass IS falsification.

## 6. OPEN RISKS - where human interaction may still be required (READ THIS)

These are the ways the treatment could fail to be truly unattended. They are
called out LOUDLY because they are LIVE-OBSERVABLE ONLY - the offline script and
its -DryRun cannot detect them, and they must be watched for on the first live run.

- **RISK A (HIGH) - Session Startup dialog.** The front-end (vrfGui) may pop a
  "start new scenario vs load scenario" startup dialog that needs a click. It is
  governed by `showSessionStartupDialog()` / `setDoNotShowSessionStartupDialog()`
  and `startupPreference()` (C:\MAK\vrforces5.0.2\include\vrfGuiCore\vrfGuiSettingsManager.h).
  The current value is a packed flag in `vrfGui\default_GuiSettings.grsx` /
  `GuiSettings.backup` that was NOT decoded offline - UNKNOWN whether it is
  suppressed. If this dialog appears, the scripted launch is NOT unattended and the
  federation may not finish coming up until a human clicks. This is a DIFFERENT
  failure class than the crash falsifier (it is fixable by pre-disabling the
  dialog), so record it as "blocked-on-dialog", not as a crash rejection.
  MITIGATION to try if it blocks: launch the GUI once by hand, tick "do not show
  again", or pre-set the flag, then re-run the scripted launch.
- **RISK B (MEDIUM) - Terrain Correlation Warning dialog.** A modal warning "if the
  terrain loaded in the front-end might not correlate well with the terrain loaded
  in the back-end" (VRF_GROUND_TRUTH.md sec 7;
  `setDisplayCorrelationWarningMessages()`), which our runs "may be raising." Modal
  -> blocks unattended startup. Same record-as-blocked-on-dialog handling and same
  pre-disable mitigation.
- **RISK C (MEDIUM) - scenario load in combined mode.** The scenario is passed to
  the BACK-END via `--simArgs --scenarioFileName`. It is UNVERIFIED whether, in
  combined mode, the front-end then reflects that scenario cleanly or instead
  prompts the human to open a scenario. If the front-end starts empty and prompts,
  interaction is still required. Fallback if this is the blocker: bring the
  federation up scenario-less (`-NoScenario`) and load the scenario remotely
  (0.3/0.5 confirmed `loadScenario` over the remote controller) - but that changes
  the recipe and would need its own note.
- **RISK D (MEDIUM) - the --simArgs/--guiArgs override on an HLA-evolved profile.**
  The official worked example for `--simArgs --appNumber / --guiArgs --appNumber`
  uses a DIS profile. It is UNVERIFIED that the same override cleanly reaches
  vrfSimHLA1516e / vrfGui under the HLA-1516e-Evolved driver and that the fresh app
  numbers actually take effect (vs the profile's baked-in 3001/3101). If a stale
  3001/3101 federate lingers and the override silently no-ops, expect a
  stale-federate hang -> that would present as a FALSIFIER (hang at join), so it is
  covered, but the root cause would be "override ignored", not "recipe unsafe".
- **RISK E (LOW) - license validity.** `MAKLMGRD_LICENSE_FILE` is a Machine-scope
  file path and its existence is passively verified, but the filename encodes DEMO
  dates ("...DEMO_1-dec-2025.lic") and validity/expiry CANNOT be checked passively
  (no checkout permitted). Recent GUI launches through 2026-07-16 worked, so it is
  presumed valid; a license-checkout HANG in bring-up (low CPU, no rtiexec) would
  be the tell (RUNBOOK sec 7 item 2).
- **RISK F (LOW) - stale processes from a prior session.** A leftover
  vrfSim/vrfGui/rtiexec (e.g. after a crash or force-kill) can cause a
  stale-federate hang or a second federation. LaunchVrf.ps1 REFUSES to launch on
  top of existing VR-Forces/RTI processes unless `-AllowExistingVrf` is passed, and
  NEVER force-kills (RUNBOOK sec 0). The operator must clean up leftovers by hand
  (clean-stop / GUI close) before the gate.

Note: combined mode DOES open a vrfGui window - the treatment is "no human CLICKS
needed to bring the federation up", not "no window appears". Removing the window
entirely is out of scope (and would reintroduce the backend-only crash).

## 7. Step-by-step procedure (supervised live session)

Preconditions (operator, before starting): no VR-Forces/RTI processes running;
C2SIM server not required for this gate (ResetVrf is a pure VR-Forces mini-host, no
C2SIM/STOMP - RUNBOOK sec 8); pick FOUR fresh consecutive app numbers from
OPUS_EXECUTION_PLAN.md Appendix B (NEXT FREE = 3455): back-end B, front-end F,
ResetVrf-1 R1, ResetVrf-2 R2 - and record each in Appendix B as consumed.

1. **Offline confirm** (no launch): from the repo,
   `pwsh -File scripts\LaunchVrf.ps1 -DryRun -BackendAppNumber B -FrontendAppNumber F`
   -> all preconditions [OK], resolved command line correct. (This step is safe and
   was already run during drafting.)
2. **Scripted launch:**
   `pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise -BackendAppNumber B -FrontendAppNumber F`
   Watch for RISK A/B/C dialogs. Expect the script to report READY (back-end +
   rtiexec + front-end up) within the timeout. If it reports PARTIAL (no front-end)
   or NOT READY (no rtiexec) -> go to Abort criteria.
3. **Gate pass 1:** run ResetVrf --dry-run against the scripted backend (RUNBOOK
   sec 8), fresh app number R1:
   ```
   $env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
   $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
   Push-Location C:\MAK\vrforces5.0.2\bin64
   & <repo>\tools\ResetVrf\bin\Release\net10.0\win-x64\ResetVrf.exe R1 --dry-run
   Pop-Location
   ```
   Expect: joins, discovers ~2 baseline objects, 0 deletes, resigns clean, exit 0,
   NO crash.
4. **Gate pass 2:** repeat step 3 with fresh app number R2. Expect identical clean
   result. Two clean passes are required.
5. **Teardown (per RUNBOOK):**
   - ResetVrf --dry-run issues no deletes and resigns itself, so no client remains
     joined and no created objects exist to clean up.
   - Stop the scripted VR-Forces by CLOSING the vrfGui front-end (File > Exit /
     window close). `myAllowBackendShutdown=1` (verified in
     `vrfGui\applicationSettings.xml`) means the front-end cleanly shuts the
     back-end down with it. Do NOT `Stop-Process -Force` any vrf/rtiexec process
     (RUNBOOK sec 0 - a force-killed joined federate leaves a stale federate).
   - Confirm afterward: no vrfSim/vrfGui/rtiexec left running.
6. **Record** in Appendix B (B, F, R1, R2 consumed with outcome) and update
   docs/VRF_GROUNDWORK_PLAN.md 0.4 status + docs/VRF_GROUND_TRUTH.md (add the 0.4
   section) with the verbatim result.

## 8. Abort criteria (stop immediately; do not iterate live)

- Any FALSIFIER in sec 5 (0xC0000005 / hung join / client or vrfSim crash) -> STOP,
  record the exact error text + any dump filename verbatim, tear down (sec 7 step
  5), recipe REJECTED.
- Script reports NOT READY (no rtiexec within timeout): likely a blocking dialog
  (RISK A/B), a license hang (RISK E), or the LRC #8 FDD popup - STOP, note which,
  do NOT retry blindly.
- Script reports PARTIAL (back-end + rtiexec up, no front-end): combined mode is
  incomplete = the crash-risk condition - STOP, do NOT run ResetVrf against it.
- Any human-interaction dialog appears during bring-up (RISK A/B/C) -> the launch
  is not unattended; STOP after noting WHICH dialog and its exact title. This is a
  "blocked-on-dialog" outcome (recipe needs the dialog suppressed), recorded
  distinctly from a crash rejection.
- More than the pre-planned FOUR app numbers would be needed (i.e. a retry) -> stop
  and re-pre-register; no ad-hoc live iteration (VRF_GROUNDWORK_PLAN.md: "No further
  scattergun live probes").

## 9. What each result means

- **PASS (ResetVrf dry-run clean TWICE):** the scripted vrfLauncher combined-mode
  recipe is ACCEPTED as the self-launch path. The human bring-up dependency is
  removed for all later phases (Phase 1 onward can start VR-Forces via
  LaunchVrf.ps1). Record the recipe as validated in VRF_GROUND_TRUTH.md 0.4 and
  flip VRF_GROUNDWORK_PLAN.md 0.4 to DONE.
- **FAIL by crash/hang (a sec-5 falsifier):** recipe REJECTED. Revert to human GUI
  launch for later phases. Record the symptom VERBATIM (error text + dump name).
  Because combined mode is the human's own path, a crash here would be a NEW and
  important finding (it would mean even combined-mode bring-up is not reliably
  scriptable, or the --simArgs/--guiArgs override diverged something) - escalate to
  the MAK support question with the exact repro.
- **BLOCKED on a dialog (RISK A/B/C):** recipe not yet unattended, but NOT rejected
  as unsafe. The bring-up mechanism is sound; the fix is to suppress the specific
  dialog (named in the record) and re-pre-register a follow-up run. Distinct from a
  crash rejection.

## 10. Application-number budget for this gate

From OPUS_EXECUTION_PLAN.md Appendix B, NEXT FREE = 3455. This gate consumes four
consecutive numbers; suggested assignment (operator confirms against the live
ledger tail at run time and records each):
- 3455 - back-end (vrfSimHLA1516e) via LaunchVrf.ps1 --simArgs --appNumber
- 3456 - front-end (vrfGui) via LaunchVrf.ps1 --guiArgs --appNumber
- 3457 - ResetVrf --dry-run pass 1
- 3458 - ResetVrf --dry-run pass 2
Never reuse; record each join in Appendix B as consumed.

---

## 11. ADDENDUM 2026-07-18 - status change and script defects (NOT part of the pre-registration)

Everything in sections 1-10 above is the pre-registration as written BEFORE the
run and is left INTACT and unmodified. This section is an appended record only;
it does not alter the prediction, the falsifier, or the procedure.

### 11.1 STATUS CHANGE - 0.4 demoted behind Phase 1

The user DEMOTED the 0.4 live gate behind Phase 1 on 2026-07-18. The next live
session is PHASE 1 (docs/PHASE1_SESSION_SCRIPT.md) with MANUAL RUNBOOK launch of
VR-Forces; 0.4 gets its own short session AFTERWARD, on a repaired script.

Rationale: Phase 1 is the highest-value live hour in the effort, and 0.4 carries
a HIGH-risk untried mitigation (RISK A, the session-startup dialog, sec 6) plus
the three script defects recorded below. Spending the scarce live hour on the
bring-up mechanism rather than on the native baseline was judged the worse trade.

### 11.2 THREE DEFECTS in scripts/LaunchVrf.ps1 - MUST be fixed before the gate runs

Confirmed by supervisor direct read of the script on 2026-07-18.

1. **Readiness is process-presence only.** The poll breaks on
   `if ($backendUp -and $rtiUp -and ($frontUp -or -not $needFront)) { break }`
   (line 291). `$guiTitle` IS captured (line 286) but is only PRINTED (line 298),
   never tested. Yet -DryRun advertises it as a readiness signal: "vrfGui
   MainWindowTitle non-empty (front-end window is up, not stuck in a modal
   dialog)". The script does not perform that check, so it CANNOT detect RISK A
   or RISK B, and its own dry-run output overstates what it does.
2. **App-number freshness is a WARNING, not a gate.** Defaults are baked in
   (`$BackendAppNumber = 3455`, `$FrontendAppNumber = 3456`, lines 92-93) and
   omitting the flags produces only a Say-Warn (lines 122-123). This is against
   the never-reuse non-negotiable and is exactly RISK D's stale-federate trigger.
   Contrast: the new tools/SetSimRate was deliberately built with NO default
   appNo - a missing appNo is a hard exit 2. Adopt the same posture here.
3. **MAKLMGRD_LICENSE_FILE is overwritten unconditionally on the live path**
   (line 267) even when the Machine-scope value is empty or null - that case is
   only a Say-Warn (lines 165-166), so a session that HAD a working process-scope
   license value gets it blanked.

### 11.3 CONFIRMED CLEAN (supervisor-verified positive)

The script contains NO termination calls of any kind - no `Stop-Process`, no
`taskkill`, no `.Kill()`, no `CloseMainWindow`. It CANNOT force-kill a joined
federate. Its only process-creating call is a single `Start-Process` of
`vrfLauncher.exe`; everything else touching processes is read-only. This is worth
recording as a positive: the RUNBOOK sec 0 never-force-kill constraint is
structurally satisfied by the script as written.

## 12. RESULT 2026-07-18 - PREDICTION CONFIRMED, GATE PASSED

Full narrative: docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md.

THE PREDICTION (sec 4) was: after the scripted launch brings the combined
federation up, a ResetVrf --dry-run will JOIN AND READ CLEANLY - discovering the
scenario's baseline objects (2 for TropicTortoise), issuing no deletes, resigning
with NO crash - TWICE IN A ROW with fresh app numbers.

OBSERVED (appNos 3489, 3490, both identical):

    [OK] joined
    [OK] discovery complete: 3 reflected object(s) (2 deletable, 1 nil/backend skipped)
    [DRY-RUN] would delete 2 object(s); NO deletes issued
    [OK] resigned cleanly          EXIT=0

CONFIRMED. Two clean joins, the 2 expected baseline objects, zero 0xC0000005.
The crash that made the headless recipe unsafe since 2026-07-15 did not recur.

THE LAUNCH ITSELF (appNos 3484/3485, and again 3486/3487):

    pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise `
         -BackendAppNumber <fresh> -FrontendAppNumber <fresh> -AllowExistingRtiAssistant
    => [OK] READY: combined-mode VR-Forces is up ...   EXIT=0

Zero human interaction. Back-end 66 threads, front-end with a real main window,
scenario loaded, no dialog at any point.

THE MISSING PIECE THE PRE-REGISTRATION DID NOT ANTICIPATE. Sections 1-10 assumed
the only variable was the launch command. It was not. On HLA the RTI Assistant
prompts for a connection and the federate DOES NOT START until it is answered -
vendor-documented, not a bug (VR-Forces help SharedTopics\XMLrti\InstallMAK-RTI.htm;
MAK RTI Users Guide p. 4-2 even names the symptom, "The federate startup process
may appear to hang while the Choose RTI Connection dialog box is waiting for
input"). ONE-TIME per machine: answer that dialog with "Always try to use this
connection" CHECKED. Thereafter launches are silent. NEVER kill rtiAssistant,
rtiexec or rtiForwarder - an already-answered assistant is what makes unattended
launch work.

RISK A (sec 6, the session-startup dialog) DID NOT FIRE in any run.

DEFECTS: sec 11.2 recorded THREE. SIX were ultimately found and all are fixed -
the three recorded plus (4) the readiness poll waited for rtiexec, (5) UDP 4000
was required for health although it is connection-dependent, (6) the health
expression was duplicated and only one copy was corrected. Detail in the session
doc sec "LaunchVrf.ps1 - VERIFIED END TO END".

STILL NOT EXERCISED BY THIS GATE (be honest about scope): -Mode BackendOnly
(the -B crash-risk probe) was never run; cold-boot behaviour (whether the
Assistant re-prompts after a reboot) is UNTESTED - if it does, answer it once or
automate the click (the dialog is Qt, no UI Automation tree; screenshot +
coordinate click works, Connect centre is window-relative (383,553) on a 573x583
dialog).
