using C2SIM;
using Microsoft.Extensions.Logging.Abstractions;

// Cleanly stop a running VRF_C2SIM interface (C++ or .NET) by driving the C2SIM server
// to UNINITIALIZED (STOP then RESET - NOT INITIALIZE), which the interface detects and
// uses to resign from the RTI without leaving a stale federate (RUNBOOK sec 4).
//   StopIface [restUrl] [stompUrl]
string restUrl = args.Length > 0 ? args[0] : "http://127.0.0.1:8080/C2SIMServer";
string stompUrl = args.Length > 1 ? args[1] : "http://127.0.0.1:61613/topic/C2SIM";

var settings = new C2SIMSDKSettings
{
    SubmitterId = "STOPIFACE",
    RestUrl = restUrl,
    RestPassword = "v0lgenau",
    StompUrl = stompUrl,
    Protocol = "SISO-STD-C2SIM",
    ProtocolVersion = "CWIX2024v1.0.2",
};

using var sdk = new C2SIMSDK(NullLoggerFactory.Instance, settings);
Console.WriteLine($"before      : {await sdk.GetStatus()}");
Console.WriteLine("STOP ...");
await sdk.PushCommand(C2SIMSDK.C2SIMCommands.STOP);
Console.WriteLine($"after STOP  : {await sdk.GetStatus()}");
Console.WriteLine("RESET ...");
await sdk.PushCommand(C2SIMSDK.C2SIMCommands.RESET);
Console.WriteLine($"after RESET : {await sdk.GetStatus()}");
Console.WriteLine("Server driven to UNINITIALIZED; any running interface should resign cleanly now.");
return 0;
