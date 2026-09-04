# COLD-START ADVERSARIAL REVIEW - the rtiexec cause claim (commits 7f32a8b, e470e92 + working tree)

Reviewer: cold-start, no project memory, read-only. Sources: PREREG_52_RTIEXEC, PREREG_52_REFLECTION
sec 5, RESEARCH_52_HLA_CONNECTION_CONFIG, RESEARCH_RTI_CONNECTION_TRANSPORT, COLDSTART_REVIEW_2026-09-03,
the four rid files vs C:\MAK\makRti{4.6.1,5.0.1}\rid.mtl, every runs/launch52 capture named in the brief,
C:\MAK\logs callstacks, the rtiexec 15720 log, UG52/UG502/IOG/RTIUG501 PDFs (fitz), VrfFacade.cpp,
WatchRunner.cs, StartRtiExec52.ps1, the uncommitted RunC2SimScenario.ps1 diff, a read-only process
inventory. VERIFIED = a file holds it; INFERRED = prose only. Working tree moved during the review
(e470e92 landed; RunC2SimScenario.ps1 and StartRtiExec52.ps1 are now the uncommitted 5.2 profile).

## 0. Facts the record does not carry
- F1 UG52 page index 189 (printed 190), sec 5.5.1, two bullets verbatim: "If you are using the MAK RTI
  and are running multiple, concurrent federation executions, you must run the rtiexec." / "You cannot
  use the MAK RTI in lightweight mode with VR-Forces." UG502 idx 185 (printed 186) carries only the
  qualified form ("...if you are running multiple federations"). Quote REAL, change REAL. "Lightweight
  mode" is the MAK RTI term for no rtiexec (IOG 1.3 p11 "run in lightweight mode without an rtiexec";
  RTIUG501 7.3 predefined lightweight connection 4000/229.7.7.7) = RTI_useRtiExec 0. IOG (MAK-25.0)
  idx 10/82 still says lightweight is fine generically; the VR-Forces sentence is the specific one.
- F2 rid-501-rtiexec-min.mtl differs from stock 5.0.1 rid.mtl in SIX keys, not twelve: configureConnection
  WithRid, useRtiExec, udpPort, tcpPort, destAddrString, networkInterfaceAddr. The other six of the
  "twelve" (tcpForwarderAddr 127.0.0.1, forwarderPort 5000, internalMsgReliable 1, fomDataTransport 0,
  mcastDiscovery 0, forwarderRoutesFile) are already stock. rid-501-rtiexec.mtl (the "crashing" rid)
  differs from stock in SEVEN; from rid-min in exactly ONE: RTI_tcpNetworkInterfaceAddr 127.0.0.1 vs
  0.0.0.0. distributeFedFile and fomModuleMerging are 1 in stock and in BOTH rids - never a variable.
- F3 The parseCmdLine crash is NOT the rid. C:\MAK\logs\...215720-...-39028.callstack.log (thread 41792,
  0xC0000005, vl.dll <- vlHLA1516e.dll <- DtVrfSimOptions::parseCmdLine(768)) is byte-identical to the
  3848 callstack (38180) and is run 3853 - launched on the LIGHTWEIGHT rid rid-501-ridconfigured-notify4,
  the rid 3851 and 3852 had just joined on. runs/launch52/vrfSim_3853_console_rtitrace.txt holds that
  callstack, yet the ledger says 3853 "stalled at 4 threads, never joined". Neither crash produced a
  .log (crash precedes log open); the rid files were committed 22:01, after both runs; the shared trigger
  of 3848/3853 is unrecorded. Two of five sim launches that evening died this way.
- F4 launch_3854_rtiexec_min.txt (named in the brief) does not exist. Sim evidence = vrfSim_3854 log:
  argv "--deviceAddress 127.0.0.1 --hostAddressString 127.0.0.1", RTI_RID_FILE rid-min, "Joined
  federation MAK-ONE-2025", 34 SpawnPt lines; process 59296 still alive.
- F5 rtiexec 15720 log: 3 JoinMsgKind (VR-Forces Sim Engine 5.2d, remoteControl x2 = 3855, 3856), 2
  resigns; never more than TWO federates joined; forceUnlicensedForTwo 0, rtiExecPerformsLicensing 0.
  58 FomModuleDistExecMsgKind: the exec distributed FOM modules - the mechanism lightweight lacks (RM
  14.3). Exec runs "-M -R rid-501-rtiexec.mtl -P 4001 -T 4001 -A 127.255.255.255 -N 127.0.0.1
  -i 127.0.0.1 -D 5000 -r"; -M makes the CLI govern (RTIUG501 7.3 --manual), so its rid choice is moot
  for the connection block; ridConsistencyChecking 0 on all sides, so nothing would report a mismatch.
- F6 watchvrf_3856 arithmetic: at all six samples readable = ent + ctl and the (NaN,90,NaN) lines number
  exactly 19 = ctl. The placeholders are the 19 CONTROL OBJECTS (spawn points/paths), not "entities
  without a resolved location". env=19 == ctl=19 at every sample: 19 environment processes in Traffic is
  implausible - the 5.2d reflectedEnvironmentProcessList count is suspect (instrument, cheap to settle).
- F7 Entities are the sim's: Traffic.scn terrain "Ala Moana.mtf" (Honolulu) matches POS 21.29N/-157.86W;
  -NoGui; WatchVrf creates nothing; ent 44->62 while the sim log shows spawn points working. Q4: yes.
- F8 Controls: 3839 VALID (4.6.1 lightweight, Traffic, clock started 21:25:18 by 3837, 60 s, trailer,
  backends=1, vendor 0; sim 3835 has SpawnPt lines = entities existed). 3845 VALID but PAUSED (joined
  21:42:42, RunSim 3846 at 21:45:23; 40 s). 3847 CUT at 3 s - invalid, yet "3844-3847 with 0 reflected"
  leans on it: no valid RUNNING 5.0.1-lightweight negative exists. Vendor listens 3829/3834 ran against
  sim 3826, which the record itself says crashed on first tick - VOID. listen_3838 (Traffic 3835 running,
  4.6.1 lightweight, joined, 30 s, "[0, 0]") is the one valid vendor negative and is cited NOWHERE.
- F9 Interface cannot be the lightweight failure: sim and observer shared one rid (networkInterfaceAddr
  0.0.0.0 = first device) and traffic crossed BOTH ways (createone_3824 discovered "Time and Date-1:3816";
  CreateEntity answered with ObjectCreated; RunSim's run() took effect). In rtiexec mode the RTI-layer pin
  is not a second variable: it is one of the six rid keys and is REQUIRED by the loopback broadcast
  127.255.255.255. The only genuinely separable variable is VR-Forces' --deviceAddress/--hostAddressString,
  documented (UG52 Tables 10/11) as the card for UDP traffic / host address, with no HLA data role stated.
- F10 "Received 1 messages that have a received size that does not match the header size" in every 5.0.1
  lightweight federate (3845/3846/3847): foreign packets on 229.7.7.7:4000 (a 4.6.1 process or the four
  -K assistants 54616/19612/42396/49336, all still alive). Unexplained and unrecorded.
- F11 UG502 Table 6 note (dropped in UG52): "lightweight RTI connection on Linux you must enable Ignore
  Advisories or the simulation engine will not publish entities" - the only vendor MECHANISM statement.
  Checked: UG52 --ignoreAdvisories defaults true; all advisory switches read "disabled" identically in
  logs 3835/3844/3854. Advisories do not discriminate; the mechanism stays unexplained.

## 1. Rulings (Q1-Q7)
Q1 Quote real, meaning as claimed, term correct (F1). CONFIRMED.
Q2 Honest that (a)/(b) were not separated; DISHONEST by omission on what (b) is. The RTI pin is part of
   (a); F9 shows the lightweight blindness was not an interface effect; (b) as --deviceAddress has no
   documented HLA role, one confirming run, and is now hard-wired "NOT a knob" in RunC2SimScenario.ps1
   and as the VrfFacade default. Cheapest discriminator: WatchVrf 3857 against the live sim 59296 with
   the facade's --deviceAddress suppressed (observer-side, no relaunch); then one sim relaunch on rid-min
   without -DeviceAddress. Docs make (a) sufficient-by-requirement (5.5.1; RM 14.3); nothing makes (b).
Q3 4.6.1 lightweight running: valid (3839, 3838). 5.0.1 lightweight: paused only (3845); running cut
   (3847). "Traffic blind on both lightweight stacks" holds for discovery, overstated for running.
Q4 The sim's scenario (F6, F7). CONFIRMED; the NaN lines are control objects; env count suspect.
Q5 Inferred only, and REFUTED by the record's own 3853 evidence (F3). "Three keys" is wrong on two (F2).
Q6 Placeholders and ctl/env: F6. Four assistants: alive, unrecorded consequence F10. Exec on the
   superseded rid: moot under -M (F5), but "exec on rid-min" has never actually run. Licence: with two
   federates the unlicensed-for-two cap is untested; A13 is a declared gate (HANDOFF:172) with no
   implementation (DIFF A13 still "N (later)"; no runner check beyond a file-existence warn).
Q7 Previous review: gate relabel CLOSED (HANDOFF:167-171, DIFF:134-136); vendor control acknowledged
   invalid but the valid replacement (3838) unused; licence gate declared, not built; env drift recorded
   (DIFF:143) with Q1/Q2 (who spawned 54616, who changed PATH) still unanswered; doc trims restored
   (DIFF:145-146), deployed bridge still undecidable; Q4 recurs as a new uncommitted 48-line runner diff.
VERDICT on the claim: "repaired" SUPPORTED (one run, one scenario, two federates, 60 s). "Caused by
lightweight mode" SUPPORTED BY ELIMINATION (interface excluded, scenario excluded) with NO mechanism:
lightweight passed one object class and all interactions and dropped entity classes - "unexplained
residue: none material" is false. "Interface 127.0.0.1 is half the posture" NOT SUPPORTED.

## 2. Dissent log (one line each; the evidence that settles it)
- D1 vs "three extra rid keys caused the crash": settled already by 39028.callstack (3853, lightweight
  rid); relabel 3853 CRASHED, find the 3848/3853 common factor before the runner meets it again.
- D2 vs "--deviceAddress is half the posture": one WatchVrf run without it on the live sim settles it.
- D3 vs "no unexplained residue": a notify-4 observer trace in rtiexec mode vs 3824 (lightweight) showing
  FOM-module distribution / class handles settles the class-selective blindness; F10 needs a sender.
- D4 vs env=19: print the environment-process list names once.
- D5 vs licence: one 4-federate run (sim+gui+app+WatchVrf) on rid-min with the RTI licence line captured.

## 3. Top 3 defects by consequence
1. The crash attribution is false and the ledger hides the counter-evidence (F3): the runner's 5.2
   profile is being built on a trigger nobody has identified, which killed 2 of 5 sim launches.
2. A documented-no-op candidate (--deviceAddress) is baked into the facade default and the profile as
   non-tunable "documented posture" on zero discriminating evidence (F9, Q2); every future run carries a
   hidden variable and the record teaches a wrong cause.
3. Evidence hygiene: the named 3854 capture is missing (F4); void vendor runs cited, the valid one not
   (F8); a cut run counted as a control; "twelve"/"three" key counts wrong (F2); env==ctl uninvestigated;
   the licence cap untested at two federates (F5) with the licence expiring 2026-09-15.
