using VrfBridge;

// Phase 2 spike #2 runner: net10 -> C++/CLI(netcore) -> native boost-heavy vrfcontrol/vl.
// The bridge dll and all MAK native DLLs (vrfcontrol.dll, vl.dll, boost, ...) must load.

Console.WriteLine("=== Phase 2 spike #2: net10 -> C++/CLI -> native vrfcontrol/vl (boost-heavy) ===\n");

bool ok = true;

try
{
    // Proof 1: pure vrfmsgs free function (forces vrfmsgs.dll load).
    // Control-type enum values from vrfmsgs; any valid int returns a name string.
    string name0 = VrfControlProbe.ControlTypeName(0);
    string name1 = VrfControlProbe.ControlTypeName(1);
    Console.WriteLine($"vrfmsgs DtControlTypeString(0) = \"{name0}\"");
    Console.WriteLine($"vrfmsgs DtControlTypeString(1) = \"{name1}\"");
    bool msgsOk = name0 != null && name1 != null;
    Console.WriteLine($"[{(msgsOk ? "PASS" : "FAIL")}] vrfmsgs.dll loaded and callable\n");
    ok &= msgsOk;

    // Proof 2: construct a real vrfcontrol object. This is the decisive one - it forces
    // vrfcontrol.dll and every boost/vl dependency to load and run static init.
    bool ctorOk = VrfControlProbe.ConstructBackendSelector();
    Console.WriteLine($"[{(ctorOk ? "PASS" : "FAIL")}] constructed + destroyed DtAutomaticBackendSelector (vrfcontrol)");
    ok &= ctorOk;
}
catch (Exception e)
{
    Console.WriteLine($"[FAIL] threw {e.GetType().Name}: {e.Message}");
    ok = false;
}

Console.WriteLine();
Console.WriteLine(ok
    ? "SPIKE #2 PASSED - boost-heavy vrfcontrol/vl links, loads, and runs under /clr:netcore.\n" +
      "The in-process C++/CLI bridge over DtVrfRemoteController is viable."
    : "SPIKE #2 FAILED - see above. Pivot the facade to out-of-process.");
return ok ? 0 : 1;
