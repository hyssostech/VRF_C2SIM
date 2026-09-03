/*----------------------------------------------------------------*
|  VrfFacade.cpp                                                   |
|                                                                  |
|  Implementation of the VR-Forces boundary. ALL MAK Dt* types are |
|  confined to this translation unit, behind VrfFacade::Impl.      |
|                                                                  |
|  The construction sequence, the MyDtVrlinkVrfRemoteController     |
|  subclass, the three registered callbacks, the coordinate        |
|  conversions and the scripted-task builders are lifted verbatim  |
|  (behavior-for-behavior) from main.cxx / textIf.cxx /            |
|  C2SIMinterface.cpp so the rebuilt interface produces the same   |
|  golden trace.                                                   |
*-----------------------------------------------------------------*/

#include "VrfFacade.h"

// VR-Forces / VR-Link headers - none of this leaks through VrfFacade.h
#include <vrlinkNetworkInterface/vrlinkVrfRemoteController.h>
#include <vrlinkNetworkInterface/vrlinkNetworkInterface.h>
#include <vrlinkNetworkInterface/UUIDNetworkManager.h>
#include <vrfMsgTransport/vrfMessageInterface.h>
#include <vrfMsgTransport/communicationManager.h>
#include <vrfMsgTransport/radioMessageListener.h>
#include <vrfmsgs/vrfObjectMessageExecutive.h>
#include <vrfmsgs/reportMessage.h>
#include <vrftasks/taskCompleteReport.h>
#include <vrftasks/textReport.h>
#include <vl/exerciseConn.h>
#include <vl/reflectedEntity.h>
#include <vl/reflectedAggregate.h>
#include <vl/aggregateStateRepository.h>
#include <vlutil/vlProcessControl.h>
#include <vrftasks/scriptedTaskTask.h>
#include <vrftasks/scriptedTaskSet.h>
#include <vrftasks/fireAtTargetTask.h>
#include <vrftasks/moveIntoFormationTask.h>
#include <vrftasks/breachTask.h>
#include <vrftasks/patrolRouteTask.h>
#include <vrftasks/followEntityTask.h>
#include <vrftasks/requestAvailableFormationsAdmin.h>
#include <vrftasks/availableFormationsAdmin.h>
#include <vrftasks/planAndMoveToTask.h>
#include <vrfExtObjects/reflectedExtEntityList.h>
#include <vrfExtObjects/reflectedExtEntity.h>
#include <vrfExtObjects/reflectedExtAggregateList.h>
#include <vrfExtObjects/reflectedExtAggregate.h>
#include <vl/globalObjectDesignatorList.h>
#include <vl/globalObjectDesignator.h>
#include <vrftasks/radioMessageTypes.h>
#include <vrfmsgs/adminMessage.h>
#include <vrfmsgs/simInterfaceMessage.h>
#include <vrfmsgs/ifRequestTerrainProfileInformation.h>
#include <vrfmsgs/ifIntersectionInformationResponse.h>
#include <vrfmsgs/messageTypes.h>
#include <vrfutil/scenario.h>
#include <matrix/geodeticCoord.h>
#include <matrix/vlVector.h>

// VRF_API_52 = the VR-Forces 5.2d / VR-Link 5.10 build axis (VrfBridge.vcxproj Release-5.2*).
// docs/VRF_5.2_MIGRATION_DIFF.md sec G Y-6: Start/Tick follow the 5.2d remoteControl sample.
#if VRF_API_52
#include <vrfutil/appPathResolver.h>
#include <vlutil/vlFilename.h>
#include "remoteControlInit52.h"
#else
#include "remoteControlInit.h"
#endif

#include <cstring>
#include <cstdlib>
#include <string>
#include <vector>
#include <set>
#include <windows.h>   // GetModuleHandleA/GetModuleFileNameA for NativeStackInfo()

namespace {
    // matches C2SIMinterface.cpp degreesToRadians (a radians<->degrees factor)
    const double kDegRadFactor = 57.2957795131;

    DtForceType toDtForce(vrf::Force f) {
        switch (f) {
            case vrf::Force::Opposing: return DtForceOpposing;
            case vrf::Force::Neutral:  return DtForceNeutral;
            case vrf::Force::Friendly:
            default:                   return DtForceFriendly;
        }
    }

    const char* toRoeString(vrf::Roe r) {
        switch (r) {
            case vrf::Roe::FireAtWill:         return "fire-at-will";
            case vrf::Roe::HoldFire:           return "hold-fire";
            case vrf::Roe::FireWhenFiredUpon:
            default:                           return "fire-when-fired-upon";
        }
    }

    DtEntityType toDtType(const vrf::EntityTypeSpec& t) {
        return DtEntityType(t.kind, t.domain, t.country, t.category,
                            t.subcategory, t.specific, t.extra);
    }

    // Geodetic (degrees) -> geocentric DtVector, matching geodeticToGeocentric().
    DtVector toGeocentric(const vrf::Geodetic& g) {
        DtGeodeticCoord geod(g.latDeg / kDegRadFactor,
                             g.lonDeg / kDegRadFactor,
                             g.altMeters);
        return geod.geocentric();
    }

    // Free a DtList of DtVector* we allocated for createRoute/createControlArea.
    // Mirrors the interface's deleteDtList: remove each node and delete its DATA,
    // saving next BEFORE the delete. (The DtList destructor frees only the nodes,
    // so the data must be freed explicitly.) The previous inline loop
    // "delete (DtVector*)it; it = it->next()" deleted the node instead of its data
    // AND dereferenced it after freeing - a use-after-free that crashed the caller
    // thread on the first control-area/route creation.
    void freeVectorList(DtList& list) {
        DtListItem* next = nullptr;
        for (DtListItem* item = list.first(); item; item = next) {
            next = item->next();
            delete (DtVector*)list.remove(item);
        }
    }
}

// The controller subclass, moved here verbatim from main.cxx.
class MyDtVrlinkVrfRemoteController : public makVrf::DtVrlinkVrfRemoteController {
public:
    MyDtVrlinkVrfRemoteController() : makVrf::DtVrlinkVrfRemoteController() {}

    void init(DtExerciseConn* ev,
              DtReflectedEnvironmentProcessList* rel, DtReflectedEntityList* reel,
              DtReflectedAggregateList* ral,
              const DtString& uuidMarkingForNonVrForces, bool disableRemoteDiscovery
#if DtHLA
              , makVrfVrlinkExtToolkit::DtReflectedExtendedAttributesObjectList* eaol = nullptr
#endif
    ) {
        myOwnsCommunicationManager = true;
        myCommunicationManager = DtCommunicationManager::createCommunicationManager(ev->applicationId());
        myVrlinkNetworkInterface = makVrf::DtVrlinkNetworkInterface::createVrlinkNetworkInterface(ev);
        myVrlinkNetworkInterface->setDisableRemoteDiscovery(disableRemoteDiscovery);
        myCommunicationManager->addManager(myVrlinkNetworkInterface);

        if (!myMsgIf)        { createOrSetMessageInterface(nullptr); }
        if (!myVrfTDLMsgIf)  { createOrSetVrfTDLMessageInterface(nullptr); }

        makVrf::DtVrlinkVrfRemoteController::init(
            myCommunicationManager, rel, reel, ral, uuidMarkingForNonVrForces
#if DtHLA
            , eaol
#endif
        );
    }
};

namespace vrf {

// ------------------------------------------------------------------
// Impl - every MAK object lives here
// ------------------------------------------------------------------
struct VrfFacade::Impl {
    VrfFacade* owner = nullptr;                 // to fire the std::function events
    DtRemoteControlInitializer* appInit = nullptr;
    DtExerciseConn* exConn = nullptr;
    // Base type: Start() builds a MyDtVrlinkVrfRemoteController but keeps only
    // the base pointer here; StartAdopting() stores a base pointer supplied by
    // the caller. Every method below uses base-class calls only (the sole
    // derived call - the custom init(exConn,...) overload - happens in Start()
    // via a local derived pointer before it is stored here).
    makVrf::DtVrlinkVrfRemoteController* controller = nullptr;
    makVrf::DtUUIDNetworkManager* uuidMgr = nullptr;
    bool owns = true;                           // false after StartAdopting()

    // backing storage so the char* passed to DtRemoteControlInitializer stay alive
    std::vector<std::string> argvStore;
    std::vector<char*> argvPtrs;

    // Every reflected-object UUID discovered on the network after
    // BeginTrackingReflectedObjects() (deduplicated). Used by the ResetVrf tool to
    // enumerate + delete everything present (docs/RUNBOOK.md sec 8). Empty otherwise.
    std::set<std::string> reflectedUuids;

    // Set by the first RequestTerrainProfile() after the terrain-profile reply callback is
    // registered on this controller. The registration is deliberately NOT in Start() /
    // RegisterInboundCallbacks(): the 2026-07-19 record (commit 5d14eda, HANDOFF_2026-07-19
    // sec 5) rules that native changes are additive, opt-in, and never touch the default
    // Start() path. A consumer that never asks for terrain runs the unchanged path.
    bool terrainCallbackRegistered = false;

    // UUID-network-manager change callback (matches makVrf::DtUUIDChangedCallback:
    // void(DtReflectedObject*, const DtUUID&, void*)). usr = this Impl. Fires when the
    // manager resolves a UUID for a newly discovered entity/aggregate/control object;
    // registered before the discovery ticks, it accumulates every present object's UUID.
    // Defined as a static Impl member (not a free function) so it may legally name the
    // private VrfFacade::Impl - and it keeps all Dt* types out of VrfFacade.h.
    static void reflectedUuidCallback(DtReflectedObject* /*obj*/, const DtUUID& uuid, void* usr) {
        Impl* p = static_cast<Impl*>(usr);
        if (!p) return;
        const char* s = uuid.uuidString().string();
        if (s && *s) p->reflectedUuids.insert(std::string(s));
    }

    makVrf::DtVrlinkVrfRemoteController* c() const { return controller; }
};

// ------------------------------------------------------------------
// static callback trampolines (usr = VrfFacade*)
// ------------------------------------------------------------------

// createEntity/createAggregate/... completion. Was DtTextInterface::vrfObjectCreatedCb.
static void objectCreatedTrampoline(const DtString& name, const DtEntityIdentifier& id,
                                    const DtUUID& uuid, void* usr) {
    VrfFacade* self = static_cast<VrfFacade*>(usr);
    if (self && self->OnObjectCreated) {
        ObjectCreated ev;
        ev.name = name.string() ? name.string() : "";
        ev.entityId = id.string() ? id.string() : "";
        ev.uuid = uuid.uuidString().string() ? uuid.uuidString().string() : "";
        self->OnObjectCreated(ev);
    }
}

// DtReportMessageType + radio "text-report". Extraction lifted from reportCallback;
// the C2SIM interpretation stays on the caller's side (event handlers).
static void reportTrampoline(const DtVrfObjectMessage* msg, void* usr) {
    VrfFacade* self = static_cast<VrfFacade*>(usr);
    if (!self || !msg) return;

    DtReportMessage* reportMsg = (DtReportMessage*)msg;
    DtSimReportMessageType msgType = reportMsg->contentType();
    std::string kind = msgType.string() ? msgType.string() : "";

    if (kind == "task-completed-report") {
        DtTaskCompleteReport* tc = dynamic_cast<DtTaskCompleteReport*>(reportMsg->report());
        if (tc && self->OnTaskCompleted) {
            TaskCompleted ev;
            const char* mark = msg->transmitter().markingText();
            ev.unitMarking = mark ? mark : "";
            ev.taskType = tc->taskCompleted().string() ? tc->taskCompleted().string() : "";
            self->OnTaskCompleted(ev);
        }
    } else if (kind == "text-report") {
        DtTextReport* tr = dynamic_cast<DtTextReport*>(reportMsg->report());
        if (tr && self->OnTextReport) {
            TextReport ev;
            ev.text = tr->text().c_str() ? tr->text().c_str() : "";
            self->OnTextReport(ev);
        }
    }
}

static void scenarioCloseTrampoline(const DtVrfObjectMessage* /*msg*/, void* usr) {
    VrfFacade* self = static_cast<VrfFacade*>(usr);
    if (self && self->OnScenarioClosed) self->OnScenarioClosed();
}

// DtAvailableFormationsAdmin response (docs/UNIT_MOVEMENT_RESEARCH.md plan R4): an
// aggregate answering RequestAvailableFormations. transmitter() is that aggregate.
static void availableFormationsTrampoline(const DtVrfObjectMessage* msg, void* usr) {
    VrfFacade* self = static_cast<VrfFacade*>(usr);
    if (!self || !msg || !self->OnAvailableFormations) return;
    const DtAdminMessage* adminMsg = (const DtAdminMessage*)msg;
    DtAvailableFormationsAdmin* content =
        dynamic_cast<DtAvailableFormationsAdmin*>(adminMsg->adminContent());
    if (!content) return;
    AvailableFormations ev;
    ev.uuid = msg->transmitter().uuidString().string()
                  ? msg->transmitter().uuidString().string() : "";
    for (const DtString& f : content->formationList())
        ev.formations.push_back(f.string() ? f.string() : "");
    ev.currentFormation = content->currentFormation().string()
                              ? content->currentFormation().string() : "";
    self->OnAvailableFormations(ev);
}

// Object Console message: the per-unit warning / diagnostic channel behind the yellow
// Object Console badge (docs/VRF_GROUND_TRUTH.md sec 0.0 cross-finding 1 / sec 7;
// groundwork plan 0.6). Unlike the report trampolines above this is NOT a message-
// executive category callback - it is a DIRECT controller callback whose signature is
// exactly DtObjectConsoleMessageCallbackFcn (vrfRemoteController.h:112-114), so no cast
// is needed. usr = this facade. All string extraction is null-guarded like the other
// trampolines; the text is delivered UNESCAPED (the sink escapes it).
static void objectConsoleMessageTrampoline(const DtUUID& id, int notifyLevel,
                                           const DtString& message, void* usr) {
    VrfFacade* self = static_cast<VrfFacade*>(usr);
    if (self && self->OnObjectConsoleMessage) {
        ObjectConsoleMessage ev;
        ev.uuid = id.uuidString().string() ? id.uuidString().string() : "";
        ev.notifyLevel = notifyLevel;
        ev.message = message.string() ? message.string() : "";
        self->OnObjectConsoleMessage(ev);
    }
}

// Terrain-profile reply (docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 1.2-1.4).
// Registered BY CONTENT TYPE on the controller's DtVrfMessageInterface
// (vrfMessageInterface.h:164, DtMessageCallbackFcn = void(DtSimMessage*, void*)), so the
// delivered message is a DtSimInterfaceMessage carrying a
// DtIfIntersectionInformationResponse; the type() check guards the static_cast. Points are
// GEOCENTRIC (ifIntersectionInformationResponse.h:20) -> geodetic like TryGetEntityGeodetic.
// The reply is a vector of SETS, each set a vector of DtIntersectionInformation. The header
// speaks of "one set of response for each pair" for the general intersection request
// (:137-139); for the PROFILE request the back end keeps ONE result per request POINT
// (terrainProfileRequestManager.h:107-121, Results = map<int, Result>) and the header
// says userData carries the request point index (ifRequestTerrainProfileInformation.h:46-48).
// How those results are packed into sets is NOT documented, and run 20260902T101431Z
// (ROW2R) showed that reading only entry [0] of each set yields vertex 0 alone. So: walk
// EVERY entry of EVERY set; index = userData when it parses, else a running count over the
// flattened entries. An EMPTY set = no terrain data for that set index (:136-138), kept as
// an invalid sample so the caller sees the gap. usr = this facade.
static void terrainProfileTrampoline(DtSimMessage* msg, void* usr) {
    VrfFacade* self = static_cast<VrfFacade*>(usr);
    if (!self || !msg || !self->OnTerrainProfile) return;
    DtSimInterfaceMessage* im = static_cast<DtSimInterfaceMessage*>(msg);
    DtSimInterfaceContent* content = im->interfaceContent();
    if (!content || content->type() != DtIntersectionInformationResponseType) return;
    const DtIfIntersectionInformationResponse* resp =
        static_cast<const DtIfIntersectionInformationResponse*>(content);
    TerrainProfile ev;
    ev.requestId = (unsigned int)resp->responseId();
    ev.complete = resp->complete();
    const DtIfIntersectionInformationResponse::IntersectionPairInformation& pairs =
        resp->intersectionPairInformation();
    int running = 0;
    for (size_t i = 0; i < pairs.size(); ++i) {
        if (pairs[i].empty()) {
            TerrainSample gap;
            gap.index = (int)i;
            ev.samples.push_back(gap);
            continue;
        }
        for (size_t j = 0; j < pairs[i].size(); ++j, ++running) {
            const DtIntersectionInformation& info = pairs[i][j];
            TerrainSample s;
            s.index = running;
            const char* ud = info.userData().string();
            if (ud && *ud) {
                char* end = nullptr;
                long parsed = std::strtol(ud, &end, 10);
                if (end && *end == '\0') s.index = (int)parsed;
            }
            DtGeodeticCoord geod;
            geod.setGeocentric(info.intersectionPoint());
            s.point.latDeg = geod.lat() * kDegRadFactor;
            s.point.lonDeg = geod.lon() * kDegRadFactor;
            s.point.altMeters = geod.alt();
            s.valid = true;
            ev.samples.push_back(s);
        }
    }
    self->OnTerrainProfile(ev);
}

// ------------------------------------------------------------------
// VrfFacade
// ------------------------------------------------------------------
VrfFacade::VrfFacade() : p_(new Impl) { p_->owner = this; }

VrfFacade::~VrfFacade() { Stop(); delete p_; p_ = nullptr; }

bool VrfFacade::Start(const StartupConfig& cfg) {
    // Build the synthetic command line the DtRemoteControlInitializer expects,
    // exactly as main.cxx did for each protocol.
    std::vector<std::string>& s = p_->argvStore;
    s.clear();
    char appNum[16]; std::snprintf(appNum, sizeof(appNum), "%d", cfg.applicationNumber);
    char site[16];   std::snprintf(site, sizeof(site), "%d", cfg.siteId);

#if DtHLA
    s.push_back("bin64\\c2simVRFHLA1516e");
    s.push_back("--rprFomVersion"); s.push_back(cfg.rprFomVersion);
    for (const std::string& fom : cfg.fomModules) {
        s.push_back("--fomModules"); s.push_back(fom);
    }
    s.push_back("-a"); s.push_back(appNum);
    s.push_back("-s"); s.push_back(site);
#if VRF_API_52
    // 5.2d: the federation identity comes from the connection config file (Y-2,
    // MAK-ONE-2025-Config.xml); an explicit value on the command line overrides it.
    if (!cfg.federation.empty())  { s.push_back("--execName");    s.push_back(cfg.federation); }
    if (!cfg.fedFileName.empty()) { s.push_back("--fedFileName"); s.push_back(cfg.fedFileName); }
#else
    s.push_back("--execName"); s.push_back(cfg.federation);
    s.push_back("--fedFileName"); s.push_back(cfg.fedFileName);
#endif
    s.push_back("-n"); s.push_back("1");
#else
    char disVer[16]; std::snprintf(disVer, sizeof(disVer), "%d", cfg.disVersion);
    char disPort[16]; std::snprintf(disPort, sizeof(disPort), "%d", cfg.disPort);
    s.push_back("bin64\\c2simVRF");
    s.push_back("--disVersion"); s.push_back(disVer);
    s.push_back("--deviceAddress"); s.push_back(cfg.deviceAddress);
    s.push_back("--disPort"); s.push_back(disPort);
    s.push_back("-a"); s.push_back(appNum);
    s.push_back("-s"); s.push_back(site);
    s.push_back("-x"); s.push_back("1");
    s.push_back("-n"); s.push_back("2");
#endif

    p_->argvPtrs.clear();
    for (std::string& str : s) p_->argvPtrs.push_back(&str[0]);
    int argCount = (int)p_->argvPtrs.size();

#if VRF_API_52
    // 5.2d sample: the initializer takes the connection config file (MAK-ONE-2025-Config.xml
    // resolved from the VR-Forces settings tree unless the caller names one).
    std::string configFile = cfg.connectionConfigFile;
    if (configFile.empty()) {
        configFile = DtFilename::combine(
            makVrf::DtAppPathResolver::Instance().connectionsSettingsDirectory(),
            DtFilename(DtDefaultConfigFile)).c_str();
    }
    p_->appInit = new DtRemoteControlInitializer(argCount, p_->argvPtrs.data(), DtString(configFile.c_str()));
#else
    p_->appInit = new DtRemoteControlInitializer(argCount, p_->argvPtrs.data());
#endif
    p_->appInit->parseCmdLine();

    // Build via the local derived type so the custom init(exConn,...) overload
    // is reachable, then keep only the base pointer (all later calls are base).
    // (5.2d added a DtReflectedAerodromeList* to the BASE DtExerciseConn* overload;
    // this derived overload forwards to the DtCommunicationManager* overload, unchanged.)
    MyDtVrlinkVrfRemoteController* newController = new MyDtVrlinkVrfRemoteController();
    p_->exConn = new DtExerciseConn(*p_->appInit);
#if VRF_API_52
    // 5.2: call the BASE init(DtExerciseConn*, rel, reel, ral, ael, marking,
    // disableRemoteDiscovery=false) - the overload the 5.2d sample uses. Its doc
    // (vrlinkVrfRemoteController.h :90-93): it creates the communication manager
    // AND, when disableRemoteDiscovery is false, the DtRemoteObjectManager that
    // discovers state data. Our 5.0.2-era derived overload replicates only the
    // 5.0.2 wiring (comm manager + vrlink interface) and never creates that
    // manager - on 5.2 that left every observer BLIND (reflected=0 while the
    // command/backend-state channel worked; PREREG_52_TOOLJOIN_2026-09-03.md,
    // appNos 3808/3810/3811). Qualified call = the base overload explicitly.
    newController->makVrf::DtVrlinkVrfRemoteController::init(
        p_->exConn, nullptr, nullptr, nullptr, nullptr, "entity-identifier", false);
#else
    newController->init(p_->exConn, nullptr, nullptr, nullptr, "entity-identifier", true);
#endif
    p_->controller = newController;
    p_->owns = true;

    p_->controller->eventManager()->setProcessEventsImmediately(true);
#if VRF_API_52
    p_->controller->setMonitorBackendState(true);   // 5.2d sample; API new in 5.2
#endif
    p_->controller->vrfMessageInterface()->setSessionId(cfg.sessionId);

    // register the inbound callbacks (usr = this facade)
    DtVrfObjectMessageExecutive* msgExec = p_->controller->objectMessageExecutive();
    msgExec->addMessageCallbackByCategory(
        DtReportMessageType, (DtVrfObjectMessageCallbackFcn)reportTrampoline, this);
    p_->controller->radioMessageListener()->addMessageCallback(
        (DtVrfObjectMessageCallbackFcn)reportTrampoline, this);
    msgExec->addMessageCallbackByCategory(
        DtCloseScenarioMessageType, (DtVrfObjectMessageCallbackFcn)scenarioCloseTrampoline, this);
    // typed (per-content-type) callback for the available-formations reply (plan R4)
    msgExec->addMessageCallback(DtAvailableFormationsResponseAdminType,
        (DtVrfObjectMessageCallbackFcn)availableFormationsTrampoline, this);
    // Object Console per-unit warnings (groundwork plan 0.6). Direct controller callback
    // (exact DtObjectConsoleMessageCallbackFcn signature) - not a msgExec category.
    p_->controller->addObjectConsoleMessageCallback(objectConsoleMessageTrampoline, this);

    // host address + uuid manager
    p_->controller->setHostInetAddr(&(std::string(cfg.hostInetAddr))[0]);
    p_->uuidMgr = p_->controller->uuidNetworkManager();

    return p_->controller != nullptr;
}

bool VrfFacade::StartAdopting(void* controllerPtr, void* exConnPtr, void* uuidMgrPtr) {
    // Transition-only: adopt a controller/exConn/uuidMgr the caller created and
    // owns (see the header). The command methods then drive the SAME controller
    // as the existing textIf path. We deliberately register NO inbound callbacks
    // (textIf still owns them; registering would double-fire) and take NO
    // ownership (owns=false -> Stop() will not delete the caller's objects).
    p_->controller = static_cast<makVrf::DtVrlinkVrfRemoteController*>(controllerPtr);
    p_->exConn     = static_cast<DtExerciseConn*>(exConnPtr);
    p_->uuidMgr    = static_cast<makVrf::DtUUIDNetworkManager*>(uuidMgrPtr);
    p_->appInit    = nullptr;   // not ours
    p_->owns       = false;
    return p_->controller != nullptr;
}

void VrfFacade::Stop() {
    if (!p_) return;
    // Only tear down what we created; an adopted controller/exConn belongs to
    // its owner (owns==false after StartAdopting).
    if (p_->owns) {
        delete p_->controller;
        delete p_->exConn;
        delete p_->appInit;
    }
    p_->controller = nullptr;
    p_->terrainCallbackRegistered = false;
    p_->exConn = nullptr;
    p_->appInit = nullptr;
    p_->uuidMgr = nullptr;
    p_->owns = true;
    p_->reflectedUuids.clear();
}

void* VrfFacade::GetController() const { return p_->controller; }
void* VrfFacade::GetExConn() const { return p_->exConn; }

void VrfFacade::RegisterInboundCallbacks() {
    if (!p_->controller) return;
    // Identical to the registration block in Start(): report messages via the
    // object message executive, radio text/spot reports via the radio listener,
    // scenario-close via the executive. usr = this facade.
    DtVrfObjectMessageExecutive* msgExec = p_->controller->objectMessageExecutive();
    msgExec->addMessageCallbackByCategory(
        DtReportMessageType, (DtVrfObjectMessageCallbackFcn)reportTrampoline, this);
    p_->controller->radioMessageListener()->addMessageCallback(
        (DtVrfObjectMessageCallbackFcn)reportTrampoline, this);
    msgExec->addMessageCallbackByCategory(
        DtCloseScenarioMessageType, (DtVrfObjectMessageCallbackFcn)scenarioCloseTrampoline, this);
    // Object Console per-unit warnings (groundwork plan 0.6) - mirrors Start(). textIf
    // never registered this callback, so there is no double-fire risk on the adopt path.
    p_->controller->addObjectConsoleMessageCallback(objectConsoleMessageTrampoline, this);
}

void VrfFacade::Tick() {
    if (!p_->controller) return;
#if !VRF_API_52
    // 5.0.2 oracle loop. Dropped on 5.2 per Y-6 (the 5.2d sample does not drive the clock).
    p_->exConn->clock()->setSimTime(p_->exConn->clock()->elapsedRealTime());
#endif
    p_->exConn->drainInput();
    p_->controller->tick();
}

int VrfFacade::BackendCount() const {
    return p_->controller ? p_->controller->backends().count() : 0;
}

std::string VrfFacade::NativeStackInfo() {
#if VRF_API_52
    std::string info = "5.2|";
#else
    std::string info = "5.0.2|";
#endif
    HMODULE h = GetModuleHandleA("vrfcontrol.dll");
    if (!h) return info + "(vrfcontrol.dll not loaded)";
    char path[MAX_PATH * 2] = {0};
    DWORD n = GetModuleFileNameA(h, path, sizeof(path) - 1);
    return info + (n ? std::string(path, n) : std::string("(GetModuleFileName failed)"));
}

bool VrfFacade::AllBackendsReady() const {
    return p_->controller && p_->controller->allBackendsReady();
}

void VrfFacade::Run()  { if (p_->controller) p_->controller->run(); }
void VrfFacade::Pause(){ if (p_->controller) p_->controller->pause(); }
void VrfFacade::SetTimeMultiplier(int multiple) {
    if (p_->controller) p_->controller->setTimeMultiplier(multiple);
}

void VrfFacade::SetExerciseStartTime(int y, int mo, int d, int h, int mi, int se) {
    if (!p_->controller) return;
    DtScenario* scenario = new DtScenario(DtString("DATE-TIME"));
    scenario->setExerciseStartDateAndTime(y, mo, d, h, mi, se);
    p_->controller->setExerciseStartTime(scenario);
}

void VrfFacade::CreateEntity(const EntityTypeSpec& type, const Geodetic& pos,
                             Force force, double headingDeg, const std::string& name) {
    p_->controller->createEntity(objectCreatedTrampoline, this,
        toDtType(type), toGeocentric(pos), toDtForce(force),
        (DtReal)(headingDeg / kDegRadFactor), DtString(name.c_str()));
}

void VrfFacade::CreateAggregate(const EntityTypeSpec& type, const Geodetic& pos,
                                Force force, double headingDeg, const std::string& name,
                                AggregateState state, bool createSubordinates) {
    DtAggregateState st = (state == AggregateState::Aggregated)
                              ? DtAggregated : DtDisaggregated;
    p_->controller->createAggregate(objectCreatedTrampoline, this,
        toDtType(type), toGeocentric(pos), toDtForce(force),
        (DtReal)(headingDeg / kDegRadFactor), DtString(name.c_str()),
        DtString::nullString(), DtSimSendToAll, st, DtUUID::nullUUID(), createSubordinates);
}

void VrfFacade::CreateWaypoint(const Geodetic& pos, const std::string& name) {
    p_->controller->createWaypoint(objectCreatedTrampoline, this,
        toGeocentric(pos), DtString(name.c_str()));
}

void VrfFacade::CreateRoute(const std::vector<Geodetic>& points, const std::string& name) {
    DtList list;
    for (const Geodetic& g : points) list.add(new DtVector(toGeocentric(g)));
    p_->controller->createRoute(objectCreatedTrampoline, this, list, DtString(name.c_str()));
    freeVectorList(list);
}

void VrfFacade::CreateControlArea(const std::vector<Geodetic>& perimeter,
                                  const std::string& name, const std::string& label,
                                  const std::string& uuid) {
    DtList list;
    for (const Geodetic& g : perimeter) list.add(new DtVector(toGeocentric(g)));
    // Parity: the interface assigns the area's C2SIM uuid to the created
    // tactical graphic; empty falls back to the clean nullUUID default.
    DtUUID areaUuid = uuid.empty() ? DtUUID::nullUUID() : DtUUID(uuid.c_str());
    p_->controller->createControlArea(objectCreatedTrampoline, this, list,
        DtString(name.c_str()), DtString(label.c_str()), DtSimSendToAll, areaUuid);
    freeVectorList(list);
}

void VrfFacade::SetAltitude(const std::string& uuid, double altitudeMeters) {
    p_->controller->setAltitude(DtUUID(uuid), altitudeMeters, TRUE);
}

void VrfFacade::SetLocation(const std::string& uuid, const Geodetic& pos) {
    p_->controller->setLocation(DtUUID(uuid), toGeocentric(pos));
}

void VrfFacade::SetTarget(const std::string& uuid, const std::string& targetUuid) {
    p_->controller->setTarget(DtUUID(uuid), DtUUID(targetUuid));
}

void VrfFacade::SetRulesOfEngagement(const std::string& uuid, Roe roe) {
    p_->controller->setRulesOfEngagement(DtUUID(uuid), toRoeString(roe), DtSimSendToAll);
}

void VrfFacade::MoveToLocation(const std::string& uuid, const Geodetic& pos) {
    DtVector v = toGeocentric(pos);
    p_->controller->moveToLocation(DtUUID(uuid), DtVector64(v.x(), v.y(), v.z()), DtSimSendToAll);
}

void VrfFacade::MoveAlongRoute(const std::string& uuid, const std::string& routeUuid) {
    p_->controller->moveAlongRoute(DtUUID(uuid), DtUUID(routeUuid), DtSimSendToAll);
}

void VrfFacade::PlanAndMoveTo(const std::string& uuid, const std::string& controlPointUuid) {
    // DtPlanAndMoveToTask : DtMoveToTask - the PLANNED (pathfinding) move to a control
    // point (R11). The base task addresses a waypoint OBJECT, not raw coordinates.
    DtPlanAndMoveToTask task;
    task.init();
    task.setControlPoint(DtUUID(controlPointUuid));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

namespace {
    // Recursive worker for GetAggregateMembers: collect the aggregate state's ENTITY
    // members, then descend into SUB-AGGREGATES (company-type units publish their
    // elements as sub-aggregates, not entities - live R10 finding). Depth-capped
    // against designator cycles/garbage.
    void collectMembers(makVrf::DtUUIDNetworkManager* mgr,
                        const DtAggregateStateRepository* asr,
                        int depth, std::vector<vrf::AggregateMember>& out) {
        if (!asr || depth > 3) return;
        DtReflectedExtEntityList* ents = mgr->entityList();
        if (ents) {
            const DtGlobalObjectDesignatorList& members = asr->entities();
            for (int i = 0; i < members.numObjects(); ++i) {
                bool valid = false;
                const DtGlobalObjectDesignator& des = members.object(i, &valid);
                if (!valid) continue;
                DtReflectedExtEntity* ent = ents->lookupEE(des);
                if (!ent) continue;               // silent/not-yet-reflected member
                vrf::AggregateMember m;
                DtUUID u = mgr->uuidFor(ent);
                m.uuid = u.uuidString().string() ? u.uuidString().string() : "";
                const char* mark = ent->entityStateRep() ? ent->entityStateRep()->markingText() : nullptr;
                m.name = mark ? mark : "";
                if (!m.uuid.empty()) out.push_back(m);
            }
        }
        DtReflectedExtAggregateList* aggs = mgr->aggregateList();
        if (aggs) {
            const DtGlobalObjectDesignatorList& subs = asr->subAggregates();
            for (int i = 0; i < subs.numObjects(); ++i) {
                bool valid = false;
                const DtGlobalObjectDesignator& des = subs.object(i, &valid);
                if (!valid) continue;
                DtReflectedExtAggregate* sub = aggs->lookupEA(des);
                if (!sub) continue;
                // DtExtAggregateStateRepository derives DtAggregateStateRepository.
                collectMembers(mgr, sub->extAggregateStateRep(), depth + 1, out);
            }
        }
    }
}

std::vector<AggregateMember> VrfFacade::GetAggregateMembers(const std::string& aggregateUuid) const {
    std::vector<AggregateMember> out;
    if (!p_->uuidMgr) return out;
    DtReflectedObject* obj = p_->uuidMgr->reflectedObjectFor(DtUUID(aggregateUuid));
    if (!obj) return out;

    // Typed path first; the dynamic_cast is known to MISS for disaggregated aggregates
    // across the MAK DLL boundary (same RTTI issue as TryGetEntityGeodetic), so fall
    // back to a static_cast - valid ONLY because the caller guarantees this uuid is an
    // aggregate it created (see the header CAVEAT).
    DtAggregateStateRepository* asr = nullptr;
    if (DtReflectedAggregate* agg = dynamic_cast<DtReflectedAggregate*>(obj))
        asr = agg->aggregateStateRep();
    if (!asr)
        asr = static_cast<DtReflectedAggregate*>(obj)->aggregateStateRep();
    if (!asr) return out;

    // Entity members first, then recurse into published SUB-aggregates (companies
    // publish platoon/section sub-aggregates whose states carry the entity members).
    collectMembers(p_->uuidMgr, asr, 0, out);
    return out;
}

void VrfFacade::SetAggregateFormation(const std::string& uuid, const std::string& formationName) {
    // No-op if 'uuid' is not an aggregate leader (per the controller contract).
    p_->controller->setAggregateFormation(DtUUID(uuid), DtString(formationName.c_str()), DtSimSendToAll);
}

void VrfFacade::ReorganizeAggregate(const std::string& uuid) {
    // "Only useful when automatic reorganization is not enabled" (vrfRemoteController.h:1569)
    // - which is the shipped default (auto-promote-in-formation False in every movement
    // sysdef). (Re)establishes leader/echelon assignments so the disaggregated move-along
    // controller has a LEAD subordinate to forward routes to (UNIT_MOVEMENT_RESEARCH.md).
    p_->controller->reorganizeAggregate(DtUUID(uuid), DtSimSendToAll);
}

void VrfFacade::RequestAvailableFormations(const std::string& uuid) {
    // No controller convenience method exists for this admin content (verified against
    // vrfRemoteController.h); wrap it in a DtAdminMessage addressed to the aggregate and
    // send via sendMessageToObject. The reply lands in availableFormationsTrampoline.
    // The message keeps only a POINTER to the content, but sendMessageToObject serializes
    // synchronously (same lifetime model as the stack DtSimTask objects sent above).
    DtRequestAvailableFormationsAdmin req;
    req.init();
    DtAdminMessage msg;
    msg.setAdminContent(&req);
    msg.setRecipient(DtUUID(uuid));
    p_->controller->sendMessageToObject(&msg, DtSimSendToAll);
}

void VrfFacade::MoveIntoFormation(const std::string& uuid, const Geodetic& pos,
                                  double headingDeg, const std::string& formationName) {
    DtMoveIntoFormationTask task;
    task.init();
    task.setLocation(toGeocentric(pos));
    task.setHeading(headingDeg / kDegRadFactor);  // task wants radians (createEntity does the same)
    task.setFormationName(DtString(formationName.c_str()));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::Breach(const std::string& uuid, const std::string& breachTargetUuid) {
    // DtBreachTask: go to the obstacle (breach target) and breach it. Layer 2: the BREACH verb
    // (docs/SEMANTIC_MAPPING.md Unit 2). The target must be a VRF UUID known to the sim.
    DtBreachTask task;
    task.init();
    task.setBreachTarget(DtUUID(breachTargetUuid));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::PatrolRoute(const std::string& uuid, const std::string& routeUuid) {
    // DtPatrolRouteTask: patrol back and forth along the (already-created) route. Layer 2 for
    // SCREEN/SCOUT (Reconnoiter). The route is resolved by name, like MoveAlongRoute.
    DtPatrolRouteTask task;
    task.init();
    task.setRoute(DtUUID(routeUuid));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::FollowEntity(const std::string& uuid, const std::string& targetUuid) {
    // DtFollowEntityTask: follow the target entity (dynamic; no route). Layer 2 for ESCRT.
    // Offset left at default (0); a trailing offset could be set later.
    DtFollowEntityTask task;
    task.init();
    task.setEntityToFollow(DtUUID(targetUuid));
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::DeleteObject(const std::string& uuid) {
    // Counterpart to createEntity/createAggregate/createRoute/createControlArea. Tells the
    // backend(s) to remove the object; safe no-op if the uuid is unknown to VRF.
    p_->controller->deleteObject(DtUUID(uuid));
}

void VrfFacade::BeginTrackingReflectedObjects() {
    // Register on the UUID network manager's per-type change callbacks so every reflected
    // object's UUID lands in p_->reflectedUuids as it is discovered/resolved. The base
    // reflected lists expose only first()/last() (no iterator), so callback-collection is
    // the way to enumerate them. Call BEFORE the first Tick() so no discovery is missed.
    if (!p_->uuidMgr) return;
    p_->uuidMgr->addEntityUUIDChangedCallback(&Impl::reflectedUuidCallback, p_);
    p_->uuidMgr->addAggregateUUIDChangedCallback(&Impl::reflectedUuidCallback, p_);
    p_->uuidMgr->addEnvironmentalUUIDChangedCallback(&Impl::reflectedUuidCallback, p_);
}

std::vector<std::string> VrfFacade::GetAllReflectedUuids() const {
    return std::vector<std::string>(p_->reflectedUuids.begin(), p_->reflectedUuids.end());
}

void VrfFacade::FireAtTarget(const std::string& uuid, const std::string& targetUuid,
                             bool autoSelectWeapon, int maxRounds) {
    DtFireAtTargetTask task;
    task.init();
    task.setTarget(DtUUID(targetUuid));
    task.setAutoSelectWeapon(autoSelectWeapon);
    if (maxRounds > 0) task.setMaxRoundsToFire(maxRounds);
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::RunScriptedTask(const std::string& uuid, const std::string& scriptId,
                                const std::vector<ScriptVar>& vars) {
    DtScriptedTaskTask task;
    task.init();
    task.setScriptId(scriptId.c_str());
    for (const ScriptVar& v : vars) {
        if (v.kind == ScriptVar::Kind::ObjectUuid) {
            DtRwObjectName* var = new DtRwObjectName(v.name.c_str());
            var->setUUID(DtUUID(v.uuidValue));
            task.variables().addVariable(var);
        } else {
            DtRwReal* var = new DtRwReal(v.name.c_str());
            var->setValue(v.realValue);
            task.variables().addVariable(var);
        }
    }
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::SendScriptedSet(const std::string& uuid, const std::string& scriptId,
                                const std::vector<ScriptVar>& vars) {
    DtScriptedTaskSet set;
    set.init();
    set.setScriptId(scriptId.c_str());
    for (const ScriptVar& v : vars) {
        if (v.kind == ScriptVar::Kind::ObjectUuid) {
            DtRwObjectName* var = new DtRwObjectName(v.name.c_str());
            var->setUUID(DtUUID(v.uuidValue));
            set.variables().addVariable(var);
        } else {
            DtRwReal* var = new DtRwReal(v.name.c_str());
            var->setValue(v.realValue);
            set.variables().addVariable(var);
        }
    }
    p_->controller->sendSetDataMsg(DtUUID(uuid), &set, DtSimSendToAll);
}

unsigned int VrfFacade::RequestTerrainProfile(const std::vector<Geodetic>& points) {
    // docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md sec 1.1/1.4. Stack content; the
    // message interface serializes it inside createAndDeliverMessage (vrfMessageInterface.h
    // :62-65), the same lifetime model as the stack DtSimTask objects sent above. Complete
    // reply only (sendPartialInformation=false) so the caller correlates ONE message per
    // request id; the id comes from the controller's own counter (vrfRemoteController.h:249).
    // DtSimSendToAll as for every other message here: each back end answers, the caller
    // keeps the first. The reply lands in terrainProfileTrampoline, whose callback is
    // registered here on first use (once per controller) so that Start() and
    // RegisterInboundCallbacks() stay byte-for-byte the pre-feature path for every consumer
    // (2026-07-19 rule; Impl::terrainCallbackRegistered).
    if (!p_->controller || points.empty()) return 0;
    if (!p_->terrainCallbackRegistered) {
        // sim-interface message callback by content type (vrfMessageInterface.h:164), not
        // an object-message category (design doc sec 1.4).
        p_->controller->vrfMessageInterface()->addMessageCallback(
            DtIntersectionInformationResponseType, terrainProfileTrampoline, this);
        p_->terrainCallbackRegistered = true;
    }
    unsigned int id = p_->controller->generateRequestId();
    DtIfRequestTerrainProfileInformation req;
    req.setRequestId((int)id);
    req.setSendPartialInformation(false);
    std::vector<DtVector> pts;
    pts.reserve(points.size());
    for (const Geodetic& g : points) pts.push_back(toGeocentric(g));
    req.setPoints(pts);
    p_->controller->vrfMessageInterface()->createAndDeliverMessage(DtSimSendToAll, req);
    return id;
}

bool VrfFacade::TryGetEntityGeodetic(const std::string& uuid, Geodetic& out) const {
    if (!p_->uuidMgr) return false;
    DtReflectedObject* obj = p_->uuidMgr->reflectedObjectFor(DtUUID(uuid));
    if (!obj) return false;

    // Resolve the location from EITHER an entity or an aggregate. The C++ oracle
    // getUnitGeodeticFromSim static_cast'd every reflected object to DtReflectedEntity*
    // (wrong-type UB for an aggregate, but it happened to yield a usable location, so the
    // disaggregated aggregate 11.MechBn moved - PORT.md sec 5/8). This port handles the
    // aggregate case PROPERLY: DtReflectedAggregate exposes aggregateStateRep(), whose
    // DtAggregateStateRepository shares DtBaseEntityStateRepository::location() with an
    // entity's DtEntityStateRepository - so both paths return the same geocentric vector.
    // Without this, dynamic_cast<DtReflectedEntity*> returns null for an aggregate and the
    // caller ABANDONS the task, breaking the golden aggregate-move.
    DtBaseEntityStateRepository* sr = nullptr;
    if (DtReflectedEntity* ent = dynamic_cast<DtReflectedEntity*>(obj))
        sr = ent->entityStateRep();
    else if (DtReflectedAggregate* agg = dynamic_cast<DtReflectedAggregate*>(obj))
        sr = agg->aggregateStateRep();
    // Parity fallback: the C++ oracle getUnitGeodeticFromSim read the base state repository
    // via a BLIND static_cast to DtReflectedEntity* and it worked for a disaggregated
    // aggregate (11/14.MechBn moved, PORT.md sec 5). Live-verified here: the typed path
    // resolves entities, but the aggregate dynamic_cast misses (concrete reflected type /
    // RTTI across the MAK DLL boundary), so 14.MechBn abandoned. static_cast reads the same
    // base-offset myStateRep, so location() still yields the object's location.
    if (!sr)
        sr = static_cast<DtReflectedEntity*>(obj)->entityStateRep();
    if (!sr) return false;
    DtVector geoLocation = sr->location();
    DtGeodeticCoord geod;
    geod.setGeocentric(geoLocation);
    out.latDeg = geod.lat() * kDegRadFactor;
    out.lonDeg = geod.lon() * kDegRadFactor;
    out.altMeters = geod.alt();
    return true;
}

} // namespace vrf
