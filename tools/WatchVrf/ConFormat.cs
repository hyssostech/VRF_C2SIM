using System.Globalization;
using System.Text;

namespace WatchVrf;

// CON,<t>,<uuid>,<level>,<message> line formatting for the Object Console capture stream
// (groundwork plan 0.6). tools/WatchVrf emits these ALONGSIDE its POS,... lines, from the
// same process and the same UTC clock base, so both streams share one timeline.
//
// This type is deliberately dependency-free (no VrfBridge / MAK references) so the
// --con-selftest path can run fully offline without loading the native bridge DLL.
//
// FIELD / ESCAPING RULE (documented ONCE here; keep any consumer/parser in sync):
//   The line is comma-separated, exactly like POS. The first four fields
//   (tag, t, uuid, level) are guaranteed comma/quote/newline-free - t is a number, uuid
//   is VRF marking text (colons, no commas), level is an int - so they are written RAW,
//   the same way POS writes its fields (POS never quotes because its fields are always
//   safe; CON keeps that convention for its safe fields).
//   The LAST field, <message>, is arbitrary VR-Forces text and MAY contain commas, double
//   quotes and newlines. It is ALWAYS emitted as one RFC-4180 quoted field so it can never
//   break CSV field-splitting, and control chars are C-escaped first so each CON record
//   stays on exactly ONE physical line (the log interleaves one-line POS / # / CON records
//   and is read line-by-line):
//     1. backslash  \        -> \\     (two chars; makes the escape reversible)
//     2. carriage return \r  -> \r     (two chars: backslash + 'r')
//     3. line feed       \n  -> \n     (two chars: backslash + 'n')
//     4. wrap the whole field in double quotes; double any embedded " -> "" (RFC 4180)
//   All four are applied in a single left-to-right pass below.
//   To DECODE: strip the outer quotes, then scan left-to-right undoubling "" -> " and
//   reversing \\ -> \, \r -> CR, \n -> LF (the ConSelfTest decoder does exactly this and
//   asserts round-trip equality).
public static class ConFormat
{
    // Escape an arbitrary string into a single-line, RFC-4180 quoted CSV field.
    public static string EscapeField(string s)
    {
        s ??= "";
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break; // step 1
                case '\r': sb.Append("\\r"); break;  // step 2
                case '\n': sb.Append("\\n"); break;  // step 3
                case '"': sb.Append("\"\""); break;  // step 4 (RFC-4180 quote doubling)
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    // Build a full CON line. 't' is elapsed seconds from the shared run base (same base and
    // UTC clock as the POS lines - callers MUST pass a value computed off that base so the
    // two streams do not mix time bases). Culture-invariant so '.' stays the decimal sep.
    public static string Line(double t, string uuid, int notifyLevel, string message)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"CON,{t},{uuid},{notifyLevel},{EscapeField(message)}");
    }
}
