namespace VrfC2SimApp.Preflight;

/// <summary>One sampled point along a leg. Immutable data - the scorer never fetches it.</summary>
public sealed record LegSample(double S, double Lat, double Lon, double Z, SoilSample Soil);

/// <summary>Everything one leg scored to. Field names mirror leg_check.py's --json keys.</summary>
public sealed record LegMetrics
{
    public int Index { get; init; }
    public double LengthM { get; init; }
    public int Samples { get; init; }
    public double Sustained { get; init; }          // the measured discriminator
    public double SustainedWindowM { get; init; }
    public double Short { get; init; }              // reported, NEVER the verdict
    public double ShortWindowM { get; init; }
    public double WorstLat { get; init; }
    public double WorstLon { get; init; }
    public double WorstSM { get; init; }
    public double WorstZM { get; init; }
    public (double Lat, double Lon) WorstFrom { get; init; }
    public (double Lat, double Lon) WorstTo { get; init; }
    public string Soil { get; init; } = "";
    public string SoilDesc { get; init; } = "";
    public string SoilSource { get; init; } = "";
    public string SoilSurfChar { get; init; } = "";
    public string SoilType { get; init; } = "";
    public bool SoilAssumed { get; init; }
    public double Factor { get; init; }
    public double LimitRaw { get; init; }           // min max-slope over the unit's vehicles
    public double Limit { get; init; }              // that, derated by the soil in the window
    public double Ratio { get; init; }
    public double ClimbM { get; init; }
    public double DescendM { get; init; }
    public int NanSamples { get; init; }
    public double NanFraction { get; init; }
    public bool NoVerdict { get; init; }
    public bool Flagged { get; init; }
    public (double Lat, double Lon) Start { get; init; }
    public (double Lat, double Lon) End { get; init; }

    // ---- WHICH ELEVATION LEVEL THIS LEG WAS SCORED AT (AO-independent pre-flight) ----------
    // The COARSEST level any sample on this leg resolved to, or 0 for "no elevation data at any
    // level of the cascade". Set by PreflightService, which owns the tiles; the scorer is pure
    // and never asks for one. 0 is NOT a footnote: it means nothing about this leg was checked.
    public int ElevationLevel { get; init; }

    // ---- F2: SAMPLES WHOSE ELEVATION WE NEVER GOT AN ANSWER FOR ---------------------------
    // How many of this leg's samples could not be resolved because a tile FETCH FAILED - a
    // timeout, a connection or DNS failure, a 5xx - as opposed to the server answering "no tile
    // here". The two used to be indistinguishable, so one dropped packet at L13 permanently
    // downgraded a ~2.4 km area to L12 and every later verdict over that ground was quietly
    // scored on a DEM the threshold was never calibrated on.
    //
    // ANY non-zero value makes the leg NO VERDICT (set by PreflightService, which owns the
    // tiles; the scorer is pure and never asks for one). It is NOT folded into NanSamples: the
    // NaN-fraction rule has a calibrated tolerance (MaxNanFraction) for ordinary tile gaps, and
    // a network failure is not a tile gap - it is the instrument not looking.
    public int ElevationFetchFailures { get; init; }

    // ---- WATER (CLCplus class 100 / any catalogue row that resolves to a water soil) --------
    // A vehicle on deep water has acceleration-factor 0.000 in the vendor's own
    // ground-tracked.sysdef, i.e. a dead stop the back end reports as TaskRunning for ever.
    // Water inside the worst window already drives the ratio to +infinity and flags the leg, but
    // water ANYWHERE ELSE on the leg was invisible - hence an explicit count.
    public int WaterSamples { get; init; }
    public double WaterFraction { get; init; }
    public string WaterSoil { get; init; } = "";
    public string WaterSource { get; init; } = "";
    public string WaterDesc { get; init; } = "";
    public (double Lat, double Lon) WaterFirst { get; init; }
    public double WaterFirstSM { get; init; }

    /// <summary>True when any sample of this leg sits on ground the vendor calls water.</summary>
    public bool Water => WaterSamples > 0;
}

/// <summary>
/// THE PURE SCORER. Samples in, metrics out: no tiles, no files, no network, no clock, no
/// logging. Every number it produces is a function of its arguments alone, which is what
/// makes the fixture comparison against tools/preflight/leg_check.py meaningful.
///
/// The metric, from docs/experiments/PREFLIGHT_CALIBRATION_2026-09-13.md:
///   sustained = the largest MEAN UPHILL grade over any sliding 40 m window along the leg;
///   limit     = the performing unit's min max-slope x the acceleration-factor of the worst
///               soil INSIDE that window;
///   ratio     = sustained / limit, flagged at >= 0.92.
/// The verdict rests on SUSTAINED extent, not on a single steep window: FINDING sec 7
/// measured every leader surmounting short pitches well above its limit while no leader ever
/// surmounted a 55 m window above 0.714.
///
/// A flag is an ESTIMATE off terrain tiles, never a vendor verdict, and never a claim about
/// why a unit stopped - two of the four P11 leg-1 stops are on ground this scores as benign.
/// </summary>
public static class LegScorer
{
    /// <summary>Sample spacing along a leg (m).</summary>
    public const double DefaultStepM = 8.0;

    /// <summary>The sustained window (m) - an OPERATING POINT, not a constant (see --sensitivity).</summary>
    public const double DefaultWindowM = 40.0;

    /// <summary>The short window (m): reported, never the verdict.</summary>
    public const double DefaultShortWindowM = 20.0;

    /// <summary>Midpoint of the 0.096-wide gap between frozen and clean-mover ratios in P11.</summary>
    public const double DefaultThreshold = 0.92;

    /// <summary>A route segment shorter than this is not a leg - it is the same point twice.</summary>
    public const double MinLegM = 1.0;

    /// <summary>Above this fraction of missing elevation samples a leg gets NO VERDICT.</summary>
    public const double MaxNanFraction = 0.01;

    /// <summary>
    /// The soils of <c>ground-tracked.sysdef</c>'s soil-list that are WATER: deep-water
    /// (acceleration-factor 0.000000, stopping-factor 0.000000) and shallow-water (0.700).
    /// PURE, and deliberately a name test rather than a factor test - a factor of zero can also
    /// come from a catalogue the chain could not read, and those two must never be confused.
    /// </summary>
    public static bool IsWaterSoil(string soil)
        => string.Equals(soil, "deep-water", StringComparison.Ordinal)
        || string.Equals(soil, "shallow-water", StringComparison.Ordinal);

    /// <summary>How many samples a leg of this length gets: the count, so the caller can place them.</summary>
    public static int SampleCount(double lengthM, double stepM)
        => Math.Max(2, (int)Math.Ceiling(lengthM / stepM) + 1);

    /// <summary>The along-leg distance of sample i of n.</summary>
    public static double SampleDistance(double lengthM, int i, int n)
        => n <= 1 ? 0.0 : lengthM * i / (n - 1);

    /// <summary>
    /// Score one leg. <paramref name="samples"/> must be in along-leg order and carry NaN Z
    /// where the elevation tile was missing - the NaNs are counted, never imputed.
    /// </summary>
    public static LegMetrics Score((double Lat, double Lon) a, (double Lat, double Lon) b,
                                   IReadOnlyList<LegSample> samples, double lengthM,
                                   double limitRaw, double stepM, double windowM,
                                   double shortWindowM, double threshold)
    {
        int n = samples.Count;
        var z = new double[n];
        var ok = new bool[n];
        int nanN = 0;
        for (int i = 0; i < n; i++)
        {
            z[i] = samples[i].Z;
            ok[i] = !double.IsNaN(z[i]);
            if (!ok[i]) nanN++;
        }

        double climb = 0, descend = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (!ok[i] || !ok[i + 1]) continue;
            climb += Math.Max(0.0, z[i + 1] - z[i]);
            descend += Math.Max(0.0, z[i] - z[i + 1]);
        }

        var (sustained, i0, i1) = Worst(samples, z, ok, lengthM, windowM, stepM);
        var (shortG, j0, j1) = Worst(samples, z, ok, lengthM, shortWindowM, stepM);

        int mid = (i0 + i1) / 2;
        // The governing limit over the window is the WORST (lowest) derated limit inside it;
        // on a tie the FIRST such sample wins, as python's min() does.
        var gov = samples[i0];
        double govLimit = limitRaw * gov.Soil.Factor;
        for (int i = i0 + 1; i <= i1 && i < n; i++)
        {
            double l = limitRaw * samples[i].Soil.Factor;
            if (l < govLimit) { govLimit = l; gov = samples[i]; }
        }

        double limit = limitRaw * gov.Soil.Factor;
        double nanFraction = n > 0 ? (double)nanN / n : 0.0;
        bool noVerdict = n > 0 && nanFraction > MaxNanFraction;
        double ratio = limit > 0 ? sustained / limit : double.PositiveInfinity;

        // WATER, counted over the WHOLE leg and not just the worst window. A sample on water is
        // not a grade problem the ratio can express - it is a soil whose acceleration-factor is
        // zero - so it is carried out of here as its own fact and reported as its own finding.
        int waterN = 0;
        var water = default(LegSample);
        for (int i = 0; i < n; i++)
        {
            if (!IsWaterSoil(samples[i].Soil?.Soil)) continue;
            waterN++;
            water ??= samples[i];
        }

        return new LegMetrics
        {
            LengthM = lengthM,
            Samples = n,
            Sustained = sustained,
            SustainedWindowM = n > 1 ? samples[i1].S - samples[i0].S : 0.0,
            Short = shortG,
            ShortWindowM = n > 1 ? samples[j1].S - samples[j0].S : 0.0,
            WorstLat = samples[mid].Lat,
            WorstLon = samples[mid].Lon,
            WorstSM = samples[mid].S,
            WorstZM = samples[mid].Z,
            WorstFrom = (samples[i0].Lat, samples[i0].Lon),
            WorstTo = (samples[i1].Lat, samples[i1].Lon),
            Soil = gov.Soil.Soil,
            SoilDesc = gov.Soil.Description,
            SoilSource = gov.Soil.Source,
            SoilSurfChar = gov.Soil.SurfChar,
            SoilType = gov.Soil.SoilType,
            SoilAssumed = gov.Soil.Assumed,
            Factor = gov.Soil.Factor,
            LimitRaw = limitRaw,
            Limit = limit,
            Ratio = ratio,
            ClimbM = climb,
            DescendM = descend,
            NanSamples = nanN,
            NanFraction = nanFraction,
            NoVerdict = noVerdict,
            Flagged = ratio >= threshold && lengthM > windowM && !noVerdict,
            Start = a,
            End = b,
            WaterSamples = waterN,
            WaterFraction = n > 0 ? (double)waterN / n : 0.0,
            WaterSoil = water?.Soil?.Soil ?? "",
            WaterSource = water?.Soil?.Source ?? "",
            WaterDesc = water?.Soil?.Description ?? "",
            WaterFirst = water != null ? (water.Lat, water.Lon) : (0.0, 0.0),
            WaterFirstSM = water?.S ?? 0.0,
        };
    }

    /// <summary>
    /// The largest MEAN UPHILL grade over any sliding window of about <paramref name="winM"/>
    /// metres -> (grade, first index, last index). Windows with a missing endpoint are skipped
    /// (and already counted as NaN samples).
    ///
    /// NOTE the window is a SAMPLE COUNT, k = round(win/step), and the rounding is
    /// round-half-to-EVEN on both sides of this port: python's round(20/8) is 2, not 3, so the
    /// "20 m" short window is really 16 m at the default step. Faithful, and only the short
    /// window - never the verdict - is affected.
    /// </summary>
    private static (double Grade, int I0, int I1) Worst(IReadOnlyList<LegSample> samples,
                                                        double[] z, bool[] ok,
                                                        double lengthM, double winM, double stepM)
    {
        int n = samples.Count;
        if (lengthM <= 0) return (0.0, 0, 0);
        int k = Math.Max(1, (int)Math.Round(winM / stepM, MidpointRounding.ToEven));
        if (k >= n) k = n - 1;
        double best = -9.9;
        int bi = 0, bj = Math.Min(k, n - 1);
        for (int i = 0; i < n - k; i++)
        {
            double run = samples[i + k].S - samples[i].S;
            if (run <= 0 || !ok[i] || !ok[i + k]) continue;
            double g = (z[i + k] - z[i]) / run;
            if (g > best) { best = g; bi = i; bj = i + k; }
        }
        if (best < -9.0) return (0.0, 0, Math.Min(k, n - 1));
        return (best, bi, bj);
    }
}

/// <summary>
/// PURE replication of the route the interface itself builds
/// (VrfC2SimService.ExecuteTaskOnTick, "ORIGIN VERTEX DROP"): the route STARTS at the unit's
/// live position, and leading vertices within DropOriginVertexMeters of the unit's AUTHORED
/// position are dropped once the unit has been spread further than that from it - never all
/// of them. Getting this wrong would score a leg the interface never drives.
/// </summary>
public static class PreflightRoute
{
    public sealed record Built(List<(double Lat, double Lon)> Route, int Dropped, string Note);

    public static Built Build((double Lat, double Lon) start,
                              IReadOnlyList<(double Lat, double Lon)> points,
                              (double Lat, double Lon)? authored,
                              double dropOriginMeters)
    {
        var pts = points?.ToList() ?? new List<(double, double)>();
        int skip = 0;
        string note = "";
        if (dropOriginMeters > 0 && pts.Count > 1 && authored.HasValue)
        {
            double spread = TileMath.DistanceMeters(start.Lat, start.Lon,
                                                    authored.Value.Lat, authored.Value.Lon);
            if (spread > dropOriginMeters)
            {
                while (skip < pts.Count - 1
                       && TileMath.DistanceMeters(pts[skip].Lat, pts[skip].Lon,
                                                  authored.Value.Lat, authored.Value.Lon) <= dropOriginMeters)
                    skip++;
                if (skip > 0)
                    note = $"dropped {skip} leading vertex(es) on the authored origin " +
                           $"({authored.Value.Lat:F5},{authored.Value.Lon:F5}); the unit was spread {spread:F0} m from it";
            }
        }
        var route = new List<(double Lat, double Lon)> { start };
        route.AddRange(pts.Skip(skip));
        return new Built(route, skip, note);
    }
}
