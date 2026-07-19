using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;
using VrfC2Sim.Tools;

// Diagnostic: subscribe via the SDK and log EVERY inbound event, to root-cause whether the
// .NET SDK STOMP path receives server broadcasts (init/order/report/status).
//   StompProbe [seconds] [protocolVersion] [restPassword]
// Defaults to the TOOL settings that ListenReports/PushInit use (known to work).
//
// Argument handling uses the shared tools/Shared/ToolArgs.cs standard (exit 0 success /
// 1 operational failure / 2 usage error with nothing done; usage text to STDERR).

string[] UsageText() => new[]
{
    "usage: StompProbe.exe [seconds] [protocolVersion] [restPassword]",
    "",
    "  seconds          Optional. Whole number > 0. Default 60.",
    "  protocolVersion  Optional. Default 'CWIX2024v1.0.2'.",
    "  restPassword     Optional. Default is the known-good tool password.",
    "",
    "examples:  StompProbe.exe",
    "           StompProbe.exe 120",
};

// This tool accepts NO options, so any "--token" is a mistake - reject it rather than
// treat it as a positional (ToolArgs.UnknownFlags documents why that matters).
string[] unknown = ToolArgs.UnknownFlags(args);
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}.", UsageText());

string[] positional = ToolArgs.Positionals(args);

int secs = 60;
if (positional.Length > 0 &&
    !ToolArgs.TryPositiveInt(positional[0], "seconds", out secs, out string problem))
    return ToolArgs.Usage(problem, UsageText());

string version = positional.Length > 1 ? positional[1] : "CWIX2024v1.0.2";
string password = positional.Length > 2 ? positional[2] : "v0lgenau";

var settings = new C2SIMSDKSettings
{
    SubmitterId = "STOMPPROBE",
    RestUrl = "http://127.0.0.1:8080/C2SIMServer",
    RestPassword = password,
    StompUrl = "http://127.0.0.1:61613/topic/C2SIM",
    Protocol = "SISO-STD-C2SIM",
    ProtocolVersion = version,
};
Console.WriteLine($"StompProbe: version='{version}' password='{(password.Length>0?"***":"(empty)")}' listening {secs}s");

using var sdk = new C2SIMSDK(NullLoggerFactory.Instance, settings);
int total = 0;
void Log(string kind, string body) =>
    Console.WriteLine($"  [{DateTime.UtcNow:HH:mm:ss.fff}] #{System.Threading.Interlocked.Increment(ref total)} {kind} ({body?.Length ?? 0} chars)");

sdk.C2SIMMessageReceived += (_, e) => Log("RAW", e.Body);
sdk.InitializationReceived += (_, e) => Log("Initialization", e.Body);
sdk.ObjectInitializationReceived += (_, e) => Log("ObjectInitialization", e.Body);
sdk.OrderReceived += (_, e) => Log("Order", e.Body);
sdk.ReportReceived += (_, e) => Log("Report", e.Body);
sdk.StatusChangedReceived += (_, e) => { Log("StatusChanged", e.Body); Console.WriteLine($"      BODY: {e.Body}"); };
sdk.Error += (_, e) => Console.WriteLine($"  !! ERROR: {e.Message}");

await sdk.Connect();
Console.WriteLine("connected + subscribed; waiting for broadcasts ...");
await Task.Delay(TimeSpan.FromSeconds(secs));
await sdk.Disconnect();
Console.WriteLine($"=== received {total} inbound messages ===");
return ToolArgs.ExitOk;
