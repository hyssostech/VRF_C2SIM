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

        int n = 0;
        foreach (var t in data.Tasks)
        {
            Console.WriteLine($"--- task[{n++}] '{t.TaskName}' uuid={Short(t.TaskUuid)}");
            Console.WriteLine($"    taskee(PerformingEntity): {t.TaskeeUuid}");
            Console.WriteLine($"    action: {t.ActionCode}   ROE: {Blank(t.RuleOfEngagementCode)}");
            Console.WriteLine($"    affectedEntity: {Blank(t.AffectedEntity)}");
            Console.WriteLine($"    mapGraphic: {(t.MapGraphicUuid.Length == 0 ? "(none -> inline points)" : t.MapGraphicUuid)}");
            Console.WriteLine($"    points: {t.Points.Count}");
            foreach (var p in t.Points)
                Console.WriteLine($"      {p.Lat.ToString("R", CultureInfo.InvariantCulture)}," +
                                  $"{p.Lon.ToString("R", CultureInfo.InvariantCulture)}," +
                                  $"{(p.Elev.HasValue ? p.Elev.Value.ToString("R", CultureInfo.InvariantCulture) : "(none)")}");
            if (t.SimulationStartMs > 0 || t.RelativeDelayMs > 0 || t.StartAfterTaskUuid.Length > 0)
                Console.WriteLine($"    timing: simStartMs={t.SimulationStartMs} relDelayMs={t.RelativeDelayMs} " +
                                  $"startAfter={Blank(t.StartAfterTaskUuid)}");
        }
        return 0;
    }

    private static string Short(string uuid) => uuid.Length > 8 ? uuid.Substring(0, 8) + "..." : uuid;
    private static string Blank(string s) => s.Length == 0 ? "(none)" : s;
}
