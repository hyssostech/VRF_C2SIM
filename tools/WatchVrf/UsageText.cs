namespace WatchVrf;

// The single source of truth for WatchVrf's usage block, shared by BOTH entry paths:
// Program.cs (which guards the offline --con-selftest dispatch) and WatchRunner (which
// validates the live-observation arguments).
//
// WHY ITS OWN FILE: WatchRunner references VrfBridge, and the --con-selftest path must
// stay fully offline - it must never load the native bridge DLL. If Program.cs called a
// helper defined ON WatchRunner just to print usage, the offline path would take a
// dependency on the type that exists to be avoided. This class holds nothing but strings.
internal static class WatchVrfUsage
{
    public static string[] Lines() => new[]
    {
        "usage: WatchVrf.exe [applicationNumber] [durationSecs] [sampleSecs] [federation]",
        "       WatchVrf.exe --con-selftest",
        "",
        "  applicationNumber  Optional. Integer 1..65535. Default 3399.",
        "                     Use a FRESH, ledgered appNo every run (RUNBOOK sec 7).",
        "  durationSecs       Optional. Whole number > 0. Default 120.",
        "  sampleSecs         Optional. Whole number > 0. Default 15.",
        "  federation         Optional. Default 'CWIX-2024'.",
        "",
        "  --con-selftest     Offline check of the CON,... line formatting. Takes NO other",
        "                     arguments: it joins no federation and observes nothing, so",
        "                     pairing it with observation arguments is a contradiction.",
        "",
        "WatchVrf is the MOVEMENT ORACLE: an unparseable argument is a HARD FAILURE,",
        "never a silent fallback to a default, because the resulting trace would",
        "describe something other than what the caller asked to observe.",
        "",
        "examples:  WatchVrf.exe 3399 120 15",
        "           WatchVrf.exe 3401 600 5 CWIX-2024",
        "           WatchVrf.exe --con-selftest",
    };
}
