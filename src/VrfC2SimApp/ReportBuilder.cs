using C2SIM;
using S = C2SIM.Schema102; // SISO-STD-C2SIM 1.0.2 (CWIX2024) generated types; XML ns C2SIM/1.1

namespace VrfC2SimApp;

/// <summary>
/// Builds C2SIM ReportBody messages by CONSTRUCTING the SDK's XSD-generated schema types
/// and SERIALIZING them via C2SIMSDK.FromC2SIMObject - the output analog of InitParser/
/// OrderParser's schema-typed input. The result is a bare &lt;ReportBody&gt; (PushReportMessage
/// wraps it in MessageBody/DomainMessageBody).
///
/// This deliberately does NOT reproduce the C++ interface's hand-assembled report strings
/// (textIf.cxx c2simPositionPart*/c2simTaskStatusPart*): that template assembly emits
/// MALFORMED xml for the task-status report (stray/duplicated ReportID/ReportingEntity
/// fragments) and EMPTY enum-valued health fields (the sec-6 aggregate-health bug). The
/// schema-typed build produces well-formed, schema-valid xml with the same SEMANTIC content.
///
/// Content mapping (from textIf.cxx sendStatusReport / sendC2simReport + the golden capture
/// docs/golden-trace/reports-captured_wire-xml.log):
///   - Task status: ReportContent/TaskStatus{TimeOfObservation, CurrentTask=taskUuid,
///     TaskStatusCode}; ReportingEntity = the taskee (senderUuid). No SubjectEntity (the
///     TaskStatus schema type has none).
///   - Position: ReportContent/PositionReportContent{TimeOfObservation, Location(lat/lon),
///     SubjectEntity=uuid}; ReportingEntity = uuid. EntityHealthStatus is OMITTED - this
///     slice carries no health data from the bridge, and the golden's empty health elements
///     were the bug; health enrichment is a later slice.
/// </summary>
public static class ReportBuilder
{
    // The C++ hardcodes these sender/receiver uuids in every report (textIf.cxx:266-267,
    // 326-327). "TODO: determine who is sender" is noted there; reproduced verbatim.
    private const string ZeroUuid = "00000000-0000-0000-0000-000000000000";

    /// <summary>Task-complete status report (TASKCMPLT) for a taskee's current task.</summary>
    public static string BuildTaskCompleteReport(string taskeeUuid, string taskUuid,
                                                 string isoDateTime, string reportId)
    {
        var body = new S.ReportBodyType
        {
            FromSender = ZeroUuid,
            ToReceiver = ZeroUuid,
            ReportContent = new[]
            {
                new S.ReportContentType
                {
                    Item = new S.TaskStatusType
                    {
                        TimeOfObservation = Time(isoDateTime),
                        CurrentTask = taskUuid ?? "",
                        TaskStatusCode = S.TaskStatusCodeType.TASKCMPLT,
                    }
                }
            },
            ReportID = reportId,
            ReportingEntity = taskeeUuid,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    /// <summary>Position report (single content) for one subject entity at lat/lon.</summary>
    public static string BuildPositionReport(string subjectUuid, double latDeg, double lonDeg,
                                             string isoDateTime, string reportId)
    {
        var body = new S.ReportBodyType
        {
            FromSender = ZeroUuid,
            ToReceiver = ZeroUuid,
            ReportContent = new[]
            {
                new S.ReportContentType
                {
                    Item = new S.PositionReportContentType
                    {
                        TimeOfObservation = Time(isoDateTime),
                        Location = Geo(latDeg, lonDeg),
                        SubjectEntity = subjectUuid,
                        // EntityHealthStatus omitted - see class remarks.
                    }
                }
            },
            ReportID = reportId,
            ReportingEntity = subjectUuid,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    /// <summary>Position report BUNDLE (P4b): ONE ReportBody carrying N PositionReportContent
    /// blocks - the C++-parity shape (frozen oracle textIf.cxx:435-544 sendC2simReport). POSITION
    /// reports only are bundled; the ReportID applies to the WHOLE bundle and is minted at send
    /// (passed in as reportId). FromSender/ToReceiver stay ZeroUuid as in the single-content build.
    ///
    /// ReportingEntity choice (oracle evidence): the C++ bundle envelope fills its ONE
    /// &lt;ReportingEntity&gt; with the `uuid` argument of sendC2simReport at FLUSH time
    /// (c2simPositionPart9 "&lt;/ReportID&gt;&lt;ReportingEntity&gt;" + uuid + c2simPositionPart10).
    /// That value is INCONSISTENT across the three C++ flush paths: the count-full flush uses the
    /// LAST fix's uuid (textIf.cxx:479), the size-overflow flush uses the NEXT (overflowing) fix's
    /// uuid (:516, not even in the sent bundle), and the ~2 s reminder-thread flush uses the FIRST
    /// fix's uuid (waitForBundle is started at numberReportsInBundle==1 with that uuid, :501-505 ->
    /// :560). So the C++ has NO single principled bundle reporting entity. Per plan 3.1, given that
    /// ambiguity the first choice is the FIRST fix's subject uuid - it matches the C++ timer-driven
    /// whole-bundle flush (the common trickle path) and is closest to single-report semantics
    /// (ReportingEntity == the first subject). Fallback ZeroUuid only for an empty bundle (the
    /// service never flushes an empty one, but the builder stays robust).</summary>
    public static string BuildPositionReportBundle(
        IEnumerable<(string uuid, double latDeg, double lonDeg)> fixes,
        string isoDateTime, string reportId)
    {
        var list = fixes as IReadOnlyList<(string uuid, double latDeg, double lonDeg)>
                   ?? fixes.ToList();
        var body = new S.ReportBodyType
        {
            FromSender = ZeroUuid,
            ToReceiver = ZeroUuid,
            ReportContent = list.Select(f => new S.ReportContentType
            {
                Item = new S.PositionReportContentType
                {
                    TimeOfObservation = Time(isoDateTime),
                    Location = Geo(f.latDeg, f.lonDeg),
                    SubjectEntity = f.uuid,
                    // EntityHealthStatus omitted - see class remarks.
                }
            }).ToArray(),
            ReportID = reportId,
            ReportingEntity = list.Count > 0 ? list[0].uuid : ZeroUuid,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    private static S.TimeInstantType Time(string iso)
        => new() { Item = new S.DateTimeType { IsoDateTime = iso } };

    private static S.LocationType Geo(double latDeg, double lonDeg)
        => new() { Item = new S.GeodeticCoordinateType { Latitude = latDeg, Longitude = lonDeg } };
}
