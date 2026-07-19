using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;
using VrfC2Sim.Tools;

// Push a C2SIM Order and record everything the server echoes back on STOMP.
//   PushOrder <order.xml> [seconds-to-listen] [restUrl] [stompUrl]
//
// ARGUMENT HANDLING (2026-07-19): args[0] was a bare unchecked index, so a no-argument
// run died with an IndexOutOfRangeException carrying no message at all; and the listen
// duration used int.Parse, whose FormatException does not name the offending argument.
// Both are now usage errors (exit 2) with a named cause.
//
// ENDPOINT ARGUMENTS: restUrl/stompUrl were HARDCODED to localhost with no override, so
// an unattended runner could not point this tool at a non-localhost server at all. They
// are now optional positionals with the same defaults and the same names PushInit uses.
// NOTE the ordering difference from PushInit: [seconds-to-listen] stays at position 1
// because moving it would silently change the meaning of every existing invocation of
// this tool. Same argument NAMES, deliberately different ORDER - check the usage text.
//
// EXIT CODES: 0 success, 1 operational failure (the server rejected the order), 2 usage
// error. The captured bus log is written in all non-usage cases, including a rejected
// push, because the rejection is exactly when the trace is most worth having.

string[] UsageText() => new[]
{
    "usage: PushOrder.exe <order.xml> [seconds-to-listen] [restUrl] [stompUrl]",
    "",
    "  order.xml          REQUIRED. Path to the C2SIM Order XML to push.",
    "  seconds-to-listen  Optional. Default 30. Whole seconds, 0..86400.",
    "  restUrl            Optional. Default 'http://127.0.0.1:8080/C2SIMServer'.",
    "  stompUrl           Optional. Default 'http://127.0.0.1:61613/topic/C2SIM'.",
    "",
    "example:  PushOrder.exe data\\order.xml",
    "          PushOrder.exe data\\order.xml 60 http://10.0.0.5:8080/C2SIMServer http://10.0.0.5:61613/topic/C2SIM",
};

// PushOrder has NO --dry-run mode - it always performs a real push.
string[] unknown = ToolArgs.UnknownFlags(args);
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}. PushOrder takes "
                        + "positional arguments only and has NO --dry-run mode.", UsageText());

string[] positional = ToolArgs.Positionals(args);

if (positional.Length == 0)
    return ToolArgs.Usage("Missing <order.xml>. It is REQUIRED and has no default.", UsageText());
if (positional.Length > 4)
    return ToolArgs.Usage($"Too many arguments ({positional.Length}); expected at most 4 "
                        + "(<order.xml> [seconds-to-listen] [restUrl] [stompUrl]). "
                        + $"Got: {string.Join(" ", positional)}", UsageText());

string orderPath = positional[0];
string problem;
int listenSecs = 30;
string restUrl = "http://127.0.0.1:8080/C2SIMServer";
string stompUrl = "http://127.0.0.1:61613/topic/C2SIM";

if (positional.Length > 1
    && !ToolArgs.TryIntInRange(positional[1], "seconds-to-listen", 0, 86400, out listenSecs, out problem))
    return ToolArgs.Usage(problem, UsageText());
if (positional.Length > 2 && !ToolArgs.TryUrl(positional[2], "restUrl", out restUrl, out problem, requireHttp: true))
    return ToolArgs.Usage(problem, UsageText());
if (positional.Length > 3 && !ToolArgs.TryUrl(positional[3], "stompUrl", out stompUrl, out problem))
    return ToolArgs.Usage(problem, UsageText());

// Check the file BEFORE connecting, so a typo costs nothing and touches no server.
if (!File.Exists(orderPath))
    return ToolArgs.Usage($"order xml '{orderPath}' does not exist (resolved from "
                        + $"'{Directory.GetCurrentDirectory()}'). Nothing was pushed.", UsageText());

var settings = new C2SIMSDKSettings
{
    SubmitterId = "GOLDENTRACE",
    RestUrl = restUrl,
    RestPassword = "v0lgenau",
    StompUrl = stompUrl,
    Protocol = "SISO-STD-C2SIM",
    ProtocolVersion = "CWIX2024v1.0.2",
};

using var sdk = new C2SIMSDK(NullLoggerFactory.Instance, settings);

var log = new List<string>();
void Stamp(string kind, string body)
{
    string line = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {kind} ({body?.Length ?? 0} chars)";
    Console.WriteLine(line);
    log.Add(line + "\n" + body);
}

sdk.OrderReceived += (_, e) => Stamp("ORDER", e.Body);
sdk.ReportReceived += (_, e) => Stamp("REPORT", e.Body);
sdk.StatusChangedReceived += (_, e) => Stamp("STATUS", e.Body);
sdk.ObjectInitializationReceived += (_, e) => Stamp("OBJECTINIT", e.Body);
sdk.Error += (_, e) => Console.WriteLine($"!! Error: {e.Message}");

await sdk.Connect();
Console.WriteLine($"subscribed; server status = {await sdk.GetStatus()}");

string xml = await File.ReadAllTextAsync(orderPath);
Console.WriteLine($"pushing order: {orderPath} ({xml.Length} chars)");
C2SIMServerResponse resp = await sdk.PushOrderMessage(xml);
Console.WriteLine($"push result  : {resp.Status} {resp.Message}");

Console.WriteLine($"listening {listenSecs}s for reports ...");
await Task.Delay(TimeSpan.FromSeconds(listenSecs));
await sdk.Disconnect();

// Dead assignment removed: outPath was computed from the order's directory and then
// immediately overwritten by the BaseDirectory path on the very next line, so the first
// computation never had any effect. The BaseDirectory location is the one that shipped.
string outPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "c2sim-bus.log"));
await File.WriteAllTextAsync(outPath, string.Join("\n\n", log));
Console.WriteLine($"captured {log.Count} bus messages -> {outPath}");

// The push result decides the exit code. This used to fall off the end with an implicit
// 0, so an unattended runner treated a REJECTED order as a successful one and went on to
// wait for movement that was never going to happen. The bus log is written first (above)
// so the trace survives either way.
if (!resp.IsSuccess)
{
    Console.Error.WriteLine($"[FAIL] server rejected the order: {resp.Status} {resp.Message}");
    return ToolArgs.ExitFailure;
}
return ToolArgs.ExitOk;
