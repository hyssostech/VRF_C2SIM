using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using VrfC2Sim;
using VrfC2Sim.Tools;

// tools/PauseSim - PAUSE or RESUME the VR-Forces scenario by REMOTE CONTROL.
//
// Joins the federation, issues DtVrfRemoteController::pause() or ::run() via
// VrfBridge.Pause() / VrfBridge.Run() (VrfBridge.cpp:340-341 -> VrfFacade::Pause / ::Run,
// VrfFacade.cpp:825-826 -> controller->pause() / controller->run()), reads the back end's
// own answer back, and resigns CLEANLY. Pure VR-Forces: no C2SIM / STOMP. It creates,
// deletes and tasks NOTHING - the only state it changes is play/pause.
//
// RESUME IS run(). The 5.2 remote-control sample has no separate "resume":
// examples/remoteControl/commandLineRemoteController.cxx:1044-1050 ("run" -> run(),
// "Running!") and :1095-1102 ("pause" -> pause(), "Pausing!") are the pair, and neither
// takes an address, so BOTH apply to ALL back ends (the address-carrying "simrun" /
// "simpause" variants in the same sample send a DtSimControlEvent to ONE back end - not
// what this tool wants). So "PauseSim resume" = Run(), exactly as tools/RunSim sends it,
// and the two tools differ only in that RunSim may also set a multiplier.
//
// WHY THIS TOOL EXISTS: the Q5 live probe (assessment live gate 11, validation V5) has to
// PAUSE a running scenario mid-run and RESUME it, to observe that a paused scenario does
// not age a C2SIM task clock. Nothing in the repo could pause: VrfC2SimApp calls Run() on
// the C2SIM flow only, and tools/RunSim is a bare Run() sender with no counterpart.
//
// WHAT IT READS BACK, AND WHAT THAT IS WORTH. Two independent readings, not one:
//   * VrfBridge.SimTimeSeconds() wraps DtVrfRemoteController::simTime() - the SCENARIO clock,
//     the one the vendor sample prints as "Sim time from sim engine status"
//     (commandLineRemoteController.cxx:1247-1252). It STOPS while the scenario is paused,
//     which makes it the reading the Q5 probe actually cares about: the tool samples it,
//     holds ~2 s of ticks, and samples again. THIS ONE IS ALWAYS AVAILABLE. It is sampled a
//     THIRD time, before the command, and that is not decoration: simTimeBefore vs
//     simTimeAfterCmd says whether the scenario was RUNNING in the seconds before the pause,
//     which is the difference between "this tool stopped the clock" and "the clock was already
//     stopped". Both numbers are on the [RESULT] line; read them together.
//   * VrfBridge.BackendControlState() wraps DtVrfRemoteController::backendsControlState()
//     (vrfRemoteController.h:320-323 on 5.2d) - 1 Paused, 2 Running, plus our own -3/-2/-1/0
//     for "no vendor answer" (VrfFacade.h). It is the FIRST back end's CACHED state, updated
//     when a status message arrives, so this tool TICKS AND POLLS it rather than reading it
//     once. IT IS READ BY REFLECTION - see the next paragraph.
// A state reading that POSITIVELY CONTRADICTS the command (Running after a pause, Paused
// after a resume, after 10 s of status rounds) is exit 1. A tool that exits 0 while nothing
// happened is the false green this repo has been bitten by six times (lessons-false-greens).
//
// WHY BackendControlState IS REFLECTED AND NOT CALLED DIRECTLY. It is an STP-809 member: it
// exists in src/VrfBridge/VrfBridge.cpp on main, but NOT in the DEPLOYED Release-5.2
// VrfBridge.dll, which is still the gate G-A pin of 2026-09-14 (RUNBOOK sec 9;
// SHA256 99B7B235..., which carries SimTimeSeconds / BackendCount / TryGetEntityKinematics
// and not this). A direct call therefore does not COMPILE against the deployed bridge, and
// this tool must be buildable and deployable TODAY - a partial redeploy of the bridge is the
// documented trap (RUNBOOK sec 9, "WHY A PARTIAL DEPLOY IS DANGEROUS"), and re-pinning all
// eleven consumers is gate G-A's job, not this tool's. So the member is looked up once by
// reflection: present -> used; absent -> reported as controlState=Unavailable and the verdict
// rests on the scenario clock alone, which is stated in the output rather than hidden. WHEN
// G-A RE-PINS the bridge with the STP-809 members, this tool starts reading the state with NO
// CODE CHANGE, and the reflection can then be collapsed into a direct call.
//
// STACK-AWARE: the federation identity comes from tools/Shared/StackIdentity.cs, which reads
// the bound stack from the LOADED native DLLs - never from a build flag. On 5.2 the join is
// the CONFIG-FILE join (execName MAK-ONE-2025; empty Federation/FedFileName/FomModules,
// because config FOM modules are ADDITIVE - MIGRATION_DIFF A2/A9); on 5.0.2 it is CWIX-2024 +
// RPR_FOM_v2.0_1516-2010.xml + the 3 MAK modules.
//
// LAUNCH ENV, 5.2 (PREREG_52_LAUNCH_2026-09-03): PATH prefixed with
// C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin,
// RTI_RID_FILE = config\rid-501-rtiexec-min.mtl, RTI_ASSISTANT_DISABLE=1,
// MAKLMGRD_LICENSE_FILE (User scope first, then Machine), cwd = C:\MAK\vrforces5.2d\bin64,
// and a FRESH ApplicationNumber each invocation.
//   & <repo>\tools\PauseSim\bin\Release-5.2\net10.0\win-x64\PauseSim.exe pause <freshAppNo>
// LAUNCH ENV, 5.0.2 (RUNBOOK sec 7): RTI 4.6.1 on PATH, cwd = C:\MAK\vrforces5.0.2\bin64,
// and bin\Release\ instead of bin\Release-5.2\.
//
// ONE appNumber PER INVOCATION. Each invocation is a whole join/resign cycle, so a pause and
// a later resume take TWO ledgered numbers - the same rule tools/SetSimRate carries
// (OPUS_EXECUTION_PLAN.md Appendix B, the SetSimRate NOTE: "four invocations, four numbers").
// This tool NEVER allocates one itself.
//
// Args: <pause|resume> <applicationNumber> [federation] [--dry-run] [--help]

// The bound native stack, read from the LOADED DLLs. Same probe as VrfC2SimApp
// --runtime-check; NoInlining so the bridge assembly is resolved only when it is called.
[MethodImpl(MethodImplOptions.NoInlining)]
static string NativeStackLine()
{
    try { return "native stack = " + VrfBridge.NativeStackInfo(); }
    catch (Exception e) { return "native stack = UNAVAILABLE (" + e.GetType().Name + ": " + e.Message + ")"; }
}

// OUR sentinel for "this bridge build has no BackendControlState" - distinct from every
// value the vendor or the facade can return (VrfFacade.h uses -3..2). See the header.
const int ControlUnavailable = -99;

// Looked up ONCE. A null here is the stale-pin case, not an error.
MethodInfo controlStateMethod = null;
try { controlStateMethod = typeof(VrfBridge).GetMethod("BackendControlState", Type.EmptyTypes); }
catch { controlStateMethod = null; }

int ReadControlState(VrfBridge b)
{
    if (controlStateMethod == null) return ControlUnavailable;
    try { return (int)controlStateMethod.Invoke(b, null); }
    catch { return ControlUnavailable; }
}

// The BackendControl values of VrfFacade.h / VrfBridge.cpp:287-297, by name. A LOCAL mapping
// and not a shared enum on purpose: the non-negative numbers are the vendor's own control
// types and the negatives are ours, both documented at the bridge - a second definition that
// drifted would be worse than a lookup that is obviously local.
static string ControlName(int v) => v switch
{
    2   => "Running",
    1   => "Paused",
    0   => "Unknown",       // DtUnknownControlType with back ends still in the list
    -1  => "Unreadable",    // no controller, or the vendor call threw - "no reading"
    -2  => "NoBackend",     // the vendor says no remote back end exists
    -3  => "Other",         // a vendor control type that is none of the three above
    -99 => "Unavailable",   // OURS: this VrfBridge build predates BackendControlState
    _   => "Undefined(" + v.ToString(CultureInfo.InvariantCulture) + ")",
};

// -1.0 is the facade's "no reading" (no controller, or no back end discovered) - never a
// scenario legitimately sitting at t = 0, which is why it is printed as a word.
static string SimTimeText(double t) =>
    t < 0 ? "none" : t.ToString("F3", CultureInfo.InvariantCulture);

// Usage text goes to STDERR on an argument error (exit 2) and to STDOUT for --help
// (exit 0), so a runner capturing stdout for data never ingests a failure block.
static void PrintUsage(System.IO.TextWriter w)
{
    w.WriteLine("usage: PauseSim.exe <pause|resume> <applicationNumber> [federation] [--dry-run]");
    w.WriteLine("       PauseSim.exe --help");
    w.WriteLine();
    w.WriteLine("  pause | resume     REQUIRED. pause -> controller->pause(); resume -> controller->run()");
    w.WriteLine("                     (the 5.2 remoteControl sample's own pair; neither takes an");
    w.WriteLine("                     address, so both apply to ALL back ends).");
    w.WriteLine("  applicationNumber  REQUIRED. NO DEFAULT - use a FRESH, ledgered appNo for EVERY");
    w.WriteLine("                     invocation (RUNBOOK sec 7; Appendix B). A pause and a later");
    w.WriteLine("                     resume are TWO joins and therefore TWO numbers.");
    w.WriteLine("  federation         Optional. Default is stack-aware (5.0.2 -> CWIX-2024;");
    w.WriteLine("                     5.2 -> connection-config identity; tools/Shared/StackIdentity.cs).");
    w.WriteLine("  --dry-run          Print the plan and EXIT. Joins nothing, sends nothing.");
    w.WriteLine();
    w.WriteLine("exit: 0 issued and read back (the verdict is on the [RESULT] line); 1 not joined /");
    w.WriteLine("      no back end / the back end CONTRADICTED the command; 2 usage.");
    w.WriteLine();
    w.WriteLine("examples:  PauseSim.exe pause  4361     # stop the scenario clock");
    w.WriteLine("           PauseSim.exe resume 4362     # start it again (controller->run())");
}

static int Usage(string problem)
{
    Console.Error.WriteLine($"[FAIL] {problem}");
    Console.Error.WriteLine();
    PrintUsage(Console.Error);
    return 2;
}

// --config <path> (V6 harvest 2026-09-15): the VR-Link connection config is resolved
// EXPLICITLY - arg > env Vrf__ConnectionConfigFile > the loaded stack's own tree - because the
// vendor default is CWD-RELATIVE and silently falls back to built-in defaults when the cwd is
// not the VR-Forces bin64. Taken out of args FIRST so the parsing below never sees either
// token (tools/Shared/ConnectionConfig.cs).
if (!ConnectionConfig.TryTakeFlag(args, out args, out string connArg, out string connProblem))
    return Usage(connProblem);

bool dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));
bool help = args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase));
var unknownFlags = args.Where(a => a.StartsWith("--", StringComparison.Ordinal))
                       .Where(a => !string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase))
                       .ToArray();
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

// One line saying whether the state read-back is available on THIS bridge build, printed by
// --help and by every real invocation, so a reader never has to guess which half of the
// verdict was in play.
string controlReaderLine = controlStateMethod != null
    ? "control-state read-back = AVAILABLE (VrfBridge.BackendControlState, STP-809)"
    : "control-state read-back = UNAVAILABLE on this VrfBridge build (pre-STP-809 pin; " +
      "RUNBOOK sec 9). The verdict will rest on the scenario clock alone.";

// --help is the NO-JOIN plan printer (the runner's self-test convention).
if (help)
{
    Console.WriteLine("=== PauseSim - PAUSE or RESUME the VR-Forces scenario (remote control) ===");
    Console.WriteLine("    " + NativeStackLine());
    var planCfg = new StartupConfig { Protocol = VrfProtocol.Hla1516e, SiteId = 1, SessionId = 1 };
    Console.WriteLine("    " + StackIdentity.Apply(planCfg, positional.Length >= 3 ? positional[2] : null));
    Console.WriteLine("    " + ConnectionConfig.Resolve(connArg).Banner);
    Console.WriteLine("    " + controlReaderLine);
    Console.WriteLine();
    Console.WriteLine("    PLAN: join -> tick until a back end is discovered (cap 15 s) ->");
    Console.WriteLine("          read simTime (+ control state when available) -> Pause() or Run() ->");
    Console.WriteLine("          tick 3-10 s polling the control state -> hold 2 s and re-read");
    Console.WriteLine("          simTime -> print one [RESULT] line -> resign cleanly.");
    Console.WriteLine("    RESUME IS run(): the vendor's own pause/run pair, neither address-scoped.");
    Console.WriteLine("    THIS INVOCATION JOINED NOTHING and changed nothing.");
    Console.WriteLine();
    PrintUsage(Console.Out);
    return 0;
}

if (unknownFlags.Length > 0)
    return Usage($"unknown option(s): {string.Join(" ", unknownFlags)}. PauseSim accepts --dry-run and --help only.");

// -- argument validation: fail LOUDLY and non-zero, never guess a default ---------

if (positional.Length == 0)
    return Usage("No arguments. Both <pause|resume> and <applicationNumber> are required.");

string actionArg = positional[0].ToLowerInvariant();
bool isPause;
if (actionArg == "pause")       isPause = true;
else if (actionArg == "resume") isPause = false;
else if (actionArg == "run")
    return Usage("Action 'run' is spelled 'resume' here, to keep this tool's two actions a pair. " +
                 "(It IS controller->run() underneath - see the header - and tools/RunSim sends the " +
                 "same call when a scenario has never been started.)");
else
    return Usage($"Action '{positional[0]}' is not one of: pause, resume.");

if (positional.Length < 2)
    return Usage("Missing <applicationNumber>. It is REQUIRED and has NO default - supply a FRESH " +
                 "appNo that is not in use by VR-Forces, WatchVrf, or any prior join. A pause and a " +
                 "later resume are TWO invocations and therefore TWO numbers.");

if (!int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int appNumber))
    return Usage($"ApplicationNumber '{positional[1]}' is not an integer.");

if (appNumber <= 0 || appNumber > 65535)
    return Usage($"ApplicationNumber {appNumber} is out of range (expected 1..65535).");

string federation = positional.Length >= 3 && !string.IsNullOrWhiteSpace(positional[2])
    ? positional[2]
    : null;   // null = stack default (5.0.2 CWIX-2024; 5.2 config-file identity)

string action      = isPause ? "pause" : "resume";
string call        = isPause ? "bridge.Pause() -> controller->pause()" : "bridge.Run() -> controller->run()";
int    expectState = isPause ? 1 : 2;   // BackendControl.Paused / .Running

// Federation / FedFileName / FomModules are filled by the BOUND STACK, not by constants here:
// on 5.2 the connection config owns them and our 5.0.2 list would be additive on top of the
// shipped 17 modules (MIGRATION_DIFF A2/A9). Kept identical to tools/RunSim.
var cfg = new StartupConfig
{
    Protocol = VrfProtocol.Hla1516e,
    ApplicationNumber = appNumber,
    SiteId = 1,
    SessionId = 1,
    HostInetAddr = "127.0.0.1",
};
string fedDesc = StackIdentity.Apply(cfg, federation);
// Resolve the connection config, apply it to cfg, and REFUSE to join when it is missing:
// without it VR-Link joins with built-in defaults and the tool reports a successful join and
// then sees nothing (V6, 2026-09-15).
var conn = ConnectionConfig.Resolve(connArg);
conn.ApplyTo(cfg);

Console.WriteLine("=== PauseSim - PAUSE or RESUME the VR-Forces scenario (remote control) ===");
Console.WriteLine($"    {fedDesc}  appNumber={appNumber}  action={action}");
Console.WriteLine($"    {NativeStackLine()}");
Console.WriteLine($"    {conn.Banner}");
// A --dry-run JOINS NOTHING, so a missing config is reported there, not refused: a dry run's
// contract is 'arguments validated, no action taken', and turning it into exit 1 would make a
// runner's own dry run fail on a machine where the real run is fine. tools/ResetVrf is the
// deliberate exception - its --dry-run DOES join, so it refuses like a real run.
if (!conn.Ok)
{
    if (!dryRun) { Console.Error.WriteLine(conn.RefusalText); return 1; }
    Console.WriteLine("[DRY-RUN] the connection config above does NOT exist; a real run would " +
                      "REFUSE to join. Nothing was joined either way.");
}
Console.WriteLine($"    {controlReaderLine}");
Console.WriteLine($"    started {DateTime.Now:yyyy-MM-dd HH:mm:ss} local / {DateTime.UtcNow:HH:mm:ss} UTC");
Console.WriteLine($"    ACTION: {call} on ALL backends (no address argument).");
Console.WriteLine("    This tool creates/deletes/tasks NOTHING. (use a FRESH appNumber each invocation)\n");

if (dryRun)
{
    Console.WriteLine("[DRY-RUN] arguments validated; the plan above is what a real run would do.");
    Console.WriteLine($"[DRY-RUN] NOT joining, NOT sending {(isPause ? "pause()" : "run()")}. No appNumber was consumed.");
    return 0;
}

VrfBridge bridge = null;
try
{
    bridge = new VrfBridge();

    // 1. JOIN the federation.
    Console.WriteLine("[..] bridge.Start() - joining the federation...");
    if (!bridge.Start(cfg))
    {
        Console.Error.WriteLine($"[FAIL] bridge.Start() returned false - NOT joined, {action} NOT sent. " +
                                "Check: the bound stack's RTI on PATH (5.0.2 -> makRti4.6.1, 5.2 -> " +
                                "makRti5.0.1 + RTI_RID_FILE + RTI_ASSISTANT_DISABLE), MAKLMGRD_LICENSE_FILE, " +
                                "FED/FOM, cwd = VRF bin64, fresh appNumber, VR-Forces actually running.");
        return 1;
    }
    Console.WriteLine($"[OK] joined (BackendCount={bridge.BackendCount()} immediately after Start).");

    // 2. SETTLE: tick until a backend is actually discovered. LOAD-BEARING - backends are NOT
    //    known at the instant Start() returns (same idiom as tools/RunSim / SetSimRate).
    //    Issuing pause()/run() against zero known backends risks a silent no-op reported as
    //    success, AND simTime() returns -1 with no back end, so there would be no reading to
    //    compare against either.
    Console.WriteLine("[..] settling - ticking until a backend is discovered (up to 15 s)...");
    var swSettle = Stopwatch.StartNew();
    int backends = 0;
    while (swSettle.Elapsed < TimeSpan.FromSeconds(15))
    {
        bridge.Tick();
        Thread.Sleep(50);
        backends = bridge.BackendCount();
        if (backends > 0) break;
    }

    if (backends == 0)
    {
        Console.Error.WriteLine($"[FAIL] no backend discovered after {swSettle.Elapsed.TotalSeconds:F0} s " +
                                $"(BackendCount=0). The {action} was NOT sent - sending it now would be a " +
                                "silent no-op reported as success. Confirm VR-Forces is running with a " +
                                "scenario loaded and a simulation backend connected, then retry with a " +
                                "FRESH appNumber.");
        Console.Error.WriteLine("[..] bridge.Stop() - resigning cleanly...");
        bridge.Stop();
        Console.Error.WriteLine($"[OK] resigned. Mark appNumber {appNumber} as USED.");
        return 1;
    }
    Console.WriteLine($"[OK] {backends} backend(s) discovered after {swSettle.Elapsed.TotalSeconds:F1} s.");

    // 3. READ BEFORE. Both readers are read-only, send nothing on the wire, and read state the
    //    tick mutates - so they are called BETWEEN ticks (VrfFacade.h).
    double simBefore     = bridge.SimTimeSeconds();
    int    controlBefore = ReadControlState(bridge);
    Console.WriteLine($"[OK] before: simTime={SimTimeText(simBefore)} s  controlState={ControlName(controlBefore)}");
    if (controlBefore == expectState)
        Console.WriteLine($"[WARN] the back end ALREADY reports {ControlName(expectState)} before the command. " +
                          $"The {action} below is then a no-op that will read back as CONFIRMED - which says " +
                          "nothing about whether THIS tool caused it. Read controlBefore, not just the verdict.");

    // 4. Issue the remote call. pause() / run() take no address argument
    //    (vrfRemoteController.h), so they apply to ALL backends.
    Console.WriteLine($"[..] {call} - issuing...");
    if (isPause) bridge.Pause(); else bridge.Run();
    Console.WriteLine("[OK] command issued (queued on the controller).");

    // 5. FLUSH AND CONFIRM, in one loop. LOAD-BEARING on both counts: the call only QUEUES onto
    //    the remote controller (without ticks this process exits before it leaves), and the
    //    control state is a CACHED value that changes only when the next back-end STATUS message
    //    arrives - so a single read straight after the send would be a race, not a reading.
    //    The 3 s floor is the flush tools/RunSim and SetSimRate both use; the loop then runs on
    //    to 10 s, which covers several status rounds, and stops as soon as the state agrees.
    //    With no state reader on this bridge build the loop simply flushes for the full 10 s.
    Console.WriteLine($"[..] flushing and polling the control state for {ControlName(expectState)} (3 s floor, 10 s cap)...");
    var swConfirm = Stopwatch.StartNew();
    int controlAfter = ReadControlState(bridge);
    while (swConfirm.Elapsed < TimeSpan.FromSeconds(10))
    {
        bridge.Tick();
        Thread.Sleep(50);
        controlAfter = ReadControlState(bridge);
        if (controlAfter == expectState && swConfirm.Elapsed > TimeSpan.FromSeconds(3)) break;
    }
    double confirmSecs = swConfirm.Elapsed.TotalSeconds;
    Console.WriteLine($"[OK] flushed; controlState={ControlName(controlAfter)} after {confirmSecs:F1} s.");

    // 6. THE CLOCK - the reading that does not depend on the cached control state, and the only
    //    one available on a pre-STP-809 bridge. Sample the scenario clock, hold 2 s of ticks,
    //    sample it again: a PAUSED scenario does not advance it, a RUNNING one does (and under
    //    fixed-frame-run-to-complete it advances FASTER than wall time, which is why the test is
    //    "moved at all", never a rate).
    double simAfterCmd = bridge.SimTimeSeconds();
    var swHold = Stopwatch.StartNew();
    while (swHold.Elapsed < TimeSpan.FromSeconds(2)) { bridge.Tick(); Thread.Sleep(50); }
    double holdSecs      = swHold.Elapsed.TotalSeconds;
    double simAfterHold  = bridge.SimTimeSeconds();
    bool   clockReadable = simAfterCmd >= 0 && simAfterHold >= 0;
    double clockDelta    = clockReadable ? (simAfterHold - simAfterCmd) : double.NaN;
    Console.WriteLine($"[OK] clock over a {holdSecs:F1} s hold: {SimTimeText(simAfterCmd)} -> {SimTimeText(simAfterHold)} s" +
                      (clockReadable ? $" (delta {clockDelta.ToString("F3", CultureInfo.InvariantCulture)} s)" : " (NO READING)"));

    // 7. THE VERDICT - the two readings joined, and BASIS says which of them carried it.
    //    Deliberately not either one alone: the state is a CACHED value, and a clock that did
    //    not move in 2 s is not by itself "paused" (a back end mid-save has a flat clock too -
    //    VrfFacade.h on ActiveBackendCount).
    bool stateReadable    = (controlAfter == 1 || controlAfter == 2);
    bool stateAgrees      = (controlAfter == expectState);
    bool stateContradicts = stateReadable && !stateAgrees;
    bool clockAgrees      = clockReadable && (isPause ? (clockDelta <= 0.0005) : (clockDelta > 0.0005));
    string basis = (stateAgrees ? "state" : "") + (clockAgrees ? (stateAgrees ? "+clock" : "clock") : "");
    if (basis.Length == 0) basis = "none";

    string verdict;
    string why;
    int exitCode;
    if (stateContradicts)
    {
        verdict  = isPause ? "PAUSE_CONTRADICTED" : "RESUME_CONTRADICTED";
        why      = $"the back end reports {ControlName(controlAfter)} after {confirmSecs:F1} s of status rounds";
        exitCode = 1;
    }
    else if (clockAgrees && (stateAgrees || !stateReadable))
    {
        verdict  = isPause ? "PAUSE_CONFIRMED" : "RESUME_CONFIRMED";
        why      = "the scenario clock " + (isPause ? "did not advance" : "advanced") + " over the hold" +
                   (stateAgrees ? $" and controlState={ControlName(controlAfter)}"
                                : $" (control state {ControlName(controlAfter)} - clock only)");
        exitCode = 0;
    }
    else
    {
        verdict  = isPause ? "PAUSE_UNCONFIRMED" : "RESUME_UNCONFIRMED";
        why      = "the command was issued and flushed, but " +
                   (clockReadable
                        ? $"the clock delta {clockDelta.ToString("F3", CultureInfo.InvariantCulture)} s is not what a {action} predicts"
                        : "the scenario clock gave NO READING") +
                   (stateAgrees ? $" (controlState={ControlName(controlAfter)} does agree)" : "");
        exitCode = 0;
    }

    // 8. Clean stop -> resign. NEVER force-kill a joined federate (RUNBOOK sec 0).
    Console.WriteLine("[..] bridge.Stop() - resigning from the federation...");
    bridge.Stop();
    Console.WriteLine("[OK] resigned cleanly.");

    // ONE machine-readable line, last, on stdout. The runner greps "[RESULT] PauseSim" out of
    // this tool's stdout log and puts the fields in the run manifest, so THE FORMAT IS A
    // CONTRACT: "[RESULT] PauseSim " then space-separated key=value pairs, no spaces inside a
    // value. Add fields at the END; never rename or reorder one.
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
        "[RESULT] PauseSim action={0} verdict={1} basis={2} exit={3} appNumber={4} backends={5} " +
        "controlBefore={6} controlAfter={7} confirmSecs={8:F1} simTimeBefore={9} " +
        "simTimeAfterCmd={10} simTimeAfterHold={11} holdSecs={12:F1} clockDelta={13} utc={14}",
        action, verdict, basis, exitCode, appNumber, backends,
        ControlName(controlBefore), ControlName(controlAfter), confirmSecs, SimTimeText(simBefore),
        SimTimeText(simAfterCmd), SimTimeText(simAfterHold), holdSecs,
        clockReadable ? clockDelta.ToString("F3", CultureInfo.InvariantCulture) : "none",
        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)));
    Console.WriteLine($"     {verdict}: {why}.");
    if (exitCode != 0)
        Console.Error.WriteLine($"[FAIL] {verdict} - the scenario was NOT {(isPause ? "paused" : "resumed")}. " +
                                "Do NOT treat the rest of the run as if it had been.");
    else if (verdict.EndsWith("UNCONFIRMED", StringComparison.Ordinal))
        Console.WriteLine($"[WARN] {verdict} - the command went out but nothing PROVED it took effect. " +
                          "Confirm against the trace / the object consoles before scoring on it.");
    Console.WriteLine($"     Mark appNumber {appNumber} as USED in the ledger.");
    return exitCode;
}
catch (Exception e)
{
    Console.Error.WriteLine($"[FAIL] {e.GetType().Name}: {e.Message}");
    Console.Error.WriteLine(e.StackTrace);
    Console.Error.WriteLine($"[FAIL] the {action} may or may not have been applied - VERIFY before scoring. " +
                            $"Mark appNumber {appNumber} as USED regardless.");
    return 1;
}
finally
{
    bridge?.Dispose();
}
