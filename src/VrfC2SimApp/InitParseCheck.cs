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

        return 0;
    }

    private static string Group(IEnumerable<string> vals) =>
        string.Join(", ", vals.GroupBy(v => v.Length == 0 ? "(none)" : v)
                               .OrderByDescending(g => g.Count())
                               .Select(g => $"{g.Key}={g.Count()}"));
}
