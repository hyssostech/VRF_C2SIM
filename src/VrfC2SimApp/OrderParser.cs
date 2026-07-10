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

        S.MessageBodyType body;
        try { body = C2SIMSDK.ToC2SIMObject<S.MessageBodyType>(xml); }
        catch { return data; }
        if (body?.Item is not S.DomainMessageBodyType dmb) return data;
        if (dmb.Item is not S.OrderBodyType order) return data;

        data.OrderId = (order.OrderID ?? "").Trim();

        foreach (var t in order.Task ?? Array.Empty<S.TaskType>())
        {
            var m = t?.Item;
            if (m == null) continue;

            var (simMs, startAfter, relMs) = TimingOf(m);
            var task = new OrderTask
            {
                TaskUuid = (m.UUID ?? "").Trim(),
                TaskName = (m.Name ?? "").Trim(),
                TaskeeUuid = (m.PerformingEntity ?? "").Trim(),
                AffectedEntity = FirstOrEmpty(m.AffectedEntity),
                ActionCode = m.TaskActionCode.ToString(),
                RuleOfEngagementCode = RoeCodeOf(m.RuleOfEngagement),
                MapGraphicUuid = FirstOrEmpty(m.MapGraphicID),
                SimulationStartMs = simMs,
                StartAfterTaskUuid = startAfter,
                RelativeDelayMs = relMs,
            };
            foreach (var loc in m.Location ?? Array.Empty<S.LocationType>())
                if (loc?.Item is S.GeodeticCoordinateType g)
                    task.Points.Add((g.Latitude, g.Longitude, ElevOf(g)));

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

    private static (long simMs, string startAfter, long relMs) TimingOf(S.ManeuverWarfareTaskType m)
    {
        long simMs = 0;
        if (m.StartTime?.Item is S.SimulationTimeType st && st.DelayTimeAmount != null)
            simMs = Math.Max(0, FindTotalIsoMs(st.DelayTimeAmount.IsoTimeDuration));

        string startAfter = "";
        long relMs = 0;
        var atr = (m.ActionTemporalRelationship ?? Array.Empty<S.ActionTemporalRelationshipType>())
                  .FirstOrDefault();
        if (atr != null)
        {
            startAfter = (atr.TemporalAssociationWithAction ?? "").Trim();
            if (atr.Duration != null) relMs = Math.Max(0, FindTotalIsoMs(atr.Duration.IsoTimeDuration));
        }
        return (simMs, startAfter, relMs);
    }

    private static double? ElevOf(S.GeodeticCoordinateType g)
        => g.AltitudeAGLSpecified ? g.AltitudeAGL
         : g.AltitudeMSLSpecified ? g.AltitudeMSL : (double?)null;

    private static string FirstOrEmpty(string[] arr)
        => (arr ?? Array.Empty<string>()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim() ?? "";

    /// <summary>
    /// Faithful port of C2SIMxmlHandler::findTotalIsoMs (C2SIMxmlHandler.cpp:245).
    /// Decodes "P00Y00M00DT00H00M00S" to milliseconds; returns -1 if the format is
    /// invalid (every P/Y/M/DT/H/M/S designator must be present).
    /// PARITY QUIRK reproduced, NOT fixed: the month term uses 30*60*60 seconds (30
    /// "hours", not 30 days) exactly as the C++ does. Behavior-neutral for the golden
    /// trace (all durations are zero); do not correct here (parity-first, PORT.md sec 5).
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
            result += 108000L * long.Parse(remain.Substring(0, moPos));                // 30*60*60 (C++ quirk)

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
