using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VrfC2Sim.Tools;

// Shared argument-handling helpers for the tools/ command line utilities.
//
// WHY THIS EXISTS (2026-07-19): every tool had grown its own local static Fail()/Usage()
// (tools/CreateOne/Program.cs:35, tools/SetSimRate/Program.cs:43) and the tools that had
// NOT grown one either acted on no arguments at all or threw a raw stack trace. Neither
// is safe inside an UNATTENDED runner: a stack trace is not a contract, and a tool that
// acts with no arguments cannot be probed.
//
// THE STANDARD THIS ENCODES:
//   exit 0  success
//   exit 1  operational failure (the action was attempted and did not succeed)
//   exit 2  usage / argument error - NO ACTION WAS TAKEN
// Usage text goes to STDERR so a runner capturing stdout for data does not ingest it.
//
// Included into each tool via a <Compile Include="..\Shared\ToolArgs.cs" Link="..."/>
// item rather than a shared project: these tools are single-file top-level-statement
// programs with no other shared code, and a project reference would be more ceremony
// than the 3 helpers below are worth.
internal static class ToolArgs
{
    public const int ExitOk = 0;
    public const int ExitFailure = 1;
    public const int ExitUsage = 2;

    /// <summary>
    /// Print "[FAIL] problem" then the usage block to STDERR and return exit code 2.
    /// Callers use this as "return ToolArgs.Usage(...)" so that no action can follow.
    /// </summary>
    public static int Usage(string problem, params string[] usageLines)
    {
        Console.Error.WriteLine("[FAIL] " + problem);
        Console.Error.WriteLine();
        foreach (string line in usageLines) Console.Error.WriteLine(line);
        return ExitUsage;
    }

    /// <summary>True if the token is an option (starts with "--").</summary>
    public static bool IsFlag(string a) => a != null && a.StartsWith("--", StringComparison.Ordinal);

    /// <summary>
    /// Options present in args that are not in <paramref name="known"/>. An unknown option
    /// MUST be rejected, never silently consumed as a positional: tools/ResetVrf accepts
    /// --dry-run, so an operator carrying that habit into a tool that lacks it would
    /// otherwise perform a REAL action believing it was a no-op.
    /// Comparison is ordinal and case-sensitive, matching the flags the tools declare.
    /// </summary>
    public static string[] UnknownFlags(string[] args, params string[] known)
    {
        var set = new HashSet<string>(known ?? Array.Empty<string>(), StringComparer.Ordinal);
        return (args ?? Array.Empty<string>()).Where(a => IsFlag(a) && !set.Contains(a)).ToArray();
    }

    /// <summary>Non-option tokens, in order.</summary>
    public static string[] Positionals(string[] args)
        => (args ?? Array.Empty<string>()).Where(a => !IsFlag(a)).ToArray();

    /// <summary>Case-sensitive presence test for an option.</summary>
    public static bool HasFlag(string[] args, string flag)
        => (args ?? Array.Empty<string>()).Any(a => string.Equals(a, flag, StringComparison.Ordinal));

    /// <summary>
    /// Parse an int, invariant culture, producing a message that NAMES the offending
    /// argument. int.Parse throws a FormatException whose text does not say which
    /// argument was bad; in an unattended log that is close to useless.
    /// </summary>
    public static bool TryInt(string raw, string argName, out int value, out string problem)
    {
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            problem = $"{argName} '{raw}' is not an integer.";
            return false;
        }
        problem = null;
        return true;
    }

    /// <summary>As TryInt, additionally rejecting values outside [min, max].</summary>
    public static bool TryIntInRange(string raw, string argName, int min, int max,
                                     out int value, out string problem)
    {
        if (!TryInt(raw, argName, out value, out problem)) return false;
        if (value < min || value > max)
        {
            problem = $"{argName} {value} is out of range (expected {min}..{max}).";
            return false;
        }
        problem = null;
        return true;
    }

    /// <summary>
    /// Parse a double, invariant culture, rejecting NaN/Infinity. NumberStyles.Float
    /// ACCEPTS "NaN" and "Infinity" and every relational test against NaN is false, so a
    /// bare range check downstream would let them through (see CreateOne's note).
    /// </summary>
    public static bool TryFiniteDouble(string raw, string argName, out double value, out string problem)
    {
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            problem = $"{argName} '{raw}' is not a number.";
            return false;
        }
        if (!double.IsFinite(value))
        {
            // Report the RAW argument: .NET renders infinity with a non-ASCII glyph.
            problem = $"{argName} '{raw}' is not a finite number.";
            return false;
        }
        problem = null;
        return true;
    }

    /// <summary>
    /// Reject an endpoint that is not a well-formed absolute URL with a host. Catches the
    /// common unattended-runner mistakes: a bare hostname, or a host:port with no scheme
    /// (note "127.0.0.1:8080/x" DOES parse as an absolute Uri - with scheme "127.0.0.1"
    /// and an EMPTY host - which is why the Host check below is the load-bearing one).
    ///
    /// SCHEME IS DELIBERATELY NOT RESTRICTED TO HTTP unless the caller asks. C2SIMSDK
    /// discards the STOMP url's scheme entirely - it rebuilds the client from Host, Port
    /// and PathAndQuery (C2SIMSSDK.cs:78-86) - so a non-http stomp url worked before this
    /// validation existed, and rejecting it here would narrow previously-valid input.
    /// The REST url is different: its Uri is handed to HttpClient, which genuinely
    /// requires http/https, so callers pass requireHttp: true for that one.
    /// </summary>
    public static bool TryUrl(string raw, string argName, out string value, out string problem,
                              bool requireHttp = false)
    {
        value = raw;
        if (string.IsNullOrWhiteSpace(raw))
        {
            problem = $"{argName} is empty.";
            return false;
        }
        if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri uri) || string.IsNullOrEmpty(uri.Host))
        {
            problem = $"{argName} '{raw}' is not an absolute URL with a host "
                    + "(expected something like http://127.0.0.1:8080/C2SIMServer).";
            return false;
        }
        if (requireHttp && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            problem = $"{argName} '{raw}' has scheme '{uri.Scheme}'; http or https is required "
                    + "(this url is used to build an HttpClient request).";
            return false;
        }
        problem = null;
        return true;
    }
}
