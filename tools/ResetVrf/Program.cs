using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using VrfC2Sim;
using VrfC2Sim.Tools;

// tools/ResetVrf - HARD reset of a live VR-Forces federation (docs/RUNBOOK.md sec 8).
//
// Joins the federation, discovers EVERY reflected object (entities, aggregates, control
// objects) - including ORPHANS left by a crashed or force-killed run that Solution A
// (the app's delete-on-stop) cannot reach - and DeleteObject()s each one for a full clean
// slate, then resigns CLEANLY (no stale federate). Pure VR-Forces: no C2SIM / STOMP.
//
// STACK-AWARE (2026-09-15): the federation identity is NOT hard-coded any more. It comes
// from tools/Shared/StackIdentity.cs, which reads the bound stack from the loaded native
// DLLs (VrfBridge.NativeStackInfo()) - never from a build flag. On 5.2 the join is the
// CONFIG-FILE join (execName MAK-ONE-2025 from appData\settings\connections\
// MAK-ONE-2025-Config.xml; empty Federation/FedFileName/FomModules, because config FOM
// modules are ADDITIVE - MIGRATION_DIFF A2/A9); on 5.0.2 it is the old CWIX-2024 +
// RPR_FOM_v2.0_1516-2010.xml + 3 MAK modules. An explicit federation argument overrides
// execName on either stack.
//
// LAUNCH ENV, 5.2 (PREREG_52_LAUNCH_2026-09-03; the same env tools/SetAlt documents): PATH
// prefixed with C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin,
// RTI_RID_FILE = config\rid-501-rtiexec-min.mtl, RTI_ASSISTANT_DISABLE=1,
// MAKLMGRD_LICENSE_FILE (User scope first, then Machine), cwd = C:\MAK\vrforces5.2d\bin64
// so ..\appData resolves the connection config, and a FRESH ApplicationNumber each run.
//   & <repo>\tools\ResetVrf\bin\Release-5.2\net10.0\win-x64\ResetVrf.exe <freshAppNo> --dry-run
// LAUNCH ENV, 5.0.2 (unchanged - RUNBOOK sec 7/8): RTI 4.6.1 on PATH, cwd =
// C:\MAK\vrforces5.0.2\bin64, and bin\Release\ instead of bin\Release-5.2\.
//
// Args: <applicationNumber> [federation] [--dry-run|--list] [--help]
//   applicationNumber  REQUIRED. NO DEFAULT - the old baked-in 3299 was removed 2026-09-15:
//                      a default invites silent reuse of a burned appNo, which steals a
//                      federate slot (RUNBOOK sec 7, "never allocate one").
//   federation         Optional. Default is stack-aware (see above).
//   --dry-run (alias --list): DISCOVER + report only; issue NO deletes. It STILL JOINS -
//                      that is the point: it is the read-only half of a before/after
//                      verification (discover N -> real reset -> re-discover 0). Use
//                      --help for the no-join plan.
//   --help             Print the plan and the bound native stack; join NOTHING; exit 0.

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
    w.WriteLine("usage: ResetVrf.exe <applicationNumber> [federation] [--dry-run|--list]");
    w.WriteLine("       ResetVrf.exe --help");
    w.WriteLine();
    w.WriteLine("  applicationNumber  REQUIRED. NO DEFAULT - use a FRESH, ledgered appNo every");
    w.WriteLine("                     run (RUNBOOK sec 7). Reusing one steals a federate slot.");
    w.WriteLine("  federation         Optional. Default is stack-aware (5.0.2 -> CWIX-2024;");
    w.WriteLine("                     5.2 -> connection-config identity; tools/Shared/StackIdentity.cs).");
    w.WriteLine("  --dry-run/--list   JOIN and discover, but issue NO deletes.");
}

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    PrintUsage(Console.Error);
    return 2;
}

bool dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(a, "--list", StringComparison.OrdinalIgnoreCase));
bool help = args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase));
var unknown = args.Where(a => a.StartsWith("--", StringComparison.Ordinal))
                  .Where(a => !string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(a, "--list", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase))
                  .ToArray();
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

// --help is the NO-JOIN plan printer (the runner's self-test convention). It reports the
// bound stack and the identity this tool WOULD join with, and does nothing else.
if (help)
{
    Console.WriteLine("=== ResetVrf - hard reset of a live VR-Forces federation (RUNBOOK sec 8) ===");
    Console.WriteLine("    " + NativeStackLine());
    var planCfg = new StartupConfig { Protocol = VrfProtocol.Hla1516e, SiteId = 1, SessionId = 1 };
    Console.WriteLine("    " + StackIdentity.Apply(planCfg, positional.Length >= 2 ? positional[1] : null));
    Console.WriteLine();
    Console.WriteLine("    PLAN: join -> BeginTrackingReflectedObjects -> tick until the discovered count");
    Console.WriteLine("          settles (cap 20 s) -> DeleteObject each non-nil uuid -> flush ~3 s ->");
    Console.WriteLine("          resign cleanly. --dry-run stops before the deletes (it still JOINS).");
    Console.WriteLine("    THIS INVOCATION JOINED NOTHING and deleted nothing.");
    Console.WriteLine();
    PrintUsage(Console.Out);
    return 0;
}

if (unknown.Length > 0)
    return Usage($"unknown option(s): {string.Join(" ", unknown)}. ResetVrf accepts --dry-run, --list and --help only.");

if (positional.Length == 0)
    return Usage("Missing <applicationNumber>. It is REQUIRED and has NO default - supply a " +
                 "FRESH appNo that is not in use by VR-Forces, WatchVrf, or any prior run.");

if (!int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int appNumber))
    return Usage($"ApplicationNumber '{positional[0]}' is not an integer.");

if (appNumber <= 0 || appNumber > 65535)
    return Usage($"ApplicationNumber {appNumber} is out of range (expected 1..65535).");

string federation = positional.Length >= 2 && !string.IsNullOrWhiteSpace(positional[1])
    ? positional[1]
    : null;   // null = stack default (5.0.2 CWIX-2024; 5.2 config-file identity)

var cfg = new StartupConfig
{
    Protocol = VrfProtocol.Hla1516e,
    ApplicationNumber = appNumber,
    SiteId = 1,
    SessionId = 1,
    HostInetAddr = "127.0.0.1",
};
// Federation / FedFileName / FomModules are filled by the bound stack, not by constants
// here: on 5.2 the connection config owns them (MIGRATION_DIFF A2/A9).
string fedDesc = StackIdentity.Apply(cfg, federation);

Console.WriteLine("=== ResetVrf - hard reset of a live VR-Forces federation (RUNBOOK sec 8) ===");
Console.WriteLine($"    {fedDesc}  appNumber={appNumber}  dryRun={dryRun}  (use a FRESH appNumber each run)");
Console.WriteLine($"    {NativeStackLine()}\n");

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();

    // 1. JOIN the federation.
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.WriteLine("[FAIL] bridge.Start() returned false. Check: the bound stack's RTI on PATH " +
                          "(5.0.2 -> makRti4.6.1, 5.2 -> makRti5.0.1 + RTI_RID_FILE + RTI_ASSISTANT_DISABLE), " +
                          "MAKLMGRD_LICENSE_FILE, FED/FOM, cwd = VRF bin64, fresh appNumber.");
        return 1;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()}).");

    // 2. Collect reflected UUIDs. Register BEFORE the first Tick() so no discovery is missed,
    //    then tick until the discovered count stops growing (or a cap).
    bridge.BeginTrackingReflectedObjects();
    Console.WriteLine("[..] discovering reflected objects (ticking)...");
    var sw = Stopwatch.StartNew();
    int lastCount = -1, stableChecks = 0;
    while (true)
    {
        for (int i = 0; i < 10; i++) { bridge.Tick(); Thread.Sleep(50); } // ~0.5 s of ticks
        int n = bridge.GetAllReflectedUuids().Count();
        if (n != lastCount)
        {
            Console.WriteLine($"    discovered {n} object(s) so far ({sw.Elapsed.TotalSeconds:F0}s).");
            lastCount = n;
            stableChecks = 0;
        }
        else stableChecks++;

        // Settle: found something and the count held steady for ~2 s (min 4 s window).
        if (n > 0 && stableChecks >= 4 && sw.Elapsed > TimeSpan.FromSeconds(4)) break;
        // Empty federation: nothing discovered after 8 s.
        if (n == 0 && sw.Elapsed > TimeSpan.FromSeconds(8)) break;
        // Hard cap.
        if (sw.Elapsed > TimeSpan.FromSeconds(20)) break;
    }

    var all = bridge.GetAllReflectedUuids().ToList();
    // Skip nil / zero uuids (e.g. "VRF_UUID:0:0:0" - the entity-identifier nil, or an
    // all-zero GUID). These are backend/control artifacts, not created objects; deleting one
    // is at best a no-op and could poke a backend object, so leave them alone.
    var uuids = all.Where(u => !IsNilUuid(u)).ToList();
    int skipped = all.Count - uuids.Count;
    Console.WriteLine($"[OK] discovery complete: {all.Count} reflected object(s) " +
                      $"({uuids.Count} deletable, {skipped} nil/backend skipped).");

    // Show a sample of what was found (uuids are opaque, but the count + a few is useful).
    foreach (var u in all.Take(12)) Console.WriteLine($"       {u}{(IsNilUuid(u) ? "   [skip: nil]" : "")}");
    if (all.Count > 12) Console.WriteLine($"       ... and {all.Count - 12} more.");

    if (uuids.Count == 0)
    {
        Console.WriteLine("     Nothing deletable - the federation is already clean, or nothing was " +
                          "discovered (confirm VR-Forces is up and a scenario is loaded).");
    }
    else if (dryRun)
    {
        Console.WriteLine($"[DRY-RUN] would delete {uuids.Count} object(s); NO deletes issued.");
    }
    else
    {
        // 3. Delete each discovered (non-nil) object.
        Console.WriteLine($"[..] deleting {uuids.Count} object(s)...");
        foreach (var u in uuids) bridge.DeleteObject(u);
        Console.WriteLine($"[OK] {uuids.Count} deleteObject command(s) issued.");

        // 4. Tick to flush the delete messages to the backend.
        Console.WriteLine("[..] flushing deletes (ticking ~3 s)...");
        var swFlush = Stopwatch.StartNew();
        while (swFlush.Elapsed < TimeSpan.FromSeconds(3)) { bridge.Tick(); Thread.Sleep(50); }
        Console.WriteLine("[OK] deletes flushed.");
    }

    // 5. Clean stop -> resign. NEVER force-kill a joined federate (RUNBOOK sec 0).
    Console.WriteLine("[..] bridge.Stop() - resigning from the federation...");
    bridge.Stop();
    Console.WriteLine("[OK] resigned cleanly. Verify the VR-Forces GUI now shows an empty scenario.");
    Console.WriteLine($"     Mark appNumber {appNumber} as USED in the ledger.");
    return 0;
}
catch (Exception e)
{
    Console.WriteLine($"[FAIL] {e.GetType().Name}: {e.Message}");
    Console.WriteLine(e.StackTrace);
    return 1;
}
finally
{
    bridge?.Dispose();
}

// A nil / zero uuid ("VRF_UUID:0:0:0" entity-identifier nil, or an all-zero GUID) is a
// backend/control artifact, not a created object - never a delete target.
static bool IsNilUuid(string u)
{
    if (string.IsNullOrWhiteSpace(u)) return true;
    return u.EndsWith(":0:0:0", StringComparison.Ordinal)
        || u.Contains("00000000-0000-0000-0000-000000000000", StringComparison.Ordinal);
}
