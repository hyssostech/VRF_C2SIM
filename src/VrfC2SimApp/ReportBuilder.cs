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
///     SubjectEntity=uuid, and - when the simulation read succeeds - HeadingAngle (degrees
///     true, north = 0) and Speed (metres/second), B7/STP-784}; ReportingEntity = uuid.
///     EntityHealthStatus is OMITTED - this slice carries no health data from the bridge,
///     and the golden's empty health elements were the bug; health enrichment is a later
///     slice. Heading/speed are omitted the same way when their read fails (see Position).
/// </summary>
public static class ReportBuilder
{
    // The C++ hardcodes these sender/receiver uuids in every report (textIf.cxx:266-267,
    // 326-327). "TODO: determine who is sender" is noted there; reproduced verbatim.
    private const string ZeroUuid = "00000000-0000-0000-0000-000000000000";

    /// <summary>
    /// Task-status report for a taskee's current task, with the C2SIM status code as a
    /// PARAMETER. Two codes are emitted by this interface today: TASKCMPLT (the vendor's or the
    /// arrival-evidence completion, C15) and TASKABRT (the progress watchdog, C16 - a move whose
    /// unit stopped making progress and which VR-Forces will never report; see StallPolicy.cs).
    /// Both go to the C2SIM server through THIS one builder and the service's single
    /// PushReportAsync path, so a TASKABRT is schema-identical to a TASKCMPLT but for the code.
    /// </summary>
    public static string BuildTaskStatusReport(string taskeeUuid, string taskUuid,
                                               S.TaskStatusCodeType code,
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
                        TaskStatusCode = code,
                    }
                }
            },
            ReportID = reportId,
            ReportingEntity = taskeeUuid,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    /// <summary>Task-complete status report (TASKCMPLT) for a taskee's current task.</summary>
    public static string BuildTaskCompleteReport(string taskeeUuid, string taskUuid,
                                                 string isoDateTime, string reportId)
        => BuildTaskStatusReport(taskeeUuid, taskUuid, S.TaskStatusCodeType.TASKCMPLT, isoDateTime, reportId);

    /// <summary>Position report (single content) for one subject entity at lat/lon, with
    /// OPTIONAL heading and speed (B7 / STP-784).</summary>
    /// <param name="headingDeg">Degrees TRUE, north = 0, range [0, 360) - the schema's own
    /// units (C2SIM_SMX_LOX_CWIX2024.xsd:344-350 HeadingAngleType, "heading direction in
    /// degrees where north is zero"). null = the simulation read FAILED: the element is
    /// omitted entirely, never sent as 0.</param>
    /// <param name="speedMps">Ground speed in METRES PER SECOND (xsd:599-605 SpeedType,
    /// "Speed in meters/second."). null = omitted, as for heading.</param>
    public static string BuildPositionReport(string subjectUuid, double latDeg, double lonDeg,
                                             string isoDateTime, string reportId,
                                             double? headingDeg = null, double? speedMps = null)
    {
        var body = new S.ReportBodyType
        {
            FromSender = ZeroUuid,
            ToReceiver = ZeroUuid,
            ReportContent = new[]
            {
                new S.ReportContentType
                {
                    Item = Position(subjectUuid, latDeg, lonDeg, isoDateTime, headingDeg, speedMps)
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
        IEnumerable<(string uuid, double latDeg, double lonDeg, double? headingDeg, double? speedMps)> fixes,
        string isoDateTime, string reportId)
    {
        var list = fixes as IReadOnlyList<(string uuid, double latDeg, double lonDeg, double? headingDeg, double? speedMps)>
                   ?? fixes.ToList();
        var body = new S.ReportBodyType
        {
            FromSender = ZeroUuid,
            ToReceiver = ZeroUuid,
            ReportContent = list.Select(f => new S.ReportContentType
            {
                Item = Position(f.uuid, f.latDeg, f.lonDeg, isoDateTime, f.headingDeg, f.speedMps)
            }).ToArray(),
            ReportID = reportId,
            ReportingEntity = list.Count > 0 ? list[0].uuid : ZeroUuid,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    /// <summary>
    /// R-SURFACE-PROXY (user ruling 2026-07-17; docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md
    /// sec 7.1 item 5): tell downstream C2SIM consumers, at creation time, that this unit is
    /// represented in the simulation by a template that is NOT its doctrinal identity.
    ///
    /// Vehicle: ObservationReport / NameObservation, the schema's own channel for "what this
    /// actor is called and marked in the simulation" (ActorReference + Name + Marking). No
    /// private extension, no new message type; a consumer that ignores NameObservation is
    /// unaffected, one that reads it sees the substitution in full.
    /// </summary>
    public static string BuildTypeSubstitutionReport(string unitUuid, string c2simName,
                                                     string marking, string substitution,
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
                    Item = new S.ObservationReportContentType
                    {
                        TimeOfObservation = Time(isoDateTime),
                        Observation = new[]
                        {
                            new S.ObservationType
                            {
                                Item = new S.NameObservationType
                                {
                                    ActorReference = unitUuid ?? "",
                                    Name = c2simName ?? "",
                                    // The substitution rides in Marking: it is the free-text
                                    // field the standard reserves for the simulation's own label.
                                    Marking = string.IsNullOrEmpty(substitution)
                                        ? (marking ?? "")
                                        : $"{marking} [{substitution}]",
                                }
                            }
                        }
                    }
                }
            },
            ReportID = reportId,
            ReportingEntity = unitUuid ?? ZeroUuid,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    /// <summary>
    /// One PositionReportContent block - the SINGLE place heading/speed are attached, so the
    /// single-report and the P4b bundle paths cannot drift apart.
    ///
    /// OMISSION (B7 / STP-784): HeadingAngle and Speed are xs:double elements with
    /// minOccurs="0" (xsd:3213/3215), which xsd.exe generated as a non-nullable double PLUS a
    /// companion HeadingAngleSpecified / SpeedSpecified bool marked [XmlIgnore]
    /// (C2SIM_SMX_LOX_CWIX2024.cs: fields 10908-10916, properties HeadingAngle 10952 /
    /// HeadingAngleSpecified 10963 / Speed 10983 / SpeedSpecified 10994). XmlSerializer emits the element
    /// ONLY when the *Specified flag is true, so leaving the flag false is what actually keeps
    /// the element off the wire - assigning the value alone would silently emit nothing, and
    /// assigning a value without meaning to would emit a FABRICATED 0. A null argument here
    /// therefore sets neither, and a failed simulation read reports no heading and no speed
    /// rather than a unit stopped and pointing due north.
    /// </summary>
    private static S.PositionReportContentType Position(
        string subjectUuid, double latDeg, double lonDeg, string isoDateTime,
        double? headingDeg, double? speedMps)
    {
        var c = new S.PositionReportContentType
        {
            TimeOfObservation = Time(isoDateTime),
            Location = Geo(latDeg, lonDeg),
            SubjectEntity = subjectUuid,
            // EntityHealthStatus omitted - see class remarks.
        };
        if (headingDeg.HasValue)
        {
            c.HeadingAngle = headingDeg.Value;      // degrees true, north = 0 (xsd:344-350)
            c.HeadingAngleSpecified = true;
        }
        if (speedMps.HasValue)
        {
            c.Speed = speedMps.Value;               // metres/second (xsd:599-605)
            c.SpeedSpecified = true;
        }
        return c;
    }

    private static S.TimeInstantType Time(string iso)
        => new() { Item = new S.DateTimeType { IsoDateTime = iso } };

    private static S.LocationType Geo(double latDeg, double lonDeg)
        => new() { Item = new S.GeodeticCoordinateType { Latitude = latDeg, Longitude = lonDeg } };
}
