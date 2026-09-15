using C2SIM;
using S = C2SIM.Schema102; // SISO-STD-C2SIM 1.0.2 (CWIX2024) generated types; XML ns C2SIM/1.1

namespace VrfC2SimApp;

/// <summary>
/// Parses a C2SIM Order message into <see cref="OrderData"/> by DESERIALIZING into the
/// SDK's XSD-generated schema types (C2SIM.Schema102 via ToC2SIMObject) - the same
/// schema-driven approach as <see cref="InitParser"/>, NOT hand-navigating element names.
/// Tasks are read from MessageBody -> DomainMessageBody -> OrderBody -> Task[] ->
/// ManeuverWarfareTask via typed properties.
///
/// The field mapping mirrors the C++ SAX handler (C2SIMxmlHandler.cpp) so the resulting
/// VR-Forces command stream matches the golden trace:
///   - taskeeUuid  = PerformingEntity           (:2037-2038)
///   - taskUuid    = UUID                        (:2020-2022)
///   - taskName    = Name                        (:2028-2030)  (route name = taskName + " ROUTE")
///   - mapGraphicUuid = MapGraphicID             (:2061-2063)
///   - ruleOfEngagementCode = WeaponRuleOfEngagementCode (:2074-2076)
///   - simulationStartMs / relativeDelayMs via findTotalIsoMs (:245)
/// BEYOND PARITY (R4, user ruling 2026-09-14 - "completion is given by the end time"):
///   - durationMs = Duration/IsoTimeDuration (schema :4132), which the C++ never read;
///   - absoluteStartUtc = StartTime/DateTime/IsoDateTime, the other TimeInstantType branch.
///
/// This is PURE (no bridge / MAK dependency) so it can be reviewed and tested offline
/// (VrfC2SimApp --parse-order &lt;file&gt;).
/// </summary>
public static class OrderParser
{
    public static OrderData Parse(string xml)
    {
        var data = new OrderData();
        if (string.IsNullOrWhiteSpace(xml)) return data;

        // Root-robust: an order FILE is <MessageBody><DomainMessageBody><OrderBody>, but the SDK's
        // live OrderReceived event delivers the BARE <OrderBody> (the dispatch descends into
        // DomainMessageBody). All three types carry [XmlRoot], so each deserializes alone.
        //
        // SNIFF THE ROOT, DO NOT GUESS (B5, 2026-09-14). This used to TRY the MessageBody overload
        // and catch the failure - but ToC2SIMObject LOGS "Failed to deserialize xml to type
        // C2SIM.Schema102.MessageBodyType ... (1, 2)" through the SDK's own logger before it throws
        // (C2SIMSSDK.cs:823), so the catch hid the exception and not the ERROR line: every run log
        // carried one for the inbound order. One sniff, one overload, no false error - the SDK's own
        // STOMP pump dispatches exactly this way (C2SIMSSDK.cs:639-676). See C2SimXml.
        // The middle shape (DomainMessageBody-rooted) is handled too: the old code could not read it
        // at all (both speculative attempts failed, two ERROR lines and an empty order).
        S.OrderBodyType order = null;
        try
        {
            string root = C2SimXml.RootLocalName(xml);
            order = root == C2SimXml.MessageBody
                  ? (C2SIMSDK.ToC2SIMObject<S.MessageBodyType>(xml)?.Item as S.DomainMessageBodyType)?.Item as S.OrderBodyType
                  : root == C2SimXml.DomainMessageBody
                  ? C2SIMSDK.ToC2SIMObject<S.DomainMessageBodyType>(xml)?.Item as S.OrderBodyType
                  : C2SIMSDK.ToC2SIMObject<S.OrderBodyType>(xml);
        }
        catch { return data; }
        if (order == null) return data;

        data.OrderId = (order.OrderID ?? "").Trim();

        foreach (var t in order.Task ?? Array.Empty<S.TaskType>())
        {
            var m = t?.Item;
            if (m == null) continue;

            var (simMs, startAfter, relMs, absStart) = TimingOf(m);
            long durationMs = DurationMsOf(m.Duration);
            var task = new OrderTask
            {
                TaskUuid = (m.UUID ?? "").Trim(),
                TaskName = (m.Name ?? "").Trim(),
                TaskeeUuid = (m.PerformingEntity ?? "").Trim(),
                AffectedEntity = FirstOrEmpty(m.AffectedEntity),
                ActionCode = m.TaskActionCode.ToString(),
                RuleOfEngagementCode = RoeCodeOf(m.RuleOfEngagement),
                MapGraphicUuid = FirstOrEmpty(m.MapGraphicID),
                MapGraphicUuids = AllNonEmpty(m.MapGraphicID),
                SimulationStartMs = simMs,
                StartAfterTaskUuid = startAfter,
                RelativeDelayMs = relMs,
                DurationMs = Math.Max(0, durationMs),
                AbsoluteStartUtc = absStart,
            };
            // R4: a Duration that is PRESENT but unreadable must not pass as "no duration" - the
            // task would then have no end time at all and (for a hold-type verb) never complete.
            if (m.Duration != null && durationMs < 0)
                data.Warnings.Add($"task '{task.TaskName}' Duration '{m.Duration.IsoTimeDuration}' is not the " +
                                  "P00Y00M00DT00H00M00S form findTotalIsoMs decodes; the task has NO end time");
            // LocationType (schema :3610-3625) is a CHOICE of GeodeticCoordinate or
            // RelativeLocation. Only the geodetic branch is supported (user ruling 2026-09-14:
            // RelativeLocation is unsupported and STP does not emit it) - but V4b reads the SHAPE of
            // this list, so a point dropped in silence would change the shape it reads (a ring can
            // lose the vertex that closes it). Say what was dropped.
            int droppedLocations = 0;
            foreach (var loc in m.Location ?? Array.Empty<S.LocationType>())
                if (loc?.Item is S.GeodeticCoordinateType g)
                    task.Points.Add((g.Latitude, g.Longitude, ElevOf(g)));
                else if (loc?.Item != null)
                    droppedLocations++;
            if (droppedLocations > 0)
                data.Warnings.Add($"task '{task.TaskName}' carries {droppedLocations} Location element(s) that are " +
                                  "NOT a GeodeticCoordinate (RelativeLocation is the schema's other branch and is " +
                                  "NOT supported); they are dropped, so this task's geometry is incomplete");

            // Surface what the executor silently assumes about temporal relationships:
            // only the FIRST one is honored, and it is treated as start-after-END (STREND).
            // (An ABSENT code also deserializes as the enum default ENDEND - both real
            // orders always carry an explicit STREND, so a non-STREND value here is either
            // a genuinely different association or a missing code; both deserve a warn.)
            var atrs = m.ActionTemporalRelationship ?? Array.Empty<S.ActionTemporalRelationshipType>();
            if (atrs.Length > 1)
                data.Warnings.Add($"task '{task.TaskName}' has {atrs.Length} ActionTemporalRelationships; " +
                                  "only the first is honored");
            if (atrs.Length > 0 && atrs[0].ActionTemporalAssociationCode != S.ActionTemporalAssociationCodeType.STREND)
                data.Warnings.Add($"task '{task.TaskName}' ActionTemporalAssociationCode=" +
                                  $"{atrs[0].ActionTemporalAssociationCode} (or absent); the sequencer " +
                                  "assumes STREND (start after predecessor ends)");

            data.Tasks.Add(task);
        }
        return data;
    }

    // ---- typed navigation helpers -----------------------------------------

    // ROE: RuleOfEngagement[] -> MipWeaponUseROE -> WeaponROECode (CodeType) ->
    // Item (WeaponRuleOfEngagementCodeType enum) -> string e.g. "ROETight".
    private static string RoeCodeOf(S.RuleOfEngagementType[] roes)
    {
        foreach (var r in roes ?? Array.Empty<S.RuleOfEngagementType>())
        {
            var code = (r?.Item as S.MipWeaponUseROEType)?.WeaponROECode?.Item;
            if (code != null) return code.ToString();
        }
        return "";
    }

    /// <summary>
    /// R4: the task's own Duration (ManeuverWarfareTaskType.Duration, schema :4132) in ms.
    /// Returns -1 when the element is present but undecodable (the caller warns) and 0 when it
    /// is absent - an absent Duration is not an error, it just leaves the task with no end time.
    /// </summary>
    private static long DurationMsOf(S.DurationType d)
        => d == null ? 0 : FindTotalIsoMs(d.IsoTimeDuration);

    private static (long simMs, string startAfter, long relMs, DateTime? absStart) TimingOf(S.ManeuverWarfareTaskType m)
    {
        long simMs = 0;
        DateTime? absStart = null;
        // StartTime is a TimeInstantType: SimulationTime (a relative DelayTimeAmount - the form
        // STP exports), DateTime (an absolute IsoDateTime), or RelativeTime (no delay amount in
        // the schema, so nothing to honour). R4 accepts the first two.
        if (m.StartTime?.Item is S.SimulationTimeType st && st.DelayTimeAmount != null)
            simMs = Math.Max(0, FindTotalIsoMs(st.DelayTimeAmount.IsoTimeDuration));
        else if (m.StartTime?.Item is S.DateTimeType dt && !string.IsNullOrWhiteSpace(dt.IsoDateTime)
                 && DateTime.TryParse(dt.IsoDateTime, System.Globalization.CultureInfo.InvariantCulture,
                                      System.Globalization.DateTimeStyles.AdjustToUniversal
                                      | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
            absStart = parsed;

        string startAfter = "";
        long relMs = 0;
        var atr = (m.ActionTemporalRelationship ?? Array.Empty<S.ActionTemporalRelationshipType>())
                  .FirstOrDefault();
        if (atr != null)
        {
            startAfter = (atr.TemporalAssociationWithAction ?? "").Trim();
            if (atr.Duration != null) relMs = Math.Max(0, FindTotalIsoMs(atr.Duration.IsoTimeDuration));
        }
        return (simMs, startAfter, relMs, absStart);
    }

    private static double? ElevOf(S.GeodeticCoordinateType g)
        => g.AltitudeAGLSpecified ? g.AltitudeAGL
         : g.AltitudeMSLSpecified ? g.AltitudeMSL : (double?)null;

    private static string FirstOrEmpty(string[] arr)
        => (arr ?? Array.Empty<string>()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim() ?? "";

    /// <summary>R1: every non-blank entry, in document order (MapGraphicID is a LIST in the
    /// schema, and a task that names several graphics means all of them).</summary>
    private static IReadOnlyList<string> AllNonEmpty(string[] arr)
        => (arr ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Select(s => s.Trim()).ToArray();

    /// <summary>
    /// Port of C2SIMxmlHandler::findTotalIsoMs (C2SIMxmlHandler.cpp:245).
    /// Decodes "P00Y00M00DT00H00M00S" to milliseconds; returns -1 if the format is
    /// invalid (every P/Y/M/DT/H/M/S designator must be present).
    ///
    /// THE MONTH TERM IS CORRECTED, not reproduced (m4 of the cold-start review of 5c67d41).
    /// The C++ multiplies months by 30*60*60 = 108,000 s - thirty HOURS - and the port carried
    /// that as a parity quirk on the recorded grounds that it was "behavior-neutral for the
    /// golden trace (all durations are zero)". That reasoning expired with R4: this function's
    /// output now DECIDES when a task completes and when its successors dispatch, so
    /// P00Y01M00DT00H00M00S would have ended a task after 30 hours instead of 30 days. The
    /// golden trace is still unaffected - every Y/M/D term in every order on disk is zero,
    /// COA-STP1 included - so the correction is behaviour-neutral where parity was ever
    /// measured, and correct where it now matters. Thirty days is the same convention the day
    /// and year terms already use (86,400 and 365*86,400: nominal, not calendar).
    /// </summary>
    public static long FindTotalIsoMs(string duration)
    {
        try
        {
            if (string.IsNullOrEmpty(duration) || duration[0] != 'P') return -1;
            string remain = duration.Substring(1);

            int yPos = remain.IndexOf('Y'); if (yPos < 0) return -1;
            long result = 31536000L * long.Parse(remain.Substring(0, yPos));           // 365*24*60*60

            remain = remain.Substring(yPos + 1);
            int moPos = remain.IndexOf('M'); if (moPos < 0) return -1;
            result += 2592000L * long.Parse(remain.Substring(0, moPos));               // 30*24*60*60 (m4: the
                                                                                       // C++ used 30*60*60)

            remain = remain.Substring(moPos + 1);
            int dtPos = remain.IndexOf("DT", StringComparison.Ordinal); if (dtPos < 0) return -1;
            result += 86400L * long.Parse(remain.Substring(0, dtPos));                 // 24*60*60

            remain = remain.Substring(dtPos + 2);
            int hPos = remain.IndexOf('H'); if (hPos < 0) return -1;
            result += 3600L * long.Parse(remain.Substring(0, hPos));

            remain = remain.Substring(hPos + 1);
            int minPos = remain.IndexOf('M'); if (minPos < 0) return -1;
            result += 60L * long.Parse(remain.Substring(0, minPos));

            remain = remain.Substring(minPos + 1);
            int sPos = remain.IndexOf('S'); if (sPos < 0) return -1;
            result += long.Parse(remain.Substring(0, sPos));

            return 1000L * result;
        }
        catch { return -1; }
    }
}
