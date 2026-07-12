using System.Globalization;
using System.Linq;
using VrfC2Sim;

// tools/WatchVrf - MEMBER-LEVEL position telemetry for a live VR-Forces federation
// (docs/UNIT_MOVEMENT_RESEARCH.md plan R3). GUI-independent observation channel.
//
// Joins the federation as a read-only observer, discovers EVERY reflected object via the
// ResetVrf reflection machinery (BeginTrackingReflectedObjects), then samples each
// object's geodetic position on an interval and prints CSV lines:
//     POS,<elapsed-seconds>,<uuid>,<latDeg>,<lonDeg>,<altM>
// Objects with no readable location (routes, areas, not-yet-resolved) are skipped.
// Subordinate entities of disaggregated units ARE reflected objects, so this captures
// the member-level picture the hung GUI cannot show (runaway vs scatter vs march).
// Resigns CLEANLY at the end (no stale federate). Pure VR-Forces: no C2SIM / STOMP.
//
// LAUNCH ENV (identical to the app - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.0.2\bin64, and a
// FRESH ApplicationNumber each run.
//
// Args: [applicationNumber] [durationSecs] [sampleSecs] [federation]
// Defaults: 3399, 120, 15, CWIX-2024.

var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
int appNumber = 3399, durationSecs = 120, sampleSecs = 15;
string federation = "CWIX-2024";
if (positional.Length >= 1) int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out appNumber);
if (positional.Length >= 2) int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out durationSecs);
if (positional.Length >= 3) int.TryParse(positional[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out sampleSecs);
if (positional.Length >= 4 && !string.IsNullOrWhiteSpace(positional[3])) federation = positional[3];

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

Console.WriteLine("=== WatchVrf - member-level position telemetry (UNIT_MOVEMENT_RESEARCH plan R3) ===");
Console.WriteLine($"    federation={federation} appNumber={appNumber} duration={durationSecs}s sample={sampleSecs}s\n");

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.WriteLine("[FAIL] bridge.Start() returned false.");
        return 1;
    }
    bridge.BeginTrackingReflectedObjects();
    Console.WriteLine("[OK] joined; discovering + sampling (CSV lines: POS,t,uuid,lat,lon,alt)...");

    var start = DateTime.UtcNow;
    var nextSample = start.AddSeconds(3); // small settle so discovery gets going
    while ((DateTime.UtcNow - start).TotalSeconds < durationSecs)
    {
        bridge.Tick();
        Thread.Sleep(50);
        if (DateTime.UtcNow < nextSample) continue;
        nextSample = DateTime.UtcNow.AddSeconds(sampleSecs);

        double t = Math.Round((DateTime.UtcNow - start).TotalSeconds, 1);
        var uuids = bridge.GetAllReflectedUuids();
        int readable = 0;
        foreach (string u in uuids)
        {
            if (string.IsNullOrEmpty(u) || u.EndsWith(":0:0:0", StringComparison.Ordinal)) continue;
            if (!bridge.TryGetEntityGeodetic(u, out var g)) continue;
            readable++;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"POS,{t},{u},{g.LatDeg:F6},{g.LonDeg:F6},{g.AltMeters:F1}"));
        }
        Console.WriteLine($"# t={t}s reflected={uuids.Count()} readable={readable}");
    }

    Console.WriteLine("[..] bridge.Stop() - resigning...");
    bridge.Stop();
    Console.WriteLine("[OK] resigned cleanly.");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
    try { bridge?.Stop(); } catch { /* best effort */ }
    return 2;
}
finally
{
    bridge?.Dispose();
}
