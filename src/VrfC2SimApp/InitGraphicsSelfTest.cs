namespace VrfC2SimApp;

/// <summary>
/// Offline check that the init parser lifts the LINE and POINT tactical graphics, with their
/// C2SIM uuids, out of a real C2SIM Initialization (<c>--initgraphics-selftest [file]</c>).
/// Build item V3 of docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md. Pure managed:
/// no bridge, no MAK, no VR-Forces.
///
/// WHY IT EXISTS: until V3 the parser collected only S.TacticalAreaType (InitParser), so 41
/// Lines and 317 Points of COA-STP1 were parsed away. Every vendor tactical task that is not a
/// bare move takes exactly such an object as a parameter (a line of departure, a limit of
/// advance, a breach lane, a control point), so the geometry was the binding constraint, not
/// the verb table. Each number below is COUNTED FROM data\COA-STP1_Initialization.xml and would
/// have been 0 before the change.
///
/// The expectations are pinned to that file on purpose: it is the one order/init pair with the
/// full verb and geometry spread, it is in the repo, and a silent change to it (a re-export
/// from STP) is exactly the thing this test should catch. Pass a path to run it against another
/// init - then only the STRUCTURAL checks apply and the COA-STP1 counts are reported, not
/// asserted.
/// </summary>
public static class InitGraphicsSelfTest
{
    private static int _fail;

    // Counted from data\COA-STP1_Initialization.xml on 2026-09-14 (element census also recorded
    // in the assessment sec 1.5: 409 TacticalGraphic = 317 Point + 41 Line + 35 TacticalArea +
    // 16 TaskGraphic).
    private const string Fixture = "COA-STP1_Initialization.xml";
    private const int ExpectedAreas = 35;
    private const int ExpectedLines = 41;
    private const int ExpectedLineRoutes = 37;      // LineType.Item = RouteType
    private const int ExpectedLineBoundaries = 4;   // LineType.Item = BoundaryType
    private const int ExpectedPoints = 317;
    // Of the 41 lines, 10 carry a single vertex; those are NOT creatable as a VR-Forces route.
    private const int ExpectedCreatableLines = 31;

    public static int Run(string path = null)
    {
        string file = path ?? FindFixture();
        if (file == null || !File.Exists(file))
        {
            Console.WriteLine($"initgraphics-selftest: fixture not found (looked for data/{Fixture} " +
                              $"upwards from {AppContext.BaseDirectory} and from " +
                              $"{Directory.GetCurrentDirectory()}). Pass a path as the argument.");
            return 1;
        }

        bool isCoaStp1 = Path.GetFileName(file).Equals(Fixture, StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"=== init graphics self-test (V3): {Path.GetFileName(file)} ===");
        if (!isCoaStp1)
            Console.WriteLine("  NOT the COA-STP1 fixture: structural checks only, counts reported.");

        var data = InitParser.Parse(File.ReadAllText(file));

        // ---- counts (the regression lock on the fixture) ----
        Count("areas parsed", data.Areas.Count, ExpectedAreas, isCoaStp1);
        Count("lines parsed", data.Lines.Count, ExpectedLines, isCoaStp1);
        Count("points parsed", data.Points.Count, ExpectedPoints, isCoaStp1);
        Count("lines whose C2SIM Line wraps a Route", data.Lines.Count(l => l.Kind == "Route"),
              ExpectedLineRoutes, isCoaStp1);
        Count("lines whose C2SIM Line wraps a Boundary", data.Lines.Count(l => l.Kind == "Boundary"),
              ExpectedLineBoundaries, isCoaStp1);

        // ---- uuids: the whole point of V3 is that each graphic keeps its C2SIM uuid, because
        // that uuid becomes the VRF uuid and is how a task will name the object later. ----
        Check("every line carries a non-empty uuid",
              data.Lines.All(l => l.Uuid.Length > 0),
              $"{data.Lines.Count(l => l.Uuid.Length == 0)} without");
        Check("every point carries a non-empty uuid",
              data.Points.All(p => p.Uuid.Length > 0),
              $"{data.Points.Count(p => p.Uuid.Length == 0)} without");
        Check("every area carries a non-empty uuid",
              data.Areas.All(a => a.Uuid.Length > 0),
              $"{data.Areas.Count(a => a.Uuid.Length == 0)} without");

        var allUuids = data.Areas.Select(a => a.Uuid)
            .Concat(data.Lines.Select(l => l.Uuid))
            .Concat(data.Points.Select(p => p.Uuid)).ToList();
        Check("all graphic uuids are DISTINCT across areas + lines + points",
              allUuids.Count == allUuids.Distinct().Count(),
              $"{allUuids.Count} total, {allUuids.Distinct().Count()} distinct");
        var unitUuids = data.Units.Select(u => u.Uuid).ToHashSet();
        Check("no graphic uuid collides with a UNIT uuid",
              !allUuids.Any(unitUuids.Contains),
              $"{allUuids.Count(unitUuids.Contains)} colliding");

        // ---- names: createWaypoint/createRoute both document "the name must be unique (if
        // specified)" (vrfRemoteController.h:987, :1019). A duplicate is a live hazard. ----
        var allNames = data.Areas.Select(a => a.Name)
            .Concat(data.Lines.Select(l => l.Name))
            .Concat(data.Points.Select(p => p.Name)).ToList();
        Check("all graphic names are non-empty",
              allNames.All(n => n.Length > 0), $"{allNames.Count(n => n.Length == 0)} empty");
        Check("all graphic names are DISTINCT (createWaypoint/createRoute want unique names)",
              allNames.Count == allNames.Distinct().Count(),
              $"{allNames.Count} total, {allNames.Distinct().Count()} distinct");
        var unitNames = data.Units.Select(u => u.Name).ToHashSet();
        Check("no graphic name collides with a UNIT name (the name->uuid map is shared)",
              !allNames.Any(unitNames.Contains), $"{allNames.Count(unitNames.Contains)} colliding");

        // ---- geometry: every created graphic must carry real coordinates ----
        Check("every point has exactly ONE location (the C2SIM PointType shape)",
              data.Points.All(p => p.Points.Count == 1),
              $"{data.Points.Count(p => p.Points.Count != 1)} with a different count");
        Check("no point sits at 0,0 (an unparsed coordinate would)",
              data.Points.All(p => p.HasPosition &&
                                   (Math.Abs(p.Position.Lat) > 1e-9 || Math.Abs(p.Position.Lon) > 1e-9)),
              $"{data.Points.Count(p => !p.HasPosition || (Math.Abs(p.Position.Lat) <= 1e-9 && Math.Abs(p.Position.Lon) <= 1e-9))} at/near 0,0");
        Check("no line vertex sits at 0,0",
              data.Lines.All(l => l.Points.All(v => Math.Abs(v.Lat) > 1e-9 || Math.Abs(v.Lon) > 1e-9)),
              "");

        // ---- the creation plan DispatchInit would produce from this parse ----
        int creatableLines = data.Lines.Count(l => l.Points.Count >= 2);
        int degenerate = data.Lines.Count - creatableLines;
        Count("lines creatable as VR-Forces routes (>=2 vertices)", creatableLines,
              ExpectedCreatableLines, isCoaStp1);
        Console.WriteLine($"  plan: CreateControlArea x{data.Areas.Count}, CreateRoute x{creatableLines} " +
                          $"({degenerate} line(s) skipped for <2 vertices), " +
                          $"CreateWaypoint x{data.Points.Count(p => p.HasPosition)}");
        Console.WriteLine($"  line vertex histogram: " +
                          string.Join(", ", data.Lines.GroupBy(l => l.Points.Count).OrderBy(g => g.Key)
                                                .Select(g => $"{g.Key}pt x{g.Count()}")));
        Console.WriteLine("  NOTE (vendor, measured): createPhaseLine takes EXACTLY two points");
        Console.WriteLine("  (vrfRemoteController.h:1054-1072) and only ONE of these lines has two, so a");
        Console.WriteLine("  line is created as a ROUTE (:1023-1039). Whether the vendor's line-taking");
        Console.WriteLine("  scripted tasks accept a route object is a LIVE question, not decided here.");

        // ---- a parse that finds nothing must not be mistaken for a clean parse ----
        Check("parsing an empty document yields no graphics and does not throw",
              InitParser.Parse("") is { Lines.Count: 0, Points.Count: 0, Areas.Count: 0 }, "");

        Console.WriteLine(_fail == 0
            ? "initgraphics-selftest: ALL CHECKS PASSED"
            : $"initgraphics-selftest: FAILED ({_fail})");
        return _fail == 0 ? 0 : 1;
    }

    /// <summary>
    /// Find data/COA-STP1_Initialization.xml by walking up from the executable and from the
    /// current directory. The exe lives at src\VrfC2SimApp\bin\&lt;cfg&gt;\net10.0\win-x64\, so the
    /// repo root is five or six levels up depending on the configuration - walking beats
    /// hard-coding either depth.
    /// </summary>
    private static string FindFixture()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            for (int i = 0; dir != null && i < 10; i++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "data", Fixture);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private static void Count(string label, int actual, int expected, bool assert)
    {
        if (assert) Report($"{label} = {expected}", actual == expected, $"actual={actual}");
        else Console.WriteLine($"  [--] {label}: {actual} (not asserted - not the COA-STP1 fixture)");
    }

    private static void Check(string label, bool ok, string detail) => Report(label, ok, detail);

    private static void Report(string label, bool ok, string detail)
    {
        if (!ok) _fail++;
        Console.WriteLine($"  [{(ok ? "OK" : "FAIL")}] {label}{(detail.Length > 0 ? "  (" + detail + ")" : "")}");
    }
}
