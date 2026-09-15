using System.Globalization;
using C2SIM;
using S = C2SIM.Schema102;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// The C2SIM carrier for a pre-flight warning, built the way ReportBuilder builds every other
/// report: by CONSTRUCTING the SDK's XSD-generated types and serializing them, never by
/// assembling xml text.
///
/// SHAPE (from tools/preflight/leg_check.py's emit_c2sim): ONE ReportBody per flagged leg,
/// carrying TWO Observations. C2SIM 1.0.2 has no observation type that carries a location AND
/// free text, so the pair is the schema's answer: a LocationObservation for WHERE, and a
/// NameObservation whose Marking carries the numbers and the wording. No field is invented.
///
/// ADDRESSING: FromSender/ToReceiver are the zero uuid, as in ReportBuilder - the C++ oracle
/// hardcodes them in every report and this interface has followed it since.
///
/// PRECISION IS PART OF THE CONTRACT: latitude/longitude are rounded to 6 decimals and the
/// altitude to 1, which is what the python emitter's "%.6f"/"%.1f" put on the wire. Without
/// that rounding the same geometry would serialize as 34.656069999999995 here and 34.656070
/// there, and the two bodies would never be byte-comparable even though they mean the same
/// thing. Rounding is half-to-EVEN to match python's formatting.
/// </summary>
public static class PreflightReports
{
    private const string ZeroUuid = "00000000-0000-0000-0000-000000000000";

    /// <summary>
    /// The wording the calibration record fixes, and the reason it is worded that way: the
    /// tool never asserts that the vehicles cannot traverse the ground, because it is an
    /// estimate off terrain tiles and not a vendor verdict.
    /// </summary>
    public static string Verdict(double ratio, double threshold)
        => $"PREDICTED IMPASSABLE (pre-flight estimate, ratio {F(ratio, 2)} vs threshold {F(threshold, 2)})";

    /// <summary>The Marking text of the NameObservation - identical to the python emitter's.</summary>
    public static string Marking(string taskName, string template, LegMetrics leg, double threshold)
        => $"ROUTE PRE-FLIGHT: task {taskName} leg {leg.Index} - {F(leg.SustainedWindowM, 0)} m of " +
           $"sustained {F(leg.Sustained, 3)} rise-over-run on {leg.Soil}, {F(leg.WorstSM / 1000.0, 2)} km " +
           $"along the leg; {(string.IsNullOrEmpty(template) ? "unit" : template)} limit {F(leg.Limit, 3)} " +
           $"(max-slope {F(leg.LimitRaw, 2)} x soil {F(leg.Factor, 2)}). {Verdict(leg.Ratio, threshold)}.";

    /// <summary>
    /// One flagged leg -> one bare ReportBody (PushReportMessage wraps it in
    /// MessageBody/DomainMessageBody, exactly as for a position or task-status report).
    /// </summary>
    public static string BuildLegWarningReport(string unitUuid, string unitName, string taskName,
                                               string template, LegMetrics leg, double threshold,
                                               string isoDateTime, string reportId)
    {
        string actor = unitUuid ?? "";
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
                        TimeOfObservation = new S.TimeInstantType
                        {
                            Item = new S.DateTimeType { IsoDateTime = isoDateTime }
                        },
                        Observation = new[]
                        {
                            // WHERE: the centre of the window that failed.
                            new S.ObservationType
                            {
                                Item = new S.LocationObservationType
                                {
                                    ActorReference = actor,
                                    Location = new S.LocationType
                                    {
                                        Item = new S.GeodeticCoordinateType
                                        {
                                            AltitudeMSL = Round(leg.WorstZM, 1),
                                            AltitudeMSLSpecified = true,
                                            Latitude = Round(leg.WorstLat, 6),
                                            Longitude = Round(leg.WorstLon, 6),
                                        }
                                    }
                                }
                            },
                            // WHAT: the numbers and the verdict wording.
                            new S.ObservationType
                            {
                                Item = new S.NameObservationType
                                {
                                    ActorReference = actor,
                                    Marking = Marking(taskName, template, leg, threshold),
                                    Name = unitName ?? "",
                                }
                            },
                        }
                    }
                }
            },
            ReportID = reportId,
            ReportingEntity = actor,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    /// <summary>
    /// THE EMISSION POLICY, in one pure function: one ReportBody per FLAGGED leg, and
    /// nothing at all for a leg that passed or that got NO VERDICT. A leg without a verdict
    /// is silent on purpose - missing tiles are not evidence of good ground, and inventing a
    /// warning from them would be exactly the false alarm DEMO_READINESS row 20 is guarding
    /// against.
    ///
    /// Kept separate from the service so the policy can be tested without a bridge, a tile or
    /// a federation: N flagged legs -> N reports, no flagged legs -> none.
    /// </summary>
    public static List<string> BuildForTask(TaskPreflight task, double threshold,
                                            string isoDateTime, Func<string> newReportId)
    {
        var outp = new List<string>();
        if (task?.Legs == null) return outp;
        foreach (var leg in task.Legs)
        {
            if (!leg.Flagged) continue;
            outp.Add(BuildLegWarningReport(task.UnitUuid, task.UnitName, task.TaskName,
                                           task.Template, leg, threshold,
                                           isoDateTime, newReportId()));
        }
        return outp;
    }

    // ================= THE LATERAL ROUTE SHIFT (docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md)
    // A shift CHANGES WHERE UNITS DRIVE, so it is never silent to the C2 side: the same
    // Location + Name observation pair as a warning, with the location at the first OFFSET
    // waypoint - where the detour stands clear of the face - and the numbers in the Marking.

    /// <summary>The Marking of a leg the interface detoured.</summary>
    public static string ShiftMarking(string taskName, string unitName, LegShift s)
        => $"ROUTE SHIFT: task {taskName} ({unitName}) leg {s.LegIndex} - the authored line scored " +
           $"{F(s.BaseRatio, 3)} against this unit's own limit; the interface detoured it {F(Math.Abs(s.OffsetMeters), 0)} m " +
           $"{s.SideWord} of the authored line around that window, scoring {F(s.ShiftedRatio, 3)}" +
           (double.IsNaN(s.BandMax) ? "" : $" (formation band max {F(s.BandMax, 3)})") +
           $". STP's own vertices are unchanged and in order; the detour lies between them.";

    /// <summary>The Marking of a flagged leg NO offset in the band could clear.</summary>
    public static string NoShiftMarking(string taskName, string unitName, LegShift s)
        => $"ROUTE SHIFT NOT APPLIED: task {taskName} ({unitName}) leg {s.LegIndex} - the leg scored " +
           $"{F(s.BaseRatio, 3)} and no cleared line was found within +/-{F(s.BandSearchedMeters, 0)} m" +
           (double.IsNaN(s.BestRatioTried) ? ""
              : $" (best candidate {F(s.BestOffsetTried, 0)} m at {F(s.BestRatioTried, 3)})") +
           ". The task is dispatched on the line as authored.";

    /// <summary>
    /// One shift outcome -> one bare ReportBody, built exactly like the warning: a
    /// LocationObservation for WHERE and a NameObservation for WHAT. The location is the first
    /// inserted waypoint for a shift, and the flagged window's centre when nothing was inserted -
    /// in both cases the point an operator would look at.
    /// </summary>
    public static string BuildShiftReport(string unitUuid, string unitName, string taskName,
                                          LegShift shift, LegMetrics leg,
                                          string isoDateTime, string reportId)
    {
        string actor = unitUuid ?? "";
        double lat = shift.Shifted ? shift.In.Lat : leg.WorstLat;
        double lon = shift.Shifted ? shift.In.Lon : leg.WorstLon;
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
                        TimeOfObservation = new S.TimeInstantType
                        {
                            Item = new S.DateTimeType { IsoDateTime = isoDateTime }
                        },
                        Observation = new[]
                        {
                            new S.ObservationType
                            {
                                Item = new S.LocationObservationType
                                {
                                    ActorReference = actor,
                                    Location = new S.LocationType
                                    {
                                        Item = new S.GeodeticCoordinateType
                                        {
                                            AltitudeMSL = Round(leg.WorstZM, 1),
                                            AltitudeMSLSpecified = true,
                                            Latitude = Round(lat, 6),
                                            Longitude = Round(lon, 6),
                                        }
                                    }
                                }
                            },
                            new S.ObservationType
                            {
                                Item = new S.NameObservationType
                                {
                                    ActorReference = actor,
                                    Marking = shift.Shifted
                                        ? ShiftMarking(taskName, unitName, shift)
                                        : NoShiftMarking(taskName, unitName, shift),
                                    Name = unitName ?? "",
                                }
                            },
                        }
                    }
                }
            },
            ReportID = reportId,
            ReportingEntity = actor,
        };
        return C2SIMSDK.FromC2SIMObject(body);
    }

    /// <summary>
    /// THE SHIFT EMISSION POLICY, pure: one ReportBody per FLAGGED leg the shift looked at -
    /// whether it moved it or not - and NOTHING for a leg that was never flagged. Silence on an
    /// untouched leg is the point: the C2 side hears only where the interface acted or declined
    /// to act.
    /// </summary>
    public static List<string> BuildForShift(string unitUuid, string unitName, string taskName,
                                             IReadOnlyList<LegShift> shifts,
                                             IReadOnlyList<LegMetrics> legs,
                                             string isoDateTime, Func<string> newReportId)
    {
        var outp = new List<string>();
        if (shifts == null) return outp;
        foreach (var s in shifts)
        {
            var leg = legs?.FirstOrDefault(l => l.Index == s.LegIndex);
            if (leg == null) continue;
            outp.Add(BuildShiftReport(unitUuid, unitName, taskName, s, leg, isoDateTime, newReportId()));
        }
        return outp;
    }

    /// <summary>Half-to-even, like python's "%.*f", so the two emitters agree digit for digit.</summary>
    private static double Round(double v, int digits)
        => double.IsNaN(v) || double.IsInfinity(v) ? v : Math.Round(v, digits, MidpointRounding.ToEven);

    private static string F(double v, int digits)
        => Round(v, digits).ToString("F" + digits.ToString(CultureInfo.InvariantCulture),
                                     CultureInfo.InvariantCulture);
}
