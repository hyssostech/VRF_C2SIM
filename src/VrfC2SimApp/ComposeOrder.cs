using System;
using System.Collections.Generic;
using System.Linq;

namespace VrfC2SimApp;

// N2 (docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md C7 / ORBAT_LOADING_REQUIREMENTS G2): the order in which
// children are attached to a composed parent fixes the leader (designator 1) and the echelon IDs
// (UG52 18.1.1 p438-439, 13.3.1 p365). The C2SIM init AUTHORS that order in UnitType.Subordinate[];
// the app's creation list is UUID-sorted (oracle parity), so the attach list is re-ordered here.
public static class ComposeOrder
{
    /// <summary>
    /// Order <paramref name="childNames"/> by the parent's declared subordinate uuids: declared
    /// children first, in declared order; children the parent did not declare follow in their
    /// original order. Names whose uuid cannot be resolved count as undeclared.
    /// </summary>
    public static List<string> ByDeclared(IReadOnlyList<string> declaredUuids, IReadOnlyList<string> childNames,
                                          Func<string, string> uuidOf)
    {
        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < declaredUuids.Count; i++)
            if (!string.IsNullOrEmpty(declaredUuids[i]) && !rank.ContainsKey(declaredUuids[i])) rank[declaredUuids[i]] = i;
        var declared = new List<(int Rank, string Name)>();
        var rest = new List<string>();
        foreach (var n in childNames)
        {
            var u = uuidOf(n) ?? "";
            if (u.Length > 0 && rank.TryGetValue(u, out var r)) declared.Add((r, n)); else rest.Add(n);
        }
        return declared.OrderBy(d => d.Rank).Select(d => d.Name).Concat(rest).ToList();
    }
}

// Offline check: `VrfC2SimApp --compose-selftest` (no bridge).
public static class ComposeOrderSelfTest
{
    public static int Run()
    {
        int failures = 0;
        void Check(string what, IEnumerable<string> got, params string[] want)
        {
            var g = string.Join(",", got); var w = string.Join(",", want);
            Console.WriteLine((g == w ? "[PASS] " : "[FAIL] ") + what + (g == w ? "" : $"  got [{g}] want [{w}]"));
            if (g != w) failures++;
        }
        var uuid = new Dictionary<string, string> { ["1141"] = "u-1141", ["1142"] = "u-1142", ["1143"] = "u-1143", ["1144"] = "u-1144" };
        string U(string n) => uuid.TryGetValue(n, out var v) ? v : "";

        // creation order is UUID-sorted (1143 < 1141 < 1142 in the R9 init); declared = 1141, 1142, 1143
        Check("declared order wins over creation order",
              ComposeOrder.ByDeclared(new[] { "u-1141", "u-1142", "u-1143" }, new[] { "1143", "1141", "1142" }, U),
              "1141", "1142", "1143");
        Check("undeclared children follow, in creation order",
              ComposeOrder.ByDeclared(new[] { "u-1142" }, new[] { "1143", "1141", "1142", "1144" }, U),
              "1142", "1143", "1141", "1144");
        Check("no declaration = creation order unchanged",
              ComposeOrder.ByDeclared(Array.Empty<string>(), new[] { "1143", "1141" }, U),
              "1143", "1141");
        Check("declared uuid with no created child is ignored",
              ComposeOrder.ByDeclared(new[] { "u-9999", "u-1141" }, new[] { "1143", "1141" }, U),
              "1141", "1143");
        Check("unresolvable name counts as undeclared",
              ComposeOrder.ByDeclared(new[] { "u-1141" }, new[] { "ghost", "1141" }, _ => ""),
              "ghost", "1141");
        Console.WriteLine(failures == 0 ? "compose-selftest: ALL CHECKS PASSED" : $"compose-selftest: {failures} FAILED");
        return failures == 0 ? 0 : 1;
    }
}
