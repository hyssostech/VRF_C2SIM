/*----------------------------------------------------------------*
|  VrfBridge.cpp                                                   |
|                                                                  |
|  C++/CLI (/clr:netcore) managed wrapper over the pure-native     |
|  vrf::VrfFacade. This is the ONLY managed translation unit; the  |
|  facade + remoteControlInit compile as native (see the vcxproj), |
|  so no boost/MAK type crosses the managed boundary - only POD    |
|  marshalled through msclr.                                       |
|                                                                  |
|  Slice 1: the OUTBOUND path (lifecycle + create + task).         |
|  Slice 2: INBOUND callbacks - the facade's std::function members |
|  are wired (in the ctor, before Start) to managed events via a   |
|  native lambda capturing a gcroot back to this bridge. Dispatch  |
|  is SYNCHRONOUS on the VR-Forces tick thread (Phase 1 parity); a |
|  future step marshals off-thread. Because the native facade      |
|  holds a gcroot to this managed object, the consumer MUST Dispose |
|  the bridge (the .NET app owns one long-lived bridge and does).  |
*-----------------------------------------------------------------*/

#include "VrfFacade.h"

#include <msclr/marshal_cppstd.h>
#include <msclr/gcroot.h>
#include <string>

using namespace System;
using namespace System::Collections::Generic;
using namespace msclr::interop;

namespace VrfC2Sim {

// -- POD mirrors of the facade's boundary types (managed) ------------

public enum class VrfProtocol { Dis, Hla1516e };
public enum class Force { Friendly, Opposing, Neutral };

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

    // -- object creation (async; completion via the ObjectCreated event) --
    void CreateEntity(EntityTypeSpec type, Geodetic pos, Force force,
                      double headingDeg, String^ name) {
        _facade->CreateEntity(ToNative(type), ToNative(pos), ToNative(force),
                              headingDeg, ToStd(name));
    }

    // -- tasking -----------------------------------------------------
    void MoveAlongRoute(String^ uuid, String^ routeUuid) {
        _facade->MoveAlongRoute(ToStd(uuid), ToStd(routeUuid));
    }

    void SetAggregateFormation(String^ uuid, String^ formationName) {
        _facade->SetAggregateFormation(ToStd(uuid), ToStd(formationName));
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
