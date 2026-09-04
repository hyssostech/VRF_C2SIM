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
// Args: [applicationNumber] [durationSecs] [sampleSecs] [federation] [--stop-file <path>]
//       [--diag] [--no-wait-ext]
// Defaults: 3399, 120, 15, CWIX-2024, no stop file (duration only), diagnostics off.
// --stop-file: the runner touches <path> when its observation window (plus trail) is over;
// the tick loop polls for it about once a second, emits one '# STOP requested' line and
// falls through to the SAME bridge.Stop() resign as the duration expiry. durationSecs is
// then the upper bound (safety net for a runner that dies mid-run).
// --diag / --no-wait-ext: the 5.2 OBSERVATION-CHANNEL instruments (2026-09-03). --diag adds
// '# DIAG' lines and per-sample reflected-LIST counts that bypass the UUID callbacks the
// 'reflected=' figure comes from; --no-wait-ext flips the one facade lever that changes what
// is reflected. Everything still goes to stdout in the existing line shapes - no new files -
// so a caller tees the stream. See WatchVrfUsage for the full contract.
internal static class WatchRunner
{
    // Argument handling uses the shared tools/Shared/ToolArgs.cs standard (0 success /
    // 1 operational failure / 2 usage error with nothing done; usage text to STDERR).
    // The usage block itself lives in WatchVrfUsage so the offline --con-selftest path can
    // print it without touching this bridge-referencing type.
    public static int Run(string[] args)
    {
        // --stop-file <path> is the ONE option valid on the LIVE path (2026-09-01, runner
        // turnaround). It is a VALUE-taking option, so it is extracted FIRST with
        // TryTakeOptionValue - otherwise Positionals() would see its path as a stray
        // positional and mis-assign it (see the ToolArgs note on that helper).
        string stopFile = null;
        string problem;
        if (!ToolArgs.TryTakeOptionValue(args, WatchVrfUsage.StopFileFlag, out args, out stopFile, out problem))
            return ToolArgs.Usage(problem, WatchVrfUsage.Lines());
        if (stopFile != null)
        {
            try { stopFile = Path.GetFullPath(stopFile); }
            catch (Exception ex)
            {
                return ToolArgs.Usage($"{WatchVrfUsage.StopFileFlag} '{stopFile}' is not a usable path: "
                                    + $"{ex.GetType().Name}: {ex.Message}", WatchVrfUsage.Lines());
            }
            // A pre-existing stop file would end the observation on the first poll and leave
            // a trace with no samples that still reports exit 0 - the false-green shape this
            // project keeps hitting. Refuse BEFORE joining, so nothing is consumed.
            if (File.Exists(stopFile))
                return ToolArgs.Usage($"{WatchVrfUsage.StopFileFlag} '{stopFile}' ALREADY EXISTS; the observation "
                                    + "would end immediately. Remove it or pass a fresh path. Nothing joined.",
                                      WatchVrfUsage.Lines());
        }

        // Two VALUELESS options on the LIVE path (2026-09-03, 5.2 observation-channel
        // instruments). Both are read AFTER --stop-file's pair has been taken out and
        // BEFORE UnknownFlags, which is told about them so they are not rejected. Neither
        // is a positional, so Positionals() already ignores them.
        bool diag = ToolArgs.HasFlag(args, WatchVrfUsage.DiagFlag);
        bool noWaitExt = ToolArgs.HasFlag(args, WatchVrfUsage.NoWaitExtFlag);

        // No OTHER options are valid on the LIVE path. --con-selftest and --capabilities are
        // dispatched in Program.cs and only when they are args[0]; reaching here with one
        // (e.g. "WatchVrf 3399 --con-selftest") means the caller asked for two different
        // things at once. For the movement oracle that MUST be a hard failure, not a
        // silently-ignored token.
        string[] unknown = ToolArgs.UnknownFlags(args, WatchVrfUsage.DiagFlag, WatchVrfUsage.NoWaitExtFlag);
        if (unknown.Length > 0)
            return ToolArgs.Usage($"unknown or misplaced option(s): {string.Join(" ", unknown)}. "
                                + "--con-selftest and --capabilities are offline-only and must be the sole argument.",
                                  WatchVrfUsage.Lines());

        string[] positional = ToolArgs.Positionals(args);
        int appNumber = 3399, durationSecs = 120, sampleSecs = 15;
        string federation = null;   // null = stack default (5.0.2 CWIX-2024; 5.2 config-file identity)

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
            // OPT-IN probe lever; false (the bridge default) leaves Start() on the shipped
            // path. See the --no-wait-ext usage text and VrfFacade.h.
            DisableWaitForVrfExtendedData = noWaitExt,
        };
        // Stack-aware identity (tools/Shared/StackIdentity.cs): 5.0.2 keeps the
        // CWIX-2024 constants; 5.2 joins via the connection config (MAK-ONE-2025).
        string fedDesc = StackIdentity.Apply(cfg, federation);

        Console.WriteLine("=== WatchVrf - position + Object Console telemetry (R3 / groundwork 0.6) ===");
        Console.WriteLine($"    {fedDesc} appNumber={appNumber} duration={durationSecs}s sample={sampleSecs}s"
                        + (stopFile != null ? $" stop-file={stopFile} (duration is the upper bound)" : "")
                        + (diag ? " diag=on" : "") + (noWaitExt ? " no-wait-ext=on" : "") + "\n");

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

            // JOIN-TIME DIAGNOSTICS. '#' lines on stdout like every other non-CSV record, so
            // an existing trace reader skips them and a caller can simply tee the stream.
            //
            // WHY THE LICENCE LINE. Assistant-free RTI (RTI_ASSISTANT_DISABLE) removes the
            // License Not Found dialog, MAK RTI Users Guide 8.3 then runs the federate
            // UNLICENSED, and 8.2 says licensed and unlicensed federates exchange NO
            // messages - which looks exactly like reflected=0 (COLDSTART_REVIEW_2026-09-03
            // R2). Without this line an empty trace cannot be told from an unlicensed one.
            if (diag)
            {
                Emit("# DIAG stack=" + VrfBridge.NativeStackInfo());
                Emit(string.Create(CultureInfo.InvariantCulture,
                    $"# DIAG licence rti={(VrfBridge.HaveRtiLicense() ? 1 : 0)} "
                  + $"vrlink={(VrfBridge.HaveVrLinkLicense() ? 1 : 0)} "
                  + $"no-wait-ext={(noWaitExt ? 1 : 0)}"));
            }

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
            // --stop-file poll cadence. One File.Exists per second is nothing; per 50 ms tick
            // would be 20 stats/s for no gain in stop latency that matters at a 2 s sample
            // cadence. The stop is checked BEFORE the sample-due test so a stop that lands
            // between samples ends the run without waiting for the next sample.
            var nextStopCheck = start.AddSeconds(1);
            while ((DateTime.UtcNow - start).TotalSeconds < durationSecs)
            {
                bridge.Tick();
                Thread.Sleep(50);
                if (stopFile != null && DateTime.UtcNow >= nextStopCheck)
                {
                    nextStopCheck = DateTime.UtcNow.AddSeconds(1);
                    if (File.Exists(stopFile))
                    {
                        double ts = Math.Round((DateTime.UtcNow - start).TotalSeconds, 1);
                        // A '#' line like the per-sample summary, so every trace reader that
                        // already skips comments skips this one too. Recorded so a scorer can
                        // tell "stopped on request at t" from "ran out its duration".
                        Emit(string.Create(CultureInfo.InvariantCulture,
                            $"# STOP requested via stop-file at t={ts}s (duration cap was {durationSecs}s)"));
                        break;
                    }
                }
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
                // The summary line keeps its exact shape when --diag is off. With --diag it
                // GAINS trailing fields rather than becoming a second line, so both numbers
                // for one instant stay on one record: 'reflected=' is what OUR UUID-change
                // callbacks collected, ent=/agg=/env=/ctl= are the reflected lists' own
                // count()s. The two disagreeing is the H2 finding; both zero kills H2 with H3.
                string summary = string.Create(CultureInfo.InvariantCulture,
                    $"# t={t}s reflected={uuids.Count()} readable={readable}");
                if (diag)
                {
                    var c = bridge.ReflectedCounts();
                    summary += string.Create(CultureInfo.InvariantCulture,
                        $" ent={c.Entities} agg={c.Aggregates} env={c.EnvironmentProcesses}"
                      + $" ctl={c.ControlObjects} extattr={c.ExtendedAttributes}"
                      + $" waitext={(c.WaitingForVrfExtendedData ? 1 : 0)}"
                      + $" discovered={bridge.HasDiscoveredObjects()}");
                }
                Emit(summary);
            }

            // VR-Forces' OWN per-type breakdown, once, after the whole observation window -
            // the vendor cross-check on the ent=/agg=/env=/ctl= numbers above. Bracketed
            // because its text is MAK notify format, not CSV, so a reader that skips '#'
            // lines must also skip whatever lies between the two markers.
            // AN EMPTY BRACKET IS NOT A RESULT. The MAK notify stream is NOT this process's
            // Console.Out: where it lands is a VR-Forces/notify-level matter, so nothing
            // between the markers means "not captured here", never "VR-Forces reported
            // zero objects". The numbers that ARE ours are the '# t=' fields.
            if (diag)
            {
                Emit("# DIAG vendor printReflectedObjectCounts BEGIN (MAK notify stream, not this "
                   + "tool's stdout; empty means NOT CAPTURED, not zero)");
                bridge.PrintReflectedObjectCounts();
                Emit("# DIAG vendor printReflectedObjectCounts END");
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
