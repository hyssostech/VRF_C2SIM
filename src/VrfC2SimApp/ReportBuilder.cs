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

    private static S.TimeInstantType Time(string iso)
        => new() { Item = new S.DateTimeType { IsoDateTime = iso } };

    private static S.LocationType Geo(double latDeg, double lonDeg)
        => new() { Item = new S.GeodeticCoordinateType { Latitude = latDeg, Longitude = lonDeg } };
}
