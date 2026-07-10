using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;

// Reset the C2SIM server and share an initialization, then leave it RUNNING.
//   PushInit <init.xml> [restUrl] [stompUrl]
string initPath = args.Length > 0 ? args[0] : throw new ArgumentException("need init xml path");
string restUrl = args.Length > 1 ? args[1] : "http://127.0.0.1:8080/C2SIMServer";
string stompUrl = args.Length > 2 ? args[2] : "http://127.0.0.1:61613/topic/C2SIM";

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
