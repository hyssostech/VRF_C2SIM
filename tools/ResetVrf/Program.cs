using System.Diagnostics;
using System.Globalization;
using System.Linq;
using VrfC2Sim;

// tools/ResetVrf - HARD reset of a live VR-Forces federation (docs/RUNBOOK.md sec 8).
//
// Joins the federation, discovers EVERY reflected object (entities, aggregates, control
// objects) - including ORPHANS left by a crashed or force-killed run that Solution A
// (the app's delete-on-stop) cannot reach - and DeleteObject()s each one for a full clean
// slate, then resigns CLEANLY (no stale federate). Pure VR-Forces: no C2SIM / STOMP.
//
// LAUNCH ENV (identical to the app - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.0.2\bin64, and a FRESH
// ApplicationNumber each run (a lingering federate steals the slot). Example (PowerShell):
//   $env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
//   $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
//   Push-Location C:\MAK\vrforces5.0.2\bin64
//   & <repo>\tools\ResetVrf\bin\Release\net10.0\win-x64\ResetVrf.exe 3299
//   Pop-Location
//
// Args: [applicationNumber] [federation] [--dry-run]. Defaults: 3299, CWIX-2024.
//   --dry-run (alias --list): DISCOVER + report only; issue NO deletes. Read-only - safe
//   to see what is present, and the basis for a before/after verification (discover N ->
//   real reset -> re-discover 0).

bool dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(a, "--list", StringComparison.OrdinalIgnoreCase));
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

int appNumber = 3299;
string federation = "CWIX-2024";
if (positional.Length >= 1 &&
    int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedApp))
    appNumber = parsedApp;
if (positional.Length >= 2 && !string.IsNullOrWhiteSpace(positional[1]))
    federation = positional[1];

// FED / FOM must match VR-Forces' running federation (appsettings.json Vrf, RUNBOOK sec 7).
// These are environment constants, not app logic; if VR-Forces' command line differs, read
// its --fedFileName / --fomModules and edit here.
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

Console.WriteLine("=== ResetVrf - hard reset of a live VR-Forces federation (RUNBOOK sec 8) ===");
Console.WriteLine($"    federation={federation}  appNumber={appNumber}  dryRun={dryRun}  (use a FRESH appNumber each run)\n");

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();

    // 1. JOIN the federation.
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.WriteLine("[FAIL] bridge.Start() returned false. Check: RTI 4.6.1 on PATH, " +
                          "MAKLMGRD_LICENSE_FILE (Machine), FED/FOM, cwd = VRF bin64, fresh appNumber.");
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
