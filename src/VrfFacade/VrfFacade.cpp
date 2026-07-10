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
#include <vrfutil/scenario.h>
#include <matrix/geodeticCoord.h>
#include <matrix/vlVector.h>

#include "remoteControlInit.h"

#include <cstring>
#include <string>
#include <vector>

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
    s.push_back("--execName"); s.push_back(cfg.federation);
    s.push_back("--fedFileName"); s.push_back(cfg.fedFileName);
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

    p_->appInit = new DtRemoteControlInitializer(argCount, p_->argvPtrs.data());
    p_->appInit->parseCmdLine();

    // Build via the local derived type so the custom init(exConn,...) overload
    // is reachable, then keep only the base pointer (all later calls are base).
    MyDtVrlinkVrfRemoteController* newController = new MyDtVrlinkVrfRemoteController();
    p_->exConn = new DtExerciseConn(*p_->appInit);
    newController->init(p_->exConn, nullptr, nullptr, nullptr, "entity-identifier", true);
    p_->controller = newController;
    p_->owns = true;

    p_->controller->eventManager()->setProcessEventsImmediately(true);
    p_->controller->vrfMessageInterface()->setSessionId(cfg.sessionId);

    // register the three inbound callbacks (usr = this facade)
    DtVrfObjectMessageExecutive* msgExec = p_->controller->objectMessageExecutive();
    msgExec->addMessageCallbackByCategory(
        DtReportMessageType, (DtVrfObjectMessageCallbackFcn)reportTrampoline, this);
    p_->controller->radioMessageListener()->addMessageCallback(
        (DtVrfObjectMessageCallbackFcn)reportTrampoline, this);
    msgExec->addMessageCallbackByCategory(
        DtCloseScenarioMessageType, (DtVrfObjectMessageCallbackFcn)scenarioCloseTrampoline, this);

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
    p_->exConn = nullptr;
    p_->appInit = nullptr;
    p_->uuidMgr = nullptr;
    p_->owns = true;
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
}

void VrfFacade::Tick() {
    if (!p_->controller) return;
    p_->exConn->clock()->setSimTime(p_->exConn->clock()->elapsedRealTime());
    p_->exConn->drainInput();
    p_->controller->tick();
}

int VrfFacade::BackendCount() const {
    return p_->controller ? p_->controller->backends().count() : 0;
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

void VrfFacade::SetAggregateFormation(const std::string& uuid, const std::string& formationName) {
    // No-op if 'uuid' is not an aggregate leader (per the controller contract).
    p_->controller->setAggregateFormation(DtUUID(uuid), DtString(formationName.c_str()), DtSimSendToAll);
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
