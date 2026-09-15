using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using VrfC2Sim;
using VrfC2Sim.Tools;

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
// STACK-AWARE (2026-09-15): the federation identity comes from tools/Shared/StackIdentity.cs,
// which reads the bound stack from the loaded native DLLs - never from a build flag. On 5.2
// the join is the CONFIG-FILE join (execName MAK-ONE-2025; empty Federation/FedFileName/
// FomModules, because config FOM modules are ADDITIVE - MIGRATION_DIFF A2/A9); on 5.0.2 it is
// CWIX-2024 + RPR_FOM_v2.0_1516-2010.xml + the 3 MAK modules.
//
// LAUNCH ENV, 5.2 (PREREG_52_LAUNCH_2026-09-03): PATH prefixed with
// C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin,
// RTI_RID_FILE = config\rid-501-rtiexec-min.mtl, RTI_ASSISTANT_DISABLE=1,
// MAKLMGRD_LICENSE_FILE (User scope first, then Machine), cwd = C:\MAK\vrforces5.2d\bin64,
// and a FRESH ApplicationNumber each run.
//   & <repo>\tools\SetSimRate\bin\Release-5.2\net10.0\win-x64\SetSimRate.exe 20 <freshAppNo>
// LAUNCH ENV, 5.0.2 (unchanged - RUNBOOK sec 7): RTI 4.6.1 on PATH, cwd =
// C:\MAK\vrforces5.0.2\bin64, and bin\Release\ instead of bin\Release-5.2\.
//
// Args: <multiplier> <applicationNumber> [federation] [--dry-run] [--help]
//   multiplier          REQUIRED. > 0 and a whole number (1, "1.0", 20). The bridge
//                       signature is SetTimeMultiplier(int) - VrfFacade.h:219 - so a
//                       fractional value cannot be represented and is REJECTED rather
//                       than silently truncated.
//   applicationNumber   REQUIRED. NO DEFAULT, BY DESIGN. A baked-in default invites
//                       silent reuse of a burned appNo, which violates the never-reuse
//                       rule (RUNBOOK sec 7) and steals a federate slot from a tool that
//                       may be observing - e.g. WatchVrf. Missing => hard failure.
//   federation          Optional, default stack-aware (tools/Shared/StackIdentity.cs).
//   --dry-run           Validate the arguments, print the plan and the bound stack, and
//                       EXIT - join nothing, send nothing.
//   --help              Print the plan and the bound native stack; join nothing; exit 0.

// The bound native stack, read from the LOADED DLLs. Same probe as VrfC2SimApp
// --runtime-check; NoInlining so the bridge assembly is resolved only when it is called.
[MethodImpl(MethodImplOptions.NoInlining)]
static string NativeStackLine()
{
    try { return "native stack = " + VrfBridge.NativeStackInfo(); }
    catch (Exception e) { return "native stack = UNAVAILABLE (" + e.GetType().Name + ": " + e.Message + ")"; }
}

// Usage text goes to STDERR on an argument error (exit 2) and to STDOUT for --help
// (exit 0), so a runner capturing stdout for data never ingests a failure block.
static void PrintUsage(System.IO.TextWriter w)
{
    w.WriteLine("usage: SetSimRate.exe <multiplier> <applicationNumber> [federation] [--dry-run]");
    w.WriteLine("       SetSimRate.exe --help");
    w.WriteLine();
    w.WriteLine("  multiplier         REQUIRED. Simulation time multiplier: > 0, whole number.");
    w.WriteLine("                     1 = real time (use this to restore after a fast run).");
    w.WriteLine("                     '1.0' is accepted and means 1. Fractional values are");
    w.WriteLine("                     rejected: the bridge takes an int (VrfFacade.h:219).");
    w.WriteLine("  applicationNumber  REQUIRED. NO DEFAULT - use a FRESH, ledgered appNo every");
    w.WriteLine("                     run (RUNBOOK sec 7). Reusing one steals a federate slot.");
    w.WriteLine("  federation         Optional. Default is stack-aware (5.0.2 -> CWIX-2024;");
    w.WriteLine("                     5.2 -> connection-config identity; tools/Shared/StackIdentity.cs).");
    w.WriteLine("  --dry-run          Print the plan and EXIT. Joins nothing, sends nothing.");
    w.WriteLine();
    w.WriteLine("examples:  SetSimRate.exe 20 3457      # go to 20x");
    w.WriteLine("           SetSimRate.exe 1  3458      # back to real time");
}

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    PrintUsage(Console.Error);
    return 2;
}

// --config <path> (V6 harvest 2026-09-15): the VR-Link connection config is resolved
// EXPLICITLY - arg > env Vrf__ConnectionConfigFile > the loaded stack's own tree - because the
// vendor default is CWD-RELATIVE and silently falls back to built-in defaults when the cwd is
// not the VR-Forces bin64. Taken out of args FIRST so the parsing below never sees either
// token (tools/Shared/ConnectionConfig.cs).
// --settle-secs N (V6c arm A1): how long to wait for a VR-Forces back end before refusing.
// DEFAULT 15 s - unchanged. Taken out of args before the parsing below, like --config.
if (!SettleCap.TryTakeFlag(args, out args, out int settleSecs, out string settleProblem))
    return Usage(settleProblem);

if (!ConnectionConfig.TryTakeFlag(args, out args, out string connArg, out string connProblem))
    return Usage(connProblem);

bool dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));
bool help = args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase));
var unknownFlags = args.Where(a => a.StartsWith("--", StringComparison.Ordinal))
                       .Where(a => !string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase))
                       .ToArray();
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

// --help is the NO-JOIN plan printer (the runner's self-test convention).
if (help)
{
    Console.WriteLine("=== SetSimRate - set the VR-Forces simulation time multiplier (remote control) ===");
    Console.WriteLine("    " + NativeStackLine());
    var planCfg = new StartupConfig { Protocol = VrfProtocol.Hla1516e, SiteId = 1, SessionId = 1 };
    Console.WriteLine("    " + StackIdentity.Apply(planCfg, positional.Length >= 3 ? positional[2] : null));
    Console.WriteLine("    " + ConnectionConfig.Resolve(connArg).Banner);
    Console.WriteLine();
    Console.WriteLine("    PLAN: join -> tick until a backend is discovered (cap 15 s) ->");
    Console.WriteLine("          SetTimeMultiplier(n) -> flush ~3 s -> resign cleanly.");
    Console.WriteLine("    THIS INVOCATION JOINED NOTHING and changed nothing.");
    Console.WriteLine();
    PrintUsage(Console.Out);
    return 0;
}

if (unknownFlags.Length > 0)
    return Usage($"unknown option(s): {string.Join(" ", unknownFlags)}. SetSimRate accepts --dry-run and --help only.");

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
    : null;   // null = stack default (5.0.2 CWIX-2024; 5.2 config-file identity)

// Soft guard: a plausible-but-wrong big number is far more likely a typo (200 for 20) than
// an intent. Warn loudly, but do not block - the operator may genuinely want it.
if (multiplier > 100)
    Console.WriteLine($"[WARN] multiplier {multiplier} is unusually high - confirm this is not a typo.");

// Federation / FedFileName / FomModules are filled by the BOUND STACK, not by constants
// here: on 5.2 the connection config owns them and our 5.0.2 list would be additive on top
// of the shipped 17 modules (MIGRATION_DIFF A2/A9). Kept identical to tools/ResetVrf.
var cfg = new StartupConfig
{
    Protocol = VrfProtocol.Hla1516e,
    ApplicationNumber = appNumber,
    SiteId = 1,
    SessionId = 1,
    HostInetAddr = "127.0.0.1",
};
string fedDesc = StackIdentity.Apply(cfg, federation);
// Resolve the connection config, apply it to cfg, and REFUSE to join when it is missing:
// without it VR-Link joins with built-in defaults and the tool reports a successful join and
// then sees nothing (V6, 2026-09-15).
var conn = ConnectionConfig.Resolve(connArg);
conn.ApplyTo(cfg);

Console.WriteLine("=== SetSimRate - set the VR-Forces simulation time multiplier (remote control) ===");
Console.WriteLine($"    {fedDesc}  appNumber={appNumber}  multiplier={multiplier}x");
Console.WriteLine($"    {NativeStackLine()}");
Console.WriteLine($"    {conn.Banner}");
Console.WriteLine($"    {SettleCap.Banner(settleSecs)}");
// A --dry-run JOINS NOTHING, so a missing config is reported there, not refused: a dry run's
// contract is 'arguments validated, no action taken', and turning it into exit 1 would make a
// runner's own dry run fail on a machine where the real run is fine. tools/ResetVrf is the
// deliberate exception - its --dry-run DOES join, so it refuses like a real run.
if (!conn.Ok)
{
    if (!dryRun) { Console.Error.WriteLine(conn.RefusalText); return 1; }
    Console.WriteLine("[DRY-RUN] the connection config above does NOT exist; a real run would " +
                      "REFUSE to join. Nothing was joined either way.");
}
Console.WriteLine($"    started {DateTime.Now:yyyy-MM-dd HH:mm:ss} local / {DateTime.UtcNow:HH:mm:ss} UTC");
Console.WriteLine($"    ACTION: set simulation clock to {multiplier}x real time on ALL backends.");
Console.WriteLine("    This tool creates/deletes/tasks NOTHING. (use a FRESH appNumber each run)\n");

if (dryRun)
{
    Console.WriteLine("[DRY-RUN] arguments validated; the plan above is what a real run would do.");
    Console.WriteLine("[DRY-RUN] NOT joining, NOT sending SetTimeMultiplier. No appNumber was consumed.");
    return 0;
}

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();

    // 1. JOIN the federation.
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.Error.WriteLine("[FAIL] bridge.Start() returned false - NOT joined, multiplier NOT set. " +
                                "Check: the bound stack's RTI on PATH (5.0.2 -> makRti4.6.1, 5.2 -> " +
                                "makRti5.0.1 + RTI_RID_FILE + RTI_ASSISTANT_DISABLE), MAKLMGRD_LICENSE_FILE, " +
                                "FED/FOM, cwd = VRF bin64, fresh appNumber, VR-Forces actually running.");
        return 1;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()} immediately after Start).");

    // 2. SETTLE: tick until a backend is actually discovered. LOAD-BEARING - backends are
    //    NOT known at the instant Start() returns; they are discovered over subsequent
    //    ticks (this is why tools/ResetVrf ticks before it trusts BackendCount /
    //    discovery). Issuing setTimeMultiplier against zero known backends risks a silent
    //    no-op: the tool would report success while the clock never changed.
    Console.WriteLine($"[..] settling - ticking until a backend is discovered (up to {settleSecs} s)...");
    var swSettle = Stopwatch.StartNew();
    int backends = 0;
    while (swSettle.Elapsed < TimeSpan.FromSeconds(settleSecs))
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
                      $"(appNumber={appNumber}, {fedDesc}) at " +
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
