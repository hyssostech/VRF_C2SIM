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

    /// <summary>The best DataExtent over the Mojave AO (FINDING_EARLY_STOPS_2026-09-13 sec 7).</summary>
    public const int ElevationLevel = 13;

    /// <summary>Elevation tiles are 257x257 postings; 256 of them are the tile's own span.</summary>
    public const int TilePixels = 257;

    public const int Seg = TilePixels - 1;

    public const double EarthRadiusM = 6371000.0;

    /// <summary>
    /// The land-cover tilesets in DESCENDING ground resolution - the first with data wins.
    /// The levels are the DEEPEST the server actually serves (probed 2026-09-13: 154 and 165
    /// return "no tile" at L13, 188 at L11). An L13 request falls through SILENTLY, which is
    /// why these are pinned rather than derived.
    /// </summary>
    public static readonly (int Dataset, int Level, string Label)[] LandCoverSources =
    {
        (154, 12, "CA FVEG 15m"),
        (165, 12, "NLCD 30m"),
        (188, 10, "Copernicus 100m"),
    };

    /// <summary>Degrees between elevation postings at a level.</summary>
    public static double Posting(int level) => (180.0 / Math.Pow(2, level)) / Seg;

    /// <summary>Posting size in metres (east-west, north-south) at a latitude.</summary>
    public static (double EastWest, double NorthSouth) PostingMeters(double latDeg, int level = ElevationLevel)
    {
        double p = Posting(level);
        return (p * 111320.0 * Math.Cos(latDeg * Math.PI / 180.0), p * 111320.0);
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
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<(int ds, int level, int x, int y), GeoTiffFloat.Raster> _elev = new();
    private readonly ConcurrentDictionary<(int ds, int level, int x, int y), PngImage.Image> _cover = new();
    private readonly ConcurrentDictionary<(int ds, int level, int x, int y), bool> _failed = new();

    public int Fetched { get; private set; }
    public int CacheHits { get; private set; }
    public string CacheDirectory => _cacheDir;

    public TileSource(string cacheDir, bool offline = false, bool nearest = false, HttpClient http = null)
    {
        _cacheDir = cacheDir;
        _offline = offline;
        _nearest = nearest;
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

    /// <summary>Cached bytes, or a fetch, or null. minBytes rejects the server's "no tile" bodies.</summary>
    private byte[] Bytes(int ds, int level, int x, int y, string ext, int minBytes)
    {
        string fn = FilePath(ds, level, x, y, ext);
        try
        {
            var fi = new FileInfo(fn);
            if (fi.Exists && fi.Length >= minBytes)
            {
                CacheHits++;
                return File.ReadAllBytes(fn);
            }
        }
        catch { /* unreadable cache entry - fall through to the fetch */ }

        if (_offline || _failed.ContainsKey((ds, level, x, y))) return null;

        byte[] data;
        try
        {
            data = _http.GetByteArrayAsync($"{TmsBase}/{ds}/{level}/{x}/{y}.{ext}")
                        .GetAwaiter().GetResult();
        }
        catch { _failed[(ds, level, x, y)] = true; return null; }

        if (data == null || data.Length < minBytes) { _failed[(ds, level, x, y)] = true; return null; }
        try { File.WriteAllBytes(fn, data); } catch { /* cache is an optimisation, not a requirement */ }
        Fetched++;
        return data;
    }

    private GeoTiffFloat.Raster ElevationTile(int level, int x, int y)
        => _elev.GetOrAdd((TileMath.ElevationDataset, level, x, y),
                          k => GeoTiffFloat.Decode(Bytes(k.ds, k.level, k.x, k.y, "tif", 1000)));

    private PngImage.Image CoverTile(int ds, int level, int x, int y)
        => _cover.GetOrAdd((ds, level, x, y),
                           k => PngImage.Decode(Bytes(k.ds, k.level, k.x, k.y, "png", 100)));

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
    /// Terrain height at a point, bilinear (or nearest with the constructor flag) over the
    /// streamed postings. NaN when the tile is missing - and the NaN is allowed to propagate
    /// through the bilinear arithmetic exactly as it does in python, so a leg that clips a
    /// data hole ends up with NaN samples and NO VERDICT rather than a fabricated number.
    /// </summary>
    public double Elevation(double latDeg, double lonDeg, int level = TileMath.ElevationLevel)
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
