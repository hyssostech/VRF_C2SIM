namespace VrfC2SimApp;

/// <summary>
/// Offline check of InitParser against a real C2SIM init file. Pure managed (no bridge,
/// no MAK, no VR-Forces): `VrfC2SimApp --parse-init &lt;file&gt; [clientId]`.
/// Prints a summary so the parse can be eyeballed against the golden trace (which
/// created 49 units + 4 areas from the STP init).
/// </summary>
public static class InitParseCheck
{
    public static int Run(string path, string clientId = "STP")
    {
        if (!File.Exists(path)) { Console.WriteLine($"file not found: {path}"); return 1; }
        var data = InitParser.Parse(File.ReadAllText(path));

        Console.WriteLine($"=== InitParser check: {Path.GetFileName(path)} ===");
        Console.WriteLine($"SystemName: {data.SystemName}");
        Console.WriteLine($"Units: {data.Units.Count}");
        Console.WriteLine($"  with location: {data.Units.Count(u => u.Latitude.Length > 0)}");
        Console.WriteLine($"  hostility: {Group(data.Units.Select(u => u.HostilityCode))}");
        Console.WriteLine($"  systemName: {Group(data.Units.Select(u => u.SystemName))}");

        // Units the interface WOULD create: our clientId + hostility + coords present.
        int wouldCreate = data.Units.Count(u =>
            u.Uuid.Length > 0 && u.SystemName == clientId &&
            u.HostilityCode.Length > 0 && u.Latitude.Length > 0 && u.Longitude.Length > 0);
        Console.WriteLine($"  would create (clientId={clientId}): {wouldCreate}  (golden trace: 49)");

        Console.WriteLine($"Areas: {data.Areas.Count}  (golden trace: 4)");
        foreach (var a in data.Areas)
            Console.WriteLine($"  area '{a.Name}' pts={a.Points.Count}");

        // ---- V3: the LINE and POINT graphics, and what would be created from them -------------
        // The interface used to parse these away entirely (only S.TacticalAreaType was collected),
        // so this section is the first place the objective/phase-line/control-point geometry STP
        // ships is visible without opening the XML. COA-STP1: 35 areas + 41 lines + 317 points.
        Console.WriteLine($"Lines: {data.Lines.Count}  " +
                          $"(Route={data.Lines.Count(l => l.Kind == "Route")}, " +
                          $"Boundary={data.Lines.Count(l => l.Kind == "Boundary")}; " +
                          $"with uuid={data.Lines.Count(l => l.Uuid.Length > 0)})");
        Console.WriteLine($"  vertex histogram: {Histogram(data.Lines.Select(l => l.Points.Count))}");
        int creatableLines = data.Lines.Count(l => l.Points.Count >= 2);
        Console.WriteLine($"  creatable as VRF routes (>=2 vertices): {creatableLines}" +
                          $"  (skipped, <2 vertices: {data.Lines.Count - creatableLines})");
        foreach (var l in data.Lines.Take(5))
            Console.WriteLine($"    line '{l.Name}' [{l.Kind}] pts={l.Points.Count} uuid={l.Uuid}");

        Console.WriteLine($"Points: {data.Points.Count}  (with uuid={data.Points.Count(p => p.Uuid.Length > 0)}, " +
                          $"with position={data.Points.Count(p => p.HasPosition)})");
        Console.WriteLine($"  location-count histogram: {Histogram(data.Points.Select(p => p.Points.Count))}");
        foreach (var p in data.Points.Take(5))
            Console.WriteLine($"    point '{p.Name}' uuid={p.Uuid} " +
                              (p.HasPosition ? $"{p.Position.Lat},{p.Position.Lon}" : "(NO POSITION)"));

        // The GRAPHICS CREATION PLAN - what DispatchInit would queue, per flag. Printed next to
        // the unit placement plan below so one command shows everything an init would create.
        Console.WriteLine("Graphics creation plan (what DispatchInit would queue):");
        Console.WriteLine($"  areas  -> CreateControlArea x{data.Areas.Count}  (always; uuid = the C2SIM uuid)");
        Console.WriteLine($"  lines  -> CreateRoute       x{creatableLines}   (only when Vrf:CreateInitLines=true; default false)");
        Console.WriteLine($"  points -> CreateWaypoint    x{data.Points.Count(p => p.HasPosition)}   (only when Vrf:CreateInitPoints=true; default false)");
        // Uniqueness matters: createWaypoint/createRoute document "the name must be unique (if
        // specified)" (vrfRemoteController.h:987, :1019). Say so here rather than find out live.
        var allGraphicNames = data.Areas.Select(a => a.Name)
            .Concat(data.Lines.Select(l => l.Name))
            .Concat(data.Points.Select(p => p.Name)).ToList();
        var nameDups = allGraphicNames.GroupBy(n => n).Where(g => g.Count() > 1).ToList();
        var unitNames = data.Units.Select(u => u.Name).ToHashSet();
        int nameVsUnit = allGraphicNames.Count(unitNames.Contains);
        Console.WriteLine($"  graphic names: {allGraphicNames.Count} total, {allGraphicNames.Distinct().Count()} distinct, " +
                          $"{nameDups.Count} duplicated, {nameVsUnit} colliding with a UNIT name" +
                          (nameDups.Count > 0 ? "  <-- createWaypoint/createRoute want unique names" : ""));

        Console.WriteLine("First 6 creatable units (name | host | sidc | dis | lat,lon):");
        foreach (var u in data.Units.Where(u =>
                     u.SystemName == clientId && u.Latitude.Length > 0).Take(6))
            Console.WriteLine($"  {u.Name,-14} {u.HostilityCode,-6} {u.SymbolId,-15} {u.DisEntityType,-18} {u.Latitude},{u.Longitude}");

        // What the app would CREATE, grouped by the PLANNED VR-Forces DIS type (via
        // UnitTranslator, i.e. the SIDC dispatch - NOT the init's DisEntityType). This is
        // the E1 view: per-type formation names key on the CREATED aggregate type
        // (NEXT_SESSION_GUIDANCE sec 4 E1), so experiments need units per created type.
        var plans = data.Units
            .Where(u => u.Uuid.Length > 0 && u.SystemName == clientId &&
                        u.HostilityCode.Length > 0 && u.Latitude.Length > 0 && u.Longitude.Length > 0)
            .Select(u => (Unit: u, Plan: UnitTranslator.Plan(
                string.IsNullOrEmpty(u.ElevationAgl) ? u with { ElevationAgl = "1000.0" } : u)))
            .ToList();
        Console.WriteLine("Planned creations by created type (up to 3 examples each - name uuid lat,lon):");
        foreach (var g in plans.GroupBy(p => (p.Plan.IsAggregate, Type: TypeStr(p.Plan.Type)))
                               .OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"  {(g.Key.IsAggregate ? "AGG" : "ENT")} {g.Key.Type} x{g.Count()}");
            foreach (var p in g.Take(3))
                Console.WriteLine($"      {p.Unit.Name,-16} {p.Unit.Uuid} {p.Unit.Latitude},{p.Unit.Longitude}");
        }

        // R8 view (docs/UNIT_MOVEMENT_RESEARCH.md sec 4): stacked-coordinate groups among
        // the creatable units - identical spawn coordinates are the COA-STP1 pathology
        // that blocks aggregate marching. Same grouping key as the runtime DeStacker.
        var stacks = plans
            .GroupBy(p => DeStacker.CoordKey(p.Plan.Pos.LatDeg, p.Plan.Pos.LonDeg))
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Count())
            .ToList();
        Console.WriteLine($"Stacked-coordinate groups (2+ creatable units at identical lat/lon): {stacks.Count}" +
                          (stacks.Count > 0 ? $"  ({stacks.Sum(g => g.Count())} units affected; Vrf:DeStackCreates would spread them)" : ""));
        foreach (var g in stacks.Take(5))
            Console.WriteLine($"  {g.Count()} units at {g.Key.Lat},{g.Key.Lon}: " +
                              string.Join(", ", g.Take(4).Select(p => p.Unit.Name)) + (g.Count() > 4 ? ", ..." : ""));

        return 0;
    }

    private static string TypeStr(VrfC2Sim.EntityTypeSpec t)
        => $"{t.Kind}.{t.Domain}.{t.Country}.{t.Category}.{t.Subcategory}.{t.Specific}.{t.Extra}";

    private static string Histogram(IEnumerable<int> counts) =>
        string.Join(", ", counts.GroupBy(c => c).OrderBy(g => g.Key).Select(g => $"{g.Key}pt x{g.Count()}"));

    private static string Group(IEnumerable<string> vals) =>
        string.Join(", ", vals.GroupBy(v => v.Length == 0 ? "(none)" : v)
                               .OrderByDescending(g => g.Count())
                               .Select(g => $"{g.Key}={g.Count()}"));
}
