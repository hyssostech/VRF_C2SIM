using System.Globalization;
using System.Text;

namespace WatchVrf;

// Offline check of the emitted trace line formatting (groundwork plan 0.6):
//     WatchVrf --con-selftest
// Covers CON,... (Object Console) plus the BCON,... backend-console, TSK,... / RPT,...
// task-completion and text-report, and RAW,... un-extrapolated motion lines.
// The flag name is kept as --con-selftest deliberately: it is part of
// WatchVrf's argument surface and renaming it would break existing callers.
// Pure managed, no VrfBridge / MAK / live VR-Forces - so it runs without the native bridge
// DLL on PATH. Matches the repo selftest convention (VrfC2SimApp/*SelfTest.cs): a static
// Run() that prints [PASS]/[FAIL] rows and returns 0 on success, 1 on failure.
//
// Covers the acceptance case explicitly: a message containing BOTH a comma and a quote,
// plus newline/backslash handling and a full round-trip decode (encode then decode == in).
public static class ConSelfTest
{
    public static int Run()
    {
        int failures = 0;

        // 1. Exact field encodings (the escaping rule in ConFormat, spelled out).
        CheckEq(ref failures, ConFormat.EscapeField("plain"), "\"plain\"", "plain text -> quoted");
        CheckEq(ref failures, ConFormat.EscapeField("a,b"), "\"a,b\"", "comma stays inside one quoted field");
        CheckEq(ref failures, ConFormat.EscapeField("say \"hi\""), "\"say \"\"hi\"\"\"", "quote -> doubled (RFC 4180)");
        // The acceptance case: comma AND quote together.
        CheckEq(ref failures, ConFormat.EscapeField("a,\"b\""), "\"a,\"\"b\"\"\"", "comma + quote together");
        CheckEq(ref failures, ConFormat.EscapeField("l1\nl2"), "\"l1\\nl2\"", "LF -> \\n (record stays single-line)");
        CheckEq(ref failures, ConFormat.EscapeField("l1\r\nl2"), "\"l1\\r\\nl2\"", "CRLF -> \\r\\n");
        CheckEq(ref failures, ConFormat.EscapeField("c:\\x"), "\"c:\\\\x\"", "backslash -> doubled");

        // 2. A full CON line for a comma+quote message (the exact line a run would emit).
        string line = ConFormat.Line(12.5, "1:1:0:2001", 1, "route failed, \"no path\"");
        CheckEq(ref failures, line,
            "CON,12.5,1:1:0:2001,1,\"route failed, \"\"no path\"\"\"",
            "full CON line (comma + quote in message)");

        // 3. Every CON line is exactly ONE physical line even when the message has newlines.
        string multi = ConFormat.Line(1.0, "1:1:0:9", 2, "step1\nstep2\r\nstep3");
        Check(ref failures, !multi.Contains('\n') && !multi.Contains('\r'),
            "CON line has no raw CR/LF even for a multi-line message");

        // 4. Round-trip: decode(escape(x)) == x for a battery of nasty inputs.
        string[] cases =
        {
            "",
            "plain",
            "a,b,c",
            "he said \"go\"",
            "a,\"b\",c",          // comma + quote (acceptance)
            "trailing comma,",
            "\"leading quote",
            "back\\slash",
            "back\\\\slash and \"q\"",
            "line1\nline2",
            "line1\r\nline2\ttab",
            "mix: a,\"b\"\n\\c\r",
        };
        foreach (string original in cases)
        {
            string field = ConFormat.EscapeField(original);
            string decoded = DecodeField(field);
            Check(ref failures, decoded == original,
                $"round-trip: [{Show(original)}]" + (decoded == original ? "" : $" != [{Show(decoded)}]"));
        }

        // 5. Line() splits cleanly into 5 CSV fields, message last, via a real CSV read.
        {
            var fields = ParseCsvLine("CON,3.4,1:2:3:4,0,\"a,b \"\"q\"\" c\"");
            Check(ref failures, fields.Count == 5, $"CON parses to 5 CSV fields (got {fields.Count})");
            if (fields.Count == 5)
            {
                CheckEq(ref failures, fields[0], "CON", "field0 = CON");
                CheckEq(ref failures, fields[2], "1:2:3:4", "field2 = uuid");
                CheckEq(ref failures, fields[4], "a,b \"q\" c", "field4 = decoded message (comma+quote)");
            }
        }

        // 6. TSK,... task-completion lines (VrfBridge TaskCompleted -> UnitMarking, TaskType).
        //    4 fields, BOTH strings quoted - neither is guaranteed comma-free.
        CheckEq(ref failures, ConFormat.TaskLine(31.0, "TF-Alpha", "move-along"),
            "TSK,31,\"TF-Alpha\",\"move-along\"", "full TSK line (plain marking + taskType)");
        CheckEq(ref failures, ConFormat.TaskLine(4.5, "A Co, 1st", "move-along"),
            "TSK,4.5,\"A Co, 1st\",\"move-along\"", "TSK marking containing a comma stays one field");
        {
            var fields = ParseCsvLine(ConFormat.TaskLine(4.5, "A Co, 1st", "move-along"));
            Check(ref failures, fields.Count == 4, $"TSK parses to 4 CSV fields (got {fields.Count})");
            if (fields.Count == 4)
            {
                CheckEq(ref failures, fields[0], "TSK", "TSK field0 = TSK");
                CheckEq(ref failures, fields[2], "A Co, 1st", "TSK field2 = decoded marking");
                CheckEq(ref failures, fields[3], "move-along", "TSK field3 = taskType");
            }
        }

        // 7. RPT,... text-report lines (VrfBridge TextReport -> Text only; no uuid exists).
        CheckEq(ref failures, ConFormat.ReportLine(7.25, "POSITION \"tank1\" 39.0 -76.0"),
            "RPT,7.25,\"POSITION \"\"tank1\"\" 39.0 -76.0\"",
            "full RPT line (quotes in report text doubled)");
        {
            var fields = ParseCsvLine(ConFormat.ReportLine(7.25, "POSITION \"tank1\" 39.0 -76.0"));
            Check(ref failures, fields.Count == 3, $"RPT parses to 3 CSV fields (got {fields.Count})");
            if (fields.Count == 3)
            {
                CheckEq(ref failures, fields[0], "RPT", "RPT field0 = RPT");
                CheckEq(ref failures, fields[2], "POSITION \"tank1\" 39.0 -76.0", "RPT field2 = decoded text");
            }
        }

        // 8. Both new line types stay on ONE physical line, and survive null payloads
        //    (a managed String^ property can arrive null; EscapeField maps null -> "").
        string tskMulti = ConFormat.TaskLine(2.0, "u\nit", "move\r\nalong");
        Check(ref failures, !tskMulti.Contains('\n') && !tskMulti.Contains('\r'),
            "TSK line has no raw CR/LF even for multi-line fields");
        string rptMulti = ConFormat.ReportLine(2.0, "line1\nline2");
        Check(ref failures, !rptMulti.Contains('\n') && !rptMulti.Contains('\r'),
            "RPT line has no raw CR/LF even for a multi-line report");
        CheckEq(ref failures, ConFormat.TaskLine(1, null, null), "TSK,1,\"\",\"\"",
            "TSK tolerates null marking/taskType (-> empty fields)");
        CheckEq(ref failures, ConFormat.ReportLine(1, null), "RPT,1,\"\"",
            "RPT tolerates null text (-> empty field)");

        // 9. APPEND-ONLY guard: the new types must not collide with the existing tags, and
        //    POS/CON formatting is unchanged (re-asserted here so a future edit to ConFormat
        //    that "unifies" the tags fails loudly instead of silently breaking consumers).
        CheckEq(ref failures, ConFormat.Line(1.0, "1:1:0:5", 1, "x"), "CON,1,1:1:0:5,1,\"x\"",
            "CON line format unchanged (append-only)");

        // 10. RAW,... un-extrapolated position + velocity (paired with each POS sample).
        //     9 fields, all unquoted. lat/lon F6 + alt F1 EXACTLY as POS formats them, so the
        //     two lines are digit-for-digit comparable; velocity F3.
        CheckEq(ref failures,
            ConFormat.RawLine(12.5, "1:1:0:2001", 39.123456, -76.654321, 123.5, 1.5, -0.25, 0.0),
            "RAW,12.5,1:1:0:2001,39.123456,-76.654321,123.5,1.500,-0.250,0.000",
            "full RAW line (F6 lat/lon, F1 alt, F3 vel)");
        {
            var fields = ParseCsvLine(ConFormat.RawLine(3.0, "1:2:3:4", 1.0, 2.0, 3.0, 4.0, 5.0, 6.0));
            Check(ref failures, fields.Count == 9, $"RAW parses to 9 CSV fields (got {fields.Count})");
            if (fields.Count == 9)
            {
                CheckEq(ref failures, fields[0], "RAW", "RAW field0 = RAW");
                CheckEq(ref failures, fields[2], "1:2:3:4", "RAW field2 = uuid");
                CheckEq(ref failures, fields[3], "1.000000", "RAW field3 = rawLat (F6)");
                CheckEq(ref failures, fields[5], "3.0", "RAW field5 = rawAlt (F1)");
                CheckEq(ref failures, fields[8], "6.000", "RAW field8 = velZ (F3)");
            }
        }
        // A RAW line must never be mistaken for a POS line and vice versa: different tag,
        // different field count. This is the guard that keeps POS byte-compatible.
        Check(ref failures,
            !ConFormat.RawLine(1, "u", 0, 0, 0, 0, 0, 0).StartsWith("POS", StringComparison.Ordinal),
            "RAW does not masquerade as POS");

        // 11. BCON,... backend console lines. Same 5-field shape and escaping rule as CON,
        //     but a DISTINCT tag - a consumer filtering field0 == "CON" must not pick these up.
        CheckEq(ref failures, ConFormat.BackendLine(8.0, "1:3201", 1, "backend warn, \"x\""),
            "BCON,8,1:3201,1,\"backend warn, \"\"x\"\"\"",
            "full BCON line (comma + quote in message)");
        {
            var fields = ParseCsvLine(ConFormat.BackendLine(8.0, "1:3201", 1, "plain"));
            Check(ref failures, fields.Count == 5, $"BCON parses to 5 CSV fields (got {fields.Count})");
            if (fields.Count == 5)
            {
                CheckEq(ref failures, fields[0], "BCON", "BCON field0 = BCON");
                CheckEq(ref failures, fields[2], "1:3201", "BCON field2 = simAddress");
            }
        }
        {
            string bMulti = ConFormat.BackendLine(1, "1:1", 0, "y\nz\r\nw");
            Check(ref failures, !bMulti.Contains('\n') && !bMulti.Contains('\r'),
                "BCON line has no raw CR/LF even for a multi-line message");
        }
        CheckEq(ref failures, ConFormat.BackendLine(1, "1:1", 0, null), "BCON,1,1:1,0,\"\"",
            "BCON tolerates null message (-> empty field)");

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // Reverse ConFormat.EscapeField: strip outer quotes, then one left-to-right pass
    // undoubling "" and reversing \\, \r, \n. (See ConFormat's escaping-rule comment.)
    private static string DecodeField(string field)
    {
        if (field.Length < 2 || field[0] != '"' || field[^1] != '"')
            throw new FormatException("field is not quoted");
        string inner = field.Substring(1, field.Length - 2);
        var sb = new StringBuilder(inner.Length);
        for (int i = 0; i < inner.Length; i++)
        {
            char c = inner[i];
            if (c == '"' && i + 1 < inner.Length && inner[i + 1] == '"') { sb.Append('"'); i++; }
            else if (c == '\\' && i + 1 < inner.Length)
            {
                char n = inner[i + 1];
                switch (n)
                {
                    case '\\': sb.Append('\\'); i++; break;
                    case 'r': sb.Append('\r'); i++; break;
                    case 'n': sb.Append('\n'); i++; break;
                    default: sb.Append(c); break; // lone backslash (should not occur)
                }
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    // Minimal RFC-4180 CSV line reader (independent of ConFormat) to prove the emitted line
    // is standard-parseable: unquoted fields split on comma; a quoted field undoubles "".
    private static List<string> ParseCsvLine(string line)
    {
        var outFields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { outFields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        outFields.Add(sb.ToString());
        // Note: this parser returns the CSV-level field. For the message field that still
        // carries the \\, \r, \n C-escapes; the test's field4 case uses a message with no
        // backslash/newline so the CSV-level value equals the decoded text.
        return outFields;
    }

    private static string Show(string s) =>
        s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");

    private static void CheckEq(ref int failures, string actual, string expected, string label)
    {
        bool ok = actual == expected;
        Check(ref failures, ok, label + (ok ? "" : $" (got [{Show(actual)}], want [{Show(expected)}])"));
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  [{(ok ? "PASS" : "FAIL")}] {label}"));
        if (!ok) failures++;
    }
}
