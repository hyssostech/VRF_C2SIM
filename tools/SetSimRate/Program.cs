using System.Diagnostics;
using System.Globalization;
using System.Linq;
using VrfC2Sim;

// tools/SetSimRate - set the VR-Forces simulation time multiplier by REMOTE CONTROL.
//
// Joins the federation, issues DtVrfRemoteController::setTimeMultiplier
// (vrfRemoteController.h:827) via VrfBridge.SetTimeMultiplier (VrfBridge.cpp:208),
// flushes it to the backends, and resigns CLEANLY. Pure VR-Forces: no C2SIM / STOMP.
// It creates, deletes and tasks NOTHING - the only state it changes is the sim clock rate.
//
// WHY THIS TOOL EXISTS (Phase 1 Step 4, decision D1): the only other caller of
// SetTimeMultiplier in the port is VrfC2SimApp (VrfC2SimService.cs:215-216), and it is
// unusable here for three independent reasons: it fires once inside the C2SIM late-join
// block, it needs a running C2SIM server, and its guard is `if (TimeMultiplier > 1)` so it
// can NEVER restore 1x. This tool sets the multiplier BOTH up and back down.
// The GUI Time Scale toolbar is not an alternative above 15x (it is capped at 15 by
// default: myTimescaleHigh=15 in default_GuiSettings.grsx).
//
// LAUNCH ENV (identical to the app - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.0.2\bin64, and a FRESH
// ApplicationNumber each run (a lingering federate steals the slot). Example (PowerShell):
//   $env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
//   $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
//   Push-Location C:\MAK\vrforces5.0.2\bin64
//   & <repo>\tools\SetSimRate\bin\Release\net10.0\win-x64\SetSimRate.exe 20 3457
//   Pop-Location
//
// Args: <multiplier> <applicationNumber> [federation]
//   multiplier          REQUIRED. > 0 and a whole number (1, "1.0", 20). The bridge
//                       signature is SetTimeMultiplier(int) - VrfFacade.h:219 - so a
//                       fractional value cannot be represented and is REJECTED rather
//                       than silently truncated.
//   applicationNumber   REQUIRED. NO DEFAULT, BY DESIGN. A baked-in default invites
//                       silent reuse of a burned appNo, which violates the never-reuse
//                       rule (RUNBOOK sec 7) and steals a federate slot from a tool that
//                       may be observing - e.g. WatchVrf. Missing => hard failure.
//   federation          Optional, default CWIX-2024 (must match the running federation).

const string DefaultFederation = "CWIX-2024";

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("usage: SetSimRate.exe <multiplier> <applicationNumber> [federation]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  multiplier         REQUIRED. Simulation time multiplier: > 0, whole number.");
    Console.Error.WriteLine("                     1 = real time (use this to restore after a fast run).");
    Console.Error.WriteLine("                     '1.0' is accepted and means 1. Fractional values are");
    Console.Error.WriteLine("                     rejected: the bridge takes an int (VrfFacade.h:219).");
    Console.Error.WriteLine("  applicationNumber  REQUIRED. NO DEFAULT - use a FRESH, ledgered appNo every");
    Console.Error.WriteLine("                     run (RUNBOOK sec 7). Reusing one steals a federate slot.");
    Console.Error.WriteLine("  federation         Optional. Default 'CWIX-2024'.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("examples:  SetSimRate.exe 20 3457      # go to 20x");
    Console.Error.WriteLine("           SetSimRate.exe 1  3458      # back to real time");
    return 2;
}

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

// -- argument validation: fail LOUDLY and non-zero, never guess a default ---------

if (positional.Length == 0)
    return Usage("No arguments. Both <multiplier> and <applicationNumber> are required.");

if (positional.Length < 2)
    return Usage("Missing <applicationNumber>. It is REQUIRED and has NO default - " +
                 "supply a FRESH appNo that is not in use by VR-Forces, WatchVrf, or any prior run.");

if (!double.TryParse(positional[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double rawMultiplier))
    return Usage($"Multiplier '{positional[0]}' is not a number.");

if (double.IsNaN(rawMultiplier) || double.IsInfinity(rawMultiplier))
    return Usage($"Multiplier '{positional[0]}' is not a finite number.");

if (rawMultiplier <= 0)
    return Usage($"Multiplier must be greater than 0; got {rawMultiplier.ToString(CultureInfo.InvariantCulture)}. " +
                 "Use 1 for real time. (0 or negative would stop or reverse the clock and is not a " +
                 "supported operation for this tool.)");

if (rawMultiplier != Math.Floor(rawMultiplier))
    return Usage($"Multiplier {rawMultiplier.ToString(CultureInfo.InvariantCulture)} is fractional. " +
                 "The bridge signature is SetTimeMultiplier(int) (VrfFacade.h:219, VrfBridge.cpp:208), " +
                 "so a fractional multiplier CANNOT be represented. Refusing to truncate silently - " +
                 "pass a whole number.");

if (rawMultiplier > int.MaxValue)
    return Usage($"Multiplier {rawMultiplier.ToString(CultureInfo.InvariantCulture)} is out of range for int.");

int multiplier = (int)rawMultiplier;

if (!int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int appNumber))
    return Usage($"ApplicationNumber '{positional[1]}' is not an integer.");

if (appNumber <= 0 || appNumber > 65535)
    return Usage($"ApplicationNumber {appNumber} is out of range (expected 1..65535).");

string federation = positional.Length >= 3 && !string.IsNullOrWhiteSpace(positional[2])
    ? positional[2]
    : DefaultFederation;

// Soft guard: a plausible-but-wrong big number is far more likely a typo (200 for 20) than
// an intent. Warn loudly, but do not block - the operator may genuinely want it.
if (multiplier > 100)
    Console.WriteLine($"[WARN] multiplier {multiplier} is unusually high - confirm this is not a typo.");

// FED / FOM must match VR-Forces' running federation (appsettings.json Vrf, RUNBOOK sec 7).
// These are environment constants, not app logic; if VR-Forces' command line differs, read
// its --fedFileName / --fomModules and edit here. Kept identical to tools/ResetVrf.
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

Console.WriteLine("=== SetSimRate - set the VR-Forces simulation time multiplier (remote control) ===");
Console.WriteLine($"    federation={federation}  appNumber={appNumber}  multiplier={multiplier}x");
Console.WriteLine($"    started {DateTime.Now:yyyy-MM-dd HH:mm:ss} local / {DateTime.UtcNow:HH:mm:ss} UTC");
Console.WriteLine($"    ACTION: set simulation clock to {multiplier}x real time on ALL backends.");
Console.WriteLine("    This tool creates/deletes/tasks NOTHING. (use a FRESH appNumber each run)\n");

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();

    // 1. JOIN the federation.
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.Error.WriteLine("[FAIL] bridge.Start() returned false - NOT joined, multiplier NOT set. " +
                                "Check: RTI 4.6.1 on PATH, MAKLMGRD_LICENSE_FILE (Machine), FED/FOM, " +
                                "cwd = VRF bin64, fresh appNumber, VR-Forces actually running.");
        return 1;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()} immediately after Start).");

    // 2. SETTLE: tick until a backend is actually discovered. LOAD-BEARING - backends are
    //    NOT known at the instant Start() returns; they are discovered over subsequent
    //    ticks (this is why tools/ResetVrf ticks before it trusts BackendCount /
    //    discovery). Issuing setTimeMultiplier against zero known backends risks a silent
    //    no-op: the tool would report success while the clock never changed.
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
                                "(BackendCount=0). The multiplier was NOT sent - sending it now would be a " +
                                "silent no-op reported as success. Confirm VR-Forces is running with a " +
                                "scenario loaded and a simulation backend connected, then retry with a " +
                                "FRESH appNumber.");
        Console.Error.WriteLine("[..] bridge.Stop() - resigning cleanly...");
        bridge.Stop();
        Console.Error.WriteLine($"[OK] resigned. Mark appNumber {appNumber} as USED.");
        return 1;
    }
    Console.WriteLine($"[OK] {backends} backend(s) discovered after {swSettle.Elapsed.TotalSeconds:F1} s.");

    // 3. Issue the remote call. setTimeMultiplier has no address argument
    //    (vrfRemoteController.h:827), so it applies to ALL backends.
    Console.WriteLine($"[..] SetTimeMultiplier({multiplier}) - issuing remote control message...");
    bridge.SetTimeMultiplier(multiplier);
    Console.WriteLine("[OK] command issued (queued on the controller).");

    // 4. Tick to flush the message to the backends. LOAD-BEARING: SetTimeMultiplier only
    //    queues onto the remote controller; without ticks this process exits before the
    //    message ever leaves. Same idiom as the delete flush in tools/ResetVrf.
    Console.WriteLine("[..] flushing (ticking ~3 s)...");
    var swFlush = Stopwatch.StartNew();
    while (swFlush.Elapsed < TimeSpan.FromSeconds(3)) { bridge.Tick(); Thread.Sleep(50); }
    Console.WriteLine("[OK] flushed.");

    // 5. Clean stop -> resign. NEVER force-kill a joined federate (RUNBOOK sec 0).
    Console.WriteLine("[..] bridge.Stop() - resigning from the federation...");
    bridge.Stop();
    Console.WriteLine($"[OK] resigned cleanly. RESULT: simulation time multiplier set to {multiplier}x " +
                      $"(appNumber={appNumber}, federation={federation}) at " +
                      $"{DateTime.Now:HH:mm:ss} local / {DateTime.UtcNow:HH:mm:ss} UTC.");
    Console.WriteLine("     VERIFY IN THE GUI: there is no getter on the remote controller " +
                      "(vrfRemoteController.h has no timeMultiplier() accessor), so this tool CANNOT " +
                      "read the value back. Confirm the rate visually before trusting it.");
    Console.WriteLine($"     Mark appNumber {appNumber} as USED in the ledger.");
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"[FAIL] {e.GetType().Name}: {e.Message}");
    Console.Error.WriteLine(e.StackTrace);
    Console.Error.WriteLine($"[FAIL] multiplier may or may not have been applied - VERIFY IN THE GUI. " +
                            $"Mark appNumber {appNumber} as USED regardless.");
    return 1;
}
finally
{
    bridge?.Dispose();
}
