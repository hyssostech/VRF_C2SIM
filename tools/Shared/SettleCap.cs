using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VrfC2Sim.Tools;

// The BACK-END SETTLE CAP, as an option instead of a constant (2026-09-15, V6c arm A1).
//
// WHY. Every bridge tool that joins waits a hard-coded 15 s for a VR-Forces back end and then
// refuses. That number was never measured against anything: BackendCount is fed ONLY by
// VR-Forces status MESSAGES through DtVrfBackendListener (vrfcontrol/vrfBackendListener.h:73-78,
// :178-186), the listener asks for one exactly ONCE from its constructor (sendRequest, protected,
// :265-268) and there is no public re-request, and NO header or vendor guide states how often a
// back end emits status unprompted. In V6 and V6b every late joiner sat at BackendCount=0 for the
// whole 15 s while VrfC2SimApp - which joins seconds after PushInit - had it in 0.1 s
// (docs/experiments/V6_LIVE_JOIN_GATE_2026-09-15.md).
//
// The surviving explanation is a cadence longer than 15 s, or no unprompted cadence at all. Those
// two are told apart by ONE measurement: wait much longer and see whether it ever arrives, and at
// what t. That is arm A1 of PREREG_V6C_LATE_JOINER_2026-09-15.md, and it needs the cap to be an
// argument.
//
// THE DEFAULT IS UNCHANGED (15 s). A tool invoked without --settle-secs behaves exactly as it did
// before this file existed; nothing about the refusal rule changes. The option only lets an
// operator (or a prereg arm) buy more patience deliberately, and the chosen value is PRINTED so a
// log never has to be guessed at.
internal static class SettleCap
{
    /// <summary>The option name.</summary>
    public const string Flag = "--settle-secs";

    /// <summary>The historical constant every tool used, and still the default.</summary>
    public const int DefaultSeconds = 15;

    /// <summary>Accepted range. 3600 s is an hour - past that, the operator wants a different tool.</summary>
    public const int MinSeconds = 1;
    public const int MaxSeconds = 3600;

    /// <summary>
    /// Take "--settle-secs N" out of args. Returns true when the option is ABSENT (seconds =
    /// DefaultSeconds, rest unchanged) or present exactly once with a usable value; false, with
    /// 'problem' set, for the operator errors that must not be guessed at - repeated, missing
    /// value, an option where the value belongs, a non-integer, or out of range.
    /// </summary>
    public static bool TryTakeFlag(string[] args, out string[] rest, out int seconds, out string problem)
    {
        args ??= Array.Empty<string>();
        rest = args;
        seconds = DefaultSeconds;
        problem = null;

        var kept = new List<string>(args.Length);
        bool seen = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], Flag, StringComparison.Ordinal)) { kept.Add(args[i]); continue; }
            if (seen) { problem = Flag + " was given more than once."; return false; }
            seen = true;
            if (i + 1 >= args.Length) { problem = Flag + " requires a value (seconds)."; return false; }
            string v = args[i + 1];
            if (v.StartsWith("--", StringComparison.Ordinal))
            {
                problem = Flag + " requires a value; got the option '" + v + "'.";
                return false;
            }
            if (!int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
            {
                problem = Flag + " '" + v + "' is not an integer.";
                return false;
            }
            if (n < MinSeconds || n > MaxSeconds)
            {
                problem = Flag + " " + n.ToString(CultureInfo.InvariantCulture)
                        + " is out of range (expected " + MinSeconds + ".." + MaxSeconds + ").";
                return false;
            }
            seconds = n;
            i++;    // consume the value too
        }
        rest = kept.ToArray();
        return true;
    }

    /// <summary>The banner line, so a log always says which cap produced its verdict.</summary>
    public static string Banner(int seconds) =>
        "back-end settle cap = " + seconds.ToString(CultureInfo.InvariantCulture) + " s"
        + (seconds == DefaultSeconds ? " (default)" : " (" + Flag + ")");

    // Offline suite. Pure: no bridge, no RTI, no file system, no network.
    // Returns the number of FAILED checks (0 = pass), used as the caller's exit code.
    public static int SelfTest(TextWriter w)
    {
        int ok = 0, fail = 0;
        void Check(string name, bool condition, string detail)
        {
            if (condition) { ok++; w.WriteLine("  ok   " + name); }
            else { fail++; w.WriteLine("  FAIL " + name + "  -> " + detail); }
        }

        string[] rest; int secs; string problem;
        w.WriteLine("=== SettleCap.SelfTest - the back-end settle cap option, offline ===");

        Check("absent -> the historical 15 s default, args unchanged",
              TryTakeFlag(new[] { "4400", "--dry-run" }, out rest, out secs, out problem)
              && secs == DefaultSeconds && rest.Length == 2,
              "secs=" + secs);

        Check("present -> the value, and BOTH tokens removed",
              TryTakeFlag(new[] { "4400", Flag, "180", "--dry-run" }, out rest, out secs, out problem)
              && secs == 180 && rest.Length == 2 && rest[0] == "4400" && rest[1] == "--dry-run",
              "secs=" + secs + " rest=" + (rest == null ? "null" : string.Join(",", rest)));

        Check("no value -> usage error",
              !TryTakeFlag(new[] { "4400", Flag }, out rest, out secs, out problem) && problem != null,
              "problem=" + problem);

        Check("followed by an option -> usage error",
              !TryTakeFlag(new[] { Flag, "--dry-run" }, out rest, out secs, out problem) && problem != null,
              "problem=" + problem);

        Check("given twice -> usage error",
              !TryTakeFlag(new[] { Flag, "30", Flag, "60" }, out rest, out secs, out problem) && problem != null,
              "problem=" + problem);

        Check("non-integer -> usage error naming the argument",
              !TryTakeFlag(new[] { Flag, "soon" }, out rest, out secs, out problem)
              && problem != null && problem.Contains("soon"),
              "problem=" + problem);

        Check("0 is out of range",
              !TryTakeFlag(new[] { Flag, "0" }, out rest, out secs, out problem) && problem != null,
              "problem=" + problem);

        Check("3601 is out of range",
              !TryTakeFlag(new[] { Flag, "3601" }, out rest, out secs, out problem) && problem != null,
              "problem=" + problem);

        Check("1 and 3600 are the accepted edges",
              TryTakeFlag(new[] { Flag, "1" }, out rest, out secs, out problem) && secs == 1
              && TryTakeFlag(new[] { Flag, "3600" }, out rest, out secs, out problem) && secs == 3600,
              "secs=" + secs);

        Check("the banner names the default as such",
              Banner(DefaultSeconds).Contains("(default)") && !Banner(180).Contains("(default)"),
              Banner(DefaultSeconds) + " / " + Banner(180));

        w.WriteLine("=== SettleCap.SelfTest: " + ok + " ok / " + fail + " fail ===");
        return fail;
    }
}
