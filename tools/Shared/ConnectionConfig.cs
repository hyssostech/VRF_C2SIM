using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using VrfC2Sim;

namespace VrfC2Sim.Tools;

// Shared VR-Link CONNECTION CONFIG resolution for every bridge consumer that calls
// VrfBridge.Start() (2026-09-15, V6 harvest).
//
// WHY THIS EXISTS. On the 5.2 stack the federation identity - execName MAK-ONE-2025 and the
// 17 NETN/MAK FOM modules - lives in a connection config XML, not in our constants
// (tools/Shared/StackIdentity.cs). src/VrfC2SimApp names that file explicitly
// (Vrf__ConnectionConfigFile); every tool left StartupConfig.ConnectionConfigFile NULL and
// took the vendor default instead.
//
// WHAT THE VENDOR DEFAULT ACTUALLY IS - measured, not assumed (scratchpad v6harvest/probe:
// a native exe that prints the resolver's answer and joins nothing):
//
//     DtDefaultConfigFile            = "MAK-ONE-2025-Config.xml"
//                                      (vrlink5.10\include\vlpi\exerciseConnConfig.h:24)
//     connectionsSettingsDirectory() = "..\appData\settings\connections"    <- RELATIVE
//     resolved                       = "..\appData\settings\connections\MAK-ONE-2025-Config.xml"
//
// It is RELATIVE TO THE PROCESS WORKING DIRECTORY. MAK_VRFDIR, MAK_VRFDIR64, MAK_VRLDIR and
// MAK_RTIDIR do NOT enter into it (the probe varied them and the answer never moved; the
// names do not occur in any 5.2d / vrlink5.10 binary the bridge loads). So the default is
// correct ONLY while the process cwd is the 5.2d bin64, and silently wrong everywhere else:
// with the wrong cwd the vendor prints
//     "Unable to load configuration file: ..\appData\settings\connections\MAK-ONE-2025-Config.xml"
// and carries on with BUILT-IN defaults - a different execName and no FOM modules - so the
// federate joins SOMETHING and then finds nothing. That is a false green of exactly the shape
// this repo has been bitten by six times (lessons-false-greens), and a cwd is not a contract.
//
// THE RULE THIS ENCODES. Resolve the file EXPLICITLY, in this order:
//   1. --config <path>                   the operator names it; wins over everything
//   2. env Vrf__ConnectionConfigFile     the same variable the runner gives the app
//   3. the BOUND STACK's own tree        <VrfRoot>\appData\settings\connections\<file>, where
//                                        VrfRoot is the parent of the directory the LOADED
//                                        vrfcontrol.dll lives in (VrfBridge.NativeStackInfo()
//                                        - a runtime fact, never a build flag; the same rule
//                                        StackIdentity follows).
// Then PRINT the resolved path and whether it exists BEFORE Start(), and REFUSE to Start when
// it does not exist. No cwd is trusted and no silent fallback is taken.
//
// 5.0.2: the bridge IGNORES ConnectionConfigFile (VrfBridge.cpp:276-277 sits inside the 5.2
// path; identity there is CWIX-2024 + the 3 MAK modules). On that stack this helper resolves
// nothing, applies nothing and never refuses - it only says so.
internal static class ConnectionConfig
{
    /// <summary>The option every bridge consumer accepts.</summary>
    public const string Flag = "--config";

    /// <summary>The environment variable the runner already sets for the app.</summary>
    public const string EnvVar = "Vrf__ConnectionConfigFile";

    /// <summary>DtDefaultConfigFile - vrlink5.10\include\vlpi\exerciseConnConfig.h:24.</summary>
    public const string FileName = "MAK-ONE-2025-Config.xml";

    /// <summary>Where a VR-Forces install keeps it, relative to the install root.</summary>
    public const string RelativeDir = @"appData\settings\connections";

    /// <summary>One resolution attempt. Produced by Resolve(); never constructed by a caller.</summary>
    public sealed class Resolution
    {
        /// <summary>The resolved path; "" when nothing resolved at all.</summary>
        public string Path { get; internal set; } = "";

        /// <summary>Provenance: which of the three rules produced Path.</summary>
        public string Source { get; internal set; } = "";

        /// <summary>File.Exists(Path) at resolution time.</summary>
        public bool Exists { get; internal set; }

        /// <summary>True only on the 5.2 stack; on 5.0.2 the bridge ignores the field.</summary>
        public bool Applies { get; internal set; }

        /// <summary>
        /// False ONLY when the stack needs the file and the file is not there. A consumer that
        /// gets false must NOT call Start(): the vendor would fall back to built-in defaults
        /// and join a federation nobody asked for.
        /// </summary>
        public bool Ok => !Applies || Exists;

        /// <summary>The one line every tool prints in its banner, before Start().</summary>
        public string Banner => Applies
            ? "connection config = " + (Path.Length == 0 ? "(UNRESOLVED)" : Path)
              + "  exists=" + (Exists ? "YES" : "NO") + "  source=" + Source
            : "connection config = n/a on stack 5.0.2 (identity is CWIX-2024 + 3 MAK modules; "
              + "the bridge ignores the file)";

        /// <summary>What to print on stderr when Ok is false.</summary>
        public string RefusalText =>
            "[FAIL] the VR-Link connection config was not found, so NOTHING was joined." + Environment.NewLine +
            "       resolved: " + (Path.Length == 0
                ? "(nothing - the loaded stack did not report a vrfcontrol.dll path)" : Path) + Environment.NewLine +
            "       source  : " + Source + Environment.NewLine +
            "       Without it VR-Link falls back to BUILT-IN defaults (a different execName, no FOM" + Environment.NewLine +
            "       modules) and joins a federation the simulator is not in: the tool would report a" + Environment.NewLine +
            "       successful join and then see nothing. Name the file explicitly:" + Environment.NewLine +
            "         " + Flag + " <path>\\" + FileName + "        (or set " + EnvVar + ")" + Environment.NewLine +
            "       The shipped one is <VrfRoot>\\" + RelativeDir + "\\" + FileName + ".";

        /// <summary>
        /// Whether ApplyTo will actually write Path into a StartupConfig. Exposed as a pure
        /// predicate so SelfTest can pin the rule WITHOUT constructing a StartupConfig -
        /// that type lives in the mixed-mode VrfBridge assembly, and touching it would make
        /// the suite need the MAK native stack on PATH. An offline suite that needs the
        /// runtime it is meant to be independent of is not an offline suite.
        /// </summary>
        public bool WillApply => Applies && Exists;

        /// <summary>Set StartupConfig.ConnectionConfigFile when - and only when - it applies.</summary>
        public void ApplyTo(StartupConfig cfg)
        {
            if (cfg == null) return;
            if (WillApply) cfg.ConnectionConfigFile = Path;
        }
    }

    // -- flag extraction ---------------------------------------------------------
    // Self-contained (NOT ToolArgs.TryTakeOptionValue) so a csproj can link this file without
    // also linking ToolArgs.cs: tools/ResetVrf and tools/CreateTaskAgg link StackIdentity only.
    // Same contract as ToolArgs': true when the option is absent, or present exactly once with
    // a usable value; false, with 'problem' set, for the three operator errors.
    public static bool TryTakeFlag(string[] args, out string[] rest, out string value, out string problem)
    {
        args ??= Array.Empty<string>();
        rest = args;
        value = null;
        problem = null;

        var kept = new List<string>(args.Length);
        bool seen = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], Flag, StringComparison.Ordinal)) { kept.Add(args[i]); continue; }
            if (seen) { problem = Flag + " was given more than once."; return false; }
            seen = true;
            if (i + 1 >= args.Length) { problem = Flag + " requires a value (the connection config XML)."; return false; }
            string v = args[i + 1];
            if (v.StartsWith("--", StringComparison.Ordinal))
            {
                problem = Flag + " requires a value; got the option '" + v + "'.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(v)) { problem = Flag + " was given an empty value."; return false; }
            value = v;
            i++;    // consume the value too
        }
        rest = kept.ToArray();
        return true;
    }

    // -- resolution --------------------------------------------------------------

    /// <summary>
    /// The live resolution: --config, then Vrf__ConnectionConfigFile, then the bound stack's
    /// own tree. Touches the bridge (NativeStackInfo) and the file system.
    /// </summary>
    public static Resolution Resolve(string explicitArg)
        => Resolve(explicitArg,
                   Environment.GetEnvironmentVariable(EnvVar),
                   VrfRootFromLoadedStack(),
                   File.Exists,
                   Is52());

    /// <summary>
    /// The PURE resolution - every input injected, no bridge and no file system. This is the
    /// overload SelfTest exercises; the live one above is a wrapper over it, so the rule and
    /// its test cannot drift apart.
    /// </summary>
    public static Resolution Resolve(string explicitArg, string envValue, string vrfRoot,
                                     Func<string, bool> fileExists, bool applies)
    {
        var r = new Resolution { Applies = applies };

        if (!string.IsNullOrWhiteSpace(explicitArg))
        {
            r.Path = explicitArg.Trim();
            r.Source = Flag;
        }
        else if (!string.IsNullOrWhiteSpace(envValue))
        {
            r.Path = envValue.Trim();
            r.Source = "env " + EnvVar;
        }
        else if (!string.IsNullOrWhiteSpace(vrfRoot))
        {
            r.Path = System.IO.Path.Combine(vrfRoot.Trim(), RelativeDir, FileName);
            r.Source = "bound stack (loaded vrfcontrol.dll -> " + vrfRoot.Trim() + ")";
        }
        else
        {
            r.Path = "";
            r.Source = "UNRESOLVED - no " + Flag + ", no " + EnvVar
                     + ", and the loaded stack did not report a vrfcontrol.dll path";
        }

        r.Exists = r.Path.Length > 0 && fileExists != null && fileExists(r.Path);
        return r;
    }

    /// <summary>
    /// The VR-Forces install root behind the LOADED native stack, or null when it cannot be
    /// read. NativeStackInfo() is "stack|full path to vrfcontrol.dll", so the root is the
    /// parent of the DLL's directory (...\vrforces5.2d\bin64\vrfcontrol.dll -> ...\vrforces5.2d).
    /// </summary>
    public static string VrfRootFromLoadedStack()
    {
        try { return ParseVrfRoot(NativeStackInfoSafe()); }
        catch { return null; }
    }

    /// <summary>Pure half of VrfRootFromLoadedStack, so it is testable without the bridge.</summary>
    public static string ParseVrfRoot(string nativeStackInfo)
    {
        if (string.IsNullOrWhiteSpace(nativeStackInfo)) return null;
        int bar = nativeStackInfo.IndexOf('|');
        if (bar < 0 || bar + 1 >= nativeStackInfo.Length) return null;
        string dll = nativeStackInfo.Substring(bar + 1).Trim();
        // "(vrfcontrol.dll not loaded)" and "(GetModuleFileName failed)" are the facade's own
        // sentinels (VrfFacade.cpp NativeStackInfo) - not paths, and never to be made into one.
        if (dll.Length == 0 || dll.StartsWith("(", StringComparison.Ordinal)) return null;
        if (!System.IO.Path.IsPathRooted(dll)) return null;
        string binDir = System.IO.Path.GetDirectoryName(dll);
        if (string.IsNullOrEmpty(binDir)) return null;
        string root = System.IO.Path.GetDirectoryName(binDir);
        return string.IsNullOrEmpty(root) ? null : root;
    }

    // NoInlining so the bridge assembly is resolved only when these are actually called - the
    // same rule every tool's NativeStackLine() follows.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string NativeStackInfoSafe()
    {
        try { return VrfBridge.NativeStackInfo() ?? ""; }
        catch { return ""; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Is52()
    {
        try { return StackIdentity.Is52(); }
        catch { return false; }
    }

    // -- offline self-test -------------------------------------------------------
    // The CHECKS are pure: no RTI, no federation, no file system, no network, and nothing in
    // here constructs a bridge type. Run it with
    //   ResetVrf.exe --config-selftest
    // It still needs the 5.2 PATH, and that is a property of the HOST, not of this suite: the
    // tool is a top-level-statement program, its Main names VrfBridge types further down, and
    // the JIT resolves the whole method before the first statement runs. Returns the number of
    // FAILED checks (0 = pass), which the caller uses as its exit code - so a broken resolution
    // rule is a non-zero exit, not a line to be read.
    public static int SelfTest(TextWriter w)
    {
        int ok = 0, fail = 0;
        void Check(string name, bool condition, string detail)
        {
            if (condition) { ok++; w.WriteLine("  ok   " + name); }
            else { fail++; w.WriteLine("  FAIL " + name + "  -> " + detail); }
        }

        const string root = @"C:\MAK\vrforces5.2d";
        string derived = System.IO.Path.Combine(root, RelativeDir, FileName);
        const string argPath = @"D:\given\by\arg.xml";
        const string envPath = @"D:\given\by\env.xml";
        var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { argPath, envPath, derived };
        Func<string, bool> all = p => present.Contains(p);
        Func<string, bool> none = p => false;
        string[] rest; string val; string problem;

        w.WriteLine("=== ConnectionConfig.SelfTest - the resolution rule, offline ===");

        var a = Resolve(argPath, envPath, root, all, true);
        Check("arg wins over env and over the stack default",
              a.Path == argPath && a.Source == Flag, a.Path + " / " + a.Source);

        var b = Resolve(null, envPath, root, all, true);
        Check("env wins over the stack default",
              b.Path == envPath && b.Source.Contains(EnvVar), b.Path + " / " + b.Source);

        var c = Resolve(null, null, root, all, true);
        Check("stack default used when neither is given", c.Path == derived, c.Path);

        var d = Resolve("   ", "  ", root, all, true);
        Check("whitespace-only arg/env count as absent", d.Path == derived, d.Path);

        var e = Resolve(null, null, root, none, true);
        Check("missing file -> not Ok (the consumer must refuse to Start)",
              !e.Ok && !e.Exists, "Ok=" + e.Ok);

        var f = Resolve(null, null, root, all, true);
        Check("present file -> Ok", f.Ok && f.Exists, "Ok=" + f.Ok);

        var g = Resolve(null, null, null, none, true);
        Check("no stack root and no override -> UNRESOLVED and not Ok",
              g.Path.Length == 0 && !g.Ok, g.Path + " Ok=" + g.Ok);

        var h = Resolve(null, null, root, none, false);
        Check("stack 5.0.2 -> never refuses (the bridge ignores the file)", h.Ok, "Ok=" + h.Ok);

        Check("stack 5.0.2 -> ApplyTo writes NOTHING (the bridge ignores the field)",
              !h.WillApply, "WillApply=" + h.WillApply);

        Check("stack 5.2 + present -> ApplyTo writes the resolved path",
              f.WillApply && f.Path == derived, "WillApply=" + f.WillApply + " " + f.Path);

        Check("stack 5.2 + missing -> ApplyTo writes NOTHING",
              !e.WillApply, "WillApply=" + e.WillApply);

        Check("derived path is <root>\\appData\\settings\\connections\\" + FileName,
              derived == @"C:\MAK\vrforces5.2d\appData\settings\connections\MAK-ONE-2025-Config.xml",
              derived);

        Check("ParseVrfRoot of a real NativeStackInfo string",
              ParseVrfRoot(@"5.2|C:\MAK\vrforces5.2d\bin64\vrfcontrol.dll") == root,
              "" + ParseVrfRoot(@"5.2|C:\MAK\vrforces5.2d\bin64\vrfcontrol.dll"));

        Check("ParseVrfRoot of the not-loaded sentinel is null",
              ParseVrfRoot("5.2|(vrfcontrol.dll not loaded)") == null, "not null");

        Check("ParseVrfRoot of a bar-less string is null", ParseVrfRoot("5.2") == null, "not null");

        Check("TryTakeFlag removes BOTH tokens",
              TryTakeFlag(new[] { "4400", Flag, "x.xml", "--dry-run" }, out rest, out val, out problem)
              && val == "x.xml" && rest.Length == 2 && rest[0] == "4400" && rest[1] == "--dry-run",
              "val=" + val + " rest=" + (rest == null ? "null" : string.Join(",", rest)));

        Check("TryTakeFlag absent -> true, null value, args unchanged",
              TryTakeFlag(new[] { "4400" }, out rest, out val, out problem) && val == null && rest.Length == 1,
              "val=" + val);

        Check("TryTakeFlag with no value -> usage error",
              !TryTakeFlag(new[] { "4400", Flag }, out rest, out val, out problem) && problem != null,
              "problem=" + problem);

        Check("TryTakeFlag followed by an option -> usage error",
              !TryTakeFlag(new[] { Flag, "--dry-run" }, out rest, out val, out problem) && problem != null,
              "problem=" + problem);

        Check("TryTakeFlag twice -> usage error",
              !TryTakeFlag(new[] { Flag, "a.xml", Flag, "b.xml" }, out rest, out val, out problem) && problem != null,
              "problem=" + problem);

        w.WriteLine("=== ConnectionConfig.SelfTest: " + ok + " ok / " + fail + " fail ===");
        return fail;
    }
}
