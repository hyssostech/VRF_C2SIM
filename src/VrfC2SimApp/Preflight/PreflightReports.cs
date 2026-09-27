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

    // ================= WATER (STP-802 / the Suwalki AO) ======================================
    // A vehicle on deep water has acceleration-factor 0.000000 and stopping-factor 0.000000
    // (ground-tracked.sysdef's soil-list), i.e. a dead stop the vendor reports as TaskRunning
    // for ever - the silent-freeze shape a demo cannot afford. Before CLCplus was in the source
    // list the pre-flight was BLIND to it at Suwalki: 154/165 serve nothing there and
    // Copernicus's water class 80 is commented out of the vendor catalogue, so water resolved to
    // no soil and no verdict while the SIM, which composes CLCplus, stopped the vehicle anyway.
    //
    // THIS IS A FINDING, NOT A REFUSAL. Nothing in the pre-flight refuses a task and nothing
    // here starts: the leg is reported and dispatched. It does not size or side a shift either -
    // water reaches the chooser only the way any other ground does, through the SAME sampler
    // (a water sample's zero factor makes a candidate polyline score +infinity, so the search
    // simply never accepts one). "Unknown ground is never clear" and "C2 may size a shift, never
    // side one" both still hold.

    /// <summary>The Marking of a leg that crosses water.</summary>
    public static string WaterMarking(string taskName, string unitName, LegMetrics leg)
        => $"ROUTE PRE-FLIGHT - WATER ON THE LINE: task {taskName} ({unitName}) leg {leg.Index} - " +
           $"{leg.WaterSamples} of {leg.Samples} sample(s) along this leg classify as {leg.WaterSoil}" +
           (string.IsNullOrEmpty(leg.WaterSource) ? "" : $" ({leg.WaterSource}" +
                (string.IsNullOrEmpty(leg.WaterDesc) ? "" : $": {leg.WaterDesc}") + ")") +
           $", first at {F(leg.WaterFirstSM / 1000.0, 2)} km along. The vendor's own soil table gives " +
           "deep water acceleration-factor 0.000, so a ground vehicle driven onto it STOPS and the task " +
           "stays TaskRunning. This is an ESTIMATE off land-cover tiles, not a vendor verdict, and NOTHING " +
           "was refused or altered by it - the leg is reported and dispatched.";

    /// <summary>
    /// One leg with water -> one bare ReportBody, the same Location + Name pair as every other
    /// pre-flight finding. The location is the FIRST water sample - where the line enters it.
    /// AltitudeMSL is NOT set: the elevation under a water class is not a claim worth making.
    /// </summary>
    public static string BuildWaterFindingReport(string unitUuid, string unitName, string taskName,
                                                 LegMetrics leg, string isoDateTime, string reportId)
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
                            new S.ObservationType
                            {
                                Item = new S.LocationObservationType
                                {
                                    ActorReference = actor,
                                    Location = new S.LocationType
                                    {
                                        Item = new S.GeodeticCoordinateType
                                        {
                                            Latitude = Round(leg.WaterFirst.Lat, 6),
                                            Longitude = Round(leg.WaterFirst.Lon, 6),
                                        }
                                    }
                                }
                            },
                            new S.ObservationType
                            {
                                Item = new S.NameObservationType
                                {
                                    ActorReference = actor,
                                    Marking = WaterMarking(taskName, unitName, leg),
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
    /// THE WATER EMISSION POLICY, pure and on its own so BOTH readers can use it: one
    /// ReportBody per leg that carries a water sample, in leg order, and nothing for a leg that
    /// does not. Separate from <see cref="BuildForTask"/> because the lateral shift's reader
    /// emits per FLAGGED leg and would otherwise stay silent about water it never flagged.
    /// </summary>
    public static List<string> BuildWaterFindings(string unitUuid, string unitName, string taskName,
                                                  IReadOnlyList<LegMetrics> legs,
                                                  string isoDateTime, Func<string> newReportId)
    {
        var outp = new List<string>();
        if (legs == null) return outp;
        foreach (var leg in legs)
            if (leg.WaterSamples > 0)
                outp.Add(BuildWaterFindingReport(unitUuid, unitName, taskName, leg,
                                                 isoDateTime, newReportId()));
        return outp;
    }

    /// <summary>
    /// THE EMISSION POLICY, in one pure function: at most ONE ReportBody per leg, and nothing
    /// at all for a leg that passed or that got NO VERDICT. A leg without a verdict is silent on
    /// purpose - missing tiles are not evidence of good ground, and inventing a warning from them
    /// would be exactly the false alarm DEMO_READINESS row 20 is guarding against.
    ///
    /// WATER OUTRANKS THE GRADE FLAG on the same leg. A water sample inside the worst window
    /// derates the limit to zero, so the ratio is +infinity and the grade sentence would read
    /// "sustained 0.03 vs limit 0.000" - true arithmetic, useless English. The water sentence is
    /// the one that names what is actually there, so it REPLACES the flag report rather than
    /// joining it, and it is also emitted for a leg whose water lies outside the worst window and
    /// which is therefore not flagged at all.
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
            if (leg.WaterSamples > 0)
                outp.Add(BuildWaterFindingReport(task.UnitUuid, task.UnitName, task.TaskName,
                                                 leg, isoDateTime, newReportId()));
            else if (leg.Flagged)
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
        => (s.FlagWater
              // RL-20260927-01: a leg flagged for OSM water says what it went round; a ratio is not
              // the reason (and on the aggregate profile there is no ratio at all).
              ? $"ROUTE SHIFT: task {taskName} ({unitName}) leg {s.LegIndex} - the authored line was flagged " +
                $"({s.FlagReason}); the interface detoured it {F(Math.Abs(s.OffsetMeters), 0)} m {s.SideWord} of the " +
                "authored line onto a line clear of OSM water"
              : $"ROUTE SHIFT: task {taskName} ({unitName}) leg {s.LegIndex} - the authored line scored " +
                $"{F(s.BaseRatio, 3)} against this unit's own limit; the interface detoured it {F(Math.Abs(s.OffsetMeters), 0)} m " +
                $"{s.SideWord} of the authored line around that window, scoring {F(s.ShiftedRatio, 3)}") +
           (double.IsNaN(s.BandMax) ? "" : $" (formation band max {F(s.BandMax, 3)})") +
           (s.BandNotCleared
              ? $" - NOTE: no offset on the {s.SideWord} side could also clear the formation band, so this "
                + "detour is chosen on the route line alone and some formation slots may sit on flagged ground"
              : "") +
           (s.EndpointMoved
              ? ". STP's vertices are kept in order - an endpoint of this leg was moved off water or a building " +
                "and is reported separately; the detour lies between them."
              : ". STP's own vertices are unchanged and in order; the detour lies between them.");

    /// <summary>The Marking of a flagged leg NO offset in the band could clear.</summary>
    public static string NoShiftMarking(string taskName, string unitName, LegShift s)
    {
        if (s.RiverCrossing)
            return $"ROUTE SHIFT NOT APPLIED - RIVER CROSSING: task {taskName} ({unitName}) leg {s.LegIndex} - " +
                   $"{s.FlagReason}, and OSM water lies on the line at BOTH ends of the +/-{F(s.BandSearchedMeters, 0)} m " +
                   "lateral band, so no lateral detour can clear it: this needs a road/bridge crossing - STP " +
                   "authoring. Nothing was searched; the task is dispatched on the line as authored.";
        if (s.FlagWater)
            return $"ROUTE SHIFT NOT APPLIED: task {taskName} ({unitName}) leg {s.LegIndex} - the leg was flagged " +
                   $"({s.FlagReason}) and no line clear of OSM water was found within +/-{F(s.BandSearchedMeters, 0)} m. " +
                   "The task is dispatched on the line as authored.";
        return $"ROUTE SHIFT NOT APPLIED: task {taskName} ({unitName}) leg {s.LegIndex} - the leg scored " +
               $"{F(s.BaseRatio, 3)} and no cleared line was found within +/-{F(s.BandSearchedMeters, 0)} m" +
               (double.IsNaN(s.BestRatioTried) ? ""
                  : $" (best candidate {F(s.BestOffsetTried, 0)} m at {F(s.BestRatioTried, 3)})") +
               ". The task is dispatched on the line as authored.";
    }

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
        // RL-20260927-01: a leg flagged for WATER is located where the water starts, and carries no
        // altitude - the slope window's height is not a claim about that point.
        bool water = shift.FlagWater && leg.Osm != null;
        double lat = shift.Shifted ? shift.In.Lat : water ? leg.Osm.WaterFirst.Lat : leg.WorstLat;
        double lon = shift.Shifted ? shift.In.Lon : water ? leg.Osm.WaterFirst.Lon : leg.WorstLon;
        bool withAltitude = !water && !double.IsNaN(leg.WorstZM);
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
                                            AltitudeMSL = withAltitude ? Round(leg.WorstZM, 1) : 0.0,
                                            AltitudeMSLSpecified = withAltitude,
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

    // ============= THE ROUTE-EXTENT REFUSAL (STP-833; RouteExtentPolicy) ====================
    // A REFUSAL is the loudest thing this interface does to an order, so it is never silent to
    // the C2 side either: the same Location + Name observation pair as a warning or a shift, with
    // the location at the OFFENDING VERTEX - the coordinate the operator has to correct - and the
    // whole refusal sentence in the Marking. The TASKABRT that accompanies it is pushed through
    // the single emit point by the service, exactly as Q4's malformed-task refusal is.

    /// <summary>
    /// One refused task -> one bare ReportBody. AltitudeMSL is NOT set: the vertex is refused
    /// BEFORE the terrain profile authors an altitude for it, so there is no altitude to report
    /// and inventing 0 would put a sea-level claim on a Mojave coordinate.
    /// </summary>
    public static string BuildRouteExtentRefusalReport(string unitUuid, string unitName, string taskName,
                                                       RouteExtentPolicy.Verdict verdict,
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
                            // WHERE: the vertex that broke the bound.
                            new S.ObservationType
                            {
                                Item = new S.LocationObservationType
                                {
                                    ActorReference = actor,
                                    Location = new S.LocationType
                                    {
                                        Item = new S.GeodeticCoordinateType
                                        {
                                            Latitude = Round(verdict.Lat, 6),
                                            Longitude = Round(verdict.Lon, 6),
                                        }
                                    }
                                }
                            },
                            // WHAT: the refusal sentence, verbatim.
                            new S.ObservationType
                            {
                                Item = new S.NameObservationType
                                {
                                    ActorReference = actor,
                                    Marking = RouteExtentPolicy.RefusalMarking(unitName, taskName, verdict),
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

    // ============= RL-20260927-01: THE VERTEX CHECK, OSM WATER AND AGGREGATE SLOW TERRAIN =========
    // Every one of these is the same Location + Name observation pair as a warning or a shift. A MOVED
    // vertex changes where a unit drives and a KEPT bad vertex is a place a unit may stop, so neither is
    // ever silent to the C2 side; an UNVERIFIED vertex (tiles not readable) is logged, not reported -
    // missing tiles are not evidence about the ground either way.

    /// <summary>The Marking of a checked vertex: what was wrong, what moved, from where to where, why.</summary>
    public static string VertexMarking(string taskName, string unitName, VertexNudge n)
        => n.Moved
            ? $"VERTEX MOVED: task {taskName} ({unitName}) route vertex {n.RouteIndex} - the authored vertex " +
              $"({F(n.From.Lat, 6)},{F(n.From.Lon, 6)}) lies {n.Why}. The interface moved it {F(n.DistanceM, 0)} m " +
              $"{n.Compass} to ({F(n.To.Lat, 6)},{F(n.To.Lon, 6)}), the nearest ground {n.ClearOf} (ring search in " +
              $"{F(n.SearchedMeters, 0)} m; {n.Tried} point(s) tried). Every other vertex is unchanged and in order."
            : $"VERTEX NOT MOVED: task {taskName} ({unitName}) route vertex {n.RouteIndex} - the authored vertex " +
              $"({F(n.From.Lat, 6)},{F(n.From.Lon, 6)}) lies {n.Why}, and no ground {n.ClearOf} was found within " +
              $"{F(n.SearchedMeters, 0)} m ({n.Tried} point(s) tried: {n.RefusedWater} in water, {n.RefusedBuilding} " +
              $"at a building, {n.RefusedSlope} on flagged slope, {n.RefusedUnknown} unknown). The task is dispatched " +
              "to the authored vertex - this is an STP authoring defect to fix at the source.";

    /// <summary>One checked vertex -> one ReportBody, located at where the vertex NOW is (the new
    /// point for a move, the authored one when it was kept).</summary>
    public static string BuildVertexReport(string unitUuid, string unitName, string taskName, VertexNudge n,
                                           string isoDateTime, string reportId)
        => BuildPair(unitUuid, unitName, isoDateTime, reportId, n.Moved ? n.To : n.From, null,
                     VertexMarking(taskName, unitName, n));

    /// <summary>The Marking of a leg with OSM water on (or, entity level, beside) its line.</summary>
    public static string OsmWaterMarking(string taskName, string unitName, LegMetrics leg, ModelSetRules rules)
    {
        var o = leg.Osm;
        string where = rules.WaterCorridorMeters > 0
            ? $"within {F(rules.WaterCorridorMeters, 0)} m of the line"
            : "ON the line";
        string what = o.WaterIsRiverLine ? o.WaterKind : $"{o.WaterKind}, {OsmVendor.WaterDescription(o.WaterValue)}";
        string head = $"ROUTE PRE-FLIGHT - OSM WATER ON THE LINE: task {taskName} ({unitName}) leg {leg.Index} - OSM " +
                      $"{o.WaterId} ({what}) lies {where}, first at {F(o.WaterFirstSM / 1000.0, 2)} km along " +
                      $"({o.WaterSamples} of {o.Samples} sample(s)). ";
        string why = rules.ModelSet == ModelSet.AggregateTacticalLevel
            ? "It is MAK_WATERWAY: speed-factor 0 in the aggregate movement table (tank-aggregated-movement.sysdef), " +
              "so an aggregate unit STOPS at its edge."
            : "The terrain loads it as a VRFSIM Lake feature (MAK_WATERWAY, an obstacle to the ground avoider) and, " +
              "for deep-water classes, a soil of acceleration-factor 0.000 - a ground vehicle driven onto it STOPS.";
        return head + why + " This is a pre-flight estimate off the OSM tiles the sim streams, not a vendor verdict; " +
               "whether the leg was detoured is reported separately.";
    }

    /// <summary>One leg with OSM water -> one ReportBody, located where the water starts. No altitude.</summary>
    public static string BuildOsmWaterReport(string unitUuid, string unitName, string taskName, LegMetrics leg,
                                             ModelSetRules rules, string isoDateTime, string reportId)
        => BuildPair(unitUuid, unitName, isoDateTime, reportId, leg.Osm.WaterFirst, null,
                     OsmWaterMarking(taskName, unitName, leg, rules));

    /// <summary>The Marking of an aggregate leg through RESTRICTED_L2 land use (REPORT only).</summary>
    public static string SlowTerrainMarking(string taskName, string unitName, LegMetrics leg)
    {
        var o = leg.Osm;
        return $"ROUTE PRE-FLIGHT - EXPECTED SLOW (AggregateTacticalLevel): task {taskName} ({unitName}) leg {leg.Index} - " +
               $"{F(o.SlowM, 0)} m of the {F(o.LengthM, 0)} m centreline lies in OSM land use the aggregate model " +
               $"slows to speed-factor {F(ModelSetRules.AggregateSlowSpeedFactor, 2)} (forest {F(o.ForestM, 0)} m, " +
               $"swamp {F(o.SwampM, 0)} m, municipal {F(o.MunicipalM, 0)} m: MAK_TANK_RESTRICTED_L2_TERRAIN). " +
               "Reported only - nothing is flagged, refused or altered.";
    }

    public static string BuildSlowTerrainReport(string unitUuid, string unitName, string taskName, LegMetrics leg,
                                                string isoDateTime, string reportId)
        => BuildPair(unitUuid, unitName, isoDateTime, reportId, leg.Start, null,
                     SlowTerrainMarking(taskName, unitName, leg));

    /// <summary>
    /// THE PRE-DISPATCH EMISSION POLICY (RL-20260927-01), pure. In order: one report per vertex the
    /// check MOVED or had to KEEP on bad ground (none for a clear or unverified one); one per leg the
    /// shift acted on or declined (BuildForShift, unchanged); one per leg with OSM water under the
    /// model set's rule; on the aggregate profile, one per leg with RESTRICTED_L2 land use. A flagged
    /// wet leg gets both its shift row and its water row - both are true, as with CLCplus water.
    /// </summary>
    public static List<string> BuildForPreDispatch(string unitUuid, string unitName, string taskName,
                                                   PreDispatchOutcome o, ModelSetRules rules,
                                                   string isoDateTime, Func<string> newReportId)
    {
        var outp = new List<string>();
        if (o == null) return outp;
        foreach (var n in o.Vertices)
            if (n.Moved || n.Unresolved)
                outp.Add(BuildVertexReport(unitUuid, unitName, taskName, n, isoDateTime, newReportId()));
        outp.AddRange(BuildForShift(unitUuid, unitName, taskName, o.Shift.Shifts, o.Shift.Legs,
                                    isoDateTime, newReportId));
        outp.AddRange(BuildOsmFindings(unitUuid, unitName, taskName, o.Shift.Legs, rules, isoDateTime, newReportId));
        return outp;
    }

    /// <summary>The OSM leg findings on their own (both readers use them): water under the model set's
    /// rule, and - aggregate only - expected-slow land use.</summary>
    public static List<string> BuildOsmFindings(string unitUuid, string unitName, string taskName,
                                                IReadOnlyList<LegMetrics> legs, ModelSetRules rules,
                                                string isoDateTime, Func<string> newReportId)
    {
        var outp = new List<string>();
        if (legs == null) return outp;
        foreach (var leg in legs)
        {
            if (leg.Osm == null) continue;
            if (leg.Osm.Water)
                outp.Add(BuildOsmWaterReport(unitUuid, unitName, taskName, leg, rules, isoDateTime, newReportId()));
            if (rules.ReportSlowTerrain && leg.Osm.SlowM > 0)
                outp.Add(BuildSlowTerrainReport(unitUuid, unitName, taskName, leg, isoDateTime, newReportId()));
        }
        return outp;
    }

    /// <summary>The Location + Name pair every pre-flight finding is, built once for the new findings.
    /// The older builders above are left exactly as they were: their bodies are pinned byte for byte
    /// against leg_check.py's emitter.</summary>
    private static string BuildPair(string unitUuid, string unitName, string isoDateTime, string reportId,
                                    (double Lat, double Lon) where, double? altitudeMsl, string marking)
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
                            new S.ObservationType
                            {
                                Item = new S.LocationObservationType
                                {
                                    ActorReference = actor,
                                    Location = new S.LocationType
                                    {
                                        Item = new S.GeodeticCoordinateType
                                        {
                                            AltitudeMSL = altitudeMsl.HasValue ? Round(altitudeMsl.Value, 1) : 0.0,
                                            AltitudeMSLSpecified = altitudeMsl.HasValue,
                                            Latitude = Round(where.Lat, 6),
                                            Longitude = Round(where.Lon, 6),
                                        }
                                    }
                                }
                            },
                            new S.ObservationType
                            {
                                Item = new S.NameObservationType
                                {
                                    ActorReference = actor,
                                    Marking = marking,
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

    /// <summary>Half-to-even, like python's "%.*f", so the two emitters agree digit for digit.</summary>
    private static double Round(double v, int digits)
        => double.IsNaN(v) || double.IsInfinity(v) ? v : Math.Round(v, digits, MidpointRounding.ToEven);

    private static string F(double v, int digits)
        => Round(v, digits).ToString("F" + digits.ToString(CultureInfo.InvariantCulture),
                                     CultureInfo.InvariantCulture);
}
