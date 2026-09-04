# COLD-START ADVERSARIAL REVIEW - commits b8f5e1f + 529fe5c (2026-09-03)

Reviewer: cold-start, no project memory, read-only. Sources: the two commits, both preregs,
DIFF/HANDOFF/OPUS App. B, LaunchVrf52.ps1, rid copy, StackIdentity.cs, VrfFacade.cpp, runs/launch52,
MAK-ONE-2025-Config.xml, the 5.2d remoteControl sample, vrfVlKeyboard.h / vlKeyboard.h, MAK RTI 4.6.1
Reference Manual + Users Guide and UG52 4.1.2/4.1.3/5.3.1 body text (fitz), plus a read-only
process/env inventory at review time. VERIFIED = a file holds the observation; ASSUMED = prose only.

## 0. Facts from the primary sources that the narrative does not carry
- F1 runs/launch52 has NO capture for 3803 (RtiProbe), 3804/3807/3815 (CreateOne), 3809 (RunSim) or
  the rtiSimple pairs: every "joins / BackendCount=1 / entityId 1:3805:N / reciprocal reflect" claim
  is prose. Sim log 3805 has NO creation line for either CreateOne entity (no "Locally Simulated"/
  ORACLETEST at notify 3): the 5.0.2 creation-line oracle is SILENT on 5.2 and nobody noticed.
- F2 netdump_3814.txt is a PARSE ERROR on --exConnConfigFile plus the usage banner; neither
  listenHLA1516e_64.exe nor netdumpHLA1516e_64.exe contains the string "exConnConfigFile" (byte
  grep). "listen joined with the sim's own --exConnConfigFile" cannot be true as written;
  listen_3813.txt shows RTI init lines only, no join line.
- F3 A NEW rtiAssistant is alive: pid 54616, C:\MAK\makRti5.0.1\bin\gui\rtiAssistant.exe -K
  (--autoExit = LRC-spawned), owner = the user, MainModule readable (NOT elevated), MainWindowHandle
  0, started 19:45:48 local - between WatchVrf 3812 (23:44:42Z) and listen 3813 (file 19:48). Some
  4.6.1 LRC ran without RTI_ASSISTANT_DISABLE and without the MAK_RTIDIR override (Machine
  MAK_RTIDIR=makRti5.0.1 selects the 5.0.1 assistant). No record names it.
- F4 The Machine PATH now BEGINS with C:\MAK\makRti5.0.1\bin (ships its own librti1516e64.dll). Logs
  3799/3801 (12:13/12:29) show no 5.0.1 entry; log 3805 (19:31) does. The environment changed mid-
  session; memory's "PATH still lists makRti4.6.1" is false; any 5.0.2 process launched without a
  PATH prefix binds the 5.0.1 RTI DLL; elevated assistant pid 97708 is gone and nothing says how.
- F5 scripts/RunC2SimScenario.ps1 carries 155 UNCOMMITTED lines (a 5.2 profile with MAK_RTIDIR/
  RTI_RID_FILE/RTI_ASSISTANT_DISABLE) that belong to neither commit under review.

## 1. Rulings
R1 "Elevated 5.0.1 assistant on 6003 version-rejects every 4.6.1 LRC; blocks both stacks."
- Competing: any assistant is version-locked (symmetric); the culprit is "one assistant per port,
  version-locked, and Machine MAK_RTIDIR=5.0.1 makes every unguarded LRC re-spawn a 5.0.1 one".
- Falsifier in record: the assistant's toasts + rtiSimple 4.6.1 vs 5.0.1 behaviour - ASSUMED (user-
  supplied, no file). VERIFIED only: 3799 log "RTI Connection failed." with RTI_RID_FILE=4.6.1 rid
  and no DISABLE; 3801 log joins with DISABLE + repo rid (a TWO-variable change).
- "Elevated" is INFERRED from window invisibility (see R10); never tested (Path/MainModule access).
- Verdict WEAK on framing, CONFIRMED on "an assistant blocked 4.6.1 LRCs". F3 proves the standing
  hazard is the Machine env, not one leftover process: it re-created itself today. Neither RTI
  manual (4.6.1 or 5.0.1) documents assistant version checking - the toast is the only source.
R2 "DISABLE + rid(configureConnectionWithRid 1) is a documented, complete, headless bypass; rid alone
   is not."
- Ref Manual 5.2.10 (quoted): "create an environment variable called RTI_ASSISTANT_DISABLE. It does
  not require a value. Its existence causes the RTI to not create the RTI Assistant." Ch. 7: "To force
  the RTI to use the values specified for these parameters instead of those in the connection
  configuration, set RTI_configureConnectionWithRid to 1." CONFIRMED as documented; the rid diff vs
  C:\MAK\makRti4.6.1\rid.mtl is exactly line 30 (0->1) plus a 7-line ';;' header - VERIFIED.
- "rid alone is NOT enough" rests on one toast at 12:25:20 - ASSUMED.
- Licensing hole (Users Guide 8.3, quoted): "If the RTI Assistant is disabled, Disable Unlicensed for
  Two is disabled, and the MAK RTI cannot check out a license, the federate automatically runs in
  unlicensed mode." 8.2 lightweight: "A third unlicensed federate that attempts to join will be
  forcibly resigned"; licensed and unlicensed federates "will not exchange any messages". Evidence of
  licensed runs: NONE in the record (no license line in any log); circumstantial only (sim+gui+
  CreateOne exchanged messages once). The bypass REMOVED the License Not Found dialog, the only
  visible license failure; a 5-federate run with the licence lapsed (2026-09-15) or unreachable
  silently degrades to 2 federates reflecting nothing - the reflected=0 shape. Verdict: CONFIRMED as
  bypass, WEAK as "complete" - DtHaveRtiLicense() pre-flight (A13, parked "later") is now a gate.
R3 "Independent mode is the right frame; 5.0.2 vrfLauncher CLI invalid; combined needs a GUI-saved
   connection." UG52 5.3.1 body: "You must launch a predefined connection from the vrfLauncher at
   least once so that VR-Forces can save the network address information it requires to launch."
   UG52 4.1.2 body matches the script's argv shape. Verdict CONFIRMED (VERIFIED against the PDF).
R4 "5.2 config-file join is correct; explicit federation arg is a safe override."
- VrfFacade::Start always pushes --rprFomVersion 2.0 (VrfFacade.h:95 default, VrfBridge.cpp:115):
  equal to the file's 2.0 rev 2 -> a no-op match, not an override; --rprFomRevision / --netnFom* are
  not pushed, so the file's rev 2 / 3.0 rev 1 stand. Whether VR-Link resets the revision when
  --rprFomVersion is re-parsed is ASSUMED (no doc cited; join success does not test FOM parity in
  lightweight mode).
- "entityId 1:3805:N proves a shared federation": the CreateEntity interaction reached THE sim and the
  sim answered on the control channel - adequate for interactions, VERIFIED only by prose (F1).
- "Safe override": RtiProbe is create-OR-join, so --execName X != MAK-ONE-2025 CREATES a parallel
  federation and exits 0. The override is a silent false-green path. Verdict WEAK.
R5 "The tool join gate is PASSED" vs reflected=0 on WatchVrf-5.2 and the vendor observer.
- A gate the movement oracle cannot see is not passed: RunC2SimScenario.ps1 references WatchVrf 63
  times and every Phase 2 claim (PREREG_R9_52 static -> moving -> settled, POS/RPT agreement,
  frame_gaps, run_census) is a WatchVrf reflection claim. Relabel: CONTROL CHANNEL PASSED,
  OBSERVATION CHANNEL FAILED; with the creation-line oracle also silent (F1), 5.2 has ZERO working
  entity-truth channels.
- The "vendor observer also blind" leg is REFUTED as evidence (F2/F3): listen cannot take the flag it
  was said to take, printed no join, and an LRC spawned a 5.0.1 assistant in that window - the
  observer most likely reproduced attempt-1's failure, not the sim's silence. Strongest competing
  hypothesis for reflected=0 that the record never names: subscription/class mismatch - the 5.2 sim
  publishes entities under classes our 4.6.1-era reflected lists (or the NETN-Physical module set)
  do not subscribe to; falsifier = the GUI shows them (already named) AND a working vendor observer
  under the FULL env. Verdict: "PASSED" REFUTED as a label; the control-channel result stands.
R6 Facade 5.2 branch -> BASE init(..., false). vrlinkVrfRemoteController.h:92 says the flag governs
   DtRemoteObjectManager (state data), not entity reflection - and 5.0.2 WatchVrf reflected entities
   for two months with the derived overload and true. 3811 (derived,false) and 3812 (base,false) both
   gave 0: the hypothesis is falsified, yet VrfFacade.cpp:428-436 still asserts it as the CAUSE of
   observer blindness, and DIFF sec H line 118 still says "KEPT: disableRemoteDiscovery=true" while
   line 145 says false. Sample parity is a defensible reason to keep it; "it fixed blindness" is not.
   Verdict WEAK: keep for parity or revert for one-variable, but delete the causal comment either way.
R7 App numbers. Rule as written (App. B :927/:1242): "Never reuse"; "numbers allocated but not
   consumed are BURNED, not recycled" - so NO number is reusable, burned, consumed or refused.
   Ledger honesty: 3799/3800 BURNED - honest. 3804 - CLAIMED with purpose only; the refusal (exit 1)
   lives in the prereg, not the ledger. 3813 - no join evidence (F2/F3): "outcome unknown". 3814 -
   the record shows a parse error and no second invocation: never joined -> BURNED, not consumed;
   any flagless rerun is unrecorded. 3815 - "observed by netdump 3814" is unsupported.
R8 "Prototype zero not automatable: DtGetInputLine reads the keyboard only."
   vrfVlKeyboard.h:23-26: DtGetInputLine is non-blocking via DtCharWaiting ("input to be read from
   the keyboard") - consistent with _kbhit(), which ignores redirected stdin; the sample has no
   --script/batch/file option (grep). VERIFIED for PIPED stdin (the observation itself is prose
   only). Overstated as "not automatable": a pseudo-console (ConPTY / pywinpty) or console-input
   injection satisfies _kbhit; VR-Link's own DtPollBlockingInputLine (vlKeyboard.h) polls stdin.
   Verdict WEAK - "not automatable by pipe" is right; the demotion of 5.b was decided on one method.
R9 Doc trims - deleted facts and where they survive:
- "the 5.0.2 Release DLL was rebuilt v145 too" (DIFF CLOSED) - survives NOWHERE in docs; HANDOFF still
  reports deployed bridge A7504441 (v143?) - which bridge is deployed is now undecidable from docs.
- "--sessionId ... must equal the sim engine's" (DIFF CLOSED) - live constraint, survives only in
  UG52 4.1.3 and by accident in the scripts' hard-coded 1.
- "5.b is DEMOTED to symptom discrimination" (HANDOFF) - deleted; sec G still says 5.b ADOPTED as an
  instrument; COLD_START_MAP row 43 still says ADOPT. Three documents, two rulings.
- "piped stdin ignored" (HANDOFF/DIFF) - the observation behind R8 survives nowhere.
- "5.0.2 runs ALSO blocked until reboot or assistant exit" -> remedy dropped; LaunchVrf.ps1 still
  reads Machine MAK_RTIDIR (:213) and sets no per-process override: the 5.0.2 regression control is
  blocked by F3/F4 and no NEXT row says so.
- "manifest records NativeStackInfo + rid" -> rid requirement dropped (sec H keeps NativeStackInfo
  only); "watchdog must treat a clean exit as a failed start" (A12) dropped, still true for
  -UseRtiAssistant. Survive elsewhere (checked): FileLoadException text (START_HERE.md); QPAIR/
  merged-build/-q/type-map stamps, commits and "1,732 EXACT" (their preregs); 3750-3798 (App. B).
R10 "Elevated windows are invisible to non-elevated queries; empty MainWindowTitle != no dialog."
- UIPI blocks messages INTO higher-integrity windows (SendMessage/PostMessage, hooks, SendInput), so
  "AnswerRtiDialog.ps1 cannot click it" is correct Win32. Enumeration/caption reads (EnumWindows,
  GetWindowText, FindWindow) are not what UIPI blocks; "invisible" is not established semantics and
  has cheaper explanations: a tray-only assistant has NO main window at any IL (pid 54616, non-
  elevated, shows MainWindowHandle 0 now) and the 5.0.1 dialog title/class may differ from the
  4.6.1 string matched. Verdict WEAK. LaunchVrf52.ps1 :205 only WORDS the lesson; it tests nothing
  (no MainModule/OpenProcess probe, no assistant binary version/path check via Win32_Process) and
  leaves -UseRtiAssistant on the same blind title match.

## 2. Dissent log (one line each; the evidence that settles it)
- D1 vs "join gate PASSED": settle with ONE evidence file per claimed join (stdout captures for
  3803/3807/3809/3815) and a WatchVrf trace that reflects >0 on 5.2.
- D2 vs "vendor observer also blind": re-run listen under the FULL env (vrlink5.10\bin64 holds an
  identical MAK-ONE-2025-Config.xml), capture its join line; read assistant 54616's history (-H).
- D3 vs "5.0.1 installer leftover" as root cause: settle by who put makRti5.0.1\bin first in the
  Machine PATH between 12:29 and 19:31.
- D4 vs "prototype zero not automatable": one ConPTY-driven run of remoteControlHLA1516e settles it.
- D5 vs the R6 causal comment: one WatchVrf run on the derived overload; still 0 = delete the comment.

## 3. Top 3 defects by consequence
1. The 5.2 stack has NO working entity-truth channel (WatchVrf reflected=0 AND the creation-line log
   oracle silent), yet the gate is labelled PASSED and Phase 2 is queued on it. Relabel, and make
   "one observer reflects >0" the gate.
2. The vendor-observer control (3813/3814) is invalid (no --exConnConfigFile support; an unguarded
   4.6.1 LRC spawned a 5.0.1 assistant at 19:45:48) and its result was used to move suspicion off
   our bridge. Environment drift (Machine PATH now leads with makRti5.0.1\bin; new assistant alive;
   pid 97708 gone) is unrecorded and blocks the 5.0.2 regression control.
3. Assistant-free mode removed the only visible license failure; with no license evidence in any
   log and a licence expiring 2026-09-15, a silently-unlicensed 2-federate cap is a false-green
   waiting for the first 5-federate run. A13 must become a gate now.

## 4. Questions only the user can answer
- Q1 What ran at 19:45:48 on 2026-09-03 that spawned rtiAssistant 54616 (5.0.1, -K)? Was listen
  3813 launched from a shell without RTI_ASSISTANT_DISABLE / MAK_RTIDIR?
- Q2 Who changed the Machine PATH to lead with C:\MAK\makRti5.0.1\bin between 12:29 and 19:31 (RTI
  Chooser? installer repair?), and how did the elevated assistant pid 97708 end?
- Q3 Did the vrfGui (3806) show the two CreateOne entities on screen? (The named falsifier; it was
  available live and is not recorded.)
- Q4 Is the uncommitted 155-line RunC2SimScenario.ps1 change intended as part of this work?
- Q5 Which bridge is deployed (v143 A7504441 or the v145 rebuild)?
