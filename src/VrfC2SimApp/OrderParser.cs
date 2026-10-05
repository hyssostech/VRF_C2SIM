using System.Collections;   // ReferenceEqualityComparer, for the shared graph walk
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
        CollectGraphics(order, data);

        foreach (var t in order.Task ?? Array.Empty<S.TaskType>())
        {
            var m = t?.Item;
            if (m == null) continue;

            string taskLabel = (m.Name ?? "").Trim();
            var (simMs, startAfter, relMs, absStart) = TimingOf(m, taskLabel, data.ShortFormDurations);
            long durationMs = m.Duration == null ? 0
                            : Decode(m.Duration.IsoTimeDuration, $"task '{taskLabel}' Duration", data.ShortFormDurations);
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
            // THE C2SIM FORM IS THE SCHEMA'S. IsoTimeDurationBaseType is an xs:string restricted by the pattern
            //   [P]{1}[0-9]{2}[Y]{1}[0-9]{2}[M]{1}[0-9]{2}[D]{1}T{1}[0-9]{2}[H]{1}[0-9]{2}[M]{1}[0-9]{2}[S]{1}
            // (C2SIM_SMX_LOX_CWIX2024.xsd:17-24), i.e. EVERY field two digits, all of them present. The
            // canonical ISO-8601 short form "PT20M" is NOT valid C2SIM 1.1 - and it is what the real STP
            // export writes (all 46 values in data/STP-IRON-STORM-SYNTHETIC_Order.xml; Jira STP-848).
            // SUPERSEDED 2026-10-04 BY RL-20261004-06 (3): the 2026-09-20 choice to REFUSE the short form
            // here was a seat choice, not a ruling, and the owner reversed it - the interface ALSO accepts
            // the short form, decoded to the same milliseconds as its pattern-form twin (FindTotalIsoMs).
            // It is never accepted SILENTLY: every short-form value read is collected and the order gets
            // ONE warning (ShortFormWarning, below) naming it non-conforming C2SIM 1.1, accepted for
            // interoperability, STP-848 - the producer fix stays open on STP's side. What remains refused
            // is a value in NEITHER form; that one still leaves the task with no end time.
            if (m.Duration != null && durationMs < 0)
                data.Warnings.Add($"task '{task.TaskName}' Duration '{m.Duration.IsoTimeDuration}' is MALFORMED " +
                                  "and VIOLATES THE C2SIM 1.1 SCHEMA: IsoTimeDuration must match " +
                                  "P##Y##M##DT##H##M##S with every field present and two digits wide (xsd:17-24), " +
                                  "e.g. P00Y00M00DT00H20M00S for 20 minutes; the interface also accepts the ISO-8601 " +
                                  "short form (PT20M, RL-20261004-06), and this value is neither. This task " +
                                  "therefore has NO END TIME: R4 cannot close it on its Duration, and its STREND " +
                                  "successors will wait out the gate. FIX THE PRODUCER'S EXPORT");
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
        // RL-20261004-06 (3): ONE warning per order for every short-form value it read, never one per
        // value (an STP export carries dozens) and never none.
        if (data.ShortFormDurations.Count > 0)
            data.Warnings.Add(ShortFormWarning(data.ShortFormDurations));
        return data;
    }

    /// <summary>The phrase every short-form warning carries, so a log grep and the self-tests can
    /// find it (RL-20261004-06 (3)).</summary>
    public const string ShortFormWarningMarker = "accepted for interoperability, STP-848, RL-20261004-06";

    /// <summary>The once-per-order warning for IsoTimeDuration values read in the ISO-8601 short
    /// form: how many, which (the first three), and that they are non-conforming C2SIM 1.1.</summary>
    public static string ShortFormWarning(IReadOnlyList<string> shortForms)
        => $"{shortForms.Count} IsoTimeDuration value(s) in this order are in the ISO-8601 SHORT form " +
           $"(e.g. {string.Join("; ", shortForms.Take(3))}{(shortForms.Count > 3 ? "; ..." : "")}) - " +
           "NON-CONFORMING C2SIM 1.1 (IsoTimeDurationBaseType requires P##Y##M##DT##H##M##S, every field " +
           $"two digits, xsd:17-24) - {ShortFormWarningMarker}: each is decoded to the same milliseconds " +
           "as its pattern-form twin (PT20M = P00Y00M00DT00H20M00S). The producer's export should still be fixed";

    /// <summary>Decode one IsoTimeDuration and, if it was in the ISO-8601 short form, record where
    /// (<paramref name="where"/> and the value) for the order's one warning.</summary>
    private static long Decode(string value, string where, List<string> shortForms)
    {
        long ms = DecodeIsoDuration(value, out var form);
        if (form == IsoDurationForm.IsoShort) shortForms.Add($"{where} '{value}'");
        return ms;
    }

    /// <summary>
    /// THE ORDER'S OWN TACTICAL GRAPHICS (2026-09-20). Collected exactly the way InitParser
    /// collects the init's: walk the deserialized graph for TacticalGraphic WRAPPERS - never for
    /// Route/Point/TacticalArea loose in the graph - so a RouteType that turns up somewhere else
    /// in a future message cannot be mistaken for a graphic. Same walker, same geodetic reader.
    ///
    /// ALL FIVE BRANCHES OF THE CHOICE ARE HANDLED except NBC_Event (nothing reads one):
    /// Line/{Route,Boundary} and TaskGraphic contribute their vertices IN ORDER (a line is a path,
    /// a task symbol's anchors are read as one - see InitTaskGraphic); Point contributes its
    /// position; TacticalArea is registered as an AREA and the resolver reduces it to its centroid,
    /// which is the same reading the init path gives an area.
    ///
    /// A ONE-VERTEX Line or TaskGraphic is registered as a POINT rather than as a degenerate path:
    /// it is still a place the task can be about, and inventing a second vertex to make it a line
    /// would be manufacturing geometry. A graphic with NO vertices at all is dropped and counted by
    /// the caller - there is nothing to resolve to.
    /// </summary>
    private static void CollectGraphics(S.OrderBodyType order, OrderData data)
    {
        var wrappers = new List<S.TacticalGraphicType>();
        InitParser.Walk(order, new HashSet<object>(ReferenceEqualityComparer.Instance), node =>
        {
            if (node is S.TacticalGraphicType g) wrappers.Add(g);
        });

        foreach (var tg in wrappers)
        {
            string name = "", uuid = "", element = "";
            S.EntityStateType state = null;
            bool isArea = false;
            switch (tg?.Item)
            {
                case S.LineType line when line.Item is S.RouteType rt:
                    name = rt.Name; uuid = rt.UUID; state = rt.CurrentState; element = "Route"; break;
                case S.LineType line2 when line2.Item is S.BoundaryType bt:
                    name = bt.Name; uuid = bt.UUID; state = bt.CurrentState; element = "Boundary"; break;
                case S.PointType pt:
                    name = pt.Name; uuid = pt.UUID; state = pt.CurrentState; element = "Point"; break;
                case S.TacticalAreaType ar:
                    name = ar.Name; uuid = ar.UUID; state = ar.CurrentState; element = "TacticalArea";
                    isArea = true; break;
                case S.TaskGraphicType tgr:
                    name = tgr.Name; uuid = tgr.UUID; state = tgr.CurrentState; element = "TaskGraphic"; break;
                default:
                    continue;   // an unmodelled branch (NBC_Event, or a Line flavour we do not read)
            }
            uuid = (uuid ?? "").Trim();
            if (uuid.Length == 0) continue;   // nothing can reference it; not registrable
            var g = new OrderGraphic
            {
                Name = (name ?? "").Trim(),
                Uuid = uuid,
                Element = element,
                Kind = isArea ? TaskGraphic.KindArea : TaskGraphic.KindLine,
            };
            foreach (var geo in InitParser.AllGeodetics(state))
                g.Points.Add((geo.Latitude, geo.Longitude, InitParser.ElevD(geo)));
            if (g.Points.Count == 0)
            {
                data.Warnings.Add($"order graphic '{g.Name}' ({element}, uuid {uuid}) carries NO " +
                                  "GeodeticCoordinate - it is not registered, and any task naming it by " +
                                  "MapGraphicID will fall back to its embedded Location");
                continue;
            }
            if (!isArea && g.Points.Count == 1) g = g with { Kind = TaskGraphic.KindPoint };
            data.Graphics.Add(g);
        }
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

    // R4: the task's own Duration (ManeuverWarfareTaskType.Duration, schema :4132) is decoded in
    // Parse: -1 when the element is present but undecodable (Parse warns) and 0 when it is absent -
    // an absent Duration is not an error, it just leaves the task with no end time.

    // Every IsoTimeDuration this parser reads goes through Decode (both forms, RL-20261004-06): the
    // task Duration (Parse), the SimulationTime DelayTimeAmount and the ActionTemporalRelationship
    // Duration (here). A malformed delay clamps to 0, as it always has.
    private static (long simMs, string startAfter, long relMs, DateTime? absStart) TimingOf(
        S.ManeuverWarfareTaskType m, string taskLabel, List<string> shortForms)
    {
        long simMs = 0;
        DateTime? absStart = null;
        // StartTime is a TimeInstantType: SimulationTime (a relative DelayTimeAmount - the form
        // STP exports), DateTime (an absolute IsoDateTime), or RelativeTime. R4 accepts the first
        // two. (Correction 2026-10-04: RelativeTimeType DOES carry a DelayTimeAmount in the
        // generated schema types - the STP export writes 14 - but this parser does not read it, in
        // either duration form; such a task starts when its STREND gate opens.)
        if (m.StartTime?.Item is S.SimulationTimeType st && st.DelayTimeAmount != null)
            simMs = Math.Max(0, Decode(st.DelayTimeAmount.IsoTimeDuration,
                                       $"task '{taskLabel}' StartTime/SimulationTime/DelayTimeAmount", shortForms));
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
            if (atr.Duration != null)
                relMs = Math.Max(0, Decode(atr.Duration.IsoTimeDuration,
                                           $"task '{taskLabel}' ActionTemporalRelationship/Duration", shortForms));
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

    /// <summary>Which spelling an IsoTimeDuration value was decoded from.</summary>
    public enum IsoDurationForm
    {
        /// <summary>Neither form (or a negative pattern-form total): refused.</summary>
        Malformed,
        /// <summary>The C2SIM 1.1 pattern form P##Y##M##DT##H##M##S (xsd:17-24).</summary>
        C2SimPattern,
        /// <summary>The ISO-8601 short form (PT20M, P1DT2H): NOT valid C2SIM 1.1, accepted
        /// for interoperability since RL-20261004-06 (STP-848).</summary>
        IsoShort,
    }

    /// <summary>
    /// Decodes an IsoTimeDuration to milliseconds in EITHER accepted spelling and says which one it
    /// was (RL-20261004-06 (3)); returns -1 (form Malformed) for a value in neither.
    /// (1) The C2SIM 1.1 pattern form is tried FIRST, by the unchanged port below, so conforming
    ///     input decodes exactly as it always has (its quirks included - every designator must be
    ///     present but any field width is read, so P1Y2M3DT4H5M6S is pattern form here; a negative
    ///     term still gives a negative total, reported as Malformed).
    /// (2) Only if that refuses the value is the ISO-8601 short form tried:
    ///       P [n Y] [n M] [n D] [T [n H] [n M] [n S]]
    ///     designators in that order, each at most once, n = one or more ASCII digits (any width),
    ///     at least one component, and a 'T' must be followed by at least one time component.
    ///     Y and M take the pattern form's NOMINAL values (365 d, 30 d), so P1Y equals
    ///     P01Y00M00DT00H00M00S and P1M equals P00Y01M00DT00H00M00S - the same calendar
    ///     approximation, applied the same way, so twins always agree.
    ///     REFUSED (-1), as the pattern form refuses its equivalents: fractions (PT1.5S, PT1,5S),
    ///     signs (-PT20M, PT-20M), weeks (P2W - the C2SIM pattern has no week field, so there is no
    ///     twin to agree with), lower case, surrounding whitespace, an empty "P" or "PT", and any
    ///     total that overflows.
    /// </summary>
    public static long DecodeIsoDuration(string duration, out IsoDurationForm form)
    {
        long ms = FindTotalIsoMsPatternForm(duration);
        if (ms != -1)
        {
            form = ms >= 0 ? IsoDurationForm.C2SimPattern : IsoDurationForm.Malformed;
            return ms;
        }
        ms = FindTotalIsoMsShortForm(duration);
        form = ms >= 0 ? IsoDurationForm.IsoShort : IsoDurationForm.Malformed;
        return ms;
    }

    /// <summary>
    /// Decodes an IsoTimeDuration in the C2SIM 1.1 pattern form OR the ISO-8601 short form
    /// (RL-20261004-06); -1 when it is neither. See <see cref="DecodeIsoDuration"/>.
    /// </summary>
    public static long FindTotalIsoMs(string duration) => DecodeIsoDuration(duration, out _);

    private static readonly System.Text.RegularExpressions.Regex IsoShortForm = new(
        @"\AP(?:([0-9]+)Y)?(?:([0-9]+)M)?(?:([0-9]+)D)?(?:(T)(?:([0-9]+)H)?(?:([0-9]+)M)?(?:([0-9]+)S)?)?\z",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>The ISO-8601 short form, by the rules in <see cref="DecodeIsoDuration"/>.</summary>
    private static long FindTotalIsoMsShortForm(string duration)
    {
        if (string.IsNullOrEmpty(duration)) return -1;
        var mt = IsoShortForm.Match(duration);
        if (!mt.Success) return -1;
        bool anyDate = mt.Groups[1].Success || mt.Groups[2].Success || mt.Groups[3].Success;
        bool anyTime = mt.Groups[5].Success || mt.Groups[6].Success || mt.Groups[7].Success;
        if (mt.Groups[4].Success && !anyTime) return -1;   // "PT", "P1DT": a T with nothing after it
        if (!anyDate && !anyTime) return -1;               // "P": no component at all
        long[] secondsPer = { 0, 31536000L, 2592000L, 86400L, 0, 3600L, 60L, 1L };   // same terms as the pattern form
        try
        {
            long total = 0;
            for (int g = 1; g <= 7; g++)
                if (g != 4 && mt.Groups[g].Success)
                    total = checked(total + secondsPer[g] * long.Parse(mt.Groups[g].Value,
                                        System.Globalization.NumberStyles.None,
                                        System.Globalization.CultureInfo.InvariantCulture));
            return checked(1000L * total);
        }
        catch (OverflowException) { return -1; }
    }

    /// <summary>
    /// Port of C2SIMxmlHandler::findTotalIsoMs (C2SIMxmlHandler.cpp:245) - the C2SIM 1.1
    /// pattern form only. Decodes "P00Y00M00DT00H00M00S" to milliseconds; returns -1 if the
    /// format is invalid (every P/Y/M/DT/H/M/S designator must be present).
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
    private static long FindTotalIsoMsPatternForm(string duration)
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
