/*----------------------------------------------------------------*
|  VrfFacade.h                                                     |
|                                                                  |
|  A pure-native C++ boundary around the MAK VR-Forces remote      |
|  control API. It exposes ONLY std:: and POD types - no Dt* MAK   |
|  types appear here - so a C++/CLI (.NET) layer, or an            |
|  out-of-process adapter, can consume it without seeing the       |
|  boost-heavy MAK headers. Every MAK type stays inside            |
|  VrfFacade.cpp behind a pimpl.                                   |
|                                                                  |
|  Phase 1 goal: the existing C2SIM interface is rebuilt on top    |
|  of this facade with behavior unchanged (verified against the    |
|  golden trace). No C2SIM / STOMP / Xerces logic lives here -     |
|  this is strictly the VR-Forces control + observation surface    |
|  the interface actually uses at runtime.                         |
*-----------------------------------------------------------------*/

#pragma once

#include <string>
#include <vector>
#include <functional>

namespace vrf {

// ------------------------------------------------------------------
// POD value types (degrees / metres; no MAK types)
// ------------------------------------------------------------------

// Geodetic position. Latitude/longitude in DEGREES, altitude in metres.
// The facade converts to/from MAK's geocentric DtVector internally.
struct Geodetic {
    double latDeg = 0.0;
    double lonDeg = 0.0;
    double altMeters = 0.0;
};

// DIS entity-type 7-tuple (kind, domain, country, category,
// subcategory, specific, extra) - exactly the DtEntityType arguments.
struct EntityTypeSpec {
    int kind = 0, domain = 0, country = 0, category = 0,
        subcategory = 0, specific = 0, extra = 0;
};

enum class Force { Friendly, Opposing, Neutral };

enum class Roe { FireAtWill, HoldFire, FireWhenFiredUpon };

// How an aggregate is created. The current interface always uses
// Disaggregated + createSubordinates=true; both are exposed here so the
// port can change them without editing the facade (see golden-trace note
// on aggregate movement).
enum class AggregateState { Aggregated, Disaggregated };

// One variable of a VR-Forces scripted task (Lua). Either an object
// reference (by UUID) or a real number, matching DtRwObjectName / DtRwReal.
struct ScriptVar {
    enum class Kind { ObjectUuid, Real } kind = Kind::Real;
    std::string name;        // e.g. "pickupPoint", "altitudeAgl"
    std::string uuidValue;   // used when kind == ObjectUuid
    double      realValue = 0.0; // used when kind == Real

    static ScriptVar Object(const std::string& n, const std::string& uuid) {
        ScriptVar v; v.kind = Kind::ObjectUuid; v.name = n; v.uuidValue = uuid; return v;
    }
    static ScriptVar Number(const std::string& n, double val) {
        ScriptVar v; v.kind = Kind::Real; v.name = n; v.realValue = val; return v;
    }
};

// ------------------------------------------------------------------
// Startup configuration (replaces the synthetic vrfArgv[] in main.cxx)
// ------------------------------------------------------------------

enum class Protocol { DIS, HLA1516e };

struct StartupConfig {
    Protocol protocol = Protocol::HLA1516e;

    // Common
    int applicationNumber = 3201;
    int siteId = 1;
    int sessionId = 1;
    std::string hostInetAddr = "127.0.0.1"; // controller host address (setHostInetAddr)

    // DIS
    std::string deviceAddress = "127.0.0.1"; // --deviceAddress (broadcast/loopback)
    int disVersion = 7;
    int disPort = 3000;

    // HLA 1516e
    std::string federation;                  // --execName
    std::string fedFileName;                 // --fedFileName (full path)
    std::vector<std::string> fomModules;     // --fomModules (full paths, in order)
    std::string rprFomVersion = "2.0";

    // 5.2 build axis only (VRF_API_52): the VR-Link connection config file the
    // initializer loads first (MAK-ONE-2025-Config.xml). Empty = resolve the shipped
    // file from the VR-Forces settings tree as the 5.2d remoteControl sample does.
    // Ignored by the 5.0.2 build.
    std::string connectionConfigFile;

    // OPT-IN diagnostic lever, default false = the shipped behaviour (Start() executes
    // nothing extra). When true, Start() calls
    // DtReflectedExtEntityList::setWaitForVrfExtendedData(false)
    // (vrfExtObjects/reflectedExtEntityList.h:77 on 5.2d, :75 on 5.0.2) on the entity list
    // the UUID network manager owns, right after init.
    // WHY: that flag defaults TRUE (:238 myWaitForVrfExtendedData) and readyToAdd()
    // WITHHOLDS a VR-Forces object from the reflected list until its VRF object data
    // arrives or theTimeoutRequestTime elapses (:163-170, :186). H3 of
    // docs/experiments/RESEARCH_52_OBSERVER_DISCOVERY_2026-09-03.md: that withholding is a
    // candidate cause of the 5.2 reflected=0 symptom. Only the ENTITY list has this setter
    // - the aggregate and control-object lists do not declare it (reflectedExtAggregateList.h,
    // reflectedControlObjectList.h have setPropertyPrototypes but no wait flag).
    bool disableWaitForVrfExtendedData = false;
};

// Sizes of the controller's REFLECTED LISTS, read straight off the lists themselves
// (DtReflectedObjectList::count(), vl/reflectedObjectListHLA.h:98 in vrlink 5.8 AND 5.10),
// so the numbers do NOT depend on our UUID-change callbacks the way GetAllReflectedUuids()
// does. This is the H2 discriminator of
// docs/experiments/RESEARCH_52_OBSERVER_DISCOVERY_2026-09-03.md: "objects are in the lists
// but the UUID callbacks never fire" vs "nothing is reflected into this process at all".
// -1 = the list was not reachable (controller not started, or no public accessor).
struct ReflectedListCounts {
    int entities = -1;              // uuidNetworkManager()->entityList()
    int aggregates = -1;            // uuidNetworkManager()->aggregateList()
    // ENV AND CTL ARE THE SAME LIST - they are NOT two populations, and a reader must not
    // add them. VR-Forces publishes its control objects AS environment processes, so the
    // one list object is reached through two differently-typed accessors:
    //   - DtReflectedControlObjectList IS-A DtReflectedEnvironmentProcessList
    //     (vrfExtObjects/reflectedControlObjectList.h:24 on 5.2d, :23 on 5.0.2).
    //   - DtVrlinkVrfRemoteController holds exactly ONE such member,
    //     myReflectedEnvironmentProcessList (vrlinkVrfRemoteController.h:167 on 5.2d, :165
    //     on 5.0.2), and exposes it under BOTH reflectedEnvironmentProcessList() and
    //     controlObjectList() (:143-144 on 5.2d, :141-142 on 5.0.2). There is no second
    //     member for either accessor to return.
    //   - The vendor's own remote-object manager says the same thing twice: its
    //     environmentals member is TYPED DtReflectedControlObjectList* and its
    //     reflectedEnvironmentProcessList() RETURNS that type (remoteObjectManager.h:479
    //     and :159 on 5.0.2), and the factory that fills it is
    //     createReflectedControlObjectList(), documented as "Called to create the reflected
    //     environmental list" (remoteObjectManager.h:280-282 on 5.2d, :222 on 5.0.2).
    // Hence the vendor's printReflectedObjectCounts() total is entities + aggregates +
    // control objects with NO separate environment term: it prints exactly the four labels
    // "Reflected Entities" / "Reflected Aggregates" / "Reflected Control Objects" /
    // "Reflected Objects" (string constants in bin64/vrlinkNetworkInterfaceDIS.dll and
    // vrlinkNetworkInterfaceHLA1516e.dll). Observed 2026-09-03: entities 44-62 + aggregates
    // 0 + control objects 19 = total 63-81, while this struct reported env=19 ctl=19.
    // environmentProcesses is KEPT (it is a real count of a real list, and dropping it would
    // silently rewrite older traces' field set) but it is a DUPLICATE of controlObjects
    // whenever environmentAliasesControlObjects is 1.
    int environmentProcesses = -1;  // controller->reflectedEnvironmentProcessList()
    int controlObjects = -1;        // uuidNetworkManager()->controlObjectList()
    // Runtime PROOF of the aliasing above rather than an assumption carried from headers:
    // 1 = the two accessors returned the SAME object, so env is a duplicate of ctl and a
    // total must count it once; 0 = they returned DIFFERENT objects, which would falsify
    // the reading above and is the case a trace must flag; -1 = at least one side was
    // unreachable, so nothing was compared.
    int environmentAliasesControlObjects = -1;
    // ALWAYS -1: the reflected extended-attributes object list has NO public accessor on
    // either stack. DtUUIDNetworkManager keeps it as a protected member with no getter
    // beside the three public ones (UUIDNetworkManager.h:123-125 vs :189-192 on 5.2d), and
    // DtVrlinkNetworkInterface's DtReflectedObjectLists is protected too
    // (vrlinkNetworkInterface.h:783; only setReflectedLists(:510) is public). The one
    // public finder, DtExtendedAttributesExistenceListener::findExtendedAttributesObjectList
    // (vrvVrl/DtExtendedAttributesExistenceListener.h:60), belongs to VR-Vantage's vrvVrl,
    // which this bridge does not link. Reported as unknown rather than faked.
    int extendedAttributes = -1;
    // The entity list's current waitForVrfExtendedData() (reflectedExtEntityList.h:80 on
    // 5.2d, :78 on 5.0.2) - so a trace records whether the H3 lever was actually in effect.
    bool waitingForVrfExtendedData = false;
};

// ------------------------------------------------------------------
// Event payloads (POD). Delivered on the VR-Forces message/tick thread,
// exactly where the corresponding MAK callbacks fire today. (A future
// .NET layer must copy + marshal these off-thread; Phase 1 keeps the
// same synchronous dispatch as the current code for behavior parity.)
// ------------------------------------------------------------------

// Fired when VR-Forces confirms creation of an object the facade requested
// (entity, aggregate, waypoint, route, control area). Correlate by 'name'.
struct ObjectCreated {
    std::string name;      // the unique name passed to Create*
    std::string entityId;  // DtEntityIdentifier string
    std::string uuid;      // VRF UUID string (what SetAltitude/tasking use)
};

// Raw VR-Forces radio "text-report" (Lua-emitted POSITION / OBSERVATION).
struct TextReport {
    std::string text;
};

// VR-Forces "task-completed-report".
struct TaskCompleted {
    std::string unitMarking; // transmitter().markingText()
    std::string taskType;    // taskCompleted().string(), e.g. "move-along"
};

// Response to RequestAvailableFormations: the formation names an aggregate can
// assume plus its current formation (empty when uninitialized) - the direct
// oracle for "which names are valid for THIS unit" and "did my set take"
// (docs/UNIT_MOVEMENT_RESEARCH.md plan R4).
struct AvailableFormations {
    std::string uuid;                    // the responding aggregate's VRF uuid
    std::vector<std::string> formations; // valid names per the unit's matched .entity
    std::string currentFormation;        // "" if none / uninitialized
};

// One member ENTITY of a (disaggregated) aggregate, read from the aggregate's
// published state (R10 subordinate fan-out, docs/UNIT_MOVEMENT_RESEARCH.md sec 4c).
struct AggregateMember {
    std::string uuid;  // the member's VRF uuid (taskable)
    std::string name;  // the member's marking text (matches completion callbacks)
};

// A VR-Forces Object Console message captured remotely via
// DtVrfRemoteController::addObjectConsoleMessageCallback (vrfRemoteController.h:1970;
// the delivered signature is the typedef DtObjectConsoleMessageCallbackFcn at
// vrfRemoteController.h:112-114 = void(const DtUUID& id, int notifyLevel,
// const DtString& message, void*)). This is the per-unit warning channel BEHIND the
// yellow Object Console badge (docs/VRF_GROUND_TRUTH.md sec 0.0 cross-finding 1 and
// sec 7). notifyLevel: 0 fatal, 1 warning, 2 diagnostic, 3 verbose, 4 debug (sec 7,
// default 2). message is free text and MAY contain commas / quotes / newlines - it is
// delivered UNESCAPED; the consumer is responsible for any CSV escaping (groundwork
// plan 0.6; tools/WatchVrf emits it as the CON,... stream).
struct ObjectConsoleMessage {
    std::string uuid;     // the object's VRF uuid (marking-text based)
    int notifyLevel = 0;  // 0=fatal,1=warn,2=diag,3=verbose,4=debug
    std::string message;  // the console message text (unescaped)
};

// One point of a terrain-profile reply (docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md).
// The back end answers DtIfRequestTerrainProfileInformation with a
// DtIfIntersectionInformationResponse whose points are GEOCENTRIC
// (vrfmsgs/ifIntersectionInformationResponse.h:20); the facade converts each to geodetic,
// so point.altMeters is the terrain height in the same ellipsoid datum as every other
// altitude here. 'index' is the request point the sample answers (the reply's userData,
// ifRequestTerrainProfileInformation.h:46-48). valid=false: the back end returned an EMPTY
// set for that point (no terrain data, ifIntersectionInformationResponse.h:136-138).
struct TerrainSample {
    int index = -1;
    bool valid = false;
    Geodetic point;
};

struct TerrainProfile {
    unsigned int requestId = 0;  // == the id RequestTerrainProfile returned (responseId())
    bool complete = true;        // false only for partial replies (we request complete ones)
    std::vector<TerrainSample> samples;
};

// ------------------------------------------------------------------
// The facade
// ------------------------------------------------------------------

class VrfFacade {
public:
    VrfFacade();
    ~VrfFacade();
    VrfFacade(const VrfFacade&) = delete;
    VrfFacade& operator=(const VrfFacade&) = delete;

    // -- lifecycle ------------------------------------------------
    // Builds the exercise connection + remote controller, registers the
    // internal callbacks, and joins the federation/DIS network. Returns
    // false on failure. Must be called once before anything else.
    bool Start(const StartupConfig& cfg);

    // Transition-only (Phase 1 rewire) alternative to Start: instead of
    // creating its own controller/exConn/uuidMgr, the facade ADOPTS ones that
    // the caller already created and still owns. Used while call sites migrate
    // onto the facade one batch at a time - both the caller's existing path and
    // the facade drive the same controller, so the build stays green between
    // batches. The void* args are, in order, a
    // makVrf::DtVrlinkVrfRemoteController* (pass the BASE pointer), a
    // DtExerciseConn*, and a makVrf::DtUUIDNetworkManager*. Does NOT register
    // the inbound callbacks (the caller still owns them during the transition,
    // so registering here would double-fire) and does NOT take ownership
    // (Stop() will not delete an adopted controller). Returns false on failure.
    bool StartAdopting(void* controllerPtr, void* exConnPtr, void* uuidMgrPtr);

    // Transition accessors: hand the facade-owned controller / exercise
    // connection back to legacy code (textIf) as opaque void* during the
    // rewire, so textIf->controller() and textIf's exConn keep working while
    // state reads (getUnitGeodeticFromSim, backends) and a few dead paths still
    // live in the interface. The void* are a
    // makVrf::DtVrlinkVrfRemoteController* and a DtExerciseConn*. Removed in
    // Phase 4 when those uses move into the .NET port.
    void* GetController() const;
    void* GetExConn() const;

    // Transition (final flip): register the inbound report / scenario-close
    // trampolines on the adopted controller, so the facade (not textIf) fires
    // OnTextReport / OnTaskCompleted / OnScenarioClosed. Only for StartAdopting
    // mode (Start() already registers them). Call once after StartAdopting AND
    // remove textIf's own registration, or the callbacks double-fire. (Object-
    // created stays per-call; it already routes through the facade.)
    void RegisterInboundCallbacks();

    // Tears down the controller and connection (only those the facade owns;
    // an adopted controller/exConn is left for its owner to delete).
    void Stop();

    // One iteration of the drive loop: advances the sim clock, drains
    // input, and ticks the controller. The caller owns the loop + sleep.
    void Tick();

    int  BackendCount() const;
    bool AllBackendsReady() const;

    // -- observation-channel diagnostics (read-only; no protocol traffic) ----
    // Sizes of the reflected lists themselves - see ReflectedListCounts. Safe before Start()
    // (everything reads -1) and on the StartAdopting() path. Call between ticks, like the
    // other state reads: the lists mutate on the tick thread.
    ReflectedListCounts ReflectedCounts() const;

    // makVrf::DtRemoteObjectManager::hasDiscoveredObjects()
    // (vrlinkNetworkInterface/remoteObjectManager.h:110 on 5.2d, :121 on 5.0.2) - VR-Forces'
    // own answer to "has anything remote been discovered". 1 yes, 0 no, -1 UNKNOWN. Unknown
    // whenever the manager cannot be reached: before Start(), after Stop(), on the
    // StartAdopting() path (the adopted controller's concrete type is not ours to assume),
    // or when init was given disableRemoteDiscovery=true, which is documented NOT to create
    // the manager at all (vrlinkVrfRemoteController.h:92-93).
    int HasDiscoveredObjects() const;

    // Ask VR-Forces to print its own per-type reflected-object breakdown
    // (makVrf::DtRemoteObjectManager::printReflectedObjectCounts(),
    // remoteObjectManager.h:85-86 on 5.2d, :86-87 on 5.0.2). The text goes to the MAK
    // NOTIFY stream in the vendor's own format, NOT to our CSV, so a caller must bracket it
    // in its trace. No-op when the manager is unreachable (see HasDiscoveredObjects).
    void PrintReflectedObjectCounts() const;

    // VR-Link's licence probes (vl/checkLicense.h:19/:27 - identical in vrlink 5.8 and
    // 5.10, exported by vlHLA1516e.lib / vlHLA4.lib, both already in the link set).
    // R2 of docs/experiments/COLDSTART_REVIEW_2026-09-03.md: assistant-free mode removes the
    // License Not Found dialog, and MAK RTI Users Guide 8.3 then lets the federate run
    // UNLICENSED while 8.2 says licensed and unlicensed federates exchange no messages -
    // exactly the reflected=0 shape. These are the pre-flight that record makes a gate.
    // The header's own caveat: a successful check does not guarantee a later object
    // creation succeeds, because the licence manager's state can change in between.
    // NOTE the MAK RTI itself exposes no licence-state query we can call here: the only
    // licence API in makRti4.6.1/include is the Assistant wire protocol
    // (MAK/assistant/assistantLicQueryMsg.h:26-65, isUnlicensed() etc.), and
    // RTI_ASSISTANT_DISABLE is precisely what our runs set.
    static bool HaveVrLinkLicense();
    static bool HaveRtiLicense();

    // Which MAK stack this process actually loaded: "<build tag>|<full path of vrfcontrol.dll>"
    // (build tag = "5.2" for VRF_API_52 builds, else "5.0.2"). The native DLLs are found by
    // NAME on PATH, so a 5.2 bridge silently binds 5.0.2 DLLs when PATH is not set per
    // process (import failure at best). Log this at startup; the runner records it.
    static std::string NativeStackInfo();

    // -- scenario / simulation control ----------------------------
    void Run();
    void Pause();
    void SetTimeMultiplier(int multiple);
    void SetExerciseStartTime(int year, int month, int day,
                              int hour, int minute, int second);

    // -- object teardown ------------------------------------------
    // Delete a VR-Forces object (entity / aggregate / route / control area) by its VRF
    // UUID - the counterpart to the Create* calls (controller->deleteObject). Lets the
    // caller remove everything it created so objects do NOT accumulate in VR-Forces across
    // runs (accumulation degrades create/route reflection - see docs/RUNBOOK.md sec 7/8).
    void DeleteObject(const std::string& uuid);

    // -- reflected-object enumeration (hard VR-Forces reset) ------
    // Start collecting the VRF UUID of EVERY object the facade discovers on the network
    // (entities, aggregates, control objects), via the UUID network manager's change
    // callbacks. Call ONCE right after Start() and BEFORE the first Tick(); then Tick() for
    // a few seconds so discovery + UUID resolution complete; then read GetAllReflectedUuids().
    // Intended for the ResetVrf tool: join a live federation, discover EVERYTHING present
    // (incl. ORPHANS left by a crashed/force-killed run that Solution A's delete-on-stop
    // cannot reach), and DeleteObject() each for a full clean slate (docs/RUNBOOK.md sec 8).
    // Not for the app's normal path (it tracks only what IT created). Single-threaded use:
    // the callbacks fire on the tick thread; snapshot between ticks.
    void BeginTrackingReflectedObjects();

    // Snapshot (de-duplicated) of the UUIDs collected since BeginTrackingReflectedObjects().
    std::vector<std::string> GetAllReflectedUuids() const;

    // -- object creation (asynchronous) ---------------------------
    // These return immediately; completion arrives via OnObjectCreated
    // with a matching 'name'. The caller correlates and may then use the
    // reported uuid for SetAltitude / tasking.
    void CreateEntity(const EntityTypeSpec& type, const Geodetic& pos,
                      Force force, double headingDeg, const std::string& name);

    void CreateAggregate(const EntityTypeSpec& type, const Geodetic& pos,
                         Force force, double headingDeg, const std::string& name,
                         AggregateState state = AggregateState::Disaggregated,
                         bool createSubordinates = true);

    void CreateWaypoint(const Geodetic& pos, const std::string& name);

    void CreateRoute(const std::vector<Geodetic>& points, const std::string& name);

    // uuid: the VRF UUID to assign the created tactical graphic. The C2SIM
    // interface passes the area's C2SIM uuid here today; empty -> nullUUID.
    void CreateControlArea(const std::vector<Geodetic>& perimeter,
                           const std::string& name, const std::string& label,
                           const std::string& uuid = "");

    // -- attribute setters ----------------------------------------
    void SetAltitude(const std::string& uuid, double altitudeMeters);
    void SetLocation(const std::string& uuid, const Geodetic& pos); // magic move
    void SetTarget(const std::string& uuid, const std::string& targetUuid);
    void SetRulesOfEngagement(const std::string& uuid, Roe roe);

    // -- tasking --------------------------------------------------
    void MoveToLocation(const std::string& uuid, const Geodetic& pos);
    void MoveAlongRoute(const std::string& uuid, const std::string& routeUuid);

    // Pathfinding move to a CONTROL POINT (DtPlanAndMoveToTask, sent via sendTaskMsg).
    // The destination is an existing waypoint/control-point OBJECT (DtMoveToTask has no
    // raw-coordinate setter) - create one via CreateWaypoint and pass its uuid/name here.
    // R11 probe (docs/UNIT_MOVEMENT_RESEARCH.md sec 4c): does the PLANNED point-move
    // produce a path at locations where moveAlongRoute's leader-path plan is EMPTY?
    void PlanAndMoveTo(const std::string& uuid, const std::string& controlPointUuid);

    // Enumerate the member ENTITIES of a reflected (disaggregated) aggregate from its
    // PUBLISHED aggregate state (the entities designator list) - uuid + marking each.
    // R10 subordinate fan-out: entity moves are proven where unit leader-path planning
    // fails, so the caller can task members directly (they revert to unit control on
    // completion). Read-only; returns empty if the uuid does not resolve, is not an
    // aggregate we can read, or publishes no members (caller logs + falls back).
    // CAVEAT: the caller must pass an AGGREGATE uuid - like TryGetEntityGeodetic, the
    // typed dynamic_cast can miss across the MAK DLL boundary and the fallback is a
    // static_cast that is only valid for a real aggregate.
    std::vector<AggregateMember> GetAggregateMembers(const std::string& aggregateUuid) const;

    // Set an aggregate's formation by name ("Wedge","Column","Line","Vee","Echelon").
    // Safe no-op on non-aggregate entities. A disaggregated aggregate needs a VALID
    // formation for its set-maneuver; without one VRF keeps an unresolvable default
    // and the unit will not move (Phase 4 spike - not parity; see PORT.md sec 10).
    // NOTE (docs/UNIT_MOVEMENT_RESEARCH.md sec 1.5): on a DISAGGREGATED unit this SNAPS
    // members instantly into their slots; on an aggregated unit it is bookkeeping.
    void SetAggregateFormation(const std::string& uuid, const std::string& formationName);

    // Reorganize an aggregate: (re)establish the leader/echelon assignments and close
    // the formation. The remote lever for units whose formation controller ships
    // auto-promote-in-formation OFF (the VRF default) - a remotely-created unit may lack
    // an established LEAD subordinate, and the disaggregated move-along controller
    // forwards the route to the lead (docs/UNIT_MOVEMENT_RESEARCH.md sec 1.3, plan R2).
    // Per the controller contract: no effect if 'uuid' is not an aggregate leader.
    void ReorganizeAggregate(const std::string& uuid);

    // Ask an aggregate which formation names it can assume, and what its current
    // formation is. ASYNCHRONOUS: the reply arrives via OnAvailableFormations (plan R4;
    // DtRequestAvailableFormationsAdmin -> DtAvailableFormationsAdmin).
    void RequestAvailableFormations(const std::string& uuid);

    // Move an aggregate INTO FORMATION at a location (DtMoveIntoFormationTask, sent via
    // sendTaskMsg). The PROPER aggregate maneuver: it moves the set to 'pos' oriented to
    // 'headingDeg', getting/holding the named formation - unlike moveAlongRoute, which only
    // sets a formation state and often leaves a disaggregated set stuck (PORT.md sec 10 /
    // docs/SEMANTIC_MAPPING.md Unit 4). headingDeg is degrees (converted to the radians the
    // task wants); formationName is a valid Title-Case name ("Wedge"/"Column"/...).
    void MoveIntoFormation(const std::string& uuid, const Geodetic& pos,
                           double headingDeg, const std::string& formationName);

    // Breach the obstacle 'breachTargetUuid' (DtBreachTask). Layer 2: the BREACH verb - go to
    // the obstacle and breach it (docs/SEMANTIC_MAPPING.md Unit 2). Target must be a VRF UUID.
    void Breach(const std::string& uuid, const std::string& breachTargetUuid);

    // Patrol the (already-created) route back and forth (DtPatrolRouteTask). Layer 2 for
    // SCREEN/SCOUT (Reconnoiter). routeUuid MUST BE A REAL "VRF_UUID:..." STRING, like
    // MoveAlongRoute's - CORRECTED 2026-09-02; the old claim "routeUuid is the route name,
    // resolved like MoveAlongRoute" was true of neither call. DtUUID's string ctor
    // (rwUUID.h:246-253) yields a VALID uuid only from the "VRF_UUID:" form; any other
    // string becomes a marking-text lookup carried in a 36-byte blob (rwUUID.h:412
    // char myData[36]), so a name over 34 characters reaches the back end CUT TO 35 and the
    // route reference never resolves - the unit is tasked and silently freezes (proven by
    // manipulation, docs/experiments/PREREG_ROUTE_NAME_LENGTH_2026-09-02.md sec 6). Callers
    // pass the uuid the ObjectCreated callback carried (VrfC2SimService.OnVrfObjectCreated).
    // NOTE: a patrol never self-completes (it reverses at the ends until retasked/triggered).
    void PatrolRoute(const std::string& uuid, const std::string& routeUuid);

    // Follow the target entity (DtFollowEntityTask; dynamic, no route). Layer 2 for ESCRT.
    void FollowEntity(const std::string& uuid, const std::string& targetUuid);

    // Fire at the target entity (DtFireAtTargetTask, sent via sendTaskMsg like
    // RunScriptedTask). autoSelectWeapon lets VRF choose the weapon; maxRounds <= 0 leaves
    // the task default (unbounded). The target must be a VRF UUID known to the sim; an
    // unknown target is a VRF-side no-op. Layer 2 of the semantic map: the ATTACK-family
    // verbs (ATTACK/DESTRY/FIX/DISRPT/PENTRT) map here (docs/SEMANTIC_MAPPING.md).
    void FireAtTarget(const std::string& uuid, const std::string& targetUuid,
                      bool autoSelectWeapon = true, int maxRounds = 0);

    // Scripted (Lua) task, sent via a task message (e.g. evacuate_civilians).
    void RunScriptedTask(const std::string& uuid, const std::string& scriptId,
                         const std::vector<ScriptVar>& vars);

    // Scripted (Lua) set-data, sent via a set-data message (e.g. set_point_agl).
    void SendScriptedSet(const std::string& uuid, const std::string& scriptId,
                         const std::vector<ScriptVar>& vars);

    // -- state read (pure; does NOT task the unit) ----------------
    // Reads the reflected entity's current geocentric location and returns
    // it as geodetic. Returns false if no reflected entity exists for the
    // uuid (e.g. an aggregate, which has no DtReflectedEntity).
    bool TryGetEntityGeodetic(const std::string& uuid, Geodetic& out) const;

    // -- terrain query (asynchronous) -----------------------------
    // Ask the simulating back end(s) for the terrain height under each point
    // (DtIfRequestTerrainProfileInformation, sent DtSimSendToAll with
    // sendPartialInformation=false -> ONE complete reply per back end). Points are sent
    // geocentric (the protocol convention; the request header states no frame - see the
    // design doc sec 0). Returns the request id to correlate OnTerrainProfile
    // (TerrainProfile::requestId), 0 if not started. No reply is guaranteed (unpaged
    // terrain, scenario close): the CALLER owns the timeout.
    unsigned int RequestTerrainProfile(const std::vector<Geodetic>& points);

    // -- events (set before Start; called on the VRF message thread) ---
    std::function<void(const ObjectCreated&)> OnObjectCreated;
    std::function<void(const TextReport&)>    OnTextReport;
    std::function<void(const TaskCompleted&)> OnTaskCompleted;
    std::function<void()>                     OnScenarioClosed;
    std::function<void(const AvailableFormations&)> OnAvailableFormations;
    // Per-unit Object Console warnings (groundwork plan 0.6). Registered on the
    // controller in Start() (and RegisterInboundCallbacks()); fires on the tick thread.
    std::function<void(const ObjectConsoleMessage&)> OnObjectConsoleMessage;
    // Reply to RequestTerrainProfile (also fires for any other
    // DtIfIntersectionInformationResponse on the session - correlate on requestId).
    std::function<void(const TerrainProfile&)> OnTerrainProfile;

private:
    struct Impl;
    Impl* p_ = nullptr; // all MAK Dt* types live behind here, in VrfFacade.cpp
};

} // namespace vrf
