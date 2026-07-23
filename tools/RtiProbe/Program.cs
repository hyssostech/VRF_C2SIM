using System.Diagnostics;
using VrfC2Sim;
using VrfC2Sim.Tools;

// tools/RtiProbe - a dedicated, minimal RTI READINESS probe.
//
// WHY THIS EXISTS (2026-07-23, STEP 1 of the launch hardening -
// docs/RTI_LAUNCH_HARDENING_DESIGN.md, ADJUDICATION ADDENDUM A2-A7):
// two live confirming runs went VOID on infrastructure. RUN 2: the VR-Forces
// back-end lost a TCP race at createFederationExecution against a fresh-booting
// RTI ("connection has been BROKEN" = listener up but not yet able to service a
// create/join), and the runner then blindly pushed init/order at a back-end that
// never joined -> a confusing VOID. This tool is the pre-launch GATE: it PROVES
// the RTI can service a create-or-join RIGHT NOW - the exact createFederation-
// Execution the back-end will do (VrfBridge -> VrfFacade::Start builds
// new DtExerciseConn(--execName <federation>), VrfFacade.cpp:319-325) - BEFORE
// VR-Forces launches, and the runner FAILS THE LAUNCH LOUDLY if it cannot.
//
// A2/A3: the C1 gate must run BEFORE the back-end exists, so it cannot be CreateOne
// (CreateOne refuses to act without a discovered back-end). Like WatchVrf, this
// tool joins as a throwaway federate that needs no back-end, then resigns cleanly.
// It is a DEDICATED tool (not a new WatchVrf mode) so the WatchVrf scoring-oracle
// exit-code contract is never put at risk (supervisor decision, DESIGN doc).
//
// A7: retry-with-backoff is INTERNAL, on a SINGLE ledgered appNumber (cleaner
// ledger than burning one integer per external attempt). The retry is what waits
// out a cold-create window: a create/join that FAILS (Start returns false or
// throws before joining) did not register a federate, so re-attempting on the same
// appNumber is safe - it is exactly the RUN-2 race being absorbed.
//
// EXIT CONTRACT (exact - the runner switches on this; RtiProbe exit == runner gate):
//   0  RTI serviceable: a create-or-join SUCCEEDED and RtiProbe resigned cleanly.
//   1  RTI NOT ready: every attempt failed (operational). Mirrors WatchVrf's
//      rationale that an operational failure after a POSSIBLE join is exit 1, never
//      2. Also returned if a join SUCCEEDED but the clean resign failed - see below.
//   2  usage / bad args - NO ACTION TAKEN (via tools/Shared/ToolArgs.cs standard;
//      usage text to STDERR). For the runner this can only mean it built bad args.
//
// Self-resigns on EVERY exit path (best-effort Stop + Dispose in catch/finally) so a
// killed parent cannot strand a joined federate - the same posture as WatchVrf.
//
// LAUNCH ENV (identical to CreateOne/WatchVrf - RUNBOOK sec 7): RTI 4.6.1 on PATH,
// MAKLMGRD_LICENSE_FILE from Machine scope, cwd = C:\MAK\vrforces5.0.2\bin64, and a
// FRESH ApplicationNumber (Appendix B ledger; never reuse across runs).
//
// Args: <appNumber> [federation=CWIX-2024] [maxAttempts=5] [settleSecs=2] [backoffSecs=3]
//   appNumber is MANDATORY. Do NOT run this with a valid appNumber offline: it attempts
//   a REAL create/join against a live RTI federation.

static string[] UsageLines() => new[]
{
    "usage:  RtiProbe.exe <appNumber> [federation] [maxAttempts] [settleSecs] [backoffSecs]",
    "        appNumber   MANDATORY, 1..65535, must be FRESH (Appendix B ledger; never reuse).",
    "        federation  default CWIX-2024.",
    "        maxAttempts default 5   (internal retries on the SAME appNumber).",
    "        settleSecs  default 2   (tick this long after a join before resigning).",
    "        backoffSecs default 3   (sleep this long between failed attempts).",
    "",
    "        exit 0 = RTI serviceable (create/join OK, clean resign);",
    "             1 = RTI NOT ready (all attempts failed / operational);",
    "             2 = usage / bad args (NO action taken).",
    "",
    "example:  RtiProbe.exe 3597",
    "          RtiProbe.exe 3597 CWIX-2024 5 2 3",
};

// --- argument handling (tools/Shared/ToolArgs.cs standard: 0 ok / 1 op / 2 usage) ---
string[] unknown = ToolArgs.UnknownFlags(args);
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}. "
                        + "RtiProbe takes positional arguments only.", UsageLines());

string[] positional = ToolArgs.Positionals(args);
if (positional.Length < 1)
    return ToolArgs.Usage("missing appNumber.", UsageLines());

if (!ToolArgs.TryIntInRange(positional[0], "appNumber", 1, 65535, out int appNumber, out string problem))
    return ToolArgs.Usage(problem, UsageLines());

string federation = "CWIX-2024";
int maxAttempts = 5, settleSecs = 2, backoffSecs = 3;

if (positional.Length >= 2 && !string.IsNullOrWhiteSpace(positional[1])) federation = positional[1];
if (positional.Length >= 3 &&
    !ToolArgs.TryPositiveInt(positional[2], "maxAttempts", out maxAttempts, out problem))
    return ToolArgs.Usage(problem, UsageLines());
if (positional.Length >= 4 &&
    !ToolArgs.TryPositiveInt(positional[3], "settleSecs", out settleSecs, out problem))
    return ToolArgs.Usage(problem, UsageLines());
if (positional.Length >= 5 &&
    !ToolArgs.TryPositiveInt(positional[4], "backoffSecs", out backoffSecs, out problem))
    return ToolArgs.Usage(problem, UsageLines());

// FED / FOM must match the running federation (RUNBOOK sec 7) - identical constants to
// CreateOne and WatchVrf, because this probe must exercise the SAME create/join path.
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

Console.WriteLine("=== RtiProbe - RTI readiness gate (create-or-join, internal retry+backoff) ===");
Console.WriteLine($"    federation={federation}  appNumber={appNumber}  maxAttempts={maxAttempts}  "
                + $"settle={settleSecs}s  backoff={backoffSecs}s");
Console.WriteLine("    exit 0 = serviceable (join OK, clean resign); 1 = NOT ready; 2 = usage.");
Console.WriteLine();

for (int attempt = 1; attempt <= maxAttempts; attempt++)
{
    VrfBridge bridge = null;
    bool started = false;
    try
    {
        bridge = new VrfBridge();
        Console.WriteLine($"[..] attempt {attempt}/{maxAttempts}: bridge.Start() - create-or-join {federation}...");
        started = bridge.Start(cfg);
        if (started)
        {
            // Let the join settle (mirror CreateOne/WatchVrf ~50ms tick cadence) so a
            // create-or-join that returns true but is still wiring up is exercised, not
            // just the immediate return.
            var settle = Stopwatch.StartNew();
            while (settle.Elapsed < TimeSpan.FromSeconds(settleSecs)) { bridge.Tick(); Thread.Sleep(50); }

            Console.WriteLine("[..] bridge.Stop() - resigning cleanly...");
            bridge.Stop();
            bridge.Dispose();
            bridge = null;   // resign+dispose done; keep the finally from double-disposing
            Console.WriteLine($"[OK] RTI serviceable on attempt {attempt}/{maxAttempts}: "
                            + $"created/joined {federation} and resigned cleanly.");
            return ToolArgs.ExitOk;
        }

        // Start returned false: the create/join did not succeed (RTI not ready yet).
        // Resign any half-open connection before retrying - Start can fail AFTER the
        // exercise connection was constructed (CreateOne/Program.cs:131-138 note).
        Console.WriteLine($"[..] attempt {attempt}/{maxAttempts}: bridge.Start() returned false "
                        + "(RTI not serviceable yet).");
        try { bridge.Stop(); } catch { /* best effort - never leave a joined federate */ }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[..] attempt {attempt}/{maxAttempts}: bridge.Start() threw "
                        + $"{ex.GetType().Name}: {ex.Message}");
        try { bridge?.Stop(); } catch { /* best effort - never leave a joined federate */ }

        // A throw AFTER a successful join means the RTI DID service the create/join but the
        // clean resign failed. Do NOT loop back to retry on the SAME appNumber - that could
        // collide with the federate this attempt may have left registered (the stale-federate
        // trap, RUNBOOK sec 0). Treat it as a terminal not-cleanly-serviceable outcome: the
        // finally disposes, and we refuse readiness (exit 1) rather than risk a poisoned join.
        if (started)
        {
            Console.WriteLine("[FAIL] joined but the clean resign FAILED; NOT retrying on the same "
                            + "appNumber (stale-federate risk). Refusing to declare the RTI serviceable.");
            return ToolArgs.ExitFailure;
        }
    }
    finally
    {
        try { bridge?.Dispose(); } catch { /* best effort */ }
    }

    if (attempt < maxAttempts)
    {
        Console.WriteLine($"[..] backing off {backoffSecs}s before retry...");
        Thread.Sleep(TimeSpan.FromSeconds(backoffSecs));
    }
}

Console.WriteLine($"[FAIL] RTI NOT serviceable after {maxAttempts} attempt(s) against {federation} "
                + $"(appNumber {appNumber}). The RTI could not service a create/join. Refusing to "
                + "declare readiness - the launch must NOT proceed.");
return ToolArgs.ExitFailure;
