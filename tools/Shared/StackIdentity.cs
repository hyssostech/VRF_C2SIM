using System;
using VrfC2Sim;

namespace VrfC2Sim.Tools;

// Stack-aware federation identity for the bridge tools (2026-09-03, DIFF sec H).
//
// WHY: the tools' 5.0.2 constants (Federation CWIX-2024, RPR_FOM_v2.0_1516-2010.xml,
// the 3 MAK-*-6/7/2 modules) describe a federation that DOES NOT EXIST on the 5.2d
// stack: 5.2 identity lives in appData\settings\connections\MAK-ONE-2025-Config.xml
// (execName MAK-ONE-2025, 17 FOM modules incl. NETN; DIFF row A2), which VR-Link
// ALWAYS loads, and config FOM modules are ADDITIVE (row A9) - so submitting the old
// list would join with VRFExt-6 AND VRFExt-12 together, or fail on missing files.
//
// THE RULE: which stack is bound is a RUNTIME fact of the loaded native DLLs, read
// from VrfBridge.NativeStackInfo() ("<5.2|5.0.2>|<vrfcontrol.dll path>") - never
// from a build flag the deploy could contradict. On 5.2 the tool joins the
// CONFIG-FILE way: Federation/FedFileName EMPTY, FomModules EMPTY (VrfFacade::Start
// only pushes --execName/--fedFileName when non-empty). An explicit federation
// argument still overrides execName on either stack.
//
// LAUNCH ENV a 5.2 tool needs (PREREG_52_LAUNCH_2026-09-03.md): cwd = the 5.2d
// bin64 (so ../appData resolves the connection config), 5.2 PATH prefix,
// MAK_RTIDIR/RTI_RID_FILE = the SAME rid as the sim (config/rid-461-
// ridconfigured.mtl) and RTI_ASSISTANT_DISABLE set - federates that do not share
// the rid do not share a connection.
public static class StackIdentity
{
    /// <summary>"5.2" or "5.0.2" - the stack the loaded native DLLs belong to.</summary>
    public static string Stack()
    {
        string info = VrfBridge.NativeStackInfo() ?? "";
        int bar = info.IndexOf('|');
        return bar > 0 ? info.Substring(0, bar) : info;
    }

    public static bool Is52() => Stack() == "5.2";

    /// <summary>
    /// Fill cfg's federation identity for the bound stack. federationArg is the
    /// tool's positional federation argument, or null/"" when the user did not pass
    /// one. Returns a one-line description for the tool's banner.
    /// </summary>
    public static string Apply(StartupConfig cfg, string federationArg)
    {
        bool explicitFed = !string.IsNullOrWhiteSpace(federationArg);
        if (Is52())
        {
            // Config-file join: identity comes from MAK-ONE-2025-Config.xml.
            cfg.Federation  = explicitFed ? federationArg : "";
            cfg.FedFileName = "";
            cfg.FomModules.Clear();
            return explicitFed
                ? $"stack=5.2  federation={federationArg} (explicit override; FOM identity still from the connection config)"
                : "stack=5.2  federation=(from connection config; execName MAK-ONE-2025 expected)";
        }
        cfg.Federation  = explicitFed ? federationArg : "CWIX-2024";
        cfg.FedFileName = "RPR_FOM_v2.0_1516-2010.xml";
        cfg.FomModules.Clear();
        cfg.FomModules.Add("MAK-VRFExt-6_evolved.xml");
        cfg.FomModules.Add("MAK-DIGuy-7_evolved.xml");
        cfg.FomModules.Add("MAK-LgrControl-2_evolved.xml");
        return $"stack=5.0.2  federation={cfg.Federation}";
    }
}
