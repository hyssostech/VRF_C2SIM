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
// Observation-channel diagnostics only (ReflectedCounts / HasDiscoveredObjects /
// PrintReflectedObjectCounts / Have*License). None of these headers is used by the
// pre-existing command path.
#include <vrfExtObjects/reflectedControlObjectList.h>
#include <vl/reflectedEnvironmentProcessList.h>
#include <vrlinkNetworkInterface/remoteObjectManager.h>
#include <vl/checkLicense.h>
#include <vl/globalObjectDesignatorList.h>
#include <vl/globalObjectDesignator.h>
#include <vrftasks/radioMessageTypes.h>
#include <vrfmsgs/adminMessage.h>
#include <vrfmsgs/simInterfaceMessage.h>
#include <vrfmsgs/ifRequestTerrainProfileInformation.h>
#include <vrfmsgs/ifIntersectionInformationResponse.h>
#include <vrfmsgs/messageTypes.h>
#include <vrfutil/scenario.h>
// STP-809: the back-end CONTROL STATE and ACTIVE count (BackendControlState /
// ActiveBackendCount). messageTypes.h above carries DtUnknownControlType /
// DtPauseControlType / DtRunControlType; these two add the listener and the DtBackend
// entries it holds. Neither is touched by the pre-existing command path.
#include <vrfcontrol/vrfBackendListener.h>
#include <vrfutil/backend.h>
#include <matrix/geodeticCoord.h>
#include <matrix/vlVector.h>
// B7: geocentric -> topographic conversion for the kinematics read
// (DtGetHeadingFromGeocentric, DtLatLon_to_GeocToTopo) and DtDcmVecMul.
#include <matrix/topoCoord.h>
#include <matrix/vlDcm.h>
#include <matrix/vlTaitBryan.h>
// Scripted-task variable reader/writers (V2). scriptedTaskTask.h already pulls
// rwVariableBindings.h + rwString.h + rwUUID.h; the rest are named explicitly so the
// DescribeScriptVars read-back does not depend on a transitive include.
#include <readerWriter/rwBoolean.h>
#include <readerWriter/rwInt.h>
#include <readerWriter/rwReal.h>
#include <readerWriter/rwString.h>
#include <readerWriter/rwVector.h>
#include <vrfutil/rwUUID.h>

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
#include <cmath>     // fmod/sqrt/isfinite for TryGetEntityKinematics
#include <cstdio>      // snprintf (DescribeScriptVars value formatting)
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

    // Inverse of toGeocentric; used by DescribeScriptVars to read a location variable
    // back in the units the caller supplied it in.
    vrf::Geodetic toGeodetic(const DtVector& v) {
        DtGeodeticCoord geod;
        geod.setGeocentric(v);
        vrf::Geodetic g;
        g.latDeg = geod.lat() * kDegRadFactor;
        g.lonDeg = geod.lon() * kDegRadFactor;
        g.altMeters = geod.alt();
        return g;
    }

    // ONE place where a vrf::ScriptVar becomes a VR-Forces scripted-task variable.
    // Shared by RunScriptedTask (DtScriptedTaskTask), SendScriptedSet (DtScriptedTaskSet)
    // and DescribeScriptVars, so the self-test exercises the shipping path and not a copy.
    //
    // Every branch is the vendor's own DtScriptedTask::setValue overload with its DEFAULT
    // type constant (scriptedTaskTask.h:90-97; the constants in
    // vrfutil/vrfScriptedTasksConstants.h). That is the pattern MAK's own plugin sample
    // uses - examples/displayStateData/plugin.cxx:121-132 builds a DtScriptedTaskTask,
    // setScriptId("launch_flight_mission"), then setValue("Airbase", obj->uuid()) and
    // setValue("Loadout", "Intercept") with no explicit type argument.
    //
    // WHY NOT the old "variables().addVariable(new DtRw*)" form (which this replaces, and
    // which examples/addTask/addTaskGui/DtTaskRetreatDialog.cxx:64 still shows): that writes
    // the VALUE binding only and leaves variableDataTypes() empty, so the receiver gets no
    // scripted-task type for the variable. setValue writes both (scriptedTaskTask.h:140-146).
    // RunScriptedTask/SendScriptedSet had NO caller in src/VrfC2SimApp when this changed
    // (grep, 2026-09-14), so no existing verb's behaviour moves.
    void addScriptVar(DtScriptedTask& task, const vrf::ScriptVar& v) {
        const DtString name(v.name.c_str());
        switch (v.kind) {
            case vrf::ScriptVar::Kind::ObjectUuid:
                task.setValue(name, DtUUID(v.uuidValue));            // "simulationobject"
                break;
            case vrf::ScriptVar::Kind::Bool:
                task.setValue(name, v.boolValue);                    // "checkbox"
                break;
            case vrf::ScriptVar::Kind::Integer:
                task.setValue(name, v.intValue);                     // "integer"
                break;
            case vrf::ScriptVar::Kind::String:
                task.setValue(name, v.stringValue);                  // "string"
                break;
            case vrf::ScriptVar::Kind::Location:
                task.setValue(name, toGeocentric(v.locValue));       // "location"
                break;
            case vrf::ScriptVar::Kind::Real:
            default:
                task.setValue(name, v.realValue);                    // "double"
                break;
        }
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

    // DIAGNOSTIC-ONLY accessor (VrfFacade::HasDiscoveredObjects /
    // PrintReflectedObjectCounts). DtVrlinkVrfRemoteController keeps its network interface
    // in a PROTECTED member with no public getter (vrlinkVrfRemoteController.h:177 on 5.2d,
    // :175 on 5.0.2), and makVrf::DtRemoteObjectManager - the only public holder of
    // hasDiscoveredObjects() / printReflectedObjectCounts() - hangs off it
    // (vrlinkNetworkInterface.h:464 on 5.2d, :382 on 5.0.2; the network interface's own
    // printReflectedObjectCounts override is protected, :599 / :502). A derived class may
    // read its base's protected member; this changes no wiring. Null until an init() runs.
    makVrf::DtVrlinkNetworkInterface* diagNetworkInterface() const {
        return myVrlinkNetworkInterface;
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

    // STP-832. Vendor reason for the last Start() that failed; cleared at the top of every
    // Start() and NOT cleared by Stop(), so a caller may read it after resigning. See
    // VrfFacade::LastStartError() in the header.
    std::string lastStartError;

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

    // -- observation-channel diagnostics (no state of their own) ---------------
    // The controller Start() built, TYPED. Start() always constructs a
    // MyDtVrlinkVrfRemoteController and sets owns=true; StartAdopting() stores a caller's
    // controller of unknown concrete type and sets owns=false. So 'owns' already records
    // exactly when this cast is valid - Start() needs no extra bookkeeping for it.
    MyDtVrlinkVrfRemoteController* diagOwnController() const {
        return owns ? static_cast<MyDtVrlinkVrfRemoteController*>(controller) : nullptr;
    }

    // The remote object manager behind hasDiscoveredObjects() /
    // printReflectedObjectCounts(). Null when the controller is not ours, not started, or
    // was inited with disableRemoteDiscovery=true (vrlinkVrfRemoteController.h:92-93 says
    // the manager is then never created).
    makVrf::DtRemoteObjectManager* diagRemoteObjectManager() const {
        MyDtVrlinkVrfRemoteController* mine = diagOwnController();
        if (!mine) return nullptr;
        makVrf::DtVrlinkNetworkInterface* netIf = mine->diagNetworkInterface();
        return netIf ? netIf->remoteObjectManager() : nullptr;
    }
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
            // taskCompleteReport.h:84-90 - false means the task FAILED and is no longer
            // being processed. Read straight through; the vendor defaults it to true
            // (:87) so an old or minimal report still reads as a success.
            ev.success = tc->success();
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

// STP-832. DtExerciseConn::InitializationStatus as text. The two protocol builds of
// DtExerciseConn declare DIFFERENT enumerators (vl/exerciseConnHLA.h:92-102 vs
// vl/exerciseConnDIS.h:92-95, the same on VR-Link 5.8 and 5.10), so the mapping is per
// protocol. Only ever reached on Start()'s failure path.
static const char* connInitStatusName(DtExerciseConn::InitializationStatus s) {
    switch (s) {
    case DtExerciseConn::DtINIT_SUCCESS:                return "DtINIT_SUCCESS";
#if DtHLA
    case DtExerciseConn::DtCOULD_NOT_CREATE_RTIAMB:     return "DtCOULD_NOT_CREATE_RTIAMB";
    case DtExerciseConn::DtCOULD_NOT_CREATE_FEDEX:      return "DtCOULD_NOT_CREATE_FEDEX";
    case DtExerciseConn::DtCOULD_NOT_JOIN_FEDEX:        return "DtCOULD_NOT_JOIN_FEDEX";
    case DtExerciseConn::DtCOULD_NOT_OPEN_FED:          return "DtCOULD_NOT_OPEN_FED";
    case DtExerciseConn::DtCOULD_NOT_PARSE_FED:         return "DtCOULD_NOT_PARSE_FED";
    case DtExerciseConn::DtCOULD_NOT_FIND_FOMMAPPER:    return "DtCOULD_NOT_FIND_FOMMAPPER";
    case DtExerciseConn::DtERROR_OPENING_FOMMAPPER_LIB: return "DtERROR_OPENING_FOMMAPPER_LIB";
    case DtExerciseConn::DtCOULD_NOT_CONNECT_RTIAMB:    return "DtCOULD_NOT_CONNECT_RTIAMB";
#else
    case DtExerciseConn::DtCOULD_NOT_CREATE_SOCKET:     return "DtCOULD_NOT_CREATE_SOCKET";
#endif
    default: break;
    }
    return "unrecognized-InitializationStatus";
}

bool VrfFacade::Start(const StartupConfig& cfg) {
    p_->lastStartError.clear();   // STP-832: only the CURRENT call's failure may be reported
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
    // 5.2d: the network interface for UDP (best-effort) traffic - the 5.0.2 Launcher's
    // "Network Interface Address" (RESEARCH_52_HLA_CONNECTION_CONFIG_2026-09-04 P3). VR-Link
    // HLA takes --deviceAddress like DIS (UG52 Table 11 p180-181); the sim is launched with the
    // same value, so both ends' UDP legs sit on one interface. Default 127.0.0.1 (VrfFacade.h).
    if (!cfg.deviceAddress.empty()) { s.push_back("--deviceAddress"); s.push_back(cfg.deviceAddress); }
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
    // STP-832. Pass the vendor's InitializationStatus out-parameter. Two effects, both
    // required (vl/exerciseConnHLA.h:84-91 and :900-913): (1) DtFatalError - and so DtAbort,
    // which "exits the program" (vlutil/vlPrint.h:441-445) - is NOT called when the RTI
    // refuses the create or the join; (2) we can SEE the failure instead of running on a
    // connection the vendor documents as undefined. The vendor's own sample does exactly
    // this and lets the failed connection's destructor run on the error return
    // (vrlink5.10/examples/simple/listen.cxx:124-150; the f18 sample, :192-211, is the same
    // shape with a heap connection). SUCCESS PATH UNCHANGED: on DtINIT_SUCCESS the block
    // below is skipped and every statement that follows is the statement that was there
    // before - the only difference is that the constructor was handed somewhere to write
    // DtINIT_SUCCESS.
    DtExerciseConn::InitializationStatus connStatus = DtExerciseConn::DtINIT_SUCCESS;
    p_->exConn = new DtExerciseConn(*p_->appInit, &connStatus);
    if (connStatus != DtExerciseConn::DtINIT_SUCCESS) {
        std::string why = "VR-Link exercise connection init FAILED: ";
        why += connInitStatusName(connStatus);
        char code[24]; std::snprintf(code, sizeof(code), " (%d)", (int)connStatus);
        why += code;
        if (!cfg.federation.empty()) { why += ", federation '"; why += cfg.federation; why += "'"; }
#if DtHLA
        // rtiError() is a plain accessor over a DtString the connection filled while
        // initializing - "used to store the exceptions thrown by RTI during the connection
        // initialization stage" (exerciseConnHLA.h:410-412). Reading a member is not
        // "using" the unusable connection, and it is the ONLY place the RTI's own words
        // survive once DtFatalError is suppressed. Defensive catch: on the failure path a
        // throw here must not replace a clean false with an exception.
        try {
            DtString rtiErr = p_->exConn->rtiError();
            const char* txt = rtiErr.string();
            if (txt && *txt) {
                // rtiError() is MULTI-LINE (measured 2026-09-15: "... FOM Reader reports"
                // then "Bad FDD File. Could not find Document Root in FDD File."). Flatten
                // it so a consumer logs ONE line and a line-oriented reader gets all of it.
                std::string flat(txt);
                for (size_t i = 0; i < flat.size(); ++i)
                    if (flat[i] == '\r' || flat[i] == '\n' || flat[i] == '\t') flat[i] = ' ';
                while (!flat.empty() && flat[flat.size() - 1] == ' ') flat.erase(flat.size() - 1);
                if (!flat.empty()) { why += ": "; why += flat; }
            }
        } catch (...) { why += ": (rtiError() unavailable)"; }
#endif
        p_->lastStartError = why;
        // Release what THIS call allocated and leave the facade exactly as a never-started
        // one, so the caller can simply call Start() again (RtiProbe's retry loop does).
        // The controller is deleted too: its constructor opens a file-transporter receive
        // port (vrlinkVrfRemoteController.h:52-57), so leaking one per retry would leak a
        // socket per retry. Deletion order is newest-allocated first; the controller was
        // never init()ed with this connection, so the two are independent.
        delete newController;
        delete p_->exConn;   p_->exConn = nullptr;
        delete p_->appInit;  p_->appInit = nullptr;
        p_->controller = nullptr;
        p_->uuidMgr = nullptr;
        p_->owns = true;
        return false;
    }
#if VRF_API_52
    // 5.2: call the BASE init(DtExerciseConn*, rel, reel, ral, ael, marking,
    // disableRemoteDiscovery=false) - the overload the 5.2d sample uses
    // (examples\remoteControl\main.cxx:47-49). Its doc
    // (vrlinkVrfRemoteController.h :90-93): it creates the communication manager
    // AND, when disableRemoteDiscovery is false, the DtRemoteObjectManager that
    // discovers state data.
    // THIS IS PARITY WITH THE SAMPLE, NOT A FIX. The hypothesis that the derived
    // overload's not creating the DtRemoteObjectManager CAUSED observer blindness is
    // FALSIFIED: run 3811 (derived overload, disableRemoteDiscovery=false) and run 3812
    // (this base overload) both reported reflected=0
    // (docs/experiments/PREREG_52_TOOLJOIN_2026-09-03.md sec 6). Keeping or reverting
    // this line is the supervisor's call; it is recorded here so no later session reads
    // it as a settled cause. Qualified call = the base overload explicitly.
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

    // OPT-IN extended-data handshake lever (StartupConfig::disableWaitForVrfExtendedData,
    // default false). With the default this block executes NOTHING and Start() is the
    // pre-feature path statement for statement (2026-07-19 rule: native changes are
    // additive and opt-in). Set, it clears the entity list's myWaitForVrfExtendedData so
    // readyToAdd() stops withholding VR-Forces objects that have no VRF object data yet
    // (reflectedExtEntityList.h:74-80 / :163-170 on 5.2d). setPropertyPrototypes is
    // deliberately NOT called: we hold no prototypes to supply, and passing the empty ones
    // is exactly what the constructor already did (:37-39), so it would decode nothing new.
    if (cfg.disableWaitForVrfExtendedData && p_->uuidMgr) {
        if (DtReflectedExtEntityList* entityList = p_->uuidMgr->entityList())
            entityList->setWaitForVrfExtendedData(false);
    }

    return p_->controller != nullptr;
}

// STP-832. See the header. Empty unless the LAST Start() on this facade returned false.
std::string VrfFacade::LastStartError() const {
    return p_ ? p_->lastStartError : std::string();
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

// STP-809. THE MAPPING IS THE POINT OF THIS FUNCTION - see VrfFacade.h for the vendor trail
// and for what the answer does NOT say. Non-negative results are the vendor's own constants
// verbatim; the negative ones are ours.
int VrfFacade::BackendControlState() const {
    if (!p_ || !p_->controller) return BackendControlUnreadable;
    try {
        const int state = p_->controller->backendsControlState();
        if (state == DtPauseControlType) return BackendControlPaused;
        if (state == DtRunControlType)   return BackendControlRunning;
        if (state == DtUnknownControlType) {
            // The vendor folds "no back end exists" and "no back end has said anything" into
            // this one value (vrfRemoteController.h:320-323). Split them: that is the only
            // information added here, and backends().count() is what BackendCount() reads.
            return p_->controller->backends().count() <= 0
                       ? BackendControlNoBackend : BackendControlUnknown;
        }
        return BackendControlOther;   // RunDuration / RunComplete / Rewind / Step, or newer
    } catch (...) {
        return BackendControlUnreadable;   // no exception crosses the facade boundary
    }
}

// STP-809. -1 = no reading. 0 = the vendor positively reports that no KNOWN back end is
// simulatable or in transition, which is the discriminator BackendCount() cannot give:
// backends().count() keeps an entry doTimeouts() has deactivated.
int VrfFacade::ActiveBackendCount() const {
    if (!p_ || !p_->controller) return -1;
    try {
        DtVrfBackendListener* listener = p_->controller->backendListener();
        if (!listener) return -1;
        const DtList* list = listener->backendList();
        if (!list) return -1;
        int active = 0;
        // vlutil/vlList.h:66-69 - the documented DtList walk. The envelope holds void*; the
        // element type is the vendor's own ("a list of DtBackend objects for all known
        // backends", vrfBackendListener.h:92-93).
        for (DtListItem* item = list->first(); item; item = item->next()) {
            const DtBackend* be = static_cast<const DtBackend*>(item->data());
            if (!be) continue;
            // Transition states count as OPERATING on purpose (VrfFacade.h): a back end that
            // is loading or saving has a flat sim clock and is not dead.
            if (be->isInSimulatableState() || be->isInTransitionStatus()) ++active;
        }
        return active;
    } catch (...) {
        return -1;
    }
}

double VrfFacade::SimTimeSeconds() const {
    // -1.0 means "no reading" - see VrfFacade.h. The back-end gate is the point of this
    // function: simTime() with no address returns the FIRST back end's time, and with no back
    // end discovered there is no first one, so whatever it returns (0.0, most likely) would be
    // indistinguishable from a scenario legitimately sitting at t = 0.
    if (!p_ || !p_->controller) return -1.0;
    try {
        if (p_->controller->backends().count() <= 0) return -1.0;
        return p_->controller->simTime();
    } catch (...) {
        return -1.0;   // no exception crosses the facade boundary
    }
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

// -- observation-channel diagnostics ---------------------------------------------
// All read-only: they send nothing on the wire and register no callback, so a consumer
// that never calls them runs the unchanged path (2026-07-19 rule).

ReflectedListCounts VrfFacade::ReflectedCounts() const {
    // Every field stays -1 unless its list is actually reachable, so "no controller" and
    // "list is empty" can never be confused - the difference is the whole point here.
    ReflectedListCounts counts;
    if (!p_->controller) return counts;
    // The three lists the UUID network manager owns and exposes (UUIDNetworkManager.h
    // :123-125, identical on both stacks). count() is DtReflectedObjectList's
    // (vl/reflectedObjectListHLA.h:98); DtReflectedExtEntityList/DtReflectedExtAggregateList/
    // DtReflectedControlObjectList all derive from it through DtReflectedEntityList /
    // DtReflectedAggregateList / DtReflectedEnvironmentProcessList.
    if (makVrf::DtUUIDNetworkManager* mgr = p_->controller->uuidNetworkManager()) {
        if (DtReflectedExtEntityList* entityList = mgr->entityList()) {
            counts.entities = entityList->count();
            counts.waitingForVrfExtendedData = entityList->waitForVrfExtendedData();
        }
        if (DtReflectedExtAggregateList* aggregateList = mgr->aggregateList())
            counts.aggregates = aggregateList->count();
        if (DtReflectedControlObjectList* controlList = mgr->controlObjectList())
            counts.controlObjects = controlList->count();
    }
    // The environment-process list hangs off the controller, not the UUID manager
    // (vrlinkVrfRemoteController.h:143 on 5.2d, :141 on 5.0.2). It is the SAME OBJECT as
    // the control-object list read just above - VR-Forces publishes control objects as
    // environment processes and keeps one list for both. See ReflectedListCounts in
    // VrfFacade.h for the full header trail; the check below proves it at runtime instead
    // of trusting that reading.
    if (DtReflectedEnvironmentProcessList* envList = p_->controller->reflectedEnvironmentProcessList())
        counts.environmentProcesses = envList->count();
    // Pointer identity, not equal counts: two lists that merely HAPPEN to hold 19 objects
    // each would report 1 from a count comparison and 0 from this one. The control-object
    // pointer is upcast to its base (DtReflectedControlObjectList : public
    // DtReflectedEnvironmentProcessList, reflectedControlObjectList.h:24 on 5.2d, :23 on
    // 5.0.2) so the comparison is between like subobjects and not a raw address pun.
    // Both accessors are plain member returns, so calling them again costs nothing.
    {
        DtReflectedEnvironmentProcessList* envSide = p_->controller->reflectedEnvironmentProcessList();
        DtReflectedControlObjectList* ctlSide = 0;
        if (makVrf::DtUUIDNetworkManager* aliasMgr = p_->controller->uuidNetworkManager())
            ctlSide = aliasMgr->controlObjectList();
        if (envSide && ctlSide)
            counts.environmentAliasesControlObjects =
                (static_cast<DtReflectedEnvironmentProcessList*>(ctlSide) == envSide) ? 1 : 0;
    }
    // counts.extendedAttributes stays -1 - see the header for why it is unreachable.
    return counts;
}

int VrfFacade::HasDiscoveredObjects() const {
    makVrf::DtRemoteObjectManager* rom = p_->diagRemoteObjectManager();
    if (!rom) return -1;                       // unknown, NOT "no"
    return rom->hasDiscoveredObjects() ? 1 : 0;
}

void VrfFacade::PrintReflectedObjectCounts() const {
    if (makVrf::DtRemoteObjectManager* rom = p_->diagRemoteObjectManager())
        rom->printReflectedObjectCounts();
}

bool VrfFacade::HaveVrLinkLicense() { return DtHaveVrLinkLicense(); }
bool VrfFacade::HaveRtiLicense()    { return DtHaveRtiLicense(); }

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

void VrfFacade::CreateWaypoint(const Geodetic& pos, const std::string& name,
                               const std::string& uuid) {
    // Vendor signature (vrfRemoteController.h:999-1007): fcn, usr, geocentricPosition,
    // uniqueName, label, addr, startingUUID. The label and address keep their documented
    // defaults; only the uuid is new, and an empty one reproduces the pre-V3 call exactly.
    DtUUID startingUuid = uuid.empty() ? DtUUID::nullUUID() : DtUUID(uuid.c_str());
    p_->controller->createWaypoint(objectCreatedTrampoline, this,
        toGeocentric(pos), DtString(name.c_str()),
        DtString::nullString(), DtSimSendToAll, startingUuid);
}

void VrfFacade::CreateRoute(const std::vector<Geodetic>& points, const std::string& name,
                            const std::string& uuid) {
    // Vendor signature (vrfRemoteController.h:1032-1039): fcn, usr, vertices, uniqueName,
    // label, addr, startingUUID. As above: empty uuid == the pre-V3 call.
    DtList list;
    for (const Geodetic& g : points) list.add(new DtVector(toGeocentric(g)));
    DtUUID startingUuid = uuid.empty() ? DtUUID::nullUUID() : DtUUID(uuid.c_str());
    p_->controller->createRoute(objectCreatedTrampoline, this, list, DtString(name.c_str()),
        DtString::nullString(), DtSimSendToAll, startingUuid);
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

void VrfFacade::AddToOrganization(const std::string& childUuid, const std::string& superiorUuid) {
    // vrfRemoteController.h:1334-1339 - addToOrganization(objectId, newSuperiorId): the child is
    // detached from any prior superior first. Child may be an entity OR an aggregate (nested).
    p_->controller->addToOrganization(DtUUID(childUuid), DtUUID(superiorUuid));
}

void VrfFacade::SetObjectNotifyLevel(const std::string& uuid, int notifyLevel) {
    // vrfRemoteController.h:1953 - setObjectNotifyLevel(objectName, DtNotifyLevelType, addr).
    // DtNotifyLevelType (vlutil/vlPrint.h:39-46): DtNlFatal 0 .. DtNlDebug 4; clamp, never cast
    // an out-of-range int into the enum.
    if (notifyLevel < 0) notifyLevel = 0;
    if (notifyLevel > 4) notifyLevel = 4;
    p_->controller->setObjectNotifyLevel(DtUUID(uuid), static_cast<DtNotifyLevelType>(notifyLevel));
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
    for (const ScriptVar& v : vars) addScriptVar(task, v);
    p_->controller->sendTaskMsg(DtUUID(uuid), &task);
}

void VrfFacade::SendScriptedSet(const std::string& uuid, const std::string& scriptId,
                                const std::vector<ScriptVar>& vars) {
    DtScriptedTaskSet set;
    set.init();
    set.setScriptId(scriptId.c_str());
    for (const ScriptVar& v : vars) addScriptVar(set, v);
    p_->controller->sendSetDataMsg(DtUUID(uuid), &set, DtSimSendToAll);
}

std::vector<std::string> VrfFacade::DescribeScriptVars(const std::vector<ScriptVar>& vars) {
    // No controller, no federation, nothing sent: a DtScriptedTaskTask is a plain
    // reader/writer object (scriptedTaskTask.h:275 DtScriptedTaskTemplate<DtSimTask>).
    DtScriptedTaskTask task;
    task.init();
    task.setScriptId("selftest");
    for (const ScriptVar& v : vars) addScriptVar(task, v);

    std::vector<std::string> out;
    out.reserve(vars.size());
    for (const ScriptVar& v : vars) {
        const DtString name(v.name.c_str());
        const DtReaderWriter* rw = task.variables().findVariableBinding(name);
        std::string rwType = "?";
        std::string value  = "?";
        if (rw) {
            const char* t = rw->readerWriterType();          // readerWriter.h:361
            if (t) rwType = t;
            // Decode from the CONCRETE reader/writer the vendor chose, so a wrong
            // overload shows up as a cast miss rather than a plausible-looking value.
            if (const DtRwObjectName* u = dynamic_cast<const DtRwObjectName*>(rw)) {
                value = std::string(u->uuidString().c_str());  // rwUUID.h:82
            } else if (const DtRwBoolean* b = dynamic_cast<const DtRwBoolean*>(rw)) {
                value = b->value() ? "true" : "false";          // rwBoolean.h:90
            } else if (const DtRwInt* i = dynamic_cast<const DtRwInt*>(rw)) {
                value = std::to_string(i->value());             // rwInt.h:104
            } else if (const DtRwString* s = dynamic_cast<const DtRwString*>(rw)) {
                value = std::string(s->c_str());                // DtRwString IS a DtString
            } else if (const DtRwVector* p = dynamic_cast<const DtRwVector*>(rw)) {
                Geodetic g = toGeodetic(*p);                    // rwVector.h:23 IS a DtVector
                char buf[128];
                std::snprintf(buf, sizeof(buf), "%.6f,%.6f,%.3f", g.latDeg, g.lonDeg, g.altMeters);
                value = buf;
            } else if (const DtRwReal* r = dynamic_cast<const DtRwReal*>(rw)) {
                // AFTER DtRwVector: both are numeric, but DtRwVector is not a DtRwReal,
                // so order only matters against future numeric subclasses.
                char buf[64];
                std::snprintf(buf, sizeof(buf), "%.6f", (double)r->value());  // rwReal.h:103
                value = buf;
            }
        }
        // The scripted-task data type setValue recorded for this variable, read from the
        // SECOND binding set (scriptedTaskTask.h:60-63 variableDataTypes()).
        std::string dataType = "?";
        if (const DtReaderWriter* dt = task.variableDataTypes().findVariableBinding(name))
            if (const DtRwString* s = dynamic_cast<const DtRwString*>(dt))
                dataType = std::string(s->c_str());
        out.push_back(v.name + "|" + rwType + "|" + dataType + "|" + value);
    }
    return out;
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

// Resolve the base state repository of a reflected object from EITHER an entity or an
// aggregate. The C++ oracle getUnitGeodeticFromSim static_cast'd every reflected object to
// DtReflectedEntity* (wrong-type UB for an aggregate, but it happened to yield a usable
// location, so the disaggregated aggregate 11.MechBn moved - PORT.md sec 5/8). This port
// handles the aggregate case PROPERLY: DtReflectedAggregate exposes aggregateStateRep(),
// whose DtAggregateStateRepository shares DtBaseEntityStateRepository::location() with an
// entity's DtEntityStateRepository - so both paths return the same geocentric vector.
// Without this, dynamic_cast<DtReflectedEntity*> returns null for an aggregate and the
// caller ABANDONS the task, breaking the golden aggregate-move.
//
// Extracted (B7) so TryGetEntityGeodetic and TryGetEntityKinematics resolve a unit through
// ONE rule: a position and a heading/speed that disagreed about which repository to read
// would be a silent, untestable inconsistency in the same position report.
static DtBaseEntityStateRepository* stateRepOf(DtReflectedObject* obj) {
    if (!obj) return nullptr;
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
    return sr;
}

bool VrfFacade::TryGetEntityGeodetic(const std::string& uuid, Geodetic& out) const {
    if (!p_->uuidMgr) return false;
    DtReflectedObject* obj = p_->uuidMgr->reflectedObjectFor(DtUUID(uuid));
    if (!obj) return false;

    DtBaseEntityStateRepository* sr = stateRepOf(obj);
    if (!sr) return false;
    DtVector geoLocation = sr->location();
    DtGeodeticCoord geod;
    geod.setGeocentric(geoLocation);
    out.latDeg = geod.lat() * kDegRadFactor;
    out.lonDeg = geod.lon() * kDegRadFactor;
    out.altMeters = geod.alt();
    return true;
}

bool VrfFacade::TryGetEntityKinematics(const std::string& uuid,
                                       double& speedMps, double& headingDeg) const {
    if (!p_->uuidMgr) return false;
    DtReflectedObject* obj = p_->uuidMgr->reflectedObjectFor(DtUUID(uuid));
    if (!obj) return false;

    // SAME resolution rule as TryGetEntityGeodetic (see stateRepOf): entity, aggregate,
    // then the oracle's static_cast fallback. location(), velocity() and orientation() are
    // all DtBaseEntityStateRepository members, so one repository serves all three and the
    // three reads describe ONE instant of the object's state.
    DtBaseEntityStateRepository* sr = stateRepOf(obj);
    if (!sr) return false;

    const DtVector geoLocation = sr->location();

    // HEADING - vendor helper, geocentric location + geocentric Euler angles -> radians
    // true heading (matrix/topoCoord.h:46-49). It does the topographic transformation
    // itself, so there is no rotation to hand-roll here.
    double heading = DtGetHeadingFromGeocentric(geoLocation, sr->orientation()) * kDegRadFactor;
    // The helper returns the topographic yaw, which is an Euler angle and so is expected to
    // be signed (about (-180, +180]) - but that range is ASSUMED, not documented in the
    // header, so the fold below is written to be range-agnostic: it normalises ANY finite
    // input into [0, 360), which is what C2SIM HeadingAngle means ("degrees where north is
    // zero", no negative convention). The final line folds -0.0 to +0.0 so a due-north
    // heading serializes as "0" rather than "-0".
    heading = std::fmod(heading, 360.0);
    if (heading < 0.0) heading += 360.0;
    if (heading == 0.0) heading = 0.0;

    // GROUND SPEED - velocity() is m/s in GEOCENTRIC world coordinates
    // (baseEntityStateRepository.h:64-65, 121-128). Rotate it into this object's local
    // topographic frame with the vendor's own matrix, applied exactly as topoCoord.h:33-37
    // documents the call, then take the HORIZONTAL magnitude: the topographic frame is
    // X=north, Y=east, Z=down (topoCoord.h:20-23), so the vertical rate is z() and dropping
    // it leaves ground speed (a climbing/descending vehicle does not inflate its speed).
    DtGeodeticCoord geod;
    geod.setGeocentric(geoLocation);
    DtDcm geocToTopo;
    DtLatLon_to_GeocToTopo(geod, geocToTopo);
    DtVector32 vTopo;
    DtDcmVecMul(geocToTopo, sr->velocity(), vTopo);
    const double north = (double)vTopo.x(), east = (double)vTopo.y();
    double speed = std::sqrt(north * north + east * east);

    // A non-finite read is a FAILED read, not a zero: the caller omits the fields rather
    // than reporting a unit as stationary and pointing north (never send a default 0).
    if (!std::isfinite(speed) || !std::isfinite(heading)) return false;
    speedMps = speed;
    headingDeg = heading;
    return true;
}

} // namespace vrf
