using VrfC2Sim.Tools;
using WatchVrf;

// tools/WatchVrf entry point. Two modes:
//
//   WatchVrf --con-selftest       Offline check of the CON,/TSK,/RPT, trace line
//                                 formatting (groundwork plan 0.6). Pure managed; does
//                                 NOT touch VrfBridge, so it runs without the native
//                                 bridge DLL / MAK bin dirs on PATH.
//
//   WatchVrf --capabilities       Offline. Prints one capability token per stdout line
//                                 (see WatchVrfUsage.Capabilities) and exits 0. The runner
//                                 probes this BEFORE passing any optional flag, so a
//                                 deployed binary that predates a flag is detected instead
//                                 of killed with exit 2 (the -ConsoleLogDir landmine).
//
//   WatchVrf [appNo] [dur] [samp] [federation] [--stop-file <path>] [--diag] [--no-wait-ext]
//            [--no-track] [--report-backends] [--device-address <addr|none>]
//                                 LIVE observation: join the federation and stream POS,...
//                                 position lines, CON,... Object Console lines, and
//                                 TSK,... / RPT,... task-completion + text-report lines
//                                 (see WatchRunner). Requires a running VR-Forces federation.
//                                 --stop-file: end the observation EARLY (clean resign) as
//                                 soon as that file exists; [dur] stays the upper bound.
//                                 --diag / --no-wait-ext / --no-track / --report-backends:
//                                 observation-channel diagnostics, the extended-data lever,
//                                 the skip-tracking isolation (which zeroes POS by
//                                 construction) and the back-end count (see WatchVrfUsage).
//                                 --device-address: the VR-Forces-level --deviceAddress this
//                                 observer joins with - <addr>, or 'none' to push none at
//                                 all; absent keeps the facade default (PREREG_52_RTIEXEC
//                                 sec 4). Echoed in the banner and on the '# DIAG licence'
//                                 line so a trace records which arm produced it.
//
// The dispatch below references only ConSelfTest, WatchVrfUsage and ToolArgs - all pure
// managed, none of them touching VrfBridge - plus WatchRunner, whose bridge-using code
// lives inside WatchRunner.Run and is JITted only when called. So the --con-selftest and
// --capabilities paths never load VrfBridge.dll.

if (args.Length > 0 && args[0] == "--capabilities")
{
    // Same sole-argument rule as --con-selftest, for the same reason: this path observes
    // nothing, so a companion argument is a request the tool would silently drop.
    if (args.Length > 1)
        return ToolArgs.Usage(
            $"--capabilities takes no other arguments; got: {string.Join(" ", args[1..])}.",
            WatchVrfUsage.Lines());
    foreach (string cap in WatchVrfUsage.Capabilities) Console.Out.WriteLine(cap);
    return ToolArgs.ExitOk;
}

if (args.Length > 0 && args[0] == "--con-selftest")
{
    // --con-selftest observes NOTHING, so any companion argument means the caller asked
    // for the offline check and a live observation at once. Refuse rather than silently
    // dropping the observation arguments: for the movement oracle, a run that quietly
    // did less than it was told to is exactly the failure mode to avoid.
    if (args.Length > 1)
        return ToolArgs.Usage(
            $"--con-selftest takes no other arguments; got: {string.Join(" ", args[1..])}. "
          + "It is an offline formatting check and observes nothing.",
            WatchVrfUsage.Lines());
    return ConSelfTest.Run();
}

return WatchRunner.Run(args);
