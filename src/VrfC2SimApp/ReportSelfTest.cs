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

        // ---- task-status (TASKABRT) - the progress watchdog's report (C16, StallPolicy.cs) ----
        // Same builder, same path to the server, only the code differs; the round-trip proves the
        // TASKABRT enum serializes and deserializes as schema-valid xml exactly as TASKCMPLT does.
        string abrtXml = ReportBuilder.BuildTaskStatusReport(taskee, taskUuid,
                                                            S.TaskStatusCodeType.TASKABRT, iso, reportId);
        Console.WriteLine();
        Console.WriteLine("=== TaskStatus (TASKABRT) report ===");
        Console.WriteLine(abrtXml);
        var ra = ReportBodyOf(Roundtrip(abrtXml));
        if (ra != null && ra.ReportContent is { Length: 1 }
            && ra.ReportContent[0].Item is S.TaskStatusType ta)
        {
            Check(ref failures, ta.TaskStatusCode == S.TaskStatusCodeType.TASKABRT, "TaskStatusCode == TASKABRT");
            Check(ref failures, ta.CurrentTask == taskUuid, "TASKABRT CurrentTask == taskUuid");
            Check(ref failures, ra.ReportingEntity == taskee, "TASKABRT ReportingEntity == taskee");
            Check(ref failures, IsoOf(ta.TimeOfObservation) == iso, "TASKABRT TimeOfObservation == iso");
            Check(ref failures, abrtXml.Contains("TASKABRT", StringComparison.Ordinal)
                                && !abrtXml.Contains("TASKCMPLT", StringComparison.Ordinal),
                  "the TASKABRT wire xml carries TASKABRT and no TASKCMPLT");
        }
        else { failures++; Console.WriteLine("  FAIL: TASKABRT did not round-trip to a TaskStatus content"); }

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
            // B7 OMISSION: this overload passes no heading/speed (the simulation read failed or
            // was not attempted), so NEITHER element may appear. Checked on the round-tripped
            // object AND on the raw wire xml - the *Specified flag is [XmlIgnore], so only the
            // wire proves nothing was serialized, and a fabricated 0 would be the real defect.
            Check(ref failures, !pc.HeadingAngleSpecified && !pc.SpeedSpecified,
                  "no-kinematics position: HeadingAngleSpecified/SpeedSpecified both false");
            // Bare local names: an ABSENT element cannot put its name on the wire under any
            // prefix or namespace spelling, so this negative is prefix-proof and the strongest
            // form of the check.
            Check(ref failures, !posXml.Contains("HeadingAngle", StringComparison.Ordinal)
                                && !posXml.Contains("Speed", StringComparison.Ordinal),
                  "no-kinematics position: wire xml carries NEITHER HeadingAngle nor Speed");
        }
        else { failures++; Console.WriteLine("  FAIL: position did not round-trip to a PositionReportContent"); }

        // ---- position WITH heading + speed (B7 / STP-784) ----
        // Units are the schema's own: HeadingAngleType = "heading direction in degrees where
        // north is zero" (C2SIM_SMX_LOX_CWIX2024.xsd:344-350), SpeedType = "Speed in
        // meters/second." (xsd:599-605). The facade converts VR-Forces' geocentric velocity and
        // orientation into the local topographic frame before these values are ever built.
        const double headingDeg = 275.25, speedMps = 8.75;
        string kinXml = ReportBuilder.BuildPositionReport(subject, lat, lon, iso, reportId,
                                                          headingDeg, speedMps);
        Console.WriteLine();
        Console.WriteLine("=== Position report WITH HeadingAngle + Speed ===");
        Console.WriteLine(kinXml);
        var rk = ReportBodyOf(Roundtrip(kinXml));
        if (rk != null && rk.ReportContent is { Length: 1 }
            && rk.ReportContent[0].Item is S.PositionReportContentType kc)
        {
            Check(ref failures, kc.HeadingAngleSpecified && kc.HeadingAngle == headingDeg,
                  "HeadingAngle round-trips as 275.25 deg true");
            Check(ref failures, kc.SpeedSpecified && kc.Speed == speedMps,
                  "Speed round-trips as 8.75 m/s");
            Check(ref failures, kinXml.Contains("HeadingAngle", StringComparison.Ordinal)
                                && kinXml.Contains("Speed", StringComparison.Ordinal),
                  "wire xml carries both HeadingAngle and Speed");
            var kg = kc.Location?.Item as S.GeodeticCoordinateType;
            Check(ref failures, kc.SubjectEntity == subject && kg != null
                  && kg.Latitude == lat && kg.Longitude == lon,
                  "heading/speed do not disturb SubjectEntity or Location");
        }
        else { failures++; Console.WriteLine("  FAIL: heading/speed position did not round-trip"); }

        // A ZERO speed is real data (a stopped unit), NOT a missing read: it must be SENT.
        // This is the check that separates "omit when unknown" from "omit when zero" - the
        // watchdog validation run reads a frozen unit as speed 0 with a valid heading.
        string zeroXml = ReportBuilder.BuildPositionReport(subject, lat, lon, iso, reportId, 0.0, 0.0);
        var rz = ReportBodyOf(Roundtrip(zeroXml));
        if (rz != null && rz.ReportContent is { Length: 1 }
            && rz.ReportContent[0].Item is S.PositionReportContentType zc)
        {
            Check(ref failures, zc.SpeedSpecified && zc.Speed == 0.0
                                && zc.HeadingAngleSpecified && zc.HeadingAngle == 0.0,
                  "a READ zero (stopped unit, heading north) is SENT, not omitted");
        }
        else { failures++; Console.WriteLine("  FAIL: zero heading/speed position did not round-trip"); }

        // One field present, the other absent - the two are independent, so a heading read that
        // works alongside a speed read that does not must not drag the missing one onto the wire.
        string halfXml = ReportBuilder.BuildPositionReport(subject, lat, lon, iso, reportId,
                                                           headingDeg, null);
        var rh = ReportBodyOf(Roundtrip(halfXml));
        if (rh != null && rh.ReportContent is { Length: 1 }
            && rh.ReportContent[0].Item is S.PositionReportContentType hc)
        {
            Check(ref failures, hc.HeadingAngleSpecified && !hc.SpeedSpecified
                                && halfXml.Contains("HeadingAngle", StringComparison.Ordinal)
                                && !halfXml.Contains("Speed", StringComparison.Ordinal),
                  "heading present + speed absent: only HeadingAngle is serialized");
        }
        else { failures++; Console.WriteLine("  FAIL: heading-only position did not round-trip"); }

        // ---- position BUNDLE (P4b: ONE ReportBody carrying N PositionReportContent blocks) ----
        // B7: heading/speed are PER FIX - fix 0 has both (a good kinematics read), fix 1 has
        // neither (a failed read), fix 2 has heading only. One bundle therefore proves that a
        // failed read on one unit does not suppress or fabricate the fields on its neighbours.
        var fixes = new (string uuid, double latDeg, double lonDeg, double? headingDeg, double? speedMps)[]
        {
            ("aaaaaaaa-0000-0000-0000-000000000001", 58.10, 16.10, 12.5, 3.25),
            ("bbbbbbbb-0000-0000-0000-000000000002", 58.20, 16.20, null, null),
            ("cccccccc-0000-0000-0000-000000000003", 58.30, 16.30, 359.9, null),
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
            // B7 per-fix heading/speed, checked against the same table that built the bundle.
            bool kinOk = true;
            for (int i = 0; i < 3; i++)
            {
                if (rb.ReportContent[i].Item is not S.PositionReportContentType bc) { kinOk = false; break; }
                if (bc.HeadingAngleSpecified != fixes[i].headingDeg.HasValue
                    || bc.SpeedSpecified != fixes[i].speedMps.HasValue) { kinOk = false; break; }
                if (fixes[i].headingDeg.HasValue && bc.HeadingAngle != fixes[i].headingDeg.Value) { kinOk = false; break; }
                if (fixes[i].speedMps.HasValue && bc.Speed != fixes[i].speedMps.Value) { kinOk = false; break; }
            }
            Check(ref failures, kinOk,
                  "bundle heading/speed are per fix (both / neither / heading-only), present exactly where read");
            Check(ref failures, rb.ReportID == reportId, "one ReportID for the whole bundle body");
            Check(ref failures, rb.ReportingEntity == fixes[0].uuid,
                  "bundle ReportingEntity == first fix uuid (C++-parity envelope choice)");
        }
        else { failures++; Console.WriteLine("  FAIL: 3-fix bundle did not round-trip to 3 contents"); }

        // A 1-fix bundle is semantically the single-content shape (one content; ReportingEntity=subject).
        var oneFix = new (string uuid, double latDeg, double lonDeg, double? headingDeg, double? speedMps)[]
                     { (subject, lat, lon, null, null) };
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
