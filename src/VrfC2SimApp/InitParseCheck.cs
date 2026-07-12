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

        return 0;
    }

    private static string TypeStr(VrfC2Sim.EntityTypeSpec t)
        => $"{t.Kind}.{t.Domain}.{t.Country}.{t.Category}.{t.Subcategory}.{t.Specific}.{t.Extra}";

    private static string Group(IEnumerable<string> vals) =>
        string.Join(", ", vals.GroupBy(v => v.Length == 0 ? "(none)" : v)
                               .OrderByDescending(g => g.Count())
                               .Select(g => $"{g.Key}={g.Count()}"));
}
