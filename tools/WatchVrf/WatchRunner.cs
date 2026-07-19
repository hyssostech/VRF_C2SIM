using System.Globalization;
using VrfC2Sim;
using VrfC2Sim.Tools;

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
// It ALSO consumes the two task/report events the facade already raises, so the trace can
// answer whether VR-Forces ever ACCEPTED a tasking - a position-only trace cannot tell a
// rejected task from an accepted-but-immobile unit, since both look identical (a static
// POS series). Same clock base, same stream, one record per event:
//     TSK,<elapsed-seconds>,<escaped-unitMarking>,<escaped-taskType>
//     RPT,<elapsed-seconds>,<escaped-text>
// NOTE both are UUID-LESS by design: TaskCompletedEventArgs carries only UnitMarking +
// TaskType and TextReportEventArgs carries only Text (VrfBridge.cpp:125-134). The fields
// above are exactly what the events deliver - nothing is synthesized to match POS's shape.
// Correlate TSK to POS via markingText -> uuid out of band.
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
    // Argument handling uses the shared tools/Shared/ToolArgs.cs standard (0 success /
    // 1 operational failure / 2 usage error with nothing done; usage text to STDERR).
    // The usage block itself lives in WatchVrfUsage so the offline --con-selftest path can
    // print it without touching this bridge-referencing type.
    public static int Run(string[] args)
    {
        // No options are valid on the LIVE path. --con-selftest is dispatched in Program.cs
        // and only when it is args[0]; reaching here with it (e.g. "WatchVrf 3399
        // --con-selftest") means the caller asked for two different things at once. For the
        // movement oracle that MUST be a hard failure, not a silently-ignored token.
        string[] unknown = ToolArgs.UnknownFlags(args);
        if (unknown.Length > 0)
            return ToolArgs.Usage($"unknown or misplaced option(s): {string.Join(" ", unknown)}. "
                                + "--con-selftest is offline-only and must be the sole argument.",
                                  WatchVrfUsage.Lines());

        string[] positional = ToolArgs.Positionals(args);
        int appNumber = 3399, durationSecs = 120, sampleSecs = 15;
        string federation = "CWIX-2024";
        string problem;

        // HARD-FAIL on unparseable input. Previously these were TryParse calls whose bool
        // result was DISCARDED, so a typo silently produced a trace of the wrong appNumber
        // or the wrong sample cadence while still reporting success. The Try* results below
        // are all checked; nothing falls back to a default after a parse failure.
        if (positional.Length >= 1 &&
            !ToolArgs.TryIntInRange(positional[0], "applicationNumber", 1, 65535, out appNumber, out problem))
            return ToolArgs.Usage(problem, WatchVrfUsage.Lines());

        if (positional.Length >= 2 &&
            !ToolArgs.TryPositiveInt(positional[1], "durationSecs", out durationSecs, out problem))
            return ToolArgs.Usage(problem, WatchVrfUsage.Lines());

        if (positional.Length >= 3 &&
            !ToolArgs.TryPositiveInt(positional[2], "sampleSecs", out sampleSecs, out problem))
            return ToolArgs.Usage(problem, WatchVrfUsage.Lines());

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
                return ToolArgs.ExitFailure;
            }
            bridge.BeginTrackingReflectedObjects();
            Console.WriteLine("[OK] joined; discovering + sampling (POS,t,uuid,lat,lon,alt ; "
                            + "CON,t,uuid,level,msg ; TSK,t,marking,taskType ; RPT,t,text)...");

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

            // TASK OUTCOME + REPORT streams, on the SAME clock base as POS/CON.
            //
            // WHY: a position-only trace cannot distinguish "VR-Forces rejected the task",
            // "accepted it and the unit could not move", and "silently dropped it" - all
            // three look like a static POS series. TaskCompleted is the acceptance/completion
            // signal and TextReport is the radio narrative; together they say whether the
            // simulator ever acknowledged the tasking at all. Both were already wired in the
            // facade and cost nothing to consume.
            //
            // Subscribed BEFORE the tick loop for the same reason as CON: these are pumped
            // only inside bridge.Tick(). Same try/catch wrapper - a formatting fault in a
            // sink must never propagate back into the native tick.
            bridge.TaskCompleted += (s, e) =>
            {
                try
                {
                    double tt = Math.Round((DateTime.UtcNow - start).TotalSeconds, 1);
                    Emit(ConFormat.TaskLine(tt, e.UnitMarking, e.TaskType));
                }
                catch (Exception ex)
                {
                    Emit(string.Create(CultureInfo.InvariantCulture,
                        $"# TSK handler error: {ex.GetType().Name}: {ex.Message}"));
                }
            };

            bridge.TextReport += (s, e) =>
            {
                try
                {
                    double tr = Math.Round((DateTime.UtcNow - start).TotalSeconds, 1);
                    Emit(ConFormat.ReportLine(tr, e.Text));
                }
                catch (Exception ex)
                {
                    Emit(string.Create(CultureInfo.InvariantCulture,
                        $"# RPT handler error: {ex.GetType().Name}: {ex.Message}"));
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
            return ToolArgs.ExitOk;
        }
        catch (Exception ex)
        {
            // OPERATIONAL failure, so exit 1 - NOT 2. This returned 2 before, which under the
            // shared standard means "usage error, NO ACTION WAS TAKEN". By the time control
            // reaches here the bridge may have joined the federation, so claiming nothing
            // happened would tell an unattended runner it is safe to reuse the appNumber.
            Console.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
            try { bridge?.Stop(); } catch { /* best effort */ }
            return ToolArgs.ExitFailure;
        }
        finally
        {
            bridge?.Dispose();
        }
    }
}
