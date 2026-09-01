using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;
using VrfC2Sim.Tools;

// Passively record every REPORT the interface posts, so the wire-format report XML is captured.
//   ListenReports [seconds] [outPath] [--stop-file <path>]
//   ListenReports --capabilities
//
// seconds   Optional, default 120. Whole number > 0. With --stop-file this is the UPPER
//           BOUND; the listen normally ends earlier.
// outPath   Optional. Where to write the capture. If omitted the tool writes
//           reports-captured.log beside its own binary (AppContext.BaseDirectory),
//           which is the historical behavior and is PRESERVED exactly. If given, it
//           may be a file path or a directory (trailing separator, or an existing
//           directory), in which case reports-captured.log is written inside it.
//           Missing parent directories are created.
// --stop-file <path>
//           Optional (2026-09-01, runner turnaround). End the listen EARLY - disconnect
//           and write the capture - as soon as <path> exists (polled once a second).
//           The file must NOT exist at start (exit 2, nothing connected). The capture is
//           written ONLY at exit, so without this the runner had to wait out the whole
//           worst-case duration before reports-captured.log appeared.
// --capabilities
//           Offline. Prints one capability token per line and exits 0. The runner probes
//           this before passing --stop-file, so a deployed binary that predates the flag
//           is detected (falls back to duration-only) instead of killed with exit 2.
//
// Argument handling uses the shared tools/Shared/ToolArgs.cs standard (exit 0 success /
// 1 operational failure / 2 usage error with nothing done; usage text to STDERR).

const string StopFileFlag = "--stop-file";
string[] capabilities = { "capabilities", "stop-file" };

string[] UsageText() => new[]
{
    "usage: ListenReports.exe [seconds] [outPath] [--stop-file <path>]",
    "       ListenReports.exe --capabilities",
    "",
    "  seconds   Optional. Whole number > 0. Default 120. With --stop-file this is",
    "            the UPPER BOUND; the listen normally ends earlier.",
    "  outPath   Optional. File OR directory for the capture.",
    "            Default: reports-captured.log beside this binary.",
    "            Parent directories are created if missing.",
    "  --stop-file <path>",
    "            Optional. Disconnect and write the capture as soon as <path> EXISTS",
    "            (polled about once a second). Must NOT exist at start (exit 2).",
    "  --capabilities",
    "            Offline. Prints one capability token per line (currently:",
    "            capabilities, stop-file) and exits 0. Sole argument.",
    "",
    "examples:  ListenReports.exe",
    "           ListenReports.exe 300",
    "           ListenReports.exe 300 C:\\runs\\2026-07-19T1200Z\\reports.log",
    "           ListenReports.exe 1460 C:\\runs\\x\\ --stop-file C:\\runs\\x\\observers.stop",
    "           ListenReports.exe --capabilities",
};

if (args.Length > 0 && args[0] == "--capabilities")
{
    // Sole-argument rule: this path listens to nothing, so a companion argument is a
    // request the tool would silently drop.
    if (args.Length > 1)
        return ToolArgs.Usage(
            $"--capabilities takes no other arguments; got: {string.Join(" ", args[1..])}.", UsageText());
    foreach (string cap in capabilities) Console.Out.WriteLine(cap);
    return ToolArgs.ExitOk;
}

// --stop-file is the ONE value-taking option; extract it FIRST so Positionals() does not
// see its path as a stray positional (ToolArgs.TryTakeOptionValue documents why).
string stopFile = null;
string problem;
if (!ToolArgs.TryTakeOptionValue(args, StopFileFlag, out args, out stopFile, out problem))
    return ToolArgs.Usage(problem, UsageText());
if (stopFile != null)
{
    try { stopFile = Path.GetFullPath(stopFile); }
    catch (Exception ex)
    {
        return ToolArgs.Usage($"{StopFileFlag} '{stopFile}' is not a usable path: "
                            + $"{ex.GetType().Name}: {ex.Message}", UsageText());
    }
    // A pre-existing stop file would end the listen on the first poll and write an empty
    // capture that still exits 0. Refuse BEFORE connecting.
    if (File.Exists(stopFile))
        return ToolArgs.Usage($"{StopFileFlag} '{stopFile}' ALREADY EXISTS; the listen would end "
                            + "immediately. Remove it or pass a fresh path. Nothing connected.", UsageText());
}

// Any OTHER "--token" is a mistake - reject it rather than treat it as a positional
// (ToolArgs.UnknownFlags documents why that matters).
string[] unknown = ToolArgs.UnknownFlags(args);
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}.", UsageText());

string[] positional = ToolArgs.Positionals(args);

int secs = 120;
if (positional.Length > 0 &&
    !ToolArgs.TryPositiveInt(positional[0], "seconds", out secs, out problem))
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
Console.WriteLine($"listening for reports, {secs}s ..."
                + (stopFile != null ? $" (upper bound; stop-file={stopFile})" : ""));
var listenStart = DateTime.UtcNow;
var listenEnd = listenStart.AddSeconds(secs);
bool stoppedOnRequest = false;
while (DateTime.UtcNow < listenEnd)
{
    // 1 s poll granularity: the last wait is clamped so the duration cap is still honored
    // to within the poll, and the stop-file (when given) is seen within ~1 s of its touch.
    var remaining = listenEnd - DateTime.UtcNow;
    await Task.Delay(remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1));
    if (stopFile != null && File.Exists(stopFile))
    {
        stoppedOnRequest = true;
        break;
    }
}
if (stoppedOnRequest)
    Console.WriteLine($"stop requested via stop-file at t={Math.Round((DateTime.UtcNow - listenStart).TotalSeconds, 1)}s "
                    + $"(duration cap was {secs}s) - disconnecting");
await sdk.Disconnect();

await File.WriteAllTextAsync(outPath, string.Join("\n\n", captured));
Console.WriteLine($"captured {reports} reports -> {outPath}");
if (firstReport != null)
{
    Console.WriteLine("=== first report body ===");
    Console.WriteLine(firstReport.Length > 1600 ? firstReport[..1600] : firstReport);
}
return ToolArgs.ExitOk;
