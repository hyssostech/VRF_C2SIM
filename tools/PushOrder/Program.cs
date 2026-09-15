using C2SIM;
using Microsoft.Extensions.Logging;
using VrfC2Sim.Tools;

// Push a C2SIM Order and record everything the server echoes back on STOMP.
//   PushOrder <order.xml> [seconds-to-listen] [restUrl] [stompUrl] [--verbose]
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
// --verbose is a flag, not a positional, so it can go anywhere in argv (same as PushInit).
//
// RAW SERVER REPLY (2026-09-15, run 20260915T151959Z): sdk.PushOrderMessage() decodes the
// REST reply as C2SIM XML before this tool ever sees it (C2SIMSDK.PushMessage ->
// ToC2SIMObject). When the server's reply is not XML at all, XmlSerializer.Deserialize
// throws InvalidOperationException ("There is an error in XML document (1, 1)") wrapping
// an inner XmlException ("Data at the root level is invalid..."), and PushOrder built the
// SDK with NullLoggerFactory.Instance - so the SDK's own trace-level "Result = <raw reply>"
// line (C2SIMSSDK.cs PushMessage), the only place the actual server bytes are visible, was
// silently discarded and the tool crashed with an unhandled exception (exit -532462766) and
// NO diagnostic content at all. PushOrder now always captures the SDK's "Result = ..."
// trace line plus any Error/Warning line into a small ring buffer (never printed unless
// something goes wrong), and --verbose (same switch, same meaning as PushInit) echoes every
// SDK log line live, prefixed "sdk: ".
//
// EXIT CODES: 0 success, 1 operational failure (the server rejected the order), 2 usage
// error, 4 the server's reply could not be parsed as XML at all (distinct from 1: there is
// no C2SIMServerResponse.Status/Message to report because the reply was never in the
// expected shape). The captured bus log is written in all non-usage cases, including a
// rejected push, because the rejection is exactly when the trace is most worth having.

string[] UsageText() => new[]
{
    "usage: PushOrder.exe <order.xml> [seconds-to-listen] [restUrl] [stompUrl] [--verbose]",
    "",
    "  order.xml          REQUIRED. Path to the C2SIM Order XML to push.",
    "  seconds-to-listen  Optional. Default 30. Whole seconds, 0..86400.",
    "  restUrl            Optional. Default 'http://127.0.0.1:8080/C2SIMServer'.",
    "  stompUrl           Optional. Default 'http://127.0.0.1:61613/topic/C2SIM'.",
    "  --verbose          Echo the SDK's own trace-level raw server responses.",
    "",
    "example:  PushOrder.exe data\\order.xml",
    "          PushOrder.exe data\\order.xml 60 http://10.0.0.5:8080/C2SIMServer http://10.0.0.5:61613/topic/C2SIM --verbose",
};

// PushOrder has NO --dry-run mode - it always performs a real push.
string[] unknown = ToolArgs.UnknownFlags(args, "--verbose");
if (unknown.Length > 0)
    return ToolArgs.Usage($"unknown option(s): {string.Join(" ", unknown)}. PushOrder takes "
                        + "positional arguments only (plus --verbose) and has NO --dry-run mode.", UsageText());

bool verbose = ToolArgs.HasFlag(args, "--verbose");
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

var sdkLog = new RingBuffer(20);
ILoggerFactory loggerFactory = new CapturingLoggerFactory(verbose, sdkLog);
using var sdk = new C2SIMSDK(loggerFactory, settings);

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

// The server is expected to answer with a C2SIMServerResponse-shaped XML document, but a
// misbehaving server (or a wrong endpoint) can answer with something else entirely - HTML,
// plain text, an empty body. XmlSerializer.Deserialize surfaces that as InvalidOperationException
// wrapping an inner XmlException, and there is no C2SIMServerResponse to report in that case,
// so this is handled separately from a normal rejection (resp.IsSuccess == false, below).
static string Truncate(string s, int max) => s == null || s.Length <= max ? s : s.Substring(0, max) + " ...[truncated]";

C2SIMServerResponse resp;
try
{
    resp = await sdk.PushOrderMessage(xml);
}
catch (InvalidOperationException e)
{
    // C2SIMSDK.PushMessage itself catches InvalidOperationException and rethrows a NEW one
    // with the SAME message wrapping the original (C2SIMSSDK.cs PushMessage: "throw new
    // InvalidOperationException(msg, e)"), so e.InnerException is typically another
    // InvalidOperationException carrying the identical top-level message - not yet the
    // useful part. The actual cause (here, the XmlException naming the bad line/position)
    // is further down, so walk to the bottom of the chain rather than stopping one level in.
    Exception innermost = e;
    while (innermost.InnerException != null) innermost = innermost.InnerException;
    string detail = ReferenceEquals(innermost, e)
        ? e.Message
        : $"{e.Message} ({innermost.GetType().Name}: {innermost.Message})";
    Console.Error.WriteLine($"[FAIL] the server's reply was not the expected XML: {detail}");
    Console.Error.WriteLine();
    Console.Error.WriteLine($"captured SDK log (last {sdkLog.Count} of up to {sdkLog.Capacity} lines):");
    foreach (string line in sdkLog.Lines)
        Console.Error.WriteLine(Truncate(line, 2000));
    return 4;
}
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

// Minimal ILoggerFactory/ILogger pair that (a) ALWAYS captures the SDK's own "Result = ..."
// trace line (C2SIMSSDK.cs PushMessage) plus any Error/Warning line into a small ring
// buffer - so a non-XML server reply is never silently lost even when nothing is printed
// by default - and (b) when --verbose is given, echoes every SDK log line (Trace and up)
// to stdout as it arrives, prefixed "sdk: ". Same switch, same intent, as PushInit's
// ConsoleLoggerFactory; this one additionally captures.
sealed class CapturingLoggerFactory(bool verbose, RingBuffer ring) : ILoggerFactory
{
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, verbose, ring);
    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }
}

sealed class CapturingLogger(string category, bool verbose, RingBuffer ring) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        string line = $"[{logLevel}] {category}: {message}";
        if (verbose)
            Console.WriteLine("sdk: " + line);
        // Captured regardless of --verbose: the raw-reply trace line ("Result = ...") is the
        // only place the server's actual bytes appear, and Warning/Error lines are the SDK's
        // own diagnosis (e.g. "Failed to deserialize xml to type ..." from ToC2SIMObject).
        if (logLevel >= LogLevel.Warning || message.StartsWith("Result = ", StringComparison.Ordinal))
            ring.Add(line);
    }
}

// Fixed-capacity FIFO of the last N captured SDK log lines.
sealed class RingBuffer(int capacity)
{
    private readonly Queue<string> _lines = new();
    public int Capacity { get; } = capacity;
    public int Count => _lines.Count;
    public IReadOnlyList<string> Lines => _lines.ToArray();
    public void Add(string line)
    {
        _lines.Enqueue(line);
        while (_lines.Count > Capacity) _lines.Dequeue();
    }
}
