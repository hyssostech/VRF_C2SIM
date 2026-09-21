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
        failures += CheckFetchFailureVsAbsence();
        failures += CheckWaterPolicy();

        // ---- 7: SF-G - the RESOLVER, pinned on the one fixture with MapGraphicIDs ---------
        failures += CheckResolverPin(repo);

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// SF-G (cold-start review of 1d0fb69, 2026-09-21): PIN TaskGeometryResolver AGAINST
    /// tools/preflight/leg_check.py ON THE ONE FIXTURE THAT EXERCISES IT.
    ///
    /// THE GAP. The Python tool is a RE-IMPLEMENTATION of this resolver and can drift from it.
    /// Everything above compares the two on COA-STP1, which carries ZERO MapGraphicIDs - the
    /// comparison asserts that itself - so it exercises none of the assembly rules: the
    /// role split (area/point = ONE destination, line = a path), the vertex-is-the-taskee drop,
    /// the nearest-end chaining with reversal, the 5 km chain gap, the one-destination-last rule
    /// and the doubles-back truncation. The STP Iron Storm export is the only shipped fixture
    /// with MapGraphicIDs (35 references across 23 tasks), and nothing compared the two
    /// implementations on it. "0 field differences" measured where the code cannot run is not
    /// evidence about the code.
    ///
    /// WHAT IS COMPARED: per task, in order - the geometry SOURCE (MapGraphic / embedded
    /// Location / none), the vertex COUNT, and every vertex's latitude and longitude. Pure
    /// geometry: no terrain, no tile, no network, no bridge.
    ///
    /// SF-F, THE HAVERSINE-VS-FLAT-EARTH GAP, IS SETTLED BY MEASUREMENT, NOT BY ARGUMENT. This
    /// resolver's DistMeters is equirectangular (111,320 m/deg, cos at the first argument's
    /// latitude); the tool's own dist_m is haversine on R = 6,371 km. They differ by ~0.1-0.3%
    /// here, and the assembly compares against two HARD edges (100 m, 5 km). The tool's
    /// --dump-resolved therefore runs the WHOLE assembly TWICE, once under each metric, refuses
    /// to write the reference if any task resolves differently, and records how close any real
    /// decision on this fixture came to an edge. MEASURED 2026-09-21, 310 threshold comparisons:
    /// 79.8 m of margin at the 100 m edge and 1,722.6 m at the 5,000 m edge, against metric
    /// differences of ~0.3 m and ~15 m at those magnitudes. Nothing on this fixture is close.
    /// The margins are re-measured on every regeneration and re-read here, so the day a new
    /// export does come close, the reference says so rather than the comment aging into a claim.
    /// </summary>
    private static int CheckResolverPin(string repo)
    {
        int failures = 0;
        Console.WriteLine();
        Console.WriteLine("--- SF-G: TaskGeometryResolver vs leg_check.py on the Iron Storm export ---");
        string referencePath = Path.Combine(repo, "tools", "preflight",
                                            "resolver-reference-ironstorm.json");
        if (!File.Exists(referencePath))
        {
            Console.WriteLine($"  FAIL: {referencePath} not found. Produce it with\n" +
                              "    python tools/preflight/leg_check.py \\\n" +
                              "      --order data/STP-IRON-STORM-SYNTHETIC_Order.xml \\\n" +
                              "      --init  data/STP-IRON-STORM-SYNTHETIC_Initialization.xml \\\n" +
                              "      --offline --no-starts \\\n" +
                              "      --dump-resolved tools/preflight/resolver-reference-ironstorm.json");
            return 1;
        }

        using var doc = JsonDocument.Parse(Sanitize(File.ReadAllText(referencePath)));
        var root = doc.RootElement;
        string orderName = Str(root, "order");
        string initName = Str(root, "init");
        string orderPath = Path.Combine(repo, "data", orderName);
        string initPath = Path.Combine(repo, "data", initName);
        Console.WriteLine($"  reference : {referencePath} (generated {Str(root, "generated")})");
        Console.WriteLine($"  fixture   : {orderName} + {initName}");
        if (!File.Exists(orderPath) || !File.Exists(initPath))
        {
            Console.WriteLine($"  FAIL: the reference names {orderName}/{initName} and they are not in data/");
            return 1;
        }

        // The two constants must be the SAME NUMBER on both sides, or the comparison below could
        // pass while the rules differ on any fixture but this one.
        Check(ref failures, Near(D(root, "origin_coincidence_m", double.NaN),
                                 TaskGeometryResolver.OriginCoincidenceMeters, 1e-9),
              $"the tool's ORIGIN_COINCIDENCE_M == OriginCoincidenceMeters " +
              $"({TaskGeometryResolver.OriginCoincidenceMeters:F0} m)");
        Check(ref failures, Near(D(root, "chain_gap_m", double.NaN),
                                 TaskGeometryResolver.ChainGapMeters, 1e-9),
              $"the tool's CHAIN_GAP_M == ChainGapMeters ({TaskGeometryResolver.ChainGapMeters:F0} m)");

        // SF-F: the metric agreement the reference had to satisfy before it could be written.
        if (root.TryGetProperty("metric_agreement", out var ma))
        {
            int probed = ma.TryGetProperty("comparisons_probed", out var cp) ? cp.GetInt32() : 0;
            Check(ref failures, probed > 0,
                  $"SF-F: the reference records {probed} threshold comparison(s) run under BOTH " +
                  "metrics (haversine and this resolver's flat earth), all agreeing");
            if (ma.TryGetProperty("closest_to_a_threshold_m", out var edges))
                foreach (var e in edges.EnumerateObject())
                {
                    double margin = D(e.Value, "margin_m", double.NaN);
                    Console.WriteLine($"    SF-F margin at the {e.Name} m edge: {margin:F1} m " +
                                      $"({Str(e.Value, "rule")})");
                    // A decision within 1 m of an edge is inside the two metrics' disagreement at
                    // that scale, and the pin would then be pinning a coin toss.
                    Check(ref failures, margin > 1.0,
                          $"SF-F: no decision on this fixture comes within 1 m of the {e.Name} m edge");
                }
        }
        else
        {
            Console.WriteLine("  FAIL: the reference carries no metric_agreement block - regenerate it");
            failures++;
        }

        // Build the registry the service builds: the ORDER's own graphics first, then the
        // INITIALIZATION's, which replace on a uuid collision ("the init is the shared world
        // every order is written against" - TaskGraphic.Registration). The tool does the same by
        // being passed the order first and the init second.
        var init = InitParser.Parse(File.ReadAllText(initPath));
        var order = OrderParser.Parse(File.ReadAllText(orderPath));
        var graphics = new Dictionary<string, TaskGraphic>(StringComparer.Ordinal);
        foreach (var g in order.Graphics)
            graphics[g.Uuid] = new TaskGraphic(g.Uuid, g.Name, g.Kind,
                g.Points.Select(p => (p.Lat, p.Lon, (double?)p.Elev)).ToList());
        foreach (var a in init.Areas)
            graphics[a.Uuid] = new TaskGraphic(a.Uuid, a.Name, TaskGraphic.KindArea,
                a.Points.Select(p => (p.Lat, p.Lon, (double?)p.Elev)).ToList());
        foreach (var l in init.Lines)
            graphics[l.Uuid] = new TaskGraphic(l.Uuid, l.Name,
                l.Points.Count == 1 ? TaskGraphic.KindPoint : TaskGraphic.KindLine,
                l.Points.Select(p => (p.Lat, p.Lon, (double?)p.Elev)).ToList());
        foreach (var p0 in init.Points)
            graphics[p0.Uuid] = new TaskGraphic(p0.Uuid, p0.Name, TaskGraphic.KindPoint,
                p0.Points.Select(p => (p.Lat, p.Lon, (double?)p.Elev)).ToList());
        foreach (var tg in init.TaskGraphics)
            graphics[tg.Uuid] = new TaskGraphic(tg.Uuid, tg.Name,
                tg.Points.Count == 1 ? TaskGraphic.KindPoint : TaskGraphic.KindLine,
                tg.Points.Select(p => (p.Lat, p.Lon, (double?)p.Elev)).ToList());
        var unitPos = new Dictionary<string, (double Lat, double Lon)>(StringComparer.Ordinal);
        foreach (var u in init.Units)
            if (double.TryParse(u.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double la)
                && double.TryParse(u.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double lo))
                unitPos[u.Uuid] = (la, lo);

        var refTasks = root.GetProperty("tasks").EnumerateArray().ToList();
        Console.WriteLine($"  inputs    : {init.Units.Count} unit(s), {order.Tasks.Count} task(s), " +
                          $"{graphics.Count} graphic(s) registered ({order.Graphics.Count} from the order)");
        Check(ref failures, refTasks.Count == order.Tasks.Count,
              $"task count {order.Tasks.Count} == reference {refTasks.Count}");

        int mapGraphicTasks = 0, verticesCompared = 0, sourceMismatch = 0, vertexMismatch = 0;
        double worstVertexDeltaDeg = 0;
        string worstWhere = "(none)";
        for (int i = 0; i < Math.Min(refTasks.Count, order.Tasks.Count); i++)
        {
            var r = refTasks[i];
            var task = order.Tasks[i];
            string name = Str(r, "task");
            if (!string.Equals(name, task.TaskName, StringComparison.Ordinal))
            {
                Console.WriteLine($"  FAIL: task[{i}] name '{task.TaskName}' != reference '{name}'");
                failures++;
                continue;
            }
            (double Lat, double Lon)? taskee = unitPos.TryGetValue(task.TaskeeUuid, out var up) ? up : null;
            var res = TaskGeometryResolver.Resolve(task, graphics, taskee);
            string mineSource = res.Source switch
            {
                GeometrySource.MapGraphic => "map_graphic",
                GeometrySource.EmbeddedLocation => "embedded_location",
                _ => "none",
            };
            string refSource = Str(r, "source");
            if (mineSource != refSource)
            {
                Console.WriteLine($"  FAIL: task[{i}] '{name}': source '{mineSource}' != reference '{refSource}'");
                sourceMismatch++; failures++;
            }
            if (refSource == "map_graphic") mapGraphicTasks++;
            var refPts = r.GetProperty("points").EnumerateArray()
                          .Select(p => (Lat: p[0].GetDouble(), Lon: p[1].GetDouble())).ToList();
            if (refPts.Count != res.Points.Count)
            {
                Console.WriteLine($"  FAIL: task[{i}] '{name}': {res.Points.Count} vertex(es), " +
                                  $"reference {refPts.Count}");
                vertexMismatch++; failures++;
                continue;
            }
            for (int v = 0; v < refPts.Count; v++)
            {
                double dLat = Math.Abs(refPts[v].Lat - res.Points[v].Lat);
                double dLon = Math.Abs(refPts[v].Lon - res.Points[v].Lon);
                double d = Math.Max(dLat, dLon);
                if (d > worstVertexDeltaDeg) { worstVertexDeltaDeg = d; worstWhere = $"task[{i}] vertex {v + 1}"; }
                if (d > 1e-9)
                {
                    Console.WriteLine($"  FAIL: task[{i}] '{name}' vertex {v + 1}: " +
                                      $"({res.Points[v].Lat:F9},{res.Points[v].Lon:F9}) != reference " +
                                      $"({refPts[v].Lat:F9},{refPts[v].Lon:F9})");
                    vertexMismatch++; failures++;
                }
                verticesCompared++;
            }
        }

        Console.WriteLine($"  compared  : {refTasks.Count} task(s), {verticesCompared} vertex(es); " +
                          $"worst vertex delta {worstVertexDeltaDeg:E2} deg at {worstWhere}");
        Check(ref failures, sourceMismatch == 0, "every task resolves from the SAME SOURCE in both tools");
        Check(ref failures, vertexMismatch == 0, "every resolved vertex agrees to 1e-9 deg (~0.1 mm)");
        // THE ARM THAT KEEPS THIS HONEST. Without it the whole section would pass on a fixture
        // whose tasks all fall back to their embedded Location - which is exactly how the COA-STP1
        // comparison came to be read as evidence about code it never ran.
        Check(ref failures, mapGraphicTasks > 0,
              $"{mapGraphicTasks} task(s) resolve THROUGH the MapGraphicID assembly - the rules " +
              "this pin exists for are actually exercised");
        Check(ref failures, verticesCompared > 0, $"{verticesCompared} vertex(es) were compared, not zero");
        return failures;
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
    /// <summary>
    /// F2 (cold-start review of f26d4ad): *** A FAILED FETCH IS NOT AN ABSENT TILE. ***
    ///
    /// The defect: a timeout, a connection failure and a 5xx were memoised exactly like a 404, and
    /// nothing was ever invalidated - so ONE dropped L13 fetch sent the level cascade to L12,
    /// `_levelByArea` remembered L12 for that ~2.4 km area for the process lifetime, and every
    /// later verdict over the Mojave calibration terrain was scored on a DEM the 0.92 threshold
    /// was never measured against. The only line that fired said "scored at ... COARSER than L13"
    /// and could not tell the server's answer from our own lost packet.
    ///
    /// A STUBBED HttpClient, not the network: every check here is a statement about the decision,
    /// and TileSource takes its HttpClient as a constructor argument for exactly this reason. Each
    /// check FAILS on the pre-F2 code. The mirror of this section is
    /// tools/preflight/leg_check.py --selftest, "F2: a failed fetch is NOT an absent tile".
    /// </summary>
    private static int CheckFetchFailureVsAbsence()
    {
        int failures = 0;
        Console.WriteLine();
        Console.WriteLine("--- F2: ABSENT vs FAILED (stubbed HttpClient, no network) ---");

        string tmp = Path.Combine(Path.GetTempPath(), "vrfc2sim-f2-" + Guid.NewGuid().ToString("N"));
        try
        {
            // A 404 is the SERVER'S ANSWER: absent, memoised, asked for once, and the cascade may
            // act on it.
            var s404 = new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            using (var t = new TileSource(tmp, http: new HttpClient(s404)))
            {
                int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                int firstCalls = s404.Calls;
                int lvl2 = t.ResolveLevel(34.66, -116.60, out bool failed2);
                Check(ref failures, lvl == 0 && !failed && lvl2 == 0 && !failed2
                                    && s404.Calls == firstCalls && t.ExhaustedTiles == 0,
                      $"a 404 at every level is ABSENCE, not failure, and the area answer is REMEMBERED " +
                      $"({firstCalls} request(s) for 3 levels, {s404.Calls} after a second probe)");
            }

            // A body too short to be a tile is how this TMS says "no tile" with a 200. The one
            // case the old code classified correctly.
            var sShort = new StubHandler(_ => Body(new byte[] { 1, 2, 3 }));
            using (var t = new TileSource(tmp, http: new HttpClient(sShort)))
            {
                int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                Check(ref failures, lvl == 0 && !failed,
                      "a 200 with a body too short to be a tile is ABSENCE too (this TMS's 'no tile')");
            }

            // A TIMEOUT is not an answer. It must not be memoised, it must STOP the cascade rather
            // than fall through to a coarser level, and it must be retried.
            var sTimeout = new StubHandler(_ => throw new TaskCanceledException("timeout"));
            using (var t = new TileSource(tmp, http: new HttpClient(sTimeout)))
            {
                int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                int after1 = sTimeout.Calls;
                int lvl2 = t.ResolveLevel(34.66, -116.60, out bool failed2);
                Check(ref failures, lvl == 0 && failed && after1 == 1,
                      "a TIMEOUT at the START level STOPS the cascade (level 0, fetchFailed) - it is NOT " +
                      $"rescored one level down ({after1} request, not 3)");
                Check(ref failures, lvl2 == 0 && failed2 && sTimeout.Calls > after1,
                      $"and it is NOT remembered as absence - the next probe RETRIES ({sTimeout.Calls} requests)");
                // Bounded: a wedged network costs MaxFetchAttempts requests per tile, not one per sample.
                for (int i = 0; i < 10; i++) t.ResolveLevel(34.66, -116.60, out _);
                Check(ref failures, sTimeout.Calls == TileSource.MaxFetchAttempts && t.ExhaustedTiles == 1,
                      $"retrying is BOUNDED at TileSource.MaxFetchAttempts={TileSource.MaxFetchAttempts} " +
                      $"({sTimeout.Calls} requests after 12 probes)");
            }

            // A 5xx is a server that did not answer the question - same class as a timeout.
            var s503 = new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
            using (var t = new TileSource(tmp, http: new HttpClient(s503)))
            {
                int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                Check(ref failures, lvl == 0 && failed,
                      "a 503 is FAILED too - the server did not answer the question");
            }

            // THE WHOLE POINT, as one check: L13 fails, L12 would have served - and the reader must
            // NOT hand back L12. A silently coarser score on calibration terrain is the defect.
            int call = 0;
            var sMixed = new StubHandler(_ =>
            {
                call++;
                if (call == 1) throw new HttpRequestException("connection reset");
                return Body(new byte[4000]);   // L12 would answer
            });
            using (var t = new TileSource(tmp, http: new HttpClient(sMixed)))
            {
                int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                Check(ref failures, lvl == 0 && failed,
                      "a FAILED start level is NEVER rescored at the next level down, even when that level " +
                      "would have served - the silent downgrade F2 names");
            }

            // THE LEG-LEVEL CONSEQUENCE, end to end: a route scored against a dead network gets
            // NO VERDICT with the failures COUNTED - not a ratio, and not a level. Before F2 the
            // same route came back with a level and a ratio off whatever tile answered next.
            var sDead = new StubHandler(_ => throw new HttpRequestException("no route to host"));
            using (var svc = new PreflightService(new PreflightOptions { CacheDir = tmp }, new HttpClient(sDead)))
            {
                var (legs, _) = svc.ScoreRoute(new[] { (34.66, -116.60), (34.665, -116.60) }, 0.94);
                var leg = legs.Count == 1 ? legs[0] : null;
                Check(ref failures, leg != null && leg.ElevationFetchFailures > 0 && leg.NoVerdict
                                    && leg.ElevationLevel == 0,
                      "a leg scored against a DEAD NETWORK is NO VERDICT with its fetch failures counted " +
                      $"({leg?.ElevationFetchFailures} of {leg?.Samples} samples) and NO level recorded - " +
                      "never a quiet ratio");
            }

            // ---- SF3 (cold-start review of 9d67f97): A NON-TILE BODY NEVER REACHES THE CACHE ----
            //
            // The hole F2 left open, reached through the BODY instead of the status: a success
            // carrying a captive-portal or proxy error page above minBytes used to be written to
            // disk as `149_13_x_y.tif` and then read back - by this reader AND by the python, on
            // every later process - as tile ABSENCE, silently downgrading that area's level for
            // good. Three things are asserted: the outcome is FAILED, the disk is untouched, and a
            // REAL tile still gets through.
            {
                string sf3 = Path.Combine(tmp, "sf3");
                Directory.CreateDirectory(sf3);
                var html = System.Text.Encoding.ASCII.GetBytes(
                    "<html><head><title>403 Forbidden</title></head><body>"
                    + new string('x', 4000) + "</body></html>");
                var sHtml = new StubHandler(_ => Body(html));
                using (var t = new TileSource(sf3, http: new HttpClient(sHtml)))
                {
                    int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                    int onDisk = Directory.GetFiles(sf3).Length;
                    Check(ref failures, lvl == 0 && failed && onDisk == 0 && t.UndecodableBodies > 0,
                          $"an HTML error page served with 200 is FAILED (not ABSENT), STOPS the cascade and is " +
                          $"NEVER written to the cache ({onDisk} file(s) on disk, {t.UndecodableBodies} " +
                          "undecodable bodies counted)");
                }
                // A TRUNCATED image - a body whose signature is right and whose content is not.
                // The scrub's cheap signature test cannot see this one; the fetch path's full
                // decode can, which is why the decision lives at the fetch.
                var truncated = new byte[3000];
                truncated[0] = 0x49; truncated[1] = 0x49; truncated[2] = 0x2A; truncated[3] = 0x00;
                var sTrunc = new StubHandler(_ => Body(truncated));
                string sf3b = Path.Combine(tmp, "sf3b");
                Directory.CreateDirectory(sf3b);
                using (var t = new TileSource(sf3b, http: new HttpClient(sTrunc)))
                {
                    int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                    Check(ref failures, lvl == 0 && failed && Directory.GetFiles(sf3b).Length == 0,
                          "a TRUNCATED tile (valid TIFF signature, unusable content) is FAILED too and is not cached");
                }
                // THE POISONED CACHE THAT ALREADY EXISTS. A file an OLDER build wrote must read as
                // FAILED - loud - not as ABSENT, which is how it used to vanish into a coarser level.
                string sf3c = Path.Combine(tmp, "sf3c");
                Directory.CreateDirectory(sf3c);
                var (px, py) = TileSource.TileIndexOf(TileMath.DefaultElevationLevel, 34.66, -116.60);
                string poisoned = Path.Combine(sf3c,
                    $"{TileMath.ElevationDataset}_{TileMath.DefaultElevationLevel}_{px}_{py}.tif");
                File.WriteAllBytes(poisoned, html);
                var sNever = new StubHandler(_ => throw new InvalidOperationException("must not be fetched"));
                using (var t = new TileSource(sf3c, http: new HttpClient(sNever)))
                {
                    int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                    Check(ref failures, lvl == 0 && failed,
                          "a POISONED CACHE FILE written by an older build reads as FAILED, not as tile ABSENCE - " +
                          "so it shouts instead of quietly downgrading the area's level");
                }
                // The SCRUB names it, and deletes nothing.
                var scrub = TileSource.ScrubCache(sf3c);
                Check(ref failures, scrub.UndecodableCount == 1 && scrub.Checked == 1 && File.Exists(poisoned),
                      $"the start-up cache SCRUB reports it ({scrub.UndecodableCount} of {scrub.Checked} checked) " +
                      "and DELETES NOTHING");
                Check(ref failures, TileSource.ScrubCache(Path.Combine(tmp, "does-not-exist")).Files == 0,
                      "the scrub on a missing cache directory is a no-op, not a throw");
            }

            // OFFLINE IS ABSENCE, DELIBERATELY - or the fixture comparison and every offline run
            // would become a route of no-verdict legs.
            using (var t = new TileSource(tmp, offline: true))
            {
                int lvl = t.ResolveLevel(34.66, -116.60, out bool failed);
                Check(ref failures, lvl == 0 && !failed,
                      "Vrf:PreflightOffline treats a missing tile as ABSENCE, not failure - a cache miss is " +
                      "not a network error, and the existing 'NO ELEVATION DATA AT ANY LEVEL' line covers it");
            }
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { /* temp */ }
        }
        return failures;
    }

    private static HttpResponseMessage Body(byte[] bytes)
        => new(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };

    /// <summary>A scripted HttpMessageHandler: counts requests and returns (or throws) whatever the
    /// check needs. No network, no ports, no timing.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _reply;
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) => _reply = reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken ct)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(_reply(request));
        }
    }

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

        // ===== E6 (D7 harvest): THE ONCE-PER-ORDER TILE CENSUS LATCH =====
        // TileSource has counted CacheHits and Fetched since it was written and nothing ever
        // printed them - "no fetch count, no resolved elevation level logged: STILL UNFIXED", D7
        // sec 7. The counters are cumulative, so the useful figure is the per-order DELTA, and
        // that needs a latch because the scoring workers are asynchronous and the last one of an
        // order finishes long after OnOrder has returned. The rule is pure; here it is, run.
        Console.WriteLine("--- E6: the once-per-order tile census latch ---");
        {
            var latch = new TileCensusLatch();
            long g = latch.BeginOrder();
            long a = latch.Enter(), b = latch.Enter(), c = latch.Enter();
            Check(ref failures, a == g && b == g && c == g,
                  "every worker of one order shares that order's generation");
            Check(ref failures, !latch.Leave(a) && !latch.Leave(b),
                  "the first two workers to finish report NOTHING - the order is not done");
            Check(ref failures, latch.Leave(c),
                  "the LAST worker of the order reports the census - exactly once, at the moment the " +
                  "order's scoring is actually finished");
            Check(ref failures, !latch.Leave(c),
                  "...and never twice, however many times it is called");
        }
        {
            var latch = new TileCensusLatch();
            long g1 = latch.BeginOrder();
            long w1 = latch.Enter();                 // a worker of order 1, still running
            long g2 = latch.BeginOrder();            // order 2 arrives before it finishes
            long w2 = latch.Enter();
            Check(ref failures, g2 != g1 && !latch.Leave(w1),
                  "a STRAGGLER from the previous order cannot report - its generation is closed, so it " +
                  "can neither re-trigger the old census nor be counted into the new order's");
            Check(ref failures, latch.Leave(w2),
                  "...and the new order's own last worker reports normally");
        }
        {
            var latch = new TileCensusLatch();
            Check(ref failures, latch.Enter() == 0 && !latch.Leave(0),
                  "a scoring worker with no order behind it (generation 0) can never trigger a census - " +
                  "the route shift runs before any order on no path, but the rule must not depend on that");
        }
        {
            // Two successive orders, each with its own work: one census each, never a shared one.
            var latch = new TileCensusLatch();
            int censuses = 0;
            for (int order = 0; order < 2; order++)
            {
                latch.BeginOrder();
                var workers = new List<long> { latch.Enter(), latch.Enter() };
                foreach (long w in workers) if (latch.Leave(w)) censuses++;
            }
            Check(ref failures, censuses == 2,
                  $"two successive orders with two scoring workers each produce exactly {censuses} " +
                  "census line(s) - one per BATCH, and each of those orders had exactly one batch");
        }
        // ===== N8 (D8 harvest): THE LATCH MUST RE-ARM WITHIN AN ORDER =====
        // R9's three tasks are CHAINED: task 2 is scored only after task 1 has finished. So the
        // order's first worker was also its last-outstanding worker, the old latch fired once and
        // stayed shut, and tasks 2 and 3 were scored with nothing counting their tile reads - while
        // the line called its figure "cumulative for the run" (D8 run 20260921T052350Z: one census
        // at :223, the other two preflights at :311 and :319). Both halves are asserted: the
        // re-arming, and the "never twice for one worker" property it could most easily break.
        Console.WriteLine("--- N8: the census re-arms per BATCH within one order (chained tasks) ---");
        {
            var latch = new TileCensusLatch();
            long g = latch.BeginOrder();
            var batches = new List<int>();
            for (int chained = 0; chained < 3; chained++)   // three chained tasks, one worker at a time
            {
                long w = latch.Enter();
                if (latch.Leave(w, out int b)) batches.Add(b);
            }
            Check(ref failures, batches.Count == 3 && batches[0] == 1 && batches[1] == 2 && batches[2] == 3,
                  $"three CHAINED tasks of ONE order produce 3 census line(s), numbered 1,2,3 " +
                  $"(got [{string.Join(",", batches)}]) - the old latch produced exactly 1 and the " +
                  "other two tasks' tile reads were never counted (N8)");
            Check(ref failures, g > 0 && !latch.Leave(g),
                  "...and a Leave with nothing outstanding still reports NOTHING - re-arming must not " +
                  "turn a duplicate Leave into an extra census");
        }
        {
            // Mixed shape: a 2-worker batch, then a 1-worker batch, in one order.
            var latch = new TileCensusLatch();
            long g = latch.BeginOrder();
            long a = latch.Enter(), b = latch.Enter();
            bool firstEarly = latch.Leave(a);
            bool firstDue = latch.Leave(b, out int b1);
            long c = latch.Enter();
            bool secondDue = latch.Leave(c, out int b2);
            Check(ref failures, !firstEarly && firstDue && b1 == 1 && secondDue && b2 == 2,
                  $"a 2-worker batch then a 1-worker batch in ONE order report as batch 1 and batch 2 " +
                  $"(got due={firstDue}/{secondDue}, batches {b1}/{b2}), and the first batch's early " +
                  "worker still reports nothing");
            Check(ref failures, latch.BeginOrder() > g,
                  "a new order opens a new generation, and its batch numbering starts again at 1");
        }
        // ===== SF-R2: THE RUN-TOTAL LINE REPORTS WHAT IT OBSERVED, NOT WHAT IT HOPED =====
        // It used to say "FINAL - every scoring worker has finished" and nothing established
        // that: it is written before _stopTick and the tick thread's Join, and the scoring
        // workers are unjoined Task.Run bodies. The line now reads the latch's outstanding count
        // and says it. Both branches are assertable because the sentence is a pure function of
        // the number - which is the whole point: a record must not claim what it did not see.
        Console.WriteLine("--- SF-R2: the shutdown tile total states the OBSERVED outstanding count ---");
        {
            var latch = new TileCensusLatch();
            Check(ref failures, latch.OutstandingWorkers == 0,
                  "a latch with no order behind it reports 0 workers outstanding");
            latch.BeginOrder();
            long w1 = latch.Enter(), w2 = latch.Enter();
            Check(ref failures, latch.OutstandingWorkers == 2,
                  $"two scoring workers in flight -> OutstandingWorkers == 2 (got {latch.OutstandingWorkers})");
            latch.Leave(w1);
            Check(ref failures, latch.OutstandingWorkers == 1,
                  $"one leaves -> 1 outstanding (got {latch.OutstandingWorkers})");
            string busy = TileCensusLatch.DescribeRunTotalScope(latch.OutstandingWorkers);
            Check(ref failures,
                  busy.Contains("1 scoring worker(s) were STILL OUTSTANDING", StringComparison.Ordinal)
                  && busy.Contains("not the run total", StringComparison.Ordinal),
                  $"with work still out the line says SO, and refuses the words 'run total' (got: {busy})");
            latch.Leave(w2);
            string quiet = TileCensusLatch.DescribeRunTotalScope(latch.OutstandingWorkers);
            Check(ref failures, latch.OutstandingWorkers == 0
                  && quiet.Contains("no scoring worker was outstanding", StringComparison.Ordinal)
                  && quiet.Contains("it is the run total", StringComparison.Ordinal),
                  $"and only when nothing is outstanding does it call itself the run total (got: {quiet})");
            Check(ref failures,
                  !TileCensusLatch.DescribeRunTotalScope(0).Contains("FINAL", StringComparison.Ordinal)
                  && !TileCensusLatch.DescribeRunTotalScope(3).Contains("FINAL", StringComparison.Ordinal),
                  "neither branch says FINAL - nothing in this service can establish that a Task.Run " +
                  "scoring worker will never run again, so the word is gone");
        }
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
