/*----------------------------------------------------------------*
|  VrfBridge.cpp                                                   |
|                                                                  |
|  C++/CLI (/clr:netcore) managed wrapper over the pure-native     |
|  vrf::VrfFacade. This is the ONLY managed translation unit; the  |
|  facade + remoteControlInit compile as native (see the vcxproj), |
|  so no boost/MAK type crosses the managed boundary - only POD    |
|  marshalled through msclr.                                       |
|                                                                  |
|  Phase 2 slice 1: the OUTBOUND path (lifecycle + a representative |
|  create + task) to prove the facade compiles + links as a        |
|  /clr:netcore DLL under the HLA1516e MAK set. Inbound callbacks   |
|  (facade std::function -> managed events) land in slice 2.       |
*-----------------------------------------------------------------*/

#include "VrfFacade.h"

#include <msclr/marshal_cppstd.h>
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

// -- The managed bridge ----------------------------------------------

public ref class VrfBridge {
public:
    VrfBridge() : _facade(new vrf::VrfFacade()) {}

    // Dispose -> finalizer -> native teardown. vrf::~VrfFacade() calls Stop().
    ~VrfBridge() { this->!VrfBridge(); }
    !VrfBridge() {
        if (_facade) { delete _facade; _facade = nullptr; }
    }

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

    // -- object creation (async; completion via callbacks, slice 2) --
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

private:
    vrf::VrfFacade* _facade;

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

} // namespace VrfC2Sim
