# TYPEFIX CONFIRMING RUN 2 - 2026-07-22 (VOID: back-end never joined the RTI federation)

RAW OBSERVATIONS ONLY. No verdicts. The supervisor adjudicates per
PREREG_TYPEFIX_CONFIRMING_RUN.md. ASCII only. Executor: RUN 2 (fresh-boot RTI,
the registered discriminator for the RUN 1 crash).

## 0. Identity

- Run id / dir  : runs/20260723T002834Z_run (artifacts mirrored into this directory
                  with .txt/.csv extensions; the repo .gitignore drops *.log and runs/)
- Command       : pwsh -File scripts/RunC2SimScenario.ps1
                  -Init data/R9_Mojave_Lean_Initialization_NoComments.xml
                  -Order data/R9_Mojave_UnitMove_Order_NoComments.xml -RunSecs 900
                  (TimeMultiplier=1 default - no SetSimRate join; server check ON;
                   BYTE-IDENTICAL command to RUN 1 except the fresh-boot RTI)
- Repo HEAD     : ef75914 (branch main) - R9 type-mapping fix
- App exe       : src/VrfC2SimApp/bin/Release/net10.0/win-x64/VrfC2SimApp.exe
                  built 2026-07-22 19:07:09 local (SAME exe as RUN 1);
                  --translator-selftest re-run by the executor BEFORE the run = PASS
                  (22/22 [corrected: originally logged 21, a miscount; the tool emits 22
                  PASS lines]; all R9 assertions green incl. 1222.MechPlt(D)->TankPlatoonUSA).
- appNos (Appendix B marker 3591): 3591 backend, 3592 frontend, 3593 oracle-pre,
  3594 trace, 3595 app, 3596 createOne-diag. Marker advanced 3591 -> 3597 by the runner.
  6 CONSUMED, 0 BURNED.

## 1. HEADLINE (raw)

The VR-Forces BACK-END (vrfSimHLA1516e) FAILED TO JOIN/CREATE the RTI federation
execution at launch and SELF-TERMINATED CLEANLY. Its own log (vrfSim.log, last write
20:29:05 local) reads:

    Could not create Federation Execution CWIX-2024: RTI exception:
      RTIinternalError TCP connection has been broken.
    Could not create a valid network connection.  No FOM specified.
      Stopping run of back-end.

NO crash dump was written (newest dump in bin64 is still RUN 1's 19:18:32). ZERO units
were created (the app logged "No backends found for object creation" x6). The oracle
saw reflected=0 for the entire trace. No movement window ever opened for any taskee.

This is NOT the RUN 1 signature. RUN 1's back-end successfully CREATED 6 units + delivered
the order + issued 3x CreateRoute, then crashed with an access-violation DUMP at 19:18:32,
before MoveAlongRoute. RUN 2's back-end never hosted a single unit - it failed EARLIER, at
RTI federation-join, and self-stopped with an explicit RTI-transport error (no dump). The
registered H-CONTENT load (3x route-create) was therefore NEVER exercised in RUN 2.

DISTINGUISHING CONTEXT vs Cell C / RUN 1: those launches had a PERSISTED auto-connect
rtiAssistant and raised NO "Choose RTI Connection" dialog. RUN 2's fresh boot (executor
stopped the RTI trio per the consumed authorization) left NO rtiAssistant, so LaunchVrf
spawned a fresh one that PROMPTED the dialog; the executor answered it via the DPI-click
recipe. The back-end's federation-join failure occurred DURING the fresh-boot RTI bring-up
churn (multiple rtiexec/rtiForwarder instances spawning/dying), BEFORE the RTI stack settled.

## 2. TIMELINE (this machine stamps logs UTC; local = UTC-4)

| local     | UTC        | event (source) |
|-----------|------------|----------------|
| 20:28:34  | 00:28:34   | runner launched; run dir 20260723T002834Z created; marker 3591->3597 |
| 20:28:36  | 00:28:36   | vrfGui (73688) started (front-end) |
| 20:28:41  | 00:28:41   | fresh rtiAssistant (40956) started, showing "Choose RTI Connection" dialog |
| ~20:28:41 | ~00:28:41  | back-end vrfSimHLA1516e (9896) started (thr grew 8->18) |
| 20:28:50  | 00:28:50   | executor DPI-click answered the dialog (Connect at physical 2018,1316; DIALOG_GONE); RTI churn: rtiexec 72748/76380 + rtiForwarder 58800 seen |
| 20:29:05  | 00:29:05   | vrfSim.log LAST WRITE: "Could not create Federation Execution CWIX-2024: RTIinternalError TCP connection has been broken ... Stopping run of back-end." |
| 20:29:11  | 00:29:11   | LaunchVrf READY: back-end reported HEALTHY (16 threads), front-end main window up (launchvrf.stdout.txt). EXIT=0 |
| 20:29:15-18 | 00:29:15-18 | RTI stack settled: rtiexec 60672, rtiForwarder 61696 (replacing the churn instances) |
| 20:29:35  | 00:29:35   | back-end 9896 still present (thr=18); transient 2nd back-end 63492 (thr=0) appears |
| ~20:29:40-47 | ~00:29:40-47 | BOTH back-end processes (9896, 63492) GONE. No vrfSimHLA1516e thereafter (confirmed absent across repeated samples through teardown) |
| 20:30:32  | 00:30:32   | observers started: WatchVrf-trace (3594), ListenReports; watchvrf-precheck (3593) had already returned POS=0 (advisory) |
| 20:30:52-53 | 00:30:52-53 | PushInit: server RUNNING->INITIALIZING->push OK->RUNNING; QUERYINIT 6 Units SystemName=STP (pushinit.stdout.txt) |
| 20:30:57  | 00:30:57   | app: "Connected to C2SIM"; "Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0))"; 6 units dispatched; "Sim Run() queued (timeMult=1)"; then "No backends found for object creation" x6 |
| 20:31-20:33 | 00:31-00:33 | Stage 7 oracle gate polled reflected=0/readable=0 (trace t~21s..t~200s), FAILED at 180s |
| 20:34:03  | 00:34:03   | Stage 7b CreateOne (3596): joined federation CWIX-2024 with BackendCount=0; "no backend discovered after 15 s ... Refusing to issue the create." EXIT=1 |
| ~20:34:1x | ~00:34:1x  | teardown: StopIface EXIT=0 (server -> UNINITIALIZED); VrfC2SimApp (72936/appNo 3595) exited code 0 - CLEAN RESIGN |
| ~20:54:52 | ~00:54:52  | WatchVrf-trace / ListenReports run their full 1460s timers (never killed), then StopVrf (see sec 7) |

Note: the ~5.89 s trace-t0 offset convention (prereg sec 3.4) is not load-bearing here -
no task-onset alignment is possible because no MoveAlongRoute was ever issued and reflected
stayed 0 the whole trace.

## 3. VALIDITY GATES (prereg sec 2) - status

- Type-mapping active (RealTemplates, not GoldenParity): **PASSED (fix confirmed on)** -
  vrfc2simapp.log: "Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA)
  (11.1.225.3.2.0.0))". NOT a GoldenParity void. (Offline: selftest 22/22 re-run pre-launch;
  originally logged 21, a miscount.)
- 6 units created: **NOT MET** - the app DISPATCHED 6 units ("Init dispatched: 6 units + 0
  areas") but the back-end was already gone, so each create returned "No backends found for
  object creation". ZERO units instantiated. Trace reflected=0 throughout (never 3->47).
- Creation on REAL coordinates (post-init oracle GATE, stage 7 FATAL): **FAILED** - no
  real-coordinate POS line within 180s; 0 POS lines, reflected=0. Run exited 3 (stage 7).
- Order delivery / PushOrder EXIT=0: **NOT REACHED** - the runner failed at stage 7 (oracle
  gate) and never advanced to stage 8 (PushOrder + observation). The order was never pushed.
- Three MoveAlongRoute task lines: **NOT MET** - no creation, no routes, no move. n/a.
- ListenReports (RPT channel): **EMPTY** - no reports (no back-end, no movement).
- Oracle live (reflected>0): **NOT MET** - reflected=0 for the entire trace. Independently
  corroborated: CreateOne joined the same federation and found BackendCount=0.

## 4. PER-TASKEE RAW OBSERVATIONS

Not applicable - no units were created and no movement window opened. The app dispatched
6 units for creation (order of dispatch in vrfc2simapp.log):
  1222.MechPlt, 114.MechCoy, 1143.MechPlt, 1.BdeHQ, 1141.MechPlt, 1142.MechPlt
(the 3 order taskees are 1222.MechPlt, 114.MechCoy, 1.BdeHQ; the other 3 are lean-file
supporting units). ALL six create requests returned "No backends found for object creation".

POS channel: empty (reflected=0 for the whole trace; not one POS line for any object).
RPT channel: empty (no PositionReports).
No POS/RPT disagreement to quote - both channels are silent because nothing was created.

## 5. BACK-END FAILURE EVIDENCE (the discriminating artifact)

- vrfSim.backend.log.txt (copy of C:\MAK\vrforces5.0.2\bin64\vrfSim.log; LastWrite 20:29:05):
  the back-end's own report of "RTIinternalError TCP connection has been broken" on the
  attempt to create Federation Execution CWIX-2024, followed by "Stopping run of back-end."
  This is a CLEAN self-stop on a failed RTI network connection, NOT an access-violation crash.
- NO dump file written for this back-end death (bin64 newest dump = RUN 1's 19:18:32).
- vrfc2simapp.log.txt: RealTemplates active; 6 dispatched; then "No backends found for object
  creation" x6. vrfc2simapp.stderr.log EMPTY (the failure was in the back-end, not the .NET app).
- createone-diagnostic.stdout.txt: an INDEPENDENT federate joined CWIX-2024 and observed
  BackendCount=0; refused to create after 15 s. Confirms the federation existed and was
  joinable but had NO simulation back-end registered.
- watchvrf-trace.csv: "joined; discovering + sampling"; reflected=0 readable=0 on every
  sample from t=3.1s to end. (WatchVrf itself joined fine - the oracle was not blind; there
  was simply nothing to reflect.)
- launchvrf.stdout.txt: LaunchVrf declared the back-end HEALTHY by thread count (16 threads)
  at READY - this proxy passed transiently while the back-end was initializing, BEFORE it hit
  the RTI failure and self-stopped ("PROCESS PRESENCE IS NOT HEALTH", RUNBOOK). Same
  connection profile as RUN 1 ("HLA 1516 Evolved RPR 2.0 with MAK extensions"); the dialog
  appeared only because the fresh boot left no pre-existing rtiAssistant.

## 6. COMPETING HYPOTHESES FOR THE RUN-2 BACK-END FAILURE (no verdict - for supervisor)

- HR1 (fresh-boot RTI bring-up race / dialog-answer instability): the back-end tried to
  create the federation DURING the fresh-boot RTI churn (rtiexec 72748/76380 -> 60672;
  rtiForwarder 58800 -> 61696), before the stack settled at ~20:29:15-18. Its TCP link to
  the RTI broke at that moment (20:29:05). Federates that joined LATER (~20:30:32, after the
  RTI settled) - WatchVrf, the app, CreateOne - all joined CWIX-2024 successfully. RUN 1 and
  Cell C used a settled/persisted-auto-connect RTI (no dialog) and the back-end joined fine.
- HR2 (executor dialog-answer suboptimal): the executor answered "Choose RTI Connection" by a
  DPI-aware coordinate click on the Connect button (0.668, 0.949) without independently
  verifying WHICH connection was pre-selected or that "Always try to use this connection" was
  ticked. The back-end DID reach 16-18 threads and vrfGui came up connected, and later
  federates joined the federation - but a suboptimal connection selection cannot be fully
  excluded as a contributor to the broken back-end TCP link. RESIDUAL UNCERTAINTY, flagged.
- HR3 (incidental back-end/RTI instability, base rate): VR-Forces back-end federation-join
  failures have a nonzero base rate on this machine.

RELATION TO THE RUN 1 DISCRIMINATOR: RUN 2 did NOT reproduce RUN 1's crash and did NOT
produce a clean pass. It failed UPSTREAM of everything the experiment tests (the back-end
never hosted a unit), so it does not, on its own, discriminate H-RTI vs H-CONTENT/H-ENV for
the RUN 1 crash. What it adds: a distinct fresh-boot-RTI-join failure mode tied to the
cold-dialog bring-up.

## 7. END-STATE INVENTORY (final, 20:56 local)

- vrfSimHLA1516e : DEAD (self-stopped at federation-create ~20:29:05; process gone by ~20:29:47). No dump.
- vrfGui / vrfLauncher : GONE. Closed by the runner's own StopVrf (EXIT=0) at 20:54:45 once the
  observer waits released (see sec 8 dev 5). "no VR-Forces processes remain."
- VrfC2SimApp (appNo 3595, pid 72936) : EXITED code 0 - CLEAN RESIGN via StopIface (RUN-1 lesson honored).
- WatchVrf-trace (3594, pid 13936) + ListenReports (pid 70320) : STOPPED by the executor at ~20:54:34
  under supervisor authorization (they are the executor's own instruments). WatchVrf had already run
  to trace t=1442.1s of its 1460s window (719 lines, reflected=0 on every sample), so no window was lost.
- StopIface EXIT=0; StopVrf EXIT=0 ("VR-Forces is down; RTI infrastructure preserved").
- rtiAssistant 40956 (born 20:28:41) / rtiexec 60672 (20:29:15) / rtiForwarder 61696 (20:29:18) :
  the FRESH-BOOT RTI trio, UP and UNTOUCHED. Confirmed present at 20:56:17.
- Crash dump : NONE new this run. RUN 1 dump preserved: bin64\vrfSim5.0.2-MSVC++15.0_64-249613-36676.dmp.dmp (19:18:32).
- Runner exit code : 3 (RUN FAILED after VR-Forces was up; teardown ran; evidence partial). Marker 3597.
- C2SIM server (docker) : was RUNNING throughout; next PushInit self-resets.

## 8. DEVIATIONS / FLAGS

1. FRESH-BOOT AUTHORIZATION CONSUMED (as directed): the executor verified PID+name+start
   time then stopped rtiAssistant 8888 / rtiexec 68020 / rtiForwarder 68464 (all born
   ~17:46:45-55 today) before launch. VR-Forces was already fully down. After this the
   executor held NO kill authority over any rti* process and force-killed nothing else.
2. "Choose RTI Connection" DIALOG APPEARED AND WAS ANSWERED (unlike RUN 1 / Cell C, where
   persisted auto-connect held and no dialog appeared). Answered by DPI-aware coordinate
   click (SetProcessDpiAwarenessContext(-4); Connect at window-relative (0.668, 0.949);
   physical click 2018,1316 on the 1635,763 573x583 window). DIALOG_GONE confirmed. This is
   the one procedural difference from RUN 1 and is a candidate contributor to the back-end's
   RTI-join failure (sec 6 HR1/HR2).
3. NO third attempt / NO relaunch was made. The narrow-restart fallback is scoped to the
   forwarder-wedge signature (reflected=0 persisting AFTER a relaunch); a back-end that failed
   its own federation-join is not that signature, and a RUN 3 needs supervisor direction. The
   fresh-boot RTI stack was left intact for the supervisor to inspect / reuse.
4. The runner itself behaved correctly end to end: detected the dead back-end at the stage-7
   oracle gate, ran the stage-7b CreateOne disambiguation (INCONCLUSIVE - no back-end to prove
   the oracle either way), and executed clean StopIface->StopVrf teardown with no force-kill.
5. RUNNER SELF-TEARDOWN STALLED THE EXECUTOR (known wait-state issue): after the app resigned,
   the runner blocked in Complete-Background waiting the observers' full 1460s timers and did not
   re-invoke the executor. On a supervisor nudge, the executor STOPPED its own two observer tools
   (WatchVrf 13936, ListenReports 70320) - authorized as executor instruments - which released the
   runner's waits; the runner then ran its own StopVrf (EXIT=0, closed vrfGui 73688) and exited 3.
   The executor's follow-up StopVrf found nothing to do (vrfGui already gone). Side effect: the
   runner flagged "THE MOVEMENT ORACLE DIED: WatchVrf-trace exit -1" because the executor stopped
   WatchVrf; this is cosmetic - the trace had already captured its full window (reflected=0
   throughout) and there was never any movement evidence to lose. NO federate and NO rti* process
   was force-killed; the RTI trio was left untouched.

## 9. appNo LEDGER (Appendix B annotated; marker 3591 -> 3597)

- 3591 backend   : CONSUMED (joined RTI but failed federation-create; self-stopped; no dump)
- 3592 frontend  : CONSUMED (vrfGui)
- 3593 oracle-pre: CONSUMED (advisory pre-check; POS=0)
- 3594 trace     : CONSUMED (WatchVrf-trace; joined, reflected=0 whole trace)
- 3595 app       : CONSUMED (VrfC2SimApp; joined + CLEAN RESIGN, exit 0)
- 3596 createOne : CONSUMED (stage-7b diagnostic; joined BackendCount=0, exit 1)
Total: 6 CONSUMED, 0 BURNED. (Budget ~6; <=2 burned OK.) Marker at 3597.
