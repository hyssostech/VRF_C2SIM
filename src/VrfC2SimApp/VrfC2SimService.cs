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

    // Commands from SDK-event threads are queued here and executed on the tick thread.
    private readonly ConcurrentQueue<Action> _tickActions = new();
    private volatile bool _stopTick;

    // Post-create SetAltitude, deferred until ObjectCreated delivers the VRF uuid
    // (parity: the C++ factories waitForData then SetAltitude - here it is async).
    private readonly ConcurrentDictionary<string, double> _pendingAltitude = new();

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
        var tickThread = new Thread(() => TickLoop(stoppingToken))
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
        }
        catch (Exception e)
        {
            _log.LogError("C2SIM connect failed: {Msg}", C2SIMSDK.GetRootException(e).Message);
        }

        // 4. Idle until shutdown; SDK events drive the work.
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* normal on stop */ }

        // 5. Clean shutdown: stop the tick loop, disconnect, tear down the bridge.
        _log.LogInformation("Shutting down...");
        _stopTick = true;
        tickThread.Join(TimeSpan.FromSeconds(5));
        try { await _sdk.Disconnect(); } catch { /* best effort */ }
        _bridge.Stop();
        _bridge.Dispose();
    }

    private void TickLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !_stopTick)
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
        // The interface exits when the server broadcasts UNINITIALIZED (RUNBOOK sec 4).
        // TODO(parity): deserialize e.Body and switch on SystemStateCode instead of a
        // substring test.
        if (e.Body != null && e.Body.Contains("UNINITIALIZED", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogInformation("C2SIM server -> UNINITIALIZED; initiating clean stop.");
            _life.StopApplication();
        }
    }

    private void OnInitialization(object sender, C2SIMSDK.C2SIMNotificationEventParams e)
    {
        _log.LogInformation("C2SIM Initialization received ({Len} bytes).", e.Body?.Length ?? 0);

        // Parse (InitParser is a stub until the parse slice lands) then dispatch each
        // unit through UnitTranslator (the faithful port of extractC2simInit's factories).
        InitData init;
        try { init = InitParser.Parse(e.Body); }
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
        // TODO(parity, Phase 4): port executeTask. Parse the order's tasks; resolve the
        // taskee C2SIM uuid -> VRF uuid via _vrfUuidByName; enqueue the tasking
        // (createRoute + MoveAlongRoute today; the two-layer TaskActionCode -> vrftask
        // mapping is the Phase 4+ enrichment, PORT.md sec 10 / TASK_EXPANSION_PLAN.md).
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
    }

    private void OnVrfTaskCompleted(object sender, TaskCompletedEventArgs e)
    {
        _log.LogInformation("VRF task complete: {Unit} / {Task}", e.UnitMarking, e.TaskType);
        // TODO(parity, Phase 4): port reportCallback's TASKCMPLT path - build a C2SIM
        // status report for e.UnitMarking and push it. For now, wiring only.
        // _ = PushReportAsync(BuildTaskStatusReport(e));
    }

    private void OnVrfTextReport(object sender, TextReportEventArgs e)
    {
        _log.LogDebug("VRF text-report: {Text}", e.Text);
        // TODO(parity, Phase 4): port reportGenerator/position-report path - parse the
        // Lua-emitted POSITION/OBSERVATION text into a C2SIM PositionReport + push.
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
