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

        // ---- position BUNDLE (P4b: ONE ReportBody carrying N PositionReportContent blocks) ----
        var fixes = new (string uuid, double latDeg, double lonDeg)[]
        {
            ("aaaaaaaa-0000-0000-0000-000000000001", 58.10, 16.10),
            ("bbbbbbbb-0000-0000-0000-000000000002", 58.20, 16.20),
            ("cccccccc-0000-0000-0000-000000000003", 58.30, 16.30),
        };
        string bundleXml = ReportBuilder.BuildPositionReportBundle(fixes, iso, reportId);
        Console.WriteLine();
        Console.WriteLine("=== Position report BUNDLE (3 fixes) ===");
        Console.WriteLine(bundleXml);
        var rb = ReportBodyOf(Roundtrip(bundleXml));
        if (rb != null && rb.ReportContent is { Length: 3 })
        {
            Check(ref failures, true, "bundle round-trips to 3 ReportContent blocks");
            bool allMatch = true, timeOk = true;
            for (int i = 0; i < 3; i++)
            {
                if (rb.ReportContent[i].Item is S.PositionReportContentType bc)
                {
                    var bg = bc.Location?.Item as S.GeodeticCoordinateType;
                    if (bc.SubjectEntity != fixes[i].uuid || bg == null
                        || bg.Latitude != fixes[i].latDeg || bg.Longitude != fixes[i].lonDeg)
                        allMatch = false;
                    if (IsoOf(bc.TimeOfObservation) != iso) timeOk = false;
                }
                else { allMatch = false; timeOk = false; }
            }
            Check(ref failures, allMatch, "each bundle content has its own uuid/lat/lon (in order)");
            Check(ref failures, timeOk, "each bundle content TimeOfObservation == iso");
            Check(ref failures, rb.ReportID == reportId, "one ReportID for the whole bundle body");
            Check(ref failures, rb.ReportingEntity == fixes[0].uuid,
                  "bundle ReportingEntity == first fix uuid (C++-parity envelope choice)");
        }
        else { failures++; Console.WriteLine("  FAIL: 3-fix bundle did not round-trip to 3 contents"); }

        // A 1-fix bundle is semantically the single-content shape (one content; ReportingEntity=subject).
        var oneFix = new (string uuid, double latDeg, double lonDeg)[] { (subject, lat, lon) };
        string oneBundleXml = ReportBuilder.BuildPositionReportBundle(oneFix, iso, reportId);
        var r1 = ReportBodyOf(Roundtrip(oneBundleXml));
        if (r1 != null && r1.ReportContent is { Length: 1 }
            && r1.ReportContent[0].Item is S.PositionReportContentType b1)
        {
            var g1 = b1.Location?.Item as S.GeodeticCoordinateType;
            Check(ref failures, b1.SubjectEntity == subject && g1 != null
                  && g1.Latitude == lat && g1.Longitude == lon,
                  "1-fix bundle == single-content shape (uuid/lat/lon)");
            Check(ref failures, r1.ReportingEntity == subject,
                  "1-fix bundle ReportingEntity == subject (single-report semantics)");
        }
        else { failures++; Console.WriteLine("  FAIL: 1-fix bundle did not round-trip to a single content"); }

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
