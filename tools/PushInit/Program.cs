using C2SIM;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VrfC2Sim.Tools;

// Reset the C2SIM server and share an initialization, then leave it RUNNING.
//   PushInit <init.xml> [restUrl] [stompUrl] [--verbose]
//
// ARGUMENT HANDLING (2026-07-19): the missing-path case used to be a throw-expression in
// a ternary, which surfaced in an unattended runner as an unhandled ArgumentException and
// a stack trace on stderr with exit code 134 - indistinguishable from a crash. It is now
// a usage error (exit 2). Likewise the init path is checked for existence BEFORE the read,
// which previously threw a raw FileNotFoundException.
//
// Unlike StopIface the endpoints keep their localhost defaults: this tool's action is
// gated on a mandatory <init.xml> argument, so there is no destructive no-argument
// invocation to defend against.
//
// EXIT CODES: 0 success, 1 operational failure (push rejected), 2 usage error.

string[] UsageText() => new[]
{
    "usage: PushInit.exe <init.xml> [restUrl] [stompUrl] [--verbose]",
    "",
    "  init.xml   REQUIRED. Path to the C2SIM initialization XML to share.",
    "  restUrl    Optional. Default 'http://127.0.0.1:8080/C2SIMServer'.",
    "  stompUrl   Optional. Default 'http://127.0.0.1:61613/topic/C2SIM'.",
    "  --verbose  Echo the SDK's own trace-level raw server responses.",
    "",
    "example:  PushInit.exe data\\init.xml",
    "          PushInit.exe data\\init.xml http://10.0.0.5:8080/C2SIMServer http://10.0.0.5:61613/topic/C2SIM",
};

// Reject unknown options rather than letting them fall through into the positionals.
// The old filter was an exact-match drop of "--verbose" only, so "--dry-run" (which
// tools/ResetVrf and StopIface accept) would have been taken as the init.xml PATH and
// then failed with a confusing "file not found" - or, worse, shifted every later
// argument by one.
string[] unknown = ToolArgs.UnknownFlags(args, "--verbose");
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}. PushInit has NO "
                        + "--dry-run mode - it always performs a real reset and push.", UsageText());

bool verbose = ToolArgs.HasFlag(args, "--verbose");
string[] positional = ToolArgs.Positionals(args);

if (positional.Length == 0)
    return ToolArgs.Usage("Missing <init.xml>. It is REQUIRED and has no default.", UsageText());
if (positional.Length > 3)
    return ToolArgs.Usage($"Too many arguments ({positional.Length}); expected at most 3 "
                        + $"(<init.xml> [restUrl] [stompUrl]). Got: {string.Join(" ", positional)}",
                          UsageText());

string initPath = positional[0];
string problem;
string restUrl = "http://127.0.0.1:8080/C2SIMServer";
string stompUrl = "http://127.0.0.1:61613/topic/C2SIM";
if (positional.Length > 1 && !ToolArgs.TryUrl(positional[1], "restUrl", out restUrl, out problem, requireHttp: true))
    return ToolArgs.Usage(problem, UsageText());
if (positional.Length > 2 && !ToolArgs.TryUrl(positional[2], "stompUrl", out stompUrl, out problem))
    return ToolArgs.Usage(problem, UsageText());

// Check the file BEFORE touching the server. Discovering a bad path only after
// ResetToInitializing() has already run would leave the server in INITIALIZING with no
// initialization shared - a worse state than the one we started from.
if (!File.Exists(initPath))
    return ToolArgs.Usage($"init xml '{initPath}' does not exist (resolved from "
                        + $"'{Directory.GetCurrentDirectory()}'). The server was NOT touched.",
                          UsageText());

var settings = new C2SIMSDKSettings
{
    SubmitterId = "GOLDENTRACE",
    RestUrl = restUrl,
    RestPassword = "v0lgenau",
    StompUrl = stompUrl,
    Protocol = "SISO-STD-C2SIM",
    ProtocolVersion = "CWIX2024v1.0.2",
};

ILoggerFactory loggerFactory = verbose ? new ConsoleLoggerFactory() : NullLoggerFactory.Instance;
using var sdk = new C2SIMSDK(loggerFactory, settings);

Console.WriteLine($"before      : {await sdk.GetStatus()}");

Console.WriteLine("ResetToInitializing() ...");
await sdk.ResetToInitializing();
Console.WriteLine($"after reset : {await sdk.GetStatus()}");

string xml = await File.ReadAllTextAsync(initPath);
Console.WriteLine($"pushing     : {initPath} ({xml.Length} chars)");
C2SIMServerResponse resp = await sdk.PushInitializationMessage(xml);
Console.WriteLine($"push result : {resp.Status} {resp.Message}");
if (!resp.IsSuccess)
{
    return 1;
}

Console.WriteLine("SwitchToRunning() ...");
await sdk.SwitchToRunning();
Console.WriteLine($"after start : {await sdk.GetStatus()}");

// Confirm what the server will hand a late joiner
string shared = await sdk.JoinSession();
int units = System.Text.RegularExpressions.Regex.Matches(shared ?? "", "<Unit>").Count;
var sysNames = System.Text.RegularExpressions.Regex.Matches(shared ?? "", "<SystemName>([^<]*)</SystemName>")
    .Select(m => m.Groups[1].Value).Distinct();
Console.WriteLine($"QUERYINIT   : {units} Units, SystemName=[{string.Join(",", sysNames)}]");
return 0;

// Minimal console logger so --verbose surfaces the SDK's own trace-level raw server
// response (normally discarded by NullLoggerFactory) without pulling in the
// Microsoft.Extensions.Logging.Console package.
sealed class ConsoleLoggerFactory : ILoggerFactory
{
    public ILogger CreateLogger(string categoryName) => new ConsoleLogger(categoryName);
    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }
}

sealed class ConsoleLogger(string category) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Console.WriteLine($"[{logLevel}] {category}: {formatter(state, exception)}");
}
