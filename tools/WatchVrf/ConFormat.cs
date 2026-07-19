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

    // TSK,<t>,<unitMarking>,<taskType> - one record per VrfBridge.TaskCompleted event.
    //
    // NO UUID FIELD, DELIBERATELY. TaskCompletedEventArgs (VrfBridge.cpp:130-134) carries
    // exactly two strings - UnitMarking (the transmitter's markingText) and TaskType (e.g.
    // "move-along"). There is no uuid on the payload, so this line reports what the event
    // actually delivers rather than a field invented to match POS/CON's shape. Correlate to
    // POS by resolving markingText -> uuid out of band; do not assume field 2 is a uuid.
    //
    // BOTH string fields are ESCAPED with EscapeField (unlike CON's raw uuid/level, which
    // are structurally comma-free). markingText is operator-supplied VR-Forces text and
    // taskType is vendor-supplied; neither is guaranteed comma/quote/newline-free, so both
    // go through the same RFC-4180 + C-escape rule documented above. That keeps a TSK record
    // on exactly one physical line and splitting into exactly 4 CSV fields.
    public static string TaskLine(double t, string unitMarking, string taskType)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"TSK,{t},{EscapeField(unitMarking)},{EscapeField(taskType)}");
    }

    // RPT,<t>,<text> - one record per VrfBridge.TextReport event.
    //
    // NO UUID FIELD, DELIBERATELY. TextReportEventArgs (VrfBridge.cpp:125-128) carries a
    // SINGLE string: the raw VR-Forces radio text-report. The subject of the report, when
    // there is one, is embedded in that text (e.g. the Lua tracker's
    // `POSITION "entity name" <lat> <lon>`), not delivered as a separate field. Parsing it
    // out is a consumer's job; this stream records the raw text verbatim.
    //
    // The text field is ESCAPED (same rule as CON's message): report text is arbitrary and
    // routinely contains quotes, so it is always one quoted, single-line CSV field.
    public static string ReportLine(double t, string text)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"RPT,{t},{EscapeField(text)}");
    }
}
