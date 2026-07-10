using C2SIM;
using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of ReportBuilder (no bridge, no MAK, no VR-Forces):
/// `VrfC2SimApp --report-selftest`. Builds one task-status (TASKCMPLT) report and one
/// position report, prints them, and ROUND-TRIPS each back through ToC2SIMObject to prove
/// the serialized xml is well-formed and schema-valid with the expected field values
/// (compare against docs/golden-trace/reports-captured_wire-xml.log).
/// </summary>
public static class ReportSelfTest
{
    public static int Run()
    {
        int failures = 0;
        const string iso = "2026-07-10T12:00:00Z";
        const string reportId = "11111111-2222-3333-4444-555555555555";
        const string taskee = "670cfe3a-6c43-f267-ad7f-bd6e739def24";
        const string taskUuid = "a0e0eeb4-59b4-4d4f-88d2-5101538fa371";

        // ---- task-status (TASKCMPLT) ----
        string statusXml = ReportBuilder.BuildTaskCompleteReport(taskee, taskUuid, iso, reportId);
        Console.WriteLine("=== TaskStatus (TASKCMPLT) report ===");
        Console.WriteLine(statusXml);
        var rs = ReportBodyOf(Roundtrip(statusXml));
        if (rs != null && rs.ReportContent is { Length: 1 }
            && rs.ReportContent[0].Item is S.TaskStatusType ts)
        {
            Check(ref failures, ts.TaskStatusCode == S.TaskStatusCodeType.TASKCMPLT, "TaskStatusCode == TASKCMPLT");
            Check(ref failures, ts.CurrentTask == taskUuid, "CurrentTask == taskUuid");
            Check(ref failures, rs.ReportingEntity == taskee, "ReportingEntity == taskee");
            Check(ref failures, IsoOf(ts.TimeOfObservation) == iso, "TimeOfObservation == iso");
        }
        else { failures++; Console.WriteLine("  FAIL: task-status did not round-trip to a TaskStatus content"); }

        // ---- position ----
        const string subject = "001aa71b-4c26-a1ea-28b2-f7dfe8e76342";
        const double lat = 58.703, lon = 16.4992;
        string posXml = ReportBuilder.BuildPositionReport(subject, lat, lon, iso, reportId);
        Console.WriteLine();
        Console.WriteLine("=== Position report ===");
        Console.WriteLine(posXml);
        var rp = ReportBodyOf(Roundtrip(posXml));
        if (rp != null && rp.ReportContent is { Length: 1 }
            && rp.ReportContent[0].Item is S.PositionReportContentType pc)
        {
            Check(ref failures, pc.SubjectEntity == subject, "SubjectEntity == subject");
            Check(ref failures, rp.ReportingEntity == subject, "ReportingEntity == subject");
            var g = pc.Location?.Item as S.GeodeticCoordinateType;
            Check(ref failures, g != null && g.Latitude == lat && g.Longitude == lon, "Location lat/lon match");
            Check(ref failures, (pc.EntityHealthStatus?.Length ?? 0) == 0, "EntityHealthStatus omitted (no health data)");
            Check(ref failures, IsoOf(pc.TimeOfObservation) == iso, "TimeOfObservation == iso");
        }
        else { failures++; Console.WriteLine("  FAIL: position did not round-trip to a PositionReportContent"); }

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // PushReportMessage wraps a bare ReportBody in MessageBody/DomainMessageBody; do the
    // same before deserializing so ToC2SIMObject<MessageBodyType> can read it back.
    private static S.MessageBodyType Roundtrip(string reportBodyXml)
    {
        string wrapped = "<MessageBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">" +
                         "<DomainMessageBody>" + StripDecl(reportBodyXml) +
                         "</DomainMessageBody></MessageBody>";
        try { return C2SIMSDK.ToC2SIMObject<S.MessageBodyType>(wrapped); }
        catch (Exception e) { Console.WriteLine($"  FAIL: round-trip deserialize threw: {e.Message}"); return null; }
    }

    private static string StripDecl(string xml)
    {
        int i = xml.IndexOf("?>", StringComparison.Ordinal);
        return i >= 0 ? xml.Substring(i + 2).TrimStart() : xml;
    }

    // MessageBody -> DomainMessageBody -> ReportBody (same nesting OrderParser walks).
    private static S.ReportBodyType ReportBodyOf(S.MessageBodyType body)
        => (body?.Item as S.DomainMessageBodyType)?.Item as S.ReportBodyType;

    private static string IsoOf(S.TimeInstantType t) => (t?.Item as S.DateTimeType)?.IsoDateTime;

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
