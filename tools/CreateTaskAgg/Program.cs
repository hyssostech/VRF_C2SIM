using System.Diagnostics;
using System.Globalization;
using VrfC2Sim;

// tools/CreateTaskAgg - the CELL C spike tool (docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md).
//
// WHAT IT IS: remote-create a Tank Platoon (USA) aggregate with the CORRECT DIS type and then
// task it along R9's EXACT Mojave route via bare CreateRoute + MoveAlongRoute. This is
// "R9's exact path but with the CORRECT type" - the whole Cell C test. If the platoon MOVES,
// the R9 freeze was TYPE-MAPPING (the interface passed 11.1.225.1.1.3.0 -> Ground_Aggregate,
// which cannot build member offset routes); the fix is then the type table alone, no plan work.
//
// Cloned from tools/CreateOne (join / wait-backend / act / tick / resign). Like CreateOne it
// has NO default appNumber - a missing one is a hard exit 2, because a reused application
// number is the stale-federate trigger (RUNBOOK sec 0). ADDITIVE: no existing file is touched.
//
// TWO EXPLICITLY-GATED PHASES (separate invocations), because the run pipeline interleaves an
// EXTERNAL RunSim + oracle-reflection check between create and task:
//   1) create  - into the PAUSED loaded base; prints the aggregate uuid. Then the operator
//                 verifies the unit reflects (WatchVrf) and starts the sim (RunSim).
//   2) task    - AFTER RunSim: CreateRoute (R9's waypoints) + MoveAlongRoute, addressed by the
//                 aggregate uuid printed by phase 1 (passed in as an argument).
// The created aggregate is BACKEND-owned (remote-control model, same as CreateOne), so it
// persists after the create-phase federate resigns; the task-phase federate re-joins to task it.
//
// LAUNCH ENV (identical to CreateOne - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.0.2\bin64.
//   $env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
//   $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
//   Push-Location C:\MAK\vrforces5.0.2\bin64
//   & <repo>\tools\CreateTaskAgg\bin\Release\net10.0\win-x64\CreateTaskAgg.exe create <appNo>
//   & <repo>\tools\CreateTaskAgg\bin\Release\net10.0\win-x64\CreateTaskAgg.exe task   <appNo> <aggUuid>
//   Pop-Location
//
// CORRECT TYPE (VERIFIED, offline): Tank Platoon (USA) is registered in the loaded model set
// (C:\MAK\...\EntityLevel\vrfSim\Tank Platoon (USA).entity) as objectType 3:11:1:225:3:2:0:0,
// matchType 3:11:1:225:3:2:-1:-1, carrying the ground-disaggregated-movement sysdef (the
// offset-route path R9 found empty) and 4 M1A2 subordinates. So the managed EntityTypeSpec is
// Kind=11 Domain=1 Country=225 Category=3 Subcategory=2 Specific=0 Extra=0. NOTE: CreateAggregate
// takes a DIS ENUM, not a template name; VR-Forces resolves the enum -> template at the backend.
// Whether that resolution yields THIS template with its subsystems is exactly what the live run
// confirms (see the report's BLOCKER QUESTION).
//
// R9's EXACT PATH (data/R9_Mojave_UnitMove_Order.xml T_R5_PL1 + R9 POS t=3 start; the interface
// prepends the taskee's live location as route point 0 - VrfC2SimService.cs:702/724): a 3-vertex
// eastward route at constant latitude 34.612956:
//   P0 start -116.600487  (1222.MechPlt live pos, R9 trace)
//   P1       -116.594174  (order waypoint 1)
//   P2       -116.587860  (order waypoint 2)
// Route vertices default to 100 m MSL (the Fixed100 golden-parity value R9 used; VrfC2SimService.cs:722).
// Waypoint altitude is a FALSIFIED freeze cause (below-terrain variant moved), so 100 MSL is safe.
//
// FALSE-GREEN DISCIPLINE (a prior session had six tools report success while doing nothing):
//   - non-zero exit on ANY absent backend / missing ObjectCreated / null uuid;
//   - every uuid/handle received is echoed;
//   - the create is CONFIRMABLE (ObjectCreated returns the aggregate uuid);
//   - MoveAlongRoute returns VOID (facade moveAlongRoute at VrfFacade.cpp:499 is fire-and-forget)
//     so the tool CANNOT confirm the task took effect from a return value. The task-phase
//     confirmation signal is the LIVE ORACLE: WatchVrf must show the platoon static-while-paused
//     then moving after RunSim, a +4 reflected transient delta (member offset routes; the literal
//     9->13 of the fixture runs was fixture-specific - TropicTortoise's baseline count differs),
//     settling ~east along the route. The tool only guarantees the route was CREATED (uuid echoed)
//     and the move was ISSUED without error.

static int Fail(string msg)
{
    Console.WriteLine("[FAIL] " + msg);
    Console.WriteLine();
    Console.WriteLine("usage:");
    Console.WriteLine("  CreateTaskAgg.exe create <appNumber> [name] [lat] [lon] [alt] [headingDeg] [federation]");
    Console.WriteLine("  CreateTaskAgg.exe task   <appNumber> <aggUuid> [routeAltMeters] [federation]");
    Console.WriteLine();
    Console.WriteLine("  appNumber is MANDATORY and must be FRESH (Appendix B ledger; never reuse).");
    Console.WriteLine("  task's aggUuid is the uuid the create phase printed.");
    Console.WriteLine();
    Console.WriteLine("example:");
    Console.WriteLine("  CreateTaskAgg.exe create 3600");
    Console.WriteLine("  CreateTaskAgg.exe task   3601 5a3ca430-1234-5678-9abc-def012345678");
    return 2;
}

// ---- Cell C constants (verified; see the header) -----------------------------------------
const string Federation = "CWIX-2024";
const string DefaultAggName = "CELLC_TANKPLT";
const string RouteName = "CELLC_ROUTE";

// R9 golden 1222.MechPlt start (data/R9_Mojave_*, R9_region_swap_2026-07-13.txt POS t=3).
const double StartLat = 34.612956;
const double StartLon = -116.600487;
// R9 T_R5_PL1 order waypoints (data/R9_Mojave_UnitMove_Order.xml), same latitude, eastward.
const double Wp1Lon = -116.594173672085;
const double Wp2Lon = -116.587860401830;
const double DefaultBirthAlt = 10000.0;   // safe-high MSL create; ground clamp brings a ground unit down (CreateOne convention).
const double DefaultRouteAlt = 100.0;     // Fixed100 golden-parity route-vertex MSL (VrfC2SimService.cs:722).

// Tank Platoon (USA) DIS enum - objectType 3:11:1:225:3:2:0:0 (the '3' is the VRF class = aggregate).
static EntityTypeSpec TankPlatoonUsaType() => new()
{
    Kind = 11, Domain = 1, Country = 225, Category = 3, Subcategory = 2, Specific = 0, Extra = 0,
};

// ------------------------------------------------------------------------------------------

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
var flags = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (flags.Length > 0)
    return Fail($"unknown option(s): {string.Join(" ", flags)}. CreateTaskAgg takes positional "
              + "arguments only and has NO --dry-run mode - every phase performs a real action.");

if (positional.Length < 1) return Fail("missing phase (create|task).");
string phase = positional[0].ToLowerInvariant();
if (phase != "create" && phase != "task")
    return Fail($"unknown phase '{positional[0]}' (expected 'create' or 'task').");

if (positional.Length < 2) return Fail("missing appNumber.");
if (!int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var appNumber)
    || appNumber <= 0)
    return Fail($"appNumber '{positional[1]}' is not a positive integer.");

static StartupConfig MakeConfig(int appNumber, string federation)
{
    // FED / FOM must match VR-Forces' running federation (RUNBOOK sec 7) - same constants
    // CreateOne / WatchVrf use.
    var cfg = new StartupConfig
    {
        Protocol = VrfProtocol.Hla1516e,
        ApplicationNumber = appNumber,
        SiteId = 1,
        SessionId = 1,
        HostInetAddr = "127.0.0.1",
        Federation = federation,
        FedFileName = "RPR_FOM_v2.0_1516-2010.xml",
    };
    cfg.FomModules.Add("MAK-VRFExt-6_evolved.xml");
    cfg.FomModules.Add("MAK-DIGuy-7_evolved.xml");
    cfg.FomModules.Add("MAK-LgrControl-2_evolved.xml");
    return cfg;
}

// Join + wait for a backend to be discovered. Returns null on failure (already logged + resigned).
static VrfBridge JoinAndWaitBackend(StartupConfig cfg)
{
    var bridge = new VrfBridge();
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.WriteLine("[FAIL] bridge.Start() returned false. Check: RTI 4.6.1 on PATH, " +
                          "MAKLMGRD_LICENSE_FILE (Machine), FED/FOM, cwd = VRF bin64, fresh appNumber.");
        try { bridge.Stop(); } catch { /* best effort */ }
        return null;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()}).");

    Console.WriteLine("[..] waiting for a backend to be discovered (15 s cap)...");
    var swBe = Stopwatch.StartNew();
    while (bridge.BackendCount() == 0 && swBe.Elapsed < TimeSpan.FromSeconds(15))
    {
        bridge.Tick();
        Thread.Sleep(50);
    }
    if (bridge.BackendCount() == 0)
    {
        Console.WriteLine("[FAIL] no backend discovered after 15 s. Refusing to act - it would be a " +
                          "silent no-op reported as success. Confirm VR-Forces is up with a scenario " +
                          "loaded and the RTI connection is the one the backend uses.");
        bridge.Stop();
        return null;
    }
    Console.WriteLine($"[OK] backend discovered (BackendCount={bridge.BackendCount()}) after {swBe.Elapsed.TotalSeconds:F1}s.");
    return bridge;
}

return phase == "create" ? RunCreate() : RunTask();

// ============================ CREATE PHASE ================================================
int RunCreate()
{
    string name = DefaultAggName;
    double lat = StartLat, lon = StartLon, alt = DefaultBirthAlt, headingDeg = 90.0;
    string federation = Federation;

    if (positional.Length >= 3 && !string.IsNullOrWhiteSpace(positional[2])) name = positional[2];
    if (positional.Length >= 4 && !double.TryParse(positional[3], NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
        return Fail($"lat '{positional[3]}' is not a number.");
    if (positional.Length >= 5 && !double.TryParse(positional[4], NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
        return Fail($"lon '{positional[4]}' is not a number.");
    if (positional.Length >= 6 && !double.TryParse(positional[5], NumberStyles.Float, CultureInfo.InvariantCulture, out alt))
        return Fail($"alt '{positional[5]}' is not a number.");
    if (positional.Length >= 7 && !double.TryParse(positional[6], NumberStyles.Float, CultureInfo.InvariantCulture, out headingDeg))
        return Fail($"headingDeg '{positional[6]}' is not a number.");
    if (positional.Length >= 8 && !string.IsNullOrWhiteSpace(positional[7])) federation = positional[7];

    if (!double.IsFinite(lat) || lat < -90 || lat > 90)   return Fail($"lat {lat} out of range (-90..90).");
    if (!double.IsFinite(lon) || lon < -180 || lon > 180) return Fail($"lon {lon} out of range (-180..180).");
    if (!double.IsFinite(alt) || !double.IsFinite(headingDeg)) return Fail("alt/headingDeg must be finite.");

    Console.WriteLine("=== CreateTaskAgg CREATE - remote-create a Tank Platoon (USA) with the CORRECT type ===");
    Console.WriteLine($"    federation={federation}  appNumber={appNumber}  (use a FRESH appNumber each join)");
    Console.WriteLine("    type=Tank Platoon (USA)  DIS 11.1.225.3.2.0.0  (class 3 aggregate; disaggregated + subordinates)");
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
        "    name='{0}'  pos=({1:F6}, {2:F6}) alt={3:F1} m MSL  heading={4:F1} deg", name, lat, lon, alt, headingDeg));
    Console.WriteLine();

    VrfBridge bridge = null;
    string createdUuid = null, createdEntityId = null;
    try
    {
        bridge = JoinAndWaitBackend(MakeConfig(appNumber, federation));
        if (bridge == null) return 1;

        // Creation is ASYNC - the backend answers on ObjectCreated. Subscribe BEFORE issuing the
        // create so a fast reply cannot be missed. Match by NAME so a member's callback (the 4 M1A2
        // subordinates also raise ObjectCreated) cannot be mistaken for the aggregate.
        bridge.ObjectCreated += (s, e) =>
        {
            if (createdUuid != null) return;
            if (!string.Equals(e.Name, name, StringComparison.Ordinal))
            {
                Console.WriteLine($"[..] (subordinate/other ObjectCreated: name='{e.Name}' uuid={e.Uuid})");
                return;
            }
            createdUuid = e.Uuid;
            createdEntityId = e.EntityId;
            Console.WriteLine($"[OK] ObjectCreated (aggregate): name='{e.Name}' uuid={e.Uuid} entityId={e.EntityId}");
        };

        Console.WriteLine("[..] issuing CreateAggregate (Disaggregated, createSubordinates=true)...");
        bridge.CreateAggregate(TankPlatoonUsaType(),
                               new Geodetic { LatDeg = lat, LonDeg = lon, AltMeters = alt },
                               Force.Friendly, headingDeg, name,
                               AggregateState.Disaggregated, true);

        Console.WriteLine("[..] ticking for the ObjectCreated reply (20 s cap)...");
        var sw = Stopwatch.StartNew();
        while (createdUuid == null && sw.Elapsed < TimeSpan.FromSeconds(20))
        {
            bridge.Tick();
            Thread.Sleep(50);
        }
        if (createdUuid == null)
        {
            Console.WriteLine("[FAIL] no ObjectCreated reply for the aggregate within 20 s. It may or may not " +
                              "exist - check the VR-Forces GUI and WatchVrf before creating another.");
            bridge.Stop();
            return 1;
        }

        // Flush the create to the backend before resigning (same 3 s posture as CreateOne).
        Console.WriteLine("[..] flushing (ticking ~3 s)...");
        var swFlush = Stopwatch.StartNew();
        while (swFlush.Elapsed < TimeSpan.FromSeconds(3)) { bridge.Tick(); Thread.Sleep(50); }

        Console.WriteLine("[..] bridge.Stop() - resigning (the aggregate is backend-owned; it persists)...");
        bridge.Stop();
        Console.WriteLine("[OK] resigned cleanly.");
        Console.WriteLine();
        Console.WriteLine("=== RESULT (CREATE) ===");
        Console.WriteLine($"    aggregate uuid : {createdUuid}");
        Console.WriteLine($"    entityId       : {createdEntityId}");
        Console.WriteLine($"    name           : {name}");
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
            "    birth pos      : lat={0:F6} lon={1:F6} alt={2:F1} MSL", lat, lon, alt));
        Console.WriteLine();
        Console.WriteLine("    NEXT: verify this aggregate REFLECTS (WatchVrf shows it + members, static while paused),");
        Console.WriteLine("    then RunSim (cwd=bin64, mult 1), then run the TASK phase:");
        Console.WriteLine($"        CreateTaskAgg.exe task <freshAppNo> {createdUuid}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
        try { bridge?.Stop(); } catch { /* best effort - never leave a joined federate */ }
        return 1;
    }
}

// ============================= TASK PHASE ================================================
int RunTask()
{
    if (positional.Length < 3 || string.IsNullOrWhiteSpace(positional[2]))
        return Fail("task phase requires the aggregate uuid printed by the create phase.");
    string aggUuid = positional[2];
    double routeAlt = DefaultRouteAlt;
    string federation = Federation;
    if (positional.Length >= 4 && !double.TryParse(positional[3], NumberStyles.Float, CultureInfo.InvariantCulture, out routeAlt))
        return Fail($"routeAltMeters '{positional[3]}' is not a number.");
    if (positional.Length >= 5 && !string.IsNullOrWhiteSpace(positional[4])) federation = positional[4];
    if (!double.IsFinite(routeAlt)) return Fail("routeAltMeters must be finite.");

    // R9's exact 3-vertex eastward route (see the header). Point 0 = R9's known start (NOT read
    // live: TryGetEntityGeodetic returns false for a disaggregated aggregate - VrfBridge.cpp:353 /
    // PORT.md sec 8 - so hardcoding R9's start both reproduces R9's path AND sidesteps that bug).
    var routePts = new List<Geodetic>
    {
        new() { LatDeg = StartLat, LonDeg = StartLon, AltMeters = routeAlt },
        new() { LatDeg = StartLat, LonDeg = Wp1Lon,   AltMeters = routeAlt },
        new() { LatDeg = StartLat, LonDeg = Wp2Lon,   AltMeters = routeAlt },
    };

    Console.WriteLine("=== CreateTaskAgg TASK - CreateRoute + MoveAlongRoute (R9's exact path) ===");
    Console.WriteLine($"    federation={federation}  appNumber={appNumber}  aggUuid={aggUuid}");
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
        "    route '{0}' 3 pts @ {1:F1} m MSL: ({2:F6},{3:F6}) -> ({2:F6},{4:F6}) -> ({2:F6},{5:F6})",
        RouteName, routeAlt, StartLat, StartLon, Wp1Lon, Wp2Lon));
    Console.WriteLine();

    VrfBridge bridge = null;
    string routeUuid = null;
    try
    {
        bridge = JoinAndWaitBackend(MakeConfig(appNumber, federation));
        if (bridge == null) return 1;

        // CreateRoute is ASYNC; the along-route move is deferred until the route's ObjectCreated
        // fires (parity: VrfC2SimService.cs:870-929 / C2SIMinterface.cpp:2408-2421). Subscribe first.
        bridge.ObjectCreated += (s, e) =>
        {
            if (routeUuid != null) return;
            if (!string.Equals(e.Name, RouteName, StringComparison.Ordinal))
            {
                Console.WriteLine($"[..] (other ObjectCreated: name='{e.Name}' uuid={e.Uuid})");
                return;
            }
            routeUuid = e.Uuid;
            Console.WriteLine($"[OK] ObjectCreated (route): name='{e.Name}' uuid={e.Uuid}");
        };

        Console.WriteLine("[..] issuing CreateRoute...");
        bridge.CreateRoute(routePts, RouteName);

        Console.WriteLine("[..] ticking for the route ObjectCreated reply (20 s cap)...");
        var sw = Stopwatch.StartNew();
        while (routeUuid == null && sw.Elapsed < TimeSpan.FromSeconds(20))
        {
            bridge.Tick();
            Thread.Sleep(50);
        }
        if (routeUuid == null)
        {
            Console.WriteLine("[FAIL] no ObjectCreated reply for the route within 20 s. NOT issuing MoveAlongRoute " +
                              "(it would be a silent no-op). Check VR-Forces / WatchVrf.");
            bridge.Stop();
            return 1;
        }

        // Address the route by NAME - this is the shipped, C++-parity convention the working
        // interface uses (VrfC2SimService.cs:1115 passes e.Name to MoveAlongRoute; R9's aggregate
        // DID receive the move this way - its freeze was the EMPTY OFFSET route, not an unresolved
        // route). The route uuid is echoed above for the record.
        Console.WriteLine($"[..] issuing MoveAlongRoute(agg={aggUuid}, route='{RouteName}')...");
        bridge.MoveAlongRoute(aggUuid, RouteName);
        Console.WriteLine("[OK] MoveAlongRoute ISSUED. NOTE: the facade call is VOID (fire-and-forget) - the tool " +
                          "cannot confirm the task took effect. The LIVE ORACLE is the confirmation (see RESULT).");

        // Flush before resigning.
        Console.WriteLine("[..] flushing (ticking ~3 s)...");
        var swFlush = Stopwatch.StartNew();
        while (swFlush.Elapsed < TimeSpan.FromSeconds(3)) { bridge.Tick(); Thread.Sleep(50); }

        Console.WriteLine("[..] bridge.Stop() - resigning...");
        bridge.Stop();
        Console.WriteLine("[OK] resigned cleanly.");
        Console.WriteLine();
        Console.WriteLine("=== RESULT (TASK) ===");
        Console.WriteLine($"    aggregate uuid : {aggUuid}");
        Console.WriteLine($"    route name     : {RouteName}");
        Console.WriteLine($"    route uuid     : {routeUuid}");
        Console.WriteLine("    move           : MoveAlongRoute issued (VOID call - unconfirmable by this tool).");
        Console.WriteLine();
        Console.WriteLine("    CONFIRMATION SIGNAL (live oracle, NOT this tool): WatchVrf must show the platoon");
        Console.WriteLine("    static-while-paused then MOVING after RunSim, a +4 reflected transient delta over the");
        Console.WriteLine("    scenario baseline (member offset routes - the R9 buildOffsetRoute path; the fixture");
        Console.WriteLine("    runs' literal 9->13 was fixture-specific), settling ~east along the route.");
        Console.WriteLine("    MOVES => the R9 freeze was TYPE-MAPPING. FREEZES => creation/tasking path (Cells B/D).");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
        try { bridge?.Stop(); } catch { /* best effort - never leave a joined federate */ }
        return 1;
    }
}
