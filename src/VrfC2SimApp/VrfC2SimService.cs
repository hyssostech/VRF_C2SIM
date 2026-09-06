using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using C2SIM;
using VrfC2Sim;

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

    // C2SIM name -> VRF uuid correlation, populated on ObjectCreated
    // (parity: onVrfObjectCreated in C2SIMinterface.cpp).
    private readonly ConcurrentDictionary<string, string> _vrfUuidByName = new();

    // C2SIM unit-uuid -> what we created for it, retained from OnInitialization so OnOrder
    // can resolve a task's PerformingEntity (a C2SIM uuid) to the VRF object's name (the
    // _vrfUuidByName key) and its SIDC (for the ground-clamp test). Parity: executeTask
    // looks the taskee up in the C++ unit map by taskeeUuid (C2SIMinterface.cpp:2044).
    private readonly ConcurrentDictionary<string, CreatedUnit> _unitByC2SimUuid = new();

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
    private readonly List<(string uuid, double lat, double lon)> _posBundle = new();
    private readonly object _posBundleLock = new();
    // Rough serialized-size ESTIMATE constants for the SECONDARY size guard (COUNT is PRIMARY - see
    // OnVrfTextReport). We do NOT re-serialize per fix; a conservative per-fix estimate only needs
    // to flush BEFORE the real payload nears BundleMaxBytes ("STOMP may balk at larger" - the C++
    // rationale). With the defaults (10 reports / 10240 bytes) count always fires first; the size
    // guard only bites if BundleMaxReports is raised or BundleMaxBytes lowered. Overestimating is
    // safe (flush a little early).
    private const int PosBundleEnvelopeBytes = 512;  // <ReportBody> preamble/postamble + ReportID/ReportingEntity
    private const int PosBundleFixBytes = 400;       // one <PositionReportContent> block (uuid + lat/lon + timestamp + tags)

    // Per-unit in-flight task record (P0.1, replaces the last-write current-task map whose
    // completion misattribution corrupted TASKCMPLT reports + released the wrong successor
    // gates - NEXT_SESSION_GUIDANCE.md sec 2.4 DEFECT A). Written at dispatch
    // (MarkDispatched), popped at completion (OnVrfTaskCompleted); fills the TaskStatus
    // report's CurrentTask (parity: setUnitCurrentTaskUuid, C2SIMinterface.cpp:2165).
    private readonly InFlightTracker _inFlight = new();

    // Control-area keys (uuid or name) already queued for creation - the duplicate-init
    // guard for areas (units use _unitByC2SimUuid membership for the same purpose).
    private readonly ConcurrentDictionary<string, byte> _createdAreaKeys = new();

    // VRF uuid -> created-object name (reverse of _vrfUuidByName), for the R4
    // formation-reply handler (the reply carries only the uuid). Aggregates only.
    private readonly ConcurrentDictionary<string, string> _nameByVrfUuid = new();

    // VRF uuids whose R1 formation set + reorganize already ran (first reply wins;
    // later replies - e.g. the move-time diagnostic re-query - must not re-snap).
    private readonly ConcurrentDictionary<string, byte> _formationApplied = new();

    // Sequences task starts (predecessor completion + start delay), replacing the C++
    // busy-waits with async gating + a timeout. See TaskSequencer.
    private readonly TaskSequencer _sequencer = new();

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
    private sealed record PendingTerrain(DateTime Deadline, string TaskName, Action<List<TerrainHeightSample>> Continue,
                                         string FallbackNote = "dispatching with Live vertices");
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
    }
    private readonly ConcurrentDictionary<string, PendingComposition> _compositions = new();   // parent name -> composition
    private readonly ConcurrentDictionary<string, string> _childToParent = new();               // child name -> parent name
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _compositionReady = new(); // parent name -> children attached
    // EXPAND-to-compose (coarse ORBAT leaves): the offline catalog resolver, loaded lazily on the
    // first coarse leaf so a run with no composite leaves pays nothing. Read on the init thread only.
    private ObjectTypeResolver _resolver;
    private bool _resolverTried;

    public VrfC2SimService(ILoggerFactory loggerFactory, IConfiguration config,
                           IHostApplicationLifetime life)
    {
        _log = loggerFactory.CreateLogger("VrfC2Sim");
        _life = life;

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
                _log.LogInformation("Backend discovered (BackendCount={Count}) after {Secs:F1} s.",
                                    backends, swSettle.Elapsed.TotalSeconds);
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
        // what THIS run created (tracked in _vrfUuidByName); orphans from crashes/force-kills need
        // the hard reset (tools/ResetVrf). Opt out via Vrf:CleanupCreatedOnStop=false.
        if (_vrf.CleanupCreatedOnStop)
        {
            try
            {
                var created = _vrfUuidByName.Values
                    .Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
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
            if (!_pendingTerrain.IsEmpty) ExpireTerrainRequests();
            if (!_compositions.IsEmpty) ExpireCompositions();
            Thread.Sleep(50);
        }
    }

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

            toCreate.Add(plan);
            placements.Add(new PlacementInput(domain, unit.AltitudeAgl, unit.AltitudeMsl));
            hierarchy.Add((unit.Uuid, (unit.SuperiorUuid ?? "").Trim()));
            planned++;
        }

        // COMPOSE-FROM-CHILDREN (Vrf:ComposeHierarchy): classify parent/child/leaf from the declared
        // Superior chain, flip PARENT aggregate plans to createSubordinates=false (empty shell), and
        // register the compositions so OnVrfObjectCreated attaches each declared child once created
        // (vendor-sample recipe). Runs BEFORE de-stack/terrain/enqueue: it rewrites toCreate entries
        // in place and needs the full survivor set. Index-parallel with `hierarchy`.
        if (_vrf.ComposeHierarchy && toCreate.Count > 0)
            ApplyHierarchyComposition(toCreate, hierarchy);
        // Coarse ORBAT leaves (a company/battalion the ORBAT did NOT decompose): expand into their
        // doctrinal sub-units and compose, instead of the broken template higher-unit (G-A).
        if (_vrf.ComposeHierarchy && toCreate.Count > 0)
            ExpandCoarseLeaves(toCreate, placements, hierarchy);

        // R8 (opt-in, docs/UNIT_MOVEMENT_RESEARCH.md sec 4): spread units that share
        // identical init coordinates onto deterministic rings BEFORE creating them -
        // stacked spawns are the COA-STP1 pathology that blocks aggregate marching.
        if (_vrf.DeStackCreates && toCreate.Count > 1)
        {
            foreach (var g in DeStacker.Apply(toCreate, _vrf.DeStackSpacingMeters))
                _log.LogInformation("DeStack (R8): {N} units at ({Lat},{Lon}) spread onto " +
                                    "{Spacing} m rings (first unit kept in place).",
                                    g.Count, g.LatDeg, g.LonDeg, _vrf.DeStackSpacingMeters);
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
        foreach (var (uuid, name, marking, substitution) in proxiesToReport)
            _ = PushReportAsync(ReportBuilder.BuildTypeSubstitutionReport(
                    uuid, name, marking, substitution, IsoNow(), NewReportId()));

        int areasQueued = 0;
        foreach (var a in init.Areas)
        {
            // Same duplicate-delivery guard for areas (keyed by uuid, falling back to name).
            string areaKey = "area:" + (string.IsNullOrEmpty(a.Uuid) ? a.Name : a.Uuid);
            if (!_createdAreaKeys.TryAdd(areaKey, 0)) { duplicates++; continue; }
            var area = a;
            _tickActions.Enqueue(() =>
            {
                var pts = area.Points
                    .Select(pt => new Geodetic { LatDeg = pt.Lat, LonDeg = pt.Lon, AltMeters = pt.Elev })
                    .ToList();
                _bridge.CreateControlArea(pts, area.Name, "TacticalArea", area.Uuid);
            });
            areasQueued++;
        }

        if (duplicates > 0)
            _log.LogWarning("Init ({Source}): skipped {N} units/areas ALREADY created " +
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

        _log.LogInformation("Init dispatched: {Units} units + {Areas} areas queued for creation.",
                            planned, areasQueued);
    }

    /// <summary>
    /// Queue the creates on the tick thread. Extracted 2026-09-05 because there are now two
    /// callers: the immediate path (Fixed100, and any init with nothing to place) and the
    /// terrain-reply path. Enqueuing is kept even when the caller is ALREADY on the tick thread,
    /// so the command order out of an init is the same in both paths.
    /// </summary>
    private void EnqueueCreates(List<CreationPlan> plans)
    {
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
    private void ApplyHierarchyComposition(List<CreationPlan> plans, List<(string Uuid, string SuperiorUuid)> hierarchy)
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
        for (int i = 0; i < plans.Count; i++)
        {
            string sup = hierarchy[i].SuperiorUuid;
            if (string.IsNullOrEmpty(sup) || !parentUuids.Contains(sup)) continue;
            if (!childrenByParent.TryGetValue(sup, out var list)) childrenByParent[sup] = list = new List<string>();
            list.Add(plans[i].Name);
        }

        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.CompositionTimeoutSeconds));
        foreach (var kv in childrenByParent)
        {
            if (!parentName.TryGetValue(kv.Key, out var pName)) continue;  // parent not a survivor aggregate
            _compositions[pName] = new PendingComposition
            {
                ParentName = pName, ExpectedChildNames = kv.Value, Deadline = deadline
            };
            _compositionReady[pName] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
        if (_compositionReady.TryGetValue(comp.ParentName, out var tcs)) tcs.TrySetResult();
    }

    /// <summary>Tick-thread sweep (mirrors ExpireTerrainRequests): a composition past its deadline is
    /// finished from whatever children arrived, so a never-created child cannot hang the parent's tasks.</summary>
    private void ExpireCompositions()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _compositions)
        {
            if (kv.Value.Done) { _compositions.TryRemove(kv.Key, out _); continue; }
            if (kv.Value.Deadline > now) continue;
            FinishComposition(kv.Value, timedOut: true);
        }
    }

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
        int originalCount = toCreate.Count;      // only expand ORIGINAL plans, not appended children
        for (int i = 0; i < originalCount; i++)
        {
            var plan = toCreate[i];
            // A coarse leaf: an aggregate still slated for template creation (ApplyHierarchyComposition
            // did NOT flip it to a shell => it has no DECLARED children) and not already a composition.
            if (!plan.IsAggregate || !plan.CreateSubordinates || _compositions.ContainsKey(plan.Name)) continue;

            int st = plan.Type.Kind == 11 ? 3 : 1;
            var q = new[] { st, plan.Type.Kind, plan.Type.Domain, plan.Type.Country,
                            plan.Type.Category, plan.Type.Subcategory, plan.Type.Specific, plan.Type.Extra };
            var template = res.Resolve(q);
            if (template == null) continue;
            // EXPAND only when the subordinates are themselves UNITS (a company of platoons). A platoon
            // (subs are vehicles) stays a template - proven to work (1222 4/4).
            var unitSubs = template.SubordinateSpecs.Where(s => s.IsUnit).ToList();
            if (unitSubs.Count == 0) continue;

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
            }

            toCreate[i] = plan with { CreateSubordinates = false };     // empty shell
            _compositions[plan.Name] = new PendingComposition {
                ParentName = plan.Name, ExpectedChildNames = childNames,
                Deadline = DateTime.UtcNow.AddSeconds(Math.Max(1, _vrf.CompositionTimeoutSeconds)) };
            _compositionReady[plan.Name] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            foreach (var c in childNames) _childToParent[c] = plan.Name;
            _log.LogInformation("ComposeHierarchy: EXPAND coarse leaf {Parent} ({Tmpl}) -> {N} doctrinal " +
                                "sub-units (declared order) [{Kids}] + empty shell; compose via AddToOrganization.",
                                plan.Name, template.Name, childNames.Count, string.Join(", ", childNames));
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
                continue;
            }
            // Orchestrate the task off-thread: wait for its predecessor + start delay
            // (TaskSequencer), THEN marshal the bridge work onto the tick thread. The C++
            // busy-waited inline (one detached thread per task); this awaits without
            // blocking, and bounds the predecessor wait with a timeout (PORT.md sec 6).
            var t = task;
            var u = unit;
            _ = RunTaskAsync(t, u);
        }
    }

    private async Task RunTaskAsync(OrderTask task, CreatedUnit unit)
    {
        try
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(1, _vrf.TaskPredecessorTimeoutSeconds));
            var gate = await _sequencer.WaitForStartAsync(task.StartAfterTaskUuid, task.SimulationStartMs,
                                                          task.RelativeDelayMs, timeout, _stoppingToken);
            if (gate != GateResult.Proceed)
            {
                // P0.2 (DEFECT B): the predecessor never completed. The OLD behavior always
                // dispatched anyway, so all gated tasks burst-retasked their units together
                // (VRF runs ONE task at a time - each retask REPLACED the in-flight task
                // mid-route). Policy now decides; default is skip.
                string why = gate == GateResult.PredecessorAbandoned
                    ? "was skipped/abandoned upstream"
                    : $"did not complete within {_vrf.TaskPredecessorTimeoutSeconds}s of its dispatch";
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
                    return;
                }
            }
            // COMPOSE-FROM-CHILDREN: a composed PARENT (e.g. a company) is tasked only AFTER its
            // declared children are attached (AddToOrganization), else the move would drive an empty
            // shell. _compositionReady is signalled by FinishComposition on success OR on the
            // ExpireCompositions timeout, so this await always completes within CompositionTimeoutSeconds
            // of init; the generous bound is a backstop only.
            if (_vrf.ComposeHierarchy && _compositionReady.TryGetValue(unit.Name, out var readyTcs)
                && !readyTcs.Task.IsCompleted)
            {
                var composeBound = TimeSpan.FromSeconds(_vrf.CompositionTimeoutSeconds + 30);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
                var done = await Task.WhenAny(readyTcs.Task, Task.Delay(composeBound, cts.Token));
                if (done == readyTcs.Task) cts.Cancel();   // stop the timer
                else
                    _log.LogWarning("Task '{Task}': composition of {Name} not signalled within {T}s - dispatching " +
                                    "anyway (move may drive an incomplete unit).", task.TaskName, unit.Name, composeBound.TotalSeconds);
            }
            _tickActions.Enqueue(() => ExecuteTaskOnTick(task, unit));
        }
        catch (OperationCanceledException) { /* service stopping */ }
        catch (Exception e)
        {
            _log.LogError("Task '{Task}' orchestration failed: {Msg}", task.TaskName, e.Message);
            _sequencer.NotifyAbandoned(task.TaskUuid);
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
        if (!_vrfUuidByName.TryGetValue(unit.Name, out var vrfUuid))
        {
            _log.LogWarning("DROPPING TASK '{Task}' BECAUSE UNIT {Uuid} ({Name}) WAS NOT CREATED.",
                            task.TaskName, task.TaskeeUuid, unit.Name);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            return;
        }

        // OBSERVATION CHANNEL: a template unit's members were created by the sim, not by us, so
        // ObjectCreated never opened THEIR consoles. Open them now (Vrf:ObjectConsoleNotifyLevel
        // >= 0) from the aggregate's published member list, so the per-member offset-route /
        // formation messages of this task are captured too (UG52 21.9.1 p483).
        if (_vrf.ObjectConsoleNotifyLevel >= 0 && unit.IsAggregate)
        {
            var consoleMembers = _bridge.GetAggregateMembers(vrfUuid);
            if (consoleMembers is { Count: > 0 })
            {
                foreach (var m in consoleMembers)
                {
                    if (string.IsNullOrEmpty(m.Uuid)) continue;
                    _nameByVrfUuid.TryAdd(m.Uuid, m.Name ?? "");
                    _bridge.SetObjectNotifyLevel(m.Uuid, _vrf.ObjectConsoleNotifyLevel);
                }
                _log.LogInformation("VRF console level {Level} requested for {N} members of {Name}: {Members}.",
                                    _vrf.ObjectConsoleNotifyLevel, consoleMembers.Count, unit.Name,
                                    string.Join(", ", consoleMembers.Select(m => m.Name)));
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
        // init-created maps (_unitByC2SimUuid -> _vrfUuidByName) - the two-dict chain that
        // dissolves the plan's uuid-resolution blocker (SEMANTIC_MAPPING.md sec 2b). The target
        // must be an entity our clientId created at init; an out-of-scope OPFOR target degrades
        // to advance-only + a warn. The fire itself is issued AFTER the move below (advance the
        // axis, then engage); the move/fire task interaction in VRF is the live question.
        string attackTargetVrf = null;
        if (verb.Intent == TaskIntent.Attack)
        {
            if (TryResolveVrfUuid(task.AffectedEntity, out var tgt))
            {
                // Self-target guard: some coa-gpt fire-support tasks (e.g. "ProvidePriorityFires")
                // set AffectedEntity == PerformingEntity, which resolves to the taskee's own uuid.
                // FireAtTarget(self) is a degenerate no-op in VRF, so skip it (found live 2026-07-11).
                // A richer mapping would route these to provideIndirectFireTask (SEMANTIC_MAPPING.md).
                if (string.Equals(tgt, vrfUuid, StringComparison.Ordinal))
                    _log.LogInformation("ATTACK task '{Task}': affected entity is the taskee itself " +
                                        "(self-target fire-support?); no fire, advancing only.", task.TaskName);
                else
                    attackTargetVrf = tgt;
            }
            else
                _log.LogWarning("ATTACK task '{Task}': affected entity '{Aff}' is not a VRF unit we created " +
                                "(out-of-scope target?); advancing only, no fire.",
                                task.TaskName, string.IsNullOrEmpty(task.AffectedEntity) ? "(none)" : task.AffectedEntity);
        }

        // LAYER 2 - BREACH (Unit 2): resolve the affected OBSTACLE to a VRF target for a
        // DtBreachTask (approach move, then breach it). Same two-dict resolution + self-target
        // guard as ATTACK. Unresolved -> advance-only + warn (no silent drop).
        string breachTargetVrf = null;
        if (verb.Intent == TaskIntent.Breach)
        {
            if (TryResolveVrfUuid(task.AffectedEntity, out var tgt)
                && !string.Equals(tgt, vrfUuid, StringComparison.Ordinal))
                breachTargetVrf = tgt;
            else
                _log.LogWarning("BREACH task '{Task}': affected obstacle '{Aff}' not resolvable to a distinct " +
                                "VRF unit; advancing only, no breach.", task.TaskName,
                                string.IsNullOrEmpty(task.AffectedEntity) ? "(none)" : task.AffectedEntity);
        }

        // LAYER 2 - ESCRT (Escort): follow the escorted entity (DtFollowEntityTask). Following is
        // DYNAMIC - no route or point-0 needed - so dispatch it here, before the movement logic
        // (an ESCRT task may carry no route points, which would otherwise error below). Unresolved
        // escorted entity -> fall through to bare movement (warn logged).
        if (verb.Intent == TaskIntent.Escort)
        {
            if (TryResolveVrfUuid(task.AffectedEntity, out var follow)
                && !string.Equals(follow, vrfUuid, StringComparison.Ordinal))
            {
                Roe escortRoe = task.RuleOfEngagementCode == "ROEFree" ? Roe.FireAtWill
                              : task.RuleOfEngagementCode == "ROEHold" ? Roe.HoldFire
                              : Roe.FireWhenFiredUpon;
                _bridge.SetRulesOfEngagement(vrfUuid, escortRoe);
                MarkDispatched(task, unit, "follow");
                _bridge.FollowEntity(vrfUuid, follow);
                _log.LogInformation("ESCRT task '{Task}': FollowEntity {Vrf} -> {Tgt} (escort; no route).",
                                    task.TaskName, vrfUuid, follow);
                return;
            }
            _log.LogWarning("ESCRT task '{Task}': escorted entity '{Aff}' not resolvable to a distinct VRF unit; " +
                            "executing bare movement instead.", task.TaskName,
                            string.IsNullOrEmpty(task.AffectedEntity) ? "(none)" : task.AffectedEntity);
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
            _log.LogWarning("ABANDONING TASK '{Task}': could not read live location for {Name} ({Vrf}).",
                            task.TaskName, unit.Name, vrfUuid);
            _sequencer.NotifyAbandoned(task.TaskUuid);
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

        // Parity: no route points -> error, cannot execute (:2206-2210). EXCEPTION (Layer 2):
        // an ATTACK with a resolved target needs no route - engage the target in place.
        if (task.Points.Count == 0)
        {
            // In-place engagements (no move to wait for) stay immediate - P0.3 gates only
            // the advance-THEN-engage compositions.
            if (attackTargetVrf != null)
            {
                MarkDispatched(task, unit, "fire");
                _bridge.FireAtTarget(vrfUuid, attackTargetVrf);
                _log.LogInformation("ATTACK task '{Task}': no route points; FireAtTarget {Vrf} -> {Tgt} (engage in place).",
                                    task.TaskName, vrfUuid, attackTargetVrf);
                return;
            }
            if (breachTargetVrf != null)
            {
                MarkDispatched(task, unit, "breach");
                _bridge.Breach(vrfUuid, breachTargetVrf);
                _log.LogInformation("BREACH task '{Task}': no route points; Breach {Vrf} -> {Tgt} (breach in place).",
                                    task.TaskName, vrfUuid, breachTargetVrf);
                return;
            }
            _log.LogError("NO LOCATION GIVEN - CAN'T EXECUTE TASK '{Task}'.", task.TaskName);
            _sequencer.NotifyAbandoned(task.TaskUuid);
            return;
        }
        foreach (var p in task.Points)
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
                });
                _log.LogInformation("Task '{Task}': terrain profile request {Id} sent for {N} vertices; dispatch " +
                                    "deferred to the reply (timeout {T} s -> Live fallback).",
                                    task.TaskName, requestId, liveVertices.Count, _vrf.TerrainProfileTimeoutSeconds);
                return;
            }
        }

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
            MarkDispatched(task, unit, "move-into-formation");
            _bridge.MoveIntoFormation(vrfUuid, dest, headingDeg, _vrf.MoveIntoFormation);
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
            var wptQueue = _pendingRouteTasks.GetOrAdd(wptName, _ => new ConcurrentQueue<PendingRouteTask>());
            wptQueue.Enqueue(new PendingRouteTask(vrfUuid, Patrol: false, PlanMove: true));
            MarkDispatched(task, unit, "plan-move");
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
            MarkDispatched(task, unit, "move-to");
            _bridge.MoveToLocation(vrfUuid, routeGeo[^1]);
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
        // The unit is committed to this move now (the route-created callback issues the
        // along-route task); record it so the completion attributes here (P0.1) and any
        // engage below gates on it (P0.3).
        MarkDispatched(task, unit, patrol ? "patrol" : "move-along");
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
    private void MarkDispatched(OrderTask task, CreatedUnit unit, string kind)
    {
        var superseded = _inFlight.RecordDispatch(unit.Name,
            new InFlightTracker.InFlight(task.TaskUuid, task.TaskName, kind, DateTime.UtcNow));
        if (superseded is InFlightTracker.InFlight old && old.TaskUuid != task.TaskUuid)
        {
            _log.LogWarning("Unit {Name}: task '{New}' SUPERSEDES in-flight task '{Old}' ({OldUuid}) - VRF " +
                            "replaces the running task; the old task will not complete.",
                            unit.Name, task.TaskName, old.TaskName, old.TaskUuid);
            if (_pendingEngage.TryGetValue(unit.Name, out var eng) && eng.MoveTaskUuid == old.TaskUuid
                && _pendingEngage.TryRemove(new KeyValuePair<string, PendingEngage>(unit.Name, eng)))
                _log.LogWarning("Unit {Name}: cancelled the pending {Kind} tied to superseded task '{Old}'.",
                                unit.Name, eng.Kind, old.TaskName);
            // R10: a superseded task's fan-out must not complete against the new task.
            if (_fanOut.Cancel(unit.Name))
                _log.LogWarning("Unit {Name}: cancelled the member fan-out tied to superseded task '{Old}'.",
                                unit.Name, old.TaskName);
        }
        _sequencer.NotifyDispatched(task.TaskUuid);
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
        if (_fanOut.TrySynthesizeByTimeout(unitName, capturedTaskUuid, out int completed, out int total))
        {
            _log.LogWarning("fan-out straggler timeout for {Unit}: {Completed}/{Total} members done - " +
                            "synthesizing unit completion.", unitName, completed, total);
            // No VRF completion callback on the timer path -> no VRF task type to sanity-check
            // against the dispatched kind; pass empty (KindLooksRight treats empty as "can't
            // tell", so it does NOT emit a spurious attribution-anomaly warning here).
            SynthesizeUnitCompletion(unitName, "");
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
        // parity: onVrfObjectCreated correlates the requested name to its VRF uuid.
        if (!string.IsNullOrEmpty(e.Name))
        {
            _vrfUuidByName[e.Name] = e.Uuid;
            _nameByVrfUuid[e.Uuid] = e.Name;   // reverse map for console/formation replies (all paths)
        }
        _log.LogDebug("VRF created {Name} -> {Uuid}", e.Name, e.Uuid);

        // OBSERVATION CHANNEL (Vrf:ObjectConsoleNotifyLevel >= 0): open this object's console at
        // the requested level so its controllers' messages reach OnVrfObjectConsoleMessage
        // (UG52 21.9.1 p483; vrfRemoteController.h:1953). Tick thread - bridge call is safe.
        if (_vrf.ObjectConsoleNotifyLevel >= 0 && !string.IsNullOrEmpty(e.Uuid))
        {
            _bridge.SetObjectNotifyLevel(e.Uuid, _vrf.ObjectConsoleNotifyLevel);
            _log.LogInformation("VRF console level {Level} requested for {Name} ({Uuid}).",
                                _vrf.ObjectConsoleNotifyLevel, e.Name, e.Uuid);
        }

        // COMPOSE-FROM-CHILDREN (Vrf:ComposeHierarchy): attach declared children under their parent
        // shell once both exist (vendor sample commandLineRemoteController.cxx:1520-1554). This
        // callback runs on the tick thread, so the AddToOrganization bridge call inside is safe.
        if (_vrf.ComposeHierarchy && !string.IsNullOrEmpty(e.Name)
            && (_compositions.ContainsKey(e.Name) || _childToParent.ContainsKey(e.Name)))
            TryAdvanceComposition(e.Name, e.Uuid);

        // Apply any deferred SetAltitude now that we have the uuid. This callback
        // already runs on the tick thread, so the bridge call is safe here.
        if (!string.IsNullOrEmpty(e.Name) && _pendingAltitude.TryRemove(e.Name, out var alt))
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
        if (!string.IsNullOrEmpty(e.Name)
            && _vrf.AggregateFormation.Equals("auto", StringComparison.OrdinalIgnoreCase)
            && _c2SimUuidByName.TryGetValue(e.Name, out var createdC2SimUuid)
            && _unitByC2SimUuid.TryGetValue(createdC2SimUuid, out var createdUnit)
            && createdUnit.IsAggregate)
        {
            _nameByVrfUuid[e.Uuid] = e.Name;
            _bridge.RequestAvailableFormations(e.Uuid);
            _log.LogInformation("R1: created aggregate {Name} ({Uuid}) - formation list " +
                                "queried; set+reorganize follow on the reply.", e.Name, e.Uuid);
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
        if (!string.IsNullOrEmpty(e.Name) && _pendingRouteTasks.TryGetValue(e.Name, out var routeQueue)
            && routeQueue.TryDequeue(out var pending))
        {
            if (pending.Patrol)
            {
                _bridge.PatrolRoute(pending.TaskeeVrfUuid, e.Uuid);
                _log.LogInformation("Route '{Route}' ({RouteUuid}) created; PatrolRoute issued for {Vrf} (Reconnoiter).",
                                    e.Name, e.Uuid, pending.TaskeeVrfUuid);
            }
            else if (pending.PlanMove)
            {
                // R11: the created object is the destination WAYPOINT - issue the planned move.
                _bridge.PlanAndMoveTo(pending.TaskeeVrfUuid, e.Uuid);
                _log.LogInformation("Waypoint '{Wpt}' ({WptUuid}) created; PlanAndMoveTo issued for {Vrf} (R11).",
                                    e.Name, e.Uuid, pending.TaskeeVrfUuid);
            }
            else if (pending.FanOutMembers is { Count: > 0 } members)
            {
                // R10: fan the along-route move out to the member entities (same route).
                foreach (var m in members)
                    _bridge.MoveAlongRoute(m.Uuid, e.Uuid);
                _log.LogInformation("Route '{Route}' ({RouteUuid}) created; R10 fan-out MoveAlongRoute issued to " +
                                    "{N} members of {Vrf}.", e.Name, e.Uuid, members.Count, pending.TaskeeVrfUuid);
            }
            else
            {
                _bridge.MoveAlongRoute(pending.TaskeeVrfUuid, e.Uuid);
                _log.LogInformation("Route '{Route}' ({RouteUuid}) created; MoveAlongRoute issued for {Vrf}.",
                                    e.Name, e.Uuid, pending.TaskeeVrfUuid);
            }
        }
    }

    /// <summary>
    /// Resolve a C2SIM entity uuid to its VRF uuid via the init-created maps
    /// (_unitByC2SimUuid -> _vrfUuidByName). This is the two-dict chain that dissolves the
    /// TASK_EXPANSION_PLAN "uuid-resolution blocker" (SEMANTIC_MAPPING.md sec 2b). Returns
    /// false if the entity was not created by our clientId at init (e.g. an out-of-scope
    /// OPFOR target) or has not yet been confirmed created by VR-Forces.
    /// </summary>
    private bool TryResolveVrfUuid(string c2SimUuid, out string vrfUuid)
    {
        vrfUuid = "";
        if (string.IsNullOrEmpty(c2SimUuid)) return false;
        if (!_unitByC2SimUuid.TryGetValue(c2SimUuid, out var u)) return false;
        if (_vrfUuidByName.TryGetValue(u.Name, out var v) && !string.IsNullOrEmpty(v))
        {
            vrfUuid = v;
            return true;
        }
        return false;
    }

    private void OnVrfTaskCompleted(object sender, TaskCompletedEventArgs e)
    {
        _log.LogInformation("VRF task complete: {Unit} / {Task}", e.UnitMarking, e.TaskType);

        // Port of executeTask's TASKCMPLT emit (C2SIMinterface.cpp:2435), triggered here by
        // the completion callback instead of a busy-wait. Resolve the marking -> taskee
        // C2SIM uuid, attribute the completion to the unit's IN-FLIGHT task (P0.1 - the
        // callback carries no task uuid, and the old last-write map misattributed it to
        // whatever was dispatched last), then push a TaskStatus (TASKCMPLT) report.
        string name = e.UnitMarking ?? "";

        // R10: a fanned-out aggregate move completes PER MEMBER (the marking is the member
        // entity's name). Aggregate them; only when the QUORUM is met does the UNIT's
        // completion flow (SynthesizeUnitCompletion) run, under the unit's name. Late
        // stragglers arriving after a quorum/timeout synthesis are SWALLOWED here (they must
        // NOT fall through to the unit-level path, which would emit a spurious empty-uuid
        // TASKCMPLT - the "NO in-flight task recorded" bug this step removes).
        if (_fanOut.TryCompleteMember(name, out var fanUnit, out _, out int fanRemaining,
                                      out bool fanAllDone, out bool fanAlreadySynthesized))
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
            _log.LogInformation("R10 fan-out: completion quorum reached for {Unit} ({N} straggler(s) will be " +
                                "swallowed) - synthesizing the unit's task completion.", fanUnit, fanRemaining);
            SynthesizeUnitCompletion(fanUnit, e.TaskType);
            return;
        }

        // Normal (non-fanned) unit-level completion.
        SynthesizeUnitCompletion(name, e.TaskType);
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
    private void SynthesizeUnitCompletion(string name, string vrfTaskTypeForLog)
    {
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
        _sequencer.CompleteTask(taskUuid);

        // P0.3: the move completed - issue the engage that was parked on it (advance the
        // axis / approach the obstacle, THEN engage/breach - now for real, not same-tick).
        // taskUuid != null (not IsNullOrEmpty): an ATTRIBUTED task with an empty uuid must
        // still match its engage; only an UNATTRIBUTED completion (null) skips this.
        if (taskUuid != null && _pendingEngage.TryGetValue(name, out var eng)
            && eng.MoveTaskUuid == taskUuid
            && _pendingEngage.TryRemove(new KeyValuePair<string, PendingEngage>(name, eng)))
        {
            _log.LogInformation("Unit {Name}: move for task '{Task}' completed; issuing the deferred {Kind}.",
                                name, eng.TaskName, eng.Kind);
            IssueEngage(name, eng);
        }

        var report = ReportBuilder.BuildTaskCompleteReport(taskeeUuid, taskUuid ?? "", IsoNow(), NewReportId());
        _log.LogInformation("SENT TASK STATUS REPORT (TASKCMPLT) taskee={Uuid} task={Task}.",
                            taskeeUuid, taskUuid ?? "(none)");
        _ = PushReportAsync(report);
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
            List<(string uuid, double lat, double lon)> snapshot = null;
            lock (_posBundleLock)
            {
                _posBundle.Add((uuid, lat, lon));
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
        _nameByVrfUuid.TryGetValue(e.Uuid ?? "", out var objName);
        _log.LogInformation("VRF console [{Level}] {Name} ({Uuid}): {Msg}",
                            e.NotifyLevel, objName ?? "?", e.Uuid, (e.Message ?? "").TrimEnd());
    }

    private void OnVrfAvailableFormations(object sender, AvailableFormationsEventArgs e)
    {
        _nameByVrfUuid.TryGetValue(e.Uuid ?? "", out var unitName);
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
        _tickActions.Enqueue(() => pending.Continue(samples));
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
            _tickActions.Enqueue(() => pending.Continue(null));
        }
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

    private async Task PushReportAsync(string reportXml)
    {
        if (string.IsNullOrEmpty(reportXml)) return;
        try { await _sdk.PushReportMessage(reportXml); }
        catch (Exception e) { _log.LogError("PushReport failed: {Msg}", C2SIMSDK.GetRootException(e).Message); }
    }

    // ================= P4b position-report bundle helpers (see the _posBundle field block) =========

    // Running serialized-size ESTIMATE (bytes) - the SECONDARY size guard. Caller holds _posBundleLock.
    private int EstimatedBundleBytesLocked()
        => PosBundleEnvelopeBytes + _posBundle.Count * PosBundleFixBytes;

    // Snapshot + clear the buffer UNDER the lock; returns null when empty (nothing to flush). The
    // caller serializes + pushes the returned snapshot OUTSIDE the lock.
    private List<(string uuid, double lat, double lon)> DrainBundleLocked()
    {
        if (_posBundle.Count == 0) return null;
        var snap = new List<(string uuid, double lat, double lon)>(_posBundle);
        _posBundle.Clear();
        return snap;
    }

    // Build one bundle envelope from the snapshot and push it. The ReportID is minted HERE (= C++
    // "created when the bundle is sent"). Returns the push Task so the stop path can await delivery.
    private Task PushBundleSnapshot(List<(string uuid, double lat, double lon)> snapshot)
    {
        var xml = ReportBuilder.BuildPositionReportBundle(snapshot, IsoNow(), NewReportId());
        _log.LogDebug("SENT POSITION BUNDLE ({N} fixes) in one report.", snapshot.Count);
        return PushReportAsync(xml);
    }

    // Drain + push whatever is buffered (timer + stop paths). Returns the push Task (the stop path
    // AWAITs it before the SDK Disconnect); a completed no-op task when the buffer is empty.
    private Task FlushPositionBundle()
    {
        List<(string uuid, double lat, double lon)> snapshot;
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
