using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// WHAT A TEMPLATE IS on the aggregate model set, read off its PLATFORM file (C1; RL-20260927-02, RL-20260927-03).
/// UG52 72.2.1 p1419: "the aggregate-level simulation objects that are configured to use the aggregate warfare model
/// do not have the configuration options for specifying subordinates. Therefore, AggregateLevelBase.sms has a
/// platform, Aggregate Container, for use when aggregating simulation objects." The platform is the discriminator:
/// AggregateLevelAggregate.ope = a warfare-model UNIT (one simulated object, UG52 27.1 p528), PseudoAggregate.ope =
/// an Aggregate CONTAINER (no warfare model; its moves are the PA_* scripts that task its subordinates). The same
/// table as tools/aggregate/survey_magx.py PLATFORM_ROLE, which the offline gate composition_check.py reads.
/// </summary>
public enum CatalogueRole
{
    /// <summary>Nothing resolved, or a platform this table does not know.</summary>
    Unknown,
    /// <summary>AggregateLevelAggregate.ope - a warfare-model unit; created AGGREGATED (UG52 Table 68 p1470).</summary>
    Unit,
    /// <summary>PseudoAggregate.ope - an Aggregate Container; created DISAGGREGATED and populated by attaching units.</summary>
    Container,
    /// <summary>Aggregate.ope - the base abstract a type with no better match lands (an EMPTY unit - never a target).</summary>
    BaseAbstract,
    /// <summary>AircraftAggregate.ope / AirPseudoAggregate.ope - out of the ground scope of C1.</summary>
    Air,
}

/// <summary>One resolved catalogue template, reduced to what C1 reads.</summary>
public sealed record CatalogueEntry(string Name, string Label, CatalogueRole Role, int[] ObjectType,
                                    IReadOnlyList<SubordinateSpec> Subordinates, double? TravelFootprintMeters)
{
    /// <summary>The Unit Category (echelon) field of the published type (UG52 D.3.4 p1689-1690).</summary>
    public int Category => ObjectType is { Length: 8 } ? ObjectType[4] : -1;

    /// <summary>The name the vendor's GUI shows when it has one, else the .entity file's base name.</summary>
    public string DisplayName => string.IsNullOrEmpty(Label) ? Name : Label;
}

/// <summary>The catalogue C1 resolves against: the vendor best-match rule over the installed chain
/// (<see cref="ResolverCatalogue"/>), or a synthetic one in a self-test.</summary>
public interface ICatalogue
{
    /// <summary>The template the vendor's best-match rule lands for an 8-field type, or null.</summary>
    CatalogueEntry Resolve(int[] type8);

    /// <summary>What the catalogue is, for the start-up line (root SMS, template count).</summary>
    string Describe { get; }
}

/// <summary>
/// The installed chain through <see cref="ObjectTypeResolver"/> (the vendor's documented best-match rule, validated
/// 6/6 on the entity chain - VRF_GROUND_TRUTH sec 0.1.5 - and the same port tools/aggregate/survey_magx.py uses).
/// </summary>
public sealed class ResolverCatalogue : ICatalogue
{
    private readonly ObjectTypeResolver _r;

    public ResolverCatalogue(ObjectTypeResolver resolver) => _r = resolver ?? throw new ArgumentNullException(nameof(resolver));

    public ObjectTypeResolver Resolver => _r;

    public string Describe =>
        $"root {_r.RootSms}, {_r.Templates.Count} template(s) from {_r.ModelSetDirs.Count} model-set dir(s)";

    public CatalogueEntry Resolve(int[] type8)
    {
        if (type8 is not { Length: 8 }) return null;
        var t = _r.Resolve(type8);
        if (t == null) return null;
        return new CatalogueEntry(t.Name, t.GuiLabel, RoleOfPlatform(t.Platform), t.ObjectType, t.SubordinateSpecs,
                                  t.TravelFootprintMeters);
    }

    /// <summary>survey_magx.py PLATFORM_ROLE, ported. The file name only (the .entity writes
    /// "@(platforms-dir)\AggregateLevelAggregate.ope").</summary>
    public static CatalogueRole RoleOfPlatform(string platformFile)
    {
        string p = (platformFile ?? "").Trim();
        if (p.Equals("AggregateLevelAggregate.ope", StringComparison.OrdinalIgnoreCase)) return CatalogueRole.Unit;
        if (p.Equals("PseudoAggregate.ope", StringComparison.OrdinalIgnoreCase)) return CatalogueRole.Container;
        if (p.Equals("Aggregate.ope", StringComparison.OrdinalIgnoreCase)) return CatalogueRole.BaseAbstract;
        if (p.Equals("AircraftAggregate.ope", StringComparison.OrdinalIgnoreCase)
            || p.Equals("AirPseudoAggregate.ope", StringComparison.OrdinalIgnoreCase)) return CatalogueRole.Air;
        return CatalogueRole.Unknown;
    }
}

/// <summary>What <see cref="ContainerTypeRule.Choose"/> decided for one init unit.</summary>
public sealed record ContainerChoice(bool Found, int[] Type8, string TemplateName, bool ExactBranch, int Category,
                                     int Branch, string FunctionId, IReadOnlyList<string> Flags)
{
    /// <summary>"11:1:225:8:3:1:1" - the seven DIS fields, as the census and the catalogue print them.</summary>
    public string TypeText => Type8 is { Length: 8 } ? string.Join(":", Type8.Skip(1)) : "-";
}

/// <summary>
/// THE INIT RULE OF C1 (docs/DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md sec 3; RL-20260927-03: "I still want all units
/// to show on the map on initialization"). Every in-scope unit is created at init as an EMPTY Aggregate Container of
/// the right nation, echelon and branch:
///   type = (3, 11, 1, nation, echelon category, branch, specific, extra) where
///   - nation   = the DIS country of the unit's mapped row (RUS 260 for the hostile side, RL-20260927-02);
///   - echelon  = SIDC position 12 (D 3, E 5, F 6, G 7, H 8, I 9, J 10), else the C2SIM EchelonCode, else BN;
///   - branch   = the candidates of the unit's SIDC function ID, then the fallbacks 3, 4, 2 (the NEAREST branch
///                where the catalogue has none - logged, never silent);
///   - specific/extra = 1:0, then 1:1 (the generic containers publish 1:-1 or 1:1).
/// The first candidate that lands a CONTAINER (by the vendor best-match rule) wins. The vendor does the same with its
/// own generic containers (Road to Kaunas: Wolf BDE 11:1:255:8:4:1:0 lands "BDE, Mech Infantry"). This is a PORT of
/// tools/aggregate/composition_check.py container_for / init_census; --populate-selftest pins its 36 Iron Storm
/// answers, and the python gate prints the same table (--init-census).
/// DEVIATION FROM THE DESIGN, written: the design put these types into the aggregate type map's rows (sec 7). Those
/// rows are owned by the parallel lane C2 (feat/aggregate-authored-units), so the rule is applied here instead and the
/// map keeps supplying CreationPlan.MapRowId - the composition key - unchanged.
/// </summary>
public static class ContainerTypeRule
{
    /// <summary>Branch (Unit Subcategory, UG52 D.3.4 p1690) candidates per SIDC function ID, most specific first -
    /// composition_check.py BRANCH_CANDIDATES.</summary>
    public static readonly IReadOnlyDictionary<string, int[]> BranchCandidates = new Dictionary<string, int[]>(StringComparer.Ordinal)
    {
        ["UCI"] = new[] { 3, 4 }, ["UCIZ"] = new[] { 4 }, ["UCIM"] = new[] { 18, 4 }, ["UCA"] = new[] { 2 },
        ["UCATA"] = new[] { 2 }, ["UCAW"] = new[] { 4, 2 }, ["UCAAA"] = new[] { 12, 2 }, ["UCRVA"] = new[] { 30, 5, 2 },
        ["UCF"] = new[] { 7, 8 }, ["UCE"] = new[] { 10 }, ["US"] = new[] { 31 }, ["UCVR"] = new[] { 14, 3 },
        ["UCVRA"] = new[] { 14, 3 }, ["GS"] = new[] { 30 }, ["UCSGM"] = new[] { 18, 4 },
    };

    /// <summary>SIDC position 12 -> Unit Category (UG52 D.3.4 p1689).</summary>
    public static readonly IReadOnlyDictionary<char, int> EchelonCategory = new Dictionary<char, int>
    {
        ['D'] = 3, ['E'] = 5, ['F'] = 6, ['G'] = 7, ['H'] = 8, ['I'] = 9, ['J'] = 10,
    };

    /// <summary>C2SIM EchelonCode -> Unit Category, when the SIDC carries no echelon.</summary>
    public static readonly IReadOnlyDictionary<string, int> EchelonCodeCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["PLT"] = 3, ["COY"] = 5, ["BN"] = 6, ["RGT"] = 7, ["BDE"] = 8, ["DIV"] = 9, ["CORPS"] = 10,
    };

    /// <summary>Tried after a function's own candidates: infantry, mechanized infantry, armor.</summary>
    public static readonly int[] FallbackBranches = { 3, 4, 2 };

    /// <summary>The category a unit with neither a SIDC echelon nor a known EchelonCode is sized as.</summary>
    public const int DefaultCategory = 6;

    private static readonly (int Specific, int Extra)[] SpecificExtra = { (1, 0), (1, 1) };

    /// <summary>UG52 D.3.4 p1689-1690 abbreviations, as survey_magx.py prints them.</summary>
    public static string EchelonAbbr(int category) => category switch
    {
        0 => "OTHER", 1 => "VEH", 2 => "ELEM", 3 => "PLT", 4 => "BTY", 5 => "CO", 6 => "BN", 7 => "RGT", 8 => "BDE",
        9 => "DIV", 10 => "CORPS", 11 => "FORCE", 12 => "TEAM", 13 => "SQD", 14 => "SEC", -1 => "(any)",
        _ => "cat" + category.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// THE ONE DECISION. <paramref name="nationCode"/> is the DIS country of the unit's mapped row;
    /// <paramref name="sidc"/> / <paramref name="echelonCode"/> are the init's own. Pure apart from the catalogue.
    /// </summary>
    public static ContainerChoice Choose(ICatalogue catalogue, int nationCode, string sidc, string echelonCode)
    {
        var flags = new List<string>();
        string fid = UnitTypeMap.FunctionIdOf(sidc ?? "");
        char e = UnitTypeMap.EchelonCharOf(sidc ?? "");
        if (!EchelonCategory.TryGetValue(e, out int cat))
        {
            cat = EchelonCodeCategory.TryGetValue((echelonCode ?? "").Trim(), out int c2) ? c2 : DefaultCategory;
            string code = string.IsNullOrWhiteSpace(echelonCode) ? "-" : echelonCode.Trim();
            flags.Add($"no SIDC echelon ({code}) -> {EchelonAbbr(cat)}");
        }
        int[] own = BranchCandidates.TryGetValue(fid, out var b) ? b : Array.Empty<int>();
        var tried = new List<int>(own);
        foreach (int f in FallbackBranches) if (!tried.Contains(f)) tried.Add(f);
        if (catalogue != null)
        {
            for (int i = 0; i < tried.Count; i++)
            {
                foreach (var (spec, extra) in SpecificExtra)
                {
                    var t8 = new[] { 3, 11, 1, nationCode, cat, tried[i], spec, extra };
                    var hit = catalogue.Resolve(t8);
                    if (hit == null || hit.Role != CatalogueRole.Container) continue;
                    bool exact = own.Length > 0 && i == 0;
                    if (!exact)
                        flags.Add($"NEAREST branch (no {EchelonAbbr(cat)} container for {fid})");
                    return new ContainerChoice(true, t8, hit.DisplayName, exact, cat, tried[i], fid, flags);
                }
            }
        }
        flags.Add($"NO CONTAINER of nation {nationCode} at {EchelonAbbr(cat)} for {fid} (branches tried: " +
                  string.Join(", ", tried) + ")");
        return new ContainerChoice(false, null, "", false, cat, -1, fid, flags);
    }

    /// <summary>An 8-field type as the bridge's 7-field spec (the super-type is implied by the kind).</summary>
    public static VrfC2Sim.EntityTypeSpec SpecOf(int[] type8)
        => new() { Kind = type8[1], Domain = type8[2], Country = type8[3], Category = type8[4],
                   Subcategory = type8[5], Specific = type8[6], Extra = type8[7] };

    /// <summary>The bridge's 7-field spec as an 8-field type (super-type 3 for a kind-11 unit, 1 otherwise).</summary>
    public static int[] Type8Of(VrfC2Sim.EntityTypeSpec t)
        => new[] { t.Kind == 11 ? 3 : 1, t.Kind, t.Domain, t.Country, t.Category, t.Subcategory, t.Specific, t.Extra };

    /// <summary>
    /// One MEMBER of a population as a creation plan: its catalogue type, its unique name, at its (checked) slot,
    /// created AGGREGATED (UG52 Table 68 p1470: "4 Aggregated unit (a unit that does not have subordinate simulation
    /// objects)" - the vendor saves 335 of 335 warfare-model units so) with no subordinates of its own
    /// (AggregateLevelAggregate.ope:19 "can-have-subordinates False"). The service and --populate-selftest build it
    /// here, so the state the test asserts is the state the create sends.
    /// </summary>
    public static CreationPlan MemberPlan(PopulateMember member, VrfC2Sim.Force force, double latDeg, double lonDeg)
        => new(true, SpecOf(member.Leaf.ObjectType), force, 0.0, member.Name,
               new VrfC2Sim.Geodetic { LatDeg = latDeg, LonDeg = lonDeg, AltMeters = 0.0 }, null,
               TemplateName: member.Leaf.TemplateName)
           // C1d (RL-20260928-02): the member is created under the uuid its plan derived (PopulatePlanner.Plan).
           // LBL: and with its FULL designation as its vendor Label (PopulateMember.Label, "<container>.<suffix>").
           { CreateSubordinates = false, CreateAggregated = true, StartingUuid = member.Uuid ?? "", Label = member.Label ?? "" };

    /// <summary>
    /// The unit's creation plan AS A CONTAINER: the chosen container type and template, an EMPTY shell
    /// (CreateSubordinates=false, no members - RL-20260927-03) created DISAGGREGATED (the vendor saves 83 of 83
    /// containers so; UG52 Table 68 p1470 "3 Disaggregated unit (a unit composed of other simulation objects)"), at the
    /// plan's own (authored) position. The map row id is KEPT - it is the composition key (design sec 4.1 (3)). A
    /// container of the NEAREST branch is a PROXY for R-SURFACE-PROXY: what it misstates is the symbol, not a model.
    /// </summary>
    public static CreationPlan Apply(CreationPlan plan, ContainerChoice c)
    {
        if (c == null || !c.Found) return plan;
        string abbr = EchelonAbbr(c.Category);
        string flags = c.Flags.Count == 0 ? "" : "; " + string.Join("; ", c.Flags);
        string note = $"CONTAINER (RL-20260927-03): {c.TemplateName} ({c.TypeText}) by the init rule - echelon {abbr}, " +
                      $"branch {c.Branch} for {c.FunctionId}{flags}; the map row keys its composition";
        return plan with
        {
            Type = SpecOf(c.Type8),
            TemplateName = c.TemplateName,
            Fidelity = c.ExactBranch ? TypeFidelity.Exact : TypeFidelity.Proxy,
            Substitution = c.ExactBranch
                ? ""
                : $"PROXY: {c.TemplateName} - an Aggregate Container of the NEAREST branch: the catalogue has no {abbr} " +
                  $"container for {c.FunctionId} (the symbol is approximate; a container has no warfare model)",
            MapNote = string.IsNullOrEmpty(plan.MapNote) ? note : plan.MapNote + " " + note,
            CreateSubordinates = false,
            CreateAggregated = false,
        };
    }
}
