using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;
using VrfC2Sim.Tools;

// Cleanly stop a running VRF_C2SIM interface (C++ or .NET) by driving the C2SIM server
// to UNINITIALIZED (STOP then RESET - NOT INITIALIZE), which the interface detects and
// uses to resign from the RTI without leaving a stale federate (RUNBOOK sec 4).
//
//   StopIface.exe <restUrl> <stompUrl> --yes
//   StopIface.exe <restUrl> <stompUrl> --dry-run
//
// THIS TOOL IS DESTRUCTIVE. It drives a LIVE C2SIM server out of RUNNING and discards
// the session. Everything below exists to make that impossible to do by accident.
//
// WHY THE ARGUMENTS ARE REQUIRED - NO DEFAULT, BY DESIGN (the SetSimRate rule,
// SetSimRate/Program.cs:35-38): this tool previously defaulted restUrl/stompUrl to
// 127.0.0.1 when args.Length == 0. That made a bare, argument-less invocation a VALID and
// FULLY DESTRUCTIVE one. On 2026-07-18 that drove a live server RUNNING -> UNINITIALIZED
// during what the operator intended as a read-only "what are this tool's arguments?"
// probe. A tool whose no-argument behavior is "destroy the default target" cannot be
// safely probed, and cannot be safely placed in an unattended runner. The defaults are
// therefore GONE: the endpoints must be named explicitly every time.
//
// WHY --yes IS ALSO REQUIRED: naming the endpoints proves the operator knows WHICH server;
// it does not prove they meant to tear it down. --yes is the separate statement of intent,
// and NO PushCommand is issued without it.
//
// WHY --dry-run EXISTS: tools/ResetVrf accepts --dry-run, so operators carry that habit
// here (CreateOne/Program.cs:47-49 documents exactly this hazard). Rather than reject it,
// this tool honors it: --dry-run reads and reports status, states what it WOULD send, and
// exits 0 having sent NOTHING.
//
// EXIT CODES: 0 success (or a completed dry run), 1 operational failure (INCLUDING the
// server not actually reaching UNINITIALIZED), 2 usage error with NOTHING sent.

const string UsageLine = "usage: StopIface.exe <restUrl> <stompUrl> (--yes | --dry-run)";

string[] UsageText() => new[]
{
    UsageLine,
    "",
    "  restUrl    REQUIRED. NO DEFAULT - e.g. http://127.0.0.1:8080/C2SIMServer",
    "  stompUrl   REQUIRED. NO DEFAULT - e.g. http://127.0.0.1:61613/topic/C2SIM",
    "             Both are REQUIRED BY DESIGN. This tool used to default them to",
    "             localhost, which made a no-argument run a fully destructive run",
    "             against the live server. Name the target explicitly.",
    "",
    "  --yes      REQUIRED to act. Confirms you intend to drive this server to",
    "             UNINITIALIZED (STOP then RESET). Without it NOTHING is sent.",
    "  --dry-run  Report current status and what WOULD be sent, then exit 0 having",
    "             sent nothing. Mutually exclusive with --yes.",
    "",
    "examples:  StopIface.exe http://127.0.0.1:8080/C2SIMServer http://127.0.0.1:61613/topic/C2SIM --dry-run",
    "           StopIface.exe http://127.0.0.1:8080/C2SIMServer http://127.0.0.1:61613/topic/C2SIM --yes",
};

// -- argument validation: NOTHING below this block may contact the server ------------

string[] unknown = ToolArgs.UnknownFlags(args, "--yes", "--dry-run");
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}.", UsageText());

bool yes = ToolArgs.HasFlag(args, "--yes");
bool dryRun = ToolArgs.HasFlag(args, "--dry-run");
string[] positional = ToolArgs.Positionals(args);

if (positional.Length == 0 && !yes && !dryRun)
    return ToolArgs.Usage("No arguments. This tool is DESTRUCTIVE and has NO defaults: "
                        + "<restUrl>, <stompUrl> and --yes are all required. Nothing was sent.",
                          UsageText());

if (yes && dryRun)
    return ToolArgs.Usage("--yes and --dry-run are mutually exclusive. Refusing to guess "
                        + "which one you meant. Nothing was sent.", UsageText());

if (positional.Length < 1)
    return ToolArgs.Usage("Missing <restUrl>. It is REQUIRED and has NO default.", UsageText());
if (positional.Length < 2)
    return ToolArgs.Usage("Missing <stompUrl>. It is REQUIRED and has NO default.", UsageText());
if (positional.Length > 2)
    return ToolArgs.Usage($"Too many arguments ({positional.Length}); expected exactly 2 "
                        + $"(<restUrl> <stompUrl>). Got: {string.Join(" ", positional)}", UsageText());

if (!ToolArgs.TryUrl(positional[0], "restUrl", out string restUrl, out string problem, requireHttp: true))
    return ToolArgs.Usage(problem, UsageText());
if (!ToolArgs.TryUrl(positional[1], "stompUrl", out string stompUrl, out problem))
    return ToolArgs.Usage(problem, UsageText());

if (!yes && !dryRun)
    return ToolArgs.Usage("Refusing to act without explicit confirmation. Add --yes to drive "
                        + $"{restUrl} to UNINITIALIZED, or --dry-run to see what would happen. "
                        + "Nothing was sent.", UsageText());

// -- past this point the arguments are fully specified and intent is explicit --------

var settings = new C2SIMSDKSettings
{
    SubmitterId = "STOPIFACE",
    RestUrl = restUrl,
    RestPassword = "v0lgenau",
    StompUrl = stompUrl,
    Protocol = "SISO-STD-C2SIM",
    ProtocolVersion = "CWIX2024v1.0.2",
};

using var sdk = new C2SIMSDK(NullLoggerFactory.Instance, settings);

try
{
    C2SIMSDK.C2SIMServerStatus before = await sdk.GetStatus();
    Console.WriteLine($"target      : {restUrl}");
    Console.WriteLine($"before      : {before}");

    if (dryRun)
    {
        // Read-only path. GetStatus above is a query; no command is issued here.
        Console.WriteLine("DRY RUN - nothing will be sent.");
        Console.WriteLine("WOULD SEND  : STOP, then RESET (driving the server to UNINITIALIZED).");
        Console.WriteLine($"WOULD AFFECT: any interface attached to {restUrl} - it would resign from the RTI.");
        Console.WriteLine("Re-run with --yes instead of --dry-run to actually do this.");
        return ToolArgs.ExitOk;
    }

    Console.WriteLine("STOP ...");
    await sdk.PushCommand(C2SIMSDK.C2SIMCommands.STOP);
    Console.WriteLine($"after STOP  : {await sdk.GetStatus()}");

    Console.WriteLine("RESET ...");
    await sdk.PushCommand(C2SIMSDK.C2SIMCommands.RESET);

    // VERIFY. This used to "return 0" unconditionally, so a teardown that silently failed
    // reported success and an unattended runner would proceed against a still-RUNNING
    // server with a still-joined federate. The exit code must reflect the SERVER's state,
    // not merely the fact that two messages were transmitted.
    C2SIMSDK.C2SIMServerStatus after = await sdk.GetStatus();
    Console.WriteLine($"after RESET : {after}");

    if (after != C2SIMSDK.C2SIMServerStatus.UNINITIALIZED)
    {
        Console.Error.WriteLine($"[FAIL] server is {after}, expected UNINITIALIZED. The teardown did "
                              + "NOT complete. An interface may still be joined to the RTI - check "
                              + "before starting anything that needs a fresh federate.");
        return ToolArgs.ExitFailure;
    }

    Console.WriteLine("Server driven to UNINITIALIZED; any running interface should resign cleanly now.");
    return ToolArgs.ExitOk;
}
catch (Exception ex)
{
    // A raw stack trace out of a top-level statement is not a contract an unattended
    // runner can act on. Report the failure, and be explicit that the server state is now
    // UNKNOWN rather than implying nothing happened.
    Console.Error.WriteLine($"[FAIL] {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine($"[FAIL] server state at {restUrl} is now UNKNOWN - it may be partially "
                          + "torn down. Query it before relying on it.");
    return ToolArgs.ExitFailure;
}
