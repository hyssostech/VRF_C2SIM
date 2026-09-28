using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Configuration;
using VrfC2SimApp.Preflight;

namespace VrfC2SimApp;

/// <summary>
/// OFFLINE check of the OSM half of the pre-flight (RL-20260927-01; docs/PLAN_MOVEMENT_2026-09-27.md
/// M2): `VrfC2SimApp --osm-selftest [osmTileDir [rasterDir]]`.
///
/// No bridge, no network, no MAK runtime. The tiles are SYNTHETIC vector tiles built here from REAL
/// cached features - the lake Jezioro Wiersnie (OSM way 197345448, where 48 IBCT stopped 0.5 m from
/// the edge in the -1 pre-warm), its neighbour Dabiel (197345447), and the two OSM buildings the -2
/// stop's centreline ran through (857519447, 857519777) - the way tools/preflight/leg_check.py
/// --selftest builds its tile. The lake rings are the exact z14 tile-coordinate integers vr-theworld's
/// osm-water tiles carry (lane I2 cache, fetched 2026-09-26); the buildings are leg_check's own
/// selftest coordinates. The numbers asserted here were measured on those tiles BEFORE this code, with
/// exact geometry in python (scratch osmport/extract2.py, extract3.py): the -1 stop is 0.46 m outside
/// the lake; the (e) leg comes within 25 m of water at 805.9 m and enters it at 830.9 m.
///
/// The optional directory argument runs the (e) T14 line - the line E1 and G1 are registered to drive
/// (PLAN_MOVEMENT sec 1) - through the whole pre-dispatch stage on REAL tiles and prints the outcome on
/// both model sets. It asserts nothing about the ground, only that the stage ran.
/// </summary>
public static class OsmSelfTest
{
    // ---------------------------------------------------------------- the record's own points
    private static readonly (double Lat, double Lon) T14Start = (54.019389, 23.313902);   // 48 IBCT (cut A)
    private static readonly (double Lat, double Lon) T14DestE = (54.040348, 23.324206);   // the (e) destination
    private static readonly (double Lat, double Lon) T14DestExported = (54.04034819271516, 23.336444730300467); // before (e)
    private static readonly (double Lat, double Lon) StopM1 = (54.026779, 23.317195);     // -1 stop (FINDING T14 sec 1)
    private static readonly (double Lat, double Lon) StopM2 = (54.030807, 23.327063);     // -2 stop
    private static readonly (double Lat, double Lon) IWaypoint = (54.014600, 23.331500);  // cut-A (i)

    // The Mojave ridge leg (RouteShiftSelfTest; PREREG_RIDGE_AG 3.2) - for the slope half of the nudge.
    private static readonly (double Lat, double Lon) RidgeA = (34.658442, -116.740092);
    private static readonly (double Lat, double Lon) RidgeB = (34.651212159120796, -116.81163703922806);

    // ---------------------------------------------------------------- real features, tile coords
    // Jezioro Wiersnie, way 197345448, natural=water (no water= tag -> 80), in two z14 tiles.
    private static readonly int[] WiersnieN_9253_11125 =
    {
        960,1933,1028,1939,1094,1971,1173,1980,1250,1974,1379,1958,1507,1939,1602,1943,1687,1963,1731,1988,
        1762,2033,1785,2121,1814,2253,1876,2380,1922,2474,2011,2575,2119,2681,2177,2786,2192,2919,2165,3096,
        2111,3255,2008,3416,1886,3529,1823,3595,1807,3660,1820,3732,1886,3792,1955,3818,2052,3846,2124,3903,
        2218,3978,2307,4022,2356,4042,2396,4084,2401,4096,1039,4096,1039,4091,955,4014,894,3833,900,3747,
        933,3689,971,3659,1058,3662,1105,3651,1149,3615,1199,3595,1242,3591,1273,3567,1299,3523,1327,3468,
        1356,3343,1406,3234,1460,3144,1540,3075,1568,3043,1568,3020,1536,2974,1481,2948,1398,2929,1327,2899,
        1289,2828,1280,2739,1254,2616,1194,2536,1148,2510,1066,2469,1007,2410,965,2337,901,2264,815,2223,
        785,2181,786,2140,808,2103,852,2048,869,1984,909,1940,960,1933,
    };
    private static readonly int[] WiersnieS_9253_11124 =
    {
        2401,0,2408,13,2398,55,2389,83,2358,100,2344,117,2355,157,2358,224,2355,285,2335,349,2308,396,
        2221,503,2150,597,2032,651,1989,649,1917,668,1851,774,1689,847,1538,849,1495,834,1470,786,1441,716,
        1426,659,1336,498,1315,415,1219,307,1206,243,1108,211,1055,158,1039,0,2401,0,
    };
    // Dabiel, way 197345447, natural=water, in four z14 tiles.
    private static readonly int[] Dabiel_9253_11125 =
    {
        218,0,185,69,189,131,208,190,274,277,304,333,321,389,327,489,318,642,325,722,345,777,415,855,463,896,
        485,948,469,1036,452,1166,458,1276,473,1351,488,1438,561,1557,631,1670,743,1773,827,1851,839,1875,
        845,1914,835,1940,820,1962,780,1983,710,1978,621,1950,570,1927,502,1884,416,1791,319,1634,247,1497,
        203,1374,186,1279,206,1183,219,1101,206,1031,181,984,144,968,98,950,40,948,0,962,0,0,218,0,
    };
    private static readonly int[] Dabiel_9253_11126 =
    {
        1068,2852,1142,2873,1221,2880,1386,2899,1513,2932,1582,2991,1613,3069,1645,3181,1677,3216,1733,3239,
        1862,3270,1941,3292,2012,3338,2013,3401,1973,3440,1887,3445,1814,3440,1724,3401,1616,3375,1529,3374,
        1419,3386,1342,3391,1247,3358,1152,3329,1073,3322,1015,3344,956,3374,868,3442,782,3552,649,3656,535,3728,
        427,3818,321,3945,238,4055,218,4096,0,4096,0,3822,3,3815,63,3769,203,3655,267,3542,343,3433,531,3247,
        635,3085,718,2982,785,2922,841,2882,918,2874,1007,2864,1068,2852,
    };
    private static readonly int[] Dabiel_9252_11125 =
    {
        4096,0,4096,962,4041,983,3952,989,3847,933,3710,843,3702,823,3714,790,3766,745,3834,685,3876,624,
        3884,577,3878,433,3879,321,3894,214,3906,101,3941,0,4096,0,
    };
    private static readonly int[] Dabiel_9252_11126 = { 4096,3822,4096,4096,3941,4096,3944,4088,3984,4025,4062,3908,4096,3822 };

    // The two -2 hamlet buildings, leg_check.py selftest_osm_buildings (lat, lon).
    private static readonly (double, double)[] B447 =
    {
        (54.0306499, 23.3268231), (54.0307381, 23.3268017), (54.0307413, 23.3268446), (54.0307665, 23.3268392),
        (54.0307759, 23.3269519), (54.0306751, 23.3269787), (54.0306782, 23.3270162), (54.0306467, 23.3270216),
        (54.0306341, 23.326866), (54.030653, 23.3268607), (54.0306499, 23.3268231),
    };
    private static readonly (double, double)[] B777 =
    {
        (54.0308389, 23.3268714), (54.0308925, 23.326866), (54.0309083, 23.3270538), (54.0308547, 23.3270591),
        (54.0308389, 23.3268714),
    };

    private static int _failures;

    public static int Run(string realTileDir = null, string realRasterDir = null)
    {
        _failures = 0;
        string repo = FindRepoRoot();
        Console.WriteLine("=== OSM FEATURES (RL-20260927-01) - offline, synthetic tiles from real cached features ===");
        string tmp = Path.Combine(Path.GetTempPath(), "vrfc2sim-osm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            Settings(repo);
            Decoder();
            TileArithmetic();
            VendorFilters();
            Geometry();
            string cache = WriteRealFeatureCache(tmp);
            ParityBuildings(cache);
            ParityLake(cache);
            FlagRules();
            NudgeSearch();
            NudgeOnTheLake(cache, tmp);
            NudgeSlope(repo);
            RiverAndPond(tmp);
            ChooserOsm();
            WaterDetourWithSlope(repo);
            Reports();
            CacheSemantics(tmp);
            if (!string.IsNullOrEmpty(realTileDir)) RealTiles(repo, realTileDir, realRasterDir);
        }
        catch (Exception e)
        {
            Console.WriteLine($"  [FAIL] the suite threw: {e}");
            _failures++;
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { /* temp */ }
        }
        Console.WriteLine(_failures == 0 ? "osm-selftest: PASS" : $"osm-selftest: {_failures} FAILURE(S)");
        return _failures == 0 ? 0 : 1;
    }

    // ================================================================= 0. settings
    private static void Settings(string repo)
    {
        Console.WriteLine("-- 0. the settings and their defaults (Vrf:ModelSet, clearance, nudge)");
        var d = new VrfSettings();
        Check(d.ModelSet == "EntityLevel" && d.PreflightBuildingClearanceMeters == 10.0
              && d.PreflightVertexNudgeMaxMeters == 300.0,
              $"C# defaults: ModelSet {d.ModelSet}, clearance {d.PreflightBuildingClearanceMeters} m, nudge {d.PreflightVertexNudgeMaxMeters} m");
        if (repo == null) { Check(false, "repo root found (data/COA-STP1_Order.xml)"); return; }
        string app = Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
        string demo = Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");
        var shipped = new ConfigurationBuilder().AddJsonFile(app, false).Build().GetSection("Vrf").Get<VrfSettings>();
        var overlay = new ConfigurationBuilder().AddJsonFile(app, false).AddJsonFile(demo, false).Build()
                          .GetSection("Vrf").Get<VrfSettings>();
        Check(shipped.ModelSet == "EntityLevel" && shipped.PreflightBuildingClearanceMeters == 10.0
              && shipped.PreflightVertexNudgeMaxMeters == 300.0,
              "appsettings.json WRITES the three defaults down (not only compiled in)");
        Check(overlay.ModelSet == "EntityLevel" && overlay.PreflightBuildingClearanceMeters == 10.0
              && overlay.PreflightVertexNudgeMaxMeters == 300.0,
              "the Demo overlay states them too");
        string saved = Environment.GetEnvironmentVariable("Vrf__ModelSet");
        try
        {
            Environment.SetEnvironmentVariable("Vrf__ModelSet", "AggregateTacticalLevel");
            var env = new ConfigurationBuilder().AddJsonFile(app, false).AddEnvironmentVariables().Build()
                          .GetSection("Vrf").Get<VrfSettings>();
            Check(env.ModelSet == "AggregateTacticalLevel",
                  "Vrf__ModelSet=AggregateTacticalLevel reaches the setting (the aggregate lane's switch)");
        }
        finally { Environment.SetEnvironmentVariable("Vrf__ModelSet", saved); }
        Check(ModelSetRules.TryParse("EntityLevel", out var m1) && m1 == ModelSet.EntityLevel
              && ModelSetRules.TryParse("aggregatetacticallevel", out var m2) && m2 == ModelSet.AggregateTacticalLevel
              && ModelSetRules.TryParse("AggregateTacticalLevel.sms", out var m3) && m3 == ModelSet.AggregateTacticalLevel,
              "Vrf:ModelSet parses case-insensitively and accepts the .sms form");
        Check(!ModelSetRules.TryParse("Aggregate", out var m4) && m4 == ModelSet.EntityLevel
              && ModelSetRules.TryParse("", out var m5) && m5 == ModelSet.EntityLevel
              && ModelSetRules.TryParse(null, out var m6) && m6 == ModelSet.EntityLevel,
              "an unrecognised value is NOT recognised (the service falls back to EntityLevel and says so); absent/empty IS EntityLevel");
        foreach (string v in new[] { "EntityLevel", "aggregatetacticallevel", "AggregateTacticalLevel.sms", "", null, "Aggregate", "x" })
        {
            bool a = ModelSetRules.TryParse(v, out var ms);
            bool b = UnitPositionPolicy.TryParseModelSet(v, out bool agg);
            if (a != b || (ms == ModelSet.AggregateTacticalLevel) != (b && agg))
                Check(false, $"the pre-flight and the task judges (D1) parse '{v ?? "null"}' alike");
        }
        Check(true, "the pre-flight and the task judges (D1) read Vrf:ModelSet through ONE parser - 7 values agree");
        Check(ModelSetRules.Entity.UseSlope && ModelSetRules.Entity.WaterCorridorMeters == 25.0
              && !ModelSetRules.Entity.RiverLinesAreWater
              && !ModelSetRules.Aggregate.UseSlope && ModelSetRules.Aggregate.WaterCorridorMeters == 0.0
              && ModelSetRules.Aggregate.RiverLinesAreWater && ModelSetRules.Aggregate.ReportSlowTerrain,
              "the two rule sets: entity = slope + water within 25 m; aggregate = no slope, water/river ON the line, slow reported");
    }

    // ================================================================= 1. the decoder
    private static void Decoder()
    {
        Console.WriteLine("-- 1. the vector-tile decoder");
        var w = new MvtWriter("osm");
        w.Add(new Dictionary<string, object>
              {
                  ["@id"] = 857519447L, ["building"] = "yes", ["flag"] = true, ["u"] = 7UL,
                  ["neg"] = new Sint(-3), ["f"] = 2.5f, ["d"] = 1.25,
              },
              Mvt.GeomPolygon, new[] { Ring(0, 0, 100, 0, 100, 100, 0, 100, 0, 0) });
        w.Add(new Dictionary<string, object> { ["waterway"] = "river" }, Mvt.GeomLine,
              new[] { Ring(10, 10, 20, 30, 40, 50) });
        byte[] tile = w.Tile();
        var layers = Mvt.Decode(tile);
        Check(layers.Count == 1 && layers[0].Name == "osm" && layers[0].Extent == 4096 && layers[0].Features.Count == 2,
              $"one layer 'osm', extent 4096, two features (got {layers.Count} layer(s))");
        var p = layers[0].Features[0];
        Check(p.GeomType == Mvt.GeomPolygon && p.Parts.Count == 1 && p.Parts[0].Count == 5
              && p.Parts[0][0] == p.Parts[0][4] && p.Parts[0][2] == (100, 100),
              "a polygon ring comes back CLOSED (ClosePath appends the first point, as decode_mvt does)");
        Check(Mvt.Prop(p.Props, "@id") == "857519447" && Mvt.Prop(p.Props, "flag") == "True"
              && Mvt.Prop(p.Props, "u") == "7" && Mvt.Prop(p.Props, "neg") == "-3"
              && Mvt.Prop(p.Props, "f") == "2.5" && Mvt.Prop(p.Props, "d") == "1.25"
              && Mvt.Prop(p.Props, "building") == "yes",
              "every MVT value type decodes (int64, uint64, sint64, bool, float, double, string) and str()s as python does");
        var l = layers[0].Features[1];
        Check(l.GeomType == Mvt.GeomLine && l.Parts[0].Count == 3 && l.Parts[0][2] == (40, 50),
              "a line keeps its points and is NOT closed");
        var gz = Gzip(tile);
        Check(Mvt.Decode(gz).Count == 1 && Mvt.Decode(gz)[0].Features.Count == 2, "a gzip-compressed tile decodes the same");
        Check(Mvt.Decode(Array.Empty<byte>()).Count == 0, "an EMPTY body is a valid empty tile (the reader, not the decoder, decides UNKNOWN)");
        var marker = Mvt.Decode(Mvt.AbsentMarkerTile());
        Check(Mvt.IsAbsentMarker(marker) && !Mvt.IsAbsentMarker(layers),
              "the interface's own 404 marker decodes as a marker, and a real tile does not");
        Check(Throws(() => Mvt.Decode(Encoding.ASCII.GetBytes("<html><body>403 Forbidden</body></html>"))),
              "an HTML error page is REFUSED (FormatException) - it is not a tile (SF3)");
        Check(Throws(() => Mvt.Decode(tile.Take(tile.Length - 7).ToArray())), "a truncated tile is REFUSED");

        // FromLayers: which features each set KEEPS, through the vendor filters.
        var all = new MvtWriter("osm");
        all.Add(Props(10L, "highway", "tertiary", "bridge", "yes"), Mvt.GeomLine, new[] { Ring(0, 0, 50, 50) });
        all.Add(Props(11L, "highway", "footway", "bridge", "yes"), Mvt.GeomLine, new[] { Ring(0, 10, 50, 60) });
        all.Add(Props(12L, "waterway", "river", "name", "Some River"), Mvt.GeomLine, new[] { Ring(0, 100, 400, 100) });
        all.Add(Props(13L, "waterway", "stream"), Mvt.GeomLine, new[] { Ring(0, 200, 400, 200) });
        all.Add(Props(14L, "building", "yes"), Mvt.GeomPolygon, new[] { Ring(500, 500, 520, 500, 520, 520, 500, 520, 500, 500) });
        all.Add(Props(15L, "landuse", "forest"), Mvt.GeomPolygon, new[] { Ring(600, 600, 900, 600, 900, 900, 600, 900, 600, 600) });
        var ta = OsmTile.FromLayers(OsmSet.All, 9253, 11125, Mvt.Decode(all.Tile()), "t");
        Check(ta.Bridges.Count == 1 && ta.Bridges[0].Id == "10" && ta.Bridges[0].WidthM == 8.0
              && ta.Rivers.Count == 1 && ta.Rivers[0].Id == "12" && ta.Rivers[0].WidthM == 5.0 && ta.Rivers[0].Name == "Some River"
              && ta.Buildings.Count == 1 && ta.LandUse.Count == 1 && ta.Water.Count == 0,
              $"osm set: 1 road bridge (not the footway), 1 River line (not the stream) at 5 m with its name, 1 building, 1 forest (got {ta.Bridges.Count}/{ta.Rivers.Count}/{ta.Buildings.Count}/{ta.LandUse.Count})");
        var wat = new MvtWriter("osm");
        wat.Add(Props(20L, "natural", "water"), Mvt.GeomPolygon, new[] { Ring(0, 0, 100, 0, 100, 100, 0, 100, 0, 0) });
        wat.Add(Props(21L, "natural", "water", "water", "river"), Mvt.GeomPolygon, new[] { Ring(200, 0, 300, 0, 300, 100, 200, 0) });
        wat.Add(Props(22L, "place", "islet", "natural", "water"), Mvt.GeomPolygon, new[] { Ring(400, 0, 500, 0, 500, 100, 400, 0) });
        wat.Add(Props(23L, "natural", "water"), Mvt.GeomLine, new[] { Ring(0, 300, 100, 300) });
        var tw = OsmTile.FromLayers(OsmSet.Water, 9253, 11125, Mvt.Decode(wat.Tile()), "t");
        Check(tw.Water.Count == 2 && tw.Water.Any(w => w.Id == "20" && w.Value == 80) && tw.Water.Any(w => w.Id == "21" && w.Value == 82)
              && tw.Buildings.Count == 0 && tw.Bridges.Count == 0,
              $"osm-water set: natural=water (80) and water=river (82) kept, the islet and the LINE dropped (got {tw.Water.Count})");
    }

    // ================================================================= 2. tile arithmetic
    private static void TileArithmetic()
    {
        Console.WriteLine("-- 2. z14 tile arithmetic (TMS rows)");
        Check(OsmTileMath.TileOf(StopM1.Lat, StopM1.Lon) == (9253, 11125),
              $"the -1 stop is in 14_9253_11125 (FINDING_IRONSTORM_T14_STOP sec 3; got {OsmTileMath.TileOf(StopM1.Lat, StopM1.Lon)})");
        Check(OsmTileMath.TileOf(StopM2.Lat, StopM2.Lon) == (9253, 11125), "the -2 stop is in the same tile");
        var (px, py) = OsmTileMath.ToPixel(14, 9253, 11125, StopM1.Lat, StopM1.Lon, 4096);
        var back = OsmTileMath.ToLatLon(14, 9253, 11125, px, py, 4096);
        Check(Math.Abs(back.Lat - StopM1.Lat) < 1e-9 && Math.Abs(back.Lon - StopM1.Lon) < 1e-9,
              "pixel <-> lat/lon round-trips to 1e-9 deg");
        Check(px > 0 && px < 4096 && py > 0 && py < 4096, $"the stop's pixel is inside its tile ({px:F0}, {py:F0})");
        Check(OsmTileMath.FileName(9253, 11125) == "14_9253_11125.pbf", "the cache file name is leg_check's 14_<x>_<tmsy>.pbf");
    }

    // ================================================================= 3. vendor filters
    private static void VendorFilters()
    {
        Console.WriteLine("-- 3. the vendor filters, transcribed");
        Dictionary<string, object> P(params string[] kv)
        {
            var d = new Dictionary<string, object>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return d;
        }
        Check(OsmVendor.WaterValue(P("natural", "water")) == 80 && OsmVendor.WaterValue(P("natural", "water", "water", "lake")) == 81
              && OsmVendor.WaterValue(P("water", "river")) == 82 && OsmVendor.WaterValue(P("water", "swamp")) == 90
              && OsmVendor.WaterValue(P("water", "lagoon")) == 200 && OsmVendor.WaterValue(P("water", "stream_pool")) == 81,
              "selectStyle(): natural=water with NO water= tag is 80 (the T14 lake), lake 81, river 82, swamp 90, lagoon 200");
        Check(OsmVendor.WaterValue(P("place", "islet", "natural", "water")) == null
              && OsmVendor.WaterValue(P("natural", "sand")) == null && OsmVendor.WaterValue(P("natural", "scrub")) == null,
              "osm.features.water.xml filter(): islets, sand and scrub are dropped");
        Check(OsmVendor.IsBuildingKept(3, P("building", "yes")) && OsmVendor.IsBuildingKept(3, P("building", "house"))
              && !OsmVendor.IsBuildingKept(3, P("building", "roof")) && !OsmVendor.IsBuildingKept(3, P("building", "no"))
              && !OsmVendor.IsBuildingKept(3, P("building", "bridge"))
              && !OsmVendor.IsBuildingKept(3, P("building", "yes", "location", "underground"))
              && OsmVendor.IsBuildingKept(3, P("aeroway", "hangar")) && !OsmVendor.IsBuildingKept(3, P("aeroway", "runway"))
              && OsmVendor.IsBuildingKept(3, P("man_made", "tower")) && !OsmVendor.IsBuildingKept(3, P("man_made", "pier"))
              && !OsmVendor.IsBuildingKept(3, P("landuse", "residential")) && !OsmVendor.IsBuildingKept(2, P("building", "yes")),
              "the building filter (leg_check's transcription): polygons; roof/no/bridge/underground dropped; aeroway/man_made lists");
        Check(OsmVendor.IsRiverLine(2, P("waterway", "river")) && OsmVendor.IsRiverLine(2, P("waterway", "canal"))
              && OsmVendor.IsRiverLine(2, P("waterway", "tidal_channel")) && !OsmVendor.IsRiverLine(2, P("waterway", "stream"))
              && !OsmVendor.IsRiverLine(3, P("waterway", "river")) && !OsmVendor.IsRiverLine(2, P("waterway", "river", "@type", "node"))
              && OsmVendor.IsRiverLine(2, P("waterway", "river", "tunnel", "culvert")),
              "RiverL (the aggregate file): river/canal/tidal_channel lines; not streams, polygons or nodes; tunnels KEPT there");
        Check(OsmVendor.WaterwayWidthMeters(P("waterway", "river")) == 5.0 && OsmVendor.WaterwayWidthMeters(P("width", "12")) == 12.0
              && OsmVendor.WaterwayWidthMeters(P("width", "3.5 m")) == 3.5 && OsmVendor.WaterwayWidthMeters(P("width", "wide")) == 5.0,
              "MAK_WIDTH: the data's width if it has one (featureconfig.txt:165), else 5 m (:423)");
        Check(OsmVendor.LandUse(3, P("landuse", "forest")) == AggregateLandUse.Forest
              && OsmVendor.LandUse(3, P("landuse", "orchard")) == AggregateLandUse.Forest
              && OsmVendor.LandUse(3, P("landuse", "residential")) == AggregateLandUse.Municipal
              && OsmVendor.LandUse(3, P("natural", "wetland")) == AggregateLandUse.Swamp
              && OsmVendor.LandUse(3, P("landuse", "farmland")) == AggregateLandUse.None
              && OsmVendor.LandUse(2, P("landuse", "forest")) == AggregateLandUse.None,
              "aggregate RESTRICTED_L2 land use: forest/orchard, wetland (swamp), commercial/industrial/residential/construction");
        Check(OsmVendor.IsRoadBridge(2, P("highway", "tertiary", "bridge", "yes"))
              && OsmVendor.IsRoadBridge(2, P("highway", "track", "bridge", "viaduct"))
              && !OsmVendor.IsRoadBridge(2, P("highway", "footway", "bridge", "yes"))
              && !OsmVendor.IsRoadBridge(2, P("highway", "path", "bridge", "yes"))
              && !OsmVendor.IsRoadBridge(2, P("highway", "tertiary", "bridge", "no"))
              && !OsmVendor.IsRoadBridge(2, P("highway", "tertiary", "bridge", "boardwalk"))
              && !OsmVendor.IsRoadBridge(2, P("railway", "rail", "bridge", "yes"))
              && !OsmVendor.IsRoadBridge(3, P("highway", "tertiary", "bridge", "yes"))
              && !OsmVendor.IsRoadBridge(2, P("highway", "tertiary")),
              "a drivable ROAD bridge: a highway line with bridge (not no/boardwalk), not a footway/path/rail bridge");
        Check(OsmVendor.RoadWidthMeters(P("highway", "tertiary")) == 8.0 && OsmVendor.RoadWidthMeters(P("width", "11")) == 11.0
              && OsmVendor.RoadWidthMeters(P("width", "wide")) == 8.0,
              "a road's MAK_WIDTH: its width tag, else 8 m (featureconfig.txt:211)");
        Check(OsmQuery.BridgeExempt(0.0, 6.0, 8.0, 25.0) && !OsmQuery.BridgeExempt(0.0, 8.0, 8.0, 25.0)
              && OsmQuery.BridgeExempt(10.0, 30.0, 8.0, 25.0) && !OsmQuery.BridgeExempt(10.0, 33.0, 8.0, 25.0)
              && OsmQuery.BridgeExempt(0.0, 6.0, 8.0, 0.0) && !OsmQuery.BridgeExempt(0.0, 20.0, 8.0, 0.0),
              "the bridge rule: IN water only ON the deck (half width 4 + 3 m); NEAR water anywhere in the approach zone (corridor + 7 m)");
        Check(VertexNudgeSearch.CompassWord(0) == "north" && VertexNudgeSearch.CompassWord(100) == "east"
              && VertexNudgeSearch.CompassWord(200) == "south" && VertexNudgeSearch.CompassWord(315) == "north-west",
              "compass words for the report");
    }

    // ================================================================= 4. geometry
    private static void Geometry()
    {
        Console.WriteLine("-- 4. pure geometry (local metre frame)");
        var outer = new[] { (0.0, 0.0), (100.0, 0.0), (100.0, 100.0), (0.0, 100.0), (0.0, 0.0) };
        var hole = new[] { (40.0, 40.0), (60.0, 40.0), (60.0, 60.0), (40.0, 60.0), (40.0, 40.0) };
        var rings = new[] { outer, hole };
        Check(OsmGeometry.Inside((10, 10), rings) && !OsmGeometry.Inside((50, 50), rings) && !OsmGeometry.Inside((150, 50), rings),
              "even-odd over all rings: inside the lake yes, on the island no, outside no");
        Check(Math.Abs(OsmGeometry.PointPolygon((150, 50), rings) - 50.0) < 1e-9 && OsmGeometry.PointPolygon((10, 10), rings) == 0.0
              && Math.Abs(OsmGeometry.PointPolygon((50, 50), rings) - 10.0) < 1e-9,
              "point-to-polygon: 50 m outside, 0 inside, 10 m from the island's shore");
        var (d1, t1) = OsmGeometry.SegmentPolygon((-50, 50), (150, 50), rings);
        Check(d1 == 0.0 && Math.Abs(t1 - 0.25) < 1e-9, $"a segment crossing the shore is at 0 m, first crossing at t=0.25 (got {d1}, {t1:F3})");
        var (d2, _) = OsmGeometry.SegmentPolygon((-50, 130), (150, 130), rings);
        Check(Math.Abs(d2 - 30.0) < 1e-9, $"a segment passing 30 m north is at 30 m (got {d2:F3})");
        var (d3, t3) = OsmGeometry.SegmentPolyline((0, -10), (0, 10), new[] { (-5.0, 0.0), (5.0, 0.0) });
        Check(d3 == 0.0 && Math.Abs(t3 - 0.5) < 1e-9, "a segment crossing a line is at 0 m at t=0.5");
        Check(Math.Abs(OsmGeometry.PointPolyline((0, 7), new[] { (-5.0, 0.0), (5.0, 0.0) }) - 7.0) < 1e-9, "point-to-line 7 m");
        var f = OsmGeometry.Frame.At(StopM1.Lat, StopM1.Lon);
        var q = f.LatLon((100, 0));
        Check(Math.Abs(TileMath.DistanceMeters(StopM1.Lat, StopM1.Lon, q.Lat, q.Lon) - 100.0) < 0.2,
              "the local frame's 100 m east is 100 m by haversine (to 0.2 m)");
    }

    // ================================================================= synthetic tiles from real features
    private static string WriteRealFeatureCache(string tmp)
    {
        string cache = Path.Combine(tmp, "real");
        string water = Path.Combine(cache, "osm-water");
        string all = Path.Combine(cache, "osm");
        Directory.CreateDirectory(water);
        Directory.CreateDirectory(all);
        void W(int x, int y, MvtWriter w) => File.WriteAllBytes(Path.Combine(water, OsmTileMath.FileName(x, y)), w.Tile());
        var lakeN = new MvtWriter("osm");
        lakeN.Add(Props(197345448L, "natural", "water", "name", "Jezioro Wiersnie"), Mvt.GeomPolygon, new[] { Pts(WiersnieN_9253_11125) });
        lakeN.Add(Props(197345447L, "natural", "water", "name", "Dabiel"), Mvt.GeomPolygon, new[] { Pts(Dabiel_9253_11125) });
        W(9253, 11125, lakeN);
        var lakeS = new MvtWriter("osm");
        lakeS.Add(Props(197345448L, "natural", "water"), Mvt.GeomPolygon, new[] { Pts(WiersnieS_9253_11124) });
        W(9253, 11124, lakeS);
        var dN = new MvtWriter("osm");
        dN.Add(Props(197345447L, "natural", "water"), Mvt.GeomPolygon, new[] { Pts(Dabiel_9253_11126) });
        W(9253, 11126, dN);
        var dW = new MvtWriter("osm");
        dW.Add(Props(197345447L, "natural", "water"), Mvt.GeomPolygon, new[] { Pts(Dabiel_9252_11125) });
        W(9252, 11125, dW);
        var dNW = new MvtWriter("osm");
        dNW.Add(Props(197345447L, "natural", "water"), Mvt.GeomPolygon, new[] { Pts(Dabiel_9252_11126) });
        W(9252, 11126, dNW);

        // The osm set: the two -2 buildings, and - on the line 100 m west - a building=roof and a
        // landuse polygon the building filter must ignore (leg_check.py selftest_osm_buildings).
        var key = OsmTileMath.TileOf(StopM2.Lat, StopM2.Lon);
        double dlon = 100.0 / (111320.0 * Math.Cos(54.0308 * Math.PI / 180.0));
        var sq = new[] { (54.03075, 23.32685 - dlon), (54.03085, 23.32685 - dlon), (54.03085, 23.32700 - dlon),
                         (54.03075, 23.32700 - dlon), (54.03075, 23.32685 - dlon) };
        var b = new MvtWriter("osm");
        b.Add(Props(857519447L, "building", "yes"), Mvt.GeomPolygon, new[] { LatLonRing(key, B447) });
        b.Add(Props(857519777L, "building", "house"), Mvt.GeomPolygon, new[] { LatLonRing(key, B777) });
        b.Add(Props(1L, "building", "roof"), Mvt.GeomPolygon, new[] { LatLonRing(key, sq) });
        b.Add(Props(2L, "landuse", "residential"), Mvt.GeomPolygon, new[] { LatLonRing(key, sq) });
        File.WriteAllBytes(Path.Combine(all, OsmTileMath.FileName(key.X, key.TmsY)), b.Tile());
        // Every OTHER tile the (e) leg or the -1/-2 stops touch, in either set, is KNOWN and empty (the
        // marker a server 404 leaves), so an UNKNOWN count below is a statement about the reader.
        foreach (var (x, y) in OsmQuery.TilesAlong(T14Start, T14DestE, 350.0, 25.0)
                                 .Concat(OsmQuery.TilesAlong(T14Start, IWaypoint, 60.0, 8.0))
                                 .Concat(OsmQuery.TilesAlong(IWaypoint, T14DestE, 150.0, 8.0)))
            foreach (string dir in new[] { water, all })
            {
                string fn = Path.Combine(dir, OsmTileMath.FileName(x, y));
                if (!File.Exists(fn)) File.WriteAllBytes(fn, Mvt.AbsentMarkerTile());
            }
        return cache;
    }

    // ================================================================= 5a. parity: buildings
    private static void ParityBuildings(string cache)
    {
        Console.WriteLine("-- 5a. PARITY with leg_check --osm-buildings: the -2 T14 stop segment");
        using var ts = new TileSource(cache, offline: true);
        var key = OsmTileMath.TileOf(StopM2.Lat, StopM2.Lon);
        var tile = ts.GetOsmTile(OsmSet.All, key.X, key.TmsY);
        Check(tile.Known && tile.Buildings.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal)
                                 .SequenceEqual(new[] { "857519447", "857519777" }),
              "the tile decodes to the 2 kept footprints (building=roof and the landuse polygon are not buildings)");
        // 200 m of the (i) leg-2 centreline centred on the stop's own station (leg_check.py).
        double l0 = TileMath.DistanceMeters(IWaypoint.Lat, IWaypoint.Lon, T14DestE.Lat, T14DestE.Lon);
        (double Lat, double Lon) On(double s) => TileMath.Interpolate(IWaypoint.Lat, IWaypoint.Lon, T14DestE.Lat, T14DestE.Lon, s / l0);
        var a = On(1730.0);
        var b = On(1930.0);
        double dlon = 100.0 / (111320.0 * Math.Cos(54.0308 * Math.PI / 180.0));
        var aw = (a.Lat, a.Lon - dlon);
        var bw = (b.Lat, b.Lon - dlon);
        var r = OsmQuery.LegBuildings(ts.OsmProvider, a, b, 10.0, 2.0);
        Check(r.Flagged && r.UnknownTiles == 0 && r.Within.Select(x => x.Id).SequenceEqual(new[] { "857519447", "857519777" })
              && r.Within.All(x => x.DistanceM == 0.0) && r.MinM == 0.0,
              $"the -2 stop's (i) leg 2 FLAGS buildings 857519447 and 857519777 at 0.0 m (got [{string.Join(", ", r.Within.Select(x => x.Id + "@" + x.DistanceM.ToString("F1", CultureInfo.InvariantCulture)))}], unknown {r.UnknownTiles})");
        var rw = OsmQuery.LegBuildings(ts.OsmProvider, aw, bw, 10.0, 2.0);
        Check(!rw.Flagged && rw.UnknownTiles == 0 && rw.MinM.HasValue && rw.MinM.Value > 80.0,
              $"the same 200 m shifted 100 m WEST passes: clear of 10 m, nearest {rw.MinM?.ToString("F1", CultureInfo.InvariantCulture)} m (> 80), known");
        Check(OsmQuery.LegBuildings(ts.OsmProvider, aw, bw, 120.0, 2.0).Flagged,
              "... and a 120 m clearance DOES flag it (the clearance is honoured)");
        OsmTileProvider none = (set, x, y) => OsmTile.Unknown(set, x, y, "not in the dir");
        var ru = OsmQuery.LegBuildings(none, aw, bw, 10.0, 2.0);
        Check(ru.UnknownTiles > 0 && !ru.Flagged, "a leg on a tile NOT in the dir is UNKNOWN (counted), not clear");
        string empty = Path.Combine(Path.GetDirectoryName(cache), "empty0");
        Directory.CreateDirectory(Path.Combine(empty, "osm"));
        File.WriteAllBytes(Path.Combine(empty, "osm", OsmTileMath.FileName(key.X, key.TmsY)), Array.Empty<byte>());
        using var te = new TileSource(empty, offline: true);
        var t0 = te.GetOsmTile(OsmSet.All, key.X, key.TmsY);
        Check(!t0.Known && t0.State.Contains("0-byte") && OsmQuery.LegBuildings(te.OsmProvider, aw, bw, 10.0, 2.0).UnknownTiles > 0,
              $"a 0-byte tile (the python fetcher's mark for ANY non-200) is UNKNOWN too ({t0.State})");
    }

    // ================================================================= 5b. parity: the lake
    private static void ParityLake(string cache)
    {
        Console.WriteLine("-- 5b. PARITY with leg_check --osm-water: the -1 lake edge and the (e) leg");
        using var ts = new TileSource(cache, offline: true);
        var e = OsmQuery.Leg(ts.OsmProvider, T14Start, T14DestE, ModelSetRules.Entity, 8.0);
        Check(e.Water && e.WaterMinDistanceM == 0.0 && e.UnknownTiles == 0 && e.WaterId == "197345448",
              $"EntityLevel: the (e) leg has OSM water ON it - the lake 197345448 first (got water={e.Water}, min {e.WaterMinDistanceM:F1} m, {e.WaterId}, unknown {e.UnknownTiles})");
        Check(e.WaterFirstSM >= 800.0 && e.WaterFirstSM <= 815.0 && e.WaterLastSM >= 1974.0 && e.WaterLastSM <= 1990.0,
              $"... within 25 m from {e.WaterFirstSM:F0} m to {e.WaterLastSM:F0} m (exact python: 805.9 .. 1,981.8 m, Wiersnie then Dabiel)");
        var ag = OsmQuery.Leg(ts.OsmProvider, T14Start, T14DestE, ModelSetRules.Aggregate, 8.0);
        Check(ag.Water && ag.WaterFirstSM >= 828.0 && ag.WaterFirstSM <= 840.0,
              $"AggregateTacticalLevel: water ON the centreline from {ag.WaterFirstSM:F0} m (exact python: enters at 830.9 m; leg_check 2 m raster: 834 m)");
        var stop = OsmQuery.Point(ts.OsmProvider, StopM1, ModelSetRules.Entity, 10.0);
        Check(stop.Known && stop.WaterDistanceM > 0.1 && stop.WaterDistanceM < 1.0 && stop.Water && stop.WaterId == "197345448",
              $"the -1 stop (54.02678, 23.31720) is {stop.WaterDistanceM:F2} m outside the lake (FINDING: 0.5 m) - WATER for an entity (25 m)");
        var stopAgg = OsmQuery.Point(ts.OsmProvider, StopM1, ModelSetRules.Aggregate, 10.0);
        Check(stopAgg.Known && !stopAgg.Water, "... and not water for an aggregate, whose centre point must be IN it");
        var legI1 = OsmQuery.Leg(ts.OsmProvider, T14Start, IWaypoint, ModelSetRules.Entity, 8.0);
        Check(!legI1.Water && legI1.UnknownTiles == 0,
              $"the (i) leg 1 that DID drive clear is clear of the lakes by more than 25 m (nearest {legI1.WaterMinDistanceM:F0} m; lane T14: 60 m)");
    }

    // ================================================================= 6. the leg rule per model set
    private static void FlagRules()
    {
        Console.WriteLine("-- 6. the leg rule per model set (ModelSetRules.FlagLeg)");
        var dryOsm = new OsmLegFeatures { Water = false, CorridorM = 25 };
        var wetOsm = new OsmLegFeatures { Water = true, CorridorM = 25, WaterFirstSM = 830, WaterId = "197345448", WaterKind = "natural=water" };
        var slope = new LegMetrics { Index = 1, Flagged = true, Ratio = 1.098, WorstSM = 2006, Osm = dryOsm };
        var water = new LegMetrics { Index = 2, Flagged = false, Ratio = 0.10, Osm = wetOsm };
        var both = new LegMetrics { Index = 3, Flagged = true, Ratio = 1.2, Osm = wetOsm };
        var none = new LegMetrics { Index = 4, Flagged = false, Ratio = 0.3, Osm = dryOsm };
        var blindWet = new LegMetrics { Index = 5, Flagged = false, NoVerdict = true, Osm = wetOsm };
        var legacy = new LegMetrics { Index = 6, Flagged = true, Ratio = 1.0, Osm = null };
        var E = ModelSetRules.Entity;
        var A = ModelSetRules.Aggregate;
        Check(E.FlagLeg(slope).Flagged && E.FlagLeg(slope).Slope && !E.FlagLeg(slope).Water,
              "EntityLevel: the slope ratio flags a leg, as before");
        Check(E.FlagLeg(water).Flagged && E.FlagLeg(water).Water && !E.FlagLeg(water).Slope
              && E.FlagLeg(water).Reason.Contains("within 25 m"),
              "EntityLevel: OSM water within 25 m flags it INDEPENDENTLY of the slope window");
        Check(E.FlagLeg(both).Slope && E.FlagLeg(both).Water && !E.FlagLeg(none).Flagged,
              "EntityLevel: both reasons are carried; a dry flat leg is not flagged");
        Check(E.FlagLeg(blindWet).Flagged && A.FlagLeg(blindWet).Flagged,
              "a leg with NO slope verdict is still flagged for water - the two are independent");
        Check(!A.FlagLeg(slope).Flagged, "AggregateTacticalLevel: the slope ratio is OFF - a 1.098 face does not flag it");
        Check(A.FlagLeg(water).Flagged && A.FlagLeg(water).Reason.Contains("ON the centreline"),
              "AggregateTacticalLevel: water ON the centreline flags it (MAK_WATERWAY, speed-factor 0)");
        var slowOnly = new LegMetrics { Index = 7, Osm = new OsmLegFeatures { ForestM = 900, MunicipalM = 100, LandUseRead = true } };
        Check(!A.FlagLeg(slowOnly).Flagged, "AggregateTacticalLevel: forest/municipal is REPORTED as slow, never a flag");
        Check(E.FlagLeg(legacy).Flagged && E.FlagLeg(legacy).Slope,
              "with the OSM readers off (Osm null) the entity rule is exactly the slope flag - the parity runs' rule");
    }

    // ================================================================= 7. the nudge search, pure
    private static void NudgeSearch()
    {
        Console.WriteLine("-- 7. the vertex nudge search (pure, synthetic verdicts)");
        var v = (Lat: 54.0, Lon: 23.0);
        var f = OsmGeometry.Frame.At(v.Lat, v.Lon);
        var prev = f.LatLon((0, -500));   // 500 m south
        var next = f.LatLon((0, 500));    // 500 m north: v is a PASSAGE point on a straight line
        var opt = new VertexNudgeOptions();
        Func<(double Lat, double Lon), NudgeVerdict> disc(double r) => p =>
        {
            double d = TileMath.DistanceMeters(v.Lat, v.Lon, p.Lat, p.Lon);
            return new NudgeVerdict(true, d <= r, false, false, d <= r ? "IN a synthetic pond" : "clear");
        };
        var n = VertexNudgeSearch.Nudge(3, v, prev, next, opt, disc(60), disc(60), "clear of water");
        var xy = f.Xy(n.To);
        Check(n.Problem && n.Moved && Math.Abs(n.DistanceM - 75.0) < 0.5,
              $"a vertex in a 60 m pond moves to the FIRST clear ring, 75 m (got moved={n.Moved}, {n.DistanceM:F1} m)");
        Check(Math.Abs(xy.X) < 1.0,
              $"a PASSAGE POINT stays on its lane: the chosen point is ON the line between its neighbours (cross-track {xy.X:F2} m)");
        Check(n.RefusedWater > 0 && n.Tried > n.RefusedWater, $"the inner rings were tried and refused for water ({n.RefusedWater} of {n.Tried})");
        var again = VertexNudgeSearch.Nudge(3, v, prev, next, opt, disc(60), disc(60), "clear of water");
        Check(again.To == n.To, "deterministic: the same inputs move the vertex to the same point");
        var last = VertexNudgeSearch.Nudge(3, v, prev, null, opt, disc(60), disc(60), "clear of water");
        var lxy = f.Xy(last.To);
        // The 75 m ring has 19 points (18.9 deg apart); the two nearest due south cost the same and the
        // lower bearing index wins - 12 m east of the line is the ring's own spacing, not a drift.
        Check(last.Moved && lxy.Y < -70.0 && Math.Abs(lxy.X) < 13.0,
              $"a LAST vertex moves to the NEAR side, towards its predecessor (got {lxy.X:F1} E, {lxy.Y:F1} N)");
        var big = VertexNudgeSearch.Nudge(3, v, prev, next, opt, disc(400), disc(400), "clear of water");
        int ringPoints = 0;
        for (double r = 25; r <= 300 + 1e-9; r += 25) ringPoints += VertexNudgeSearch.Ring(v, r, 25).Count;
        Check(big.Unresolved && !big.Moved && big.To == v && big.Tried == ringPoints && big.RefusedWater == ringPoints
              && big.SearchedMeters == 300,
              $"no clear ground within 300 m: KEPT and unresolved, every ring point tried ({big.Tried} of {ringPoints})");
        var off = VertexNudgeSearch.Nudge(3, v, prev, next, opt with { MaxMeters = 0 }, disc(60), disc(60), "clear of water");
        Check(off.Unresolved && off.Tried == 0, "Vrf:PreflightVertexNudgeMaxMeters = 0: checked and reported, never moved");
        NudgeVerdict unknownHere(ValueTuple<double, double> _) => new(false, false, false, false, "UNKNOWN - no tile");
        var unv = VertexNudgeSearch.Nudge(3, v, prev, next, opt, p => unknownHere(p), disc(60), "clear");
        Check(unv.Unverified && !unv.Problem && !unv.Moved, "a vertex whose tiles cannot be read is UNVERIFIED and NOT moved");
        var blind = VertexNudgeSearch.Nudge(3, v, prev, next, opt, disc(60), p => unknownHere(p), "clear");
        Check(blind.Unresolved && blind.RefusedUnknown == blind.Tried && blind.Tried > 0,
              "a candidate whose tiles cannot be read is NEVER taken - unknown is never clear");
        // Slope: everything north of v is flagged slope - the search must go south even though north
        // (towards next) and south (towards prev) cost the same.
        Func<(double Lat, double Lon), NudgeVerdict> steepNorth = p =>
        {
            var q = f.Xy(p);
            bool wet = TileMath.DistanceMeters(v.Lat, v.Lon, p.Lat, p.Lon) <= 60;
            return new NudgeVerdict(true, wet, false, !wet && q.Y > 0, "x");
        };
        var s = VertexNudgeSearch.Nudge(3, v, prev, next, opt, disc(60), steepNorth, "clear");
        Check(s.Moved && f.Xy(s.To).Y < 0 && s.RefusedSlope > 0,
              $"a candidate on flagged slope is refused: the move goes south ({f.Xy(s.To).Y:F0} m N; {s.RefusedSlope} refused for slope)");
        var route = new List<(double Lat, double Lon)> { prev, v, next };
        var applied = VertexNudgeSearch.Apply(route, new[] { n with { RouteIndex = 1 },
                                                              new VertexNudge { RouteIndex = 0, Moved = true, To = (1.0, 1.0) } });
        Check(applied.Count == 3 && applied[0] == prev && applied[1] == n.To && applied[2] == next,
              "Apply moves ONLY the nudged vertex - never index 0, the unit's own position");
    }

    // ================================================================= 8. the vertex check on the lake
    private static void NudgeOnTheLake(string cache, string tmp)
    {
        Console.WriteLine("-- 8. the vertex check on the real lake (service, offline tiles)");
        string noRaster = Path.Combine(tmp, "no-raster");
        var v = RouteShift.PointAlong(T14Start, T14DestE, 880.0);   // IN Jezioro Wiersnie (830.9 .. 934.9 m)
        var route = new List<(double Lat, double Lon)> { T14Start, v };
        using (var agg = new PreflightService(new PreflightOptions
               {
                   CacheDir = noRaster, OsmCacheDir = cache, Offline = true, OsmFeatures = true,
                   ModelSet = ModelSet.AggregateTacticalLevel,
               }))
        {
            Check(agg.CheckPoint(v).Water, "the test vertex, 880 m along the (e) leg, is IN the lake");
            var rows = agg.CheckVertices(route, 1.0, out var checkedRoute);
            var n = rows.Single();
            var after = agg.CheckPoint(n.To);
            Console.WriteLine($"     aggregate: {n.Why} -> moved {n.DistanceM:F0} m {n.Compass} ({n.Tried} tried)");
            Check(n.Moved && after.Known && !after.Water && n.DistanceM <= 300.0 && Math.Abs(n.DistanceM / 25.0 - Math.Round(n.DistanceM / 25.0)) < 0.02,
                  $"AGGREGATE: moved out of the lake onto known-clear ground, on a 25 m ring ({n.DistanceM:F1} m)");
            Check(TileMath.DistanceMeters(T14Start.Lat, T14Start.Lon, n.To.Lat, n.To.Lon)
                  < TileMath.DistanceMeters(T14Start.Lat, T14Start.Lon, v.Lat, v.Lon),
                  "the destination moved to the NEAR shore (towards the unit), not beyond the lake");
            Check(checkedRoute.Count == 2 && checkedRoute[0] == T14Start && checkedRoute[1] == n.To,
                  "the checked route is the authored one with that one vertex moved; index 0 untouched");
            var pre = agg.PreDispatch(route, 1.0, new RouteShiftOptions());
            Check(pre.MovedCount == 1 && pre.Changed && pre.CheckedRoute[1] == n.To,
                  "PreDispatch runs the check first and shifts the CHECKED route");
            var reports = PreflightReports.BuildForPreDispatch("u-1", "48 IBCT", "T14", pre, agg.Rules,
                                                              "2026-09-27T00:00:00Z", NewIds());
            Check(reports.Count >= 1 && reports[0].Contains("VERTEX MOVED", StringComparison.Ordinal),
                  $"the move is REPORTED to the C2 side first ({reports.Count} report(s))");
        }
        using (var ent = new PreflightService(new PreflightOptions
               {
                   CacheDir = noRaster, OsmCacheDir = cache, Offline = true, OsmFeatures = true,
                   ModelSet = ModelSet.EntityLevel,
               }))
        {
            var n = ent.CheckVertices(route, 1.0, out _).Single();
            Console.WriteLine($"     entity, no elevation: moved={n.Moved}, {n.Tried} tried, {n.RefusedUnknown} unknown, {n.RefusedWater} water");
            Check(n.Unresolved && n.RefusedUnknown > 0,
                  "ENTITY with NO elevation: OSM-clear candidates have UNKNOWN slope and are never taken - kept, reported");
        }
        using (var ent = new PreflightService(new PreflightOptions
               {
                   CacheDir = noRaster, OsmCacheDir = cache, Offline = true, OsmFeatures = true,
                   ModelSet = ModelSet.EntityLevel, VertexNudgeMaxMeters = 0,
               }))
        {
            var n = ent.CheckVertices(route, 1.0, out var same).Single();
            Check(n.Unresolved && !n.Moved && same[1] == v, "Vrf:PreflightVertexNudgeMaxMeters = 0 keeps the vertex and still reports it");
        }
    }

    // ================================================================= 9. "not on flagged slope"
    private static void NudgeSlope(string repo)
    {
        Console.WriteLine("-- 9. the entity nudge avoids FLAGGED SLOPE (Mojave ridge face, committed raster cache)");
        string raster = Path.Combine(repo ?? "", "tools", "preflight", "preflight_cache");
        if (repo == null || !Directory.Exists(raster) || Directory.GetFiles(raster).Length == 0)
        {
            Console.WriteLine($"  [SKIP] {raster} is empty (gitignored; copy it from the main checkout) - section not evaluated");
            return;
        }
        // Where the ridge leg's own flagged window centres (s ~ 2,006 m): a building is put ON it, so
        // the vertex there is bad and the nudge has to choose among the face and the ground beside it.
        using var probe = new PreflightService(new PreflightOptions { CacheDir = raster, Offline = true });
        var ridge = probe.ScoreLeg(RidgeA, RidgeB, 0.94);
        var v = (ridge.WorstLat, ridge.WorstLon);
        var world = new SyntheticWorld();
        world.AddBuilding("synthetic-on-the-face", Square(v, 8.0));
        using var ent = new PreflightService(new PreflightOptions
                                             { CacheDir = raster, Offline = true, OsmFeatures = true, ModelSet = ModelSet.EntityLevel },
                                             null, world.Provider);
        using var agg = new PreflightService(new PreflightOptions
                                             { CacheDir = raster, Offline = true, OsmFeatures = true, ModelSet = ModelSet.AggregateTacticalLevel },
                                             null, world.Provider);
        var route = new List<(double Lat, double Lon)> { RidgeA, v, RidgeB };
        var ne = ent.CheckVertices(route, 0.94, out _).First(r => r.RouteIndex == 1);
        var na = agg.CheckVertices(route, 0.94, out _).First(r => r.RouteIndex == 1);
        var le = ent.LocalSlope(ne.To, RidgeA, RidgeB, 0.94);
        var la = ent.LocalSlope(na.To, RidgeA, RidgeB, 0.94);
        Console.WriteLine($"     vertex on the face at ({v.Item1:F6},{v.Item2:F6}), leg ratio {ridge.Ratio:F3}");
        Console.WriteLine($"     entity   : moved {ne.DistanceM:F0} m {ne.Compass}, {ne.RefusedSlope} refused for slope, local ratio there {le.Ratio:F3}");
        Console.WriteLine($"     aggregate: moved {na.DistanceM:F0} m {na.Compass} (no slope rule), local ratio there {la.Ratio:F3}");
        Check(ne.Moved && le.Known && !le.Flagged,
              $"ENTITY: the vertex moves to ground whose 40 m approach/departure windows are NOT flagged (ratio {le.Ratio:F3} < 0.92)");
        Check(ne.RefusedSlope > 0, $"... having REFUSED {ne.RefusedSlope} nearer candidate(s) on the flagged face");
        Check(na.Moved && la.Known && la.Flagged && na.To != ne.To,
              $"AGGREGATE (slope OFF) takes the cheaper point that IS on the face (ratio {la.Ratio:F3}) - the criterion is what sent the entity vertex elsewhere");
        Check(ent.Tiles.Fetched == 0 && agg.Tiles.Fetched == 0, "nothing was fetched");
    }

    // ================================================================= 10. river and pond
    private static void RiverAndPond(string tmp)
    {
        Console.WriteLine("-- 10. a RIVER crossing is reported, a POND is detoured (synthetic world, aggregate)");
        string noRaster = Path.Combine(tmp, "no-raster");
        var a = (Lat: 54.020, Lon: 23.4810);
        var b = (Lat: 54.045, Lon: 23.4810);
        var river = new SyntheticWorld();
        river.AddRiver("river-1", new[] { (54.0325, 23.40), (54.0325, 23.55) }, 5.0);
        var route = new List<(double Lat, double Lon)> { a, b };
        using (var agg = Service(noRaster, ModelSet.AggregateTacticalLevel, river))
        {
            // M3: a river crossing is a REPORT, so a planned leg keeps it (only the detour search is skipped).
            var plannedRiver = agg.ShiftRoute(route, 1.0, new RouteShiftOptions { ReportOnlyReason = "PLANNED (test)" }).Shifts.Single();
            Check(plannedRiver.RiverCrossing && !plannedRiver.ReportOnly && !plannedRiver.Shifted,
                  "M3: under ReportOnlyReason a river crossing is still REPORTED as one (a report, not a detour)");
            var o = agg.ShiftRoute(route, 1.0, new RouteShiftOptions());
            var leg = o.Legs.Single();
            Check(leg.Osm.Water && leg.Osm.WaterIsRiverLine && leg.ShiftFlagged && leg.FlagWater,
                  $"AGGREGATE: the leg crossing a River line (MAK_WIDTH 5 m) is flagged ({leg.FlagReason})");
            var s = o.Shifts.Single();
            Check(s.RiverCrossing && !s.Shifted && s.Tried.Count == 0 && s.Note.Contains("RIVER CROSSING")
                  && s.Note.Contains("needs a road/bridge; STP authoring"),
                  $"water at BOTH band ends -> reported as a RIVER CROSSING, nothing searched ({s.Note})");
            Check(!o.Changed && o.Route.SequenceEqual(route), "the authored line is kept, point for point");
            string mark = PreflightReports.NoShiftMarking("T9", "unit", s);
            Check(mark.Contains("RIVER CROSSING") && mark.Contains("road/bridge") && mark.Contains("STP") && mark.Contains("as authored"),
                  "the C2 side is told: a river crossing, needs a road/bridge, STP authoring, dispatched as authored");
        }
        using (var ent = Service(noRaster, ModelSet.EntityLevel, river))
        {
            var o = ent.ShiftRoute(route, 1.0, new RouteShiftOptions());
            Check(!o.Legs.Single().Osm.Water && o.Shifts.Count == 0,
                  "ENTITY: a River LINE is not water (MAK Earth (online).earth loads no River layer) - no flag");
        }
        // One band end over an UNREADABLE tile: not a river report - the search runs and refuses.
        var holed = new SyntheticWorld();
        holed.AddRiver("river-1", new[] { (54.0325, 23.40), (54.0325, 23.55) }, 5.0);
        var eastBand = RouteShift.BandEndLine(a, b, (1390, 1390), 600.0, 600.0);
        int eastX = OsmTileMath.TileOf(eastBand[0].Lat, eastBand[0].Lon).X;
        holed.UnknownColumn = eastX;
        using (var agg = Service(noRaster, ModelSet.AggregateTacticalLevel, holed))
        {
            Check(eastX != OsmTileMath.TileOf(a.Lat, a.Lon).X, $"(the east band end is in tile column {eastX}, the leg is not)");
            var s = agg.ShiftRoute(route, 1.0, new RouteShiftOptions()).Shifts.Single();
            Check(!s.RiverCrossing && !s.Shifted && s.Note.Contains("could not be read") && s.Note.Contains("OSM water"),
                  $"an UNKNOWN band end is not 'wet': searched, every candidate refused (unknown or wet) - {s.Note}");
        }
        // A pond 140 m in radius ON the line: the band ends are dry, the lateral search clears it. On
        // the aggregate profile "clear" is the centreline NOT touching water (the unit's centre point
        // is what the mobility table reads, UG52 27.1.4), so the first 25 m step past 140 m wins.
        var pond = new SyntheticWorld();
        var c = (Lat: 54.0325, Lon: 23.4810);
        pond.AddWater("pond-1", Disc(c, 140.0, 64), 81);
        using (var agg = Service(noRaster, ModelSet.AggregateTacticalLevel, pond))
        {
            var o = agg.ShiftRoute(route, 1.0, new RouteShiftOptions());
            var s = o.Shifts.Single();
            var detour = new List<(double Lat, double Lon)> { a };
            detour.AddRange(s.Inserted);
            detour.Add(b);
            var wet = OsmQuery.Polyline(pond.Provider, detour, ModelSetRules.Aggregate, 8.0);
            Console.WriteLine($"     pond: {s.Note}");
            Check(s.Shifted && !s.RiverCrossing && Math.Abs(Math.Abs(s.OffsetMeters) - 150.0) < 1e-9,
                  $"the SMALLEST offset that clears a 140 m pond is taken: 150 m (got {s.OffsetMeters:+0;-0} m)");
            Check(wet.WetSegments == 0 && wet.UnknownTiles == 0 && wet.MinDistanceM >= 9.5 && wet.MinDistanceM <= 10.5,
                  $"the detour is KNOWN clear of the water, and the distance is MEASURED past the corridor: 150 - 140 = 10 m (got {wet.MinDistanceM:F2} m)");
            Check(s.Tried.Where(t => Math.Abs(t.OffsetMeters) < 150).All(t => !t.Accepted && t.Refusal.StartsWith("OSM water")),
                  "every smaller offset was refused for OSM water");
            Check(o.Route.Count == 6 && o.Route[0] == a && o.Route[5] == b, "four points inserted; STP's vertices kept");
            string mark = PreflightReports.ShiftMarking("T9", "unit", s);
            Check(mark.Contains("clear of OSM water") && mark.Contains("vertices are unchanged"),
                  "the report says the detour clears OSM water, and that STP's vertices are unchanged");
            // M3 (RL-20260928-03; FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28 sec 5 item 4): THE SAME LEG, driven by a
            // vendor PLANNING task - REPORTED, NOT DETOURED. The shift above (default options) is this check's fail-first arm.
            var planned = agg.ShiftRoute(route, 1.0, new RouteShiftOptions { ReportOnlyReason = "PLANNED (test)" });
            var ps = planned.Shifts.Single();
            var pleg = planned.Legs.Single();
            Check(pleg.ShiftFlagged && pleg.FlagWater && ps.ReportOnly && !ps.Shifted && ps.Tried.Count == 0
                  && ps.Note == "PLANNED (test)" && !planned.Changed && planned.Route.SequenceEqual(route),
                  "M3: with RouteShiftOptions.ReportOnlyReason the SAME pond leg is still FLAGGED and gets its row, but nothing is " +
                  "searched and nothing inserted - the authored line is kept point for point (the default arm above shifts it 150 m)");
            string pmark = PreflightReports.NoShiftMarking("T9", "unit", ps);
            Check(pmark.StartsWith("ROUTE SHIFT NOT APPLIED - PLANNED LEG: task T9 (unit) leg 1 - flagged (", StringComparison.Ordinal)
                  && pmark.Contains("PLANNED (test)") && pmark.Contains("the planner chooses the path"),
                  $"M3: the C2 side is told the leg is PLANNED, not detoured ({pmark})");
        }
        // A LAKE DISTRICT: the pond on the leg, and two OTHER ponds sitting exactly on the +/-600 m band
        // ends. "Water at both band ends" is true and is NOT a river crossing - the search runs and
        // clears the pond on the leg at 150 m, as it would without the other two.
        var district = new SyntheticWorld();
        var fc0 = OsmGeometry.Frame.At(c.Lat, c.Lon);
        district.AddWater("pond-1", Disc(c, 140.0, 64), 81);
        district.AddWater("pond-east-600", Disc(fc0.LatLon((600.0, 0.0)), 50.0, 32), 81);
        district.AddWater("pond-west-600", Disc(fc0.LatLon((-600.0, 0.0)), 50.0, 32), 81);
        using (var agg = Service(noRaster, ModelSet.AggregateTacticalLevel, district))
        {
            var s = agg.ShiftRoute(route, 1.0, new RouteShiftOptions()).Shifts.Single();
            Console.WriteLine($"     lake district: {s.Note}");
            Check(!s.RiverCrossing && s.Shifted && Math.Abs(Math.Abs(s.OffsetMeters) - 150.0) < 1e-9,
                  $"OTHER ponds on both band ends do not make a river: searched and shifted 150 m (got river={s.RiverCrossing}, {s.OffsetMeters:+0;-0} m)");
        }
        // A BRIDGE. The plan's remedy for a river crossing is STP routing over a bridge, so a leg that
        // crosses the river ON a road bridge must not be flagged, and a vertex on the deck must not be
        // moved off it - on either model set. A 40 m-wide river polygon across the leg, a road bridge
        // along the leg over it; then the same leg 40 m beside the bridge, which is still water.
        var bridged = new SyntheticWorld();
        var fb = OsmGeometry.Frame.At(c.Lat, c.Lon);
        bridged.AddWater("river-poly", new List<(double Lat, double Lon)>
        {
            fb.LatLon((-2000, -20)), fb.LatLon((2000, -20)), fb.LatLon((2000, 20)), fb.LatLon((-2000, 20)), fb.LatLon((-2000, -20)),
        }, 82);
        var deckS = fb.LatLon((0, -45));
        var deckN = fb.LatLon((0, 45));
        bridged.AddBridge("bridge-1", new[] { (deckS.Lat, deckS.Lon), (deckN.Lat, deckN.Lon) });
        var onBridge = new List<(double Lat, double Lon)> { a, b };                 // along the bridge (x = 0)
        var beside = new List<(double Lat, double Lon)> { fb.LatLon((40, -1390)), fb.LatLon((40, 1390)) };
        foreach (var set in new[] { ModelSet.AggregateTacticalLevel, ModelSet.EntityLevel })
            using (var svc = Service(noRaster, set, bridged))
            {
                var on = svc.ShiftRoute(onBridge, 1.0, new RouteShiftOptions()).Legs.Single();
                var off = svc.ShiftRoute(beside, 1.0, new RouteShiftOptions()).Legs.Single();
                Check(!on.Osm.Water && !on.FlagWater && on.Osm.BridgeIds.Contains("bridge-1"),
                      $"{set}: the leg that crosses the river ON the road bridge is NOT flagged (bridges [{string.Join(",", on.Osm.BridgeIds)}])");
                Check(off.Osm.Water && off.FlagWater,
                      $"{set}: the same leg 40 m BESIDE the bridge crosses water and IS flagged");
                var deck = svc.CheckPoint(c);                                       // mid-river, on the deck
                var inRiver = svc.CheckPoint(fb.LatLon((40, 0)));                  // mid-river, 40 m beside it
                Check(!deck.Water && deck.OnBridge && inRiver.Water,
                      $"{set}: a vertex ON the deck is not in water (kept), one in the river 40 m beside it is");
                var n = svc.CheckVertices(new List<(double Lat, double Lon)> { a, c, b }, 1.0, out var kept)
                           .First(r => r.RouteIndex == 1);
                Check(!n.Moved && !n.Problem && n.OnBridge && kept[1] == c,
                      $"{set}: the vertex check KEEPS a passage point on the bridge deck ({n.Why})");
            }
        // THE SIDE ON THE AGGREGATE PROFILE. Every water-clear candidate there ties at ratio 0, so the
        // side is the one that stays FURTHER from water. A pond of radius 60 m centred 10 m EAST of the
        // line spans -50..+70 m: at 75 m the east detour clears it by 5 m, the west one by 25 m.
        var lopsided = new SyntheticWorld();
        var fc = OsmGeometry.Frame.At(c.Lat, c.Lon);
        lopsided.AddWater("pond-east", Disc(fc.LatLon((10.0, 0.0)), 60.0, 64), 81);
        using (var agg = Service(noRaster, ModelSet.AggregateTacticalLevel, lopsided))
        {
            var s = agg.ShiftRoute(route, 1.0, new RouteShiftOptions()).Shifts.Single();
            Console.WriteLine($"     lopsided pond: {s.Note}");
            Check(s.Shifted && Math.Abs(s.OffsetMeters + 75.0) < 1e-9 && s.SideWord == "west",
                  $"both sides clear at 75 m; the one FURTHER from water (west, 25 m vs 5 m) is taken (got {s.OffsetMeters:+0;-0} m {s.SideWord})");
        }
    }

    // ================================================================= 11. the chooser's OSM half
    private static void ChooserOsm()
    {
        Console.WriteLine("-- 11. the chooser's OSM half (pure, synthetic scorer)");
        var a = (Lat: 54.0, Lon: 23.0);
        var f = OsmGeometry.Frame.At(a.Lat, a.Lon);
        var b = f.LatLon((0, 3000));
        var leg = new LegMetrics { Index = 1, FlagWater = true, FlagReason = "OSM water", Osm = new OsmLegFeatures { Water = true } };
        var window = (1400.0, 1600.0);
        var opt = new RouteShiftOptions { ClearFormationBand = false, RequireFeaturesKnown = true };
        PolyScore Score(IReadOnlyList<(double Lat, double Lon)> poly, Func<double, (int Wet, int Unk, double Clear)> byOffset)
        {
            double off = f.Xy(poly[2]).X;      // D1's lateral offset (east = right of a northbound leg)
            var (wet, unk, clear) = byOffset(off);
            return new PolyScore(0.0, 0, wet, unk, clear);
        }
        var tie = RouteShift.ChooseForLeg(a, b, leg, opt,
            p => Score(p, off => (Math.Abs(off) < 99 ? 1 : 0, 0, off > 0 ? 50.0 : 80.0)), window);
        Check(tie.Shifted && Math.Abs(tie.OffsetMeters + 100) < 1e-6,
              $"a tie at 0 ratio goes to the side FURTHER from water (-100 m, clearance 80 vs 50; got {tie.OffsetMeters:+0;-0})");
        var unknownRight = RouteShift.ChooseForLeg(a, b, leg, opt,
            p => Score(p, off => (Math.Abs(off) < 74 ? 1 : 0, off > 0 ? 1 : 0, 60.0)), window);
        Check(unknownRight.Shifted && unknownRight.OffsetMeters < 0
              && unknownRight.Tried.Any(t => t.OffsetMeters > 0 && t.Refusal.Contains("could not be read")),
              $"a WATER-flagged leg refuses a candidate over an unreadable tile (took {unknownRight.OffsetMeters:+0;-0} m)");
        var slopeOnly = leg with { FlagWater = false };
        var lax = RouteShift.ChooseForLeg(a, b, slopeOnly, opt with { RequireFeaturesKnown = false },
            p => Score(p, off => (Math.Abs(off) < 74 ? 1 : 0, off > 0 ? 1 : 0, 60.0)), window);
        Check(lax.Shifted && lax.OffsetMeters > 0 && lax.Note.Contains("OSM water was NOT checked"),
              $"a SLOPE-only leg keeps its pre-OSM rule for unreadable tiles, and SAYS so ({lax.Note})");
        var neverWet = RouteShift.ChooseForLeg(a, b, slopeOnly, opt with { RequireFeaturesKnown = false },
            p => Score(p, off => (Math.Abs(off) < 999 ? 1 : 0, 0, 5.0)), window);
        Check(!neverWet.Shifted && neverWet.Note.Contains("ran into OSM water"),
              "a detour is NEVER driven into KNOWN water, on any leg");
        var legacy = RouteShift.ChooseForLeg(a, b, slopeOnly, opt with { RequireFeaturesKnown = false },
            p => new PolyScore(0.5, 0), window);
        Check(legacy.Shifted && Math.Abs(legacy.OffsetMeters - 25) < 1e-9 && !legacy.Note.Contains("OSM"),
              "with the OSM fields at their defaults the chooser is the pre-OSM chooser (first step, right side, no OSM words)");
        bool river = RouteShift.IsRiverCrossing(a, b, window, new RouteShiftOptions(),
            p => new RouteShift.BandEndProbe(true, false, 0, "river-1"), out string note);
        bool notRiver = RouteShift.IsRiverCrossing(a, b, window, new RouteShiftOptions(),
            p => new RouteShift.BandEndProbe(f.Xy(p[0]).X > 0, false, 0, "river-1"), out string note2);
        bool otherOnly = RouteShift.IsRiverCrossing(a, b, window, new RouteShiftOptions(),
            p => new RouteShift.BandEndProbe(false, true, 0, ""), out string note3);
        Check(river && !notRiver && note.Contains("SAME water") && note2.Contains("dry"),
              $"the river test: the SAME water at BOTH band ends is a river, at one end only is not ({note2})");
        Check(!otherOnly && note3.Contains("other water only"),
              $"OTHER water at both band ends (a lake district) is NOT a river crossing ({note3})");
        var band = RouteShift.BandEndLine(a, b, window, 600, 600);
        Check(Math.Abs(f.Xy(band[0]).X - 600) < 1 && Math.Abs(f.Xy(band[0]).Y - 800) < 1 && Math.Abs(f.Xy(band[1]).Y - 2200) < 1,
              "a band end is the leg's parallel line at the band edge, over the flagged span +/- the band");
    }

    // ================================================================= 12. water + slope together
    private static void WaterDetourWithSlope(string repo)
    {
        Console.WriteLine("-- 12. ENTITY: a leg flagged for slope AND water - the detour must clear both");
        string raster = Path.Combine(repo ?? "", "tools", "preflight", "preflight_cache");
        if (repo == null || !Directory.Exists(raster) || Directory.GetFiles(raster).Length == 0)
        {
            Console.WriteLine($"  [SKIP] {raster} is empty - section not evaluated");
            return;
        }
        var world = new SyntheticWorld();
        var pondC = RouteShift.PointAlong(RidgeA, RidgeB, 4000.0);   // flat ground (the leg past 2.3 km scores 0.346)
        world.AddWater("pond-on-the-ridge-leg", Disc(pondC, 60.0, 48), 80);
        using var ent = new PreflightService(new PreflightOptions
                                             { CacheDir = raster, Offline = true, OsmFeatures = true, ModelSet = ModelSet.EntityLevel },
                                             null, world.Provider);
        var o = ent.ShiftRoute(new List<(double Lat, double Lon)> { RidgeA, RidgeB }, 0.94, new RouteShiftOptions());
        var leg = o.Legs.Single();
        var s = o.Shifts.Single();
        Console.WriteLine($"     leg: {leg.FlagReason}");
        Console.WriteLine($"     shift: {s.Note}");
        Console.WriteLine($"     candidates: {RouteShift.DescribeCandidates(s)}");
        Check(leg.FlagSlope && leg.FlagWater, "the ridge leg is flagged for its 1.098 face AND for the pond 4 km along");
        var detour = new List<(double Lat, double Lon)> { RidgeA };
        detour.AddRange(s.Inserted);
        detour.Add(RidgeB);
        var wet = OsmQuery.Polyline(world.Provider, detour, ModelSetRules.Entity, 8.0);
        double ratio = ent.WorstRatioScorer(0.94)(detour).WorstRatio;
        var def = new RouteShiftOptions();
        // ONE detour spans BOTH windows (the face at ~2.0 km and the pond at ~3.9 km): the chooser moves
        // one window per leg (DESIGN_ROUTE_SHIFT sec 8 L1), so a leg flagged for both gets the union.
        // That long parallel stretch is NOT the short face detour the record's "north" was measured on,
        // so the side is whatever the calibrated sampler finds - what is asserted is the rules.
        Check(s.Shifted && Math.Abs(s.OffsetMeters) >= 100,
              $"it is shifted by at least the first step that clears the pond's 60 m + 25 m corridor (got {s.OffsetMeters:+0;-0} m {s.SideWord})");
        Check(wet.WetSegments == 0 && wet.UnknownTiles == 0 && ratio <= def.AcceptRatio && s.BandMax < def.Threshold,
              $"the detour is KNOWN clear of the pond (nearest {wet.MinDistanceM:F0} m), clears C1 on slope ({ratio:F3} <= {def.AcceptRatio:F3}) and C2 (band {s.BandMax:F3})");
        Check(s.Tried.Where(t => Math.Abs(t.OffsetMeters) < Math.Abs(s.OffsetMeters)).All(t => !t.Accepted),
              "no smaller magnitude was acceptable - the smallest shift that clears BOTH is the one taken");
        Check(ent.Tiles.Fetched == 0, "nothing was fetched");
    }

    // ================================================================= 13. reports
    private static void Reports()
    {
        Console.WriteLine("-- 13. what the C2 side is told");
        var moved = new VertexNudge
        {
            RouteIndex = 2, From = (54.027000, 23.318000), To = (54.026500, 23.317800), Problem = true, Moved = true,
            Why = "IN OSM water (natural=water, OSM 197345448)", DistanceM = 50, Compass = "south",
            SearchedMeters = 50, Tried = 20, ClearOf = "clear of OSM water within 25 m",
        };
        string m = PreflightReports.VertexMarking("T14", "48 IBCT", moved);
        Check(m.StartsWith("VERTEX MOVED") && m.Contains("(54.027000,23.318000)") && m.Contains("(54.026500,23.317800)")
              && m.Contains("50 m south") && m.Contains("197345448") && m.Contains("unchanged and in order"),
              "a MOVED vertex says what moved, from where, to where, how far, which way and why");
        string xml = PreflightReports.BuildVertexReport("u-1", "48 IBCT", "T14", moved, "2026-09-27T00:00:00Z", "r-1");
        Check(Occ(xml, "<LocationObservation>") == 1 && Occ(xml, "<NameObservation>") == 1
              && xml.Contains("<Latitude>54.0265</Latitude>") && !xml.Contains("<AltitudeMSL>"),
              "the report is the Location + Name pair, located at the NEW point, with no invented altitude");
        var kept = moved with { Moved = false, To = moved.From, SearchedMeters = 300, Tried = 490, RefusedWater = 490 };
        string k = PreflightReports.VertexMarking("T14", "48 IBCT", kept);
        Check(k.StartsWith("VERTEX NOT MOVED") && k.Contains("within 300 m") && k.Contains("490 in water")
              && k.Contains("STP authoring"),
              "a KEPT vertex says no clear ground was found, how far was searched, and that STP must fix it");
        var leg = new LegMetrics
        {
            Index = 1, Samples = 300,
            Osm = new OsmLegFeatures { Water = true, WaterId = "197345448", WaterKind = "natural=water", WaterValue = 80,
                                       WaterFirstSM = 806, WaterSamples = 20, Samples = 300, WaterFirst = (54.0262, 23.3169) },
        };
        string we = PreflightReports.OsmWaterMarking("T14", "48 IBCT", leg, ModelSetRules.Entity);
        string wa = PreflightReports.OsmWaterMarking("T14", "48 IBCT", leg, ModelSetRules.Aggregate);
        Check(we.Contains("within 25 m of the line") && we.Contains("Lake feature") && we.Contains("deep-water")
              && wa.Contains("ON the line") && wa.Contains("speed-factor 0") && wa.Contains("STOPS"),
              "the OSM water finding speaks each model set's physics (entity: Lake obstacle/deep-water; aggregate: MAK_WATERWAY 0)");
        var slow = new LegMetrics { Index = 2, Start = (54.0, 23.0), Osm = new OsmLegFeatures { ForestM = 800, MunicipalM = 150, LengthM = 3000, LandUseRead = true } };
        string sl = PreflightReports.SlowTerrainMarking("T9", "1 BDE", slow);
        Check(sl.Contains("EXPECTED SLOW") && sl.Contains("950 m of the 3000 m") && sl.Contains("0.25") && sl.Contains("Reported only"),
              "the aggregate slow-terrain finding is a report, never a flag");
        var water = new LegShift { LegIndex = 1, Shifted = true, FlagWater = true, FlagReason = "OSM water ON the centreline",
                                   OffsetMeters = 175, SideWord = "east", BandMax = double.NaN, EndpointMoved = true };
        string sm = PreflightReports.ShiftMarking("T9", "unit", water);
        Check(sm.Contains("clear of OSM water") && sm.Contains("moved off water or a building") && !sm.Contains("unchanged and in order"),
              "a shift on a leg whose endpoint the vertex check moved does NOT claim STP's vertices are unchanged");
        var o = new PreDispatchOutcome(new List<VertexNudge> { moved, kept with { RouteIndex = 3 }, new VertexNudge { RouteIndex = 4, Unverified = true } },
                                       new List<(double Lat, double Lon)>(),
                                       new PreflightService.RouteShiftOutcome(new List<(double Lat, double Lon)>(), new List<LegShift>(),
                                                                              new List<LegMetrics> { leg, slow }, 0));
        var e = PreflightReports.BuildForPreDispatch("u", "unit", "T", o, ModelSetRules.Entity, "t", NewIds());
        var ag = PreflightReports.BuildForPreDispatch("u", "unit", "T", o, ModelSetRules.Aggregate, "t", NewIds());
        Check(e.Count == 3 && e[0].Contains("VERTEX MOVED") && e[1].Contains("VERTEX NOT MOVED") && e[2].Contains("OSM WATER"),
              $"the policy: moved + kept vertices (not the unverified one), then the OSM water leg (entity: {e.Count})");
        Check(ag.Count == 4 && ag[3].Contains("EXPECTED SLOW"), $"... and on the aggregate profile the slow leg too ({ag.Count})");
    }

    // ================================================================= 14. the cache and the fetch
    private static void CacheSemantics(string tmp)
    {
        Console.WriteLine("-- 14. the OSM tile cache: absent, empty, failed, undecodable (stubbed HttpClient)");
        var tileBytes = new MvtWriter("osm").Add(Props(5L, "natural", "water"), Mvt.GeomPolygon,
                                                 new[] { Ring(100, 100, 200, 100, 200, 200, 100, 100) }).Tile();
        string d404 = Path.Combine(tmp, "c404");
        var s404 = new Stub(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        using (var ts = new TileSource(d404, http: new HttpClient(s404)))
        {
            var t = ts.GetOsmTile(OsmSet.Water, 9253, 11125);
            ts.GetOsmTile(OsmSet.Water, 9253, 11125);
            string fn = Path.Combine(d404, "osm-water", "14_9253_11125.pbf");
            Check(t.Known && t.Water.Count == 0 && t.State.Contains("404") && s404.Calls == 1
                  && File.Exists(fn) && Mvt.IsAbsentMarker(Mvt.Decode(File.ReadAllBytes(fn))),
                  "a 404 is the SERVER'S answer: KNOWN and empty, asked ONCE, and the marker is WRITTEN");
        }
        using (var ts = new TileSource(d404, offline: true))
        {
            var t = ts.GetOsmTile(OsmSet.Water, 9253, 11125);
            Check(t.Known && t.State.Contains("marker"), "an OFFLINE run reads that marker as KNOWN-empty, not as UNKNOWN");
        }
        string dOk = Path.Combine(tmp, "cok");
        var sOk = new Stub(_ => Body(tileBytes));
        using (var ts = new TileSource(dOk, http: new HttpClient(sOk)))
        {
            var t = ts.GetOsmTile(OsmSet.Water, 1, 2);
            Check(t.Known && t.Water.Count == 1 && t.Water[0].Id == "5" && ts.Fetched == 1
                  && File.Exists(Path.Combine(dOk, "osm-water", "14_1_2.pbf")),
                  "a 200 vector tile is decoded, filtered and CACHED under <cache>/osm-water/");
        }
        using (var ts = new TileSource(Path.Combine(tmp, "cgz"), http: new HttpClient(new Stub(_ => Body(Gzip(tileBytes))))))
            Check(ts.GetOsmTile(OsmSet.Water, 1, 2).Water.Count == 1, "a gzip body decodes");
        var s503 = new Stub(_ => new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        using (var ts = new TileSource(Path.Combine(tmp, "c503"), http: new HttpClient(s503)))
        {
            var t = ts.GetOsmTile(OsmSet.All, 3, 4);
            for (int i = 0; i < 6; i++) ts.GetOsmTile(OsmSet.All, 3, 4);
            Check(!t.Known && s503.Calls == TileSource.MaxFetchAttempts && ts.ExhaustedTiles == 1,
                  $"a 503 is UNKNOWN, NOT memoised, retried up to {TileSource.MaxFetchAttempts} times, then given up ({s503.Calls} calls)");
        }
        string dHtml = Path.Combine(tmp, "chtml");
        var html = Encoding.ASCII.GetBytes("<html><head><title>Captive portal</title></head><body>" + new string('x', 3000) + "</body></html>");
        using (var ts = new TileSource(dHtml, http: new HttpClient(new Stub(_ => Body(html)))))
        {
            var t = ts.GetOsmTile(OsmSet.Water, 5, 6);
            Check(!t.Known && ts.UndecodableBodies == 1 && !File.Exists(Path.Combine(dHtml, "osm-water", "14_5_6.pbf")),
                  "an HTML body with 200 is UNKNOWN, counted undecodable and NEVER cached (SF3)");
        }
        string dOff = Path.Combine(tmp, "coff");
        Directory.CreateDirectory(Path.Combine(dOff, "osm-water"));
        File.WriteAllBytes(Path.Combine(dOff, "osm-water", "14_7_8.pbf"), Array.Empty<byte>());
        File.WriteAllBytes(Path.Combine(dOff, "osm-water", "14_9_9.pbf"), html);
        using (var ts = new TileSource(dOff, offline: true))
        {
            var missing = ts.GetOsmTile(OsmSet.Water, 1, 1);
            var empty = ts.GetOsmTile(OsmSet.Water, 7, 8);
            var poisoned = ts.GetOsmTile(OsmSet.Water, 9, 9);
            Check(!missing.Known && missing.State.Contains("not in the tile cache"), "OFFLINE, a missing tile is UNKNOWN");
            Check(!empty.Known && empty.State.Contains("0-byte"), "OFFLINE, a 0-byte tile is UNKNOWN");
            Check(!poisoned.Known && poisoned.State.Contains("not a vector tile"), "a poisoned cache file is UNKNOWN, loud, never 'no features'");
            Check(ts.Fetched == 0, "and nothing was fetched");
        }
        var sRe = new Stub(_ => Body(tileBytes));
        using (var ts = new TileSource(dOff, http: new HttpClient(sRe)))
        {
            var t = ts.GetOsmTile(OsmSet.Water, 7, 8);
            Check(t.Known && sRe.Calls == 1, "ONLINE, a 0-byte tile is RE-FETCHED to find out what it was");
        }
        var census = TileSource.CensusOsmCache(dOff);
        Check(census.WaterFiles == 2 && census.WaterEmpty == 0 && census.Undecodable == 1,
              $"the start-up census counts files, 0-byte files and non-tiles ({census.WaterFiles}/{census.WaterEmpty}/{census.Undecodable})");
    }

    // ================================================================= optional: REAL tiles
    private static void RealTiles(string repo, string osmDir, string rasterDir)
    {
        Console.WriteLine($"-- R. the (e) T14 line on REAL tiles: {osmDir}");
        rasterDir ??= Path.Combine(repo ?? "", "tools", "preflight", "preflight_cache");
        // The (e) line (what E1/G1 are registered to drive), then the line as STP EXPORTED it, before
        // the hand edit (e) moved its destination 799 m west (data/IRONSTORM_CUTA_CHANGES.md (e)).
        foreach (var (label, dest) in new[] { ("the (e) line", T14DestE), ("the EXPORTED line, before (e)", T14DestExported) })
        foreach (var set in new[] { ModelSet.AggregateTacticalLevel, ModelSet.EntityLevel })
        {
            var route = new List<(double Lat, double Lon)> { T14Start, dest };
            Console.WriteLine($"   == {label} ==");
            using var svc = new PreflightService(new PreflightOptions
            {
                CacheDir = rasterDir, OsmCacheDir = osmDir, Offline = true, OsmFeatures = true, ModelSet = set,
                ElevationLevel = 13, ElevationMinLevel = 11,
            });
            var pre = svc.PreDispatch(route, 1.0, new RouteShiftOptions());   // M577A2 max-slope 1.0
            Console.WriteLine($"   {set}: {svc.Rules.Describe()}");
            foreach (var n in pre.Vertices)
                Console.WriteLine($"     vertex {n.RouteIndex}: {(n.Moved ? "MOVED" : n.Unresolved ? "KEPT (bad)" : n.Unverified ? "UNVERIFIED" : "clear")} - {n.Why}");
            foreach (var l in pre.Shift.Legs)
                Console.WriteLine($"     leg {l.Index}: {l.LengthM:F0} m, flagged={l.ShiftFlagged} ({l.FlagReason}); slope ratio {l.Ratio:F3} L{l.ElevationLevel}{(l.NoVerdict ? " NO VERDICT" : "")}; " +
                                  $"OSM water={l.Osm?.Water} [{l.Osm?.WaterFirstSM:F0}..{l.Osm?.WaterLastSM:F0} m], unknown tiles {l.Osm?.UnknownTiles}, slow {l.Osm?.SlowM:F0} m");
            foreach (var s in pre.Shift.Shifts)
            {
                Console.WriteLine($"     shift leg {s.LegIndex}: {s.Note}");
                if (s.Shifted)
                    Console.WriteLine($"       inserted {string.Join(" ", s.Inserted.Select(p => p.Lat.ToString("F6", CultureInfo.InvariantCulture) + "," + p.Lon.ToString("F6", CultureInfo.InvariantCulture)))}");
                Console.WriteLine($"       candidates: {RouteShift.DescribeCandidates(s)}");
            }
            // DIAGNOSTIC: what the band ends hold, by identity, and what ANY water there would say.
            foreach (var l in pre.Shift.Legs.Where(x => x.FlagWater))
            {
                var span = (l.Osm.WaterFirstSM, l.Osm.WaterLastSM);
                var opt = new RouteShiftOptions();
                bool river = RouteShift.IsRiverCrossing(l.Start, l.End, span, opt, svc.SameWaterProbe(l.Start, l.End), out string note);
                var anyE = OsmQuery.WaterHits(svc.Tiles.OsmProvider, RouteShift.BandEndLine(l.Start, l.End, span, +600, 600), svc.Rules, 8.0);
                var anyW = OsmQuery.WaterHits(svc.Tiles.OsmProvider, RouteShift.BandEndLine(l.Start, l.End, span, -600, 600), svc.Rules, 8.0);
                var legHits = OsmQuery.WaterHits(svc.Tiles.OsmProvider, new[] { l.Start, l.End }, svc.Rules, 8.0);
                Console.WriteLine($"     leg {l.Index} water ids on the leg [{string.Join(",", legHits.Hits.Select(h => h.Id))}]; " +
                                  $"band end +600 [{string.Join(",", anyE.Hits.Select(h => h.Id))}] (unknown {anyE.UnknownTiles}); " +
                                  $"band end -600 [{string.Join(",", anyW.Hits.Select(h => h.Id))}] (unknown {anyW.UnknownTiles})");
                Console.WriteLine($"     river test: {(river ? "RIVER" : "not a river")} - {note}");
            }
            var reports = PreflightReports.BuildForPreDispatch("u", "48 IBCT", "T14", pre, svc.Rules, "t", NewIds());
            Console.WriteLine($"     {reports.Count} report(s); tiles: {svc.Tiles.CacheHits} cache hit(s), {svc.Tiles.Fetched} fetched");
            Check(svc.Tiles.Fetched == 0, $"{set}: the real-tile run fetched nothing (offline)");
        }

        // COST, on the worst ground the cache holds: a ~28 km diagonal across the whole lake district,
        // the OSM half only (the 30 s route-shift budget has to hold a leg like this, and a nudge search).
        var d0 = (53.975, 23.11);
        var d1 = (54.165, 23.40);
        foreach (var rules in new[] { ModelSetRules.Entity, ModelSetRules.Aggregate })
        {
            using var ts = new TileSource(rasterDir, offline: true, osmCacheDir: osmDir);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var leg = OsmQuery.Leg(ts.OsmProvider, d0, d1, rules, 8.0);
            long legMs = sw.ElapsedMilliseconds;
            sw.Restart();
            var poly = OsmQuery.Polyline(ts.OsmProvider, new[] { d0, d1 }, rules, 8.0);
            long polyMs = sw.ElapsedMilliseconds;
            Console.WriteLine($"   COST {rules.ModelSet}: {leg.LengthM / 1000.0:F1} km diagonal - Leg {legMs} ms (water={leg.Water}, " +
                              $"{leg.WaterSamples} wet sample(s), unknown tiles {leg.UnknownTiles}, slow {leg.SlowM:F0} m); " +
                              $"Polyline {polyMs} ms ({poly.WetSegments} wet segment(s))");
        }
    }

    // ================================================================= helpers
    private static PreflightService Service(string raster, ModelSet set, SyntheticWorld world)
        => new(new PreflightOptions { CacheDir = raster, Offline = true, OsmFeatures = true, ModelSet = set },
               null, world.Provider);

    /// <summary>A synthetic OSM world: every tile of a set holds every feature of that set (the
    /// queries filter by distance, so duplicates are harmless), except a tile column that can be
    /// made UNKNOWN.</summary>
    private sealed class SyntheticWorld
    {
        private readonly List<OsmArea> _water = new(), _buildings = new();
        private readonly List<OsmLine> _rivers = new(), _bridges = new();
        public int? UnknownColumn { get; set; }

        public void AddBridge(string id, (double, double)[] pts, double widthM = OsmVendor.DefaultRoadWidthMeters)
        {
            var list = pts.Select(p => (Lat: p.Item1, Lon: p.Item2)).ToList();
            _bridges.Add(new OsmLine(id, "highway=tertiary bridge", widthM, list, GeoBox.Of(list)));
        }

        public void AddWater(string id, List<(double Lat, double Lon)> ring, int value)
            => _water.Add(new OsmArea(id, "natural=water", value, new[] { ring }, GeoBox.Of(ring)));

        public void AddBuilding(string id, List<(double Lat, double Lon)> ring)
            => _buildings.Add(new OsmArea(id, "building=yes", 0, new[] { ring }, GeoBox.Of(ring)));

        public void AddRiver(string id, (double, double)[] pts, double widthM)
        {
            var list = pts.Select(p => (Lat: p.Item1, Lon: p.Item2)).ToList();
            _rivers.Add(new OsmLine(id, "waterway=river", widthM, list, GeoBox.Of(list)));
        }

        public OsmTile Provider(OsmSet set, int x, int y)
        {
            if (UnknownColumn.HasValue && x == UnknownColumn.Value) return OsmTile.Unknown(set, x, y, "a hole in the synthetic world");
            return set == OsmSet.Water
                ? new OsmTile { Set = set, X = x, TmsY = y, Known = true, State = "synthetic", Water = _water }
                : new OsmTile { Set = set, X = x, TmsY = y, Known = true, State = "synthetic", Buildings = _buildings,
                                Rivers = _rivers.Select(r => Clip(r, x, y)).Where(r => r != null).ToList(),
                                Bridges = _bridges.Select(r => Clip(r, x, y)).Where(r => r != null).ToList() };
        }

        /// <summary>A river line CLIPPED to the tile, as a real vector tile carries it - so a tile
        /// next to an unreadable one cannot "know" the water inside it. (Areas are left whole: the
        /// synthetic ponds each sit well inside a known region.)</summary>
        private static OsmLine Clip(OsmLine r, int x, int y)
        {
            var nw = OsmTileMath.ToLatLon(14, x, y, 0, 0, 4096);
            var se = OsmTileMath.ToLatLon(14, x, y, 4096, 4096, 4096);
            var pts = new List<(double Lat, double Lon)>();
            for (int i = 0; i + 1 < r.Points.Count; i++)
            {
                var (p, q) = (r.Points[i], r.Points[i + 1]);
                double t0 = 0, t1 = 1;
                double dLat = q.Lat - p.Lat, dLon = q.Lon - p.Lon;
                bool Lb(double pk, double qk)              // Liang-Barsky: inside when pk*t <= qk
                {
                    if (pk == 0) return qk >= 0;
                    double t = qk / pk;
                    if (pk < 0) { if (t > t1) return false; if (t > t0) t0 = t; }
                    else { if (t < t0) return false; if (t < t1) t1 = t; }
                    return true;
                }
                if (!Lb(-dLon, p.Lon - nw.Lon) || !Lb(dLon, se.Lon - p.Lon)
                    || !Lb(-dLat, p.Lat - se.Lat) || !Lb(dLat, nw.Lat - p.Lat)) continue;
                var a = (p.Lat + dLat * t0, p.Lon + dLon * t0);
                var b = (p.Lat + dLat * t1, p.Lon + dLon * t1);
                if (pts.Count == 0 || pts[^1] != a) pts.Add(a);
                pts.Add(b);
            }
            return pts.Count >= 2 ? new OsmLine(r.Id, r.Kind, r.WidthM, pts, GeoBox.Of(pts)) : null;
        }
    }

    private static List<(double Lat, double Lon)> Disc((double Lat, double Lon) c, double radiusM, int n)
    {
        var f = OsmGeometry.Frame.At(c.Lat, c.Lon);
        var ring = new List<(double Lat, double Lon)>();
        for (int i = 0; i < n; i++)
        {
            double th = 2 * Math.PI * i / n;
            ring.Add(f.LatLon((radiusM * Math.Sin(th), radiusM * Math.Cos(th))));
        }
        ring.Add(ring[0]);
        return ring;
    }

    private static List<(double Lat, double Lon)> Square((double Lat, double Lon) c, double halfM)
    {
        var f = OsmGeometry.Frame.At(c.Lat, c.Lon);
        var r = new List<(double Lat, double Lon)>
        {
            f.LatLon((-halfM, -halfM)), f.LatLon((halfM, -halfM)), f.LatLon((halfM, halfM)), f.LatLon((-halfM, halfM)),
        };
        r.Add(r[0]);
        return r;
    }

    private static Dictionary<string, object> Props(long id, params string[] kv)
    {
        var d = new Dictionary<string, object> { ["@id"] = id, ["@type"] = "way" };
        for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
        return d;
    }

    private static List<(int X, int Y)> Ring(params int[] xy) => Pts(xy);

    private static List<(int X, int Y)> Pts(int[] xy)
    {
        var l = new List<(int X, int Y)>();
        for (int i = 0; i + 1 < xy.Length; i += 2) l.Add((xy[i], xy[i + 1]));
        return l;
    }

    /// <summary>lat/lon -> this tile's integer pixels, rounded half-to-even as leg_check's encoder.</summary>
    private static List<(int X, int Y)> LatLonRing((int X, int TmsY) key, IEnumerable<(double Lat, double Lon)> pts)
        => pts.Select(p =>
        {
            var (px, py) = OsmTileMath.ToPixel(14, key.X, key.TmsY, p.Lat, p.Lon, 4096);
            return ((int)Math.Round(px, MidpointRounding.ToEven), (int)Math.Round(py, MidpointRounding.ToEven));
        }).ToList();

    private static byte[] Gzip(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true)) gz.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static Func<string> NewIds()
    {
        int n = 0;
        return () => "00000000-0000-0000-0000-" + (++n).ToString("D12", CultureInfo.InvariantCulture);
    }

    private static int Occ(string hay, string needle)
    {
        int n = 0;
        for (int i = hay.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = hay.IndexOf(needle, i + 1, StringComparison.Ordinal)) n++;
        return n;
    }

    private static bool Throws(Action a)
    {
        try { a(); return false; }
        catch (FormatException) { return true; }
    }

    private static HttpResponseMessage Body(byte[] bytes)
        => new(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };

    private sealed class Stub : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _reply;
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public Stub(Func<HttpRequestMessage, HttpResponseMessage> reply) => _reply = reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(_reply(request));
        }
    }

    /// <summary>A zig-zag (sint64) MVT value, so the decoder's sint path is exercised.</summary>
    private readonly record struct Sint(long Value);

    /// <summary>
    /// A MINIMAL Mapbox Vector Tile WRITER for the self-test - leg_check.py's _mvt_tile, generalised to
    /// lines and to every value type: one layer; rings are written open with a ClosePath, lines open.
    /// </summary>
    private sealed class MvtWriter
    {
        private readonly string _name;
        private readonly List<string> _keys = new();
        private readonly List<object> _vals = new();
        private readonly List<byte[]> _features = new();

        public MvtWriter(string name) => _name = name;

        public MvtWriter Add(IDictionary<string, object> props, int geomType, IEnumerable<List<(int X, int Y)>> parts)
        {
            var tags = new List<byte>();
            foreach (var kv in props.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (!_keys.Contains(kv.Key)) _keys.Add(kv.Key);
                int vi = _vals.FindIndex(v => Equals(v, kv.Value));
                if (vi < 0) { _vals.Add(kv.Value); vi = _vals.Count - 1; }
                Varint(tags, (ulong)_keys.IndexOf(kv.Key));
                Varint(tags, (ulong)vi);
            }
            var geom = new List<byte>();
            int cx = 0, cy = 0;
            foreach (var part0 in parts)
            {
                var part = part0.ToList();
                bool polygon = geomType == Mvt.GeomPolygon;
                if (polygon && part.Count > 1 && part[0] == part[^1]) part.RemoveAt(part.Count - 1);
                for (int i = 0; i < part.Count; i++)
                {
                    if (i == 0) Varint(geom, (1 << 3) | 1);
                    else if (i == 1) Varint(geom, (ulong)(((part.Count - 1) << 3) | 2));
                    Varint(geom, Zz(part[i].X - cx));
                    Varint(geom, Zz(part[i].Y - cy));
                    cx = part[i].X;
                    cy = part[i].Y;
                }
                if (polygon) Varint(geom, (1 << 3) | 7);
            }
            var f = new List<byte>();
            Delimited(f, 2, tags.ToArray());
            Varint(f, 3 << 3);
            Varint(f, (ulong)geomType);
            Delimited(f, 4, geom.ToArray());
            _features.Add(f.ToArray());
            return this;
        }

        public byte[] Tile()
        {
            var layer = new List<byte>();
            Varint(layer, 15 << 3);
            Varint(layer, 2);
            Delimited(layer, 1, Encoding.UTF8.GetBytes(_name));
            foreach (var f in _features) Delimited(layer, 2, f);
            foreach (var k in _keys) Delimited(layer, 3, Encoding.UTF8.GetBytes(k));
            foreach (var v in _vals) Delimited(layer, 4, Value(v));
            Varint(layer, 5 << 3);
            Varint(layer, 4096);
            var tile = new List<byte>();
            Delimited(tile, 3, layer.ToArray());
            return tile.ToArray();
        }

        private static byte[] Value(object v)
        {
            var b = new List<byte>();
            switch (v)
            {
                case string s: Delimited(b, 1, Encoding.UTF8.GetBytes(s)); break;
                case float f: Varint(b, (2 << 3) | 5); b.AddRange(BitConverter.GetBytes(f)); break;
                case double d: Varint(b, (3 << 3) | 1); b.AddRange(BitConverter.GetBytes(d)); break;
                case long l: Varint(b, 4 << 3); Varint(b, unchecked((ulong)l)); break;
                case ulong u: Varint(b, 5 << 3); Varint(b, u); break;
                case Sint si: Varint(b, 6 << 3); Varint(b, unchecked((ulong)((si.Value << 1) ^ (si.Value >> 63)))); break;
                case bool t: Varint(b, 7 << 3); Varint(b, t ? 1UL : 0UL); break;
                default: throw new ArgumentException("unsupported MVT value " + v);
            }
            return b.ToArray();
        }

        private static ulong Zz(int n) => (ulong)(uint)((n << 1) ^ (n >> 31));

        private static void Varint(List<byte> b, ulong v)
        {
            while (v >= 0x80) { b.Add((byte)(v | 0x80)); v >>= 7; }
            b.Add((byte)v);
        }

        private static void Delimited(List<byte> b, int field, byte[] payload)
        {
            Varint(b, (ulong)((field << 3) | 2));
            Varint(b, (ulong)payload.Length);
            b.AddRange(payload);
        }
    }

    private static void Check(bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) _failures++;
    }

    private static string FindRepoRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "data", "COA-STP1_Order.xml"))) return d.FullName;
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "data", "COA-STP1_Order.xml"))) return d.FullName;
        return null;
    }
}
