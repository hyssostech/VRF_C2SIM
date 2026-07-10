using VrfC2Sim;

// Phase 2 runtime-load smoke: net10 -> C++/CLI(netcore) VrfBridge -> native VrfFacade.
// Constructing VrfBridge forces VrfBridge.dll, Ijwhost.dll, and every MAK native DLL
// the facade links (vrfcontrol, vl, vrfmsgs, ...) to LOAD and run static init in-process.
// We deliberately do NOT call Start() - that needs live VR-Forces + a federation.
// Construction alone proves the IJW seam and the native facade ctor.

Console.WriteLine("=== VrfBridge runtime-load smoke (construct + dispose, no Start) ===\n");

try
{
    using (var bridge = new VrfBridge())
    {
        Console.WriteLine("[PASS] new VrfBridge() - IJW load + native vrf::VrfFacade constructed in-process");
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
    Console.WriteLine("SMOKE PASSED - the managed bridge loads and the native facade lives in-process under net10.");
    return 0;
}
catch (Exception e)
{
    Console.WriteLine($"[FAIL] {e.GetType().Name}: {e.Message}");
    Console.WriteLine(e.StackTrace);
    return 1;
}
