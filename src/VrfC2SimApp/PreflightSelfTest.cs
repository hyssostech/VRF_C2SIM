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

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
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
