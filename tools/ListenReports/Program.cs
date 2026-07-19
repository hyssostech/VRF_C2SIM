using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;
using VrfC2Sim.Tools;

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
// Argument handling uses the shared tools/Shared/ToolArgs.cs standard (exit 0 success /
// 1 operational failure / 2 usage error with nothing done; usage text to STDERR).

string[] UsageText() => new[]
{
    "usage: ListenReports.exe [seconds] [outPath]",
    "",
    "  seconds   Optional. Whole number > 0. Default 120.",
    "  outPath   Optional. File OR directory for the capture.",
    "            Default: reports-captured.log beside this binary.",
    "            Parent directories are created if missing.",
    "",
    "examples:  ListenReports.exe",
    "           ListenReports.exe 300",
    "           ListenReports.exe 300 C:\\runs\\2026-07-19T1200Z\\reports.log",
};

// This tool accepts NO options, so any "--token" is a mistake - reject it rather than
// treat it as a positional (ToolArgs.UnknownFlags documents why that matters).
string[] unknown = ToolArgs.UnknownFlags(args);
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}.", UsageText());

string[] positional = ToolArgs.Positionals(args);

int secs = 120;
if (positional.Length > 0 &&
    !ToolArgs.TryPositiveInt(positional[0], "seconds", out secs, out string problem))
    return ToolArgs.Usage(problem, UsageText());

// Resolve the output path BEFORE connecting, so a bad path fails fast instead of after a
// full capture window has been spent.
string outPath;
if (positional.Length > 1 && !string.IsNullOrWhiteSpace(positional[1]))
{
    string requested = positional[1];
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
        return ToolArgs.Usage($"outPath '{requested}' is not a usable path: "
                            + $"{ex.GetType().Name}: {ex.Message}", UsageText());
    }

    string parent = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(parent))
    {
        try { Directory.CreateDirectory(parent); }
        catch (Exception ex)
        {
            return ToolArgs.Usage($"could not create output directory '{parent}': "
                                + $"{ex.GetType().Name}: {ex.Message}", UsageText());
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
return ToolArgs.ExitOk;
