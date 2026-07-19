using VrfC2Sim.Tools;
using WatchVrf;

// tools/WatchVrf entry point. Two modes:
//
//   WatchVrf --con-selftest       Offline check of the CON,... Object Console line
//                                 formatting (groundwork plan 0.6). Pure managed; does
//                                 NOT touch VrfBridge, so it runs without the native
//                                 bridge DLL / MAK bin dirs on PATH.
//
//   WatchVrf [appNo] [dur] [samp] [federation]
//                                 LIVE observation: join the federation and stream POS,...
//                                 position lines + CON,... Object Console lines (see
//                                 WatchRunner). Requires a running VR-Forces federation.
//
// The dispatch below references only ConSelfTest, WatchVrfUsage and ToolArgs - all pure
// managed, none of them touching VrfBridge - plus WatchRunner, whose bridge-using code
// lives inside WatchRunner.Run and is JITted only when called. So the --con-selftest path
// never loads VrfBridge.dll.

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
