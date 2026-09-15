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
    public bool AllowLifeforms { get; init; }
    public string FriendlyNation { get; init; } = "USA";
    public string OpposingNation { get; init; } = "RUS";
}

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

    public PreflightOptions Options => _opt;
    public TileSource Tiles => _tiles;
    public SoilChain Soil => _soil;
    public VendorSms Sms => _sms;

    /// <summary>The vehicle-only proxies a DI-Guy lifeform template falls back to.</summary>
    public const string FriendlyLifeformProxy = "Tank Platoon (USA)";
    public const string HostileLifeformProxy = "Tank Platoon (RUS)";

    public PreflightService(PreflightOptions opt)
    {
        _opt = opt;
        _tiles = new TileSource(opt.CacheDir, opt.Offline, opt.Nearest);
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

    /// <summary>Sample one leg's straight line and score it. I/O per sample; PURE arithmetic after.</summary>
    public LegMetrics ScoreLeg((double Lat, double Lon) a, (double Lat, double Lon) b, double limitRaw)
    {
        double length = TileMath.DistanceMeters(a.Lat, a.Lon, b.Lat, b.Lon);
        int n = LegScorer.SampleCount(length, _opt.StepM);
        var samples = new List<LegSample>(n);
        for (int i = 0; i < n; i++)
        {
            double s = LegScorer.SampleDistance(length, i, n);
            double f = length == 0 ? 0.0 : s / length;
            var (la, lo) = TileMath.Interpolate(a.Lat, a.Lon, b.Lat, b.Lon, f);
            samples.Add(new LegSample(s, la, lo, _tiles.Elevation(la, lo), _soil.Classify(_tiles, la, lo)));
        }
        return LegScorer.Score(a, b, samples, length, limitRaw, _opt.StepM, _opt.WindowM,
                               _opt.ShortWindowM, _opt.Threshold);
    }

    /// <summary>
    /// Score a route the interface is about to drive. A segment under
    /// <see cref="LegScorer.MinLegM"/> is NOT a leg - it is a chained start sitting on the
    /// task's own first vertex - and is skipped and counted.
    /// </summary>
    public (List<LegMetrics> Legs, int Degenerate) ScoreRoute(IReadOnlyList<(double Lat, double Lon)> route,
                                                              double limitRaw)
    {
        var legs = new List<LegMetrics>();
        int degenerate = 0;
        for (int i = 0; i + 1 < (route?.Count ?? 0); i++)
        {
            if (TileMath.DistanceMeters(route[i].Lat, route[i].Lon,
                                        route[i + 1].Lat, route[i + 1].Lon) < LegScorer.MinLegM)
            { degenerate++; continue; }
            legs.Add(ScoreLeg(route[i], route[i + 1], limitRaw) with { Index = i + 1 });
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
            var (legs, _) = ScoreRoute(poly, limitRaw);
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
        var (legs, degenerate) = ScoreRoute(route, limitRaw);
        var shifts = new List<LegShift>();
        var scorer = WorstRatioScorer(limitRaw);
        foreach (var leg in legs)
        {
            if (!leg.Flagged) continue;
            int i = leg.Index - 1;
            if (i < 0 || i + 1 >= route.Count) continue;
            shifts.Add(RouteShift.ChooseForLeg(route[i], route[i + 1], leg, shiftOptions, scorer));
        }
        var applied = RouteShift.Apply(route, shifts);
        return new RouteShiftOutcome(applied, shifts, legs, degenerate);
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
