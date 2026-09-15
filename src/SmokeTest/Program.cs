using System.Linq;
using System.Runtime.CompilerServices;
using VrfC2Sim;
using VrfC2Sim.Tools;

// Phase 2 runtime-load smoke: net10 -> C++/CLI(netcore) VrfBridge -> native VrfFacade.
// Constructing VrfBridge forces VrfBridge.dll, Ijwhost.dll, and every MAK native DLL
// the facade links (vrfcontrol, vl, vrfmsgs, ...) to LOAD and run static init in-process.
// We deliberately do NOT call Start() - that needs live VR-Forces + a federation.
// Construction alone proves the IJW seam and the native facade ctor.
//
// STACK-AWARE (2026-09-15): built for either MAK stack via -p:BridgeConfig
// (Release = 5.0.2, Release-5.2 = 5.2d), and it REPORTS which one actually bound,
// read from the loaded native DLLs via tools/Shared/StackIdentity.cs - never from a
// build flag the deploy could contradict. SmokeTest has NO federation identity and NO
// appNumber to get wrong: it never calls Start(), so the 5.0.2 CWIX-2024 constants the
// other three tools carried were never in this file.
//
// LAUNCH ENV: the bound stack's bin64 on PATH (5.2 -> C:\MAK\vrforces5.2d\bin64;
// C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin) so the native DLLs resolve. No RTI
// connection, no licence and no running VR-Forces are needed - nothing is joined.

// The bound native stack, read from the LOADED DLLs. Same probe as VrfC2SimApp
// --runtime-check; NoInlining so the bridge assembly is resolved only when it is called.
[MethodImpl(MethodImplOptions.NoInlining)]
static string NativeStackLine()
{
    try { return "native stack = " + VrfBridge.NativeStackInfo() + "  (stack=" + StackIdentity.Stack() + ")"; }
    catch (Exception e) { return "native stack = UNAVAILABLE (" + e.GetType().Name + ": " + e.Message + ")"; }
}

// --help / --dry-run: the runner's self-test convention - print the plan and the bound
// stack, construct nothing, exit 0. (SmokeTest joins nothing in either mode; --dry-run
// is here so an operator carrying the habit from ResetVrf/SetSimRate is not surprised.)
if (args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
               || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)
               || string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine("=== VrfBridge runtime-load smoke (construct + dispose, no Start) ===");
    Console.WriteLine("    " + NativeStackLine());
    Console.WriteLine();
    Console.WriteLine("usage: SmokeTest.exe             # construct + dispose the bridge; no Start(), no join");
    Console.WriteLine("       SmokeTest.exe --help      # this text plus the bound native stack; constructs nothing");
    Console.WriteLine();
    Console.WriteLine("  SmokeTest takes NO arguments: no appNumber (it never joins, so it can never burn");
    Console.WriteLine("  one) and no federation (it has no identity to get wrong).");
    Console.WriteLine();
    Console.WriteLine("  PLAN: new VrfBridge() -> BackendCount() -> subscribe ObjectCreated / TaskCompleted /");
    Console.WriteLine("        TextReport / ScenarioClosed -> Dispose(). Exit 0 = the IJW seam and the native");
    Console.WriteLine("        facade ctor work against the bound stack.");
    Console.WriteLine("  THIS INVOCATION CONSTRUCTED NOTHING.");
    return 0;
}

var unknown = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (unknown.Length > 0)
{
    Console.Error.WriteLine($"[FAIL] unknown option(s): {string.Join(" ", unknown)}. " +
                            "SmokeTest accepts --help and --dry-run only.");
    return 2;
}

Console.WriteLine("=== VrfBridge runtime-load smoke (construct + dispose, no Start) ===\n");

try
{
    using (var bridge = new VrfBridge())
    {
        Console.WriteLine("[PASS] new VrfBridge() - IJW load + native vrf::VrfFacade constructed in-process");
        // WHICH stack bound is the load-bearing fact for a side-by-side 5.0.2 / 5.2d install:
        // a Release-5.2 build that somehow resolved the 5.0.2 vrfcontrol.dll would pass every
        // line below and still be the wrong binary. Print it, do not infer it.
        Console.WriteLine($"       {NativeStackLine()}");
        Console.WriteLine($"       BackendCount() before Start = {bridge.BackendCount()} (expected 0)");

        // Subscribe to the inbound events (slice 2). We cannot FIRE them without a
        // live VR-Forces + a running scenario, but subscribing proves the managed
        // event surface is consumable and the gcroot callback thunks are wired.
        bridge.ObjectCreated  += (s, e) => Console.WriteLine($"       ObjectCreated: {e.Name} -> {e.Uuid}");
        bridge.TaskCompleted  += (s, e) => Console.WriteLine($"       TaskCompleted: {e.UnitMarking} / {e.TaskType}");
        bridge.TextReport     += (s, e) => Console.WriteLine($"       TextReport: {e.Text}");
        bridge.ScenarioClosed += (s, e) => Console.WriteLine("       ScenarioClosed");
        Console.WriteLine("[PASS] subscribed to ObjectCreated / TaskCompleted / TextReport / ScenarioClosed");
    }
    Console.WriteLine("[PASS] dispose - native facade teardown (~VrfFacade -> Stop) ran without fault\n");
    Console.WriteLine($"SMOKE PASSED on stack {StackIdentity.Stack()} - the managed bridge loads and the " +
                      "native facade lives in-process under net10.");
    return 0;
}
catch (Exception e)
{
    Console.WriteLine($"[FAIL] {e.GetType().Name}: {e.Message}");
    Console.WriteLine(e.StackTrace);
    return 1;
}
