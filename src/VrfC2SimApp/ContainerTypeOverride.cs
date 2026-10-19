using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// STP-1296 (D114(a); docs/experiments/PREREG_STP1296_GROUP_TASK_2026-10-19.md sec 1c): Vrf:ContainerTypeOverride, a
/// RUN-ONLY switch, OFF by default, that PINS the init container type of one type-map row ahead of the init rule.
/// <para>
/// WHY. Move (Group) needs ground-echelon-group-tactics-enabler, which the containers the init rule picks for an
/// infantry brigade (BDE Light Infantry / BN_Light Infantry) do not carry. "BDE Airborne PA.entity" does (:13), on
/// PseudoAggregate.ope (:3), type 11:1:-1:8:4:1:111 (stp-data 3f6c01c runs/stp1296/G0a-followup.md). The init rule
/// (<see cref="ContainerTypeRule.Choose"/>) lands BDE Light Infantry at branch 3, specific/extra (1,1), BEFORE any
/// branch-4 candidate, so a pin cannot be added to its candidate list: it must be decided ahead of the loop.
/// </para>
/// <para>
/// FORMAT: "&lt;map row id&gt;=&lt;category&gt;:&lt;subcategory&gt;:&lt;specific&gt;:&lt;extra&gt;", several separated
/// by ';' - e.g. "F-UCI-H=8:4:1:111". The nation stays the unit's own (the vendor matchType's -1 country is a wildcard,
/// ObjectTypeResolver.cs:17, :51). Empty = OFF: <see cref="ChooseFor"/> then returns exactly what
/// <see cref="ContainerTypeRule.Choose"/> returns. A malformed value is a start refusal, never a silent OFF. A pinned
/// type that does not resolve to a CONTAINER is refused for that unit, loudly - never created as something else.
/// </para>
/// </summary>
public static class ContainerTypeOverride
{
    public const string Ref = "STP-1296";

    /// <summary>Parses the setting. Empty or whitespace = OFF (an empty map, no error).</summary>
    public static bool TryParse(string text, out IReadOnlyDictionary<string, int[]> pins, out string error)
    {
        var map = new Dictionary<string, int[]>(StringComparer.Ordinal);
        pins = map;
        error = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        foreach (string raw in text.Split(';'))
        {
            string item = raw.Trim();
            if (item.Length == 0) continue;
            int eq = item.IndexOf('=');
            string key = eq > 0 ? item.Substring(0, eq).Trim() : "";
            string[] f = eq > 0 ? item.Substring(eq + 1).Split(':') : Array.Empty<string>();
            var v = new int[4];
            bool ok = key.Length > 0 && f.Length == 4;
            for (int i = 0; ok && i < 4; i++)
                ok = int.TryParse(f[i].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v[i]);
            if (!ok)
            {
                error = $"Vrf:ContainerTypeOverride item '{item}' is not <map row id>=<category>:<subcategory>:<specific>:<extra> ({Ref})";
                pins = new Dictionary<string, int[]>(StringComparer.Ordinal);
                return false;
            }
            if (!map.TryAdd(key, v))
            {
                error = $"Vrf:ContainerTypeOverride names map row '{key}' twice ({Ref})";
                pins = new Dictionary<string, int[]>(StringComparer.Ordinal);
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// The init container decision with the pins applied: a unit whose map row is pinned gets the pinned type (if it
    /// resolves to a CONTAINER), every other unit - and every unit when the map is empty - gets
    /// <see cref="ContainerTypeRule.Choose"/>'s answer, the same object it returns.
    /// </summary>
    public static ContainerChoice ChooseFor(ICatalogue catalogue, int nationCode, string sidc, string echelonCode,
                                            string mapRowId, IReadOnlyDictionary<string, int[]> pins)
    {
        if (pins == null || pins.Count == 0 || string.IsNullOrEmpty(mapRowId) || !pins.TryGetValue(mapRowId, out var pin))
            return ContainerTypeRule.Choose(catalogue, nationCode, sidc, echelonCode);
        var t8 = new[] { 3, 11, 1, nationCode, pin[0], pin[1], pin[2], pin[3] };
        string fid = UnitTypeMap.FunctionIdOf(sidc ?? "");
        string typeText = string.Join(":", t8.Skip(1));
        var flags = new List<string>
        {
            $"OVERRIDE (Vrf:ContainerTypeOverride, {Ref}, run-only): map row {mapRowId} pinned to {typeText}, ahead of the init rule",
        };
        var hit = catalogue?.Resolve(t8);
        if (hit == null || hit.Role != CatalogueRole.Container)
        {
            flags.Add($"NO CONTAINER at the pinned type {typeText} ({(hit == null ? "nothing resolves" : "it resolves to a " + hit.Role)})");
            return new ContainerChoice(false, null, "", false, pin[0], -1, fid, flags);
        }
        return new ContainerChoice(true, t8, hit.DisplayName, true, pin[0], pin[1], fid, flags);
    }

    /// <summary>
    /// --populate-selftest section (p18): offline, on a synthetic catalogue - parsing, OFF byte-identity, the pin, the
    /// refusals, and the DIRTY CONTROL that the init rule's candidate loop cannot reach the pinned type.
    /// </summary>
    public static void SelfTest(Action<bool, string, string> check)
    {
        Console.WriteLine($"--- (p18) {Ref}: Vrf:ContainerTypeOverride - OFF is the init rule, ON pins one map row ---");
        var cat = new PinCatalogue();
        cat.Add(new[] { 3, 11, 1, 225, 8, 3, 1, 1 }, "BDE", CatalogueRole.Container);
        cat.Add(new[] { 3, 11, 1, 225, 8, 4, 1, 0 }, "BDE, Mech Infantry", CatalogueRole.Container);
        cat.Add(new[] { 3, 11, 1, 225, 8, 4, 1, 111 }, "BDE Airborne PA", CatalogueRole.Container);
        cat.Add(new[] { 3, 11, 1, 225, 6, 3, 1, 0 }, "BN, Light Infantry", CatalogueRole.Container);
        cat.Add(new[] { 3, 11, 1, 225, 8, 2, 1, 0 }, "BDE, Armor", CatalogueRole.Container);
        cat.Add(new[] { 3, 11, 1, 225, 5, 4, 0, 0 }, "Mech CO (USA, M2)", CatalogueRole.Unit);

        bool okEmpty = TryParse("", out var off, out string e0);
        check(okEmpty && off.Count == 0 && e0 == null, "an EMPTY setting is OFF: no pins, no error", e0 ?? "");
        bool okWs = TryParse("  ", out var off2, out _);
        check(okWs && off2.Count == 0, "a whitespace-only setting is OFF too", "");
        bool okOne = TryParse("F-UCI-H=8:4:1:111", out var one, out string e1);
        check(okOne && one.Count == 1 && one.TryGetValue("F-UCI-H", out var p) && p.SequenceEqual(new[] { 8, 4, 1, 111 }),
              "\"F-UCI-H=8:4:1:111\" parses to ONE pin, category 8 subcategory 4 specific 1 extra 111", e1 ?? "");
        bool okTwo = TryParse(" F-UCI-H = 8:4:1:111 ; F-UCA-H=8:2:1:0; ", out var two, out string e2);
        check(okTwo && two.Count == 2, "two pins with spaces and a trailing ';' parse", e2 ?? "");
        foreach (string bad in new[] { "F-UCI-H=8:4:1", "=8:4:1:111", "F-UCI-H:8:4:1:111", "F-UCI-H=8:x:1:111",
                                       "F-UCI-H=8:4:1:111;F-UCI-H=8:3:1:1" })
        {
            bool okBad = TryParse(bad, out var none, out string eb);
            check(!okBad && none.Count == 0 && !string.IsNullOrEmpty(eb),
                  $"a malformed setting '{bad}' is REFUSED with a reason (a start refusal, never a silent OFF)", eb ?? "no error");
        }

        // OFF = byte-identical: every unit, empty pins or pins for another row, gets Choose's own answer.
        var units = new (string Sidc, string Ech, string Row)[]
        {
            ("SFGPUCI----H---", "BDE", "F-UCI-H"), ("SFGPUCA----H---", "BDE", "F-UCA-H"), ("SFGPUCI----F---", "BN", "F-UCI-F"),
            ("SFGPUCIZ---H---", "BDE", "F-UCIZ-H"), ("SFGPUCI--------", "BDE", "F-UCI-H"), ("SFGPUCI----H---", "", null),
        };
        var otherRow = new Dictionary<string, int[]>(StringComparer.Ordinal) { ["F-NOT-USED"] = new[] { 8, 4, 1, 111 } };
        int same = 0, total = 0;
        var diffs = new List<string>();
        foreach (var u in units)
            foreach (var pins in new IReadOnlyDictionary<string, int[]>[] { null, off, otherRow })
            {
                total++;
                var a = ContainerTypeRule.Choose(cat, 225, u.Sidc, u.Ech);
                var b = ChooseFor(cat, 225, u.Sidc, u.Ech, u.Row, pins);
                if (Signature(a) == Signature(b)) same++;
                else diffs.Add($"{u.Sidc}/{u.Row}: {Signature(a)} vs {Signature(b)}");
            }
        check(same == total, $"OFF is BYTE-IDENTICAL: with no pin for the unit's row, the decision equals the init rule's ({same} of {total})",
              string.Join("; ", diffs.Take(3)));

        // DIRTY CONTROL: the init rule itself, with BDE Airborne PA in the catalogue, still lands BDE Light Infantry.
        var rule = ContainerTypeRule.Choose(cat, 225, "SFGPUCI----H---", "BDE");
        check(rule.Found && rule.TypeText == "11:1:225:8:3:1:1" && rule.TemplateName == "BDE",
              "DIRTY CONTROL: the init rule's candidate loop lands BDE (8:3:1:1) for an infantry brigade, not BDE Airborne PA - " +
              "a candidate-list change cannot reach 8:4:1:111", $"{rule.TypeText} '{rule.TemplateName}'");

        // ON: the pinned row lands the pinned container.
        var pinned = ChooseFor(cat, 225, "SFGPUCI----H---", "BDE", "F-UCI-H", one);
        check(pinned.Found && pinned.TypeText == "11:1:225:8:4:1:111" && pinned.TemplateName == "BDE Airborne PA"
              && pinned.ExactBranch && pinned.Flags.Any(f => f.StartsWith("OVERRIDE", StringComparison.Ordinal)),
              "ON: map row F-UCI-H is pinned to 11:1:225:8:4:1:111 'BDE Airborne PA', flagged OVERRIDE on its TYPE MAP line",
              $"{pinned.TypeText} '{pinned.TemplateName}' [{string.Join("; ", pinned.Flags)}]");
        var unpinned = ChooseFor(cat, 225, "SFGPUCA----H---", "BDE", "F-UCA-H", one);
        check(Signature(unpinned) == Signature(ContainerTypeRule.Choose(cat, 225, "SFGPUCA----H---", "BDE")),
              "ON: a unit of ANOTHER map row is untouched (the init rule's answer)", Signature(unpinned));

        // Refusals: a pin that lands a UNIT, or nothing, is refused - never created as something else.
        var toUnit = TryParse("F-UCI-H=5:4:0:0", out var unitPin, out _)
            ? ChooseFor(cat, 225, "SFGPUCI----H---", "BDE", "F-UCI-H", unitPin) : null;
        check(toUnit != null && !toUnit.Found && toUnit.Flags.Any(f => f.StartsWith("NO CONTAINER", StringComparison.Ordinal)),
              "a pin that resolves to a UNIT is REFUSED for that unit (no container), loudly", toUnit == null ? "-" : string.Join("; ", toUnit.Flags));
        var toNothing = TryParse("F-UCI-H=8:99:1:1", out var nonePin, out _)
            ? ChooseFor(cat, 225, "SFGPUCI----H---", "BDE", "F-UCI-H", nonePin) : null;
        check(toNothing != null && !toNothing.Found, "a pin that resolves to NOTHING is REFUSED for that unit", toNothing == null ? "-" : string.Join("; ", toNothing.Flags));
        var noCat = ChooseFor(null, 225, "SFGPUCI----H---", "BDE", "F-UCI-H", one);
        check(!noCat.Found, "no catalogue + a pin = refused, as the init rule refuses without one", string.Join("; ", noCat.Flags));
    }

    /// <summary>Everything a <see cref="ContainerChoice"/> decides, as one comparable string.</summary>
    public static string Signature(ContainerChoice c) => c == null ? "null"
        : $"{c.Found}|{c.TypeText}|{c.TemplateName}|{c.ExactBranch}|{c.Category}|{c.Branch}|{c.FunctionId}|{string.Join("^", c.Flags)}";

    private sealed class PinCatalogue : ICatalogue
    {
        private readonly Dictionary<string, CatalogueEntry> _byType = new(StringComparer.Ordinal);
        public void Add(int[] t8, string name, CatalogueRole role)
            => _byType[string.Join(":", t8)] = new CatalogueEntry(name, name, role, t8, Array.Empty<SubordinateSpec>(), null);
        public CatalogueEntry Resolve(int[] type8)
            => type8 != null && _byType.TryGetValue(string.Join(":", type8), out var e) ? e : null;
        public string Describe => $"synthetic, {_byType.Count} entries";
    }
}
