using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using C2SIM;
using VrfC2SimApp.Preflight;
using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// FIXTURE COMPARISON of the ported pre-flight against the python tool it came from
/// (tools/preflight/leg_check.py): `VrfC2SimApp --preflight-selftest [reference.json [obs.xml]]`.
/// No bridge, no MAK runtime, no network - the tool's committed tile cache is read offline.
///
/// The reference is produced by the tool itself, e.g.
///   python tools/preflight/leg_check.py --typemap data/unit-type-map-52-nolifeform.json \
///       --text --json tools/preflight/preflight-reference.json \
///       --c2sim-observations tools/preflight/preflight-reference-observations.xml
/// and this then re-derives the SAME order with the C# component and asserts:
///   1. the ROUTE the two build is the same (start rule, origin-vertex drop, task chaining);
///   2. the unit's resolved vehicle limit is the same (type map -> template -> .entity tree);
///   3. every leg's sustained grade, ratio, soil and FLAG match (ratios to 1e-3);
///   4. each flagged leg's ObservationReport is byte-identical to the tool's once BOTH have
///      been round-tripped through the SDK's schema types.
///
/// The run's own PARAMETERS (step, windows, threshold, drop-origin, type map) are taken FROM
/// the reference file, so a tool run at other settings is still compared like for like and
/// cannot produce a false green.
/// </summary>
public static class PreflightSelfTest
{
    public static int Run(string referenceJson = null, string referenceObs = null)
    {
        int failures = 0;
        string repo = FindRepoRoot();
        if (repo == null)
        {
            Console.Error.WriteLine("PREFLIGHT SELFTEST: cannot locate the repo root (data/COA-STP1_Order.xml).");
            return 2;
        }
        referenceJson ??= Path.Combine(repo, "tools", "preflight", "preflight-reference.json");
        referenceObs ??= Path.Combine(repo, "tools", "preflight", "preflight-reference-observations.xml");
        if (!File.Exists(referenceJson))
        {
            Console.Error.WriteLine($"PREFLIGHT SELFTEST: reference {referenceJson} not found. Produce it with\n" +
                                    "  python tools/preflight/leg_check.py --typemap data/unit-type-map-52-nolifeform.json \\\n" +
                                    "      --text --json tools/preflight/preflight-reference.json \\\n" +
                                    "      --c2sim-observations tools/preflight/preflight-reference-observations.xml");
            return 2;
        }

        using var doc = JsonDocument.Parse(Sanitize(File.ReadAllText(referenceJson)));
        var root = doc.RootElement;
        var pars = root.GetProperty("parameters");
        double step = D(pars, "step_m", LegScorer.DefaultStepM);
        double window = D(pars, "sustained_window_m", LegScorer.DefaultWindowM);
        double shortW = D(pars, "short_window_m", LegScorer.DefaultShortWindowM);
        double threshold = D(pars, "threshold", LegScorer.DefaultThreshold);
        double dropOrigin = D(pars, "drop_origin_meters", 100.0);
        string typeMapName = pars.TryGetProperty("typemap", out var tm) ? tm.GetString() : "unit-type-map-52.json";

        Console.WriteLine("=== ROUTE PRE-FLIGHT: C# port vs tools/preflight/leg_check.py ===");
        Console.WriteLine($"reference : {referenceJson}");
        Console.WriteLine($"generated : {(root.TryGetProperty("generated", out var g) ? g.GetString() : "?")}");
        Console.WriteLine($"parameters: step {step:F0} m, window {window:F0} m, short {shortW:F0} m, " +
                          $"threshold {threshold:F2}, drop-origin {dropOrigin:F0} m, typemap {typeMapName}");

        string cache = Path.Combine(repo, "tools", "preflight", "preflight_cache");
        var opt = new PreflightOptions
        {
            CacheDir = cache,
            StepM = step,
            WindowM = window,
            ShortWindowM = shortW,
            Threshold = threshold,
            DropOriginMeters = dropOrigin,
            Offline = true,          // the fixture comparison NEVER touches the network
        };
        using var svc = new PreflightService(opt);
        Console.WriteLine($"tiles     : {cache} (offline)");
        foreach (var s in svc.Soil.Sources) Console.WriteLine($"vendor    : {s}");
        if (svc.Soil.Sources.Count == 0)
        {
            Console.WriteLine("  FAIL: no vendor soil sources readable - every factor would be a fallback");
            failures++;
        }
        if (!svc.Sms.Ok)
        {
            Console.WriteLine($"  FAIL: vendor SMS not found at {svc.Sms.Directory}");
            failures++;
        }

        var init = InitParser.Parse(File.ReadAllText(Path.Combine(repo, "data", "COA-STP1_Initialization.xml")));
        var order = OrderParser.Parse(File.ReadAllText(Path.Combine(repo, "data", "COA-STP1_Order.xml")));
        var typeMap = UnitTypeMap.Load(Path.Combine(repo, "data", typeMapName));
        var starts = PreflightService.LoadStarts(Path.Combine(repo, "tools", "preflight", "starts_P11.csv"));
        Console.WriteLine($"inputs    : {init.Units.Count} unit(s), {order.Tasks.Count} task(s), " +
                          $"{starts.Count} start position(s)");

        var mine = svc.RunOrder(init, order, typeMap, starts);

        // ---- 1/2/3: route, limit, and every leg -------------------------------------------
        var refTasks = root.GetProperty("tasks").EnumerateArray().ToList();
        if (refTasks.Count != mine.Count)
        {
            Console.WriteLine($"  FAIL: task count {mine.Count} != reference {refTasks.Count}");
            failures++;
        }

        int legsCompared = 0, flaggedMine = 0, flaggedRef = 0, noVerdict = 0, degenerate = 0;
        double worstRatioDelta = 0, worstSustainedDelta = 0;
        string worstWhere = "(none)";
        var rows = new List<string>();

        for (int t = 0; t < Math.Min(refTasks.Count, mine.Count); t++)
        {
            var r = refTasks[t];
            var m = mine[t];
            string name = r.GetProperty("task").GetString();
            if (name != m.TaskName)
            {
                Console.WriteLine($"  FAIL: task {t} name '{m.TaskName}' != reference '{name}'");
                failures++;
                continue;
            }
            string unit = r.GetProperty("unit").GetString();
            if (unit != m.UnitName)
            {
                Console.WriteLine($"  FAIL: {name}: unit '{m.UnitName}' != reference '{unit}'");
                failures++;
            }

            // Route: the start rule + the origin-vertex drop + the task chaining, vertex by vertex.
            if (r.TryGetProperty("route", out var rr))
            {
                var refRoute = rr.EnumerateArray()
                                 .Select(p => (Lat: p[0].GetDouble(), Lon: p[1].GetDouble())).ToList();
                if (refRoute.Count != m.Route.Count)
                {
                    Console.WriteLine($"  FAIL: {name}: route has {m.Route.Count} vertex(es), reference {refRoute.Count}");
                    failures++;
                }
                else
                {
                    for (int v = 0; v < refRoute.Count; v++)
                        if (Math.Abs(refRoute[v].Lat - m.Route[v].Lat) > 1e-9
                            || Math.Abs(refRoute[v].Lon - m.Route[v].Lon) > 1e-9)
                        {
                            Console.WriteLine($"  FAIL: {name}: route vertex {v} " +
                                              $"({m.Route[v].Lat:F6},{m.Route[v].Lon:F6}) != reference " +
                                              $"({refRoute[v].Lat:F6},{refRoute[v].Lon:F6})");
                            failures++;
                            break;
                        }
                }
            }
            if (r.TryGetProperty("limit_raw", out var lr) && lr.ValueKind == JsonValueKind.Number
                && Math.Abs(lr.GetDouble() - m.LimitRaw) > 1e-9)
            {
                Console.WriteLine($"  FAIL: {name} ({unit}): limit_raw {m.LimitRaw:F3} != reference {lr.GetDouble():F3} " +
                                  $"(template '{m.Template}' vs '{Str(r, "template")}')");
                failures++;
            }
            if (r.TryGetProperty("degenerate_legs", out var dl) && dl.GetInt32() != m.DegenerateLegs)
            {
                Console.WriteLine($"  FAIL: {name}: {m.DegenerateLegs} degenerate leg(s), reference {dl.GetInt32()}");
                failures++;
            }
            degenerate += m.DegenerateLegs;

            var refLegs = r.GetProperty("legs").EnumerateArray().ToList();
            if (refLegs.Count != m.Legs.Count)
            {
                Console.WriteLine($"  FAIL: {name}: {m.Legs.Count} leg(s), reference {refLegs.Count}");
                failures++;
                continue;
            }
            for (int i = 0; i < refLegs.Count; i++)
            {
                var rl = refLegs[i];
                var ml = m.Legs[i];
                legsCompared++;
                string where = $"{name} leg {ml.Index} ({unit})";

                double refSust = D(rl, "sustained", 0), refRatio = D(rl, "ratio", 0);
                double dS = Math.Abs(refSust - ml.Sustained);
                double dR = Math.Abs(refRatio - ml.Ratio);
                // NaN must never be compared with '>': every such test is FALSE and would
                // report a pass for two numbers that are not even numbers (see Near).
                if (!double.IsNaN(dS) && dS > worstSustainedDelta) worstSustainedDelta = dS;
                if (!double.IsNaN(dR) && dR > worstRatioDelta) { worstRatioDelta = dR; worstWhere = where; }

                if (!Near(refSust, ml.Sustained, 1e-3)) { Console.WriteLine($"  FAIL: {where}: sustained {ml.Sustained:F4} != reference {refSust:F4}"); failures++; }
                if (!Near(refRatio, ml.Ratio, 1e-3)) { Console.WriteLine($"  FAIL: {where}: ratio {ml.Ratio:F4} != reference {refRatio:F4}"); failures++; }
                if (!Near(D(rl, "limit", 0), ml.Limit, 1e-3)) { Console.WriteLine($"  FAIL: {where}: limit {ml.Limit:F4} != reference {D(rl, "limit", 0):F4}"); failures++; }
                if (!Near(D(rl, "length_m", 0), ml.LengthM, 0.5)) { Console.WriteLine($"  FAIL: {where}: length {ml.LengthM:F1} m != reference {D(rl, "length_m", 0):F1} m"); failures++; }

                string refSoil = Str(rl, "soil");
                if (refSoil != ml.Soil) { Console.WriteLine($"  FAIL: {where}: soil '{ml.Soil}' != reference '{refSoil}'"); failures++; }
                string refSource = Str(rl, "soil_source");
                if (refSource != ml.SoilSource) { Console.WriteLine($"  FAIL: {where}: soil source '{ml.SoilSource}' != reference '{refSource}'"); failures++; }
                if (!Near(D(rl, "factor", 0), ml.Factor, 1e-9)) { Console.WriteLine($"  FAIL: {where}: soil factor {ml.Factor:F2} != reference {D(rl, "factor", 0):F2}"); failures++; }

                bool refFlag = B(rl, "flagged");
                if (refFlag != ml.Flagged) { Console.WriteLine($"  FAIL: {where}: flagged={ml.Flagged} != reference {refFlag}"); failures++; }
                bool refNv = B(rl, "no_verdict");
                if (refNv != ml.NoVerdict) { Console.WriteLine($"  FAIL: {where}: no_verdict={ml.NoVerdict} != reference {refNv}"); failures++; }
                int refNan = rl.TryGetProperty("nan_samples", out var ns) ? ns.GetInt32() : 0;
                if (refNan != ml.NanSamples) { Console.WriteLine($"  FAIL: {where}: nan_samples {ml.NanSamples} != reference {refNan}"); failures++; }

                if (ml.Flagged) flaggedMine++;
                if (refFlag) flaggedRef++;
                if (ml.NoVerdict) noVerdict++;
                if (refFlag || ml.Flagged)
                    rows.Add($"  {where,-52} {ml.LengthM,8:F0} m  sust {ml.Sustained:F3}  " +
                             $"limit {ml.Limit:F3} ({ml.Soil})  ratio {ml.Ratio:F3}  " +
                             $"{(ml.Flagged ? "FLAGGED" : "passed")}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"--- flagged legs ({flaggedMine} of {legsCompared} checked; " +
                          $"{degenerate} skipped under {LegScorer.MinLegM:F0} m; {noVerdict} without a verdict) ---");
        foreach (var line in rows) Console.WriteLine(line);
        Console.WriteLine();
        Check(ref failures, flaggedMine == flaggedRef,
              $"flagged set size {flaggedMine} == reference {flaggedRef}");
        Console.WriteLine($"  worst ratio deviation {worstRatioDelta:E2} (tolerance 1e-3) at {worstWhere}");
        Console.WriteLine($"  worst sustained deviation {worstSustainedDelta:E2}");
        Console.WriteLine($"  tiles: {svc.Tiles.CacheHits} cache read(s), {svc.Tiles.Fetched} fetched");
        Check(ref failures, svc.Tiles.Fetched == 0, "no tile was fetched (the comparison ran offline)");

        // ---- 4: the ObservationReport bodies ----------------------------------------------
        if (File.Exists(referenceObs))
            failures += CompareObservations(referenceObs, mine, threshold);
        else
        {
            Console.WriteLine($"  FAIL: reference observations {referenceObs} not found - " +
                              "re-run the tool with --c2sim-observations");
            failures++;
        }

        // ---- 5: the EMISSION POLICY, independent of any tile ------------------------------
        failures += CheckEmissionPolicy(mine, threshold);

        // ---- 6: AO INDEPENDENCE - the elevation cascade and the water finding -------------
        failures += CheckElevationCascade(svc, mine);
        failures += CheckWaterPolicy();

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// What the order-receipt hook is allowed to send: one ReportBody per FLAGGED leg, each
    /// carrying the LocationObservation + NameObservation PAIR, and NOTHING for an order whose
    /// legs all pass or whose legs got no verdict. Checked on the real order and then on three
    /// synthetic tasks, so the policy is pinned without a tile, a bridge or a federation.
    /// </summary>
    private static int CheckEmissionPolicy(List<TaskPreflight> mine, double threshold)
    {
        int failures = 0;
        Console.WriteLine();
        Console.WriteLine("--- emission policy (PreflightReports.BuildForTask) ---");
        const string iso = "2026-09-14T00:00:00Z";
        int seq = 0;
        Func<string> ids = () => $"00000000-0000-0000-0000-{(++seq):D12}";

        // (a) the real order: N flagged legs -> exactly N reports, each with the pair.
        int flagged = mine.Sum(t => t.Legs.Count(l => l.Flagged));
        int emitted = 0, pairs = 0;
        foreach (var t in mine)
            foreach (var xml in PreflightReports.BuildForTask(t, threshold, iso, ids))
            {
                emitted++;
                int loc = Occurrences(xml, "<LocationObservation>");
                int nam = Occurrences(xml, "<NameObservation>");
                if (loc == 1 && nam == 1) pairs++;
                else Console.WriteLine($"  FAIL: a report carried {loc} LocationObservation(s) and {nam} NameObservation(s)");
            }
        Check(ref failures, emitted == flagged, $"the order's {flagged} flagged leg(s) yield {emitted} report(s)");
        Check(ref failures, pairs == emitted, $"all {emitted} report(s) carry exactly one Location + one Name observation");

        // (b) a task with no flagged leg emits NOTHING.
        var clean = new TaskPreflight
        {
            TaskName = "T_CLEAN", UnitName = "unit", UnitUuid = "u-1", Template = "Tank Platoon (USA)",
            Legs = new List<LegMetrics>
            {
                new() { Index = 1, Flagged = false, Ratio = 0.40, Soil = "hard-packed" },
                new() { Index = 2, Flagged = false, Ratio = 0.85, Soil = "sand" },
            },
        };
        Check(ref failures, PreflightReports.BuildForTask(clean, threshold, iso, ids).Count == 0,
              "a task whose legs all pass emits nothing");

        // (c) a task whose leg got NO VERDICT emits nothing either - missing tiles are not
        //     evidence of good ground, and they are not evidence of bad ground.
        var blind = new TaskPreflight
        {
            TaskName = "T_BLIND", UnitName = "unit", UnitUuid = "u-2", Template = "Tank Platoon (USA)",
            Legs = new List<LegMetrics> { new() { Index = 1, Flagged = false, NoVerdict = true, Ratio = 9.9 } },
        };
        Check(ref failures, PreflightReports.BuildForTask(blind, threshold, iso, ids).Count == 0,
              "a leg with NO VERDICT emits nothing, however bad its ratio looks");

        // (d) N flagged legs on one task -> N reports, in leg order.
        var three = new TaskPreflight
        {
            TaskName = "T_THREE", UnitName = "unit", UnitUuid = "u-3", Template = "Tank Platoon (USA)",
            Legs = new List<LegMetrics>
            {
                new() { Index = 1, Flagged = true, Ratio = 1.10, Soil = "sand", Limit = 0.752, LimitRaw = 0.94, Factor = 0.80, Sustained = 0.826, SustainedWindowM = 40 },
                new() { Index = 2, Flagged = false, Ratio = 0.50, Soil = "sand", Limit = 0.752, LimitRaw = 0.94, Factor = 0.80 },
                new() { Index = 3, Flagged = true, Ratio = 0.95, Soil = "sand", Limit = 0.752, LimitRaw = 0.94, Factor = 0.80, Sustained = 0.714, SustainedWindowM = 40 },
            },
        };
        var got = PreflightReports.BuildForTask(three, threshold, iso, ids);
        Check(ref failures, got.Count == 2, "2 of 3 legs flagged -> 2 reports");
        Check(ref failures, got.Count == 2 && got[0].Contains("leg 1", StringComparison.Ordinal)
                            && got[1].Contains("leg 3", StringComparison.Ordinal),
              "the reports name legs 1 and 3, in leg order");
        Check(ref failures, got.Count > 0 && got.All(x => x.Contains("PREDICTED IMPASSABLE (pre-flight estimate,", StringComparison.Ordinal)),
              "every report carries the PREDICTED IMPASSABLE (pre-flight estimate, ...) wording");
        return failures;
    }

    /// <summary>
    /// THE ELEVATION LEVEL IS A SETTING WITH A FALLBACK, and the Mojave AO must not notice.
    ///
    /// Offline on the committed cache, which holds 149_13 tiles and nothing else, so every
    /// assertion here is about behaviour and not about which tiles happen to be warm:
    ///   - the defaults are 13 and 11, and the options carry them into the tile source;
    ///   - every scored leg of the reference order resolved at L13 - the cascade never fired,
    ///     which is exactly why section 3 above compared byte-identical;
    ///   - a cascade told to start BELOW the cache's only level resolves NOTHING and says so
    ///     with level 0 and NaN, rather than fabricating a height from a parent tile;
    ///   - the posting arithmetic behind the calibration caveat is the measured one.
    /// </summary>
    private static int CheckElevationCascade(PreflightService svc, List<TaskPreflight> mine)
    {
        int failures = 0;
        Console.WriteLine();
        Console.WriteLine("--- elevation level + fallback (Vrf:PreflightElevationLevel) ---");
        Check(ref failures, TileMath.DefaultElevationLevel == 13 && TileMath.DefaultMinElevationLevel == 11,
              $"the shipped cascade is L{TileMath.DefaultElevationLevel} down to L{TileMath.DefaultMinElevationLevel}");
        Check(ref failures, svc.Tiles.ElevationLevel == TileMath.DefaultElevationLevel
                            && svc.Tiles.ElevationMinLevel == TileMath.DefaultMinElevationLevel,
              $"the options reach the tile source (L{svc.Tiles.ElevationLevel} -> L{svc.Tiles.ElevationMinLevel})");

        var levels = mine.SelectMany(t => t.Legs).Select(l => l.ElevationLevel).Distinct().OrderBy(x => x).ToList();
        Check(ref failures, levels.Count == 1 && levels[0] == TileMath.DefaultElevationLevel,
              $"every leg of the reference order resolved at L{TileMath.DefaultElevationLevel} - the fallback " +
              $"never fires on the Mojave AO (levels seen: {string.Join(",", levels)})");

        // The Mojave calibration point, at the level the calibration was made on and one below.
        var (ew13, ns13) = TileMath.PostingMeters(34.66, 13);
        var (ew12, ns12) = TileMath.PostingMeters(34.66, 12);
        Check(ref failures, Math.Abs(ew13 - 7.86) < 0.02 && Math.Abs(ns13 - 9.55) < 0.02,
              $"L13 posting at 34.66 N is 7.86 x 9.55 m (FINDING sec 7; got {ew13:F2} x {ns13:F2})");
        Check(ref failures, Math.Abs(ew12 / ew13 - 2.0) < 1e-9 && Math.Abs(ns12 / ns13 - 2.0) < 1e-9,
              "one level coarser doubles the posting in both axes - the smoothing is a factor of two");
        var (_, pns13) = TileMath.WindowPostings(34.66, 13, 40.0);
        var (_, pns12) = TileMath.WindowPostings(34.66, 12, 40.0);
        Check(ref failures, Math.Abs(pns13 - 4.19) < 0.02 && Math.Abs(pns12 - 2.09) < 0.02,
              $"the 40 m window spans {pns13:F2} postings N-S at L13 and {pns12:F2} at L12 - HALF the " +
              "evidence for the same verdict");
        Check(ref failures, TileMath.CalibrationNote(54.1, 12, 40.0, 0.92).Contains("MISSED", StringComparison.Ordinal)
                            && !TileMath.CalibrationNote(34.66, 13, 40.0, 0.92).Contains("MISSED", StringComparison.Ordinal),
              "the calibration note carries the one-sided-bias caveat at a coarser level, and not at L13");

        // A cascade that cannot reach the cache's only level must resolve NOTHING.
        using (var blind = new TileSource(svc.Tiles.CacheDirectory, offline: true, nearest: false,
                                          http: null, elevationLevel: 12, elevationMinLevel: 11))
        {
            double z = blind.Elevation(34.65607, -116.76144, out int used);
            Check(ref failures, used == 0 && double.IsNaN(z),
                  "with the cascade pinned below the cached level, the sample is level 0 / NaN - no height is " +
                  $"invented from a parent tile (got L{used} / {z})");
        }
        using (var deep = new TileSource(svc.Tiles.CacheDirectory, offline: true, nearest: false,
                                         http: null, elevationLevel: 14, elevationMinLevel: 11))
        {
            double z = deep.Elevation(34.65607, -116.76144, out int used);
            Check(ref failures, used == 13 && Math.Abs(z - 1585.6) < 0.5,
                  $"starting ABOVE the served level falls back to L13 and returns the calibration height " +
                  $"(got L{used} / {z:F2} m)");
        }
        // The clamp: a floor above the start must not silently disable the cascade.
        using (var odd = new TileSource(svc.Tiles.CacheDirectory, offline: true, nearest: false,
                                        http: null, elevationLevel: 12, elevationMinLevel: 15))
            Check(ref failures, odd.ElevationLevel == 12 && odd.ElevationMinLevel == 12,
                  $"a floor above the start level is clamped to it (L{odd.ElevationLevel} -> L{odd.ElevationMinLevel})");
        return failures;
    }

    /// <summary>
    /// WATER IS A FINDING, and it is reported even when the grade flag would not fire.
    /// Pure: synthetic legs, no tile, no bridge, no federation.
    /// </summary>
    private static int CheckWaterPolicy()
    {
        int failures = 0;
        Console.WriteLine();
        Console.WriteLine("--- water findings (PreflightReports.BuildForTask / BuildWaterFindings) ---");
        const string iso = "2026-09-20T00:00:00Z";
        int seq = 0;
        Func<string> ids = () => $"00000000-0000-0000-0000-{(++seq):D12}";

        Check(ref failures, LegScorer.IsWaterSoil("deep-water") && LegScorer.IsWaterSoil("shallow-water")
                            && !LegScorer.IsWaterSoil("sand") && !LegScorer.IsWaterSoil("hard-packed")
                            && !LegScorer.IsWaterSoil(null),
              "deep-water and shallow-water are water; sand, hard-packed and 'no soil' are not");

        // A leg the grade flag would NOT catch: water off the worst window, ratio well clear.
        var wetLeg = new LegMetrics
        {
            Index = 2, Flagged = false, Ratio = 0.30, Samples = 100, Soil = "hard-packed",
            WaterSamples = 7, WaterFraction = 0.07, WaterSoil = "deep-water",
            WaterSource = "CLCplus 10m", WaterDesc = "Water", WaterFirst = (54.12, 23.50),
            WaterFirstSM = 1234.0, ElevationLevel = 12,
        };
        var dryLeg = new LegMetrics { Index = 1, Flagged = false, Ratio = 0.20, Samples = 100, ElevationLevel = 12 };
        var task = new TaskPreflight
        {
            TaskName = "T_WET", UnitName = "278th ACR", UnitUuid = "u-w", Template = "Tank Platoon (USA)",
            Legs = new List<LegMetrics> { dryLeg, wetLeg },
        };
        var got = PreflightReports.BuildForTask(task, 0.92, iso, ids);
        Check(ref failures, got.Count == 1, $"an unflagged leg that crosses water still emits one report (got {got.Count})");
        Check(ref failures, got.Count == 1 && got[0].Contains("WATER ON THE LINE", StringComparison.Ordinal),
              "and that report is the WATER finding");
        Check(ref failures, got.Count == 1 && got[0].Contains("NOTHING was refused or altered", StringComparison.Ordinal),
              "the water finding says outright that nothing was refused or altered");
        Check(ref failures, got.Count == 1 && Occurrences(got[0], "<LocationObservation>") == 1
                            && Occurrences(got[0], "<NameObservation>") == 1,
              "it carries the same Location + Name pair as every other pre-flight finding");
        Check(ref failures, got.Count == 1 && !got[0].Contains("<AltitudeMSL>", StringComparison.Ordinal),
              "and NO AltitudeMSL - the height under a water class is not a claim worth making");

        // A leg that is BOTH flagged and wet emits the water sentence, not the grade sentence:
        // a water sample inside the window derates the limit to zero and the ratio to infinity.
        var bothLeg = wetLeg with { Index = 3, Flagged = true, Ratio = double.PositiveInfinity, Limit = 0.0 };
        var both = PreflightReports.BuildForTask(
            task with { TaskName = "T_BOTH", Legs = new List<LegMetrics> { bothLeg } }, 0.92, iso, ids);
        Check(ref failures, both.Count == 1 && both[0].Contains("WATER ON THE LINE", StringComparison.Ordinal)
                            && !both[0].Contains("PREDICTED IMPASSABLE", StringComparison.Ordinal),
              "a leg that is flagged AND wet emits the water sentence instead of the grade sentence - one " +
              "report per leg, and the true one");

        // The shift reader's separate emitter: per wet leg, and silent otherwise.
        var shiftSide = PreflightReports.BuildWaterFindings("u-w", "278th ACR", "T_WET",
                                                            new List<LegMetrics> { dryLeg, wetLeg, bothLeg },
                                                            iso, ids);
        Check(ref failures, shiftSide.Count == 2,
              $"BuildWaterFindings emits one per WET leg and nothing for a dry one (got {shiftSide.Count} of 3 legs)");
        Check(ref failures, PreflightReports.BuildWaterFindings("u", "unit", "T", new List<LegMetrics> { dryLeg },
                                                               iso, ids).Count == 0,
              "a route with no water is silent");

        // And the cold-cache guard's rule, the one proposal applied from the route-shift review.
        Check(ref failures, VrfC2SimService.RouteShiftCanScore(offline: false, cachedFiles: 0)
                            && VrfC2SimService.RouteShiftCanScore(offline: true, cachedFiles: 1)
                            && VrfC2SimService.RouteShiftCanScore(offline: false, cachedFiles: 900)
                            && !VrfC2SimService.RouteShiftCanScore(offline: true, cachedFiles: 0)
                            && VrfC2SimService.RouteShiftCanScore(offline: true, cachedFiles: -1),
              "the route shift is skipped ONLY when offline AND the cache is known empty - an empty cache " +
              "with fetching allowed still runs, and an unreadable cache is not evidence of emptiness");
        return failures;
    }

    private static int Occurrences(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal)) n++;
        return n;
    }

    /// <summary>
    /// Byte-compare each flagged leg's ReportBody against the tool's, with BOTH sides passed
    /// through the SDK's own deserialize+serialize so the comparison is of schema content and
    /// not of two emitters' whitespace. The volatile fields (ReportID, TimeOfObservation) are
    /// taken FROM the reference body, so a difference is never just a clock or a fresh guid.
    /// </summary>
    private static int CompareObservations(string path, List<TaskPreflight> mine, double threshold)
    {
        int failures = 0;
        Console.WriteLine("--- ObservationReport bodies (both sides round-tripped through the SDK types) ---");
        var refBodies = new List<XElement>();
        try
        {
            var xdoc = XDocument.Parse(File.ReadAllText(path));
            refBodies = xdoc.Root.Elements().Where(e => e.Name.LocalName == "ReportBody").ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine($"  FAIL: cannot parse {path}: {e.Message}");
            return 1;
        }

        var flagged = new List<(TaskPreflight Task, LegMetrics Leg)>();
        foreach (var t in mine)
            foreach (var lg in t.Legs)
                if (lg.Flagged) flagged.Add((t, lg));

        Check(ref failures, refBodies.Count == flagged.Count,
              $"{flagged.Count} report(s) built == {refBodies.Count} in the reference");

        int compared = 0, identical = 0;
        for (int i = 0; i < Math.Min(refBodies.Count, flagged.Count); i++)
        {
            var rb = refBodies[i];
            string ns = rb.Name.NamespaceName;
            string reportId = rb.Element(XName.Get("ReportID", ns))?.Value ?? "";
            string iso = rb.Descendants(XName.Get("IsoDateTime", ns)).FirstOrDefault()?.Value ?? "";

            string refXml;
            try { refXml = Canonical(rb.ToString()); }
            catch (Exception e) { Console.WriteLine($"  FAIL: reference report {i} would not round-trip: {e.Message}"); failures++; continue; }

            var (task, leg) = flagged[i];
            string mineXml;
            try
            {
                mineXml = Canonical(PreflightReports.BuildLegWarningReport(
                    task.UnitUuid, task.UnitName, task.TaskName, task.Template, leg, threshold, iso, reportId));
            }
            catch (Exception e) { Console.WriteLine($"  FAIL: built report {i} would not round-trip: {e.Message}"); failures++; continue; }

            compared++;
            if (string.Equals(refXml, mineXml, StringComparison.Ordinal)) identical++;
            else
            {
                failures++;
                Console.WriteLine($"  FAIL: report {i} ({task.TaskName} leg {leg.Index}) differs:");
                Console.WriteLine($"    {FirstDifference(refXml, mineXml)}");
            }
        }
        Console.WriteLine($"  {identical} of {compared} report(s) byte-identical after the round trip");
        if (identical > 0 && compared > 0)
        {
            var (task, leg) = flagged[0];
            Console.WriteLine("  sample Marking: " + PreflightReports.Marking(task.TaskName, task.Template, leg, threshold));
        }
        return failures;
    }

    /// <summary>Deserialize a bare ReportBody through the SDK and serialize it again.</summary>
    private static string Canonical(string reportBodyXml)
    {
        string wrapped = "<MessageBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">" +
                         "<DomainMessageBody>" + StripDecl(reportBodyXml) +
                         "</DomainMessageBody></MessageBody>";
        var body = C2SIMSDK.ToC2SIMObject<S.MessageBodyType>(wrapped);
        var rb = (body?.Item as S.DomainMessageBodyType)?.Item as S.ReportBodyType;
        if (rb == null) throw new InvalidOperationException("not a ReportBody");
        return C2SIMSDK.FromC2SIMObject(rb);
    }

    private static string StripDecl(string xml)
    {
        int i = xml.IndexOf("?>", StringComparison.Ordinal);
        return i >= 0 ? xml.Substring(i + 2).TrimStart() : xml;
    }

    private static string FirstDifference(string a, string b)
    {
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++)
            if (a[i] != b[i])
                return $"at {i}: reference ...{Excerpt(a, i)}... built ...{Excerpt(b, i)}...";
        return a.Length == b.Length ? "(identical)" : $"length {a.Length} vs {b.Length}";
    }

    private static string Excerpt(string s, int i)
        => s.Substring(Math.Max(0, i - 30), Math.Min(70, s.Length - Math.Max(0, i - 30))).Replace("\r", "").Replace("\n", " ");

    // ---- helpers ----------------------------------------------------------

    /// <summary>
    /// python's json.dump writes bare NaN/Infinity, which is not JSON. Turn them into null;
    /// the readers below map null back to NaN so a missing-tile leg still compares.
    /// </summary>
    private static string Sanitize(string json)
        => json.Replace(": NaN", ": null").Replace(":NaN", ":null")
               .Replace(": -Infinity", ": null").Replace(":-Infinity", ":null")
               .Replace(": Infinity", ": null").Replace(":Infinity", ":null");

    /// <summary>
    /// Equality with a tolerance that does NOT pass silently on NaN. Every "if (|a-b| &gt; tol)
    /// fail" test is FALSE when either side is NaN, which reports a pass for a leg whose
    /// elevation never resolved - the exact shape of a false green. Two NaNs agree; one does not.
    /// </summary>
    private static bool Near(double a, double b, double tol)
    {
        if (double.IsNaN(a) || double.IsNaN(b)) return double.IsNaN(a) && double.IsNaN(b);
        if (double.IsInfinity(a) || double.IsInfinity(b)) return a.Equals(b);
        return Math.Abs(a - b) <= tol;
    }

    private static double D(JsonElement e, string name, double fallback)
        => e.TryGetProperty(name, out var v)
         ? (v.ValueKind == JsonValueKind.Number ? v.GetDouble() : double.NaN)
         : fallback;

    private static string Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";

    private static bool B(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }

    /// <summary>Walk up from the executable until data/COA-STP1_Order.xml is in sight.</summary>
    private static string FindRepoRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "data", "COA-STP1_Order.xml")))
                return d.FullName;
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "data", "COA-STP1_Order.xml")))
                return d.FullName;
        return null;
    }
}
