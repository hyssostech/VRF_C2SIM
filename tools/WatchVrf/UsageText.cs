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
        { "capabilities", "con-selftest", "stop-file", "diag", "no-wait-ext",
          "no-track", "report-backends", "device-address" };

    public const string StopFileFlag = "--stop-file";
    public const string DiagFlag = "--diag";
    public const string NoWaitExtFlag = "--no-wait-ext";
    public const string NoTrackFlag = "--no-track";
    public const string ReportBackendsFlag = "--report-backends";
    public const string DeviceAddressFlag = "--device-address";

    // The sentinel value that means "push NO --deviceAddress at all", as opposed to pushing
    // an address. Matched case-insensitively on purpose: treating "None" as a literal
    // interface address would be a SILENT wrong-arm run, and this flag exists to be the
    // single variable in a discriminating pair (PREREG_52_RTIEXEC sec 4).
    public const string DeviceAddressNone = "none";

    public static string[] Lines() => new[]
    {
        "usage: WatchVrf.exe [applicationNumber] [durationSecs] [sampleSecs] [federation]",
        "                    [--stop-file <path>] [--diag] [--no-wait-ext] [--no-track]",
        "                    [--report-backends] [--device-address <addr|none>]",
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
        "                     gains ent=/agg=/env=/ctl=/extattr=/waitext=/discovered=/",
        "                     envalias=/total= read STRAIGHT off the reflected lists, so they",
        "                     do not depend on the UUID callbacks 'reflected=' comes from.",
        "                     extattr is always -1 (no public accessor).",
        "                     env= AND ctl= ARE THE SAME LIST, NOT TWO POPULATIONS: VR-Forces",
        "                     publishes control objects as environment processes, so the one",
        "                     list is reached through two accessors - DtReflectedControlObject",
        "                     List derives from DtReflectedEnvironmentProcessList (vrfExtObjects",
        "                     /reflectedControlObjectList.h:24 on 5.2d, :23 on 5.0.2) and",
        "                     DtVrlinkVrfRemoteController returns its single member from both",
        "                     reflectedEnvironmentProcessList() and controlObjectList()",
        "                     (vrlinkVrfRemoteController.h:143-144/:167 on 5.2d,",
        "                     :141-142/:165 on 5.0.2). envalias= is the runtime proof of that",
        "                     (1 same object, 0 different, -1 not compared). total= is the",
        "                     vendor-style total ent+agg+ctl - env is NOT added, matching",
        "                     VR-Forces' own 'Reflected Objects' line; -1 if any term is",
        "                     unreachable. Before the resign it prints VR-Forces'",
        "                     own reflected-object breakdown, bracketed by '# DIAG vendor'",
        "                     lines because that text is MAK notify format, not CSV.",
        "  --no-wait-ext      Optional. Clear the reflected ext-entity list's",
        "                     waitForVrfExtendedData at Start, so VR-Forces objects are not",
        "                     withheld from the list while their extended data is awaited.",
        "                     A PROBE LEVER: it changes what this observer sees. Off by",
        "                     default; pair it with --diag or the trace cannot say whether",
        "                     it took effect (waitext= reports the live flag).",
        "  --no-track         Optional. SKIP BeginTrackingReflectedObjects(); everything else -",
        "                     Start, the tick loop, the CON/TSK/RPT subscriptions, the resign -",
        "                     is unchanged. The UUID-change callbacks are then never registered,",
        "                     so 'reflected=' and 'readable=' NECESSARILY STAY 0 and NO POS line",
        "                     can ever be emitted: a zero trace under this flag is the flag",
        "                     working, not a finding. It exists to isolate the one call the",
        "                     observer makes that the CreateOne federate does not - pair it with",
        "                     --diag (whose ent=/agg=/... counts do NOT come from those",
        "                     callbacks) or the run measures nothing at all.",
        "  --report-backends  Optional. Append ' backends=<n>' to every '# t=' sample line, n =",
        "                     the controller's back-end count - the same reading CreateOne",
        "                     refuses to act on when it is 0. Says whether this observer sees",
        "                     the sim engine at all, independently of any reflected object.",
        "  --device-address <addr|none>",
        "                     Optional. The VR-Forces-level network interface for UDP",
        "                     (best-effort) traffic - the 5.0.2 Launcher's 'Network Interface",
        "                     Address', passed through to VR-Link as --deviceAddress",
        "                     (UG52 Table 11 p180-181; VrfFacade.cpp Start()).",
        "                       omitted   keep the facade default (127.0.0.1),",
        "                       <addr>    use that address, e.g. --device-address 10.5.0.2,",
        "                       none      pass NO --deviceAddress at all, so VR-Forces picks",
        "                                 'the first device listed' (IOG 5.2.1).",
        "                     A PROBE LEVER, and the observer-side SINGLE VARIABLE against a",
        "                     live rtiexec sim: whether this setting matters at all is",
        "                     UNTESTED (PREREG_52_RTIEXEC sec 4). The value is echoed in the",
        "                     run banner and, with --diag, on the '# DIAG licence' line, so a",
        "                     trace always records which arm produced it.",
        "                     SCOPE: the argv push happens on the 5.2 HLA build only; the",
        "                     5.0.2 HLA argv carries no --deviceAddress, so on that build the",
        "                     flag is recorded but inert.",
        "  --con-selftest     Offline check of the CON,/TSK,/RPT, line formatting. Takes NO other",
        "                     arguments: it joins no federation and observes nothing, so",
        "                     pairing it with observation arguments is a contradiction.",
        "  --capabilities     Offline. Prints one capability token per line (currently:",
        "                     capabilities, con-selftest, stop-file, diag, no-wait-ext,",
        "                     no-track, report-backends, device-address) and exits 0.",
        "                     Sole argument.",
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
        "           WatchVrf.exe 3823 60 5 --diag --report-backends",
        "           WatchVrf.exe 3824 60 5 --diag --report-backends --no-track",
        "           WatchVrf.exe 3830 60 5 --diag --device-address none",
        "           WatchVrf.exe 3831 60 5 --diag --device-address 10.5.0.2",
        "           WatchVrf.exe --con-selftest",
        "           WatchVrf.exe --capabilities",
    };
}
