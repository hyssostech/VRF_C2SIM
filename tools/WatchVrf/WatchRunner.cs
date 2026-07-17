using System.Globalization;
using System.Linq;
using VrfC2Sim;

namespace WatchVrf;

// tools/WatchVrf live path - MEMBER-LEVEL position telemetry for a live VR-Forces
// federation (docs/UNIT_MOVEMENT_RESEARCH.md plan R3) PLUS the per-unit Object Console
// warning stream (groundwork plan 0.6). GUI-independent observation channel.
//
// Joins the federation as a read-only observer, discovers EVERY reflected object via the
// ResetVrf reflection machinery (BeginTrackingReflectedObjects), then samples each
// object's geodetic position on an interval and prints CSV lines:
//     POS,<elapsed-seconds>,<uuid>,<latDeg>,<lonDeg>,<altM>
// Objects with no readable location (routes, areas, not-yet-resolved) are skipped.
// Subordinate entities of disaggregated units ARE reflected objects, so this captures
// the member-level picture the hung GUI cannot show (runaway vs scatter vs march).
//
// In parallel it subscribes to VR-Forces' Object Console channel (the yellow warning
// badge - docs/VRF_GROUND_TRUTH.md sec 0.0/sec 7) and prints, on the SAME UTC clock base
// as the POS lines, one line per captured message:
//     CON,<elapsed-seconds>,<uuid>,<notifyLevel>,<escaped-message>
// (message escaping: see ConFormat). One process, one timeline, both streams.
//
// Resigns CLEANLY at the end (no stale federate). Pure VR-Forces: no C2SIM / STOMP.
//
// This live logic is kept OUT of Program.cs's top-level Main so the --con-selftest path
// never JITs a method that references VrfBridge, and thus never loads the native bridge
// DLL (which needs the MAK bin dirs on PATH) - the selftest stays fully offline.
//
// LAUNCH ENV (identical to the app - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.0.2\bin64, and a
// FRESH ApplicationNumber each run.
//
// Args: [applicationNumber] [durationSecs] [sampleSecs] [federation]
// Defaults: 3399, 120, 15, CWIX-2024.
internal static class WatchRunner
{
    public static int Run(string[] args)
    {
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

        Console.WriteLine("=== WatchVrf - position + Object Console telemetry (R3 / groundwork 0.6) ===");
        Console.WriteLine($"    federation={federation} appNumber={appNumber} duration={durationSecs}s sample={sampleSecs}s\n");

        // All DATA lines (POS, CON, and the # summary) go through this one lock so a CON
        // callback that arrives on a different thread than the sampling loop can never tear
        // a line or interleave mid-line. In practice the facade dispatches OnObjectConsole-
        // Message synchronously on the Tick() thread (the same thread as the loop below),
        // so the lock is normally uncontended - it is defensive, not a hot path.
        object sync = new object();
        void Emit(string s) { lock (sync) Console.Out.WriteLine(s); }

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
            Console.WriteLine("[OK] joined; discovering + sampling (POS,t,uuid,lat,lon,alt ; CON,t,uuid,level,msg)...");

            var start = DateTime.UtcNow;

            // Subscribe BEFORE the tick loop so no console message is missed (messages are
            // only pumped inside bridge.Tick(), which runs in the loop). 'start' is already
            // assigned, so the CON timestamp shares the exact base + UTC clock as POS. The
            // handler is wrapped so a formatting fault cannot propagate into the native tick.
            bridge.ObjectConsoleMessage += (s, e) =>
            {
                try
                {
                    double tc = Math.Round((DateTime.UtcNow - start).TotalSeconds, 1);
                    Emit(ConFormat.Line(tc, e.Uuid, e.NotifyLevel, e.Message));
                }
                catch (Exception ex)
                {
                    // Never let a sink error cross back into VR-Forces' tick.
                    Emit(string.Create(CultureInfo.InvariantCulture,
                        $"# CON handler error: {ex.GetType().Name}: {ex.Message}"));
                }
            };

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
                    Emit(string.Create(CultureInfo.InvariantCulture,
                        $"POS,{t},{u},{g.LatDeg:F6},{g.LonDeg:F6},{g.AltMeters:F1}"));
                }
                Emit(string.Create(CultureInfo.InvariantCulture,
                    $"# t={t}s reflected={uuids.Count()} readable={readable}"));
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
    }
}
