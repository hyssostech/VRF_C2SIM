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
    // Capability tokens printed by `WatchVrf.exe --capabilities`, one per line. A runner
    // probes these BEFORE passing an optional flag. ADD a token here whenever a new
    // optional flag is added to the live path; never remove one while the flag exists.
    public static readonly string[] Capabilities =
        { "capabilities", "con-selftest", "stop-file", "diag", "no-wait-ext" };

    public const string StopFileFlag = "--stop-file";
    public const string DiagFlag = "--diag";
    public const string NoWaitExtFlag = "--no-wait-ext";

    public static string[] Lines() => new[]
    {
        "usage: WatchVrf.exe [applicationNumber] [durationSecs] [sampleSecs] [federation]",
        "                    [--stop-file <path>] [--diag] [--no-wait-ext]",
        "       WatchVrf.exe --con-selftest",
        "       WatchVrf.exe --capabilities",
        "",
        "  applicationNumber  Optional. Integer 1..65535. Default 3399.",
        "                     Use a FRESH, ledgered appNo every run (RUNBOOK sec 7).",
        "  durationSecs       Optional. Whole number > 0. Default 120. With --stop-file this",
        "                     is the UPPER BOUND; the observation normally ends earlier.",
        "  sampleSecs         Optional. Whole number > 0. Default 15.",
        "  federation         Optional. Default is stack-aware (5.0.2 -> CWIX-2024; 5.2 -> the",
        "                     connection-config identity; tools/Shared/StackIdentity.cs).",
        "",
        "  --stop-file <path> Optional. End the observation EARLY - one '# STOP' trace line,",
        "                     then the normal clean resign - as soon as <path> EXISTS (polled",
        "                     about once a second). The file must NOT exist at start; if it",
        "                     does the tool exits 2 without joining, because the observation",
        "                     would end immediately and the trace would be silently empty.",
        "  --diag             Optional. OBSERVATION-CHANNEL DIAGNOSTICS on stdout, same line",
        "                     shapes as always (no new files). At join, two '# DIAG' lines:",
        "                     the NativeStackInfo string and the VR-Link licence probes",
        "                     (rti=/vrlink=). On every sample the existing '# t=...' summary",
        "                     gains ent=/agg=/env=/ctl=/extattr=/waitext=/discovered= read",
        "                     STRAIGHT off the reflected lists, so they do not depend on the",
        "                     UUID callbacks 'reflected=' comes from. extattr is always -1",
        "                     (no public accessor). Before the resign it prints VR-Forces'",
        "                     own reflected-object breakdown, bracketed by '# DIAG vendor'",
        "                     lines because that text is MAK notify format, not CSV.",
        "  --no-wait-ext      Optional. Clear the reflected ext-entity list's",
        "                     waitForVrfExtendedData at Start, so VR-Forces objects are not",
        "                     withheld from the list while their extended data is awaited.",
        "                     A PROBE LEVER: it changes what this observer sees. Off by",
        "                     default; pair it with --diag or the trace cannot say whether",
        "                     it took effect (waitext= reports the live flag).",
        "  --con-selftest     Offline check of the CON,/TSK,/RPT, line formatting. Takes NO other",
        "                     arguments: it joins no federation and observes nothing, so",
        "                     pairing it with observation arguments is a contradiction.",
        "  --capabilities     Offline. Prints one capability token per line (currently:",
        "                     capabilities, con-selftest, stop-file, diag, no-wait-ext) and",
        "                     exits 0. Sole argument.",
        "",
        "WatchVrf is the MOVEMENT ORACLE: an unparseable argument is a HARD FAILURE,",
        "never a silent fallback to a default, because the resulting trace would",
        "describe something other than what the caller asked to observe.",
        "",
        "examples:  WatchVrf.exe 3399 120 15",
        "           WatchVrf.exe 3401 600 5 CWIX-2024",
        "           WatchVrf.exe 3401 1460 2 CWIX-2024 --stop-file C:\\runs\\x\\observers.stop",
        "           WatchVrf.exe 3820 60 5 --diag",
        "           WatchVrf.exe 3821 60 5 --diag --no-wait-ext",
        "           WatchVrf.exe --con-selftest",
        "           WatchVrf.exe --capabilities",
    };
}
