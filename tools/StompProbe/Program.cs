using System.Globalization;
using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;

// Diagnostic: subscribe via the SDK and log EVERY inbound event, to root-cause whether the
// .NET SDK STOMP path receives server broadcasts (init/order/report/status).
//   StompProbe [seconds] [protocolVersion] [restPassword]
// Defaults to the TOOL settings that ListenReports/PushInit use (known to work).
//
// NOTE: the local Usage() helper below duplicates a pattern now present in several tools
// (SetSimRate, ListenReports, WatchVrf). Consolidate into tools/Shared/ToolArgs.cs later.

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("usage: StompProbe.exe [seconds] [protocolVersion] [restPassword]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  seconds          Optional. Whole number > 0. Default 60.");
    Console.Error.WriteLine("  protocolVersion  Optional. Default 'CWIX2024v1.0.2'.");
    Console.Error.WriteLine("  restPassword     Optional. Default is the known-good tool password.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("examples:  StompProbe.exe");
    Console.Error.WriteLine("           StompProbe.exe 120");
    return 2;
}

int secs = 60;
if (args.Length > 0)
{
    if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out secs))
        return Usage($"seconds '{args[0]}' is not an integer.");
    if (secs <= 0)
        return Usage($"seconds must be greater than 0; got {secs}.");
}
string version = args.Length > 1 ? args[1] : "CWIX2024v1.0.2";
string password = args.Length > 2 ? args[2] : "v0lgenau";

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
return 0;
