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

// Offline fidelity-table check: data/unit-type-map.json rows vs the INSTALLED VR-Forces SMS chain
// (best-match resolution + transitive composition) plus the lookup key order (no bridge start).
if (args.Length > 0 && args[0] == "--typemap-selftest")
    return TypeMapSelfTest.Run();

// Offline terrain-vertex check: TerrainProfile-mode vertex authoring / fallback decision (no bridge start).
if (args.Length > 0 && args[0] == "--terrain-selftest")
    return TerrainSelfTest.Run();

// Offline placement check: C2SIM AltitudeAGL/AltitudeMSL/none x DIS domain -> create altitude + AGL set (no bridge).
if (args.Length > 0 && args[0] == "--placement-selftest")
    return PlacementSelfTest.Run();

// Offline compose-order check: declared <Subordinate> order -> attach order (leader = first) (no bridge).
if (args.Length > 0 && args[0] == "--compose-selftest")
    return ComposeOrderSelfTest.Run();

// CONTENT ROOT = the executable's folder (2026-09-07, found by --runtime-check): the generic host
// resolves appsettings*.json against the CURRENT DIRECTORY by default, so an exe started from any
// other folder silently ran without its settings (and without the Demo overlay). The deliverable
// must run from anywhere; a settings file that is NOT beside the exe is not ours.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
// MAK runtime bootstrap from the app's own settings (MakRuntime.cs): PATH + MAK_*DIR + the RTI
// posture, BEFORE any bridge type loads. Makes the executable + appsettings the whole deliverable;
// the start script and the test runner keep working because a variable already set wins.
var rt = MakRuntime.Bootstrap(builder.Configuration, AppContext.BaseDirectory);
foreach (var a in rt.Applied) Console.WriteLine("MakRuntime: " + a);
foreach (var w in rt.Warnings) Console.Error.WriteLine("MakRuntime WARNING: " + w);
// --runtime-check: deployment smoke test for the solutions engineers - apply the bootstrap, then
// touch the bridge so the MAK stack actually binds (a wrong PATH fails HERE, not at the join),
// print which stack bound, and exit without starting the host (nothing joins, nothing is created).
if (args.Length > 0 && args[0] == "--runtime-check")
    return RuntimeCheck.Run(rt);
builder.Services.AddHostedService<VrfC2SimService>();
await builder.Build().RunAsync();
return 0;
