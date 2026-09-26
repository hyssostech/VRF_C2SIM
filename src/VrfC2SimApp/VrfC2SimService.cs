using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using C2SIM;
using VrfC2Sim;
using S = C2SIM.Schema102;   // task-status codes (TASKCMPLT / TASKABRT) for the C16 watchdog report

namespace VrfC2SimApp;

/// <summary>
/// The VRF_C2SIM interface, ported to .NET. Bridges the C2SIM server (via the
/// HyssosTech C2SIM SDK) and VR-Forces (via the native VrfBridge). Reproduces the
/// C++ c2simVRFinterface's runtime role:
///   C2SIM in : Initialization -> create units/routes/areas in VR-Forces
///              Order          -> task units (move/scripted/...)
///   VRF out  : object-created -> correlate name -> VRF uuid
///              task-complete  -> C2SIM status report (TASKCMPLT)
///              text/position  -> C2SIM position report
///
/// THREADING: the native facade is single-threaded. All bridge command calls are
/// marshalled onto the one VRF tick thread via <see cref="_tickActions"/>; the
/// bridge's own callbacks already fire on that thread. This is a deliberate,
/// safer design than the C++ interface's cross-thread controller calls - it
/// produces the same command stream, so golden-trace parity is preserved.
/// </summary>
public sealed class VrfC2SimService : BackgroundService
{
    private readonly ILogger _log;
    private readonly IHostApplicationLifetime _life;
    private readonly C2SIMSDK _sdk;
    private readonly VrfBridge _bridge;
    private readonly VrfSettings _vrf;

    // FIDELITY PASS 2026-09-02: the (functionId, echelon, nationRole) table that replaces the
    // echelon-letter dispatch for GROUND units under TypeMappingMode=FidelityTable. Null (and
    // never consulted) in the two legacy modes, so their behavior is untouched.
    private readonly UnitTypeMap _typeMap;
    private readonly NationRoles _nations;
    private readonly string _typeMapLoadError;

    // C2SIM name <-> VRF uuid correlation, populated on ObjectCreated
    // (parity: onVrfObjectCreated in C2SIMinterface.cpp).
    // B3 (2026-09-14): this is a NameRegistry, not a bare dictionary, because VR-Forces may return
    // a created object under a name SHORTER than the one we asked for - a platform's name is a DIS
    // MARKING, and "2/1_AD/25_~PXY" came back as "2/1_AD/25_" (runs/20260914T002716Z_run). The
    // registry resolves such a callback to the requested name, so every lookup below - the R1
    // position poll, the arrival-evidence check, the stall watchdog, ExecuteTaskOnTick - finds the
    // object under the name the C2SIM init gave it. See NameRegistry for the rule and its guards.
    private readonly NameRegistry _names = new();

    // C2SIM unit-uuid -> what we created for it, retained from OnInitialization so OnOrder
    // can resolve a task's PerformingEntity (a C2SIM uuid) to the VRF object's name (the
    // _names key) and its SIDC (for the ground-clamp test). Parity: executeTask
    // looks the taskee up in the C++ unit map by taskeeUuid (C2SIMinterface.cpp:2044).
    private readonly ConcurrentDictionary<string, CreatedUnit> _unitByC2SimUuid = new();
    // C2SIM uuid -> hostility code of the init unit ("HO" = red), for the R1 position-report side
    // filter (oracle C2SIMinterface.cpp:436-437). Written at init, read on the tick thread.
    private readonly ConcurrentDictionary<string, string> _hostilityByC2SimUuid = new();
    private DateTime _nextPositionReport = DateTime.MinValue;   // R1 poll schedule (tick thread)

    // Route name -> FIFO of tasks waiting for that route's ObjectCreated (the along-route
    // task cannot be issued in the same tick as the async CreateRoute; parity:
    // C2SIMinterface.cpp:2408-2421). A QUEUE, not a single slot: the VRF callback carries
    // only the created object's NAME, and duplicate TaskNames produce identical route
    // names - FIFO is the best possible attribution (a second same-named entry no longer
    // silently overwrites the first). Patrol=true issues PatrolRoute (Reconnoiter)
    // instead of MoveAlongRoute.
    private readonly record struct PendingRouteTask(string TaskeeVrfUuid, bool Patrol,
        bool PlanMove = false, IReadOnlyList<AggregateMember>? FanOutMembers = null);

    // R10: member-completion -> unit-task aggregation for fanned-out aggregate moves.
    private readonly FanOutTracker _fanOut = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<PendingRouteTask>> _pendingRouteTasks = new();

    // Unit name -> the ATTACK/BREACH engage deferred until that unit's move COMPLETES
    // (P0.3, NEXT_SESSION_GUIDANCE.md sec 2.5: issuing the engage in the same tick as the
    // move would REPLACE the move - VRF runs one task at a time). Issued from
    // OnVrfTaskCompleted when the matching move task uuid completes, or by the
    // EngageFallbackSeconds timer if the move never completes.
    private readonly record struct PendingEngage(string Kind, string TaskeeVrf, string TargetVrf,
                                                 string MoveTaskUuid, string TaskName);
    private readonly ConcurrentDictionary<string, PendingEngage> _pendingEngage = new();

    // Object name -> C2SIM unit uuid (inverse of _unitByC2SimUuid), so the VRF report
    // callbacks - which carry the object's marking/name, not its C2SIM uuid - can name the
    // subject of a report (parity: onTaskCompleted/onTextReport getUnitByName -> unit->uuid).
    private readonly ConcurrentDictionary<string, string> _c2SimUuidByName = new();

    // P4b position-report bundling (Vrf:BundlePositionReports; C++ parity textIf.cxx:435-544).
    // When enabled, OnVrfTextReport ACCUMULATES POSITION fixes here instead of pushing one report
    // each; the buffer is drained by the count/size trigger (in OnVrfTextReport), a periodic timer
    // (BundleFlushMs), and once more on clean stop BEFORE resign. _posBundleLock guards ALL buffer
    // access; the serialize + network push runs OUTSIDE the lock (snapshot-under-lock, then
    // build+push) so the lock is never held across a serialize or a PushReportAsync. TASKCMPLT is
    // NEVER bundled (it flows through SynthesizeUnitCompletion, a wholly separate path).
    // headingDeg/speedMps (B7) ride along per fix and are NULL when the kinematics read failed
    // or when the fix came from a POSITION text report, which carries lat/lon only.
    private readonly List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)> _posBundle = new();
    private readonly object _posBundleLock = new();
    // Rough serialized-size ESTIMATE constants for the SECONDARY size guard (COUNT is PRIMARY - see
    // OnVrfTextReport). We do NOT re-serialize per fix; a conservative per-fix estimate only needs
    // to flush BEFORE the real payload nears BundleMaxBytes ("STOMP may balk at larger" - the C++
    // rationale). With the defaults (10 reports / 10240 bytes) count always fires first; the size
    // guard only bites if BundleMaxReports is raised or BundleMaxBytes lowered. Overestimating is
    // safe (flush a little early).
    private const int PosBundleEnvelopeBytes = 512;  // <ReportBody> preamble/postamble + ReportID/ReportingEntity
    private const int PosBundleFixBytes = 480;       // one <PositionReportContent> block (uuid + lat/lon + timestamp + tags
                                                     // + the B7 HeadingAngle/Speed elements, ~80 B when both are present)

    // Per-unit in-flight task record (P0.1, replaces the last-write current-task map whose
    // completion misattribution corrupted TASKCMPLT reports + released the wrong successor
    // gates - NEXT_SESSION_GUIDANCE.md sec 2.4 DEFECT A). Written at dispatch
    // (MarkDispatched), popped at completion (OnVrfTaskCompleted); fills the TaskStatus
    // report's CurrentTask (parity: setUnitCurrentTaskUuid, C2SIMinterface.cpp:2165).
    private readonly InFlightTracker _inFlight = new();

    // Tactical-graphic keys ("<kind>:<uuid-or-name>") already queued for creation - the
    // duplicate-init guard for AREAS and, since V3, for LINES and POINTS too (units use
    // _unitByC2SimUuid membership for the same purpose). The kind prefix keeps an area and a
    // line that share a uuid from colliding; C2SIM gives each graphic its own uuid, so the
    // prefix is belt-and-braces, not a workaround for an observed collision.
    private readonly ConcurrentDictionary<string, byte> _createdAreaKeys = new();

    // R1: the init's graphics by the C2SIM uuid they were AUTHORED under, so a task's MapGraphicID
    // resolves to the geometry the init gave that uuid. Areas, lines AND points (M5, cold-start
    // review of 5c67d41: areas alone covered 35 of COA-STP1's 409 graphics, so a MapGraphicID
    // naming a phase line or an axis of advance matched nothing and the task fell through to its
    // embedded Location - or, on an export that drops it, to R2 in place).
    // INDEPENDENT OF Vrf:CreateInitLines / Vrf:CreateInitPoints: this map holds AUTHORED POINTS and
    // never touches a VR-Forces object, so a creation flag cannot decide whether a task gets its
    // own coordinates. Where the object IS created the uuid match is also object identity
    // (createControlArea / createRoute / createWaypoint all take the C2SIM uuid as startingUUID,
    // VrfFacade.cpp:725-737, vrfRemoteController.h:991-1039 - no mapping table).
    private readonly ConcurrentDictionary<string, TaskGraphic> _graphicsByC2SimUuid = new();

    // (The VRF uuid -> name reverse map used by the R4 formation reply and the object console -
    // both carry only a uuid - now lives in _names too: NameRegistry.TryGetName/TryAddName.)

    // VRF uuids whose R1 formation set + reorganize already ran (first reply wins;
    // later replies - e.g. the move-time diagnostic re-query - must not re-snap).
    private readonly ConcurrentDictionary<string, byte> _formationApplied = new();

    // Sequences task starts (predecessor completion + start delay), replacing the C++
    // busy-waits with async gating + a timeout. See TaskSequencer.
    private readonly TaskSequencer _sequencer = new();

    // M1 (cold-start review of 5c67d41): every task this interface has been given, by uuid, so a
    // gated task can read its PREDECESSOR'S OWN Duration and wait at least that long. Written for
    // the WHOLE order before the first task is orchestrated, because a successor may appear before
    // its predecessor in document order. Never pruned: an order's task set is tens of entries and
    // a late TaskStatus may still need to name one.
    private readonly ConcurrentDictionary<string, OrderTask> _taskByUuid = new(StringComparer.Ordinal);

    // B8: what each unit was last ANNOUNCED as being represented by (R-SURFACE-PROXY). A unit
    // re-created at order time as something else - its full template, or a composition of doctrinal
    // sub-units instead of the empty shell it was at init - announces again. See
    // SubstitutionAnnouncer.
    private readonly SubstitutionAnnouncer _substitutions = new();

    // B1: which TaskStatus code a task may still emit, and how often (TASKSTRT at dispatch,
    // ONE TASKCMPLT per task, TASKABRT for a refused / skipped / stalled / failed task, and
    // abort-then-complete - a supervisor position (RL-20260914-01 covers the code only)). Every
    // TaskStatus report in this file goes through PushTaskStatus,
    // which consults it - so the guarantees are properties of the report STREAM, not of call sites.
    private readonly TaskStatusPolicy _taskStatus = new();

    // The tasks whose END TIME (start time + Duration, the owner's temporary position RL-20260921-09)
    // has not arrived yet, and the OVERDUE ones still waiting for their unit. Armed at dispatch
    // (MarkDispatched), walked forward on the tick thread (MaybeCompleteTimedTasks), held or closed
    // by a successful completion (SynthesizeUnitCompletion -> MarkFinished) and cancelled by any
    // terminal end (PushTaskStatus). See TimedCompletionPolicy for the rule.
    private readonly TimedCompletionPolicy _timed = new();
    private DateTime _nextTimedCheck = DateTime.MinValue;
    private bool _timedClockLineLogged;
    private bool _timedUsingSim;
    // The timed-completion cadence is FIXED, not configurable: it costs one SimTimeSeconds read a
    // second, and the only thing a knob could do is make a compressed demo (Vrf:DurationScale)
    // miss its deadlines by up to the cadence.
    private const double TimedCheckSeconds = 1.0;

    // ================= THE TASK CLOCK (M2, cold-start review of 5c67d41) =========================
    // ONE MONOTONE AXIS, IN SECONDS, ON WHICH EVERY C2SIM TASK TIME IS SERVED: the Duration that
    // ends a task (R4), the StartTime delay that holds one back, and the STREND predecessor gate.
    // Vrf:TaskClock says which underlying clock feeds it ("sim", the default, with a wall
    // fallback); StallClock no longer has anything to do with it.
    //
    // WHY AN AXIS AND NOT A CLOCK READING. The three consumers must be comparable to each other
    // and to themselves ACROSS a fall back to the wall clock, whose epoch is astronomically larger
    // than a scenario clock's. The axis is the running sum of the FORWARD movement of whichever
    // clock was in effect, never accumulated across a mode change - so a paused scenario adds
    // nothing, DtVrfRemoteController::rollbackToSnapshot (vrfRemoteController.h:605) adds nothing,
    // and losing the sim reader mid-task neither completes a task early nor restarts anybody's
    // wait. It is exactly TimedCompletionPolicy's own discipline, applied once for everyone.
    // Sampled on the tick thread; READ from the SDK callback threads and the thread pool (the gate
    // pollers), hence Volatile.
    private readonly TaskClockAxis _taskAxis = new();
    private DateTime _nextTaskClockSample = DateTime.MinValue;
    private bool _taskClockConfigWarned;                       // Vrf:TaskClock typo, logged once
    // M3 + M4: the sim clock is OBSERVED ONCE PER SAMPLE for every consumer - hysteresis-confirmed
    // readability and staleness in one place (SimClockTracker), so the progress watchdog and the
    // task clock can never disagree about whether the scenario is running. The sampler below is
    // the ONLY caller of _bridge.SimTimeSeconds().
    private readonly SimClockTracker _simClock = new();
    private SimClockTracker.Observation _simClockLast =
        new(false, false, false, -1.0, -1.0, StallPolicy.SimClockStep.Flat, false, 0.0);
    // N7 (D7 harvest, 2026-09-21): the SIMULATION clock beside the wall clock on every line that
    // states an elapsed time, and the RATIO once a minute. The D7 harvest had to REGRESS the
    // level-3 behaviour-tree console rows against the trace clock to discover the scenario was
    // running at 3.00x, and D6's recorded "sim/wall 1.00" turned out to be a wall figure compared
    // with a wall figure. NO NATIVE WORK WAS NEEDED: VrfBridge.SimTimeSeconds() ->
    // VrfFacade::SimTimeSeconds() -> DtVrfRemoteController::simTime() has been the watchdog's and
    // the task clock's reader all along (VrfFacade.h:463-482), and _simClockLast is that reading,
    // already sampled once per second on the tick thread. All this adds is the stamping.
    private readonly SimWallRatio _simWallRatio = new();
    private bool _taskClockStaleWarned;                         // M4 stale warning for the TASK clock, once
    private DateTime _taskClockHoldLineUtc = DateTime.MinValue; // Q5: the HOLD line repeats, rate-limited
    private DateTime _taskClockBackendReadWarnUtc = DateTime.MinValue; // STP-809: control-state read failed
    private DateTime _simRollbackLineUtc = DateTime.MinValue;   // rate limit for the rollback line
    private int _simRollbackLinesSuppressed;                    // rollbacks the rate limit did not print
    private const double TaskClockSampleSeconds = 1.0;
    // How often a gate/delay waiting on the axis re-checks it. A WALL cadence by necessity (there
    // is nothing else to sleep on); 200 ms costs nothing against the shortest wait in a real order
    // and bounds the overshoot of a heavily compressed demo.
    private const int TaskClockPollMs = 200;

    // STP-822: BACK-END LIVENESS, on its OWN wall timer (Vrf:BackendLivenessSeconds). The rule
    // and the state machine are in BackendLivenessPolicy / BackendLivenessMonitor; what lives
    // here is the schedule and the consequences. Before this, the ONLY place the interface
    // re-read the back end was SampleTaskClock's stale branch, which needs the sim clock
    // readable-confirmed AND flat - on run 20260915T130627Z it never ran, and the interface
    // delivered 543 position reports off a STOPPED back end with 0 warnings and 0 TASKABRT
    // (V6_LIVE_JOIN_GATE sec 9.5). The monitor is touched by the tick thread only;
    // _backendLost is the copy the other phases and the R1 poll read.
    private BackendLivenessMonitor _liveness;
    private DateTime _nextLivenessCheck = DateTime.MinValue;
    private volatile bool _backendLost;
    private DateTime _backendLastGoodUtc = DateTime.MinValue;
    private bool _livenessSuppressionSaid;      // R1 suppression, said ONCE per outage
    private int _positionCyclesSuppressed;      // and counted, so the recovery line can say how many
    private bool _livenessStandDownSaid;        // C16 stand-down, said ONCE per outage
    // STP-822 part 2 / STP-823: what the VR-Forces OBJECT CONSOLE has said about navigation
    // areas. The interface already subscribes to that channel; NavAreaEvidence is the reading
    // of it, and NavAreaEvidence's class comment states exactly what it does and does not prove.
    private readonly NavAreaEvidence _navEvidence = new();
    private bool _navGateBlindWarned;           // "consoles too low to judge", said once

    /// <summary>The task-clock axis, in seconds (see the field block). Safe from any thread.</summary>
    private double TaskClockSeconds => _taskAxis.Seconds;

    /// <summary>The axis, packaged for TaskSequencer: one Now and one clock-aware delay.</summary>
    private TaskClock _taskClockAxis;

    // The service lifetime token, captured in ExecuteAsync so task orchestrations started
    // from SDK-event threads can cancel their waits on shutdown.
    private CancellationToken _stoppingToken = CancellationToken.None;

    // Commands from SDK-event threads are queued here and executed on the tick thread.
    private readonly ConcurrentQueue<Action> _tickActions = new();
    private volatile bool _stopTick;

    // Post-create SetAltitude, deferred until ObjectCreated delivers the VRF uuid
    // (parity: the C++ factories waitForData then SetAltitude - here it is async).
    private readonly ConcurrentDictionary<string, double> _pendingAltitude = new();

    // GroundWaypointAltitudeMode="TerrainProfile" (docs/DESIGN_TERRAIN_PROFILE_VERTICES_
    // 2026-09-01.md sec 3.3): terrain-height requests in flight, keyed by the bridge's request
    // id. The reply (OnVrfTerrainProfile) or the tick-loop expiry runs Continue(samples) on the
    // tick thread; samples == null = timed out. Whichever fires first removes the entry.
    // TWO consumers now share this plumbing: the ROUTE-VERTEX authoring (the original, per task)
    // and the INIT PLACEMENT query (one request for all create positions of an init, 2026-09-05).
    // FallbackNote is what the timeout warning says the consumer will do instead, so each keeps
    // its own accurate wording.
    // D1 (pass-3 review): TaskUuid/TaskeeUuid are the ROUTE consumer's, and they are what lets
    // RunTerrainContinuation give a continuation that THREW the same ending every other dispatch
    // dead end has - an abandon and one TASKABRT - instead of leaving the task silent and its
    // successors parked on the chain backstop. The INIT PLACEMENT consumer leaves them empty
    // because it has no C2SIM task to abandon.
    private sealed record PendingTerrain(DateTime Deadline, string TaskName, Action<List<TerrainHeightSample>> Continue,
                                         string FallbackNote = "dispatching with Live vertices",
                                         string TaskUuid = null, string TaskeeUuid = null);
    private readonly ConcurrentDictionary<uint, PendingTerrain> _pendingTerrain = new();

    // What the shared terrain-profile plumbing's TaskName field says for the INIT PLACEMENT
    // request - that consumer is an init, not a task.
    private const string PlacementTerrainLabel = "INIT PLACEMENT";

    // ===== PLACEMENT RE-CLAMP (2026-09-21, run 20260921T114910Z) - see PlacementReclampPolicy =====
    // One entry per LAND object this init created on the FALLBACK altitude, i.e. with nothing
    // having placed it: the create clamp had no polygon and the post-create AGL set had no ground
    // height, because the streaming terrain had not paged in. Armed by FinalizePlacement ONLY when
    // that happened - on a run whose terrain answers (the D10/R9 shape) this map stays empty, the
    // tick phase is skipped by its own guard, and the feature costs exactly nothing.
    // TICK THREAD ONLY for the mutable counters; the dictionaries are concurrent because they are
    // read off the tick thread for the record. Since 2026-09-25 (RL-20260921-06, scope
    // RL-20260925-01) no ground-contact verdict holds, refuses or abandons a task: the sweep places
    // and verifies, and the dispatch-time measurement is only logged.
    private sealed class ReclampEntry
    {
        public string Name;
        // The create position. KEPT FOR THE RECORD ONLY - it is NOT what the sweep asks the terrain
        // about and NOT where a correction is sent (DL-1): a unit that has driven is no longer
        // there, and setLocation is a teleport.
        public Geodetic Point;
        // DL-1: the LIVE position this entry's outstanding terrain query was issued for. The reply
        // is only applied if the unit is still within
        // TerrainVertexAuthoring.DefaultMaxHorizontalMismatchMeters of it.
        public Geodetic QueriedAt;
        public int CorrectionsIssued;
        public PlacementReclampPolicy.Measurement Last;
        public PlacementReclampPolicy.Outcome Outcome = PlacementReclampPolicy.Outcome.Pending;
        // BL-2: the give-up is said ONCE, but the object keeps being MEASURED. Without this flag a
        // re-measurable StillOffGround entry would re-print its ERROR on every sweep.
        public bool GaveUpLogged;
    }
    private readonly ConcurrentDictionary<string, ReclampEntry> _reclamp = new();
    // What the app has MEASURED about each object's ground contact. Absent = Unknown = permissive:
    // an object nobody measured is tasked exactly as it is today.
    private readonly ConcurrentDictionary<string, PlacementReclampPolicy.Contact> _groundContact = new();
    private DateTime _reclampArmedUtc = DateTime.MinValue;
    private DateTime _reclampLastQueryUtc = DateTime.MinValue;
    private bool _reclampQueryInFlight;
    private bool _reclampConcluded;

    // The PlacementPolicy inputs for one planned create, kept parallel to the toCreate list so the
    // terrain reply can re-decide the create altitude without re-parsing the init. DeStacker.Apply
    // rewrites plans IN PLACE (DeStacker.Apply's summary: "De-stack plans IN PLACE ...", and its
    // body assigns plans[idx]), so index i keeps meaning plan i after de-stacking.
    private readonly record struct PlacementInput(int Domain, double? Agl, double? Msl);

    /// <summary>What OnInitialization created for one C2SIM unit, so OnOrder can task it.
    /// AutoFormation is the E1 per-created-type formation name (null for entities and
    /// unmapped types) - see AutoFormationFor.</summary>
    // Domain = the DIS domain of the VR-Forces type we CREATED (SISO-REF-010.xml:3116-3119:
    // 1 Land, 2 Air, 3 Surface, 4 Subsurface) - the simulator's own classification. It replaces
    // the oracle's SIDC[2]=='G' symbology test (C2SIMinterface.cpp:2158) as the "is this a ground
    // thing" discriminator. IsAggregate is the platform-vs-unit distinction: platforms have
    // ground contact, units organize platforms (docs/VRF_ALTITUDE_FRAMES.md).
    private readonly record struct CreatedUnit(string Name, string SymbolId, bool IsAggregate,
                                               int Domain, string AutoFormation);

    // ============ COMPOSE-FROM-CHILDREN (Vrf:ComposeHierarchy) ============
    // Build a PARENT aggregate (e.g. a company) from its DECLARED C2SIM child units instead of a
    // generic template, following MAK's own sample (commandLineRemoteController.cxx:717-775 build,
    // :1520-1554 attach): the parent is created as an EMPTY shell (createSubordinates=false) and
    // each declared child is attached via AddToOrganization once BOTH the parent and the child
    // exist. All state below is registered at init (before any create is enqueued) and then read/
    // mutated ONLY on the tick thread (OnVrfObjectCreated + the ExpireCompositions sweep), so the
    // registration happens-before every arrival and no extra locking is needed.
    private sealed class PendingComposition
    {
        public string ParentName = "";
        public List<string> ExpectedChildNames = new();      // DECLARED order (fixes leader/echelon, UG52 18.1.1)
        public readonly Dictionary<string, string> ArrivedChildVrfUuid = new(); // child name -> VRF uuid
        public string ParentVrfUuid = "";                    // set when the parent shell is created ("" until then)
        public DateTime Deadline;                            // past this, attach the arrived subset + warn
        public bool Done;
        public TaskCompletionSource Ready;                   // THE gate this composition completes (never a later one registered under the same name)
    }
    private readonly ConcurrentDictionary<string, PendingComposition> _compositions = new();   // parent name -> composition
    private readonly ConcurrentDictionary<string, string> _childToParent = new();               // child name -> parent name
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _compositionReady = new(); // parent name -> children attached
    // EXPAND-to-compose (coarse ORBAT leaves): the offline catalog resolver, loaded lazily on the
    // first coarse leaf so a run with no composite leaves pays nothing. Read on the init thread only.
    private ObjectTypeResolver _resolver;
    private bool _resolverTried;

    // ============ ORDER-TIME MATERIALIZATION (Vrf:CreationPolicy=AtOrder, C13) ============
    // Per matched unit: the creation plan exactly as UnitTranslator produced it, its placement input
    // and its declared C2SIM superior, kept from init so an ORDER can materialize the unit later
    // (MaterializeUnit). Written on the init thread, read on the order thread strictly after the init
    // has dispatched (the C2SIM SDK delivers the init before any order) - concurrent maps for the
    // cross-thread handoff. Keyed by C2SIM uuid.
    private sealed record DeferredUnit(CreationPlan Plan, PlacementInput Placement, string SuperiorUuid,
                                       IReadOnlyList<string> DeclaredChildUuids, string C2SimName);
    private readonly ConcurrentDictionary<string, DeferredUnit> _deferred = new();
    private readonly ConcurrentDictionary<string, List<string>> _childUuidsBySuperior = new(); // superior uuid -> declared child uuids (init order)
    private readonly ConcurrentDictionary<string, byte> _materialized = new();                 // uuid -> materialization started (once)
    private readonly ConcurrentDictionary<string, byte> _recreatePending = new();              // unit name -> shell deleted, template re-create in flight
    private readonly ConcurrentDictionary<string, string> _reattachToParentVrfUuid = new();    // unit name -> superior shell VRF uuid to re-attach under
    // Review fixes (wf_dcad86e3, 2026-09-06): an order can reference a unit BEFORE its shell's
    // ObjectCreated has arrived (the runner pushes on the first reflected object; a demo operator
    // pushes by hand) - the materialization is then DEFERRED to the shell's arrival, never dropped.
    private readonly ConcurrentDictionary<string, (string Uuid, string Why)> _materializeOnCreated = new(); // unit name -> run MaterializeUnit when its shell arrives
    // Case 3 releases the task on REFLECTION of the re-created object (TryGetEntityGeodetic), not on
    // the ObjectCreated control message: HLA discovery is a separate path with its own latency.
    private readonly ConcurrentDictionary<string, (string Uuid, DateTime Deadline)> _awaitReflection = new();

    // ============ DEFER, DO NOT ABORT (D5b, 2026-09-21; DispatchReadiness.cs) ============
    // Run 20260921T072530Z_wayb pushed an order 0.31 s after the initialization. The interface
    // DROPPED T_R5_TK1 because 1.BdeHQ~PXY "was not created" and created that unit two log lines
    // later (:150 vs :162), and REFUSED T_R5_PL1 because a live location could not be read. Both
    // are TRANSIENT states of a healthy system reported as permanent failures of it. The state
    // machine and every sentence are in DispatchReadiness; what lives here is the waiting.
    //
    // ONE GATE PER UNIT NAME - several tasks on the same taskee share it. Registered and completed
    // on the TICK THREAD only (classification reads the bridge), awaited off it.
    private sealed class DispatchWait
    {
        public readonly TaskCompletionSource Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly bool PlannedAtInit;
        public volatile int LastStateCode;     // (int)TaskeeReadiness, refreshed by the tick sweep
        public DispatchWait(bool plannedAtInit, TaskeeReadiness state)
        { PlannedAtInit = plannedAtInit; LastStateCode = (int)state; }
    }
    private readonly ConcurrentDictionary<string, DispatchWait> _dispatchWaits = new(StringComparer.Ordinal);

    // THE INITIALIZATION'S OWN CREATION BARRIER. Every name the init asked VR-Forces to create,
    // recorded when the init's plan is FINAL and BEFORE the terrain-profile query - so a unit whose
    // create is still waiting on that reply counts as outstanding, which is exactly the D5b state.
    // It is the denominator of the READY TO TASK line and the membership test that keeps an
    // order-time materialization from deleting and re-creating objects while the init's own creates
    // are still landing on the back end (the overlap).
    private readonly ConcurrentDictionary<string, byte> _initPlannedNames = new(StringComparer.Ordinal);
    private int _initShellCount;                              // of those, AtOrder shells (members come later)
    private DateTime _initPlannedUtc = DateTime.MinValue;     // barrier start; MinValue = no init yet
    private volatile bool _initSettled;                       // tick thread writes; said once
    private bool _orderBeforeReadySaid;                       // order thread only
    // Order-time materializations HELD by that barrier. Same shape as _materializeOnCreated, drained
    // on the tick thread the moment the barrier settles or expires.
    private readonly ConcurrentDictionary<string, (string Uuid, string Why)> _materializeOnInitSettled =
        new(StringComparer.Ordinal);
    // Declared child names per composed parent (ApplyHierarchyComposition) so a case-3 re-attach can
    // restore the DECLARED subordinate order (leader = declared first, UG52 18.1.1) instead of
    // appending the re-created child last.
    private readonly ConcurrentDictionary<string, List<string>> _declaredChildNamesByParent = new();
    // N13: unit name -> the uuid whose READABILITY released that unit for tasking (ReleaseReflected's
    // success arm). ReadDeclaredChildPositions prefers it over the name registry, which can still
    // resolve a name to its DELETED SHELL in the window before the replacement's ObjectCreated binds.
    private readonly ConcurrentDictionary<string, string> _reflectedUuidByName = new(StringComparer.Ordinal);
    // N13 / STP-854: unit name -> WALL time the case-3 delete was enqueued, so ReleaseReflected can
    // print the delete -> readable round trip instead of leaving it to be inferred from a trace.
    // RETAINED after the round trip is printed (cold-start review SF-1): it is also the evidence
    // that licenses the words "re-compose transient" on the ROUTE ORIGIN line.
    private readonly ConcurrentDictionary<string, DateTime> _recreateIssuedUtc = new(StringComparer.Ordinal);
    // N13/SF-1: unit name -> WALL time its re-created object first became readable. With the issue
    // stamp above, these two bound the window in which a composed parent's published position is
    // sweeping; outside it, a gap is NOT diagnosed as a transient.
    private readonly ConcurrentDictionary<string, DateTime> _recreateReleasedUtc = new(StringComparer.Ordinal);
    // Serialises the two init deliveries (late-join QUERYINIT on the ExecuteAsync thread, a broadcast
    // on the STOMP pump): the duplicate guard is check-then-set and the per-superior child lists are
    // plain List<string>.
    private readonly object _initLock = new();

    public VrfC2SimService(ILoggerFactory loggerFactory, IConfiguration config,
                           IHostApplicationLifetime life)
    {
        _log = loggerFactory.CreateLogger("VrfC2Sim");
        _life = life;
        // M2: the ONE clock every C2SIM task time is served on (see the _taskClockSeconds block).
        _taskClockAxis = new TaskClock(() => TaskClockSeconds, TaskClockDelayAsync);

        var c2 = config.GetSection("C2SIM").Get<C2SIMSDKSettings>() ?? new C2SIMSDKSettings();
        _vrf = config.GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();

        // FidelityTable only: load data/unit-type-map.json now so a bad path/parse is reported
        // BEFORE VR-Forces is started (ExecuteAsync turns _typeMapLoadError into a refuse-to-start).
        _nations = new NationRoles(
            string.IsNullOrWhiteSpace(_vrf.FriendlyNation) ? "USA" : _vrf.FriendlyNation.Trim(),
            string.IsNullOrWhiteSpace(_vrf.OpposingNation) ? "RUS" : _vrf.OpposingNation.Trim());
        if (UsingFidelityTable)
        {
            string path = UnitTypeMap.ResolvePath(_vrf.TypeMapFile);
            if (path == null)
                _typeMapLoadError = $"Vrf:TypeMapFile '{_vrf.TypeMapFile}' was not found (searched the " +
                                    $"working directory '{Directory.GetCurrentDirectory()}', the app " +
                                    $"directory '{AppContext.BaseDirectory}' and its parents).";
            else
                try { _typeMap = UnitTypeMap.Load(path); }
                catch (Exception ex) { _typeMapLoadError = $"Vrf:TypeMapFile '{path}' failed to parse: {ex.Message}"; }
        }

        // C2SIM half. The endpoints are part of the record: since 2026-09-02 the runner points
        // every stage at a PRIVATE test server (C2SIM__RestUrl / C2SIM__StompUrl override
        // appsettings.json), and a run that heard the wrong server must say so in its own log.
        _log.LogInformation("C2SIM endpoints: rest={Rest} stomp={Stomp}", c2.RestUrl, c2.StompUrl);
        _sdk = new C2SIMSDK(loggerFactory, c2);
        _sdk.StatusChangedReceived += OnStatusChanged;
        _sdk.InitializationReceived += OnInitialization;
        _sdk.ObjectInitializationReceived += OnObjectInitialization;
        _sdk.OrderReceived += OnOrder;
        _sdk.ReportReceived += OnReport;
        _sdk.Error += OnError;

        // VR-Forces half
        _bridge = new VrfBridge();
        _bridge.ObjectCreated += OnVrfObjectCreated;
        _bridge.TaskCompleted += OnVrfTaskCompleted;
        _bridge.TextReport += OnVrfTextReport;
        _bridge.ScenarioClosed += OnVrfScenarioClosed;
        _bridge.AvailableFormations += OnVrfAvailableFormations;
        _bridge.TerrainProfile += OnVrfTerrainProfile;
        _bridge.ObjectConsoleMessage += OnVrfObjectConsoleMessage;
    }

    /// <summary>
    /// N3 (D7 harvest): WHICH BUILD IS RUNNING, from the binary itself.
    ///
    /// The runner manifest's `host.gitCommit` is the WORKING TREE's HEAD at run time, not the
    /// commit the deployed binary was built from - D7 recorded 9baddf9 for binaries built at
    /// adca180 and the discrepancy could only be settled afterwards, by hand, with git. The
    /// csproj's StampGitIdentity target writes the commit (and a +DIRTY marker for a build from
    /// an edited tree), the UTC build stamp and the bridge configuration into assembly metadata;
    /// this reads them back. "unknown" is a real answer and is printed as one.
    /// </summary>
    internal static string BuildIdentity()
    {
        var asm = System.Reflection.Assembly.GetEntryAssembly()
                  ?? typeof(VrfC2SimService).Assembly;
        string Meta(string key) =>
            asm.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
               .Cast<System.Reflection.AssemblyMetadataAttribute>()
               .FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.Ordinal))?.Value
            ?? "(not stamped - this binary predates N3)";
        string loc = "";
        try { loc = asm.Location; } catch { }
        string mtime = "";
        try { if (loc.Length > 0 && File.Exists(loc)) mtime = File.GetLastWriteTimeUtc(loc).ToString("u"); }
        catch { }
        return $"git {Meta("BuildGitCommit")}, built {Meta("BuildStampUtc")}, bridge "
             + $"{Meta("BuildBridgeConfig")}, assembly '{loc}'"
             + (mtime.Length > 0 ? $" last written {mtime}" : "");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        // N3: THE FIRST LINE OF EVERY RUN SAYS WHICH BUILD IT IS. Before this, the only build
        // identity in a run directory was the runner's record of the working tree's HEAD, which
        // is a statement about the CHECKOUT and not about the deployed binary.
        _log.LogInformation("BUILD IDENTITY: {Identity}. The runner manifest's host.gitCommit is the " +
                            "working tree's HEAD, which is a different question - THIS line is what the " +
                            "binary was built from (N3, D7 harvest).", BuildIdentity());

        // 0. FidelityTable pre-flight (JC-2, PROVISIONAL 2026-09-02). A missing/invalid table, or an
        // OpposingNation with no usable unit template, REFUSES TO START - it must not degrade into
        // the silent empty-unit trap (docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 3.5).
        if (UsingFidelityTable)
        {
            string fatal = _typeMapLoadError
                           ?? _typeMap.CheckNationSupported("friendly", _nations.Friendly)
                           ?? _typeMap.CheckNationSupported("hostile", _nations.Opposing);
            if (fatal != null)
            {
                _log.LogCritical("Vrf:TypeMappingMode=FidelityTable - REFUSING TO START. {Error}", fatal);
                _life.StopApplication();
                return;
            }
            _log.LogInformation("Type-mapping mode = FidelityTable ({Rows} rows from {File}); " +
                                "FriendlyNation={Friendly}, OpposingNation={Opposing}; " +
                                "SurfaceProxySubstitutions={Surface}.",
                                _typeMap.Rows.Count, _typeMap.SourcePath, _nations.Friendly,
                                _nations.Opposing, _vrf.SurfaceProxySubstitutions);
        }

        // 0b. CreationPolicy=AtOrder relies on the composition-ready gate and the compose recipes
        // (review fix wf_dcad86e3): without ComposeHierarchy every case-3 task would drive the
        // deleted shell. Refuse the pair, like the FidelityTable pre-flight above.
        if (_vrf.MaterializeAtOrder && !_vrf.ComposeHierarchy)
        {
            _log.LogCritical("Vrf:CreationPolicy=AtOrder requires Vrf:ComposeHierarchy=true (order-time " +
                             "materialization composes members into the init shells) - REFUSING TO START.");
            _life.StopApplication();
            return;
        }

        // 0c. PROGRESS-WATCHDOG PRE-FLIGHT (C16). The window belongs to the CLOCK - 240 WALL
        // seconds or 360 SIM seconds, both calibrated on the 2026-09-13 replay set - so the one
        // startup line names the clock AND the window that will actually be in effect, and says
        // whether that window came from the calibration or from configuration. Not a refusal: the
        // watchdog is report-only and Vrf:StallDetection ships OFF.
        if (_vrf.StallDetection)
        {
            bool stallPrefersSim = StallPolicy.ParseClockPreference(_vrf.StallClock, out bool stallClockValid);
            if (!stallClockValid)
                _log.LogWarning("Vrf:StallClock='{Value}' is neither \"sim\" nor \"wall\" - the progress watchdog " +
                                "will run on the WALL clock.", _vrf.StallClock);
            bool stallUsesSim = stallClockValid && stallPrefersSim;
            int stallWindow = (int)StallPolicy.ResolveWindowSeconds(_vrf.StallWindowSeconds, stallUsesSim);
            // THE CADENCE THE WINDOW CAN CARRY (pass-2 review F4a). MinRingDepth samples span
            // MinRingDepth - 1 intervals, so a coarser Vrf:StallCheckSeconds can never fill the
            // ring and the watchdog judges NOTHING - which 1805ee3 did in silence, on the default
            // WALL path, where 51d78a5 fired at 240 s with StallCheckSeconds 120 and 240 alike.
            // CLAMPED rather than refused: the watchdog is report-only and ships OFF, so a mis-set
            // knob must not stop a run that is otherwise fine, and the clamp restores exactly the
            // 51d78a5 outcome. This is the only place it is announced, so it is not silent either.
            int stallCheck = StallPolicy.ClampCheckSeconds(_vrf.StallCheckSeconds, stallWindow);
            // ... AND THE WINDOW BELONGS TO THE CLOCK IN EFFECT, WHICH IS NOT KNOWN YET (pass-3
            // review P11). Everything above is resolved from the CONFIGURED clock, but
            // Vrf:StallClock=sim only ASKS for the sim clock: if DtVrfRemoteController::simTime()
            // never answers, or stops answering mid-run, the watchdog falls back to WALL seconds
            // AND to the wall window - so a run configured for sim can execute on the 240 s wall
            // window (and, with a coarse Vrf:StallCheckSeconds, an 80 s cadence) while this line
            // announced 360 s and 120 s. Both pairs are printed below, and the
            // clock-mode line names the one actually in use at the first check.
            int stallWindowAlt = (int)StallPolicy.ResolveWindowSeconds(_vrf.StallWindowSeconds, false);
            int stallCheckAlt = StallPolicy.ClampCheckSeconds(_vrf.StallCheckSeconds, stallWindowAlt);
            if (stallCheck < Math.Max(1, _vrf.StallCheckSeconds)
                || (stallUsesSim && stallCheckAlt < Math.Max(1, _vrf.StallCheckSeconds)))
                _log.LogWarning("Vrf:StallCheckSeconds={Cfg} is coarser than a {W} s window can carry: the watchdog " +
                                "never judges on fewer than {Min} samples, which span {N} intervals, so a coarser " +
                                "cadence leaves the ring too shallow and the watchdog would go DORMANT with nothing " +
                                "in the log. CLAMPED to {Max} s{Alt}.",
                                _vrf.StallCheckSeconds, stallWindow, StallPolicy.MinRingDepth,
                                StallPolicy.MinRingDepth - 1, stallCheck,
                                stallUsesSim
                                    ? " on the sim clock, and to " + stallCheckAlt + " s on the "
                                      + stallWindowAlt + " s wall window if the sim clock cannot be read"
                                    : "");
            _log.LogInformation("PROGRESS WATCHDOG ON (C16, report-only): a {W} s no-progress window on the {Clock} " +
                                "clock, {M:F0} m of net displacement per member, sampled every {C} s; the window is " +
                                "{Src}. Both defaults are calibrated on the same three replayed traces - 240 WALL s, " +
                                "360 SIM s (docs/experiments/RECAL_STALL_SIMSECONDS_2026-09-13.md) - and are NOT a " +
                                "conversion of one another. FLOORS: at least {Min} samples inside the window, on " +
                                "either clock; {Floor}. On the sim clock the reader is " +
                                "DtVrfRemoteController::simTime(); if it cannot be read the watchdog falls back to " +
                                "WALL seconds AND to the wall window{Fallback}.",
                                stallWindow,
                                stallUsesSim ? "SIMULATION" : "WALL",
                                _vrf.StallMoveMeters,
                                stallCheck,
                                _vrf.StallWindowSeconds > 0 ? "the configured Vrf:StallWindowSeconds"
                                                            : "that clock's calibrated default",
                                StallPolicy.MinRingDepth,
                                stallUsesSim
                                    ? "the " + _vrf.StallMinSecondsSinceDispatch + " s post-dispatch floor is NOT "
                                      + "applied as WALL time on this clock - at a high sim/wall ratio it, and not "
                                      + "the calibrated window, would set the detection time (pass-2 review F8). "
                                      + "The cost of that (pass-3 review P6): with the floor gone, the first verdict "
                                      + "lands about window/ratio WALL seconds after a unit's watch opens - measured "
                                      + "240 s at 1.5x, 60 s at 6.21x (the top of the calibrated range), 8 s at 60x, "
                                      + "7 s at 120x - and above ~6x the " + StallPolicy.MinRingDepth + "-sample "
                                      + "floor is the ONLY guard left against a member whose HLA reflection has not "
                                      + "refreshed"
                                    : "no task is judged inside " + _vrf.StallMinSecondsSinceDispatch
                                      + " WALL seconds of its dispatch",
                                stallUsesSim
                                    ? " - and the numbers go with it: SIM " + stallWindow + " s window / sampled "
                                      + "every " + stallCheck + " s; WALL FALLBACK " + stallWindowAlt + " s / every "
                                      + stallCheckAlt + " s. The pair above is the CONFIGURED clock's; which pair is "
                                      + "in effect is not known until the first check, and the clock-mode line names "
                                      + "it then (pass-3 review P11)"
                                    : "");
        }

        // 0c-ii. THE LATERAL ROUTE SHIFT SAYS SO AT START-UP (user ruling 2026-09-20: "Route
        // shift: ON. Use as default for any run."). It is the only shipped-ON feature that changes
        // WHERE A UNIT DRIVES, and until now the first thing any log said about it was a per-task
        // line at the first ground move. A run must be able to answer "was the shift on?" from its
        // banner, because the answer changes how every track in it is read.
        //
        // THE COLD-CACHE WARNING IS THE LOAD-BEARING HALF. With no pre-warmed tiles the search
        // fetches them over HTTP at dispatch time; the dispatch is bounded by
        // Vrf:PreflightRouteShiftTimeoutSeconds and falls back to the authored line, so nothing is
        // refused - but a demo that has not pre-warmed its AO pays that wait on every ground move
        // and gets no shift out of it (unknown ground is never clear).
        // 0c-iii. F3: WHERE THE TILE CACHE IS AND HOW MUCH IS IN IT - said ONCE, UP FRONT, and
        // UNCONDITIONALLY. The route-shift banner below states it too, but only when the shift is
        // ON; the post-dispatch warnings reader and every hand-run of the pre-flight use the same
        // cache, and a run that cannot answer "was it warm?" from its own banner cannot say whether
        // a dispatch was ever in a position to be scored. The fallback is named AS a fallback so
        // the clean-rebuild hazard is visible before it bites rather than after.
        {
            string cacheDir = ResolvePreflightCacheDir(_vrf.PreflightCacheDir);
            int tiles = CountCacheFiles(cacheDir);
            bool shipped = string.IsNullOrWhiteSpace(_vrf.PreflightCacheDir);
            _log.LogInformation("ROUTE PRE-FLIGHT TILE CACHE: {Cache} - {N}, {Source}. Vrf:PreflightOffline={Off}. " +
                                "Every pre-flight reader (the lateral route shift, the post-dispatch warnings, and " +
                                "any hand-run) reads this one directory, and the file naming is byte-for-byte " +
                                "tools/preflight/leg_check.py's, so a warm copy can be dropped straight in.",
                                cacheDir,
                                tiles < 0 ? "UNREADABLE (treated as possibly warm, never as empty)"
                                          : tiles + " file(s)",
                                shipped
                                    ? "the SHIPPED FALLBACK because Vrf:PreflightCacheDir is empty - it sits INSIDE " +
                                      "THE BUILD OUTPUT and a clean rebuild DELETES it, so set Vrf:PreflightCacheDir " +
                                      "to somewhere durable for anything that must stay warm (F3)"
                                    : "set by Vrf:PreflightCacheDir",
                                _vrf.PreflightOffline);
            // SF3 (cold-start review of 9d67f97): ONE CACHE SCRUB, HERE, REPORTING ONLY. Before
            // this commit a non-tile body above minBytes was written into the cache and read back
            // as tile ABSENCE by both readers, permanently. The fetch path no longer does that, but
            // a cache warmed by an older build - or by the older leg_check.py, or shared with any
            // other tool - can still hold such a file, and its symptom (an area silently scored one
            // level coarser) is invisible. It is NOT deleted: a directory this process did not
            // write is not this process's to empty, and the file now produces a loud FAILED/NO
            // VERDICT rather than a quiet wrong answer, so the operator can act on the name.
            try
            {
                var scrub = Preflight.TileSource.ScrubCache(cacheDir);
                if (scrub.UndecodableCount > 0)
                    _log.LogWarning("ROUTE PRE-FLIGHT TILE CACHE SCRUB: {Bad} of {Checked} cached tile file(s) are " +
                                    "NOT in the format their extension claims (no TIFF/PNG signature) - almost " +
                                    "certainly an HTTP error or captive-portal page saved by an older build. Each " +
                                    "one now makes its tile FAILED (the level cascade stops and the leg gets NO " +
                                    "VERDICT), never a quiet fall-through to a coarser level. NOTHING WAS DELETED: " +
                                    "remove them from {Cache} to let those tiles be re-fetched. Examples: [{Names}].",
                                    scrub.UndecodableCount, scrub.Checked, cacheDir,
                                    string.Join(", ", scrub.SampleNames));
                else
                    _log.LogInformation("ROUTE PRE-FLIGHT TILE CACHE SCRUB: {Checked} cached tile file(s) checked, " +
                                        "all carry a valid TIFF/PNG signature (SF3).", scrub.Checked);
            }
            catch (Exception ex)
            {
                _log.LogWarning("ROUTE PRE-FLIGHT TILE CACHE SCRUB could not run ({Msg}) - the cache is used " +
                                "unchanged; an undecodable entry would surface as a FAILED tile, not as absence.",
                                ex.Message);
            }
        }

        if (_vrf.PreflightRouteShift)
        {
            string shiftCache = ResolvePreflightCacheDir(_vrf.PreflightCacheDir);
            int cachedTiles = CountCacheFiles(shiftCache);
            _log.LogInformation("LATERAL ROUTE SHIFT ON (Vrf:PreflightRouteShift, the shipped default since the user " +
                                "ruling of 2026-09-20; STP-804/806): before every GROUND move with more than one " +
                                "vertex, each leg is scored against the streamed terrain and a FLAGGED leg is " +
                                "detoured laterally - up to +/-{Band:F0} m - onto ground the same sampler scores as " +
                                "clear. STP's own vertices are never moved, dropped or reordered, and NO TASK IS " +
                                "EVER REFUSED: a timeout ({T:F0} s), a throw, a cache with no tiles or no cleared " +
                                "line all dispatch the AUTHORED line. Turn it off with Vrf:PreflightRouteShift=false " +
                                "(env Vrf__PreflightRouteShift=false). Tile cache: {Cache} ({N} files{Off}).",
                                _vrf.PreflightRouteShiftMaxMeters, _vrf.PreflightRouteShiftTimeoutSeconds,
                                shiftCache, cachedTiles < 0 ? 0 : cachedTiles,
                                _vrf.PreflightOffline ? ", offline" : "");
            if (cachedTiles == 0)
                _log.LogWarning("LATERAL ROUTE SHIFT: the tile cache {Cache} is EMPTY. {What} Pre-warm the AO's tiles " +
                                "and point Vrf:PreflightCacheDir at them (the STP-802 demo posture also sets " +
                                "Vrf:PreflightOffline=true) - otherwise every ground move's dispatch waits on the " +
                                "network for up to {T:F0} s and, where a tile never arrives, the leg is scored " +
                                "UNKNOWN and no shift is taken (unknown ground is never clear).",
                                shiftCache,
                                _vrf.PreflightOffline
                                    ? "Vrf:PreflightOffline is TRUE, so NOTHING can be scored and NO leg could ever be " +
                                      "shifted; the shift is therefore SKIPPED for this run and every ground move is " +
                                      "dispatched on its authored line at once, with no deferral and no wait."
                                    : "Every leg will fetch its tiles over HTTP at dispatch time.",
                                _vrf.PreflightRouteShiftTimeoutSeconds);
        }

        // 0c-iv. THE OTHER TWO SETTINGS THAT CHANGE WHAT A RUN MEANS, ANNOUNCED THE SAME WAY
        // (adca180 build report, FINDING 2). The route shift gets a banner, a runner PREDICTION
        // and a Stage 6c/teardown agreement check precisely because it changes where units drive.
        // Vrf:DeStackComposedSiblings changes WHERE UNITS START and Vrf:ArrivalApproachFraction
        // changes WHEN A TASK IS REPORTED COMPLETE - prereg P1 and P5 of the D7 run were both
        // scored off the app's log by hand because neither had an announcement to read. Now they
        // do, UNCONDITIONALLY (the de-stack's own per-group lines only appear when a fixture has
        // composed groups, so silence there is not evidence of OFF).
        _log.LogInformation("COMPOSED-SIBLING DE-STACK {State} (Vrf:DeStackComposedSiblings, user ruling " +
                            "2026-09-21 + N4). ON spreads composed siblings that share a coordinate onto ONE " +
                            "ring about it at EQUAL bearings, at their own echelon's spacing (table: {Table}), " +
                            "so their CENTROID - the position VR-Forces publishes for the parent aggregate - " +
                            "stays on that coordinate and the parent's route keeps its length. OFF reproduces " +
                            "the pre-2026-09-21 behaviour exactly (children held on the parent), which is what " +
                            "runs D1-D6 measured. Rotation {Rot} deg, echelon fallback {Fallback} m " +
                            "(0 = do not spread an echelon the table cannot size), overrides '{Override}'.",
                            _vrf.DeStackComposedSiblings ? "ON" : "off",
                            string.Join(", ", EchelonSpacing.TableMeters.OrderBy(kv => kv.Value)
                                                            .Select(kv => $"{kv.Key} {kv.Value:F0} m")),
                            _vrf.DeStackRotationDeg, _vrf.DeStackEchelonFallbackMeters,
                            _vrf.DeStackEchelonSpacingMeters ?? "");
        // The fraction is formatted INVARIANTLY, not with {F:F2}: the runner's agreement check
        // parses this number out of the log, and a machine whose culture writes "0,50" would make
        // a correct run read as a MISMATCH (and, worse, a wrong one read as agreement).
        _log.LogInformation("ARRIVAL APPROACH FRACTION {F} (Vrf:ArrivalApproachFraction, user ruling " +
                            "2026-09-21). Above 0 a member's traversal bar is capped at this share of ITS OWN " +
                            "distance to the last vertex at dispatch, instead of half the whole route - and " +
                            "since SF-1 that relaxation applies ONLY when the route genuinely goes somewhere " +
                            "(the taskee's own last-vertex separation is at least half the route's length). " +
                            "0 restores the pre-2026-09-21 route bar for every member, which is the " +
                            "comparability setting for runs D1-D6. Radius {R:F0} m, member fraction {Q:F2}, " +
                            "minimum travel {M:F0} m, checked every {C} s from {S} s after dispatch.",
                            _vrf.ArrivalApproachFraction.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                            _vrf.ArrivalRadiusMeters,
                            _vrf.ArrivalMemberFraction, _vrf.ArrivalMinTravelMeters,
                            _vrf.ArrivalCheckSeconds, _vrf.ArrivalMinSecondsSinceDispatch);

        // 0d-i. Vrf:DurationScale (m8 of the cold-start review of 5c67d41). ONE scale, TWO
        // opposite readings: a 0, negative or NaN scale collapsed the Duration to "no end time
        // armed" while collapsing the start delay to "dispatch now", so a run at scale 0
        // dispatched all 42 tasks at once and completed none - and said so only as a per-task
        // warning on one half. It is a configuration error, so it is caught once, loudly, here.
        _durationScale = _vrf.DurationScale;
        if (!TaskDispatchPolicy.IsUsableDurationScale(_durationScale))
        {
            _log.LogError("Vrf:DurationScale={Bad} is not a usable scale (it must be finite and greater than " +
                          "zero). A scale of zero or less is not an instruction to complete every task " +
                          "immediately - it collapses the Duration to NO end time while collapsing the start " +
                          "delay to dispatch-now, which dispatches the whole order at once and completes none " +
                          "of it. USING 1.0 (the order as written) for this run.", _vrf.DurationScale);
            _durationScale = 1.0;
        }

        // 0d-ii. Vrf:TaskPredecessorEndMarginSeconds (E5, pass-3 review). ZERO IS NOT A MARGIN.
        // TaskDispatchPolicy.PredecessorTimeoutSeconds clamps a negative one to 0 so the derived
        // window can never be SHORTER than the end time it waits for - but at 0 the window EQUALS
        // the predecessor's scaled Duration exactly, and the completion that releases the gate is
        // OBSERVED up to ~3 x (sim ratio) seconds late (the 1 s clock staircase plus the 1 s timed
        // walk). Gate and completion then race: the same non-determinism A1 was fixed to remove,
        // arriving by configuration instead. Caught once, here, like the scale above.
        _predecessorEndMargin = _vrf.TaskPredecessorEndMarginSeconds;
        if (!TaskDispatchPolicy.IsUsablePredecessorEndMargin(_predecessorEndMargin))
        {
            _log.LogError("Vrf:TaskPredecessorEndMarginSeconds={Bad} is not a usable margin (it must be " +
                          "greater than zero). At zero the STREND gate's completion window equals the " +
                          "predecessor's scaled Duration EXACTLY, while that completion is observed up to " +
                          "about 3 x the sim ratio seconds late - so the gate and the completion race and a " +
                          "successor is skipped on timing rather than on fact. USING {Good} s (the shipped " +
                          "default) for this run.", _vrf.TaskPredecessorEndMarginSeconds,
                          TaskDispatchPolicy.DefaultPredecessorEndMarginSeconds);
            _predecessorEndMargin = TaskDispatchPolicy.DefaultPredecessorEndMarginSeconds;
        }

        // 0d. TASK-CLOCK PRE-FLIGHT (R4/M2). THREE THINGS RIDE ON ONE CLOCK - the Duration that
        // ends a task, the StartTime delay that holds one back, and the STREND predecessor gate -
        // and which one that is decides whether a 42-task order runs or dies at its first gate.
        // Said once, at start-up, in the same shape as the watchdog line above.
        {
            bool taskPrefersSim = StallPolicy.ParseClockPreference(_vrf.TaskClock, out bool taskClockValid);
            if (!taskClockValid)
                _log.LogWarning("Vrf:TaskClock='{Value}' is neither \"sim\" nor \"wall\" - C2SIM task times will " +
                                "be measured on the WALL clock.", _vrf.TaskClock);
            _log.LogInformation("TASK CLOCK (R4): C2SIM task times are measured on the {Clock} clock " +
                                "(Vrf:TaskClock={Cfg}){Fallback}. It carries ALL THREE of the task Duration that " +
                                "ends a task, the StartTime/DelayTimeAmount delay that holds one back, and the " +
                                "STREND predecessor gate. Vrf:DurationScale={Scale}; a successor waits " +
                                "max(Vrf:TaskPredecessorTimeoutSeconds={Cfgt} s, the predecessor's own scaled " +
                                "Duration + Vrf:TaskPredecessorEndMarginSeconds={Margin} s) for its predecessor to " +
                                "COMPLETE (M1) and, when that predecessor is a task in the same order, " +
                                "Vrf:TaskChainBackstopSeconds={Backstop} s for it to DISPATCH at all (A1) - a " +
                                "predecessor that really dies is abandoned, not timed out. " +
                                "Vrf:StallClock governs the progress watchdog ONLY.",
                                taskClockValid && taskPrefersSim ? "SIMULATION" : "WALL",
                                _vrf.TaskClock,
                                taskClockValid && taskPrefersSim
                                    // D2 (pass-3 review): "or has gone stale" is the PRE-Q5 rule. Since
                                    // the user's Q5 ruling a clock that is merely FLAT while a back end is
                                    // still listed HOLDS task time; only an UNREADABLE reader falls back.
                                    ? " - falling back to WALL seconds only when DtVrfRemoteController::" +
                                      "simTime() cannot be READ at all, without restarting any wait. A clock " +
                                      "that is readable but FLAT while a VR-Forces back end is still there " +
                                      "and OPERATING - it reports PAUSED or RUNNING and at least one known " +
                                      "back end is still simulatable (STP-809) - is a PAUSE: task times are " +
                                      "HELD and age by nothing (Q5)"
                                    : "",
                                _durationScale, _vrf.TaskPredecessorTimeoutSeconds,
                                _predecessorEndMargin, _vrf.TaskChainBackstopSeconds);
        }

        // 1. Start VR-Forces (the bridge owns the controller/exConn).
        var cfg = BuildStartupConfig();
        _log.LogInformation("Starting VrfBridge (protocol={Protocol}, federation={Fed})...",
                            _vrf.Protocol, _vrf.Federation);
        if (!_bridge.Start(cfg))
        {
            // STP-832: the bridge now carries VR-Link's own reason for a refused
            // create/join (empty when Start() failed for some other reason).
            _log.LogError("VrfBridge.Start failed - aborting. Reason: {Reason}",
                          string.IsNullOrWhiteSpace(_bridge.LastStartError)
                              ? "(none reported)" : _bridge.LastStartError);
            _life.StopApplication();
            return;
        }
        // Which MAK stack the process really bound (DLLs resolve by NAME on PATH - a 5.2 bridge
        // over 5.0.2 DLLs is the trap). Format "<bridge build>|<vrfcontrol.dll path>".
        _log.LogInformation("VrfBridge native stack = {Stack}; ConnectionConfigFile='{Cfg}'.",
                            VrfBridge.NativeStackInfo(), _vrf.ConnectionConfigFile);

        // 1b. BACK-END SETTLE (PREREG_52_APP_SMOKE_2026-09-04 sec 4 P2). JOINING IS NOT
        // DISCOVERING: back-ends are not known at the instant Start() returns, and until this
        // block existed the log said nothing about them at all - a manifest/log reader could not
        // tell a federate that saw the sim's back-end from one talking to nobody (creates and
        // tasks against zero back-ends are silent no-ops). Same idiom, and the same 15 s cap, as
        // tools/RunSim (Program.cs:112-137) and tools/CreateOne (Program.cs:149-165). Ticking
        // here is safe: this is still the only thread touching the single-threaded facade (the
        // tick thread starts below) and _tickActions cannot have anything in it yet, because the
        // C2SIM connect that starts the event flow is step 3.
        // OBSERVATION ONLY - unlike the tools, a timeout is NOT a refusal: it logs a WARNING and
        // both arms continue exactly as before (a back-end discovered later is still used), so
        // no behaviour, exit code or existing log line changes.
        {
            var settleCap = TimeSpan.FromSeconds(15);
            var swSettle = System.Diagnostics.Stopwatch.StartNew();
            int backends = 0;
            while (swSettle.Elapsed < settleCap)
            {
                try { _bridge.Tick(); }
                catch (Exception e)
                {
                    _log.LogWarning("Backend settle: Tick failed ({Msg}); ending the settle early.", e.Message);
                    break;
                }
                backends = _bridge.BackendCount();
                if (backends > 0) break;
                Thread.Sleep(50);
            }
            if (backends > 0)
            {
                _log.LogInformation("Backend discovered (BackendCount={Count}) after {Secs:F1} s.",
                                    backends, swSettle.Elapsed.TotalSeconds);
                // The operator's one-line state (DEMO_READINESS row 15): joined, talking to a sim,
                // which type mapping is in force, and what happens next.
                _log.LogInformation("READY - joined the federation, {Count} VR-Forces back-end(s), type mapping = {Mode}, " +
                                    "compose = {Compose}, position reports every {Secs}s. Waiting for the C2SIM " +
                                    "initialization (clientId={ClientId}).",
                                    backends, _vrf.TypeMappingMode, _vrf.ComposeHierarchy ? "on" : "off",
                                    _vrf.PositionReportSeconds > 0 ? _vrf.PositionReportSeconds.ToString() : "off (0)",
                                    _vrf.ClientId);
            }
            else
                _log.LogWarning("NO BACKEND DISCOVERED after {Secs:F1} s (BackendCount=0). This interface is " +
                                "joined but has seen no VR-Forces simulation back-end, so creates and tasks " +
                                "would be silent no-ops. Confirm VR-Forces is running with a scenario loaded " +
                                "on the same RTI/exercise. Continuing anyway - a back-end that appears later " +
                                "is still used.", swSettle.Elapsed.TotalSeconds);
        }

        // STP-822: say whether the TIMED liveness read is armed, at start-up, where an operator
        // reads it - the absence of this line means a build that can be blind to a dead back end
        // for a whole run.
        if (_vrf.BackendLivenessSeconds > 0)
            _log.LogInformation("BACK-END LIVENESS (STP-822): the back end is re-read every {Secs}s on the "
                              + "wall clock; a count of ZERO held for {Conf}s (and at least {N} consecutive "
                              + "samples) is reported as a LOSS - TASKABRT for every running task, one "
                              + "ObservationReport, position reports suspended. Nav-area gate for ground "
                              + "tasks = {Gate}.",
                                _vrf.BackendLivenessSeconds, _vrf.BackendLossConfirmSeconds,
                                BackendLivenessPolicy.ConfirmSamples,
                                _vrf.RequireNavAreaForGroundTasks ? "ON (Vrf:RequireNavAreaForGroundTasks)" : "off");
        else
            _log.LogWarning("BACK-END LIVENESS IS OFF (Vrf:BackendLivenessSeconds=0). This interface will "
                          + "NOT notice a VR-Forces back end that stops: it keeps reporting positions off "
                          + "reflected attributes the RTI still holds, which is the V6d defect (STP-822).");

        // 2. Drive the sim on a dedicated thread (drain queued commands, then Tick).
        // The tick loop runs until _stopTick (NOT the host stoppingToken) so the shutdown
        // path can still enqueue + flush cleanup deletes while it is ticking (see step 5).
        var tickThread = new Thread(TickLoop)
        {
            IsBackground = true,
            Name = "vrf-tick"
        };
        tickThread.Start();

        // P4b: start the periodic POSITION-bundle flush loop (ONLY when bundling is enabled). It
        // force-flushes a partial bundle every BundleFlushMs so a trickle of reports is not held
        // (C++ reminder thread). Detached, gated on _stoppingToken; the stop path does the final
        // flush. When BundlePositionReports is false this never starts (default-off = no behavior).
        if (_vrf.BundlePositionReports && _vrf.BundleFlushMs > 0)
            _ = PositionBundleFlushLoopAsync();

        // 3. Connect to C2SIM to start receiving init/orders.
        try
        {
            await _sdk.Connect();
            _log.LogInformation("Connected to C2SIM ({Rest} / {Stomp}). clientId={ClientId}.",
                                _sdk.RestEndpoint, _sdk.StompEndpoint, _vrf.ClientId);

            // Late-join (parity: the C++ interface QUERYINITs at startup, RUNBOOK sec 3):
            // pull the CURRENT shared init, since the server is typically already RUNNING
            // with an init pushed BEFORE we connected. STOMP only delivers FUTURE messages,
            // so without this we would create 0 units.
            try
            {
                string shared = await _sdk.JoinSession();
                if (!string.IsNullOrWhiteSpace(shared) && shared.Contains("<Unit", StringComparison.Ordinal))
                    ProcessInitialization(shared, "late-join QUERYINIT");
                else
                    _log.LogInformation("Late-join: server has no current init to share ({Len} chars).",
                                        shared?.Length ?? 0);

                // Start the simulation clock (parity: C++ facade()->Run() on RUNNING,
                // C2SIMinterface.cpp:1819/1917). Enqueued after the creates so units exist
                // when the sim advances. Without this the sim never runs and tasked units
                // never move or complete (no TASKCMPLT). The server is RUNNING at late-join.
                _tickActions.Enqueue(() => _bridge.Run());
                if (_vrf.TimeMultiplier > 1)
                    _tickActions.Enqueue(() => _bridge.SetTimeMultiplier(_vrf.TimeMultiplier));
                _log.LogInformation("Sim Run() queued (start the VR-Forces clock; timeMult={Mult}).",
                                    _vrf.TimeMultiplier);
            }
            catch (Exception ex)
            {
                _log.LogWarning("Late-join QUERYINIT failed: {Msg}", C2SIMSDK.GetRootException(ex).Message);
            }
        }
        catch (Exception e)
        {
            _log.LogError("C2SIM connect failed: {Msg}", C2SIMSDK.GetRootException(e).Message);
        }

        // 4. Idle until shutdown; SDK events drive the work.
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* normal on stop */ }

        // 5. Clean shutdown: delete created objects, stop the tick loop, disconnect, tear down.
        _log.LogInformation("Shutting down...");

        // Solution A (RUNBOOK sec 8): delete every VR-Forces object this run created so they do
        // NOT accumulate across runs (accumulation degrades create/route reflection - sec 7 - and
        // is why a manual scenario reload was needed between runs). Enqueue deleteObject onto the
        // tick thread while it is STILL running, wait for the queue to drain, then a moment for the
        // messages to flush to the backend, BEFORE stopping the tick + resigning. This deletes only
        // what THIS run created (tracked in _names); orphans from crashes/force-kills need
        // the hard reset (tools/ResetVrf). Opt out via Vrf:CleanupCreatedOnStop=false.
        if (_vrf.CleanupCreatedOnStop)
        {
            try
            {
                var created = _names.CreatedUuids();
                if (created.Count > 0)
                {
                    _log.LogInformation("Cleanup: deleting {N} created VR-Forces objects before resign...",
                                        created.Count);
                    foreach (var u in created) _tickActions.Enqueue(() => _bridge.DeleteObject(u));
                    // The tick loop drains the whole queue in one iteration, then ticks flush the
                    // messages. Bounded so shutdown stays well under the host's 30s budget.
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (!_tickActions.IsEmpty && sw.Elapsed < TimeSpan.FromSeconds(8)) Thread.Sleep(50);
                    Thread.Sleep(1500); // extra ticks to flush the delete messages over the network
                    _log.LogInformation("Cleanup: {N} deletes dispatched ({Ms} ms).", created.Count, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception e)
            {
                _log.LogWarning("Cleanup-on-stop failed: {Msg}", C2SIMSDK.GetRootException(e).Message);
            }
        }

        // P4b: flush any pending POSITION bundle BEFORE resign so no accumulated fixes are lost.
        // The periodic flush loop has already stopped here (_stoppingToken is cancelled), and its
        // snapshot-under-lock serializes with this one - no double-send / no loss. AWAIT the push
        // so the bundle reaches C2SIM before the SDK Disconnect below. Default-off: the buffer is
        // always empty on the non-bundling path (no-op).
        try { await FlushPositionBundle(); }
        catch (Exception e)
        {
            _log.LogWarning("Flush-on-stop position bundle failed: {Msg}",
                            C2SIMSDK.GetRootException(e).Message);
        }

        _log.LogInformation("Reports this run: {Sent} delivered, {Failed} FAILED (a failed report is lost - " +
                            "it is never re-sent).", Interlocked.Read(ref _reportsSent), Interlocked.Read(ref _reportsFailed));
        // N8: the FINAL tile figure, beside the other end-of-run totals. Best-effort - a logging
        // line must never turn a clean shutdown into a failure.
        try { ReportRunTileTotal(); } catch { /* best effort */ }
        _stopTick = true;
        tickThread.Join(TimeSpan.FromSeconds(5));
        try { await _sdk.Disconnect(); } catch { /* best effort */ }
        _bridge.Stop();
        _bridge.Dispose();
    }

    /// <summary>
    /// V4b - THE ONE CONTROL-AREA FACTORY. Register the object's NAME, then marshal the create onto
    /// the tick thread with the C2SIM uuid as the VR-Forces uuid (createControlArea's startingUUID,
    /// vrfRemoteController.h:1091-1108, the same policy the init has always used for the 35
    /// tactical areas).
    ///
    /// THE ORDER OF THE TWO STEPS IS THE POINT, not an implementation detail. A returned name the
    /// NameRegistry never saw requested falls through to the prefix scan, whose job is to attach a
    /// truncated DIS marking to the unit it came from - so an unregistered graphic name that is a
    /// strict prefix of a still-unbound UNIT name would bind the GRAPHIC's uuid under the UNIT's
    /// name, and the unit would then be tasked by its area's uuid. Registering first makes the
    /// callback an EXACT match and short-circuits the scan.
    ///
    /// The caller owns the duplicate guard, because the two callers key it differently: the init by
    /// the graphic's own C2SIM uuid ("area:..."), a task-derived objective by the TASK's uuid
    /// (TaskGeometryInterpretation.ObjectiveAreaKey, "taskarea:...").
    /// </summary>
    private void EnqueueControlAreaCreate(string uuid, string name,
                                          IReadOnlyList<(double Lat, double Lon, double? Elev)> points)
    {
        var pts = points
            .Select(pt => new Geodetic { LatDeg = pt.Lat, LonDeg = pt.Lon, AltMeters = pt.Elev ?? 0.0 })
            .ToList();
        _names.Requested(name);   // B3: so the area's ObjectCreated is an EXACT match, not a prefix scan
        _tickActions.Enqueue(() => _bridge.CreateControlArea(pts, name, "TacticalArea", uuid));
    }

    private void TickLoop()
    {
        while (!_stopTick)
        {
            while (_tickActions.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception e) { _log.LogError("Tick action failed: {Msg}", e.Message); }
            }
            try { _bridge.Tick(); }
            catch (Exception e) { _log.LogError("Tick failed: {Msg}", e.Message); }
            // M1 (cold-start review 02b51de). EVERY phase below gets the same guard the action()
            // drain has. An unhandled throw on THIS thread ends the thread and, because Program.cs
            // installs no AppDomain.UnhandledException handler that can stop it, the PROCESS - in
            // the middle of a run, with nothing in our own log to say why. The concrete shape the
            // review found: a managed-only refresh of a deployed folder (new VrfC2SimApp.dll beside
            // an OLD VrfBridge.dll) makes the R1 poll JIT TryGetEntityKinematics ten seconds in and
            // throw MissingMethodException. Guarding does not make a stale deploy correct - it makes
            // it a loud, repeating ERROR line naming the phase instead of a dead interface.
            // M2: the task-clock axis is advanced BEFORE anything reads it. Always on - the axis
            // carries the wall clock too, and the gates wait on it whatever Vrf:TaskClock says.
            TickPhase("SampleTaskClock", true, SampleTaskClock);
            TickPhase("ExpireTerrainRequests", !_pendingTerrain.IsEmpty, ExpireTerrainRequests);
            TickPhase("ExpireShiftRequests", !_pendingShift.IsEmpty, ExpireShiftRequests);
            // PLACEMENT RE-CLAMP (2026-09-21). The guard is the whole cost control: on a run whose
            // init terrain query is answered, _reclamp is EMPTY and this phase never runs - no
            // query, no native read, no line. It is placed after the terrain expiry so a reply that
            // landed on this tick is applied before the sweep decides whether to ask again.
            TickPhase("SweepPlacementReclamp",
                      _vrf.PlacementReclamp && !_reclamp.IsEmpty && !_reclampConcluded,
                      SweepPlacementReclamp);
            TickPhase("ExpireCompositions", !_compositions.IsEmpty, ExpireCompositions);
            TickPhase("ReleaseReflected", !_awaitReflection.IsEmpty, ReleaseReflected);
            // DEFER, DO NOT ABORT (D5b): "bound" is an event (ObjectCreated), but "its location
            // reads now" is only ever a poll, and the init barrier has to expire even when nothing
            // else happens - so the sweep runs here as well as on every ObjectCreated. It costs
            // nothing once the init has settled and no task is held.
            TickPhase("SweepDispatchReadiness",
                      !_dispatchWaits.IsEmpty || (!_initSettled && _initPlannedUtc != DateTime.MinValue),
                      SweepDispatchReadiness);
            // STP-822: BEFORE the R1 poll, so a loss confirmed on this tick suppresses THIS
            // tick's position reports rather than the next one's.
            TickPhase("MaybeCheckBackendLiveness", _vrf.BackendLivenessSeconds > 0, MaybeCheckBackendLiveness);
            TickPhase("MaybeSendPositionReports", _vrf.PositionReportSeconds > 0, MaybeSendPositionReports);
            TickPhase("MaybeCheckArrivals", _vrf.ArrivalCompletion, MaybeCheckArrivals);
            TickPhase("MaybeCheckStalls", _vrf.StallDetection, MaybeCheckStalls);
            TickPhase("MaybeCompleteTimedTasks", _vrf.TimedCompletion, MaybeCompleteTimedTasks);
            Thread.Sleep(50);
        }
    }

    /// <summary>One guarded tick-loop phase (M1). <paramref name="enabled"/> keeps the call site's
    /// own gate - a disabled phase costs nothing and cannot throw - and the phase NAME goes into the
    /// log so a repeating failure is attributable without a debugger. Never rethrows: the tick
    /// thread is the simulation's clock and must outlive any one phase.</summary>
    private void TickPhase(string phase, bool enabled, Action body)
    {
        if (!enabled) return;
        try { body(); }
        catch (Exception e)
        {
            _log.LogError("Tick phase '{Phase}' FAILED ({Type}): {Msg} - the tick loop continues. A " +
                          "MissingMethodException here means the deployed VrfBridge.dll is older than " +
                          "VrfC2SimApp.dll (docs/RUNBOOK.md deploy section: rebuild the bridge and ALL " +
                          "TEN consumers so every bin copy is one hash).",
                          phase, e.GetType().Name, C2SIMSDK.GetRootException(e).Message);
        }
    }

    // R1: the oracle's periodic position-report loop (C2SIMinterface.cpp:388-460), on the tick
    // thread: every PositionReportSeconds, for every unit WE created that passes the side filter,
    // read its reflected location (TryGetEntityGeodetic - entity or aggregate, the port of
    // getUnitGeodeticFromSim) and push one C2SIM PositionReport; units with no reflected object
    // yet are skipped (the oracle printed "CAN'T MAKE POSITION REPORT" and continued). When
    // Vrf:BundlePositionReports is on, fixes go through the P4b bundle instead of one report each.
    private void MaybeSendPositionReports()
    {
        var now = DateTime.UtcNow;
        if (now < _nextPositionReport) return;
        _nextPositionReport = now.AddSeconds(_vrf.PositionReportSeconds);

        // STP-822: A STOPPED BACK END MUST NOT BE REPORTED AS A LIVE ONE. The RTI keeps every
        // reflected object after the back end stops, so TryGetEntityGeodetic goes on succeeding
        // and this loop would deliver fix after fix off attributes that stopped changing - 543 of
        // them on run 20260915T130627Z, 0 failed, while nothing in the simulation moved. The fixes
        // are SUPPRESSED (not faked, not zeroed), counted, and the suspension is said once; the
        // count goes into the recovery line.
        if (_backendLost)
        {
            _positionCyclesSuppressed++;
            if (!_livenessSuppressionSaid)
            {
                _livenessSuppressionSaid = true;
                _log.LogWarning("R1 position reports SUSPENDED: the VR-Forces back end is LOST (STP-822). "
                              + "Its reflected attributes are still READABLE but they are STALE, so no fix is "
                              + "sent until it reports again.");
            }
            return;
        }

        bool blue = !_vrf.PositionReportSides.Equals("red", StringComparison.OrdinalIgnoreCase);
        bool red  = !_vrf.PositionReportSides.Equals("blue", StringComparison.OrdinalIgnoreCase);
        int sent = 0, unresolved = 0, unreflected = 0, noKinematics = 0;
        List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)> bundle =
            _vrf.BundlePositionReports ? new() : null;
        foreach (var kv in _unitByC2SimUuid)
        {
            string c2simUuid = kv.Key, name = kv.Value.Name;
            bool hostile = _hostilityByC2SimUuid.TryGetValue(c2simUuid, out var h) && h == "HO";
            if (hostile ? !red : !blue) continue;
            // B3: the two reasons a unit is skipped are NOT the same fault and used to be counted
            // together as "no reflected object yet". NAME UNRESOLVED means no ObjectCreated has ever
            // bound a uuid to this unit's name - the state that hid the truncated-marking bug for
            // nine hours. Each is named ONCE, the first time it happens, not every cycle.
            if (!_names.TryGetUuid(name, out var vrfUuid))
            {
                unresolved++;
                if (_r1Unresolved.TryAdd(name, 0))
                    _log.LogWarning("R1: unit {Name} has NO VR-Forces uuid - no ObjectCreated has bound that name. " +
                                    "It will be skipped by the position poll, the arrival check and the stall " +
                                    "watchdog until one does.", name);
                continue;
            }
            if (!_bridge.TryGetEntityGeodetic(vrfUuid, out var g))
            {
                unreflected++;
                if (_r1Unreflected.TryAdd(name, 0))
                    _log.LogInformation("R1: unit {Name} ({Uuid}) is created but not REFLECTED yet - no position " +
                                        "to report for it (HLA discovery lags the create callback).", name, vrfUuid);
                continue;
            }
            // B7 (STP-784): heading + ground speed from the SAME reflected object. A failed
            // kinematics read is NOT a skipped position - the fix still goes out, just without
            // the two optional elements (ReportBuilder omits them; a 0 would claim the unit is
            // stopped and facing north). Speed/heading are reported even when zero, because a
            // SUCCESSFUL read of a stationary unit is real data the watchdog validation needs.
            double? headingDeg = null, speedMps = null;
            if (_bridge.TryGetEntityKinematics(vrfUuid, out var spd, out var hdg))
            {
                speedMps = spd;
                headingDeg = hdg;
            }
            else noKinematics++;
            if (bundle != null) { bundle.Add((c2simUuid, g.LatDeg, g.LonDeg, headingDeg, speedMps)); continue; }
            // NAMED arguments deliberately: the facade/bridge read is (speed, heading) but the
            // builder takes (heading, speed) - the schema's element order - and both are
            // double?, so a positional swap here would compile and silently report a unit's
            // speed as its heading.
            _ = PushReportAsync(ReportBuilder.BuildPositionReport(c2simUuid, g.LatDeg, g.LonDeg,
                                                                 IsoNow(), NewReportId(),
                                                                 headingDeg: headingDeg, speedMps: speedMps));
            sent++;
        }
        if (bundle is { Count: > 0 })
        {
            List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)> snapshot;
            lock (_posBundleLock)
            {
                foreach (var b in bundle) _posBundle.Add(b);
                snapshot = DrainBundleLocked();
            }
            // m2: `sent` is THIS CYCLE's count in every other branch, so it must be here too.
            // snapshot.Count is the whole DRAINED buffer (this cycle's fixes plus anything an
            // earlier cycle left behind), and the drain returns null when the buffer is not yet due
            // - which used to report 0 for a cycle that had just added fixes. Count what we added.
            if (snapshot != null && snapshot.Count > 0) _ = PushBundleSnapshot(snapshot);
            sent = bundle.Count;
        }
        // The first four numbers are THIS cycle's; the last pair is the run's total across every
        // report kind (review finding 12 - the unlabelled pair read as a second per-cycle count).
        _log.LogInformation("R1 position reports: {Sent} sent, {Unresolved} skipped (NAME UNRESOLVED - no VRF uuid), " +
                            "{Unreflected} skipped (no reflected object yet), {NoKin} without heading/speed " +
                            "(kinematics read failed), sides={Sides}, every {Secs}s; " +
                            "cumulative: {Delivered} sent, {Failed} failed.",
                            sent, unresolved, unreflected, noKinematics, _vrf.PositionReportSides,
                            _vrf.PositionReportSeconds,
                            Interlocked.Read(ref _reportsSent), Interlocked.Read(ref _reportsFailed));
    }

    // R1 diagnostics: units already named as unresolved / unreflected, so each is reported ONCE
    // (the pre-B3 line repeated its count every cycle and named nothing - 9 h of "1 skipped").
    private readonly ConcurrentDictionary<string, byte> _r1Unresolved = new();
    private readonly ConcurrentDictionary<string, byte> _r1Unreflected = new();

    private StartupConfig BuildStartupConfig()
    {
        var c = new StartupConfig
        {
            Protocol = _vrf.Protocol.Equals("Dis", StringComparison.OrdinalIgnoreCase)
                           ? VrfProtocol.Dis : VrfProtocol.Hla1516e,
            ApplicationNumber = _vrf.ApplicationNumber,
            SiteId = _vrf.SiteId,
            SessionId = _vrf.SessionId,
            HostInetAddr = _vrf.HostInetAddr,
            Federation = _vrf.Federation,
            FedFileName = _vrf.FedFileName,
            ConnectionConfigFile = _vrf.ConnectionConfigFile
        };
        // The VR-Forces-level UDP / best-effort interface (Vrf:DeviceAddress). Passed through
        // as configured, INCLUDING an explicit empty string: empty suppresses --deviceAddress
        // on the 5.2 HLA argv (VrfFacade::Start pushes it only when non-empty), which is the
        // unpinned arm the 5.2 interface question still needs (PREREG_52_RTIEXEC sec 4 changed
        // this address together with the RTI connection mode and never separated the two).
        // Guarded against null only, so a stack whose settings omit the key keeps the bridge's
        // own default rather than blanking the DIS argv.
        if (_vrf.DeviceAddress != null) c.DeviceAddress = _vrf.DeviceAddress;
        // 5.2 CONFIG-FILE JOIN (Vrf:ConfigFileIdentity, set by the runner's -VrfProfile 5.2).
        // Submit NO identity: VrfFacade::Start then pushes neither --execName nor --fedFileName
        // and MAK-ONE-2025-Config.xml supplies both, and the FOM module list stays EMPTY because
        // config modules are ADDITIVE (DIFF row A9). Same rule as tools/Shared/StackIdentity.cs.
        // The stack the process really binds is a RUNTIME fact, so it is logged (and cross-checked
        // by the runner) from NativeStackInfo after Start() - a build flag could contradict it.
        if (_vrf.ConfigFileIdentity)
        {
            c.Federation = "";
            c.FedFileName = "";
            c.FomModules.Clear();
            string stack = VrfBridge.NativeStackInfo() ?? "";
            _log.LogInformation("Vrf:ConfigFileIdentity - joining the CONFIG-FILE way: no --execName, "
                              + "no --fedFileName, FOM modules cleared. Native stack (pre-Start) = {Stack}.", stack);
            if (!stack.StartsWith("5.2", StringComparison.Ordinal))
                _log.LogWarning("Vrf:ConfigFileIdentity is set but this build reports stack '{Stack}'. "
                              + "Config-file identity is a 5.2 join; on 5.0.2 it would submit an EMPTY "
                              + "federation name. Check which VrfC2SimApp build was deployed.", stack);
            return c;
        }
        if (_vrf.FomModules != null)
            foreach (var m in _vrf.FomModules) c.FomModules.Add(m);
        return c;
    }

    // ================= C2SIM -> VR-Forces (inbound) =================
    // These fire on SDK threads. They PARSE C2SIM XML and enqueue bridge commands
    // onto _tickActions. The parse/translate is the Phase 4 parity port - see the
    // C++ sources named in each TODO and docs/PORT.md sec 10 / TASK_EXPANSION_PLAN.md.

    private void OnStatusChanged(object sender, C2SIMSDK.C2SIMNotificationEventParams e)
    {
        // The STOMP status broadcast body is EMPTY (<SystemMessageBody/>) and the header
        // carries no state - so a substring test on e.Body NEVER matches. Use this event
        // purely as a trigger and read the real state via REST GetStatus() (which parses
        // sessionState, like the C++ interface). The interface exits on UNINITIALIZED
        // (RUNBOOK sec 4) - e.g. driven there by tools/StopIface (STOP then RESET).
        _ = OnStatusChangedAsync();
    }

    private async Task OnStatusChangedAsync()
    {
        C2SIMSDK.C2SIMServerStatus status;
        try { status = await _sdk.GetStatus(); }
        catch (Exception ex)
        {
            _log.LogWarning("GetStatus failed: {Msg}", C2SIMSDK.GetRootException(ex).Message);
            return;
        }
        _log.LogInformation("C2SIM server state -> {State}.", status);
        if (status == C2SIMSDK.C2SIMServerStatus.UNINITIALIZED)
        {
            _log.LogInformation("Server UNINITIALIZED; initiating clean stop.");
            _life.StopApplication();
        }
        else if (status == C2SIMSDK.C2SIMServerStatus.RUNNING)
        {
            // Parity: the C++ interface runs the sim on RUNNING (C2SIMinterface.cpp:1819).
            _tickActions.Enqueue(() => _bridge.Run());
            _log.LogInformation("Server RUNNING; sim Run() queued.");
        }
    }

    private void OnInitialization(object sender, C2SIMSDK.C2SIMNotificationEventParams e)
        => ProcessInitialization(e.Body, "InitializationReceived");

    // Shared by the live InitializationReceived event and the on-connect late-join
    // (JoinSession/QUERYINIT). Parses the init then dispatches each unit through
    // UnitTranslator (the faithful port of extractC2simInit's factories).
    private void ProcessInitialization(string body, string source)
    {
        // Review fix (wf_dcad86e3): the late-join QUERYINIT (ExecuteAsync thread) and a broadcast init
        // (STOMP pump) can overlap; the body is synchronous and only enqueues bridge work, so one lock
        // serialises them without any deadlock risk.
        lock (_initLock) ProcessInitializationLocked(body, source);
    }

    private void ProcessInitializationLocked(string body, string source)
    {
        _log.LogInformation("C2SIM Initialization ({Source}, {Len} bytes).", source, body?.Length ?? 0);

        InitData init;
        try { init = InitParser.Parse(body); }
        catch (Exception ex) { _log.LogError("Init parse failed: {Msg}", ex.Message); return; }

        int planned = 0, matched = 0, duplicates = 0;
        // R9 type-mapping fix (docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md). "GoldenParity"
        // reproduces the byte-for-byte golden-trace objectTypes; anything else (default
        // "RealTemplates") maps ArmorPlatoon to the real Tank Platoon (USA) Cell-C mover.
        var typeMapping = UsingFidelityTable ? TypeMapping.FidelityTable
            : string.Equals(_vrf.TypeMappingMode, "GoldenParity", StringComparison.OrdinalIgnoreCase)
            ? TypeMapping.GoldenParity : TypeMapping.RealTemplates;
        if (typeMapping == TypeMapping.FidelityTable)
            _log.LogInformation("Type-mapping mode = FidelityTable (ground dispatch from {File}; " +
                                "friendly={Friendly}, opposing={Opposing}).",
                                _typeMap.SourcePath, _nations.Friendly, _nations.Opposing);
        else
            _log.LogInformation("Type-mapping mode = {Mode} (ArmorPlatoon -> {Target}).",
                typeMapping, typeMapping == TypeMapping.GoldenParity ? "Ground_Aggregate (11.1.225.1.1.3.0)" : "Tank Platoon (USA) (11.1.225.3.2.0.0)");
        int unmapped = 0;
        var proxiesToReport = new List<(string Uuid, string Name, string Marking, string Substitution)>();
        var toCreate = new List<CreationPlan>();   // collected, then (optionally) de-stacked, then enqueued
        var placements = new List<PlacementInput>();   // index-parallel to toCreate (see PlacementInput)
        // Index-parallel to toCreate: (this unit's C2SIM uuid, its declared Superior uuid) - the raw
        // material for Vrf:ComposeHierarchy parent/child classification (ApplyHierarchyComposition).
        var hierarchy = new List<(string Uuid, string SuperiorUuid)>();
        // Index-parallel to toCreate: the ECHELON KEY the composed-sibling de-stack sizes its rings
        // from (user ruling 2026-09-21; EchelonSpacing.KeyOf of the init's own EchelonCode/SIDC).
        // "" = an echelon the table does not cover, which is a FALLBACK, not a spacing.
        var echelons = new List<string>();
        // SF-3: index-parallel (unit name, resolved key, WHY that key) - logged once per init so
        // "which row sized this ring" is in the record rather than in a reader's head.
        var echelonProvenance = new List<(string Name, string Key, string Why)>();
        // parent C2SIM uuid -> its AUTHORED <Subordinate> uuid order (N2: the attach order = the
        // declared order, so the declared first child becomes the leader, UG52 18.1.1).
        var declaredByParent = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var u in init.Units)
        {
            if (string.IsNullOrEmpty(u.Uuid)) continue;
            if (u.SystemName != _vrf.ClientId) continue;          // only our units (RUNBOOK sec 2)
            matched++;
            // Guard duplicate init delivery (late-join QUERYINIT + a broadcast can both
            // arrive): a unit we already planned/created must not be created twice.
            if (_unitByC2SimUuid.ContainsKey(u.Uuid)) { duplicates++; continue; }
            if (string.IsNullOrEmpty(u.HostilityCode))
            {
                _log.LogWarning("Unit {Name} missing Hostility - skipping.", u.Name);
                continue;
            }
            _hostilityByC2SimUuid[u.Uuid] = u.HostilityCode;

            var unit = u;
            if (string.IsNullOrEmpty(unit.Latitude) || string.IsNullOrEmpty(unit.Longitude))
            {
                // TODO(parity): fall back to the SUPERIOR unit's lat/lon (needs the
                // superior map from the parser). For now, skip.
                _log.LogWarning("Unit {Name} missing lat/lon - skipping (parent fallback TODO).", unit.Name);
                continue;
            }
            // ORACLE PARITY ONLY. The oracle invented "1000.0" when C2SIM gave no altitude
            // (C2SIMinterface.cpp:1378-1379) on the folk belief that "1000.0 triggers VRForces
            // Gound Clamping" (:685) - not a VR-Forces behaviour, just "above the ground at Bogaland".
            // The string feeds UnitTranslator's byte-parity plan (pos.AltMeters, PostCreateAltitude)
            // which only the Fixed100 path acts on. The Live/TerrainProfile PLACEMENT below ignores it
            // and reads the typed AltitudeAgl/AltitudeMsl instead.
            if (string.IsNullOrEmpty(unit.ElevationAgl))
                unit = unit with { ElevationAgl = "1000.0" };

            var plan = UnitTranslator.Plan(unit, typeMapping, _typeMap, _nations);

            // FidelityTable: a row that is AUTHORED_PENDING (a declared coverage gap) or a key that
            // matched nothing FAILS LOUDLY and the unit is NOT created. Emitting anything here would
            // land a zero-subordinate Country-0 abstract or Ground_Aggregate - an EMPTY unit that
            // looks created and can never fight (survey sec 3.5). Never an intentional fallthrough.
            if (plan.Fidelity is TypeFidelity.AuthoredPending or TypeFidelity.Failed)
            {
                unmapped++;
                _log.LogError("TYPE MAP {Fidelity}: unit {Name} (SIDC '{Sidc}', echelonCode '{Ech}') has NO " +
                              "usable VR-Forces template and is NOT created. {Note} {Why}",
                              plan.Fidelity, unit.Name, unit.SymbolId, unit.EchelonCode,
                              plan.MapNote, plan.Substitution);
                continue;
            }
            if (typeMapping == TypeMapping.FidelityTable)
            {
                // R-SURFACE-PROXY: annotate the MARKING (bounded - see VrfSettings.ProxyMarkingTag)
                // and queue the substitution for the report stream. The log line always carries the
                // full text, whether or not the marking had room for the tag.
                if (plan.Fidelity == TypeFidelity.Proxy && _vrf.SurfaceProxySubstitutions)
                {
                    string tagged = plan.Name + _vrf.ProxyMarkingTag;
                    if (tagged.Length <= MaxVrfMarkingChars) plan = plan with { Name = tagged };
                    else
                        _log.LogWarning("Proxy marking tag NOT appended to '{Name}': '{Tagged}' exceeds the " +
                                        "{Max}-character marking-text limit; the substitution is still " +
                                        "reported and logged.", plan.Name, tagged, MaxVrfMarkingChars);
                    proxiesToReport.Add((unit.Uuid, unit.Name, plan.Name, plan.Substitution));
                }
                _log.LogInformation("TYPE MAP {Fidelity}: {Name} -> {Template} ({Type}) [{Note}]{Sub}",
                                    plan.Fidelity, plan.Name, plan.TemplateName,
                                    FormatSpec(plan.Type), plan.MapNote,
                                    plan.Substitution.Length == 0 ? "" : " " + plan.Substitution);
            }

            // Create-time terrain-clamp fix (docs/SUPERVISED_RECOVERY_PLAN.md sec 3b;
            // MOJAVE_ROOTCAUSE_INVESTIGATION parts 13/13c). Ground units are otherwise born at a
            // fixed MSL (ElevationAgl default 1000) that sits BELOW high-elevation terrain: VRF's
            // create ground-clamp can DROP an above-terrain birth to the surface but cannot RAISE a
            // below-terrain one, so the unit is born BURIED.
            //   *** CORRECTED 2026-09-04: this sentence used to end "...and never executes
            //   movement". THAT IS FALSIFIED - birth altitude is not the freeze discriminator
            //   (CORRECTIONS_LOG "Birth altitude"). Burial is real; the freeze link is not.
            //   The clamp direction itself IS now verified - PREREG_CLAMP_DIRECTION_2026-09-04.
            //   ALSO: burial is avoidable WITHOUT this workaround - setAltitude takes an
            //   aboveGroundLevel flag (vrfRemoteController.h:1372) and VrfFacade.cpp:739 already
            //   passes TRUE; the "SKIP the deferred SetAltitude" branch below is what stops it
            //   firing. See docs/VRF_ALTITUDE_FRAMES.md before changing this. ***
            // Gated on
            // the SAME Vrf:GroundWaypointAltitudeMode string the route path uses (case-insensitive
            // "Live") and the SAME per-unit ground predicate the route path applies (SIDC battle-
            // dimension char at index 2 == 'G'; the route path reads it off CreatedUnit.SymbolId,
            // which is this same unit.SymbolId - constant across the unit's tasks, so per-unit).
            //   Fixed100 (parity): create at the plan altitude + register the deferred SetAltitude.
            //   Live + GROUND (RETIRED 2026-09-05): used to create at 10000 m MSL and SKIP the
            //     deferred SetAltitude. Now: PlacementPolicy (authored lat/lon + AGL set).
            //   Live + NON-ground (air/sea): parity behavior, unchanged.
            //
            // *** THE "Live + GROUND" BRANCH IS DEPRECATED - WRONG FRAME. The deferred
            // SetAltitude it skips is an AGL set (VrfFacade.cpp:739 passes aboveGroundLevel
            // TRUE), and an AGL set places a unit on the ground in ONE call with no birth
            // altitude and no terrain query. VERIFIED 2026-09-04: a buried entity at -0.0 m
            // was lifted to the surface by exactly that call (PREREG_CLAMP_DIRECTION sec 8a,
            // tools/SetAlt). So this branch skips the correct mechanism in favour of a
            // workaround for a frame we chose ourselves. Retirement = stop overriding the
            // create altitude and stop skipping the deferred SetAltitude; it needs a prereg +
            // confirming run because it changes creation for every unit. Do not "fix" it by
            // re-justifying the 10000 m birth. Canonical: docs/VRF_ALTITUDE_FRAMES.md. ***
            // PLACEMENT (Live / TerrainProfile modes). Every line below has a documented basis:
            //  - C2SIM states altitude as AltitudeAGL ("distance vertically above ground level") or
            //    AltitudeMSL ("above mean sea level"), BOTH OPTIONAL (C2SIM_SMX_LOX_CWIX2024.xsd
            //    :155, :163, :2716-2717). Every init in data/ carries NEITHER.
            //  - VR-Forces places a created object on the terrain by default: createEntity /
            //    createAggregate default groundClamp=true (vrfRemoteController.h:1275, :1291); the
            //    create message: "placed on the nearest polygon" (ifCreateVrfObject.h:210-212).
            //  - The placement RULE is the simulator's: "ground ... entities are placed on the ground
            //    ... at the highest possible terrain intersection" (UG52 14.3.3, help
            //    vrf_newEntityPlacement.htm); each member platform is place()d with clampToGround
            //    (vrfMovingObjectStateRepository.h:251-253). The AGL set below is belt-and-braces:
            //    setAltitude(uuid, m, aboveGroundLevel) (vrfRemoteController.h:1372-1374; VrfFacade.cpp
            //    :739 passes TRUE) - documented "ignored if the vehicle is not an air-going vehicle"
            //    (setAltitudeRequest.h:24-25) yet observed to lift a ground M1A2 (PREREG_CLAMP_DIRECTION
            //    sec 8a). On a UNIT it is documented to do NOTHING (no altitude callback in either unit
            //    set-controller); the documented unit lever is setLocation -> snap-into-formation ->
            //    per-member clamp (setLocationRequest.h:31-32). See PlacementPolicy.cs header.
            //  - Domain is the DIS domain of the type we create (SISO-REF-010.xml:3116-3119:
            //    1 Land, 2 Air, 3 Surface, 4 Subsurface), not the SIDC symbology character the
            //    oracle tested (C2SIMinterface.cpp:2158).
            // So the create position is the AUTHORED lat/lon, and its ALTITUDE is - since 2026-09-05 -
            // the back end's OWN TERRAIN HEIGHT under that point plus Vrf:CreateClearanceMeters, i.e.
            // the object is created AT the surface. That is MAK's own documented pattern, twice over:
            // the shipped remoteControl sample creates a Tank_Plt and its M1A2 members at a point that
            // is already at the terrain ("Points are from Ala Moana terrain",
            // commandLineRemoteController.cxx:710-772 - 1.0 m above the ellipsoid there) and never
            // calls setAltitude. The terrain height comes from one DtIfRequestTerrainProfileInformation
            // for ALL create positions of the init (ifRequestTerrainProfileInformation.h:45-51), issued
            // in the block after the de-stack; when it does not answer within
            // TerrainProfileTimeoutSeconds the creates go out at the FALLBACK altitude - C2SIM's MSL if
            // given, else 0 - which is exactly what this code did before, with a WARN naming it.
            // RETIRED here 2026-09-05 (user direction): the 10000 m MSL birth + skipped SetAltitude,
            // and the SIDC 'G' test. Record: docs/VRF_ALTITUDE_FRAMES.md.
            bool liveMode = IsLiveLikeAltitudeMode();   // Live or TerrainProfile (identical creation)
            int domain = plan.Type.Domain;
            if (liveMode)
            {
                // Decide with NO terrain height yet, so plan.Pos already carries the fallback value
                // even if the query is never issued or never answered; FinalizePlacement re-decides
                // with the reply and overwrites it. The rule itself is PlacementPolicy.Decide (pure;
                // --placement-selftest) - this block only applies it. The deferred AGL set is
                // registered there too, so it can never be registered for a create that is still
                // waiting on the terrain reply.
                var d = PlacementPolicy.Decide(domain, unit.AltitudeAgl, unit.AltitudeMsl,
                                               _vrf.AirDefaultAltitudeAglMeters, null, _vrf.CreateClearanceMeters);
                plan = plan with { Pos = new Geodetic { LatDeg = plan.Pos.LatDeg, LonDeg = plan.Pos.LonDeg, AltMeters = d.CreateAltMeters } };
            }
            else if (plan.PostCreateAltitude is double alt)
            {
                // Fixed100: the golden-parity escape hatch - the oracle's behaviour byte-for-byte,
                // including its frame error (ElevationAgl+1 sent as AGL; C2SIMinterface.cpp:721-724).
                // No terrain query, no PLACEMENT line: this branch is unchanged by the 2026-09-05 work.
                _pendingAltitude[plan.Name] = alt;
            }

            // Retain the taskee lookup so OnOrder can resolve PerformingEntity -> VRF uuid,
            // and the inverse (name -> uuid) so the report callbacks can name their subject.
            _unitByC2SimUuid[unit.Uuid] = new CreatedUnit(plan.Name, unit.SymbolId, plan.IsAggregate,
                plan.Type.Domain, plan.IsAggregate ? AutoFormationFor(plan.Type) : null);
            _c2SimUuidByName[plan.Name] = unit.Uuid;
            // The AUTHORED position (before any de-stack) - the coordinate STP also writes as the
            // route's first vertex (the origin-vertex drop in ExecuteTaskOnTick, sec 3f).
            _authoredPosByName[plan.Name] = (plan.Pos.LatDeg, plan.Pos.LonDeg);
            // The template this unit landed - the route pre-flight resolves its vehicles' own
            // max-slope from it, and names it in the warning.
            if (!string.IsNullOrEmpty(plan.TemplateName)) _templateByName[plan.Name] = plan.TemplateName;

            toCreate.Add(plan);
            placements.Add(new PlacementInput(domain, unit.AltitudeAgl, unit.AltitudeMsl));
            hierarchy.Add((unit.Uuid, (unit.SuperiorUuid ?? "").Trim()));
            // SF-3: WHICH ROW, AND WHY. The echelon spacing table is the GROUND subset, so a unit
            // whose SIDC battle dimension is air/space/naval must NOT be handed a ground row - and
            // whichever way it goes, the reason is recorded here and printed once per init below,
            // instead of being an unwritten property of a pure function.
            echelons.Add(EchelonSpacing.KeyOf(unit.EchelonCode, unit.SymbolId, out string echProv));
            echelonProvenance.Add((plan.Name, echelons[^1], echProv));
            if (unit.DeclaredSubordinates is { Count: > 0 }) declaredByParent[unit.Uuid] = unit.DeclaredSubordinates;
            if (_vrf.MaterializeAtOrder)
            {
                // C13: keep the FULL plan for order time; the create below becomes a shell (see the
                // CreationPolicy block after composition).
                string supUuid = (unit.SuperiorUuid ?? "").Trim();
                _deferred[unit.Uuid] = new DeferredUnit(plan, placements[^1], supUuid,
                                                        unit.DeclaredSubordinates, unit.Name);
                if (supUuid.Length > 0)
                    _childUuidsBySuperior.GetOrAdd(supUuid, _ => new List<string>()).Add(unit.Uuid);
            }
            planned++;
        }

        // COMPOSE-FROM-CHILDREN (Vrf:ComposeHierarchy): classify parent/child/leaf from the declared
        // Superior chain, flip PARENT aggregate plans to createSubordinates=false (empty shell), and
        // register the compositions so OnVrfObjectCreated attaches each declared child once created
        // (vendor-sample recipe). Runs BEFORE de-stack/terrain/enqueue: it rewrites toCreate entries
        // in place and needs the full survivor set. Index-parallel with `hierarchy`.
        // C14 SCOPE (cold-start review SF2, 2026-09-20): the indices of plans that will be COMPOSED
        // INTO a parent aggregate, collected as the compositions are registered and handed to the
        // de-stack below. A composed child's place is its parent's formation's, never a ring slot.
        // Empty whenever Vrf:ComposeHierarchy is off, and then every plan is independent - the
        // behaviour before this change.
        var composedChildIndices = new HashSet<int>();
        var composedGroups = new List<(int ParentIndex, IReadOnlyList<int> ChildIndices)>();
        if (_vrf.ComposeHierarchy && toCreate.Count > 0)
            ApplyHierarchyComposition(toCreate, hierarchy, declaredByParent, composedChildIndices, composedGroups);
        if (_vrf.MaterializeAtOrder && toCreate.Count > 0)
        {
            // C13 (CreationPolicy=AtOrder): SHELLS ONLY at init. Every aggregate is created as an EMPTY
            // shell at its authored position - it displays, reflects its position and sits in the
            // organization tree (ApplyHierarchyComposition above attaches declared child shells to
            // parent shells). Members follow when an order references the unit (MaterializeUnit).
            // Expansion is NOT run here; it runs per referenced unit. Platforms are created as-is.
            int flipped = 0;
            for (int i = 0; i < toCreate.Count; i++)
                if (toCreate[i].IsAggregate && toCreate[i].CreateSubordinates)
                { toCreate[i] = toCreate[i] with { CreateSubordinates = false }; flipped++; }
            if (_vrf.ComposeHierarchy) GetResolver();   // load the catalog here (init thread); order thread only reads
            // N15 (D9, 2026-09-21). THIS LINE USED TO COUNT TWO DIFFERENT THINGS AND CALL ONE OF
            // THEM BY THE WRONG NAME. It printed `flipped` as the shell count and
            // `toCreate.Count - flipped` as "platform(s) created in full" - so a COMPOSED PARENT,
            // which ApplyHierarchyComposition had ALREADY set to CreateSubordinates=false and which
            // this loop therefore does not flip, was counted as a PLATFORM. On R9 lean that printed
            // "4 ... EMPTY shells ... 2 platform(s) created in full" for an init with five
            // aggregates and ONE platform, while the INIT CREATION BARRIER line below correctly
            // said 5 - and the D9 prereg registered the wrong 4 and scored a correct run a MISS.
            // Both lines now take their numbers from CreationCensus over this same final list.
            var census = CreationCensus.Of(toCreate);
            _log.LogInformation("CreationPolicy=AtOrder (C13): {Shells} unit(s) created as EMPTY shells at their " +
                                "authored positions ({Flipped} flipped to empty here, {Composed} already an empty " +
                                "COMPOSED PARENT shell); members are created when an order first references a " +
                                "unit. {Platforms} platform(s) created in full. The INIT CREATION BARRIER line " +
                                "counts the same shells over the same plan list (N15).",
                                census.EmptyShells, flipped, census.EmptyShells - flipped, census.Platforms);
        }
        // Coarse ORBAT leaves (a company/battalion the ORBAT did NOT decompose): expand into their
        // doctrinal sub-units and compose, instead of the broken template higher-unit (G-A).
        else if (_vrf.ComposeHierarchy && toCreate.Count > 0)
            ExpandCoarseLeaves(toCreate, placements, hierarchy, composedChildIndices, composedGroups);

        // R8 (opt-in, docs/UNIT_MOVEMENT_RESEARCH.md sec 4): spread units that share
        // identical init coordinates onto deterministic rings BEFORE creating them -
        // stacked spawns are the COA-STP1 pathology that blocks aggregate marching.
        //
        // SCOPE (C14 as ruled, narrowed 2026-09-20): INDEPENDENT objects only. See CompositionPlan.
        if (_vrf.DeStackCreates && toCreate.Count > 1)
        {
            // SF-4: the positions as the init authored them (after the cascade, before EITHER
            // pass). ApplyComposedSiblings needs them to tell "these children share their PARENT's
            // coordinate" (ring the parent wherever the independent pass left it) from "these
            // children share some OTHER point" (ring that point) - a distinction that cannot be
            // made after Apply has rewritten the list. Cheap: one struct per plan, once per init.
            var posBeforeIndependent = toCreate.Select(p => p.Pos).ToList();
            foreach (var g in DeStacker.Apply(toCreate, _vrf.DeStackSpacingMeters, _vrf.DeStackRotationDeg,
                                              composedChildIndices))
                _log.LogInformation("DeStack (R8/C14): {N} INDEPENDENT units at ({Lat},{Lon}) spread onto " +
                                    "{Spacing} m rings rotated {Rot} deg (first unit kept in place).",
                                    g.Count, g.LatDeg, g.LonDeg, _vrf.DeStackSpacingMeters, _vrf.DeStackRotationDeg);
            // COMPOSED SIBLINGS AT THEIR OWN ECHELON'S SCALE (user ruling 2026-09-21, option C).
            // RUNS AFTER the independent pass on purpose: if a parent was itself spread as an
            // independent unit, its children must ring the position it ENDED at, not the one the
            // init authored - and since SF-4 the code DOES that (it used to anchor on the
            // children's shared coordinate and never read the parent's plan). The parent is never
            // a candidate here - it keeps its coordinate, which is what makes every taskee's route
            // start where it started before; N4 makes the children's CENTROID keep it too, which
            // is what makes the route the same LENGTH as before.
            while (echelons.Count < toCreate.Count) echelons.Add("");   // synthesized children: no C2SIM echelon
            if (_vrf.DeStackComposedSiblings && composedGroups.Count > 0)
            {
                // SF-3: WHICH ECHELON ROW EVERY UNIT RESOLVED TO, AND WHY - the whole init on one
                // line, grouped, with the air/naval refusals named individually because THAT is the
                // case the silence used to hide. The table is the GROUND subset (EchelonSpacing).
                var refusedDomain = echelonProvenance.Where(e => e.Why.Contains("BATTLE DIMENSION")).ToList();
                _log.LogInformation("ECHELON ROWS (SF-3, the de-stack's GROUND spacing table): {Dist} over {N} " +
                                    "planned unit(s). {Refused}{Sample}",
                                    string.Join(", ", echelonProvenance
                                        .GroupBy(e => e.Key.Length == 0 ? "(no row - fallback)" : e.Key)
                                        .OrderByDescending(g => g.Count())
                                        .Select(g => $"{g.Key} x{g.Count()}")),
                                    echelonProvenance.Count,
                                    refusedDomain.Count == 0
                                        ? "No unit was refused a row for its battle dimension. "
                                        : refusedDomain.Count + " unit(s) were REFUSED a ground row because their "
                                          + "SIDC battle dimension is air/space/naval - they fall back to "
                                          + "Vrf:DeStackEchelonFallbackMeters (" + _vrf.DeStackEchelonFallbackMeters
                                          + " m; 0 = not spread): ["
                                          + string.Join("; ", refusedDomain.Take(10).Select(e => e.Name + ": " + e.Why))
                                          + (refusedDomain.Count > 10 ? "; ..." : "") + "]. ",
                                    "Provenance of the first few: ["
                                    + string.Join("; ", echelonProvenance.Take(4).Select(e => e.Name + ": " + e.Why))
                                    + (echelonProvenance.Count > 4 ? "; ..." : "") + "]");
                var table = EchelonSpacing.WithOverrides(_vrf.DeStackEchelonSpacingMeters, out string tableNote);
                double SpacingFor(string key) =>
                    !string.IsNullOrEmpty(key) && table.TryGetValue(key, out double m)
                        ? m : _vrf.DeStackEchelonFallbackMeters;
                var spreadGroups = DeStacker.ApplyComposedSiblings(
                    toCreate, composedGroups, echelons, SpacingFor, _vrf.DeStackRotationDeg,
                    out var skippedGroups, posBeforeIndependent);
                foreach (var g in spreadGroups)
                    _log.LogInformation("DeStack (C14 echelon scope, user rulings 2026-09-21 + N4): {N} COMPOSED " +
                                        "sibling(s) of {Parent} shared ({Lat},{Lon}) and were spread onto ONE ring " +
                                        "of radius {Radius:F1} m about {Anchor} at EQUAL bearings {Bearing:F1} deg " +
                                        "apart (start {Rot} deg), which holds their CENTROID - the position " +
                                        "VR-Forces publishes for the composed aggregate - on that same coordinate " +
                                        "(D7/N4: hex slots moved it 262 m and shortened the taskee's route by " +
                                        "74 m). The radius is derived from the {Echelon} echelon's {Spacing} m " +
                                        "spacing (longest shipped formation for that echelon {Span:F1} m) so the " +
                                        "MINIMUM sibling separation is exactly that spacing. The parent keeps its " +
                                        "position and the centre is left empty. Moved: [{Moved}].{Note}",
                                        g.Count, g.ParentName, g.LatDeg, g.LonDeg, g.RadiusMeters,
                                        g.AnchoredOnParent
                                            ? "THE PARENT'S OWN current position"
                                            : "the children's shared coordinate (the parent is elsewhere)",
                                        g.Count > 0 ? 360.0 / g.Count : 0.0, _vrf.DeStackRotationDeg,
                                        g.EchelonKey, g.SpacingMeters,
                                        EchelonSpacing.SpanMeters.TryGetValue(g.EchelonKey, out double sp) ? sp : 0.0,
                                        string.Join(", ", g.Moved.Take(20).Select(m => $"{m.Name} {m.Meters:F0} m"))
                                        + (g.Moved.Count > 20 ? ", ..." : ""),
                                        tableNote.Length > 0 ? " " + tableNote : "");
                foreach (var s in skippedGroups)
                    _log.LogInformation("DeStack (C14 echelon scope): {N} composed sibling(s) of {Parent} were " +
                                        "NOT spread - {Reason}.", s.Count, s.ParentName, s.Reason);
                // SF-B (cold-start review of 1d0fb69): the pass sizes each ring WITHIN its group
                // and never looks ACROSS groups. Two parents 700 m apart with 3-child platoon
                // rings leave 295.8 m between the rings, under the ruled 350 m; at N >= 6 they
                // interpenetrate. DETECTION ONLY - nothing is moved, because a cross-group solve
                // is a ruling and would change every shipped fixture. WARN, because the whole
                // value of this line is that somebody sees it: Information is where the last
                // silent geometry defect lived for a week.
                foreach (var p in DeStacker.FindRingOverlaps(spreadGroups))
                    _log.LogWarning("DeStack: {Detail}", DeStacker.DescribeRingProximity(p));
            }
            else if (composedChildIndices.Count > 0)
                _log.LogInformation("DeStack (C14 scope): {N} unit(s) were NOT considered because " +
                                    "Vrf:ComposeHierarchy composes them INTO a parent aggregate and " +
                                    "Vrf:DeStackComposedSiblings is OFF - their place comes from the parent's " +
                                    "formation (UG52 25.2.1), not from the init coordinate. This is the " +
                                    "pre-2026-09-21 behaviour, kept for comparability with runs D1-D6. Held " +
                                    "with their parent: [{Names}].",
                                    composedChildIndices.Count,
                                    string.Join(", ", composedChildIndices.OrderBy(i => i)
                                                                         .Take(20).Select(i => toCreate[i].Name))
                                    + (composedChildIndices.Count > 20 ? ", ..." : ""));
            // Review fix: the stored order-time plans must carry the DE-STACKED position, else the
            // members would be born at the authored point, away from their shell. `hierarchy` is
            // index-parallel to toCreate here (no expansion has run in AtOrder mode).
            if (_vrf.MaterializeAtOrder)
                for (int i = 0; i < toCreate.Count && i < hierarchy.Count; i++)
                    if (_deferred.TryGetValue(hierarchy[i].Uuid, out var dd))
                        _deferred[hierarchy[i].Uuid] = dd with { Plan = dd.Plan with { Pos = toCreate[i].Pos } };
        }

        // PLACEMENT (Live / TerrainProfile): ask the back end for the terrain height under every
        // create position - ONE request for the whole init - and create each object AT the surface.
        // MUST run AFTER the de-stack: de-stacking moves units off their authored lat/lon, and a
        // terrain height queried at the old point would be the wrong point's answer.
        // Fixed100 never queries; its creates go out immediately, byte-for-byte as before.
        // ORDERING: in the querying modes the unit creates are now enqueued AFTER the control-area
        // creates below (they wait for the reply), where they used to precede them. Nothing couples
        // the two - a DtIfCreateVrfObject for a TacticalArea neither reads nor is read by a unit
        // create - but it IS a departure from the golden command order, so a trace comparison must
        // expect areas first. Fixed100, the golden-parity mode, keeps the original order.
        // DEFER, DO NOT ABORT (D5b): record the init's OWN creation set here - the plan is final and
        // nothing has been issued yet, so a unit whose create is about to be deferred to a terrain
        // reply is counted as outstanding. That is precisely the state that dropped T_R5_TK1.
        RecordInitCreationBarrier(toCreate);
        if (toCreate.Count > 0 && IsLiveLikeAltitudeMode())
            StartPlacementTerrainQuery(toCreate, placements, source);
        else
            EnqueueCreates(toCreate);

        // R-SURFACE-PROXY: one ObservationReport/NameObservation per substituted unit, so a
        // downstream C2SIM consumer sees WHICH template stands in and why (ReportBuilder
        // .BuildTypeSubstitutionReport). Fire-and-forget, exactly like the position reports.
        // B8: the representation is recorded as it is announced, so an order-time re-creation as
        // something ELSE announces again (MaterializeUnit) - and the representation is read from the
        // FINAL plan, after the AtOrder shell flip and the coarse-leaf expansion have rewritten it.
        var finalPlanByName = new Dictionary<string, CreationPlan>(StringComparer.Ordinal);
        foreach (var fp in toCreate) finalPlanByName[fp.Name] = fp;
        foreach (var (uuid, name, marking, substitution) in proxiesToReport)
        {
            // Review finding 8: when the final plan is not in this batch, record a SENTINEL - not
            // the unit's own name, which is a false representation and would SUPPRESS the
            // re-announcement outright if it ever equalled a template name.
            string rep = finalPlanByName.TryGetValue(marking, out var fplan)
                ? SubstitutionAnnouncer.Representation(fplan.TemplateName, fplan.CreateSubordinates, 0)
                : SubstitutionAnnouncer.UnknownRepresentation;
            if (!_substitutions.ShouldAnnounce(marking, rep)) continue;
            _ = PushReportAsync(ReportBuilder.BuildTypeSubstitutionReport(
                    uuid, name, marking, substitution, IsoNow(), NewReportId()), ReportKind.Observation);
        }
        // Units whose type mapped EXACTLY are not announced - there is no substitution to report -
        // but their representation is recorded, so that if an order later re-creates one as
        // something else, that CHANGE is what the announcement is measured against.
        foreach (var fp in toCreate)
            if (_substitutions.Current(fp.Name).Length == 0)
                _substitutions.Record(fp.Name, SubstitutionAnnouncer.Representation(
                    fp.TemplateName, fp.CreateSubordinates, 0));

        int areasQueued = 0, areasRegistered = 0;
        foreach (var a in init.Areas)
        {
            // Same duplicate-delivery guard for areas (keyed by uuid, falling back to name).
            string areaKey = "area:" + (string.IsNullOrEmpty(a.Uuid) ? a.Name : a.Uuid);
            if (!_createdAreaKeys.TryAdd(areaKey, 0)) { duplicates++; continue; }
            var area = a;
            // R1: register it under its C2SIM uuid BEFORE the create is queued - an order can
            // arrive while the creates are still draining, and the geometry a MapGraphicID names
            // is the AUTHORED geometry either way (the create carries these very points).
            if (!string.IsNullOrEmpty(area.Uuid))
            {
                RegisterInitGraphic(new TaskGraphic(
                    area.Uuid, area.Name, TaskGraphic.KindArea,
                    area.Points.Select(pt => (pt.Lat, pt.Lon, (double?)pt.Elev)).ToList()), source);
                areasRegistered++;
            }
            // V4b: ONE control-area factory, shared with the task path (EnqueueControlAreaCreate) -
            // same name registration, same tick marshalling, same uuid policy. The dedupe stays HERE
            // because the two callers key it differently (an init graphic by its own uuid, a task by
            // the task's).
            EnqueueControlAreaCreate(area.Uuid, area.Name,
                area.Points.Select(pt => (pt.Lat, pt.Lon, (double?)pt.Elev)).ToList());
            areasQueued++;
        }

        // ---- V3: the init's LINE and POINT graphics --------------------------------------------
        // Same code path shape as the areas above, deliberately: same duplicate-delivery guard
        // (_createdAreaKeys, keyed by uuid with a name fallback), same _tickActions.Enqueue, same
        // uuid policy (the C2SIM uuid IS the VRF uuid - vrfRemoteController.h:991-1011 / :1023-1039
        // take the same optional startingUUID createControlArea does), and the same console
        // handling, which needs no code here: OnVrfObjectCreated raises every created object's
        // console to Vrf:ObjectConsoleNotifyLevel by uuid, graphics included.
        //
        // WHY A LINE BECOMES A ROUTE AND NOT A PHASE LINE - measured, not chosen by taste.
        // The vendor has both: createPhaseLine takes EXACTLY TWO points (vrfRemoteController.h
        // :1054-1072, "const DtVector& dbPoint1, const DtVector& dbPoint2") and createRoute takes a
        // DtList of vertices (:1023-1039). COA-STP1's 41 lines have this vertex histogram
        // (counted from data\COA-STP1_Initialization.xml, 2026-09-14):
        //     1 pt x10, 2 x1, 3 x2, 4 x3, 5 x10, 7 x1, 8 x3, 11 x3, 13 x6, 14 x1, 15 x1.
        // EXACTLY ONE of the 41 would fit createPhaseLine. So a route is the only vendor object
        // that preserves the authored geometry, and forking on vertex count would produce two
        // different object classes for one C2SIM element type. CONSEQUENCE, recorded because it is
        // a live question for build items V5/V6: the vendor's line-taking scripted tasks
        // (company_seize departLine, co_clear Limit of Advance, the breach lane) are authored
        // against the GUI's line graphic, and whether they accept a route object is a run-only
        // question - the help describes the parameter, not the accepted class.
        // A line with fewer than 2 vertices is NOT created: a one-vertex route is not a line in any
        // frame, and inventing a second vertex would be manufacturing geometry. It is counted and
        // reported instead.
        //
        // ALTITUDE. Every vertex goes out at its AUTHORED elevation, which is 0 for all of
        // COA-STP1 because C2SIM ships neither AltitudeAGL nor AltitudeMSL on these graphics
        // (xsd :2716-2717, both optional; measured absent in every init in data/). That is exactly
        // what the 35 areas already do. SEPARATE SENTENCE, because the two must not be fused: the
        // terrain-anchoring machinery (StartPlacementTerrainQuery / TerrainVertexAuthoring,
        // Vrf:AltitudeMode) exists for the routes a UNIT DRIVES, where a buried vertex is a
        // movement problem; these are CONTROL graphics and nothing drives them. IF a build item
        // later hands one of these routes to a move task, its vertices need the terrain query
        // first - that is an open item, not something decided here.
        // M5 (cold-start review of 5c67d41): REGISTER every line and point for R1 resolution FIRST,
        // and unconditionally. This is the authored geometry a MapGraphicID names; whether the
        // graphic is also CREATED in VR-Forces is a separate question answered by the two flags
        // below, and a task must not lose its own coordinates to a display setting. Done before any
        // create is enqueued for the same reason the area registration is (an order can arrive
        // while the creates are still draining).
        int linesRegistered = 0, pointsRegistered = 0;
        foreach (var l in init.Lines)
        {
            if (string.IsNullOrEmpty(l.Uuid) || l.Points.Count == 0) continue;
            RegisterInitGraphic(new TaskGraphic(
                l.Uuid, l.Name, TaskGraphic.KindLine,
                l.Points.Select(pt => (pt.Lat, pt.Lon, (double?)pt.Elev)).ToList()), source);
            linesRegistered++;
        }
        foreach (var p in init.Points)
        {
            // A C2SIM Point's FIRST location is its position (InitPoint.Position); any others are
            // kept by the parser rather than silently dropped, but a point graphic is ONE place.
            if (string.IsNullOrEmpty(p.Uuid) || !p.HasPosition) continue;
            var pos = p.Position;
            RegisterInitGraphic(new TaskGraphic(
                p.Uuid, p.Name, TaskGraphic.KindPoint,
                new List<(double, double, double?)> { (pos.Lat, pos.Lon, (double?)pos.Elev) }), source);
            pointsRegistered++;
        }

        // TASK SYMBOLS (2026-09-20). Registered for R1 resolution and NEVER queued for creation:
        // a TaskGraphic is a mission symbol, not a control measure, so there is no VR-Forces object
        // class it belongs in - but an order that names one by MapGraphicID is naming real authored
        // coordinates, and until now it got a warning and a silent fallback instead of them.
        // KIND: >=2 anchors are contributed in order (the reading V4b already applies to the same
        // points when STP linearises them into embedded Locations); exactly 1 is a point.
        int taskGraphicsRegistered = 0;
        foreach (var tg in init.TaskGraphics)
        {
            if (string.IsNullOrEmpty(tg.Uuid) || tg.Points.Count == 0) continue;
            RegisterInitGraphic(new TaskGraphic(
                tg.Uuid, tg.Name,
                tg.Points.Count >= 2 ? TaskGraphic.KindLine : TaskGraphic.KindPoint,
                tg.Points.Select(pt => (pt.Lat, pt.Lon, (double?)pt.Elev)).ToList()), source);
            taskGraphicsRegistered++;
        }

        int linesQueued = 0, linesDegenerate = 0, pointsQueued = 0, pointsEmpty = 0;
        // MERGE NOTE (feat/tasking-foundation -> feat/integration, 2026-09-14). V3 was written
        // against a tree in which the name -> VRF-uuid map was a bare dictionary. It is now a
        // NameRegistry (B3), and OnVrfObjectCreated binds EVERY ObjectCreated through it - graphics
        // included. Two consequences make the selection/registration pass below mandatory rather
        // than tidy:
        //   1. A returned name the registry never SAW REQUESTED falls through to the prefix scan,
        //      whose whole job is to attach a truncated marking to the unit it came from. A route
        //      or waypoint name that happens to be a strict prefix of exactly one still-unbound
        //      UNIT name would therefore bind the GRAPHIC's uuid under the UNIT's name, and R1
        //      would then report a control point's position as that unit's and ExecuteTaskOnTick
        //      would task it. Registering the name makes the callback an EXACT match, which
        //      short-circuits the scan before it starts - exactly what the area loop above does
        //      with _names.Requested(area.Name).
        //   2. Registration happens for the WHOLE batch BEFORE the first create is enqueued, which
        //      is the rule EnqueueCreates follows for units (f0d1c68) and for the same reason: the
        //      tick thread drains _tickActions concurrently with this method, so a create issued in
        //      an earlier iteration can produce its ObjectCreated - and a CACHED name resolution -
        //      while later names are still unregistered. Two loops cost nothing.
        // Only names we actually ASK VR-Forces to create are registered: with both flags off (the
        // default) nothing below runs and the registry is byte-for-byte what it was before V3.
        var linesToCreate = new List<InitLine>();
        var pointsToCreate = new List<InitPoint>();
        if (_vrf.CreateInitLines)
        {
            foreach (var l in init.Lines)
            {
                if (l.Points.Count < 2) { linesDegenerate++; continue; }
                string key = "line:" + (string.IsNullOrEmpty(l.Uuid) ? l.Name : l.Uuid);
                if (!_createdAreaKeys.TryAdd(key, 0)) { duplicates++; continue; }
                linesToCreate.Add(l);
            }
        }
        if (_vrf.CreateInitPoints)
        {
            foreach (var p in init.Points)
            {
                if (!p.HasPosition) { pointsEmpty++; continue; }
                string key = "point:" + (string.IsNullOrEmpty(p.Uuid) ? p.Name : p.Uuid);
                if (!_createdAreaKeys.TryAdd(key, 0)) { duplicates++; continue; }
                pointsToCreate.Add(p);
            }
        }
        if (linesToCreate.Count > 0 || pointsToCreate.Count > 0)
        {
            foreach (var l in linesToCreate) _names.Requested(l.Name);
            foreach (var p in pointsToCreate) _names.Requested(p.Name);
            // Say ONCE which of the now-larger requested set are strict prefixes of others at the
            // DIS marking width. A graphic is not a DIS entity and its name is not truncated, so a
            // graphic/unit prefix pair cannot mis-bind the GRAPHIC - but it CAN make a genuinely
            // truncated UNIT callback ambiguous, and an ambiguous unit is an unbound unit. That is
            // a property of the fixture's naming, so it must be visible at create time rather than
            // as a silent non-binding nine hours later. Advisory only; nothing is refused.
            WarnOnPrefixedNames();
        }
        foreach (var l in linesToCreate)
        {
            var line = l;
            _tickActions.Enqueue(() =>
            {
                var pts = line.Points
                    .Select(pt => new Geodetic { LatDeg = pt.Lat, LonDeg = pt.Lon, AltMeters = pt.Elev })
                    .ToList();
                _bridge.CreateRoute(pts, line.Name, line.Uuid);
            });
            linesQueued++;
        }
        foreach (var p in pointsToCreate)
        {
            var point = p;
            _tickActions.Enqueue(() =>
            {
                var pos = point.Position;
                _bridge.CreateWaypoint(
                    new Geodetic { LatDeg = pos.Lat, LonDeg = pos.Lon, AltMeters = pos.Elev },
                    point.Name, point.Uuid);
            });
            pointsQueued++;
        }
        // Always say what was PARSED, even when creation is off, so a run log shows the geometry
        // the order could have been given and nobody re-discovers it from the XML.
        _log.LogInformation("Init ({Source}) graphics: {Areas} area(s) queued; " +
                            "{ParsedLines} line(s) parsed -> {Lines} queued ({DegenerateLines} with <2 vertices " +
                            "skipped), {ParsedPoints} point(s) parsed -> {Points} queued ({EmptyPoints} with no " +
                            "position skipped). Vrf:CreateInitLines={LinesOn} Vrf:CreateInitPoints={PointsOn}. " +
                            "R1 RESOLUTION (M5) is independent of those flags: {Registered} graphic(s) are now " +
                            "addressable by MapGraphicID ({RegAreas} area(s), {RegLines} line(s), {RegPoints} " +
                            "point(s), {RegTask} task symbol(s) from this delivery; a task symbol is registered " +
                            "for resolution and never created as an object).",
                            source, areasQueued, init.Lines.Count, linesQueued, linesDegenerate,
                            init.Points.Count, pointsQueued, pointsEmpty,
                            _vrf.CreateInitLines, _vrf.CreateInitPoints,
                            _graphicsByC2SimUuid.Count, areasRegistered, linesRegistered, pointsRegistered,
                            taskGraphicsRegistered);

        if (duplicates > 0)
            _log.LogWarning("Init ({Source}): skipped {N} units/graphics ALREADY created " +
                            "(duplicate init delivery - late-join + broadcast?).", source, duplicates);

        // Fail LOUDLY when nothing matched the clientId (a silent 0 here cost live-run time:
        // appsettings ships ClientId=STP, but e.g. the COA-STP1 init needs C2SIM). `matched`
        // not `planned` - units that matched but were skipped for missing fields already
        // warned individually and must not masquerade as a ClientId mismatch.
        // MADE ACTIONABLE 2026-09-20 (ClientIdPolicy). The old line named the ClientId and a
        // bare comma-joined list of SystemNames - no counts, no override to type, and nothing
        // on the C2SIM bus, so the producer that pushed the init saw a healthy interface doing
        // nothing. The filter itself is UNCHANGED and deliberately so; what changed is that the
        // failure now says what to type and reaches the C2 side.
        //
        // SF6 (cold-start review of 9d67f97): AND IT IS NOW GATED, so it does not cry wolf. The
        // trigger used to be "no unit matched", which is true of every foreign init on a shared
        // server - each producing an ERROR and a bus report for a message that was never ours.
        // ClientIdPolicy.Severity decides: LOUD only while this app holds NO units of its own
        // (nothing created, nothing taskable, the run is dead), otherwise one INFO line and
        // nothing on the bus. `_unitByC2SimUuid` spans every initialization of the run, so a
        // first init that worked protects the later foreign ones from being shouted about.
        var severity = ClientIdPolicy.Severity(matched, init.Units.Count, _unitByC2SimUuid.Count);
        if (severity != ClientIdPolicy.MismatchSeverity.None)
        {
            var counts = ClientIdPolicy.SystemNameCounts(init.Units);
            if (severity == ClientIdPolicy.MismatchSeverity.Foreign)
            {
                _log.LogInformation("{Diagnostic}", ClientIdPolicy.ForeignInitMessage(
                    source, _vrf.ClientId, init.Units.Count, _unitByC2SimUuid.Count, counts));
            }
            else
            {
                string diagnostic = ClientIdPolicy.MismatchMessage(source, _vrf.ClientId, init.Units.Count, counts);
                _log.LogError("{Diagnostic}", diagnostic);
                // The SAME sentence out on the observation channel R-SURFACE-PROXY and R2 already use.
                // STP discards ObservationReports today (Q3/STP-800, recorded not worked around), but
                // an init-time finding that reaches the bus at all is the difference between a producer
                // that can see the problem and one that cannot - and every other producer gets it now.
                _ = PushReportAsync(ReportBuilder.BuildTypeSubstitutionReport(
                        ReportBuilder.ZeroUuid, ClientIdPolicy.Marker, ClientIdPolicy.Marker, diagnostic,
                        IsoNow(), NewReportId()), ReportKind.Observation);
            }
        }

        if (unmapped > 0)
            _log.LogError("Init ({Source}): {N} unit(s) had NO usable VR-Forces template and were NOT " +
                          "created (see the TYPE MAP errors above). Fix data/unit-type-map.json or " +
                          "author the missing templates - do NOT let them fall through to a generic.",
                          source, unmapped);
        if (proxiesToReport.Count > 0)
            _log.LogInformation("Init ({Source}): {N} PROXY substitution(s) surfaced to C2SIM " +
                                "(R-SURFACE-PROXY).", source, proxiesToReport.Count);

        _log.LogInformation("Init dispatched: {Units} units + {Areas} areas + {Lines} lines + " +
                            "{Points} points queued for creation.",
                            planned, areasQueued, linesQueued, pointsQueued);
    }

    /// <summary>
    /// SF5 (cold-start review of 9d67f97): the INIT is the one writer allowed to OVERWRITE a
    /// published graphic, and it now SAYS SO when it does.
    ///
    /// The four init registration sites (areas, lines, points, task symbols) assign
    /// unconditionally, which is the intended rule - the initialization is the shared world every
    /// order is written against, so it wins over an order graphic that got in first, and over an
    /// earlier init's graphic under the same uuid. What was wrong is that it happened in SILENCE:
    /// a task in flight could be driving geometry that had just been replaced under it with no line
    /// anywhere. An IDENTICAL re-registration (the duplicate init delivery a late-join QUERYINIT
    /// plus a broadcast produces) stays silent, exactly as the order path does.
    /// </summary>
    private void RegisterInitGraphic(TaskGraphic g, string source)
    {
        _graphicsByC2SimUuid.TryGetValue(g.Uuid, out var existing);
        if (TaskGraphic.Classify(existing, g) == TaskGraphic.Registration.Conflicting)
            _log.LogWarning("Init ({Source}) graphic '{Name}' ({Kind}) REPLACES a graphic already published " +
                            "under uuid {Uuid} - '{Other}' ({OtherKind}) with different content, from an order " +
                            "or from an earlier initialization. The INITIALIZATION WINS (it is the shared world " +
                            "every order is written against), so any task that resolves this id from now on " +
                            "gets the NEW geometry; a task already dispatched keeps the route it was given. " +
                            "One uuid naming two different graphics is a data defect in the export (SF5).",
                            source, g.Name, g.Kind, g.Uuid, existing.Name, existing.Kind);
        _graphicsByC2SimUuid[g.Uuid] = g;
    }

    /// <summary>
    /// Queue the creates on the tick thread. Extracted 2026-09-05 because there are now two
    /// callers: the immediate path (Fixed100, and any init with nothing to place) and the
    /// terrain-reply path. Enqueuing is kept even when the caller is ALREADY on the tick thread,
    /// so the command order out of an init is the same in both paths.
    /// </summary>
    private void EnqueueCreates(List<CreationPlan> plans)
    {
        _createsIssuedUtc = DateTime.UtcNow;   // composition clocks start here, not at planning
        // B3 (review fix): register EVERY name before ANY create is issued. The tick thread runs
        // concurrently with this method, so a create enqueued in an earlier iteration can produce
        // its ObjectCreated - and its truncated-name resolution - while later names are still
        // unregistered; the resolution is CACHED, so a name that would have been ambiguous once the
        // whole batch was known could otherwise be attributed to the only candidate visible at that
        // instant. Two loops cost nothing and make the registry complete before the first create.
        foreach (var p in plans) _names.Requested(p.Name);
        WarnOnPrefixedNames();
        foreach (var p in plans)
        {
            _tickActions.Enqueue(() =>
            {
                if (p.IsAggregate)
                    _bridge.CreateAggregate(p.Type, p.Pos, p.Force, p.HeadingDeg, p.Name,
                                            AggregateState.Disaggregated, p.CreateSubordinates);
                else
                    _bridge.CreateEntity(p.Type, p.Pos, p.Force, p.HeadingDeg, p.Name);
            });
        }
    }

    // Pairs already reported by WarnOnPrefixedNames, so each is said ONCE however many batches run.
    private readonly ConcurrentDictionary<string, byte> _reportedPrefixPairs = new(StringComparer.Ordinal);

    /// <summary>
    /// NAME PRE-FLIGHT (B3 review finding 1), in the style of the 0c watchdog pre-flight: the moment
    /// a batch of names is registered - at init AND at every order-time materialization, because the
    /// interface's own derived names (EXPAND children, proxy tags) do not exist until then - say
    /// ONCE which of them are strict prefixes of others at the DIS marking width. Those are the
    /// pairs in which a truncated callback for the longer unit is indistinguishable from an exact
    /// callback for the shorter one; NameRegistry keeps the exact binding and warns per object, but
    /// the operator should know the hazard is in the fixture BEFORE any object comes back. Advisory
    /// only - nothing is refused and nothing changes. COA-STP1's real instance is "510/40~PXY"
    /// (exactly 10 chars) with its four EXPAND children.
    /// </summary>
    private void WarnOnPrefixedNames()
    {
        var fresh = _names.PrefixPairs(NameRegistry.MarkingTruncationWidth)
                          .Where(p => _reportedPrefixPairs.TryAdd(p.Shorter.Length + ":" + p.Longer, 0))
                          .ToList();
        if (fresh.Count == 0) return;
        _log.LogWarning("NAME PRE-FLIGHT: {N} requested name pair(s) where the shorter is a strict PREFIX of the " +
                        "longer and is short enough to survive truncation at the {Width}-character DIS marking " +
                        "width - if the longer unit is created as a PLATFORM its marking comes back as the " +
                        "shorter name and the two cannot be told apart: [{Pairs}]. The exact name wins and every " +
                        "such callback is logged; the fix is to keep every unit name inside that width " +
                        "(docs/PORT.md sec 6).",
                        fresh.Count, NameRegistry.MarkingTruncationWidth,
                        string.Join("; ", fresh.Select(p => $"'{p.Shorter}' < '{p.Longer}'")));
    }

    // ============ COMPOSE-FROM-CHILDREN (Vrf:ComposeHierarchy) ============
    // See PendingComposition (fields) and docs/experiments/PREREG_COMPOSE_A_2026-09-05.md. The
    // vendor-sample recipe (commandLineRemoteController.cxx:717-775 build, :1520-1554 attach):
    // create the PARENT as an empty shell, create the members, then AddToOrganization in the
    // object-created callback once both exist; then task the parent (VR-Forces recurses).

    /// <summary>
    /// Classify each planned unit as PARENT / CHILD / LEAF from the declared C2SIM Superior chain,
    /// flip PARENT aggregates to an EMPTY shell (CreateSubordinates=false), and register a
    /// PendingComposition per parent. `hierarchy` is index-parallel to `plans` ((uuid, superiorUuid)).
    /// Mutates `plans` in place. Runs at init BEFORE any create is enqueued (happens-before arrivals).
    /// </summary>
    private void ApplyHierarchyComposition(List<CreationPlan> plans, List<(string Uuid, string SuperiorUuid)> hierarchy,
                                           IReadOnlyDictionary<string, IReadOnlyList<string>> declaredByParent = null,
                                           HashSet<int> composedChildIndices = null,
                                           List<(int ParentIndex, IReadOnlyList<int> ChildIndices)> composedGroups = null)
    {
        if (plans.Count != hierarchy.Count)
        {
            _log.LogError("ComposeHierarchy: plans/hierarchy length mismatch ({P} vs {H}) - skipping.",
                          plans.Count, hierarchy.Count);
            return;
        }
        // ONE definition of parent/child, shared with the create-time de-stack (CompositionPlan).
        // Before 2026-09-20 this classification lived only here, and the de-stack - which must not
        // displace a unit whose place comes from its parent's formation - had no way to ask it.
        var comp = CompositionPlan.Classify(plans, hierarchy);
        foreach (int i in comp.NonAggregateParentIndices)
            _log.LogWarning("ComposeHierarchy: {Name} has declared children but is NOT an aggregate - " +
                            "cannot compose; created as-is, its children become standalone.", plans[i].Name);
        if (composedChildIndices != null)
            foreach (int i in comp.ComposedChildIndices) composedChildIndices.Add(i);
        // The same children GROUPED BY PARENT, for the composed-sibling de-stack (user ruling
        // 2026-09-21). Taken from the ONE classifier, never re-derived at the call site.
        if (composedGroups != null)
            foreach (var g in comp.ComposedGroups) composedGroups.Add(g);
        var parentUuids = comp.ParentUuids;
        if (parentUuids.Count == 0) return;   // flat init - nothing to compose

        var parentName = new Dictionary<string, string>();   // parentUuid -> parent plan name (survivor aggregates only)
        for (int i = 0; i < plans.Count; i++)
        {
            string uuid = hierarchy[i].Uuid;
            if (string.IsNullOrEmpty(uuid) || !parentUuids.Contains(uuid)) continue;
            parentName[uuid] = plans[i].Name;
            plans[i] = plans[i] with { CreateSubordinates = false };   // EMPTY shell - no template phantom
        }

        // parentUuid -> ordered child names (declared/init order fixes the leader/echelon, UG52 18.1.1)
        var childrenByParent = new Dictionary<string, List<string>>();
        var childUuidByName = new Dictionary<string, string>();
        for (int i = 0; i < plans.Count; i++)
        {
            string sup = hierarchy[i].SuperiorUuid;
            if (string.IsNullOrEmpty(sup) || !parentUuids.Contains(sup)) continue;
            if (!childrenByParent.TryGetValue(sup, out var list)) childrenByParent[sup] = list = new List<string>();
            list.Add(plans[i].Name);
            childUuidByName[plans[i].Name] = hierarchy[i].Uuid;
        }
        // N2: the creation list is UUID-sorted (InitParser:118, oracle parity), which is NOT the authored
        // order. Attach in the parent's DECLARED <Subordinate> order so the declared first child is the
        // leader (designator 1) - UG52 18.1.1 / 13.3.1. Children the parent did not declare keep their
        // creation order after the declared ones.
        if (declaredByParent != null)
        {
            foreach (var key in childrenByParent.Keys.ToList())
            {
                if (!declaredByParent.TryGetValue(key, out var declared) || declared.Count == 0) continue;
                var before = childrenByParent[key];
                var after = ComposeOrder.ByDeclared(declared, before, n => childUuidByName.TryGetValue(n, out var cu) ? cu : "");
                if (!after.SequenceEqual(before))
                    _log.LogInformation("ComposeHierarchy: {Parent} children attach in the DECLARED <Subordinate> order " +
                                        "[{After}] (creation order was [{Before}]); first = leader (UG52 18.1.1).",
                                        parentName.TryGetValue(key, out var pn) ? pn : key,
                                        string.Join(", ", after), string.Join(", ", before));
                childrenByParent[key] = after;
            }
        }

        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.CompositionTimeoutSeconds));
        foreach (var kv in childrenByParent)
        {
            if (!parentName.TryGetValue(kv.Key, out var pName)) continue;  // parent not a survivor aggregate
            var readyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _compositions[pName] = new PendingComposition
            {
                ParentName = pName, ExpectedChildNames = kv.Value, Deadline = deadline, Ready = readyTcs
            };
            _compositionReady[pName] = readyTcs;
            _declaredChildNamesByParent[pName] = kv.Value;
            foreach (var c in kv.Value) _childToParent[c] = pName;
            _log.LogInformation("ComposeHierarchy: {Parent} -> EMPTY shell; will attach {N} declared child unit(s) " +
                                "[{Children}] via AddToOrganization once created.",
                                pName, kv.Value.Count, string.Join(", ", kv.Value));
        }
    }

    /// <summary>Tick-thread: a VR-Forces object was just created; advance any composition it belongs
    /// to (as the parent shell and/or as a declared child).</summary>
    private void TryAdvanceComposition(string name, string vrfUuid)
    {
        if (_compositions.TryGetValue(name, out var asParent))   // `name` is a parent shell
        {
            asParent.ParentVrfUuid = vrfUuid;
            AttachIfComplete(asParent);
        }
        if (_childToParent.TryGetValue(name, out var parentOfChild)
            && _compositions.TryGetValue(parentOfChild, out var comp))   // `name` is a declared child
        {
            comp.ArrivedChildVrfUuid[name] = vrfUuid;
            AttachIfComplete(comp);
        }
    }

    private void AttachIfComplete(PendingComposition comp)
    {
        if (comp.Done || string.IsNullOrEmpty(comp.ParentVrfUuid)) return;         // parent shell not created yet
        if (comp.ArrivedChildVrfUuid.Count < comp.ExpectedChildNames.Count) return; // wait for all children
        FinishComposition(comp, timedOut: false);
    }

    /// <summary>Attach the arrived children (declared order) under the parent shell and signal ready.
    /// Runs on the tick thread (AddToOrganization is a bridge call).</summary>
    private void FinishComposition(PendingComposition comp, bool timedOut)
    {
        if (comp.Done) return;
        comp.Done = true;
        if (string.IsNullOrEmpty(comp.ParentVrfUuid))
        {
            _log.LogError("ComposeHierarchy: parent shell {Parent} was never created within {T}s - children " +
                          "cannot be attached; its task will drop.", comp.ParentName, _vrf.CompositionTimeoutSeconds);
        }
        else
        {
            int attached = 0;
            foreach (var childName in comp.ExpectedChildNames)   // DECLARED order: first = leader (UG52 18.1.1)
            {
                if (comp.ArrivedChildVrfUuid.TryGetValue(childName, out var childUuid))
                {
                    _bridge.AddToOrganization(childUuid, comp.ParentVrfUuid);
                    attached++;
                }
                else
                    _log.LogWarning("ComposeHierarchy: child {Child} of {Parent} never created within {T}s - " +
                                    "attaching without it.", childName, comp.ParentName, _vrf.CompositionTimeoutSeconds);
            }
            _log.LogInformation("ComposeHierarchy: {Parent} composed - {N}/{M} declared children attached{TO}.",
                                comp.ParentName, attached, comp.ExpectedChildNames.Count, timedOut ? " (TIMED OUT)" : "");
        }
        _compositions.TryRemove(comp.ParentName, out _);
        // Complete THIS composition's own gate (review fix): a by-name lookup here would signal a
        // gate registered later under the same name (case-1 order-time materialization) prematurely.
        if (comp.Ready != null) comp.Ready.TrySetResult();
        else if (_compositionReady.TryGetValue(comp.ParentName, out var tcs)) tcs.TrySetResult();
    }

    /// <summary>Tick-thread sweep (mirrors ExpireTerrainRequests): a composition past its deadline is
    /// finished from whatever children arrived, so a never-created child cannot hang the parent's tasks.</summary>
    private void ExpireCompositions()
    {
        // ACTIVITY-BASED (COA-STP1 run 1, 174427Z): a composition's deadline is registered at PLANNING,
        // but at scale the creates leave only after the terrain-profile wait (10 s) and 369 of them
        // are issued tick by tick, while the sim answers the whole batch inside ~10 s once they are
        // out. With a fixed 15 s from registration every one of 64 parents "expired" seconds before
        // its own ObjectCreated returned. So a composition may expire only when CompositionTimeout-
        // Seconds have passed since the LATEST of: its registration deadline, the moment the creates
        // were handed to the bridge, and the last ObjectCreated received from the sim.
        var now = DateTime.UtcNow;
        var quiet = TimeSpan.FromSeconds(Math.Max(1, _vrf.CompositionTimeoutSeconds));
        if (_createsIssuedUtc != DateTime.MinValue && now - _createsIssuedUtc < quiet) return;
        if (_lastObjectCreatedUtc != DateTime.MinValue && now - _lastObjectCreatedUtc < quiet) return;
        foreach (var kv in _compositions)
        {
            if (kv.Value.Done) { _compositions.TryRemove(kv.Key, out _); continue; }
            if (kv.Value.Deadline > now) continue;
            FinishComposition(kv.Value, timedOut: true);
        }
    }
    private DateTime _createsIssuedUtc = DateTime.MinValue;     // tick thread: when EnqueueCreates ran
    private DateTime _lastObjectCreatedUtc = DateTime.MinValue; // tick thread: last ObjectCreated seen

    /// <summary>Load the offline catalog resolver once (lazy). Home = Vrf:VrfHome, else MAK_VRFDIR
    /// (set by the 5.2 runner), else the resolver default. A missing catalog is a WARN, not a crash -
    /// coarse leaves then fall back to template creation.</summary>
    private ObjectTypeResolver GetResolver()
    {
        if (_resolverTried) return _resolver;
        _resolverTried = true;
        try
        {
            string home = !string.IsNullOrWhiteSpace(_vrf.VrfHome) ? _vrf.VrfHome
                        : Environment.GetEnvironmentVariable("MAK_VRFDIR") is { Length: > 0 } m ? m
                        : ObjectTypeResolver.DefaultVrfHome;
            if (!Directory.Exists(ObjectTypeResolver.ModelSetsDir(home)))
            {
                _log.LogWarning("ComposeHierarchy: no VR-Forces catalog at {Dir} - cannot EXPAND coarse " +
                                "leaves; set Vrf:VrfHome. They fall back to template creation.",
                                ObjectTypeResolver.ModelSetsDir(home));
                return null;
            }
            _resolver = ObjectTypeResolver.LoadChain(home);
            _log.LogInformation("ComposeHierarchy: catalog loaded from {Home} (root {Sms}, {N} templates) " +
                                "for coarse-leaf expansion.", home, _resolver.RootSms, _resolver.Templates.Count);
        }
        catch (Exception e)
        {
            _log.LogWarning("ComposeHierarchy: catalog load failed ({Msg}) - coarse leaves fall back to " +
                            "template creation.", e.Message);
            _resolver = null;
        }
        return _resolver;
    }

    /// <summary>
    /// EXPAND-to-compose (Vrf:ComposeHierarchy): a COARSE LEAF aggregate - a childless unit whose
    /// catalog template is itself composed of UNIT sub-units (a company/battalion; a platoon whose
    /// members are vehicles is NOT expanded - it works as a template) - must not be created as a
    /// template (template higher-units scatter: G-A + mechanism wf_16e3e97f). Instead follow the
    /// vendor recipe with the member list read from the mapped template's .entity: create the leaf as
    /// an EMPTY shell, create each doctrinal sub-unit as an aggregate (createSubordinates=true - the
    /// PROVEN platoon path), and compose via AddToOrganization. Members are created + attached in the
    /// .entity's DECLARED order (the vendor's own composition order - NO reordering). Full TO&E incl
    /// the HQ (user ruling 2026-09-06). Appends synthesized children to toCreate/placements/hierarchy
    /// and registers the composition; runs AFTER ApplyHierarchyComposition, BEFORE de-stack/enqueue.
    /// </summary>
    private void ExpandCoarseLeaves(List<CreationPlan> toCreate, List<PlacementInput> placements,
                                   List<(string Uuid, string SuperiorUuid)> hierarchy,
                                   HashSet<int> composedChildIndices = null,
                                   List<(int ParentIndex, IReadOnlyList<int> ChildIndices)> composedGroups = null)
    {
        var res = GetResolver();
        if (res == null) return;                 // no catalog -> leaves fall back to template (logged)
        // N4 (2026-09-06): RECURSIVE. A synthesized sub-unit that is itself coarse (a battalion leaf's
        // companies) is expanded in turn, else it would be created as a TEMPLATE company - the exact
        // path C1b closed (a template HQ-section vehicle never reaches its slot, the unit never moves;
        // PREREG_CONSOLE_CHANNEL sec 6). The loop therefore runs over the GROWING list; depth is
        // bounded (MaxExpandDepth) and the platoon rule still stops it: a template whose subordinates
        // are vehicles is never expanded.
        const int MaxExpandDepth = 3;            // leaf -> sub-unit -> sub-sub-unit (bn -> coy -> plt)
        var depthOf = new Dictionary<string, int>();   // synthesized child name -> depth below its leaf
        for (int i = 0; i < toCreate.Count; i++)
        {
            var plan = toCreate[i];
            // A coarse leaf: an aggregate still slated for template creation (ApplyHierarchyComposition
            // did NOT flip it to a shell => it has no DECLARED children) and not already a composition.
            if (!plan.IsAggregate || !plan.CreateSubordinates || _compositions.ContainsKey(plan.Name)) continue;
            int depth = depthOf.TryGetValue(plan.Name, out var d) ? d : 0;
            if (depth >= MaxExpandDepth)
            {
                _log.LogWarning("ComposeHierarchy: {Name} is {Depth} levels below its leaf - expansion stops here " +
                                "(MaxExpandDepth); it is created as a template.", plan.Name, depth);
                continue;
            }

            int st = plan.Type.Kind == 11 ? 3 : 1;
            var q = new[] { st, plan.Type.Kind, plan.Type.Domain, plan.Type.Country,
                            plan.Type.Category, plan.Type.Subcategory, plan.Type.Specific, plan.Type.Extra };
            var template = res.Resolve(q);
            if (template == null) continue;
            // EXPAND only when the subordinates are themselves UNITS (a company of platoons). A platoon
            // (subs are vehicles) stays a template - proven to work (1222 4/4). A MIXED template (vehicles
            // AND sub-units, e.g. "Mechanized Platoon (USA Army M2)": 4 IFVs + HQ section + 3 rifle squads)
            // also stays a template: expanding it would synthesize the squads and DROP the vehicles
            // (readiness audit 2026-09-06; 22 COA-STP1 units map to such templates), and the template's
            // own mounting ("assign transports to squads") is the vendor's business.
            var unitSubs = template.SubordinateSpecs.Where(s => s.IsUnit).ToList();
            if (!ComposeOrder.IsPureHigherUnit(template.SubordinateSpecs.Count, unitSubs.Count))
            {
                if (unitSubs.Count > 0)
                    _log.LogInformation("ComposeHierarchy: {Name} ({Tmpl}) is a MIXED template ({Units} unit + {Veh} vehicle " +
                                        "subordinates) - created as a template, not expanded (vehicles would be lost).",
                                        plan.Name, template.Name, unitSubs.Count, template.SubordinateSpecs.Count - unitSubs.Count);
                continue;
            }

            var childNames = new List<string>();
            var childIndices = new List<int>();
            int n = 0;
            foreach (var s in unitSubs)          // DECLARED order - no reordering (vendor composition)
            {
                n++;
                string handle = string.IsNullOrEmpty(s.FunctionHandle) ? "SUB" : s.FunctionHandle;
                string childName = MakeChildName(plan.Name, handle, n);
                var ot = s.ObjectType;
                var childType = new EntityTypeSpec {
                    Kind = ot[1], Domain = ot[2], Country = ot[3], Category = ot[4],
                    Subcategory = ot[5], Specific = ot[6], Extra = ot[7] };
                var childPlan = new CreationPlan(true, childType, plan.Force, plan.HeadingDeg,
                                                 childName, plan.Pos, null) { CreateSubordinates = true };
                // A synthesized sub-unit is born ON ITS LEAF'S COORDINATE and is composed into it by
                // AddToOrganization below, so it is a COMPOSED CHILD in exactly the sense the
                // de-stack must not touch (CompositionPlan): its place is the leaf's formation's,
                // and spreading it would put a company's own platoons 700 m off their shell. It
                // carries no C2SIM uuid, so CompositionPlan.Classify cannot see it - the index is
                // recorded here instead.
                composedChildIndices?.Add(toCreate.Count);
                childIndices.Add(toCreate.Count);
                toCreate.Add(childPlan);
                placements.Add(new PlacementInput(ot[2], null, null));  // child DIS domain; placed on terrain
                hierarchy.Add(("", ""));                                // synthetic - not a C2SIM unit
                childNames.Add(childName);
                depthOf[childName] = depth + 1;                        // the loop will visit it (recursive)
            }

            toCreate[i] = plan with { CreateSubordinates = false };     // empty shell
            // These synthesized sub-units are composed siblings too, so the 2026-09-21 de-stack is
            // told about them - it will find no C2SIM echelon for them (they carry no uuid, let
            // alone an EchelonCode) and report them as not spread rather than move them at a
            // spacing nobody derived.
            if (composedGroups != null && childIndices.Count > 0)
                composedGroups.Add((i, childIndices));
            var expandReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _compositions[plan.Name] = new PendingComposition {
                ParentName = plan.Name, ExpectedChildNames = childNames,
                Deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.CompositionTimeoutSeconds)), Ready = expandReady };
            _compositionReady[plan.Name] = expandReady;
            foreach (var c in childNames) _childToParent[c] = plan.Name;
            _log.LogInformation("ComposeHierarchy: EXPAND coarse leaf {Parent} ({Tmpl}, depth {Depth}) -> {N} doctrinal " +
                                "sub-units (declared order) [{Kids}] + empty shell; compose via AddToOrganization.",
                                plan.Name, template.Name, depth, childNames.Count, string.Join(", ", childNames));
        }
    }

    /// <summary>
    /// ORDER-TIME MATERIALIZATION (Vrf:CreationPolicy=AtOrder, C13): give a shell its members the first
    /// time an order references it. Order thread. Three cases, each the recipe already proven at init:
    ///  1. a unit with DECLARED children: materialize each child (recursively); the parent is ready when
    ///     all its children are (its shell and its organization already exist from init);
    ///  2. a coarse leaf whose template is a PURE higher unit (a tank company): EXPAND-to-compose into
    ///     the existing shell (ExpandCoarseLeaves on a one-plan list; the shell's uuid is pre-resolved
    ///     so the children attach as soon as they are created);
    ///  3. a leaf whose template carries platforms (a platoon, a mixed template, a CP-proxy HQ section):
    ///     the shell is DELETED and the unit re-created as the TEMPLATE (createSubordinates=true, the
    ///     proven 1222 4/4 path); OnVrfObjectCreated re-attaches it under its superior shell.
    /// Registers _compositionReady[name] BEFORE returning, so RunTaskAsync's existing await gates the
    /// task. Idempotent per unit. Creates go through the same terrain-placement path as init.
    /// </summary>
    private void MaterializeUnit(string c2simUuid, string why)
    {
        if (string.IsNullOrEmpty(c2simUuid) || _materialized.ContainsKey(c2simUuid)) return;   // once per unit
        if (!_deferred.TryGetValue(c2simUuid, out var d)) return;          // not a planned unit
        var plan = d.Plan;
        if (!plan.IsAggregate) { _materialized.TryAdd(c2simUuid, 0); return; }   // platforms were created in full at init
        string name = plan.Name;

        // DEFER, DO NOT ABORT (D5b) - THE OVERLAP. Do not create, delete or re-create anything for
        // this unit while the INITIALIZATION's own creates are still outstanding. In run
        // 20260921T072530Z_wayb the init's six creates (:156-166) and four order-driven
        // delete-and-re-create pairs (:172-194) landed on the back end about a second apart, and
        // the back end faulted 1.3 s after the order (UNDIAGNOSED, n=1 - this removes the overlap,
        // it does not explain the crash). The gate is pre-registered FIRST, by the same mechanism
        // as the shell-not-reflected deferral below, so RunTaskAsync's existing composition await
        // holds the task and nothing dispatches into a shell that has not been given its members.
        if (!InitCreationsSettled(out int initOutstanding))
        {
            _compositionReady.GetOrAdd(name, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            _materializeOnInitSettled[name] = (c2simUuid, why);
            _log.LogInformation("{Line}", DispatchReadiness.MaterializeHeldLine(
                name, why, initOutstanding, _initPlannedNames.Count));
            return;
        }

        // Case 1: declared children (iterated in the DECLARED order, N2: the declared first child is the leader).
        if (_childUuidsBySuperior.TryGetValue(c2simUuid, out var childUuids) && childUuids.Count > 0)
        {
            _materialized.TryAdd(c2simUuid, 0);
            var ordered = d.DeclaredChildUuids is { Count: > 0 }
                ? ComposeOrder.ByDeclared(d.DeclaredChildUuids, childUuids, u => u) : childUuids;
            var childReady = new List<Task>();
            // The parent's OWN init composition (child shells attaching) may still be pending: chain it
            // (review fix) - FinishComposition completes that composition's own Ready, never this gate.
            if (_compositions.TryGetValue(name, out var initComp) && !initComp.Done && initComp.Ready != null)
                childReady.Add(initComp.Ready.Task);
            foreach (var cu in ordered)
            {
                MaterializeUnit(cu, why + " -> declared child of " + name);
                if (_deferred.TryGetValue(cu, out var cd) && _compositionReady.TryGetValue(cd.Plan.Name, out var ct))
                    childReady.Add(ct.Task);
            }
            var parentTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            // D5b: a gate pre-registered by a DEFERRAL - the init-barrier hold above, or any earlier
            // reference - is already being awaited by RunTaskAsync, and this line REPLACES it in the
            // map. Chain it, exactly as the case-2/case-3 path does at the bottom of this method.
            // Before the barrier hold existed this could not happen (the only pre-registration site
            // was BELOW this case), so the chaining had never been needed here.
            _compositionReady.TryGetValue(name, out var preGateForChildren);
            _compositionReady[name] = parentTcs;
            if (preGateForChildren != null && !ReferenceEquals(preGateForChildren, parentTcs))
                _ = parentTcs.Task.ContinueWith(_ => preGateForChildren.TrySetResult(), TaskScheduler.Default);
            _ = Task.WhenAll(childReady).ContinueWith(_ => parentTcs.TrySetResult(), TaskScheduler.Default);
            _log.LogInformation("MATERIALIZE {Name} ({Why}): {N} declared child unit(s) materialized; the unit is " +
                                "ready when they are.", name, why, childUuids.Count);
            return;
        }

        if (!_names.TryGetUuid(name, out var shellUuid))
        {
            // Review fix: the shell's ObjectCreated has not arrived yet (the order came within seconds of
            // the init). DEFER: register the gate now so the task waits, and run this again from
            // OnVrfObjectCreated when the shell arrives. Not marked materialized.
            _compositionReady.GetOrAdd(name, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            _materializeOnCreated[name] = (c2simUuid, why);
            _log.LogInformation("MATERIALIZE {Name} ({Why}): its shell is not reflected yet - deferred to the shell's " +
                                "ObjectCreated; the task waits on the composition gate.", name, why);
            return;
        }
        _materialized.TryAdd(c2simUuid, 0);
        // A gate pre-registered by the deferral above (or by any earlier reference) may already be
        // awaited by RunTaskAsync: whatever gate the recipes below install must complete it too.
        _compositionReady.TryGetValue(name, out var preGate);

        var toCreate = new List<CreationPlan> { plan with { CreateSubordinates = true } };
        var placements = new List<PlacementInput> { d.Placement };
        var hierarchy = new List<(string Uuid, string SuperiorUuid)> { (c2simUuid, d.SuperiorUuid) };

        // Case 2: pure higher unit -> expand into the existing shell.
        if (_vrf.ComposeHierarchy && GetResolver() != null) ExpandCoarseLeaves(toCreate, placements, hierarchy);
        if (toCreate.Count > 1)
        {
            // ExpandCoarseLeaves registered the composition expecting the shell's ObjectCreated; the shell
            // already exists - pre-resolve it and create only the sub-units.
            if (_compositions.TryGetValue(name, out var comp)) comp.ParentVrfUuid = shellUuid;
            toCreate.RemoveAt(0); placements.RemoveAt(0); hierarchy.RemoveAt(0);
            _log.LogInformation("MATERIALIZE {Name} ({Why}): EXPAND into the existing shell {Uuid} - {N} sub-unit " +
                                "create(s) issued.", name, why, shellUuid, toCreate.Count);
        }
        else
        {
            // B1 (cold-start review of 945e054) - A LAST-RESORT GUARD. NOT a structural guarantee,
            // and the delta review of 27960f6 was right to say so: what actually closes B1 is the
            // CAP in DispatchReadiness.BarrierSeconds, which makes the drain precede every task's
            // composition backstop by an inequality. This is the net under it, and the net has
            // THREE HOLES, named here so nobody mistakes it for the fix:
            //   (i)   _inFlight is written LATE - RecordDispatch runs at the final send (see
            //         MarkDispatched below), after the route-shift check, the terrain-profile query
            //         and the CreateRoute/route-created hop. A task that has passed
            //         TryDispatchOrHold but is still inside that pipeline is INVISIBLE here, and
            //         deleting its unit then is worse than deleting it mid-move: the pipeline is
            //         holding a uuid that no longer exists.
            //   (ii)  it is keyed on the unit being DELETED. For a COMPOSED PARENT the in-flight
            //         task is on the parent and the deletes are on its declared children, through
            //         the case-1 recursion above - so this guard would NOT have caught the original
            //         B1 on D5b's own fixture (task on 114.MechCoy~PXY, deletes on 114x.MechPlt).
            //   (iii) when it DOES refuse, only the first task is named. Every other task on this
            //         unit then classifies Ready (unparked, bound, readable) and drives the same
            //         empty shell with no line of its own.
            // Widening it (an "entered ExecuteTaskOnTick" set, ancestor lookup, a per-task repeat)
            // is a real change to the dispatch path and belongs in its own lane, not in the fix for
            // an ordering defect the cap already closes.
            // So: do NOT delete; say so as loudly as the log allows; leave the task on what it has;
            // and RELEASE the composition gate, because nothing else will now complete it.
            if (_inFlight.TryGetCurrent(name, out var dispatched))
            {
                _log.LogError("{Line}", DispatchReadiness.RefusedDeleteUnderDispatchedTask(name, dispatched.TaskName));
                if (_compositionReady.TryGetValue(name, out var strandedGate)) strandedGate.TrySetResult();
                if (preGate != null) preGate.TrySetResult();
                return;
            }
            // Case 3: template with platforms -> delete the shell, re-create as the template.
            _compositionReady[name] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _recreatePending[name] = 0;
            // m1: this is the ONE legitimate rebind - the shell is about to be deleted and the same
            // requested name re-created as its template, so the second ObjectCreated for it carries
            // the REAL uuid. Announce it; without this the registry refuses the rebind and every map
            // keeps pointing at the deleted shell. One-shot, consumed by that bind.
            _names.ExpectRebind(name);
            bool reattach = false;
            // Only an AGGREGATE superior can take it back (ApplyHierarchyComposition refuses a platform parent
            // the same way - review fix).
            if (d.SuperiorUuid.Length > 0 && _deferred.TryGetValue(d.SuperiorUuid, out var sup) && sup.Plan.IsAggregate
                && _names.TryGetUuid(sup.Plan.Name, out var supUuid))
            {
                _reattachToParentVrfUuid[name] = supUuid;
                reattach = true;
            }
            var shellToDelete = shellUuid;
            _tickActions.Enqueue(() => _bridge.DeleteObject(shellToDelete));
            // N13 / STP-854 instrument: the app log carries no per-line stamps, so the D9 and D5c
            // harvests could only BOUND the delete -> reflected round trip from the 2 s observer
            // sample. Stamp it here and measure it in ReleaseReflected.
            var issuedUtc = DateTime.UtcNow;
            _recreateIssuedUtc[name] = issuedUtc;
            _log.LogInformation("MATERIALIZE {Name} ({Why}): shell {Uuid} deleted at WALL " +
                                "{Wall:yyyy-MM-ddTHH:mm:ss.fffZ}; re-creating as the TEMPLATE " +
                                "{Tmpl} with its members{Re}.", name, why, shellUuid, issuedUtc,
                                plan.TemplateName, reattach ? " and re-attaching under its superior" : "");
        }
        if (preGate != null && _compositionReady.TryGetValue(name, out var newGate) && !ReferenceEquals(preGate, newGate))
            _ = newGate.Task.ContinueWith(_ => preGate.TrySetResult(), TaskScheduler.Default);
        // B8: the unit is no longer what it was announced as at init (an empty shell). Announce what
        // now represents it - the full template, or the N doctrinal sub-units it was composed from -
        // so the C2SIM side's picture does not stay stuck at creation time. Silent when the
        // representation is unchanged, and when there is no substitution to report at all.
        AnnounceSubstitution(c2simUuid, d, name, toCreate.Count > 1 ? toCreate.Count : 0);
        if (IsLiveLikeAltitudeMode()) StartPlacementTerrainQuery(toCreate, placements, "ORDER MATERIALIZATION");
        else EnqueueCreates(toCreate);
    }

    /// <summary>
    /// R-SURFACE-PROXY re-announcement (B8). Called when an order-time materialization has decided
    /// HOW the unit is now built: composedFrom &gt; 0 means EXPAND-to-compose (that many doctrinal
    /// sub-units), 0 means the template with its own members. Emits one NameObservation only when
    /// that representation DIFFERS from what this unit was last announced as - a re-creation as the
    /// same thing says nothing - and only when there is a substitution to report (a proxy template,
    /// or a composition standing in for the unit's own type). Gated by Vrf:SurfaceProxySubstitutions,
    /// the same switch as the init announcement.
    /// </summary>
    private void AnnounceSubstitution(string c2simUuid, DeferredUnit d, string name, int composedFrom)
    {
        if (!_vrf.SurfaceProxySubstitutions) return;
        var plan = d.Plan;
        if (!SubstitutionAnnouncer.Substituted(plan.Substitution, composedFrom)) return;
        string rep = SubstitutionAnnouncer.Representation(plan.TemplateName, true, composedFrom);
        string was = _substitutions.Current(name);   // read BEFORE ShouldAnnounce records the new one
        if (!_substitutions.ShouldAnnounce(name, rep)) return;
        string text = plan.Substitution.Length > 0 ? rep + " - " + plan.Substitution : rep;
        _log.LogInformation("R-SURFACE-PROXY: {Name} is now represented by '{Rep}' (it was announced as " +
                            "'{Was}') - re-announcing the substitution to C2SIM.",
                            name, rep, was.Length == 0 ? "(nothing)" : was);
        _ = PushReportAsync(ReportBuilder.BuildTypeSubstitutionReport(
                c2simUuid, string.IsNullOrEmpty(d.C2SimName) ? name : d.C2SimName, name, text,
                IsoNow(), NewReportId()), ReportKind.Observation);
    }

    /// <summary>Tick thread (review fix): a case-3 re-created unit is released for tasking when its NEW
    /// object is REFLECTED (readable through TryGetEntityGeodetic), or at the deadline with a warning -
    /// the ObjectCreated control message precedes HLA discovery by an unbounded interval.</summary>
    private void ReleaseReflected()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _awaitReflection)
        {
            bool reflected = _bridge.TryGetEntityGeodetic(kv.Value.Uuid, out _);
            if (!reflected && now < kv.Value.Deadline) continue;
            if (!_awaitReflection.TryRemove(kv.Key, out _)) continue;
            // N13: the round trip, MEASURED rather than inferred from a 2 s observer sample (D9 sec
            // 4.3(b) asked for exactly this, and the D5c harvest had to bound every create-to-delete
            // gap at 0.05-0.6 s for want of it). _recreateIssuedUtc is stamped where the delete is
            // enqueued; this is where the replacement became readable.
            _recreateReleasedUtc[kv.Key] = now;
            string roundTrip = _recreateIssuedUtc.TryGetValue(kv.Key, out var issuedUtc)
                ? " Re-create round trip (delete issued -> readable): "
                  + (now - issuedUtc).TotalSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                  + " s."
                : "";
            if (!reflected)
                _log.LogWarning("MATERIALIZE {Name}: re-created object {Uuid} not reflected within {T}s - releasing " +
                                "its task anyway, at WALL {Wall:yyyy-MM-ddTHH:mm:ss.fffZ}.{Trip}",
                                kv.Key, kv.Value.Uuid, _vrf.CompositionTimeoutSeconds, now, roundTrip);
            else
            {
                // N13: remember the uuid whose readability RELEASED this unit. ReadDeclaredChildPositions
                // prefers it over _names.TryGetUuid, which between the delete and the replacement's
                // ObjectCreated still resolves the name to the DELETED SHELL's uuid (NameRegistry's
                // one-shot ExpectRebind is consumed by that later bind) - and the RTI keeps a deleted
                // object's last reflected attributes readable, so that lookup can SUCCEED on a shell.
                _reflectedUuidByName[kv.Key] = kv.Value.Uuid;
                _log.LogInformation("MATERIALIZE {Name}: re-created object {Uuid} reflected at WALL " +
                                    "{Wall:yyyy-MM-ddTHH:mm:ss.fffZ}; ready for tasking.{Trip}",
                                    kv.Key, kv.Value.Uuid, now, roundTrip);
            }
            if (_compositionReady.TryGetValue(kv.Key, out var tcs)) tcs.TrySetResult();
        }
    }

    /// <summary>
    /// N13: the DECLARED CHILDREN of a composed parent, with their own reflected positions, for
    /// <see cref="RouteOriginPolicy"/>. Tick thread only (same thread and same two steps that
    /// <see cref="ReleaseReflected"/> used to release them for tasking, so it cannot see a view of
    /// the unit that the release never saw).
    ///
    /// RETURNS NULL for every taskee that is not a composed parent: <c>_declaredChildNamesByParent</c>
    /// is written ONLY by <c>ApplyHierarchyComposition</c>, so a platform, an independent aggregate
    /// and every taskee at all when Vrf:ComposeHierarchy is off get a null here and the policy's
    /// NotComposed arm returns the published read untouched.
    ///
    /// WHICH UUID "THE CHILD" MEANS, and why it is not simply the name registry's (the STP-852 delta
    /// review's point, restated by the D5c harvest). Between the case-3 DELETE being enqueued and the
    /// replacement's ObjectCreated arriving, <c>_names.TryGetUuid</c> still resolves that name to the
    /// DELETED SHELL's uuid - NameRegistry's one-shot <c>ExpectRebind</c> is consumed by that later
    /// bind, not by the delete - and the RTI keeps a deleted object's last reflected attributes
    /// readable (the STP-822 note on this same call), so the lookup can SUCCEED on a shell. This
    /// method therefore prefers <c>_reflectedUuidByName</c>, the uuid whose readability actually
    /// RELEASED the child for tasking in <see cref="ReleaseReflected"/>, and falls back to the name
    /// registry only for a child that was never re-created (nothing was deleted, so there is no
    /// window). Either way the geometry agrees: a shell and its replacement are created at the SAME
    /// de-stacked placement (the stored order-time plan carries the de-stacked Pos), which is why
    /// D5c's route origin was exact even though its dispatch preceded the re-creates.
    ///
    /// THE DEFECT'S OWN MECHANISM CANNOT OCCUR HERE. What puts the vendor's published centroid on
    /// R/2 or R/4 is a membership with a slot MISSING or a slot counted TWICE. This enumerates the
    /// DECLARED CHILD NAMES, each exactly once, and the policy refuses to average unless every one
    /// of them is readable - so neither a short sum nor a double count is representable.
    ///
    /// A child released by the reflection TIMEOUT (which logs its own warning) has no recorded
    /// reflected uuid; if the registry lookup also fails it is simply not readable here and the
    /// policy falls back rather than averaging a partial membership.
    /// </summary>
    private List<RouteOriginPolicy.Child> ReadDeclaredChildPositions(CreatedUnit unit)
    {
        if (!unit.IsAggregate || string.IsNullOrEmpty(unit.Name)) return null;
        if (!_declaredChildNamesByParent.TryGetValue(unit.Name, out var childNames)
            || childNames == null || childNames.Count == 0) return null;
        var children = new List<RouteOriginPolicy.Child>(childNames.Count);
        foreach (var childName in childNames)
        {
            bool proven = _reflectedUuidByName.TryGetValue(childName, out var childUuid);
            if (!proven && !_names.TryGetUuid(childName, out childUuid)) childUuid = null;
            if (!string.IsNullOrEmpty(childUuid) && _bridge.TryGetEntityGeodetic(childUuid, out var g))
                children.Add(new RouteOriginPolicy.Child(childName, true, g.LatDeg, g.LonDeg, g.AltMeters,
                                                         proven));
            else
                children.Add(new RouteOriginPolicy.Child(childName, false, 0.0, 0.0, 0.0, proven));
        }
        return children;
    }

    /// <summary>
    /// N13 / SF-1 (cold-start review of a34c35b): HOW LONG AGO this composed parent's declared
    /// children were last delete/re-created, in WALL seconds, or NaN when no such stamp exists.
    /// This is the ONLY evidence that licenses the ROUTE ORIGIN line to call a gap between the
    /// parent's published position and its children's centroid a RE-COMPOSE TRANSIENT.
    ///
    /// WHY IT IS NEEDED. <see cref="RouteOriginPolicy"/> is pure and cannot tell a transient from a
    /// unit under way, and D8 measured 16-25 m between a MOVING company's published position and its
    /// direct children's centroid with NO re-compose in flight at all (d8_harvest_report.md:278-290).
    /// Without this, every second task on a composed parent would have printed a diagnosis of a
    /// re-compose that did not happen, and a harvest greps that string as fact.
    ///
    /// THE MOST RECENT of the RELEASE stamps (the re-created object became readable) and, for a
    /// child whose re-create is still outstanding, the ISSUE stamp (the delete was enqueued) - a
    /// re-create still in flight is the most transient state there is, and D5c showed a dispatch
    /// can reach here before any release. Tick thread; the maps are concurrent and are cleared when
    /// a new initialization arms the barrier.
    /// </summary>
    private double SecondsSinceChildRecompose(IReadOnlyList<string> childNames)
    {
        if (childNames == null || childNames.Count == 0) return double.NaN;
        var now = DateTime.UtcNow;
        double best = double.NaN;
        foreach (var childName in childNames)
        {
            DateTime? when = null;
            if (_recreateReleasedUtc.TryGetValue(childName, out var released)) when = released;
            if (_recreateIssuedUtc.TryGetValue(childName, out var issued)
                && (when == null || issued > when.Value)) when = issued;
            if (when == null) continue;
            double age = (now - when.Value).TotalSeconds;
            if (double.IsNaN(best) || age < best) best = age;
        }
        return best;
    }

    // ================= DEFER, DO NOT ABORT (D5b, 2026-09-21) =================

    /// <summary>
    /// Record the INITIALIZATION's own creation set (init thread, before anything is issued). Runs
    /// once per distinct init delivery - the duplicate guard upstream means a re-pushed init does
    /// not re-arm it - and a genuinely NEW initialization re-arms the barrier for its own objects,
    /// which is what it should do.
    /// </summary>
    private void RecordInitCreationBarrier(List<CreationPlan> plans)
    {
        if (plans == null || plans.Count == 0) return;
        // Review note (945e054): a genuinely NEW initialization re-arms the barrier FOR ITS OWN
        // OBJECTS, which is what this method's own contract says. Accumulating both inits' names
        // made READY TO TASK report a cumulative denominator that belonged to no single init.
        _initPlannedNames.Clear();
        // N13: a genuinely NEW initialization re-plans and re-creates everything, so a uuid proved
        // readable for the PREVIOUS init's object of the same name is dead. Drop both N13 maps with
        // the barrier, for the same reason _initPlannedNames is dropped: they describe ONE init.
        _reflectedUuidByName.Clear();
        _recreateIssuedUtc.Clear();
        _recreateReleasedUtc.Clear();
        foreach (var p in plans)
        {
            if (string.IsNullOrEmpty(p.Name)) continue;
            _initPlannedNames[p.Name] = 0;
        }
        // N15: ONE census, shared with the C13 line above, so the two can never print different
        // shell counts for the same initialization again.
        int shells = CreationCensus.Of(plans).EmptyShells;
        _initShellCount = shells;
        _initSettled = false;
        _initPlannedUtc = DateTime.UtcNow;
        // TWO NUMBERS, ONE SETTING, AND THEY ARE NOT THE SAME NUMBER (D5c harvest, 2026-09-21): this
        // line used to quote "Vrf:DispatchReadinessTimeoutSeconds (20 s)" while the hold lines quote
        // the same key with its CONFIGURED 60 s. Both were right - 20 s is the B1 CAP this barrier
        // runs under, 60 s is the setting it is capped FROM - and nothing said so, which is a defect
        // in the line and not in either number. Both are now printed, labelled.
        _log.LogInformation("INIT CREATION BARRIER: {N} object(s) planned by this initialization " +
                            "({Shells} empty shell(s)), armed at WALL {Wall:yyyy-MM-ddTHH:mm:ss.fffZ}. " +
                            "Until they are bound, a task whose taskee is not taskable yet is HELD rather " +
                            "than dropped and an order-time materialization does not delete/re-create " +
                            "anything. THE BOUND THAT APPLIES HERE IS {T:F0} s: it is {Key} (configured " +
                            "{Cfg:F0} s) CAPPED so the work this barrier releases finishes before a task's " +
                            "own composition backstop gives up on it (B1). A HOLD LINE NAMING THE SAME " +
                            "SETTING MEANS THE CONFIGURED VALUE, NOT THIS CAPPED ONE - two different " +
                            "numbers, one setting, both correct. The {Ready} line says when the wait is over.",
                            _initPlannedNames.Count, shells, _initPlannedUtc,
                            DispatchReadiness.BarrierSeconds(_vrf.DispatchReadinessTimeoutSeconds,
                                             _vrf.CompositionTimeoutSeconds,
                                             _vrf.TerrainProfileTimeoutSeconds),
                            DispatchReadiness.TimeoutSettingKey,
                            _vrf.DispatchReadinessTimeoutSeconds > 0.0
                                ? _vrf.DispatchReadinessTimeoutSeconds
                                : DispatchReadiness.ObservationWindowSeconds,
                            DispatchReadiness.ReadyToTaskPrefix);
    }

    /// <summary>How many of the init's planned names are bound to a VR-Forces object. Managed maps
    /// only - safe from any thread (the order thread reads it for the ORDER BEFORE READY line).</summary>
    private int BoundInitNameCount()
    {
        int n = 0;
        foreach (var name in _initPlannedNames.Keys) if (_names.TryGetUuid(name, out _)) n++;
        return n;
    }

    /// <summary>
    /// Have the INITIALIZATION's own creates all bound? True when there is no init to wait for,
    /// when the barrier has already settled or expired, and when the feature is turned off
    /// (Vrf:DispatchReadinessTimeoutSeconds = 0 is the pre-2026-09-21 behaviour, in which an
    /// order-time materialization fires the instant the order names the unit).
    /// </summary>
    private bool InitCreationsSettled(out int outstanding)
    {
        outstanding = 0;
        if (_vrf.DispatchReadinessTimeoutSeconds <= 0.0) return true;
        if (_initSettled || _initPlannedUtc == DateTime.MinValue) return true;
        foreach (var name in _initPlannedNames.Keys)
            if (!_names.TryGetUuid(name, out _)) outstanding++;
        return outstanding == 0;
    }

    /// <summary>TICK THREAD ONLY - it reads the bridge. The taskee's state, from the managed maps
    /// plus one live-location read, through the shared rule in DispatchReadiness.</summary>
    private TaskeeReadiness ClassifyTaskee(string unitName, bool plannedAtInit)
    {
        bool bound = _names.TryGetUuid(unitName, out var vrfUuid);
        bool readable = bound && _bridge.TryGetEntityGeodetic(vrfUuid, out _);
        // B1 (cold-start review of 945e054): a unit whose order-time materialization is parked
        // behind the init barrier is never READY, however bound and readable its init shell is -
        // the object that answers is the empty shell, and the parked work would delete it once the
        // barrier settles.
        //
        // WHERE THIS ACTUALLY EARNS ITS PLACE (delta review of 27960f6), which is NOT the ordering
        // the cap already closes: the park uses _compositionReady.GetOrAdd, so if that name already
        // carries a COMPLETED gate from an earlier materialization, GetOrAdd hands back the stale
        // completed TCS, RunTaskAsync's gate loop sees IsCompleted and skips it, and the task
        // arrives here with the unit still parked. This catches exactly that, and the cross-order
        // case (a second order parks a unit while a first order's task on it is being classified).
        // It does NOT cover the AffectedEntity: TryDispatchOrHold classifies only the TASKEE, and an
        // affected entity's park is held off by the cap alone (its gate is in RunTaskAsync's `gates`
        // list, so the same inequality covers it).
        bool parked = _materializeOnInitSettled.ContainsKey(unitName);
        // GROUND CONTACT IS NOT A READINESS STATE (2026-09-25, RL-20260921-06; scope RL-20260925-01).
        // From 8aeb127 (2026-09-21) until then a unit measured off the terrain was held here as
        // BOUND-BUT-NOT-ON-THE-GROUND until a read-back confirmed its correction - and run
        // 20260921T143243Z showed the read-back never landing and two legitimate tasks ended on
        // units an independent trace shows on the terrain. The placement correction stays (the
        // init sweep); nothing holds a task on it.
        return DispatchReadiness.Classify(plannedAtInit, _names.IsRequested(unitName), bound,
                                          readable, parked);
    }

    /// <summary>
    /// TICK THREAD. ONE re-evaluation point for every wait this feature owns:
    ///   - each HELD TASK's gate is completed the moment its taskee is bound AND readable;
    ///   - the INIT BARRIER settles when every init-planned name is bound and one live location has
    ///     been read (the READY TO TASK line, once), or EXPIRES at
    ///     Vrf:DispatchReadinessTimeoutSeconds with the honest line - and either way the
    ///     order-time materializations it held are released, so nothing can wedge on it.
    /// Called from the tick loop AND from OnVrfObjectCreated: binding is an event, but "its location
    /// reads now" is only ever a poll, and the barrier has to expire even if nothing else happens.
    /// </summary>
    private void SweepDispatchReadiness()
    {
        foreach (var kv in _dispatchWaits)
        {
            if (kv.Value.Ready.Task.IsCompleted) continue;
            var s = ClassifyTaskee(kv.Key, kv.Value.PlannedAtInit);
            kv.Value.LastStateCode = (int)s;
            if (s == TaskeeReadiness.Ready) kv.Value.Ready.TrySetResult();
        }

        if (_initSettled || _initPlannedUtc == DateTime.MinValue) return;
        int planned = _initPlannedNames.Count;
        int bound = 0;
        var missing = new List<string>();
        foreach (var name in _initPlannedNames.Keys)
        {
            if (_names.TryGetUuid(name, out _)) bound++;
            else if (missing.Count < 8) missing.Add(name);
        }
        // THE LOCATION READ IS TAKEN ONLY ONCE EVERYTHING IS BOUND, and it stops at the first
        // success. Settling needs both conditions, so probing before the names are all bound buys
        // nothing and would cost one native read per bound object per 50 ms tick AND per
        // ObjectCreated - which on a 128-unit initialization is exactly the kind of cost a
        // bookkeeping sweep must not add to the tick thread.
        bool anyRead = false;
        if (planned > 0 && bound == planned)
            foreach (var name in _initPlannedNames.Keys)
                if (_names.TryGetUuid(name, out var uuid) && _bridge.TryGetEntityGeodetic(uuid, out _))
                { anyRead = true; break; }
        double waited = (DateTime.UtcNow - _initPlannedUtc).TotalSeconds;
        bool settled = planned > 0 && bound == planned && anyRead;
        bool expired = waited >= DispatchReadiness.BarrierSeconds(_vrf.DispatchReadinessTimeoutSeconds,
                                             _vrf.CompositionTimeoutSeconds,
                                             _vrf.TerrainProfileTimeoutSeconds);
        if (!settled && !expired) return;

        // SET FIRST, THEN SAY IT, THEN DRAIN: the held materializations re-enter MaterializeUnit,
        // which asks InitCreationsSettled again - it must already be true or they would re-queue.
        _initSettled = true;
        if (settled)
            _log.LogInformation("{Line}", DispatchReadiness.ReadyToTaskLine(
                bound, planned, _vrf.MaterializeAtOrder ? _initShellCount : 0, waited));
        else
            _log.LogWarning("{Line}", DispatchReadiness.NotReadyToTaskLine(
                bound, planned, string.Join(", ", missing) + (bound + missing.Count < planned ? ", ..." : ""),
                waited));
        DrainHeldMaterializations();
    }

    /// <summary>Tick thread: run every order-time materialization the init barrier held, in one
    /// pass, now that the init's own creates are done with the back end.</summary>
    private void DrainHeldMaterializations()
    {
        if (_materializeOnInitSettled.IsEmpty) return;
        int n = 0;
        foreach (var name in _materializeOnInitSettled.Keys.ToList())
        {
            if (!_materializeOnInitSettled.TryRemove(name, out var held)) continue;
            n++;
            MaterializeUnit(held.Uuid, held.Why + " (held for the initialization; released)");
        }
        if (n > 0)
            _log.LogInformation("MATERIALIZE: {N} order-time materialization(s) held for the " +
                                "initialization were released together - the init's creates and these " +
                                "delete/re-creates no longer overlap on the back end.", n);
    }

    /// <summary>
    /// TICK THREAD, and the ONE entry point a task's first dispatch pass now takes.
    ///
    /// A taskee that is READY falls straight through to ExecuteTaskOnTick in this same tick action,
    /// on this same thread, having logged nothing and waited for nothing - so a run in which
    /// nothing races is byte-for-byte the run it is today. A taskee in a TRANSIENT state is HELD
    /// (one loud line) and dispatched from HoldThenDispatchAsync when it becomes taskable. Anything
    /// that must be refused promptly - a taskee the init never planned, a back end STP-822 has
    /// declared LOST, or the feature turned off - falls through to ExecuteTaskOnTick and reaches
    /// exactly the abort it reaches today.
    /// </summary>
    private void TryDispatchOrHold(OrderTask task, CreatedUnit unit)
    {
        bool plannedAtInit = _unitByC2SimUuid.ContainsKey(task.TaskeeUuid ?? "");
        // BL-2 (cold-start review of 3151fec). Re-open the measurement for a unit that carries an
        // off-the-terrain verdict, so the verdict is re-taken against the terrain as it is NOW (a
        // late terrain page makes it wrong). Since 2026-09-25 the verdict holds nothing - the task
        // is classified exactly as if it had never been measured (RL-20260921-06) - so this only
        // keeps the placement record honest: the re-armed sweep runs concurrently on the tick
        // thread and prints CLEARED if it finds the unit on the terrain. It delays nothing.
        ReMeasureGroundContactIfStale(unit.Name);
        var state = ClassifyTaskee(unit.Name, plannedAtInit);
        if (!DispatchReadiness.ShouldHold(state, _vrf.DispatchReadinessTimeoutSeconds, _backendLost))
        {
            ExecuteTaskOnTick(task, unit);
            return;
        }
        // A gate that has already been completed is STALE - an order-time re-create can take a unit
        // back out of the taskable state - so it is replaced rather than re-used.
        var wait = _dispatchWaits.AddOrUpdate(unit.Name,
            _ => new DispatchWait(plannedAtInit, state),
            (_, old) => old.Ready.Task.IsCompleted ? new DispatchWait(plannedAtInit, state) : old);
        _log.LogWarning("{Line}", DispatchReadiness.HoldLine(task.TaskName, unit.Name, state,
                                                             _vrf.DispatchReadinessTimeoutSeconds));
        _ = HoldThenDispatchAsync(task, unit, state, wait, DateTime.UtcNow, TaskClockSeconds);
    }

    /// <summary>
    /// OFF the tick thread: wait out the hold, then dispatch or abandon.
    ///
    /// The wait ends on the GATE (completed by SweepDispatchReadiness, which runs on the tick and
    /// on every ObjectCreated), on the BOUND, or on a back-end LOSS - and a loss ends it at once,
    /// because a stopped simulator produces BOUND-BUT-NOT-READABLE for as long as anyone cares to
    /// wait and STP-822 already knows it is gone.
    ///
    /// The ending on failure is the one every other dispatch dead end in this file uses:
    /// NotifyAbandoned so the STREND successors fail FAST, and ONE TASKABRT through the single emit
    /// point with the state it was still in NAMED.
    /// </summary>
    private async Task HoldThenDispatchAsync(OrderTask task, CreatedUnit unit, TaskeeReadiness startState,
                                             DispatchWait wait, DateTime startedUtc, double startClock)
    {
        try
        {
            double bound = Math.Max(0.1, _vrf.DispatchReadinessTimeoutSeconds);
            var deadline = startedUtc.AddSeconds(bound);
            bool released = false;
            while (!released)
            {
                // The service is going away: leave silently, like RunTaskAsync's own
                // OperationCanceledException arm. Without this the linked token makes every
                // Task.Delay complete instantly and the loop spins out the whole bound.
                if (_stoppingToken.IsCancellationRequested) return;
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                // One second at a time so a back-end LOSS declared mid-hold is acted on within a
                // second instead of at the bound - the liveness signal is not an event we can await.
                var slice = remaining > TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : remaining;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
                var done = await Task.WhenAny(wait.Ready.Task, Task.Delay(slice, cts.Token));
                if (done == wait.Ready.Task) { cts.Cancel(); released = true; break; }
                if (_backendLost) break;
            }
            _dispatchWaits.TryRemove(new KeyValuePair<string, DispatchWait>(unit.Name, wait));
            double wall = (DateTime.UtcNow - startedUtc).TotalSeconds;
            if (released)
            {
                _log.LogInformation("{Line}", DispatchReadiness.ReleasedLine(
                    task.TaskName, unit.Name, startState, wall, TaskClockSeconds - startClock,
                    _vrf.TaskClock));
                _tickActions.Enqueue(() => DeferredDispatch.Run(
                    () => ExecuteTaskOnTick(task, unit), task.TaskUuid, task.TaskName,
                    DeferredDispatch.ReadinessRelease, _sequencer,
                    reason => PushTaskStatus(task.TaskeeUuid, task.TaskUuid,
                                             S.TaskStatusCodeType.TASKABRT, reason),
                    ex => _log.LogError("Task '{Task}' DISPATCH FAILED on the VR-Forces tick thread after " +
                                        "its readiness hold ({Type}: {Msg}) - it is abandoned and reported " +
                                        "TASKABRT so its STREND successors fail fast.",
                                        task.TaskName, ex.GetType().Name, ex.Message)));
                return;
            }
            var last = (TaskeeReadiness)wait.LastStateCode;
            string reason = _backendLost
                ? DispatchReadiness.BackendLostAbortReason(task.TaskName, unit.Name, last, wall)
                : DispatchReadiness.TimeoutAbortReason(task.TaskName, unit.Name, last, wall);
            _log.LogError("{Reason}", reason);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT, reason);
        }
        catch (OperationCanceledException) { /* service stopping */ }
        catch (Exception e)
        {
            // The hold itself died, so nothing will ever dispatch this task: the same ending.
            _log.LogError("Task '{Task}' readiness hold failed ({Type}: {Msg}) - abandoned.",
                          task.TaskName, e.GetType().Name, e.Message);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                           $"ABANDONED: the readiness hold for task '{task.TaskName}' failed ({e.Message})");
        }
    }

    /// <summary>A short, unique VRF marking for a synthesized sub-unit: "&lt;parent&gt;.&lt;handle&gt;&lt;n&gt;",
    /// trimmed to the marking limit.</summary>
    private static string MakeChildName(string parent, string handle, int n)
    {
        string suffix = "." + handle + n;
        int room = MaxVrfMarkingChars - suffix.Length;
        string p = parent.Length <= room ? parent : parent.Substring(0, Math.Max(1, room));
        return p + suffix;
    }

    /// <summary>
    /// INIT PLACEMENT terrain query (2026-09-05). ONE DtIfRequestTerrainProfileInformation for ALL
    /// create positions of this init (ifRequestTerrainProfileInformation.h:45-51 - the request is a
    /// plain vector of points and carries no task, so nothing binds this plumbing to the route
    /// path); the reply gives each object a create altitude AT the terrain, which is what MAK's own
    /// sample does (commandLineRemoteController.cxx:710-772) and what UG52 14.3.3 says the
    /// simulator then honours ("ground ... entities are placed on the ground ... at the highest
    /// possible terrain intersection").
    /// THREADING: the request is issued from a tick action because the native facade is
    /// single-threaded; the reply (OnVrfTerrainProfile) and the timeout sweep (ExpireTerrainRequests)
    /// both run Continue on that same thread, so FinalizePlacement and the create enqueue never race.
    /// CREATION IS NEVER BLOCKED ON THE QUERY: a request that cannot be sent finalizes immediately,
    /// and a request that is not answered is expired by the tick loop after
    /// TerrainProfileTimeoutSeconds - both with the pre-2026-09-05 fallback altitudes and a WARN.
    /// </summary>
    private void StartPlacementTerrainQuery(List<CreationPlan> plans, List<PlacementInput> inputs, string source)
    {
        // The request points ARE the (post-de-stack) create positions, so reply sample #i answers
        // plan i - the reply's user data "is the index of the terrain profile request satisfied with
        // the response" (ifRequestTerrainProfileInformation.h:47), which the facade puts in
        // TerrainHeightSample.Index (VrfFacade.h:234-246).
        //
        // *** OPEN - the request point's ALTITUDE. The request is a plain vector of geocentric
        // points (ifRequestTerrainProfileInformation.h:51) and NO vendor source says what role
        // their altitude plays. The back end's own per-point result is {soilType, testPoint,
        // terrainHeight} (vrfobjcore/terrainProfileRequestManager.h:109-117), which reads like a
        // height-of-terrain lookup at the test point rather than a ray cast from the requested
        // altitude - but the reply the facade actually reads is an intersectionPoint()
        // (VrfFacade.cpp:384), and "intersection" is ray language. These points carry the create
        // altitude (0, or the authored C2SIM MSL), which at a high-elevation AOI is ~1150 m BELOW
        // the surface; the route path has only ever sent points ABOVE it. If the altitude does
        // matter, the samples come back invalid or out of frame and every object falls back to the
        // pre-2026-09-05 altitude - the PLACEMENT summary line ("N of M came from the TERRAIN
        // QUERY") is the discriminator, and no run is silently placed on a fiction. Do not claim
        // either way without a run or a vendor statement. ***
        var points = plans.Select(p => p.Pos).ToList();
        _tickActions.Enqueue(() =>
        {
            uint requestId;
            try { requestId = _bridge.RequestTerrainProfile(points); }
            catch (Exception ex)
            {
                // Guard added 2026-09-05 (cold-start review): an exception here is otherwise
                // swallowed by TickLoop, leaving no pending entry and no FinalizePlacement, so the
                // creates would SILENTLY never happen. Fall back and create anyway.
                _log.LogWarning(ex, "Init ({Source}): terrain-profile request THREW for {N} create " +
                                "position(s) - creating at the FALLBACK altitudes.", source, points.Count);
                FinalizePlacement(plans, inputs, points, null);
                return;
            }
            if (requestId == 0)
            {
                _log.LogWarning("Init ({Source}): terrain-profile request for {N} create position(s) was NOT SENT " +
                                "(no controller, or no points) - creating at the FALLBACK altitudes (C2SIM MSL if " +
                                "given, else 0); the default create clamp is then the only thing placing them " +
                                "(ifCreateVrfObject.h:210-212).", source, points.Count);
                FinalizePlacement(plans, inputs, points, null);
                return;
            }
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.TerrainProfileTimeoutSeconds));
            _pendingTerrain[requestId] = new PendingTerrain(
                deadline, PlacementTerrainLabel,
                samples => FinalizePlacement(plans, inputs, points, samples),
                "creating at the FALLBACK altitudes (C2SIM MSL if given, else 0)");
            _log.LogInformation("Init ({Source}): terrain-profile request {Id} sent for {N} create position(s); " +
                                "creation deferred to the reply (timeout {T} s -> fallback altitudes).",
                                source, requestId, points.Count, _vrf.TerrainProfileTimeoutSeconds);
        });
    }

    /// <summary>
    /// Apply the terrain reply (or its absence) to every planned create, then queue the creates.
    /// Runs on the tick thread - see StartPlacementTerrainQuery. samples == null means no terrain
    /// height for anything (timeout, or the request was never sent).
    /// </summary>
    private void FinalizePlacement(List<CreationPlan> plans, List<PlacementInput> inputs,
                                   List<Geodetic> points, List<TerrainHeightSample> samples)
    {
        var terrain = ResolvePlacementTerrain(points, samples);
        int fromTerrain = 0;
        for (int i = 0; i < plans.Count; i++)
        {
            double? th = terrain.TryGetValue(i, out double h) ? h : null;
            var input = inputs[i];
            var d = PlacementPolicy.Decide(input.Domain, input.Agl, input.Msl,
                                           _vrf.AirDefaultAltitudeAglMeters, th, _vrf.CreateClearanceMeters);
            var p = plans[i];
            p = p with { Pos = new Geodetic { LatDeg = p.Pos.LatDeg, LonDeg = p.Pos.LonDeg, AltMeters = d.CreateAltMeters } };
            plans[i] = p;
            // Vrf:PlacementAglSet=false suppresses the belt-and-braces set so a run measures the
            // CREATE alone (PREREG_PLACEMENT_R9_52 A1; VrfSettings.PlacementAglSet). Default true.
            bool setRegistered = _vrf.PlacementAglSet && d.SetAglMeters is double;
            if (setRegistered) _pendingAltitude[p.Name] = d.SetAglMeters.Value;
            if (d.CreateAltFromTerrain) fromTerrain++;
            // The set field reports what was ACTUALLY registered, not what the policy computed:
            // with Vrf:PlacementAglSet=false the policy still returns a value but none is sent, and
            // the log must not claim a set that did not happen (caught in the seat's own review).
            // THE WALL STAMP (2026-09-21, queued item): a placement is a moment in a STREAMING
            // terrain's life, and without a stamp a harvest cannot line these lines up against
            // when the terrain became sampleable. Stamped here, not taken once for the batch, so
            // a slow enqueue is visible.
            //
            // *** THE STAMP GOES AT THE END, AFTER THE FIXED "PLACEMENT:" PREFIX. *** BL-3 of the
            // cold-start review of 3151fec: a first version wrote "PLACEMENT at WALL <stamp>:",
            // which deletes the substring "PLACEMENT:" - and RunnerLib.ps1:1227-1228
            // (Get-PlacementRows) rejects on exactly that literal, so it returned EMPTY and the
            // Stage-7d warm/cold cache-state indicator (RunC2SimScenario.ps1:4870-4911) degraded
            // to "UNKNOWN for this run". That indicator is the discriminator for the very defect
            // this lane repairs. Adding to the END of a log line is free; moving its prefix is not.
            _log.LogInformation("PLACEMENT: {Kind} {Name} domain={Domain} created at authored lat/lon; create alt " +
                                "{CreateAlt} m from the {AltSource} (terrain height under the create point: " +
                                "{Terrain}); post-create SetAltitude: {Set} - {Why} (WALL {Wall:yyyy-MM-ddTHH:mm:ss.fffZ}).",
                                p.IsAggregate ? "UNIT" : "PLATFORM", p.Name, input.Domain, d.CreateAltMeters,
                                d.CreateAltFromTerrain ? "TERRAIN QUERY" : "FALLBACK",
                                th is double t ? FormattableString.Invariant($"{t:F1} m") : "UNKNOWN",
                                setRegistered ? $"{d.SetAglMeters.Value} m ABOVE GROUND LEVEL"
                                    : (d.SetAglMeters is double sup ? $"SUPPRESSED (policy {sup} m; Vrf:PlacementAglSet=false)" : "none"),
                                d.Why, DateTime.UtcNow);
            // A LAND PLATFORM placed on the FALLBACK has been placed by NOTHING (see
            // PlacementReclampPolicy): queue it for re-measurement once it is bound.
            //
            // AIR / SURFACE / SUBSURFACE are excluded - the vendor rule for them is not "at the
            // terrain" (UG52 14.3.3), so a terrain gap says nothing about them.
            //
            // *** AGGREGATES ARE EXCLUDED TOO (BL-1, cold-start review of 3151fec). *** The only
            // altitude this sweep can read for a unit is its PUBLISHED Z, and this repo's settled
            // note forbids reading it as ground contact: "an AGGREGATE's published Z is a derived
            // quantity whose exact rule is NOT DOCUMENTED ... Verifying 'on the ground' means
            // reading the MEMBERS, never the aggregate's Z" (VRF_ALTITUDE_FRAMES sec 1a). A first
            // version enrolled them, and the consequence was not cosmetic: it would measure the
            // wrong quantity, "correct" it, read it back, disagree, and mark the unit off the
            // ground - on D10's composition (9 UNIT / 1 PLATFORM) that is nine taskees in ten.
            // WHAT COVERS AN AGGREGATE INSTEAD: the dispatch-time measurement
            // (LogGroundContactAtDispatch), which measures it on its MEMBERS' CENTROID
            // (RouteOriginPolicy, the origin this method's caller passes as entityAlt) - the right
            // quantity, taken at the right time. Since 2026-09-25 it is logged, not acted on.
            if (_vrf.PlacementReclamp && !d.CreateAltFromTerrain && !p.IsAggregate
                && input.Domain == PlacementPolicy.DomainLand && !string.IsNullOrEmpty(p.Name))
                _reclamp[p.Name] = new ReclampEntry { Name = p.Name, Point = p.Pos };
        }
        _log.LogInformation("PLACEMENT summary: {T} of {N} create altitude(s) came from the TERRAIN QUERY, " +
                            "{F} from the FALLBACK (WALL {Wall:yyyy-MM-ddTHH:mm:ss.fffZ}).",
                            fromTerrain, plans.Count, plans.Count - fromTerrain, DateTime.UtcNow);
        ArmPlacementReclamp(plans.Count);
        EnqueueCreates(plans);
    }

    /// <summary>
    /// TICK THREAD (called from FinalizePlacement). Arm the re-clamp if - and only if - a LAND
    /// object was created on the FALLBACK. On a run whose init terrain query is answered this does
    /// nothing at all and prints nothing: `_reclamp` is empty, so the tick phase's own guard skips
    /// it forever and the R9/D10 shape is unchanged in lines and in timing.
    /// </summary>
    private void ArmPlacementReclamp(int planned)
    {
        if (_reclamp.IsEmpty) return;
        // A second init re-arms the window for whatever IT put in the map (the map is keyed by
        // name, so a re-planned object replaces its own entry).
        _reclampArmedUtc = DateTime.UtcNow;
        _reclampLastQueryUtc = DateTime.MinValue;   // the first sweep may query at once
        _reclampQueryInFlight = false;
        _reclampConcluded = false;
        _log.LogWarning("{Line}", PlacementReclampPolicy.ArmedLine(
            _reclamp.Count, planned, _vrf.PlacementReclampSeconds, _vrf.PlacementReclampRetrySeconds,
            _vrf.PlacementReclampToleranceMeters));
    }

    /// <summary>
    /// TICK THREAD. Re-ask the terrain for every object still waiting on a measurement, measure it
    /// against its own live altitude, correct what is off the ground with the documented call, and
    /// CONFIRM BY READING THE ALTITUDE BACK on a later sweep. Bounded by
    /// Vrf:PlacementReclampSeconds; one query in flight at a time; no faster than
    /// Vrf:PlacementReclampRetrySeconds.
    ///
    /// It never blocks a create, never touches the init barrier and never delays or refuses a
    /// dispatch (2026-09-25, RL-20260921-06): a unit with a task in flight is not corrected
    /// (SkippedTaskInFlightLine), and no ground-contact verdict holds a task.
    /// </summary>
    private void SweepPlacementReclamp()
    {
        double elapsed = (DateTime.UtcNow - _reclampArmedUtc).TotalSeconds;
        bool expired = PlacementReclampPolicy.Expired(elapsed, _vrf.PlacementReclampSeconds);
        if (expired && !_reclampQueryInFlight)
        {
            ConcludePlacementReclamp(elapsed);
            return;
        }
        if (!PlacementReclampPolicy.MayQuery(_reclampQueryInFlight,
                                             (DateTime.UtcNow - _reclampLastQueryUtc).TotalSeconds,
                                             _vrf.PlacementReclampRetrySeconds))
            return;
        // Only objects that are BOUND can be measured - there is no live altitude to read
        // otherwise - so an unbound object simply waits for the next sweep inside the bound.
        //
        // BL-2: StillOffGround IS MEASURED AGAIN. A verdict is a measurement, not a label, and the
        // thing that produced it - a terrain page that had not arrived - is exactly the thing that
        // can change underneath it. What is NOT repeated is the correction (CorrectionsIssued has
        // reached MaxCorrections, so Decide returns GiveUp and issues nothing) or its ERROR line
        // (GaveUpLogged). Only a settled ON-the-ground outcome stops being measured.
        //
        // *** THE TERRAIN IS ASKED ABOUT WHERE THE UNIT IS NOW, NOT WHERE IT WAS BORN. *** DL-1
        // (delta review of b3f9c38). A first version queried and corrected at the enrolled CREATE
        // point. That was survivable while the correction was an altitude-only setAltitude; the
        // moment it became a setLocation - "force a location for (sometimes called TELEPORTING) an
        // entity", setLocationRequest.h:26 - it became a yank back to the birth coordinate for any
        // unit that had begun driving. And the MEASUREMENT was already wrong on its own terms: it
        // compared an altitude read at the unit's CURRENT position against a terrain height sampled
        // at its BIRTH position, which on D10's relief (route altitudes 1,127-1,370 m) is hundreds
        // of metres of disagreement for a perfectly healthy moving unit.
        //
        // REACHABLE, not theoretical: an enrolled platform is UNMEASURED until a reply lands, and
        // Contact.Unknown is permissive, so it is taskable and can be driving while this window is
        // open - in run 20260921T114910Z the first TASKSTRTs land ~32 s into what would be a 60 s
        // window. MEASUREMENT: in that run the two platforms measured off the terrain displaced
        // 0.0 m, so no correction of theirs could have yanked a moving unit; that is the only
        // reason the window did not bite. WHY they did not move is NOT established and is not
        // claimed here (CORRECTION 2026-09-21: this comment read "the two buried platforms happened
        // not to move" - docs/VRF_ALTITUDE_FRAMES.md sec 5 falsifies "born buried, therefore never
        // moves" and sec 7's first tripwire fires on that word pairing). Nothing in this guard
        // rests on the reason; it rests on the 0.0 m.
        var pending = new List<ReclampEntry>();
        var points = new List<Geodetic>();
        foreach (var e in _reclamp.Values)
        {
            if (e.Outcome != PlacementReclampPolicy.Outcome.Pending
                && e.Outcome != PlacementReclampPolicy.Outcome.StillOffGround) continue;
            if (!_names.TryGetUuid(e.Name, out var uuid)) continue;
            // No live read, no measurement: an object we cannot locate is one we cannot judge.
            if (!_bridge.TryGetEntityGeodetic(uuid, out var live)) continue;
            e.QueriedAt = live;
            pending.Add(e);
            points.Add(live);
        }
        if (pending.Count == 0) return;
        uint requestId;
        try { requestId = _bridge.RequestTerrainProfile(points); }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "{Prefix}: the terrain-profile request THREW for {N} object(s) - " +
                            "retrying at the next interval inside the bound.",
                            PlacementReclampPolicy.Prefix, points.Count);
            _reclampLastQueryUtc = DateTime.UtcNow;
            return;
        }
        _reclampLastQueryUtc = DateTime.UtcNow;
        if (requestId == 0) return;      // no controller / no points: try again at the next interval
        _reclampQueryInFlight = true;
        _pendingTerrain[requestId] = new PendingTerrain(
            DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.TerrainProfileTimeoutSeconds)),
            PlacementReclampPolicy.Prefix,
            samples => ApplyPlacementReclamp(pending, points, samples),
            "leaving those objects unmeasured and re-asking at the next interval");
    }

    /// <summary>
    /// TICK THREAD, from the terrain reply (or its timeout). Measure, correct, or verify - one
    /// object at a time, through the pure policy so the log can never disagree with what was done.
    /// </summary>
    private void ApplyPlacementReclamp(List<ReclampEntry> pending, List<Geodetic> points,
                                       List<TerrainHeightSample> samples)
    {
        _reclampQueryInFlight = false;
        var terrain = ResolvePlacementTerrain(points, samples);
        for (int i = 0; i < pending.Count; i++)
        {
            var e = pending[i];
            if (!terrain.TryGetValue(i, out double th)) continue;          // no answer for this one yet
            if (!_names.TryGetUuid(e.Name, out var uuid)) continue;
            if (!_bridge.TryGetEntityGeodetic(uuid, out var live)) continue;
            // DL-1: the unit may have driven between the query and its reply, and then this terrain
            // height is about ground it no longer stands on. Not an error - just an answer that
            // does not apply. Skip it; the next sweep asks again about where it is by then. The
            // bound is the repo's existing calibrated "this sample answers this point" distance
            // (TerrainVertexAuthoring.DefaultMaxHorizontalMismatchMeters), not a second number for
            // the same idea.
            if (PlacementReclampPolicy.DriftedSinceQuery(e.QueriedAt.LatDeg, e.QueriedAt.LonDeg,
                                                         live.LatDeg, live.LonDeg))
                continue;
            var m = PlacementReclampPolicy.Measure(live.AltMeters, th, _vrf.PlacementReclampToleranceMeters);
            var before = e.Last;
            e.Last = m;
            bool wasOffGround = e.Outcome == PlacementReclampPolicy.Outcome.StillOffGround;
            _groundContact[e.Name] = m.Contact;
            switch (PlacementReclampPolicy.Decide(m.Contact, e.CorrectionsIssued))
            {
                case PlacementReclampPolicy.Action.Correct:
                    // GUARD 1 (DL-1): NEVER TELEPORT A UNIT THAT IS UNDER A TASK. A wrong altitude
                    // on an idle unit is a placement defect; a setLocation into a running move
                    // pulls the vendor's movement controller out of its own plan after the C2 side
                    // has been told the task started. The measurement still stands and is re-taken
                    // when a later task asks about this unit (BL-2), so nothing is lost by waiting:
                    // a unit is corrected BEFORE it is tasked, never during.
                    if (_inFlight.TryGetCurrent(e.Name, out var busy))
                    {
                        _log.LogWarning("{Line}", PlacementReclampPolicy.SkippedTaskInFlightLine(
                            e.Name, busy.TaskName, m));
                        break;
                    }
                    // THE VENDOR'S OWN LEVER FOR THIS CLASS. setLocation, not setAltitude:
                    // setAltitudeRequest.h:23-25 says the altitude request "is ignored if the
                    // vehicle is not an air-going vehicle", while setLocationRequest.h:26-32 says
                    // "Ground vehicles will be clamped to the terrain surface". Already exposed -
                    // VrfBridge.cpp:429 -> VrfFacade.cpp:986 - so no native change.
                    //
                    // THE LAT/LON IS THE LIVE READ TAKEN THREE LINES ABOVE, not the create point
                    // (DL-1). The altitude is what the create would have used had the terrain
                    // answered; the header says it is discarded for a ground vehicle, so nothing
                    // rests on it. Built from the live read so that the request is, by
                    // construction, an ALTITUDE change and not a move.
                    var fix = PlacementReclampPolicy.CorrectionLocation(
                        live.LatDeg, live.LonDeg, th, _vrf.CreateClearanceMeters);
                    // GUARD 2 (DL-1): the invariant, asserted rather than asserted in a comment.
                    if (PlacementReclampPolicy.WouldMoveHorizontally(live.LatDeg, live.LonDeg,
                                                                     fix.LatDeg, fix.LonDeg))
                    {
                        _log.LogError("{Line}", PlacementReclampPolicy.RefusedToMoveLine(
                            e.Name, live.LatDeg, live.LonDeg, fix.LatDeg, fix.LonDeg));
                        break;
                    }
                    e.CorrectionsIssued++;
                    _log.LogWarning("{Line}", PlacementReclampPolicy.CorrectionLine(
                        e.Name, m, _vrf.PlacementReclampToleranceMeters,
                        fix.LatDeg, fix.LonDeg, fix.AltMeters));
                    _bridge.SetLocation(uuid, fix);
                    break;
                case PlacementReclampPolicy.Action.GiveUp:
                    e.Outcome = PlacementReclampPolicy.Outcome.StillOffGround;
                    // BL-2: said ONCE. The object stays measurable; repeating the ERROR every sweep
                    // would turn one finding into a log flood and tell a reader nothing new.
                    if (!e.GaveUpLogged)
                    {
                        e.GaveUpLogged = true;
                        _log.LogError("{Line}", PlacementReclampPolicy.GaveUpLine(e.Name, m));
                    }
                    break;
                default:
                    // On the ground. Which ENDING that is depends on whether we had to act - and,
                    // BL-2, on whether we are WITHDRAWING an earlier off-the-terrain verdict.
                    e.Outcome = PlacementReclampPolicy.Conclude(m.Contact, e.CorrectionsIssued);
                    if (wasOffGround)
                        _log.LogWarning("{Line}", PlacementReclampPolicy.ClearedLine(e.Name, m));
                    else if (e.Outcome == PlacementReclampPolicy.Outcome.Reclamped)
                        _log.LogInformation("{Line}", PlacementReclampPolicy.VerifiedLine(e.Name, before, m));
                    break;
            }
        }
        // Everything settled early? Say so now rather than at the bound. A StillOffGround entry is
        // SETTLED for the purpose of closing this window - it is re-measured when a new task asks
        // about it, which re-arms a new window - so it does not hold the summary open.
        bool anyPending = false;
        foreach (var e in _reclamp.Values)
            if (e.Outcome == PlacementReclampPolicy.Outcome.Pending) { anyPending = true; break; }
        if (!anyPending) ConcludePlacementReclamp((DateTime.UtcNow - _reclampArmedUtc).TotalSeconds);
    }

    /// <summary>
    /// TICK THREAD, from the ROUTE terrain reply. The reply carries the terrain height under vertex 0
    /// and the caller carries the taskee's live altitude - the measurement run 20260921T114910Z made
    /// (`app:1135` / `app:1939`, "live -0.0 m vs terrain 145.4 m ... authoring from terrain anyway").
    ///
    /// MEASURE AND LOG, NEVER HOLD (2026-09-25, RL-20260921-06; scope approved as RL-20260925-01).
    /// From 8aeb127 until then this was a GATE: a taskee measured off the terrain was HELD as
    /// BOUND-BUT-NOT-ON-THE-GROUND, sent a setLocation, and dispatched only once a read-back agreed -
    /// or REFUSED with a TASKABRT and its follow-ons abandoned. In run 20260921T143243Z that read-back
    /// never landed (why is undiagnosed) and the gate ended two tasks on units an independent trace
    /// shows on the terrain. Now the task is ALWAYS dispatched: the measurement updates the ground-
    /// contact record and is logged, one line either way. NO setLocation is sent here any more: the
    /// move follows immediately, and a setLocation into a running move is exactly the case the
    /// sweep's own in-flight guard refuses (SkippedTaskInFlightLine) - whether a setLocation issued
    /// just before a movement task is applied before that task starts is not citable from the vendor
    /// docs. The placement correction itself stays in the init-time sweep (SweepPlacementReclamp).
    ///
    /// SILENT WHEN IT KNOWS NOTHING: no reply, no usable sample for vertex 0, or the feature off,
    /// and it logs nothing.
    /// </summary>
    private void LogGroundContactAtDispatch(OrderTask task, CreatedUnit unit, List<Geodetic> liveVertices,
                                            double entityAltMeters, List<TerrainHeightSample> samples)
    {
        if (!_vrf.PlacementReclamp || samples == null || liveVertices == null || liveVertices.Count == 0)
            return;
        // Same frame check and same echo/no-data guard the placement path uses, so a reply in the
        // wrong frame or a "terrain 0.0 = no intersection" answer (terrainDatabase.h:398-399) can
        // never manufacture a measurement.
        var terrain = ResolvePlacementTerrain(liveVertices, samples);
        if (!terrain.TryGetValue(0, out double th)) return;
        var m = PlacementReclampPolicy.Measure(entityAltMeters, th, _vrf.PlacementReclampToleranceMeters);
        // This read is newer than anything the init sweep has, and it is taken under the taskee's
        // OWN position, so it replaces the record either way.
        _groundContact[unit.Name] = m.Contact;
        if (m.Contact == PlacementReclampPolicy.Contact.OffGround)
            _log.LogWarning("{Line}", PlacementReclampPolicy.DispatchGateOffTerrainLine(
                task.TaskName, unit.Name, m, _vrf.PlacementReclampToleranceMeters));
        else
            // SF-2 (cold-start review): ONE short line per ground dispatch measured ON the terrain -
            // the only evidence for the 50-100 m band between the tolerance and
            // TerrainVertexAuthoring's 100 m NOTE threshold. No extra native call: the measurement
            // is already in hand from the route's own terrain reply.
            _log.LogInformation("{Line}", PlacementReclampPolicy.GatePassedLine(
                unit.Name, m, _vrf.PlacementReclampToleranceMeters));
    }

    /// <summary>
    /// TICK THREAD. Re-open the sweep's window so the tick phase runs again.
    ///
    /// *** IT DOES NOT TOUCH _reclampQueryInFlight. *** DS-2 (delta review of b3f9c38): clearing it
    /// here while a query is genuinely outstanding lets the next sweep issue a SECOND one, and two
    /// replies then run ApplyPlacementReclamp over overlapping entry lists built from different
    /// points. MaxCorrections still caps requests at one per object, so it is not a double
    /// teleport - but a STALE reply landing after a newer one overwrites _groundContact and can
    /// re-assert OffGround immediately after CLEARED was printed (until 2026-09-25 that re-held a
    /// unit the app had just released; today it only mis-states the record). Resetting _reclampLastQueryUtc is all a re-open needs: it makes the NEXT
    /// query legal as soon as the in-flight one has landed.
    ///
    /// THE WINDOW RESTARTS, AND THAT IS THE INTENT, NOT A LEAK. _reclampArmedUtc is re-anchored, so
    /// Vrf:PlacementReclampSeconds is "60 s PER WINDOW" and not "60 s from the fallback creates" -
    /// a unit that keeps being asked about keeps being looked at. The settings comment and RUNBOOK
    /// 11h say this in those words.
    /// </summary>
    private void ReopenPlacementReclampWindow()
    {
        _reclampArmedUtc = DateTime.UtcNow;
        _reclampLastQueryUtc = DateTime.MinValue;
        _reclampConcluded = false;
    }

    /// <summary>
    /// BL-2 (cold-start review of 3151fec). TICK THREAD, at the top of every dispatch decision. A
    /// unit the app has judged OFF THE TERRAIN gets its measurement re-opened, so the judgement is
    /// re-taken against the terrain as it is NOW rather than as it was when the verdict was formed.
    ///
    /// A verdict is a measurement with a timestamp, not a property of the unit. The thing that
    /// produced it - a terrain page that had not arrived - is precisely the thing that changes
    /// underneath it, and on a streaming terrain it changes in the direction that makes the verdict
    /// wrong. The sweep issues NO new correction (the entry has already spent its one) and NO
    /// repeat ERROR (GaveUpLogged); it only re-measures, and it prints CLEARED if the unit is now
    /// on the terrain.
    ///
    /// Cheap and silent when there is nothing to do: the map lookup is the whole cost for every
    /// unit that was never measured or was measured on the ground.
    /// </summary>
    private void ReMeasureGroundContactIfStale(string unitName)
    {
        if (!_vrf.PlacementReclamp || string.IsNullOrEmpty(unitName)) return;
        if (!_groundContact.TryGetValue(unitName, out var contact)
            || contact != PlacementReclampPolicy.Contact.OffGround) return;
        if (!_reclamp.TryGetValue(unitName, out var e)) return;
        // Settled ON the ground is not re-opened; only a standing OFF-the-ground verdict is.
        if (e.Outcome != PlacementReclampPolicy.Outcome.StillOffGround
            && e.Outcome != PlacementReclampPolicy.Outcome.Pending) return;
        ReopenPlacementReclampWindow();
    }

    /// <summary>TICK THREAD. One summary, once per window. A still-Pending entry is concluded by
    /// PlacementReclampPolicy.ConcludeAtBound: CORRECTED with no read-back, measured off the terrain
    /// and not corrected (a task was in flight), or - only when nothing was measured at all - NEVER
    /// MEASURED. The false "32 NEVER MEASURED" of run 20260921T143243Z was the first case counted
    /// as the third (RL-20260921-06 measurement).</summary>
    private void ConcludePlacementReclamp(double wallSeconds)
    {
        if (_reclampConcluded) return;
        _reclampConcluded = true;
        // A CENSUS OF THE MAP, not running counters (caught in this lane's own review): a new task can
        // re-open the window for one unit (BL-2), and a summary that mixed cumulative totals with a
        // per-window count would be two different questions answered in one sentence.
        var outcomes = new List<PlacementReclampPolicy.Outcome>();
        foreach (var e in _reclamp.Values)
        {
            if (e.Outcome == PlacementReclampPolicy.Outcome.Pending)
            {
                e.Outcome = PlacementReclampPolicy.ConcludeAtBound(e.Outcome, e.CorrectionsIssued, e.Last.Contact);
                if (e.Outcome == PlacementReclampPolicy.Outcome.NeverMeasured)
                    _groundContact.TryRemove(e.Name, out _);   // nothing was measured: Unknown
            }
            outcomes.Add(e.Outcome);
        }
        _log.LogWarning("{Line}", PlacementReclampPolicy.SummaryLine(
            PlacementReclampPolicy.Tally(outcomes), wallSeconds));
    }

    /// <summary>
    /// Reply sample -> terrain height per create-point index. Same FRAME check the route path
    /// applies (TerrainVertexAuthoring.DefaultMaxHorizontalMismatchMeters): a sample whose returned
    /// lat/lon is not under the point it claims to answer is not an answer for it, and a request or
    /// reply in the wrong frame lands nowhere near the points, so every sample fails here and the
    /// whole init falls back rather than being placed at a fiction.
    /// ECHO / NO-DATA GUARD (added 2026-09-05 per the cold-start review; an earlier version of this
    /// comment argued it was unnecessary - that was wrong). The back end returns terrainHeight 0.0
    /// when it finds no intersection (terrainDatabase.h:398-399); a create point sent at altitude 0
    /// that comes back "0.0 at its own lat/lon" would otherwise pass the frame check and be logged
    /// as a real sea-level answer. Reject a height within EchoToleranceMeters of the request point's
    /// own altitude - the fallback for that point is create-at-0 regardless, so it costs nothing.
    /// A genuine sea-level object is domain surface/subsurface, which never takes the terrain branch.
    /// </summary>
    private Dictionary<int, double> ResolvePlacementTerrain(List<Geodetic> points, List<TerrainHeightSample> samples)
    {
        var byIndex = new Dictionary<int, double>();
        if (samples == null) return byIndex;
        foreach (var s in samples)
        {
            if (!s.Valid || s.Index < 0 || s.Index >= points.Count) continue;
            var v = points[s.Index];
            double off = TerrainVertexAuthoring.DistMeters(v.LatDeg, v.LonDeg, s.LatDeg, s.LonDeg);
            if (off > TerrainVertexAuthoring.DefaultMaxHorizontalMismatchMeters)
            {
                _log.LogWarning("PLACEMENT: terrain sample #{Idx} came back {Off:F0} m from the create point it " +
                                "claims to answer - REJECTED (frame check); that object falls back.", s.Index, off);
                continue;
            }
            // ECHO / NO-DATA GUARD (added 2026-09-05 per the cold-start review). The back end
            // returns terrainHeight 0.0 when it finds no intersection (terrainDatabase.h:398-399;
            // terrainProfileRequestManager.h:111 defaults terrainHeight(0.)). A create point sent
            // at altitude 0 that comes back "terrain 0.0" at its own lat/lon would pass the frame
            // check and be logged as a real TERRAIN QUERY answer. Reject a height within 1 cm of
            // the request point's own altitude (same constant as TerrainVertexAuthoring.cs:30):
            // the fallback for that point is create-at-0 anyway, so rejecting it costs nothing and
            // stops a no-data 0 from masquerading as a sea-level terrain answer.
            if (Math.Abs(s.TerrainAltMeters - v.AltMeters) < TerrainVertexAuthoring.EchoToleranceMeters)
            {
                _log.LogWarning("PLACEMENT: terrain sample #{Idx} returned {H:F2} m = the request point's own " +
                                "altitude (echo / no-data, terrainDatabase.h:398-399) - REJECTED; that object " +
                                "falls back.", s.Index, s.TerrainAltMeters);
                continue;
            }
            byIndex.TryAdd(s.Index, s.TerrainAltMeters);   // first answer per point wins
        }
        return byIndex;
    }

    private void OnObjectInitialization(object sender, C2SIMSDK.C2SIMNotificationEventParams e)
    {
        // Routes/graphics that arrive as ObjectInitialization after the main init
        // (the SDK added this event for exactly this - PORT.md sec 7).
        _log.LogInformation("C2SIM ObjectInitialization received ({Len} bytes).", e.Body?.Length ?? 0);
        // TODO(parity): parse + enqueue CreateRoute / CreateControlArea.
    }

    private void OnOrder(object sender, C2SIMSDK.C2SIMNotificationEventParams e)
    {
        _log.LogInformation("C2SIM Order received ({Len} bytes).", e.Body?.Length ?? 0);
        // E6: open this order's tile-census generation. The census itself is printed by whichever
        // scoring worker of this generation finishes last (ReportTileCensus).
        _tileCensus.BeginOrder();

        // Bare-movement parity port of executeTask (C2SIMinterface.cpp:2028). Parse the
        // order's tasks; for each, resolve the taskee (PerformingEntity, a C2SIM uuid) to
        // the unit we created at init, then enqueue the tasking onto the tick thread. The
        // two-layer TaskActionCode -> vrftask mapping is the Phase 4+ enrichment
        // (PORT.md sec 10 / TASK_EXPANSION_PLAN.md); this reproduces the bare projector.
        OrderData order;
        try { order = OrderParser.Parse(e.Body); }
        catch (Exception ex) { _log.LogError("Order parse failed: {Msg}", ex.Message); return; }

        foreach (var w in order.Warnings)
            _log.LogWarning("Order parse: {Warning}", w);

        // ---- THE ORDER'S OWN GRAPHICS, REGISTERED BEFORE ANY TASK IS TRANSLATED (2026-09-20) ----
        //
        // WHY HERE AND NOT AT INIT. `_graphicsByC2SimUuid` used to be filled ONLY inside
        // ProcessInitializationLocked, on the assumption that an order references graphics the
        // initialization created. The C2SIM schema never said that: `MapGraphicID` is a plain
        // UUID-patterned string (xsd:408-415) with no xs:keyref behind it, and OrderBodyType
        // carries its own `Entity` list BEFORE its `Task` list precisely so an order can define
        // the objects its tasks are about (xsd:2960-2977; "This message may define tasks, or it
        // may refer to tasks defined elsewhere", xsd:2963). Measured on the real STP export
        // (STP-IRON-STORM-SYNTHETIC, 2026-09-20): 35 MapGraphicID references, 34 of them naming a
        // graphic carried IN THE ORDER, ZERO naming an init graphic - so the init-only map
        // resolved NOTHING and every task silently fell back to its embedded Location.
        //
        // BEFORE THE TASK LOOP, and synchronously, for the reason the init registration is done
        // before its creates are enqueued: RunTaskAsync hands the task to a worker, and a
        // resolution that raced the registration would be a different answer on different runs.
        //
        // WHOEVER PUBLISHED THE UUID FIRST WINS. The init is the shared world every order is
        // written against, and an EARLIER order's graphic may already be driving a task in flight;
        // a later message redefining a uuid that is already published is a data defect, not an
        // update, and silently taking the newer one would make a task's geometry depend on message
        // order. Reported, never guessed at. (The map spans orders, like _taskByUuid.)
        // SF5 (cold-start review of 9d67f97): THE COLLISION CHECK READS THE CONTENT, NOT ONLY THE
        // UUID. Re-pushing the SAME order into a live interface - which the demo posture does -
        // used to emit one WARNING per graphic (33 on the Iron Storm export), each asserting "Two
        // different graphics under one uuid is a data defect in the export", which is FALSE of an
        // idempotent re-push and trains an operator to ignore the line that matters. Same id + same
        // content is now a silent re-registration counted on the summary line; same id + DIFFERENT
        // content is ONE warning per id, and keeps the published graphic exactly as before.
        //
        // LIFETIME, stated because it was not (and is asserted by --stpexport-selftest):
        //   * `_graphicsByC2SimUuid` is NEVER cleared. It spans orders, like `_taskByUuid`.
        //   * A NEW ORDER adds its graphics; any uuid already published KEEPS its existing graphic
        //     (first publisher wins) whichever message published it. An order therefore cannot
        //     redefine geometry an in-flight task may already be driving.
        //   * A NEW INITIALIZATION is the one exception and the only writer that OVERWRITES: the
        //     init registrations below assign unconditionally, so an init that arrives after an
        //     order replaces an order graphic under the same uuid. "The init wins" is the intended
        //     rule (the init is the shared world every order is written against) - it is now SAID,
        //     with a line naming the replacement, instead of happening in silence.
        if (order.Graphics.Count > 0)
        {
            int added = 0, collided = 0, identical = 0;
            foreach (var g in order.Graphics)
            {
                var incoming = new TaskGraphic(
                    g.Uuid, g.Name, g.Kind,
                    g.Points.Select(pt => (pt.Lat, pt.Lon, (double?)pt.Elev)).ToList());
                _graphicsByC2SimUuid.TryGetValue(g.Uuid, out var existing);
                var what = TaskGraphic.Classify(existing, incoming);
                if (what != TaskGraphic.Registration.New)
                {
                    if (what == TaskGraphic.Registration.Identical) { identical++; continue; }
                    collided++;
                    _log.LogWarning("Order graphic '{Name}' ({Element}) re-uses uuid {Uuid}, which is ALREADY " +
                                    "PUBLISHED as '{Other}' ({Kind}) with DIFFERENT CONTENT - by the " +
                                    "initialization, or by an earlier order in this run. The graphic already " +
                                    "under that uuid is KEPT and this one is IGNORED: tasks may already be " +
                                    "driving it, so redefining it would make a task's geometry depend on " +
                                    "message order. Two different graphics under one uuid is a data defect in " +
                                    "the export. (An identical re-publication is NOT this case and is silent.)",
                                    g.Name, g.Element, g.Uuid, existing.Name, existing.Kind);
                    continue;
                }
                _graphicsByC2SimUuid[g.Uuid] = incoming;
                added++;
            }
            if (identical > 0)
                _log.LogInformation("ORDER GRAPHICS: {N} graphic(s) in this order were ALREADY PUBLISHED WITH " +
                                    "IDENTICAL CONTENT (same name, kind and vertices) and were re-registered as " +
                                    "no-ops - the normal result of re-pushing the same order. Not a data defect " +
                                    "and not warned about (SF5).", identical);
            _log.LogInformation("ORDER GRAPHICS: {N} tactical graphic(s) carried by this order " +
                                "([{Breakdown}]) - {Added} registered for MapGraphicID resolution, {Identical} " +
                                "already published with IDENTICAL content (re-push, silent), {Collided} " +
                                "ignored as CONFLICTING uuid collisions with an already-published graphic. " +
                                "{Total} graphic(s) are now " +
                                "addressable. They are REGISTERED ONLY: no VR-Forces object is created from an " +
                                "order graphic (V4b still creates a task's objective area from its own geometry " +
                                "when Vrf:CreateTaskObjectiveAreas is on).",
                                order.Graphics.Count,
                                string.Join(", ", order.Graphics.GroupBy(g => g.Element)
                                                       .OrderByDescending(gr => gr.Count())
                                                       .Select(gr => $"{gr.Key} x{gr.Count()}")),
                                added, identical, collided, _graphicsByC2SimUuid.Count);
        }

        // Operator summary (DEMO_READINESS row 15): what this order asks for, before per-task lines.
        _log.LogInformation("ORDER: {Tasks} task(s) for {Taskees} taskee(s); verbs [{Verbs}].",
                            order.Tasks.Count,
                            order.Tasks.Select(t => t.TaskeeUuid ?? "").Distinct().Count(),
                            string.Join(", ", order.Tasks.Select(t => t.ActionCode ?? "?").GroupBy(c => c)
                                                    .OrderByDescending(g => g.Count())
                                                    .Select(g => g.Count() > 1 ? $"{g.Key} x{g.Count()}" : g.Key)));

        // DEFER, DO NOT ABORT (D5b): SAY SO when the order beat the initialization's own creations.
        // The runbook's only "we are live" signal is the READY line, which fires before the init is
        // even pushed; the line an operator should wait for is READY TO TASK. Said once per run,
        // and it is a WARNING because it names a race the operator can simply not run.
        // S1 (cold-start review of 945e054): gate on the OUTSTANDING CREATIONS, which is what the
        // sentence describes - NOT on _initSettled, which also requires one live location read. HLA
        // discovery after ObjectCreated is unbounded, so a cleanly-gated Way A run whose names were
        // all bound before the order could otherwise be WARNED about a race that did not happen.
        if (!_orderBeforeReadySaid && _initPlannedUtc != DateTime.MinValue)
        {
            int boundNow = BoundInitNameCount();
            if (boundNow < _initPlannedNames.Count)
            {
                _orderBeforeReadySaid = true;
                _log.LogWarning("{Line}", DispatchReadiness.OrderBeforeReadyLine(
                    boundNow, _initPlannedNames.Count));
            }
        }

        // M1: record the WHOLE order before orchestrating any of it. A gated task derives its
        // predecessor window from the predecessor's Duration, and STREND predecessors are not
        // guaranteed to come first in document order.
        foreach (var task in order.Tasks)
            if (!string.IsNullOrEmpty(task.TaskUuid)) _taskByUuid[task.TaskUuid] = task;

        // E3 (pass-3 review; SUPERVISOR DECISION extending Q4, not the user's ruling). WALK THE
        // PREDECESSOR GRAPH ONCE, BEFORE ANYTHING DISPATCHES. A startAfterTaskUuid that names its
        // own task, or two that name each other, make predecessorInThisOrder TRUE - so phase 1
        // takes the A1 backstop, and a cycle is the one dead end nothing ever abandons, because
        // every task on it waits for another task on it. Every task on the loop would hang a full
        // Vrf:TaskChainBackstopSeconds (a sim day) and then be TASKABRT'd with "never dispatched
        // within 86400s of order receipt" - true, and about the wrong thing. Q4 ruled that a
        // malformed task is refused loudly and at once instead of being carried on an invented
        // number; a cyclic reference is malformed in exactly that sense, and the whole graph is in
        // hand right here. The graph is _taskByUuid, which is what the GATE consults - so what is
        // checked is the same graph that would take the backstop (it spans orders; N2).
        var predecessorByUuid = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in _taskByUuid) predecessorByUuid[kv.Key] = kv.Value.StartAfterTaskUuid;
        var onPredecessorCycle = TaskDispatchPolicy.FindPredecessorCycles(predecessorByUuid);

        // E4 (pass-3 review): SAY HOW DEEP THIS ORDER IS, AGAINST THE BACKSTOP THAT BOUNDS IT.
        // Nothing compared the two. COA-STP1 is safe - its longest DISPATCH lead is 16,800 s
        // against 86,400 - but a deeper chain, or a Vrf:DurationScale above 1, is truncated at the
        // backstop and the operator finds out hours later as a burst of "never dispatched within
        // 86400s" lines with no way to tell they were arithmetic rather than a wedge. The graph is
        // in hand here and the arithmetic is the gate's own, so it costs one line.
        {
            var chain = new List<TaskDispatchPolicy.ChainNode>();
            foreach (var t in order.Tasks)
                chain.Add(new TaskDispatchPolicy.ChainNode(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs,
                                                           Math.Max(t.SimulationStartMs, t.RelativeDelayMs)));
            double lead = TaskDispatchPolicy.LongestChainLeadSeconds(chain, _durationScale);
            double end = TaskDispatchPolicy.LongestChainEndSeconds(chain, _durationScale);
            double backstop = TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(
                true, 0.0, _vrf.TaskChainBackstopSeconds);
            _log.LogInformation("CHAIN DEPTH: the deepest STREND chain in this order dispatches its last task " +
                                "{L:F0} s of TASK CLOCK after order receipt and is armed to end at {E:F0} s " +
                                "(Vrf:DurationScale={Scale}); Vrf:TaskChainBackstopSeconds is {B:F0} s. A1: the " +
                                "backstop is what bounds phase 1 for a predecessor that is a task in this " +
                                "order, so it has to outlive that lead.", lead, end, _durationScale, backstop);
            if (lead >= backstop)
                _log.LogWarning("CHAIN DEEPER THAN THE BACKSTOP: the deepest chain in this order needs {L:F0} s " +
                                "of task clock to reach its last dispatch, and Vrf:TaskChainBackstopSeconds is " +
                                "only {B:F0} s. Every task whose predecessor has not dispatched by then WILL BE " +
                                "SKIPPED with \"never dispatched within {Bq:F0}s of order receipt\", however " +
                                "healthy the chain is. Raise Vrf:TaskChainBackstopSeconds above the lead, or " +
                                "compress the order with Vrf:DurationScale (E4).", lead, backstop, backstop);
        }

        foreach (var task in order.Tasks)
        {
            if (string.IsNullOrEmpty(task.TaskeeUuid))
            {
                _log.LogWarning("Order task '{Name}' has no PerformingEntity - skipping.", task.TaskName);
                _sequencer.NotifyAbandoned(task.TaskUuid); // successors fail fast, not slow-timeout
                continue;
            }
            // E3: refused HERE, before any orchestration starts and therefore before any TASKSTRT -
            // the same shape as Q4's refusal. Tasks that merely POINT AT the loop without being on
            // it are not refused here: their gate fails PredecessorAbandoned the moment these
            // abandons land, which is the existing cascade.
            if (onPredecessorCycle.Contains(task.TaskUuid))
            {
                string cycle = TaskDispatchPolicy.DescribePredecessorCycle(task.TaskUuid, predecessorByUuid);
                _log.LogError("Task '{Task}' is MALFORMED and will NOT be executed: its startAfterTaskUuid " +
                              "chain forms a CYCLE ({Cycle}) - the task is gated, directly or through its " +
                              "predecessors, on ITSELF, so nothing on that chain can ever start. Left alone " +
                              "every task on the loop would wait out Vrf:TaskChainBackstopSeconds " +
                              "({B:F0} s of task clock) and then be skipped anyway. FIX THE ORDER: break the " +
                              "loop, or make one of these tasks a root (E3, extending the Q4 ruling of " +
                              "2026-09-14).", task.TaskName, cycle, _vrf.TaskChainBackstopSeconds);
                _sequencer.NotifyAbandoned(task.TaskUuid);
                PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"{TaskDispatchPolicy.CyclicPredecessorRefusal} - task '{task.TaskName}' ({cycle})");
                continue;
            }
            // Parity: executeTask errors if the taskee was never in the initialization
            // (C2SIMinterface.cpp:1965). Here the taskee must be one we created at init.
            if (!_unitByC2SimUuid.TryGetValue(task.TaskeeUuid, out var unit))
            {
                _log.LogError("TASKEEUUID {Uuid} NOT FOUND IN C2SIMINITIALIZATION - CANNOT EXECUTE TASK '{Name}'.",
                              task.TaskeeUuid, task.TaskName);
                _sequencer.NotifyAbandoned(task.TaskUuid);
                // B1 review finding 7: a dispatch dead end with a taskee uuid in hand is a task that
                // will never run, which is exactly what TASKABRT is for. Silence here left STP
                // waiting forever for a status it was never going to get.
                PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"REFUSED: taskee {task.TaskeeUuid} is not in the C2SIM initialization - " +
                               "this interface has no unit to task");
                continue;
            }
            // Orchestrate the task off-thread: wait for its predecessor + start delay
            // (TaskSequencer), THEN marshal the bridge work onto the tick thread. The C++
            // busy-waited inline (one detached thread per task); this awaits without
            // blocking, and bounds the predecessor wait with a timeout (PORT.md sec 6).
            // C13 (CreationPolicy=AtOrder): give the referenced unit(s) their members NOW, before the
            // task orchestration starts; MaterializeUnit registers the composition-ready gate that
            // RunTaskAsync already awaits, so the task cannot drive an empty shell.
            if (_vrf.MaterializeAtOrder)
            {
                MaterializeUnit(task.TaskeeUuid, $"task '{task.TaskName}' performer");
                if (!string.IsNullOrEmpty(task.AffectedEntity) && task.AffectedEntity != task.TaskeeUuid
                    && _unitByC2SimUuid.ContainsKey(task.AffectedEntity))
                    MaterializeUnit(task.AffectedEntity, $"task '{task.TaskName}' affected entity");
            }
            var t = task;
            var u = unit;
            _ = RunTaskAsync(t, u);
        }
    }

    /// <summary>R4: one place applies Vrf:DurationScale to an authored order time, so the Duration
    /// that ENDS a task and the StartTime that HOLDS one back can never be compressed differently.
    /// The scale itself is VALIDATED ONCE at start-up (m8) - a non-finite or non-positive value is
    /// rejected there and replaced with 1.0 - so this is a pure multiply, with no second opinion
    /// about what a bad scale means.</summary>
    private long ScaleOrderMs(long ms) => TaskDispatchPolicy.ScaleOrderMs(ms, _durationScale);

    // The VALIDATED Vrf:DurationScale (m8). Resolved once in ExecuteAsync; never read from _vrf
    // again, so the two halves of the order's clock cannot be compressed by different numbers.
    private double _durationScale = 1.0;

    // The VALIDATED Vrf:TaskPredecessorEndMarginSeconds (E5). Resolved once in ExecuteAsync for
    // the same reason: a value the start-up line has already refused must not be read again by
    // the gate derivation and quietly applied anyway.
    private double _predecessorEndMargin = TaskDispatchPolicy.DefaultPredecessorEndMarginSeconds;

    private async Task RunTaskAsync(OrderTask task, CreatedUnit unit)
    {
        try
        {
            // M1 (cold-start review of 5c67d41). THE GATE MUST OUTLIVE THE END TIME IT WAITS FOR.
            // R4 makes a predecessor's completion its ARMED END TIME - dispatch + Duration x
            // Vrf:DurationScale - and the flat Vrf:TaskPredecessorTimeoutSeconds expired first by
            // construction (600 s default against COA-STP1's 4,800 s and 7,200 s Durations: all 31
            // gated tasks skipped, 11 dispatches out of 42). The configured value stays the FLOOR;
            // a predecessor that carries a Duration raises the window to its own end time plus
            // Vrf:TaskPredecessorEndMarginSeconds.
            // A1 (pass-2 review of 0c96f50). AND THE GATE MUST ALSO OUTLIVE ITS PREDECESSOR'S LEAD
            // TIME. The window above covers the predecessor's DURATION; phase 1 of the gate - "has
            // it dispatched at all?" - is measured from THIS task's wait-start, which is order
            // receipt for all 42 tasks (the foreach above fires them in one loop). A d2 task
            // therefore demanded that its predecessor dispatch within 4,860 s while that
            // predecessor was itself waiting out a 7,200 s root: measured, 21 of 42 dispatched and
            // 21 were TASKABRT'd, at the defaults AND at the Demo overlay. Phase 1 now takes its
            // own window - the backstop when the predecessor is a task in THIS order (a real dead
            // end ABANDONS it, which fails the gate at once), the configured value only for a
            // DANGLING reference nothing will ever speak for.
            OrderTask predTask = null;
            bool predecessorInThisOrder = !string.IsNullOrEmpty(task.StartAfterTaskUuid)
                                          && _taskByUuid.TryGetValue(task.StartAfterTaskUuid, out predTask);
            // B4 (pass-2 review): the long window is derived from an end time that only exists
            // when Vrf:TimedCompletion is ON. With R4 turned off - the documented evidence-only
            // escape hatch - no timer is ever armed, so deriving 4,860 s from the predecessor's
            // Duration made the eventual skip eight times slower and quieter than the configured
            // 600 s, on the very setting an operator reaches for when something is already wrong.
            double predecessorEndSeconds = TaskDispatchPolicy.PredecessorEndSeconds(
                _vrf.TimedCompletion, predecessorInThisOrder, predTask?.DurationMs ?? 0L, _durationScale);
            double timeoutSeconds = TaskDispatchPolicy.PredecessorTimeoutSeconds(
                _vrf.TaskPredecessorTimeoutSeconds, predecessorEndSeconds,
                _predecessorEndMargin);
            double dispatchTimeoutSeconds = TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(
                predecessorInThisOrder, timeoutSeconds, _vrf.TaskChainBackstopSeconds);
            if (!string.IsNullOrEmpty(task.StartAfterTaskUuid))
                _log.LogInformation("Task '{Task}': gated on {Pred}, which {Where} and is armed to end {End:F0} s " +
                                    "after ITS dispatch. It has {D:F0} s to DISPATCH ({Why}) and then {T:F0} s to " +
                                    "COMPLETE; the configured Vrf:TaskPredecessorTimeoutSeconds={Cfg} s " +
                                    "(+{Margin} s margin) is the floor. M1/A1: a gate shorter than the end time it " +
                                    "waits for, or than its predecessor's own lead time, skips the successor by " +
                                    "construction.",
                                    task.TaskName, task.StartAfterTaskUuid,
                                    predecessorInThisOrder ? "IS a task in this order" : "is NOT in this order",
                                    predecessorEndSeconds, dispatchTimeoutSeconds,
                                    predecessorInThisOrder
                                        ? "A1: the Vrf:TaskChainBackstopSeconds backstop - a predecessor that really " +
                                          "dies is ABANDONED, which fails this gate at once"
                                        : "a DANGLING reference: nothing will ever dispatch or abandon it, so the " +
                                          "configured window is what bounds the wait",
                                    timeoutSeconds, _vrf.TaskPredecessorTimeoutSeconds,
                                    _predecessorEndMargin);
            // R4, the START half. The delay itself is NOT new - TaskSequencer has always waited
            // StartTime/SimulationTime/DelayTimeAmount before dispatching, and that is what keeps
            // COA-STP1's T13 (3h20m) from going out with the rest of the order. Two things are new:
            //   - the ABSOLUTE form of StartTime (TimeInstantType/DateTime) becomes a delay against
            //     order receipt, so an order from a producer that dates its tasks instead of
            //     delaying them is no longer dispatched immediately;
            //   - Vrf:DurationScale compresses the wait exactly as it compresses the Duration, so a
            //     demo that shortens a 2 h task does not then wait 3h20m for its successor.
            long startMs = task.SimulationStartMs;
            if (startMs == 0 && task.AbsoluteStartUtc is DateTime absoluteStart)
            {
                startMs = (long)Math.Max(0.0, (absoluteStart - DateTime.UtcNow).TotalMilliseconds);
                _log.LogInformation("Task '{Task}': StartTime is the ABSOLUTE form ({At:O}) - dispatching " +
                                    "{S:F0} s after order receipt.", task.TaskName, absoluteStart, startMs / 1000.0);
            }
            long scaledStartMs = ScaleOrderMs(startMs);
            long scaledRelativeMs = ScaleOrderMs(task.RelativeDelayMs);
            if (scaledStartMs > 0 || scaledRelativeMs > 0)
                _log.LogInformation("Task '{Task}': start delay {S:F0} s (order says {O:F0} s; " +
                                    "Vrf:DurationScale={Scale}) - it will not dispatch before then.",
                                    task.TaskName, Math.Max(scaledStartMs, scaledRelativeMs) / 1000.0,
                                    Math.Max(startMs, task.RelativeDelayMs) / 1000.0, _durationScale);
            var gate = await _sequencer.WaitForStartAsync(task.StartAfterTaskUuid, scaledStartMs,
                                                          scaledRelativeMs, timeoutSeconds,
                                                          _taskClockAxis, _stoppingToken,
                                                          dispatchTimeoutSeconds,
                                                          // RL-20260921-09: a predecessor whose unit is
                                                          // OVERDUE is waited for up to the chain backstop
                                                          // from its dispatch, not skipped at end + margin.
                                                          _vrf.TaskChainBackstopSeconds);
            if (gate != GateResult.Proceed)
            {
                // P0.2 (DEFECT B): the predecessor never completed. The OLD behavior always
                // dispatched anyway, so all gated tasks burst-retasked their units together
                // (VRF runs ONE task at a time - each retask REPLACED the in-flight task
                // mid-route). Policy now decides; default is skip.
                // B7: the two timeouts are different failures and the log has to say which. One
                // sentence for both reported every A1 skip against a dispatch that never happened.
                string why = TaskDispatchPolicy.GateFailureReason(gate, dispatchTimeoutSeconds,
                                                                  timeoutSeconds);
                string policy = (_vrf.PredecessorTimeoutPolicy ?? "skip").Trim().ToLowerInvariant();
                bool busy = _inFlight.IsBusy(unit.Name);
                bool dispatch = policy == "force" || (policy == "whenidle" && !busy);
                _log.LogWarning("Task '{Task}' predecessor {Pred} {Why}; policy={Policy}, unit {Name} is {State} " +
                                "-> {Action}.", task.TaskName, task.StartAfterTaskUuid, why,
                                policy, unit.Name, busy ? "BUSY (task in flight)" : "idle",
                                dispatch ? "dispatching" : "NOT dispatched");
                if (!dispatch)
                {
                    _sequencer.NotifyAbandoned(task.TaskUuid); // successors fail fast
                    // B1: a successor SKIPPED because its predecessor never completed is not going to
                    // be executed either - report it instead of leaving it silently unanswered
                    // (supervisor 2026-09-14).
                    PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                                   $"SKIPPED: predecessor {task.StartAfterTaskUuid} {why}; policy={policy}");
                    return;
                }
            }
            // COMPOSE-FROM-CHILDREN: a composed PARENT (e.g. a company) is tasked only AFTER its
            // declared children are attached (AddToOrganization), else the move would drive an empty
            // shell. _compositionReady is signalled by FinishComposition on success OR on the
            // ExpireCompositions timeout, so this await always completes within CompositionTimeoutSeconds
            // of init; the generous bound is a backstop only.
            // Review fix (wf_dcad86e3): gate on the TASKEE and, in AtOrder mode, on a distinct
            // AffectedEntity that is being materialized too - otherwise an ATTACK/BREACH/ESCRT would
            // resolve its target to the deleted shell while the re-create is in flight. The
            // ComposeHierarchy guard is gone: _compositionReady is only ever populated by the compose,
            // expand and materialize paths, so its presence is the condition.
            var gates = new List<string> { unit.Name };
            if (_vrf.MaterializeAtOrder && !string.IsNullOrEmpty(task.AffectedEntity)
                && task.AffectedEntity != task.TaskeeUuid
                && _unitByC2SimUuid.TryGetValue(task.AffectedEntity, out var affectedUnit))
                gates.Add(affectedUnit.Name);
            foreach (var gateName in gates)
            {
                if (!_compositionReady.TryGetValue(gateName, out var readyTcs) || readyTcs.Task.IsCompleted) continue;
                var composeBound = TimeSpan.FromSeconds(_vrf.CompositionTimeoutSeconds + 30);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
                var done = await Task.WhenAny(readyTcs.Task, Task.Delay(composeBound, cts.Token));
                if (done == readyTcs.Task) cts.Cancel();   // stop the timer
                else
                    _log.LogWarning("Task '{Task}': composition of {Name} not signalled within {T}s - dispatching " +
                                    "anyway (move may drive an incomplete unit).", task.TaskName, gateName, composeBound.TotalSeconds);
            }
            // A1 FOLLOW-UP, caught reviewing my own change. The tick-action drain catches a throw
            // and LOGS it (TickLoop) but tells the sequencer NOTHING, so a dispatch that died on the
            // tick thread left its successors waiting - for the derived window before A1, and for
            // the CHAIN BACKSTOP after it. A1 made this one narrow path much slower rather than
            // faster, which is exactly the trade the backstop is not supposed to make. Every other
            // dead end in this file abandons the task and tells STP; so does this one now.
            // D1 (pass-3 review): the ending is DeferredDispatch.Run, shared with the terrain-profile
            // re-entry (RunTerrainContinuation) - which is where the DEFAULT ground move is really
            // dispatched, and which this guard did not cover when it was written inline here.
            // DEFER, DO NOT ABORT (D5b): the first pass now enters through TryDispatchOrHold, which
            // classifies the taskee ON THIS THREAD (the classification reads the bridge) and either
            // calls ExecuteTaskOnTick in this same tick action - the unchanged path, for a taskee
            // that is ready, and for every state that must be refused promptly - or HOLDS the task.
            // Under CreationPolicy=AtOrder a platform "created in full" at init registers no
            // composition gate at all, so an order that beat the init's creates fell straight
            // through the gate above and was DROPPED at :3117 for a unit created a second later.
            _tickActions.Enqueue(() => DeferredDispatch.Run(
                () => TryDispatchOrHold(task, unit), task.TaskUuid, task.TaskName,
                DeferredDispatch.FirstPass, _sequencer,
                reason => PushTaskStatus(task.TaskeeUuid, task.TaskUuid,
                                         S.TaskStatusCodeType.TASKABRT, reason),
                ex => _log.LogError("Task '{Task}' DISPATCH FAILED on the VR-Forces tick thread " +
                                    "({Type}: {Msg}) - it is abandoned and reported TASKABRT so its " +
                                    "STREND successors fail fast.",
                                    task.TaskName, ex.GetType().Name, ex.Message)));
        }
        catch (OperationCanceledException) { /* service stopping */ }
        catch (Exception e)
        {
            _log.LogError("Task '{Task}' orchestration failed: {Msg}", task.TaskName, e.Message);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            // B1 review finding 7: the orchestration threw, so nothing will ever be dispatched for
            // this task. (The OperationCanceledException case above is service shutdown, not a task
            // failure, and deliberately stays silent - the server is going away too.)
            PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                           $"ABANDONED: orchestration of task '{task.TaskName}' failed before dispatch " +
                           $"({e.Message})");
        }
    }

    /// <summary>
    /// Runs on the VRF tick thread: the bare-movement body of executeTask
    /// (C2SIMinterface.cpp:2213-2424). Reads the taskee's live location as point 0,
    /// ground-clamps, appends the task's inline route points, applies ROE + the
    /// (parity no-op) SetTarget, then MoveToLocation (single point) or CreateRoute +
    /// deferred MoveAlongRoute. terrainRoute: the TerrainProfile-mode re-entry passes the
    /// terrain-authored vertices here (null on the first pass and in every other mode).
    /// shiftedRoute: the ROUTE SHIFT re-entry passes the route with its inserted waypoints here
    /// (null on the first pass, and on every pass when Vrf:PreflightRouteShift is turned off - it
    /// is ON by default since the user ruling of 2026-09-20). NOT NULL DOES NOT MEAN SHIFTED: the
    /// worker and the timeout sweep both re-enter here, and both pass a route - the authored one
    /// when nothing was shifted - which is what stops the re-entry queueing a second check.
    /// </summary>
    private void ExecuteTaskOnTick(OrderTask task, CreatedUnit unit, List<Geodetic> terrainRoute = null,
                                   List<Geodetic> shiftedRoute = null)
    {
        // Resolve the VRF uuid via the created object's name. Parity: executeTask drops
        // the task if the unit was not created (C2SIMinterface.cpp:2046-2050).
        if (!_names.TryGetUuid(unit.Name, out var vrfUuid))
        {
            _log.LogWarning("DROPPING TASK '{Task}' BECAUSE UNIT {Uuid} ({Name}) WAS NOT CREATED.",
                            task.TaskName, task.TaskeeUuid, unit.Name);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            // B1 review finding 7: this is precisely the path a NAME-UNRESOLVED unit takes - the G1
            // failure class B3 exists to fix. If B3's resolution ever misses, STP must hear about
            // it instead of the task vanishing (nine hours of silence in run G6).
            PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                           $"DROPPED: unit {unit.Name} has no VR-Forces object bound to its name - it was never " +
                           "created, or its created object could not be correlated to the name we requested");
            return;
        }

        // R1 - WHERE THIS TASK'S GEOMETRY COMES FROM. MapGraphicID(s) resolved against the
        // graphics created at init win; the task's embedded Location - valid C2SIM, and what STP
        // exports today - is used when they are absent or match nothing. The lines say WHICH path
        // was taken, on every task, so a run log answers the question without inference.
        // Logged on the FIRST pass only: the TerrainProfile reply re-enters this method with the
        // same task and would otherwise say it all twice.
        // SF9: the taskee's AUTHORED position goes in, so the resolver can recognise a graphic whose
        // leading vertex IS the unit's start and not splice routes that drive back through it. The
        // authored coordinate (not the live one) on purpose - it is the value STP itself writes as a
        // task's first vertex and the one Vrf:DropOriginVertexMeters is already measured against,
        // and it is in hand here, before the live read. A unit with no recorded authored position
        // (an order-time materialization that never ran through the init planner) passes null and
        // gets the pre-SF9 behaviour for the origin drop only.
        (double Lat, double Lon)? taskeeAuthored =
            _authoredPosByName.TryGetValue(unit.Name, out var ap) ? ap : null;
        var geometry = TaskGeometryResolver.Resolve(task, _graphicsByC2SimUuid, taskeeAuthored);
        // V4b - WHAT THOSE POINTS MEAN, PER VERB. The resolver says WHERE the geometry came from;
        // this says WHAT IT IS. C2SIM attaches no shape to a Location list (LocationType is a bare
        // choice of GeodeticCoordinate or RelativeLocation), so an objective ring, an axis of
        // advance and a control point arrive as the same XML and were all driven as routes before
        // this item. The rule is (verb, shape) -> Route | ObjectiveArea | Point, and it applies to
        // the EMBEDDED Location only: geometry that came from a MapGraphicID was already typed by
        // the graphic it names. See docs/experiments/DESIGN_V4B_EMBEDDED_LOCATION_2026-09-14.md.
        var reading = TaskGeometryInterpretation.Interpret(task.ActionCode, geometry.Points, geometry.Source);
        if (terrainRoute == null)
        {
            foreach (var line in geometry.Log)
                _log.LogInformation("Task '{Task}': {Line}.", task.TaskName, line);
            // M5: an unmatched MapGraphicID is a gap between the order and the initialization that
            // SILENTLY changes what the unit does - it falls through to the embedded Location or,
            // on an export that carries only the id, to R2 in place. It is a WARNING.
            foreach (var line in geometry.Warnings)
                _log.LogWarning("Task '{Task}': {Line}.", task.TaskName, line);
            _log.LogInformation("Task '{Task}': {Line}.", task.TaskName, reading.Log);
            if (reading.Note != null)
                _log.LogInformation("Task '{Task}': {Line}.", task.TaskName, reading.Note);
        }
        // THE OBJECTIVE AREA ITSELF. Created through the same factory the init uses, from the ring's
        // own vertices, under the TASK's C2SIM uuid - so V5/V6 can bind a vendor tactical task's
        // objective parameter to it with no extra map. EXACTLY ONCE per task: this method is
        // re-entered for the SAME task by the TerrainProfile reply (the default ground path) and an
        // order can be delivered twice, so the guard is the init's own _createdAreaKeys dictionary.
        // NEVER for a MapGraphicID ring - the init already created that object under that uuid.
        if (reading.CreateObjectiveArea && reading.AreaVertices.Count >= 3)
        {
            string objectiveName = TaskGeometryInterpretation.ObjectiveAreaName(task.TaskName);
            if (!_vrf.CreateTaskObjectiveAreas)
            {
                if (terrainRoute == null)
                    _log.LogInformation("Task '{Task}': objective area '{Name}' NOT created " +
                                        "(Vrf:CreateTaskObjectiveAreas=false); the move to its centroid is " +
                                        "unchanged, but no VR-Forces object carries the ring.",
                                        task.TaskName, objectiveName);
            }
            else if (TaskGeometryInterpretation.ShouldCreateObjectiveArea(_createdAreaKeys, task.TaskUuid, task.TaskName))
            {
                EnqueueControlAreaCreate(task.TaskUuid, objectiveName, reading.AreaVertices);
                _log.LogInformation("Task '{Task}': objective area '{Name}' queued for creation from {N} ring " +
                                    "vertex(es) under the TASK's own uuid {Uuid} (V4b).",
                                    task.TaskName, objectiveName, reading.AreaVertices.Count, task.TaskUuid);
            }
        }
        var taskPoints = reading.Points;

        // OBSERVATION CHANNEL: a template unit's members were created by the sim, not by us, so
        // ObjectCreated never opened THEIR consoles. Open them now (Vrf:ObjectConsoleNotifyLevel
        // >= 0) from the aggregate's published member list, so the per-member offset-route /
        // formation messages of this task are captured too (UG52 21.9.1 p483).
        if (_vrf.ObjectConsoleMemberNotifyLevel >= 0 && unit.IsAggregate)
        {
            var consoleMembers = _bridge.GetAggregateMembers(vrfUuid);
            if (consoleMembers is { Count: > 0 })
            {
                // N5 (D7 harvest): DE-DUPLICATE BEFORE REQUESTING, AND SAY HOW MANY ARE DISTINCT.
                // VrfFacade::collectMembers recurses to depth 3 WITHOUT de-duplicating, so a member
                // published under two sub-aggregates appears TWICE - run D7 asked for "64 members
                // of 114.MechCoy~PXY" and listed 64 entries of which 16 were exact uuid duplicates
                // (48 distinct). Nothing was harmed (SetObjectNotifyLevel is idempotent and the
                // ARRIVAL EVIDENCE path already de-duplicates - the C16 fix), but the line taught a
                // harvest a member count that is not the unit's. Same de-duplication as C16: keyed
                // on the uuid, ordinal, first occurrence kept.
                var seenUuids = new HashSet<string>(StringComparer.Ordinal);
                var distinctMembers = consoleMembers.Where(m => !string.IsNullOrEmpty(m.Uuid)
                                                                && seenUuids.Add(m.Uuid)).ToList();
                int blankUuids = consoleMembers.Count(m => string.IsNullOrEmpty(m.Uuid));
                foreach (var m in distinctMembers)
                {
                    _names.TryAddName(m.Uuid, m.Name ?? "");
                    _bridge.SetObjectNotifyLevel(m.Uuid, _vrf.ObjectConsoleMemberNotifyLevel);
                    _navEvidence.ConsoleOpened(m.Uuid, _vrf.ObjectConsoleMemberNotifyLevel);
                }
                // Members are named WITH their uuids (2026-09-06): the console rows the observer captures
                // are uuid-keyed, and without this line's uuids a member's account could not be attributed
                // to its unit offline (PREREG_ORDER_TIME_MATERIALIZATION sec 3.3, instrument gap).
                _log.LogInformation("VRF console level {Level} requested for {N} DISTINCT member(s) of {Name} " +
                                    "({Raw} published, {Dup} duplicate uuid(s) from VrfFacade::collectMembers' " +
                                    "un-deduplicated depth-3 recursion, {Blank} with no uuid - N5): {Members}.",
                                    _vrf.ObjectConsoleMemberNotifyLevel, distinctMembers.Count, unit.Name,
                                    consoleMembers.Count,
                                    consoleMembers.Count - distinctMembers.Count - blankUuids, blankUuids,
                                    string.Join(", ", distinctMembers.Select(m => $"{m.Name} [{m.Uuid}]")));
            }
            else
                _log.LogInformation("VRF console: {Name} ({Vrf}) publishes NO members at task time - " +
                                    "only the aggregate's own console is open.", unit.Name, vrfUuid);
        }

        // The unit's in-flight record (P0.1) is written by MarkDispatched at each point a
        // VRF task is actually issued below - NOT here, so a task that aborts before
        // tasking VRF does not clobber the unit's real in-flight task.

        // LAYER 1 of the two-layer semantic map (docs/SEMANTIC_MAPPING.md): classify the
        // C2SIM verb. TODAY this only surfaces the semantic gap - every verb still executes
        // the bare movement projector below (Layer 2 dispatch lands in later units), so there
        // is ZERO behavior/golden-trace change. When a verb's Layer-2 composition is wired,
        // this becomes the switch that routes it (Breach, Attack, ...).
        var verb = VerbMapping.Classify(task.ActionCode);
        if (!verb.Recognized)
            _log.LogWarning("Task '{Task}' has UNRECOGNIZED verb '{Code}' (not in the semantic map); " +
                            "executing bare movement. Add it to VerbMapping (SEMANTIC_MAPPING.md sec 6).",
                            task.TaskName, verb.ActionCode);
        else if (!verb.Implemented)
            _log.LogInformation("Task '{Task}' verb={Code} -> intent={Intent} ({Comp}); " +
                                "Layer-2 not yet wired - executing bare movement.",
                                task.TaskName, verb.ActionCode, verb.Intent, verb.Composition);

        // LAYER 2 - ATTACK-family (ATTACK/DESTRY/FIX/DISRPT/PENTRT): resolve the affected
        // entity (a C2SIM uuid) to a VRF target for a DtFireAtTargetTask. Resolution uses the
        // init-created maps (_unitByC2SimUuid -> _names) - the two-dict chain that
        // dissolves the plan's uuid-resolution blocker (SEMANTIC_MAPPING.md sec 2b). The target
        // must be an entity our clientId created at init. R3 (user ruling 2026-09-14) settled the
        // SELF-TARGET case - STP names the taskee as the affected entity on all 42 COA-STP1 tasks,
        // and that is the objective, not an error - while an AffectedEntity we did not create is
        // still a scope/data gap and still warns (m3). Either way the task routes to its own
        // geometry and is never refused. The fire itself is issued AFTER the move below (advance
        // the axis, then engage); the move/fire task interaction in VRF is the live question.
        string attackTargetVrf = null;
        if (verb.Intent == TaskIntent.Attack)
            attackTargetVrf = ResolveAffectedTarget(task, vrfUuid, "ATTACK", "fire");

        // LAYER 2 - BREACH (Unit 2): resolve the affected OBSTACLE to a VRF target for a
        // DtBreachTask (approach move, then breach it). Same resolution as ATTACK; anything but a
        // distinct entity routes to the task's geometry (R3), which for a breach is the obstacle's
        // own location as the order drew it.
        string breachTargetVrf = null;
        if (verb.Intent == TaskIntent.Breach)
            breachTargetVrf = ResolveAffectedTarget(task, vrfUuid, "BREACH", "breach");

        // LAYER 2 - ESCRT (Escort): follow the escorted entity (DtFollowEntityTask). Following is
        // DYNAMIC - no route or point-0 needed - so dispatch it here, before the movement logic
        // (an ESCRT task may carry no route points, which would otherwise error below). Anything
        // but a distinct escorted entity -> the task's geometry (R3): fall through to the movement
        // below, and - when there is no geometry either - to R2's in-place execution.
        if (verb.Intent == TaskIntent.Escort)
        {
            string follow = ResolveAffectedTarget(task, vrfUuid, "ESCRT", "escort");
            if (follow != null)
            {
                Roe escortRoe = task.RuleOfEngagementCode == "ROEFree" ? Roe.FireAtWill
                              : task.RuleOfEngagementCode == "ROEHold" ? Roe.HoldFire
                              : Roe.FireWhenFiredUpon;
                _bridge.SetRulesOfEngagement(vrfUuid, escortRoe);
                MarkDispatched(task, unit, "follow");
                _bridge.FollowEntity(vrfUuid, follow);
                _arrivalReported.TryRemove(unit.Name, out _);   // the new VRF task is issued: its completion is its own
                ClearStallState(unit.Name);                     // ... and its own progress window (C16)
                _log.LogInformation("ESCRT task '{Task}': FollowEntity {Vrf} -> {Tgt} (escort; no route).",
                                    task.TaskName, vrfUuid, follow);
                return;
            }
        }

        // "Ground" = the DIS domain of the type we CREATED (SISO-REF-010.xml:3116 Land=1), not the
        // oracle's SIDC[2]=='G' symbology test (C2SIMinterface.cpp:2158). Replaced 2026-09-05.
        bool isGround = unit.Domain == 1;

        // Point 0 = the unit's live location from the sim (getUnitGeodeticFromSim, :2228).
        // KNOWN LIVE-RUN RISK (PORT.md sec 8): the port facade's TryGetEntityGeodetic uses
        // dynamic_cast and returns null for a DISAGGREGATED AGGREGATE (DtReflectedAggregate),
        // whereas the C++ oracle's static_cast returns a location and the aggregate moves.
        // So this abandon-path may fire for aggregates until the facade is reconciled -
        // that is the golden-aggregate-move blocker to resolve before the live parity run.
        if (!_bridge.TryGetEntityGeodetic(vrfUuid, out var live))
        {
            _log.LogError("NO PERFORMING UNIT - CAN'T EXECUTE TASK '{Task}': could not read a live location for " +
                          "{Name} ({Vrf}).", task.TaskName, unit.Name, vrfUuid);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            // m6 (cold-start review of 5c67d41): this is the ONE real "no performing unit" case
            // left - the unit exists in our maps but the simulation will not give us its position,
            // so nothing can be issued for it - and it used to abandon the chain in SILENCE, which
            // is exactly what B1 was built to end. The refusal that USED to live further down, at
            // the zero-geometry decision, was unreachable: ForZeroGeometry was called with the
            // literal performerResolved:true, because every path that could not resolve the
            // performer has already returned by then. This one.
            PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                           $"REFUSED at dispatch: task '{task.TaskName}' has no performing unit to execute it - " +
                           $"{unit.Name}'s live location could not be read from the simulation");
            return;
        }

        // THE ROUTE ORIGIN OF A COMPOSED PARENT (N13, run D9 2026-09-21; RouteOriginPolicy.cs).
        // `live` above is the parent's OWN published position, and for a COMPOSED parent that is
        // the one quantity on this path that is NOT trustworthy at this instant: its children have
        // just been deleted and re-created, the composition gate waited for THE CHILDREN to reflect
        // (ReleaseReflected), and VR-Forces publishes a composed aggregate at its DIRECT CHILDREN's
        // centroid - so while the membership is a mixture the parent is published on a transient
        // sweep (D9: R/2 at 300 deg, then R/4 at 120 deg against the 202.1 m ring). The children's
        // own positions are the settled answer and they are readable NOW, by the same two steps that
        // released them: NameRegistry -> TryGetEntityGeodetic, on this same tick thread.
        // EVERY OTHER TASKEE IS BYTE-FOR-BYTE UNCHANGED: _declaredChildNamesByParent is written only
        // by ApplyHierarchyComposition, so a platform, an independent aggregate (whose own object is
        // re-created whole, with its members, and therefore has no mixture) and every taskee at all
        // when Vrf:ComposeHierarchy is off take the NotComposed arm, which returns `live` untouched.
        var originChildren = ReadDeclaredChildPositions(unit);
        var originDecision = RouteOriginPolicy.Decide(live.LatDeg, live.LonDeg, live.AltMeters, originChildren);
        // Once per task. ExecuteTaskOnTick is re-entered by the route-shift worker and by the
        // terrain-profile reply, and only the FIRST pass's origin survives (pass 2 replaces routeGeo
        // with the shifted route built from it, pass 3 with the terrain-authored one).
        if (terrainRoute == null && shiftedRoute == null)
        {
            // SF-1: the line may only DIAGNOSE a re-compose transient on this evidence. Nothing in
            // the pure policy can tell one from a moving company (D8: 16-25 m with no re-compose).
            _declaredChildNamesByParent.TryGetValue(unit.Name, out var originChildNames);
            string originLine = RouteOriginPolicy.Line(unit.Name, originDecision, originChildren,
                                                       SecondsSinceChildRecompose(originChildNames));
            if (originLine != null)
            {
                if (originDecision.From == RouteOriginPolicy.Source.ChildrenCentroid)
                    _log.LogInformation("{Line}", originLine);
                else
                    _log.LogWarning("{Line}", originLine);   // both fallback arms are WARN
            }
        }
        var origin = new Geodetic
        {
            LatDeg = originDecision.LatDeg,
            LonDeg = originDecision.LonDeg,
            AltMeters = originDecision.AltMeters,
        };

        // LAYER 2 - A VERB THAT NAMES NO MOVEMENT (TaskIntent.HoldInPlace: ExecutePlanPhase).
        // R2 ruled that a task WITHOUT GEOMETRY is executed at the performing unit's own position;
        // this is the same ending reached through the VERB. It sits HERE - after the live position
        // is in hand and before routeGeo is built - because the answer must not depend on whether
        // the order also drew a line: a marker with a route is still a marker, and turning that
        // route into a drive is the "fake move" the vocabulary work exists to stop.
        //
        // NOTHING IS HIDDEN. The geometry the task carries is COUNTED in the line below, so a run
        // log says both that the unit stayed put and exactly how much authored geometry was not
        // driven - which is what an operator needs to decide whether the ORDER should have used a
        // movement code (for a forward passage of lines, STP's own code is CNFPSL).
        if (verb.Intent == TaskIntent.HoldInPlace)
        {
            // Q4 (user ruling 2026-09-14) applies UNCHANGED: no vendor task is issued here, so the
            // C2SIM Duration is the only thing that can ever end this task. Without one it would
            // sit in flight for the rest of the run and its STREND successors would wait out the
            // gate - the exact shape Q4 refuses. Same refusal, same emit point, same abandon.
            if (task.DurationMs <= 0)
            {
                _log.LogError("Task '{Task}' is MALFORMED and will NOT be executed: its verb '{Code}' names no " +
                              "movement, so no VR-Forces task is issued and NOTHING but its own Duration could " +
                              "ever end it - and the order gives it no USABLE Duration. Either " +
                              "ManeuverWarfareTask/Duration is absent, or it is present in a form the C2SIM 1.1 " +
                              "schema does not allow (IsoTimeDuration must be P##Y##M##DT##H##M##S with every " +
                              "field present, xsd:17-24; the short ISO form PT20M is NOT valid C2SIM) - the " +
                              "order-parse warnings above say which. Left alone this task would sit in flight " +
                              "for the run and its STREND successors would wait out the chain backstop. FIX THE " +
                              "ORDER (Q4, user ruling 2026-09-14).", task.TaskName, verb.ActionCode);
                _sequencer.NotifyAbandoned(task.TaskUuid);
                PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"{TaskDispatchPolicy.NoMovementVerbNoDurationRefusal} - task " +
                               $"'{task.TaskName}', verb '{verb.ActionCode}'");
                return;
            }
            // SF4 (cold-start review of 9d67f97): A HOLD ON A BUSY UNIT IS REFUSED, NOT FAKED.
            // See TaskDispatchPolicy.HoldInPlaceUnitBusyRefusal for the rule and its citations.
            // Without this, MarkDispatched's supersede arm would TASKABRT a task VR-Forces is
            // STILL RUNNING, log "VRF replaces the running task" when nothing replaced it, and
            // then report the unit as standing still while it drove on.
            bool busy = _inFlight.TryGetCurrent(unit.Name, out var running);
            if (TaskDispatchPolicy.HoldInPlaceMustRefuse(busy, running.TaskUuid, task.TaskUuid))
            {
                _log.LogError("Task '{Task}' is REFUSED: its verb '{Code}' names no movement, so executing it " +
                              "would mean STOPPING {Name}, which is already performing task '{Old}' " +
                              "({OldUuid}), which VR-Forces is running. No vendor halt is exposed to this " +
                              "interface (the facade offers tasks, not a per-unit stop; Pause is the whole " +
                              "simulation), so the unit would KEEP DRIVING while this task's report claimed it " +
                              "was in place. The running task is LEFT ALONE - it is not superseded and not " +
                              "aborted - and this one is refused with its cause named (SF4; Q1's " +
                              "supersede-abort applies only when the taskee is demonstrably not performing the " +
                              "old task). The {N} geometry point(s) this task carries are not driven either.",
                              task.TaskName, verb.ActionCode, unit.Name, running.TaskName,
                              running.TaskUuid, taskPoints.Count);
                _sequencer.NotifyAbandoned(task.TaskUuid);
                PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"{TaskDispatchPolicy.HoldInPlaceUnitBusyRefusal} - task '{task.TaskName}', " +
                               $"verb '{verb.ActionCode}'; {unit.Name} is performing '{running.TaskName}'");
                return;
            }
            // Exactly the R2 ending, and for the same reason it is written that way there: NO
            // destination is recorded (MarkDispatched dest: null), so the progress watchdog stays
            // off a unit that is CORRECTLY standing still, and NOTHING is cleared - this dispatch
            // issues no vendor task, so nothing was replaced (and by the guard above, nothing was
            // running).
            MarkDispatched(task, unit, "hold-in-place");
            _log.LogInformation("Task '{Task}': verb {Code} -> intent={Intent} ({Comp}). Executing IN PLACE at " +
                                "{Name}'s own position ({Lat:F5},{Lon:F5}); NO VR-Forces task is issued and the " +
                                "{N} geometry point(s) this task carries are NOT driven. The task ends at its " +
                                "end time and its successors follow.",
                                task.TaskName, verb.ActionCode, verb.Intent, verb.Composition,
                                unit.Name, origin.LatDeg, origin.LonDeg, taskPoints.Count);
            _ = PushReportAsync(ReportBuilder.BuildTypeSubstitutionReport(
                    task.TaskeeUuid, unit.Name, unit.Name,
                    $"task '{task.TaskName}': verb {verb.ActionCode} names no movement - " +
                    $"executing at the performing unit's position, {taskPoints.Count} authored geometry " +
                    "point(s) not driven",
                    IsoNow(), NewReportId()), ReportKind.Observation);
            return;
        }

        // Ground waypoint altitude (VrfSettings.GroundWaypointAltitudeMode): "Fixed100" is the
        // golden-parity 100 m MSL; "Live" puts ground waypoints just above the unit's OWN terrain
        // altitude so VRF's offset-route ground clamp succeeds at high-elevation regions (the
        // Mojave freeze). See docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md.
        double groundWpAlt = IsLiveLikeAltitudeMode()
            ? origin.AltMeters + _vrf.GroundWaypointLiveClearanceMeters
            : 100.0;

        var routeGeo = new List<Geodetic>
        {
            new() { LatDeg = origin.LatDeg, LonDeg = origin.LonDeg, AltMeters = isGround ? groundWpAlt : origin.AltMeters }
        };

        // NO GEOMETRY (R2, user ruling 2026-09-14: "a task without geometry uses the geometry of
        // the performing (who) unit"). The C++ parity behaviour was an ERROR that abandoned the
        // task AND, through NotifyAbandoned, its whole STREND chain (:2206-2210; run G6 lost the
        // air-defence chain T9-T12 exactly that way). The decision table is TaskDispatchPolicy -
        // the performer is resolved by construction here (every path that could not resolve it has
        // already returned above), so the only outcomes reachable are the three that EXECUTE.
        if (taskPoints.Count == 0)
        {
            var zeroGeometry = TaskDispatchPolicy.ForZeroGeometry(
                performerResolved: true, hasAttackTarget: attackTargetVrf != null,
                hasBreachTarget: breachTargetVrf != null);
            // In-place engagements (no move to wait for) stay immediate - P0.3 gates only
            // the advance-THEN-engage compositions.
            if (zeroGeometry == ZeroGeometryAction.EngageInPlace)
            {
                MarkDispatched(task, unit, "fire");
                _bridge.FireAtTarget(vrfUuid, attackTargetVrf);
                _arrivalReported.TryRemove(unit.Name, out _);
                ClearStallState(unit.Name);
                _log.LogInformation("ATTACK task '{Task}': no route points; FireAtTarget {Vrf} -> {Tgt} (engage in place).",
                                    task.TaskName, vrfUuid, attackTargetVrf);
                return;
            }
            if (zeroGeometry == ZeroGeometryAction.BreachInPlace)
            {
                MarkDispatched(task, unit, "breach");
                _bridge.Breach(vrfUuid, breachTargetVrf);
                _arrivalReported.TryRemove(unit.Name, out _);
                ClearStallState(unit.Name);
                _log.LogInformation("BREACH task '{Task}': no route points; Breach {Vrf} -> {Tgt} (breach in place).",
                                    task.TaskName, vrfUuid, breachTargetVrf);
                return;
            }
            // Q4 (USER RULING 2026-09-14). NO DURATION **AND** NO GEOMETRY IS MALFORMED. R2 gives
            // the task a place (where the unit stands) and R4 gives it an end (its Duration); a
            // task with neither has no vendor task to evidence it and no authored time to end it,
            // so it would sit "in flight" for the rest of the run and its successors would wait
            // out the gate. The supervisor default invented an end time (Vrf:DefaultHoldSeconds,
            // 60 s); the user ruled that a number which is not in the order is not ours to invent,
            // and that the order is at fault. So it is refused, loudly, like every other dead end:
            // ERROR naming BOTH missing elements, TASKABRT through the single emit point, and
            // NotifyAbandoned so the successors fail fast instead of waiting.
            if (TaskDispatchPolicy.IsMalformedZeroGeometryTask(zeroGeometry, task.DurationMs))
            {
                _log.LogError("Task '{Task}' is MALFORMED and will NOT be executed: the order gives it NO " +
                              "Duration (ManeuverWarfareTask/Duration/IsoTimeDuration) AND NO geometry (no " +
                              "MapGraphicID and no Location). R2 would execute it at {Name}'s own position and " +
                              "R4 would end it at its Duration - with neither, no VR-Forces task is issued, " +
                              "nothing could ever complete it, and its STREND successors would wait out the " +
                              "gate. FIX THE ORDER: give the task a Duration, or a geometry, or both " +
                              "(Q4, user ruling 2026-09-14).", task.TaskName, unit.Name);
                _sequencer.NotifyAbandoned(task.TaskUuid);
                PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"{TaskDispatchPolicy.MalformedZeroGeometryRefusal} - task '{task.TaskName}'");
                return;
            }
            if (zeroGeometry == ZeroGeometryAction.ExecuteInPlace)
            {
                // R2: the unit's OWN position is the task's geometry. No vendor task is issued -
                // "execute where you are" is what a unit already does - so the dispatch is the
                // TASKSTRT + the end time (MarkDispatched arms both), and the C2SIM side is told
                // how the geometry was derived instead of being left to infer it from silence.
                // NO destination is recorded (MarkDispatched dest: null), which is what keeps the
                // progress watchdog off a unit that is CORRECTLY standing still.
                MarkDispatched(task, unit, "hold-in-place");
                // m2 (cold-start review of 5c67d41): NOTHING IS CLEARED HERE. The two lines that
                // used to stand here cleared the arrival-evidence swallow and the progress
                // watchdog's window "because the new VRF task is issued" - and this is the one
                // dispatch kind that issues NO vendor task at all, so the OLD task keeps running
                // permanently rather than transiently. Clearing here un-swallowed the old task's
                // late completion and re-armed a progress window on a unit that has no destination.
                // NOT DONE, and recorded as a live question rather than invented here: issuing a
                // vendor HOLD so the in-place claim becomes true in the simulation. That changes
                // what the units DO and needs a run, not a review.
                _log.LogInformation("Task '{Task}' carries NO geometry: executing IN PLACE at {Name}'s own " +
                                    "position ({Lat:F5},{Lon:F5}) - R2 (user ruling 2026-09-14). No move is " +
                                    "issued; the task ends at its end time and its successors follow.",
                                    task.TaskName, unit.Name, origin.LatDeg, origin.LonDeg);
                _ = PushReportAsync(ReportBuilder.BuildTypeSubstitutionReport(
                        task.TaskeeUuid, unit.Name, unit.Name,
                        $"task '{task.TaskName}': {TaskDispatchPolicy.ZeroGeometryObservation}",
                        IsoNow(), NewReportId()), ReportKind.Observation);
                return;
            }
            // ZeroGeometryAction.Refuse IS NOT REACHABLE HERE and no branch pretends otherwise
            // (m6). ForZeroGeometry is called with the literal performerResolved:true because every
            // path that could not resolve the performer has already returned - the last of them is
            // the TryGetEntityGeodetic failure above, which is where the refusal and its TASKABRT
            // now live. The policy still HAS the Refuse outcome, and --rulings-selftest still
            // checks it, because it is the rule; this call site simply cannot produce it.
            // It is kept as a GUARD, not as policy, and it is never silent: if a future change
            // ever makes the performer predicate real, this reports rather than abandoning quietly.
            // It does NOT throw - this runs on the tick thread, and a refusal is not worth a
            // process-level event.
            _log.LogError("UNREACHABLE zero-geometry action {Action} for task '{Task}' - the performer is " +
                          "resolved by construction at this point. Treating it as a refusal.",
                          zeroGeometry, task.TaskName);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                           $"REFUSED at dispatch: task '{task.TaskName}' has no performing unit to execute it");
            return;
        }
        // ORIGIN VERTEX DROP (Vrf:DropOriginVertexMeters; PREREG_ASSEMBLY_LAYOUT 3f): STP's first route
        // point is the unit's own authored position. Once the unit has been spread away from it, that
        // vertex would march every unit back to the single assembly coordinate where the pile re-forms.
        // Drop the LEADING task points that sit on the authored origin when the unit is no longer
        // there - never all of them (a task whose only point is the origin keeps it).
        int skip = 0;
        if (_vrf.DropOriginVertexMeters > 0 && taskPoints.Count > 1
            && _authoredPosByName.TryGetValue(unit.Name, out var authored)
            && TerrainVertexAuthoring.DistMeters(origin.LatDeg, origin.LonDeg, authored.Lat, authored.Lon) > _vrf.DropOriginVertexMeters)
        {
            while (skip < taskPoints.Count - 1
                   && TerrainVertexAuthoring.DistMeters(taskPoints[skip].Lat, taskPoints[skip].Lon, authored.Lat, authored.Lon) <= _vrf.DropOriginVertexMeters)
                skip++;
            if (skip > 0)
                _log.LogInformation("Task '{Task}': dropped {N} leading route point(s) on {Name}'s authored origin " +
                                    "({Lat:F5},{Lon:F5}) - the unit was spread {D:F0} m from it; the route starts at its live position.",
                                    task.TaskName, skip, unit.Name, authored.Lat, authored.Lon,
                                    TerrainVertexAuthoring.DistMeters(origin.LatDeg, origin.LonDeg, authored.Lat, authored.Lon));
        }
        foreach (var p in taskPoints.Skip(skip))
            routeGeo.Add(new Geodetic
            {
                LatDeg = p.Lat,
                LonDeg = p.Lon,
                AltMeters = isGround ? groundWpAlt : (p.Elev ?? 0.0)
            });

        // ROUTE EXTENT AND PLAUSIBILITY (STP-833; Vrf:RouteExtentCheck, DEFAULT ON;
        // RouteExtentPolicy.cs). THE EARLIEST POINT AT WHICH THE WHOLE GEOMETRY EXISTS: the
        // taskee's live position is routeGeo[0], the task's own Location/objective has already
        // been read through TaskGeometryResolver + TaskGeometryInterpretation into taskPoints, and
        // the origin-vertex drop has been applied - and NOTHING has been sent to the back end yet.
        // Deliberately BEFORE the route shift and the terrain-profile request: a route to another
        // continent must not cost a tile search or a terrain round-trip, and it must never reach
        // `RequestTerrainProfile`, which on V6f answered for the Swedish vertices and made the
        // authoring log read "all 3 vertices authored from terrain".
        //
        // WHAT IT COSTS A HEALTHY TASK: two haversines per vertex, no allocation beyond the
        // coordinate copy, no I/O. Successful tasks are byte-identical - the check returns Ok and
        // the next line is the one that ran before it.
        //
        // A VIOLATION IS MALFORMED IN Q4'S SENSE (user ruling 2026-09-14): the order is at fault,
        // so the interface refuses LOUDLY rather than inventing a substitute geometry - ERROR
        // naming the vertex, the distance and the bound; TASKABRT through the single emit point;
        // one ObservationReport carrying the same sentence; and NotifyAbandoned so the successors
        // fail fast instead of waiting out the chain backstop, exactly as Q4's refusal does.
        if (_vrf.RouteExtentCheck)
        {
            var extentVertices = new List<(double Lat, double Lon)>(routeGeo.Count);
            foreach (var g in routeGeo) extentVertices.Add((g.LatDeg, g.LonDeg));
            var extent = RouteExtentPolicy.KnownExtent();
            if (extent == null && !_routeExtentNoteLogged)
            {
                _routeExtentNoteLogged = true;
                _log.LogInformation("{Note}", RouteExtentPolicy.ExtentUnavailableNote);
            }
            // The setting is passed THROUGH to the policy (it is also the `if` above, which is what
            // makes a disabled check cost nothing): the OFF arm of --routeextent-selftest then
            // exercises the same call this line makes, not a separate imitation of it.
            var extentVerdict = RouteExtentPolicy.Check(_vrf.RouteExtentCheck, origin.LatDeg, origin.LonDeg,
                                                        extentVertices, _vrf.MaxVertexFromTaskeeKm,
                                                        _vrf.MaxRouteLegKm, extent);
            if (extentVerdict.Violated)
            {
                _log.LogError("Task '{Task}' is MALFORMED and will NOT be executed: {Why}",
                              task.TaskName, RouteExtentPolicy.RefusalMarking(unit.Name, task.TaskName, extentVerdict));
                _sequencer.NotifyAbandoned(task.TaskUuid);
                PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"{RouteExtentPolicy.MalformedGeometryRefusal} - task '{task.TaskName}': " +
                               $"{extentVerdict.Reason} - refused, not dispatched");
                _ = PushReportAsync(Preflight.PreflightReports.BuildRouteExtentRefusalReport(
                                        task.TaskeeUuid, unit.Name, task.TaskName, extentVerdict,
                                        IsoNow(), NewReportId()),
                                    ReportKind.Observation);
                return;
            }
        }

        // THE LATERAL ROUTE SHIFT (Vrf:PreflightRouteShift, DEFAULT ON since the user ruling of
        // 2026-09-20 - STP-804/806;
        // docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md). The route geometry is final here -
        // the live start and the origin-vertex drop have both been applied - and the altitudes are
        // NOT yet authored, which is exactly the moment to insert waypoints: the terrain profile
        // below then authors the inserted vertices along with every other one.
        //
        // OFF THE TICK THREAD, LIKE EVERY OTHER PRE-FLIGHT READ: a cold leg fetches terrain tiles
        // over HTTP and the search scores several candidate polylines, so this pass COPIES the
        // vertices, hands them to a worker and RETURNS with nothing marked. The worker re-enters
        // through _tickActions + DeferredDispatch.Run, the same shape the terrain-profile
        // continuation uses, so a throw there ends the task properly instead of parking it (D1).
        //
        // IT NEVER REFUSES A TASK. Every way out of the worker - a shift, no shift, a throw, the
        // timeout sweep - dispatches this task; the only question is which line it drives.
        //
        // NOT WHERE THE ROUTE IS ABOUT TO BE COLLAPSED. Both aggregate branches below
        // (MoveIntoFormation and the R11 PlanAndMove probe) throw the intermediate vertices away
        // and drive to routeGeo[^1]. Shifting there would cost the search, change nothing the
        // unit drives, and REPORT a detour to the C2 side that never happened - the one thing
        // this feature must never do.
        bool routeWillBeCollapsed = unit.IsAggregate
            && (!string.IsNullOrEmpty(_vrf.MoveIntoFormation) || _vrf.AggregatePlanAndMove);
        if (shiftedRoute != null)
            routeGeo = shiftedRoute;
        else if (_vrf.PreflightRouteShift && isGround && terrainRoute == null && routeGeo.Count > 1
                 && !routeWillBeCollapsed && RouteShiftCanScore())
        {
            QueueRouteShift(task, unit, routeGeo);
            return;
        }
        else if (_vrf.PreflightRouteShift && routeWillBeCollapsed)
            _log.LogInformation("Task '{Task}' ({Unit}): ROUTE SHIFT skipped - this aggregate dispatch drives to the " +
                                "route's FINAL point only, so there is no line between vertices to shift.",
                                task.TaskName, unit.Name);

        // GroundWaypointAltitudeMode="TerrainProfile" (docs/DESIGN_TERRAIN_PROFILE_VERTICES_
        // 2026-09-01.md sec 3.3): ask the back end for the terrain height under each ground
        // vertex and RETURN; the reply (or the timeout) re-enters this method with the authored
        // route in terrainRoute. Nothing has been marked dispatched yet, so the re-entry does the
        // bookkeeping exactly once. Live/Fixed100 and non-ground units never take this branch.
        if (terrainRoute != null)
            routeGeo = terrainRoute;
        else if (isGround && IsTerrainProfileMode())
        {
            var liveVertices = routeGeo;
            double entityAlt = origin.AltMeters;
            uint requestId = _bridge.RequestTerrainProfile(liveVertices);
            if (requestId == 0)
                _log.LogWarning("Task '{Task}': terrain profile request not sent - falling back to Live vertices.",
                                task.TaskName);
            else
            {
                var deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.TerrainProfileTimeoutSeconds));
                _pendingTerrain[requestId] = new PendingTerrain(deadline, task.TaskName, samples =>
                {
                    var r = TerrainVertexAuthoring.Apply(liveVertices, samples, _vrf.TerrainClearanceMeters, entityAlt);
                    if (r.Mode == TerrainVertexAuthoring.Mode.Terrain)
                        _log.LogInformation("Terrain profile {Id} for task '{Task}': all {N} vertices authored from " +
                                            "terrain + {Clr} m clearance; alts [{Alts}].", requestId, task.TaskName,
                                            r.Vertices.Count, _vrf.TerrainClearanceMeters,
                                            string.Join(", ", r.Vertices.Select(v => v.AltMeters.ToString("F1"))));
                    else
                        _log.LogWarning("Terrain profile {Id} for task '{Task}': {Mode} - {Reason}; {Kept} vertex(es) " +
                                        "keep the Live altitude.", requestId, task.TaskName, r.Mode, r.Reason, r.KeptLive.Count);
                    if (r.Note != null)
                        _log.LogInformation("Terrain profile {Id} for task '{Task}': {Note}.", requestId, task.TaskName, r.Note);
                    // GROUND CONTACT AT DISPATCH (2026-09-21; since 2026-09-25 measure-and-log only,
                    // RL-20260921-06). This reply carries the terrain height under vertex 0 AND the
                    // taskee's live altitude, the measurement run 20260921T114910Z made (`app:1135`,
                    // `app:1939`). It is logged and recorded; it no longer holds or refuses the task.
                    LogGroundContactAtDispatch(task, unit, liveVertices, entityAlt, samples);
                    ExecuteTaskOnTick(task, unit, r.Vertices);
                },
                // D1: THIS continuation is the dispatch (nothing above it has been marked), so it
                // carries the task's identity and RunTerrainContinuation can end the task properly
                // when it throws instead of leaving it silent to STP and its successors parked.
                TaskUuid: task.TaskUuid, TaskeeUuid: task.TaskeeUuid);
                _log.LogInformation("Task '{Task}': terrain profile request {Id} sent for {N} vertices; dispatch " +
                                    "deferred to the reply (timeout {T} s -> Live fallback).",
                                    task.TaskName, requestId, liveVertices.Count, _vrf.TerrainProfileTimeoutSeconds);
                return;
            }
        }

        // ROUTE PRE-FLIGHT (Vrf:PreflightWarnings, DEFAULT OFF - DEMO_READINESS row 20). The route
        // is final here: the live start, the origin-vertex drop and any terrain authoring have all
        // been applied, so this scores exactly what is about to be driven. It is a WARNING channel
        // and nothing else - it never refuses, delays or alters the task, and the dispatch below
        // runs whatever it finds.
        //
        // OFF THE TICK THREAD, ALWAYS. A cold leg fetches terrain tiles over HTTP; doing that here
        // would stall the simulation for as long as the network takes. The vertices are COPIED and
        // handed to a worker, which scores them and pushes its reports through the same
        // PushReportAsync every other report uses.
        //
        // GROUND ONLY. The whole metric is a tracked vehicle's max-slope derated by the soil it is
        // driving on; an air platform does not drive over the ridge it crosses, so scoring its route
        // would manufacture warnings about ground it never touches.
        // NAV-AREA PRECONDITION FOR A GROUND MOVE (Vrf:RequireNavAreaForGroundTasks, DEFAULT
        // OFF - the default is the user's to set). *** NOT A CRASH GUARD. *** It was built on
        // V6d's reading - a ground move with no nav area STOPS the back end - and V6e stopped the
        // back end identically WITH a nav area, so that cause statement is WITHDRAWN
        // (V6_LIVE_JOIN_GATE sec 11; the stopped state is a runaway allocation, not a nav
        // failure). What survives: without a navigation area a ground move is planned by the
        // FEATURE planner on one straight part, silently - a fidelity precondition an operator
        // may choose to refuse on. WHAT THE EVIDENCE IS AND IS NOT: see NavAreaEvidence - it
        // reads the object console's own rows, it can see nothing below level 3 (there it warns
        // once and DISPATCHES rather than refusing on ignorance), and a row from another object
        // proves an area is loaded, not that this taskee's start point is inside it.
        if (_vrf.RequireNavAreaForGroundTasks && isGround && routeGeo.Count > 0)
        {
            double navWall = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
            var navUuids = new List<string> { vrfUuid };
            if (unit.IsAggregate)
            {
                // The MEMBERS are what drive, and it is their consoles that print the row.
                try
                {
                    var navMembers = _bridge.GetAggregateMembers(vrfUuid);
                    if (navMembers != null)
                        foreach (var m in navMembers)
                            if (!string.IsNullOrEmpty(m.Uuid)) navUuids.Add(m.Uuid);
                }
                catch (Exception ex) { _log.LogDebug(ex, "NAV GATE: member list unreadable for {Name}.", unit.Name); }
            }
            var navVerdict = _navEvidence.Decide(navUuids, navWall, _vrf.NavAreaEvidenceSeconds);
            if (NavAreaEvidence.ShouldRefuse(navVerdict))
            {
                string navWhy = NavAreaEvidence.RefusalMarking(unit.Name, _vrf.NavAreaEvidenceSeconds, navVerdict);
                _log.LogError("Task '{Task}': {Why}", task.TaskName, navWhy);
                _sequencer.NotifyAbandoned(task.TaskUuid);
                PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"no navigation area evidence for {unit.Name}");
                _ = PushReportAsync(BackendLivenessPolicy.BuildStateChangeReport(navWhy, IsoNow(), NewReportId()),
                                    ReportKind.Observation);
                return;
            }
            if (!navVerdict.CanSee)
            {
                if (!_navGateBlindWarned)
                {
                    _navGateBlindWarned = true;
                    _log.LogWarning("{Why}", NavAreaEvidence.BlindWarning(_vrf.ObjectConsoleNotifyLevel));
                }
            }
            else
                _log.LogInformation("Task '{Task}': nav-area evidence OK for {Name} - area '{Area}', from {Whose}.",
                                    task.TaskName, unit.Name, navVerdict.Area,
                                    navVerdict.TaskeeEvidence
                                        ? "this unit's own console row"
                                        : "ANOTHER object's row (an area is loaded; this is not proof that "
                                          + "this unit's start point is inside it)");
        }

        if (_vrf.PreflightWarnings && isGround && routeGeo.Count > 1)
            QueuePreflight(task, unit, routeGeo);

        // Rules of engagement (:2374-2379): ROEFree -> FireAtWill, ROEHold -> HoldFire,
        // everything else (incl. ROETight) -> FireWhenFiredUpon.
        Roe roe = task.RuleOfEngagementCode == "ROEFree" ? Roe.FireAtWill
                : task.RuleOfEngagementCode == "ROEHold" ? Roe.HoldFire
                : Roe.FireWhenFiredUpon;
        _bridge.SetRulesOfEngagement(vrfUuid, roe);

        // SetTarget - PARITY of the known bug (PORT.md sec 6, C2SIMinterface.cpp:2385):
        // the C++ passes the C2SIM taskee uuid where VRF expects a VRF uuid, plus the
        // affected entity's C2SIM uuid, so it is a silent no-op in VRF. Reproduced here;
        // the fix (distinct C2SimUuid/VrfUuid types) is a later Phase 4 item.
        _bridge.SetTarget(task.TaskeeUuid, task.AffectedEntity);

        // LAYER 2 - Unit 4 (docs/SEMANTIC_MAPPING.md): the PROPER aggregate maneuver. For an
        // AGGREGATE, when Vrf:MoveIntoFormation is set, issue DtMoveIntoFormationTask to the
        // route's FINAL point in the named formation INSTEAD of moveAlongRoute + SetAggregateFormation
        // - the real fix for the stuck-aggregate finding (most COA-STP1 aggregates stayed stuck with
        // Wedge alone; PORT.md sec 10). Aggregate-only + opt-in, so entity moves are unchanged (golden
        // parity). This collapses intermediate waypoints to the destination (the diagnostic "does the
        // set move in formation" path); it takes precedence over the Wedge enrichment for aggregates.
        if (unit.IsAggregate && !string.IsNullOrEmpty(_vrf.MoveIntoFormation))
        {
            var dest = routeGeo[^1];
            double headingDeg = BearingDeg(routeGeo[0], dest);
            // The route is COLLAPSED here (this dispatch drives to the final point only), so the
            // journey recorded for STP-837 is the line actually driven, not the authored polyline.
            MarkDispatched(task, unit, "move-into-formation", dest, new List<Geodetic> { routeGeo[0], dest });
            _bridge.MoveIntoFormation(vrfUuid, dest, headingDeg, _vrf.MoveIntoFormation);
            _arrivalReported.TryRemove(unit.Name, out _);
            ClearStallState(unit.Name);
            _log.LogInformation("Task '{Task}': MoveIntoFormation for AGGREGATE {Name} ({Vrf}) -> " +
                                "{Lat}/{Lon} formation '{Form}' hdg {Hdg:F0}deg (Unit 4; {N} route pts -> destination).",
                                task.TaskName, unit.Name, vrfUuid, dest.LatDeg, dest.LonDeg,
                                _vrf.MoveIntoFormation, headingDeg, routeGeo.Count);
            // Preserve ATTACK/BREACH semantics on this early return - but COMPLETION-GATED
            // (P0.3): issuing the engage now would REPLACE the formation move just issued.
            if (attackTargetVrf != null)
                DeferEngageUntilMoveCompletes(unit, task, "fire", vrfUuid, attackTargetVrf);
            if (breachTargetVrf != null)
                DeferEngageUntilMoveCompletes(unit, task, "breach", vrfUuid, breachTargetVrf);
            return;
        }

        // R11 PROBE (opt-in via Vrf:AggregatePlanAndMove; docs/UNIT_MOVEMENT_RESEARCH.md
        // sec 4c): for an AGGREGATE, create a waypoint at the route's FINAL point and issue
        // the PLANNED pathfinding move (DtPlanAndMoveToTask) to it INSTEAD of CreateRoute +
        // MoveAlongRoute - does the planner produce a path where the move-along leader plan
        // is EMPTY (the R9 Mojave finding)? Waypoint creation is async like routes: the
        // task is deferred to the waypoint's ObjectCreated.
        if (unit.IsAggregate && _vrf.AggregatePlanAndMove)
        {
            string wptName = task.TaskName + " WPT";
            _names.Requested(wptName);   // B3: the waypoint's ObjectCreated is matched by this name
            var wptQueue = _pendingRouteTasks.GetOrAdd(wptName, _ => new ConcurrentQueue<PendingRouteTask>());
            wptQueue.Enqueue(new PendingRouteTask(vrfUuid, Patrol: false, PlanMove: true));
            _pendingRouteUnit[wptName] = unit.Name;   // the arrival swallow clears when the VRF task is issued (route-created)
            // Collapsed like the formation move above: PlanAndMoveTo drives to the final point.
            MarkDispatched(task, unit, "plan-move", routeGeo[^1],
                           new List<Geodetic> { routeGeo[0], routeGeo[^1] });
            if (attackTargetVrf != null)
                DeferEngageUntilMoveCompletes(unit, task, "fire", vrfUuid, attackTargetVrf);
            if (breachTargetVrf != null)
                DeferEngageUntilMoveCompletes(unit, task, "breach", vrfUuid, breachTargetVrf);
            _bridge.CreateWaypoint(routeGeo[^1], wptName);
            _log.LogInformation("Task '{Task}': R11 CreateWaypoint '{Wpt}' for AGGREGATE {Name}; " +
                                "PlanAndMoveTo deferred to waypoint-created ({N} route pts -> final point).",
                                task.TaskName, wptName, unit.Name, routeGeo.Count);
            return;
        }

        // ENRICHMENT (opt-in via Vrf:AggregateFormation; "" = off = golden parity, PORT.md
        // sec 10): a disaggregated aggregate freezes on moveAlongRoute because its default
        // formation is unresolvable ("column-left"). Setting a VALID formation before the
        // move unblocks it (no-op on non-aggregate entities). Set here, before CreateRoute,
        // so it applies during the route-creation round-trip ahead of the deferred move
        // (the C++ spike used SetAggregateFormation + DtSleep(.5) right before MoveAlongRoute).
        // "auto" = E1 (guidance sec 4): resolve the name PER CREATED TYPE - formation names
        // are per-unit-type and CASE-INCONSISTENT, so one global name can never fit all.
        if (!string.IsNullOrEmpty(_vrf.AggregateFormation))
        {
            string formation = _vrf.AggregateFormation;
            if (formation.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                // R1: with auto, the formation was already SET (+ the unit REORGANIZED)
                // at creation via the query-driven reply - re-snapping here would teleport
                // members mid-run. At move time only RE-QUERY as a diagnostic: the reply
                // logs whether the create-time set actually TOOK (current='...').
                formation = null;
                if (unit.IsAggregate)
                    _bridge.RequestAvailableFormations(vrfUuid);
            }
            if (formation != null)
            {
                _bridge.SetAggregateFormation(vrfUuid, formation);
                _log.LogInformation("Set aggregate formation '{Form}' on {Name} ({Vrf}) before move.",
                                    formation, unit.Name, vrfUuid);
            }
        }

        // Single point -> MoveToLocation; otherwise CreateRoute then move along it (:2393).
        if (routeGeo.Count == 1)
        {
            MarkDispatched(task, unit, "move-to", routeGeo[^1], routeGeo);
            _bridge.MoveToLocation(vrfUuid, routeGeo[^1]);
            _arrivalReported.TryRemove(unit.Name, out _);
            ClearStallState(unit.Name);
            _log.LogInformation("Task '{Task}': MoveToLocation for {Name} ({Vrf}).",
                                task.TaskName, unit.Name, vrfUuid);
            // Layer 2 + P0.3: engage/breach AFTER the move COMPLETES (same-tick issue would
            // replace the move - VRF runs one task at a time).
            if (attackTargetVrf != null)
                DeferEngageUntilMoveCompletes(unit, task, "fire", vrfUuid, attackTargetVrf);
            if (breachTargetVrf != null)
                DeferEngageUntilMoveCompletes(unit, task, "breach", vrfUuid, breachTargetVrf);
            return;
        }

        // CreateRoute is async; defer the along-route task until the route's ObjectCreated fires
        // (parity: the C++ waits for the route to register before moveAlongRoute, :2408-2421).
        string routeName = task.TaskName + " ROUTE";
        _names.Requested(routeName);   // B3: the route's ObjectCreated is matched by this name
        // Layer 2: RECONNOITER (SCREEN/SCOUT) PATROLS the route (back and forth) instead of
        // moving along it once - defer PatrolRoute; every other verb defers MoveAlongRoute.
        bool patrol = verb.Intent == TaskIntent.Reconnoiter;
        var routeQueue = _pendingRouteTasks.GetOrAdd(routeName, _ => new ConcurrentQueue<PendingRouteTask>());
        if (!routeQueue.IsEmpty)
            _log.LogWarning("Route name '{Route}' already has {N} pending task(s) - duplicate TaskName in " +
                            "the order; same-named routes are matched FIFO as they are created.",
                            routeName, routeQueue.Count);
        // R10 SUBORDINATE FAN-OUT (opt-in via Vrf:SubordinateFanOut; UNIT_MOVEMENT_RESEARCH.md
        // sec 4c): task the aggregate's member ENTITIES directly instead of the unit - the
        // unlock for regions where the unit leader-path plan comes back EMPTY (R9 Mojave)
        // while entity moves work. Members are read from the aggregate's published state;
        // 0 members -> loud log + normal aggregate move. Completion: the unit's TASKCMPLT
        // is synthesized when ALL fanned members complete (FanOutTracker).
        IReadOnlyList<AggregateMember>? fanOutMembers = null;
        if (_vrf.SubordinateFanOut && unit.IsAggregate && !patrol)
        {
            var members = _bridge.GetAggregateMembers(vrfUuid);
            if (members is { Count: > 0 })
            {
                fanOutMembers = members;
                _log.LogInformation("Task '{Task}': R10 fan-out - {N} member entities of {Name} will be " +
                                    "tasked directly: {Members}.", task.TaskName, members.Count, unit.Name,
                                    string.Join(", ", members.Select(m => m.Name)));
            }
            else
                _log.LogWarning("Task '{Task}': R10 fan-out requested but {Name} ({Vrf}) publishes NO " +
                                "member entities - falling back to the aggregate-level move.",
                                task.TaskName, unit.Name, vrfUuid);
        }
        routeQueue.Enqueue(new PendingRouteTask(vrfUuid, patrol, FanOutMembers: fanOutMembers));
        _pendingRouteUnit[routeName] = unit.Name;   // the arrival swallow clears when the VRF task is issued (route-created)
        // The unit is committed to this move now (the route-created callback issues the
        // along-route task); record it so the completion attributes here (P0.1) and any
        // engage below gates on it (P0.3).
        MarkDispatched(task, unit, patrol ? "patrol" : "move-along", patrol ? (Geodetic?)null : routeGeo[^1],
                       patrol ? null : routeGeo);
        if (fanOutMembers != null)
        {
            _fanOut.Register(unit.Name, task.TaskUuid, fanOutMembers.Select(m => m.Name),
                             _vrf.FanOutCompletionFraction);
            // R10 robustness: a detached HARD-CAP straggler timer (measured from Register, not
            // idle). If a member never completes, it synthesizes the unit completion with a
            // warning after FanOutStragglerSeconds. The captured task uuid is the supersession
            // guard inside the tracker (a later retask under the same unit name must not be
            // synthesized by THIS fan-out's timer). 0 = OFF.
            if (_vrf.FanOutStragglerSeconds > 0)
                _ = FanOutStragglerAsync(unit.Name, task.TaskUuid);
        }
        // Layer 2 + P0.3: the ATTACK-family fire / BREACH is issued when the along-route
        // move COMPLETES (advance the axis / approach the obstacle, THEN engage/breach) -
        // no longer in the same tick as MoveAlongRoute, which would have replaced it.
        if (attackTargetVrf != null)
            DeferEngageUntilMoveCompletes(unit, task, "fire", vrfUuid, attackTargetVrf);
        if (breachTargetVrf != null)
            DeferEngageUntilMoveCompletes(unit, task, "breach", vrfUuid, breachTargetVrf);
        _bridge.CreateRoute(routeGeo, routeName);
        _log.LogInformation("Task '{Task}': CreateRoute '{Route}' ({Count} pts) for {Name}; {Action} deferred to route-created.",
                            task.TaskName, routeName, routeGeo.Count, unit.Name, patrol ? "patrol" : "move");
    }

    /// <summary>
    /// P0.1: record a task as the unit's in-flight task at the moment a VRF task command is
    /// actually issued. Logs + handles supersession (VRF runs one task at a time - a retask
    /// REPLACES the in-flight task; the superseded task's completion will never arrive, so
    /// its pending engage is cancelled and its successors are left to their gate policy).
    /// Also tells the sequencer the task dispatched (P0.2: successors' completion clock
    /// starts here, not at order arrival).
    /// </summary>
    /// <summary>
    /// R3 (user ruling 2026-09-14: "the target IS the objective"). Resolve a task's AffectedEntity
    /// to a VR-Forces uuid that can be NAMED as a target, and return null when there is none - in
    /// which case the caller routes the task to its own geometry, which is the objective.
    ///
    /// What this replaced: three copies of a guard that treated AffectedEntity == PerformingEntity
    /// as an error or as "no target" (ATTACK "self-target fire-support?; no fire, advancing only";
    /// BREACH and ESCRT "not resolvable to a distinct VRF unit", both at WARNING). STP names the
    /// taskee as the affected entity in EVERY task it exports (C2SimXmlBuilder.cs:427-429), so on
    /// COA-STP1 that fired on all 42 tasks and read as 42 degradations of a working order. It is
    /// not a degradation: doctrinally the objective is what the task is about and enemies may
    /// happen to be inside it, and VR-Forces' own tactical tasks take the objective GRAPHIC as
    /// their parameter (company_seize, co_clear, company_breach, plt_attack_by_fire,
    /// unit-attack-to-objective) - never a named enemy entity.
    /// The decision table is TaskDispatchPolicy.ForTarget; this method only logs and returns.
    /// </summary>
    private string ResolveAffectedTarget(OrderTask task, string vrfUuid, string intentLabel, string engagement)
    {
        string tgt = null;
        bool has = !string.IsNullOrEmpty(task.AffectedEntity);
        bool resolved = has && TryResolveVrfUuid(task.AffectedEntity, out tgt);
        bool isSelf = resolved && string.Equals(tgt, vrfUuid, StringComparison.Ordinal);
        var resolution = TaskDispatchPolicy.ForTarget(has, resolved, isSelf);
        if (resolution == TargetResolution.DistinctEntity) return tgt;

        // m3 (cold-start review of 5c67d41): R3 IS ABOUT SELF-TARGETING, and only SelfIsObjective
        // gets R3's wording and R3's Information level. An AffectedEntity this interface did not
        // create is a SCOPE OR DATA GAP - an out-of-scope OPFOR entity, or an init/order mismatch -
        // and saying "the target IS the objective" about it asserts the doctrinal ruling for a case
        // the ruling never covered. The OUTCOME is the same either way (route to the objective,
        // never refuse: TaskDispatchPolicy.FallsBackToGeometry), because there is nothing else the
        // interface could correctly do; what differs is what the operator is told and how loudly.
        if (resolution == TargetResolution.SelfIsObjective)
        {
            _log.LogInformation("{Intent} task '{Task}': the order names the PERFORMING UNIT as the affected " +
                                "entity (STP does this on every task), so THE TARGET IS THE OBJECTIVE (R3, user " +
                                "ruling 2026-09-14) - the task's geometry is what it is about and the unit is " +
                                "routed there. No {Engagement} against a named entity is issued.",
                                intentLabel, task.TaskName, engagement);
            return null;
        }
        if (resolution == TargetResolution.Unresolved)
            _log.LogWarning("{Intent} task '{Task}': the affected entity '{Entity}' is NOT an object this " +
                            "interface created - it is out of this clientId's scope, or the order and the " +
                            "initialization disagree. The task is dispatched to its own geometry and NO " +
                            "{Engagement} against a named entity is issued, so the engagement the order asked " +
                            "for does not happen.",
                            intentLabel, task.TaskName, task.AffectedEntity, engagement);
        else
            _log.LogWarning("{Intent} task '{Task}': the order names NO affected entity, so there is nothing to " +
                            "{Engagement} at. The task is dispatched to its own geometry.",
                            intentLabel, task.TaskName, engagement);
        return null;
    }

    /// <param name="route">STP-837: the vertex list the unit was actually given. For the two
    /// AGGREGATE kinds that COLLAPSE the route to its final point (MoveIntoFormation, the R11
    /// plan-move) the caller passes the two points that will really be driven, not the authored
    /// polyline - the traversal bar must measure the journey the unit was asked to make, not one
    /// the dispatch threw away. null for kinds with no journey (fire, breach, hold-in-place,
    /// follow, patrol), which the arrival monitor already skips for want of a destination.</param>
    private void MarkDispatched(OrderTask task, CreatedUnit unit, string kind, Geodetic? dest = null,
                                IReadOnlyList<Geodetic> route = null)
    {
        // NOTE (review wf_62e5bdf7): the arrival-evidence swallow flag is NOT cleared here - it is
        // cleared where the replacing VR-Forces command is actually ISSUED (the synchronous bridge
        // calls below, the route-created callback for deferred kinds, IssueEngage), because a
        // deferred kind's old task keeps running until then and its late completion must still
        // be swallowed.
        double routeLengthM = double.NaN;
        double? startLat = null, startLon = null;
        if (route is { Count: > 0 })
        {
            startLat = route[0].LatDeg;
            startLon = route[0].LonDeg;
            var routeLatLon = new List<(double Lat, double Lon)>(route.Count);
            foreach (var g in route) routeLatLon.Add((g.LatDeg, g.LonDeg));
            routeLengthM = RouteExtentPolicy.PathLengthMeters(routeLatLon);
        }
        // N7: the SIM clock's mark at dispatch, so every later "N s after dispatch" can be stated
        // on both clocks. _simClockLast is the tick thread's own 1 Hz observation, already taken;
        // reading the field costs nothing and calls no bridge.
        var simObsAtDispatch = _simClockLast;
        double simAtDispatch = simObsAtDispatch.ReadableConfirmed
                               && double.IsFinite(simObsAtDispatch.SimSeconds)
                               && simObsAtDispatch.SimSeconds >= 0.0
                                   ? simObsAtDispatch.SimSeconds : double.NaN;
        var dispatchedUtc = DateTime.UtcNow;
        var superseded = _inFlight.RecordDispatch(unit.Name,
            new InFlightTracker.InFlight(task.TaskUuid, task.TaskName, kind, dispatchedUtc,
                                         dest?.LatDeg, dest?.LonDeg, task.TaskeeUuid ?? "",
                                         routeLengthM, startLat, startLon, simAtDispatch));
        _log.LogInformation("DISPATCHED {Name} task '{Task}' ({Kind}) at WALL {Wall:yyyy-MM-ddTHH:mm:ss.fffZ}, " +
                            "SIMULATION clock {Sim}. Every elapsed figure this run prints for this task is " +
                            "labelled WALL or SIMULATION (N7); neither is the other.",
                            unit.Name, task.TaskName, kind, dispatchedUtc,
                            double.IsNaN(simAtDispatch)
                                ? "NOT READABLE (DtVrfRemoteController::simTime() reports no back end)"
                                : FormattableString.Invariant($"{simAtDispatch:F1} s"));
        if (superseded is InFlightTracker.InFlight old && old.TaskUuid != task.TaskUuid)
        {
            _log.LogWarning("Unit {Name}: task '{New}' SUPERSEDES in-flight task '{Old}' ({OldUuid}) - VRF " +
                            "replaces the running task; the old task will not complete.",
                            unit.Name, task.TaskName, old.TaskName, old.TaskUuid);
            // m1 (cold-start review of 5c67d41; supervisor Q1 default 2026-09-14). THE REPORT
            // STREAM MUST NOT CONTRADICT THE LOG. The line just above says the old task will not
            // complete - and its R4 end time was left armed, so it duly reported TASKCMPLT at its
            // authored end time and released ITS successors onto a unit doing something else.
            // Vrf:SupersededTaskCode decides which reading wins; the default is TASKABRT AT THE
            // SUPERSEDE POINT, because the taskee is demonstrably not performing it. PushTaskStatus
            // cancels the armed end time for any terminal code, so one call does both.
            string supersededCode = (_vrf.SupersededTaskCode ?? "TASKABRT").Trim();
            if (!TaskDispatchPolicy.SupersedeAbandonsSuccessors(supersededCode))
            {
                // 2026-09-25 (completion unit): under RL-20260921-09 a task WITH a destination whose
                // unit has not arrived is not completed by its end time - it waits for the arrival,
                // and a superseded task's unit will never arrive for it. So this non-default setting
                // marks the old task FINISHED here: it still reports TASKCMPLT at its end time, as
                // configured, or at once if that end time has already passed.
                var supersededVerdict = _timed.MarkFinished(old.TaskUuid);
                _log.LogInformation("Unit {Name}: task '{Old}' keeps its armed end time " +
                                    "(Vrf:SupersededTaskCode=TASKCMPLT) - it will report TASKCMPLT when the " +
                                    "order says it ends, even though VR-Forces is no longer running it, and its " +
                                    "STREND successors keep waiting for that completion{Now}.",
                                    unit.Name, old.TaskName,
                                    supersededVerdict == TimedCompletionPolicy.FinishVerdict.EmitNow
                                        ? " (its end time has already passed: reported now)" : "");
                if (supersededVerdict == TimedCompletionPolicy.FinishVerdict.EmitNow)
                {
                    _sequencer.CompleteTask(old.TaskUuid);
                    PushTaskStatus(old.TaskeeUuid, old.TaskUuid, S.TaskStatusCodeType.TASKCMPLT,
                                   $"SUPERSEDED by task '{task.TaskName}' on {unit.Name} after its end time " +
                                   "(Vrf:SupersededTaskCode=TASKCMPLT)");
                }
            }
            else
            {
                PushTaskStatus(old.TaskeeUuid, old.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               $"SUPERSEDED: task '{task.TaskName}' replaced it on {unit.Name}; VR-Forces runs " +
                               "one task at a time, so it is not being performed");
                // B1 (pass-2 review) + Q1 (USER RULING 2026-09-14). THE GATE MUST NOT CONTRADICT THE
                // REPORT WE JUST SENT. Without this the successors of a task the interface has just
                // declared NOT PERFORMED went on waiting out the full derived window - since M1 up
                // to 7,260 s, i.e. LONGER than before that fix - and were then skipped anyway. They
                // are exactly as dead as their predecessor, so they are abandoned at the supersede
                // point and each reports its own TASKABRT at once, like every other dead end.
                _sequencer.NotifyAbandoned(old.TaskUuid);
            }
            if (_pendingEngage.TryGetValue(unit.Name, out var eng) && eng.MoveTaskUuid == old.TaskUuid
                && _pendingEngage.TryRemove(new KeyValuePair<string, PendingEngage>(unit.Name, eng)))
                _log.LogWarning("Unit {Name}: cancelled the pending {Kind} tied to superseded task '{Old}'.",
                                unit.Name, eng.Kind, old.TaskName);
            // R10: a superseded task's fan-out must not complete against the new task.
            if (_fanOut.Cancel(unit.Name))
                _log.LogWarning("Unit {Name}: cancelled the member fan-out tied to superseded task '{Old}'.",
                                unit.Name, old.TaskName);
        }
        // C16 cold-start review finding 3: A NEW MOVE TASK GETS A NEW PROGRESS WINDOW. The
        // arrival-evidence swallow above is deferred on purpose - that is _arrivalReported, a
        // different map, and a deferred kind's OLD task keeps running until the replacing command
        // is issued - but the stall window has no such reason to survive a dispatch. 51d78a5
        // re-armed 60 s of grace here because its anchor was this record's own DispatchedUtc;
        // 1616614 moved the anchor to the watch's first sample and left nothing at all on the two
        // paths where a NEW in-flight record with a destination is written before ClearStallState
        // runs: :2049 (R11 plan-move) and :2146 (CreateRoute + MoveAlongRoute, the DEFAULT
        // aggregate move). There the previous task's ring could satisfy the window on the same
        // tick the new task was recorded, and the TASKABRT would be stamped with the NEW task uuid
        // while being computed entirely from the OLD task's samples - unbounded whenever the
        // route-created callback never arrives, which is exactly the silent-freeze mode this
        // watchdog exists for. Only the SAMPLES are dropped: the one-report-per-unit-task flag
        // still clears where the replacing VR-Forces command is actually issued (ClearStallState),
        // which is the conservative direction.
        if (dest is not null)
        {
            _stallSamples.TryRemove(unit.Name, out _);
            // STP-837: WHERE EVERY MEMBER STOOD WHEN THE TASK WAS DISPATCHED. Arrival evidence
            // now has to show that a member WENT somewhere, and "somewhere" is measured from
            // here. Read through the same TryReadMemberPositions C15 and C16 share, on the same
            // (tick) thread, so the baseline and the later samples can never come from different
            // views of the unit. A unit whose members are not readable yet gets a null map and
            // the record's own StartLat/StartLon - the taskee's dispatch position - stands in.
            _dispatchPositions[unit.Name] =
                new DispatchBaseline(task.TaskUuid ?? "",
                                     TryReadMemberPositions(unit.Name, out var atDispatch, out _)
                                         ? atDispatch : null);
        }
        _sequencer.NotifyDispatched(task.TaskUuid, TaskClockSeconds);
        // B1: the task has STARTED. This is the one point every dispatch path reaches (it is what
        // records the in-flight task), so it is where the C2SIM consumer is told - one TASKSTRT per
        // dispatch; a re-entered dispatch (the TerrainProfile second pass) is suppressed by the
        // policy, a genuine re-task announces again.
        PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKSTRT,
                       $"dispatched to {unit.Name} as '{kind}'");

        // ARM THE END TIME HERE, for the same reason TASKSTRT is pushed here - this is the one point
        // every dispatch path reaches. endTime = dispatch + Duration x Vrf:DurationScale (the
        // owner's temporary position, RL-20260921-09). Register is first-dispatch-wins, so the
        // TerrainProfile re-entry does not restart it. The DESTINATION flag is what decides whether
        // the task can be OVERDUE: kinds with no journey (fire, breach, hold-in-place, follow,
        // patrol) are dispatched with dest = null and end at their end time; a task WITH one that
        // has not arrived by then is completed when it arrives (TimedCompletionPolicy).
        if (_vrf.TimedCompletion)
        {
            double seconds = ScaleOrderMs(task.DurationMs) / 1000.0;
            // Q4 (USER RULING 2026-09-14): NOTHING IS INVENTED HERE. A task with no Duration AND no
            // geometry is MALFORMED and was already refused at dispatch (the zero-geometry block
            // above), so what reaches this line without a Duration is a task that HAS geometry -
            // a move - and therefore has arrival evidence to complete on. The supervisor default
            // that armed Vrf:DefaultHoldSeconds here is gone, knob and all.
            if (task.DurationMs <= 0)
                _log.LogWarning("Task '{Task}': the order gives NO Duration, so this task has no end time - " +
                                "it completes only on its own evidence (arrival, or a VR-Forces completion), " +
                                "and until it does its STREND successors wait at the gate.", task.TaskName);
            else if (seconds <= 0.0)
                _log.LogWarning("Task '{Task}': Vrf:DurationScale={Scale} collapses its {D:F0} s Duration to " +
                                "zero - NO end time is armed.",
                                task.TaskName, _durationScale, task.DurationMs / 1000.0);
            else if (_timed.Register(task.TaskUuid, task.TaskeeUuid, task.TaskName, unit.Name, seconds,
                                     hasDestination: dest is not null))
                _log.LogInformation("Task '{Task}': end time armed at {S:F0} s from dispatch " +
                                    "(C2SIM Duration {D:F0} s x Vrf:DurationScale {Scale}) - {Rule} " +
                                    "(temporary position on completion, RL-20260921-09).",
                                    task.TaskName, seconds, task.DurationMs / 1000.0, _durationScale,
                                    dest is not null
                                        ? "it has a destination: an earlier arrival is held to this end " +
                                          "time, and if the unit is still travelling then it completes " +
                                          "when it arrives"
                                        : "it has no destination: it completes at this end time");
        }
    }

    /// <summary>
    /// P0.3: park an ATTACK/BREACH engage until the unit's move task COMPLETES
    /// (OnVrfTaskCompleted issues it). A configurable fallback timer covers moves that
    /// never complete (Vrf:EngageFallbackSeconds; 0 disables the fallback).
    /// </summary>
    private void DeferEngageUntilMoveCompletes(CreatedUnit unit, OrderTask task, string kind,
                                               string taskeeVrf, string targetVrf)
    {
        var eng = new PendingEngage(kind, taskeeVrf, targetVrf, task.TaskUuid, task.TaskName);
        _pendingEngage[unit.Name] = eng;
        _log.LogInformation("Task '{Task}': {Kind} {Vrf} -> {Tgt} deferred until the move COMPLETES " +
                            "(completion-gated; fallback {S}s).",
                            task.TaskName, kind, taskeeVrf, targetVrf, _vrf.EngageFallbackSeconds);
        if (_vrf.EngageFallbackSeconds > 0)
            _ = EngageFallbackAsync(unit.Name, eng);
    }

    private async Task EngageFallbackAsync(string unitName, PendingEngage eng)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(_vrf.EngageFallbackSeconds), _stoppingToken); }
        catch (OperationCanceledException) { return; }
        // Remove-if-still-this-engage: if the completion (or a supersede) already consumed
        // it, this exact KeyValuePair no longer exists and TryRemove fails - no double fire.
        if (_pendingEngage.TryRemove(new KeyValuePair<string, PendingEngage>(unitName, eng)))
        {
            _log.LogWarning("Unit {Name}: move for task '{Task}' did not complete within {S}s; " +
                            "issuing the {Kind} via fallback (it will replace the still-running move).",
                            unitName, eng.TaskName, _vrf.EngageFallbackSeconds, eng.Kind);
            IssueEngage(unitName, eng);
        }
    }

    /// <summary>
    /// R10 fan-out straggler timeout (Vrf:FanOutStragglerSeconds). A detached hard-cap timer
    /// started at Register: if the quorum has not synthesized the unit completion within the
    /// window, synthesize it anyway WITH A WARNING so one stuck member cannot hold the unit
    /// task open. Idempotent + supersession-safe via the tracker (Synthesized flag + the
    /// captured task uuid); if all members completed first the fan-out is gone and this no-ops.
    /// The Task.Delay is gated on the service token; cancellation on shutdown is swallowed.
    /// </summary>
    private async Task FanOutStragglerAsync(string unitName, string capturedTaskUuid)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(_vrf.FanOutStragglerSeconds), _stoppingToken); }
        catch (OperationCanceledException) { return; }
        if (_fanOut.TrySynthesizeByTimeout(unitName, capturedTaskUuid, out int completed, out int total,
                                           out bool anyFailed))
        {
            _log.LogWarning("fan-out straggler timeout for {Unit}: {Completed}/{Total} members done - " +
                            "synthesizing unit completion{Failed}.", unitName, completed, total,
                            anyFailed ? " as a FAILURE (at least one member's task failed)" : "");
            // No VRF completion callback on the timer path -> no VRF task type to sanity-check
            // against the dispatched kind; pass empty (KindLooksRight treats empty as "can't
            // tell", so it does NOT emit a spurious attribution-anomaly warning here).
            // m8: the timer path used to synthesize success=true BY CONSTRUCTION. Members that never
            // reported are unknown - not failures - but a member that DID report a failure is
            // evidence the unit's task did not succeed, and it must reach the C2SIM code.
            SynthesizeUnitCompletion(unitName, "", !anyFailed);
        }
    }

    /// <summary>Issue a parked engage on the tick thread, re-recording it as the unit's
    /// in-flight task (same C2SIM task uuid, engage kind) so ITS completion attributes.</summary>
    private void IssueEngage(string unitName, PendingEngage eng)
    {
        _inFlight.RecordDispatch(unitName,
            new InFlightTracker.InFlight(eng.MoveTaskUuid, eng.TaskName, eng.Kind, DateTime.UtcNow));
        _tickActions.Enqueue(() =>
        {
            if (eng.Kind == "breach") _bridge.Breach(eng.TaskeeVrf, eng.TargetVrf);
            else _bridge.FireAtTarget(eng.TaskeeVrf, eng.TargetVrf);
            // The engage replaces the move in VR-Forces: the next completion is the ENGAGE's and must
            // attribute (review wf_62e5bdf7) - drop any arrival-evidence swallow for this unit.
            _arrivalReported.TryRemove(unitName, out _);
            ClearStallState(unitName);
        });
        _log.LogInformation("{Kind} {Vrf} -> {Tgt} issued (task '{Task}').",
                            eng.Kind == "breach" ? "BREACH: Breach" : "ATTACK: FireAtTarget",
                            eng.TaskeeVrf, eng.TargetVrf, eng.TaskName);
    }

    private void OnReport(object sender, C2SIMSDK.C2SIMNotificationEventParams e)
    {
        // The interface GENERATES reports; it does not consume them. Logged for tracing.
        _log.LogDebug("C2SIM Report received ({Len} bytes) - ignored (interface is a producer).",
                      e.Body?.Length ?? 0);
    }

    private void OnError(object sender, Exception e)
    {
        _log.LogError("C2SIM error: {Msg}. Restart recommended.",
                      C2SIMSDK.GetRootException(e).Message);
    }

    // ================= VR-Forces -> C2SIM (outbound) =================
    // These fire on the VRF tick thread. Correlation is cheap + inline; network
    // pushes go off-thread so they do not stall the tick.

    private void OnVrfObjectCreated(object sender, ObjectCreatedEventArgs e)
    {
        _lastObjectCreatedUtc = DateTime.UtcNow;   // keeps composition deadlines from firing mid-batch
        // parity: onVrfObjectCreated correlates the requested name to its VRF uuid.
        // B3: the callback's name may be the DIS-marking TRUNCATION of the name we asked for
        // ("2/1_AD/25_" for "2/1_AD/25_~PXY"). Resolve it ONCE here and use the resolved `name`
        // for the whole callback - every map below (_compositions, _childToParent, _recreatePending,
        // _reattachToParentVrfUuid, _pendingAltitude, _c2SimUuidByName, _pendingRouteTasks) is keyed
        // by the name we REQUESTED, so a truncated callback used to miss all of them silently.
        var bind = _names.Bind(e.Name, e.Uuid);
        string name = bind.Name;
        if (bind.Truncated)
            _log.LogWarning("VRF returned created object '{Returned}' for the name we requested, '{Requested}' " +
                            "({Uuid}) - a platform's name is its DIS MARKING and is truncated. Correlating it to " +
                            "the requested name; every lookup uses that name.", e.Name, name, e.Uuid);
        if (bind.Ambiguous)
            _log.LogError("VRF returned created object '{Returned}' ({Uuid}), which is the truncation of MORE THAN " +
                          "ONE name we requested - it cannot be attributed and is left under the returned name. " +
                          "The units sharing that prefix will not be found by name.", e.Name, e.Uuid);
        // B3 review finding 1: the returned name is EXACTLY one we asked for, so it is bound to that
        // unit - but it is ALSO the prefix of names we asked for and have not yet seen created, any
        // of which would come back as this same string if VR-Forces truncated its marking. The
        // binding stands (the exact match is the only defensible reading); this says out loud that
        // it is a reading, so a mis-bound unit is a log line and not nine hours of silence.
        // m1: the registry REFUSED to re-point an already-bound requested name at this object.
        // The prior binding stands; this object is unattributed. Loud, because the alternative
        // (the pre-fix behaviour) was a live unit silently answering for the wrong object.
        if (bind.RefusedRebind)
            _log.LogError("NAME REBIND REFUSED: VRF returned created object '{Returned}' ({Uuid}), but that name " +
                          "is ALREADY BOUND to {Prior} and no re-create was announced for it. KEEPING THE PRIOR " +
                          "BINDING; this object is left unattributed. If this is a unit you expected to see, its " +
                          "name collides with another object's marking (docs/PORT.md sec 6).",
                          e.Name, e.Uuid, bind.PriorUuid);
        if (bind.PrefixedCandidates is { Count: > 0 })
            _log.LogWarning("NAME COLLISION RISK: VRF returned created object '{Returned}' ({Uuid}), which is " +
                            "EXACTLY a name we requested and ALSO the prefix of {N} requested name(s) still " +
                            "awaiting creation [{Candidates}]. BOUND TO THE EXACT NAME. If any of those is " +
                            "created as a PLATFORM its marking truncates to this same string and would be " +
                            "attributed to the wrong unit - keep every requested name inside the " +
                            "{Width}-character marking width (docs/PORT.md sec 6).",
                            e.Name, e.Uuid, bind.PrefixedCandidates.Count,
                            string.Join(", ", bind.PrefixedCandidates), NameRegistry.MarkingTruncationWidth);
        if (!string.IsNullOrEmpty(name))
        {
            // ORDER-TIME MATERIALIZATION deferred to this shell's arrival (review fix): run it now, on the
            // tick thread - MaterializeUnit only enqueues bridge work and touches concurrent maps.
            if (_materializeOnCreated.TryRemove(name, out var deferredMat))
                MaterializeUnit(deferredMat.Uuid, deferredMat.Why + " (shell reflected)");
        }
        _log.LogDebug("VRF created {Name} -> {Uuid}", name, e.Uuid);

        // OBSERVATION CHANNEL (Vrf:ObjectConsoleNotifyLevel >= 0): open this object's console at
        // the requested level so its controllers' messages reach OnVrfObjectConsoleMessage
        // (UG52 21.9.1 p483; vrfRemoteController.h:1953). Tick thread - bridge call is safe.
        if (_vrf.ObjectConsoleNotifyLevel >= 0 && !string.IsNullOrEmpty(e.Uuid))
        {
            _bridge.SetObjectNotifyLevel(e.Uuid, _vrf.ObjectConsoleNotifyLevel);
            // STP-822 part 2: remember the level we ASKED FOR, per object. "Could we have seen a
            // nav-area row for this unit?" must be answered from what was actually opened, not
            // from the setting - the nav gate refuses only when it COULD have seen one.
            _navEvidence.ConsoleOpened(e.Uuid, _vrf.ObjectConsoleNotifyLevel);
            _log.LogInformation("VRF console level {Level} requested for {Name} ({Uuid}).",
                                _vrf.ObjectConsoleNotifyLevel, name, e.Uuid);
        }

        // COMPOSE-FROM-CHILDREN (Vrf:ComposeHierarchy): attach declared children under their parent
        // shell once both exist (vendor sample commandLineRemoteController.cxx:1520-1554). This
        // callback runs on the tick thread, so the AddToOrganization bridge call inside is safe.
        if (_vrf.ComposeHierarchy && !string.IsNullOrEmpty(name)
            && (_compositions.ContainsKey(name) || _childToParent.ContainsKey(name)))
            TryAdvanceComposition(name, e.Uuid);

        // ORDER-TIME MATERIALIZATION case 3 (C13): a shell that was deleted and re-created as its
        // TEMPLATE has arrived. Re-attach it under its superior shell (if it had one) and release the
        // task waiting on it. _names already carries the NEW uuid (bound at the top).
        if (!string.IsNullOrEmpty(name) && _recreatePending.TryRemove(name, out _))
        {
            string parentUuid = null;
            if (_reattachToParentVrfUuid.TryRemove(name, out var pu))
            {
                parentUuid = pu;
                // Restore the DECLARED subordinate order (review fix): addToOrganization appends, so
                // re-add EVERY declared child of that parent in declared order (each re-add detaches and
                // appends; the final order is the declared one). Children whose own re-create is still in
                // flight are skipped here and re-ordered again when they arrive.
                if (_names.TryGetName(pu, out var parentName)
                    && _declaredChildNamesByParent.TryGetValue(parentName, out var declaredNames))
                {
                    foreach (var childName in declaredNames)
                    {
                        if (_recreatePending.ContainsKey(childName)) continue;
                        if (_names.TryGetUuid(childName, out var childUuid))
                            _bridge.AddToOrganization(childUuid, pu);
                    }
                }
                else
                    _bridge.AddToOrganization(e.Uuid, pu);
            }
            // Release on REFLECTION (ReleaseReflected on the tick loop), not on this control message.
            _awaitReflection[name] = (e.Uuid, DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.CompositionTimeoutSeconds)));
            _log.LogInformation("MATERIALIZE {Name}: re-created as the template ({Uuid}){Re}; waiting for it to reflect.",
                                name, e.Uuid, parentUuid == null ? "" : " and re-attached under " + parentUuid);
        }

        // Apply any deferred SetAltitude now that we have the uuid. This callback
        // already runs on the tick thread, so the bridge call is safe here.
        if (!string.IsNullOrEmpty(name) && _pendingAltitude.TryRemove(name, out var alt))
            _bridge.SetAltitude(e.Uuid, alt);

        // R1 (docs/UNIT_MOVEMENT_RESEARCH.md): with Vrf:AggregateFormation=auto, repair a
        // created AGGREGATE's formation state AT CREATION - not at move time, which the
        // research showed is structurally too late. QUERY-DRIVEN (supersedes the static
        // per-type map): ask the unit which formation names IT actually accepts (R4);
        // the reply (OnVrfAvailableFormations) picks a valid name, SETS it (snapping
        // members into clean geometry at the spawn point) and REORGANIZES (establishes
        // the lead subordinate - auto-promote is off in VRF). Ground truth beats static
        // analysis: the first R5 run's read-backs showed ALL units here accept only
        // LOWERCASE names, contradicting the .entity files' Title-Case company lists.
        if (!string.IsNullOrEmpty(name)
            && _vrf.AggregateFormation.Equals("auto", StringComparison.OrdinalIgnoreCase)
            && _c2SimUuidByName.TryGetValue(name, out var createdC2SimUuid)
            && _unitByC2SimUuid.TryGetValue(createdC2SimUuid, out var createdUnit)
            && createdUnit.IsAggregate)
        {
            // (the uuid -> name binding is already in _names from the Bind at the top)
            _bridge.RequestAvailableFormations(e.Uuid);
            _log.LogInformation("R1: created aggregate {Name} ({Uuid}) - formation list " +
                                "queried; set+reorganize follow on the reply.", name, e.Uuid);
        }

        // If this created object is a route with tasks awaiting it, issue the FIRST pending
        // one now that the route is registered (parity: executeTask's wait-then-
        // moveAlongRoute, :2408-2421). FIFO per route name - see _pendingRouteTasks; the
        // QUEUE is still keyed by the route NAME (that is all CreateRoute gave us), but the
        // TASK is addressed by the route's REAL uuid, e.Uuid.
        // WHY (2026-09-02, docs/experiments/PREREG_ROUTE_UUID_FIX_2026-09-02.md): these
        // tasks carry the route as a DtUUID (moveAlongTasks.h setRoute(const DtUUID&),
        // patrolRouteTask.h, planAndMoveToTask.h setControlPoint). DtUUID's string ctor
        // (C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h:246-253) sets a VALID uuid only from
        // a "VRF_UUID:..." string; anything else falls back to a marking-text lookup held in
        // a 36-byte blob (rwUUID.h:412 char myData[36] = 1 type byte + 35 payload), so a name
        // longer than 34 characters arrived at the back end CUT TO 35 and the route reference
        // never resolved - the aggregate was tasked and then silently froze (probe run
        // 20260902T143638Z: route name 44 chars, 0 offset routes, 0.0 m in 900 samples).
        // e.Uuid IS the "VRF_UUID:..." form (the ObjectCreated callback carries the DtUUID -
        // vrfRemoteController.h:102-103 - and VrfFacade.cpp:211 forwards uuid.uuidString()),
        // i.e. the exact same path the taskee uuid already uses successfully. The C2SIM task
        // name stays in the log line and on the route OBJECT (CreateRoute's DtString is
        // unbounded); it is no longer what the task has to resolve.
        // NOTE (P0.3): the ATTACK/BREACH engage is NO LONGER issued here - it now waits for
        // the move to COMPLETE (OnVrfTaskCompleted), since a same-tick engage would replace
        // the move (NEXT_SESSION_GUIDANCE.md sec 2.5).
        if (!string.IsNullOrEmpty(name) && _pendingRouteTasks.TryGetValue(name, out var routeQueue)
            && routeQueue.TryDequeue(out var pending))
        {
            // The replacing VR-Forces task is issued in this block (same tick action): from here on a
            // vendor completion for this unit belongs to the NEW task - drop the arrival-evidence swallow.
            if (_pendingRouteUnit.TryRemove(name, out var issuedForUnit))
            {
                _arrivalReported.TryRemove(issuedForUnit, out _);
                ClearStallState(issuedForUnit);
            }
            if (pending.Patrol)
            {
                _bridge.PatrolRoute(pending.TaskeeVrfUuid, e.Uuid);
                _log.LogInformation("Route '{Route}' ({RouteUuid}) created; PatrolRoute issued for {Vrf} (Reconnoiter).",
                                    name, e.Uuid, pending.TaskeeVrfUuid);
            }
            else if (pending.PlanMove)
            {
                // R11: the created object is the destination WAYPOINT - issue the planned move.
                _bridge.PlanAndMoveTo(pending.TaskeeVrfUuid, e.Uuid);
                _log.LogInformation("Waypoint '{Wpt}' ({WptUuid}) created; PlanAndMoveTo issued for {Vrf} (R11).",
                                    name, e.Uuid, pending.TaskeeVrfUuid);
            }
            else if (pending.FanOutMembers is { Count: > 0 } members)
            {
                // R10: fan the along-route move out to the member entities (same route).
                foreach (var m in members)
                    _bridge.MoveAlongRoute(m.Uuid, e.Uuid);
                _log.LogInformation("Route '{Route}' ({RouteUuid}) created; R10 fan-out MoveAlongRoute issued to " +
                                    "{N} members of {Vrf}.", name, e.Uuid, members.Count, pending.TaskeeVrfUuid);
            }
            else
            {
                _bridge.MoveAlongRoute(pending.TaskeeVrfUuid, e.Uuid);
                _log.LogInformation("Route '{Route}' ({RouteUuid}) created; MoveAlongRoute issued for {Vrf}.",
                                    name, e.Uuid, pending.TaskeeVrfUuid);
            }
        }

        // DEFER, DO NOT ABORT (D5b): THIS is the event that can end a hold and settle the init
        // barrier, so re-evaluate both here rather than waiting up to a tick for the sweep. Guarded
        // like a tick phase - this callback is the tick thread and must outlive any one phase.
        TickPhase("SweepDispatchReadiness(ObjectCreated)",
                  !_dispatchWaits.IsEmpty || (!_initSettled && _initPlannedUtc != DateTime.MinValue),
                  SweepDispatchReadiness);
    }

    /// <summary>
    /// Resolve a C2SIM entity uuid to its VRF uuid via the init-created maps
    /// (_unitByC2SimUuid -> _names). This is the two-dict chain that dissolves the
    /// TASK_EXPANSION_PLAN "uuid-resolution blocker" (SEMANTIC_MAPPING.md sec 2b). Returns
    /// false if the entity was not created by our clientId at init (e.g. an out-of-scope
    /// OPFOR target) or has not yet been confirmed created by VR-Forces.
    /// </summary>
    private bool TryResolveVrfUuid(string c2SimUuid, out string vrfUuid)
    {
        vrfUuid = "";
        if (string.IsNullOrEmpty(c2SimUuid)) return false;
        if (!_unitByC2SimUuid.TryGetValue(c2SimUuid, out var u)) return false;
        return _names.TryGetUuid(u.Name, out vrfUuid);
    }

    // ARRIVAL-EVIDENCE COMPLETION (user ruling 2026-09-07; ArrivalPolicy.cs; VrfSettings.Arrival*).
    // Tick thread. For every in-flight task with a destination, read the unit's members (the
    // entity itself for a platform), count those within ArrivalRadiusMeters of the last vertex,
    // and when MORE THAN ArrivalMemberFraction of them are there, report TASKCMPLT through the
    // same path a vendor completion takes (SynthesizeUnitCompletion pops the in-flight record,
    // releases the successors, issues a deferred engage). The vendor's own completion for that
    // task, if it ever comes, is swallowed once (OnVrfTaskCompleted).
    // STP-833: the "rule (c) is skipped, and here is why" line is a CONFIGURATION fact, not a
    // per-task event - said once per run, like the nav gate's blind warning.
    private bool _routeExtentNoteLogged;
    // STP-837: said once per run - a move dispatched with no recorded start is a code defect,
    // not a per-task event.
    private bool _arrivalNoBaselineWarned;
    private readonly ConcurrentDictionary<string, string> _arrivalReported = new();   // unit name -> task uuid reported from evidence
    // STP-837: where each member stood at dispatch, per unit, under the task uuid that wrote it
    // (a stale baseline from a previous task must never be measured against the current one).
    private sealed record DispatchBaseline(string TaskUuid, Dictionary<string, (double Lat, double Lon)> ByUuid);
    private readonly ConcurrentDictionary<string, DispatchBaseline> _dispatchPositions = new();
    // unit name -> task uuid already named as un-closable by arrival evidence (one line per task).
    private readonly ConcurrentDictionary<string, string> _arrivalNotClosable = new();
    private readonly ConcurrentDictionary<string, string> _pendingRouteUnit = new();  // route/waypoint name -> unit name (swallow cleared when its VRF task is issued)
    private readonly ConcurrentDictionary<string, (double Lat, double Lon)> _authoredPosByName = new();  // unit name -> C2SIM authored position (origin-vertex drop)
    private readonly ConcurrentDictionary<string, string> _templateByName = new();    // unit name -> the VR-Forces template the type map landed (route pre-flight)
    private DateTime _nextArrivalCheck = DateTime.MinValue;

    // ============ ROUTE PRE-FLIGHT (Vrf:PreflightWarnings; DEMO_READINESS row 20) ============
    // Built on FIRST USE, never at start-up: with BOTH readers off nothing here reads a vendor
    // file, opens a socket or creates a directory. SINCE 2026-09-20 THAT IS NO LONGER THE SHIPPED
    // CASE - Vrf:PreflightRouteShift is ON by default, so the first GROUND move task of a run
    // builds this service, reads the vendor SMS and the MAK land-cover catalogues, and creates
    // the tile-cache directory. Vrf:PreflightWarnings still ships OFF, so the post-dispatch
    // scoring is still the operator's choice; this construction is now the route shift's.
    // volatile: the fast path reads this OUTSIDE the lock, and several task workers can race it.
    private volatile Preflight.PreflightService _preflight;
    private readonly object _preflightLock = new();
    private bool _preflightDisabled;          // one construction failure retires it for the run
    // E6: the once-per-order tile census (see Preflight.TileCensusLatch). The two counter marks
    // are the cumulative readings when the order's first scoring worker started; the census is the
    // DELTA. Written under the latch's own generation discipline, so no extra lock is needed here.
    private readonly Preflight.TileCensusLatch _tileCensus = new();
    private int _tileHitsAtOrder, _tileFetchesAtOrder;

    private Preflight.PreflightService GetPreflight()
    {
        if (_preflight != null) return _preflight;
        lock (_preflightLock)
        {
            if (_preflight != null || _preflightDisabled) return _preflight;
            try
            {
                string cache = ResolvePreflightCacheDir(_vrf.PreflightCacheDir);
                var opt = new Preflight.PreflightOptions
                {
                    CacheDir = cache,
                    SharedData = _vrf.PreflightSharedDataDir,
                    StepM = _vrf.PreflightStepMeters,
                    WindowM = _vrf.PreflightWindowMeters,
                    ShortWindowM = _vrf.PreflightShortWindowMeters,
                    Threshold = _vrf.PreflightThreshold,
                    DropOriginMeters = _vrf.DropOriginVertexMeters,
                    Offline = _vrf.PreflightOffline,
                    ElevationLevel = _vrf.PreflightElevationLevel,
                    ElevationMinLevel = _vrf.PreflightElevationMinLevel,
                    FriendlyNation = _nations.Friendly,
                    OpposingNation = _nations.Opposing,
                };
                if (!string.IsNullOrWhiteSpace(_vrf.VrfHome)) opt = opt with { VrfHome = _vrf.VrfHome };
                _preflight = new Preflight.PreflightService(opt);
                // The old wording here was "Warnings only - no task is ever refused or altered",
                // which stopped being true the moment the route shift shipped ON (2026-09-20):
                // the shift ALTERS the line a unit drives. Refusal is still never on the table.
                _log.LogInformation("ROUTE PRE-FLIGHT enabled: threshold {T:F2} on a {W:F0} m sustained window, " +
                                    "step {S:F0} m, tiles cached in {Cache}{Off}. Readers: warnings {Warn}, " +
                                    "LATERAL ROUTE SHIFT {Shift} (the shift CHANGES the line a unit drives; " +
                                    "neither reader ever refuses a task).",
                                    opt.Threshold, opt.WindowM, opt.StepM, cache,
                                    opt.Offline ? " (offline)" : "",
                                    _vrf.PreflightWarnings ? "ON" : "off",
                                    _vrf.PreflightRouteShift ? "ON" : "off");
                // WHICH ELEVATION LEVEL IS IN USE, said once and up front. The level is an AO
                // property, not a constant, and the threshold above was calibrated at L13 only -
                // a run that quotes a ratio without saying which DEM produced it is quoting an
                // un-anchored number. The actual level per area is probed lazily and appears
                // again on every verdict.
                // NOTE the placeholders are POSITIONAL here (Microsoft.Extensions.Logging binds
                // by order, not by name), so a repeated {L} would silently consume the next
                // argument and shift every value after it. Each name appears exactly once.
                _log.LogInformation("ROUTE PRE-FLIGHT elevation: dataset {Ds}, start level L{Level} " +
                                    "(Vrf:PreflightElevationLevel), falling back one level at a time to L{Min} " +
                                    "(Vrf:PreflightElevationMinLevel) wherever the server has NO DATA at the level " +
                                    "above. The Mojave AO is served at L13; the Suwalki AO at L12 only, so the " +
                                    "fallback is what stops every leg there reading 'NO VERDICT - tiles missing'. " +
                                    "A leg that resolves at NO level is reported as a WARNING, never as clear.",
                                    Preflight.TileMath.ElevationDataset, _preflight.Tiles.ElevationLevel,
                                    _preflight.Tiles.ElevationMinLevel);
                // The CALIBRATION REFERENCE, stated as what it is: the threshold's own anchor, at
                // the Mojave latitude and level it was measured on. A leg scored at a coarser
                // level prints its OWN note beside its verdict, so the two can be compared.
                _log.LogInformation("ROUTE PRE-FLIGHT calibration reference: {Note}",
                                    Preflight.TileMath.CalibrationNote(34.66, Preflight.TileMath.DefaultElevationLevel,
                                                                       opt.WindowM, opt.Threshold));
                foreach (var s in _preflight.Soil.Sources) _log.LogInformation("ROUTE PRE-FLIGHT vendor data: {Source}", s);
                if (!_preflight.Sms.Ok)
                    _log.LogWarning("ROUTE PRE-FLIGHT: vendor SMS not found at {Dir} - every unit falls back to " +
                                    "max-slope {Fallback:F2} and the warnings say so.",
                                    _preflight.Sms.Directory, Preflight.VendorSms.MaxSlopeFallbackMin);
            }
            catch (Exception e)
            {
                _preflightDisabled = true;
                _log.LogError("ROUTE PRE-FLIGHT disabled for this run - could not start: {Msg}", e.Message);
            }
            return _preflight;
        }
    }

    /// <summary>
    /// THE TWO FINDINGS EVERY READER OWES, whichever of them scored the route (STP-802).
    ///
    /// 1. NO ELEVATION AT ANY LEVEL. The old code pinned the level at 13, so an AO the server
    ///    serves one level coarser produced a NaN per sample, "NO VERDICT - tiles missing" per
    ///    leg, nothing flagged and - with the shift ON by default - no shift, silently. That is a
    ///    false green, so a leg whose cascade found NOTHING is a WARNING naming the leg and the
    ///    levels tried. It is never counted as clear ground.
    /// 2. WATER ON THE LINE. Deep water is acceleration-factor 0.000 in the vendor's own soil
    ///    table: a dead stop reported as TaskRunning for ever. A WARNING per leg, and the caller
    ///    pushes the matching ObservationReports - this method does the log half only, because
    ///    the two readers push on different schedules.
    ///
    /// Neither finding refuses or alters anything. Returns the number of legs with water so the
    /// caller can decide whether to build reports at all.
    /// </summary>
    /// <param name="defer">F1: when non-null the lines are BUFFERED instead of written, so a
    /// caller that has not yet claimed its dispatch cannot assert anything about the route. The
    /// post-dispatch warnings reader passes null and writes immediately - it runs AFTER the
    /// dispatch it describes, so it has nothing to earn.</param>
    private int ReportLegTerrainFindings(string taskName, string unitName,
                                         IReadOnlyList<Preflight.LegMetrics> legs,
                                         Preflight.PreflightService svc,
                                         DeferredLog defer = null)
    {
        int water = 0;
        if (legs == null) return 0;
        void Say(Action write) { if (defer == null) write(); else defer.Add(write); }
        // E6 (D7 harvest): WHICH ELEVATION LEVEL ACTUALLY SERVED THIS TASK. Until now the level
        // appeared only when it was COARSER than the L13 the threshold was calibrated at, so a run
        // scored at L13 and a run that resolved nothing at all were equally silent on the point,
        // and the D7 harvest had to record "the resolved elevation level is not logged". It is now
        // stated for every task, whatever it is, because a ratio without its DEM is un-anchored.
        Say(() =>
        _log.LogInformation("ROUTE PRE-FLIGHT task '{Task}' ({Unit}): ELEVATION LEVEL ACTUALLY USED - {Dist} over " +
                            "{N} leg(s) (cascade start L{From}, floor L{To}, dataset {Ds}; the threshold " +
                            "{Thr:F2} was calibrated at L{Cal}). L0 means NO level served that leg and NOTHING " +
                            "about it was checked (E6).",
                            taskName, unitName,
                            string.Join(", ", legs.GroupBy(l => l.ElevationLevel)
                                                  .OrderByDescending(g => g.Key)
                                                  .Select(g => $"L{g.Key} x{g.Count()}")),
                            legs.Count, svc.Tiles.ElevationLevel, svc.Tiles.ElevationMinLevel,
                            Preflight.TileMath.ElevationDataset, svc.Options.Threshold,
                            Preflight.TileMath.DefaultElevationLevel));
        foreach (var leg0 in legs)
        {
            var leg = leg0;   // captured per iteration - the closures may outlive the loop
            if (leg.ElevationFetchFailures > 0)
                Say(() =>
                _log.LogWarning("ROUTE PRE-FLIGHT task '{Task}' ({Unit}) leg {Leg}: NO VERDICT - THE ELEVATION " +
                                "SERVICE FAILED, it did not answer 'no data'. {N} of {Total} sample(s) on this leg " +
                                "could not be resolved because a tile FETCH failed (timeout, connection or 5xx) even " +
                                "after {Tries} attempt(s) per tile - a transient failure, NOT the server saying it " +
                                "has no tile there. The leg is therefore scored at NO LEVEL and carries NO VERDICT: " +
                                "it is NOT clear ground, no shift may be taken on it, and it is NOT quietly rescored " +
                                "one level coarser, because a coarser DEM would change the verdict for a reason that " +
                                "has nothing to do with the ground. Leg ({A:F5},{ALon:F5}) -> ({B:F5},{BLon:F5}). " +
                                "Fix the network or pre-warm the cache; DISREGARD this run's ratios for this leg.",
                                taskName, unitName, leg.Index, leg.ElevationFetchFailures, leg.Samples,
                                Preflight.TileSource.MaxFetchAttempts,
                                leg.Start.Lat, leg.Start.Lon, leg.End.Lat, leg.End.Lon));
            else if (leg.ElevationLevel == 0)
                Say(() =>
                _log.LogWarning("ROUTE PRE-FLIGHT task '{Task}' ({Unit}) leg {Leg}: NO ELEVATION DATA AT ANY LEVEL - " +
                                "dataset {Ds} returned no tile at L{From} down to L{To} anywhere along this leg " +
                                "({A:F5},{ALon:F5}) -> ({B:F5},{BLon:F5}). NOTHING about this leg was checked: it is " +
                                "NOT clear ground, and no shift can be taken on it (unknown ground is never clear). " +
                                "Either the AO is outside the elevation service or Vrf:PreflightElevationMinLevel " +
                                "is not deep enough for it{Off}.",
                                taskName, unitName, leg.Index, Preflight.TileMath.ElevationDataset,
                                svc.Tiles.ElevationLevel, svc.Tiles.ElevationMinLevel,
                                leg.Start.Lat, leg.Start.Lon, leg.End.Lat, leg.End.Lon,
                                svc.Options.Offline ? " - and Vrf:PreflightOffline is TRUE, so only the tile cache " +
                                                      "'" + svc.Tiles.CacheDirectory + "' was consulted" : ""));
            else if (leg.ElevationLevel < Preflight.TileMath.DefaultElevationLevel)
                Say(() =>
                _log.LogInformation("ROUTE PRE-FLIGHT task '{Task}' ({Unit}) leg {Leg}: scored at {Note}. This is the " +
                                    "SERVER'S answer - the finer level(s) returned 'no tile', not a failed fetch; a " +
                                    "fetch failure gets NO VERDICT instead of a coarser score (F2).",
                                    taskName, unitName, leg.Index,
                                    Preflight.TileMath.CalibrationNote(leg.Start.Lat, leg.ElevationLevel,
                                                                       svc.Options.WindowM, svc.Options.Threshold)));
            if (leg.WaterSamples <= 0) continue;
            water++;
            Say(() =>
            _log.LogWarning("ROUTE PRE-FLIGHT task '{Task}' ({Unit}) leg {Leg}: WATER ON THE LINE - {N} of {Total} " +
                            "sample(s) classify as {Soil} ({Src}), first at ({Lat:F5},{Lon:F5}), {Km:F2} km along. " +
                            "Deep water is acceleration-factor 0.000 in ground-tracked.sysdef, so a ground vehicle " +
                            "driven onto it STOPS and the task stays TaskRunning. This is an estimate off land-cover " +
                            "tiles: nothing is refused or altered, and the leg is dispatched.",
                            taskName, unitName, leg.Index, leg.WaterSamples, leg.Samples,
                            string.IsNullOrEmpty(leg.WaterSoil) ? "water" : leg.WaterSoil,
                            string.IsNullOrEmpty(leg.WaterSource) ? "land cover" : leg.WaterSource,
                            leg.WaterFirst.Lat, leg.WaterFirst.Lon, leg.WaterFirstSM / 1000.0));
        }
        return water;
    }

    /// <summary>
    /// Score one dispatched route OFF the tick thread and push a C2SIM ObservationReport pair for
    /// each flagged leg. The vertices are copied first: the caller's list belongs to the tick
    /// thread and is not safe to read from a worker.
    ///
    /// Failure is ALWAYS silent-but-logged. A pre-flight that throws - a missing vendor file, a
    /// dead network - must never disturb a task that VR-Forces has already been given.
    /// </summary>
    private void QueuePreflight(OrderTask task, CreatedUnit unit, List<Geodetic> routeGeo)
    {
        var route = routeGeo.Select(v => (Lat: v.LatDeg, Lon: v.LonDeg)).ToList();
        string template = _templateByName.TryGetValue(unit.Name, out var t) ? t : "";
        string taskName = task.TaskName;
        string taskeeUuid = task.TaskeeUuid;
        string unitName = unit.Name;
        // Only the LIFEFORM PROXY reads this (a DI-Guy row falls back to Tank Platoon USA/RUS),
        // but reading it off the same map the creates used keeps a red unit's proxy red.
        bool hostile = _hostilityByC2SimUuid.TryGetValue(taskeeUuid ?? "", out var hc) && hc == "HO";

        long censusGen = _tileCensus.Enter();       // E6
        _ = Task.Run(async () =>
        {
            try
            {
                var svc = GetPreflight();
                if (svc == null) return;
                MarkTileCensusStart(svc, censusGen);
                var limit = svc.LimitFor(template, hostile);
                var (legs, degenerate) = svc.ScoreRoute(route, limit.LimitRaw);
                var scored = new Preflight.TaskPreflight
                {
                    TaskName = taskName, TaskUuid = task.TaskUuid, UnitName = unitName,
                    UnitUuid = taskeeUuid, Template = limit.Template, LimitRaw = limit.LimitRaw,
                    VehicleNote = limit.Note, DegenerateLegs = degenerate,
                    Route = route, Legs = legs,
                };
                int noVerdict = legs.Count(l => l.NoVerdict);
                foreach (var leg in legs.Where(l => l.Flagged))
                    _log.LogWarning("ROUTE PRE-FLIGHT task '{Task}' ({Unit}) leg {Leg}: {Win:F0} m of {Grade:F3} on " +
                                    "{Soil} at {Lat:F4}/{Lon:F4}, {Km:F1} km along the leg; {Tmpl} limit {Limit:F3} " +
                                    "(max-slope {Raw:F2} x soil {Factor:F2}); {Verdict}. Scored at elevation " +
                                    "L{Level} (E6).",
                                    taskName, unitName, leg.Index, leg.SustainedWindowM, leg.Sustained, leg.Soil,
                                    leg.WorstLat, leg.WorstLon, leg.WorstSM / 1000.0,
                                    string.IsNullOrEmpty(limit.Template) ? "unit" : limit.Template,
                                    leg.Limit, leg.LimitRaw, leg.Factor,
                                    Preflight.PreflightReports.Verdict(leg.Ratio, svc.Options.Threshold),
                                    leg.ElevationLevel);
                // m3: a few no-verdict legs is ordinary (a tile gap). EVERY leg with no verdict
                // means the pre-flight checked NOTHING for this task - the shipped PreflightCacheDir
                // default is empty on a fresh deploy - and that must not read as an Info footnote.
                if (noVerdict > 0 && noVerdict == legs.Count)
                    _log.LogWarning("ROUTE PRE-FLIGHT task '{Task}' ({Unit}): ALL {N} leg(s) got NO VERDICT - no " +
                                    "elevation/land-cover tiles were available, so NOTHING was checked for this " +
                                    "task. Tile cache: '{Cache}' (Vrf:PreflightCacheDir; empty on a fresh deploy - " +
                                    "point it at a populated cache, e.g. tools/preflight/preflight_cache).",
                                    taskName, unitName, noVerdict, svc.Tiles.CacheDirectory);
                else if (noVerdict > 0)
                    _log.LogInformation("ROUTE PRE-FLIGHT task '{Task}' ({Unit}): {N} leg(s) got NO VERDICT - tiles " +
                                        "missing; they are neither flagged nor passed.", taskName, unitName, noVerdict);
                ReportLegTerrainFindings(taskName, unitName, legs, svc);

                // BuildForTask already emits the water finding in place of the grade flag on a
                // water leg, so this reader needs no second pass.
                var reports = Preflight.PreflightReports.BuildForTask(scored, svc.Options.Threshold,
                                                                     IsoNow(), NewReportId);
                // m6: these are ObservationReports, so a push failure must say so (the default
                // kind is Position). Neither kind retries - this is about the log being true.
                foreach (var xml in reports) await PushReportAsync(xml, ReportKind.Observation);
                if (reports.Count > 0)
                    _log.LogInformation("ROUTE PRE-FLIGHT task '{Task}' ({Unit}): {Flagged} flagged leg(s) of " +
                                        "{Total} checked; sent {Sent} ObservationReport(s).", taskName, unitName,
                                        reports.Count, legs.Count, reports.Count);
            }
            catch (Exception e)
            {
                _log.LogError("ROUTE PRE-FLIGHT task '{Task}' ({Unit}) failed - the task itself is unaffected: {Msg}",
                              taskName, unitName, C2SIMSDK.GetRootException(e).Message);
            }
            finally { ReportTileCensus(censusGen); }   // E6 - the last worker of this order prints it
        });
    }

    /// <summary>
    /// E6: record the tile counters as this order's FIRST scoring worker found them, so the census
    /// below can report the DELTA. Called by every worker; only the first of a generation has any
    /// effect, and it is the only one that can (the latch's outstanding count was 1 when it ran).
    /// </summary>
    private void MarkTileCensusStart(Preflight.PreflightService svc, long generation)
    {
        if (generation == 0 || svc == null) return;
        if (Interlocked.Exchange(ref _tileCensusMarkedGen, generation) == generation) return;
        _tileHitsAtOrder = svc.Tiles.CacheHits;
        _tileFetchesAtOrder = svc.Tiles.Fetched;
    }
    private long _tileCensusMarkedGen;

    /// <summary>
    /// E6 (D7 harvest: "no fetch count, no resolved elevation level logged - STILL UNFIXED").
    /// ONE line per order, written by whichever scoring worker of that order finishes last, saying
    /// how many tile reads came out of the cache and how many went over HTTP. Nothing inferred:
    /// the two numbers are <see cref="Preflight.TileSource"/>'s own counters, and the figure that
    /// matters - was this run's dispatch paying for tiles? - is the FETCH delta.
    /// </summary>
    private void ReportTileCensus(long generation)
    {
        if (!_tileCensus.Leave(generation, out int batch)) return;
        var svc = _preflight;
        if (svc == null) return;
        int hits = svc.Tiles.CacheHits - _tileHitsAtOrder;
        int fetches = svc.Tiles.Fetched - _tileFetchesAtOrder;
        // N8: RE-BASELINE FOR THE NEXT BATCH. The counters are monotonic, so "since the previous
        // census line" is the delta the next line must report; without this, batch 2 of a chained
        // order would re-report batch 1's reads. MarkTileCensusStart's per-generation guard has
        // already fired by now, so it cannot clobber this.
        _tileHitsAtOrder = svc.Tiles.CacheHits;
        _tileFetchesAtOrder = svc.Tiles.Fetched;
        _log.LogInformation("ROUTE PRE-FLIGHT TILE CENSUS, batch {Batch} of this order (E6/N8): {Hits} cache " +
                            "HIT(s), {Fetches} HTTP FETCH(es){Off} since the previous census line. RUNNING " +
                            "TOTAL SO FAR (not the run total - the final one is printed at shutdown): " +
                            "{CumHits} hits, {CumFetches} fetches, {Exhausted} tile(s) given up on after " +
                            "{Tries} attempts, {Undecodable} undecodable body(ies). Cache: {Cache}. A " +
                            "non-zero FETCH count means this order's dispatches waited on the network - " +
                            "pre-warm the AO's tiles to remove that wait. One line per BATCH of scoring " +
                            "work: a chained order scores one task per batch, so batch N is task N.",
                            batch, hits, fetches,
                            svc.Options.Offline ? " (Vrf:PreflightOffline is TRUE, so a fetch " +
                                                  "count above zero would be a defect)" : "",
                            svc.Tiles.CacheHits, svc.Tiles.Fetched, svc.Tiles.ExhaustedTiles,
                            Preflight.TileSource.MaxFetchAttempts, svc.Tiles.UndecodableBodies,
                            svc.Tiles.CacheDirectory);
    }

    /// <summary>
    /// N8 (D8 harvest): THE TILE TOTAL AT SHUTDOWN - the number the per-batch lines above could
    /// never be. Each of those reports a delta and a running total AS OF THAT LINE; whether any of
    /// them is the last is not knowable while the run is going.
    ///
    /// *** SF-R2 (cold-start review of 2df59ba): THIS LINE USED TO SAY "FINAL - every scoring
    /// worker has finished", AND NOTHING ESTABLISHED THAT. *** It is written before _stopTick and
    /// the tick thread's Join, and the scoring workers are unjoined Task.Run bodies that nothing
    /// awaits or drains - a route-shift worker still scoring at shutdown increments the counters
    /// AFTER this line. That is the N9 defect (a permanent record asserting what the code did not
    /// check) reappearing inside the N8 fix, which is the one place it had no excuse to.
    ///
    /// The fix is to OBSERVE rather than assert. The latch already knows how many workers are
    /// out; the line reads that number and says it, through
    /// <see cref="Preflight.TileCensusLatch.DescribeRunTotalScope"/> - a pure function of the one
    /// thing observed, so the sentence and the number cannot drift and both branches are
    /// assertable offline. Draining the workers instead would mean a shutdown that waits on a
    /// worker whose whole design is that nothing waits on it; saying what was seen is the cheaper
    /// truth and the honest one.
    ///
    /// Silent when no pre-flight service was ever built (the feature off, or a run that dispatched
    /// nothing) - there is no total to report.
    /// </summary>
    private void ReportRunTileTotal()
    {
        var svc = _preflight;
        if (svc == null) return;
        int outstanding = _tileCensus.OutstandingWorkers;
        _log.LogInformation("ROUTE PRE-FLIGHT TILE TOTAL for this RUN (E6/N8, at shutdown; {Scope}): " +
                            "{CumHits} cache HIT(s), {CumFetches} HTTP FETCH(es){Off}, {Exhausted} " +
                            "tile(s) given up on after {Tries} attempts, {Undecodable} undecodable " +
                            "body(ies). Cache: {Cache}. The per-batch CENSUS lines above are deltas and " +
                            "running totals as of each line.",
                            Preflight.TileCensusLatch.DescribeRunTotalScope(outstanding),
                            svc.Tiles.CacheHits, svc.Tiles.Fetched,
                            svc.Options.Offline ? " (Vrf:PreflightOffline is TRUE, so a fetch count above " +
                                                  "zero would be a defect)" : "",
                            svc.Tiles.ExhaustedTiles, Preflight.TileSource.MaxFetchAttempts,
                            svc.Tiles.UndecodableBodies, svc.Tiles.CacheDirectory);
    }

    // ============ THE LATERAL ROUTE SHIFT (Vrf:PreflightRouteShift; STP-804/806) ==============
    // docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md. The dispatch is DEFERRED to a worker
    // that scores the route and chooses the detour, then re-entered on the tick thread.
    //
    // THE ONE-SHOT LATCH IS THE WHOLE SAFETY PROPERTY HERE. Two independent things can continue a
    // deferred dispatch - the worker finishing and the timeout sweep firing - and if both did, the
    // task would be DISPATCHED TWICE (two routes, two MoveAlongRoute, two MarkDispatched). Claim
    // decides it: exactly one caller ever gets true.
    private sealed class PendingShift
    {
        private readonly Preflight.OneShotClaim _claim = new();
        public PendingShift(DateTime deadline, OrderTask task, CreatedUnit unit, List<Geodetic> authored)
        { Deadline = deadline; Task = task; Unit = unit; Authored = authored; }
        public DateTime Deadline { get; }
        public OrderTask Task { get; }
        public CreatedUnit Unit { get; }
        public List<Geodetic> Authored { get; }
        public bool Claim() => _claim.Claim();
    }

    private readonly ConcurrentDictionary<long, PendingShift> _pendingShift = new();
    private long _nextShiftId;

    // THE COLD-CACHE GUARD (proposal 1 of the route-shift default-ON review, applied here).
    // With Vrf:PreflightOffline TRUE and a tile cache that holds no files, NOTHING can ever be
    // scored: every sample is NaN, every candidate polyline is +infinity, no leg is ever shifted.
    // The feature is then on in name only while still deferring EVERY ground move's dispatch
    // through a worker and a 30 s timeout. That buys nothing and costs a wait, so the dispatch
    // takes the ordinary path instead - the same route, the same line, sooner.
    //
    // THE TEST IS THE CONJUNCTION, deliberately. An empty cache with fetching ALLOWED is the
    // normal cold start: the tiles arrive over HTTP and legs really are scored, so it must still
    // run (it is only warned about, at start-up). Offline is what makes emptiness permanent.
    // Evaluated ONCE - with fetching off, nothing can populate the directory mid-run - and the
    // reason is logged once, because a silently skipped shift is the false green this lane exists
    // to remove.
    private int _shiftCanScore;            // 0 = not decided, 1 = yes, 2 = no

    /// <summary>
    /// THE RULE, pure and testable: the shift can only be SKIPPED when fetching is off AND the
    /// cache is known to be empty. A cache count of -1 means "could not be read", which is not
    /// evidence of emptiness, so it runs. (Selftest: RouteShiftSelfTest section 0.)
    /// </summary>
    internal static bool RouteShiftCanScore(bool offline, int cachedFiles)
        => !offline || cachedFiles != 0;

    /// <summary>
    /// F3 (cold-start review of f26d4ad): THE ONE PLACE THE TILE CACHE PATH IS RESOLVED. Three
    /// copies of this expression existed - the start-up banner, the can-we-score gate and the
    /// PreflightService construction - and three copies of a path is how a banner starts naming a
    /// directory the pre-flight does not use.
    ///
    /// Vrf:PreflightCacheDir, else <see cref="ShippedCacheFallback"/>. The fallback is DELIBERATELY
    /// today's behaviour: it lives under the build output and a clean rebuild deletes it, which is
    /// the defect F3 names - but a durable location outside the repo is a decision about where this
    /// process writes on someone's machine, so it is offered as a SETTING (appsettings.json
    /// _PreflightCacheDir) rather than taken here.
    /// </summary>
    internal static string ResolvePreflightCacheDir(string configured)
        => string.IsNullOrWhiteSpace(configured) ? ShippedCacheFallback() : configured;

    /// <summary>The shipped default: "preflight-cache" beside the executable. Named so the
    /// start-up banner can say WHICH of the two it is using without re-deriving the test.</summary>
    internal static string ShippedCacheFallback()
        => Path.Combine(AppContext.BaseDirectory, "preflight-cache");

    /// <summary>How many files the cache holds, or -1 when the directory could not be read.
    /// -1 is NOT zero: an unreadable directory is not evidence of emptiness (RouteShiftCanScore).</summary>
    internal static int CountCacheFiles(string dir)
    {
        try { return Directory.Exists(dir) ? Directory.GetFiles(dir).Length : 0; }
        catch { return -1; }
    }

    private bool RouteShiftCanScore()
    {
        int known = Volatile.Read(ref _shiftCanScore);
        if (known != 0) return known == 1;
        string dir = ResolvePreflightCacheDir(_vrf.PreflightCacheDir);
        int files = -1;
        if (_vrf.PreflightOffline) files = CountCacheFiles(dir);   // -1 (unreadable) -> let it run
        bool can = RouteShiftCanScore(_vrf.PreflightOffline, files);
        if (!can)
            _log.LogWarning("LATERAL ROUTE SHIFT SKIPPED FOR THIS RUN: Vrf:PreflightOffline is TRUE and the tile " +
                            "cache {Cache} holds no files, so no leg can ever be scored and no route can ever be " +
                            "shifted. Every ground move is dispatched on its AUTHORED line immediately instead of " +
                            "waiting {T:F0} s per task for a search that cannot succeed. Pre-warm the AO's tiles " +
                            "(e.g. a deployed copy of tools/preflight/preflight_cache) and point " +
                            "Vrf:PreflightCacheDir at them, or set Vrf:PreflightOffline=false to fetch them.",
                            dir, _vrf.PreflightRouteShiftTimeoutSeconds);
        Volatile.Write(ref _shiftCanScore, can ? 1 : 2);
        return can;
    }

    /// <summary>The chooser's settings, straight from configuration. The THRESHOLD is the
    /// pre-flight's own - there is no second threshold anywhere in this feature.</summary>
    private Preflight.RouteShiftOptions ShiftOptions() => new()
    {
        MaxMeters = _vrf.PreflightRouteShiftMaxMeters,
        StepMeters = Math.Max(1.0, _vrf.PreflightRouteShiftStepMeters),
        MarginRatio = _vrf.PreflightRouteShiftMarginRatio,
        ClearFormationBand = _vrf.PreflightRouteShiftClearFormationBand,
        PadMeters = _vrf.PreflightRouteShiftPadMeters,
        LeadMeters = _vrf.PreflightRouteShiftLeadMeters,
        MaxTurnDegrees = _vrf.PreflightRouteShiftMaxTurnDegrees,
        Threshold = _vrf.PreflightThreshold,
    };

    /// <summary>
    /// STP's vertices, in order, with the inserted points of each shifted leg spliced between
    /// them. An inserted vertex takes the altitude of the authored vertex it follows; in the
    /// default TerrainProfile mode every altitude here is replaced by the profile reply anyway,
    /// and in the other modes all ground vertices share one value, so this is never an invented
    /// height.
    /// </summary>
    internal static List<Geodetic> SpliceShift(IReadOnlyList<Geodetic> authored,
                                               IReadOnlyList<Preflight.LegShift> shifts)
    {
        var byLeg = new Dictionary<int, Preflight.LegShift>();
        if (shifts != null)
            foreach (var s in shifts)
                if (s.Shifted) byLeg[s.LegIndex] = s;
        var outp = new List<Geodetic>(authored.Count + 4 * byLeg.Count);
        for (int i = 0; i < authored.Count; i++)
        {
            outp.Add(authored[i]);
            if (i + 1 >= authored.Count) break;
            if (!byLeg.TryGetValue(i + 1, out var s)) continue;
            double alt = authored[i].AltMeters;
            foreach (var p in s.Inserted)
                outp.Add(new Geodetic { LatDeg = p.Lat, LonDeg = p.Lon, AltMeters = alt });
        }
        return outp;
    }

    /// <summary>
    /// Defer this dispatch, score the route on a worker and re-enter with the result. The caller
    /// has already returned from the tick pass, so EVERY path out of here must end in a
    /// continuation - which is why the catch continues with the authored route rather than
    /// rethrowing, and why the timeout sweep exists for the case the worker never returns at all.
    /// </summary>
    /// <summary>
    /// F1 (cold-start review of f26d4ad): A LOG BUFFER FOR WORK THAT HAS NOT YET EARNED THE RIGHT
    /// TO BE BELIEVED.
    ///
    /// The route-shift worker wrote every "ROUTE SHIFTED 50 m north, route 3 -&gt; 7 vertices" line
    /// BEFORE it claimed the dispatch. State, reports and dispatch were always safe - the claim
    /// gates all three - but a worker that lost the race to the 30 s timeout sweep had ALREADY
    /// written a confident paragraph about a detour on a task that was dispatched on the AUTHORED
    /// line, with no marker tying the two together and in either order relative to the sweep's own
    /// warning. That is exactly the false-green shape this lane exists to remove, and it reaches
    /// the harvest reader.
    ///
    /// So: buffer, claim, then flush - or, having lost, DISCARD and say so in ONE line. Nothing is
    /// suppressed that was earned, and nothing is asserted that was not.
    /// </summary>
    private sealed class DeferredLog
    {
        private readonly List<Action> _lines = new();
        public int Count => _lines.Count;
        public void Add(Action write) => _lines.Add(write);
        public void Flush() { foreach (var w in _lines) w(); _lines.Clear(); }
        public void Discard() => _lines.Clear();
    }

    private void QueueRouteShift(OrderTask task, CreatedUnit unit, List<Geodetic> routeGeo)
    {
        long id = Interlocked.Increment(ref _nextShiftId);
        var authored = new List<Geodetic>(routeGeo);
        var pending = new PendingShift(
            DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.PreflightRouteShiftTimeoutSeconds)),
            task, unit, authored);
        _pendingShift[id] = pending;

        var route = authored.Select(v => (Lat: v.LatDeg, Lon: v.LonDeg)).ToList();
        string template = _templateByName.TryGetValue(unit.Name, out var t) ? t : "";
        bool hostile = _hostilityByC2SimUuid.TryGetValue(task.TaskeeUuid ?? "", out var hc) && hc == "HO";
        string taskName = task.TaskName, unitName = unit.Name, taskeeUuid = task.TaskeeUuid;
        var opt = ShiftOptions();

        _log.LogInformation("Task '{Task}': ROUTE SHIFT check queued for {Name} ({N} vertices); dispatch deferred " +
                            "to the result (timeout {T:F0} s -> the authored line).",
                            taskName, unitName, route.Count, _vrf.PreflightRouteShiftTimeoutSeconds);

        long censusGen = _tileCensus.Enter();       // E6
        _ = Task.Run(async () =>
        {
            List<Geodetic> shifted = null;
            List<string> reports = null;
            // F1: EVERY line this worker would write goes in here, not to the logger. See
            // DeferredLog. The queue line above is already out - it is true whoever wins - but
            // nothing about the RESULT may be said until the claim is taken.
            var pending = new DeferredLog();
            bool threw = false;
            try
            {
                var svc = GetPreflight();
                if (svc == null)
                {
                    pending.Add(() =>
                        _log.LogWarning("Task '{Task}': ROUTE SHIFT skipped - the pre-flight could not start; the " +
                                        "authored line is dispatched.", taskName));
                }
                else
                {
                    MarkTileCensusStart(svc, censusGen);   // E6
                    var limit = svc.LimitFor(template, hostile);
                    var outcome = svc.ShiftRoute(route, limit.LimitRaw, opt);
                    // BEFORE the shift rows, and for EVERY leg rather than only the flagged ones:
                    // with Vrf:PreflightWarnings off (still the shipped default) this reader is
                    // the ONLY one that runs, and it used to say nothing at all about a leg it
                    // never flagged - including a leg nothing could be sampled for.
                    int waterLegs = ReportLegTerrainFindings(taskName, unitName, outcome.Legs, svc, pending);
                    foreach (var s0 in outcome.Shifts)
                    {
                        var s = s0;   // captured per iteration - the closures outlive the loop
                        if (s.Shifted)
                            pending.Add(() =>
                                _log.LogWarning("Task '{Task}' ({Unit}) leg {Leg}: ROUTE SHIFTED {D:F0} m {Side} - ratio " +
                                            "{Base:F3} -> {New:F3}{Band}; inserted ({ILat:F6},{ILon:F6}) and " +
                                            "({OLat:F6},{OLon:F6}). STP's own vertices are unchanged and in order.",
                                            taskName, unitName, s.LegIndex, Math.Abs(s.OffsetMeters), s.SideWord,
                                            s.BaseRatio, s.ShiftedRatio,
                                            double.IsNaN(s.BandMax) ? "" : FormattableString.Invariant(
                                                $" (formation band max {s.BandMax:F3})"),
                                            s.In.Lat, s.In.Lon, s.Out.Lat, s.Out.Lon));
                        else
                            pending.Add(() =>
                                _log.LogWarning("Task '{Task}' ({Unit}) leg {Leg}: NO ROUTE SHIFT - {Note}. The task is " +
                                            "dispatched on the line as authored.", taskName, unitName, s.LegIndex, s.Note));
                        // The FALLBACK ending of the two-phase chooser: C1 chose the side, nothing on it
                        // could also clear the formation band, and the route-line rule alone was taken.
                        // It is a WARNING because some of the formation is knowingly left on flagged ground.
                        if (s.BandNotCleared)
                            pending.Add(() =>
                                _log.LogWarning("Task '{Task}' ({Unit}) leg {Leg}: the ROUTE SHIFT could NOT clear the " +
                                            "formation band anywhere on the {Side} side within +/-{Band:F0} m; it was " +
                                            "taken on the ROUTE LINE alone, so some formation slots may sit on flagged " +
                                            "ground. The side is C1's and is never traded for a band.",
                                            taskName, unitName, s.LegIndex, s.SideWord, opt.MaxMeters));
                        // Every candidate, with its missing-tile count: a feature that changes where
                        // units drive does not get to keep its reasoning to itself.
                        pending.Add(() =>
                            _log.LogInformation("Task '{Task}' ({Unit}) leg {Leg}: ROUTE SHIFT candidates - {Trace}",
                                            taskName, unitName, s.LegIndex, Preflight.RouteShift.DescribeCandidates(s)));
                    }
                    if (outcome.Changed)
                    {
                        shifted = SpliceShift(authored, outcome.Shifts);
                        int after = shifted.Count;
                        pending.Add(() =>
                            _log.LogInformation("Task '{Task}' ({Unit}): ROUTE SHIFT applied to {N} of {F} flagged leg(s); " +
                                            "route {Before} -> {After} vertices.", taskName, unitName,
                                            outcome.ShiftedCount, outcome.Shifts.Count, authored.Count, after));
                    }
                    else if (outcome.Shifts.Count == 0)
                        pending.Add(() =>
                            _log.LogInformation("Task '{Task}' ({Unit}): ROUTE SHIFT - no leg flagged; the route is " +
                                            "unchanged.", taskName, unitName));
                    reports = Preflight.PreflightReports.BuildForShift(taskeeUuid, unitName, taskName,
                                                                      outcome.Shifts, outcome.Legs,
                                                                      IsoNow(), NewReportId);
                    // The shift's own rows are per FLAGGED leg, so water on a leg it never
                    // flagged would reach the C2 side nowhere. Appended, not merged: a flagged
                    // water leg gets both its shift row and its water row, and both are true.
                    if (waterLegs > 0)
                        reports.AddRange(Preflight.PreflightReports.BuildWaterFindings(
                                             taskeeUuid, unitName, taskName, outcome.Legs,
                                             IsoNow(), NewReportId));
                }
            }
            catch (Exception e)
            {
                threw = true;
                string msg = C2SIMSDK.GetRootException(e).Message;
                pending.Add(() =>
                    _log.LogError("Task '{Task}' ({Unit}): ROUTE SHIFT failed - the task is dispatched on the line as " +
                                  "authored: {Msg}", taskName, unitName, msg));
                shifted = null;
            }
            // CLAIM BEFORE ANYTHING IS SAID OR DISPATCHED. If the timeout sweep already continued
            // this task on the authored line, a "route shifted" report would be a lie and a second
            // continuation would dispatch it twice.
            if (!ContinueShift(id, shifted))
            {
                // ORPHANED. The sweep (or a second completion) already dispatched this task, on the
                // AUTHORED line. Everything this worker computed is now about a route nobody is
                // driving, so it is DISCARDED - and said once, at WARNING, because a harvest that
                // sees the timeout must also be able to see that the search finished afterwards and
                // what it would have claimed. No leg rows, no "ROUTE SHIFTED" assertion, no reports.
                int discarded = pending.Count;
                pending.Discard();
                _log.LogWarning("Task '{Task}' ({Unit}): the ROUTE SHIFT search finished AFTER the dispatch had " +
                                "already been continued by someone else (the {T:F0} s timeout sweep, normally), so " +
                                "its result IS DISCARDED: {N} log line(s) and {R} ObservationReport(s) suppressed, " +
                                "and it {Would} have changed the route. The task was dispatched on the AUTHORED " +
                                "line; nothing below or above this line describes the route it is driving.",
                                taskName, unitName, _vrf.PreflightRouteShiftTimeoutSeconds, discarded,
                                reports?.Count ?? 0,
                                threw ? "could not say whether it would" : shifted != null ? "WOULD" : "would NOT");
                ReportTileCensus(censusGen);   // E6 - the latch must drain on EVERY exit
                return;
            }
            // The claim is ours: the route this worker computed IS the route being dispatched, so
            // everything it found is now true of the run.
            pending.Flush();
            if (reports != null)
                foreach (var xml in reports) await PushReportAsync(xml, ReportKind.Observation);
            ReportTileCensus(censusGen);   // E6 - the last worker of this order prints it
        });
    }

    /// <summary>Claim the pending shift and re-enter the dispatch on the tick thread. Returns false
    /// when someone else already continued it (the timeout sweep, or a second worker completion).</summary>
    private bool ContinueShift(long id, List<Geodetic> shifted)
    {
        if (!_pendingShift.TryGetValue(id, out var pending) || !pending.Claim()) return false;
        _pendingShift.TryRemove(id, out _);
        var route = shifted ?? pending.Authored;
        var task = pending.Task;
        var unit = pending.Unit;
        _tickActions.Enqueue(() => DeferredDispatch.Run(
            () => ExecuteTaskOnTick(task, unit, null, route),
            task.TaskUuid, task.TaskName, DeferredDispatch.RouteShiftContinuation, _sequencer,
            reason => PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKABRT, reason),
            ex => _log.LogError("Task '{Task}': THE ROUTE-SHIFT CONTINUATION THREW on the VR-Forces tick thread " +
                                "({Type}: {Msg}) - that is the pass which dispatches this task, so it was NOT " +
                                "dispatched. It is abandoned and reported TASKABRT so its STREND successors fail " +
                                "fast instead of waiting out the chain backstop.",
                                task.TaskName, ex.GetType().Name, ex.Message)));
        return true;
    }

    /// <summary>Tick-loop sweep: a shift check past its deadline dispatches the AUTHORED line.
    /// A pre-flight that is slow, wedged on a cold tile fetch or simply unlucky must never hold a
    /// task; the worst this feature may cost is today's behaviour.</summary>
    private void ExpireShiftRequests()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _pendingShift)
        {
            if (kv.Value.Deadline > now) continue;
            string name = kv.Value.Task?.TaskName;
            if (ContinueShift(kv.Key, null))
                _log.LogWarning("Task '{Task}': the ROUTE SHIFT check did not finish within {T:F0} s - dispatching " +
                                "on the line as authored.", name, _vrf.PreflightRouteShiftTimeoutSeconds);
        }
    }

    private void MaybeCheckArrivals()
    {
        var now = DateTime.UtcNow;
        if (now < _nextArrivalCheck) return;
        _nextArrivalCheck = now.AddSeconds(Math.Max(1, _vrf.ArrivalCheckSeconds));
        foreach (var kv in _inFlight.Snapshot())
        {
            string name = kv.Key;
            var rec = kv.Value;
            if (rec.DestLat is not double dlat || rec.DestLon is not double dlon) continue;
            if ((now - rec.DispatchedUtc).TotalSeconds < _vrf.ArrivalMinSecondsSinceDispatch) continue;
            if (_arrivalReported.ContainsKey(name)) continue;
            // STP-837: IS ARRIVING THERE EVIDENCE OF ANYTHING? A route whose last vertex sits
            // inside the arrival radius of the position the unit was dispatched from cannot be
            // closed this way at all - V6g's T_R5_PL1 mirrored vertex 2 onto the platoon's own
            // start, and "4/4 within 500 m (nearest 26 m)" was true before anything moved.
            double startLat = rec.StartLat ?? double.NaN, startLon = rec.StartLon ?? double.NaN;
            double lastFromStart = double.IsNaN(startLat)
                ? double.NaN
                : RouteExtentPolicy.GreatCircleMeters(startLat, startLon, dlat, dlon);
            double radius = ArrivalPolicy.RadiusFor(_vrf.ArrivalRadiusMeters, rec.RouteLengthMeters);
            if (!ArrivalPolicy.ClosableByArrival(lastFromStart, radius))
            {
                if (_arrivalNotClosable.TryGetValue(name, out var said) && said == (rec.TaskUuid ?? "")) continue;
                _arrivalNotClosable[name] = rec.TaskUuid ?? "";
                _log.LogInformation("ARRIVAL EVIDENCE CANNOT CLOSE {Name}'s task '{Task}': the route's last vertex is " +
                                    "{D:F0} m from the position the unit was dispatched from, inside the {R:F0} m " +
                                    "arrival radius of a {Len:F0} m route - standing there is not evidence of having " +
                                    "driven it (STP-837; V6g reported 4/4 'within 500 m of the last vertex, nearest " +
                                    "26 m' while one M1A2 had moved 20 m). This task closes only on a VR-Forces " +
                                    "completion: its C2SIM Duration alone no longer closes a task with a " +
                                    "destination - once overdue it waits for that completion (RL-20260921-09).",
                                    name, rec.TaskName, lastFromStart, radius, rec.RouteLengthMeters);
                continue;
            }
            if (!TryReadMemberPositions(name, out var positions, out int total)) continue;
            // WHERE EACH MEMBER IS, AND HOW FAR IT HAS COME. The baseline is that member's own
            // dispatch position when we have it; otherwise the taskee's, which is the position the
            // route was built from. A member with neither carries NaN travel and is never counted -
            // loudly, because that would silently stop a healthy task from closing.
            _dispatchPositions.TryGetValue(name, out var baseline);
            bool baselineFits = baseline != null
                                && string.Equals(baseline.TaskUuid, rec.TaskUuid ?? "", StringComparison.Ordinal);
            if (double.IsNaN(startLat) && !_arrivalNoBaselineWarned)
            {
                _arrivalNoBaselineWarned = true;
                _log.LogWarning("ARRIVAL EVIDENCE: {Name}'s in-flight task '{Task}' carries NO dispatch position, so " +
                                "the STP-837 traversal test cannot be applied and this task will not close on arrival " +
                                "evidence. Every move dispatch records one; this means a dispatch path was added " +
                                "without passing its route to MarkDispatched.", name, rec.TaskName);
            }
            var samples = new List<ArrivalPolicy.MemberSample>(positions.Count);
            foreach (var p in positions)
            {
                double dist = TerrainVertexAuthoring.DistMeters(p.Value.Lat, p.Value.Lon, dlat, dlon);
                double fromLat = startLat, fromLon = startLon;
                if (baselineFits && baseline.ByUuid != null && baseline.ByUuid.TryGetValue(p.Key, out var was))
                {
                    fromLat = was.Lat;
                    fromLon = was.Lon;
                }
                double travel = double.IsNaN(fromLat)
                    ? double.NaN
                    : TerrainVertexAuthoring.DistMeters(fromLat, fromLon, p.Value.Lat, p.Value.Lon);
                // THE MEMBER'S OWN JOURNEY (user ruling 2026-09-21): how far THIS member was from
                // the last vertex when the task was dispatched. Same baseline as the travel above,
                // so a member with no baseline has neither figure and keeps the route-length bar.
                double ownApproach = double.IsNaN(fromLat)
                    ? double.NaN
                    : TerrainVertexAuthoring.DistMeters(fromLat, fromLon, dlat, dlon);
                samples.Add(new ArrivalPolicy.MemberSample(dist, travel, ownApproach));
            }
            var d = ArrivalPolicy.DecideWithTraversal(samples, total, _vrf.ArrivalRadiusMeters,
                                                      _vrf.ArrivalMemberFraction, rec.RouteLengthMeters,
                                                      _vrf.ArrivalMinTravelMeters,
                                                      _vrf.ArrivalApproachFraction,
                                                      lastFromStart);
            // N7 (D7 harvest): EVERY CHECK, not only the one that closes, is stamped on BOTH
            // clocks. The old line said "{T:F0}s after dispatch" of a figure that is
            // DateTime.UtcNow - DispatchedUtc, i.e. WALL minus WALL, and D6's report read it as a
            // sim figure and recorded a "sim/wall 1.00" that was never measured.
            if (!d.Arrived)
                _log.LogDebug("ARRIVAL CHECK {Name} task '{Task}': {Within}/{Total} within {R:F0} m (nearest " +
                              "{Near:F0} m), route bar {Req:F0} m, lowest member bar {Low:F0} m, relaxation {Relax}; " +
                              "{Stamp}.",
                              name, rec.TaskName, d.Within, d.Total, d.RadiusMeters, d.NearestMeters,
                              d.RequiredTravelMeters, d.LowestMemberBarMeters,
                              d.ApproachRelaxationApplied ? "APPLIED" : "REFUSED (SF-1)",
                              ClockStamp(rec.DispatchedUtc, now, rec.DispatchedSimSeconds));
            if (!d.Arrived) continue;
            _arrivalReported[name] = rec.TaskUuid ?? "";
            _log.LogInformation("ARRIVAL EVIDENCE: {Name} task '{Task}' - {Within}/{Total} member(s) within {R:F0} m of the " +
                                "last vertex (nearest {Near:F0} m) AND past their OWN traversal bar (route bar {Req:F0} m, " +
                                "lowest member bar {Low:F0} m at approach fraction {F:F2}, per-member relaxation {Relax}; " +
                                "farthest travel {Far:F0} m of a {Len:F0} m route whose last vertex is {Away:F0} m from " +
                                "the dispatch position) {Stamp} - reporting completion from the unit's own " +
                                "evidence (user rulings 2026-09-07 and 2026-09-21; traversal required, STP-837); a later " +
                                "vendor completion is swallowed.",
                                name, rec.TaskName, d.Within, d.Total, d.RadiusMeters, d.NearestMeters,
                                d.RequiredTravelMeters, d.LowestMemberBarMeters, _vrf.ArrivalApproachFraction,
                                d.ApproachRelaxationApplied
                                    ? "APPLIED (the route goes somewhere: the last vertex is at least half the "
                                      + "route's length from where the taskee was dispatched)"
                                    : "REFUSED (SF-1: the last vertex is less than half the route's length from "
                                      + "where the taskee was dispatched, so every member kept the ROUTE bar)",
                                d.FarthestTravelMeters, rec.RouteLengthMeters, lastFromStart,
                                ClockStamp(rec.DispatchedUtc, now, rec.DispatchedSimSeconds));
            // R10 fan-out (opt-in): mark the unit's fan-out synthesized under THIS task uuid so the
            // later member completions and the straggler timer are swallowed by the tracker's own
            // Synthesized state instead of emitting a second, empty-uuid TASKCMPLT.
            _fanOut.TrySynthesizeByTimeout(name, rec.TaskUuid ?? "", out _, out _, out _);
            // No VRF completion callback on this path -> no VRF task type to sanity-check; empty =
            // "can't tell" for KindLooksRight (no spurious attribution-anomaly warning). The
            // provenance is the ARRIVAL EVIDENCE line above.
            SynthesizeUnitCompletion(name, "");
        }
    }

    /// <summary>
    /// N7: ONE ELAPSED-TIME STAMP, ON BOTH CLOCKS, WITH THE UNIT SPELLED OUT.
    ///
    /// Every "N s after dispatch" this interface has ever printed was
    /// <c>DateTime.UtcNow - DispatchedUtc</c> - WALL seconds - and nothing said so, which is how
    /// the D6 harvest came to record a "sim/wall 1.00" it had never measured (it compared that
    /// figure against wall capture stamps: the same clock on both sides). D7 then measured ~3.00
    /// off the level-3 console rows. So the word WALL is now in the line, and the SIMULATION
    /// clock - VrfBridge.SimTimeSeconds(), the reader the watchdog and the task clock already
    /// share - is printed beside it whenever it is readable.
    ///
    /// The sim elapsed figure needs a sim-clock mark at dispatch, which only exists for tasks
    /// dispatched since <see cref="MarkDispatched"/> started recording one; when it does not, the
    /// absolute sim time is still printed, because "the scenario clock stood at S" is evidence
    /// even without a baseline. An unreadable clock says so in words - never "0", never a blank.
    /// </summary>
    private string ClockStamp(DateTime dispatchedUtc, DateTime nowUtc, double simAtDispatch = double.NaN)
    {
        double wall = (nowUtc - dispatchedUtc).TotalSeconds;
        var obs = _simClockLast;
        if (!obs.ReadableConfirmed || !double.IsFinite(obs.SimSeconds) || obs.SimSeconds < 0.0)
            return FormattableString.Invariant($"{wall:F1} WALL s after dispatch")
                 + " (the SIMULATION clock is not readable - DtVrfRemoteController::simTime() reports"
                 + " no back end, so there is no sim figure for this line and the wall figure must"
                 + " not be read as one)";
        if (double.IsNaN(simAtDispatch))
            return FormattableString.Invariant(
                       $"{wall:F1} WALL s after dispatch; the SIMULATION clock stands at {obs.SimSeconds:F1} s")
                 + " (no sim mark was taken at this task's dispatch, so no sim elapsed figure)";
        return FormattableString.Invariant(
                   $"{wall:F1} WALL s after dispatch = {obs.SimSeconds - simAtDispatch:F1} SIMULATION s ")
             + FormattableString.Invariant($"(sim clock {simAtDispatch:F1} -> {obs.SimSeconds:F1} s)");
    }

    /// <summary>
    /// R4 (user ruling 2026-09-14, "completion is given by the end time"). Tick thread: walk every
    /// dispatched task forward on the interface's clock and close the ones whose C2SIM Duration has
    /// elapsed - one TASKCMPLT through the single emit point, and the STREND gate released so the
    /// successors dispatch, exactly as a vendor or arrival completion does.
    ///
    /// THE CLOCK IS THE ONE THE PROGRESS WATCHDOG ALREADY USES (Vrf:StallClock, StallPolicy):
    /// the back end's scenario clock when it is asked for AND readable, the wall clock otherwise.
    /// One preference, one pair of predicates, one fallback rule - a second clock abstraction
    /// here would be free to disagree with the watchdog about whether the scenario is running.
    /// The TimedCompletionPolicy is what makes the fallback safe: it accumulates FORWARD movement
    /// only and re-anchors on a mode change, so losing the sim reader mid-task neither completes
    /// the task early nor restarts its clock.
    ///
    /// NOT DONE HERE, on purpose: the VR-Forces task is NOT cancelled. The end time is the ORDER'S
    /// statement about the task, not the simulator's - the unit may still be driving, and if it
    /// later arrives or the vendor reports completion, TaskStatusPolicy suppresses the duplicate
    /// report (a task that has completed cannot complete twice). Cancelling the vendor task would
    /// be an unasked-for change to what the units do.
    /// </summary>
    /// <summary>
    /// M2: advance the TASK-CLOCK AXIS by the forward movement of whichever clock Vrf:TaskClock
    /// asks for and can actually be read. Tick thread, once a second. The one place the sim clock
    /// is read for tasking; everything else reads the axis.
    /// </summary>
    private void SampleTaskClock()
    {
        var now = DateTime.UtcNow;
        if (now < _nextTaskClockSample) return;
        _nextTaskClockSample = now.AddSeconds(TaskClockSampleSeconds);

        bool preferSim = StallPolicy.ParseClockPreference(_vrf.TaskClock, out bool clockValid);
        if (!clockValid && !_taskClockConfigWarned)
        {
            _taskClockConfigWarned = true;
            _log.LogWarning("Vrf:TaskClock='{Value}' is neither \"sim\" nor \"wall\" - C2SIM task times " +
                            "(Duration, StartTime delay, the STREND gate) are measured on the WALL clock.",
                            _vrf.TaskClock);
        }
        // ONE READ FOR EVERY CONSUMER (M3/M4). The watchdog asks for the sim clock through its own
        // knob; if EITHER it or the task clock wants it, it is read here, once, and both consume
        // the same observation. Nobody else calls SimTimeSeconds.
        bool stallPrefersSim = _vrf.StallDetection
                               && StallPolicy.ParseClockPreference(_vrf.StallClock, out _);
        double simSeconds = -1.0;
        // N7 (D7 harvest): READ IT UNCONDITIONALLY. Until now the clock was read only when the
        // task clock or the watchdog asked for it - and BOTH ship off on the base profile, so a
        // default run had no sim reading at all and every elapsed figure it printed was wall
        // seconds with nothing saying so. The read is documented read-only, sends nothing on the
        // wire and registers no callback (VrfFacade.h:478-482); it costs one call per second on a
        // thread that is already ticking, and it is what lets every line below name its clock.
        {
            try { simSeconds = _bridge.SimTimeSeconds(); }
            catch (Exception ex)
            { simSeconds = -1.0; _log.LogDebug(ex, "TASK CLOCK: sim-clock read failed; using the wall clock."); }
        }
        double wallNow = now.Ticks / (double)TimeSpan.TicksPerSecond;
        var obs = _simClock.Observe(simSeconds, wallNow, StallPolicy.ModeSwitchConfirmations,
                                    StallPolicy.StaleClockWarnSeconds);
        _simClockLast = obs;

        // N7: HOW FAST IS THE SCENARIO RUNNING? Once a minute, on a disjoint window, in the app's
        // own log - so nobody has to regress console rows against a trace to find out, and nobody
        // can record a 1.00 that was never measured.
        double ratio = _simWallRatio.Observe(obs.ReadableConfirmed ? obs.SimSeconds : double.NaN, wallNow);
        if (!double.IsNaN(ratio))
            _log.LogInformation("SIM/WALL RATIO {Ratio:F3} over the last {W:F0} WALL s (simulation clock now " +
                                "{Sim:F1} s; reader DtVrfRemoteController::simTime()). 1.000 = real time; above " +
                                "it the scenario is running fast (fixed-frame-run-to-complete); 0.000 = paused. " +
                                "EVERY 'after dispatch' figure in this log is WALL seconds unless the line says " +
                                "SIMULATION - the two are not interchangeable at this ratio.",
                                ratio, SimWallRatio.MinWindowWallSeconds, obs.SimSeconds);

        // A BACKWARDS STEP IS A FACT ABOUT THE CLOCK, not about any one consumer, so it is reported
        // here once rather than by each of them (rate-limited, and it says how many it did not
        // print - a jittering reader must read as the burst it is).
        if (obs.Step == StallPolicy.SimClockStep.RolledBack)
        {
            if ((now - _simRollbackLineUtc).TotalSeconds >= StallPolicy.LogRateLimitSeconds)
            {
                _simRollbackLineUtc = now;
                _log.LogWarning("SIM CLOCK: stepped BACKWARDS, {Was:F1} s -> {Now:F1} s " +
                                "(DtVrfRemoteController::rollbackToSnapshot, vrfRemoteController.h:605). " +
                                "This is a NEW timeline, not a stale clock: no task is completed by it and " +
                                "no wait is restarted - the task-clock axis simply adds nothing for this " +
                                "step.{Sup}", obs.PreviousSimSeconds, obs.SimSeconds,
                                _simRollbackLinesSuppressed > 0
                                    ? " (" + _simRollbackLinesSuppressed + " earlier backwards step(s) not logged)"
                                    : "");
                _simRollbackLinesSuppressed = 0;
            }
            else _simRollbackLinesSuppressed++;
        }

        // M3: THE MODE IS THE HYSTERESIS-CONFIRMED ONE. A reader alternating -1 / >= 0 at this
        // cadence would otherwise flip the mode on every sample, and the axis adds nothing across a
        // mode change - so nothing would ever be served, silently, forever.
        bool heldOnSim = preferSim && obs.ReadableConfirmed;
        // Held on the sim clock but nothing to read THIS sample (the "back end briefly out of the
        // list" case): add nothing and keep the anchor, exactly as the watchdog skips its check.
        // Serving the wall clock here instead would flip the mode on every miss, which is the very
        // starvation the hysteresis exists to prevent.
        if (heldOnSim && !obs.Readable) return;

        // Q5 (USER RULING 2026-09-14): A PAUSED SCENARIO DOES NOT AGE A TASK. M4's cure for a
        // frozen reader was to serve WALL seconds after 60 s of flatness - which burned ten
        // minutes off every armed Duration for a ten-minute coffee break. The clock now HOLDS
        // while a VR-Forces back end is still there and OPERATING, and falls back to wall only
        // when none is. The back-end reads happen ONLY in the stale branch, so the golden path
        // keeps its single per-second bridge call.
        //
        // STP-809: the three reads are BackendCount (the pre-STP-809 signal, kept as the
        // fallback), BackendControlState (Paused vs Running, DtVrfRemoteController::
        // backendsControlState) and ActiveBackendCount (how many known back ends the vendor
        // still calls simulatable or in transition). BackendCount alone could not tell a PAUSED
        // back end from one DEACTIVATED for missing its status timeout, because
        // DtVrfBackendListener::doTimeouts() deactivates such an entry instead of removing it -
        // so a back end that died IN PLACE froze task time for the rest of the run (pass-3
        // review E2). Each read is guarded on its own: one that throws leaves its own signal at
        // "no reading" and StallPolicy.TaskClockAction then decides on whatever is left, which
        // with all three gone is exactly the pre-STP-809 rule.
        bool taskSimStale = heldOnSim && obs.Stale;
        bool backEndPresent = false;
        int backendCount = 0;
        var control = StallPolicy.BackendControl.Unreadable;
        int activeBackends = -1;
        if (taskSimStale && _backendLost)
        {
            // STP-822: the timed liveness read has ALREADY confirmed this back end gone, on the
            // same three vendor signals and on its own schedule. Re-reading them here could only
            // repeat that answer a second later, and a disagreement would print "the back end
            // REPORTS PAUSED" about a back end this interface has declared lost. The verdict is
            // taken from there, which is TaskClockAction rule 1 (not one active back end -> WALL).
            backendCount = 0;
            backEndPresent = false;
            control = StallPolicy.BackendControl.NoBackend;
            activeBackends = 0;
        }
        else if (taskSimStale)
        {
            try { backendCount = _bridge.BackendCount(); backEndPresent = backendCount > 0; }
            catch (Exception ex)
            {
                // Unreadable = treat it as gone: that is the pre-Q5 behaviour, and serving wall
                // seconds is the outcome that at least keeps moving.
                _log.LogDebug(ex, "TASK CLOCK: BackendCount read failed; treating the back end as gone.");
            }
            try { control = (StallPolicy.BackendControl)ReadBackendControlState(); }
            catch (Exception ex)
            {
                // A bridge that predates STP-809 throws MissingMethodException here, which is a
                // PARTIAL DEPLOY (RUNBOOK sec 9) - so it is said once a minute, not swallowed.
                control = StallPolicy.BackendControl.Unreadable;
                if ((now - _taskClockBackendReadWarnUtc).TotalSeconds >= StallPolicy.LogRateLimitSeconds)
                {
                    _taskClockBackendReadWarnUtc = now;
                    _log.LogWarning(ex, "TASK CLOCK: the back-end CONTROL STATE could not be read " +
                                        "(STP-809); falling back to the back-end COUNT. A " +
                                        "MissingMethodException here means the deployed VrfBridge.dll " +
                                        "predates STP-809 - see RUNBOOK sec 9.");
                }
            }
            if (!Enum.IsDefined(typeof(StallPolicy.BackendControl), control))
                control = StallPolicy.BackendControl.Other;   // a value this build does not know
            try { activeBackends = ReadActiveBackendCount(); }
            catch (Exception ex)
            {
                activeBackends = -1;
                _log.LogDebug(ex, "TASK CLOCK: ActiveBackendCount read failed; no reading.");
            }
        }
        var action = StallPolicy.TaskClockAction(heldOnSim, taskSimStale, backEndPresent,
                                                 control, activeBackends);
        // The one sentence that says WHICH of the back-end states produced this outcome. Built
        // once and interpolated into whichever line fires - one emit point per outcome, as before.
        string why = taskSimStale
            ? StallPolicy.BackendStateClause(control, activeBackends, backendCount) : "";

        // _taskClockHoldLineUtc doubles as "the last line said HOLD": it is set while holding and
        // cleared on every other outcome, so a HOLD that turns into a fall back to wall - the back
        // end went away while the clock was already flat - is not swallowed by the once-only guard.
        bool wasHolding = _taskClockHoldLineUtc != DateTime.MinValue;
        if (action == StallPolicy.TaskClockOnFlat.HoldOnSim)
        {
            // REPEATED, not said once. Since STP-809 a PAUSED reading is a positive answer rather
            // than an inference - but the hold can still rest on the BackendCount fallback (an
            // Unknown or unreadable control state), and on that fallback a back end DEACTIVATED
            // for missing its status timeout still looks exactly like a paused one. The clause
            // says which case this is, and the line must not be quiet in either.
            if (!_taskClockStaleWarned
                || (now - _taskClockHoldLineUtc).TotalSeconds >= StallPolicy.LogRateLimitSeconds)
            {
                _taskClockStaleWarned = true;
                _taskClockHoldLineUtc = now;
                _log.LogWarning("TASK CLOCK: the simulation clock has not advanced past {T:F1} s for {S:F0} wall " +
                                "seconds and {Why} - the scenario is PAUSED, not gone. C2SIM task times are " +
                                "HELD: {N} task(s) waiting on an end time age by NOTHING until the scenario " +
                                "runs again (Q5, user ruling 2026-09-14).{Caveat}",
                                obs.SimSeconds, obs.FlatForWallSeconds, why, _timed.Count,
                                control == StallPolicy.BackendControl.Paused
                                    ? ""
                                    : " CAVEAT: this rests on the back-end COUNT, and the vendor's list KEEPS " +
                                      "an entry that has missed its status timeout - so if this line keeps " +
                                      "repeating and nobody paused anything, the back end has died and task " +
                                      "time will stay frozen until it returns.");
            }
        }
        else if (taskSimStale && (!_taskClockStaleWarned || wasHolding))
        {
            _taskClockStaleWarned = true;
            _taskClockHoldLineUtc = DateTime.MinValue;   // no longer holding
            _log.LogWarning("TASK CLOCK: the simulation clock has not advanced past {T:F1} s for {S:F0} wall " +
                            "seconds and {Why} - the simulation is GONE, not paused{Was}. C2SIM task times " +
                            "({N} task(s) waiting on an end time) are served on the WALL clock until a back " +
                            "end returns; no task is completed early and no wait is restarted.",
                            obs.SimSeconds, obs.FlatForWallSeconds, why,
                            wasHolding ? " (task time was being HELD until now - the back end has gone away)" : "",
                            _timed.Count);
        }
        else if (_taskClockStaleWarned && !taskSimStale)
        {
            // E1 (pass-3 review): THIS BRANCH IS TWO DIFFERENT TRANSITIONS AND ONLY ONE OF THEM IS
            // A RECOVERY. taskSimStale is heldOnSim && obs.Stale, so it also goes false when
            // heldOnSim does - i.e. when ModeSwitchConfirmations consecutive UNREADABLE samples
            // have confirmed that the sim reader is GONE, which is the hysteresis path to the wall
            // clock (the early return above only covers a single unreadable sample while the mode
            // still says readable). The one sentence here announced "readable and advancing again"
            // at the exact tick every task time moved onto the WALL clock - the inverse of what
            // happened, on the branch Q5's own "gone" case actually travels. So the two are said
            // apart. (When E1 was written the OTHER exit - the stale branch's fall back to wall -
            // could not be reached at all, because SimTimeSeconds and BackendCount read the same
            // backends().count(): E2. STP-809 made it reachable by deciding on the back end's
            // CONTROL STATE and ACTIVE count instead, so BOTH exits now occur and both are
            // exercised by the rulings suite. This branch is unchanged either way.)
            _taskClockStaleWarned = false;
            _taskClockHoldLineUtc = DateTime.MinValue;
            if (heldOnSim)
                _log.LogInformation("TASK CLOCK: the simulation clock is readable and advancing again ({T:F1} s) - " +
                                    "C2SIM task times are served on it once more.", obs.SimSeconds);
            else
                _log.LogWarning("TASK CLOCK: DtVrfRemoteController::simTime() is NO LONGER READABLE - {N} " +
                                "consecutive unreadable samples have confirmed the mode change, so C2SIM task " +
                                "times ({T} task(s) waiting on an end time) are now served on the WALL clock. " +
                                "The last sim reading was {S:F1} s. This is NOT a recovery{Was}: the axis keeps " +
                                "every second already served, restarts no wait and completes no task early, but " +
                                "from here a task ages in REAL seconds until a readable sim clock returns.",
                                StallPolicy.ModeSwitchConfirmations, _timed.Count, obs.SimSeconds,
                                wasHolding ? " and it ends a HOLD - task time was frozen until now" : "");
        }

        // HoldOnSim advances on the SIM reading, which is flat by definition here, so it adds
        // exactly nothing AND keeps the axis anchored in sim mode - no mode change, no re-anchor.
        bool usingSim = action != StallPolicy.TaskClockOnFlat.FallBackToWall;
        _taskAxis.Advance(usingSim ? obs.SimSeconds : wallNow, usingSim);
    }

    // STP-809's two bridge readers, each in its OWN method and NOT inlined. The CLR resolves a
    // cross-assembly method token when it JITs the method that CALLS it, so a bridge that predates
    // STP-809 would throw MissingMethodException while SampleTaskClock itself was being compiled -
    // BEFORE its try/catch exists, on the vrf tick thread, on the FIRST tick, taking the whole task
    // clock with it. Behind a NoInlining call the resolution happens when the helper is first
    // invoked, INSIDE the guard, and a partial deploy degrades to the BackendCount rule with one
    // WARNING a minute instead of a repeating `Tick phase FAILED`. Same reason as
    // RuntimeCheck.ProbeBridge, and RUNBOOK sec 9 is the deploy procedure that prevents it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ReadBackendControlState() => _bridge.BackendControlState();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ReadActiveBackendCount() => _bridge.ActiveBackendCount();

    /// <summary>
    /// Wait <paramref name="seconds"/> OF THE TASK CLOCK. This is what puts the StartTime delay and
    /// the STREND predecessor gate on the same clock as the Duration (M2): a wall Task.Delay here
    /// is what made a run configured for the sim clock skip every successor, because 4,800 sim
    /// seconds is 6,600-17,800 wall seconds at the measured COA-STP1 ratios.
    /// </summary>
    private async Task TaskClockDelayAsync(double seconds, CancellationToken ct)
    {
        if (!(seconds > 0.0)) return;
        double start = TaskClockSeconds;
        while (TaskClockSeconds - start < seconds)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TaskClockPollMs, ct).ConfigureAwait(false);
        }
    }

    private void MaybeCompleteTimedTasks()
    {
        var now = DateTime.UtcNow;
        if (now < _nextTimedCheck) return;
        _nextTimedCheck = now.AddSeconds(TimedCheckSeconds);
        if (_timed.Count == 0) return;

        // M2: ONE axis, already advanced by SampleTaskClock earlier in this same tick. The mode is
        // read for the LOG LINE only - the axis itself never accumulates across a mode change, so
        // handing the raw mode to Advance would re-anchor a second time for no gain (and, with an
        // unsteady reader, would re-anchor every walk and never serve anything: M3).
        bool preferSim = StallPolicy.ParseClockPreference(_vrf.TaskClock, out _);
        bool usingSim = _taskAxis.UsingSim;
        double clockNow = TaskClockSeconds;
        if (!_timedClockLineLogged || usingSim != _timedUsingSim)
        {
            _timedClockLineLogged = true;
            _timedUsingSim = usingSim;
            _log.LogInformation("TIMED COMPLETION (R4): {N} task(s) are timing out against the {Clock} clock" +
                                "{Why}; Vrf:DurationScale={Scale}.", _timed.Count,
                                usingSim ? "SIMULATION" : "WALL",
                                usingSim ? "" : (preferSim
                                    ? " - Vrf:TaskClock=sim, but the sim clock could not be read"
                                    : " (Vrf:TaskClock=wall)"),
                                _durationScale);
        }

        foreach (var p in _timed.Advance(clockNow, usingSim: true))
        {
            if (p.Kind == TimedCompletionPolicy.DueKind.OverdueAwaitingArrival)
            {
                // RL-20260921-09 (temporary position): a unit still travelling at its end time has
                // NOT completed. Nothing is reported and nothing is released here; the TASKCMPLT is
                // sent when it arrives (SynthesizeUnitCompletion -> MarkFinished -> EmitNow), and a
                // unit that is stuck instead is the stall watchdog's TASKABRT (RL-20260914-01) -
                // which this timer no longer hides. The follow-on's gate is told to keep waiting.
                _log.LogWarning("TIMED COMPLETION: task '{Task}' on {Unit} reached its END TIME - {Served:F0} s " +
                                "of a {Dur:F0} s Duration served on the {Clock} clock - but the unit has NOT " +
                                "ARRIVED: OVERDUE. No TASKCMPLT is sent now; it is sent the moment the unit " +
                                "arrives, and its follow-on tasks keep waiting until then (temporary position " +
                                "on completion, RL-20260921-09). A unit that is stuck is reported by the " +
                                "progress watchdog (Vrf:StallDetection={Stall}).",
                                p.TaskName, p.UnitName, p.Elapsed, p.DurationSeconds,
                                usingSim ? "simulation" : "wall", _vrf.StallDetection);
                _sequencer.NotifyOverdue(p.TaskUuid);
                continue;
            }
            _log.LogInformation("TIMED COMPLETION: task '{Task}' on {Unit} reached its END TIME - " +
                                "{Served:F0} s of a {Dur:F0} s Duration served on the {Clock} clock " +
                                "(C2SIM Duration x Vrf:DurationScale {Scale}). Completion is given by start " +
                                "time + Duration (temporary position, RL-20260921-09){Why}.",
                                p.TaskName, p.UnitName, p.Elapsed, p.DurationSeconds,
                                usingSim ? "simulation" : "wall", _durationScale,
                                p.HasDestination ? " - the unit had already arrived; its completion was held " +
                                                   "until now" : "");
            // m9 (cold-start review of 5c67d41): AN IN-PLACE TASK MUST RELEASE ITS UNIT. Only the
            // "hold-in-place" kind - the one that issues no vendor task, so no vendor completion
            // will ever pop the record - and only while it is STILL the unit's current task, so a
            // unit already re-tasked keeps its live record. Without this the unit stayed
            // _inFlight.IsBusy forever and PredecessorTimeoutPolicy=whenIdle would never dispatch
            // on it again. Every other kind is left alone: the vendor completion still owns it.
            if (_inFlight.TryGetCurrent(p.UnitName, out var cur)
                && string.Equals(cur.ExpectedKind, "hold-in-place", StringComparison.Ordinal)
                && _inFlight.TryCompleteIfCurrent(p.UnitName, p.TaskUuid, out _))
                _log.LogInformation("TIMED COMPLETION: {Unit} is idle again - its in-place task '{Task}' " +
                                    "issued no VR-Forces task, so nothing else would have popped it.",
                                    p.UnitName, p.TaskName);
            PushTaskStatus(p.TaskeeUuid, p.TaskUuid, S.TaskStatusCodeType.TASKCMPLT,
                           $"task '{p.TaskName}' reached the end time given by its C2SIM Duration " +
                           $"({p.DurationSeconds:F0} s after dispatch)");
            // The task is over: release its follow-ons' gate.
            _sequencer.CompleteTask(p.TaskUuid);
        }
    }

    /// <summary>
    /// Read the LIVE position of every materialized member of a unit (the entity itself for a
    /// platform). This is the ONE way the interface sees where a unit is, shared by C15
    /// (MaybeCheckArrivals) and C16 (MaybeCheckStalls) so the two policies can never drift onto
    /// different samples. Returns false when there is nothing to judge this tick - no VRF uuid
    /// yet, or an aggregate whose members have not materialized (a shell) - and the callers then
    /// skip the unit entirely, exactly as MaybeCheckArrivals always has. Tick thread only.
    /// </summary>
    private bool TryReadMemberPositions(string name, out Dictionary<string, (double Lat, double Lon)> positions,
                                        out int total)
    {
        positions = null;
        total = 0;
        if (!_names.TryGetUuid(name, out var vrfUuid)) return false;
        bool isAggregate = _c2SimUuidByName.TryGetValue(name, out var cu)
                           && _unitByC2SimUuid.TryGetValue(cu, out var created) && created.IsAggregate;
        positions = new Dictionary<string, (double Lat, double Lon)>(StringComparer.Ordinal);
        if (isAggregate)
        {
            var members = _bridge.GetAggregateMembers(vrfUuid);
            if (members is not { Count: > 0 }) return false;   // nothing readable yet (or a shell)
            // DE-DUPLICATION (review D1, 2026-09-13). VrfFacade::collectMembers recurses to depth 3
            // WITHOUT de-duplicating, so a member published under two sub-aggregates appears TWICE
            // in this list. The sample below is a DICTIONARY keyed by uuid - the duplicate lands in
            // ONE entry - so the count it is judged against must be of DISTINCT uuids too. Counting
            // members.Count would let the duplicate weigh against arrival while contributing a
            // single position: strictly harder than the pre-C16 sampler, which appended the
            // duplicate distance twice AND counted it twice (consistent on both sides of the
            // fraction). NOT positions.Count: a member whose position cannot be READ must still
            // count against arrival. A member with an empty uuid cannot be identified, so it can
            // neither be sampled nor de-duplicated and is left out of both sides.
            // ArrivalSelfTest carries the before/after decision table.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            total = members.Count(m => string.IsNullOrEmpty(m.Uuid) || seen.Add(m.Uuid));   // unreadable (empty-uuid) members still count; duplicates once
            foreach (var m in members)
            {
                if (string.IsNullOrEmpty(m.Uuid) || !_bridge.TryGetEntityGeodetic(m.Uuid, out var g)) continue;
                positions[m.Uuid] = (g.LatDeg, g.LonDeg);
            }
        }
        else
        {
            total = 1;
            if (_bridge.TryGetEntityGeodetic(vrfUuid, out var g)) positions[vrfUuid] = (g.LatDeg, g.LonDeg);
        }
        return true;
    }

    // PROGRESS WATCHDOG (C16, REPORT-ONLY; StallPolicy.cs; VrfSettings.Stall*; default OFF).
    // Tick thread, beside MaybeCheckArrivals. VR-Forces 5.2 never reports a unit that stops
    // making progress while its move task runs - the base give-up test "always returns false"
    // and the move-to script has no progress test (FINDING_EARLY_STOPS_2026-09-13 sec 6a) - so
    // the interface watches for it: keep a per-unit ring buffer of member positions covering
    // StallWindowSeconds on the clock Vrf:StallClock selects - by default the BACK END's own
    // simulation clock, VrfBridge.SimTimeSeconds() (wall seconds when it is set to "wall", or
    // whenever that reader has nothing to report) - and when NO member's NET displacement over
    // that window reaches StallMoveMeters, send ONE TaskStatus with TASKABRT for that task uuid.
    //
    // WHAT THIS PATH NEVER DOES (the user's "report-only" ruling of 2026-09-13): it issues no
    // VR-Forces command of any kind, does not re-task, does not pop the in-flight record, does
    // not release a sequencer gate and does not touch _pendingEngage. STP is told; the
    // simulation is left exactly as it was, so a vendor completion that does eventually arrive
    // still flows through OnVrfTaskCompleted unchanged. One report per unit-task, ever.
    private sealed class StallSamples
    {
        // Clock = seconds on whichever clock the watchdog is running on (see MaybeCheckStalls);
        // NOT a wall timestamp. StartClock is that clock when this task's watch opened, i.e. the
        // first check after the dispatch that called ClearStallState - it anchors the grace.
        public readonly List<(double Clock, Dictionary<string, (double Lat, double Lon)> P)> Ring = new();
        public double StartClock = double.NaN;
    }
    private readonly ConcurrentDictionary<string, StallSamples> _stallSamples = new();   // unit name -> position window
    private readonly ConcurrentDictionary<string, string> _stallReported = new();        // unit name -> task uuid reported TASKABRT
    private DateTime _nextStallCheck = DateTime.MinValue;
    private int _stallClockMode;   // 0 = not announced yet, 1 = simulation clock, 2 = wall clock
    // Clock-mode hysteresis and stale-clock detection (cold-start review of 1616614, findings 8
    // and 9) MOVED to SimClockTracker in the cold-start review of 5c67d41 (M3/M4): one observer,
    // consumed by this watchdog and by the R4 task clock alike, so the two can never disagree
    // about whether the scenario is running. What remains here is this watchdog's own reaction to
    // the shared verdict. All of it is touched only on the tick thread.
    private DateTime _stallModeLineUtc = DateTime.MinValue;       // rate limit for the mode line
    private int _stallModeLinesSuppressed;                        // mode changes the rate limit did not print
    private bool _stallClockConfigWarned;                         // Vrf:StallClock typo, logged once
    private bool _stallSimClockStaleWarned;                       // stale-clock warning, logged once per stall
    private double _stallLastCheckClock = double.NaN;             // previous check's clock, for the ratio
    private double _stallLastCheckWall = double.NaN;              // previous check's wall seconds
    // DORMANCY WATCH (pass-2 review F4b, re-armed by pass-3 review P4). 08146a2 armed the line on
    // ONE CAUSE - the cadence already at its 1 s floor with every ring below MinRingDepth - which
    // is the high-ratio cause only and is never even true under MODE THRASH, the measured case.
    // The arm is now the observable condition: nothing has satisfied JudgeReady while samples were
    // being taken. It is measured on the watchdog's OWN MONOTONE UN-JUDGED AXIS - the running sum
    // of each check's advance of whichever clock was in effect, never accumulated across a mode
    // change, because a scenario clock and DateTime.UtcNow's epoch are not comparable quantities.
    // The axis deliberately SURVIVES a mode change: a watchdog that keeps flipping modes is
    // exactly the thing this line exists to report.
    private double _stallUnjudgedClock;                           // the axis
    private double _stallJudgeableAtClock;                        // its value when a unit was last judgeable
    private DateTime _stallDormantLineUtc = DateTime.MinValue;    // rate limit for the line that says so
    private int _stallDormantLinesSuppressed;                     // warnings the rate limit did not print

    /// <summary>Drop a unit's watchdog state. Called wherever the arrival-evidence swallow is
    /// dropped - i.e. whenever a NEW VRF task is issued for the unit: the new task gets its own
    /// window and its own single report, and the old task's samples must not leak into it.</summary>
    private void ClearStallState(string unitName)
    {
        _stallReported.TryRemove(unitName, out _);
        _stallSamples.TryRemove(unitName, out _);
    }

    /// <summary>
    /// STP-822: IS THE VR-FORCES BACK END STILL THERE? Asked every Vrf:BackendLivenessSeconds on
    /// the WALL clock, from the tick loop, independent of the task clock and of whether anything
    /// is held on an end time - the two conditions that made the pre-STP-822 interface blind for
    /// a whole run. Three read-only vendor calls per sample, each guarded on its own; the rule is
    /// BackendLivenessPolicy's and is NEVER satisfied by one sample.
    ///
    /// ONLY A STATE CHANGE IS ACTED ON OR LOGGED. On a confirmed LOSS: every running task gets
    /// one TASKABRT through the B1 emit point and is abandoned so its STREND successors fail fast
    /// (the gate must not contradict the report just sent - the Q1 rule); ONE ObservationReport
    /// goes to STP; the R1 poll stops sending; C16 stands down and its rings are dropped so
    /// nothing is ever measured ACROSS the outage. On RECOVERY: one ObservationReport, reports
    /// resume - and NOTHING is re-tasked. Restarting a task nobody asked for would be the
    /// interface inventing an order.
    /// </summary>
    private void MaybeCheckBackendLiveness()
    {
        var now = DateTime.UtcNow;
        if (now < _nextLivenessCheck) return;
        _nextLivenessCheck = now.AddSeconds(Math.Max(1, _vrf.BackendLivenessSeconds));
        _liveness ??= new BackendLivenessMonitor(Math.Max(0, _vrf.BackendLossConfirmSeconds));

        // Each read guarded on its own, exactly as the task clock's stale branch guards them: one
        // that throws leaves ITS signal at "no reading" and the classifier decides on what is
        // left. A deployment carrying a VrfBridge that predates STP-809 throws
        // MissingMethodException on the two new members and still detects the loss on
        // BackendCount - the column WatchVrf --report-backends measured dropping 1 -> 0 exactly
        // 121 s after dispatch on both quiet runs. DEBUG, not WARNING: the task clock already
        // says the partial-deploy sentence once a minute (RUNBOOK sec 9) and this sampler must
        // not turn that into a second stream of the same news.
        int backendCount = -1, activeBackends = -1;
        int control = (int)StallPolicy.BackendControl.Unreadable;
        try { backendCount = _bridge.BackendCount(); }
        catch (Exception ex) { _log.LogDebug(ex, "LIVENESS: BackendCount read failed; no reading."); }
        try { activeBackends = ReadActiveBackendCount(); }
        catch (Exception ex) { _log.LogDebug(ex, "LIVENESS: ActiveBackendCount read failed; no reading."); }
        try { control = ReadBackendControlState(); }
        catch (Exception ex) { _log.LogDebug(ex, "LIVENESS: BackendControlState read failed; no reading."); }
        if (!Enum.IsDefined(typeof(StallPolicy.BackendControl), control))
            control = (int)StallPolicy.BackendControl.Other;   // a vendor value this build does not know

        double wallNow = now.Ticks / (double)TimeSpan.TicksPerSecond;
        var sample = new BackendLivenessPolicy.Sample(activeBackends, backendCount, control);
        var transition = _liveness.Observe(wallNow, sample);
        if (BackendLivenessPolicy.Classify(sample) == BackendLivenessPolicy.Reading.Present)
            _backendLastGoodUtc = now;
        if (transition == BackendLivenessPolicy.Transition.None) return;

        string active = activeBackends < 0 ? "unreadable" : activeBackends.ToString();
        if (transition == BackendLivenessPolicy.Transition.Loss)
        {
            _backendLost = true;
            double noStatus = _liveness.NoStatusSeconds(wallNow);
            string lastGoodIso = _backendLastGoodUtc == DateTime.MinValue
                               ? "(never)"
                               : _backendLastGoodUtc.ToString("yyyy-MM-ddTHH:mm:ssZ",
                                                             System.Globalization.CultureInfo.InvariantCulture);
            string why = BackendLivenessPolicy.LossReason(noStatus);
            var snapshot = _inFlight.Snapshot();
            _log.LogError("BACK END LOST: {Why}. Last good reading {Iso}; {N} task(s) in flight - each is "
                        + "reported TASKABRT and ABANDONED, position reports are SUSPENDED and the progress "
                        + "watchdog stands down until it reports again. Signals now: BackendCount={Count}, "
                        + "active={Active}, control={Control}. A back end that stops mid-run looks exactly "
                        + "like this, and on this stack a STOPPED 5.2d back end ALSO RUNS AWAY at "
                        + "~2.2 GB/min (V6_LIVE_JOIN_GATE sec 11.3), so treat a lost back end as a "
                        + "machine-safety event, not only a reporting one.",
                          why, lastGoodIso, snapshot.Count, backendCount, active,
                          (StallPolicy.BackendControl)control);
            foreach (var kv in snapshot)
            {
                var rec = kv.Value;
                string taskeeUuid = !string.IsNullOrEmpty(rec.TaskeeUuid) ? rec.TaskeeUuid
                                  : (_c2SimUuidByName.TryGetValue(kv.Key, out var cu) ? cu : "");
                PushTaskStatus(taskeeUuid, rec.TaskUuid ?? "", S.TaskStatusCodeType.TASKABRT, why);
                // Q1's rule, applied to a death instead of a supersede: a successor waiting on a
                // task we have just declared dead must fail NOW, not at the end of its window.
                _sequencer.NotifyAbandoned(rec.TaskUuid);
            }
            // 2026-09-25 (completion unit): a task whose unit FINISHED EARLY is no longer in flight -
            // its TASKCMPLT is held for its end time (RL-20260921-09) - so the snapshot above misses
            // it, and a dead back end would still get a TASKCMPLT at that end time. It is treated
            // like the in-flight set: TASKABRT (which cancels the held timer) and its follow-ons
            // abandoned.
            foreach (var held in _timed.HeldAfterFinish())
            {
                PushTaskStatus(held.TaskeeUuid, held.TaskUuid, S.TaskStatusCodeType.TASKABRT,
                               why + " (its unit had finished early and the task was waiting for its end time)");
                _sequencer.NotifyAbandoned(held.TaskUuid);
            }
            // NOTHING IS MEASURED ACROSS THE OUTAGE. C16's rings are stamped with positions from
            // before the loss; judged after it they would call every unit stalled - for a reason
            // that is not the taskee's.
            _stallSamples.Clear();
            _stallLastCheckClock = double.NaN;
            _positionCyclesSuppressed = 0;
            _livenessSuppressionSaid = false;
            _livenessStandDownSaid = false;
            _ = PushReportAsync(BackendLivenessPolicy.BuildStateChangeReport(
                    BackendLivenessPolicy.LossMarking(noStatus, lastGoodIso, snapshot.Count),
                    IsoNow(), NewReportId()), ReportKind.Observation);
            return;
        }

        _backendLost = false;
        double lostFor = _liveness.LostForSeconds(wallNow);
        _log.LogWarning("BACK END RECOVERED after {S:F0} s (BackendCount={Count}, active={Active}): position "
                      + "reports resume and the progress watchdog judges again. {N} position-report cycle(s) "
                      + "were suppressed. NOTHING IS RE-TASKED - the tasks aborted at the loss stay aborted; "
                      + "a new order is required (STP-822).",
                        lostFor, backendCount, active, _positionCyclesSuppressed);
        _ = PushReportAsync(BackendLivenessPolicy.BuildStateChangeReport(
                BackendLivenessPolicy.RecoveryMarking(lostFor, activeBackends),
                IsoNow(), NewReportId()), ReportKind.Observation);
    }

    private void MaybeCheckStalls()
    {
        var now = DateTime.UtcNow;
        if (now < _nextStallCheck) return;
        // STP-822: A BACK-END LOSS IS NOT A UNIT STALL. While the back end is gone every unit is
        // motionless for a reason this watchdog cannot see and does not own; judging here would
        // stamp a second, contradictory verdict on tasks the loss has already aborted and would
        // blame the taskee for the simulator. The rings were dropped at the loss, so nothing is
        // measured across the outage either.
        if (_backendLost)
        {
            _nextStallCheck = now.AddSeconds(Math.Max(1, _vrf.StallCheckSeconds));
            if (!_livenessStandDownSaid)
            {
                _livenessStandDownSaid = true;
                _log.LogWarning("STALL WATCHDOG: STANDING DOWN - the VR-Forces back end is LOST (STP-822). "
                              + "No unit is judged until it reports again; the tasks that were running have "
                              + "already been reported TASKABRT by the liveness check.");
            }
            return;
        }
        // Wall seconds as an absolute double on the SAME source the watchdog always used
        // (DateTime.UtcNow); only differences are ever taken, so the epoch is irrelevant.
        double wallNow = now.Ticks / (double)TimeSpan.TicksPerSecond;
        // Provisional. Replaced below by the cadence CLAMPED to the window actually in effect
        // (pass-2 review F4a), and in sim mode shortened again by the measured sim/wall ratio.
        _nextStallCheck = now.AddSeconds(Math.Max(1, _vrf.StallCheckSeconds));

        // CLOCK SELECTION (Vrf:StallClock, default "wall"). Everything MEASURED below - the window
        // and the post-dispatch grace - is in seconds on the clock chosen here.
        //
        // The sim reading is the BACK END's scenario clock, VrfBridge.SimTimeSeconds() ->
        // DtVrfRemoteController::simTime() (vrfRemoteController.h:355-356 on 5.2d): it runs fast
        // under fixed-frame-run-to-complete and STOPS while the scenario is paused. -1.0 = no
        // reading, and then the watchdog keeps its old wall-clock behaviour rather than going
        // blind. The VALUE is only validated here (review finding 7): anything that is not
        // exactly "sim" or "wall" is a configuration error and resolves to WALL, which is the
        // DEFAULT because it is the mode this interface has actually MEASURED LIVE so far. BOTH
        // windows are calibrated - 240 wall s and 360 sim s, from the same three replayed traces -
        // but only the wall clock has been exercised in a run. (The "RE-CALIBRATION OWED"
        // cross-reference that used to stand here was refuted by this same branch, which did the
        // re-calibration; pass-2 review F7.)
        bool preferSim = StallPolicy.ParseClockPreference(_vrf.StallClock, out bool clockValid);
        if (!clockValid && !_stallClockConfigWarned)
        {
            _stallClockConfigWarned = true;
            _log.LogWarning("Vrf:StallClock='{Value}' is neither \"sim\" nor \"wall\" - the progress watchdog " +
                            "measures its window on the WALL clock (the mode measured live so far).",
                            _vrf.StallClock);
        }
        // ONE OBSERVER (M3/M4 of the cold-start review of 5c67d41). The sim clock is read and
        // judged ONCE PER SAMPLE by SampleTaskClock -> SimClockTracker, at the top of the same tick
        // loop, and the watchdog consumes that observation instead of taking its own reading and
        // running its own copy of the hysteresis. Two consequences, both deliberate:
        //   - the watchdog and the R4 task clock can never disagree about whether the scenario is
        //     running (they may still be configured to different PREFERENCES);
        //   - the confirmations are now counted at the sampler's 1 s cadence rather than at this
        //     watchdog's (>= 1 s, adaptive), so a sustained change is adopted sooner. The guard is
        //     unchanged in kind: an alternating reader still never flips the mode.
        // NaN IS NOT A READING (pass-2 review F2) - SimClockTracker uses the same
        // StallPolicy.UsingSimClock predicate, so a NaN or +Inf reading is "no reading" there too.
        var simObs = _simClockLast;
        double simSeconds = simObs.SimSeconds;
        bool simReadable = preferSim && simObs.Readable;
        int observedMode = (preferSim && simObs.ReadableConfirmed) ? 1 : 2;
        int heldMode = _stallClockMode;
        bool announceMode = false;
        int nextMode = observedMode;
        if (nextMode != heldMode)
        {
            // Every ring is dropped on a real change: its stamps are on the OLD clock and mixing
            // the two would produce a nonsense window - better to re-open every watch than to
            // mis-abort one. The HYSTERESIS above (review finding 8) is what makes that safe: a
            // reader alternating -1 / >= 0 at the check cadence never reaches here, so it can no
            // longer wipe the watchdog's whole memory every tick while logging a line about it.
            // The line itself is rate-limited, and says how many changes it did not print.
            if (heldMode != 0)
            {
                _stallSamples.Clear();
                _stallSimClockStaleWarned = false;
                _stallLastCheckClock = double.NaN;
                // The un-judged axis is NOT reset here (pass-3 review P4): a mode that keeps
                // flipping drops every ring on every flip and is one of the ways the watchdog goes
                // silent, so the axis has to carry across the change that causes it. Setting
                // _stallLastCheckClock to NaN already makes THIS check contribute a zero advance,
                // which is the only part that would have been nonsense.
            }
            _stallClockMode = nextMode;
            announceMode = true;
        }

        // Measure on the HELD mode, not on the raw reading: while a change is still unconfirmed
        // the rings are stamped with the held clock and must be read on it. In sim mode with no
        // reading this tick there is nothing to measure at all, so the check is skipped whole -
        // the ring is preserved and picked up again when the reader answers.
        // (A mode CHANGE can never take this return: switching TO sim requires a reading >= 0,
        // and switching to wall leaves usingSim false - so no announcement is ever swallowed.)
        bool usingSim = _stallClockMode == 1;

        // THE WINDOW BELONGS TO THE CLOCK. Vrf:StallWindowSeconds = 0 - the shipped default - takes
        // the calibration derived for whichever clock is actually in use: 240 WALL seconds, or 360
        // SIM seconds from the 2026-09-13 sim-second re-calibration
        // (docs/experiments/RECAL_STALL_SIMSECONDS_2026-09-13.md). They are not a conversion of one
        // another - P11's ratio swings 1.10x-1.99x inside one run - so falling back to the wall
        // clock mid-run also falls back to the wall window, which is the correct pair.
        double window = StallPolicy.ResolveWindowSeconds(_vrf.StallWindowSeconds, usingSim);
        // ... AND THE CADENCE BELONGS TO THE WINDOW (pass-2 review F4a). MinRingDepth samples span
        // MinRingDepth - 1 intervals, so a configured cadence coarser than
        // window / (MinRingDepth - 1) can never fill the ring and NOTHING is ever judged - which
        // 1805ee3 did silently, on the DEFAULT wall path, where 51d78a5 still fired. Clamped, not
        // refused: the watchdog is report-only, so a mis-set knob must not stop a run, and the
        // clamp restores the 51d78a5 outcome. The 0c pre-flight says so once, at start-up.
        int checkSeconds = StallPolicy.ClampCheckSeconds(_vrf.StallCheckSeconds, window);
        _nextStallCheck = now.AddSeconds(checkSeconds);
        if (usingSim && !simReadable) return;
        double clockNow = StallPolicy.SelectClock(usingSim, simSeconds, wallNow);
        if (announceMode)
        {
            // ONE line per real mode change, rate-limited (a change drops every ring, so it has to
            // be visible, but an unsteady reader must not be able to fill the log with it); the
            // line says how many changes it did not print.
            // (pass-2 review N2: the STATUS lines have their own rate-limit constant now - reusing
            // StaleClockWarnSeconds here tied this log rate to the stale-clock threshold.)
            if ((now - _stallModeLineUtc).TotalSeconds >= StallPolicy.LogRateLimitSeconds)
            {
                _log.LogInformation("STALL WATCHDOG: the {W} s no-progress window is measured on the {Clock} clock{Why}.{Sup}",
                                    (int)window, usingSim ? "SIMULATION" : "WALL",
                                    usingSim ? "" : (preferSim
                                        ? " - Vrf:StallClock=sim, but the sim clock could not be read (no back end reporting)"
                                        : " (Vrf:StallClock=wall)"),
                                    _stallModeLinesSuppressed > 0
                                        ? " (" + _stallModeLinesSuppressed + " earlier mode change(s) not logged)" : "");
                _stallModeLineUtc = now;
                _stallModeLinesSuppressed = 0;
            }
            else _stallModeLinesSuppressed++;
        }

        // CADENCE (review finding 4). The cadence stays a WALL sampling rate, but on the sim clock
        // it also sets the ring's RESOLUTION: at ratio r one 5 s step advances the window by 5r
        // sim seconds, and past r = window / (MinRingDepth x cadence) the front prune leaves only
        // the two entries it must keep, so the ring-depth floor could never be cleared. The
        // cadence therefore follows the clock - never slower than configured, never faster than 1 s.
        double appliedCadence = checkSeconds;
        if (usingSim && !double.IsNaN(_stallLastCheckClock) && wallNow > _stallLastCheckWall)
        {
            double ratio = (clockNow - _stallLastCheckClock) / (wallNow - _stallLastCheckWall);
            appliedCadence = StallPolicy.NextCheckSeconds(checkSeconds, window, ratio);
            _nextStallCheck = now.AddSeconds(appliedCadence);
        }
        // The advance of the clock in effect since the previous SAMPLING check, for the dormancy
        // axis (pass-3 review P4). Zero across a mode change - _stallLastCheckClock is NaN there -
        // and zero on a backwards step, so the axis is monotone whatever the reader does.
        double clockAdvance = (!double.IsNaN(_stallLastCheckClock) && clockNow > _stallLastCheckClock)
                            ? clockNow - _stallLastCheckClock : 0.0;
        _stallLastCheckClock = clockNow;
        _stallLastCheckWall = wallNow;

        var snapshot = _inFlight.Snapshot();
        var live = new HashSet<string>(StringComparer.Ordinal);
        bool anyMoveInFlight = false;
        // MOVE tasks only. A task with no destination (engage, breach, fire in place) has no
        // progress to make and standing still IS its correct behaviour.
        foreach (var kv in snapshot)
            if (kv.Value.DestLat is not null && kv.Value.DestLon is not null)
            { live.Add(kv.Key); anyMoveInFlight = true; }

        // STALE (not paused) SIM CLOCK - review finding 9. VrfFacade::SimTimeSeconds gates on
        // backends().count() > 0, and a back end that misses its status timeout is DEACTIVATED,
        // not removed (vrfBackendListener.h:161-163 against :154-155), so count() stays > 0 and
        // the reader returns its last cached value forever. That is indistinguishable from a
        // paused scenario, and both must stop the watchdog judging - but a silent stop in front
        // of the silence C16 exists to catch is not acceptable, so it says so, once.
        //
        // A BACKWARDS STEP IS A CHANGE, NOT A NON-ADVANCE (pass-2 review F3). The "last advanced"
        // mark used to be a HIGH-WATER one compared with `>`, so after DtVrfRemoteController::
        // rollbackToSnapshot the clock was genuinely advancing BELOW that mark, the advance branch
        // was never taken, and 60 wall s later the watchdog suspended judging for EVERY unit until
        // the clock climbed back - measured 420 wall s for a 475 sim s rollback - while telling
        // the operator the scenario was paused or the back end had stopped answering. That rule,
        // and the mark it keeps, now live in SimClockTracker (M3/M4 of the cold-start review of
        // 5c67d41), which decides Advanced / RolledBack / Flat and Stale ONCE for every consumer;
        // Admit still handles a rollback correctly and explicitly on this side.
        bool staleHold = false;
        if (usingSim)
        {
            // STALE (not PAUSED) is decided ONCE, by SimClockTracker, for every consumer; the
            // rollback line is reported there too, because a backwards step is a fact about the
            // clock and not about this watchdog. What stays here is this watchdog's REACTION:
            // suspend judging while the clock is stale AND something is actually moving.
            if (anyMoveInFlight && simObs.Stale)
            {
                staleHold = true;
                if (!_stallSimClockStaleWarned)
                {
                    _stallSimClockStaleWarned = true;
                    _log.LogWarning("STALL WATCHDOG: the simulation clock has not advanced past {T:F1} s for {S:F0} " +
                                    "wall seconds while {N} move task(s) are in flight - the scenario is PAUSED, or " +
                                    "the back end has stopped answering (a back end that misses its status timeout " +
                                    "is deactivated, not removed, so the reader keeps returning its last value). " +
                                    "No unit is judged until this clock moves again.",
                                    clockNow, simObs.FlatForWallSeconds, live.Count);
                }
            }
            else if (_stallSimClockStaleWarned && !simObs.Stale)
            {
                _stallSimClockStaleWarned = false;
                _log.LogInformation("STALL WATCHDOG: the simulation clock is advancing again ({T:F1} s); " +
                                    "the no-progress window is being measured once more.", clockNow);
            }
        }

        int deepestRing = 0;
        bool anySampled = false;
        bool anyJudgeable = false;
        if (!staleHold)
        foreach (var kv in snapshot)
        {
            string name = kv.Key;
            var rec = kv.Value;
            if (rec.DestLat is null || rec.DestLon is null) continue;
            // ONE REPORT PER UNIT-TASK (pass-2 review F6). The map is documented as "unit name ->
            // task uuid reported TASKABRT" and the C16 header promises one report per unit-TASK,
            // but 1805ee3 tested ContainsKey(name) and never compared the uuid - so a unit that
            // stalled once and was then RE-TASKED went unwatched for the rest of its life in the
            // in-flight set (the bottom-of-method prune only drops units that have LEFT that set,
            // and a re-tasked unit is live). That is exactly the failure mode the watchdog exists
            // for: MarkDispatched now drops the SAMPLES on a re-task, so without this the flag was
            // the only thing left pinning the unit shut.
            if (_stallReported.TryGetValue(name, out var reportedFor)
                && reportedFor == (rec.TaskUuid ?? "")) continue;   // already reported for THIS task
            if (_arrivalReported.ContainsKey(name)) continue;   // C15 already reported it complete
            if (!TryReadMemberPositions(name, out var positions, out int total)) continue;

            var samples = _stallSamples.GetOrAdd(name, _ => new StallSamples());
            var ring = samples.Ring;
            // The watch opens at the first sample after the dispatch that cleared this unit's
            // state, so StartClock is the dispatch anchor on the selected clock (within one
            // StallCheckSeconds, plus however long the members took to reflect). StallPolicy.Admit
            // owns the ring: it appends, REPLACES a sample that did not advance the clock (a
            // paused scenario, or a status period coarser than the cadence), drops everything
            // from an abandoned timeline on a snapshot rollback and re-arms the watch there, and
            // then front-prunes to one sample at or before the window edge.
            samples.StartClock = StallPolicy.Admit(ring, clockNow, positions, window, samples.StartClock);
            // DEFENCE IN DEPTH AT THE CRASH POINT (pass-3 review P1). Admit REFUSES a non-finite
            // clock, so on a unit's first check it can return with the ring still EMPTY - and
            // 08146a2 indexed ring[0] on the very next line. The predicate above (UsingSimClock,
            // which now requires double.IsFinite) is what makes that unreachable; this is the
            // brace to that belt, because MaybeCheckStalls is called bare from TickLoop and an
            // unhandled exception on the vrf-tick thread terminates the interface process.
            if (ring.Count == 0) continue;
            if (ring.Count > deepestRing) deepestRing = ring.Count;
            anySampled = true;

            var oldest = ring[0];
            // Grace + full window on the selected clock, AND the floors that clock cannot supply
            // (review findings 3 and 4): MinRingDepth samples inside the window - on BOTH clocks -
            // and, on the WALL clock only, StallMinSecondsSinceDispatch of WALL time since THIS
            // record's own dispatch (the anchor 51d78a5 used and 1616614 dropped). Never judge on
            // a partial window: a unit 60 s into its watch has only 60 s of history, and 50 m over
            // 60 s is a different (much stricter) test than 50 m over 240.
            // The wall floor is NOT ANDed on the sim clock (pass-2 review F8): 360 sim s is ~58
            // wall s at G5's 6.21x, so above ratio ~6x the floor - not the calibrated window - set
            // the detection time (measured at 60x: wall 60 s / sim 3,600 s), negating the "fires
            // EARLIER in wall time" property the 360 s default was chosen for. MinRingDepth, which
            // 51d78a5 did not have, covers the two-sample case the floor incidentally guarded.
            if (!StallPolicy.JudgeReady(clockNow, oldest.Clock, samples.StartClock, window,
                                        _vrf.StallMinSecondsSinceDispatch, ring.Count,
                                        (now - rec.DispatchedUtc).TotalSeconds,
                                        applyWallFloor: !usingSim)) continue;
            anyJudgeable = true;   // at least one unit reached the gate - the watchdog is not dormant

            var displacements = new List<double>();
            foreach (var cur in positions)
                if (oldest.P.TryGetValue(cur.Key, out var was))
                    displacements.Add(TerrainVertexAuthoring.DistMeters(was.Lat, was.Lon, cur.Value.Lat, cur.Value.Lon));
            var d = StallPolicy.Decide(displacements, total, _vrf.StallMoveMeters, _vrf.StallMinMembersWithData);
            if (!d.Stalled) continue;

            _stallReported[name] = rec.TaskUuid ?? "";
            _log.LogInformation("STALL: unit {Name} task {Task}: no member moved more than {M:F0} m in the last {W} " +
                                "{Clock} s (max {Max:F1} m); TASKABRT reported.",
                                name, rec.TaskName, _vrf.StallMoveMeters, (int)window,
                                _stallClockMode == 1 ? "SIM" : "wall", d.MaxMeters);
            if (!_c2SimUuidByName.TryGetValue(name, out var taskeeUuid))
            {
                _log.LogWarning("STALL for '{Name}' but no C2SIM uuid known - no TASKABRT report sent.", name);
                continue;
            }
            // REPORT-ONLY for the task itself: it stays in flight, its armed end time is NOT cancelled
            // (reportOnlyAbort), and a unit that later arrives still reports TASKCMPLT. Before the
            // completion unit of 2026-09-25 this abort was usually SUPPRESSED - the Duration timer had
            // already reported TASKCMPLT for the unarrived unit - and it released nothing.
            PushTaskStatus(taskeeUuid, rec.TaskUuid ?? "", S.TaskStatusCodeType.TASKABRT,
                           "STALLED (C16 progress watchdog) - report only: the task stays in flight, no " +
                           "VR-Forces command is issued and nothing is re-tasked; its follow-on tasks are " +
                           "abandoned", reportOnlyAbort: true);
            // D2 (the owner, 2026-09-25, RL-20260925-01): the follow-ons of a unit reported STUCK are
            // ABANDONED, each with its own TASKABRT, as every other abort does (supersede, back-end
            // loss, vendor failure). Its chain is closed; its own task may still complete later.
            _sequencer.NotifyAbandoned(rec.TaskUuid);
        }
        // SILENT DORMANCY, SAID OUT LOUD - ON THE CONDITION, NOT ON ONE CAUSE (pass-2 review F4b,
        // re-armed by pass-3 review P4). 08146a2 armed this line only when the cadence was already
        // at its 1 s floor AND every sampled ring was below MinRingDepth - i.e. on the HIGH-RATIO
        // cause alone. Measured, that misses MODE THRASH: a reader out for ModeSwitchConfirmations
        // or more CONSECUTIVE checks - the deactivated-back-end shape this watchdog exists to
        // survive - flips the mode for real, every flip drops every ring and swaps the window
        // 360 <-> 240, and at 1.5x the cadence never leaves its configured value, so the arming
        // condition is never true. 3 consecutive misses in every 10 reads at 1.5x with a frozen
        // unit for 3,000 wall s produced 0 verdicts, 0 judgeable checks, 40 mode lines and not one
        // word that no unit was being watched. A reader that keeps going out, and units re-tasked
        // faster than one window, have the same signature.
        //
        // The arm is now the OBSERVABLE condition - nothing satisfied JudgeReady while samples were
        // being taken - on the watchdog's own monotone un-judged axis (see the field comments), and
        // DormancyWindows whole windows, not one: the first window is the minimum any ring must
        // span before a verdict is even possible, so one window of silence is ordinary start-up.
        // The high-ratio explanation survives as a HINT when the cadence is at its floor, where it
        // is the likely cause. Rate-limited like the mode line, with the count it did not print.
        if (!anySampled)
        {
            // Nothing was there to judge: no move task in flight, a stale-clock hold or a sim-clock
            // blackout - and the last two announce themselves. The axis does not advance and the
            // watch re-arms, so a quiet stretch can never accumulate into a false dormancy line.
            _stallJudgeableAtClock = _stallUnjudgedClock;
        }
        else
        {
            _stallUnjudgedClock += clockAdvance;
            if (anyJudgeable) _stallJudgeableAtClock = _stallUnjudgedClock;
            else if (StallPolicy.DormancyDue(_stallJudgeableAtClock, _stallUnjudgedClock, window,
                                             samplingActive: true))
            {
                if ((now - _stallDormantLineUtc).TotalSeconds >= StallPolicy.LogRateLimitSeconds)
                {
                    _stallDormantLineUtc = now;
                    _log.LogWarning("STALL WATCHDOG: NO UNIT IS BEING JUDGED. Nothing has satisfied the window " +
                                    "gate for {N} whole {W} s windows of the {Clock} clock while samples were " +
                                    "being taken (deepest sample ring {Deep}, {Min} required; sampling cadence " +
                                    "{C:F1} s).{Hint} Causes with this signature: a sim/wall ratio too high for " +
                                    "the ring to fill, a clock mode that keeps flipping (every change drops every " +
                                    "ring), a position reader that keeps going out, or units re-tasked faster than " +
                                    "one window. Raise Vrf:StallWindowSeconds, or slow the scenario down.{Sup}",
                                    StallPolicy.DormancyWindows, (int)window,
                                    usingSim ? "SIMULATION" : "WALL",
                                    deepestRing, StallPolicy.MinRingDepth, appliedCadence,
                                    appliedCadence <= 1.0
                                        ? " The sampling cadence is already at its 1 s floor, so the sim/wall "
                                          + "ratio is the likely cause: the offline model puts the boundary where "
                                          + "the ring stops filling between 150x and 200x (the analytic ceiling, "
                                          + "window / ((MinRingDepth - 1) x 1 s) = "
                                          + StallPolicy.MaxCheckSeconds(window) + "x, is conservative against it)."
                                        : "",
                                    _stallDormantLinesSuppressed > 0
                                        ? " (" + _stallDormantLinesSuppressed
                                          + " earlier dormancy warning(s) not logged)" : "");
                    _stallDormantLinesSuppressed = 0;
                }
                else _stallDormantLinesSuppressed++;
            }
        }

        // Units whose task is no longer in flight (completed, superseded, or never a move) keep no
        // window: the buffer must not grow across a whole run. The one-report flag is pruned with
        // it (review D4): a unit that has left the in-flight set has no task left to report
        // against, the next dispatch clears the flag anyway (ClearStallState), and leaving it
        // behind would pin a dead unit name in the map for the life of the process.
        foreach (var key in _stallSamples.Keys)
            if (!live.Contains(key)) _stallSamples.TryRemove(key, out _);
        foreach (var key in _stallReported.Keys)
            if (!live.Contains(key)) _stallReported.TryRemove(key, out _);
        // STP-837's two maps ride the same prune, for the same reason: a unit that has left the
        // in-flight set has no task left to measure travel for, and the next dispatch rewrites
        // its baseline anyway.
        foreach (var key in _dispatchPositions.Keys)
            if (!live.Contains(key)) _dispatchPositions.TryRemove(key, out _);
        foreach (var key in _arrivalNotClosable.Keys)
            if (!live.Contains(key)) _arrivalNotClosable.TryRemove(key, out _);
    }

    private void OnVrfTaskCompleted(object sender, TaskCompletedEventArgs e)
    {
        // B3: the callback carries the marking the SIM holds, which for a platform is the TRUNCATED
        // one - resolve it to the name we requested before any map is touched, or the completion is
        // unattributable ("Task-complete for 'X' but no C2SIM uuid known - no report sent"). A
        // fan-out MEMBER name was never requested, so it passes through unchanged.
        string marking = _names.Resolve(e.UnitMarking ?? "");
        // DID THE TASK SUCCEED? The vendor's report carries success() - "success being false
        // indicates that the task has failed and is no longer being processed"
        // (vrforces5.2d/include/vrftasks/taskCompleteReport.h:84-90), and the vendor DEFAULTS it
        // to true (:87), so a report that never carries the flag still reads as a success.
        // WIRED 2026-09-14 on feat/integration: VrfFacade reads report->success() into
        // TaskCompleted::success, VrfBridge raises it as TaskCompletedEventArgs.Success, and it
        // arrives here. Until this line the whole vendor-failure path below was dead code and a
        // FAILED task was reported to STP as TASKCMPLT. Evidence that it matters - run G2
        // (docs/experiments/READ_G2_1-6_MESH_STOP_2026-09-14.md): 1-6's leader printed "Entity not
        // embarked on same object as target [%1]. Ending task Route 54", then "Controller ...
        // maneuver-in-formation task has Failed" at sim 320.4, the unit was re-formed under
        // another leader, and the interface reported NOTHING for nine hours. What false now does
        // is TaskStatusPolicy.CodeForCompletion(success, taskContinues) -> TASKABRT, no successor
        // release, no parked engage (SynthesizeUnitCompletion; --report-selftest covers both).
        bool success = e.Success;
        _log.LogInformation("VRF task complete: {Unit} / {Task} (success={Ok})", marking, e.TaskType, success);
        // A vendor completion for a task already reported from arrival evidence: swallow it ONCE
        // (VR-Forces runs one task at a time and a re-task abandons the old one without a
        // callback, so this can only be the pre-empted task's own late completion).
        // C16: drop the watchdog window for whatever this marking names. On the unit-level path
        // the marking IS the unit; under R10 fan-out it is a MEMBER entity's name and this clear is
        // a no-op (review D3) - the unit's own window is dropped in SynthesizeUnitCompletion, which
        // every completion path reaches under the UNIT's name.
        if (!string.IsNullOrEmpty(marking)) ClearStallState(marking);
        if (!string.IsNullOrEmpty(marking) && _arrivalReported.TryRemove(marking, out var reportedTask))
        {
            _log.LogInformation("VRF completion for {Unit} after the arrival-evidence report of task {Task} - swallowed.",
                                marking, reportedTask);
            return;
        }

        // Port of executeTask's TASKCMPLT emit (C2SIMinterface.cpp:2435), triggered here by
        // the completion callback instead of a busy-wait. Resolve the marking -> taskee
        // C2SIM uuid, attribute the completion to the unit's IN-FLIGHT task (P0.1 - the
        // callback carries no task uuid, and the old last-write map misattributed it to
        // whatever was dispatched last), then push a TaskStatus (TASKCMPLT) report.
        string name = marking;

        // R10: a fanned-out aggregate move completes PER MEMBER (the marking is the member
        // entity's name). Aggregate them; only when the QUORUM is met does the UNIT's
        // completion flow (SynthesizeUnitCompletion) run, under the unit's name. Late
        // stragglers arriving after a quorum/timeout synthesis are SWALLOWED here (they must
        // NOT fall through to the unit-level path, which would emit a spurious empty-uuid
        // TASKCMPLT - the "NO in-flight task recorded" bug this step removes).
        if (_fanOut.TryCompleteMember(name, success, out var fanUnit, out _, out int fanRemaining,
                                      out bool fanAllDone, out bool fanAlreadySynthesized,
                                      out bool fanAnyFailed))
        {
            if (fanAlreadySynthesized)
            {
                _log.LogDebug("R10 fan-out: late straggler {Member} of {Unit} after synthesis - swallowed.",
                              name, fanUnit);
                return;
            }
            if (!fanAllDone)
            {
                _log.LogInformation("R10 fan-out: member {Member} of {Unit} completed; {N} member(s) remaining.",
                                    name, fanUnit, fanRemaining);
                return;
            }
            // m8: the unit's outcome is the AND of its members'. Before this, only the member
            // whose completion happened to MEET the quorum decided the unit's code, so members
            // 1..n-1 reporting success=false were counted as completions and reported TASKCMPLT.
            _log.LogInformation("R10 fan-out: completion quorum reached for {Unit} ({N} straggler(s) will be " +
                                "swallowed) - synthesizing the unit's task completion{Failed}.",
                                fanUnit, fanRemaining,
                                fanAnyFailed ? " as a FAILURE (at least one member's task failed)" : "");
            SynthesizeUnitCompletion(fanUnit, e.TaskType, success && !fanAnyFailed);
            return;
        }

        // Normal (non-fanned) unit-level completion.
        SynthesizeUnitCompletion(name, e.TaskType, success);
    }

    /// <summary>
    /// Emit the unit-level TASKCMPLT (the factored tail of OnVrfTaskCompleted). Called from the
    /// completion-callback quorum branch AND from the straggler timer, so it must be safe OFF
    /// the tick thread: _inFlight / _sequencer / _c2SimUuidByName / _pendingEngage are all
    /// thread-safe, PushReportAsync is fire-and-forget, and the ONE side effect that touches the
    /// bridge (a deferred engage) goes through IssueEngage, which ENQUEUES on _tickActions - it
    /// does NOT call _bridge.* directly. INVARIANT: keep this method free of any direct _bridge.*
    /// call (plan 2.10); a future bridge action here MUST route through _tickActions.Enqueue.
    /// Double-fire safety: _inFlight.TryComplete REMOVES the in-flight record, and the tracker's
    /// Synthesized flag blocks the second trigger - so only ONE of {quorum, timeout} ever reaches
    /// here for a given task.
    /// </summary>
    /// <param name="success">What VR-Forces said about the task: false = "the task has FAILED and
    /// is no longer being processed" (DtTaskCompleteReport::success(), vrftasks/taskCompleteReport.h
    /// :84-90). A failure reports TASKABRT instead of TASKCMPLT, does NOT release the successors'
    /// gate (it abandons them, so they fail fast instead of waiting out the predecessor timeout) and
    /// does NOT fire the engage parked on the move. Every non-vendor path here is a success by
    /// construction (arrival evidence, fan-out quorum, straggler timer).</param>
    private void SynthesizeUnitCompletion(string name, string vrfTaskTypeForLog, bool success = true)
    {
        // C16 (review D3): the unit's task is over on EVERY path that reaches here - vendor
        // completion, R10 fan-out quorum, fan-out straggler timeout, arrival evidence - so drop
        // its progress window and its one-report flag under the UNIT's name. The R10 paths never
        // clear otherwise: OnVrfTaskCompleted keys its clear by e.UnitMarking, which under fan-out
        // is the MEMBER entity name. NOTE this is deliberately NOT a suppressor: a unit that has
        // already reported TASKABRT and then arrives still sends TASKCMPLT (abort-then-complete - a
        // supervisor position (RL-20260914-01 covers the code only)).
        ClearStallState(name);

        if (!_c2SimUuidByName.TryGetValue(name, out var taskeeUuid))
        {
            _log.LogWarning("Task-complete for '{Name}' but no C2SIM uuid known - no report sent.", name);
            return;
        }

        string taskUuid = null;
        if (_inFlight.TryComplete(name, out var fin))
        {
            taskUuid = fin.TaskUuid;
            if (!InFlightTracker.KindLooksRight(fin.ExpectedKind, vrfTaskTypeForLog))
                _log.LogWarning("Unit {Name}: completed VRF task type '{VrfType}' does not look like the " +
                                "dispatched kind '{Kind}' (task '{Task}') - attribution anomaly; still " +
                                "attributed by the in-flight record.",
                                name, vrfTaskTypeForLog, fin.ExpectedKind, fin.TaskName);
        }
        else
            _log.LogWarning("Task-complete for '{Name}' with NO in-flight task recorded - unattributed " +
                            "(report sent with empty task uuid).", name);

        // THE TEMPORARY POSITION ON COMPLETION (RL-20260921-09): a SUCCESS on a task whose end time
        // is armed is HELD until that end time (the timer then reports it and releases the
        // follow-ons), or - if the task was already OVERDUE - reported complete NOW. A failure is
        // never marked: it is an immediate TASKABRT whatever the timer says. With no armed timer
        // (no Duration, Vrf:TimedCompletion off, or the task already ended) the verdict is
        // NotTimed and everything below is exactly the evidence-only completion it was before.
        var verdict = success && taskUuid != null
                    ? _timed.MarkFinished(taskUuid) : TimedCompletionPolicy.FinishVerdict.NotTimed;
        if (verdict == TimedCompletionPolicy.FinishVerdict.Hold)
            _log.LogInformation("Unit {Name}: task '{Task}' FINISHED BEFORE ITS END TIME - the TASKCMPLT and the " +
                                "release of its follow-on tasks are HELD until start time + Duration " +
                                "(temporary position on completion, RL-20260921-09).",
                                name, fin.TaskName ?? taskUuid);
        else if (verdict == TimedCompletionPolicy.FinishVerdict.EmitNow)
            _log.LogInformation("Unit {Name}: task '{Task}' was OVERDUE and the unit has now ARRIVED - reported " +
                                "complete now, and its follow-on tasks are released (RL-20260921-09).",
                                name, fin.TaskName ?? taskUuid);

        // Release any task gated on this one (parity: setTaskIsComplete unblocked the C++
        // busy-wait on getTaskIsComplete; here it completes the successor's await). Only
        // the ATTRIBUTED task's gate releases - a superseded task's gate stays closed.
        if (TimedCompletionPolicy.ReleasesSuccessorsNow(success, verdict)) _sequencer.CompleteTask(taskUuid);
        else if (!success) _sequencer.NotifyAbandoned(taskUuid);   // a FAILED task never completes: successors fail fast

        // P0.3: the move completed - issue the engage that was parked on it (advance the
        // axis / approach the obstacle, THEN engage/breach - now for real, not same-tick).
        // taskUuid != null (not IsNullOrEmpty): an ATTRIBUTED task with an empty uuid must
        // still match its engage; only an UNATTRIBUTED completion (null) skips this.
        bool taskContinues = false;
        if (success && taskUuid != null && _pendingEngage.TryGetValue(name, out var eng)
            && eng.MoveTaskUuid == taskUuid
            && _pendingEngage.TryRemove(new KeyValuePair<string, PendingEngage>(name, eng)))
        {
            _log.LogInformation("Unit {Name}: move for task '{Task}' completed; issuing the deferred {Kind}.",
                                name, eng.TaskName, eng.Kind);
            IssueEngage(name, eng);
            // Review finding 4: ONE C2SIM task, TWO VR-Forces tasks. The engage has just been
            // re-recorded as this unit's in-flight task under the SAME task uuid, so the C2SIM task
            // is NOT over - reporting TASKCMPLT here would both mislead STP (the attack has not
            // happened yet) and consume the task's single completion slot, silently suppressing the
            // engage's own TASKCMPLT when it finally arrives. Report progress instead.
            taskContinues = true;
        }

        // THE CODE, from the same helper the offline flow uses (--rulings-selftest t10). Held: the
        // move half of an advance-then-engage task still says TASKINPRG, anything else says nothing
        // until the end time. Overdue and now arrived: TASKCMPLT - and for an ATTACK / BREACH the
        // parked engage above has STILL been issued (the owner's decision of 2026-09-25,
        // RL-20260925-01).
        var maybeCode = TimedCompletionPolicy.CompletionCode(success, taskContinues, verdict);
        if (maybeCode is not S.TaskStatusCodeType code) return;
        PushTaskStatus(taskeeUuid, taskUuid ?? "", code,
                       !success ? $"unit {name}: VR-Forces reported the task FAILED (success=false) - it is no " +
                                  "longer being processed"
                       : verdict == TimedCompletionPolicy.FinishVerdict.EmitNow
                           ? $"unit {name} arrived after its task's end time (start time + Duration) - complete on " +
                             "arrival" + (taskContinues ? "; the deferred engage has been issued" : "")
                       : taskContinues ? $"unit {name} completed the MOVE half of its task; the deferred engage " +
                                         "is now in flight under the same task - TASKCMPLT follows " +
                                         (verdict == TimedCompletionPolicy.FinishVerdict.Hold
                                              ? "at the task's end time (start time + Duration)" : "when it ends")
                       : $"unit {name} completed its task");
    }

    private void OnVrfTextReport(object sender, TextReportEventArgs e)
    {
        _log.LogDebug("VRF text-report: {Text}", e.Text);

        // Port of onTextReport's POSITION path (textIf.cxx:1029-1085): the Lua tracking
        // script emits `POSITION "entity name" <latDeg> <lonDeg>`. Parse it, resolve the
        // name -> C2SIM uuid, and push a PositionReport. (Aggregate-component de-dup and
        // multi-content bundling - textIf.cxx:1046-1066 - are deferred; each POSITION line
        // emits one report here. Non-POSITION text is ignored, as in the C++.)
        if (!TryParsePosition(e.Text, out var objectName, out double lat, out double lon))
            return;
        objectName = _names.Resolve(objectName);   // B3: the Lua line carries the sim's (truncated) marking
        if (!_c2SimUuidByName.TryGetValue(objectName, out var uuid))
        {
            // Not one of our units (e.g. an aggregate subordinate) - the C++ returns here too.
            _log.LogDebug("POSITION for unknown/uncreated '{Name}' - ignored.", objectName);
            return;
        }

        // P4b (opt-in): accumulate the fix into the bundle and flush on the count (or size) trigger;
        // the periodic timer + stop path cover the partial-bundle cases. TASKCMPLT is NEVER bundled
        // (separate path). When BundlePositionReports is false, fall through to EXACTLY today's
        // single-report path below (byte-for-byte parity - the default-off invariant).
        if (_vrf.BundlePositionReports)
        {
            List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)> snapshot = null;
            lock (_posBundleLock)
            {
                // No heading/speed here: a POSITION text report carries lat/lon ONLY (the C++
                // strtok parse below), so both are null and the elements are omitted. The R1
                // poll is the path that reads kinematics from the reflected object.
                _posBundle.Add((uuid, lat, lon, null, null));
                if (_posBundle.Count >= _vrf.BundleMaxReports ||
                    EstimatedBundleBytesLocked() >= _vrf.BundleMaxBytes)
                    snapshot = DrainBundleLocked();
            }
            _log.LogDebug("Position fix for {Name} ({Uuid}) {Lat}/{Lon} {State}.",
                          objectName, uuid, lat, lon, snapshot == null ? "buffered" : "flushing bundle");
            if (snapshot != null) _ = PushBundleSnapshot(snapshot);
            return;
        }

        var report = ReportBuilder.BuildPositionReport(uuid, lat, lon, IsoNow(), NewReportId());
        _log.LogDebug("Position report for {Name} ({Uuid}) {Lat}/{Lon}.", objectName, uuid, lat, lon);
        _ = PushReportAsync(report);
    }

    // Parse `POSITION "entity name" <latDeg> <lonDeg>` (faithful to the C++ strtok parse,
    // textIf.cxx:1029-1036: keyword, then the quoted name, then two space-separated numbers).
    private static bool TryParsePosition(string text, out string name, out double lat, out double lon)
    {
        name = ""; lat = 0; lon = 0;
        if (string.IsNullOrEmpty(text)) return false;
        text = text.Trim();
        if (!text.StartsWith("POSITION", StringComparison.Ordinal)) return false;
        int q1 = text.IndexOf('"');
        int q2 = q1 >= 0 ? text.IndexOf('"', q1 + 1) : -1;
        if (q1 < 0 || q2 < 0) return false;
        name = text.Substring(q1 + 1, q2 - q1 - 1);
        var rest = text.Substring(q2 + 1)
                       .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (rest.Length < 2) return false;
        return double.TryParse(rest[0], System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out lat)
            && double.TryParse(rest[1], System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out lon);
    }

    /// <summary>
    /// SUPERSEDED (retained as the record of the E1 static analysis): the per-DIS-type
    /// formation-name map derived from the .entity files. The R5 live read-backs proved
    /// static analysis UNRELIABLE - the runtime lists are all lowercase even where the
    /// files say Title-Case - so the auto path now QUERIES each unit's own list
    /// (RequestAvailableFormations -> OnVrfAvailableFormations) and this map's value is
    /// no longer consulted for setting. See docs/UNIT_MOVEMENT_RESEARCH.md sec 4.
    /// </summary>
    private static string AutoFormationFor(EntityTypeSpec t)
    {
        if (t.Kind != 11) return null; // not an aggregate type
        return (t.Country, t.Category, t.Subcategory, t.Specific, t.Extra) switch
        {
            (225, 2, 1, 1, 0) => "column",   // Scout           11.1.225.2.1.1.0  -> Ground_Aggregate
            (225, 1, 1, 3, 0) => "column",   // ArmorPlatoon    11.1.225.1.1.3.0  -> Ground_Aggregate (GoldenParity)
            (225, 3, 2, 0, 0) => "column",   // ArmorPlatoon    11.1.225.3.2.0.0  -> Tank Platoon (USA) (RealTemplates, R9 fix)
            (225, 5, 2, 0, 0) => "Column",   // ArmorCompany    11.1.225.5.2.0.0  -> Tank Company (USA)
            (225, 5, 20, 0, 0) => "Wedge",   // ArmorCoHQ       11.1.225.5.20.0.0 -> ambiguous match
            (0, 13, 34, 0, 1) => "Wedge",    // MobileIrregular 11.1.0.13.34.0.1  -> C2simEx
            _ => null,
        };
    }

    private static string IsoNow()
        => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);

    private static string NewReportId() => Guid.NewGuid().ToString();

    // Initial great-circle bearing from 'from' to 'to', degrees (0 = North, clockwise). Used to
    // orient a MoveIntoFormation (Unit 4) toward its destination. Small-scale, so exact model is
    // not critical - the key question is whether the aggregate MOVES, not perfect facing.
    private static double BearingDeg(Geodetic from, Geodetic to)
    {
        double lat1 = from.LatDeg * Math.PI / 180.0, lat2 = to.LatDeg * Math.PI / 180.0;
        double dLon = (to.LonDeg - from.LonDeg) * Math.PI / 180.0;
        double y = Math.Sin(dLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        double brng = Math.Atan2(y, x) * 180.0 / Math.PI;
        return (brng + 360.0) % 360.0;
    }

    private void OnVrfScenarioClosed(object sender, EventArgs e)
    {
        _log.LogInformation("VR-Forces scenario closed; initiating clean stop.");
        _life.StopApplication();
    }

    // R4 read-back + R1 apply (docs/UNIT_MOVEMENT_RESEARCH.md): an aggregate answered
    // RequestAvailableFormations with the names IT actually accepts (ground truth for
    // the scenario's model set) and its current formation. Fires on the tick thread.
    // FIRST reply per unit (auto mode): pick a valid name - prefer "column" (route
    // march), else the first listed - then SET it (snap members into clean geometry)
    // and REORGANIZE (establish the lead subordinate). Later replies (e.g. the
    // move-time diagnostic re-query) only log, so the unit is never re-snapped mid-run.
    // Object console messages (UG52 21.9): the vendor's per-object channel for what the engine,
    // the object's plan/controllers and other objects say about it. Logged verbatim with the
    // object's level and name so a run's evidence carries the unit's own account of a task
    // (formation, leader, subordinate dispatch) instead of our inference from positions.
    private void OnVrfObjectConsoleMessage(object sender, ObjectConsoleMessageEventArgs e)
    {
        _names.TryGetName(e.Uuid ?? "", out var objName);
        string text = DecodeConsoleText(e.Message);
        // STP-822 part 2: the SAME rows the runner's stage-7d READY gate greps out of this log
        // (scripts/RunnerLib.ps1 Get-NavAreaRows) are read here, in band, as they arrive - a
        // "New Primary nav area" row is the simulator's own acquisition of a navigation area.
        _navEvidence.Observe(e.Uuid, text, DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond);
        _log.LogInformation("VRF console [{Level}] {Name} ({Uuid}): {Msg}",
                            e.NotifyLevel, objName ?? "?", e.Uuid, text);
    }

    // The sim wraps console text in a DtRwTranslatableStringObject XML blob (a "string-queue" of
    // <string> parts: the translatable template followed by its arguments, e.g. "%1: Controller %2
    // beginning to process %3 task (ID=%4)" | "92.466" | "...move-along-controller" | ...). Log the
    // parts joined with " | " instead of the raw multi-line XML; plain (non-XML) text passes through.
    private static readonly System.Text.RegularExpressions.Regex ConsoleStringPart =
        new(@"<string[^>]*>(.*?)</string>", System.Text.RegularExpressions.RegexOptions.Singleline);
    // the translatable template part is itself wrapped in an ESCAPED <string translate=...> tag
    private static readonly System.Text.RegularExpressions.Regex ConsoleNestedTag = new(@"^<string[^>]*>");

    internal static string DecodeConsoleText(string raw)
    {
        var s = (raw ?? "").TrimEnd();
        if (!s.StartsWith("<?xml", StringComparison.Ordinal) && !s.Contains("<string", StringComparison.Ordinal))
            return s;
        var parts = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in ConsoleStringPart.Matches(s))
        {
            var p = ConsoleNestedTag.Replace(System.Net.WebUtility.HtmlDecode(m.Groups[1].Value).Trim(), "").Trim();
            if (p.Length > 0) parts.Add(p);
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : s;
    }

    private void OnVrfAvailableFormations(object sender, AvailableFormationsEventArgs e)
    {
        _names.TryGetName(e.Uuid ?? "", out var unitName);
        _log.LogInformation("VRF formations for {Name} ({Uuid}): [{List}]  current='{Cur}'.",
                            unitName ?? "?", e.Uuid,
                            e.Formations == null ? "" : string.Join(", ", e.Formations),
                            e.CurrentFormation ?? "");

        if (!_vrf.AggregateFormation.Equals("auto", StringComparison.OrdinalIgnoreCase)) return;
        if (string.IsNullOrEmpty(e.Uuid) || unitName == null) return;        // not one of ours
        if (!_formationApplied.TryAdd(e.Uuid, 0)) return;                    // already applied

        if (e.Formations == null || e.Formations.Count == 0)
        {
            _log.LogWarning("R1: unit {Name} ({Uuid}) reports an EMPTY formation list - no " +
                            "formation can resolve for its type; unit-level movement is " +
                            "unlikely to work (UNIT_MOVEMENT_RESEARCH.md).", unitName, e.Uuid);
            return;
        }
        string pick = e.Formations.FirstOrDefault(f => f.Equals("column", StringComparison.OrdinalIgnoreCase))
                      ?? e.Formations[0];
        _bridge.SetAggregateFormation(e.Uuid, pick);
        _bridge.ReorganizeAggregate(e.Uuid);
        _log.LogInformation("R1: {Name} ({Uuid}) - set formation '{Pick}' (from its own list) " +
                            "+ reorganize.", unitName, e.Uuid, pick);
    }

    /// <summary>Reply to a terrain-profile request (VRF tick thread). Unknown ids (late after
    /// the timeout, or another sender's intersection query) are dropped.</summary>
    private void OnVrfTerrainProfile(object sender, TerrainProfileEventArgs e)
    {
        // We ask for complete replies (sendPartialInformation=false). Should a back end send
        // partials anyway, consuming the first would drop the completing message as stale; log
        // it and wait - the complete message or the timeout sweep finishes the request.
        if (!e.Complete && _pendingTerrain.ContainsKey(e.RequestId))
        {
            _log.LogInformation("Terrain profile reply {Id}: partial (Complete=false, {N} samples) - waiting for the " +
                                "complete reply.", e.RequestId, e.Samples?.Count ?? 0);
            return;
        }
        if (!_pendingTerrain.TryRemove(e.RequestId, out var pending))
        {
            _log.LogDebug("Terrain profile reply {Id} matches no pending request ({N} samples) - dropped.",
                          e.RequestId, e.Samples?.Count ?? 0);
            return;
        }
        var samples = e.Samples ?? new List<TerrainHeightSample>();
        // Reply SHAPE at Information level: ROW2R (run 20260902T101431Z) could not tell "the back
        // end sent one sample" from "the facade read one sample" because this was Debug-only.
        _log.LogInformation("Terrain profile reply {Id}: {N} sample(s) [{Shape}].", e.RequestId, samples.Count,
                            string.Join(" ", samples.Select(x => x.Valid
                                ? FormattableString.Invariant($"#{x.Index}:{x.LatDeg:F5},{x.LonDeg:F5},{x.TerrainAltMeters:F1}")
                                : $"#{x.Index}:none")));
        _tickActions.Enqueue(() => RunTerrainContinuation(pending, samples));
    }

    /// <summary>Tick-loop sweep: a request past its deadline continues with null = Live fallback.</summary>
    private void ExpireTerrainRequests()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _pendingTerrain)
        {
            if (kv.Value.Deadline > now || !_pendingTerrain.TryRemove(kv.Key, out var pending)) continue;
            _log.LogWarning("Terrain profile request {Id} for task '{Task}' got no reply within {T} s - {Fallback}.",
                            kv.Key, pending.TaskName, _vrf.TerrainProfileTimeoutSeconds, pending.FallbackNote);
            _tickActions.Enqueue(() => RunTerrainContinuation(pending, null));
        }
    }

    /// <summary>
    /// D1 (pass-3 cold-start review of `8db033e`). THE ONE PLACE A TERRAIN-PROFILE CONTINUATION IS
    /// RUN, so that a throw inside it cannot be swallowed the way TickLoop's drain swallows one.
    ///
    /// Both enqueue sites - the reply (OnVrfTerrainProfile) and the timeout sweep
    /// (ExpireTerrainRequests) - come through here, because for the ROUTE consumer this
    /// continuation IS the dispatch: in the DEFAULT Vrf:GroundWaypointAltitudeMode
    /// ("TerrainProfile", VrfSettings.cs, overridden in neither settings file) the first pass of
    /// ExecuteTaskOnTick only asks the back end for terrain heights and returns with NOTHING
    /// marked, and this re-entry creates the route, calls the bridge and runs MarkDispatched. A
    /// throw here used to reach only the drain's `catch`, which logs and returns: no TASKSTRT, no
    /// TASKABRT, no abandon, and successors parked on Vrf:TaskChainBackstopSeconds (86,400 s of
    /// task clock) because A1 removed the configured bound for an in-order predecessor.
    ///
    /// The INIT PLACEMENT consumer shares this plumbing and carries no task uuid, so its ending is
    /// the ERROR alone - there is no C2SIM task to abandon, and the creates it was going to place
    /// simply did not happen. That is still strictly louder than the drain's one-line swallow.
    /// </summary>
    private void RunTerrainContinuation(PendingTerrain pending, List<TerrainHeightSample> samples)
    {
        bool hasTask = !string.IsNullOrEmpty(pending.TaskUuid);
        Action<Exception> logError = hasTask
            ? ex => _log.LogError("Task '{Task}': THE TERRAIN-PROFILE CONTINUATION THREW on the VR-Forces " +
                                  "tick thread ({Type}: {Msg}) - that is the pass which creates the route, " +
                                  "calls the bridge and marks the task dispatched, so this task was NOT " +
                                  "dispatched. It is abandoned and reported TASKABRT so its STREND " +
                                  "successors fail fast instead of waiting out the chain backstop (D1).",
                                  pending.TaskName, ex.GetType().Name, ex.Message)
            : ex => _log.LogError("{Label}: the terrain-profile continuation THREW on the VR-Forces tick " +
                                  "thread ({Type}: {Msg}). There is no C2SIM task to abandon here - this is " +
                                  "the init placement query - so the creates it was going to place did NOT " +
                                  "happen.", pending.TaskName, ex.GetType().Name, ex.Message);
        DeferredDispatch.Run(() => pending.Continue(samples), pending.TaskUuid, pending.TaskName,
                             DeferredDispatch.TerrainContinuation,
                             hasTask ? _sequencer : null,
                             hasTask ? reason => PushTaskStatus(pending.TaskeeUuid, pending.TaskUuid,
                                                                S.TaskStatusCodeType.TASKABRT, reason)
                                     : null,
                             logError);
    }

    private bool UsingFidelityTable =>
        string.Equals(_vrf.TypeMappingMode, "FidelityTable", StringComparison.OrdinalIgnoreCase);

    // The back end resolves marking-text references through a 35-byte blob (1 type byte + 35
    // payload, C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h:412), so a name longer than 34
    // characters arrives CUT and stops resolving - the 2026-09-02 route-uuid finding, which cost
    // a whole probe run. The proxy marking tag is appended only when the result still fits.
    private const int MaxVrfMarkingChars = 34;

    private static string FormatSpec(EntityTypeSpec t)
        => $"{t.Kind}.{t.Domain}.{t.Country}.{t.Category}.{t.Subcategory}.{t.Specific}.{t.Extra}";

    private bool IsTerrainProfileMode() =>
        _vrf.GroundWaypointAltitudeMode.Equals("TerrainProfile", StringComparison.OrdinalIgnoreCase);

    // "Live" and "TerrainProfile" share the create path and the Live vertex arithmetic.
    private bool IsLiveLikeAltitudeMode() =>
        _vrf.GroundWaypointAltitudeMode.Equals("Live", StringComparison.OrdinalIgnoreCase) || IsTerrainProfileMode();

    // B2: cumulative report delivery, appended to the R1 line and logged once at shutdown. Touched
    // from every thread that pushes (tick, SDK events, the bundle timer) - Interlocked, not ++.
    private long _reportsSent;
    private long _reportsFailed;

    /// <summary>
    /// Push one report and KNOW WHETHER IT ARRIVED (B2). The SDK's PushReportMessage returns the
    /// server's C2SIMServerResponse - Status OK or ERROR (C2SIMServerResponse.cs:31) - and does NOT
    /// throw on ERROR (C2SIMSSDK.cs:505-528), so the answer is inspected here. A TASK-STATUS push is
    /// retried (Vrf:TaskStatusPushTries, backing off 1/2/4 s): there is one of those per task per
    /// outcome and nothing re-sends it. A POSITION push is never retried - the next poll carries the
    /// same information - and an OBSERVATION is informational. Either way the outcome is counted and
    /// a final failure is LOUD: in run G6, 129 pushes failed with "The response ended prematurely"
    /// and nothing in the log said so.
    /// </summary>
    private async Task PushReportAsync(string reportXml, ReportKind kind = ReportKind.Position)
    {
        if (string.IsNullOrEmpty(reportXml)) return;
        int tries = kind == ReportKind.TaskStatus ? Math.Max(1, _vrf.TaskStatusPushTries) : 1;
        var outcome = await ReportPush.SendAsync(
            async xml =>
            {
                var resp = await _sdk.PushReportMessage(xml);
                // Review finding 6: a NULL response is the server sending an EMPTY BODY, which is
                // reachable (SendTrans never checks the status code - C2SIMClientRestLib.cs:377-401)
                // and is not a confirmation of anything. ReportPush.Interpret counts it as a failed
                // delivery for a TaskStatus (retried) and leaves Position/Observation alone; the
                // measured G6 "response ended prematurely" is a THROW, not this case, and is already
                // counted as a failed attempt. See ReportPush.Interpret for the full citation chain.
                if (resp == null)
                    _log.LogWarning("PushReport ({Kind}): {Why}.", kind, ReportPush.EmptyBodyMessage);
                return ReportPush.Interpret(resp == null, resp?.IsSuccess ?? false, resp?.Message, kind);
            },
            reportXml, tries,
            attempt => ReportPush.BackoffFor(attempt, _vrf.TaskStatusPushBackoffMs),
            delay => Task.Delay(delay, _stoppingToken),
            why => _log.LogWarning("PushReport ({Kind}): {Why}.", kind, why),
            _stoppingToken);

        if (outcome.Ok)
        {
            Interlocked.Increment(ref _reportsSent);
            if (outcome.Attempts > 1)
                _log.LogInformation("PushReport ({Kind}): delivered on attempt {N}.", kind, outcome.Attempts);
        }
        else
        {
            Interlocked.Increment(ref _reportsFailed);
            _log.LogError("PUSH FAILED ({Kind}) after {N} attempt(s) - THE REPORT IS LOST: {Why}. " +
                          "Cumulative: {Sent} sent, {Failed} failed.",
                          kind, outcome.Attempts, outcome.LastMessage,
                          Interlocked.Read(ref _reportsSent), Interlocked.Read(ref _reportsFailed));
        }
    }

    /// <summary>
    /// THE one place a TaskStatus report leaves this interface (B1). It applies TaskStatusPolicy -
    /// one TASKSTRT per dispatch, one TASKCMPLT per task, one TASKABRT per task and never after
    /// that task's TASKCMPLT - logs what it sent (or why it did not), and pushes. Callable from
    /// any thread: the policy is thread-safe and the push is fire-and-forget.
    /// </summary>
    /// <param name="reportOnlyAbort">TRUE only for the progress watchdog's stall TASKABRT, which
    /// leaves the task in flight in VR-Forces: it does NOT cancel the armed end time
    /// (TimedCompletionPolicy.CancelsTimer), so a recovered unit is still held to its end time and
    /// a late one still completes on arrival.</param>
    private void PushTaskStatus(string taskeeUuid, string taskUuid, S.TaskStatusCodeType code, string why,
                                bool reportOnlyAbort = false)
    {
        // Any TERMINAL end cancels the task's timed end, BEFORE anything else - a completion that
        // cannot be SENT has still happened, and a completion that is suppressed as a duplicate has
        // still happened; leaving the timer armed behind either would fire a second, later
        // TASKCMPLT for a task that is already over. (m5 of the cold-start review of 5c67d41: this
        // used to sit BELOW the taskee guard, which inverted its own argument - a status with no
        // taskee uuid left the timer running.) The timed completion itself arrives here with its
        // entry already removed; Cancel then returns false and says nothing.
        if (TimedCompletionPolicy.CancelsTimer(code, reportOnlyAbort) && _timed.Cancel(taskUuid))
            _log.LogInformation("TIMED COMPLETION: the end time armed for task {Task} is cancelled - " +
                                "{Code} reached the reporting point first ({Why}).", taskUuid, code, why);
        if (string.IsNullOrEmpty(taskeeUuid))
        {
            _log.LogWarning("TASK STATUS {Code} for task {Task} NOT SENT - no C2SIM taskee uuid ({Why}).",
                            code, string.IsNullOrEmpty(taskUuid) ? "(none)" : taskUuid, why);
            return;
        }

        bool allowed = _taskStatus.ShouldEmit(code, taskUuid);
        if (!allowed)
        {
            _log.LogInformation("TASK STATUS {Code} for task {Task} SUPPRESSED by the emission rules " +
                                "(already reported for this task, or the task has already completed): {Why}.",
                                code, string.IsNullOrEmpty(taskUuid) ? "(none)" : taskUuid, why);
            return;
        }
        var xml = ReportBuilder.BuildTaskStatusReport(taskeeUuid, taskUuid ?? "", code, IsoNow(), NewReportId());
        _log.LogInformation("SENT TASK STATUS REPORT ({Code}) taskee={Uuid} task={Task} - {Why}.",
                            code, taskeeUuid, string.IsNullOrEmpty(taskUuid) ? "(none)" : taskUuid, why);
        _ = PushReportAsync(xml, ReportKind.TaskStatus);
    }

    // ================= P4b position-report bundle helpers (see the _posBundle field block) =========

    // Running serialized-size ESTIMATE (bytes) - the SECONDARY size guard. Caller holds _posBundleLock.
    private int EstimatedBundleBytesLocked()
        => PosBundleEnvelopeBytes + _posBundle.Count * PosBundleFixBytes;

    // Snapshot + clear the buffer UNDER the lock; returns null when empty (nothing to flush). The
    // caller serializes + pushes the returned snapshot OUTSIDE the lock.
    private List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)> DrainBundleLocked()
    {
        if (_posBundle.Count == 0) return null;
        var snap = new List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)>(_posBundle);
        _posBundle.Clear();
        return snap;
    }

    // Build one bundle envelope from the snapshot and push it. The ReportID is minted HERE (= C++
    // "created when the bundle is sent"). Returns the push Task so the stop path can await delivery.
    private Task PushBundleSnapshot(List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)> snapshot)
    {
        var xml = ReportBuilder.BuildPositionReportBundle(snapshot, IsoNow(), NewReportId());
        _log.LogDebug("SENT POSITION BUNDLE ({N} fixes) in one report.", snapshot.Count);
        return PushReportAsync(xml);
    }

    // Drain + push whatever is buffered (timer + stop paths). Returns the push Task (the stop path
    // AWAITs it before the SDK Disconnect); a completed no-op task when the buffer is empty.
    private Task FlushPositionBundle()
    {
        List<(string uuid, double lat, double lon, double? headingDeg, double? speedMps)> snapshot;
        lock (_posBundleLock) { snapshot = DrainBundleLocked(); }
        return snapshot == null ? Task.CompletedTask : PushBundleSnapshot(snapshot);
    }

    // Periodic force-flush of a PARTIAL bundle (C++ ~2 s reminder thread) so a trickle of POSITION
    // reports is not held indefinitely. Gated on _stoppingToken; cancellation on shutdown ends the
    // loop cleanly and the stop path does the final flush. Started only when bundling is enabled.
    private async Task PositionBundleFlushLoopAsync()
    {
        try
        {
            while (!_stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_vrf.BundleFlushMs), _stoppingToken);
                _ = FlushPositionBundle();
            }
        }
        catch (OperationCanceledException) { /* normal on stop */ }
    }
}
