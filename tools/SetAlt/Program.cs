using System.Diagnostics;
using System.Globalization;
using VrfC2Sim;
using VrfC2Sim.Tools;

// tools/SetAlt - set ONE existing object's altitude ABOVE GROUND LEVEL in a live federation,
// and report what it was asked to do so a separate observer can score the result.
//
// WHY THIS EXISTS (2026-09-04): docs/VRF_ALTITUDE_FRAMES.md sec 1 says an entity's altitude
// can be set directly in AGL - vrfRemoteController.h:1372
// `setAltitude(uuid, altitude, bool aboveGroundLevel = false)` - and VrfFacade.cpp:739 already
// passes TRUE, so VrfBridge.SetAltitude(uuid, metres) IS an AGL set. That claim had never been
// EXERCISED end to end: no tool called SetAltitude at all (0 hits across tools/). It was the
// one "ASSUMED, NOT VERIFIED" line in PREREG_CLAMP_DIRECTION_2026-09-04, and the user asked for
// it to be confirmed. An unexercised capability is not a capability.
//
// ADDITIVE: no existing file is touched. Join / act / tick / resign is cloned from
// tools/CreateOne (which cloned tools/ResetVrf). Like CreateOne it has NO default appNumber -
// a missing one is a hard exit 2, because a reused application number is the stale-federate
// trigger (RUNBOOK sec 0).
//
// AGL, NOT MSL: the altitude argument is METRES ABOVE THE TERRAIN at the object's location.
// 0 means "on the surface". This tool cannot send an MSL set - the bridge entry point is
// hard-wired to aboveGroundLevel=TRUE, which is exactly the path under test.
//
// LAUNCH ENV (5.2 stack): PATH prefixed with vrforces5.2d\bin64;vrlink5.10\bin64;
// makRti5.0.1\bin, RTI_RID_FILE = config\rid-501-rtiexec-min.mtl, RTI_ASSISTANT_DISABLE=1,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.2d\bin64.

static int Fail(string msg)
{
    Console.WriteLine("[FAIL] " + msg);
    Console.WriteLine();
    Console.WriteLine("usage:  SetAlt.exe <appNumber> <uuid> <metresAboveGroundLevel> [federation]");
    Console.WriteLine("        appNumber is MANDATORY and must be FRESH (Appendix B ledger; never reuse).");
    Console.WriteLine("        uuid is the VRF UUID string reported by CreateOne / WatchVrf.");
    Console.WriteLine("        The altitude is ABOVE TERRAIN, not MSL. 0 = on the surface.");
    Console.WriteLine();
    Console.WriteLine("example:  SetAlt.exe 3913 VRF_UUID:a2035220-e0f2-034d-a95a-c75ea8d82d31 0");
    return 2;
}

var flags = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (flags.Length > 0) return Fail($"unknown flag '{flags[0]}'.");
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

if (positional.Length < 3) return Fail("need <appNumber> <uuid> <metresAboveGroundLevel>.");
if (!int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int appNumber))
    return Fail($"appNumber '{positional[0]}' is not an integer.");
if (appNumber <= 0) return Fail($"appNumber {appNumber} must be positive.");
string uuid = positional[1];
if (string.IsNullOrWhiteSpace(uuid)) return Fail("uuid is empty.");
if (!double.TryParse(positional[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double aglMetres))
    return Fail($"metresAboveGroundLevel '{positional[2]}' is not a number.");
// A relational test against NaN is false, so range-check explicitly (CreateOne's note).
if (!double.IsFinite(aglMetres)) return Fail($"metresAboveGroundLevel '{positional[2]}' is not finite.");
string federation = positional.Length >= 4 ? positional[3] : null;

var cfg = new StartupConfig
{
    Protocol = VrfProtocol.Hla1516e,
    ApplicationNumber = appNumber,
    SiteId = 1,
    SessionId = 1,
    HostInetAddr = "127.0.0.1",
};
string fedDesc = StackIdentity.Apply(cfg, federation);

Console.WriteLine("=== SetAlt - set ONE object's altitude ABOVE GROUND LEVEL ===");
Console.WriteLine($"    {fedDesc}  appNumber={appNumber}  (use a FRESH appNumber each run)");
Console.WriteLine($"    uuid={uuid}");
Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
    "    requested: {0:F1} m ABOVE TERRAIN (NOT MSL)", aglMetres));
Console.WriteLine();

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();

    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.WriteLine("[FAIL] bridge.Start() returned false. Check: the stack's RTI on PATH, " +
                          "MAKLMGRD_LICENSE_FILE (Machine), FED/FOM, cwd = VRF bin64, fresh appNumber.");
        try { bridge.Stop(); } catch { /* best effort */ }
        return 1;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()}).");

    // A backend must exist to act on. Discovery is not instant after Start(). Without this
    // the set would be a silent no-op reported as success (lessons-false-greens).
    Console.WriteLine("[..] waiting for a backend to be discovered (15 s cap)...");
    var swBe = Stopwatch.StartNew();
    while (bridge.BackendCount() == 0 && swBe.Elapsed < TimeSpan.FromSeconds(15))
    {
        bridge.Tick();
        Thread.Sleep(50);
    }
    if (bridge.BackendCount() == 0)
    {
        Console.WriteLine("[FAIL] no backend discovered after 15 s. Refusing to issue the set - " +
                          "it would be a silent no-op reported as success.");
        bridge.Stop();
        return 1;
    }
    Console.WriteLine($"[OK] backend discovered (BackendCount={bridge.BackendCount()}) after {swBe.Elapsed.TotalSeconds:F1}s.");

    Console.WriteLine("[..] issuing SetAltitude (aboveGroundLevel=TRUE, fixed in VrfFacade.cpp:739)...");
    bridge.SetAltitude(uuid, aglMetres);

    // SetAltitude is FIRE-AND-FORGET: there is no reply message and no callback, so this tool
    // CANNOT confirm the effect itself. Flush, then let an independent observer score it.
    // Saying "[OK] done" here would be exactly the false green this project keeps hitting.
    Console.WriteLine("[..] flushing (ticking ~3 s)...");
    var swFlush = Stopwatch.StartNew();
    while (swFlush.Elapsed < TimeSpan.FromSeconds(3)) { bridge.Tick(); Thread.Sleep(50); }

    Console.WriteLine("[..] bridge.Stop() - resigning from the federation...");
    bridge.Stop();
    Console.WriteLine("[OK] resigned cleanly.");
    Console.WriteLine();
    Console.WriteLine("=== RESULT ===");
    Console.WriteLine($"    uuid      : {uuid}");
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
        "    requested : {0:F1} m ABOVE TERRAIN", aglMetres));
    Console.WriteLine("    SENT, NOT CONFIRMED: this message has no reply. Run WatchVrf and read the");
    Console.WriteLine("    POS altitude for this uuid. On terrain of height H the expected reflected");
    Console.WriteLine("    MSL altitude is H + the requested value.");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
    try { bridge?.Stop(); } catch { /* best effort - never leave a joined federate */ }
    return 1;
}
