/*----------------------------------------------------------------*
|  VrfBridge.cpp                                                   |
|                                                                  |
|  C++/CLI (/clr:netcore) managed wrapper over the pure-native     |
|  vrf::VrfFacade. This is the ONLY managed translation unit; the  |
|  facade + remoteControlInit compile as native (see the vcxproj), |
|  so no boost/MAK type crosses the managed boundary - only POD    |
|  marshalled through msclr.                                       |
|                                                                  |
|  Surface: the full vrf::VrfFacade command + observation API      |
|  (lifecycle, sim control, create*, setters, tasking, scripted    |
|  tasks, state read) plus the 4 inbound events. Transition-only   |
|  facade members (StartAdopting, GetController, RegisterInbound)  |
|  are intentionally NOT exposed - the port owns the controller via |
|  Start.                                                          |
|                                                                  |
|  Callbacks: the facade's std::function members are wired (ctor,   |
|  before Start) to managed events via namespace-scope native       |
|  functor thunks holding a gcroot (a managed member may not define  |
|  a lambda - C3923). Dispatch is SYNCHRONOUS on the VRF tick        |
|  thread (Phase 1 parity). The facade holds a gcroot to this       |
|  object, so the consumer MUST Dispose the bridge.                 |
*-----------------------------------------------------------------*/

#include "VrfFacade.h"

#include <msclr/marshal_cppstd.h>
#include <msclr/gcroot.h>
#include <string>
#include <vector>

using namespace System;
using namespace System::Collections::Generic;
using namespace msclr::interop;

namespace VrfC2Sim {

// -- POD mirrors of the facade's boundary types (managed) ------------

public enum class VrfProtocol { Dis, Hla1516e };
public enum class Force { Friendly, Opposing, Neutral };
public enum class AggregateState { Aggregated, Disaggregated };
public enum class Roe { FireAtWill, HoldFire, FireWhenFiredUpon };
public enum class ScriptVarKind { ObjectUuid, Real };

public value struct Geodetic {
    double LatDeg;
    double LonDeg;
    double AltMeters;
};

public value struct EntityTypeSpec {
    int Kind;
    int Domain;
    int Country;
    int Category;
    int Subcategory;
    int Specific;
    int Extra;
};

// One variable of a VR-Forces scripted (Lua) task/set. Mirrors vrf::ScriptVar.
public value struct ScriptVar {
    ScriptVarKind Kind;
    String^ Name;
    String^ UuidValue; // used when Kind == ObjectUuid
    double  RealValue; // used when Kind == Real

    static ScriptVar Object(String^ name, String^ uuid) {
        ScriptVar v; v.Kind = ScriptVarKind::ObjectUuid; v.Name = name; v.UuidValue = uuid; return v;
    }
    static ScriptVar Number(String^ name, double value) {
        ScriptVar v; v.Kind = ScriptVarKind::Real; v.Name = name; v.RealValue = value; return v;
    }
};

public ref class StartupConfig {
public:
    VrfProtocol Protocol;
    int ApplicationNumber;
    int SiteId;
    int SessionId;
    String^ HostInetAddr;
    // DIS
    String^ DeviceAddress;
    int DisVersion;
    int DisPort;
    // HLA 1516e
    String^ Federation;
    String^ FedFileName;
    List<String^>^ FomModules;
    String^ RprFomVersion;

    StartupConfig() {
        // Defaults mirror vrf::StartupConfig
        Protocol = VrfProtocol::Hla1516e;
        ApplicationNumber = 3201;
        SiteId = 1;
        SessionId = 1;
        HostInetAddr = "127.0.0.1";
        DeviceAddress = "127.0.0.1";
        DisVersion = 7;
        DisPort = 3000;
        FomModules = gcnew List<String^>();
        RprFomVersion = "2.0";
    }
};

// -- Event payloads (managed) ----------------------------------------

public ref class ObjectCreatedEventArgs : EventArgs {
public:
    property String^ Name;      // the unique name passed to Create*
    property String^ EntityId;  // DtEntityIdentifier string
    property String^ Uuid;      // VRF UUID string (SetAltitude / tasking use this)
};

public ref class TextReportEventArgs : EventArgs {
public:
    property String^ Text;      // raw VR-Forces radio text-report
};

public ref class TaskCompletedEventArgs : EventArgs {
public:
    property String^ UnitMarking; // transmitter markingText
    property String^ TaskType;    // e.g. "move-along"
};

// -- The managed bridge ----------------------------------------------

public ref class VrfBridge {
public:
    VrfBridge() : _facade(new vrf::VrfFacade()) {
        WireCallbacks(); // set the facade's std::function members BEFORE Start
    }

    // Dispose -> finalizer -> native teardown. vrf::~VrfFacade() calls Stop().
    // This also destroys the facade's std::functions, releasing the gcroots to
    // this object (breaks the native->managed reference held for callbacks).
    ~VrfBridge() { this->!VrfBridge(); }
    !VrfBridge() {
        if (_facade) { delete _facade; _facade = nullptr; }
    }

    // -- inbound events (fire on the VRF tick thread; see file header) --
    event EventHandler<ObjectCreatedEventArgs^>^ ObjectCreated;
    event EventHandler<TextReportEventArgs^>^    TextReport;
    event EventHandler<TaskCompletedEventArgs^>^ TaskCompleted;
    event EventHandler^                          ScenarioClosed;

    // -- lifecycle ---------------------------------------------------
    bool Start(StartupConfig^ cfg) {
        vrf::StartupConfig n;
        n.protocol = (cfg->Protocol == VrfProtocol::Hla1516e)
                         ? vrf::Protocol::HLA1516e : vrf::Protocol::DIS;
        n.applicationNumber = cfg->ApplicationNumber;
        n.siteId = cfg->SiteId;
        n.sessionId = cfg->SessionId;
        n.hostInetAddr = ToStd(cfg->HostInetAddr);
        n.deviceAddress = ToStd(cfg->DeviceAddress);
        n.disVersion = cfg->DisVersion;
        n.disPort = cfg->DisPort;
        n.federation = ToStd(cfg->Federation);
        n.fedFileName = ToStd(cfg->FedFileName);
        if (cfg->FomModules != nullptr) {
            for each (String ^ m in cfg->FomModules)
                n.fomModules.push_back(ToStd(m));
        }
        if (cfg->RprFomVersion != nullptr)
            n.rprFomVersion = ToStd(cfg->RprFomVersion);
        return _facade->Start(n);
    }

    void Stop() { if (_facade) _facade->Stop(); }
    void Tick() { _facade->Tick(); }

    int  BackendCount()     { return _facade->BackendCount(); }
    bool AllBackendsReady() { return _facade->AllBackendsReady(); }

    // -- scenario / simulation control -------------------------------
    void Run()   { _facade->Run(); }
    void Pause() { _facade->Pause(); }
    void SetTimeMultiplier(int multiple) { _facade->SetTimeMultiplier(multiple); }
    void SetExerciseStartTime(int year, int month, int day,
                              int hour, int minute, int second) {
        _facade->SetExerciseStartTime(year, month, day, hour, minute, second);
    }

    // -- object creation (async; completion via the ObjectCreated event) --
    void CreateEntity(EntityTypeSpec type, Geodetic pos, Force force,
                      double headingDeg, String^ name) {
        _facade->CreateEntity(ToNative(type), ToNative(pos), ToNative(force),
                              headingDeg, ToStd(name));
    }

    // Convenience overload: the interface always uses Disaggregated + subordinates.
    void CreateAggregate(EntityTypeSpec type, Geodetic pos, Force force,
                         double headingDeg, String^ name) {
        CreateAggregate(type, pos, force, headingDeg, name,
                        AggregateState::Disaggregated, true);
    }
    void CreateAggregate(EntityTypeSpec type, Geodetic pos, Force force,
                         double headingDeg, String^ name,
                         AggregateState state, bool createSubordinates) {
        _facade->CreateAggregate(ToNative(type), ToNative(pos), ToNative(force),
                                 headingDeg, ToStd(name), ToNative(state),
                                 createSubordinates);
    }

    void CreateWaypoint(Geodetic pos, String^ name) {
        _facade->CreateWaypoint(ToNative(pos), ToStd(name));
    }

    // Delete a VR-Forces object by VRF uuid (counterpart to Create*; lets the app clean up
    // everything it created so objects don't accumulate across runs - RUNBOOK sec 7/8).
    void DeleteObject(String^ uuid) { _facade->DeleteObject(ToStd(uuid)); }

    // Reflected-object enumeration for a hard VR-Forces reset (RUNBOOK sec 8). Call
    // BeginTrackingReflectedObjects() right after Start() and before ticking; Tick() a few
    // seconds so discovery completes; then GetAllReflectedUuids() returns every present
    // object's uuid to DeleteObject(). Reaches ORPHANS that Solution A cannot (tools/ResetVrf).
    void BeginTrackingReflectedObjects() { _facade->BeginTrackingReflectedObjects(); }
    IEnumerable<String^>^ GetAllReflectedUuids() {
        std::vector<std::string> v = _facade->GetAllReflectedUuids();
        auto list = gcnew List<String^>((int)v.size());
        for (const std::string& s : v) list->Add(marshal_as<String^>(s));
        return list;
    }

    void CreateRoute(IEnumerable<Geodetic>^ points, String^ name) {
        _facade->CreateRoute(ToNativePoints(points), ToStd(name));
    }

    // uuid empty -> nullUUID. The C2SIM interface assigns the area's C2SIM uuid.
    void CreateControlArea(IEnumerable<Geodetic>^ perimeter, String^ name, String^ label) {
        CreateControlArea(perimeter, name, label, nullptr);
    }
    void CreateControlArea(IEnumerable<Geodetic>^ perimeter, String^ name,
                           String^ label, String^ uuid) {
        _facade->CreateControlArea(ToNativePoints(perimeter), ToStd(name),
                                   ToStd(label), ToStd(uuid));
    }

    // -- attribute setters -------------------------------------------
    void SetAltitude(String^ uuid, double altitudeMeters) {
        _facade->SetAltitude(ToStd(uuid), altitudeMeters);
    }
    void SetLocation(String^ uuid, Geodetic pos) { // magic move
        _facade->SetLocation(ToStd(uuid), ToNative(pos));
    }
    void SetTarget(String^ uuid, String^ targetUuid) {
        _facade->SetTarget(ToStd(uuid), ToStd(targetUuid));
    }
    void SetRulesOfEngagement(String^ uuid, Roe roe) {
        _facade->SetRulesOfEngagement(ToStd(uuid), ToNative(roe));
    }

    // -- tasking -----------------------------------------------------
    void MoveToLocation(String^ uuid, Geodetic pos) {
        _facade->MoveToLocation(ToStd(uuid), ToNative(pos));
    }
    void MoveAlongRoute(String^ uuid, String^ routeUuid) {
        _facade->MoveAlongRoute(ToStd(uuid), ToStd(routeUuid));
    }
    void SetAggregateFormation(String^ uuid, String^ formationName) {
        _facade->SetAggregateFormation(ToStd(uuid), ToStd(formationName));
    }
    // Fire at the target entity (DtFireAtTargetTask). Auto-selects the weapon, default round
    // count. Layer 2 of the semantic map: the ATTACK-family verbs map here.
    void FireAtTarget(String^ uuid, String^ targetUuid) {
        _facade->FireAtTarget(ToStd(uuid), ToStd(targetUuid));
    }
    void RunScriptedTask(String^ uuid, String^ scriptId, IEnumerable<ScriptVar>^ vars) {
        _facade->RunScriptedTask(ToStd(uuid), ToStd(scriptId), ToNativeVars(vars));
    }
    void SendScriptedSet(String^ uuid, String^ scriptId, IEnumerable<ScriptVar>^ vars) {
        _facade->SendScriptedSet(ToStd(uuid), ToStd(scriptId), ToNativeVars(vars));
    }

    // -- state read (pure; does NOT task the unit) -------------------
    // Returns false (and a zeroed result) if no reflected entity exists for the
    // uuid (e.g. an aggregate, which has no DtReflectedEntity).
    bool TryGetEntityGeodetic(String^ uuid,
                              [System::Runtime::InteropServices::Out] Geodetic% result) {
        vrf::Geodetic n;
        bool ok = _facade->TryGetEntityGeodetic(ToStd(uuid), n);
        Geodetic g;
        g.LatDeg = n.latDeg; g.LonDeg = n.lonDeg; g.AltMeters = n.altMeters;
        result = g;
        return ok;
    }

internal:
    // Called from the native callback thunks (below); construct args + raise the event.
    void RaiseObjectCreated(String^ name, String^ entityId, String^ uuid) {
        auto e = gcnew ObjectCreatedEventArgs();
        e->Name = name; e->EntityId = entityId; e->Uuid = uuid;
        ObjectCreated(this, e);
    }
    void RaiseTextReport(String^ text) {
        auto e = gcnew TextReportEventArgs();
        e->Text = text;
        TextReport(this, e);
    }
    void RaiseTaskCompleted(String^ unitMarking, String^ taskType) {
        auto e = gcnew TaskCompletedEventArgs();
        e->UnitMarking = unitMarking; e->TaskType = taskType;
        TaskCompleted(this, e);
    }
    void RaiseScenarioClosed() {
        ScenarioClosed(this, EventArgs::Empty);
    }

private:
    vrf::VrfFacade* _facade;

    // Point the facade's std::function members at the native thunks (defined at
    // namespace scope below - a managed member function may not define a local
    // class, i.e. a lambda: error C3923). Defined out-of-line after the thunks.
    void WireCallbacks();

    // std::string <- String^ (null-safe: nullptr -> empty)
    static std::string ToStd(String^ s) {
        if (s == nullptr) return std::string();
        return marshal_as<std::string>(s);
    }
    static vrf::Force ToNative(Force f) {
        switch (f) {
            case Force::Opposing: return vrf::Force::Opposing;
            case Force::Neutral:  return vrf::Force::Neutral;
            default:              return vrf::Force::Friendly;
        }
    }
    static vrf::AggregateState ToNative(AggregateState s) {
        return (s == AggregateState::Aggregated)
                   ? vrf::AggregateState::Aggregated
                   : vrf::AggregateState::Disaggregated;
    }
    static vrf::Roe ToNative(Roe r) {
        switch (r) {
            case Roe::HoldFire:           return vrf::Roe::HoldFire;
            case Roe::FireWhenFiredUpon:  return vrf::Roe::FireWhenFiredUpon;
            default:                      return vrf::Roe::FireAtWill;
        }
    }
    static vrf::Geodetic ToNative(Geodetic g) {
        vrf::Geodetic n;
        n.latDeg = g.LatDeg; n.lonDeg = g.LonDeg; n.altMeters = g.AltMeters;
        return n;
    }
    static vrf::EntityTypeSpec ToNative(EntityTypeSpec t) {
        vrf::EntityTypeSpec n;
        n.kind = t.Kind; n.domain = t.Domain; n.country = t.Country;
        n.category = t.Category; n.subcategory = t.Subcategory;
        n.specific = t.Specific; n.extra = t.Extra;
        return n;
    }
    static std::vector<vrf::Geodetic> ToNativePoints(IEnumerable<Geodetic>^ pts) {
        std::vector<vrf::Geodetic> out;
        if (pts != nullptr) for each (Geodetic g in pts) out.push_back(ToNative(g));
        return out;
    }
    static std::vector<vrf::ScriptVar> ToNativeVars(IEnumerable<ScriptVar>^ vars) {
        std::vector<vrf::ScriptVar> out;
        if (vars != nullptr) for each (ScriptVar v in vars) {
            if (v.Kind == ScriptVarKind::ObjectUuid)
                out.push_back(vrf::ScriptVar::Object(ToStd(v.Name), ToStd(v.UuidValue)));
            else
                out.push_back(vrf::ScriptVar::Number(ToStd(v.Name), v.RealValue));
        }
        return out;
    }
};

// -- native callback thunks ------------------------------------------
// Namespace-scope native functors (NOT members of the managed class, so no
// C3923) each holding a gcroot to the bridge. Assigned to the facade's
// std::function members; when the facade fires them on the VRF tick thread they
// marshal the POD payload and raise the managed event. gcroot keeps the bridge
// alive for the facade's lifetime - hence the Dispose requirement (file header).
namespace {

struct ObjectCreatedThunk {
    msclr::gcroot<VrfBridge^> self;
    void operator()(const vrf::ObjectCreated& o) const {
        self->RaiseObjectCreated(marshal_as<String^>(o.name),
                                 marshal_as<String^>(o.entityId),
                                 marshal_as<String^>(o.uuid));
    }
};

struct TextReportThunk {
    msclr::gcroot<VrfBridge^> self;
    void operator()(const vrf::TextReport& t) const {
        self->RaiseTextReport(marshal_as<String^>(t.text));
    }
};

struct TaskCompletedThunk {
    msclr::gcroot<VrfBridge^> self;
    void operator()(const vrf::TaskCompleted& t) const {
        self->RaiseTaskCompleted(marshal_as<String^>(t.unitMarking),
                                 marshal_as<String^>(t.taskType));
    }
};

struct ScenarioClosedThunk {
    msclr::gcroot<VrfBridge^> self;
    void operator()() const {
        self->RaiseScenarioClosed();
    }
};

} // anonymous namespace

void VrfBridge::WireCallbacks() {
    _facade->OnObjectCreated  = ObjectCreatedThunk{ msclr::gcroot<VrfBridge^>(this) };
    _facade->OnTextReport     = TextReportThunk{ msclr::gcroot<VrfBridge^>(this) };
    _facade->OnTaskCompleted  = TaskCompletedThunk{ msclr::gcroot<VrfBridge^>(this) };
    _facade->OnScenarioClosed = ScenarioClosedThunk{ msclr::gcroot<VrfBridge^>(this) };
}

} // namespace VrfC2Sim
