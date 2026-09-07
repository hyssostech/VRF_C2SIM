using System;
using System.Runtime.CompilerServices;
using VrfC2Sim;   // the C++/CLI bridge (VrfBridge.dll)

namespace VrfC2SimApp;

/// <summary>
/// `VrfC2SimApp --runtime-check` (2026-09-07): the deployment smoke test. After MakRuntime has
/// prepared the process from appsettings, load the bridge and report which MAK stack bound.
/// Exit 0 = the executable, its settings and the MAK install on this machine fit together;
/// exit 1 = they do not (the message names what is missing). Starts no host, joins nothing.
/// </summary>
public static class RuntimeCheck
{
    public static int Run(MakRuntime.Outcome rt)
    {
        Console.WriteLine("runtime-check: bootstrap applied {0} item(s), {1} warning(s).", rt.Applied.Count, rt.Warnings.Count);
        try
        {
            string stack = ProbeBridge();
            Console.WriteLine("runtime-check: VrfBridge loaded; native stack = " + stack);
            Console.WriteLine(rt.Warnings.Count == 0 ? "runtime-check: OK" : "runtime-check: OK with warnings (see above)");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("runtime-check: FAILED to load the VR-Forces bridge - " + e.GetType().Name + ": " + e.Message);
            Console.Error.WriteLine("runtime-check: the MAK bin directories must be reachable (Vrf:VrfHome / VrLinkHome / RtiHome " +
                                    "in appsettings, or on the process PATH) and match the bridge build (5.2d / VR-Link 5.10 / RTI 5.0.1).");
            return 1;
        }
    }

    // Kept out of Run's own body so the bridge assembly is not resolved before the bootstrap ran.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ProbeBridge() => VrfBridge.NativeStackInfo();
}
