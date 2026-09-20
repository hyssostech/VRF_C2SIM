using System.Collections.Concurrent;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// The TMS index arithmetic, PURE and separated from the fetching so it can be reasoned
/// about (and self-tested) without a tile in hand. Every formula here is a transcription of
/// tools/preflight/leg_check.py's Tiles class; the constants are the ones the calibration
/// record rests on (docs/experiments/PREFLIGHT_CALIBRATION_2026-09-13.md).
/// </summary>
public static class TileMath
{
    /// <summary>The elevation dataset VR-Forces itself streams (elevation.worldwide.online.xml:24-35).</summary>
    public const int ElevationDataset = 149;

    /// <summary>
    /// THE LEVEL IS AN AO PROPERTY, NOT A CONSTANT. 13 is the deepest DataExtent the server
    /// serves over the MOJAVE AO (FINDING_EARLY_STOPS_2026-09-13 sec 7) and it stays the
    /// default, so every Mojave number ever published is reproduced byte for byte. It is NOT
    /// universal: dataset 149 returns NO DATA at L13 over the Suwalki Gap (measured 2026-09-20,
    /// 0 of 25 grid samples over the SuwalkiN20 box) and serves that ground at L12 instead.
    /// Configure with Vrf:PreflightElevationLevel; <see cref="DefaultMinElevationLevel"/> is the
    /// floor of the automatic fallback that finds L12 without being told.
    /// </summary>
    public const int DefaultElevationLevel = 13;

    /// <summary>
    /// The FLOOR of the "no data at L -> try L-1" cascade (Vrf:PreflightElevationMinLevel).
    /// 11 is two levels of fallback: deep enough to reach any ground MAK Earth serves at all
    /// (Suwalki answers at 12, and 11/10 are its parents), shallow enough that a genuinely
    /// unserved AO ends in a LOUD "no data at any level" rather than silently scoring a
    /// continent-sized posting as terrain.
    /// </summary>
    public const int DefaultMinElevationLevel = 11;

    /// <summary>Elevation tiles are 257x257 postings; 256 of them are the tile's own span.</summary>
    public const int TilePixels = 257;

    public const int Seg = TilePixels - 1;

    public const double EarthRadiusM = 6371000.0;

    /// <summary>
    /// The land-cover tilesets in DESCENDING ground resolution - the first with data wins.
    /// The levels are the DEEPEST the server actually serves (probed 2026-09-13: 154 and 165
    /// return "no tile" at L13, 188 at L11). An L13 request falls through SILENTLY, which is
    /// why these are pinned rather than derived.
    ///
    /// CLCplus (59, L14) IS THE EUROPEAN LAYER THE VENDOR'S OWN TERRAIN COMPOSES
    /// (biomes.landcover.coverage.online.xml:50) and it is FIRST because it is the finest:
    /// 10 m against Copernicus's 100 m. It covers Europe only - probed 2026-09-20 it returns a
    /// class at the Suwalki AO (21 woodland / 51 grassland) and NO TILE over North America - so
    /// at Mojave the cascade falls through to exactly the three sources it used before and every
    /// Mojave verdict is unchanged. It also closes the WATER blind spot: CLCplus class 100 is
    /// live (preset="Water" -> deep-water, acceleration-factor 0.000) whereas Copernicus's water
    /// class 80 is COMMENTED OUT in the vendor catalogue and resolves to no soil at all.
    /// </summary>
    public static readonly (int Dataset, int Level, string Label)[] LandCoverSources =
    {
        (59, 14, "CLCplus 10m"),
        (154, 12, "CA FVEG 15m"),
        (165, 12, "NLCD 30m"),
        (188, 10, "Copernicus 100m"),
    };

    /// <summary>Degrees between elevation postings at a level.</summary>
    public static double Posting(int level) => (180.0 / Math.Pow(2, level)) / Seg;

    /// <summary>Posting size in metres (east-west, north-south) at a latitude.</summary>
    public static (double EastWest, double NorthSouth) PostingMeters(double latDeg,
                                                                     int level = DefaultElevationLevel)
    {
        double p = Posting(level);
        return (p * 111320.0 * Math.Cos(latDeg * Math.PI / 180.0), p * 111320.0);
    }

    /// <summary>
    /// HOW MANY NATIVE POSTINGS THE SUSTAINED WINDOW SPANS at a level and latitude - the one
    /// number that says what a coarser DEM does to a verdict. The window is a sliding mean
    /// UPHILL grade (<see cref="LegScorer"/>), so relief shorter than a posting is AVERAGED
    /// AWAY before the scorer ever sees it: the fewer postings under the window, the more a real
    /// face reads as a gentle one.
    ///
    /// The 0.92 threshold was calibrated at L13 over the Mojave AO, where the 40 m window spans
    /// 4.2 postings north-south (9.55 m at 34.66 N). At L12 over Suwalki the posting is 19.1 m
    /// north-south at 54.1 N and the SAME window spans 2.1 - about half. A one-level-coarser DEM
    /// can therefore only SMOOTH a face, never sharpen it, so the error is one-sided: the ratio
    /// is biased LOW and a flag can be MISSED, never invented. Read that as the calibration
    /// being CONSERVATIVE at L12, not as it transferring.
    /// </summary>
    public static (double EastWest, double NorthSouth) WindowPostings(double latDeg, int level,
                                                                      double windowM)
    {
        var (ew, ns) = PostingMeters(latDeg, level);
        return (ew > 0 ? windowM / ew : 0.0, ns > 0 ? windowM / ns : 0.0);
    }

    /// <summary>One line an operator can read: the posting and the window span at a level.</summary>
    public static string CalibrationNote(double latDeg, int level, double windowM, double threshold)
    {
        var (ew, ns) = PostingMeters(latDeg, level);
        var (pew, pns) = WindowPostings(latDeg, level, windowM);
        string caveat = level >= DefaultElevationLevel
            ? FormattableString.Invariant($"the level the {threshold:F2} threshold was calibrated on")
            : FormattableString.Invariant(
                  $"COARSER than the L{DefaultElevationLevel} the {threshold:F2} threshold was calibrated on - the ")
              + "sustained window is averaged over fewer postings, so a real face reads LOWER and a flag can be "
              + "MISSED (never invented)";
        return FormattableString.Invariant(
            $"elevation L{level} at {latDeg:F2} N: posting {ew:F1} m E-W x {ns:F1} m N-S, so the {windowM:F0} m sustained window spans {pew:F1} x {pns:F1} postings - {caveat}");
    }

    /// <summary>Python's floor division - NOT C#'s truncating '/' - for a global sample index.</summary>
    public static (int Tile, int Pixel) SplitIndex(int global, int seg = Seg)
    {
        int t = (int)Math.Floor((double)global / seg);
        return (t, global - t * seg);
    }

    /// <summary>Great-circle metres (the haversine leg_check.py uses, same R_EARTH).</summary>
    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dla = (lat2 - lat1) * Math.PI / 180.0;
        double dlo = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Pow(Math.Sin(dla / 2.0), 2)
                 + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                 * Math.Pow(Math.Sin(dlo / 2.0), 2);
        return 2.0 * EarthRadiusM * Math.Asin(Math.Sqrt(Math.Min(1.0, a)));
    }

    /// <summary>
    /// Linear in lat/lon - exact enough over a few km, and it IS the straight line the
    /// vendor's ground-vehicle-move-to drives where no navigation mesh covers the leg.
    /// </summary>
    public static (double Lat, double Lon) Interpolate(double lat1, double lon1,
                                                       double lat2, double lon2, double f)
        => (lat1 + (lat2 - lat1) * f, lon1 + (lon2 - lon1) * f);
}

/// <summary>
/// WHERE ALL THE PRE-FLIGHT'S I/O LIVES: the on-disk tile cache and the HTTP fetch. The
/// scorer (<see cref="LegScorer"/>) never sees this type - it is handed sampled numbers.
///
/// CACHE LAYOUT: one flat directory of "{dataset}_{level}_{x}_{y}.{tif|png}" files - byte
/// for byte the naming tools/preflight/leg_check.py uses, deliberately, so the tool's
/// committed cache can be dropped in and the self-test runs with no network at all.
///
/// FETCH: python's urllib gets 403 from vr-theworld.com and curl does not, so the request
/// carries the header set curl sends (User-Agent: curl/..., Accept: */*) rather than the
/// default .NET ones. A tile that fails once is remembered and not retried in this process.
///
/// THREADING: the memory cache is concurrent and the fetch is synchronous, but NOTHING here
/// may run on the VR-Forces tick thread - a tile fetch would stall the simulation. The
/// service path calls this from a worker (see PreflightService).
/// </summary>
public sealed class TileSource : IDisposable
{
    public const string TmsBase = "http://vr-theworld.com/vr-theworld/tiles/1.0.0";

    private readonly string _cacheDir;
    private readonly bool _offline;
    private readonly bool _nearest;
    private readonly int _elevLevel;
    private readonly int _elevMinLevel;
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<(int ds, int level, int x, int y), GeoTiffFloat.Raster> _elev = new();
    private readonly ConcurrentDictionary<(int ds, int level, int x, int y), PngImage.Image> _cover = new();

    // F2 (cold-start review of f26d4ad). *** ABSENT IS NOT FAILED. ***
    //
    // This used to be one `_failed` set that a 404, a DNS failure, a connection refusal, a 5xx and
    // a 60 s timeout all landed in, and that nothing ever cleared. One dropped L13 fetch therefore
    // memoised "this tile does not exist", the cascade fell to L12, `_levelByArea` remembered L12
    // for that ~2.4 km area FOR THE PROCESS LIFETIME, and every later leg over that ground was
    // scored on a DEM the 0.92 threshold was not calibrated on - silently, because the only line
    // that fires says "scored at ... COARSER than L13" and cannot tell the server's answer from
    // our own dropped packet.
    //
    // The two are now different facts with different consequences:
    //   ABSENT  - the SERVER answered and has no tile here (404/410/204, or a body below minBytes,
    //             which is how this TMS says "no tile" with a 200). A property of the ground. It IS
    //             memoised, it IS allowed to drive the level cascade, and it costs one request.
    //   FAILED  - we never got the server's answer (timeout, connect/DNS failure, 5xx, IO). A
    //             property of the network. NOT memoised as absence, retried up to
    //             MaxFetchAttempts per tile across the process, and if it still fails the caller
    //             gets NO VERDICT and a loud WARN - never a quieter score one level down.
    private readonly ConcurrentDictionary<(int ds, int level, int x, int y), bool> _absent = new();
    private readonly ConcurrentDictionary<(int ds, int level, int x, int y), int> _failures = new();

    /// <summary>
    /// How many times ONE tile may be fetched before the process gives up on it for good. It is a
    /// bound on a WEDGED network, not a retry policy with backoff: each attempt already carries the
    /// HttpClient's own 60 s timeout, and the route-shift deadline (30 s) will have dispatched the
    /// authored line long before three of them elapse - which is correct, and is now SAID rather
    /// than absorbed into a coarser number. There is no sleep between attempts: they happen on
    /// successive sample requests, not in a loop.
    /// </summary>
    public const int MaxFetchAttempts = 3;

    /// <summary>Tiles this process has given up fetching (MaxFetchAttempts reached without an
    /// answer). Read by the self-tests and by nothing that makes a verdict.</summary>
    public int ExhaustedTiles => _failures.Count(kv => kv.Value >= MaxFetchAttempts);

    // m7 (cold-start review 02b51de): several pre-flight workers score routes concurrently and
    // both counters are written from Bytes(). Plain int++ is read-modify-write and UNDER-COUNTS, and
    // --preflight-selftest ASSERTS `Fetched == 0` - a lost increment there would turn a live network
    // fetch into a passing offline run. Interlocked on an int field; the properties stay read-only
    // to callers. Volatile read is enough for a log/assert (no ordering is implied).
    private int _fetched;
    private int _cacheHits;
    public int Fetched => Volatile.Read(ref _fetched);
    public int CacheHits => Volatile.Read(ref _cacheHits);
    public string CacheDirectory => _cacheDir;

    /// <summary>The level the cascade STARTS at (Vrf:PreflightElevationLevel).</summary>
    public int ElevationLevel => _elevLevel;

    /// <summary>The level the cascade STOPS at (Vrf:PreflightElevationMinLevel).</summary>
    public int ElevationMinLevel => _elevMinLevel;

    // THE RESOLVED LEVEL PER AREA. An "area" is one tile AT THE START LEVEL - the finest cell
    // whose coverage the probe actually establishes. A COARSER cell (say the floor level's tile)
    // would be cheaper but wrong: it would let one tile's presence decide for its neighbours, so
    // an AO where the start level is PARTIAL would keep scoring the holes as NaN instead of
    // falling back, which is the very failure this cascade exists to remove. Coverage is patchy
    // at this scale in practice - measured 2026-09-20, dataset 149 serves the Mojave AO at L13
    // and Lake Tahoe, 200 km away, only at L12.
    //
    // The extra cost is bounded and paid once: the probe reads the tile the sample needs anyway,
    // and a tile the server does not have is remembered in _failed, so an AO served one level
    // down costs one 404 per start-level tile and nothing thereafter. Value: the level that
    // returned data, or 0 for "no data at any level" - so a blind area costs ONE probe, not one
    // per sample, and the loud report is emitted per leg rather than 5,000 times.
    private readonly ConcurrentDictionary<(int x, int y), int> _levelByArea = new();

    public TileSource(string cacheDir, bool offline = false, bool nearest = false,
                      HttpClient http = null,
                      int elevationLevel = TileMath.DefaultElevationLevel,
                      int elevationMinLevel = TileMath.DefaultMinElevationLevel)
    {
        _cacheDir = cacheDir;
        _offline = offline;
        _nearest = nearest;
        // A level below the floor, or a floor above the level, would silently disable the
        // cascade; clamp both and keep the pair ordered rather than throwing inside a worker.
        _elevLevel = Math.Clamp(elevationLevel, 1, 20);
        _elevMinLevel = Math.Clamp(Math.Min(elevationMinLevel, _elevLevel), 1, _elevLevel);
        Directory.CreateDirectory(_cacheDir);
        _http = http ?? NewCurlLikeClient();
    }

    private static HttpClient NewCurlLikeClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        // The header set curl sends. urllib's defaults get 403 from this server; curl's do not.
        c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "curl/8.4.0");
        c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        return c;
    }

    private string FilePath(int ds, int level, int x, int y, string ext)
        => Path.Combine(_cacheDir, $"{ds}_{level}_{x}_{y}.{ext}");

    /// <summary>Why a tile request produced no bytes - see the <c>_absent</c>/<c>_failures</c>
    /// note. <c>Ok</c> carries the bytes; <c>Absent</c> and <c>Failed</c> carry null.</summary>
    public enum TileOutcome { Ok, Absent, Failed }

    /// <summary>
    /// Cached bytes, or a fetch. minBytes rejects the server's "no tile" bodies, which this TMS
    /// serves with a 200 and a few bytes rather than a 404 - so a SHORT BODY IS ABSENCE, not a
    /// failure, and is the case the original code got right.
    ///
    /// The status code is read rather than left to <c>GetByteArrayAsync</c>'s throw-on-failure,
    /// because that call collapses "the server says there is no such tile" and "the server never
    /// answered" into one exception - which is the whole of F2.
    /// </summary>
    private byte[] Bytes(int ds, int level, int x, int y, string ext, int minBytes, out TileOutcome outcome)
    {
        string fn = FilePath(ds, level, x, y, ext);
        try
        {
            var fi = new FileInfo(fn);
            if (fi.Exists && fi.Length >= minBytes)
            {
                Interlocked.Increment(ref _cacheHits);
                outcome = TileOutcome.Ok;
                return File.ReadAllBytes(fn);
            }
        }
        catch { /* unreadable cache entry - fall through to the fetch */ }

        var key = (ds, level, x, y);
        // OFFLINE IS ABSENCE, DELIBERATELY. Vrf:PreflightOffline means "score only what the cache
        // holds"; a tile that is not in the cache is not coming, so the cascade may fall through to
        // a level that IS cached and a leg with no cached tile at any level ends in the existing
        // loud "NO ELEVATION DATA AT ANY LEVEL". Calling it FAILED would turn every offline run -
        // including the fixture comparison - into a route of no-verdict legs.
        if (_offline) { outcome = TileOutcome.Absent; return null; }
        if (_absent.ContainsKey(key)) { outcome = TileOutcome.Absent; return null; }
        if (_failures.TryGetValue(key, out int fails) && fails >= MaxFetchAttempts)
        { outcome = TileOutcome.Failed; return null; }

        byte[] data;
        try
        {
            using var resp = _http.GetAsync($"{TmsBase}/{ds}/{level}/{x}/{y}.{ext}").GetAwaiter().GetResult();
            int code = (int)resp.StatusCode;
            if (code == 404 || code == 410 || code == 204)
            {
                // The server's own answer: there is no tile here. Definitive, memoised, and the
                // one outcome the level cascade is entitled to act on.
                _absent[key] = true;
                outcome = TileOutcome.Absent;
                return null;
            }
            if (!resp.IsSuccessStatusCode)
            {
                // 5xx, 429, a proxy's 502 - the server did not answer the QUESTION. Transient by
                // assumption, so it is counted and retried, never remembered as absence.
                _failures.AddOrUpdate(key, 1, (_, n) => n + 1);
                outcome = TileOutcome.Failed;
                return null;
            }
            data = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Timeout (TaskCanceledException), DNS/connect failure, a truncated body: we never got
            // the server's answer. Same treatment as a 5xx.
            _failures.AddOrUpdate(key, 1, (_, n) => n + 1);
            outcome = TileOutcome.Failed;
            return null;
        }

        if (data == null || data.Length < minBytes)
        {
            // A 200 with a body too small to be a tile IS this TMS's "no tile" - the original
            // reading, and the only one of the old four that was genuinely absence.
            _absent[key] = true;
            outcome = TileOutcome.Absent;
            return null;
        }
        try { File.WriteAllBytes(fn, data); } catch { /* cache is an optimisation, not a requirement */ }
        Interlocked.Increment(ref _fetched);
        outcome = TileOutcome.Ok;
        return data;
    }

    // MEMOISE THE DECODE ONLY WHEN THE ANSWER IS DEFINITIVE (F2, second half). The old code was
    // `_elev.GetOrAdd(key, k => Decode(Bytes(...)))`, and ConcurrentDictionary.GetOrAdd STORES
    // whatever the factory returns - including the null a failed fetch produced - and never
    // re-invokes it. So even with the fetch made retryable, one dropped packet would have been
    // frozen in the decode cache instead. A FAILED outcome is therefore not stored at all, which
    // is what makes the retry in Bytes reachable on the next sample.
    private GeoTiffFloat.Raster ElevationTile(int level, int x, int y)
        => ElevationTile(level, x, y, out _);

    private GeoTiffFloat.Raster ElevationTile(int level, int x, int y, out TileOutcome outcome)
    {
        var key = (TileMath.ElevationDataset, level, x, y);
        if (_elev.TryGetValue(key, out var known))
        {
            outcome = known != null ? TileOutcome.Ok : TileOutcome.Absent;
            return known;
        }
        var raster = GeoTiffFloat.Decode(Bytes(key.ElevationDataset, level, x, y, "tif", 1000, out outcome));
        // A body that arrived but would not DECODE is not a network failure - it is a tile this
        // reader cannot use, which is indistinguishable from absence for every consumer.
        if (outcome == TileOutcome.Ok && raster == null) outcome = TileOutcome.Absent;
        if (outcome != TileOutcome.Failed) _elev[key] = raster;
        return raster;
    }

    private PngImage.Image CoverTile(int ds, int level, int x, int y)
    {
        var key = (ds, level, x, y);
        if (_cover.TryGetValue(key, out var known)) return known;
        var img = PngImage.Decode(Bytes(ds, level, x, y, "png", 100, out var outcome));
        if (outcome != TileOutcome.Failed) _cover[key] = img;
        return img;
    }

    /// <summary>
    /// One posting by GLOBAL sample index: gi = column (lon), gj = row from the SOUTH.
    /// When the owning tile is missing, the three tiles that SHARE that edge posting are
    /// tried (the python does the same) before the sample is given up as NaN.
    /// </summary>
    private double Posting(int level, int gi, int gj)
    {
        var (x, i) = TileMath.SplitIndex(gi);
        var (y, j) = TileMath.SplitIndex(gj);
        var t = ElevationTile(level, x, y);
        if (t == null)
        {
            foreach (var (xx, ii, yy, jj) in new[]
                     {
                         (x - 1, i + TileMath.Seg, y, j),
                         (x, i, y - 1, j + TileMath.Seg),
                         (x - 1, i + TileMath.Seg, y - 1, j + TileMath.Seg),
                     })
            {
                if (ii < 0 || ii > TileMath.Seg || jj < 0 || jj > TileMath.Seg) continue;
                var t2 = ElevationTile(level, xx, yy);
                if (t2 != null) return Pixel(t2, ii, jj);
            }
            return double.NaN;
        }
        return Pixel(t, i, j);
    }

    // Row 0 of the decoded raster is the tile's NORTH edge; j counts from the south.
    private static double Pixel(GeoTiffFloat.Raster t, int i, int j)
    {
        int row = TileMath.Seg - j;
        if (i < 0 || i >= t.Width || row < 0 || row >= t.Height) return double.NaN;
        return t.At(i, row);
    }

    /// <summary>
    /// THE LEVEL THIS AREA IS SERVED AT, or 0 when NO level in the cascade has a tile here.
    /// Probed once per area and remembered (see <see cref="_levelByArea"/>). The probe asks only
    /// whether the tile that OWNS the point decodes - the neighbour-tile fallback in
    /// <see cref="Posting"/> is a sampling detail and deliberately not part of the decision, so
    /// the level a leg is scored at is a property of the ground and not of which edge it clipped.
    /// </summary>
    public int ResolveLevel(double latDeg, double lonDeg) => ResolveLevel(latDeg, lonDeg, out _);

    /// <summary>
    /// The cascade, with F2's distinction carried out to the caller.
    ///
    /// THE LOAD-BEARING RULE: a level that FAILED to fetch STOPS the cascade and is NOT memoised.
    /// Falling through to the next level down on a failed fetch is exactly the silent downgrade
    /// F2 names - it would score the ground one level coarser for a reason that has nothing to do
    /// with the ground, on the very terrain the 0.92 threshold was calibrated on - and memoising
    /// that conclusion would make one dropped packet permanent for the process. So:
    ///   ABSENT at L  -> the server has no tile at L; try L-1. This is the cascade doing its job.
    ///   FAILED  at L -> we do not KNOW whether the server has a tile at L, so we cannot conclude
    ///                   anything about L-1 either. Stop, return 0 with failed=true, remember
    ///                   nothing, and let the caller shout.
    /// A retry costs one more request per sample until MaxFetchAttempts is reached per tile, which
    /// is what bounds a wedged network.
    /// </summary>
    public int ResolveLevel(double latDeg, double lonDeg, out bool fetchFailed)
    {
        fetchFailed = false;
        var key = AreaKey(latDeg, lonDeg);
        if (_levelByArea.TryGetValue(key, out int known)) return known;
        int found = 0;
        for (int level = _elevLevel; level >= _elevMinLevel; level--)
        {
            var (x, y) = TileIndex(level, latDeg, lonDeg);
            if (ElevationTile(level, x, y, out var outcome) != null) { found = level; break; }
            if (outcome == TileOutcome.Failed) { fetchFailed = true; return 0; }
        }
        _levelByArea[key] = found;
        return found;
    }

    /// <summary>The area cell a point belongs to: one tile at the START level of the cascade.</summary>
    private (int x, int y) AreaKey(double latDeg, double lonDeg) => TileIndex(_elevLevel, latDeg, lonDeg);

    /// <summary>The (x, y) of the tile that OWNS a point at a level.</summary>
    private static (int x, int y) TileIndex(int level, double latDeg, double lonDeg)
    {
        double p = TileMath.Posting(level);
        var (x, _) = TileMath.SplitIndex((int)Math.Floor((lonDeg + 180.0) / p));
        var (y, _) = TileMath.SplitIndex((int)Math.Floor((latDeg + 90.0) / p));
        return (x, y);
    }

    /// <summary>
    /// Terrain height at a point, at the deepest level of the cascade that serves this area.
    /// <paramref name="levelUsed"/> is 0 when no level did, in which case the value is NaN and
    /// the caller MUST say so out loud - a leg nothing could be sampled for is not a clear leg.
    /// </summary>
    public double Elevation(double latDeg, double lonDeg, out int levelUsed)
        => Elevation(latDeg, lonDeg, out levelUsed, out _);

    /// <summary>The form that carries F2's verdict: <paramref name="fetchFailed"/> true means the
    /// NaN is OUR failure, not the server's answer, and the leg it belongs to gets NO VERDICT.</summary>
    public double Elevation(double latDeg, double lonDeg, out int levelUsed, out bool fetchFailed)
    {
        levelUsed = ResolveLevel(latDeg, lonDeg, out fetchFailed);
        return levelUsed == 0 ? double.NaN : ElevationAt(latDeg, lonDeg, levelUsed);
    }

    /// <summary>The cascade form without the level - what most callers want.</summary>
    public double Elevation(double latDeg, double lonDeg) => Elevation(latDeg, lonDeg, out _);

    /// <summary>
    /// Terrain height at ONE GIVEN LEVEL, bilinear (or nearest with the constructor flag) over
    /// the streamed postings - no cascade. NaN when the tile is missing, and the NaN is allowed
    /// to propagate through the bilinear arithmetic exactly as it does in python, so a leg that
    /// clips a data hole ends up with NaN samples and NO VERDICT rather than a fabricated number.
    /// </summary>
    public double ElevationAt(double latDeg, double lonDeg, int level)
    {
        double p = TileMath.Posting(level);
        double gi = (lonDeg + 180.0) / p;
        double gj = (latDeg + 90.0) / p;
        int i0 = (int)Math.Floor(gi), j0 = (int)Math.Floor(gj);
        double fi = gi - i0, fj = gj - j0;
        if (_nearest)
            return Posting(level, i0 + (fi >= 0.5 ? 1 : 0), j0 + (fj >= 0.5 ? 1 : 0));
        double z00 = Posting(level, i0, j0);
        double z10 = Posting(level, i0 + 1, j0);
        double z01 = Posting(level, i0, j0 + 1);
        double z11 = Posting(level, i0 + 1, j0 + 1);
        return z00 * (1 - fi) * (1 - fj) + z10 * fi * (1 - fj)
             + z01 * (1 - fi) * fj + z11 * fi * fj;
    }

    /// <summary>
    /// The RAW land-cover class value one tileset reports at a point, or null for "no tile".
    /// 0 means the tileset has no data there and the caller falls through to the next source.
    /// </summary>
    public int? LandCoverClass(int ds, int level, double latDeg, double lonDeg)
    {
        double size = 180.0 / Math.Pow(2, level);
        int x = (int)((lonDeg + 180.0) / size);
        int y = (int)((latDeg + 90.0) / size);
        double fx = ((lonDeg + 180.0) - x * size) / size;
        double fy = ((latDeg + 90.0) - y * size) / size;
        int px = Math.Min(255, (int)(fx * 256));
        int py = Math.Min(255, 255 - (int)(fy * 256));
        var im = CoverTile(ds, level, x, y);
        if (im == null) return null;
        int v = im.Value(px, py);
        return v < 0 ? null : v;
    }

    public void Dispose() => _http?.Dispose();
}
