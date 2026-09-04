# RESEARCH - MAK RTI 4.6.1 lightweight vs rtiexec: discovery, transport, golden path

Date 2026-09-03. Read-only. Sources: C:\MAK\makRti4.6.1\rid.mtl; RM = doc\RTIReferenceManual.pdf,
UG = doc\RTIUsersGuide.pdf (fitz-extracted to scratchpad); %APPDATA%\MAK\RTI; runs\launch52. Read and
not repeated: PREREG_52_REFLECTION, RESEARCH_52_OBSERVER_DISCOVERY, COLDSTART_REVIEW sec 1 R2.
SCOPE (coordinator, mid-task): run 3826 (741 objects) is VOID - the sim tick thread died (0xC0000005
in DtDiGuyController::determineInitialHandItem), so listen 3829/3834 and watchvrf 3833 say nothing
about the sim. Q5 is ranked against the 3816 (empty-scenario) set only.

## A. VERIFIED - docs (cited)
D1 rid.mtl:22-29 - with RTI_configureConnectionWithRid 0 the connection block is overridden by the
   assistant and RTI_mcastDiscoveryEnabled is "always 0". Our copy sets it 1, so rid:73-102 are live.
D2 RM 5.1 p.5-2 - rtiexec is REQUIRED for reliable transport (TCP), time management, sync points,
   save/restore, the assistant network map. "Object attribute updates and interactions sent with best
   effort transport always get exchanged directly among the federates."
D3 RM 6.3.1 p.6-5 (lightweight limits) - create/join/destroy always "succeed"; each federate picks
   its own id (pid mod 10000); the fedex identifier is first-3 + last-2 chars of the name; no crash
   detection; name reservation is local-only; "FOM module merging is not supported."
D4 RM 6.8.4 p.6-18 - RTI_processUnknownUpdatesForDiscovery lets "updates from unknown objects cause
   discovery ... fault tolerance for dropped register messages when internal messages are sent using
   UDP". Default 1; rid:270 = 1.
D5 RM 9.4 p.9-5 - multicast discovery is the only documented way to locate a forwarder/rtiexec without
   RTI_tcpForwarderAddr; when it fails the federate "defaults to best effort only". Ours is off.
D6 rid.mtl:89-93, RM Table 4-1 p.4-11 / A-7 - RTI_fomDataTransportTypeControl: 0 = per-FOM, 1 = "All
   FOM data is sent best effort ... the forced setting when using lightweight configurations", 2 =
   all reliable (legal only when configureConnectionWithRid is 1).
D7 rid.mtl:83-87, RM A-6 - RTI_internalMsgReliableWhenUsingRtiexec "is ignored if RTI_useRtiExec is
   set to 0", so internal bookkeeping (register/discover/subscribe) is UDP for us - the case D4
   exists for.
D8 RM 14.3 p.14-5 - "If you use FOM modules, the RTI must distribute the FED file ... The use of FOM
   modules also requires the use of the rtiexec and that internal messages be sent reliably." RM
   14.2.1 p.14-4 carves out lightweight ONLY if every federate names the modules in its own rid via
   (RTI-addCreateFomModule)/(RTI-addJoinFomModule). Ours are commented out (rid:304-305).
D9 UG 7.3 p.7-7/7-8 + Table 7-2 - predefined rtiexec connection: 4001, 229.7.7.7, full compliance,
   forwarder port 5000; predefined LIGHTWEIGHT connection: 4000, 229.7.7.7. Assistant connections
   override rid and command line "unless you force" them - RTI_configureConnectionWithRid 1
   (federates) or --manual (rtiexec/forwarder).
D11 UG Table 4-1 p.4-9/4-10 (rtiexec): -M/--manual == RTI_configureConnectionWithRid 1; -R rid path
   (overrides RTI_RID_FILE); -P udpPort; -T tcpPort; -A destAddrString; -N udpNetworkInterfaceAddr;
   -i tcpNetworkInterfaceAddr; -D distributedForwarderPort; -r useReliable; -f forceFullCompliance;
   -K autoExit; -l logfile; -n level; -q quiet. -A, -f, -P, -r, -T "are ignored unless --manual is
   used". RM Table 5-2 p.5-14 is the matching rtiForwarder set.
D12 RM 10.1.2 p.10-3 - rid recipe for centralized TCP forwarding: useRtiExec 1,
   internalMsgReliableWhenUsingRtiexec 1, fomDataTransportTypeControl 0, tcpForwarderAddr = the
   forwarder IP, plus tcpPort. "You must run the RTI Forwarder if you are using reliable transport."
D13 There is NO RTI_rtiExecAddr/Port setting: a regex sweep of rid.mtl, RM and UG yields only
   RTI_rtiExecLogFileName, RTI_rtiExecPerformsLicensing, RTI_rtiExecReconnectPause. Federates reach
   the rtiexec via RTI_tcpForwarderAddr + RTI_tcpPort (RM 7.4 p.7-8, D12).

## B. VERIFIED - the saved 5.0.2 golden-path connection (Q3), verbatim

%APPDATA%\MAK\RTI\4.6\Legatus\connections.xml (the whole file, one rtiexec entry):
  <rtiexec name="Legatus (127.0.0.1), 4001, 127.255.255.255 [127.0.0.1], 4001, Forwarder: 5000"
   givenName="" host="Legatus" tcpAddress="127.0.0.1" tcpPort="4001"
   udpAddress="127.255.255.255" udpPort="4001" udpInterface="127.0.0.1"
   additionalForwarderConnectionToMake="0.0.0.0" forwarderPort="5000" chosen="1"
   ownerHost="Legatus"/>
  <configurations name="Non fully compliant" fullyCompliant="0" ridConfigName=""
   ridIsPreconfigured="1" chosen="1"/>
The 5.0 sibling (...\RTI\5.0\Legatus\connections.xml) holds a LIGHTWEIGHT entry instead: <lightweight
  name="229.7.7.7 [10.5.0.2], 4000" udpAddress="229.7.7.7" udpPort="4000" udpInterface="10.5.0.2"
  chosen="1" ownerHost="Legatus"/>
Deltas vs our rid: loopback BROADCAST 127.255.255.255 on interface 127.0.0.1 (not multicast 229.7.7.7
on 0.0.0.0); ports 4001/4001 (not 4000/4000); an rtiexec at 127.0.0.1 with a forwarder on 5000;
fullyCompliant=0 (rtiexec WITHOUT force-full-compliance).

## C. VERIFIED - wire evidence, 3816 set
E1 createone_3824_notify4.txt: we sent 251 RequestMsgKind / DtRequestByClass
   (requestAttributeValueUpdate by class) and got back exactly ONE DiscObjMsgKind, ONE
   reflectAttributeValues, ONE DdmUpdateRoMsgKind - :117754 "FedAmb=>discoverObjectInstance
   2113929217 67 Time and Date-1:3816 8939" (8939 = the sim). Class requests work without an
   rtiexec; the sim answered for all it had - one object.
E2 Same trace :118157-118170: our CreateEntity interaction (payload "NOTIFY4TEST") was "Queued ...
   for Reliable Transport" / "Transport: Reliable" with NO rtiexec and NO rtiForwarder running
   (launch_3816.txt preconditions), and the sim answered "[OK] ObjectCreated ... entityId=1:3816:10"
   (:118341). Reliable-tagged FOM traffic is not being silently dropped here.
E3 Transport polarity is the opposite of the suspicion: in the sim's FOM dump (vrfSim_3816_*.log)
   MAK_TimeAndDate:3384 is HLAreliable, while BaseEntity/PhysicalEntity :494-516 and every
   VrfExtendedAttributes attribute :2858-2863 are HLAbestEffort. The one object that arrives is the
   reliable one; the missing ones are best-effort.
E4 listen_3829_paused.txt:14 "Joined federation MAK-ONE-2025 with federate type VR-Link Listen" - the
   prior report's H1 ("listen never joined") is CLOSED, though 3829 itself is void.
E5 Wire FedExName "MAK25" = first-3 + last-2 of MAK-ONE-2025 (D3): the stacks agree on the federation
   identifier. Handles 7390 / 8939 / 1686 are distinct, so D3's id-collision mode is excluded.
   watchvrf_3819_diag.txt: licence rti=1 vrlink=1.

## D. Answers
Q1 With mcastDiscoveryEnabled 0 and no rtiexec there is no beacon and no central store: peers meet
   ONLY over UDP on RTI_destAddrString:RTI_udpPort (D2, D5). Discovery is register-message driven,
   with update-driven discovery as the UDP-loss fallback (D4, D7); a late joiner needs a retransmitted
   register or ANY update/response. requestAttributeValueUpdate / provideAttributeValueUpdate DO work
   without rtiexec (E1), so a quiescent publisher is reachable IF it registered the object.
Q2 No doc says reliable traffic is dropped; the documented behaviour is downgrade - forced best effort
   in lightweight configurations (D6), the reliable-internal switch ignored (D7), "defaults to best
   effort only" when no forwarder is found (D5). E2/E3 confirm it. "Interactions arrive, objects
   never" is NOT a reliable-vs-best-effort story.
Q3 Section B, verbatim.
Q4 rid-configured equivalent of that rtiexec loopback connection (keys per D11-D13; there is no
   rtiexec-address key - the forwarder address is the rendezvous):
     (setqb RTI_configureConnectionWithRid 1) (setqb RTI_useRtiExec 1) (setqb RTI_tcpPort 4001)
     (setqb RTI_internalMsgReliableWhenUsingRtiexec 1) (setqb RTI_fomDataTransportTypeControl 0)
     (setqb RTI_forceFullCompliance 0) (setqb RTI_tcpForwarderAddr "127.0.0.1")
     (setqb RTI_udpPort 4001) (setqb RTI_destAddrString "127.255.255.255")
     (setqb RTI_networkInterfaceAddr "127.0.0.1") (setqb RTI_tcpNetworkInterfaceAddr "127.0.0.1")
     (setqb RTI_distributedForwarderPort 5000) (setqb RTI_mcastDiscoveryEnabled 0)
     (setqb RTI_distributeFedFile 1) (setqb RTI_fomModuleMerging 1)
   Headless start from C:\MAK\makRti4.6.1\bin, same RTI_ASSISTANT_DISABLE env as the federates:
     rtiexec.exe -M -R <repo>\config\rid-461-rtiexec.mtl -P 4001 -T 4001 -A 127.255.255.255
                 -N 127.0.0.1 -i 127.0.0.1 -D 5000 -r -K -l <runs>\rtiexec_<n>.log -n 3
   -M is mandatory or -P/-T/-A/-r are ignored (D11). It listens on TCP 4001 and UDP 4001 and starts
   its own rtiForwarder, exiting if it cannot (UG 4.2.1 p.4-11, RM 5.3 p.5-12); to start one anyway,
   RM Table 5-2 with -M -P 4001 -T 4001 -D 5000. Verify without the assistant via the -l log plus
   listeners on TCP/UDP 4001 owned by the rtiexec pid - NOT via the rti tool, whose handles are "the
   handle the RTI Assistant uses" (RM 5.4 p.5-16). Project rule stands - START rtiexec/rtiForwarder,
   never kill them; a per-run rtiexec is docs-backed but UNTESTED.

## E. Q5 - ranked hypotheses (3816 set), cheapest test first
H1 (top) PUBLISHER SILENCE: the sim never registers an HLA object for a remotely created entity in
   this state, so there is nothing to discover. Fits every datum symmetrically - no creation line in
   the sim log, one object answered out of 251 class requests (E1), vendor reflected counts 0.
   Falsifier: a register/publish line for entityId 1:3816:N in a notify-4 SIM log, or the entity in
   the running vrfGui. Test: relaunch at --notifyLevel 4, CreateOne, grep the SIM log for MultiRegObj.
H2 FOM/HANDLE DIVERGENCE: lightweight does not merge FOM modules (D3) and modular FOMs are documented
   as requiring rtiexec + reliable internals + FED distribution (D8), none of which we have; our rid
   never lists the 17 modules, so D8's carve-out is unmet and each LRC derives class handles from its
   own merge. Falsifier: identical handles for BaseEntity.PhysicalEntity on both sides. Test: diff the
   sim notify-4 handle table against createone_3824_notify4.txt - H1's relaunch covers it.
H3 CLOCK-GATED PUBLICATION: publication happens only while the sim clock runs, so a paused sim
   registers nothing. Same symptom as H1, different fix. Falsifier: entities still undiscovered after
   a RunSim(play) that verifiably took effect. Test: the Traffic run already in flight - play, watch.
H4 (e) RELIABLE TRAFFIC NEEDS THE FORWARDER: near-dead on arrival. E2 delivered a reliable interaction
   with no forwarder; E3 shows the arriving object is the reliable one; D5/D6 document downgrade, not
   loss. Residual test: a rid copy with RTI_fomDataTransportTypeControl 1 (no change expected). Last.
H5 SOMETHING ELSE: D3's remaining lightweight failure modes - federate-id collision, silent
   wrong-federation join - are both excluded by E5. No further doc-supported candidate.
Order: (1) one notify-4 sim relaunch, serving H1 (register line) and H2 (handle table); (2)
play-then-watch on the vehicle-only scenario [H3]; (3) rid transport variant [H4]; (4) only if H1/H2
survive, stand up the Q4 rtiexec connection - the one change that both restores the 5.0.2 golden path
and makes FOM-module merging legal (D8).

## F. INFERRED / not verified
- That the sim binds the same interface we do (both take networkInterfaceAddr 0.0.0.0 = "first device
  found", UG Table 7-2); never measured, and the 5.0 assistant file pins 10.5.0.2.
- Lightweight behaviour when RTI_tcpForwarderAddr names a host with nothing listening: D5 is written
  for the mcast-discovery path, not this one.
- Whether VR-Forces passes its 17 FOM modules on the HLA-Evolved join call (RM 14.2 would treat that
  as spec-defined) or relies on rid merging - not checked in the 5.2 sources.
- Whether MAK_TimeAndDate arriving proves handle agreement or only echoes a name string; H2 is open
  because of it.
