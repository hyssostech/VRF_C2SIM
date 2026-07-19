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
        "                    [--console-log-dir <path>]",
        "       WatchVrf.exe --con-selftest",
        "",
        "  applicationNumber  Optional. Integer 1..65535. Default 3399.",
        "                     Use a FRESH, ledgered appNo every run (RUNBOOK sec 7).",
        "  durationSecs       Optional. Whole number > 0. Default 120.",
        "  sampleSecs         Optional. Whole number > 0. Default 15.",
        "  federation         Optional. Default 'CWIX-2024'.",
        "",
        "  --console-log-dir <path>",
        "                     Optional. ARM BACKEND-SIDE CONSOLE CAPTURE. For every uuid",
        "                     discovered, once: raise that object's notify level to the",
        "                     maximum (4 = debug) and ask the simulating backend to write",
        "                     that object's console to <path>/console-<uuid>.log. The trace",
        "                     records each arming as CONARM,<t>,<uuid>,<path>.",
        "                     WHY: an empty CON stream cannot distinguish 'no warnings were",
        "                     raised' from 'warnings were raised and not delivered to this",
        "                     observer'. The backend writes the file on ITS OWN filesystem,",
        "                     bypassing the delivery path under suspicion, so a populated file",
        "                     beside an empty CON stream proves a delivery gap and an empty",
        "                     file proves silence.",
        "                     CAVEATS: <path> is resolved by the BACKEND, which may be another",
        "                     machine; the directory is created locally, which proves the path",
        "                     is well-formed, not that the backend can write it. VR-Forces",
        "                     acknowledges neither call, so an unwritable path is a SILENT",
        "                     no-op. Colons in a uuid are mapped to '_' in the filename.",
        "                     Omit the flag and the run behaves exactly as it always has.",
        "",
        "  --con-selftest     Offline check of the CON,/TSK,/RPT, line formatting. Takes NO other",
        "                     arguments: it joins no federation and observes nothing, so",
        "                     pairing it with observation arguments is a contradiction.",
        "",
        "WatchVrf is the MOVEMENT ORACLE: an unparseable argument is a HARD FAILURE,",
        "never a silent fallback to a default, because the resulting trace would",
        "describe something other than what the caller asked to observe.",
        "",
        "examples:  WatchVrf.exe 3399 120 15",
        "           WatchVrf.exe 3401 600 5 CWIX-2024",
        "           WatchVrf.exe 3402 600 2 CWIX-2024 --console-log-dir runs\\20260719_run\\console",
        "           WatchVrf.exe --con-selftest",
    };
}
