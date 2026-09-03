# PREREG - first VR-Forces 5.2d launch (independent mode, no scenario)

Date 2026-09-03. Tier STANDARD (a launch is reversible; no cause claim yet).
Plan: ~/.claude/plans/velvet-tickling-hamster.md Phase 1 deploy step, re-framed
2026-09-03: the 5.0.2 launch path (LaunchVrf.ps1 / vrfLauncher combined mode) is
INVALID on 5.2d, so the deploy step starts with a new independent-mode launcher,
scripts/LaunchVrf52.ps1, and a first live launch under this prereg.

## 1. Docs consulted (cited, read before writing)
- UG52 4.1.2 "Starting Independent VR-Forces Executables" (p.133): `vrfGui --siteId 1
  --appNumber N --hla1516e`, `vrfSimHLA1516e --siteId 1 --appNumber M`; 4.1.3 session
  ID default 1; 4.1.4 HLA example (p.135) uses NO further options ("use default values
  to specify the HLA configuration parameters").
- UG52 Table 11 vrfSim options (p.181): -n|--notifyLevel (default 2), --logFileName,
  --exConnConfigFile ("./appData/settings/connections/MAK-ONE-YYYY-Config.xml defines
  the default connections settings"), -L relative to ./bin, -q.
- UG52 5.3.1 (combined mode CLI needs a Launcher-saved connection) - why not combined.
- appData/settings/connections/MAK-ONE-2025-Config.xml (read on disk): execName
  "MAK-ONE-2025", fedFileName RPR_FOM_v2.0_1516-2010.xml, rprFomVersion 2.0 rev 2,
  netnFomVersion 3.0 rev 1, 17 FOM modules (NETN-BASE/ETR/Physical/METOC/MRM +
  MAK-*-evolved incl. VRFExt-12 + 2 RPR IFF).
- makRti4.6.1/rid.mtl: RTI_useRtiExec 0, tcpForwarderAddr 127.0.0.1, udpPort 4000;
  5.0.1 rid.mtl differs (DIFF sec H / memory vrf-52-migration-phase0).
- Machine env (read 2026-09-03): MAK_RTIDIR and RTI_RID_FILE point at makRti5.0.1;
  PATH lists 5.0.2/5.8/4.6.1 first. LaunchVrf52.ps1 overrides all of these per process.

## 2. The ONE variable
The 5.2d stack (vrforces5.2d + vrlink5.10 + makRti4.6.1, independent mode, default
MAK-ONE-2025 connection config). Everything else at its documented default: session 1,
site 1, NO scenario (-L omitted), GUI present, notify 3, log redirected to
runs/launch52/. NOT varied here: headless (-NoGui), any scenario, our own federate
(RtiProbe/CreateOne/WatchVrf 5.2 builds - the NEXT prereg), FOM module choices.

## 3. App numbers (ledgered BEFORE the join, OPUS_EXECUTION_PLAN.md App. B)
- 3799 back-end vrfSimHLA1516e (5.2d)
- 3800 front-end vrfGui (5.2d)
NEXT FREE advanced to 3801.

## 4. Predictions (written before launch)
P1 HIGH: the back-end process survives 120 s and its thread count rises above 8
   (5.0.2 healthy = 23-67; a 5.2 value is RECORDED here as the new baseline).
   Falsifier: exit before the deadline, or thread count stuck at 2-4. A miss = STOP
   (read the log; suspects in order: RTI Assistant version/prompt, license, config).
P2 HIGH: the back-end log is created at the -LogFile path (so --logFileName is
   honoured and nothing lands in C:\MAK\logs). Falsifier: no file at that path.
P3 MEDIUM: vrfGui gets a non-empty MainWindowTitle within 120 s. Falsifier: empty
   title = modal (documented candidate: Scenario Startup dialog, UG52 4.1.1 Fig 17).
   A miss is NOT a stop - it is the expected shape of the headless question; record
   the dialog and the setting that disables it.
P4 MEDIUM: no NEW rtiAssistant spawns and no "Choose RTI Connection" dialog appears
   (pid 97708 already listens on 6003). Falsifier: dialog -> AnswerRtiDialog.ps1;
   a second assistant or a "port 6003 in use" line in the log -> record which RTI
   version each belongs to (this discriminates the 4.6.1-vs-5.0.1 assistant question).
P5 LOW (observation, no confidence): whether rtiexec/rtiForwarder appear - connection-
   dependent; whichever happens is recorded, never gated on.
Success for this prereg = P1 AND P2. Federation JOIN is NOT claimed by this prereg;
the log lines naming the federation/connection are recorded for the next one.

## 5. Procedure
1. `scripts/LaunchVrf52.ps1 -DryRun -BackendAppNumber 3799 -FrontendAppNumber 3800`
   (self-test 2026-09-03: 3 negative controls exit 2, positive exit 0).
2. Same command live, capture stdout to runs/launch52/launch_3799.txt.
3. Record: exit code, back-end thread count, GUI title, process inventory
   (rtiAssistant/rtiexec/rtiForwarder pids), log tail (connection + FOM lines).
4. Leave the instance UP for the next prereg (5.2 tool join) if P1 holds; if it must
   come down, close the GUI/sim by their own means - never the RTI processes.

## 6. Result (2026-09-03, two attempts; runs/launch52/)
ATTEMPT 1 (3799/3800): P1 MISSED - back-end exited ~2 s, log tail `RTI Connection
failed.` -> STOP honoured; falsification pass ran. P3 also missed at poll time but
the GUI titled later ("VR-Forces GUI") - the GUI outlives its failed RTI connect.
Diagnosis (CONFIRMED, vendor-corroborated): the RTI 5.0.1 installer left an
ELEVATED 5.0.1 rtiAssistant (pid 97708) holding port 6003; it version-rejects
every 4.6.1 LRC. Evidence: (a) 4.6.1 rtiSimple1516e_64 -> "Received shutdown
message from RTI Assistant"; (b) 5.0.1 rtiSimple -> connects and joins; (c) the
assistant's own toasts, supplied by the user: "RTI component was using a
different RTI version than the RTI Assistant" at 12:13:42 / 12:14:14 / 12:23:19 /
12:25:20 - exactly our 4.6.1 attempts. Competing hypothesis (unanswered dialog /
no stored connection) FALSIFIED: 4.6.1 still rejected after a connection was
stored (12:22 window). Also falsified: RTI_configureConnectionWithRid=1 alone
(assistant still contacted, 12:25:20 toast).
INSTRUMENT DEFECT found: an ELEVATED assistant's windows are INVISIBLE to
non-elevated queries - MainWindowTitle reads empty and FindWindow misses the
'Choose RTI Connection' dialog even while the user sees it. "no window title" is
NOT "no dialog", and AnswerRtiDialog.ps1 cannot see or click an elevated dialog.
FIX (documented, both MAK RTI 4.6.1 Reference Manual 5.2.10): per-process
RTI_ASSISTANT_DISABLE (existence disables all assistant use) + repo rid
config/rid-461-ridconfigured.mtl (RTI_configureConnectionWithRid 1; lightweight,
no rtiexec, UDP/TCP 4000, mcast 229.7.7.7). Gated: 4.6.1 rtiSimple then joins and
exchanges interactions with zero assistant contact. This retires the
once-per-boot Choose-RTI-Connection dialog automation for everything launched
with this env.
ATTEMPT 2 (3801/3802, assistant-free env): exit 0 READY in ~3 polls. P1 PASS -
back-end 36 threads (the 5.2 HEALTHY BASELINE; 5.0.2 band was 23-67). P2 PASS -
log at the -LogFile path (188 lines at failure, 5210 healthy; NOTE the sim ALSO
writes its default C:\MAK\logs\vrfSim*.log regardless). P3 PASS - GUI titled
"VR-Forces GUI". P4 PASS - no new assistant, no dialog. P5 - no rtiexec/
rtiForwarder (lightweight connection, as expected). BONUS beyond this prereg's
claims, from the log (line 189): "Joined federation MAK-ONE-2025 with federate
type VR-Forces Sim Engine 5.2d" - the first 5.2 JOIN, federation name
MAK-ONE-2025 (the connection config's execName; CWIX-2024 is gone). Instance
LEFT UP for the 5.2 tool-join gate.
Adversarial review: strongest competing hypothesis for attempt 1 (no stored
connection, not version) was checked and falsified above; residual unexplained -
none material (the GUI's silent RTI failure at attempt 1 matches the version
toast at 12:13:42 counting one component per process). Federation join by OUR
tools is still unproven - next prereg.
