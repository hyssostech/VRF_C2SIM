# RESEARCH - how a 5.2 observer discovers sim-engine entities (reflected=0)

Date 2026-09-03. Read-only docs+headers study for the OPEN item in
PREREG_52_TOOLJOIN_2026-09-03.md sec 6. No launches, builds or edits.
Read: VRFUsersGuide, MAKInteroperabilityGuide, VRFMigrationGuide (nothing on remote
control or reflection), VRF5.2ReleaseNotes (C:\MAK\vrforces5.2d\doc); MAK RTI 4.6.1
RTIReferenceManual; the 5.2d headers; examples\remoteControl; runs/launch52
artifacts. docs.mak.com classref: 404 on the guessed URL, not used.

## A. VERIFIED (cited)

V1. Sessions gate CONTROL, not reflection. UG 3.2.3 p.115: "Sim engines are always
    part of a session. GUIs can join a session or operate independently... When a GUI
    is not joined to a session, it cannot control sim engines. However, it can open a
    terrain and view exercises running on its port. In this case, it is simply a
    viewer." UG 4.3 p.139 repeats it; default session id is 1 (UG 4.1.3 p.133). The
    id rides IN the VRF object-data message: vrfExtProtocol/ifVrfObjectData.h:139-142
    "the id that is used to say whether or not front ends that are running in a
    particular session can control these objects".
V2. The 5.2 sample's init is exactly ours. examples\remoteControl\main.cxx:47
    `init(exConn, nullptr, nullptr, nullptr, nullptr, "entity-identifier", false)`,
    then :49 sets the session id on the message interface only - identical to
    VrfFacade.cpp Start() after the 3812 change. That hypothesis is closed.
V3. The sample never enumerates simulation objects.
    commandLineRemoteController.cxx:440-445: `list` prints "Listing known sim engines"
    and walks `controller()->backends()`. remoteControl is control-only.
V4. Subscription is FOM-mapper-derived and VRF properties are NOT decoded unless
    prototypes are set. vrfExtObjects/reflectedExtEntityList.h:32-39 "obtains the set
    of object classes to subscribe to from the exConn's fomMapper... properties will
    not be decoded until setPropertyPrototypes is called"; :74-80 and :236-240
    `myWaitForVrfExtendedData` defaults TRUE, with timeout theTimeoutRequestTime.
V5. Passing no lists means the UUID manager owns them (UUIDNetworkManager.h:81-83).
    The three callbacks we hook (:132-139) are UUID-CHANGE callbacks, not list-
    addition callbacks; uuidFor() returns null for unresolved objects (:116-118).
V6. Unused in-process diagnostics: vrlinkNetworkInterface.h:464-466
    remoteObjectManager(), :599 printReflectedObjectCounts(); remoteObjectManager.h
    :109-110 hasDiscoveredObjects(); UUIDNetworkManager.h:123-125 entityList() etc.
V7. NETN/MAK classes are SUBCLASSES of the RPR PhysicalEntity tree, not a parallel
    tree. Interop Guide 2.4.7 p.45-46: "the NETN FOM adds several subclasses to extend
    the RPR FOM PhysicalEntity object classes... MAK further subclasses some of the
    NETN and RPR FOM classes" (MAK_GroundVehicle, MAK-Physical-2_evolved.xml).
    Supported list 2.3 p.28; the 17-module set 2.1 p.24-25 = MAK-ONE-2025-Config.xml.
V8. DDM is off at the RTI for everyone, despite the rid asking for it.
    config/rid-461-ridconfigured.mtl:173 `(setqb RTI_dataDistMgmt 1)` sits OUTSIDE the
    connection block (39-111), so RTI_configureConnectionWithRid does not override it
    - yet runs/launch52/vrfSim_3805...log:202 prints "DDM is disabled in the RTI." and
    every joiner prints "Call to disabled RTI service: getDimensionHandle" after
    loading THAT rid (listen_3813.txt, watchvrf_3812.txt). RTIReferenceManual A-1
    gives RTI_dataDistMgmt "Default: 0", its settings table 4-19 "Default: Enabled" -
    self-inconsistent. 6.3.1: DDM works in lightweight mode but "functionality is
    limited".
V9. THE VENDOR CONTROL IS NOT A CONTROL. --exConnConfigFile is a VR-Forces/VR-Vantage
    option (vrfGuiCore/vrfGuiCommandLineParser.inl:446,
    vrvCore/DtStealthCommandLineProcessor.inl:439). It appears nowhere in
    C:\MAK\vrlink5.10\include, and netdumpHLA1516e_64 rejected it outright
    (netdump_3814.txt "PARSE ERROR: Argument: --exConnConfigFile"; that run produced
    no wire capture at all). listen_3813.txt shows no parse error and no federation
    name - only the rid path, the DDM warning, and "[0,0] Initializing / Executing".
V10. A wrong-federation join is SILENT here. RTIReferenceManual 6.3.1:
    "joinFederationExecution() ... returns successfully even if the named federation
    execution has never been created"; the federation identifier is "the first three
    characters plus the last two characters of the federation execution name".
V11. The sim joined and built the entity: vrfSim_3805...log:191 "Joined federation
    MAK-ONE-2025 ... VR-Forces Sim Engine 5.2d"; tail: "DtReactToCollisionEvent-
    Actuator: Constructor called for entity M2 1".
V12. 5.2 changed publisher behaviour on missing FOM classes. Release Notes VRF-8063:
    "Publisher should handle missing FOM classes more gracefully. Added checks in the
    vrfVrlinkExt publishers and reflected lists before trying to access any VR-Link
    hlaObjects. The application also now prints a warning." (VRF-9054 added the
    MAK-ONE-YYYY-Config.xml FOM selection we depend on.)

## B. The five specific questions

(i)  NO. Session membership is control authority (V1); an unjoined GUI still views the
     exercise. The session id travels WITH the reflected object, so it cannot gate
     receiving it. Ours is 1, same as the sample (V2). Residual:
     DtVrlinkNetworkInterface::setVrfSessionId (vrlinkNetworkInterface.h:304-309) is a
     separate setter we never call; propagation from base init is unproven.
(ii) NO. NETN/MAK classes subclass PhysicalEntity (V7), HLA promotes discovery to the
     subscribed superclass, and both sides load the same 17 modules from one config.
     This only bites an observer that joined WITHOUT that config - i.e. listen (V9).
(iii) Unproven. No doc describes an interest-gated publish; Interop 2.2 lists Entity
     State / PhysicalEntity subclasses as "Send / Receive". The one documented
     silent-stop path is VRF-8063 (V12), and it prints a warning.
(iv) DDM-disabled is real (V8) and universal, but not obviously sufficient: two
     rtiSimple1516e peers reflected objects over this same rid (PREREG sec 6). It
     stays on the list only because VR-Link's ext-list constructors are DDM-region
     aware (reflectedExtEntityList.h:47-60) and a throwing getDimensionHandle inside
     subscribe would be invisible to us.
(v)  Nothing - `list` shows sim engines, not objects (V3).

## C. RANKED hypotheses

H1 (highest). The corroboration is fake: listen never joined MAK-ONE-2025, so "both
   observers saw nothing" is really "one observer saw nothing".
   Cite V9, V10. Falsifier: evidence listen was in MAK-ONE-2025 with the 17 modules (a
   federation/module line from the tool, or its join in the sim log at raised notify).
   Cheapest test: re-run listen with flags VR-Link actually has (exec name,
   --fomModules / --setFomModuleList - usage block in netdump_3814.txt), requiring it
   to print the federation joined. Lever: verify the instrument first.

H2. We count the wrong thing. BeginTrackingReflectedObjects (VrfFacade.cpp ~:767)
   hooks UUID-CHANGE callbacks, which fire only when the manager resolves a UUID for
   an already-discovered object. If entities are in the list but the VRF extended
   attributes never decode - no property prototypes were ever set (V4) - reflected
   stays 0 while discovery is fine. Cite V4, V5. Falsifier:
   printReflectedObjectCounts()/hasDiscoveredObjects() also report zero. Cheapest
   test: call them and walk uuidNetworkManager()->entityList()->first() in the same
   tick loop (V6) - no protocol change needed. Lever: list-addition callbacks and
   setPropertyPrototypes, not UUID callbacks.

H3. The ext list withholds entities awaiting VRF extended data
   (reflectedExtEntityList.h:74-80, default true, timeout theTimeoutRequestTime).
   Cite V4. Falsifier: a non-zero reflected count with waiting still enabled.
   Cheapest test: the H2 probe, read after >30 s (past any timeout); still empty
   kills H3 with H2. Lever: setWaitForVrfExtendedData(false).

H4. The sim silently stopped publishing because a FOM class is missing in the merged
   FOM. Cite V12 - the 5.2 change turns a crash into a warning plus no publication.
   Falsifier: no such warning in the sim log AND the vrfGui showing the entity.
   Cheapest test: grep the sim log at raised notify for the VRF-8063 warning, and look
   at the already-joined vrfGui - the live falsifier PREREG sec 6 already nominated.
   Lever: the FOM module list in MAK-ONE-2025-Config.xml.

H5. Disabled DDM breaks VR-Link's subscribe path for the ext lists. Cite V8 plus
   reflectedExtEntityList.h:47-60. Falsifier: an observer with DDM forced on still
   reports zero, or a non-DDM subscriber discovering VR-Forces objects. Cheapest test:
   rtiSimple1516e as a pure subscriber against the live sim; if it discovers the sim's
   entities, H5 and H4 both die and the defect is entirely ours (H2/H3). Lever:
   RTI_dataDistMgmt / rid choice.

H6 (lowest). Late-joiner discovery is not replayed on this assistant-free lightweight
   connection (V10 - no central object store). Falsifier: an observer started BEFORE
   entity creation still reports zero. Cheapest test: reverse the order - observer
   first, then CreateOne. Lever: setReflectedListRequestUpdates(true)
   (vrlinkNetworkInterface.h:219) / requestUpdatesForRemoteNonVrfObjects()
   (remoteObjectManager.h:132).

## D. INFERRED / not verified
- That listen ignored rather than rejected --exConnConfigFile; only its output file is
  evidence, and it names no federation.
- Why RTI_dataDistMgmt 1 is not honoured - unexplained symptom, not a footnote.
- Whether base init propagates a session id to DtVrlinkNetworkInterface.
- No wire-level ground truth exists: netdump never ran (V9).

## E. Ordering (cheapest first; all in-process work before any protocol change)
1. H2/H3 probe: printReflectedObjectCounts + walk entityList in the tick loop.
2. H4 falsifier: does the already-joined vrfGui show the entity?
3. H1: re-run listen with real VR-Link flags, requiring a printed federation name.
4. H5: rtiSimple1516e as subscriber against the live sim.
5. H6: observer-before-create ordering.
