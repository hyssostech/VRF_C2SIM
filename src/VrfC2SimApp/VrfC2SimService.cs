using System.Collections.Concurrent;
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
    // ONE TASKCMPLT per task, TASKABRT for a refused / skipped / stalled / failed task, and the
    // abort-then-complete rule). Every TaskStatus report in this file goes through PushTaskStatus,
    // which consults it - so the guarantees are properties of the report STREAM, not of call sites.
    private readonly TaskStatusPolicy _taskStatus = new();

    // R4 (user ruling 2026-09-14): the tasks whose END TIME has not arrived yet. Armed at dispatch
    // (MarkDispatched), walked forward on the tick thread (MaybeCompleteTimedTasks) and cancelled
    // by any real end (PushTaskStatus). See TimedCompletionPolicy for the rule.
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
    private bool _taskClockStaleWarned;                         // M4 stale warning for the TASK clock, once
    private DateTime _taskClockHoldLineUtc = DateTime.MinValue; // Q5: the HOLD line repeats, rate-limited
    private DateTime _simRollbackLineUtc = DateTime.MinValue;   // rate limit for the rollback line
    private int _simRollbackLinesSuppressed;                    // rollbacks the rate limit did not print
    private const double TaskClockSampleSeconds = 1.0;
    // How often a gate/delay waiting on the axis re-checks it. A WALL cadence by necessity (there
    // is nothing else to sleep on); 200 ms costs nothing against the shortest wait in a real order
    // and bounds the overshoot of a heavily compressed demo.
    private const int TaskClockPollMs = 200;

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
    // Declared child names per composed parent (ApplyHierarchyComposition) so a case-3 re-attach can
    // restore the DECLARED subordinate order (leader = declared first, UG52 18.1.1) instead of
    // appending the re-created child last.
    private readonly ConcurrentDictionary<string, List<string>> _declaredChildNamesByParent = new();
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

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
                                      "that is readable but FLAT while a VR-Forces back end is still present " +
                                      "is a PAUSE: task times are HELD and age by nothing (Q5)"
                                    : "",
                                _durationScale, _vrf.TaskPredecessorTimeoutSeconds,
                                _vrf.TaskPredecessorEndMarginSeconds, _vrf.TaskChainBackstopSeconds);
        }

        // 1. Start VR-Forces (the bridge owns the controller/exConn).
        var cfg = BuildStartupConfig();
        _log.LogInformation("Starting VrfBridge (protocol={Protocol}, federation={Fed})...",
                            _vrf.Protocol, _vrf.Federation);
        if (!_bridge.Start(cfg))
        {
            _log.LogError("VrfBridge.Start failed - aborting.");
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
        _stopTick = true;
        tickThread.Join(TimeSpan.FromSeconds(5));
        try { await _sdk.Disconnect(); } catch { /* best effort */ }
        _bridge.Stop();
        _bridge.Dispose();
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
            TickPhase("ExpireCompositions", !_compositions.IsEmpty, ExpireCompositions);
            TickPhase("ReleaseReflected", !_awaitReflection.IsEmpty, ReleaseReflected);
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
        if (_vrf.ComposeHierarchy && toCreate.Count > 0)
            ApplyHierarchyComposition(toCreate, hierarchy, declaredByParent);
        if (_vrf.MaterializeAtOrder && toCreate.Count > 0)
        {
            // C13 (CreationPolicy=AtOrder): SHELLS ONLY at init. Every aggregate is created as an EMPTY
            // shell at its authored position - it displays, reflects its position and sits in the
            // organization tree (ApplyHierarchyComposition above attaches declared child shells to
            // parent shells). Members follow when an order references the unit (MaterializeUnit).
            // Expansion is NOT run here; it runs per referenced unit. Platforms are created as-is.
            int shells = 0;
            for (int i = 0; i < toCreate.Count; i++)
                if (toCreate[i].IsAggregate && toCreate[i].CreateSubordinates)
                { toCreate[i] = toCreate[i] with { CreateSubordinates = false }; shells++; }
            if (_vrf.ComposeHierarchy) GetResolver();   // load the catalog here (init thread); order thread only reads
            _log.LogInformation("CreationPolicy=AtOrder (C13): {Shells} unit(s) created as EMPTY shells at their " +
                                "authored positions; members are created when an order first references a unit. " +
                                "{Platforms} platform(s) created in full.", shells, toCreate.Count - shells);
        }
        // Coarse ORBAT leaves (a company/battalion the ORBAT did NOT decompose): expand into their
        // doctrinal sub-units and compose, instead of the broken template higher-unit (G-A).
        else if (_vrf.ComposeHierarchy && toCreate.Count > 0)
            ExpandCoarseLeaves(toCreate, placements, hierarchy);

        // R8 (opt-in, docs/UNIT_MOVEMENT_RESEARCH.md sec 4): spread units that share
        // identical init coordinates onto deterministic rings BEFORE creating them -
        // stacked spawns are the COA-STP1 pathology that blocks aggregate marching.
        if (_vrf.DeStackCreates && toCreate.Count > 1)
        {
            foreach (var g in DeStacker.Apply(toCreate, _vrf.DeStackSpacingMeters, _vrf.DeStackRotationDeg))
                _log.LogInformation("DeStack (R8): {N} units at ({Lat},{Lon}) spread onto " +
                                    "{Spacing} m rings rotated {Rot} deg (first unit kept in place).",
                                    g.Count, g.LatDeg, g.LonDeg, _vrf.DeStackSpacingMeters, _vrf.DeStackRotationDeg);
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
                _graphicsByC2SimUuid[area.Uuid] = new TaskGraphic(
                    area.Uuid, area.Name, TaskGraphic.KindArea,
                    area.Points.Select(pt => (pt.Lat, pt.Lon, (double?)pt.Elev)).ToList());
                areasRegistered++;
            }
            _names.Requested(area.Name);   // B3: so an area's ObjectCreated is an EXACT match, not a prefix scan
            _tickActions.Enqueue(() =>
            {
                var pts = area.Points
                    .Select(pt => new Geodetic { LatDeg = pt.Lat, LonDeg = pt.Lon, AltMeters = pt.Elev })
                    .ToList();
                _bridge.CreateControlArea(pts, area.Name, "TacticalArea", area.Uuid);
            });
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
            _graphicsByC2SimUuid[l.Uuid] = new TaskGraphic(
                l.Uuid, l.Name, TaskGraphic.KindLine,
                l.Points.Select(pt => (pt.Lat, pt.Lon, (double?)pt.Elev)).ToList());
            linesRegistered++;
        }
        foreach (var p in init.Points)
        {
            // A C2SIM Point's FIRST location is its position (InitPoint.Position); any others are
            // kept by the parser rather than silently dropped, but a point graphic is ONE place.
            if (string.IsNullOrEmpty(p.Uuid) || !p.HasPosition) continue;
            var pos = p.Position;
            _graphicsByC2SimUuid[p.Uuid] = new TaskGraphic(
                p.Uuid, p.Name, TaskGraphic.KindPoint,
                new List<(double, double, double?)> { (pos.Lat, pos.Lon, (double?)pos.Elev) });
            pointsRegistered++;
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
                            "point(s) from this delivery).",
                            source, areasQueued, init.Lines.Count, linesQueued, linesDegenerate,
                            init.Points.Count, pointsQueued, pointsEmpty,
                            _vrf.CreateInitLines, _vrf.CreateInitPoints,
                            _graphicsByC2SimUuid.Count, areasRegistered, linesRegistered, pointsRegistered);

        if (duplicates > 0)
            _log.LogWarning("Init ({Source}): skipped {N} units/graphics ALREADY created " +
                            "(duplicate init delivery - late-join + broadcast?).", source, duplicates);

        // Fail LOUDLY when nothing matched the clientId (a silent 0 here cost live-run time:
        // appsettings ships ClientId=STP, but e.g. the COA-STP1 init needs C2SIM). `matched`
        // not `planned` - units that matched but were skipped for missing fields already
        // warned individually and must not masquerade as a ClientId mismatch.
        if (matched == 0 && init.Units.Count > 0)
        {
            var systemNames = string.Join(", ", init.Units
                .Select(u => u.SystemName).Where(s => !string.IsNullOrEmpty(s)).Distinct());
            _log.LogError("Init ({Source}): 0 of {N} units matched Vrf:ClientId='{Id}' - NOTHING will be " +
                          "created or taskable. Init SystemName(s): [{Names}]. Set Vrf:ClientId to match " +
                          "(RUNBOOK sec 2).", source, init.Units.Count, _vrf.ClientId, systemNames);
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
                                           IReadOnlyDictionary<string, IReadOnlyList<string>> declaredByParent = null)
    {
        if (plans.Count != hierarchy.Count)
        {
            _log.LogError("ComposeHierarchy: plans/hierarchy length mismatch ({P} vs {H}) - skipping.",
                          plans.Count, hierarchy.Count);
            return;
        }
        var survivorUuids = new HashSet<string>(
            hierarchy.Select(h => h.Uuid).Where(u => !string.IsNullOrEmpty(u)));
        // A unit is a PARENT iff some SURVIVING unit names it as Superior.
        var parentUuids = new HashSet<string>(
            hierarchy.Where(h => !string.IsNullOrEmpty(h.SuperiorUuid) && survivorUuids.Contains(h.SuperiorUuid))
                     .Select(h => h.SuperiorUuid));
        if (parentUuids.Count == 0) return;   // flat init - nothing to compose

        var parentName = new Dictionary<string, string>();   // parentUuid -> parent plan name (survivor aggregates only)
        for (int i = 0; i < plans.Count; i++)
        {
            string uuid = hierarchy[i].Uuid, name = plans[i].Name;
            if (!parentUuids.Contains(uuid)) continue;
            if (!plans[i].IsAggregate)
            {
                _log.LogWarning("ComposeHierarchy: {Name} has declared children but is NOT an aggregate - " +
                                "cannot compose; created as-is, its children become standalone.", name);
                parentUuids.Remove(uuid);
                continue;
            }
            parentName[uuid] = name;
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
                                   List<(string Uuid, string SuperiorUuid)> hierarchy)
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
                toCreate.Add(childPlan);
                placements.Add(new PlacementInput(ot[2], null, null));  // child DIS domain; placed on terrain
                hierarchy.Add(("", ""));                                // synthetic - not a C2SIM unit
                childNames.Add(childName);
                depthOf[childName] = depth + 1;                        // the loop will visit it (recursive)
            }

            toCreate[i] = plan with { CreateSubordinates = false };     // empty shell
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
            _compositionReady[name] = parentTcs;
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
            _log.LogInformation("MATERIALIZE {Name} ({Why}): shell {Uuid} deleted; re-creating as the TEMPLATE " +
                                "{Tmpl} with its members{Re}.", name, why, shellUuid, plan.TemplateName,
                                reattach ? " and re-attaching under its superior" : "");
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
            if (!reflected)
                _log.LogWarning("MATERIALIZE {Name}: re-created object {Uuid} not reflected within {T}s - releasing " +
                                "its task anyway.", kv.Key, kv.Value.Uuid, _vrf.CompositionTimeoutSeconds);
            else
                _log.LogInformation("MATERIALIZE {Name}: re-created object {Uuid} reflected; ready for tasking.",
                                    kv.Key, kv.Value.Uuid);
            if (_compositionReady.TryGetValue(kv.Key, out var tcs)) tcs.TrySetResult();
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
            _log.LogInformation("PLACEMENT: {Kind} {Name} domain={Domain} created at authored lat/lon; create alt " +
                                "{CreateAlt} m from the {AltSource} (terrain height under the create point: " +
                                "{Terrain}); post-create SetAltitude: {Set} - {Why}.",
                                p.IsAggregate ? "UNIT" : "PLATFORM", p.Name, input.Domain, d.CreateAltMeters,
                                d.CreateAltFromTerrain ? "TERRAIN QUERY" : "FALLBACK",
                                th is double t ? FormattableString.Invariant($"{t:F1} m") : "UNKNOWN",
                                setRegistered ? $"{d.SetAglMeters.Value} m ABOVE GROUND LEVEL"
                                    : (d.SetAglMeters is double sup ? $"SUPPRESSED (policy {sup} m; Vrf:PlacementAglSet=false)" : "none"),
                                d.Why);
        }
        _log.LogInformation("PLACEMENT summary: {T} of {N} create altitude(s) came from the TERRAIN QUERY, " +
                            "{F} from the FALLBACK.", fromTerrain, plans.Count, plans.Count - fromTerrain);
        EnqueueCreates(plans);
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

        // Operator summary (DEMO_READINESS row 15): what this order asks for, before per-task lines.
        _log.LogInformation("ORDER: {Tasks} task(s) for {Taskees} taskee(s); verbs [{Verbs}].",
                            order.Tasks.Count,
                            order.Tasks.Select(t => t.TaskeeUuid ?? "").Distinct().Count(),
                            string.Join(", ", order.Tasks.Select(t => t.ActionCode ?? "?").GroupBy(c => c)
                                                    .OrderByDescending(g => g.Count())
                                                    .Select(g => g.Count() > 1 ? $"{g.Key} x{g.Count()}" : g.Key)));

        // M1: record the WHOLE order before orchestrating any of it. A gated task derives its
        // predecessor window from the predecessor's Duration, and STREND predecessors are not
        // guaranteed to come first in document order.
        foreach (var task in order.Tasks)
            if (!string.IsNullOrEmpty(task.TaskUuid)) _taskByUuid[task.TaskUuid] = task;

        foreach (var task in order.Tasks)
        {
            if (string.IsNullOrEmpty(task.TaskeeUuid))
            {
                _log.LogWarning("Order task '{Name}' has no PerformingEntity - skipping.", task.TaskName);
                _sequencer.NotifyAbandoned(task.TaskUuid); // successors fail fast, not slow-timeout
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
                _vrf.TaskPredecessorEndMarginSeconds);
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
                                    _vrf.TaskPredecessorEndMarginSeconds);
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
                                                          dispatchTimeoutSeconds);
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
            _tickActions.Enqueue(() => DeferredDispatch.Run(
                () => ExecuteTaskOnTick(task, unit), task.TaskUuid, task.TaskName,
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
    /// </summary>
    private void ExecuteTaskOnTick(OrderTask task, CreatedUnit unit, List<Geodetic> terrainRoute = null)
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
        var geometry = TaskGeometryResolver.Resolve(task, _graphicsByC2SimUuid);
        if (terrainRoute == null)
        {
            foreach (var line in geometry.Log)
                _log.LogInformation("Task '{Task}': {Line}.", task.TaskName, line);
            // M5: an unmatched MapGraphicID is a gap between the order and the initialization that
            // SILENTLY changes what the unit does - it falls through to the embedded Location or,
            // on an export that carries only the id, to R2 in place. It is a WARNING.
            foreach (var line in geometry.Warnings)
                _log.LogWarning("Task '{Task}': {Line}.", task.TaskName, line);
        }
        var taskPoints = geometry.Points;

        // OBSERVATION CHANNEL: a template unit's members were created by the sim, not by us, so
        // ObjectCreated never opened THEIR consoles. Open them now (Vrf:ObjectConsoleNotifyLevel
        // >= 0) from the aggregate's published member list, so the per-member offset-route /
        // formation messages of this task are captured too (UG52 21.9.1 p483).
        if (_vrf.ObjectConsoleMemberNotifyLevel >= 0 && unit.IsAggregate)
        {
            var consoleMembers = _bridge.GetAggregateMembers(vrfUuid);
            if (consoleMembers is { Count: > 0 })
            {
                foreach (var m in consoleMembers)
                {
                    if (string.IsNullOrEmpty(m.Uuid)) continue;
                    _names.TryAddName(m.Uuid, m.Name ?? "");
                    _bridge.SetObjectNotifyLevel(m.Uuid, _vrf.ObjectConsoleMemberNotifyLevel);
                }
                // Members are named WITH their uuids (2026-09-06): the console rows the observer captures
                // are uuid-keyed, and without this line's uuids a member's account could not be attributed
                // to its unit offline (PREREG_ORDER_TIME_MATERIALIZATION sec 3.3, instrument gap).
                _log.LogInformation("VRF console level {Level} requested for {N} members of {Name}: {Members}.",
                                    _vrf.ObjectConsoleMemberNotifyLevel, consoleMembers.Count, unit.Name,
                                    string.Join(", ", consoleMembers.Select(m => $"{m.Name} [{m.Uuid}]")));
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

        // Ground waypoint altitude (VrfSettings.GroundWaypointAltitudeMode): "Fixed100" is the
        // golden-parity 100 m MSL; "Live" puts ground waypoints just above the unit's OWN terrain
        // altitude so VRF's offset-route ground clamp succeeds at high-elevation regions (the
        // Mojave freeze). See docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md.
        double groundWpAlt = IsLiveLikeAltitudeMode()
            ? live.AltMeters + _vrf.GroundWaypointLiveClearanceMeters
            : 100.0;

        var routeGeo = new List<Geodetic>
        {
            new() { LatDeg = live.LatDeg, LonDeg = live.LonDeg, AltMeters = isGround ? groundWpAlt : live.AltMeters }
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
                                    task.TaskName, unit.Name, live.LatDeg, live.LonDeg);
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
            && TerrainVertexAuthoring.DistMeters(live.LatDeg, live.LonDeg, authored.Lat, authored.Lon) > _vrf.DropOriginVertexMeters)
        {
            while (skip < taskPoints.Count - 1
                   && TerrainVertexAuthoring.DistMeters(taskPoints[skip].Lat, taskPoints[skip].Lon, authored.Lat, authored.Lon) <= _vrf.DropOriginVertexMeters)
                skip++;
            if (skip > 0)
                _log.LogInformation("Task '{Task}': dropped {N} leading route point(s) on {Name}'s authored origin " +
                                    "({Lat:F5},{Lon:F5}) - the unit was spread {D:F0} m from it; the route starts at its live position.",
                                    task.TaskName, skip, unit.Name, authored.Lat, authored.Lon,
                                    TerrainVertexAuthoring.DistMeters(live.LatDeg, live.LonDeg, authored.Lat, authored.Lon));
        }
        foreach (var p in taskPoints.Skip(skip))
            routeGeo.Add(new Geodetic
            {
                LatDeg = p.Lat,
                LonDeg = p.Lon,
                AltMeters = isGround ? groundWpAlt : (p.Elev ?? 0.0)
            });

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
            double entityAlt = live.AltMeters;
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
            MarkDispatched(task, unit, "move-into-formation", dest);
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
            MarkDispatched(task, unit, "plan-move", routeGeo[^1]);
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
            MarkDispatched(task, unit, "move-to", routeGeo[^1]);
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
        MarkDispatched(task, unit, patrol ? "patrol" : "move-along", patrol ? (Geodetic?)null : routeGeo[^1]);
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

    private void MarkDispatched(OrderTask task, CreatedUnit unit, string kind, Geodetic? dest = null)
    {
        // NOTE (review wf_62e5bdf7): the arrival-evidence swallow flag is NOT cleared here - it is
        // cleared where the replacing VR-Forces command is actually ISSUED (the synchronous bridge
        // calls below, the route-created callback for deferred kinds, IssueEngage), because a
        // deferred kind's old task keeps running until then and its late completion must still
        // be swallowed.
        var superseded = _inFlight.RecordDispatch(unit.Name,
            new InFlightTracker.InFlight(task.TaskUuid, task.TaskName, kind, DateTime.UtcNow,
                                         dest?.LatDeg, dest?.LonDeg, task.TaskeeUuid ?? ""));
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
                _log.LogInformation("Unit {Name}: task '{Old}' keeps its armed end time " +
                                    "(Vrf:SupersededTaskCode=TASKCMPLT) - it will report TASKCMPLT when the " +
                                    "order says it ends, even though VR-Forces is no longer running it, and its " +
                                    "STREND successors keep waiting for that completion.",
                                    unit.Name, old.TaskName);
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
        if (dest is not null) _stallSamples.TryRemove(unit.Name, out _);
        _sequencer.NotifyDispatched(task.TaskUuid, TaskClockSeconds);
        // B1: the task has STARTED. This is the one point every dispatch path reaches (it is what
        // records the in-flight task), so it is where the C2SIM consumer is told - one TASKSTRT per
        // dispatch; a re-entered dispatch (the TerrainProfile second pass) is suppressed by the
        // policy, a genuine re-task announces again.
        PushTaskStatus(task.TaskeeUuid, task.TaskUuid, S.TaskStatusCodeType.TASKSTRT,
                       $"dispatched to {unit.Name} as '{kind}'");

        // R4: ARM THE END TIME HERE, for the same reason TASKSTRT is pushed here - this is the one
        // point every dispatch path reaches. endTime = dispatch + Duration x Vrf:DurationScale.
        // Register is first-dispatch-wins, so the TerrainProfile re-entry does not restart it.
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
            else if (_timed.Register(task.TaskUuid, task.TaskeeUuid, task.TaskName, unit.Name, seconds))
                _log.LogInformation("Task '{Task}': end time armed at {S:F0} s from dispatch " +
                                    "(C2SIM Duration {D:F0} s x Vrf:DurationScale {Scale}) - R4.",
                                    task.TaskName, seconds, task.DurationMs / 1000.0, _durationScale);
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
    private readonly ConcurrentDictionary<string, string> _arrivalReported = new();   // unit name -> task uuid reported from evidence
    private readonly ConcurrentDictionary<string, string> _pendingRouteUnit = new();  // route/waypoint name -> unit name (swallow cleared when its VRF task is issued)
    private readonly ConcurrentDictionary<string, (double Lat, double Lon)> _authoredPosByName = new();  // unit name -> C2SIM authored position (origin-vertex drop)
    private readonly ConcurrentDictionary<string, string> _templateByName = new();    // unit name -> the VR-Forces template the type map landed (route pre-flight)
    private DateTime _nextArrivalCheck = DateTime.MinValue;

    // ============ ROUTE PRE-FLIGHT (Vrf:PreflightWarnings; DEMO_READINESS row 20) ============
    // Built on FIRST USE, never at start-up: when the feature is off - which is the shipped
    // default - nothing here reads a vendor file, opens a socket or creates a directory.
    // volatile: the fast path reads this OUTSIDE the lock, and several task workers can race it.
    private volatile Preflight.PreflightService _preflight;
    private readonly object _preflightLock = new();
    private bool _preflightDisabled;          // one construction failure retires it for the run

    private Preflight.PreflightService GetPreflight()
    {
        if (_preflight != null) return _preflight;
        lock (_preflightLock)
        {
            if (_preflight != null || _preflightDisabled) return _preflight;
            try
            {
                string cache = string.IsNullOrWhiteSpace(_vrf.PreflightCacheDir)
                    ? Path.Combine(AppContext.BaseDirectory, "preflight-cache")
                    : _vrf.PreflightCacheDir;
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
                    FriendlyNation = _nations.Friendly,
                    OpposingNation = _nations.Opposing,
                };
                if (!string.IsNullOrWhiteSpace(_vrf.VrfHome)) opt = opt with { VrfHome = _vrf.VrfHome };
                _preflight = new Preflight.PreflightService(opt);
                _log.LogInformation("ROUTE PRE-FLIGHT enabled: threshold {T:F2} on a {W:F0} m sustained window, " +
                                    "step {S:F0} m, tiles cached in {Cache}{Off}. Warnings only - no task is ever " +
                                    "refused or altered.", opt.Threshold, opt.WindowM, opt.StepM, cache,
                                    opt.Offline ? " (offline)" : "");
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

        _ = Task.Run(async () =>
        {
            try
            {
                var svc = GetPreflight();
                if (svc == null) return;
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
                                    "(max-slope {Raw:F2} x soil {Factor:F2}); {Verdict}.",
                                    taskName, unitName, leg.Index, leg.SustainedWindowM, leg.Sustained, leg.Soil,
                                    leg.WorstLat, leg.WorstLon, leg.WorstSM / 1000.0,
                                    string.IsNullOrEmpty(limit.Template) ? "unit" : limit.Template,
                                    leg.Limit, leg.LimitRaw, leg.Factor,
                                    Preflight.PreflightReports.Verdict(leg.Ratio, svc.Options.Threshold));
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
        });
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
            if (!TryReadMemberPositions(name, out var positions, out int total)) continue;
            var distances = new List<double>();
            foreach (var p in positions.Values)
                distances.Add(TerrainVertexAuthoring.DistMeters(p.Lat, p.Lon, dlat, dlon));
            var d = ArrivalPolicy.Decide(distances, total, _vrf.ArrivalRadiusMeters, _vrf.ArrivalMemberFraction);
            if (!d.Arrived) continue;
            _arrivalReported[name] = rec.TaskUuid ?? "";
            _log.LogInformation("ARRIVAL EVIDENCE: {Name} task '{Task}' - {Within}/{Total} member(s) within {R} m of the " +
                                "last vertex (nearest {Near:F0} m) {T:F0}s after dispatch - reporting completion from the " +
                                "unit's own evidence (user ruling 2026-09-07); a later vendor completion is swallowed.",
                                name, rec.TaskName, d.Within, d.Total, _vrf.ArrivalRadiusMeters, d.NearestMeters,
                                (now - rec.DispatchedUtc).TotalSeconds);
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
        if (preferSim || stallPrefersSim)
        {
            try { simSeconds = _bridge.SimTimeSeconds(); }
            catch (Exception ex)
            { simSeconds = -1.0; _log.LogDebug(ex, "TASK CLOCK: sim-clock read failed; using the wall clock."); }
        }
        double wallNow = now.Ticks / (double)TimeSpan.TicksPerSecond;
        var obs = _simClock.Observe(simSeconds, wallNow, StallPolicy.ModeSwitchConfirmations,
                                    StallPolicy.StaleClockWarnSeconds);
        _simClockLast = obs;

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
        // while a VR-Forces back end is still there and falls back to wall only when there is
        // none. The back-end read happens ONLY in the stale branch, so the golden path keeps its
        // single per-second bridge call.
        bool taskSimStale = heldOnSim && obs.Stale;
        bool backEndPresent = false;
        if (taskSimStale)
        {
            try { backEndPresent = _bridge.BackendCount() > 0; }
            catch (Exception ex)
            {
                // Unreadable = treat it as gone: that is the pre-Q5 behaviour, and serving wall
                // seconds is the outcome that at least keeps moving.
                _log.LogDebug(ex, "TASK CLOCK: BackendCount read failed; treating the back end as gone.");
            }
        }
        var action = StallPolicy.TaskClockAction(heldOnSim, taskSimStale, backEndPresent);

        // _taskClockHoldLineUtc doubles as "the last line said HOLD": it is set while holding and
        // cleared on every other outcome, so a HOLD that turns into a fall back to wall - the back
        // end went away while the clock was already flat - is not swallowed by the once-only guard.
        bool wasHolding = _taskClockHoldLineUtc != DateTime.MinValue;
        if (action == StallPolicy.TaskClockOnFlat.HoldOnSim)
        {
            // REPEATED, not said once: BackendCount cannot tell a paused back end from one that
            // has been DEACTIVATED for missing its status timeout (StallPolicy.TaskClockAction
            // documents the limit), so the one thing this line must not be is quiet.
            if (!_taskClockStaleWarned
                || (now - _taskClockHoldLineUtc).TotalSeconds >= StallPolicy.LogRateLimitSeconds)
            {
                _taskClockStaleWarned = true;
                _taskClockHoldLineUtc = now;
                _log.LogWarning("TASK CLOCK: the simulation clock has not advanced past {T:F1} s for {S:F0} wall " +
                                "seconds and a VR-Forces back end IS still present (BackendCount>0) - the " +
                                "scenario is PAUSED. C2SIM task times are HELD: {N} task(s) waiting on an end " +
                                "time age by NOTHING until the scenario runs again (Q5, user ruling " +
                                "2026-09-14). CAVEAT: the back-end list KEEPS an entry that has missed its " +
                                "status timeout, so if this line keeps repeating and nobody paused anything, " +
                                "the back end has died and task time will stay frozen until it returns.",
                                obs.SimSeconds, obs.FlatForWallSeconds, _timed.Count);
            }
        }
        else if (taskSimStale && (!_taskClockStaleWarned || wasHolding))
        {
            _taskClockStaleWarned = true;
            _taskClockHoldLineUtc = DateTime.MinValue;   // no longer holding
            _log.LogWarning("TASK CLOCK: the simulation clock has not advanced past {T:F1} s for {S:F0} wall " +
                            "seconds and NO VR-Forces back end is present (BackendCount=0) - the simulation " +
                            "is GONE, not paused{Was}. C2SIM task times ({N} task(s) waiting on an end time) " +
                            "are served on the WALL clock until a back end returns; no task is completed " +
                            "early and no wait is restarted.",
                            obs.SimSeconds, obs.FlatForWallSeconds,
                            wasHolding ? " (task time was being HELD until now - the back end has gone away)" : "",
                            _timed.Count);
        }
        else if (_taskClockStaleWarned && !taskSimStale)
        {
            _taskClockStaleWarned = false;
            _taskClockHoldLineUtc = DateTime.MinValue;
            _log.LogInformation("TASK CLOCK: the simulation clock is readable and advancing again ({T:F1} s) - " +
                                "C2SIM task times are served on it once more.", obs.SimSeconds);
        }

        // HoldOnSim advances on the SIM reading, which is flat by definition here, so it adds
        // exactly nothing AND keeps the axis anchored in sim mode - no mode change, no re-anchor.
        bool usingSim = action != StallPolicy.TaskClockOnFlat.FallBackToWall;
        _taskAxis.Advance(usingSim ? obs.SimSeconds : wallNow, usingSim);
    }

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
            _log.LogInformation("TIMED COMPLETION: task '{Task}' on {Unit} reached its END TIME - " +
                                "{Served:F0} s of a {Dur:F0} s Duration served on the {Clock} clock " +
                                "(C2SIM Duration x Vrf:DurationScale {Scale}). R4: completion is given by " +
                                "the end time.", p.TaskName, p.UnitName, p.Elapsed, p.DurationSeconds,
                                usingSim ? "simulation" : "wall", _durationScale);
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
            // The chain does not care HOW the task ended (R4): release the successors' gate.
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

    private void MaybeCheckStalls()
    {
        var now = DateTime.UtcNow;
        if (now < _nextStallCheck) return;
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
            PushTaskStatus(taskeeUuid, rec.TaskUuid ?? "", S.TaskStatusCodeType.TASKABRT,
                           "STALLED (C16 progress watchdog) - report only: the task stays in flight, no " +
                           "VR-Forces command is issued and nothing is re-tasked");
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
        // already reported TASKABRT and then arrives still sends TASKCMPLT (C16 ruling).
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

        // Release any task gated on this one (parity: setTaskIsComplete unblocked the C++
        // busy-wait on getTaskIsComplete; here it completes the successor's await). Only
        // the ATTRIBUTED task's gate releases - a superseded task's gate stays closed.
        if (success) _sequencer.CompleteTask(taskUuid);
        else _sequencer.NotifyAbandoned(taskUuid);   // a FAILED task never completes: successors fail fast

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

        var code = TaskStatusPolicy.CodeForCompletion(success, taskContinues);
        PushTaskStatus(taskeeUuid, taskUuid ?? "", code,
                       !success ? $"unit {name}: VR-Forces reported the task FAILED (success=false) - it is no " +
                                  "longer being processed"
                       : taskContinues ? $"unit {name} completed the MOVE half of its task; the deferred engage " +
                                         "is now in flight under the same task - TASKCMPLT follows when it ends"
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
        _log.LogInformation("VRF console [{Level}] {Name} ({Uuid}): {Msg}",
                            e.NotifyLevel, objName ?? "?", e.Uuid, DecodeConsoleText(e.Message));
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
    private void PushTaskStatus(string taskeeUuid, string taskUuid, S.TaskStatusCodeType code, string why)
    {
        // R4: any REAL end cancels the task's timed end, BEFORE anything else - a completion that
        // cannot be SENT has still happened, and a completion that is suppressed as a duplicate has
        // still happened; leaving the timer armed behind either would fire a second, later
        // TASKCMPLT for a task that is already over. (m5 of the cold-start review of 5c67d41: this
        // used to sit BELOW the taskee guard, which inverted its own argument - a status with no
        // taskee uuid left the timer running.) The timed completion itself arrives here with its
        // entry already removed; Cancel then returns false and says nothing.
        if (TimedCompletionPolicy.CancelsTimer(code) && _timed.Cancel(taskUuid))
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
