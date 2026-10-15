using System.Globalization;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// `--ao-selftest` - OFFLINE proof of the AO intake (AoExtent): synthetic initializations with known extents, the margin
/// arithmetic, the skipped-position rule, and one real committed initialization parsed end to end when it is found.
/// `--ao-from-init FILE [--margin-km K]` is the CLI: it prints the AO and the warm-up command for it (STP-805).
/// </summary>
public static class AoExtentSelfTest
{
    public static int Run()
    {
        int fails = 0;
        void Check(string what, bool cond)
        { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }

        var d = new InitData();
        d.Units.Add(new InitUnit { Name = "A", Latitude = "54.0", Longitude = "23.2" });
        d.Units.Add(new InitUnit { Name = "B", Latitude = "54.1", Longitude = "23.3" });
        d.Units.Add(new InitUnit { Name = "NoPos", Latitude = "", Longitude = "" });
        d.Units.Add(new InitUnit { Name = "Bad", Latitude = "fifty", Longitude = "23" });
        var area = new InitArea(); area.Points.Add((53.95, 23.25, 0)); area.Points.Add((54.05, 23.40, 0)); d.Areas.Add(area);
        var line = new InitLine(); line.Points.Add((54.20, 23.10, 0)); d.Lines.Add(line);

        var r0 = AoExtent.FromInit(d, 0);
        Check("raw: the box holds every unit and every graphic vertex (S 53.95 W 23.10 N 54.20 E 23.40)",
              r0 != null && r0.Raw.South == 53.95 && r0.Raw.West == 23.10 && r0.Raw.North == 54.20 && r0.Raw.East == 23.40);
        Check("units: 2 read, 2 with no readable position SKIPPED (never read as 0,0)",
              r0.Units == 2 && r0.UnitsSkipped == 2 && r0.Raw.South > 50);
        Check("graphics: 3 vertices counted", r0.GraphicPoints == 3);
        Check("edges: named - S and E by the area, W and N by the line (a far outlier is visible, not silent)",
              r0.Edges.S.StartsWith("area") && r0.Edges.E.StartsWith("area") && r0.Edges.W.StartsWith("line")
              && r0.Edges.N.StartsWith("line") && AoExtent.SummaryLine(r0).Contains("Edges set by"));

        var r2 = AoExtent.FromInit(d, 2.0);
        double dLat = r2.Raw.South - r2.Box.South;
        Check("margin: 2 km of latitude is 2 / 111.32 degrees", Math.Abs(dLat - 2.0 / 111.32) < 1e-9);
        double dLon = r2.Raw.West - r2.Box.West;
        double expectLon = 2.0 / (111.32 * Math.Cos((53.95 + 54.20) / 2 * Math.PI / 180));
        Check("margin: 2 km of longitude is scaled by cos(mid-latitude)", Math.Abs(dLon - expectLon) < 1e-9);
        Check("margin: the widened box is ~4 km wider and taller than the raw one",
              Math.Abs(AoExtent.HeightKm(r2.Box) - AoExtent.HeightKm(r2.Raw) - 4.0) < 0.01);
        Check("empty: an initialization with no position at all gives NO AO (null), not a box at 0,0",
              AoExtent.FromInit(new InitData { Units = { new InitUnit { Latitude = "", Longitude = "" } } }, 2) == null);
        Check("text: the box prints as S,W,N,E that --warm-preflight-cache parses back",
              TileWarmup.ParseBox(AoExtent.BoxText(r2.Box), out _) is TileWarmup.Box back && Math.Abs(back.North - r2.Box.North) < 1e-5);
        Check("summary: names the counts and the margin", AoExtent.SummaryLine(r2).Contains("2 unit(s)")
              && AoExtent.SummaryLine(r2).Contains("2.0 km margin") && AoExtent.SummaryLine(r2).Contains("SKIPPED"));

        // A real committed initialization, end to end (InitParser), when one is found above the exe.
        string init = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null && init == null; dir = dir.Parent)
        {
            string c = Path.Combine(dir.FullName, "data", "COA-STP1_Initialization.xml");
            if (File.Exists(c)) init = c;
        }
        if (init == null) Console.WriteLine("  [SKIPPED] real init: data/COA-STP1_Initialization.xml not found above the exe");
        else
        {
            var real = AoExtent.FromInit(InitParser.Parse(File.ReadAllText(init)), 2.0);
            Check($"real init: data/COA-STP1_Initialization.xml gives an AO ({(real == null ? "none" : AoExtent.BoxText(real.Box))}) " +
                  "with units and graphics in it", real != null && real.Units > 0 && real.GraphicPoints > 0
                  && real.Box.South < real.Box.North && real.Box.West < real.Box.East);
        }

        Console.WriteLine(fails == 0 ? "ao-selftest: ALL CHECKS PASSED" : $"ao-selftest: {fails} CHECK(S) FAILED");
        return fails == 0 ? 0 : 1;
    }

    /// <summary>--ao-from-init FILE [--margin-km K]. Exit 0 AO printed, 1 no position in the file, 2 usage.</summary>
    public static int Cli(string[] args)
    {
        if (args.Length < 2 || !File.Exists(args[1]))
        { Console.WriteLine("usage: --ao-from-init INITIALIZATION.xml [--margin-km K]  (default margin 2 km)"); return 2; }
        double margin = 2.0;
        int mi = Array.IndexOf(args, "--margin-km");
        if (mi > 0 && (mi + 1 >= args.Length
                       || !double.TryParse(args[mi + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out margin)
                       || margin < 0))
        { Console.WriteLine("--margin-km K (a non-negative number)"); return 2; }
        var r = AoExtent.FromInit(InitParser.Parse(File.ReadAllText(args[1])), margin);
        if (r == null) { Console.WriteLine("NO AO: no unit or graphic in the file has a readable position."); return 1; }
        Console.WriteLine(AoExtent.SummaryLine(r));
        if (TileWarmup.Refusal(TileWarmup.Build(r.Box), force: false) is string refused)
            Console.WriteLine("TOO BIG for one warm-up: " + refused + ". Check the edges above - a far-away item sets them.");
        Console.WriteLine($"next: VrfC2SimApp --warm-preflight-cache {AoExtent.BoxText(r.Box)} <CACHE_DIR> --dry-run");
        return 0;
    }
}
