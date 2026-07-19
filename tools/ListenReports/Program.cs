using System.Globalization;
using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;

// Passively record every REPORT the interface posts, so the wire-format report XML is captured.
//   ListenReports [seconds] [outPath]
//
// seconds   Optional, default 120. Whole number > 0.
// outPath   Optional. Where to write the capture. If omitted the tool writes
//           reports-captured.log beside its own binary (AppContext.BaseDirectory),
//           which is the historical behavior and is PRESERVED exactly. If given, it
//           may be a file path or a directory (trailing separator, or an existing
//           directory), in which case reports-captured.log is written inside it.
//           Missing parent directories are created.
//
// NOTE: the local Usage() helper below duplicates a pattern now present in several tools
// (SetSimRate, StompProbe, WatchVrf). Consolidate into tools/Shared/ToolArgs.cs later.

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("usage: ListenReports.exe [seconds] [outPath]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  seconds   Optional. Whole number > 0. Default 120.");
    Console.Error.WriteLine("  outPath   Optional. File OR directory for the capture.");
    Console.Error.WriteLine("            Default: reports-captured.log beside this binary.");
    Console.Error.WriteLine("            Parent directories are created if missing.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("examples:  ListenReports.exe");
    Console.Error.WriteLine("           ListenReports.exe 300");
    Console.Error.WriteLine("           ListenReports.exe 300 C:\\runs\\2026-07-19T1200Z\\reports.log");
    return 2;
}

int secs = 120;
if (args.Length > 0)
{
    if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out secs))
        return Usage($"seconds '{args[0]}' is not an integer.");
    if (secs <= 0)
        return Usage($"seconds must be greater than 0; got {secs}.");
}

// Resolve the output path BEFORE connecting, so a bad path fails fast instead of after a
// full capture window has been spent.
string outPath;
if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
{
    string requested = args[1];
    bool looksLikeDirectory =
        requested.EndsWith(Path.DirectorySeparatorChar) ||
        requested.EndsWith(Path.AltDirectorySeparatorChar) ||
        Directory.Exists(requested);
    try
    {
        outPath = looksLikeDirectory
            ? Path.GetFullPath(Path.Combine(requested, "reports-captured.log"))
            : Path.GetFullPath(requested);
    }
    catch (Exception ex)
    {
        return Usage($"outPath '{requested}' is not a usable path: {ex.GetType().Name}: {ex.Message}");
    }

    string parent = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(parent))
    {
        try { Directory.CreateDirectory(parent); }
        catch (Exception ex)
        {
            return Usage($"could not create output directory '{parent}': {ex.GetType().Name}: {ex.Message}");
        }
    }
}
else
{
    // UNCHANGED historical behavior: beside the binary.
    outPath = Path.Combine(AppContext.BaseDirectory, "reports-captured.log");
}

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

await File.WriteAllTextAsync(outPath, string.Join("\n\n", captured));
Console.WriteLine($"captured {reports} reports -> {outPath}");
if (firstReport != null)
{
    Console.WriteLine("=== first report body ===");
    Console.WriteLine(firstReport.Length > 1600 ? firstReport[..1600] : firstReport);
}
return 0;
