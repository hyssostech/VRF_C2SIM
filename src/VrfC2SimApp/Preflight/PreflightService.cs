using System.Globalization;

namespace VrfC2SimApp.Preflight;

/// <summary>Everything the pre-flight needs to run, resolved once at construction.</summary>
public sealed record PreflightOptions
{
    public string CacheDir { get; init; } = "";
    public string VrfHome { get; init; } = @"C:\MAK\vrforces5.2d";
    public string SharedData { get; init; } = @"C:\MAK\SharedData\19\latest";
    public double StepM { get; init; } = LegScorer.DefaultStepM;
    public double WindowM { get; init; } = LegScorer.DefaultWindowM;
    public double ShortWindowM { get; init; } = LegScorer.DefaultShortWindowM;
    public double Threshold { get; init; } = LegScorer.DefaultThreshold;
    public double DropOriginMeters { get; init; } = 100.0;
    public bool Offline { get; init; }
    public bool Nearest { get; init; }

    /// <summary>
    /// Where the elevation cascade STARTS and STOPS (Vrf:PreflightElevationLevel /
    /// Vrf:PreflightElevationMinLevel). The defaults reproduce the Mojave calibration exactly;
    /// the fallback is what makes the pre-flight AO-independent (Suwalki serves L12, not L13).
    /// </summary>
    public int ElevationLevel { get; init; } = TileMath.DefaultElevationLevel;
    public int ElevationMinLevel { get; init; } = TileMath.DefaultMinElevationLevel;
    public bool AllowLifeforms { get; init; }
    public string FriendlyNation { get; init; } = "USA";
    public string OpposingNation { get; init; } = "RUS";

    // ---- RL-20260927-01: OSM FEATURES, THE VERTEX CHECK AND THE MODEL-SET LEG RULE ------------

    /// <summary>Vrf:ModelSet - which rule set flags a leg (ModelSetRules).</summary>
    public ModelSet ModelSet { get; init; } = ModelSet.EntityLevel;

    /// <summary>
    /// Read the OSM water / building / land-use tiles. DEFAULT FALSE HERE, on purpose: every
    /// fixture comparison against tools/preflight/leg_check.py runs with the tool's defaults, and the
    /// tool's OSM readers are opt-in (--osm-water / --osm-buildings). The interface turns this ON
    /// (VrfC2SimService.GetPreflight), so every live pre-dispatch stage reads them.
    /// </summary>
    public bool OsmFeatures { get; init; }

    /// <summary>Vrf:PreflightBuildingClearanceMeters: a vertex whose nearest OSM footprint is
    /// closer than this is bad ground (leg_check.py's DEF_BUILDING_CLEARANCE).</summary>
    public double BuildingClearanceMeters { get; init; } = 10.0;

    /// <summary>Vrf:PreflightVertexNudgeMaxMeters; 0 = check and report, never move.</summary>
    public double VertexNudgeMaxMeters { get; init; } = 300.0;

    /// <summary>The ring spacing of the nudge search (25 m, the vendor's slot spacing).</summary>
    public double VertexNudgeStepMeters { get; init; } = 25.0;

    /// <summary>Vrf:PreflightSlotWaterClearanceMeters (RL-20261004-01): a populated container's MEMBER SLOT
    /// (CheckSlot) within this distance of OSM water is bad ground, and its nudge target must lie farther than
    /// this from every OSM water feature. 10 m = the planner's obstacle buffer (RL-20260928-03). 0 = the rule
    /// before RL-20261004-01 (a slot is bad only IN the water). Authored route VERTICES do not read it.</summary>
    public double SlotWaterClearanceMeters { get; init; } = 10.0;

    /// <summary>Self-test seam: read the OSM sets from here instead of CacheDir (empty = CacheDir,
    /// the shipped layout: &lt;cache&gt;/osm-water and &lt;cache&gt;/osm next to the raster tiles).</summary>
    public string OsmCacheDir { get; init; } = "";
}

/// <summary>
/// The whole pre-dispatch stage for one route (RL-20260927-01): the vertex check, then the lateral
/// shift on the checked route. <see cref="Vertices"/> holds one row per authored vertex that was
/// checked (clear rows included, so a log can say how many were looked at).
/// </summary>
public sealed record PreDispatchOutcome(List<VertexNudge> Vertices,
                                        List<(double Lat, double Lon)> CheckedRoute,
                                        PreflightService.RouteShiftOutcome Shift)
{
    public int MovedCount => Vertices.Count(v => v.Moved);
    public int UnresolvedCount => Vertices.Count(v => v.Unresolved);
    public int UnverifiedCount => Vertices.Count(v => v.Unverified);
    public bool Changed => MovedCount > 0 || Shift.Changed;
}

/// <summary>
/// One populated container's MEMBER SLOT after the slot check (PreflightService.CheckSlot): where the member is
/// created, and the verdict clause its L-SLOT line prints ("clear", "SLOT MOVED ...", "KEPT ON BAD GROUND ...",
/// "UNVERIFIED ...").
/// </summary>
public sealed record SlotCheck(VertexNudge Nudge, (double Lat, double Lon) Point, string Verdict);

/// <summary>The vehicle limit resolved for one unit, with the note that explains it.</summary>
public sealed record UnitLimit(string Template, double LimitRaw, IReadOnlyList<string> Vehicles, string Note);

/// <summary>One task's pre-flight: the route as it will be driven, and every leg's score.</summary>
public sealed record TaskPreflight
{
    public string TaskName { get; init; } = "";
    public string TaskUuid { get; init; } = "";
    public string UnitName { get; init; } = "";
    public string UnitUuid { get; init; } = "";
    public string Template { get; init; } = "";
    public double LimitRaw { get; init; }
    public string VehicleNote { get; init; } = "";
    public string StartSource { get; init; } = "";
    public int DroppedVertices { get; init; }
    public int DegenerateLegs { get; init; }
    public List<(double Lat, double Lon)> Route { get; init; } = new();
    public List<LegMetrics> Legs { get; init; } = new();
    public string Note { get; init; } = "";
}

/// <summary>
/// ORCHESTRATION: turns a route into scored legs by pulling the tiles the scorer needs.
/// The I/O (tiles, vendor files) is all behind <see cref="TileSource"/>/<see cref="SoilChain"/>/
/// <see cref="VendorSms"/>; the arithmetic is all in the pure <see cref="LegScorer"/>. This
/// class only decides WHICH points to sample.
///
/// *** NEVER CALL THIS FROM THE VR-FORCES TICK THREAD. *** A cold leg fetches tiles over
/// HTTP; on the tick thread that stalls the simulation. The service path runs it on a worker
/// and pushes the reports from there.
/// </summary>
public sealed class PreflightService : IDisposable
{
    private readonly PreflightOptions _opt;
    private readonly TileSource _tiles;
    private readonly SoilChain _soil;
    private readonly VendorSms _sms;
    private readonly OsmTileProvider _osm;

    public PreflightOptions Options => _opt;
    public TileSource Tiles => _tiles;
    public SoilChain Soil => _soil;
    public VendorSms Sms => _sms;

    /// <summary>The vehicle-only proxies a DI-Guy lifeform template falls back to.</summary>
    public const string FriendlyLifeformProxy = "Tank Platoon (USA)";
    public const string HostileLifeformProxy = "Tank Platoon (RUS)";

    public PreflightService(PreflightOptions opt) : this(opt, null) { }

    /// <summary>
    /// The form that takes an HttpClient, so a self-test can put a SCRIPTED server behind the
    /// tile source and assert what a timeout or a 5xx does to a leg's verdict (F2) without a
    /// network, a port or a wall-clock wait. null = the ordinary curl-like client.
    /// </summary>
    internal PreflightService(PreflightOptions opt, HttpClient http) : this(opt, http, null) { }

    /// <summary>
    /// RL-20260927-01 self-test seam, the same shape as the HttpClient one: a SYNTHETIC OSM world in
    /// place of the tile cache, so the leg rule, the vertex check and the river test can be driven on
    /// ground built for the purpose. null = the tile source's own OSM tiles (production).
    /// </summary>
    internal PreflightService(PreflightOptions opt, HttpClient http, OsmTileProvider osmOverride)
    {
        _opt = opt;
        _tiles = new TileSource(opt.CacheDir, opt.Offline, opt.Nearest, http,
                                opt.ElevationLevel, opt.ElevationMinLevel,
                                string.IsNullOrEmpty(opt.OsmCacheDir) ? null : opt.OsmCacheDir);
        _osm = osmOverride ?? _tiles.OsmProvider;
        _soil = new SoilChain(opt.SharedData, opt.VrfHome);
        _sms = new VendorSms(Path.Combine(opt.VrfHome, "data", "simulationModelSets",
                                          "EntityLevel", "vrfSim"));
    }

    /// <summary>
    /// The performing unit's own limit on level ground. The lifeform proxy is a FALLBACK for a
    /// type map that still carries DI-Guy rows: no vendor ground vehicle exceeds max-slope 1.0
    /// and every lifeform is 1.5 or 1.57, so the test is unambiguous. The runs themselves used
    /// data/unit-type-map-52-nolifeform.json, which has the substitution baked in already.
    /// </summary>
    public UnitLimit LimitFor(string template, bool hostile)
    {
        double limitRaw;
        List<(string Label, double? MaxSlope)> veh = new();
        string note = "";
        if (_sms.Ok && !string.IsNullOrEmpty(template))
        {
            var (lim, v) = _sms.MinMaxSlope(template);
            veh = v;
            if (lim.HasValue && lim.Value >= VendorSms.LifeformSlope && !_opt.AllowLifeforms)
            {
                string proxy = hostile ? HostileLifeformProxy : FriendlyLifeformProxy;
                var (lim2, veh2) = _sms.MinMaxSlope(proxy);
                if (lim2.HasValue)
                {
                    note = $"lifeform template '{template}' (max-slope {lim.Value:F2}) replaced by the " +
                           $"vehicle-only proxy '{proxy}' - the DI-Guy data package is absent and the sim " +
                           "crashes on the first human; P11 ran the same substitution";
                    template = proxy;
                    lim = lim2;
                    veh = veh2;
                }
            }
            if (lim.HasValue)
                return new UnitLimit(template, lim.Value,
                                     veh.Select(x => x.Label).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList(), note);
        }
        limitRaw = VendorSms.MaxSlopeFallbackMin;
        note = $"FALLBACK max-slope {limitRaw:F2} - template '{template}' did not resolve to vehicles";
        return new UnitLimit(template, limitRaw,
                             veh.Select(x => x.Label).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList(), note);
    }

    /// <summary>
    /// Sample one leg's straight line and score it. I/O per sample; PURE arithmetic after.
    ///
    /// THE ELEVATION LEVEL IS RECORDED, NOT ASSUMED. Each sample is taken at the deepest level
    /// of the cascade that serves its area; the leg carries out the COARSEST level any of its
    /// samples used, and 0 when NO level served any of them - which is the case the caller has
    /// to shout about, because a leg nothing could be sampled for is not a clear leg.
    /// </summary>
    public LegMetrics ScoreLeg((double Lat, double Lon) a, (double Lat, double Lon) b, double limitRaw)
        => ScoreLeg(a, b, limitRaw, withOsm: true);

    /// <summary>The model set's rules (Vrf:ModelSet; RL-20260927-01).</summary>
    public ModelSetRules Rules => ModelSetRules.For(_opt.ModelSet);

    /// <summary>
    /// As <see cref="ScoreLeg((double Lat, double Lon), (double Lat, double Lon), double)"/>, and -
    /// when the OSM readers are on and <paramref name="withOsm"/> - the leg's OSM features and the
    /// model set's verdict on it. A candidate polyline's scorer passes false: it asks the OSM question
    /// itself, exactly, without the samples a report needs.
    /// </summary>
    public LegMetrics ScoreLeg((double Lat, double Lon) a, (double Lat, double Lon) b, double limitRaw,
                               bool withOsm)
    {
        var slope = ScoreSlope(a, b, limitRaw);
        var osm = withOsm && _opt.OsmFeatures
            ? OsmQuery.Leg(_osm, a, b, Rules, _opt.StepM)
            : null;
        var leg = slope with { Osm = osm };
        var (flagged, fs, fw, reason) = Rules.FlagLeg(leg);
        return leg with { ShiftFlagged = flagged, FlagSlope = fs, FlagWater = fw, FlagReason = reason };
    }

    private LegMetrics ScoreSlope((double Lat, double Lon) a, (double Lat, double Lon) b, double limitRaw)
    {
        double length = TileMath.DistanceMeters(a.Lat, a.Lon, b.Lat, b.Lon);
        int n = LegScorer.SampleCount(length, _opt.StepM);
        var samples = new List<LegSample>(n);
        int coarsest = 0, served = 0, fetchFailures = 0;
        for (int i = 0; i < n; i++)
        {
            double s = LegScorer.SampleDistance(length, i, n);
            double f = length == 0 ? 0.0 : s / length;
            var (la, lo) = TileMath.Interpolate(a.Lat, a.Lon, b.Lat, b.Lon, f);
            double z = _tiles.Elevation(la, lo, out int level, out bool fetchFailed);
            if (fetchFailed) fetchFailures++;
            if (level > 0)
            {
                served++;
                if (coarsest == 0 || level < coarsest) coarsest = level;
            }
            samples.Add(new LegSample(s, la, lo, z, _soil.Classify(_tiles, la, lo)));
        }
        var scored = LegScorer.Score(a, b, samples, length, limitRaw, _opt.StepM, _opt.WindowM,
                                     _opt.ShortWindowM, _opt.Threshold);
        // F2: a leg that lost even ONE sample to a FETCH FAILURE has NO VERDICT, whatever the
        // NaN-fraction tolerance says and whatever level its other samples resolved at. The
        // alternative is a ratio computed off a partially-unknown line, or - worse, and what used
        // to happen - a ratio computed one level coarser because the failure was read as absence.
        // The ELEVATION LEVEL is forced to 0 with it: "the coarsest level some samples used" is
        // not a description of a leg nothing could be established for.
        return scored with
        {
            ElevationLevel = fetchFailures > 0 ? 0 : (served == 0 ? 0 : coarsest),
            ElevationFetchFailures = fetchFailures,
            NoVerdict = scored.NoVerdict || fetchFailures > 0,
        };
    }

    /// <summary>
    /// Score a route the interface is about to drive. A segment under
    /// <see cref="LegScorer.MinLegM"/> is NOT a leg - it is a chained start sitting on the
    /// task's own first vertex - and is skipped and counted.
    /// </summary>
    public (List<LegMetrics> Legs, int Degenerate) ScoreRoute(IReadOnlyList<(double Lat, double Lon)> route,
                                                              double limitRaw)
        => ScoreRoute(route, limitRaw, withOsm: true);

    public (List<LegMetrics> Legs, int Degenerate) ScoreRoute(IReadOnlyList<(double Lat, double Lon)> route,
                                                              double limitRaw, bool withOsm)
    {
        var legs = new List<LegMetrics>();
        int degenerate = 0;
        for (int i = 0; i + 1 < (route?.Count ?? 0); i++)
        {
            if (TileMath.DistanceMeters(route[i].Lat, route[i].Lon,
                                        route[i + 1].Lat, route[i + 1].Lon) < LegScorer.MinLegM)
            { degenerate++; continue; }
            legs.Add(ScoreLeg(route[i], route[i + 1], limitRaw, withOsm) with { Index = i + 1 });
        }
        return (legs, degenerate);
    }

    /// <summary>
    /// A route as the LATERAL ROUTE SHIFT left it: the vertices to drive, what each flagged leg
    /// became, and the scores of the route as AUTHORED (so a report can say "1.098 -> 0.792").
    /// <see cref="Shifts"/> carries a row per FLAGGED leg - shifted or not - and is empty when
    /// nothing was flagged, in which case <see cref="Route"/> is the input, point for point.
    /// </summary>
    public sealed record RouteShiftOutcome(List<(double Lat, double Lon)> Route,
                                           List<LegShift> Shifts,
                                           List<LegMetrics> Legs,
                                           int Degenerate)
    {
        public int ShiftedCount => Shifts.Count(s => s.Shifted);
        public int UnshiftableCount => Shifts.Count(s => !s.Shifted);
        public bool Changed => ShiftedCount > 0;
    }

    /// <summary>
    /// Score a polyline: its WORST leg ratio and its MISSING-TILE COUNT - the two numbers the pure
    /// chooser needs, and the only way terrain reaches it.
    ///
    /// A polyline carrying any leg with NO VERDICT scores +infinity, i.e. it can never be accepted.
    /// The NaN count is reported separately and in FULL because the chooser is stricter still: a
    /// SHIFT refuses a candidate with a single unknown sample, while the FLAG keeps the calibrated
    /// <see cref="LegScorer.MaxNanFraction"/> tolerance. Missing tiles are not evidence of good
    /// ground, and a shift onto unscored terrain would be exactly the invention this feature exists
    /// to avoid.
    /// </summary>
    public Func<IReadOnlyList<(double Lat, double Lon)>, PolyScore> WorstRatioScorer(double limitRaw)
        => poly =>
        {
            var (legs, _) = ScoreRoute(poly, limitRaw, withOsm: false);
            if (legs.Count == 0) return new PolyScore(double.PositiveInfinity, 0);
            double worst = 0.0;
            int nan = 0;
            bool noVerdict = false;
            foreach (var l in legs)
            {
                nan += l.NanSamples;
                if (l.NoVerdict) noVerdict = true;
                else if (l.Ratio > worst) worst = l.Ratio;
            }
            return new PolyScore(noVerdict ? double.PositiveInfinity : worst, nan);
        };

    /// <summary>
    /// THE PRE-DISPATCH STAGE: score the route the interface is about to drive and, for every
    /// FLAGGED leg, choose a lateral detour and splice its two waypoints in.
    ///
    /// *** NEVER FROM THE VR-FORCES TICK THREAD *** - the same rule as the rest of this class,
    /// and more so: the search scores several candidate polylines.
    ///
    /// It never refuses and never reorders: an unflagged leg is untouched, a flagged leg that no
    /// offset clears is left exactly as authored with its reason on the LegShift, and the
    /// authored vertices are copied through in order either way.
    /// </summary>
    public RouteShiftOutcome ShiftRoute(IReadOnlyList<(double Lat, double Lon)> route,
                                        double limitRaw, RouteShiftOptions shiftOptions)
    {
        var rules = Rules;
        var (legs, degenerate) = ScoreRoute(route, limitRaw);
        var shifts = new List<LegShift>();
        // The aggregate profile scores no slope, so the formation band (a slope criterion) has
        // nothing to say there; everything else in the options is the same search.
        var opt = rules.UseSlope ? shiftOptions : shiftOptions with { ClearFormationBand = false };
        // OSM readers off (every parity run): EXACTLY the pre-RL-20260927-01 scorer and flag.
        var scorer = _opt.OsmFeatures ? CandidateScorer(limitRaw) : WorstRatioScorer(limitRaw);
        // M3 (RL-20260928-03): a route a vendor PLANNING task drives is REPORTED, never detoured - every flagged leg
        // keeps its row (and a river crossing its report), but no lateral search runs and nothing is spliced in.
        bool reportOnly = !string.IsNullOrEmpty(opt.ReportOnlyReason);
        foreach (var leg in legs)
        {
            if (!leg.ShiftFlagged) continue;
            int i = leg.Index - 1;
            if (i < 0 || i + 1 >= route.Count) continue;
            if (!leg.FlagWater)
            {
                if (reportOnly) { shifts.Add(RouteShift.ReportOnlyShift(leg, opt)); continue; }
                // Flagged for slope alone: the pre-OSM search, except that a candidate with KNOWN
                // OSM water on it is refused (a detour is never driven into a known lake).
                shifts.Add(RouteShift.ChooseForLeg(route[i], route[i + 1], leg, opt, scorer));
                continue;
            }
            // Flagged for WATER: the same lateral search round the water's own span (plus the slope
            // window when both flagged), accepting only KNOWN water-clear lines - unless the water
            // runs right across the band, which no lateral detour can clear.
            var legOpt = opt with { RequireFeaturesKnown = true };
            var waterSpan = (leg.Osm.WaterFirstSM, leg.Osm.WaterLastSM);
            if (RouteShift.IsRiverCrossing(route[i], route[i + 1], waterSpan, legOpt,
                                           SameWaterProbe(route[i], route[i + 1]), out string probe))
            {
                shifts.Add(RouteShift.RiverCrossingShift(leg, legOpt, probe));
                continue;
            }
            if (reportOnly) { shifts.Add(RouteShift.ReportOnlyShift(leg, opt)); continue; }
            shifts.Add(RouteShift.ChooseForLeg(route[i], route[i + 1], leg, legOpt, scorer, DetourSpan(leg)));
        }
        var applied = RouteShift.Apply(route, shifts);
        return new RouteShiftOutcome(applied, shifts, legs, degenerate);
    }

    /// <summary>The span a WATER-flagged leg's detour must clear: where its water lies within the
    /// corridor, widened to the slope window when the leg was flagged for both.</summary>
    private static (double Start, double End) DetourSpan(LegMetrics leg)
    {
        double s0 = leg.Osm.WaterFirstSM, s1 = leg.Osm.WaterLastSM;
        if (leg.FlagSlope)
        {
            double w0 = TileMath.DistanceMeters(leg.Start.Lat, leg.Start.Lon, leg.WorstFrom.Lat, leg.WorstFrom.Lon);
            double w1 = TileMath.DistanceMeters(leg.Start.Lat, leg.Start.Lon, leg.WorstTo.Lat, leg.WorstTo.Lon);
            s0 = Math.Min(s0, w0);
            s1 = Math.Max(s1, w1);
        }
        return (s0, s1);
    }

    /// <summary>
    /// THE CANDIDATE SCORER once the OSM readers are on (RL-20260927-01): the slope half exactly as
    /// <see cref="WorstRatioScorer"/> (entity level only - the aggregate scores no slope, and reads no
    /// elevation for a candidate at all), plus the exact OSM water test of the whole polyline.
    /// </summary>
    public Func<IReadOnlyList<(double Lat, double Lon)>, PolyScore> CandidateScorer(double limitRaw)
    {
        var rules = Rules;
        var slope = WorstRatioScorer(limitRaw);
        return poly =>
        {
            double worst = 0.0;
            int nan = 0;
            if (rules.UseSlope)
            {
                var s = slope(poly);
                worst = s.WorstRatio;
                nan = s.NanSamples;
            }
            if (!_opt.OsmFeatures) return new PolyScore(worst, nan);
            var (wet, minD, unknown) = OsmQuery.Polyline(_osm, poly, rules, _opt.StepM);
            return new PolyScore(worst, nan, wet, unknown, minD);
        };
    }

    /// <summary>
    /// The river test's probe for the leg a-&gt;b (no elevation read): is the water on a band-end line the
    /// SAME water - same OSM id, or same non-empty OSM name - as the water within the corridor of the
    /// leg itself? See RouteShift.IsRiverCrossing for why "same" and not "any".
    /// </summary>
    public Func<IReadOnlyList<(double Lat, double Lon)>, RouteShift.BandEndProbe> SameWaterProbe(
        (double Lat, double Lon) a, (double Lat, double Lon) b)
    {
        var rules = Rules;
        var (legHits, _) = OsmQuery.WaterHits(_osm, new[] { a, b }, rules, _opt.StepM);
        var ids = new HashSet<string>(legHits.Select(h => h.Id), StringComparer.Ordinal);
        var names = new HashSet<string>(legHits.Where(h => h.Name.Length > 0).Select(h => h.Name), StringComparer.Ordinal);
        return poly =>
        {
            var (hits, unknown) = OsmQuery.WaterHits(_osm, poly, rules, _opt.StepM);
            var same = hits.Where(h => ids.Contains(h.Id) || (h.Name.Length > 0 && names.Contains(h.Name)))
                           .Select(h => h.Id).Distinct(StringComparer.Ordinal).ToList();
            return new RouteShift.BandEndProbe(same.Count > 0, hits.Count > same.Count, unknown,
                                               string.Join(",", same.Take(3)));
        };
    }

    /// <summary>What <see cref="OsmQuery.Point"/> says about one point under this service's rules.</summary>
    public OsmPointCheck CheckPoint((double Lat, double Lon) p)
        => OsmQuery.Point(_osm, p, Rules, _opt.BuildingClearanceMeters);

    /// <summary>
    /// <see cref="CheckPoint((double Lat, double Lon))"/> with the water corridor widened to at least
    /// <paramref name="waterClearanceM"/>: water within that distance is a hit, and the reported water distance
    /// is measured that far out. The model set's own corridor wins when it is wider (entity level, 25 m).
    /// </summary>
    public OsmPointCheck CheckPoint((double Lat, double Lon) p, double waterClearanceM)
    {
        var rules = Rules;
        if (waterClearanceM > rules.WaterCorridorMeters) rules = rules with { WaterCorridorMeters = waterClearanceM };
        return OsmQuery.Point(_osm, p, rules, _opt.BuildingClearanceMeters);
    }

    /// <summary>
    /// THE MEMBER-SLOT CHECK of a populated container (C1; the caller is VrfC2SimService.CheckPopulateSlots,
    /// off the tick thread): one planned ring slot through the point test and, when it is bad, the nudge search
    /// (VertexNudgeSearch with no neighbours - the first clear point of the smallest ring, by bearing from north).
    /// Unknown ground is never clear and never "bad": an unreadable slot is kept, UNVERIFIED.
    /// </summary>
    public SlotCheck CheckSlot(int slotNumber, (double Lat, double Lon) slot)
    {
        var nopt = new VertexNudgeOptions
        {
            MaxMeters = Math.Max(0.0, _opt.VertexNudgeMaxMeters),
            StepMeters = Math.Max(1.0, _opt.VertexNudgeStepMeters),
        };
        string clearOf = FormattableString.Invariant(
            $"clear of OSM water and of OSM buildings within {_opt.BuildingClearanceMeters:F0} m");
        var n = VertexNudgeSearch.Nudge(slotNumber, slot, null, null, nopt,
                                        p => NudgeVerdict.Of(CheckPoint(p)),
                                        c => NudgeVerdict.Of(CheckPoint(c)),
                                        clearOf);
        if (n.Moved)
            return new SlotCheck(n, n.To, FormattableString.Invariant(
                $"SLOT MOVED {n.DistanceM:F0} m {n.Compass} - the planned slot lies {n.Why}; the new point is the nearest ground {n.ClearOf}"));
        if (n.Unresolved)
            return new SlotCheck(n, slot, FormattableString.Invariant(
                $"KEPT ON BAD GROUND - the slot lies {n.Why} and no ground {n.ClearOf} was found within {n.SearchedMeters:F0} m"));
        if (n.Unverified)
            return new SlotCheck(n, slot, $"UNVERIFIED - {n.Why}; the slot is kept");
        return new SlotCheck(n, slot, n.OnBridge ? n.Why : "clear");
    }

    /// <summary>
    /// "NOT ON FLAGGED SLOPE" for a nudge candidate on the entity profile: the calibrated sustained
    /// window (Vrf:PreflightWindowMeters, 40 m) along the APPROACH to the candidate (from its
    /// predecessor's direction) and along the DEPARTURE from it (towards its successor), each scored
    /// by the same LegScorer against the unit's own limit. Known = both windows had every sample;
    /// flagged = either window's ratio reaches the threshold. Local on purpose: a leg flagged by a
    /// ridge two kilometres away says nothing about whether THIS point is drivable.
    /// </summary>
    public (bool Known, bool Flagged, double Ratio) LocalSlope((double Lat, double Lon) c,
                                                                (double Lat, double Lon)? prev,
                                                                (double Lat, double Lon)? next,
                                                                double limitRaw)
    {
        double w = _opt.WindowM;
        double worst = 0.0;
        bool known = true;
        if (prev.HasValue && TileMath.DistanceMeters(prev.Value.Lat, prev.Value.Lon, c.Lat, c.Lon) >= LegScorer.MinLegM)
        {
            var leg = ScoreSlope(RouteShift.PointAlong(c, prev.Value, w), c, limitRaw);
            if (leg.NoVerdict) known = false; else worst = Math.Max(worst, leg.Ratio);
        }
        if (next.HasValue && TileMath.DistanceMeters(c.Lat, c.Lon, next.Value.Lat, next.Value.Lon) >= LegScorer.MinLegM)
        {
            var leg = ScoreSlope(c, RouteShift.PointAlong(c, next.Value, w), limitRaw);
            if (leg.NoVerdict) known = false; else worst = Math.Max(worst, leg.Ratio);
        }
        return (known, known && worst >= _opt.Threshold, worst);
    }

    /// <summary>
    /// THE VERTEX CHECK (RL-20260927-01): every AUTHORED vertex of the route - index 0, the unit's
    /// own live position, is never touched - is tested against OSM water and buildings under the
    /// model set's rules, and a bad one is nudged to the nearest clear ground (VertexNudgeSearch).
    /// Sequential: a vertex's predecessor is the already-checked one, so the search's "stay on the
    /// lane" cost is measured against the route that will actually be driven.
    /// </summary>
    public List<VertexNudge> CheckVertices(IReadOnlyList<(double Lat, double Lon)> route, double limitRaw,
                                           out List<(double Lat, double Lon)> checkedRoute)
    {
        var rules = Rules;
        var outp = new List<VertexNudge>();
        checkedRoute = route?.ToList() ?? new List<(double Lat, double Lon)>();
        if (!_opt.OsmFeatures || checkedRoute.Count < 2) return outp;
        var nopt = new VertexNudgeOptions
        {
            MaxMeters = Math.Max(0.0, _opt.VertexNudgeMaxMeters),
            StepMeters = Math.Max(1.0, _opt.VertexNudgeStepMeters),
        };
        string clearOf = FormattableString.Invariant(
                             $"clear of OSM water{(rules.WaterCorridorMeters > 0 ? $" within {rules.WaterCorridorMeters:F0} m" : "")}")
                       + FormattableString.Invariant($" and of OSM buildings within {_opt.BuildingClearanceMeters:F0} m")
                       + (rules.UseSlope ? " and not on flagged slope" : "");
        var route0 = checkedRoute;
        for (int i = 1; i < route0.Count; i++)
        {
            var v = route0[i];
            var prev = route0[i - 1];
            (double Lat, double Lon)? next = i + 1 < route0.Count ? route0[i + 1] : null;
            // A chained start that sits ON the task's first vertex is the unit's own position.
            if (TileMath.DistanceMeters(prev.Lat, prev.Lon, v.Lat, v.Lon) < LegScorer.MinLegM) continue;
            var n = VertexNudgeSearch.Nudge(i, v, prev, next, nopt,
                p => NudgeVerdict.Of(CheckPoint(p)),
                c =>
                {
                    var pc = CheckPoint(c);
                    if (!pc.Known || pc.Bad || !rules.UseSlope) return NudgeVerdict.Of(pc);
                    var (known, flagged, ratio) = LocalSlope(c, prev, next, limitRaw);
                    return new NudgeVerdict(known, false, false, flagged,
                        !known ? "slope UNKNOWN (no elevation under the 40 m windows)"
                               : FormattableString.Invariant($"local slope ratio {ratio:F3}"));
                },
                clearOf);
            outp.Add(n);
            if (n.Moved) route0[i] = n.To;
        }
        return outp;
    }

    /// <summary>
    /// THE PRE-DISPATCH STAGE, whole (RL-20260927-01): the vertex check, then the lateral shift on the
    /// checked route. The authored vertices are copied through in order; a vertex the check moved is
    /// the only one that changes, and every shift row whose leg ends on a moved vertex says so.
    /// *** NEVER FROM THE VR-FORCES TICK THREAD *** - it reads tiles.
    /// </summary>
    public PreDispatchOutcome PreDispatch(IReadOnlyList<(double Lat, double Lon)> route, double limitRaw,
                                          RouteShiftOptions shiftOptions)
    {
        var vertices = CheckVertices(route, limitRaw, out var checkedRoute);
        var shift = ShiftRoute(checkedRoute, limitRaw, shiftOptions);
        var moved = new HashSet<int>(vertices.Where(v => v.Moved).Select(v => v.RouteIndex));
        if (moved.Count > 0)
            shift = shift with
            {
                Shifts = shift.Shifts.Select(s => moved.Contains(s.LegIndex) || moved.Contains(s.LegIndex - 1)
                                                  ? s with { EndpointMoved = true } : s).ToList(),
            };
        return new PreDispatchOutcome(vertices, checkedRoute, shift);
    }

    /// <summary>
    /// M3 (RL-20260928-03): TOUCH every tile of one OSM set within <paramref name="bandM"/> of each leg of the route,
    /// through this service's own provider - so an ONLINE run fetches and caches what it lacks, exactly as the water
    /// and building reads do, and an offline run (Vrf:PreflightOffline) reads only its cache. The AUTO planner calls it
    /// for the sim's road set (<see cref="OsmSet.Highways"/>) so that the dispatch-time road check, which never
    /// fetches (OsmCacheReader, on the tick thread), finds the tiles on disk. Returns (known, unknown) tile counts.
    /// *** NEVER FROM THE VR-FORCES TICK THREAD *** - it may fetch.
    /// </summary>
    public (int Known, int Unknown) WarmOsm(OsmSet set, IReadOnlyList<(double Lat, double Lon)> route, double bandM)
    {
        int known = 0, unknown = 0;
        var seen = new HashSet<(int, int)>();
        if (route == null) return (0, 0);
        for (int i = 0; i + 1 < route.Count; i++)
            foreach (var key in OsmQuery.TilesNear(route[i], route[i + 1], bandM))
            {
                if (!seen.Add(key)) continue;
                var t = _osm(set, key.X, key.TmsY);
                if (t != null && t.Known) known++; else unknown++;
            }
        return (known, unknown);
    }

    /// <summary>
    /// The whole-order walk, as tools/preflight/leg_check.py does it: a unit's SECOND and later
    /// tasks start at the end of its previous task's route, because the interface sequences a
    /// unit's tasks in declared order. Used by the self-test; the live path scores the ONE
    /// route it is about to dispatch instead.
    /// </summary>
    public List<TaskPreflight> RunOrder(InitData init, OrderData order, UnitTypeMap typeMap,
                                        IReadOnlyDictionary<string, (double Lat, double Lon)> starts,
                                        Action<string> progress = null)
    {
        // A repeated uuid keeps the LAST unit, as the tool's dict assignment does.
        var unitByUuid = new Dictionary<string, InitUnit>(StringComparer.Ordinal);
        foreach (var u in init.Units) unitByUuid[u.Uuid] = u;
        var chainPos = new Dictionary<string, (double Lat, double Lon)>(StringComparer.Ordinal);
        var results = new List<TaskPreflight>();

        foreach (var task in order.Tasks)
        {
            unitByUuid.TryGetValue(task.TaskeeUuid ?? "", out var unit);
            string performer = task.TaskeeUuid ?? "";
            string uname = unit != null ? unit.Name
                         : "uuid:" + performer.Substring(0, Math.Min(8, performer.Length));
            if (task.Points == null || task.Points.Count == 0)
            {
                results.Add(new TaskPreflight
                {
                    TaskName = task.TaskName, TaskUuid = task.TaskUuid, UnitName = uname,
                    UnitUuid = task.TaskeeUuid, Note = "no route points in the order",
                });
                continue;
            }

            bool hostile = unit != null && unit.HostilityCode == "HO";
            string template = "";
            if (unit != null)
            {
                var match = typeMap.Lookup(UnitTypeMap.FunctionIdOf(unit.SymbolId),
                                           UnitTypeMap.EchelonCharOf(unit.SymbolId),
                                           unit.EchelonCode,
                                           hostile ? "hostile" : "friendly",
                                           hostile ? _opt.OpposingNation : _opt.FriendlyNation);
                template = match.Row?.TemplateName ?? "";
            }
            var limit = LimitFor(template, hostile);

            (double Lat, double Lon)? authored = null;
            if (unit != null
                && double.TryParse(unit.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double alat)
                && double.TryParse(unit.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double alon))
                authored = (alat, alon);

            (double Lat, double Lon) start;
            string startSrc;
            if (chainPos.TryGetValue(uname, out var chained))
            { start = chained; startSrc = "end of this unit's previous task's route"; }
            else if (starts != null && starts.TryGetValue(uname, out var fromRun))
            { start = fromRun; startSrc = "run trace (DeStack-spread position)"; }
            else if (authored.HasValue)
            { start = authored.Value; startSrc = "authored position (initialization)"; }
            else
            { start = (task.Points[0].Lat, task.Points[0].Lon); startSrc = "first route vertex"; }

            var pts = task.Points.Select(p => (p.Lat, p.Lon)).ToList();
            var built = PreflightRoute.Build(start, pts, authored, _opt.DropOriginMeters);
            chainPos[uname] = built.Route[^1];

            progress?.Invoke($"{task.TaskName} ({uname}): {built.Route.Count - 1} leg(s)");
            var (legs, degenerate) = ScoreRoute(built.Route, limit.LimitRaw);

            results.Add(new TaskPreflight
            {
                TaskName = task.TaskName, TaskUuid = task.TaskUuid,
                UnitName = uname, UnitUuid = task.TaskeeUuid,
                Template = limit.Template, LimitRaw = limit.LimitRaw, VehicleNote = limit.Note,
                StartSource = startSrc, DroppedVertices = built.Dropped,
                DegenerateLegs = degenerate, Route = built.Route, Legs = legs,
            });
        }
        return results;
    }

    /// <summary>unit,lat,lon CSV of the REAL start positions (the interface spreads units at init).</summary>
    public static Dictionary<string, (double Lat, double Lon)> LoadStarts(string path)
    {
        var starts = new Dictionary<string, (double, double)>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return starts;
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return starts;
        var header = lines[0].Split(',').Select(h => h.Trim()).ToList();
        int iu = header.IndexOf("unit"), ila = header.IndexOf("lat"), ilo = header.IndexOf("lon");
        if (iu < 0 || ila < 0 || ilo < 0) return starts;
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var p = line.Split(',');
            if (p.Length <= Math.Max(iu, Math.Max(ila, ilo))) continue;
            if (double.TryParse(p[ila], NumberStyles.Float, CultureInfo.InvariantCulture, out double la)
                && double.TryParse(p[ilo], NumberStyles.Float, CultureInfo.InvariantCulture, out double lo))
                starts[p[iu].Trim()] = (la, lo);
        }
        return starts;
    }

    public void Dispose() => _tiles?.Dispose();
}
