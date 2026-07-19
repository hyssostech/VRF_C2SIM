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
    // SCREEN/SCOUT (Reconnoiter). routeUuid is the route name, resolved like MoveAlongRoute.
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

    // -- events (set before Start; called on the VRF message thread) ---
    std::function<void(const ObjectCreated&)> OnObjectCreated;
    std::function<void(const TextReport&)>    OnTextReport;
    std::function<void(const TaskCompleted&)> OnTaskCompleted;
    std::function<void()>                     OnScenarioClosed;
    std::function<void(const AvailableFormations&)> OnAvailableFormations;
    // Per-unit Object Console warnings (groundwork plan 0.6). Registered on the
    // controller in Start() (and RegisterInboundCallbacks()); fires on the tick thread.
    std::function<void(const ObjectConsoleMessage&)> OnObjectConsoleMessage;

private:
    struct Impl;
    Impl* p_ = nullptr; // all MAK Dt* types live behind here, in VrfFacade.cpp
};

} // namespace vrf
