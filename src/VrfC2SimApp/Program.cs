using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VrfC2SimApp;

// VRF_C2SIM .NET app entry point. Hosts the C2SIM SDK (the C2SIM half) and the
// VrfBridge (the VR-Forces half) inside one BackgroundService. Configuration comes
// from appsettings.json (auto-loaded by the host) + command line + env.
//
// RUNTIME: because VrfBridge is a native x64 (/clr:netcore) assembly over the MAK
// libraries, the MAK bin dirs MUST be on PATH before this runs, e.g.:
//   C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin
// and VR-Forces (HLA CWIX-2024) + the C2SIM server must be up (see docs/RUNBOOK.md).

// Offline parity self-test of the C2SIM->VRF unit translation (no host, no VR-Forces).
if (args.Length > 0 && args[0] == "--translator-selftest")
    return TranslatorSelfTest.Run();

// Offline init-parse check: parse a C2SIM init file and print a summary (no bridge).
// Optional 3rd arg is the clientId (SystemName) whose units WOULD be created; default STP.
if (args.Length >= 2 && args[0] == "--parse-init")
    return InitParseCheck.Run(args[1], args.Length >= 3 ? args[2] : "STP");

// Offline order-parse check: parse a C2SIM order file and print a summary (no bridge).
if (args.Length >= 2 && args[0] == "--parse-order")
    return OrderParseCheck.Run(args[1]);

// Offline report-builder check: build + round-trip a task-status + position report (no bridge).
if (args.Length > 0 && args[0] == "--report-selftest")
    return ReportSelfTest.Run();

// Offline task-sequencer check: predecessor gating / delay / timeout (no bridge).
if (args.Length > 0 && args[0] == "--sequencer-selftest")
    return SequencerSelfTest.Run();

// Offline verb-mapping check: C2SIM TaskActionCode -> TaskIntent classification (no bridge).
if (args.Length > 0 && args[0] == "--verb-selftest")
    return VerbMappingSelfTest.Run();

// Offline de-stack check: R8 create-time de-stacking grouping + ring geometry (no bridge).
if (args.Length > 0 && args[0] == "--destack-selftest")
    return DeStackSelfTest.Run();

// Offline fan-out check: R10 member-completion aggregation (no bridge).
if (args.Length > 0 && args[0] == "--fanout-selftest")
    return FanOutSelfTest.Run();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<VrfC2SimService>();
await builder.Build().RunAsync();
return 0;
