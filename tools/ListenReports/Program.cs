using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;

// Passively record every REPORT the interface posts, so the wire-format report XML is captured.
//   ListenReports [seconds]
int secs = args.Length > 0 ? int.Parse(args[0]) : 120;

var settings = new C2SIMSDKSettings
{
    SubmitterId = "REPORTLISTENER",
    RestUrl = "http://127.0.0.1:8080/C2SIMServer",
    RestPassword = "v0lgenau",
    StompUrl = "http://127.0.0.1:61613/topic/C2SIM",
    Protocol = "SISO-STD-C2SIM",
    ProtocolVersion = "CWIX2024v1.0.2",
};

using var sdk = new C2SIMSDK(NullLoggerFactory.Instance, settings);

int reports = 0;
var captured = new List<string>();
string firstReport = null;

sdk.ReportReceived += (_, e) =>
{
    int n = Interlocked.Increment(ref reports);
    firstReport ??= e.Body;
    captured.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] REPORT #{n} ({e.Body?.Length ?? 0} chars)\n{e.Body}");
    // Pull out the report content type + any position for a live one-liner
    string kind = e.Body?.Contains("PositionReportContent") == true ? "Position"
                : e.Body?.Contains("ObservationReportContent") == true ? "Observation"
                : e.Body?.Contains("TaskStatus") == true ? "TaskStatus" : "other";
    Console.WriteLine($"  REPORT #{n}: {kind}");
};
sdk.Error += (_, e) => Console.WriteLine($"  !! {e.Message}");

await sdk.Connect();
Console.WriteLine($"listening for reports, {secs}s ...");
await Task.Delay(TimeSpan.FromSeconds(secs));
await sdk.Disconnect();

string outPath = Path.Combine(AppContext.BaseDirectory, "reports-captured.log");
await File.WriteAllTextAsync(outPath, string.Join("\n\n", captured));
Console.WriteLine($"captured {reports} reports -> {outPath}");
if (firstReport != null)
{
    Console.WriteLine("=== first report body ===");
    Console.WriteLine(firstReport.Length > 1600 ? firstReport[..1600] : firstReport);
}
