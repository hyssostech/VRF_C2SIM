using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;

// Push a C2SIM Order and record everything the server echoes back on STOMP.
//   PushOrder <order.xml> [seconds-to-listen]
string orderPath = args[0];
int listenSecs = args.Length > 1 ? int.Parse(args[1]) : 30;

var settings = new C2SIMSDKSettings
{
    SubmitterId = "GOLDENTRACE",
    RestUrl = "http://127.0.0.1:8080/C2SIMServer",
    RestPassword = "v0lgenau",
    StompUrl = "http://127.0.0.1:61613/topic/C2SIM",
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

string outPath = Path.Combine(Path.GetDirectoryName(orderPath) ?? ".", "..", "c2sim-bus.log");
outPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "c2sim-bus.log"));
await File.WriteAllTextAsync(outPath, string.Join("\n\n", log));
Console.WriteLine($"captured {log.Count} bus messages -> {outPath}");
