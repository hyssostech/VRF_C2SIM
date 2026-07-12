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

    // C2SIM name -> VRF uuid correlation, populated on ObjectCreated
    // (parity: onVrfObjectCreated in C2SIMinterface.cpp).
    private readonly ConcurrentDictionary<string, string> _vrfUuidByName = new();

    // C2SIM unit-uuid -> what we created for it, retained from OnInitialization so OnOrder
    // can resolve a task's PerformingEntity (a C2SIM uuid) to the VRF object's name (the
    // _vrfUuidByName key) and its SIDC (for the ground-clamp test). Parity: executeTask
    // looks the taskee up in the C++ unit map by taskeeUuid (C2SIMinterface.cpp:2044).
    private readonly ConcurrentDictionary<string, CreatedUnit> _unitByC2SimUuid = new();

    // Route name -> taskee VRF uuid, for a move deferred until the route's ObjectCreated
    // fires. Mirrors executeTask waiting for the route to register before moveAlongRoute
    // (C2SIMinterface.cpp:2408-2421): createRoute is async, so MoveAlongRoute cannot be
    // issued in the same tick as CreateRoute.
    private readonly ConcurrentDictionary<string, string> _pendingRouteMove = new();

    // Route name -> (taskee VRF uuid, target VRF uuid), for an ATTACK-family fire deferred
    // to AFTER the MoveAlongRoute is issued (advance the axis, THEN engage). Layer 2 of the
    // semantic map (SEMANTIC_MAPPING.md); issued in OnVrfObjectCreated alongside the move.
    private readonly ConcurrentDictionary<string, (string Taskee, string Target)> _pendingRouteFire = new();

    // Route name -> (taskee VRF uuid, breach-target VRF uuid), for a BREACH deferred to AFTER the
    // approach MoveAlongRoute (advance to the obstacle, THEN breach it). Layer 2 Unit 2.
    private readonly ConcurrentDictionary<string, (string Taskee, string Target)> _pendingRouteBreach = new();

    // Route name -> taskee VRF uuid, for a RECONNOITER (SCREEN/SCOUT) that PATROLS its route
    // instead of moving along it once: on route-created, issue PatrolRoute instead of
    // MoveAlongRoute. Layer 2 (Reconnoiter); disjoint from _pendingRouteMove.
    private readonly ConcurrentDictionary<string, string> _pendingRoutePatrol = new();

    // Object name -> C2SIM unit uuid (inverse of _unitByC2SimUuid), so the VRF report
    // callbacks - which carry the object's marking/name, not its C2SIM uuid - can name the
    // subject of a report (parity: onTaskCompleted/onTextReport getUnitByName -> unit->uuid).
    private readonly ConcurrentDictionary<string, string> _c2SimUuidByName = new();

    // Object name -> its current task's C2SIM uuid, set when OnOrder dispatches a task
    // (parity: setUnitCurrentTaskUuid, C2SIMinterface.cpp:2165), read by OnVrfTaskCompleted
    // to fill the TaskStatus report's CurrentTask.
    private readonly ConcurrentDictionary<string, string> _currentTaskUuidByName = new();

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

    /// <summary>What OnInitialization created for one C2SIM unit, so OnOrder can task it.</summary>
    private readonly record struct CreatedUnit(string Name, string SymbolId, bool IsAggregate);

    public VrfC2SimService(ILoggerFactory loggerFactory, IConfiguration config,
                           IHostApplicationLifetime life)
    {
        _log = loggerFactory.CreateLogger("VrfC2Sim");
        _life = life;

        var c2 = config.GetSection("C2SIM").Get<C2SIMSDKSettings>() ?? new C2SIMSDKSettings();
        _vrf = config.GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();

        // C2SIM half
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
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

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

        // 2. Drive the sim on a dedicated thread (drain queued commands, then Tick).
        // The tick loop runs until _stopTick (NOT the host stoppingToken) so the shutdown
        // path can still enqueue + flush cleanup deletes while it is ticking (see step 5).
        var tickThread = new Thread(TickLoop)
        {
            IsBackground = true,
            Name = "vrf-tick"
        };
        tickThread.Start();

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
            FedFileName = _vrf.FedFileName
        };
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

        int planned = 0;
        foreach (var u in init.Units)
        {
            if (string.IsNullOrEmpty(u.Uuid)) continue;
            if (u.SystemName != _vrf.ClientId) continue;          // only our units (RUNBOOK sec 2)
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
            if (string.IsNullOrEmpty(unit.ElevationAgl))
                unit = unit with { ElevationAgl = "1000.0" };     // ground-clamp default (:1445-1446)

            var plan = UnitTranslator.Plan(unit);
            if (plan.PostCreateAltitude is double alt)
                _pendingAltitude[plan.Name] = alt;

            // Retain the taskee lookup so OnOrder can resolve PerformingEntity -> VRF uuid,
            // and the inverse (name -> uuid) so the report callbacks can name their subject.
            _unitByC2SimUuid[unit.Uuid] = new CreatedUnit(plan.Name, unit.SymbolId, plan.IsAggregate);
            _c2SimUuidByName[plan.Name] = unit.Uuid;

            var p = plan;
            _tickActions.Enqueue(() =>
            {
                if (p.IsAggregate)
                    _bridge.CreateAggregate(p.Type, p.Pos, p.Force, p.HeadingDeg, p.Name,
                                            AggregateState.Disaggregated, true);
                else
                    _bridge.CreateEntity(p.Type, p.Pos, p.Force, p.HeadingDeg, p.Name);
            });
            planned++;
        }

        foreach (var a in init.Areas)
        {
            var area = a;
            _tickActions.Enqueue(() =>
            {
                var pts = area.Points
                    .Select(pt => new Geodetic { LatDeg = pt.Lat, LonDeg = pt.Lon, AltMeters = pt.Elev })
                    .ToList();
                _bridge.CreateControlArea(pts, area.Name, "TacticalArea", area.Uuid);
            });
        }

        _log.LogInformation("Init dispatched: {Units} units + {Areas} areas queued for creation.",
                            planned, init.Areas.Count);
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

        foreach (var task in order.Tasks)
        {
            if (string.IsNullOrEmpty(task.TaskeeUuid))
            {
                _log.LogWarning("Order task '{Name}' has no PerformingEntity - skipping.", task.TaskName);
                continue;
            }
            // Parity: executeTask errors if the taskee was never in the initialization
            // (C2SIMinterface.cpp:1965). Here the taskee must be one we created at init.
            if (!_unitByC2SimUuid.TryGetValue(task.TaskeeUuid, out var unit))
            {
                _log.LogError("TASKEEUUID {Uuid} NOT FOUND IN C2SIMINITIALIZATION - CANNOT EXECUTE TASK '{Name}'.",
                              task.TaskeeUuid, task.TaskName);
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
            if (gate == GateResult.PredecessorTimeout)
                _log.LogWarning("Task '{Task}' predecessor {Pred} did not complete within {Secs}s; " +
                                "dispatching anyway (not hanging - the C++ busy-wait bug).",
                                task.TaskName, task.StartAfterTaskUuid, _vrf.TaskPredecessorTimeoutSeconds);
            _tickActions.Enqueue(() => ExecuteTaskOnTick(task, unit));
        }
        catch (OperationCanceledException) { /* service stopping */ }
        catch (Exception e)
        {
            _log.LogError("Task '{Task}' orchestration failed: {Msg}", task.TaskName, e.Message);
        }
    }

    /// <summary>
    /// Runs on the VRF tick thread: the bare-movement body of executeTask
    /// (C2SIMinterface.cpp:2213-2424). Reads the taskee's live location as point 0,
    /// ground-clamps, appends the task's inline route points, applies ROE + the
    /// (parity no-op) SetTarget, then MoveToLocation (single point) or CreateRoute +
    /// deferred MoveAlongRoute.
    /// </summary>
    private void ExecuteTaskOnTick(OrderTask task, CreatedUnit unit)
    {
        // Resolve the VRF uuid via the created object's name. Parity: executeTask drops
        // the task if the unit was not created (C2SIMinterface.cpp:2046-2050).
        if (!_vrfUuidByName.TryGetValue(unit.Name, out var vrfUuid))
        {
            _log.LogWarning("DROPPING TASK '{Task}' BECAUSE UNIT {Uuid} ({Name}) WAS NOT CREATED.",
                            task.TaskName, task.TaskeeUuid, unit.Name);
            return;
        }

        // Record this task as the unit's current task (parity: setUnitCurrentTaskUuid,
        // :2165) so OnVrfTaskCompleted can name it in the TaskStatus report.
        if (!string.IsNullOrEmpty(task.TaskUuid))
            _currentTaskUuidByName[unit.Name] = task.TaskUuid;

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
                _bridge.FollowEntity(vrfUuid, follow);
                _log.LogInformation("ESCRT task '{Task}': FollowEntity {Vrf} -> {Tgt} (escort; no route).",
                                    task.TaskName, vrfUuid, follow);
                return;
            }
            _log.LogWarning("ESCRT task '{Task}': escorted entity '{Aff}' not resolvable to a distinct VRF unit; " +
                            "executing bare movement instead.", task.TaskName,
                            string.IsNullOrEmpty(task.AffectedEntity) ? "(none)" : task.AffectedEntity);
        }

        // Ground units clamp elevation to 100 (VRF ground-clamping; :2237, :2266, :2290).
        bool isGround = unit.SymbolId.Length > 2 && unit.SymbolId[2] == 'G';

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
            return;
        }

        var routeGeo = new List<Geodetic>
        {
            new() { LatDeg = live.LatDeg, LonDeg = live.LonDeg, AltMeters = isGround ? 100.0 : live.AltMeters }
        };

        // Parity: no route points -> error, cannot execute (:2206-2210). EXCEPTION (Layer 2):
        // an ATTACK with a resolved target needs no route - engage the target in place.
        if (task.Points.Count == 0)
        {
            if (attackTargetVrf != null)
            {
                _bridge.FireAtTarget(vrfUuid, attackTargetVrf);
                _log.LogInformation("ATTACK task '{Task}': no route points; FireAtTarget {Vrf} -> {Tgt} (engage in place).",
                                    task.TaskName, vrfUuid, attackTargetVrf);
                return;
            }
            if (breachTargetVrf != null)
            {
                _bridge.Breach(vrfUuid, breachTargetVrf);
                _log.LogInformation("BREACH task '{Task}': no route points; Breach {Vrf} -> {Tgt} (breach in place).",
                                    task.TaskName, vrfUuid, breachTargetVrf);
                return;
            }
            _log.LogError("NO LOCATION GIVEN - CAN'T EXECUTE TASK '{Task}'.", task.TaskName);
            return;
        }
        foreach (var p in task.Points)
            routeGeo.Add(new Geodetic
            {
                LatDeg = p.Lat,
                LonDeg = p.Lon,
                AltMeters = isGround ? 100.0 : (p.Elev ?? 0.0)
            });

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
            _bridge.MoveIntoFormation(vrfUuid, dest, headingDeg, _vrf.MoveIntoFormation);
            _log.LogInformation("Task '{Task}': MoveIntoFormation for AGGREGATE {Name} ({Vrf}) -> " +
                                "{Lat}/{Lon} formation '{Form}' hdg {Hdg:F0}deg (Unit 4; {N} route pts -> destination).",
                                task.TaskName, unit.Name, vrfUuid, dest.LatDeg, dest.LonDeg,
                                _vrf.MoveIntoFormation, headingDeg, routeGeo.Count);
            // Preserve ATTACK/BREACH semantics on this early return: an aggregate that also has a
            // resolved engagement target advances in formation THEN engages (don't silently drop it).
            if (attackTargetVrf != null)
            {
                _bridge.FireAtTarget(vrfUuid, attackTargetVrf);
                _log.LogInformation("ATTACK task '{Task}': FireAtTarget {Vrf} -> {Tgt} after MoveIntoFormation.",
                                    task.TaskName, vrfUuid, attackTargetVrf);
            }
            if (breachTargetVrf != null)
            {
                _bridge.Breach(vrfUuid, breachTargetVrf);
                _log.LogInformation("BREACH task '{Task}': Breach {Vrf} -> {Tgt} after MoveIntoFormation.",
                                    task.TaskName, vrfUuid, breachTargetVrf);
            }
            return;
        }

        // ENRICHMENT (opt-in via Vrf:AggregateFormation; "" = off = golden parity, PORT.md
        // sec 10): a disaggregated aggregate freezes on moveAlongRoute because its default
        // formation is unresolvable ("column-left"). Setting a VALID formation before the
        // move unblocks it (no-op on non-aggregate entities). Set here, before CreateRoute,
        // so it applies during the route-creation round-trip ahead of the deferred move
        // (the C++ spike used SetAggregateFormation + DtSleep(.5) right before MoveAlongRoute).
        if (!string.IsNullOrEmpty(_vrf.AggregateFormation))
        {
            _bridge.SetAggregateFormation(vrfUuid, _vrf.AggregateFormation);
            _log.LogInformation("Set aggregate formation '{Form}' on {Name} ({Vrf}) before move.",
                                _vrf.AggregateFormation, unit.Name, vrfUuid);
        }

        // Single point -> MoveToLocation; otherwise CreateRoute then move along it (:2393).
        if (routeGeo.Count == 1)
        {
            _bridge.MoveToLocation(vrfUuid, routeGeo[^1]);
            _log.LogInformation("Task '{Task}': MoveToLocation for {Name} ({Vrf}).",
                                task.TaskName, unit.Name, vrfUuid);
            // Layer 2: ATTACK-family engages the resolved target after closing to the point.
            if (attackTargetVrf != null)
            {
                _bridge.FireAtTarget(vrfUuid, attackTargetVrf);
                _log.LogInformation("ATTACK task '{Task}': FireAtTarget {Vrf} -> {Tgt} after move.",
                                    task.TaskName, vrfUuid, attackTargetVrf);
            }
            // Layer 2: BREACH breaches the obstacle after closing to the point.
            if (breachTargetVrf != null)
            {
                _bridge.Breach(vrfUuid, breachTargetVrf);
                _log.LogInformation("BREACH task '{Task}': Breach {Vrf} -> {Tgt} after move.",
                                    task.TaskName, vrfUuid, breachTargetVrf);
            }
            return;
        }

        // CreateRoute is async; defer the along-route task until the route's ObjectCreated fires
        // (parity: the C++ waits for the route to register before moveAlongRoute, :2408-2421).
        string routeName = task.TaskName + " ROUTE";
        // Layer 2: RECONNOITER (SCREEN/SCOUT) PATROLS the route (back and forth) instead of
        // moving along it once - defer PatrolRoute; every other verb defers MoveAlongRoute.
        bool patrol = verb.Intent == TaskIntent.Reconnoiter;
        if (patrol)
            _pendingRoutePatrol[routeName] = vrfUuid;
        else
            _pendingRouteMove[routeName] = vrfUuid;
        // Layer 2: defer the ATTACK-family fire / BREACH to AFTER the along-route task (advance the
        // axis / approach the obstacle, THEN engage / breach) - issued in OnVrfObjectCreated.
        if (attackTargetVrf != null)
            _pendingRouteFire[routeName] = (vrfUuid, attackTargetVrf);
        if (breachTargetVrf != null)
            _pendingRouteBreach[routeName] = (vrfUuid, breachTargetVrf);
        _bridge.CreateRoute(routeGeo, routeName);
        _log.LogInformation("Task '{Task}': CreateRoute '{Route}' ({Count} pts) for {Name}; {Action} deferred to route-created.",
                            task.TaskName, routeName, routeGeo.Count, unit.Name, patrol ? "patrol" : "move");
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
            _vrfUuidByName[e.Name] = e.Uuid;
        _log.LogDebug("VRF created {Name} -> {Uuid}", e.Name, e.Uuid);

        // Apply any deferred SetAltitude now that we have the uuid. This callback
        // already runs on the tick thread, so the bridge call is safe here.
        if (!string.IsNullOrEmpty(e.Name) && _pendingAltitude.TryRemove(e.Name, out var alt))
            _bridge.SetAltitude(e.Uuid, alt);

        // If this created object is a route awaiting its move, issue it now that the
        // route is registered (parity: executeTask's wait-then-moveAlongRoute, :2408-2421).
        // MoveAlongRoute resolves the route by name, so pass e.Name (== routeName).
        if (!string.IsNullOrEmpty(e.Name) && _pendingRouteMove.TryRemove(e.Name, out var taskeeVrfUuid))
        {
            _bridge.MoveAlongRoute(taskeeVrfUuid, e.Name);
            _log.LogInformation("Route '{Route}' created; MoveAlongRoute issued for {Vrf}.",
                                e.Name, taskeeVrfUuid);

            // Layer 2: an ATTACK-family task advances the axis THEN engages - issue the
            // deferred FireAtTarget now that the move is under way (SEMANTIC_MAPPING.md).
            if (_pendingRouteFire.TryRemove(e.Name, out var fire))
            {
                _bridge.FireAtTarget(fire.Taskee, fire.Target);
                _log.LogInformation("ATTACK: FireAtTarget {Vrf} -> {Tgt} after MoveAlongRoute (route '{Route}').",
                                    fire.Taskee, fire.Target, e.Name);
            }
            // Layer 2: a BREACH approaches the obstacle along the route THEN breaches it (Unit 2).
            if (_pendingRouteBreach.TryRemove(e.Name, out var breach))
            {
                _bridge.Breach(breach.Taskee, breach.Target);
                _log.LogInformation("BREACH: Breach {Vrf} -> {Tgt} after MoveAlongRoute (route '{Route}').",
                                    breach.Taskee, breach.Target, e.Name);
            }
        }
        // Layer 2: RECONNOITER (SCREEN/SCOUT) patrols its route instead of moving along it once.
        else if (!string.IsNullOrEmpty(e.Name) && _pendingRoutePatrol.TryRemove(e.Name, out var patrolVrfUuid))
        {
            _bridge.PatrolRoute(patrolVrfUuid, e.Name);
            _log.LogInformation("Route '{Route}' created; PatrolRoute issued for {Vrf} (Reconnoiter).",
                                e.Name, patrolVrfUuid);
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
        // C2SIM uuid + current task uuid, then push a TaskStatus (TASKCMPLT) report.
        string name = e.UnitMarking ?? "";
        if (!_c2SimUuidByName.TryGetValue(name, out var taskeeUuid))
        {
            _log.LogWarning("Task-complete for '{Name}' but no C2SIM uuid known - no report sent.", name);
            return;
        }
        _currentTaskUuidByName.TryGetValue(name, out var taskUuid);

        // Release any task gated on this one (parity: setTaskIsComplete unblocked the C++
        // busy-wait on getTaskIsComplete; here it completes the successor's await).
        _sequencer.CompleteTask(taskUuid);

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

    private async Task PushReportAsync(string reportXml)
    {
        if (string.IsNullOrEmpty(reportXml)) return;
        try { await _sdk.PushReportMessage(reportXml); }
        catch (Exception e) { _log.LogError("PushReport failed: {Msg}", C2SIMSDK.GetRootException(e).Message); }
    }
}
