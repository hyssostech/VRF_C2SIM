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
// Beside every POS line it emits the SAME object's UN-EXTRAPOLATED state:
//     RAW,<elapsed-seconds>,<uuid>,<rawLat>,<rawLon>,<rawAlt>,<velX>,<velY>,<velZ>
// POS reports location() - the position computed THROUGH VR-Link's dead-reckoning
// approximator. RAW reports lastSetLocation() and lastSetVelocity(): the values VR-Forces
// last actually SENT (baseEntityStateRepository.h:118/:133). Same t, same uuid, so the two
// are directly comparable - sustained POS-vs-RAW divergence indicts the approximator,
// agreement exonerates it. lat/lon F6 and alt F1 exactly as POS formats them; velocity is
// GEOCENTRIC m/s at F3 (an ECEF frame - velY is NOT "north"), float-precision at source.
//
// In parallel it subscribes to VR-Forces' Object Console channel (the yellow warning
// badge - docs/VRF_GROUND_TRUTH.md sec 0.0/sec 7) and prints, on the SAME UTC clock base
// as the POS lines, one line per captured message:
//     CON,<elapsed-seconds>,<uuid>,<notifyLevel>,<escaped-message>
// (message escaping: see ConFormat). One process, one timeline, both streams.
//
// It also subscribes to the BACKEND console - a second, INDEPENDENT console path, per sim
// engine rather than per object:
//     BCON,<elapsed-seconds>,<simAddress>,<notifyLevel>,<escaped-message>
// This exists to disambiguate an empty CON stream: without it, "no warnings were raised"
// and "warnings were raised but never delivered" are indistinguishable. Traffic on BCON
// while CON stays silent localises the fault to the object-console delivery path.
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
// OPTIONALLY (--console-log-dir <path>) it also arms BACKEND-SIDE console capture: for every
// uuid it discovers, once, it raises that object's notify level to the maximum and asks the
// simulating backend to write that object's console to <path>/console-<uuid>.log on the
// BACKEND'S OWN filesystem. This exists because an empty CON stream cannot, by itself,
// distinguish "no warnings were raised" from "warnings were raised and not delivered here" -
// the backend-written file bypasses the delivery path entirely, so a populated file beside an
// empty CON stream proves a delivery gap and an empty file proves silence. Each arming is
// recorded in the trace:
//     CONARM,<elapsed-seconds>,<uuid>,<path>
// WITHOUT the flag, nothing about the run changes - no calls are made and no CONARM lines are
// emitted. The flag is the whole opt-in.
//
// Args: [applicationNumber] [durationSecs] [sampleSecs] [federation] [--console-log-dir <path>]
// Defaults: 3399, 120, 15, CWIX-2024, no backend-side capture.
internal static class WatchRunner
{
    // Argument handling uses the shared tools/Shared/ToolArgs.cs standard (0 success /
    // 1 operational failure / 2 usage error with nothing done; usage text to STDERR).
    // The usage block itself lives in WatchVrfUsage so the offline --con-selftest path can
    // print it without touching this bridge-referencing type.

    // The only option the LIVE path accepts. Value-taking, so it is extracted (flag AND
    // value) before Positionals()/UnknownFlags() run - see ToolArgs.TryTakeOptionValue.
    private const string ConsoleLogDirFlag = "--console-log-dir";

    // DtNotifyLevelType (vlutil/vlPrint.h:39) is 0 fatal .. 4 debug, and the facade CLAMPS
    // into that range (VrfFacade.cpp:828-836), so the maximum is passed as a literal. The
    // point of arming at the maximum is to remove thresholding as an explanation for an
    // empty console stream: at level 4 nothing the object emits is filtered out.
    private const int MaxNotifyLevel = 4;

    public static int Run(string[] args)
    {
        // --console-log-dir takes a VALUE, so both tokens come out of the array first.
        // If the pair stayed in, ToolArgs.Positionals would hand the directory path to the
        // applicationNumber parser and the run would fail describing the wrong thing.
        string problem;
        if (!ToolArgs.TryTakeOptionValue(args, ConsoleLogDirFlag,
                                         out string[] rest, out string consoleLogDir, out problem))
            return ToolArgs.Usage(problem, WatchVrfUsage.Lines());

        // No OTHER option is valid on the LIVE path. --con-selftest is dispatched in
        // Program.cs and only when it is args[0]; reaching here with it (e.g. "WatchVrf 3399
        // --con-selftest") means the caller asked for two different things at once. For the
        // movement oracle that MUST be a hard failure, not a silently-ignored token.
        string[] unknown = ToolArgs.UnknownFlags(rest);
        if (unknown.Length > 0)
            return ToolArgs.Usage($"unknown or misplaced option(s): {string.Join(" ", unknown)}. "
                                + "--con-selftest is offline-only and must be the sole argument.",
                                  WatchVrfUsage.Lines());

        string[] positional = ToolArgs.Positionals(rest);
        int appNumber = 3399, durationSecs = 120, sampleSecs = 15;
        string federation = "CWIX-2024"; // 'problem' is already declared above.

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

        // Resolve and CREATE the console-log directory. POSITION IS LOAD-BEARING, in BOTH
        // directions, and this is the only spot that satisfies both constraints:
        //   * AFTER every argument check above. This block has a SIDE EFFECT (it creates a
        //     directory), and exit 2 under the shared ToolArgs standard means "usage error,
        //     NO ACTION WAS TAKEN". Doing it earlier left a directory behind on runs that
        //     then exited 2 for an unrelated bad argument - measured, not theorised.
        //   * BEFORE bridge.Start(). A bad path discovered after the join could no longer be
        //     reported as exit 2 at all, because a federate would already be in the
        //     federation. Failing here still creates nothing on failure, so exit 2 stays true.
        //
        // NOTE the directory is created LOCALLY but the path is consumed by the BACKEND, which
        // may be a different machine. Local creation proves the path is well-formed and
        // writable HERE; it does not prove the backend can write it. The banner says so
        // rather than leaving it assumed, because an unwritable backend path is a SILENT no-op
        // in the VR-Forces API and would otherwise masquerade as "no messages were raised" -
        // the exact wrong conclusion this feature exists to prevent.
        if (consoleLogDir != null)
        {
            try
            {
                consoleLogDir = Path.GetFullPath(consoleLogDir);
                Directory.CreateDirectory(consoleLogDir);
            }
            catch (Exception ex)
            {
                return ToolArgs.Usage(
                    $"{ConsoleLogDirFlag} '{consoleLogDir}' is not usable: "
                  + $"{ex.GetType().Name}: {ex.Message}",
                    WatchVrfUsage.Lines());
            }
        }

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
        Console.WriteLine($"    federation={federation} appNumber={appNumber} duration={durationSecs}s sample={sampleSecs}s");
        if (consoleLogDir != null)
        {
            Console.WriteLine($"    console-log-dir={consoleLogDir} (backend-side capture ARMED at notifyLevel={MaxNotifyLevel})");
            Console.WriteLine("    NOTE: the file is written by the BACKEND on the BACKEND's filesystem. If the");
            Console.WriteLine("          backend is not this machine, that path is resolved THERE and this local");
            Console.WriteLine("          directory will stay empty. The API acknowledges nothing either way.");
        }
        Console.WriteLine();

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
                            + "RAW,t,uuid,lat,lon,alt,vx,vy,vz ; CON,t,uuid,level,msg ; "
                            + "BCON,t,simAddr,level,msg ; TSK,t,marking,taskType ; RPT,t,text)...");

            var start = DateTime.UtcNow;

            // uuids already armed for backend-side console capture (--console-log-dir only).
            // Ordinal, because a uuid is an opaque identifier, not display text.
            var armed = new HashSet<string>(StringComparer.Ordinal);

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

            // BACKEND console stream, on the same clock base and through the same Emit lock.
            // WHY: an empty CON stream is ambiguous - it cannot distinguish "VR-Forces raised
            // no object-console warnings" from "warnings were raised but never delivered to
            // us". BCON is an INDEPENDENT delivery path (per sim engine, not per object), so
            // traffic here alongside an empty CON localises the fault to the object-console
            // path. Subscribed before the tick loop for the same reason as CON, and wrapped in
            // the same try/catch so a sink fault never crosses back into the native tick.
            bridge.BackendConsoleMessage += (s, e) =>
            {
                try
                {
                    double tb = Math.Round((DateTime.UtcNow - start).TotalSeconds, 1);
                    Emit(ConFormat.BackendLine(tb, e.SimAddress, e.NotifyLevel, e.Message));
                }
                catch (Exception ex)
                {
                    Emit(string.Create(CultureInfo.InvariantCulture,
                        $"# BCON handler error: {ex.GetType().Name}: {ex.Message}"));
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

                // ---- BACKEND-SIDE console capture, armed ONCE per uuid -------------------
                // Deliberately a SEPARATE pass, run before the POS pass and NOT filtered by
                // TryGetEntityGeodetic. A unit that never moves may also never present a
                // readable location, and an immobile unit is precisely the one whose console
                // we need; gating arming on readability would arm only the objects we already
                // have telemetry for. The ":0:0:0" null-uuid guard is kept - that is not an
                // object.
                //
                // WHY BACKEND-SIDE AT ALL: two live runs captured ZERO CON lines from six
                // created units. That is ambiguous - "VR-Forces raised no warnings" and
                // "warnings were raised but not delivered to this observer federate" produce
                // an identical empty stream. logObjectConsoleToFile makes the backend write
                // the object's console to its OWN filesystem, bypassing the delivery path
                // under suspicion, so the two cases stop looking alike:
                //     file has warnings + CON empty  -> the DELIVERY path is at fault
                //     file empty                     -> nothing was raised, PROVEN not inferred
                // The notify level is raised first so a low default threshold cannot be the
                // third explanation.
                //
                // ONCE per uuid, tracked in 'armed': discovery is incremental, so this loop
                // re-sees every previously-known uuid on every tick. Re-issuing
                // logObjectConsoleToFile each tick would re-open the same file on the backend
                // for the lifetime of the run, for every object, to no benefit.
                if (consoleLogDir != null)
                {
                    foreach (string u in uuids)
                    {
                        if (string.IsNullOrEmpty(u) || u.EndsWith(":0:0:0", StringComparison.Ordinal)) continue;
                        if (!armed.Add(u)) continue;
                        string path = Path.Combine(consoleLogDir,
                                                   $"console-{ConFormat.SafeUuidForFilename(u)}.log");
                        try
                        {
                            bridge.SetObjectNotifyLevel(u, MaxNotifyLevel);
                            bridge.LogObjectConsoleToFile(u, path);
                            Emit(ConFormat.ArmLine(t, u, path));
                        }
                        catch (Exception ex)
                        {
                            // Diagnostic instrumentation must never take down the oracle:
                            // POS remains the primary product of this run. Recorded as a
                            // comment (not a CONARM line - nothing was successfully armed)
                            // and NOT retried, since 'armed' already contains the uuid.
                            Emit(string.Create(CultureInfo.InvariantCulture,
                                $"# CONARM failed for {u}: {ex.GetType().Name}: {ex.Message}"));
                        }
                    }
                }

                int readable = 0;
                foreach (string u in uuids)
                {
                    if (string.IsNullOrEmpty(u) || u.EndsWith(":0:0:0", StringComparison.Ordinal)) continue;
                    if (!bridge.TryGetEntityGeodetic(u, out var g)) continue;
                    readable++;
                    Emit(string.Create(CultureInfo.InvariantCulture,
                        $"POS,{t},{u},{g.LatDeg:F6},{g.LonDeg:F6},{g.AltMeters:F1}"));

                    // RAW,... paired with the POS line just emitted (same t, same uuid).
                    // POS is location() - THROUGH the dead-reckoning approximator; RAW is
                    // lastSetLocation()/lastSetVelocity() - what VR-Forces actually sent.
                    // Emitted only on success and only AFTER the POS line, so POS's content,
                    // ordering and field layout are byte-identical to before this addition.
                    // TryGetEntityMotion resolves the same objects as TryGetEntityGeodetic, so
                    // a failure here is not expected; it is tolerated silently rather than
                    // suppressing the POS sample, because POS remains the primary oracle and
                    // must not become dependent on the diagnostic stream succeeding.
                    if (bridge.TryGetEntityMotion(u, out var m))
                        Emit(ConFormat.RawLine(t, u,
                                               m.Raw.LatDeg, m.Raw.LonDeg, m.Raw.AltMeters,
                                               m.VelX, m.VelY, m.VelZ));
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
