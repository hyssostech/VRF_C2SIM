using System.Diagnostics;
using System.Globalization;
using System.Linq;
using VrfC2Sim;

// tools/RunSim - START the VR-Forces simulation clock (Run) by REMOTE CONTROL.
//
// Joins the federation, issues DtVrfRemoteController::run() via VrfBridge.Run()
// (VrfBridge.cpp:206 -> VrfFacade::Run -> controller->run()), optionally sets a time
// multiplier, flushes to the backends, and resigns CLEANLY. Pure VR-Forces: no C2SIM/STOMP.
// It creates, deletes and tasks NOTHING - the only state it changes is play/pause + rate.
//
// WHY THIS TOOL EXISTS: a scenario loaded via LaunchVrf starts PAUSED. The only in-repo
// caller of Run() is VrfC2SimApp (VrfC2SimService.cs:214/354), which is bound to the C2SIM
// server flow. To RUN a STOCK (non-C2SIM) scenario headless as a known-good baseline - MAK's
// own authored units, zero of our creation/tasking code - we need a bare Run() sender.
// Same pattern and launch env as tools/SetSimRate.
//
// LAUNCH ENV (identical to SetSimRate/app - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE (Machine), cwd = C:\MAK\vrforces5.0.2\bin64, FRESH appNumber.
//
// Args: <applicationNumber> [multiplier] [federation]
//   applicationNumber  REQUIRED. NO DEFAULT. A fresh, ledgered appNo every run (RUNBOOK
//                      sec 7); reusing one steals a federate slot from WatchVrf etc.
//   multiplier         Optional whole number > 0. If > 1, ALSO SetTimeMultiplier. Default 1.
//   federation         Optional, default CWIX-2024 (must match the running federation).

const string DefaultFederation = "CWIX-2024";

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("usage: RunSim.exe <applicationNumber> [multiplier] [federation]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  applicationNumber  REQUIRED. NO DEFAULT - use a FRESH, ledgered appNo every");
    Console.Error.WriteLine("                     run (RUNBOOK sec 7). Reusing one steals a federate slot.");
    Console.Error.WriteLine("  multiplier         Optional. Whole number > 0 (default 1 = real time). If > 1,");
    Console.Error.WriteLine("                     also issues SetTimeMultiplier so movement is visible faster.");
    Console.Error.WriteLine("  federation         Optional. Default 'CWIX-2024'.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("examples:  RunSim.exe 3550          # play at real time");
    Console.Error.WriteLine("           RunSim.exe 3550 10       # play at 10x");
    return 2;
}

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

// -- argument validation: fail LOUDLY and non-zero, never guess a default ---------

if (positional.Length == 0)
    return Usage("Missing <applicationNumber>. It is REQUIRED and has NO default - " +
                 "supply a FRESH appNo that is not in use by VR-Forces, WatchVrf, or any prior run.");

if (!int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int appNumber))
    return Usage($"ApplicationNumber '{positional[0]}' is not an integer.");

if (appNumber <= 0 || appNumber > 65535)
    return Usage($"ApplicationNumber {appNumber} is out of range (expected 1..65535).");

int multiplier = 1;
if (positional.Length >= 2)
{
    if (!int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out multiplier))
        return Usage($"Multiplier '{positional[1]}' is not an integer (the bridge takes SetTimeMultiplier(int)).");
    if (multiplier <= 0)
        return Usage($"Multiplier must be greater than 0; got {multiplier}. Use 1 for real time.");
    if (multiplier > 100)
        Console.WriteLine($"[WARN] multiplier {multiplier} is unusually high - confirm this is not a typo.");
}

string federation = positional.Length >= 3 && !string.IsNullOrWhiteSpace(positional[2])
    ? positional[2]
    : DefaultFederation;

// FED / FOM must match VR-Forces' running federation (appsettings.json Vrf, RUNBOOK sec 7).
// Kept identical to tools/SetSimRate / tools/ResetVrf.
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

Console.WriteLine("=== RunSim - START the VR-Forces simulation clock (remote control) ===");
Console.WriteLine($"    federation={federation}  appNumber={appNumber}  multiplier={multiplier}x");
Console.WriteLine($"    started {DateTime.Now:yyyy-MM-dd HH:mm:ss} local / {DateTime.UtcNow:HH:mm:ss} UTC");
Console.WriteLine("    ACTION: controller->run() (play) on ALL backends" +
                  (multiplier > 1 ? $", then set {multiplier}x." : ".") +
                  " This tool creates/deletes/tasks NOTHING.\n");

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();

    // 1. JOIN the federation.
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.Error.WriteLine("[FAIL] bridge.Start() returned false - NOT joined, Run NOT sent. " +
                                "Check: RTI 4.6.1 on PATH, MAKLMGRD_LICENSE_FILE (Machine), FED/FOM, " +
                                "cwd = VRF bin64, fresh appNumber, VR-Forces actually running.");
        return 1;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()} immediately after Start).");

    // 2. SETTLE: tick until a backend is actually discovered. LOAD-BEARING - backends are NOT
    //    known at the instant Start() returns (same idiom as tools/SetSimRate / ResetVrf).
    //    Issuing Run() against zero known backends risks a silent no-op reported as success.
    Console.WriteLine("[..] settling - ticking until a backend is discovered (up to 15 s)...");
    var swSettle = Stopwatch.StartNew();
    int backends = 0;
    while (swSettle.Elapsed < TimeSpan.FromSeconds(15))
    {
        bridge.Tick();
        Thread.Sleep(50);
        backends = bridge.BackendCount();
        if (backends > 0) break;
    }

    if (backends == 0)
    {
        Console.Error.WriteLine($"[FAIL] no backend discovered after {swSettle.Elapsed.TotalSeconds:F0} s " +
                                "(BackendCount=0). Run() was NOT sent - sending it now would be a silent " +
                                "no-op reported as success. Confirm VR-Forces is running with a scenario " +
                                "loaded and a simulation backend connected, then retry with a FRESH appNumber.");
        Console.Error.WriteLine("[..] bridge.Stop() - resigning cleanly...");
        bridge.Stop();
        Console.Error.WriteLine($"[OK] resigned. Mark appNumber {appNumber} as USED.");
        return 1;
    }
    Console.WriteLine($"[OK] {backends} backend(s) discovered after {swSettle.Elapsed.TotalSeconds:F1} s.");

    // 3. Issue the remote calls. run() / setTimeMultiplier have no address argument
    //    (vrfRemoteController.h:819/:827), so they apply to ALL backends.
    Console.WriteLine("[..] bridge.Run() - issuing controller->run() (start the clock)...");
    bridge.Run();
    if (multiplier > 1)
    {
        Console.WriteLine($"[..] bridge.SetTimeMultiplier({multiplier}) - issuing...");
        bridge.SetTimeMultiplier(multiplier);
    }
    Console.WriteLine("[OK] command(s) issued (queued on the controller).");

    // 4. Tick to flush the message(s) to the backends. LOAD-BEARING: the calls only queue
    //    onto the remote controller; without ticks this process exits before they leave.
    Console.WriteLine("[..] flushing (ticking ~3 s)...");
    var swFlush = Stopwatch.StartNew();
    while (swFlush.Elapsed < TimeSpan.FromSeconds(3)) { bridge.Tick(); Thread.Sleep(50); }
    Console.WriteLine("[OK] flushed.");

    // 5. Clean stop -> resign. NEVER force-kill a joined federate (RUNBOOK sec 0).
    Console.WriteLine("[..] bridge.Stop() - resigning from the federation...");
    bridge.Stop();
    Console.WriteLine($"[OK] resigned cleanly. RESULT: simulation clock STARTED (Run)" +
                      (multiplier > 1 ? $" at {multiplier}x" : "") +
                      $" (appNumber={appNumber}, federation={federation}) at " +
                      $"{DateTime.Now:HH:mm:ss} local / {DateTime.UtcNow:HH:mm:ss} UTC.");
    Console.WriteLine("     There is no getter on the remote controller for run-state, so this tool CANNOT " +
                      "read it back - confirm movement via WatchVrf telemetry.");
    Console.WriteLine($"     Mark appNumber {appNumber} as USED in the ledger.");
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"[FAIL] {e.GetType().Name}: {e.Message}");
    Console.Error.WriteLine(e.StackTrace);
    Console.Error.WriteLine("[FAIL] Run may or may not have been applied - VERIFY via WatchVrf. " +
                            $"Mark appNumber {appNumber} as USED regardless.");
    return 1;
}
finally
{
    bridge?.Dispose();
}
