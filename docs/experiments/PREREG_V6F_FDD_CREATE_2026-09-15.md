# PREREG - V6f launch failures at federation create (2026-09-15, seat)

Written BEFORE probe P1 and before any third V6f launch. Tier HEAVY (cause claim).

## 1. Verified observations (primary sources: runs/launch52 vendor copies, rtiexec log)

- V6f attempts 1 (14:23:21Z, sim appNo 4488) and 2 (14:26:23Z, appNo 4495) both died at
  startup: "Could not create Federation Execution MAK-ONE-2025: RTI exception: ErrorReadingFDD
  Failed to process FOM file <X> ... FOM Reader reports / Bad FDD File. Could not find Document
  Root in FDD File." X = MAK-DIGuy-7_evolved.xml (attempt 1), RPR_FOM_v2.0_1516-2010.xml (attempt 2).
- rtiexec log (rtiexec_20260915T114003Z...36840.log): for both attempts the CREATE was processed by
  rtiexec ("Creating federation MAK-ONE-2025 with handle N. Using FDD files: <18>"), all 58
  FomModuleDistExec blocks arrived in the SAME order and sizes as in every successful create, the
  exchange took 3 s as always, then rtiexec's XML parser reported
  "Entity: line 991: parser error : Extra content at the end of the document" (DI-Guy, a 990-line
  file) / "Entity: line 25669: ..." (RPR, a 25,668-line file), then "line 1: Start tag expected",
  and "Sending Create Response = Error". So the error is raised in rtiexec's FOM Reader on a
  buffer that holds the complete document followed by extra bytes.
- History: 40/40 harvested sim launches since 09-07 and 6/6 since this rtiexec started (11:40Z)
  created the federation successfully. RtiProbe (Stage 2c) DESTROYS the federation on its clean
  stop when it is the last federate, so the sim CREATES on every launch; "Joined federation" in
  the vendor log is the join after a successful create. The create path is not new.
- RtiProbe's own real create at 14:26:16Z (between the two failures) succeeded, as did all 51
  creates it has issued on this rtiexec.
- Launch identity: args, cwd (bin64), rid, connection config, 183 environment variable NAMES and
  PATH length, and the 22 startup lines before the create are identical between the good V6e
  launch (13:56:48Z) and the failed ones. Boot 04:31Z (before V6e). 124 GB disk, 39 GB RAM free.
- Files: every copy of both modules under C:\MAK is intact (equal hashes in vrforces5.2d/bin64,
  vrlink5.10/bin64, vrlink5.10/data/foms; mtimes 2025-10-16). DI-Guy is CRLF (990 CR), RPR is
  LF-only (0 CR); the block sizes rtiexec received equal size minus CR count in ALL creates, so
  the "one junk byte per line from a text-mode read" reading is FALSIFIED by the RPR case.
- Machine: no executor file writes in either window (13:54-13:58Z, 14:20-14:28Z); no Defender or
  disk events; the only event-log entries are three VrfC2SimApp crashes at 14:20:34-57Z from the
  liveness lane's worktree (FileNotFoundException on an assembly under .claude/worktrees/liveness),
  unrelated to the sim.
- Docs read: MAK RTI 5.0.1 Reference Manual (FED file distribution, RTI_distributeFedFile /
  RTI_fullFedFileDistribution / RTI_preferLocalFomModules), Release Notes 5.0.1 (nothing on FDD),
  the vendor rid.mtl comments; web search for the exact parser message: nothing MAK-specific.

## 2. Hypotheses

- H-recv (leading): rtiexec pid 36840 (up since 11:40Z; served 6 sim creates, many joins, and the
  V6e sim's abnormal exit at 31 GB working set) holds a latent parse-buffer fault: a module buffer
  is parsed past its length into stale bytes, so WHICH module fails depends on heap layout.
  RtiProbe's success in between is a favourable layout, not a different code path.
- H-send: the sim's LRC ships a module buffer with junk after the document. The declared sizes
  are exact, so the junk would have to sit inside the declared length; no mechanism found, and the
  same LRC build sent identical inputs in 40 successful creates.
- H-load: FALSIFIED (idle machine, identical exchange timing).

## 3. Probe P1 - RtiProbe x5 real creates on the current rtiexec (machine idle, ledgered appNos)

Each RtiProbe run creates, joins, resigns and destroys MAK-ONE-2025 (exactly Stage 2c).
- Prediction under H-recv (MEDIUM, heap-dependent): at least 1 of 5 fails with the parser error.
- Prediction under H-send: 5/5 succeed (RtiProbe is 51/51 on this rtiexec).
- Reading: ANY failure -> H-recv confirmed -> STOP; the remedy (restart rtiexec) is the user's
  call under the standing rule (never kill rtiexec/rtiForwarder/rtiAssistant). 5/5 -> H-recv not
  excluded, H-send not supported; proceed to P2.

### 3.1 P1 RESULT (15:09:37-15:10:55Z, after the managed rebuild on main 3c71025)
appNos 4502-4506 claimed (ledger marker -> 4507). 5/5 exit 0 "created/joined (config-file
identity) and resigned cleanly". rtiexec view (log lines 182157 -> 206692): Create Response
Success=5, Error=0, parser error lines=0. Reading per sec 3: H-recv NOT excluded, H-send NOT
supported -> P2.

## 4. P2 - third V6f launch (single trigger v6f_go.sh, idle machine, merged app, appNo 4487 late tool)

Not a HIGH prediction either way; this launch is a diagnostic.
- Sim creates and joins (as in 40/40 before) -> the failure was a transient rtiexec state; V6f
  proceeds as pre-registered (PREREG_V6C_LATE_JOINER amendments 1-5 + the platoon-only order),
  now doubling as the STP-822 liveness live confirmation.
- Sim fails a third time with the same parser error while RtiProbe's Stage 2c create succeeds ->
  the sim's create is failing SYSTEMATICALLY on this rtiexec -> STOP. Ask the user for an rtiexec
  restart; no further relaunch without it. If it still fails after a fresh rtiexec, H-send is
  promoted and MAK support (with the user) is the next step.
- Any OTHER failure mode -> STOP and report; do not iterate.

### 4.1 P2 RESULT (15:11:34Z run, sim appNo 4507): THIRD IDENTICAL FAILURE -> STOP on relaunching
Stage 2c RtiProbe create = Success (rtiexec 208240, joined, resigned, destroyed 211578); the sim's
create at 15:11:48Z = "Entity: line 25669: parser error : Extra content at the end of the document"
on RPR_FOM_v2.0_1516-2010.xml, Create Response = Error (213157). Tally on this rtiexec since 14:23Z:
sim 3/3 FAIL, RtiProbe 6/6 SUCCESS, interleaved. The sender identity therefore matters: a
sender-independent receiver "coin flip" would put this split near 1 in 500. Both hypotheses stay
open (H-recv as receiver state that is sensitive to the sim's stream; H-send as the sim's stream
itself); the rtiexec restart remains the user's decision; the user was notified 15:2xZ.

## 4.2 P3 - workaround probe, pre-registered BEFORE any run: the sim takes the JOIN path
Mechanism (MAK RTI Reference Manual, FED file distribution; vendor rid.mtl: "Fedex distributes FED
file during join versus federates reading from disk"): if MAK-ONE-2025 already exists with a
federate joined, the sim's createFederationExecution returns AlreadyExists and VR-Link joins; the
sim never ships the module set, so the failing exchange never happens. In ordinary VR-Forces use
the first federate (GUI or sim) creates and the rest join, so this is a normal posture, not a hack.
Arm: a HOLDER federate = RtiProbe <ledgered appNo> MAK-ONE-2025 1 900 3 (settle 900 s) started
before the runner and verified joined in the rtiexec log; then ONE V6f launch through v6f_go.sh.
The runner's Stage 2c RtiProbe joins the existing federation, its destroy fails ("federates
joined") harmlessly, the sim joins.
Predictions: (a) HIGH: the sim logs "Joined federation MAK-ONE-2025" and reaches READY (the join
path has 49/49 successes on this rtiexec, including 6 sims). A miss here = STOP for good; nothing
else is tried without the user. (b) V6f itself then runs as pre-registered (PREREG_V6C amendments
1-5 + the platoon-only order + STP-822 live confirmation). (c) If the holder's join itself fails,
STOP. Cost: one appNo, one extra remoteControl federate for <= 15 min; no effect on BackendCount
(status messages come from the back end only).
Reading: (a) met -> STP-825 is confined to the sim's CREATE-side module distribution on this
rtiexec instance; the join path is a documented operational workaround until the rtiexec restart.

### 4.3 P3 RESULT (run 20260915T151959Z, sim appNo 4515, holder appNo 4514 pid 75212)
Prediction (a) MET: the holder created (Success, joined 215012); Stage 2c RtiProbe joined/resigned;
the sim's create got "Could not create federation MAK-ONE-2025, because it already exists." (218139)
and JOINED (vendor copy: Joined federation = 1, create failures = 0), READY by thread count,
observers up, init pushed (RUNNING, 6 units), nav-area gate passed. STP-825 is confined to the sim's
CREATE path on this rtiexec; the join path is the operational workaround (RUNBOOK 9c).
The run then FAILED at Stage 8 for an unrelated defect: PushOrder crashed on the server's reply
(STP-830) - the SDK's DetermineProtocol took the literal "<Task>" inside the order file's leading
comment as the root tag, sent the order as BML, and the server answered plain text. Fixed in the data
file (one-line comment); PushOrder hardening in flight. V6f itself (the discriminator) is still owed:
next launch = holder + go script, unchanged recipe.

## 4.4 P4 - V6f on a FRESH rtiexec (pre-registered 15:48Z before the launch)
Fact: rtiForwarder died ~15:34:06Z and rtiexec pid 36840 then exited by itself ("Primary TCP
connection has been broken. Perhaps the RTI Forwarder is no longer running ... Exiting"; its log's
last lines). Nobody in the seat's chain stopped either process (the seat's only kill was its own
capture proxy pid 15952 at 15:33:30Z; the hardening executor reports stopping only its fake server
pid 19000). Cause of the forwarder's death OPEN. Stage 2r (ENSURE-UP) will start a fresh rtiexec
and forwarder at the next launch, so the next V6f launch runs the sim's CREATE on a fresh rtiexec
without a holder - which is the direct test of H-recv.
Predictions: (a) under H-recv (MEDIUM): the sim's create SUCCEEDS on the fresh rtiexec ("Joined
federation", READY). (b) if the create FAILS again with the same parser error on a fresh rtiexec:
H-recv FALSIFIED as "long-lived state"; the sim's stream (H-send) or something persistent in the
sim's module read is promoted -> STOP, MAK support with the user. (c) any other failure -> STOP.
V6f itself then runs as pre-registered (platoon-only order, STP-822 live confirmation).

### 4.5 P4/P5/P6 RESULTS (15:50-16:01Z) - H-recv FALSIFIED, sender read FALSIFIED, cause OPEN
P4: rtiForwarder died ~15:34:06Z (an unrelated user process then took port 5000; forwarder port moved
to 5002, commit c0c4185) and rtiexec exited; on the FRESH rtiexec 75168 the runner's Stage 2c RtiProbe
create was rejected the same way (MAK-DynamicTerrain-2, line 248) and RtiProbe crashed (0xC0000005 in
VrfFacade.Start on the failed create). P5: RtiProbe x3 on it: FAIL, FAIL, OK. P6: RtiProbe x8 interleaved
cwd=bin64 vs cwd=LF-only module copies: 1/4 and 1/4, the LF arm failing on the LF-only RPR file -> the
creator's text-mode read is not the mechanism. Defender excluded (only detection today = the seat's own
pwsh polling loop at 15:50Z; signature updates 04:42Z/11:00Z). Verdict: an intermittent corruption in the
transport/receive path (LRC -> forwarder -> rtiexec) that appeared during the afternoon; joins never fail.
Next for the cause: wire capture (relay on a spare port via a per-process rid copy) and the vendor
(tcpPacketBundlingSize 50000 vs 65 KB FOM blocks) - with the user. Operational: JOIN path via a retrying holder.

## 5. Records owed after the outcome
Jira ticket (new, this failure, verified facts only); RUNBOOK sec 9 addendum; handoff V6 line;
this prereg + results committed under docs/experiments.
