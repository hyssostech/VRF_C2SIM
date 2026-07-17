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
// The dispatch below references only WatchVrf-assembly types (ConSelfTest, WatchRunner);
// the bridge-using code lives inside WatchRunner.Run, which is JITted only when called -
// so the --con-selftest path never loads VrfBridge.dll.

if (args.Length > 0 && args[0] == "--con-selftest")
    return ConSelfTest.Run();

return WatchRunner.Run(args);
