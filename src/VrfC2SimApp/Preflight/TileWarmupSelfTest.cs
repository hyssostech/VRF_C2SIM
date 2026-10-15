namespace VrfC2SimApp.Preflight;

/// <summary>
/// `--warm-cache-selftest` - OFFLINE proof of STP-805's tile warm-up (TileWarmup). No network: the plan is checked
/// against the SAME tile arithmetic the readers use (TileSource.TileIndexOf, LandCoverClass's grid, OsmTileMath), and
/// the run is driven through an OFFLINE TileSource on an empty temporary cache, which must fetch nothing and write
/// nothing.
/// </summary>
public static class TileWarmupSelfTest
{
    public static int Run()
    {
        int fails = 0;
        void Check(string what, bool cond)
        { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }

        // ===== 1. Parsing =====
        var ok = TileWarmup.ParseBox("53.94,23.10,54.12,23.42", out var err);
        Check("parse: a valid S,W,N,E box", ok is not null && err == null && ok.Value.North == 54.12);
        Check("parse: three numbers are refused", TileWarmup.ParseBox("53.9,23.1,54.1", out _) is null);
        Check("parse: S >= N is refused", TileWarmup.ParseBox("54.2,23.1,54.1,23.4", out _) is null);
        Check("parse: a non-number is refused, naming it", TileWarmup.ParseBox("53.9,abc,54.1,23.4", out var e2) is null
              && e2.Contains("abc"));
        Check("parse: invariant culture (a comma decimal is not accepted)", TileWarmup.ParseBox("53,9,23,1,54,1,23,4", out _) is null);

        // ===== 2. The plan covers exactly what the readers ask for =====
        var b = ok.Value;
        var plan = TileWarmup.Build(b);
        var corners = new[] { (b.South, b.West), (b.South, b.East), (b.North, b.West), (b.North, b.East),
                              ((b.South + b.North) / 2, (b.West + b.East) / 2) };
        var elevKeys = plan.ElevationCentres.Select(c => TileSource.TileIndexOf(plan.ElevationLevel, c.Lat, c.Lon)).ToHashSet();
        Check($"elevation: every corner's L{plan.ElevationLevel} tile is in the plan (TileSource.TileIndexOf)",
              corners.All(c => elevKeys.Contains(TileSource.TileIndexOf(plan.ElevationLevel, c.Item1, c.Item2))));
        Check("elevation: no tile twice, and each centre maps back to a distinct tile", elevKeys.Count == plan.ElevationCentres.Count);
        double size = 180.0 / Math.Pow(2, plan.ElevationLevel);
        int expect = ((int)Math.Floor((b.East + 180) / size) - (int)Math.Floor((b.West + 180) / size) + 1)
                   * ((int)Math.Floor((b.North + 90) / size) - (int)Math.Floor((b.South + 90) / size) + 1);
        Check($"elevation: the count is the grid's ({expect})", plan.ElevationCentres.Count == expect);
        foreach (var (ds, level, label) in TileMath.LandCoverSources)
        {
            double s = 180.0 / Math.Pow(2, level);
            var keys = plan.CoverCentres.Where(c => c.Ds == ds && c.Level == level)
                           .Select(c => ((int)((c.Lon + 180) / s), (int)((c.Lat + 90) / s))).ToHashSet();
            Check($"land cover {label}: every corner's tile at L{level} is in the plan (LandCoverClass's grid)",
                  corners.All(c => keys.Contains(((int)((c.Item2 + 180) / s), (int)((c.Item1 + 90) / s)))));
        }
        foreach (var set in Enum.GetValues<OsmSet>())
        {
            var keys = plan.OsmTiles.Where(t => t.Set == set).Select(t => (t.X, t.TmsY)).ToHashSet();
            Check($"OSM {set}: every corner's z{OsmTileMath.Zoom} tile is in the plan (OsmTileMath.TileOf)",
                  corners.All(c => keys.Contains(OsmTileMath.TileOf(c.Item1, c.Item2))));
        }
        Check("plan line: names the counts", TileWarmup.PlanLine(plan).Contains($"{plan.ElevationCentres.Count} elevation tile(s)"));

        // ===== 3. The size guard =====
        var big = TileWarmup.Build(TileWarmup.ParseBox("50,20,56,28", out _).Value);
        Check($"guard: a 6 x 8 degree box ({big.ElevationCentres.Count} tiles) is REFUSED without --force",
              TileWarmup.Refusal(big, force: false) is string r && r.StartsWith("REFUSED"));
        Check("guard: --force lets it through", TileWarmup.Refusal(big, force: true) == null);
        Check("guard: the demo-sized box is not refused", TileWarmup.Refusal(plan, force: false) == null);

        // ===== 4. A run through an OFFLINE TileSource on an empty cache fetches and writes nothing =====
        string tmp = Path.Combine(Path.GetTempPath(), "warmup-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var small = TileWarmup.Build(TileWarmup.ParseBox("54.00,23.20,54.02,23.24", out _).Value);
            using var ts = new TileSource(tmp, offline: true);
            var (e, c, o, unknown) = TileWarmup.Run(small, ts);
            Check("offline run: every planned tile was requested",
                  e == small.ElevationCentres.Count && c == small.CoverCentres.Count && o == small.OsmTiles.Count);
            Check("offline run: NOTHING was fetched", ts.Fetched == 0);
            Check("offline run: NOTHING was written to the cache", !Directory.EnumerateFiles(tmp, "*", SearchOption.AllDirectories).Any());
        }
        finally { try { Directory.Delete(tmp, true); } catch { } }

        // ===== 5. The plan names the SAME files the readers find: against the committed (warm) cache, offline =====
        string committed = null;
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null && committed == null; d = d.Parent)
        {
            string c = Path.Combine(d.FullName, "tools", "preflight", "preflight_cache");
            if (Directory.Exists(c)) committed = c;
        }
        if (committed == null)
            Console.WriteLine("  [SKIPPED] warm cache: tools/preflight/preflight_cache not found above the exe");
        else
        {
            var warm = TileWarmup.Build(TileWarmup.ParseBox("54.00,23.20,54.02,23.24", out _).Value);
            using var ts = new TileSource(committed, offline: true);
            TileWarmup.Run(warm, ts);
            Check($"warm cache: the plan's requests HIT the committed cache ({ts.CacheHits} hit(s)) and fetch nothing",
                  ts.CacheHits > 0 && ts.Fetched == 0);
        }

        Console.WriteLine(fails == 0 ? "warm-cache-selftest: ALL CHECKS PASSED" : $"warm-cache-selftest: {fails} CHECK(S) FAILED");
        return fails == 0 ? 0 : 1;
    }

    /// <summary>The CLI: --warm-preflight-cache S,W,N,E CACHE_DIR [--dry-run] [--force] [--level N].
    /// Exit 0 done, 1 tiles could not be fetched (network) or bodies were not tiles, 2 usage / refused.</summary>
    public static int Cli(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("usage: --warm-preflight-cache S,W,N,E CACHE_DIR [--dry-run] [--force] [--level N]");
            return 2;
        }
        var box = TileWarmup.ParseBox(args[1], out var err);
        if (box is null) { Console.WriteLine("box: " + err); return 2; }
        string cache = args[2];
        bool dry = args.Contains("--dry-run"), force = args.Contains("--force");
        int level = TileMath.DefaultElevationLevel;
        int li = Array.IndexOf(args, "--level");
        if (li > 0 && (li + 1 >= args.Length || !int.TryParse(args[li + 1], out level))) { Console.WriteLine("--level N"); return 2; }
        var plan = TileWarmup.Build(box.Value, level);
        Console.WriteLine(TileWarmup.PlanLine(plan));
        if (TileWarmup.Refusal(plan, force) is string refused) { Console.WriteLine(refused); return 2; }
        if (dry) { Console.WriteLine("DRY RUN: nothing fetched."); return 0; }
        Directory.CreateDirectory(cache);
        using var ts = new TileSource(cache, offline: false, elevationLevel: level);
        var (e, c, o, unknown) = TileWarmup.Run(plan, ts, Console.WriteLine);
        Console.WriteLine($"WARM-UP DONE into {cache}: {e} elevation + {c} land-cover + {o} OSM tile request(s); " +
                          $"{ts.Fetched} fetched over the network, {ts.CacheHits} already cached, {ts.ExhaustedTiles} given up " +
                          $"after {TileSource.MaxFetchAttempts} attempts, {ts.UndecodableBodies} non-tile body(ies), " +
                          $"{unknown} OSM tile(s) still UNKNOWN. A run can now set Vrf:PreflightCacheDir={cache} and " +
                          "Vrf:PreflightOffline=true.");
        return ts.ExhaustedTiles > 0 || ts.UndecodableBodies > 0 || unknown > 0 ? 1 : 0;
    }
}
