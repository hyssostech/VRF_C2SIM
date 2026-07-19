using System.Globalization;
using System.Text;

namespace WatchVrf;

// Trace-line formatting for every non-POS stream tools/WatchVrf emits. All of them share
// one process and one UTC clock base with the POS,... lines, so the whole trace is a single
// timeline:
//   CON,...  Object Console messages, per unit          (groundwork plan 0.6)
//   BCON,... Backend console messages, per sim engine   (independent second console path)
//   TSK,...  task-completion events
//   RPT,...  radio text-reports
//   RAW,...  un-extrapolated position + velocity, paired with each POS sample
//   CONARM,... one record per uuid armed for BACKEND-SIDE console capture (--console-log-dir)
// Each tag is DISTINCT and every format here is APPEND-ONLY: existing tags and their field
// counts never change, so a consumer that filters on one tag ignores the rest untouched.
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

    // RAW,<t>,<uuid>,<rawLat>,<rawLon>,<rawAlt>,<velX>,<velY>,<velZ>
    //   - one record per sampled object per sample tick, emitted ALONGSIDE that object's
    //     POS line (same t, same uuid), never instead of it.
    //
    // WHY A SEPARATE LINE TYPE, NOT WIDER POS FIELDS: POS is the project's movement oracle
    // and every existing consumer splits it into exactly 6 fields. Appending columns to POS
    // would break all of them; a new tag is invisible to a parser that filters on "POS".
    //
    // WHAT THE FIELDS MEAN. POS carries location() - the position computed THROUGH VR-Link's
    // dead-reckoning approximator. RAW carries lastSetLocation() and lastSetVelocity(): the
    // values VR-Forces last actually SENT, with no extrapolation
    // (baseEntityStateRepository.h:118 / :133). Comparing POS against RAW for the same
    // (t, uuid) is the point of this stream: sustained disagreement indicts the approximator,
    // agreement exonerates it and indicts something upstream.
    //
    // UNITS/PRECISION: lat/lon degrees at F6 and alt metres at F1, matching POS exactly so
    // the two lines are directly comparable digit-for-digit. Velocity is GEOCENTRIC
    // metres/second at F3 - an ECEF frame, NOT local ENU: velY is not "north". It also
    // originates as float (DtVector32), so trailing digits carry float precision only.
    //
    // NO ESCAPING: every field is a number except uuid, which is VRF marking text (colons,
    // structurally comma-free) - the same assumption POS has always made for the same field.
    public static string RawLine(double t, string uuid,
                                 double rawLat, double rawLon, double rawAlt,
                                 double velX, double velY, double velZ)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"RAW,{t},{uuid},{rawLat:F6},{rawLon:F6},{rawAlt:F1},{velX:F3},{velY:F3},{velZ:F3}");
    }

    // BCON,<t>,<simAddress>,<level>,<escaped-message> - one record per BACKEND console
    // message (VrfBridge.BackendConsoleMessage), the per-sim-engine counterpart to CON.
    //
    // WHY: with only the CON stream, an empty console trace is ambiguous - "VR-Forces raised
    // no warnings" and "warnings were raised but never reached us" are indistinguishable.
    // BCON is an independent delivery path: traffic here while CON stays empty localises the
    // fault to the object-console path rather than to VR-Forces' silence.
    //
    // Field 2 is the DtSimulationAddress string (e.g. "1:3201"), structurally comma-free like
    // CON's uuid, so it is written raw; the message is escaped by the identical rule as CON.
    public static string BackendLine(double t, string simAddress, int notifyLevel, string message)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"BCON,{t},{simAddress},{notifyLevel},{EscapeField(message)}");
    }

    // CONARM,<t>,<uuid>,<escaped-path> - one record per uuid the run ARMED for backend-side
    // console capture (WatchVrf --console-log-dir), emitted once per uuid at the moment the
    // two requests were sent: SetObjectNotifyLevel(uuid, 4) then
    // LogObjectConsoleToFile(uuid, path).
    //
    // WHY IT IS A REQUEST RECORD, NOT A RESULT RECORD. Neither controller call has any
    // acknowledgement in the VR-Forces API (VrfFacade.h:409-427) and the file is written by
    // the BACKEND on the backend's own filesystem, which may not even be this machine. So
    // this line records what was ASKED FOR and where the file was asked to go. It asserts
    // nothing about whether the file exists. Reading the trace later, CONARM is what makes
    // an absent file interpretable: without it, "no file" and "never requested" look the same.
    //
    // The path is ESCAPED (a directory name may legally contain a comma or a quote); the uuid
    // is written raw, the same structural assumption POS and CON make for the same field.
    public static string ArmLine(double t, string uuid, string path)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"CONARM,{t},{uuid},{EscapeField(path)}");
    }

    // Map a VR-Forces uuid to a filesystem-SAFE basename component.
    //
    // LOAD-BEARING, NOT COSMETIC. A uuid is colon-delimited ("1:1:0:2001") and ':' is not a
    // legal Windows filename character - "console-1:1:0:2001.log" is parsed as an NTFS
    // alternate-data-stream reference, so the backend's write would fail SILENTLY (the API
    // reports nothing) and the empty-file result would be misread as "no messages were
    // raised". That is exactly the inference this whole feature exists to make safe, so the
    // sanitisation has to happen before the name reaches the backend.
    //
    // Every character outside [A-Za-z0-9._-] becomes '_'. The mapping is deliberately NOT
    // reversible; correlation back to the real uuid is via the CONARM line, which carries
    // the uuid and the path together.
    public static string SafeUuidForFilename(string uuid)
    {
        uuid ??= "";
        var sb = new StringBuilder(uuid.Length);
        foreach (char c in uuid)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                   || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-';
            sb.Append(ok ? c : '_');
        }
        return sb.Length == 0 ? "empty" : sb.ToString();
    }
}
