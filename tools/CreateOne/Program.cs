using System.Diagnostics;
using System.Globalization;
using VrfC2Sim;

// tools/CreateOne - create ONE entity in a live VR-Forces federation, then report the
// uuid the backend assigns it.
//
// WHY THIS EXISTS (2026-07-18): the WatchVrf movement oracle was verified to DISCOVER
// objects, but every POS line observed carried NaN latitude/altitude - because the only
// objects in a freshly loaded TropicTortoise are non-entity CONTROL objects (terrain
// page-in area, global terrain damage) which have no meaningful position. Whether
// WatchVrf reports REAL coordinates for a REAL entity was therefore unverified, and the
// entire Phase 1 baseline rests on that. This tool creates exactly one entity so the
// question can be answered in a minute instead of discovered mid-session.
// See docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md.
//
// ADDITIVE: no existing file is touched. Cloned from tools/ResetVrf (join / act / tick /
// resign). Like SetSimRate it has NO default appNumber - a missing one is a hard exit 2,
// because a reused application number is the stale-federate trigger (RUNBOOK sec 0).
//
// LAUNCH ENV (identical to ResetVrf - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.0.2\bin64.
//   $env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
//   $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
//   Push-Location C:\MAK\vrforces5.0.2\bin64
//   & <repo>\tools\CreateOne\bin\Release\net10.0\win-x64\CreateOne.exe <appNo> [lat] [lon] [alt] [name]
//   Pop-Location
//
// Defaults are a single M1A2 Abrams (DIS 1.1.225.1.1.3.0 - the real installed template
// used by PHASE1_SESSION_SCRIPT.md) at a COA-STP1 AO coordinate taken from the shipped
// order data. Altitude defaults to 10000 m MSL: the buried-birth altitude bug froze
// entities created at low MSL, and a safe-high MSL create is the shipped fix in both
// codebases (ground clamp brings it down). Do NOT lower this default casually.

static int Fail(string msg)
{
    Console.WriteLine("[FAIL] " + msg);
    Console.WriteLine();
    Console.WriteLine("usage:  CreateOne.exe <appNumber> [latDeg] [lonDeg] [altMeters] [name] [federation]");
    Console.WriteLine("        appNumber is MANDATORY and must be FRESH (Appendix B ledger; never reuse).");
    Console.WriteLine();
    Console.WriteLine("example:  CreateOne.exe 3480");
    Console.WriteLine("          CreateOne.exe 3480 34.5172 -116.9735 10000 ORACLETEST");
    return 2;
}

// Reject unknown flags rather than silently dropping them. tools/ResetVrf accepts
// --dry-run, so an operator carrying that habit here would otherwise perform a REAL
// create believing it was a no-op. CreateOne has no dry-run mode.
var flags = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (flags.Length > 0)
    return Fail($"unknown option(s): {string.Join(" ", flags)}. CreateOne takes positional "
              + "arguments only and has NO --dry-run mode - it always performs a real create.");

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (positional.Length < 1) return Fail("missing appNumber.");
if (!int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var appNumber)
    || appNumber <= 0)
    return Fail($"appNumber '{positional[0]}' is not a positive integer.");

double lat = 34.517156470326704;    // COA-STP1 AO (Mojave / TropicTortoise), from data/ order XML
double lon = -116.97352492302609;
double alt = 10000.0;               // safe-high MSL create; ground clamp brings it down
string name = "ORACLETEST";
string federation = "CWIX-2024";

if (positional.Length >= 2 && !double.TryParse(positional[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
    return Fail($"latDeg '{positional[1]}' is not a number.");
if (positional.Length >= 3 && !double.TryParse(positional[2], NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
    return Fail($"lonDeg '{positional[2]}' is not a number.");
if (positional.Length >= 4 && !double.TryParse(positional[3], NumberStyles.Float, CultureInfo.InvariantCulture, out alt))
    return Fail($"altMeters '{positional[3]}' is not a number.");
if (positional.Length >= 5 && !string.IsNullOrWhiteSpace(positional[4])) name = positional[4];
if (positional.Length >= 6 && !string.IsNullOrWhiteSpace(positional[5])) federation = positional[5];

// NaN / Infinity note: NumberStyles.Float ACCEPTS "NaN" and "Infinity", and every
// relational test against NaN is false - so a bare range check lets NaN through.
// That would be especially perverse here: this tool exists to decide whether NaN in
// WatchVrf output is real or an artifact. Reject non-finite values explicitly.
// Report the RAW ARGUMENT, not the parsed double: .NET renders infinity as the
// non-ASCII "infinity" glyph, which violates this project's ASCII-only rule and
// mangles on a Windows console.
if (!double.IsFinite(lat))
    return Fail($"latDeg '{(positional.Length >= 2 ? positional[1] : "?")}' is not a finite number.");
if (!double.IsFinite(lon))
    return Fail($"lonDeg '{(positional.Length >= 3 ? positional[2] : "?")}' is not a finite number.");
if (!double.IsFinite(alt))
    return Fail($"altMeters '{(positional.Length >= 4 ? positional[3] : "?")}' is not a finite number.");
if (lat < -90 || lat > 90)   return Fail($"latDeg {lat} out of range (-90..90).");
if (lon < -180 || lon > 180) return Fail($"lonDeg {lon} out of range (-180..180).");

// FED / FOM must match VR-Forces' running federation (RUNBOOK sec 7) - same constants
// ResetVrf and WatchVrf use.
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

// M1A2_Abrams_MBT - DIS 1.1.225.1.1.3.0 (0.1 content catalog; PHASE1_SESSION_SCRIPT.md).
var type = new EntityTypeSpec
{
    Kind = 1, Domain = 1, Country = 225, Category = 1,
    Subcategory = 1, Specific = 3, Extra = 0,
};
var pos = new Geodetic { LatDeg = lat, LonDeg = lon, AltMeters = alt };

Console.WriteLine("=== CreateOne - create ONE entity in a live VR-Forces federation ===");
Console.WriteLine($"    federation={federation}  appNumber={appNumber}  (use a FRESH appNumber each run)");
Console.WriteLine($"    type=M1A2_Abrams_MBT (DIS 1.1.225.1.1.3.0)  name='{name}'");
Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
    "    pos=({0:F6}, {1:F6}) alt={2:F1} m MSL", lat, lon, alt));
Console.WriteLine();

VrfBridge bridge = null;
string createdUuid = null, createdEntityId = null;
try
{
    bridge = new VrfBridge();

    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        // Start() can fail AFTER the exercise connection was constructed (i.e. after
        // joining), so resign explicitly. ResetVrf gets this via a finally/Dispose;
        // CreateOne must not leave a joined federate behind (RUNBOOK sec 0).
        Console.WriteLine("[FAIL] bridge.Start() returned false. Check: RTI 4.6.1 on PATH, " +
                          "MAKLMGRD_LICENSE_FILE (Machine), FED/FOM, cwd = VRF bin64, fresh appNumber.");
        try { bridge.Stop(); } catch { /* best effort */ }
        return 1;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()}).");

    // Creation is ASYNC - the backend answers on the ObjectCreated event. Subscribe
    // BEFORE issuing the create so a fast reply cannot be missed.
    bridge.ObjectCreated += (s, e) =>
    {
        if (createdUuid != null) return;   // first one wins; we only create one
        createdUuid = e.Uuid;
        createdEntityId = e.EntityId;
        Console.WriteLine($"[OK] ObjectCreated: name='{e.Name}' uuid={e.Uuid} entityId={e.EntityId}");
    };

    // A backend must exist to create on. Discovery is not instant after Start().
    Console.WriteLine("[..] waiting for a backend to be discovered (15 s cap)...");
    var swBe = Stopwatch.StartNew();
    while (bridge.BackendCount() == 0 && swBe.Elapsed < TimeSpan.FromSeconds(15))
    {
        bridge.Tick();
        Thread.Sleep(50);
    }
    if (bridge.BackendCount() == 0)
    {
        Console.WriteLine("[FAIL] no backend discovered after 15 s. Refusing to issue the create - " +
                          "it would be a silent no-op reported as success. Confirm VR-Forces is up " +
                          "with a scenario loaded, and that the RTI connection is the one the backend uses.");
        bridge.Stop();
        return 1;
    }
    Console.WriteLine($"[OK] backend discovered (BackendCount={bridge.BackendCount()}) after {swBe.Elapsed.TotalSeconds:F1}s.");

    Console.WriteLine("[..] issuing CreateEntity...");
    bridge.CreateEntity(type, pos, Force.Friendly, 0.0, name);

    // Tick until the ObjectCreated callback lands, or give up.
    Console.WriteLine("[..] ticking for the ObjectCreated reply (20 s cap)...");
    var sw = Stopwatch.StartNew();
    while (createdUuid == null && sw.Elapsed < TimeSpan.FromSeconds(20))
    {
        bridge.Tick();
        Thread.Sleep(50);
    }

    if (createdUuid == null)
    {
        Console.WriteLine("[FAIL] no ObjectCreated reply within 20 s. The entity may or may not exist - " +
                          "check the VR-Forces GUI and WatchVrf before creating another.");
        bridge.Stop();
        return 1;
    }

    // Flush the create to the backend before resigning (same 3 s posture as ResetVrf's
    // delete flush - calibrated by analogy, NOT measured for this message).
    Console.WriteLine("[..] flushing (ticking ~3 s)...");
    var swFlush = Stopwatch.StartNew();
    while (swFlush.Elapsed < TimeSpan.FromSeconds(3)) { bridge.Tick(); Thread.Sleep(50); }

    Console.WriteLine("[..] bridge.Stop() - resigning from the federation...");
    bridge.Stop();
    Console.WriteLine("[OK] resigned cleanly.");
    Console.WriteLine();
    Console.WriteLine("=== RESULT ===");
    Console.WriteLine($"    uuid     : {createdUuid}");
    Console.WriteLine($"    entityId : {createdEntityId}");
    Console.WriteLine($"    name     : {name}");
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
        "    requested: lat={0:F6} lon={1:F6} alt={2:F1}", lat, lon, alt));
    Console.WriteLine();
    Console.WriteLine("    NEXT: run WatchVrf and confirm a POS line for this uuid carries REAL");
    Console.WriteLine("    coordinates (not NaN). That is the movement-oracle check this tool exists for.");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
    try { bridge?.Stop(); } catch { /* best effort - never leave a joined federate */ }
    return 1;
}
