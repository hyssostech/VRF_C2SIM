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
// CONNECTION CONFIG (V6 harvest 2026-09-15). On 5.2 the federation identity lives in
// MAK-ONE-2025-Config.xml and the VENDOR DEFAULT PATH IS CWD-RELATIVE
// ("..\appData\settings\connections"), so a tool launched from anywhere but the 5.2d bin64
// silently joined with BUILT-IN defaults - a different execName, no FOM modules - and then saw
// nothing. This tool now resolves the file EXPLICITLY (--config > env Vrf__ConnectionConfigFile
// > the loaded vrfcontrol.dll's own tree), PRINTS it with an exists= flag before Start(), and
// REFUSES to join when it is not there. See tools/Shared/ConnectionConfig.cs.
//
// EXIT CODES (the --dry-run pair criterion has its own code, so a runner never has to grep):
//   0  the action completed: a real reset was issued, OR a --dry-run found NOTHING deletable
//      (the federation is already clean - what the AFTER half of a reset pair expects)
//   1  operational failure: connection config missing, not joined, NO BACK END, or an exception
//   2  usage / argument error - no action taken
//   3  --dry-run found deletable objects, i.e. the federation is NOT clean (what the BEFORE
//      half of a reset pair expects). Never returned by a real reset.
//
// Args: <applicationNumber> [federation] [--config <path>] [--dry-run|--list] [--help]
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
    w.WriteLine("usage: ResetVrf.exe <applicationNumber> [federation] [--config <path>] [--dry-run|--list]");
    w.WriteLine("       ResetVrf.exe --help | --config-selftest");
    w.WriteLine();
    w.WriteLine("  applicationNumber  REQUIRED. NO DEFAULT - use a FRESH, ledgered appNo every");
    w.WriteLine("                     run (RUNBOOK sec 7). Reusing one steals a federate slot.");
    w.WriteLine("  federation         Optional. Default is stack-aware (5.0.2 -> CWIX-2024;");
    w.WriteLine("                     5.2 -> connection-config identity; tools/Shared/StackIdentity.cs).");
    w.WriteLine("  --config <path>    The VR-Link connection config XML (MAK-ONE-2025-Config.xml).");
    w.WriteLine("                     Default: env " + ConnectionConfig.EnvVar + ", else the loaded");
    w.WriteLine("                     stack's <VrfRoot>\\" + ConnectionConfig.RelativeDir + "\\" + ConnectionConfig.FileName + ".");
    w.WriteLine("                     The tool REFUSES to join when that file is not there.");
    w.WriteLine("  --dry-run/--list   JOIN and discover, but issue NO deletes. Exit 3 when anything");
    w.WriteLine("                     deletable was found, 0 when the federation is clean.");
    w.WriteLine("  --config-selftest  Run the OFFLINE connection-config resolution suite and exit.");
    w.WriteLine("                     Joins nothing, reads no file, needs no VR-Forces.");
}

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    PrintUsage(Console.Error);
    return 2;
}

// --config <path> is taken out of args FIRST so the positional / unknown-flag parsing below
// never sees either token. ConnectionConfig owns the option name and the three-rule order.
if (!ConnectionConfig.TryTakeFlag(args, out args, out string connArg, out string connProblem))
    return Usage(connProblem);

// The offline resolution suite. NO join, NO file system, NO VR-Forces - it is the regression
// test for tools/Shared/ConnectionConfig.cs and exits with the number of FAILED checks.
if (args.Any(a => string.Equals(a, "--config-selftest", StringComparison.OrdinalIgnoreCase)))
    return ConnectionConfig.SelfTest(Console.Out);

bool dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(a, "--list", StringComparison.OrdinalIgnoreCase));
bool help = args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase));
var unknown = args.Where(a => a.StartsWith("--", StringComparison.Ordinal))
                  .Where(a => !string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(a, "--list", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(a, "--config-selftest", StringComparison.OrdinalIgnoreCase))
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
    Console.WriteLine("    " + ConnectionConfig.Resolve(connArg).Banner);
    Console.WriteLine();
    Console.WriteLine("    PLAN: join -> tick until a BACK END is discovered (cap 15 s; REFUSE at 0, in");
    Console.WriteLine("          both modes) -> BeginTrackingReflectedObjects -> tick until the discovered");
    Console.WriteLine("          count settles (cap 20 s) -> DeleteObject each uuid that is NOT a non-VRF");
    Console.WriteLine("          entity-identifier id -> flush ~3 s -> resign cleanly. --dry-run stops");
    Console.WriteLine("          before the deletes (it still JOINS) and exits 3 if anything was found.");
    Console.WriteLine("    THIS INVOCATION JOINED NOTHING and deleted nothing.");
    Console.WriteLine();
    PrintUsage(Console.Out);
    return 0;
}

if (unknown.Length > 0)
    return Usage($"unknown option(s): {string.Join(" ", unknown)}. ResetVrf accepts --config <path>, --dry-run, --list, --config-selftest and --help only.");

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

// The connection config is resolved and PRINTED before anything joins, and a missing file is a
// REFUSAL, not a fallback: without it VR-Link joins with built-in defaults and the tool would
// report "[OK] joined" and then discover nothing (V6, 2026-09-15).
var conn = ConnectionConfig.Resolve(connArg);
conn.ApplyTo(cfg);

Console.WriteLine("=== ResetVrf - hard reset of a live VR-Forces federation (RUNBOOK sec 8) ===");
Console.WriteLine($"    {fedDesc}  appNumber={appNumber}  dryRun={dryRun}  (use a FRESH appNumber each run)");
Console.WriteLine($"    {NativeStackLine()}");
Console.WriteLine($"    {conn.Banner}\n");

if (!conn.Ok)
{
    Console.Error.WriteLine(conn.RefusalText);
    return 1;
}

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
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()} immediately after Start).");

    // 1b. WAIT FOR A BACK END, exactly as tools/SetSimRate does. LOAD-BEARING: back ends are
    //     not known at the instant Start() returns, and a federate that never discovers one is
    //     BLIND - in V6 (2026-09-15) this tool reported "3 deletable" and "deletes flushed" at
    //     BackendCount=0 while all 48 real objects stayed in the scenario. A blind read is not
    //     evidence, so this refuses in BOTH modes: --dry-run included, because the dry run is
    //     the BEFORE half of a verification pair and a blind count would poison the pair.
    Console.WriteLine("[..] waiting for a back end to be discovered (15 s cap)...");
    var swBackend = Stopwatch.StartNew();
    int backends = 0;
    while (swBackend.Elapsed < TimeSpan.FromSeconds(15))
    {
        bridge.Tick();
        Thread.Sleep(50);
        backends = bridge.BackendCount();
        if (backends > 0) break;
    }
    if (backends == 0)
    {
        Console.Error.WriteLine($"[FAIL] no back end discovered after {swBackend.Elapsed.TotalSeconds:F0} s " +
                                "(BackendCount=0). NOTHING was deleted, and the discovery below would be " +
                                "meaningless: a federate that sees no back end also sees no VR-Forces object " +
                                "data, so the only uuids it can report are non-VRF placeholders. Confirm " +
                                "VR-Forces is up with a scenario loaded, that this federate uses the SAME " +
                                "connection config and rid as the simulator, then retry with a FRESH appNumber.");
        Console.Error.WriteLine("[..] bridge.Stop() - resigning cleanly...");
        bridge.Stop();
        Console.Error.WriteLine($"[OK] resigned. Mark appNumber {appNumber} as USED.");
        return 1;
    }
    Console.WriteLine($"[OK] {backends} back end(s) discovered after {swBackend.Elapsed.TotalSeconds:F1} s.");

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
    var uuids = all.Where(u => !IsNotDeletable(u)).ToList();
    int skipped = all.Count - uuids.Count;
    Console.WriteLine($"[OK] discovery complete: {all.Count} reflected object(s) " +
                      $"({uuids.Count} deletable, {skipped} nil/backend skipped).");

    // Show a sample of what was found (uuids are opaque, but the count + a few is useful).
    foreach (var u in all.Take(12))
        Console.WriteLine($"       {u}{(IsNotDeletable(u) ? "   [skip: " + SkipReason(u) + "]" : "")}");
    if (all.Count > 12) Console.WriteLine($"       ... and {all.Count - 12} more.");

    if (uuids.Count == 0)
    {
        Console.WriteLine("     Nothing deletable - the federation is already clean, or nothing was " +
                          "discovered (confirm VR-Forces is up and a scenario is loaded).");
    }
    else if (dryRun)
    {
        Console.WriteLine($"[DRY-RUN] would delete {uuids.Count} object(s); NO deletes issued.");
        Console.WriteLine("[..] bridge.Stop() - resigning from the federation...");
        bridge.Stop();
        Console.WriteLine("[OK] resigned cleanly.");
        Console.WriteLine($"     Mark appNumber {appNumber} as USED in the ledger.");
        // EXIT 3 = NOT CLEAN. Its own code, so the before/after halves of a reset are a pair of
        // EXIT CODES (3 then 0) instead of a grep over two logs.
        return 3;
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

// WHICH UUIDS ARE NEVER A DELETE TARGET.
//
// VR-Forces publishes its own objects with a real UUID attribute. For an object that does NOT
// carry one, makVrf::DtNonVrfUUIDResolver SYNTHESISES an id under the scheme the controller was
// initialised with - ours is "entity-identifier" (VrfFacade.cpp passes it to
// DtVrlinkVrfRemoteController::init; the three schemes are listed in
// vrlinkNetworkInterface/nonVrfUUIDResolver.h and UUIDNetworkManager.h). That scheme spells the
// object's DIS-style identifier and appends the list it came from: the literals "-entity",
// "-unit" and "-control-object" sit side by side in the string pool of
// vrlinkNetworkInterfaceHLA1516e.dll, next to "entity-identifier" / "global-identifier" /
// "marking-text".
//
// So "VRF_UUID:0:0:0-entity" / "-unit" / "-control-object" are NOT three objects and NOT
// VR-Forces objects: they are ONE PLACEHOLDER PER REFLECTED LIST, built from the NULL identifier
// 0:0:0 because no attribute data had arrived, then de-duplicated by the set behind
// GetAllReflectedUuids(). The V6 run (2026-09-15) issued three deleteObject calls against
// exactly those and reported "deletes flushed" while every real object stayed in the scenario
// (the oracle trace held reflected=48 right across the window). The old test - EndsWith
// ":0:0:0" - could not see them because the list suffix comes AFTER the zeros.
//
// A non-VRF id with a REAL identifier (e.g. "VRF_UUID:1:3001:25-entity") is a foreign federate's
// object: VR-Forces did not create it and deleteObject on it is a no-op at best, so the whole
// scheme is excluded, not just the null case.
static bool IsNotDeletable(string u) => SkipReason(u) != null;

// null = deletable. Otherwise the reason, printed beside the uuid.
static string SkipReason(string u)
{
    if (string.IsNullOrWhiteSpace(u)) return "empty";
    if (u.Contains("00000000-0000-0000-0000-000000000000", StringComparison.Ordinal)) return "nil guid";
    if (u.EndsWith(":0:0:0", StringComparison.Ordinal)) return "nil entity-identifier";
    foreach (string suffix in new[] { "-entity", "-unit", "-control-object" })
        if (u.EndsWith(suffix, StringComparison.Ordinal))
            return u.Contains(":0:0:0" + suffix, StringComparison.Ordinal)
                 ? "non-VRF placeholder (null id, one per reflected list)"
                 : "non-VRF object (entity-identifier scheme; VR-Forces did not create it)";
    return null;
}
