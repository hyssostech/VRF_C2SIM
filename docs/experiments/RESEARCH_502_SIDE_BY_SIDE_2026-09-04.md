# RESEARCH - keeping the 5.0.2 golden path runnable beside the running 5.0.1 RTI (2026-09-04)

RESEARCH ONLY: nothing launched, nothing changed, nothing under C:\MAK touched. Docs read first.

## 0. LIVE STATE (read-only, 2026-09-04 ~07:4x local) - CORRECTS THE BRIEF
- Machine MAK_RTIDIR=C:\MAK\makRti5.0.1, RTI_RID_FILE=C:\MAK\makRti5.0.1\rid.mtl, PATH[0]=
  C:\MAK\makRti5.0.1\bin (VERIFIED, [Environment]::GetEnvironmentVariable).
- **NO rtiAssistant process exists; NOTHING listens on 6003.** The elevated 5.0.1 assistant of
  2026-09-03 is GONE, so the version gate is LATENT, not active - it re-arms the instant any
  federate spawns an assistant, and under the Machine env that assistant is 5.0.1.
- rtiexec 15720 + rtiForwarder 43728 (ours, never kill) LISTEN on TCP 127.0.0.1:4001 and
  192.168.234.1:5000; 4101/5100 free. vrfSimHLA1516e 64364 holds UDP 4001 - a LIVE 5.2 federate.
- DLL HAZARD MECHANISM, VERIFIED: makRti4.6.1\bin and makRti5.0.1\bin export IDENTICAL basenames
  (librti1516e64.dll, libRTI-NG_64.dll, exec_64.dll, forwarder_64.dll, assistant_64.dll ...) and MAK
  binds by name on PATH, so PATH ORDER ALONE picks the RTI: with Machine PATH[0]=makRti5.0.1\bin an
  unprefixed 5.0.2 launch loads the 5.0.1 LRC. THE blocker - and INDEPENDENT of the assistant.

## 1. WHAT 5.0.2 REQUIRES OF THE MAK RTI (VERIFIED, verbatim)
- UG502 5.5.1 p186: "If you are using the MAK RTI and are running multiple, concurrent federation
  executions, you must run the rtiexec. In other words, you cannot use the MAK RTI in lightweight
  mode if you are running multiple federations." So 5.0.2 does NOT ban lightweight outright - UG52
  5.5.1 p190 DROPPED that qualifier, 5.0.2 kept it.
- BUT RTI 4.6.1 RM 14.3 p14-5: "If you use FOM modules, the RTI must distribute the FED file
  (RTI_distributeFedFile) ... The use of FOM modules also requires the use of the rtiexec and that
  internal messages be sent reliably (RTI_useRtiExec and RTI_internalMsgReliableWhenUsingRtiexec)."
  The golden profile declares three FOM modules (MAK-VRFExt-6_evolved.xml; MAK-DIGuy-7_evolved.xml;
  MAK-LgrControl-2_evolved.xml) => rtiexec REQUIRED on 5.0.2, by a different clause than 5.2's.
  ADVERSARIAL: RM 14.2.1 p14-4 says FOM modules ARE supported in lightweight mode if every federate
  declares them (RTI-addCreateFomModule/RTI-addJoinFomModule) in its rid; 14.2.1 and 14.3 are in
  tension. 14.3 wins anyway because the golden path was NEVER lightweight - so rtiexec mode is a
  REPRODUCTION requirement here, not only a doc requirement.
- Golden connection, %APPDATA%\MAK\RTI\4.6\Legatus\connections.xml, re-read today, unchanged:
  <rtiexec tcpAddress="127.0.0.1" tcpPort="4001" udpAddress="127.255.255.255" udpPort="4001"
  udpInterface="127.0.0.1" forwarderPort="5000" chosen="1"/> + <configurations name="Non fully
  compliant" fullyCompliant="0" ridConfigName="" ridIsPreconfigured="1" chosen="1"/>
- RTI 4.6.1 UG Table 4-1 p4-9/4-10 = the same rtiexec CLI as 5.0.1 (-M -R -P -T -A -N -i -D -r -l -n
  -K). -M|--manual is MANDATORY ("-P/-T/-A/-r ... ignored unless --manual is used"; "equivalent to
  setting RTI_configureConnectionWithRid to 1"); -R overrides RTI_RID_FILE.
- MULTIPLE rtiexecs ON ONE HOST: the constraint is PER FEDERATION - UG 1.2 p1-4 / 4.2 p4-8 "Do not
  run more than one instance of the rtiexec per federation." Two rtiexecs on two federations are NOT
  prohibited; separation is by PORT (UG 7.4 Table 7-4: udp/tcp 4000, fwd 5000, mcast 6001, asst 6003).
- Assistant-free: RM 4.6.1 5.2.10 p5-11 "To disable the RTI Assistant, create an environment
  variable called RTI_ASSISTANT_DISABLE. It does not require a value."

## 2. THE PLAN
2a. NEW `config/rid-461-rtiexec-min.mtl` = a copy of C:\MAK\makRti4.6.1\rid.mtl with SEVEN setqb
    lines changed. **CONFIRMED: every key exists in the stock 4.6.1 rid** (stock line numbers):
      L30 configureConnectionWithRid 0->1 ; L73 useRtiExec 0->1 ; L74 udpPort 4000->4101
      L75 tcpPort 4000->4101 ; L80 distributedForwarderPort 5000->5100 (the three NON-COLLIDING)
      L76 destAddrString "229.7.7.7"->"127.255.255.255" ; L99 networkInterfaceAddr "0.0.0.0"->
      "127.0.0.1".  (rid-501-rtiexec-min needed only SIX: 5000 was already right in the 5.0.1 stock.)
    ALREADY STOCK-CORRECT in 4.6.1 and matching the golden connection - do NOT touch:
    tcpForwarderAddr "127.0.0.1" L77, tcpNetworkInterfaceAddr "0.0.0.0" L102, forceFullCompliance 0
    L66 (== golden fullyCompliant="0"), internalMsgReliableWhenUsingRtiexec 1 L87,
    fomDataTransportTypeControl 0 L93, mcastDiscoveryEnabled 0 L96, distributeFedFile 1 L288,
    fomModuleMerging 1 L296, force/disableUnlicensedForTwo 0 L16/17. Keeping tcpNetworkInterfaceAddr
    at stock is deliberate - the SUPERSET 5.0.1 rid that set it crashed the sim in parseCmdLine.
2b. NEW `scripts/StartRtiExec.ps1` (4.6.1 twin of StartRtiExec52; ENSURE-UP, never kills), cwd
    C:\MAK\makRti4.6.1\bin:  rtiexec.exe -M -R <repo>\config\rid-461-rtiexec-min.mtl -P 4101 -T 4101
      -A 127.255.255.255 -N 127.0.0.1 -i 127.0.0.1 -D 5100 -r -l <repo>\runs\launch502\rtiexec.log -n 3
    No -K (it must outlive the run). Readiness = TCP 4101 LISTEN, not process presence; its own
    rtiForwarder is discovered (UG 4.2.1 p4-11). StartRtiExec52 REFUSES 4.6.1: separate script.
2c. LaunchVrf.ps1 CHANGES (mirror LaunchVrf52's env block; today it only READS Machine MAK_RTIDIR at
    :213-219 and warns, and sets NO per-process override - :408+ touches only the licence var):
      $env:PATH = 'C:\MAK\makRti4.6.1\bin;C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;'+PATH
      MAK_RTIDIR=C:\MAK\makRti4.6.1 ; MAK_VRFDIR=vrforces5.0.2 ; MAK_VRLDIR=vrlink5.8
      RTI_RID_FILE=<repo>\config\rid-461-rtiexec-min.mtl ; RTI_ASSISTANT_DISABLE=1 (-UseRtiAssistant
      reverts, as on 5.2). INVARIANT: no other MAK RTI bin may precede makRti4.6.1\bin on that PATH.
    The 6b assistant precondition (:294-344) INVERTS - "no pre-existing rtiAssistant" becomes the
    EXPECTED state, not a warning. EVERY 5.0.2 federate needs the SAME env and the SAME rid
    (vrfLauncher, RtiProbe, WatchVrf, CreateOne, RunSim, ResetVrf, the app) or they share no conn.
2d. DOES --usePredefinedConnection STILL WORK ASSISTANT-FREE? INFERRED YES. Evidence: the profile
    "HLA 1516 Evolved RPR 2.0 with MAK extensions.xml" holds ONLY VR-Forces-level keys (appNumber
    3001/FE 3101, fedFileName, federationName CWIX-2024, fomModules, hostAddress 127.0.0.1, siteId,
    sessionId) and ZERO RTI connection keys - no tcpPort, udpPort, destAddr, forwarder port. The RTI
    connection is a DIFFERENT LAYER, from the assistant or (here) the rid, so 5.0.2 need NOT go
    independent-mode. If it must: UG502 4.1.2 p133 `vrfGui -s 1 -a 3000 --hla1516e` +
    `vrfSimHLA1516e -s 1 -a 3001`, -i for session (4.1.3 p134), and -x / -F / --rprFomVersion /
    --fomModules (5.5.1-5.5.3 p186). Table 9 p182: --usePredefinedConnection is 5.0.2-only.

## 3. GOLDEN-TRACE IMPACT - what the seat must declare
| Parameter | Golden (assistant) | Under the plan | Trace-relevant |
| connection TYPE | rtiexec | rtiexec | YES - UNCHANGED |
| source of connection values | assistant store | repo rid (WithRid 1) | mechanism only, values equal |
| TCP / UDP rendezvous port | 4001 / 4001 | 4101 / 4101 | no |
| distributedForwarderPort | 5000 | 5100 | no |
| internal msgs reliable | ASSUMED 1 | 1 (stock) | YES - see below |
| RTI Assistant | present, answered once/boot | disabled | no, EXCEPT licensing (sec 4) |
UNCHANGED, therefore not variables: destAddr 127.255.255.255, interface 127.0.0.1, fullyCompliant 0,
3 FOM modules + distributeFedFile 1, notifyLevel 2, rtiexec owner (assistant vs our headless -M).
ONE GENUINELY OPEN VARIABLE: internalMsgReliableWhenUsingRtiexec under the golden path came from the
assistant's PRECONFIGURED "Non fully compliant" RTI-settings entry (ridConfigName="",
ridIsPreconfigured="1") held in the assistant's store, not in any file this research could read. The
rid value 1 is the stock default and matches -r, so a difference is unlikely - but it is ASSUMED.
FOR THE SEAT: the only trace-relevant class - connection TYPE - does NOT change; ports are rendezvous
plumbing. Declare TWO variables (port move, connection-source move); falsifier = R9 below 3/3.

## 4. LICENSING - A HARD GATE
- RM 4.6.1 App. A p A-19: RTI_rtiExecPerformsLicensing "Enables (1) or disables (0) license checkout
  from the rtiexec. The rtiexec will check out a license for each joining federate. Default 0." STOCK
  4.6.1 rid L550 = 0 and the 5.0.1 min rid L572 = 0 - NEITHER stack delegates licensing; each federate
  checks out its own via MAKLMGRD_LICENSE_FILE (UG 8.1 p8-2 warns of the unlicensed trap otherwise).
- UG 8.2 p8-3, rtiexec case: a federation "will not allow a third unlicensed federate to join";
  "Licensed and unlicensed federates cannot interoperate" - a licensed joiner blocks an unlicensed
  one even as the second federate, and vice versa. In LIGHTWEIGHT mode that failure is SILENT
  ("unlicensed federates behave as if licensed federates do not exist") - rtiexec mode fails louder.
- RTI_ASSISTANT_DISABLE removes the License Not Found dialog, so a checkout failure is INVISIBLE,
  and a 5.0.2 run uses MORE than two federates (vrfSim + vrfGui + app + RtiProbe/WatchVrf). VERIFY
  DtHaveRtiLicense()==1 on EVERY federate (WatchVrf --diag) plus the joined count in the rtiexec -l
  log, as a pre-flight. Demo licence lapses 2026-09-15.

## 5. "A REBOOT RESTORES 4.6.1 FIRST" - FALSE. Do not plan on it.
The 5.0.1 values are at **Machine** scope (sec 0), written to the registry by the RTI 5.0.1 installer;
a reboot re-reads exactly those, RE-ESTABLISHING the 5.0.1-first environment rather than undoing it.
Also: the assistant that spawns is whichever the FIRST LRC finds on PATH (5.0.1), so an unprefixed
5.0.2 launch after a reboot version-rejects its own 4.6.1 LRC (2026-09-03 19:45:48). 2c is the fix.

## 6. VERIFIED / INFERRED / LIVE-ONLY
VERIFIED (doc quoted, or file/registry read today): all of sec 0; every citation inline above; every
rid key and line number in 2a; connections.xml; the launcher profile XML.
INFERRED: --usePredefinedConnection is orthogonal to the assistant (2d - strong, from the profile's
contents); 4101/5100 do not collide (free today - a snapshot, not a guarantee); the golden path's
internal-msgs-reliable was 1 (sec 3); rtiexec is required for 5.0.2 by FOM modules (14.3 > 14.2.1).
LIVE RUN ONLY: (a) a 4.6.1 rtiexec on 4101/5100 coexisting with the 5.0.1 one on 4001/5000 without
either forwarder mis-binding; (b) that the 5.0.2 stack really binds the 4.6.1 LRC under the PATH
prefix (NativeStackInfo + the rtiexec log, never process presence); (c) that vrfLauncher
--usePredefinedConnection completes assistant-free with no dialog; (d) that all federates are LICENSED;
(e) THE REGRESSION CONTROL - R9 reproducing 3/3 TASKCMPLT on the re-postured 5.0.2.
ORDER: rid (2a) -> StartRtiExec (2b) -> LaunchVrf env (2c) -> licence pre-flight -> R9 control. Do
not run any of it against pid 64364's live 5.2 federation without a fresh ruling.
