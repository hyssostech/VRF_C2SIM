using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VrfC2SimApp;

/// <summary>One entry of a composition row (data/unit-composition-52-aggregate.json "subordinates").</summary>
public sealed record CompositionEntry(string Function, int Count, string Role, string ObjectType, string TemplateName,
                                      string Compose, string Fidelity, string Note);

/// <summary>One row of the authored composition table: the sub-units a tasked container is populated with.
/// <paramref name="Variant"/> is the row's "variant" (package C2): "catalogue", "authored" or "all" - "" when the row
/// carries none (a C1-schema row, which belongs to every variant, like "all").</summary>
public sealed record CompositionRow(string Id, IReadOnlyList<string> MapRowIds, string ContainerObjectType,
                                    string ContainerTemplate, string Depth, IReadOnlyList<CompositionEntry> Subordinates,
                                    string Variant = "");

/// <summary>One declared VARIANT of the table ("variants"; package C2, RL-20260927-04): the model set its rows need,
/// that set's .sms and the fixture built on it - as the file writes them.</summary>
public sealed record CompositionVariantInfo(string Name, string ModelSet, string Sms, string Fixture, string Note);

/// <summary>
/// THE AUTHORED COMPOSITION TABLE (Vrf:CompositionFile, default data/unit-composition-52-aggregate.json; C1,
/// RL-20260927-02: "populate the containers, not proxies"; D-2 of RL-20260927-04: compose to doctrine, FM 3-96).
/// PURE: JSON in, rows out. The schema is the one tools/aggregate/composition_check.py gates offline; this reader
/// takes the fields it needs BY NAME and ignores every other one, so the parallel lane that authors the missing US
/// unit types (C2, feat/aggregate-authored-units) can ADD fields and rows without a code change. schemaVersion 1 only:
/// a different major is refused rather than guessed at.
/// VARIANTS (C1b; package C2): the file declares "variants" and a "defaultVariant", and each row a "variant". The
/// loaded table is the WHOLE file; the service narrows it to ONE variant (<see cref="ForVariant"/>, selected by
/// <see cref="CompositionVariants.Select"/>) before anything resolves against it.
/// </summary>
public sealed class CompositionTable
{
    public const string DefaultFile = "data/unit-composition-52-aggregate.json";

    public IReadOnlyList<CompositionRow> Rows { get; }
    public string SourcePath { get; }
    public string ModelSetKey { get; }

    /// <summary>The declared variants by name (case-insensitive); empty for a table that declares none (the C1 schema).</summary>
    public IReadOnlyDictionary<string, CompositionVariantInfo> Variants { get; }

    /// <summary>The file's "defaultVariant" ("" when it declares none).</summary>
    public string DefaultVariant { get; }

    /// <summary>The variant this table was narrowed to by <see cref="ForVariant"/>; "" = the whole file.</summary>
    public string SelectedVariant { get; }

    /// <summary>How many rows the FILE holds (a narrowed table keeps the count for the start-up line).</summary>
    public int FileRowCount { get; }

    private CompositionTable(IReadOnlyList<CompositionRow> rows, string path, string modelSetKey,
                             IReadOnlyDictionary<string, CompositionVariantInfo> variants, string defaultVariant,
                             string selectedVariant, int fileRowCount)
    {
        Rows = rows;
        SourcePath = path;
        ModelSetKey = modelSetKey;
        Variants = variants;
        DefaultVariant = defaultVariant;
        SelectedVariant = selectedVariant;
        FileRowCount = fileRowCount;
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
                rows.Add(new CompositionRow(Str(r, "id"), ids, cType, cTmpl, Str(r, "depth"), subs, Str(r, "variant")));
            }
        }
        var variants = new Dictionary<string, CompositionVariantInfo>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("variants", out var vs) && vs.ValueKind == JsonValueKind.Object)
            foreach (var p in vs.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.Object && !string.IsNullOrWhiteSpace(p.Name))
                    variants[p.Name.Trim()] = new CompositionVariantInfo(p.Name.Trim(), Str(p.Value, "modelSet"),
                                                                         Str(p.Value, "sms"), Str(p.Value, "fixture"),
                                                                         Str(p.Value, "note"));
        return new CompositionTable(rows, path, Str(root, "modelSetKey"), variants, Str(root, "defaultVariant"), "",
                                    rows.Count);
    }

    private static string Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "").Trim() : "";

    /// <summary>
    /// THE VARIANT'S TABLE (C1b): the rows of <paramref name="variant"/> plus the rows valid in every variant ("all", or
    /// a row with no variant field) - composition_check.py variant_rows. Everything downstream (ForMapRow, ById, the
    /// compose references) then sees ONE variant: a map row that each variant claims once is one claim, not a TABLE
    /// DEFECT, and a compose reference ACROSS variants names no row (the offline gate's own rule). Call it on the loaded
    /// (whole) table; <paramref name="variant"/> is a name <see cref="CompositionVariants.Select"/> accepted.
    /// </summary>
    public CompositionTable ForVariant(string variant)
    {
        string v = (variant ?? "").Trim();
        var rows = Rows.Where(r => CompositionVariants.RowBelongsTo(r.Variant, v)).ToList();
        return new CompositionTable(rows, SourcePath, ModelSetKey, Variants, DefaultVariant, v, FileRowCount);
    }

    /// <summary>Rows whose variant is neither "all" nor a declared variant (with no declaration: neither "all" nor
    /// "catalogue") - they belong to NO variant, so the start-up names each one instead of dropping it silently.</summary>
    public IReadOnlyList<string> UndeclaredRowVariants()
        => Rows.Where(r => !string.IsNullOrWhiteSpace(r.Variant)
                           && !string.Equals(r.Variant, CompositionVariants.AllVariants, StringComparison.OrdinalIgnoreCase)
                           && (Variants.Count > 0
                               ? !Variants.ContainsKey(r.Variant)
                               : !string.Equals(r.Variant, CompositionVariants.Catalogue, StringComparison.OrdinalIgnoreCase)))
               .Select(r => $"row {r.Id}: variant '{r.Variant}'")
               .ToList();

    public CompositionRow ById(string id)
        => string.IsNullOrEmpty(id) ? null : Rows.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.Ordinal));

    /// <summary>The rows that claim a type-map row id (CreationPlan.MapRowId). The FIRST is the one used; a second
    /// claimant is reported by <see cref="CompositionResolver.Resolve"/> as a table defect, not silently chosen.</summary>
    public IReadOnlyList<CompositionRow> ForMapRow(string mapRowId)
        => string.IsNullOrEmpty(mapRowId)
            ? Array.Empty<CompositionRow>()
            : Rows.Where(r => r.MapRowIds.Contains(mapRowId, StringComparer.Ordinal)).ToList();
}

/// <summary>
/// THE COMPOSITION VARIANT AND ITS DERIVED-SMS GUARD (C1b - the integration package C2 named in
/// docs/experiments/AGGREGATE_AUTHORED_UNITS_2026-09-27.md sec 10; RL-20260927-04, D-2 revised: author the US unit
/// types the catalogue lacks). PURE apart from reading the fixture archive; the service's start-up preflight calls it
/// and --populate-selftest (p13) pins it.
///
/// SELECTION: Vrf:CompositionVariant (default catalogue; blank = the file's defaultVariant) names ONE declared variant
/// and the resolver sees only that variant's rows plus the "all" rows (<see cref="CompositionTable.ForVariant"/>). A
/// name the file does not declare REFUSES the start - a misspelt variant must not quietly run another composition.
///
/// THE GUARD: rows with AUTHORED content - the "authored" variant, a variant whose declared model set is not the
/// table's own, an entry of fidelity AUTHORED, or an entry naming a type of the type map's authoredRows (the CONTENT
/// decides, not only the name) - run ONLY when the scenario the back end loads is on the DERIVED set
/// (C2SIM_AggregateTacticalLevel) AND the app's catalogue is rooted at that same .sms file. On the shipped
/// AggregateTacticalLevel.sms the authored types land EMPTY generic containers or the base abstract (C2's wrong-SMS
/// hazard, record sec 7), so the start is REFUSED - never a fallback to another variant or another catalogue.
///
/// THE FIXTURE'S SMS is read the way the runner's Stage 0 reads it (scripts/RunnerLib.ps1 Get-ScenarioModelSet, the
/// input of Test-ModelSetPairing): the scenario archive &lt;VrfHome&gt;\userData\scenarios\&lt;name&gt;.scnx (the
/// runner's $ScenarioScnxPath rule) or a rooted .scnx path, its first *.scn entry, and the (Simulation-Model-Set-Files
/// "...") line - the line the back end takes its model set from (UG52 13.7 p368: the model set is fixed per scenario).
/// WHY THE FILE AND NOT A RUNNER SETTING: the runner exports no setting that tells the derived set from the shipped one
/// - Vrf__ModelSet is the FAMILY, and Get-ModelSetFromSms follows the derived set's include to AggregateTacticalLevel -
/// and it does not export the scenario either. So the app is told WHICH scenario (Vrf:Scenario, the runner's -Scenario
/// value) and reads the SMS line itself. Unset or unreadable is UNKNOWN, and UNKNOWN never passes the guard.
/// </summary>
public static class CompositionVariants
{
    public const string Catalogue = "catalogue";
    public const string Authored = "authored";
    /// <summary>A row valid in every variant.</summary>
    public const string AllVariants = "all";
    /// <summary>The ruling every line of this feature names.</summary>
    public const string Ruling = "RL-20260927-04";
    /// <summary>Package C2's derived set (tools/sms/Deploy-C2SimAggregateSms.ps1) - the required set when the file's own
    /// variants name no other.</summary>
    public const string DerivedModelSet = "C2SIM_AggregateTacticalLevel";

    private const StringComparison Ci = StringComparison.OrdinalIgnoreCase;

    /// <summary>composition_check.py variant_rows: a row belongs to <paramref name="variant"/> when it is of that
    /// variant, of "all", or carries no variant (a C1-schema row).</summary>
    public static bool RowBelongsTo(string rowVariant, string variant)
    {
        string v = (rowVariant ?? "").Trim();
        return v.Length == 0 || string.Equals(v, AllVariants, Ci) || string.Equals(v, (variant ?? "").Trim(), Ci);
    }

    /// <summary>What <see cref="Select"/> decided: the declared variant name (the file's spelling), where the name came
    /// from, its declaration (null for the implicit variant of a table that declares none), or the refusal.</summary>
    public sealed record Selection(string Name, string Source, CompositionVariantInfo Info, string Refusal)
    {
        public bool Refused => Refusal != null;
    }

    /// <summary>Vrf:CompositionVariant -> ONE declared variant. Blank = the file's defaultVariant, else catalogue. A
    /// table that declares no variants (the C1 schema) has ONE implicit variant, catalogue, holding every row. Any
    /// other name is REFUSED, naming what the file declares.</summary>
    public static Selection Select(CompositionTable table, string setting)
    {
        string raw = (setting ?? "").Trim();
        string want, source;
        if (raw.Length > 0) { want = raw; source = "Vrf:CompositionVariant"; }
        else if (!string.IsNullOrWhiteSpace(table?.DefaultVariant))
        {
            want = table.DefaultVariant.Trim();
            source = "Vrf:CompositionVariant blank -> the file's defaultVariant";
        }
        else { want = Catalogue; source = "Vrf:CompositionVariant blank and no defaultVariant -> catalogue"; }
        string path = table?.SourcePath ?? "(no composition table)";
        if (table == null || table.Variants.Count == 0)
            return string.Equals(want, Catalogue, Ci)
                ? new Selection(Catalogue, source, null, null)
                : new Selection(want, source, null,
                    $"COMPOSITION VARIANT '{want}' ({source}) is not a variant of {path}: it declares no variants, so " +
                    $"its only variant is '{Catalogue}' (every row). REFUSING rather than guessing ({Ruling})");
        if (table.Variants.TryGetValue(want, out var info)) return new Selection(info.Name, source, info, null);
        return new Selection(want, source, null,
            $"COMPOSITION VARIANT '{want}' ({source}) is not a variant of {path}: it declares " +
            $"{string.Join(", ", table.Variants.Keys.OrderBy(k => k, StringComparer.Ordinal))}. REFUSING rather than " +
            $"running another composition ({Ruling})");
    }

    private static string NormType(string t)
        => UnitTypeMap.ParseObjectType(t) is int[] f ? string.Join(":", f) : (t ?? "").Trim();

    /// <summary>WHY the rows of a narrowed table need the derived set, or null when they do not - the guard's trigger.
    /// "" = the variant IS "authored" (nothing more to say); otherwise the reason: a declared model set other than the
    /// table's own, an AUTHORED entry, or an entry naming a type of the type map's authoredRows - a catalogue row that
    /// names an authored type needs the derived set just the same (composition_check.py forbids it offline; the app
    /// does not rely on that).</summary>
    public static string AuthoredContent(CompositionTable variantTable, Selection selection,
                                         IReadOnlyList<UnitTypeRow> authoredRows)
    {
        if (selection != null && string.Equals(selection.Name, Authored, Ci)) return "";
        var info = selection?.Info;
        if (info != null && info.ModelSet.Length > 0 && !string.Equals(info.ModelSet, variantTable?.ModelSetKey ?? "", Ci))
            return $"variant '{info.Name}' declares the model set {info.ModelSet}";
        var authoredTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var a in authoredRows ?? Array.Empty<UnitTypeRow>())
            if (!string.IsNullOrWhiteSpace(a.ObjectType)) authoredTypes.TryAdd(NormType(a.ObjectType), a.Id);
        foreach (var r in variantTable?.Rows ?? Array.Empty<CompositionRow>())
            foreach (var s in r.Subordinates)
            {
                if (string.Equals(s.Fidelity, "AUTHORED", Ci))
                    return $"row {r.Id} entry {s.Function} is fidelity AUTHORED";
                if (authoredTypes.TryGetValue(NormType(s.ObjectType), out var id))
                    return $"row {r.Id} entry {s.Function} names the authored type {s.ObjectType} ({id})";
            }
        return null;
    }

    /// <summary>The derived set the authored content needs: the selected variant's declared model set when it is not
    /// the table's own, else the file's "authored" variant's, else <see cref="DerivedModelSet"/>. Never the table's own
    /// (shipped) model set - a declaration that says so is ignored, not obeyed.</summary>
    public static (string Name, string Sms) RequiredDerivedSet(CompositionTable table, CompositionVariantInfo selected)
    {
        string own = table?.ModelSetKey ?? "";
        if (selected != null && selected.ModelSet.Length > 0 && !string.Equals(selected.ModelSet, own, Ci))
            return (selected.ModelSet, selected.Sms);
        if (table != null && table.Variants.TryGetValue(Authored, out var a) && a.ModelSet.Length > 0
            && !string.Equals(a.ModelSet, own, Ci))
            return (a.ModelSet, a.Sms);
        return (DerivedModelSet, "");
    }

    /// <summary>What the fixture archive says (RunnerLib.ps1 Get-ScenarioModelSet's Found / Readable / Sms / Via).</summary>
    public sealed record FixtureSmsReading(string Setting, string ScnxPath, bool Found, bool Readable, string Sms, string Via)
    {
        public bool Known => Found && Readable;
    }

    /// <summary>The scenario archive a Vrf:Scenario value names: a rooted path as given, else
    /// &lt;vrfHome&gt;\userData\scenarios\&lt;name&gt;.scnx - the runner's $ScenarioScnxPath rule ("" when unset).</summary>
    public static string ScenarioArchivePath(string scenarioSetting, string vrfHome)
    {
        string s = (scenarioSetting ?? "").Trim();
        if (s.Length == 0) return "";
        if (Path.IsPathRooted(s)) return s;
        return Path.Combine(vrfHome ?? "", "userData", "scenarios", s.EndsWith(".scnx", Ci) ? s : s + ".scnx");
    }

    /// <summary>PORT of RunnerLib.ps1 Get-ScenarioModelSet: the archive's first *.scn entry and its
    /// (Simulation-Model-Set-Files "...") line, by the runner's own regex. Never throws.</summary>
    public static FixtureSmsReading ReadFixtureSms(string scenarioSetting, string vrfHome)
    {
        string s = (scenarioSetting ?? "").Trim();
        if (s.Length == 0) return new FixtureSmsReading(s, "", false, false, "", "Vrf:Scenario is not set");
        string path = ScenarioArchivePath(s, vrfHome);
        if (!File.Exists(path)) return new FixtureSmsReading(s, path, false, false, "", "no such file");
        try
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".scn", Ci));
            if (entry == null) return new FixtureSmsReading(s, path, true, false, "", "no .scn inside the archive");
            string scn;
            using (var sr = new StreamReader(entry.Open())) scn = sr.ReadToEnd();
            var m = Regex.Match(scn, "\\(Simulation-Model-Set-Files\\s+\"([^\"]*)\"\\s*\\)");
            if (!m.Success)
                return new FixtureSmsReading(s, path, true, false, "", "the .scn has no Simulation-Model-Set-Files line");
            return new FixtureSmsReading(s, path, true, true, m.Groups[1].Value, "Simulation-Model-Set-Files of " + entry.FullName);
        }
        catch (Exception ex)
        {
            return new FixtureSmsReading(s, path, true, false, "", "unreadable: " + ex.Message);
        }
    }

    /// <summary>The model-set NAME of an SMS string - its file name without .sms, as Get-ModelSetFromSms reads the leaf:
    /// "C:\C2SIM\vrf-sms\C2SIM_AggregateTacticalLevel.sms" -> C2SIM_AggregateTacticalLevel.</summary>
    public static string SmsModelSetName(string sms)
    {
        string leaf = (sms ?? "").Replace('/', '\\').Split('\\').Last().Trim();
        return leaf.EndsWith(".sms", Ci) ? leaf[..^4] : leaf;
    }

    /// <summary>The file an SMS string names on this machine: $(DATA_DIR) is &lt;vrfHome&gt;\data (the vendor macro;
    /// build_fixture.py expand_vendor_macros), and a relative path is relative to the executable's directory,
    /// &lt;vrfHome&gt;\bin64 (UG52 Table 15 p271).</summary>
    public static string SmsFilePath(string sms, string vrfHome)
    {
        string p = (sms ?? "").Trim().Replace("$(DATA_DIR)", Path.Combine(vrfHome ?? "", "data"), Ci);
        if (p.Length == 0) return "";
        if (!Path.IsPathRooted(p)) p = Path.Combine(vrfHome ?? "", "bin64", p);
        try { return Path.GetFullPath(p); } catch { return p; }
    }

    /// <summary>The .sms file a catalogue root names (ObjectTypeResolver.RootSms): a ROOTED path as given, else a
    /// model-set NAME in &lt;vrfHome&gt;\data\simulationModelSets.</summary>
    public static string CatalogueSmsPath(string rootSms, string vrfHome)
    {
        string r = (rootSms ?? "").Trim();
        if (r.Length == 0) return "";
        if (!Path.IsPathRooted(r))
            r = Path.Combine(ObjectTypeResolver.ModelSetsDir(vrfHome ?? ""), (r.EndsWith(".sms", Ci) ? r[..^4] : r) + ".sms");
        try { return Path.GetFullPath(r); } catch { return r; }
    }

    /// <summary>One line for a fixture reading: what it loads, or why it is UNKNOWN.</summary>
    public static string Describe(FixtureSmsReading f)
    {
        if (f == null || string.IsNullOrEmpty(f.Setting)) return "Vrf:Scenario is not set";
        if (!f.Found) return $"Vrf:Scenario '{f.Setting}' -> {f.ScnxPath}: no such file";
        if (!f.Readable) return $"{f.ScnxPath}: {f.Via}";
        return $"the fixture {f.ScnxPath} loads {f.Sms}";
    }

    /// <summary>The guard's verdict. <see cref="Why"/> is null when the rows need no derived set.</summary>
    public sealed record Verdict(string Why, string RequiredName, string RequiredSms, string Refusal)
    {
        public bool NeedsDerivedSet => Why != null;
        public bool Refused => Refusal != null;
    }

    /// <summary>
    /// THE GUARD. Rows with authored content (<see cref="AuthoredContent"/>) pass only when (1) the fixture's SMS is
    /// KNOWN, (2) it is the required derived set (by model-set name), and (3) the app's catalogue is rooted at the SAME
    /// .sms file - otherwise the start is REFUSED, the text beginning "COMPOSITION VARIANT &lt;name&gt; needs the
    /// derived SMS ...". Rows with no authored content need nothing: the derived set only ADDS types
    /// (typemap_check.py), so the catalogue variant runs on the shipped or the derived SMS alike.
    /// </summary>
    public static Verdict Check(CompositionTable variantTable, Selection selection, IReadOnlyList<UnitTypeRow> authoredRows,
                                FixtureSmsReading fixture, string catalogueRootSms, string vrfHome)
    {
        string why = AuthoredContent(variantTable, selection, authoredRows);
        if (why == null) return new Verdict(null, "", "", null);
        var (name, sms) = RequiredDerivedSet(variantTable, selection?.Info);
        string head = $"COMPOSITION VARIANT {selection?.Name ?? "(none)"} needs the derived SMS {name}" +
                      (string.IsNullOrEmpty(sms) ? "" : $" ({sms})") + (why.Length > 0 ? $" ({why})" : "");
        if (fixture == null || !fixture.Known)
            return new Verdict(why, name, sms,
                $"{head}, but the fixture's SMS is UNKNOWN ({Describe(fixture)}). Name the scenario the back end loads in " +
                "Vrf:Scenario (the runner's -Scenario value, or a rooted .scnx path) - it is read like the runner's Stage 0 " +
                $"(RunnerLib.ps1 Get-ScenarioModelSet), and an unverified SMS never passes ({Ruling})");
        if (!string.Equals(SmsModelSetName(fixture.Sms), name, Ci))
            return new Verdict(why, name, sms,
                $"{head}, but the fixture {fixture.ScnxPath} loads {fixture.Sms}: there the authored types land EMPTY " +
                "generic containers or the base abstract (the wrong-SMS hazard, docs/experiments/" +
                $"AGGREGATE_AUTHORED_UNITS_2026-09-27.md sec 7). Run it on a fixture built on {name} " +
                $"(IronStorm_Centre_52_Aggregate_C2SIM), or set Vrf:CompositionVariant={Catalogue} ({Ruling})");
        string fixtureFile = SmsFilePath(fixture.Sms, vrfHome);
        string catalogueFile = CatalogueSmsPath(catalogueRootSms, vrfHome);
        if (!string.Equals(fixtureFile, catalogueFile, Ci))
            return new Verdict(why, name, sms,
                $"{head}: the fixture {fixture.ScnxPath} loads it ({fixture.Sms}), but the app's catalogue is rooted at " +
                $"{(string.IsNullOrWhiteSpace(catalogueRootSms) ? "(nothing - it did not load)" : catalogueRootSms)}: the " +
                "container rule and the composition would be resolved against a set the simulator does not load. Set " +
                $"Vrf:CatalogueSms={fixtureFile} ({Ruling})");
        return new Verdict(why, name, sms, null);
    }

    /// <summary>The TYPE MAP line of one authoredRows type - the init's "TYPE MAP {Fidelity}: ..." shape, printing the
    /// PARSED fidelity - with what the catalogue lands for it. Ok = fidelity Authored and it lands its OWN warfare-model
    /// UNIT (the template it names).</summary>
    public static (bool Ok, string Text) AuthoredTypeLine(UnitTypeRow a, CatalogueEntry hit, string catalogueRoot)
    {
        bool ok = a != null && a.Fidelity == TypeFidelity.Authored && hit != null && hit.Role == CatalogueRole.Unit
                  && (string.Equals(hit.DisplayName, a.TemplateName, StringComparison.Ordinal)
                      || string.Equals(hit.Name, a.TemplateName, StringComparison.Ordinal));
        string lands = hit == null ? "NOTHING" : $"'{hit.DisplayName}', a {hit.Role}";
        return (ok, $"TYPE MAP {a?.Fidelity}: {a?.Id} -> {a?.TemplateName} ({a?.ObjectType}) [authoredRows; {a?.NationRole} " +
                    $"{a?.Nation} {a?.EchelonCode}] lands {lands} in the catalogue rooted at {catalogueRoot}" +
                    (ok ? " - its own warfare-model UNIT, which the authored composition variant creates."
                        : " - NOT its own UNIT: a composition leaf of this type is REFUSED (the catalogue is not the derived " +
                          "set tools/sms/Deploy-C2SimAggregateSms.ps1 builds, or the row is not AUTHORED)."));
    }

    /// <summary>THE START-UP LINE: the variant and where it came from, its rows, the SMS the fixture loads, the
    /// catalogue, the guard's verdict and the ruling.</summary>
    public static string StartupLine(Selection selection, CompositionTable variantTable, FixtureSmsReading fixture,
                                     string catalogueRoot, Verdict verdict, int authoredRowCount)
    {
        string rows = variantTable == null ? "no table"
            : $"{variantTable.Rows.Count} of the {variantTable.FileRowCount} row(s) of {variantTable.SourcePath} (its own " +
              $"and the '{AllVariants}' rows: {string.Join(", ", variantTable.Rows.Select(r => r.Id))})";
        string guard = verdict == null || !verdict.NeedsDerivedSet
            ? "It needs no derived SMS: catalogue units only, which resolve alike on the shipped and the derived set."
            : verdict.Refused
                ? "It needs the derived SMS " + verdict.RequiredName + " and the guard REFUSED the start (see the refusal)."
                : $"It needs the derived SMS {verdict.RequiredName}" +
                  (verdict.Why.Length > 0 ? $" ({verdict.Why})" : "") +
                  ": the fixture loads it and the catalogue is rooted at the same file.";
        string authored = verdict != null && verdict.NeedsDerivedSet
            ? $"{authoredRowCount} authoredRows type(s) read with fidelity {TypeFidelity.Authored} and IN USE (the TYPE MAP " +
              "Authored lines)."
            : $"{authoredRowCount} authoredRows type(s) in the type map, NOT in use (they serve the '{Authored}' variant only).";
        return $"COMPOSITION VARIANT {selection?.Name} ({selection?.Source}; {Ruling}): {rows}. SMS: {Describe(fixture)}. " +
               $"Catalogue: {(string.IsNullOrWhiteSpace(catalogueRoot) ? "(not loaded)" : catalogueRoot)}. {guard} {authored}";
    }
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
        {
            // C1b: rows of DIFFERENT variants claiming one map row mean the table was never narrowed to one variant
            // (the C2 data read variant-blind) - said as such, still refused rather than silently taking the first.
            bool mixed = rows.Select(r => (r.Variant ?? "").Trim().ToLowerInvariant()).Distinct().Count() > 1;
            return PopulatePlan.Refuse(PopulateSource.Table, rows[0].Id,
                $"TABLE DEFECT: map row '{mapRowId}' is claimed by {rows.Count} composition rows " +
                $"({string.Join(", ", rows.Select(r => string.IsNullOrEmpty(r.Variant) ? r.Id : $"{r.Id} [{r.Variant}]"))}) - " +
                (mixed && string.IsNullOrEmpty(table.SelectedVariant)
                    ? "rows of DIFFERENT variants: the table was not narrowed to one variant (Vrf:CompositionVariant, " +
                      "CompositionTable.ForVariant)"
                    : "one row per map row"));
        }
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

/// <summary>One planned member of a population: its unique name and its slot on the ring. NameNote is null for the
/// plain "container.suffix" name and says why otherwise (C1c: the ~k tag that kept it unique within 30).</summary>
public sealed record PopulateMember(int Slot, string Name, PopulateLeaf Leaf, double NorthMeters, double EastMeters,
                                    double LatDeg, double LonDeg, double BearingDeg, string NameNote = null);

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
    /// <summary>C1c (2026-09-28): the longest member name - 30 characters (<see cref="VrfNames.AggregateMarkingChars"/>),
    /// which fits the 31-character aggregate marking field and so comes back from VR-Forces EXACTLY (a longer aggregate
    /// name comes back as its first 30, 107 of 107). It was 34 (VrfC2SimService.MaxVrfMarkingChars, a DtUUID reference
    /// limit), and run G1 lost 22 of its 23 members to names that agreed in their first 30 characters.</summary>
    public const int MaxNameChars = VrfNames.AggregateMarkingChars;

    /// <summary>A leaf of this echelon rank or below sizes the ring (composition_check.py SIZING_RANK_MAX = CO).</summary>
    public static readonly int SizingRankMax = CompositionResolver.Rank(5) ?? 4;

    private const double MetersPerDegLat = 111_320.0;   // DeStacker's constant

    /// <summary>"&lt;container, cut&gt;[~k].&lt;suffix&gt;" within <see cref="MaxNameChars"/> (VrfNames.ChildName) - the
    /// shape of VrfC2SimService.MakeChildName, so a member reads as its container's child in every log. The container
    /// is cut to (30 - 1 - suffix length), so the member's WHOLE name survives and VR-Forces returns it exactly.</summary>
    public static string MemberName(string container, string suffix, int disambiguator = 0)
        => VrfNames.ChildName(container, suffix, disambiguator);

    /// <summary>
    /// The ring and the member names. <paramref name="conflictOf"/> (C1c) is the run's own name check - the service
    /// passes NameRegistry.KeyConflict - and returns why a candidate name is taken, or null. Every member name is
    /// at most 30 characters, unique among the members, not the container's own 30-character marking, and cleared by
    /// <paramref name="conflictOf"/>; a candidate that is not takes the first free ~k tag (k = 2 .. 99), deterministic
    /// for the same run state, and the population is REFUSED (NAME COLLISION) only when none is free.
    /// </summary>
    public static PopulateLayout Plan(string containerName, double anchorLat, double anchorLon,
                                      IReadOnlyList<PopulateLeaf> leaves, double rotationDeg,
                                      Func<string, string> conflictOf = null)
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
        string containerKey = VrfNames.Key(containerName);
        for (int k = 0; k < leaves.Count; k++)
        {
            var (north, east) = DeStacker.EqualBearingOffset(k, leaves.Count, radius, rotationDeg);
            string name = VrfNames.UniqueChildName(containerName, leaves[k].Suffix, candidate =>
                names.Contains(candidate) ? "is another member's name"
                : string.Equals(candidate, containerKey, StringComparison.Ordinal)
                    ? "is the container's own 30-character marking"
                    : conflictOf?.Invoke(candidate) is string c ? "collides with '" + c + "' within 30 characters"
                    : null,
                out int tag, out string why);
            if (name == null)
                return new PopulateLayout(Array.Empty<PopulateMember>(), spacing, radius, reach,
                    $"NAME COLLISION: member '{leaves[k].Suffix}' of '{containerName}' has no name unique within " +
                    $"{MaxNameChars} characters (the ~2..~{VrfNames.MaxDisambiguator} tags included): {why}");
            names.Add(name);
            double bearing = ((rotationDeg + 360.0 * k / leaves.Count) % 360.0 + 360.0) % 360.0;
            members.Add(new PopulateMember(k, name, leaves[k], north, east,
                                           anchorLat + north / MetersPerDegLat, anchorLon + east / metersPerDegLon,
                                           radius > 0.0 ? bearing : 0.0,
                                           tag > 1 ? $"tag {VrfNames.Tag(tag)}: the plain name {why}" : null));
        }
        return new PopulateLayout(members, spacing, radius, reach, null);
    }
}
