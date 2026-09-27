using System.Globalization;
using System.Text.Json;

namespace VrfC2SimApp;

/// <summary>One entry of a composition row (data/unit-composition-52-aggregate.json "subordinates").</summary>
public sealed record CompositionEntry(string Function, int Count, string Role, string ObjectType, string TemplateName,
                                      string Compose, string Fidelity, string Note);

/// <summary>One row of the authored composition table: the sub-units a tasked container is populated with.</summary>
public sealed record CompositionRow(string Id, IReadOnlyList<string> MapRowIds, string ContainerObjectType,
                                    string ContainerTemplate, string Depth, IReadOnlyList<CompositionEntry> Subordinates);

/// <summary>
/// THE AUTHORED COMPOSITION TABLE (Vrf:CompositionFile, default data/unit-composition-52-aggregate.json; C1,
/// RL-20260927-02: "populate the containers, not proxies"; D-2 of RL-20260927-04: compose to doctrine, FM 3-96).
/// PURE: JSON in, rows out. The schema is the one tools/aggregate/composition_check.py gates offline; this reader
/// takes the fields it needs BY NAME and ignores every other one, so the parallel lane that authors the missing US
/// unit types (C2, feat/aggregate-authored-units) can ADD fields and rows without a code change. schemaVersion 1 only:
/// a different major is refused rather than guessed at.
/// </summary>
public sealed class CompositionTable
{
    public const string DefaultFile = "data/unit-composition-52-aggregate.json";

    public IReadOnlyList<CompositionRow> Rows { get; }
    public string SourcePath { get; }
    public string ModelSetKey { get; }

    private CompositionTable(IReadOnlyList<CompositionRow> rows, string path, string modelSetKey)
    {
        Rows = rows;
        SourcePath = path;
        ModelSetKey = modelSetKey;
    }

    /// <summary>Same search as the type map (UnitTypeMap.ResolvePath): working directory, app directory, and every
    /// directory above the app directory.</summary>
    public static string ResolvePath(string configured)
        => UnitTypeMap.ResolvePath(string.IsNullOrWhiteSpace(configured) ? DefaultFile : configured);

    public static CompositionTable Load(string path) => Parse(File.ReadAllText(path), path);

    public static CompositionTable Parse(string json, string path = "(inline)")
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        int schema = root.TryGetProperty("schemaVersion", out var sv) && sv.ValueKind == JsonValueKind.Number
            ? sv.GetInt32() : 0;
        if (schema != 1)
            throw new InvalidDataException($"schemaVersion {schema} - this reader knows schema 1 only (add fields, " +
                                           "never rename them; composition_check.py gates the same schema)");
        var rows = new List<CompositionRow>();
        if (root.TryGetProperty("rows", out var rs) && rs.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in rs.EnumerateArray())
            {
                var ids = new List<string>();
                if (r.TryGetProperty("mapRowIds", out var m) && m.ValueKind == JsonValueKind.Array)
                    foreach (var x in m.EnumerateArray())
                        if (x.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(x.GetString()))
                            ids.Add(x.GetString().Trim());
                string cType = "", cTmpl = "";
                if (r.TryGetProperty("container", out var c) && c.ValueKind == JsonValueKind.Object)
                {
                    cType = Str(c, "objectType");
                    cTmpl = Str(c, "templateName");
                }
                var subs = new List<CompositionEntry>();
                if (r.TryGetProperty("subordinates", out var ss) && ss.ValueKind == JsonValueKind.Array)
                    foreach (var s in ss.EnumerateArray())
                        subs.Add(new CompositionEntry(
                            Str(s, "function"),
                            s.TryGetProperty("count", out var n) && n.ValueKind == JsonValueKind.Number ? n.GetInt32() : 0,
                            Str(s, "role"), Str(s, "objectType"), Str(s, "templateName"), Str(s, "compose"),
                            Str(s, "fidelity"), Str(s, "note")));
                rows.Add(new CompositionRow(Str(r, "id"), ids, cType, cTmpl, Str(r, "depth"), subs));
            }
        }
        return new CompositionTable(rows, path, Str(root, "modelSetKey"));
    }

    private static string Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "").Trim() : "";

    public CompositionRow ById(string id)
        => string.IsNullOrEmpty(id) ? null : Rows.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.Ordinal));

    /// <summary>The rows that claim a type-map row id (CreationPlan.MapRowId). The FIRST is the one used; a second
    /// claimant is reported by <see cref="CompositionResolver.Resolve"/> as a table defect, not silently chosen.</summary>
    public IReadOnlyList<CompositionRow> ForMapRow(string mapRowId)
        => string.IsNullOrEmpty(mapRowId)
            ? Array.Empty<CompositionRow>()
            : Rows.Where(r => r.MapRowIds.Contains(mapRowId, StringComparer.Ordinal)).ToList();
}

/// <summary>Where a container's members come from (design sec 4.1, in precedence order).</summary>
public enum PopulateSource
{
    /// <summary>Nothing: the container has no composition - a MOVE on it is refused (never a vacuous success).</summary>
    None,
    /// <summary>(1) The STP TO: its declared C2SIM subordinates present in the init are ATTACHED (they exist as
    /// containers; never re-created) and each is populated by these rules in turn.</summary>
    StpTo,
    /// <summary>(2) The container template's own configured &lt;subordinate&gt; list (its .entity).</summary>
    Catalogue,
    /// <summary>(3) The authored table, looked up by the type-map row id.</summary>
    Table,
}

/// <summary>One simulated leaf a flat population creates: a warfare-model UNIT, created AGGREGATED.</summary>
/// <param name="Path">The composition path, e.g. "C-USA-BDE-UCI/INF2/RIF3" - for the record.</param>
/// <param name="Suffix">The unique member-name suffix built from that path below the row, e.g. "INF2RIF3".</param>
public sealed record PopulateLeaf(string Path, string Suffix, string Function, int[] ObjectType, string TemplateName,
                                  string Fidelity, double? TravelFootprintMeters, int Category);

/// <summary>What <see cref="CompositionResolver.Resolve"/> decided for one tasked container.</summary>
public sealed record PopulatePlan(PopulateSource Source, string RowId, IReadOnlyList<PopulateLeaf> Leaves,
                                  IReadOnlyList<string> ExistingChildren, int SubContainersFlattened, string Refusal)
{
    public bool Refused => Refusal != null;
    public static PopulatePlan Refuse(PopulateSource source, string rowId, string why)
        => new(source, rowId, Array.Empty<PopulateLeaf>(), Array.Empty<string>(), 0, why);
}

/// <summary>
/// THE COMPOSITION RESOLVER (C1; design secs 4.1-4.4; RL-20260927-02, RL-20260927-03, RL-20260927-04 D-2/D-8).
/// PURE: a table, a catalogue and a unit's facts in; a flat list of simulated leaves (or a refusal) out.
///
/// PRECEDENCE: (1) the STP TO's declared subordinates present in the init (attached, never re-created); (2) the
/// container template's own configured members (CATALOGUE); (3) the authored table by the type-map row id
/// (CreationPlan.MapRowId). None of them = REFUSED: an empty container's move "ends at once" (PA_Move_Along_Route.lua
/// tick(): allSubordinatesComplete() over an empty snapshot) and must never read as a success.
///
/// FLAT (D-8, RL-20260927-04): the table keeps its nested ORBAT (battalion sub-containers), and every sub-container is
/// FLATTENED to its simulated leaves - every leaf attaches directly to the tasked container, the vendor-exercised
/// shape (TF 2/5/CAV). The design's reason (sec 4.4): PA_Move_Along_Route.lua:133 ends every subordinate's move with
/// the built-in "move-to-location-retrograde-task", which a sub-container has no controller for.
///
/// REFUSAL OF UNKNOWN TYPES: every leaf must land a catalogue warfare-model UNIT and every sub-container a CONTAINER
/// that composes to something, by the vendor best-match rule - the same gates as composition_check.py's resolution /
/// recursion / echelon gates. A NEW row (the C2 lane's authored types) is accepted with no code change as soon as its
/// types resolve in the loaded catalogue (Vrf:CatalogueSms names a derived SMS when they live in one).
/// </summary>
public static class CompositionResolver
{
    /// <summary>composition_check.py MAX_DEPTH.</summary>
    public const int MaxDepth = 4;

    /// <summary>Echelon RANK from the Unit Category (UG52 D.3.4 p1689-1690): Team, Squad and Section are VR-Forces
    /// extensions numbered ABOVE the army echelons, so the category number is not an order - composition_check.py
    /// ECHELON_RANK.</summary>
    public static int? Rank(int category) => category switch
    {
        1 => 0, 2 => 1, 12 => 2, 13 => 2, 14 => 2, 3 => 3, 4 => 4, 5 => 4, 6 => 5, 7 => 6, 8 => 7, 9 => 8, 10 => 9,
        11 => 10, _ => null,
    };

    public static PopulatePlan Resolve(string containerName, CatalogueEntry container, string mapRowId,
                                       IReadOnlyList<string> declaredChildrenPresent, CompositionTable table,
                                       ICatalogue catalogue)
    {
        // (1) THE STP TO.
        if (declaredChildrenPresent is { Count: > 0 })
            return new PopulatePlan(PopulateSource.StpTo, "", Array.Empty<PopulateLeaf>(),
                                    declaredChildrenPresent.ToList(), 0, null);

        int? containerRank = container != null ? Rank(container.Category) : null;

        // (2) THE TEMPLATE'S OWN CONFIGURED MEMBERS.
        if (container != null && container.Role == CatalogueRole.Container && container.Subordinates is { Count: > 0 })
        {
            var leaves = new List<PopulateLeaf>();
            int flattened = 0;
            string why = ExpandCatalogue(container, catalogue, "", "CATALOGUE:" + container.DisplayName, 1, leaves,
                                         ref flattened);
            if (why != null) return PopulatePlan.Refuse(PopulateSource.Catalogue, "", why);
            return Finish(PopulateSource.Catalogue, "", leaves, flattened, containerRank);
        }

        // (3) THE AUTHORED TABLE.
        var rows = table?.ForMapRow(mapRowId) ?? Array.Empty<CompositionRow>();
        if (rows.Count == 0)
            return PopulatePlan.Refuse(PopulateSource.None, "",
                $"NO COMPOSITION for {containerName}: its type-map row '{(string.IsNullOrEmpty(mapRowId) ? "(none)" : mapRowId)}' " +
                $"is named by no row of {table?.SourcePath ?? "(no composition table loaded)"} and its container template " +
                $"'{container?.DisplayName ?? "(unresolved)"}' configures no subordinates");
        if (rows.Count > 1)
            return PopulatePlan.Refuse(PopulateSource.Table, rows[0].Id,
                $"TABLE DEFECT: map row '{mapRowId}' is claimed by {rows.Count} composition rows " +
                $"({string.Join(", ", rows.Select(r => r.Id))}) - one row per map row");
        return ExpandRow(rows[0], table, catalogue, containerRank);
    }

    /// <summary>Expand one table row to its FLAT leaves (D-8). Public so the start-up validation and the self-test
    /// can walk every row.</summary>
    public static PopulatePlan ExpandRow(CompositionRow row, CompositionTable table, ICatalogue catalogue,
                                         int? containerRank = null)
    {
        var leaves = new List<PopulateLeaf>();
        int flattened = 0;
        string why = ExpandRowInto(row, table, catalogue, "", row.Id, 0, new List<string>(), leaves, ref flattened);
        if (why != null) return PopulatePlan.Refuse(PopulateSource.Table, row.Id, why);
        return Finish(PopulateSource.Table, row.Id, leaves, flattened, containerRank);
    }

    private static PopulatePlan Finish(PopulateSource source, string rowId, List<PopulateLeaf> leaves, int flattened,
                                       int? containerRank)
    {
        if (leaves.Count == 0)
            return PopulatePlan.Refuse(source, rowId, "the composition expands to NO simulated leaf");
        // UG52 40.80 p902: "For aggregate-level scenarios, the superior must be a higher echelon unit."
        if (containerRank is int cr)
            foreach (var l in leaves)
                if (Rank(l.Category) is not int lr || lr >= cr)
                    return PopulatePlan.Refuse(source, rowId,
                        $"ECHELON: leaf {l.Path} ({l.TemplateName}, {ContainerTypeRule.EchelonAbbr(l.Category)}) is not " +
                        "below the container it would join (UG52 40.80 p902: the superior must be a higher echelon unit)");
        return new PopulatePlan(source, rowId, leaves, Array.Empty<string>(), flattened, null);
    }

    private static string ExpandRowInto(CompositionRow row, CompositionTable table, ICatalogue catalogue,
                                        string suffixPrefix, string pathPrefix, int depth, List<string> stack,
                                        List<PopulateLeaf> leaves, ref int flattened)
    {
        if (stack.Contains(row.Id)) return $"compose CYCLE {string.Join(" -> ", stack.Append(row.Id))}";
        if (depth > MaxDepth) return $"row {row.Id} is deeper than MaxDepth {MaxDepth}";
        if (row.Subordinates.Count == 0) return $"row {row.Id} has NO subordinates (an EMPTY container cannot move)";
        stack.Add(row.Id);
        try
        {
            foreach (var s in row.Subordinates)
            {
                string where = $"{pathPrefix}/{s.Function}";
                if (s.Count < 1) return $"{where}: count {s.Count} < 1";
                var t8 = UnitTypeMap.ParseObjectType(s.ObjectType);
                if (t8 == null || t8[0] != 3 || t8[1] != 11 || t8[2] != 1)
                    return $"{where}: objectType '{s.ObjectType}' is not an 8-field kind-11 LAND unit type";
                var hit = catalogue?.Resolve(t8);
                if (hit == null || hit.Role is CatalogueRole.BaseAbstract or CatalogueRole.Unknown or CatalogueRole.Air)
                    return $"{where}: UNKNOWN TYPE - {s.ObjectType} lands {(hit == null ? "nothing" : hit.DisplayName + " (" + hit.Role + ")")} " +
                           "in the catalogue (not a warfare-model unit or a container)";
                bool wantUnit = string.Equals(s.Role, "UNIT", StringComparison.OrdinalIgnoreCase);
                bool wantContainer = string.Equals(s.Role, "CONTAINER", StringComparison.OrdinalIgnoreCase);
                if (!wantUnit && !wantContainer) return $"{where}: role '{s.Role}' is neither UNIT nor CONTAINER";
                if (wantUnit && hit.Role != CatalogueRole.Unit)
                    return $"{where}: says UNIT, but {s.ObjectType} lands {hit.DisplayName}, a {hit.Role}";
                if (wantContainer && hit.Role != CatalogueRole.Container)
                    return $"{where}: says CONTAINER, but {s.ObjectType} lands {hit.DisplayName}, a {hit.Role}";
                for (int i = 1; i <= s.Count; i++)
                {
                    string seg = s.Function + i.ToString(CultureInfo.InvariantCulture);
                    if (wantUnit)
                    {
                        leaves.Add(new PopulateLeaf($"{where}{i}", suffixPrefix + seg, s.Function, hit.ObjectType,
                                                    hit.DisplayName, s.Fidelity, hit.TravelFootprintMeters, hit.Category));
                        continue;
                    }
                    flattened++;
                    string compose = s.Compose ?? "";
                    if (compose.Length == 0) return $"{where}{i}: CONTAINER entry with no 'compose'";
                    if (string.Equals(compose, "CATALOGUE", StringComparison.Ordinal))
                    {
                        string why = ExpandCatalogue(hit, catalogue, suffixPrefix + seg, $"{where}{i}", depth + 1, leaves,
                                                     ref flattened);
                        if (why != null) return why;
                        continue;
                    }
                    var sub = table?.ById(compose);
                    if (sub == null) return $"{where}{i}: compose ref '{compose}' names no row";
                    string w2 = ExpandRowInto(sub, table, catalogue, suffixPrefix + seg, $"{where}{i}", depth + 1, stack,
                                              leaves, ref flattened);
                    if (w2 != null) return w2;
                }
            }
            return null;
        }
        finally { stack.RemoveAt(stack.Count - 1); }
    }

    private static string ExpandCatalogue(CatalogueEntry tmpl, ICatalogue catalogue, string suffixPrefix, string where,
                                          int depth, List<PopulateLeaf> leaves, ref int flattened)
    {
        if (depth > MaxDepth) return $"{where}: CATALOGUE expansion deeper than MaxDepth {MaxDepth}";
        if (tmpl.Subordinates is not { Count: > 0 })
            return $"{where}: CATALOGUE compose on {tmpl.DisplayName}, which configures NO subordinates (an EMPTY " +
                   "container - its move would end at once)";
        for (int k = 0; k < tmpl.Subordinates.Count; k++)
        {
            var spec = tmpl.Subordinates[k];
            string handle = string.IsNullOrEmpty(spec.FunctionHandle) ? "SUB" : spec.FunctionHandle;
            // composition_check.py expand_catalogue: the ordinal is the position in the template's list.
            string seg = handle + (k + 1).ToString(CultureInfo.InvariantCulture);
            string at = $"{where}/{seg}";
            var hit = catalogue?.Resolve(spec.ObjectType);
            if (hit == null || hit.Role is CatalogueRole.BaseAbstract or CatalogueRole.Unknown or CatalogueRole.Air)
                return $"{at}: UNKNOWN TYPE - {string.Join(":", spec.ObjectType ?? Array.Empty<int>())} lands " +
                       $"{(hit == null ? "nothing" : hit.DisplayName + " (" + hit.Role + ")")} in the catalogue";
            if (hit.Role == CatalogueRole.Unit)
            {
                leaves.Add(new PopulateLeaf(at, suffixPrefix + seg, handle, hit.ObjectType, hit.DisplayName, "CAT",
                                            hit.TravelFootprintMeters, hit.Category));
                continue;
            }
            flattened++;
            string why = ExpandCatalogue(hit, catalogue, suffixPrefix + seg, at, depth + 1, leaves, ref flattened);
            if (why != null) return why;
        }
        return null;
    }
}

/// <summary>One planned member of a population: its unique name and its slot on the ring.</summary>
public sealed record PopulateMember(int Slot, string Name, PopulateLeaf Leaf, double NorthMeters, double EastMeters,
                                    double LatDeg, double LonDeg, double BearingDeg);

/// <summary>The ring a flat population is born on.</summary>
public sealed record PopulateLayout(IReadOnlyList<PopulateMember> Members, double SpacingMeters, double RadiusMeters,
                                    double ReachMeters, string Refusal)
{
    public bool Refused => Refusal != null;
}

/// <summary>
/// PLACEMENT OF A FLAT POPULATION (design sec 5; C1, RL-20260927-03). Every member is born on ONE ring round the
/// container's point, at equal bearings, at the centroid-preserving radius r = s / (2 sin(pi / N))
/// (DeStacker.CentroidPreservingRadius / EqualBearingOffset) - so the members' centroid, which is where the vendor's
/// aggregate actuator publishes the container (vrfmodel/disaggregatedActuator.h:9-26; 61 of 61 vendor containers at
/// the mean of their immediate subordinates), stays where the map showed the container. The spacing s is 2 x the
/// largest member reach: a unit's Travel-posture footprint radius (UG52 27.1.2-27.1.3), except that a leaf ABOVE
/// company echelon takes a slot but does not size the ring (its footprint is a battalion's area; an overlap only
/// lowers MAXIMUM speed, UG52 27.1.4). The member listed first (the HQ in every row) takes slot 0. A PORT of
/// composition_check.py reach() / flat_ring(); PURE.
/// </summary>
public static class PopulatePlanner
{
    /// <summary>The interface's marking limit for a name it asks VR-Forces for (VrfC2SimService.MaxVrfMarkingChars).</summary>
    public const int MaxNameChars = 34;

    /// <summary>A leaf of this echelon rank or below sizes the ring (composition_check.py SIZING_RANK_MAX = CO).</summary>
    public static readonly int SizingRankMax = CompositionResolver.Rank(5) ?? 4;

    private const double MetersPerDegLat = 111_320.0;   // DeStacker's constant

    /// <summary>"&lt;container, trimmed&gt;.&lt;suffix&gt;" within <see cref="MaxNameChars"/> - the shape of
    /// VrfC2SimService.MakeChildName, so a member reads as its container's child in every log.</summary>
    public static string MemberName(string container, string suffix)
    {
        string s = "." + suffix;
        int room = MaxNameChars - s.Length;
        string p = container ?? "";
        if (p.Length > room) p = p.Substring(0, Math.Max(1, room));
        return p + s;
    }

    public static PopulateLayout Plan(string containerName, double anchorLat, double anchorLon,
                                      IReadOnlyList<PopulateLeaf> leaves, double rotationDeg)
    {
        if (leaves == null || leaves.Count == 0)
            return new PopulateLayout(Array.Empty<PopulateMember>(), 0.0, 0.0, 0.0, "no leaf to place");
        var sizing = new List<double>();
        var all = new List<double>();
        foreach (var l in leaves)
        {
            double r = l.TravelFootprintMeters is double f && f > 0.0 && double.IsFinite(f) ? f : 0.0;
            all.Add(r);
            if (CompositionResolver.Rank(l.Category) is not int rank || rank <= SizingRankMax) sizing.Add(r);
        }
        double spacing = 2.0 * (sizing.Count > 0 ? sizing.Max() : all.Max());
        double radius = DeStacker.CentroidPreservingRadius(leaves.Count, spacing);
        double reach = radius + all.Max();
        double metersPerDegLon = MetersPerDegLat * Math.Max(Math.Cos(anchorLat * Math.PI / 180.0), 0.01);
        var members = new List<PopulateMember>(leaves.Count);
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (int k = 0; k < leaves.Count; k++)
        {
            var (north, east) = DeStacker.EqualBearingOffset(k, leaves.Count, radius, rotationDeg);
            string name = MemberName(containerName, leaves[k].Suffix);
            if (!names.Add(name))
                return new PopulateLayout(Array.Empty<PopulateMember>(), spacing, radius, reach,
                    $"NAME COLLISION: member name '{name}' is produced twice within {MaxNameChars} characters");
            double bearing = ((rotationDeg + 360.0 * k / leaves.Count) % 360.0 + 360.0) % 360.0;
            members.Add(new PopulateMember(k, name, leaves[k], north, east,
                                           anchorLat + north / MetersPerDegLat, anchorLon + east / metersPerDegLon,
                                           radius > 0.0 ? bearing : 0.0));
        }
        return new PopulateLayout(members, spacing, radius, reach, null);
    }
}
