using System.Globalization;
using System.Text.Json;

namespace VrfC2SimApp;

/// <summary>
/// How good the landed VR-Forces template is for the C2SIM unit it represents
/// (docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 5, R-SURFACE-PROXY).
/// </summary>
public enum TypeFidelity
{
    /// <summary>Legacy modes (GoldenParity / RealTemplates) - fidelity was never evaluated.</summary>
    Unspecified = 0,
    /// <summary>A real composed template of the right branch AND echelon.</summary>
    Exact,
    /// <summary>A real composed template of the wrong branch, echelon or nation - must be surfaced.</summary>
    Proxy,
    /// <summary>A declared coverage gap: nothing usable is installed. The unit is NOT created.</summary>
    AuthoredPending,
    /// <summary>No table row matched at all (a table defect). The unit is NOT created.</summary>
    Failed,
}

/// <summary>One row of data/unit-type-map.json.</summary>
public sealed record UnitTypeRow
{
    public string Id { get; init; } = "";
    public string SurveyRow { get; init; } = "";
    public string FunctionId { get; init; } = "";   // "" = echelon-only row; "(none)" = blank SIDC field
    public char Echelon { get; init; }              // SIDC position 12, verbatim; '*' = catch-all
    public string EchelonCode { get; init; } = "";  // C2SIM Unit/EchelonCode
    public string NationRole { get; init; } = "";   // "friendly" | "hostile"
    public string Nation { get; init; } = "";       // "USA" | "RUS" | "PRC"
    public bool IsAggregate { get; init; }
    public string ObjectType { get; init; } = "";   // 8-field VR-Forces type, "" when AuthoredPending
    public string TemplateName { get; init; } = "";
    public TypeFidelity Fidelity { get; init; }
    public string ProxyNote { get; init; } = "";

    /// <summary>The eight fields of <see cref="ObjectType"/>, or null when the row has none.</summary>
    public int[] Fields => UnitTypeMap.ParseObjectType(ObjectType);

    /// <summary>A one-line R-SURFACE-PROXY substitution string for the marking / report stream.</summary>
    public string SubstitutionText =>
        Fidelity == TypeFidelity.Exact || string.IsNullOrEmpty(ProxyNote)
            ? "" : $"{Fidelity}: {TemplateName} - {ProxyNote}";
}

/// <summary>The nation names in force for one run (Vrf:FriendlyNation / Vrf:OpposingNation).</summary>
public sealed record NationRoles(string Friendly, string Opposing);

/// <summary>What a lookup produced, plus how it got there (logged by the service).</summary>
public readonly record struct TypeMapMatch(UnitTypeRow Row, string KeyUsed, string Note);

/// <summary>
/// The fidelity mapping table (data/unit-type-map.json) and its lookup, per
/// docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 7.1. PURE - no bridge, no MAK
/// dependency - so --typemap-selftest can exercise it offline.
///
/// Key order, most specific first (sec 7.1 item 2):
///   (a) the init's own SISOEntityType when non-zero, BACKSTOPPED by this table
///       (JC-1, supervisor's PROVISIONAL ruling 2026-09-02: the init wins, but only
///       for a type the table covers - otherwise a declared 3:11:1:71:... would
///       silently create a zero-subordinate Country-0 abstract, survey sec 3.5);
///   (b) (functionId, SIDC echelon char, nationRole);
///   (c) (functionId, C2SIM EchelonCode, nationRole);
///   (d) (SIDC echelon char, nationRole) - the echelon-only rows;
///   (e) the nation's catch-all row ('*'), logged as a miss.
/// A row with fidelity AuthoredPending, or no row at all, FAILS LOUDLY and the unit is
/// not created - Ground_Aggregate is never an intentional target.
/// </summary>
public sealed class UnitTypeMap
{
    public IReadOnlyList<UnitTypeRow> Rows { get; }
    public IReadOnlyDictionary<string, int> Nations { get; }   // name -> DIS country code
    public string SourcePath { get; }

    private UnitTypeMap(IReadOnlyList<UnitTypeRow> rows, IReadOnlyDictionary<string, int> nations, string path)
    {
        Rows = rows;
        Nations = nations;
        SourcePath = path;
    }

    // ---- loading ----------------------------------------------------------

    /// <summary>
    /// Resolve <paramref name="configured"/> (Vrf:TypeMapFile, e.g. "data/unit-type-map.json")
    /// against the working directory, then the app directory, then by walking UP from the app
    /// directory - the app is launched both from the repo root (scripts/RunC2SimScenario.ps1)
    /// and from its bin folder. Returns null when nothing is found.
    /// </summary>
    public static string ResolvePath(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured)) return null;
        if (Path.IsPathRooted(configured))
            return File.Exists(configured) ? configured : null;

        var candidates = new List<string> { Path.GetFullPath(configured) };
        string baseDir = AppContext.BaseDirectory;
        candidates.Add(Path.GetFullPath(Path.Combine(baseDir, configured)));
        for (var d = new DirectoryInfo(baseDir); d != null; d = d.Parent)
            candidates.Add(Path.GetFullPath(Path.Combine(d.FullName, configured)));
        return candidates.FirstOrDefault(File.Exists);
    }

    public static UnitTypeMap Load(string path)
        => Parse(File.ReadAllText(path), path);

    public static UnitTypeMap Parse(string json, string path = "(inline)")
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var nations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("nations", out var n))
            foreach (var p in n.EnumerateObject())
                nations[p.Name] = p.Value.GetInt32();

        var rows = new List<UnitTypeRow>();
        foreach (var r in root.GetProperty("rows").EnumerateArray())
        {
            string ech = Str(r, "echelon");
            rows.Add(new UnitTypeRow
            {
                Id = Str(r, "id"),
                SurveyRow = Str(r, "surveyRow"),
                FunctionId = Str(r, "functionId"),
                Echelon = ech.Length > 0 ? ech[0] : '\0',
                EchelonCode = Str(r, "echelonCode"),
                NationRole = Str(r, "nationRole"),
                Nation = Str(r, "nation"),
                IsAggregate = r.TryGetProperty("isAggregate", out var a) && a.GetBoolean(),
                ObjectType = Str(r, "objectType"),
                TemplateName = Str(r, "templateName"),
                Fidelity = ParseFidelity(Str(r, "fidelity")),
                ProxyNote = Str(r, "proxyNote"),
            });
        }
        return new UnitTypeMap(rows, nations, path);
    }

    private static string Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";

    private static TypeFidelity ParseFidelity(string s) => s switch
    {
        "EXACT" => TypeFidelity.Exact,
        "PROXY" => TypeFidelity.Proxy,
        "AUTHORED_PENDING" => TypeFidelity.AuthoredPending,
        _ => TypeFidelity.Failed,
    };

    // ---- key extraction ---------------------------------------------------

    /// <summary>SIDC positions 5-10 (0-based 4..9), trailing '-' trimmed. "(none)" when blank.</summary>
    public static string FunctionIdOf(string sidc)
    {
        if (string.IsNullOrEmpty(sidc) || sidc.Length < 5) return "(none)";
        int end = Math.Min(10, sidc.Length);
        string f = sidc[4..end].TrimEnd('-');
        return f.Length == 0 ? "(none)" : f;
    }

    /// <summary>SIDC position 12 (0-based 11) verbatim - '-' when the field is empty.</summary>
    public static char EchelonCharOf(string sidc)
        => string.IsNullOrEmpty(sidc) || sidc.Length < 12 ? '\0' : sidc[11];

    /// <summary>friendly -> FriendlyNation, hostile -> OpposingNation.</summary>
    public static string NationFor(NationRoles roles, bool hostile)
        => hostile ? roles.Opposing : roles.Friendly;

    /// <summary>"k.d.c.cat.sub.spec.extra" (InitUnit.DisEntityType) -> the 8-field VRF type,
    /// prepending superType 3 for a Kind-11 military hierarchy and 1 otherwise. Null when the
    /// string is absent, malformed, or all-zero (COA-STP1 supplies 537 all-zero blocks).</summary>
    public static string VrfObjectTypeFromInitDis(string disEntityType)
    {
        if (string.IsNullOrWhiteSpace(disEntityType)) return null;
        var parts = disEntityType.Split('.');
        if (parts.Length != 7) return null;
        var f = new int[7];
        for (int i = 0; i < 7; i++)
            if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out f[i]))
                return null;
        if (f.All(v => v == 0)) return null;
        int superType = f[0] == 11 ? 3 : 1;
        return $"{superType}:{string.Join(':', f)}";
    }

    /// <summary>The eight fields of an "a:b:c:d:e:f:g:h" type, or null if it is not one.</summary>
    public static int[] ParseObjectType(string objectType)
    {
        if (string.IsNullOrWhiteSpace(objectType)) return null;
        var parts = objectType.Split(':');
        if (parts.Length != 8) return null;
        var f = new int[8];
        for (int i = 0; i < 8; i++)
            if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out f[i]))
                return null;
        return f;
    }

    // ---- lookup -----------------------------------------------------------

    /// <summary>
    /// Key (a)'s backstop: the row (if any) whose objectType IS the type the init declared.
    /// Nation-agnostic on purpose - a coalition partner's declared type is honoured whatever
    /// side it is on, as long as the table proves it lands a real template.
    /// </summary>
    public UnitTypeRow FindByObjectType(string vrfObjectType)
        => string.IsNullOrEmpty(vrfObjectType)
         ? null
         : Rows.FirstOrDefault(r => r.Fidelity != TypeFidelity.AuthoredPending
                                    && r.ObjectType == vrfObjectType);

    /// <summary>Keys (b) -> (c) -> (d) -> (e). Never returns a null Row.</summary>
    public TypeMapMatch Lookup(string functionId, char echelon, string echelonCode,
                               string nationRole, string nation)
    {
        bool Nat(UnitTypeRow r) =>
            string.Equals(r.NationRole, nationRole, StringComparison.OrdinalIgnoreCase)
            && string.Equals(r.Nation, nation, StringComparison.OrdinalIgnoreCase);

        // (b) function ID + SIDC echelon character
        var row = Rows.FirstOrDefault(r => Nat(r) && r.FunctionId.Length > 0
                                           && r.FunctionId == functionId && r.Echelon == echelon);
        if (row != null) return new(row, "b:functionId+sidcEchelon", "");

        // (c) function ID + C2SIM EchelonCode
        if (!string.IsNullOrEmpty(echelonCode))
        {
            row = Rows.FirstOrDefault(r => Nat(r) && r.FunctionId.Length > 0
                                           && r.FunctionId == functionId
                                           && string.Equals(r.EchelonCode, echelonCode, StringComparison.OrdinalIgnoreCase));
            if (row != null)
                return new(row, "c:functionId+echelonCode",
                           $"SIDC echelon '{Show(echelon)}' had no row; matched on EchelonCode '{echelonCode}'.");
        }

        // (d) echelon character alone
        row = Rows.FirstOrDefault(r => Nat(r) && r.FunctionId.Length == 0 && r.Echelon == echelon);
        if (row != null)
            return new(row, "d:sidcEchelon",
                       $"no row for functionId '{functionId}' ({nationRole}/{nation}); echelon-only fallback.");

        // (e) the nation's catch-all
        row = Rows.FirstOrDefault(r => Nat(r) && r.FunctionId.Length == 0 && r.Echelon == '*');
        if (row != null)
            return new(row, "e:catchAll",
                       $"NO ROW for functionId '{functionId}' echelon '{Show(echelon)}' echelonCode " +
                       $"'{echelonCode}' ({nationRole}/{nation}) - fell through to the catch-all.");

        return new(new UnitTypeRow
        {
            Id = "(unmapped)",
            NationRole = nationRole,
            Nation = nation,
            Fidelity = TypeFidelity.Failed,
            ProxyNote = $"no row and no catch-all for functionId '{functionId}' echelon " +
                        $"'{Show(echelon)}' ({nationRole}/{nation}).",
        }, "none", "");
    }

    private static string Show(char c) => c == '\0' ? "(none)" : c.ToString();

    // ---- start-up validation (JC-2) ---------------------------------------

    /// <summary>
    /// JC-2 (supervisor's PROVISIONAL ruling 2026-09-02): a nation whose rows for its role are
    /// all AuthoredPending must REFUSE TO START rather than silently create empty units (survey
    /// sec 3.5). Returns null when the nation is usable, otherwise the operator-facing error
    /// naming the missing content. Applied to BOTH roles - the hole is the same whichever side
    /// an unstocked nation is configured on.
    /// </summary>
    public string CheckNationSupported(string nationRole, string nation)
    {
        string setting = nationRole == "hostile" ? "Vrf:OpposingNation" : "Vrf:FriendlyNation";
        if (string.IsNullOrWhiteSpace(nation))
            return $"{setting} is empty. Set it to one of: " + string.Join(", ", Nations.Keys) + ".";
        if (!Nations.ContainsKey(nation))
            return $"{setting}='{nation}' is not a nation in {SourcePath}. " +
                   "Known nations: " + string.Join(", ", Nations.Keys) + ".";

        var rows = Rows.Where(r => string.Equals(r.NationRole, nationRole, StringComparison.OrdinalIgnoreCase)
                                   && string.Equals(r.Nation, nation, StringComparison.OrdinalIgnoreCase))
                       .ToList();
        if (rows.Count == 0)
            return $"{setting}='{nation}' has no {nationRole} rows at all in {SourcePath}.";

        int usable = rows.Count(r => r.Fidelity is TypeFidelity.Exact or TypeFidelity.Proxy && r.IsAggregate);
        if (usable > 0) return null;

        var pending = rows.Where(r => r.Fidelity == TypeFidelity.AuthoredPending)
                          .Select(r => r.Id).Take(6).ToList();
        return $"{setting}='{nation}' has NO usable UNIT template: " +
               $"{rows.Count(r => r.Fidelity == TypeFidelity.AuthoredPending)} of {rows.Count} rows are " +
               $"AUTHORED_PENDING (e.g. {string.Join(", ", pending)}). The installed VR-Forces 5.0.2 model-set " +
               "chain (C2simEx -> EntityLevel -> base) contains ZERO Country-" +
               (Nations.TryGetValue(nation, out var c) ? c.ToString(CultureInfo.InvariantCulture) : "?") +
               " unit templates - only platform leaves - so every aggregate request would land a " +
               "zero-subordinate Country-0 abstract or Ground_Aggregate (empty units). REFUSING TO START. " +
               "Fix: author the " + nation + " unit templates into a project-owned SMS that includes " +
               "C2simEx.sms (docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 7.4), add their rows to " +
               SourcePath + ", or set " + setting + " to a stocked nation (USA / RUS).";
    }

    /// <summary>JC-2 for the hostile role - the case the ruling names explicitly.</summary>
    public string CheckOpposingNationSupported(string nation) => CheckNationSupported("hostile", nation);
}
