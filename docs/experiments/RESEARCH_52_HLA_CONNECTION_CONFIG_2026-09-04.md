# RESEARCH - the 5.2d equivalent of the 5.0.2 HLA connection configuration

Date 2026-09-04. Read-only (no launches, no C:\MAK writes). Answers the user's 2026-09-04 question "5.0.2 had been configured for HLA connections. Did
you try to see the equivalent in 5.2D?" - we had not. It starts from the C2SIM VR-Forces interface's OWN setup instructions. Sources: IFACE =
c2simVRFinterfacev2.36\ - vrfLauncher.pdf (a screenshot of the 5.0.2 "Simulation Connections Configuration -- VR-Forces GUI + Simulation Engine"
dialog, rendered with fitz and read field by field), README.txt, VRFadditionalFiles\README.txt, runc2simVRFHLApRTI.bat, runc2simVRFHLApTI-NPS.bat,
runc2simVRFDIS.bat, C2SIM-VRForcesv2.26.pdf, plus OpenC2SIM Interfaces\c2simVRFinterface\README.md; UG52 / UG502 = VRFUsersGuide.pdf of vrforces5.2d /
5.0.2; IOG, INST, RN, MG = MAKInteroperabilityGuide (MAK-25.0-03-251119), MAK_ONE_Installation_Guide, VRF5.2ReleaseNotes, VRFMigrationGuide; RTIUG501
= makRti5.0.1\doc\RTIUsersGuide; RM = MAK RTI 4.6.1 Reference Manual; DISK = installed files. PDFs fitz-extracted to the session scratchpad, never into
the repo. Read and NOT repeated: PREREG_52_REFLECTION sec 5, RESEARCH_RTI_CONNECTION_TRANSPORT (D1-D13, sec B), RESEARCH_52_OBSERVER_DISCOVERY,
VRF_5.2_MIGRATION_DIFF rows A1-A13.

## 1. Every parameter the interface's setup requires -> its 5.2d equivalent

"5.0.2" = the vrfLauncher.pdf panel unless marked. NEVER SUPPLIED = absent from our 5.2d independent-mode launches, which pass only --siteId
--appNumber --sessionId --notifyLevel --logFileName --scenarioFileName [--exConnConfigFile], plus --hla1516e for the GUI (scripts\LaunchVrf52.ps1).

| # | 5.0.2 parameter (value) | 5.2d carrier + citation | Status |
|---|---|---|---|
| P1 | Launcher connection "HLA 1516 Evolved RPR 2.0 with MAK extensions"; IFACE README "For HLA you must start VRForces using the vrfLauncher as shown in vrfLauncher.pdf" | Ships only in factory\settings\vrfLauncher\Legacy\ (version="9"), NOT in appData, whose set is "DIS (7) localhost", "DIS (7)", "HLA 1516 Evolved", "HLA 4" (UG52 4.7 p147; DISK). Combined mode = `vrfLauncher --connection "HLA 1516 Evolved"` (UG52 Table 12 p187, 5.3.1 p188; MG 2.1 p16 "The VR-Forces Launcher was refactored") | NEVER SUPPLIED - we bypass the Launcher (UG52 4.1.2 p132-133) |
| P2 | Protocol HLA Evolved | vrfSimHLA1516e.exe + `vrfGui --hla1516e` (UG52 4.1.2 p133) | supplied |
| P3 | Network Interface Address 127.0.0.1 | sec 2: profile `<hostAddress>`; CLI `--deviceAddress` and `(--hostAddressString\|-H)` on BOTH vrfGui (Table 10 p177) and vrfSim (Table 11 p180-181) | NEVER SUPPLIED - biggest gap |
| P4 | Federation Name CWIX-2022 (bat arg 18 = CWIX-2023 / CWIX-2022; IFACE README default MAK-RPR-2.0) | MAK-ONE-2025-Config.xml `<execName value="MAK-ONE-2025"/>`; CLI (--execName\|-x) (UG52 4.7.4 Table 6 p150-151; IOG 1.8.1 p20) | default only - c2simVRF arg 18 and appsettings must change to match |
| P5 | FED File Name RPR_FOM_v2.0_1516-2010.xml | Config.xml `<fedFileName>`; CLI (--fedFileName\|-F) (same cites) | default (same value) |
| P6 | FOM Mapping = Use RPR FOM, RPR FOM Version 2.0, no custom mapper | Config.xml `<rprFomVersion>` + NEW `<rprFomRevision>2`, `<netnFomVersion>3.0`, `<netnFomRevision>1`; CLI --rprFomVersion / --rprFomRevision / --netnFom*; mapper = (--fomMapperLib\|-f) (UG52 4.7.4 Table 6 p151; 5.5.3-5.5.6 p191) | default; the 3 revision keys have no 5.0.2 counterpart |
| P7 | FOM Modules, exactly three: MAK-VRFExt-6_evolved.xml, MAK-DIGuy-7_evolved.xml, MAK-LgrControl-2_evolved.xml; IFACE README "This includes the three FOM Modules indicated in the launcher" | Config.xml `<fomModules>` = 17 (NETN-BASE/ETR/Physical/METOC/MRM, MAK-Physical-2, Aerodrome-1, METOC-3, VRFExt-12, DIGuy-7, LgrControl-2, VRFAggregate-7, DynamicTerrain-2, VRLExt-3, DER-1, RPR-Enumerations_Experimental_IFF, RPR-MAK_Experimental_IFF-4); CLI --setFomModuleList "a,b" or --fomModules (repeat); ORDER matters (IOG 5.3 p84) | NO EQUIVALENT for the 3-module list - sec 3 |
| P8 | Local Settings Designator (empty) | (--localSettingsDesignator\|-S) (UG52 Table 10 p176, Table 11 p186); no config-file key | supplied (empty = default) |
| P9 | Ignore Advisories off; Use Absolute Time Stamps off | --ignoreAdvisories (Table 10 p175, Table 11 p185); --useAbsoluteTimeStamps (4.7.4 Table 6 p150) | supplied (defaults) |
| P10 | Session ID 2; IFACE README "SessionID ... must match the numbers in the .bat file"; VRFadditionalFiles item d: mismatch gives "No backends found" | profile `<sessionId>`; CLI (--sessionId\|-i); "You cannot change the session ID during runtime" (UG52 4.1.3 p133) | supplied; must equal c2simVRF arg 12 |
| P11 | Back-end Site 2 / App 3001, Front-end Site 2 / App 3101 (one panel field each) | No profile pair any more: per-application rows (Number / Site ID / Application ID) in the Launcher advanced grid, "The Next Application ID increments"; CLI (--siteId\|-s) + (--appNumber\|-a), defaults vrfGui 3101 / vrfSim 3001 (UG52 4.1.2 p133, 4.8.2 p155-156, 5.4.1 p189) | supplied (ledgered) |
| P12 | c2simVRF's own federate: app 3201 (bat arg 13), site (14), session (12), VRF address 127.0.0.1 (arg 8); "the combination of Site ID and Application Number must not be duplicated" | Unchanged concept; our bridge is a third federate on the same federation | supplied |
| P13 | Additional Command Line Arguments Front-End / Back-End (empty) | `--run "app" additionalargs-- <args>`; the profile keys Additional{Back,Front}EndCommandLineArguments survive only in the Legacy/Advanced factory files (UG52 5.3.2 p188; DISK) | n/a (empty) |
| P14 | "Set As Auto Connect" button | profile `<autoconnect>` = the Auto Start star column (UG52 4.7.6 p153); `<selectatstart>` controls the dialog (HLA profiles ship 1) | n/a (no Launcher) |
| P15 | RTI = PITCH prti1516e. IFACE README: PATH must START with C:\Program Files\prti1516e\lib\vc141_64, ...\lib, ...\jre\bin\server; CLASSPATH must add prti1516e.jar, prti1516.jar, prti.jar; the .bat sets exactly that | Any RTI supporting IEEE 1516-2010 built with the same compiler is link compatible (IOG 1.3 p10-11); PATH/CLASSPATH mechanism unchanged (IOG 1.3 p11; INST 4.2.1 p40) | NEVER SUPPLIED - all 5.2d work is on the MAK RTI (4.6.1, then 5.0.1). The golden path was never a MAK-RTI path |
| P16 | PATH must contain C:/MAK/vrforces5.0.2/bin64 and C:/MAK/vrlink5.8/bin64; .bat sets QT_QPA_PLATFORM_PLUGIN_PATH and cd's to bin64 | Same requirement with 5.2d paths; the 5.2 runtime-path trap is already ledgered (migration Phase 1) | supplied |
| P17 | VRFadditionalFiles appData/data/userData copied into the install; SMS C2simEx; scenario Bogaland2.scnx | Superseded by VRF_5.2_MIGRATION_DIFF C1/C2 (terrain + SMS re-authoring, fidelity ruling) | out of scope here |
| P18 | GUI/sysdef tweaks: ground clamping, human-disaggregated-movement `(ground-clamp False)`, small-boat `(check-soil-type False)`, 10-char callback limit | Not connection configuration; already ledgered (C8, route work) | out of scope here |

## 2. P3 singled out: Network Interface Address / hostAddress

Panel value 127.0.0.1; IFACE README "The VRForces IP address must be 127.0.0.1 in all HLA .bat files (for DIS the IP address must be that of the
broadcast network being used)". The interface's own docs disagree with themselves - VRFadditionalFiles item 7 says the Launcher's "Network Interface
Address shows the actual IP address of the VRForces, not the loopback address 127.0.0.1" - but that item is written for the DIS/broadcast case, and the
HLA screenshot shows 127.0.0.1. Three layers carry it in 5.2d, all VERIFIED:
- Launcher profile: appData\settings\vrfLauncher\"HLA 1516 Evolved.xml" carries `<hostAddress>127.0.0.1` (DISK) - the ONLY address field in an HLA
  profile, hence what UG52 5.3.1 p188 means by "You must launch a predefined connection from the vrfLauncher at least once so that VR-Forces can save
  the NETWORK ADDRESS INFORMATION it requires to launch". UG52 4.1.1 p130 adds, for --gui/--sim: "After the first time, the GUI or sim engine starts
  using the most recent connection information."
- CLI, both executables: `--deviceAddress address` "Specifies the address of the network card to use for UDP traffic" and `(--hostAddressString | -H)
  address` "Specifies the host address. This is usually the same as the device address" (vrfGui Table 10 p177, vrfSim Table 11 p180; p181:
  --hostAddressString is "Called Device Address in the Launcher"; 4.7.4 Table 6 p150 maps the panel's "GUI Network Interface Address" to
  --deviceAddress, "The IP address of the host computer"). NO exConnConfig key exists for it - MAK-ONE-2025-Config.xml has no address element on the
  HLA side (DISK; IOG 1.8.1 p20 lists the HLA keys).
- RTI layer, separate: RTI_networkInterfaceAddr, default "First device found" (RTIUG501 Table 4 p72, "Local network interface on which RTI traffic will
  be SENT AND RECEIVED"), set from the assistant connection when one is chosen (RTIUG501 7.3 p73), otherwise from the rid.
We supply none of the three. The 5.0.2 golden RTI connection pinned udpInterface 127.0.0.1 with loopback BROADCAST 127.255.255.255 (transport report
sec B); the 5.0.1 assistant's stored connection pins udpInterface 10.5.0.2, a real adapter; our rid copies use 0.0.0.0; and VR-Link independently falls
back to "the broadcast address of the first device listed in the interface table" (IOG 5.2.1 p81). The RTI's interface address governs the
UDP/best-effort path while reliable traffic rides TCP to the forwarder/rtiexec - the same polarity we observe, since MAK_TimeAndDate is HLAreliable and
arrives while BaseEntity/PhysicalEntity and every VrfExtendedAttributes attribute are HLAbestEffort and never do (transport report E3). Two federates
whose UDP legs sit on different interfaces exchange no best-effort traffic yet still agree on the federation. Hypothesis, not finding - sec 6 M2, sec 7.

## 3. P7 singled out: the 3-module list has no 5.2d equivalent

RN VRF-8940 p71: 5.2 adds appData\settings\vrfSim\requiredFomClasses.mtl, read "after joining the federation"; "If the required classes are not present
in the FOM, it exits with an error". The shipped file (DISK) requires BaseEntity.PhysicalEntity, BaseEntity.AggregateEntity,
EnvironmentProcess.VrfEnvironmentProcess, VrfExtendedAttributes, DIGuyObject, METOC_Root.EnvironmentCondition, MAK_TimeAndDate + RadioSignal,
WeaponFire, MunitionDetonation, Data. VRFExt-6 + DIGuy-7 + LgrControl-2 carry neither the METOC nor the aggregate tree, so the 5.0.2 list cannot be
carried forward (INFERRED - the check is documented, the per-module class inventory is not verified). Our appsettings FomModules [VRFExt-6, DIGuy-7,
LgrControl-2] and Federation CWIX-2024 are both stale against MAK-ONE-2025-Config.xml (already rows A2/A9).

## 4. Headline: a documentation CHANGE between the two releases

UG502 5.5.1 p186: "you must run the rtiexec. In other words, you cannot use the MAK RTI in lightweight mode IF YOU ARE RUNNING MULTIPLE FEDERATIONS."
UG52 5.5.1 p190: "- If you are using the MAK RTI and are running multiple, concurrent federation executions, you must run the rtiexec.  - YOU CANNOT
USE THE MAK RTI IN LIGHTWEIGHT MODE WITH VR-FORCES."
The qualifier was dropped in 5.2. Every 5.2d run we have made is lightweight: the assistant-free rid (RTI_useRtiExec 0) and, in assistant mode, the
single saved 5.0.1 entry, a <lightweight> element (sec 5c). MAK documents that as unsupported for VR-Forces. It never bit on 5.0.2 because of P15 - the
golden path ran on the Pitch RTI, where the MAK lightweight/rtiexec distinction does not exist. Tension to record: IOG 1.3 p11 and 5.2.2 FAQ p83 keep
the generic MAK-ONE line ("you can run in lightweight mode without an rtiexec ... rtiexec [only] for DDM, Time Management, MOM, sync points,
save/restore, reliable transport"); the VR-Forces-specific sentence is narrower and later and governs vrfSim/vrfGui. FOM modules reinforce it: RM 14.3
p14-5 "The use of FOM modules also requires the use of the rtiexec and that internal messages be sent reliably", with a lightweight carve-out (RM
14.2.1 p14-4) requiring every federate to name the modules in its own rid - ours do not. The MAK RTI installs a predefined rtiexec connection on
4001/229.7.7.7 in full compliance mode and a predefined lightweight one on 4000/229.7.7.7 (RTIUG501 7.3 p72, 7.3.2 p75).

## 5. The DOCUMENTED first-run procedure, and whether it can be done HEADLESSLY

Procedure (UG52 4.1.1 p129-131 combined / 4.1.2 p132-133 independent): (1) start `vrfLauncher`, or the Tools folder's "Configure VR-Forces Connections"
(4.7.1 p148) - "If this is the first time that you are running VR-Forces, or if you have not specified a auto-start connection, the Simulation
Connections Configuration dialog box opens"; (2) select/Add/Copy/Edit a connection (4.7.2-4.7.3 p148-150) - on the HLA page, Federation Name, FED File
Name, RPR+NETN Version/Revision and FOM Modules sit under the heading "Configuration File" and go to the exConnConfig file, NOT the profile, while
Network Interface Address, Session ID and Use Absolute Time Stamps are the profile/CLI half (4.7.4 Table 6 p150-151); (3) expand advanced options for
the per-row Number / Site ID / Application ID (4.8.2 p155-156) - View Arguments prints the exact command lines, which p131 says you may "copy ... and
use them in a batch file"; (4) Launch Selected, or the per-row Launch button, which leaves the dialog open; (5) optional Auto Start star (4.7.6 p153);
(6) RTI side, INST 4.2.2 p41: "The RTI Assistant prompts you to choose an RTI configuration ... If necessary start the rtiexec. Click Connect" - that
choice lands in %APPDATA%\MAK\RTI\5.0\Legatus\.
a) exConnConfig - HEADLESS, user scope: write our own and pass --exConnConfigFile <path> (IOG 1.8.2 p20-21; UG52 Table 11 p181, Table 12 p167). Ours
   "takes precedence ... replacing default values", "The MAK-ONE-YYYY-Config.xml file is always loaded to ensure that no parameters are missing", but
   "It is critical to include all necessary parameters". No C:\MAK write.
b) Launcher profile - NOT NEEDED for independent launches: every P1-P14 field has a CLI equivalent above, --hostAddressString/--deviceAddress included.
   If we want one anyway, `--appDataDir <dir>` (UG52 Table 12 p187; RN VRF-9241 p34) and `--useUserSettingsDirectory` ("use the user login settings
   directory instead of the shared application settings", Table 12 p188) keep the write out of C:\MAK; the profile is plain boost_serialization
   key/value XML and is hand-writable.
c) RTI connection - PROBABLY, unverified. %APPDATA%\MAK\RTI\5.0\Legatus\connections.xml (2026-09-03 21:37, 402 B) is schema-identical to the 4.6 file:
   root <connections version="5.0.1" uuid="93cb048a-...">, one <lightweight name="229.7.7.7 [10.5.0.2], 4000" udpAddress="229.7.7.7" udpPort="4000"
   udpInterface="10.5.0.2" chosen="1" ownerHost="Legatus"/>, then <configurations name="Non fully compliant" fullyCompliant="0" ridConfigName=""
   ridIsPreconfigured="1" chosen="1"/>. The 4.6 sibling differs only in version="4.6.1" and in carrying an <rtiexec> element instead. Adapted to 5.0.1
   (INFERRED - schema undocumented; keep the uuid and the lightweight element, add this, move chosen="1" to it):
     <rtiexec name="Legatus (127.0.0.1), 4001, 127.255.255.255 [127.0.0.1], 4001, Forwarder: 5000" givenName="" host="Legatus"
      tcpAddress="127.0.0.1" tcpPort="4001" udpAddress="127.255.255.255" udpPort="4001" udpInterface="127.0.0.1"
      additionalForwarderConnectionToMake="0.0.0.0" forwarderPort="5000" chosen="1" ownerHost="Legatus"/>
   Every attribute maps to a documented 5.0.1 parameter (RTIUG501 Table 4 p72; uniqueness Table 5 p73). Caveats: 7.3.3 p77 connections "can only be
   edited on the machine on which they were originally defined"; 7.3.5 p78 "You cannot remove the predefined connections" - the predefined pair is
   built in and absent from this file, so the 4.6 entry is USER-CREATED. Writing it while an assistant runs may be overwritten; sequencing untested.
   This is a %APPDATA% write, not C:\MAK.
d) Why the assistant route is worth trying - RTIUG501 7.3 p73: an assistant connection sets RTI_forceFullCompliance, RTI_useRtiExec, RTI_udpPort,
   RTI_destAddrString, RTI_distributedForwarderPort, RTI_networkInterfaceAddr, RTI_tcpPort and RTI_tcpForwarderAddr; "Send Internal RTI Messages
   Reliable (TCP) is enabled"; "FOM Data Transport Type is set to 'Specified by FOM'"; "RTI_mcastDiscoveryEnabled is always forced to 0" - exactly the
   posture the lightweight rid lacks. The pure-rid alternative is already written out (transport report sec D Q4) and crashed the 5.2 sim on 5.0.1, so
   (c) is the untried branch.

## 6. What independent mode was MISSING, ranked by likely effect on "observers reflect nothing"

M1 (P15 + sec 4) The RTI itself. The golden path was Pitch prti1516e; on the MAK RTI, UG52 5.5.1 p190 says the lightweight configuration we ran is not
   a supported VR-Forces configuration at all, and RM 14.3 says FOM modules require the rtiexec. Subsumes standing H2 and supplies a mechanism for H1
   (publisher silence). Top - the only item with an explicit vendor prohibition against what we did.
M2 (P3, sec 2) The interface address, at all three layers. It governs the UDP/best-effort path, which is precisely the traffic that never arrives.
   Cheap to test and never tested.
M3 (P7, sec 3) The FOM module set and the requiredFomClasses check - never observed passing or failing; module ORDER is documented as mattering and
   MAK's own sorting is called "inconsistent" (IOG 5.3 p84).
M4 (P4) Federation identity drift: c2simVRF arg 18 and appsettings still say CWIX-*, the sim joins MAK-ONE-2025. Not the cause of a 0-reflection run in
   which our own federate joined MAK-ONE-2025 (transport report E5), but it will bite the bridge.
M5 Nothing else: sessionId, autoconnect, selectatstart and Local Settings Designator are ergonomics or empty; site/app numbers we already ledger.
Cheapest discriminator between M1 and M2: one run with sim and observer both pinned to 127.0.0.1 (--deviceAddress + --hostAddressString on both, RTI
interface likewise), capturing the sim's own join/registration trace - it separates "never published" from "published somewhere we are not listening".

## 7. VERIFIED vs INFERRED; what only a live run or MAK can settle

VERIFIED: every 5.0.2 value in sec 1 (read off the panel screenshot, the two READMEs and the three .bat files); every 5.2d carrier and citation in sec
1-5; the 5.0.2 -> 5.2 wording change on lightweight mode; the identity move from Launcher profile to exConnConfig file; the contents and timestamps of
all four appData profiles, the two Legacy and the Advanced/METOC factory profiles, MAK-ONE-2025-Config.xml, requiredFomClasses.mtl and the 5.0.1
connections.xml; that our launch script supplies no interface address, no connection name and no RTI other than the MAK one.
INFERRED (flagged, not relied on): that the 3-module list would fail the requiredFomClasses check (the check is documented, the per-module class
inventory is not); that a hand-written <rtiexec> element is accepted by the 5.0.1 assistant (only the 4.6 precedent supports it); that `<hostAddress>`
is the WHOLE of the "network address information" (it is the profile's only address field, but p130's "most recent connection information" may cover
more); that M1 rather than M2 is the operative cause - both fit every observation we hold, which is why sec 6 ends in a discriminator, not a verdict.
ONLY A LIVE RUN CAN ANSWER: whether a 5.2 sim on a 5.0.1 rtiexec connection joins at all (the rid-configured attempt crashed in
DtVrfSimOptions::parseCmdLine, PREREG_52_REFLECTION sec 5) and whether an observer then reflects > 0; whether pinning all three interface layers to
127.0.0.1 changes anything; whether the assistant rewrites or rejects a hand-edited connections.xml; whether requiredFomClasses.mtl passes - its
post-join check leaves a log line we never captured. ONLY MAK COULD ANSWER (ruled out by the user): the connections.xml schema, and how UG52 5.5.1's
blanket prohibition reconciles with IOG 1.3 p11. No code changes are proposed.
