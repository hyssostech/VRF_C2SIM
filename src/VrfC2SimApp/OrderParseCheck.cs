using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of OrderParser against a real C2SIM order file. Pure managed (no bridge,
/// no MAK, no VR-Forces): `VrfC2SimApp --parse-order &lt;file&gt;`. Prints a per-task summary
/// so the parse can be eyeballed against the golden-trace orders (e.g. 1_VRF_Move_Order:
/// one MOVE task T1_1_4_A, taskee 670cfe3a..., ROE ROETight, 2 inline points).
/// </summary>
public static class OrderParseCheck
{
    public static int Run(string path)
    {
        if (!File.Exists(path)) { Console.WriteLine($"file not found: {path}"); return 1; }
        var data = OrderParser.Parse(File.ReadAllText(path));

        Console.WriteLine($"=== OrderParser check: {Path.GetFileName(path)} ===");
        Console.WriteLine($"OrderID: {data.OrderId}");
        Console.WriteLine($"Tasks: {data.Tasks.Count}");
        foreach (var w in data.Warnings)
            Console.WriteLine($"WARN: {w}");

        int n = 0;
        foreach (var t in data.Tasks)
        {
            Console.WriteLine($"--- task[{n++}] '{t.TaskName}' uuid={Short(t.TaskUuid)}");
            Console.WriteLine($"    taskee(PerformingEntity): {t.TaskeeUuid}");
            Console.WriteLine($"    action: {t.ActionCode}   ROE: {Blank(t.RuleOfEngagementCode)}");
            Console.WriteLine($"    affectedEntity: {Blank(t.AffectedEntity)}");
            Console.WriteLine($"    mapGraphic: {(t.MapGraphicUuids.Count == 0 ? "(none -> embedded Location, STP-801)" : string.Join(", ", t.MapGraphicUuids))}");
            Console.WriteLine($"    points: {t.Points.Count}");
            foreach (var p in t.Points)
                Console.WriteLine($"      {p.Lat.ToString("R", CultureInfo.InvariantCulture)}," +
                                  $"{p.Lon.ToString("R", CultureInfo.InvariantCulture)}," +
                                  $"{(p.Elev.HasValue ? p.Elev.Value.ToString("R", CultureInfo.InvariantCulture) : "(none)")}");
            if (t.SimulationStartMs > 0 || t.RelativeDelayMs > 0 || t.StartAfterTaskUuid.Length > 0
                || t.AbsoluteStartUtc.HasValue)
                Console.WriteLine($"    timing: simStartMs={t.SimulationStartMs} relDelayMs={t.RelativeDelayMs} " +
                                  $"startAfter={Blank(t.StartAfterTaskUuid)}" +
                                  (t.AbsoluteStartUtc.HasValue
                                      ? $" absoluteStartUtc={t.AbsoluteStartUtc.Value:O}" : ""));
            // R4: the end time is dispatch + Duration, so the Duration is part of the parse.
            Console.WriteLine($"    duration: {(t.DurationMs > 0 ? t.DurationMs + " ms" : "(none)")}");
            // V4b: what those points MEAN under this verb, by the same rule the dispatch applies.
            var kind = TaskGeometryInterpretation.Classify(t.ActionCode, t.Points);
            Console.WriteLine($"    embedded shape: {kind}" + ShapeDetail(t)
                              + (t.MapGraphicUuids.Count > 0 ? "  (a MapGraphicID takes precedence at dispatch)" : ""));
        }

        // R4 SELF-TEST (a): the census the ruling is checked against - how many tasks carry a
        // Duration and a StartTime at all, and the histogram of the values. COA-STP1_Order.xml is
        // expected to print 42 / 42 with 32 x 4800000 + 10 x 7200000 and 41 x 0 + 1 x 12000000.
        // R1: which geometry path this order will take, task by task, before any run.
        int withGraphic = data.Tasks.Count(t => t.MapGraphicUuids.Count > 0);
        Console.WriteLine("=== R1 geometry census ===");
        Console.WriteLine($"tasks with a MapGraphicID: {withGraphic} of {data.Tasks.Count}");
        Console.WriteLine($"tasks on the embedded Location: " +
                          $"{data.Tasks.Count(t => t.MapGraphicUuids.Count == 0 && t.Points.Count > 0)} " +
                          $"of {data.Tasks.Count}" + (withGraphic == 0 ? " (STP-801: the export carries none)" : ""));
        Console.WriteLine($"tasks with NO geometry at all: " +
                          $"{data.Tasks.Count(t => t.MapGraphicUuids.Count == 0 && t.Points.Count == 0)} " +
                          $"of {data.Tasks.Count} (R2 executes these at the performing unit's position)");

        // V4b: the SHAPE census - what the embedded points mean, per verb, before any run. The
        // reading is the production classifier (TaskGeometryInterpretation), not a copy of it.
        Console.WriteLine("=== V4b shape census (embedded Location, per verb) ===");
        var kinds = data.Tasks.Select(t => (Verb: t.ActionCode, Kind: TaskGeometryInterpretation.Classify(t.ActionCode, t.Points))).ToList();
        foreach (var k in new[] { GeometryKind.None, GeometryKind.Point, GeometryKind.Route, GeometryKind.ObjectiveArea })
            Console.WriteLine($"{k,-14}: {kinds.Count(x => x.Kind == k)} of {data.Tasks.Count}");
        foreach (var g in kinds.GroupBy(x => x.Verb).OrderBy(g => g.Key, StringComparer.Ordinal))
            Console.WriteLine($"  {g.Key,-8} {string.Join(", ", g.GroupBy(x => x.Kind).OrderBy(x => x.Key).Select(x => $"{x.Count()} x {x.Key}"))}");
        Console.WriteLine($"objective areas to create on the fly: " +
                          $"{kinds.Count(x => x.Kind == GeometryKind.ObjectiveArea)} " +
                          "(one VR-Forces control area each, under the task's own uuid)");

        Console.WriteLine("=== R4 timing census ===");
        Console.WriteLine($"durations present: {data.Tasks.Count(t => t.DurationMs > 0)} of {data.Tasks.Count}");
        Console.WriteLine($"  histogram: {Histogram(data.Tasks.Select(t => t.DurationMs))}");
        Console.WriteLine($"start delays: {data.Tasks.Count} task(s), " +
                          $"{data.Tasks.Count(t => t.SimulationStartMs > 0 || t.RelativeDelayMs > 0 || t.AbsoluteStartUtc.HasValue)} " +
                          "of them non-zero");
        Console.WriteLine($"  histogram: {Histogram(data.Tasks.Select(t => t.SimulationStartMs))}");
        return 0;
    }

    /// <summary>"32 x 4800000 ms, 10 x 7200000 ms" - ascending by value, so two runs of the same
    /// order print the same string.</summary>
    private static string Histogram(IEnumerable<long> values)
    {
        var groups = values.GroupBy(v => v).OrderBy(g => g.Key)
                           .Select(g => $"{g.Count()} x {g.Key} ms").ToList();
        return groups.Count == 0 ? "(no tasks)" : string.Join(", ", groups);
    }

    /// <summary>The two numbers the V4b shape rule actually turns on, printed for any task that has
    /// enough points for them to mean anything: how far the figure reaches, and how much of that the
    /// last point gives back on its way to the first (1.00 = a straight run, 0.00 = a closed ring).</summary>
    private static string ShapeDetail(OrderTask t)
    {
        if (t.Points.Count < 2) return "";
        double span = TaskGeometryInterpretation.Spread(t.Points);
        // A closure ratio on a figure with no extent is arithmetic noise (the path length is zero),
        // so say what is actually true: every point is the same place.
        return span <= TaskGeometryInterpretation.PointCoincidenceMeters
             ? $"  (all {t.Points.Count} points within {span:F0} m - ONE place)"
             : $"  (span {span:F0} m, closure ratio {TaskGeometryInterpretation.ClosureRatio(t.Points):F2})";
    }

    private static string Short(string uuid) => uuid.Length > 8 ? uuid.Substring(0, 8) + "..." : uuid;
    private static string Blank(string s) => s.Length == 0 ? "(none)" : s;
}
