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

    /// <summary>What OnInitialization created for one C2SIM unit, so OnOrder can task it.
    /// AutoFormation is the E1 per-created-type formation name (null for entities and
    /// unmapped types) - see AutoFormationFor.</summary>
    private readonly record struct CreatedUnit(string Name, string SymbolId, bool IsAggregate,
                                               string AutoFormation);

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
        _bridge.AvailableFormations += OnVrfAvailableFormations;
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

        int planned = 0, matched = 0, duplicates = 0;
        // R9 type-mapping fix (docs/experiments/PREREG_TYPEFIX_CONFIRMING_RUN.md). "GoldenParity"
        // reproduces the byte-for-byte golden-trace objectTypes; anything else (default
        // "RealTemplates") maps ArmorPlatoon to the real Tank Platoon (USA) Cell-C mover.
        var typeMapping = string.Equals(_vrf.TypeMappingMode, "GoldenParity", StringComparison.OrdinalIgnoreCase)
            ? TypeMapping.GoldenParity : TypeMapping.RealTemplates;
        _log.LogInformation("Type-mapping mode = {Mode} (ArmorPlatoon -> {Target}).",
            typeMapping, typeMapping == TypeMapping.GoldenParity ? "Ground_Aggregate (11.1.225.1.1.3.0)" : "Tank Platoon (USA) (11.1.225.3.2.0.0)");
        var toCreate = new List<CreationPlan>();   // collected, then (optionally) de-stacked, then enqueued
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
            if (string.IsNullOrEmpty(unit.ElevationAgl))
                unit = unit with { ElevationAgl = "1000.0" };     // ground-clamp default (:1445-1446)

            var plan = UnitTranslator.Plan(unit, typeMapping);

            // Create-time terrain-clamp fix (docs/SUPERVISED_RECOVERY_PLAN.md sec 3b;
            // MOJAVE_ROOTCAUSE_INVESTIGATION parts 13/13c). Ground units are otherwise born at a
            // fixed MSL (ElevationAgl default 1000) that sits BELOW high-elevation terrain: VRF's
            // create ground-clamp can DROP an above-terrain birth to the surface but cannot RAISE a
            // below-terrain one, so the unit is born buried and never executes movement. Gated on
            // the SAME Vrf:GroundWaypointAltitudeMode string the route path uses (case-insensitive
            // "Live") and the SAME per-unit ground predicate the route path applies (SIDC battle-
            // dimension char at index 2 == 'G'; the route path reads it off CreatedUnit.SymbolId,
            // which is this same unit.SymbolId - constant across the unit's tasks, so per-unit).
            //   Fixed100 (parity): create at the plan altitude + register the deferred SetAltitude.
            //   Live + GROUND: create at CreateAltitudeSafeMslMeters (above all Earth terrain; the
            //     clamp places the unit on the surface) and SKIP the deferred SetAltitude.
            //   Live + NON-ground (air/sea): parity behavior, unchanged.
            bool liveMode = _vrf.GroundWaypointAltitudeMode.Equals("Live", StringComparison.OrdinalIgnoreCase);
            bool isGround = unit.SymbolId is { Length: > 2 } sidc && sidc[2] == 'G';
            if (liveMode && isGround)
            {
                double originalCreateAlt = plan.Pos.AltMeters;
                double safeAlt = _vrf.CreateAltitudeSafeMslMeters;
                plan = plan with { Pos = new Geodetic { LatDeg = plan.Pos.LatDeg, LonDeg = plan.Pos.LonDeg, AltMeters = safeAlt } };
                // Deliberately do NOT register _pendingAltitude for this unit (skip the parity SetAltitude).
                _log.LogInformation("Create-altitude mode=Live: GROUND unit {Name} created at safe MSL " +
                                    "{Safe} m (original create alt {Orig} m); parity post-create SetAltitude " +
                                    "SKIPPED (born-above-terrain + VRF ground clamp places it on the surface).",
                                    plan.Name, safeAlt, originalCreateAlt);
            }
            else if (plan.PostCreateAltitude is double alt)
            {
                _pendingAltitude[plan.Name] = alt;
            }

            // Retain the taskee lookup so OnOrder can resolve PerformingEntity -> VRF uuid,
            // and the inverse (name -> uuid) so the report callbacks can name their subject.
            _unitByC2SimUuid[unit.Uuid] = new CreatedUnit(plan.Name, unit.SymbolId, plan.IsAggregate,
                plan.IsAggregate ? AutoFormationFor(plan.Type) : null);
            _c2SimUuidByName[plan.Name] = unit.Uuid;

            toCreate.Add(plan);
            planned++;
        }

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

        foreach (var p in toCreate)
        {
            _tickActions.Enqueue(() =>
            {
                if (p.IsAggregate)
                    _bridge.CreateAggregate(p.Type, p.Pos, p.Force, p.HeadingDeg, p.Name,
                                            AggregateState.Disaggregated, true);
                else
                    _bridge.CreateEntity(p.Type, p.Pos, p.Force, p.HeadingDeg, p.Name);
            });
        }

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

        _log.LogInformation("Init dispatched: {Units} units + {Areas} areas queued for creation.",
                            planned, areasQueued);
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
            _sequencer.NotifyAbandoned(task.TaskUuid);
            return;
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
            _sequencer.NotifyAbandoned(task.TaskUuid);
            return;
        }

        // Ground waypoint altitude (VrfSettings.GroundWaypointAltitudeMode): "Fixed100" is the
        // golden-parity 100 m MSL; "Live" puts ground waypoints just above the unit's OWN terrain
        // altitude so VRF's offset-route ground clamp succeeds at high-elevation regions (the
        // Mojave freeze). See docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md.
        double groundWpAlt = _vrf.GroundWaypointAltitudeMode.Equals("Live", StringComparison.OrdinalIgnoreCase)
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
            _vrfUuidByName[e.Name] = e.Uuid;
        _log.LogDebug("VRF created {Name} -> {Uuid}", e.Name, e.Uuid);

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
        // moveAlongRoute, :2408-2421). FIFO per route name - see _pendingRouteTasks. The
        // along-route task resolves the route by name, so pass e.Name (== routeName).
        // NOTE (P0.3): the ATTACK/BREACH engage is NO LONGER issued here - it now waits for
        // the move to COMPLETE (OnVrfTaskCompleted), since a same-tick engage would replace
        // the move (NEXT_SESSION_GUIDANCE.md sec 2.5).
        if (!string.IsNullOrEmpty(e.Name) && _pendingRouteTasks.TryGetValue(e.Name, out var routeQueue)
            && routeQueue.TryDequeue(out var pending))
        {
            if (pending.Patrol)
            {
                _bridge.PatrolRoute(pending.TaskeeVrfUuid, e.Name);
                _log.LogInformation("Route '{Route}' created; PatrolRoute issued for {Vrf} (Reconnoiter).",
                                    e.Name, pending.TaskeeVrfUuid);
            }
            else if (pending.PlanMove)
            {
                // R11: the created object is the destination WAYPOINT - issue the planned move.
                _bridge.PlanAndMoveTo(pending.TaskeeVrfUuid, e.Name);
                _log.LogInformation("Waypoint '{Wpt}' created; PlanAndMoveTo issued for {Vrf} (R11).",
                                    e.Name, pending.TaskeeVrfUuid);
            }
            else if (pending.FanOutMembers is { Count: > 0 } members)
            {
                // R10: fan the along-route move out to the member entities (same route).
                foreach (var m in members)
                    _bridge.MoveAlongRoute(m.Uuid, e.Name);
                _log.LogInformation("Route '{Route}' created; R10 fan-out MoveAlongRoute issued to " +
                                    "{N} members of {Vrf}.", e.Name, members.Count, pending.TaskeeVrfUuid);
            }
            else
            {
                _bridge.MoveAlongRoute(pending.TaskeeVrfUuid, e.Name);
                _log.LogInformation("Route '{Route}' created; MoveAlongRoute issued for {Vrf}.",
                                    e.Name, pending.TaskeeVrfUuid);
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
