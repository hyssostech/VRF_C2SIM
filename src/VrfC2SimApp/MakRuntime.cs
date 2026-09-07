using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace VrfC2SimApp;

/// <summary>
/// In-process MAK runtime bootstrap (DEMO_READINESS row 6, 2026-09-07). The C++/CLI bridge and
/// the MAK libraries it links (vrfcontrol, vrlink, the RTI) are resolved BY NAME through the
/// process PATH when the bridge assembly first loads, and the RTI reads its posture from
/// environment variables. Until now a PowerShell start script (scripts/StartInterface52.ps1) or
/// the test runner set all of that around the process. This class does the same thing from the
/// app's own settings so the deliverable is the executable plus appsettings, no script:
///
///   Vrf:VrfHome     e.g. C:\MAK\vrforces5.2d   -> PATH += VrfHome\bin64;  MAK_VRFDIR
///   Vrf:VrLinkHome  e.g. C:\MAK\vrlink5.10     -> PATH += VrLinkHome\bin64; MAK_VRLDIR
///   Vrf:RtiHome     e.g. C:\MAK\makRti5.0.1    -> PATH += RtiHome\bin;     MAK_RTIDIR
///   Vrf:RidFile     the RTI rid (relative paths resolve against the app directory) -> RTI_RID_FILE
///   Vrf:RtiAssistantDisable (bool)              -> RTI_ASSISTANT_DISABLE=1
///
/// Rules: a key that is empty is left alone (the harness keeps setting the environment per
/// process, and an unconfigured base appsettings changes nothing); a CONFIGURED value overrides
/// a process variable that differs (logged - stale machine-level MAK variables are the trap on
/// developer boxes); a configured directory that does not exist is a loud warning, not a silent
/// fallback (the DLL-name-binding trap of VRF_5.2_MIGRATION_DIFF sec H). Must run BEFORE anything touches a
/// bridge type - Program.cs calls it right after the configuration is built.
/// </summary>
public static class MakRuntime
{
    public sealed record Outcome(List<string> Applied, List<string> Warnings);

    public static Outcome Bootstrap(IConfiguration config, string appDirectory)
    {
        var applied = new List<string>();
        var warnings = new List<string>();
        var vrf = config.GetSection("Vrf");

        string vrfHome = (vrf["VrfHome"] ?? "").Trim();
        string vrlHome = (vrf["VrLinkHome"] ?? "").Trim();
        string rtiHome = (vrf["RtiHome"] ?? "").Trim();
        string ridFile = (vrf["RidFile"] ?? "").Trim();
        bool assistantOff = string.Equals((vrf["RtiAssistantDisable"] ?? "").Trim(), "true", StringComparison.OrdinalIgnoreCase);

        var pathPrefix = new List<string>();
        void AddBin(string home, string sub, string envVar)
        {
            if (home.Length == 0) return;
            string bin = Path.Combine(home, sub);
            if (!Directory.Exists(bin))
            {
                warnings.Add($"{envVar}: configured MAK root '{home}' has no '{sub}' directory - the bridge will bind " +
                             "whatever the process PATH already offers (check the install).");
                return;
            }
            pathPrefix.Add(bin);
            SetIfUnset(envVar, home, applied);
        }
        AddBin(vrfHome, "bin64", "MAK_VRFDIR");
        AddBin(vrlHome, "bin64", "MAK_VRLDIR");
        AddBin(rtiHome, "bin", "MAK_RTIDIR");

        if (pathPrefix.Count > 0)
        {
            string current = Environment.GetEnvironmentVariable("PATH") ?? "";
            string[] parts = current.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var missing = pathPrefix.FindAll(p => !Array.Exists(parts,
                e => string.Equals(e.TrimEnd('\\'), p.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)));
            if (missing.Count > 0)
            {
                Environment.SetEnvironmentVariable("PATH", string.Join(";", missing) + ";" + current);
                applied.Add("PATH prefixed with " + string.Join(";", missing));
            }
            else applied.Add("PATH already carries the MAK bin directories");
        }

        if (ridFile.Length > 0)
        {
            string rid = Path.IsPathRooted(ridFile) ? ridFile : Path.GetFullPath(Path.Combine(appDirectory, ridFile));
            if (File.Exists(rid)) SetIfUnset("RTI_RID_FILE", rid, applied);
            else warnings.Add($"RTI_RID_FILE: configured rid '{ridFile}' not found at '{rid}' - the RTI uses its own default posture.");
        }
        if (assistantOff) SetIfUnset("RTI_ASSISTANT_DISABLE", "1", applied);
        return new Outcome(applied, warnings);
    }

    // A CONFIGURED value wins over the process environment (2026-09-07, found by --runtime-check on
    // the dev box: a stale machine-level MAK_VRLDIR=vrlink5.8 would otherwise ride along under a
    // 5.10 PATH). The settings file beside the exe states what the bridge was built for; the harness
    // leaves these keys EMPTY in the base appsettings, so its per-process environment is untouched.
    private static void SetIfUnset(string name, string value, List<string> applied)
    {
        string existing = Environment.GetEnvironmentVariable(name);
        if (string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))
        {
            applied.Add($"{name} already = '{value}'");
            return;
        }
        Environment.SetEnvironmentVariable(name, value);
        applied.Add(string.IsNullOrEmpty(existing)
            ? $"{name}={value}"
            : $"{name}={value} (overriding the process value '{existing}')");
    }
}
