using System.Globalization;
using System.Text.RegularExpressions;
using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// `--populate-selftest` - POPULATED AGGREGATE CONTAINERS (C1; RL-20260927-02, RL-20260927-03, RL-20260927-04;
/// docs/DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md secs 3-8). No VR-Forces, no federation, nothing sent: the INSTALLED
/// aggregate catalogue is READ (C:\MAK\vrforces5.2d, or VRF_HOME / MAK_VRFDIR), like --typemap-selftest, and the
/// bridge assembly is loaded for its value types and its offline scripted-task round trip - so the MAK bin dirs must
/// be on PATH.
///
///   (p1)  the composition table resolves, in the installed catalogue, to catalogue UNITS - the gate's pinned counts
///   (p2)  PRECEDENCE: the STP TO, then the template's configured members, then the authored table; none = refused
///   (p3)  FLATTENING (D-8): nested rows and CATALOGUE sub-containers become their leaves, in ORBAT order
///   (p4)  REFUSAL OF UNKNOWN TYPES and every other composition defect, each with its own reason
///   (p5)  A NEW ROW (the C2 lane's authored types) is accepted with no code change; a new schema major is refused
///   (p6)  THE RING: flat 48 IBCT = 17 on one ring (180 m spacing, r 490 m), centroid on the anchor, unique names
///         within 34 chars, HQ in slot 0; states by role - members AGGREGATED, containers DISAGGREGATED
///   (p7)  THE INIT RULE on all 36 created Iron Storm units: 36 containers, pinned types (composition_check.py
///         --init-census), 7 of the NEAREST branch, 2 sized from the EchelonCode
///   (p8)  THE IN-PLACE POPULATION against a FAKE BRIDGE: create -> attach (planned order) -> the publication gate ->
///         the scripted task; never a delete; no members = refused; the timeout; the partial attach; the STP TO
///   (p9)  D-6: a short vendor completion is WITHHELD, a real one handed on
///   (p10) THE ENTITY-LEVEL PATH UNCHANGED: plans, states and census as before, and the source guards that keep it so
///   (p11) THE SCRIPTS: ids and variables re-read from the vendor XML; the bridge binds their types
///   (p12) THE PUBLICATION READER and the start-up arithmetic
///   (p13) C1b - THE COMPOSITION VARIANT AND THE DERIVED-SMS GUARD (package C2's integration, RL-20260927-04): one
///         variant selected (unknown = refused), the fixture's SMS read like the runner's Stage 0, the guard both ways
///         and its bypass controls, the type map's authoredRows read as fidelity Authored, the start-up line
/// VARIANTS (C1b): the composition file holds a "catalogue" and an "authored" variant. p1 walks the SELECTED variant
/// (`--populate-selftest --variant authored`; default = the file's defaultVariant, catalogue) on ITS catalogue - the
/// installed vendor set for catalogue, the DERIVED set for authored (`--derived-sms PATH`, else env
/// C2SIM_AGGREGATE_SMS, else the file's authored "sms") - and says SKIPPED for the other variant's rows. p2-p12 are
/// C1's mechanism checks on the catalogue variant and the vendor set whatever is selected; p13's derived-set checks run
/// only when authored is selected, and FAIL then if the derived set is absent or its types do not resolve.
/// </summary>
public static class ContainerSelfTest
{
    private static int _fail;
    private static int _pass;
    private static int _skip;

    public static int Run()
    {
        Console.WriteLine("=== populated aggregate containers self-test (C1; RL-20260927-02, RL-20260927-03, RL-20260927-04) ===");
        string repo = FindRepoRoot();
        Check(repo != null, "the repository root is found (data/COA-STP1_Order.xml above the app)", AppContext.BaseDirectory);
        if (repo == null) return Finish();
        string home = Environment.GetEnvironmentVariable("VRF_HOME") is { Length: > 0 } h ? h
                    : Environment.GetEnvironmentVariable("MAK_VRFDIR") is { Length: > 0 } m ? m
                    : @"C:\MAK\vrforces5.2d";
        var res = ObjectTypeResolver.LoadModelSetChain(home, UnitPositionPolicy.AggregateModelSet);
        Check(res != null && res.Templates.Count > 400,
              "the INSTALLED aggregate catalogue loads, rooted EXACTLY at AggregateTacticalLevel (no fallback)",
              res == null ? "not found under " + home : $"{res.RootSms}: {res.Templates.Count} templates, dirs [{string.Join("; ", res.ModelSetDirs)}]");
        Check(ObjectTypeResolver.LoadModelSetChain(home, "NoSuchModelSet") == null,
              "a model set that does not exist loads NOTHING - never a silently different catalogue");
        if (res == null) return Finish();
        var cat = new ResolverCatalogue(res);
        // C1b: the file is read WHOLE and narrowed to ONE variant, exactly as the service's start-up does.
        var full = CompositionTable.Load(Path.Combine(repo, "data", "unit-composition-52-aggregate.json"));
        var table = full.ForVariant(CompositionVariants.Catalogue);
        var selected = CompositionVariants.Select(full, ArgAfter("--variant") ?? "");
        bool authoredSelected = !selected.Refused && string.Equals(selected.Name, CompositionVariants.Authored,
                                                                   StringComparison.OrdinalIgnoreCase);
        string derivedSms = ArgAfter("--derived-sms")
                            ?? (Environment.GetEnvironmentVariable("C2SIM_AGGREGATE_SMS") is { Length: > 0 } e ? e : null)
                            ?? (CompositionVariants.RequiredDerivedSet(full, null).Sms is { Length: > 0 } s ? s
                                : @"C:\C2SIM\vrf-sms\" + CompositionVariants.DerivedModelSet + ".sms");
        ResolverCatalogue derived = null;
        if (authoredSelected)
        {
            var dres = File.Exists(derivedSms) ? ObjectTypeResolver.LoadModelSetChain(home, derivedSms) : null;
            Check(dres != null && dres.Templates.Count > res.Templates.Count,
                  $"variant authored selected: the DERIVED catalogue loads from {derivedSms} (it only ADDS to the vendor set)",
                  dres == null ? "absent - deploy it: tools/sms/Deploy-C2SimAggregateSms.ps1"
                               : $"{dres.RootSms}: {dres.Templates.Count} templates vs {res.Templates.Count}");
            if (dres != null) derived = new ResolverCatalogue(dres);
        }

        P1(authoredSelected ? derived : cat, full, selected, authoredSelected ? derivedSms : "the installed vendor set");
        P2();
        P3();
        P4(cat, table);
        P5();
        var ibct = P6(cat, table);
        P7(cat, repo);
        P8(ibct);
        P9();
        P10(repo, cat);
        P11(home);
        P12(repo);
        P13(repo, home, cat, full, selected, derived, derivedSms);
        return Finish();
    }

    // ------------------------------------------------------------------------------------------------ (p1) ----
    private static void P1(ResolverCatalogue cat, CompositionTable full, CompositionVariants.Selection selected, string where)
    {
        Console.WriteLine($"--- (p1) the composition table, variant {selected.Name}, in {where} ---");
        if (selected.Refused || cat == null)
        {
            Check(false, "p1 has a declared variant and its catalogue", selected.Refusal ?? "the catalogue did not load");
            return;
        }
        var table = full.ForVariant(selected.Name);
        Check(table.Rows.Count >= 5 && table.ModelSetKey == UnitPositionPolicy.AggregateModelSet,
              "data/unit-composition-52-aggregate.json loads (schema 1, modelSetKey AggregateTacticalLevel)",
              $"{table.Rows.Count} rows, modelSetKey '{table.ModelSetKey}'");
        // composition_check.py pins: C1's catalogue rows, the 'all' rows (the same in both variants) and C2's authored rows.
        var pinned = new Dictionary<string, (int Leaves, int Flattened)>
        {
            ["C-USA-DIV-UCI"] = (1, 0), ["C-USA-BDE-UCI"] = (17, 3), ["C-USA-BN-UCI"] = (5, 0),
            ["C-USA-BDE-UCA"] = (26, 6), ["C-USA-BN-UCIZ"] = (8, 1),
            ["C-USA-DIV-UCI-A"] = (1, 0), ["C-USA-BDE-UCI-A"] = (8, 0), ["C-USA-BDE-UCA-A"] = (29, 6),
        };
        foreach (var other in full.Rows.Where(r => !CompositionVariants.RowBelongsTo(r.Variant, selected.Name)))
            Skip($"row {other.Id} [{other.Variant}] is not a row of variant {selected.Name}",
                 other.Variant.Equals(CompositionVariants.Authored, StringComparison.OrdinalIgnoreCase)
                     ? "not applicable: its AUTHORED types exist only in the derived set - run --populate-selftest --variant authored"
                     : "not applicable to this variant - run --populate-selftest --variant " + other.Variant);
        foreach (var row in table.Rows)
        {
            var c8 = UnitTypeMap.ParseObjectType(row.ContainerObjectType);
            var ce = c8 != null ? cat.Resolve(c8) : null;
            Check(ce != null && ce.Role == CatalogueRole.Container,
                  $"row {row.Id}: its container {row.ContainerObjectType} lands a CONTAINER", ce?.DisplayName ?? "nothing");
            var p = CompositionResolver.ExpandRow(row, table, cat, ce != null ? CompositionResolver.Rank(ce.Category) : null);
            Check(!p.Refused, $"row {row.Id} resolves against the catalogue (no unknown type)", p.Refusal ?? "");
            bool allUnits = p.Leaves.All(l => cat.Resolve(l.ObjectType)?.Role == CatalogueRole.Unit);
            Check(!p.Refused && allUnits, $"row {row.Id}: every leaf lands a catalogue warfare-model UNIT",
                  string.Join(", ", p.Leaves.Select(l => l.TemplateName).Distinct()));
            if (pinned.TryGetValue(row.Id, out var want))
                Check(p.Leaves.Count == want.Leaves && p.SubContainersFlattened == want.Flattened,
                      $"row {row.Id} expands to {want.Leaves} leaf unit(s) and {want.Flattened} sub-container(s) (composition_check.py pins)",
                      $"got {p.Leaves.Count} and {p.SubContainersFlattened}");
        }
    }

    // ------------------------------------------------------------------------------------------------ (p2) ----
    private static readonly int[] TUnitA = { 3, 11, 1, 225, 5, 4, 0, 0 };       // CO - a warfare-model unit
    private static readonly int[] TUnitB = { 3, 11, 1, 225, 5, 2, 0, 0 };       // CO
    private static readonly int[] THq = { 3, 11, 1, 225, 5, 5, 1, 30 };         // CO
    private static readonly int[] TBnUnit = { 3, 11, 1, 225, 6, 5, 1, 30 };     // BN - a unit
    private static readonly int[] TBnBox = { 3, 11, 1, 225, 6, 3, 1, 0 };       // BN - a container
    private static readonly int[] TBdeBox = { 3, 11, 1, 225, 8, 3, 1, 1 };      // BDE - a container
    private static readonly int[] TBdeCat = { 3, 11, 1, 225, 8, 2, 1, 0 };      // BDE - a container WITH members
    private static readonly int[] TEmptyBox = { 3, 11, 1, 225, 6, 4, 1, 0 };    // BN - a container with NO members
    private static readonly int[] TAbstract = { 3, 11, 1, 225, 5, 3, 1, 75 };   // lands the base abstract
    private static readonly int[] TAuthored = { 3, 11, 1, 225, 6, 3, 0, 500 };  // a NEW authored US unit (C2)

    private static SyntheticCatalogue Synth()
    {
        var s = new SyntheticCatalogue();
        s.Add(TUnitA, "Mech CO", CatalogueRole.Unit, 90);
        s.Add(TUnitB, "Tank CO", CatalogueRole.Unit, 150);
        s.Add(THq, "HHT", CatalogueRole.Unit, 90);
        s.Add(TBnUnit, "Cav SQDN", CatalogueRole.Unit, 600);
        s.Add(TBnBox, "BN box", CatalogueRole.Container, null);
        s.Add(TBdeBox, "BDE box", CatalogueRole.Container, null);
        s.Add(TBdeCat, "BDE with members", CatalogueRole.Container, null,
              new SubordinateSpec(THq, "HQ"), new SubordinateSpec(TUnitB, "TANK"), new SubordinateSpec(TUnitA, "MECH"));
        s.Add(TEmptyBox, "empty BN box", CatalogueRole.Container, null);
        s.Add(TAbstract, "base-sim-aggregate", CatalogueRole.BaseAbstract, null);
        return s;
    }

    private static string T(int[] t8) => string.Join(":", t8);

    private static string Json(string rows, string extraTop = "")
        => "{ \"schemaVersion\": 1, \"modelSetKey\": \"AggregateTacticalLevel\"" + extraTop + ", \"rows\": [" + rows + "] }";

    private static string Row(string id, string maps, string container, string subs)
        => $"{{ \"id\": \"{id}\", \"mapRowIds\": [{maps}], \"container\": {{ \"objectType\": \"{container}\", \"templateName\": \"x\" }}, " +
           $"\"subordinates\": [{subs}] }}";

    private static string Sub(string fn, int count, string role, int[] type, string compose = null)
        => $"{{ \"function\": \"{fn}\", \"count\": {count}, \"role\": \"{role}\", \"objectType\": \"{T(type)}\", " +
           $"\"templateName\": \"t\"{(compose == null ? "" : $", \"compose\": \"{compose}\"")} }}";

    private static void P2()
    {
        Console.WriteLine("--- (p2) precedence: the STP TO -> the template's members -> the authored table ---");
        var s = Synth();
        var t = CompositionTable.Parse(Json(
            Row("R-BDE", "\"F-X-H\"", T(TBdeBox), Sub("HQ", 1, "UNIT", THq) + "," + Sub("TANK", 2, "UNIT", TUnitB))));
        var withMembers = s.Resolve(TBdeCat);
        var plain = s.Resolve(TBdeBox);
        var p1 = CompositionResolver.Resolve("U", withMembers, "F-X-H", new[] { "child-a", "child-b" }, t, s);
        Check(p1.Source == PopulateSource.StpTo && p1.ExistingChildren.Count == 2 && p1.Leaves.Count == 0,
              "(1) declared STP TO subordinates in the init WIN - attached, never re-created, even with template members and a row",
              $"{p1.Source}, children {p1.ExistingChildren.Count}, leaves {p1.Leaves.Count}");
        var p2 = CompositionResolver.Resolve("U", withMembers, "F-X-H", null, t, s);
        Check(p2.Source == PopulateSource.Catalogue && p2.Leaves.Count == 3 && !p2.Refused,
              "(2) no TO subordinates: the container template's configured members WIN over the table row",
              $"{p2.Source}, {p2.Leaves.Count} leaves");
        var p3 = CompositionResolver.Resolve("U", plain, "F-X-H", null, t, s);
        Check(p3.Source == PopulateSource.Table && p3.RowId == "R-BDE" && p3.Leaves.Count == 3,
              "(3) a template with no members: the authored table by the type-map row id", $"{p3.Source} {p3.RowId} {p3.Leaves.Count}");
        var p4 = CompositionResolver.Resolve("U", plain, "F-NONE", null, t, s);
        Check(p4.Refused && p4.Source == PopulateSource.None && p4.Refusal.Contains("NO COMPOSITION"),
              "none of the three: REFUSED - the container stays empty and its moves are refused", p4.Refusal ?? "");
        var dup = CompositionTable.Parse(Json(
            Row("R1", "\"F-X-H\"", T(TBdeBox), Sub("HQ", 1, "UNIT", THq)) + "," +
            Row("R2", "\"F-X-H\"", T(TBdeBox), Sub("HQ", 1, "UNIT", THq))));
        var p5 = CompositionResolver.Resolve("U", plain, "F-X-H", null, dup, s);
        Check(p5.Refused && p5.Refusal.Contains("TABLE DEFECT"),
              "two rows claiming one map row is a TABLE DEFECT, not a silent choice", p5.Refusal ?? "");
    }

    // ------------------------------------------------------------------------------------------------ (p3) ----
    private static void P3()
    {
        Console.WriteLine("--- (p3) FLAT (D-8): nested rows and CATALOGUE sub-containers become their leaves ---");
        var s = Synth();
        var t = CompositionTable.Parse(Json(
            Row("R-BDE", "\"F-X-H\"", T(TBdeBox),
                Sub("HQ", 1, "UNIT", THq) + "," + Sub("INF", 2, "CONTAINER", TBnBox, "R-BN") + "," +
                Sub("CAT", 1, "CONTAINER", TBdeCat, "CATALOGUE")) + "," +
            Row("R-BN", "", T(TBnBox), Sub("RIF", 2, "UNIT", TUnitA))));
        var p = CompositionResolver.ExpandRow(t.ById("R-BDE"), t, s, CompositionResolver.Rank(8));
        string order = string.Join(",", p.Leaves.Select(l => l.Suffix));
        Check(!p.Refused && p.Leaves.Count == 8 && p.SubContainersFlattened == 3,
              "HQ + 2 BN of 2 + a CATALOGUE box of 3 = 8 leaves, 3 sub-containers flattened", $"{p.Leaves.Count} / {p.SubContainersFlattened} {p.Refusal}");
        Check(order == "HQ1,INF1RIF1,INF1RIF2,INF2RIF1,INF2RIF2,CAT1HQ1,CAT1TANK2,CAT1MECH3",
              "the leaves come in ORBAT (depth-first, declared) order with unique path suffixes", order);
        Check(p.Leaves.Select(l => l.Suffix).Distinct().Count() == p.Leaves.Count, "every suffix is unique");
    }

    // ------------------------------------------------------------------------------------------------ (p4) ----
    private static void P4(ResolverCatalogue cat, CompositionTable table)
    {
        Console.WriteLine("--- (p4) refusal of unknown types and every other composition defect ---");
        var s = Synth();
        void Refused(string label, string rows, string rowId, string expect, int? rank = null)
        {
            var t = CompositionTable.Parse(Json(rows));
            var p = CompositionResolver.ExpandRow(t.ById(rowId), t, s, rank);
            Check(p.Refused && p.Refusal.Contains(expect, StringComparison.Ordinal), $"REFUSED: {label}",
                  p.Refusal ?? "(not refused)");
        }
        Refused("a type the catalogue does not know", Row("R", "", T(TBdeBox), Sub("X", 1, "UNIT", new[] { 3, 11, 1, 999, 5, 1, 0, 0 })),
                "R", "UNKNOWN TYPE");
        Refused("a type that lands the base abstract (an EMPTY unit)", Row("R", "", T(TBdeBox), Sub("X", 1, "UNIT", TAbstract)),
                "R", "UNKNOWN TYPE");
        Refused("a UNIT entry whose type is a container", Row("R", "", T(TBdeBox), Sub("X", 1, "UNIT", TBnBox)), "R", "says UNIT");
        Refused("a CONTAINER entry whose type is a unit",
                Row("R", "", T(TBdeBox), Sub("X", 1, "CONTAINER", TUnitA, "CATALOGUE")), "R", "says CONTAINER");
        Refused("CATALOGUE on a container that configures no members",
                Row("R", "", T(TBdeBox), Sub("X", 1, "CONTAINER", TEmptyBox, "CATALOGUE")), "R", "configures NO subordinates");
        Refused("a compose ref that names no row", Row("R", "", T(TBdeBox), Sub("X", 1, "CONTAINER", TBnBox, "R-NONE")), "R",
                "names no row");
        Refused("a compose cycle", Row("R", "", T(TBdeBox), Sub("X", 1, "CONTAINER", TBnBox, "R2")) + "," +
                                   Row("R2", "", T(TBnBox), Sub("Y", 1, "CONTAINER", TBnBox, "R")), "R", "CYCLE");
        string deep = string.Join(",", Enumerable.Range(0, 7).Select(i =>
            Row("D" + i, "", T(TBnBox), Sub("S", 1, "CONTAINER", TBnBox, "D" + (i + 1)))));
        Refused("a chain deeper than MaxDepth", deep, "D0", "deeper than MaxDepth");
        Refused("a battalion-echelon leaf in a battalion container (UG52 40.80 p902)",
                Row("R", "", T(TBnBox), Sub("CAV", 1, "UNIT", TBnUnit)), "R", "ECHELON", CompositionResolver.Rank(6));
        Refused("a count of 0", Row("R", "", T(TBdeBox), Sub("X", 0, "UNIT", TUnitA)), "R", "count 0 < 1");
        Refused("an objectType that is not an 8-field kind-11 land unit",
                Row("R", "", T(TBdeBox), "{ \"function\": \"X\", \"count\": 1, \"role\": \"UNIT\", \"objectType\": \"1:1:225:1:1:3:0\" }"),
                "R", "not an 8-field");
        Refused("a row with no subordinates (an EMPTY container cannot move)", Row("R", "", T(TBdeBox), ""), "R", "NO subordinates");
        // The composition_check.py DIRTY control on the REAL catalogue: Rifle CO USMC PA's .magx type lands the abstract.
        var real = CompositionTable.Parse(Json(Row("R", "", "3:11:1:225:6:3:1:0",
            Sub("RIF", 1, "UNIT", new[] { 3, 11, 1, 225, 5, 3, 1, 75 }))));
        var rp = CompositionResolver.ExpandRow(real.ById("R"), real, cat);
        Check(rp.Refused && rp.Refusal.Contains("UNKNOWN TYPE"),
              "REFUSED on the INSTALLED catalogue: 3:11:1:225:5:3:1:75 (the USMC rifle company's .magx type) lands no unit",
              rp.Refusal ?? "(not refused)");
    }

    // ------------------------------------------------------------------------------------------------ (p5) ----
    private static void P5()
    {
        Console.WriteLine("--- (p5) a NEW row with an authored type is accepted with no code change ---");
        var s = Synth();
        s.Add(TAuthored, "Infantry BN (IBCT) (authored, C2)", CatalogueRole.Unit, 600);
        var t = CompositionTable.Parse(Json(
            Row("C-USA-DIV-NEW", "\"F-NEW-I\"", T(TBdeBox),
                "{ \"function\": \"INF\", \"count\": 3, \"role\": \"UNIT\", \"objectType\": \"" + T(TAuthored) +
                "\", \"templateName\": \"Infantry BN (IBCT)\", \"authoredBy\": \"C2\", \"futureField\": [1, 2] }"),
            ", \"c2Provenance\": { \"lane\": \"feat/aggregate-authored-units\" }"));
        var p = CompositionResolver.Resolve("U", s.Resolve(TBdeBox), "F-NEW-I", null, t, s);
        Check(!p.Refused && p.Leaves.Count == 3 && p.Leaves.All(l => l.TemplateName.StartsWith("Infantry BN")),
              "a row naming an authored unit type resolves once the catalogue carries it (added fields are ignored)",
              p.Refusal ?? $"{p.Leaves.Count} leaves");
        bool refusedMajor = false;
        try { CompositionTable.Parse("{ \"schemaVersion\": 2, \"rows\": [] }"); }
        catch (InvalidDataException) { refusedMajor = true; }
        Check(refusedMajor, "a different schema MAJOR is refused, not guessed at (add fields, never rename them)");
    }

    // ------------------------------------------------------------------------------------------------ (p6) ----
    private static PopulateLayout P6(ResolverCatalogue cat, CompositionTable table)
    {
        Console.WriteLine("--- (p6) the ring, the names and the states by role ---");
        const string Name = "48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE";
        const double Lat = 54.0357, Lon = 23.2956;
        var rows = new Dictionary<string, (int N, double Spacing, double Radius)>
        {
            ["C-USA-BDE-UCI"] = (17, 180.0, 490.0), ["C-USA-BN-UCI"] = (5, 180.0, 153.0),
            ["C-USA-BDE-UCA"] = (26, 300.0, 1244.0), ["C-USA-BN-UCIZ"] = (8, 300.0, 392.0), ["C-USA-DIV-UCI"] = (1, 180.0, 0.0),
        };
        PopulateLayout ibct = null;
        foreach (var kv in rows)
        {
            var p = CompositionResolver.ExpandRow(table.ById(kv.Key), table, cat);
            var lay = PopulatePlanner.Plan(Name, Lat, Lon, p.Leaves, 0.0);
            if (kv.Key == "C-USA-BDE-UCI") ibct = lay;
            Check(!lay.Refused && lay.Members.Count == kv.Value.N && Math.Abs(lay.SpacingMeters - kv.Value.Spacing) < 0.5
                  && Math.Abs(lay.RadiusMeters - kv.Value.Radius) < 1.0,
                  $"{kv.Key}: {kv.Value.N} member(s) on ONE ring, spacing {kv.Value.Spacing:F0} m, radius {kv.Value.Radius:F0} m " +
                  "(composition_check.py --tree, FLAT)",
                  FormattableString.Invariant($"{lay.Members.Count}, {lay.SpacingMeters:F1} m, {lay.RadiusMeters:F1} m {lay.Refusal}"));
            double metersPerDegLon = 111_320.0 * Math.Cos(Lat * Math.PI / 180.0);
            double n = lay.Members.Count == 0 ? 0 : lay.Members.Average(m => (m.LatDeg - Lat) * 111_320.0);
            double e = lay.Members.Count == 0 ? 0 : lay.Members.Average(m => (m.LonDeg - Lon) * metersPerDegLon);
            Check(Math.Sqrt(n * n + e * e) < 0.01, $"{kv.Key}: the members' centroid is ON the container point (< 0.01 m)",
                  FormattableString.Invariant($"{Math.Sqrt(n * n + e * e):F6} m"));
            Check(lay.Members.All(m => m.Name.Length <= PopulatePlanner.MaxNameChars)
                  && lay.Members.Select(m => m.Name).Distinct().Count() == lay.Members.Count
                  && lay.Members.All(m => m.Name.StartsWith(Name.Substring(0, 10), StringComparison.Ordinal)),
                  $"{kv.Key}: member names are UNIQUE, within {PopulatePlanner.MaxNameChars} chars and read as the container's",
                  string.Join(", ", lay.Members.Take(3).Select(m => m.Name)));
        }
        Check(ibct != null && ibct.Members[0].Leaf.Function == "HQ" && ibct.Members[0].Slot == 0
              && Math.Abs(ibct.ReachMeters - 1090.0) < 1.0,
              "48 IBCT: the HQ takes slot 0 (designator 1); reach 1,090 m (the cavalry squadron's 600 m footprint does not size the ring)",
              ibct == null ? "" : FormattableString.Invariant($"{ibct.Members[0].Name}, reach {ibct.ReachMeters:F0} m"));
        if (ibct != null)
        {
            var plans = ibct.Members.Select(m => ContainerTypeRule.MemberPlan(m, Force.Friendly, m.LatDeg, m.LonDeg)).ToList();
            Check(plans.All(pl => pl.IsAggregate && pl.CreateAggregated && !pl.CreateSubordinates
                                  && CreationStates.For(pl) == AggregateState.Aggregated),
                  "every MEMBER is created AGGREGATED with no subordinates of its own (UG52 Table 68 p1470)");
            var choice = new ContainerChoice(true, new[] { 3, 11, 1, 225, 8, 3, 1, 1 }, "BDE", true, 8, 3, "UCI", Array.Empty<string>());
            var shell = ContainerTypeRule.Apply(new CreationPlan(true, default, Force.Friendly, 0.0, Name,
                                                                 new Geodetic { LatDeg = Lat, LonDeg = Lon }, null,
                                                                 TypeFidelity.Proxy, "Mech CO (USA, M2)", "F-UCI-H", "PROXY: x", "row=F-UCI-H"),
                                                choice);
            Check(shell.IsAggregate && !shell.CreateSubordinates && !shell.CreateAggregated
                  && CreationStates.For(shell) == AggregateState.Disaggregated && shell.MapRowId == "F-UCI-H"
                  && shell.TemplateName == "BDE" && shell.Type.Category == 8 && shell.Fidelity == TypeFidelity.Exact
                  && shell.Substitution.Length == 0,
                  "the CONTAINER is an EMPTY shell created DISAGGREGATED, keeps its map row (the composition key), exact branch = no proxy text");
        }
        return ibct;
    }

    // ------------------------------------------------------------------------------------------------ (p7) ----
    // composition_check.py --init-census, 2026-09-27 (hostile RUS): name -> (type, template, nearest branch, sized
    // from the EchelonCode). The 4 Iron Storm units with no coordinates are not created and not listed.
    private static readonly (string Name, string Type, string Template, bool Nearest, bool NoSidcEchelon)[] Census =
    {
        ("11_CAB/28ID__FRIENDLY_ATTACK_HELICOPTER_BRIGADE", "11:1:225:8:3:1:1", "BDE", true, false),
        ("4/278_ACR/28ID__FRIENDLY_ARMORED_CAVALRY_RECON_BAT", "11:1:225:6:30:1:0", "BN Reconnaissance PA", false, false),
        ("1_TK_RGT__ENEMY_AIRBORNE_TRACKED_ARMORED", "11:1:260:6:2:1:0", "BN, Armor", false, true),
        ("28ID__FRIENDLY_INFANTRY_DIVISION", "11:1:225:9:4:1:0", "DIV, Mech Infantry", true, false),
        ("14th_Commando_BDE__ENEMY_SPECIAL_FORCES_BRIGADE_C2", "11:1:260:8:30:1:0", "BDE Reconnaissance PA", false, false),
        ("52DTG__ENEMY_MECHANIZED_INFANTRY_DIVISION_C2_HQ", "11:1:260:9:4:1:0", "DIV, Mech Infantry", false, false),
        ("364th_TK_BDE_(res)__ENEMY_AIRBORNE_TRACKED_ARMORED", "11:1:260:6:2:1:0", "BN, Armor", false, true),
        ("33DTG__ENEMY_MECHANIZED_INFANTRY_DIVISION_C2_HQ", "11:1:260:9:4:1:0", "DIV, Mech Infantry", false, false),
        ("278_ACR/28ID__FRIENDLY_ARMORED_CAVALRY_RECON_REGIM", "11:1:225:7:30:1:0", "RGT, Reconnaissance PA", false, false),
        ("4ID__FRIENDLY_INFANTRY_DIVISION", "11:1:225:9:4:1:0", "DIV, Mech Infantry", true, false),
        ("169th_Field_Artillery_Brigade/28ID__ONE_SIX_NINE_T", "11:1:225:8:7:1:0", "BDE, Artillery", false, false),
        ("1st_Battalion,_112th_Infantry_Regiment/28ID__ONE_S", "11:1:225:6:3:1:0", "BN, Light Infantry", false, false),
        ("116ABCT/28ID__ONE_ONE_SIX_TH_ARMORED_BRIGADE_COMBA", "11:1:225:8:2:1:0", "BDE, Armor", false, false),
        ("183_SEC_BDE__ENEMY_MOTORIZED_INTERNAL_SECURITY_FOR", "11:1:260:8:18:1:0", "BDE, Motorized Rifle", false, false),
        ("113_SB/28ID__FRIENDLY_FORWARD_SUPPORT_BRIGADE", "11:1:225:8:31:1:1", "BDE CSS", false, false),
        ("116_ABCT/28ID__FRIENDLY_AIRBORNE_TRACKED_ARMORED_B", "11:1:225:8:2:1:0", "BDE, Armor", false, false),
        ("1-112_IN/28ID__FRIENDLY_INFANTRY_BATTALION_TASK_FO", "11:1:225:6:3:1:0", "BN, Light Infantry", false, false),
        ("1_MOI_REG__ENEMY_MOTORIZED_INFANTRY_REGIMENT", "11:1:260:7:18:1:0", "RGT Motorized Rifle PA", false, false),
        ("28ID/III_Corps__TWO_EIGHT_TH_US_INFANTRY_DIVISION", "11:1:225:9:4:1:0", "DIV, Mech Infantry", true, false),
        ("12th_Corps__ENEMY_MOTORIZED_INFANTRY", "11:1:260:10:18:1:0", "Corps, Army", false, false),
        ("113th_Sustainment_Brigade/28ID__ONE_ONE_THREE_TH_S", "11:1:225:8:31:1:1", "BDE CSS", false, false),
        ("169_FAB/28ID__FRIENDLY_FIELD_ARTILLERY_BRIGADE", "11:1:225:8:7:1:0", "BDE, Artillery", false, false),
        ("105th_AT_BDE_(res)__ENEMY_ARMORED_ANTI_ARMORED_BRI", "11:1:260:8:2:1:0", "BDE, Armor", true, false),
        ("278_Armored_Cavalry_Regiment/28ID__TWO_SEVEN_EIGHT", "11:1:225:7:30:1:0", "RGT, Reconnaissance PA", false, false),
        ("99_ARTY_BDE__ENEMY_HOWITZER_BRIGADE", "11:1:260:8:7:1:0", "BDE, Artillery", false, false),
        ("55th_Maneuver_Enhancement_Brigade/28ID__FIVE_FIVE_", "11:1:225:8:10:1:0", "BDE, Engineer", false, false),
        ("4th_Squadron,_278th_Armored_Cavalry_Regiment/278_A", "11:1:225:6:30:1:0", "BN Reconnaissance PA", false, false),
        ("11th_Combat_Aviation_Brigade/28ID__ONE_ONE_TH_COMB", "11:1:225:8:3:1:1", "BDE", true, false),
        ("15_MOI_REG__ENEMY_MOTORIZED_INFANTRY_REGIMENT", "11:1:260:7:18:1:0", "RGT Motorized Rifle PA", false, false),
        ("56SBCT/28ID__FIVE_SIX_TH_STRYKER_BRIGADE_COMBAT_TE", "11:1:225:8:4:1:0", "BDE, Mech Infantry", false, false),
        ("55_MEB/28ID__FRIENDLY_ENGINEER_BRIGADE", "11:1:225:8:10:1:0", "BDE, Engineer", false, false),
        ("48IBCT/28ID__FOUR_EIGHT_TH_INFANTRY_BRIGADE_COMBAT", "11:1:225:8:3:1:1", "BDE", false, false),
        ("TF_SNAKE__FRIENDLY_ARMORED_CAVALRY_RECON_REGIMENT", "11:1:225:7:30:1:0", "RGT, Reconnaissance PA", false, false),
        ("48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE", "11:1:225:8:3:1:1", "BDE", false, false),
        ("4ID/III_Corps__FOUR_TH_US_INFANTRY_DIVISION", "11:1:225:9:4:1:0", "DIV, Mech Infantry", true, false),
        ("56_SBCT/28ID__FRIENDLY_WHEELED_ARMORED_BRIGADE_TAS", "11:1:225:8:4:1:0", "BDE, Mech Infantry", false, false),
    };

    private static void P7(ResolverCatalogue cat, string repo)
    {
        Console.WriteLine("--- (p7) the init rule on all 36 created Iron Storm units (hostile RUS) ---");
        var init = InitParser.Parse(File.ReadAllText(Path.Combine(repo, "data", "IRONSTORM_CUTA_Initialization.xml")));
        var map = UnitTypeMap.Load(Path.Combine(repo, "data", "unit-type-map-52-aggregate.json"));
        var nations = new NationRoles("USA", "RUS");
        int created = 0, containers = 0, nearest = 0, fromCode = 0, matched = 0;
        var wrong = new List<string>();
        foreach (var u in init.Units)
        {
            if (string.IsNullOrEmpty(u.Latitude) || string.IsNullOrEmpty(u.Longitude)) continue;
            created++;
            var plan = UnitTranslator.Plan(u with { ElevationAgl = string.IsNullOrEmpty(u.ElevationAgl) ? "1000.0" : u.ElevationAgl },
                                           TypeMapping.FidelityTable, map, nations);
            var c = ContainerTypeRule.Choose(cat, plan.Type.Country, u.SymbolId, u.EchelonCode);
            if (c.Found && cat.Resolve(c.Type8)?.Role == CatalogueRole.Container) containers++;
            if (c.Found && !c.ExactBranch) nearest++;
            if (c.Flags.Any(f => f.StartsWith("no SIDC echelon", StringComparison.Ordinal))) fromCode++;
            string key = u.Name.Length > 50 ? u.Name.Substring(0, 50) : u.Name;
            var want = Census.FirstOrDefault(x => x.Name == key);
            if (want.Name == null) { wrong.Add(key + ": not in the pinned census"); continue; }
            if (c.TypeText == want.Type && c.TemplateName == want.Template && (!c.ExactBranch) == want.Nearest) matched++;
            else wrong.Add($"{key}: got {c.TypeText} '{c.TemplateName}' nearest={!c.ExactBranch}, want {want.Type} '{want.Template}' nearest={want.Nearest}");
        }
        Check(created == 36 && containers == 36,
              "all 36 created Iron Storm units land an Aggregate CONTAINER (census: 36 containers)",
              $"created {created}, containers {containers}");
        Check(matched == 36 && wrong.Count == 0,
              "every unit's container type and template equal composition_check.py --init-census (36 of 36)",
              string.Join("; ", wrong.Take(4)));
        Check(nearest == 7 && fromCode == 2,
              "7 are of the NEAREST branch and 2 are sized from their EchelonCode (NOS -> BN) - each logged, never silent",
              $"nearest {nearest}, from EchelonCode {fromCode}");
        var hostile = init.Units.Where(u => u.HostilityCode == "HO" && !string.IsNullOrEmpty(u.Latitude)).ToList();
        Check(hostile.Count > 0 && hostile.All(u =>
              {
                  var pl = UnitTranslator.Plan(u with { ElevationAgl = "1000.0" }, TypeMapping.FidelityTable, map, nations);
                  return ContainerTypeRule.Choose(cat, pl.Type.Country, u.SymbolId, u.EchelonCode).Type8?[3] == 260;
              }),
              "every hostile container is RUS - DIS 260 (RL-20260927-02)", $"{hostile.Count} hostile unit(s)");
        var none = ContainerTypeRule.Choose(null, 225, "SFGPUCI----H---", "BDE");
        Check(!none.Found && none.Flags.Any(f => f.StartsWith("NO CONTAINER", StringComparison.Ordinal)),
              "no catalogue = no container: the unit is refused LOUDLY, never created as something else");
    }

    // ------------------------------------------------------------------------------------------------ (p8) ----
    private sealed class FakeBridge : IContainerBridge
    {
        public readonly List<(string Child, string Superior)> Attaches = new();
        public readonly List<(string Uuid, string Script, IReadOnlyList<ContainerTaskVar> Vars)> Tasks = new();
        public int Deletes;
        public Func<string, int> Published = _ => 0;
        public void AddToOrganization(string childUuid, string superiorUuid) => Attaches.Add((childUuid, superiorUuid));
        public int PublishedSubordinateCount(string aggregateUuid) => Published(aggregateUuid);
        public void RunScriptedTask(string uuid, string scriptId, IReadOnlyList<ContainerTaskVar> vars) => Tasks.Add((uuid, scriptId, vars));
        public void DeleteObject(string uuid) => Deletes++;
    }

    private static void P8(PopulateLayout ibct)
    {
        Console.WriteLine("--- (p8) the in-place population against a fake bridge ---");
        if (ibct == null || ibct.Members.Count != 17) { Check(false, "the 48 IBCT layout is available for the sequence"); return; }
        const string C = "48_IBCT", CU = "VRF_UUID:container";
        var t0 = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);
        var fake = new FakeBridge();
        var pop = new ContainerPopulator();
        Check(!pop.MoveVerdict(C, t0).Ready && pop.MoveVerdict(C, t0).Reason.Contains("never populated"),
              "a container no order populated has NO members: its move is refused", pop.MoveVerdict(C, t0).Reason);
        long gen = pop.Begin(C, CU, ibct.Members, "source 3", t0, 85.0, 15.0);
        var gate = pop.GateOf(C);
        Check(!gate.IsCompleted && !pop.MoveVerdict(C, t0).Ready, "Planned: the gate is open and a move is refused");
        Check(pop.MarkIssued(C, gen, t0.AddSeconds(2)) && !pop.MarkIssued(C, gen, t0.AddSeconds(2)),
              "the member creates are issued ONCE (a second issue is refused)");
        for (int k = 0; k < 16; k++) pop.OnMemberCreated(ibct.Members[k].Name, "VRF_UUID:m" + k, t0.AddSeconds(3), fake);
        Check(fake.Attaches.Count == 0, "16 of 17 created: nothing is attached yet (all or, at the deadline, the arrived)");
        var ev = pop.OnMemberCreated(ibct.Members[16].Name, "VRF_UUID:m16", t0.AddSeconds(3), fake);
        Check(fake.Attaches.Count == 17 && fake.Attaches.All(a => a.Superior == CU) && fake.Attaches[0].Child == "VRF_UUID:m0"
              && ev.Any(x => x.Text.Contains("17 of 17 member(s) created")),
              "the 17th: AddToOrganization for all 17, in planned order (the HQ first), to the EXISTING container",
              $"{fake.Attaches.Count} attaches");
        // FAIL-FIRST: an UNGATED dispatch - the task sent the moment the attach is requested - would reach a container
        // that publishes NO subordinate yet, and its script's init() would snapshot nothing (PA_Move_Along_Route.lua:37).
        Check(fake.PublishedSubordinateCount(CU) == 0,
              "FAIL-FIRST: at the attach the container still publishes 0 members - an ungated task would snapshot NOTHING");
        var v0 = pop.TryIssueScriptedMove(C, CU, ContainerScripts.MoveAlongRoute, ContainerScripts.AlongRoute("VRF_UUID:r"), t0.AddSeconds(4), fake);
        pop.Sweep(t0.AddSeconds(4), fake);
        Check(!v0.Ready && fake.Tasks.Count == 0 && !gate.IsCompleted,
              "attached but NOT yet published: the gate holds and NO scripted task is sent", v0.Reason);
        fake.Published = _ => 17;
        var swept = pop.Sweep(t0.AddSeconds(5), fake);
        Check(gate.IsCompleted && pop.StageOf(C) == PopulateStage.Published
              && swept.Any(x => !x.Warning && x.Text.Contains("PUBLISHES 17 subordinate(s)") && x.Text.Contains("READY FOR TASKING")),
              "the container publishes 17: the gate opens (READY FOR TASKING)", string.Join(" | ", swept.Select(x => x.Text)));
        var v1 = pop.TryIssueScriptedMove(C, CU, ContainerScripts.MoveAlongRoute, ContainerScripts.AlongRoute("VRF_UUID:r"),
                                          t0.AddSeconds(6), fake);
        Check(v1.Ready && v1.Members == 17 && fake.Tasks.Count == 1 && fake.Tasks[0].Uuid == CU
              && fake.Tasks[0].Script == "PA_Move_Along_Route"
              && string.Join(",", fake.Tasks[0].Vars) == "route=VRF_UUID:r,reverseDirection=false,startAtClosestVertex=false",
              "then ONE RunScriptedTask PA_Move_Along_Route on the CONTAINER with the vendor plan's values",
              fake.Tasks.Count == 0 ? "no task" : string.Join(",", fake.Tasks[0].Vars));
        var late = pop.OnMemberCreated(ibct.Members[3].Name, "VRF_UUID:again", t0.AddSeconds(7), fake);
        Check(fake.Attaches.Count == 17, "a repeated ObjectCreated after the population ended attaches nothing more");

        // No members.
        var popR = new ContainerPopulator();
        popR.Refuse("EMPTY", "VRF_UUID:e", "POPULATE REFUSED: NO COMPOSITION for EMPTY", t0);
        var vr = popR.TryIssueScriptedMove("EMPTY", "VRF_UUID:e", ContainerScripts.MoveAlongRoute, ContainerScripts.AlongRoute("r"), t0, fake);
        Check(popR.GateOf("EMPTY").IsCompleted && !vr.Ready && vr.Reason.Contains("NO COMPOSITION") && fake.Tasks.Count == 1,
              "no composition: the gate completes AT ONCE (no task hangs) and the move is REFUSED, nothing sent", vr.Reason);

        // Timeout: attached, never published.
        var fakeT = new FakeBridge();
        var popT = new ContainerPopulator();
        long gT = popT.Begin("T", "VRF_UUID:t", ibct.Members, "source 3", t0, 85.0, 15.0);
        popT.MarkIssued("T", gT, t0);
        foreach (var mm in ibct.Members) popT.OnMemberCreated(mm.Name, "VRF_UUID:" + mm.Slot, t0.AddSeconds(1), fakeT);
        Check(popT.Sweep(t0.AddSeconds(84), fakeT).Count == 0 && !popT.GateOf("T").IsCompleted,
              "never published: still waiting 1 s before the bound");
        var evT = popT.Sweep(t0.AddSeconds(85), fakeT);
        var vT = popT.MoveVerdict("T", t0.AddSeconds(86));
        Check(popT.GateOf("T").IsCompleted && popT.StageOf("T") == PopulateStage.TimedOut && !vT.Ready
              && vT.Reason.Contains("published 0 of 17") && evT.Any(x => x.Warning && x.Text.Contains("REFUSED (TASKABRT)")),
              "at the bound: TIMED OUT - the gate ends, a WARN names published 0 of 17 and every MOVE is refused (TASKABRT)",
              vT.Reason);

        // A member never created: at the ATTACH deadline (bound - the publication window) the arrived ones are attached.
        var fakeP = new FakeBridge();
        var popP = new ContainerPopulator();
        long gP = popP.Begin("P", "VRF_UUID:p", ibct.Members, "source 3", t0, 85.0, 15.0);
        popP.MarkIssued("P", gP, t0);
        for (int k = 1; k < 17; k++) popP.OnMemberCreated(ibct.Members[k].Name, "VRF_UUID:" + k, t0.AddSeconds(1), fakeP);
        Check(popP.Sweep(t0.AddSeconds(69), fakeP).Count == 0 && fakeP.Attaches.Count == 0, "16 of 17: nothing attached before the attach deadline");
        var evP = popP.Sweep(t0.AddSeconds(70), fakeP);
        fakeP.Published = _ => 16;
        popP.Sweep(t0.AddSeconds(71), fakeP);
        Check(fakeP.Attaches.Count == 16 && evP.Any(x => x.Warning && x.Text.Contains("only 16 of 17"))
              && popP.MoveVerdict("P", t0.AddSeconds(72)) is { Ready: true, Members: 16 },
              "at the attach deadline the 16 that exist are ATTACHED (the missing one named), published, and the move may go");

        // Planned and never issued (the slot check never finished): timed out at the attach deadline; a late issue creates nothing.
        var popN = new ContainerPopulator();
        long gN = popN.Begin("N", "VRF_UUID:n", ibct.Members, "source 3", t0, 85.0, 15.0);
        popN.Sweep(t0.AddSeconds(70), new FakeBridge());
        Check(popN.StageOf("N") == PopulateStage.TimedOut && !popN.MarkIssued("N", gN, t0.AddSeconds(71)),
              "the slot check never returned: TIMED OUT, and its late issue is refused - no member is created for an ended population");

        // Precedence (1): the STP TO's existing children, attached again; the parent needs every child published.
        var fakeS = new FakeBridge();
        var popS = new ContainerPopulator();
        var oneLeaf = new[] { ibct.Members[0] with { Name = "CHILD-A.HQ1" } };
        long gA = popS.Begin("CHILD-A", "VRF_UUID:ca", oneLeaf, "source 3", t0, 85.0, 15.0);     // CHILD-A: populated
        popS.MarkIssued("CHILD-A", gA, t0);
        popS.OnMemberCreated("CHILD-A.HQ1", "VRF_UUID:ca-hq", t0, fakeS);
        popS.Refuse("CHILD-B", "VRF_UUID:cb", "POPULATE REFUSED: NO COMPOSITION for CHILD-B", t0);  // CHILD-B: none
        long gS = popS.BeginExisting("PARENT", "VRF_UUID:par", new[] { ("CHILD-A", "VRF_UUID:ca"), ("CHILD-B", "VRF_UUID:cb") },
                                     t0, 85.0);
        popS.AttachExisting("PARENT", gS, t0, fakeS);
        fakeS.Published = u => u == "VRF_UUID:par" ? 2 : u == "VRF_UUID:ca" ? 1 : 0;
        popS.Sweep(t0.AddSeconds(1), fakeS);
        var vS = popS.MoveVerdict("PARENT", t0.AddSeconds(1));
        Check(fakeS.Attaches.Count == 3 && fakeS.Attaches[1] == ("VRF_UUID:ca", "VRF_UUID:par")
              && fakeS.Attaches[2] == ("VRF_UUID:cb", "VRF_UUID:par") && popS.StageOf("PARENT") == PopulateStage.Published
              && popS.StageOf("CHILD-A") == PopulateStage.Published && !vS.Ready && vS.Reason.Contains("CHILD-B"),
              "the STP TO: the 2 existing children re-attached in the declared order; the parent publishes them, but its " +
              "move WAITS for every child - CHILD-B has no members, so it is refused naming CHILD-B", vS.Reason);
        Check(fake.Deletes == 0 && fakeT.Deletes == 0 && fakeP.Deletes == 0 && fakeS.Deletes == 0,
              "NO DELETE in any sequence - populating in place never deletes (RL-20260927-03)");
    }

    // ------------------------------------------------------------------------------------------------ (p9) ----
    private static void P9()
    {
        Console.WriteLine("--- (p9) D-6: a short vendor completion is WITHHELD ---");
        const double R = VertexChainPolicy.DefaultVertexArrivalRadiusMeters;
        Check(ContainerCompletionPolicy.Decide(true, true, 3000.0, R) == ContainerCompletionPolicy.Verdict.WithholdShort,
              "a container 3,000 m from the route end at its vendor completion: TASKCMPLT WITHHELD");
        Check(ContainerCompletionPolicy.Decide(true, true, 40.0, R) == ContainerCompletionPolicy.Verdict.HandOn,
              "40 m from the route end: handed on to the completion rules");
        Check(ContainerCompletionPolicy.Decide(true, true, double.NaN, R) == ContainerCompletionPolicy.Verdict.HandOn,
              "an unreadable centroid is never 'short' - nothing is claimed that was not measured");
        Check(ContainerCompletionPolicy.Decide(false, true, 3000.0, R) == ContainerCompletionPolicy.Verdict.HandOn,
              "a vendor FAILURE is not withheld - it takes the TASKABRT path");
        Check(ContainerCompletionPolicy.Decide(true, false, 3000.0, R) == ContainerCompletionPolicy.Verdict.HandOn,
              "a task with no destination (a patrol) is not judged by distance");
        Check(ContainerCompletionPolicy.Decide(true, true, 3000.0, 0.0) == ContainerCompletionPolicy.Verdict.HandOn,
              "FAIL-FIRST: with the bar off (Vrf:VertexArrivalRadiusMeters 0) the same short completion is HANDED ON - the " +
              "pre-D-6 behaviour, a TASKCMPLT for a container 3 km short");
        Check(ContainerCompletionPolicy.ShortText(3000.4) == "completed short: 3000 m from the route end",
              "the WARN names it: 'completed short: D m from the route end'", ContainerCompletionPolicy.ShortText(3000.4));
    }

    // ----------------------------------------------------------------------------------------------- (p10) ----
    private static void P10(string repo, ResolverCatalogue cat)
    {
        Console.WriteLine("--- (p10) the ENTITY-LEVEL path is unchanged ---");
        // Plans exactly as UnitTranslator makes them on EntityLevel: no plan asks for AGGREGATED, so every aggregate is
        // created DISAGGREGATED exactly as before C1, and the AtOrder census is the pre-C1 one.
        void EntityCensus(string label, string init, TypeMapping mode, string mapFile, int total, int aggregates, int platforms)
        {
            var data = InitParser.Parse(File.ReadAllText(Path.Combine(repo, "data", init)));
            var map = mapFile == null ? null : UnitTypeMap.Load(Path.Combine(repo, "data", mapFile));
            var plans = new List<CreationPlan>();
            foreach (var u in data.Units)
            {
                if (string.IsNullOrEmpty(u.Uuid) || string.IsNullOrEmpty(u.HostilityCode)
                    || string.IsNullOrEmpty(u.Latitude) || string.IsNullOrEmpty(u.Longitude)) continue;
                var p = UnitTranslator.Plan(u with { ElevationAgl = string.IsNullOrEmpty(u.ElevationAgl) ? "1000.0" : u.ElevationAgl },
                                            mode, map, new NationRoles("USA", "RUS"));
                if (p.Fidelity is TypeFidelity.AuthoredPending or TypeFidelity.Failed) continue;
                plans.Add(p);
            }
            var flipped = plans.Select(p => p.IsAggregate && p.CreateSubordinates ? p with { CreateSubordinates = false } : p).ToList();
            var census = CreationCensus.Of(flipped);
            Check(plans.All(p => !p.CreateAggregated)
                  && plans.Where(p => p.IsAggregate).All(p => CreationStates.For(p) == AggregateState.Disaggregated),
                  $"{label}: no plan asks for AGGREGATED - every aggregate is created DISAGGREGATED, as before C1");
            Check(census.Total == total && census.Aggregates == aggregates && census.Platforms == platforms
                  && census.EmptyShells == aggregates,
                  $"{label}: the AtOrder census is the pre-C1 one - {total} planned, {aggregates} shells, {platforms} platforms",
                  $"{census.Total} / {census.Aggregates} / {census.Platforms} / shells {census.EmptyShells}");
        }
        // Pinned from the UNCHANGED UnitTranslator (C1 adds one defaulted field to CreationPlan and nothing else there):
        // on EntityLevel Iron Storm is 32 lone platforms + 4 units (the E1/E2 "lone platform" taskees), COA-STP1 113 + 15.
        EntityCensus("Iron Storm cut A, EntityLevel fidelity table", "IRONSTORM_CUTA_Initialization.xml", TypeMapping.FidelityTable,
               "unit-type-map-52.json", 36, 4, 32);
        EntityCensus("COA-STP1, RealTemplates", "COA-STP1_Initialization.xml", TypeMapping.RealTemplates, null, 128, 113, 15);

        // THE SOURCE GUARDS. Every C1 branch in the service is behind _containerMode (or a flag only it sets), and
        // _containerMode has ONE writer: Vrf:ModelSet through VrfSettings.
        string svcPath = Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");
        string src = File.Exists(svcPath) ? File.ReadAllText(svcPath) : "";
        Check(src.Length > 0, "the service source is on disk", svcPath);
        Check(CountOf(src, "out _containerMode") == 1
              && src.Contains("UnitPositionPolicy.TryParseModelSet(_vrf.ModelSet, out _containerMode);"),
              "_containerMode has ONE writer: Vrf:ModelSet through VrfSettings (the M2 pre-flight's read)");
        Check(src.Contains("if (_containerMode && typeMapping == TypeMapping.FidelityTable && plan.IsAggregate)")
              && src.Contains("if (_containerMode && _containerByName.ContainsKey(name))")
              && src.Contains("if (_containerMode)\r\n                        _log.LogInformation(\"Task '{Task}': its affected entity")
                 | src.Contains("if (_containerMode)\n                        _log.LogInformation(\"Task '{Task}': its affected entity")
              && src.Contains("_containerMode && _containers.HasActive")
              && src.Contains("if (_containerMode && !string.IsNullOrEmpty(marking) && _containers.IsMember(marking, out var memberOf))")
              && src.Contains("=> _containerMode && !string.IsNullOrEmpty(name) && _containerByName.ContainsKey(name);"),
              "the init rule, the population, D-5, the sweep, the member completions and IsContainerUnit are all behind _containerMode");
        Check(src.Contains("CreationStates.For(p), p.CreateSubordinates);")
              && !src.Contains("AggregateState.Disaggregated, p.CreateSubordinates);"),
              "EnqueueCreates takes the state from the plan (Disaggregated unless a container member asks)");
        Check(src.Contains("\"CreationPolicy=AtOrder (C13): {Shells} unit(s) created as EMPTY shells at their \" +")
              && src.Contains("\"authored positions ({Flipped} flipped to empty here, {Composed} already an empty \" +")
              && src.Contains("\"COMPOSED PARENT shell); members are created when an order first references a \" +")
              && src.Contains("\"unit. {Platforms} platform(s) created in full. The INIT CREATION BARRIER line \" +")
              && src.Contains("\"counts the same shells over the same plan list (N15).\","),
              "the ENTITY-level census line is byte-identical to the pre-C1 wording");
        Check(src.Contains("\"CreationPolicy=AtOrder (C13) on the AGGREGATE model set: {Containers} container(s) \" +")
              && src.Contains("\"created EMPTY at their authored positions (RL-20260927-03)"),
              "the AGGREGATE census line says 'N container(s) created EMPTY at their authored positions'");
        // A container's move is decided with every other mover's - ONE FormFor, ONE dispatch CreateRoute (the vertex
        // chain's own tripwires, RL-20260927-01, which a parallel container copy of both broke in the first build of
        // this lane) - and it never takes the aggregate MoveIntoFormation / PlanAndMove / formation / fan-out probes.
        int refusal = src.IndexOf("if (unit.IsContainer && !ContainerMayMove(task, unit, out containerMembers)) return;",
                                  StringComparison.Ordinal);
        int formationMove = src.IndexOf("if (unit.IsAggregate && !unit.IsContainer && !string.IsNullOrEmpty(_vrf.MoveIntoFormation))",
                                        StringComparison.Ordinal);
        int singleArm = src.IndexOf("if (moveForm == GroundMoveForm.SinglePointMoveTo)", StringComparison.Ordinal);
        int pointMove = src.IndexOf("IssueContainerPointMove(task, unit, vrfUuid, routeGeo, containerMembers);", StringComparison.Ordinal);
        int entityPoint = src.IndexOf("_bridge.MoveToLocation(vrfUuid, routeGeo[^1]);", StringComparison.Ordinal);
        int routeScript = src.IndexOf("ScriptId: containerScript,", StringComparison.Ordinal);
        int createRoute = src.IndexOf("_bridge.CreateRoute(routeGeo, routeName);", StringComparison.Ordinal);
        Check(refusal > 0 && formationMove > refusal && singleArm > formationMove && pointMove > singleArm
              && entityPoint > pointMove && routeScript > entityPoint && createRoute > routeScript
              && CountOf(src, "VertexChainPolicy.FormFor(") == 1 && CountOf(src, "_bridge.CreateRoute(routeGeo, routeName);") == 1,
              "a container is REFUSED before the aggregate branches, then takes the SHARED form decision: its point move " +
              "inside the single-point arm (before the entity MoveToLocation), its route script on the one CreateRoute arm",
              FormattableString.Invariant($"refusal {refusal}, MoveIntoFormation {formationMove}, single arm {singleArm}, ") +
              FormattableString.Invariant($"point {pointMove}, entity point {entityPoint}, route script {routeScript}, ") +
              FormattableString.Invariant($"CreateRoute {createRoute}"));
        Check(src.Contains("if (unit.IsAggregate && !unit.IsContainer && _vrf.AggregatePlanAndMove)")
              && src.Contains("if (!unit.IsContainer && !string.IsNullOrEmpty(_vrf.AggregateFormation))")
              && src.Contains("if (_vrf.SubordinateFanOut && unit.IsAggregate && !unit.IsContainer && !patrol)")
              && src.Contains("&& !createdUnit.IsContainer)")
              && src.Contains("bool routeWillBeCollapsed = unit.IsAggregate && !unit.IsContainer"),
              "the opt-in aggregate probes (PlanAndMove, formation set at move and at creation, fan-out, the collapsed " +
              "route) all exclude a container");
        string completion = Between(src, "private void OnVrfTaskCompleted(", "private void SynthesizeUnitCompletion(");
        Check(completion.IndexOf("WithholdShortContainerCompletion(marking, e.TaskType)", StringComparison.Ordinal) > 0
              && completion.IndexOf("WithholdShortContainerCompletion(marking, e.TaskType)", StringComparison.Ordinal)
                 < completion.IndexOf("ClearStallState(marking);", StringComparison.Ordinal),
              "D-6 is judged BEFORE the watchdog window is dropped - a withheld completion leaves the stall watch running");
        Check(ContainerStartup.OffLine("EntityLevel").Contains("exactly as before C1")
              && ContainerStartup.OffLine(null).Contains("(not set)")
              && ContainerStartup.OffLine("EntityLevel").Contains("RL-20260927-03"),
              "EntityLevel says C1 is OFF, once, naming the rulings");
        // D1's premise for a POPULATED container (design sec 8): its members are UNITS, so the entity-member reader
        // returns none and the judges read ONE position - the container's own centroid. No code change to D1.
        Check(UnitPositionPolicy.SourceFor(isAggregate: true, memberCount: 0, aggregateModelSet: true) == UnitPositionSource.AggregateLeaf,
              "a populated container publishes no ENTITY members -> AggregateLeaf: arrival and the watchdog judge its centroid (D1)");
    }

    // ----------------------------------------------------------------------------------------------- (p11) ----
    private static void P11(string home)
    {
        Console.WriteLine("--- (p11) the scripts: ids and variables from the vendor XML ---");
        string dir = Path.Combine(home, "data", "simulationModelSets", "AggregateLevelBase", "scripts");
        var ids = new[] { ContainerScripts.MoveAlongRoute, ContainerScripts.MoveToLocationDirect, ContainerScripts.PatrolRoute };
        for (int i = 0; i < ids.Length; i++)
        {
            string xml = Path.Combine(dir, ContainerScripts.VendorXmlFiles[i]);
            string text = File.Exists(xml) ? File.ReadAllText(xml) : "";
            var m = Regex.Match(text, "<myScriptId>([^<]*)</myScriptId>");
            Check(m.Success && string.Equals(m.Groups[1].Value, ids[i], StringComparison.Ordinal),
                  $"{ContainerScripts.VendorXmlFiles[i]} myScriptId is exactly '{ids[i]}' (case included)",
                  m.Success ? m.Groups[1].Value : "not found at " + xml);
        }
        string along = SafeRead(Path.Combine(dir, "PA_Move_Along_Route.xml"));
        string direct = SafeRead(Path.Combine(dir, "PA_Move_To_Location_Direct.xml"));
        string patrol = SafeRead(Path.Combine(dir, "PA_Patrol_Route.xml"));
        Check(VarType(along, "route") == "simulationobject" && VarType(along, "reverseDirection") == "checkbox"
              && VarType(along, "startAtClosestVertex") == "checkbox" && VarType(direct, "location") == "location"
              && VarType(direct, "retrograde") == "checkbox" && VarType(patrol, "route") == "simulationobject",
              "every variable the interface sets exists in its script's XML with the type it is bound as");
        string ope = SafeRead(Path.Combine(home, "data", "simulationModelSets", "AggregateLevelBase", "vrfSim", "platforms",
                                           "PseudoAggregate.ope"));
        Check(ids.All(id => ope.Contains("\"" + id + "\"", StringComparison.Ordinal)),
              "the Aggregate Container ENABLES all three (PseudoAggregate.ope script-enable-controller :115-117)");
        Check(ContainerScripts.ForForm(GroundMoveForm.SinglePointMoveTo, false) == ContainerScripts.MoveToLocationDirect
              && ContainerScripts.ForForm(GroundMoveForm.RouteTask, false) == ContainerScripts.MoveAlongRoute
              && ContainerScripts.ForForm(GroundMoveForm.RouteTask, true) == ContainerScripts.PatrolRoute
              && VertexChainPolicy.FormFor(true, true, false, 5, true) == GroundMoveForm.RouteTask
              && VertexChainPolicy.FormFor(true, true, false, 1, true) == GroundMoveForm.SinglePointMoveTo,
              "the script follows the entity path's own form: one point -> direct, a route -> along, a patrol -> patrol; a " +
              "container (an aggregate) never gets a vertex chain");
        var lines = VrfBridge.DescribeScriptVars(ContainerScriptVars.ToBridge(
            ContainerScripts.AlongRoute("VRF_UUID:route").Concat(ContainerScripts.ToLocation(54.0268, 23.3172, 0.0)).ToList()));
        string joined = string.Join(" ; ", lines);
        Check(lines.Count == 5 && lines[0].StartsWith("route|") && lines[0].Contains("|simulationobject|VRF_UUID:route")
              && lines[1].Contains("reverseDirection|") && lines[1].Contains("|checkbox|false")
              && lines[2].Contains("|checkbox|false") && lines[3].StartsWith("location|") && lines[3].Contains("|location|54.026800,23.317200")
              && lines[4].Contains("|checkbox|false"),
              "the bridge binds them as the vendor's types (a real DtScriptedTaskTask, nothing sent)", joined);
    }

    private static string VarType(string xml, string name)
    {
        var m = Regex.Match(xml, "&lt;myVariableName&gt;" + Regex.Escape(name) + "&lt;/myVariableName&gt;\\s*&lt;myType&gt;([^&]*)&lt;/myType&gt;");
        return m.Success ? m.Groups[1].Value : "";
    }

    // ----------------------------------------------------------------------------------------------- (p12) ----
    private sealed class HasCount { public int PublishedSubordinateCount(string uuid) => uuid == "x" ? 7 : -1; }
    private sealed class NoCount { public int Other(string uuid) => 0; }

    private static void P12(string repo)
    {
        Console.WriteLine("--- (p12) the publication reader and the start-up arithmetic ---");
        var bound = PublishedCountReader.Bind(new HasCount());
        Check(bound != null && bound("x") == 7 && PublishedCountReader.Bind(new NoCount()) == null
              && PublishedCountReader.Bind(null) == null,
              "the reader binds PublishedSubordinateCount(string) -> int by name, and is null where the member is absent");
        bool linked = PublishedCountReader.Present(typeof(VrfBridge));
        Console.WriteLine($"  [INFO] the linked VrfBridge.dll {(linked ? "CARRIES" : "does NOT carry")} PublishedSubordinateCount " +
                          $"- {(linked ? "a rebuilt bridge" : "the pinned bridge; on the aggregate model set the interface REFUSES TO START until the bridge is rebuilt (RUNBOOK sec 9)")}.");
        string fac = SafeRead(Path.Combine(repo, "src", "VrfFacade", "VrfFacade.cpp"));
        string hdr = SafeRead(Path.Combine(repo, "src", "VrfFacade", "VrfFacade.h"));
        string brg = SafeRead(Path.Combine(repo, "src", "VrfBridge", "VrfBridge.cpp"));
        Check(fac.Contains("int VrfFacade::PublishedSubordinateCount(const std::string& aggregateUuid) const")
              && fac.Contains("return countValid(casr->subAggregates()) + countValid(casr->entities());")
              && hdr.Contains("int PublishedSubordinateCount(const std::string& aggregateUuid) const;")
              && brg.Contains("int PublishedSubordinateCount(String^ aggregateUuid)"),
              "the native member is in the source: facade (sub-aggregates + entities, through a CONST repository) and bridge");
        string svc = SafeRead(Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs"));
        Check(svc.Contains("the loaded VrfBridge.dll has no PublishedSubordinateCount") && svc.Contains("REFUSING TO START: {Problems}"),
              "the aggregate model set REFUSES TO START without the reader - a gate that cannot see the members is not a gate");
        Check(Math.Abs(ContainerStartup.PopulateTimeoutSeconds(0.0, 30.0, 10.0, 15.0) - 85.0) < 1e-9
              && Math.Abs(ContainerStartup.PopulateTimeoutSeconds(60.0, 30.0, 10.0, 15.0) - 60.0) < 1e-9
              && ContainerStartup.PopulateTimeoutSeconds(double.NaN, 30.0, 10.0, 15.0) > 80.0,
              "the gate's bound: 85 s derived at the shipped values (30 + 10 + 2 x 15 + 15), a configured value wins");
    }

    // ----------------------------------------------------------------------------------------------- (p13) ----
    private const string VendorFixture = "IronStorm_Centre_52_Aggregate";
    private const string DerivedFixture = "IronStorm_Centre_52_Aggregate_C2SIM";

    private static string VRow(string id, string variant, string maps, string container, string subs)
        => $"{{ \"id\": \"{id}\", \"variant\": \"{variant}\", \"mapRowIds\": [{maps}], \"container\": {{ \"objectType\": " +
           $"\"{container}\", \"templateName\": \"x\" }}, \"subordinates\": [{subs}] }}";

    private static string Variants(string authoredModelSet = CompositionVariants.DerivedModelSet)
        => ", \"variants\": { \"catalogue\": { \"modelSet\": \"AggregateTacticalLevel\", \"note\": \"c\" }, \"authored\": " +
           $"{{ \"modelSet\": \"{authoredModelSet}\", \"sms\": \"C:\\\\C2SIM\\\\vrf-sms\\\\C2SIM_AggregateTacticalLevel.sms\", " +
           "\"note\": \"a\" } }, \"defaultVariant\": \"catalogue\"";

    private static void P13(string repo, string home, ResolverCatalogue cat, CompositionTable full,
                            CompositionVariants.Selection selected, ResolverCatalogue derived, string derivedSms)
    {
        Console.WriteLine("--- (p13) C1b: the composition VARIANT and the derived-SMS guard (RL-20260927-04) ---");
        bool authoredSelected = derived != null || string.Equals(selected.Name, CompositionVariants.Authored,
                                                                 StringComparison.OrdinalIgnoreCase);
        Check(!selected.Refused, $"the selected variant is declared by the file ({selected.Name}; {selected.Source})",
              selected.Refusal ?? "");
        var map = UnitTypeMap.Load(Path.Combine(repo, "data", "unit-type-map-52-aggregate.json"));
        var authoredRows = map.AuthoredRows;

        // --- the variants in the file, and the resolver on the WHOLE file (main before C1b) ---
        Check(full.Variants.Count == 2 && full.Variants.ContainsKey("catalogue") && full.Variants.ContainsKey("authored")
              && full.DefaultVariant == "catalogue" && full.UndeclaredRowVariants().Count == 0
              && full.Rows.All(r => r.Variant is "catalogue" or "authored" or "all"),
              "the file declares the variants catalogue and authored, defaultVariant catalogue, and every row carries one",
              $"{string.Join(", ", full.Variants.Keys)}; default '{full.DefaultVariant}'; " +
              string.Join(" ", full.Rows.Select(r => $"{r.Id}[{r.Variant}]")));
        var bde = cat.Resolve(new[] { 3, 11, 1, 225, 8, 3, 1, 1 });
        var blind = CompositionResolver.Resolve("48_IBCT", bde, "F-UCI-H", null, full, cat);
        Check(blind.Refused && blind.Refusal.Contains("TABLE DEFECT") && blind.Refusal.Contains("DIFFERENT variants"),
              "FAIL-FIRST: read variant-blind (main before C1b), the C2 file makes 48 IBCT's map row F-UCI-H a TABLE DEFECT - " +
              "two variants claim it, so the G1 brigade would get NO members", blind.Refusal ?? "(not refused)");

        // --- selection ---
        var sc = CompositionVariants.Select(full, "catalogue");
        var tc = full.ForVariant(sc.Name);
        string tcIds = string.Join(",", tc.Rows.Select(r => r.Id));
        Check(!sc.Refused && sc.Name == "catalogue" && tc.SelectedVariant == "catalogue" && tc.FileRowCount == full.Rows.Count
              && tcIds == "C-USA-DIV-UCI,C-USA-BDE-UCI,C-USA-BN-UCI,C-USA-BDE-UCA,C-USA-BN-UCIZ",
              "Vrf:CompositionVariant=catalogue: its 3 rows plus the 2 'all' rows, in file order", tcIds);
        var pc = CompositionResolver.Resolve("48_IBCT", bde, "F-UCI-H", null, tc, cat);
        Check(!pc.Refused && pc.RowId == "C-USA-BDE-UCI" && pc.Leaves.Count == 17,
              "... and 48 IBCT's F-UCI-H resolves to C-USA-BDE-UCI, 17 leaves (the C1 draft) - one variant, one row",
              pc.Refusal ?? $"{pc.RowId}, {pc.Leaves.Count} leaves");
        var sd = CompositionVariants.Select(full, "   ");
        Check(!sd.Refused && sd.Name == "catalogue" && sd.Source.Contains("defaultVariant"),
              "a BLANK setting takes the file's defaultVariant (catalogue)", $"{sd.Name} <- {sd.Source}");
        var sa = CompositionVariants.Select(full, "Authored");
        var ta = full.ForVariant(sa.Name);
        string taIds = string.Join(",", ta.Rows.Select(r => r.Id));
        var fa = ta.ForMapRow("F-UCI-H");
        Check(!sa.Refused && sa.Name == "authored" && sa.Info?.ModelSet == CompositionVariants.DerivedModelSet
              && taIds == "C-USA-BN-UCI,C-USA-BN-UCIZ,C-USA-DIV-UCI-A,C-USA-BDE-UCI-A,C-USA-BDE-UCA-A"
              && fa.Count == 1 && fa[0].Id == "C-USA-BDE-UCI-A",
              "Vrf:CompositionVariant=Authored (any case): its 3 rows plus the 2 'all' rows; F-UCI-H -> C-USA-BDE-UCI-A",
              $"{taIds}; F-UCI-H -> {string.Join(",", fa.Select(r => r.Id))}");
        var su = CompositionVariants.Select(full, "autored");
        Check(su.Refused && su.Refusal.StartsWith("COMPOSITION VARIANT 'autored'", StringComparison.Ordinal)
              && su.Refusal.Contains("authored, catalogue") && su.Refusal.Contains("REFUSING") && su.Refusal.Contains("RL-20260927-04"),
              "an UNKNOWN variant is REFUSED at start-up, naming the declared ones - never a fallback", su.Refusal ?? "(accepted)");
        var c1 = CompositionTable.Parse(Json(Row("R1", "\"F-X-H\"", T(TBdeBox), Sub("HQ", 1, "UNIT", THq))));
        var c1s = CompositionVariants.Select(c1, "");
        Check(!c1s.Refused && c1s.Name == "catalogue" && c1.ForVariant(c1s.Name).Rows.Count == 1
              && CompositionVariants.Select(c1, "authored").Refused,
              "a table with NO variants (the C1 schema) has one implicit variant, catalogue, holding every row; 'authored' is refused",
              CompositionVariants.Select(c1, "authored").Refusal ?? "(accepted)");
        var stray = CompositionTable.Parse(Json(
            VRow("R-OK", "catalogue", "\"F-X-H\"", T(TBdeBox), Sub("HQ", 1, "UNIT", THq)) + "," +
            VRow("R-TYPO", "authord", "\"F-X-I\"", T(TBdeBox), Sub("HQ", 1, "UNIT", THq)), Variants()));
        Check(stray.UndeclaredRowVariants().Count == 1 && stray.UndeclaredRowVariants()[0].Contains("R-TYPO")
              && stray.ForVariant("catalogue").Rows.Count == 1 && stray.ForVariant("authored").Rows.Count == 0,
              "a row of an UNDECLARED variant belongs to no variant and is REPORTED (the start-up logs it), not used",
              string.Join("; ", stray.UndeclaredRowVariants()));
        var cross = CompositionTable.Parse(Json(
            VRow("R-CAT", "catalogue", "\"F-X-H\"", T(TBdeBox), Sub("INF", 1, "CONTAINER", TBnBox, "R-AUT")) + "," +
            VRow("R-AUT", "authored", "", T(TBnBox), Sub("RIF", 2, "UNIT", TUnitA)), Variants()));
        var cx = CompositionResolver.ExpandRow(cross.ForVariant("catalogue").ById("R-CAT"), cross.ForVariant("catalogue"), Synth());
        Check(cx.Refused && cx.Refusal.Contains("names no row"),
              "a compose reference ACROSS variants names no row in the narrowed table (composition_check.py's rule)",
              cx.Refusal ?? "(expanded)");

        // --- the fixture's SMS, read as the runner's Stage 0 reads it ---
        string fx = Path.Combine(repo, "tools", "FixtureGen", "frame_variants");
        var fv = CompositionVariants.ReadFixtureSms(Path.Combine(fx, VendorFixture + ".scnx"), home);
        var fd = CompositionVariants.ReadFixtureSms(Path.Combine(fx, DerivedFixture + ".scnx"), home);
        Check(fv.Known && fv.Sms == @"$(DATA_DIR)\simulationModelSets\AggregateTacticalLevel.sms"
              && fd.Known && fd.Sms == @"C:\C2SIM\vrf-sms\C2SIM_AggregateTacticalLevel.sms",
              $"the fixtures' Simulation-Model-Set-Files, read like RunnerLib.ps1 Get-ScenarioModelSet: {VendorFixture} = " +
              $"the shipped set, {DerivedFixture} = the derived set", $"{fv.Sms} | {fd.Sms}");
        var fnone = CompositionVariants.ReadFixtureSms("", home);
        var fname = CompositionVariants.ReadFixtureSms("NoSuchScenario_C1b", home);
        var fjunk = CompositionVariants.ReadFixtureSms(Path.Combine(repo, "data", "unit-composition-52-aggregate.json"), home);
        Check(!fnone.Known && fnone.Via.Contains("not set")
              && !fname.Found && fname.ScnxPath == Path.Combine(home, "userData", "scenarios", "NoSuchScenario_C1b.scnx")
              && fjunk.Found && !fjunk.Readable,
              "a scenario NAME resolves to <VrfHome>\\userData\\scenarios\\<name>.scnx (the runner's rule); unset, missing " +
              "and not-an-archive are all UNKNOWN", $"{fnone.Via} | {fname.ScnxPath}: {fname.Via} | {fjunk.Via}");
        Check(CompositionVariants.SmsModelSetName(fd.Sms) == CompositionVariants.DerivedModelSet
              && CompositionVariants.SmsModelSetName(fv.Sms) == "AggregateTacticalLevel"
              && string.Equals(CompositionVariants.SmsFilePath(fv.Sms, home),
                               CompositionVariants.CatalogueSmsPath("AggregateTacticalLevel", home), StringComparison.OrdinalIgnoreCase)
              && string.Equals(CompositionVariants.SmsFilePath(fd.Sms, home),
                               CompositionVariants.CatalogueSmsPath(fd.Sms, home), StringComparison.OrdinalIgnoreCase),
              "an SMS string's model-set name is its file name (Get-ModelSetFromSms); $(DATA_DIR) expands to <VrfHome>\\data, " +
              "so the shipped fixture's SMS IS the default catalogue's file",
              CompositionVariants.SmsFilePath(fv.Sms, home));

        // --- THE GUARD, both ways (pure: the real fixtures, catalogue roots as the resolver reports them) ---
        string vendorRoot = UnitPositionPolicy.AggregateModelSet;   // ObjectTypeResolver.RootSms of the default catalogue
        string derivedRoot = fd.Sms;                                 // ... of Vrf:CatalogueSms = the derived .sms
        var gShipped = CompositionVariants.Check(ta, sa, authoredRows, fv, derivedRoot, home);
        Check(gShipped.Refused
              && gShipped.Refusal.StartsWith("COMPOSITION VARIANT authored needs the derived SMS C2SIM_AggregateTacticalLevel", StringComparison.Ordinal)
              && gShipped.Refusal.Contains("loads $(DATA_DIR)\\simulationModelSets\\AggregateTacticalLevel.sms")
              && gShipped.Refusal.Contains("EMPTY") && gShipped.Refusal.Contains("RL-20260927-04"),
              "THE GUARD: authored + a fixture on the SHIPPED SMS -> REFUSED: 'COMPOSITION VARIANT authored needs the derived " +
              "SMS ... the fixture ... loads <sms>'", gShipped.Refusal ?? "(ACCEPTED - the wrong-SMS hazard would run)");
        var gOk = CompositionVariants.Check(ta, sa, authoredRows, fd, derivedRoot, home);
        Check(!gOk.Refused && gOk.NeedsDerivedSet && gOk.RequiredName == CompositionVariants.DerivedModelSet,
              "THE GUARD: authored + a fixture on the DERIVED SMS + the catalogue rooted at that same file -> accepted",
              gOk.Refusal ?? $"needs {gOk.RequiredName}");
        var gNone = CompositionVariants.Check(ta, sa, authoredRows, fnone, derivedRoot, home);
        var gMissing = CompositionVariants.Check(ta, sa, authoredRows, fname, derivedRoot, home);
        var gJunk = CompositionVariants.Check(ta, sa, authoredRows, fjunk, derivedRoot, home);
        Check(gNone.Refused && gNone.Refusal.Contains("UNKNOWN") && gNone.Refusal.Contains("Vrf:Scenario is not set")
              && gMissing.Refused && gMissing.Refusal.Contains("no such file") && gJunk.Refused && gJunk.Refusal.Contains("UNKNOWN"),
              "THE GUARD: authored with the fixture UNSET, MISSING or UNREADABLE -> REFUSED (an unverified SMS never passes)",
              gNone.Refusal ?? "(accepted)");
        var gWrongCat = CompositionVariants.Check(ta, sa, authoredRows, fd, vendorRoot, home);
        Check(gWrongCat.Refused && gWrongCat.Refusal.Contains("Vrf:CatalogueSms=C:\\C2SIM\\vrf-sms\\C2SIM_AggregateTacticalLevel.sms"),
              "THE GUARD: authored + the derived fixture but the app's catalogue on the SHIPPED set -> REFUSED (set Vrf:CatalogueSms)",
              gWrongCat.Refusal ?? "(accepted)");
        var cShipped = CompositionVariants.Check(tc, sc, authoredRows, fv, vendorRoot, home);
        var cDerived = CompositionVariants.Check(tc, sc, authoredRows, fd, vendorRoot, home);
        var cNone = CompositionVariants.Check(tc, sc, authoredRows, fnone, vendorRoot, home);
        Check(!cShipped.NeedsDerivedSet && !cShipped.Refused && !cDerived.Refused && !cNone.Refused,
              "the catalogue variant needs no derived set: accepted on the shipped fixture, the derived one and none at all",
              $"{cShipped.Refusal}{cDerived.Refusal}{cNone.Refusal}");

        // --- bypass controls: the CONTENT triggers the guard, not only the name ---
        var byType = CompositionTable.Parse(Json(VRow("R-CAT", "catalogue", "\"F-X-H\"", T(TBdeBox),
            "{ \"function\": \"INF\", \"count\": 1, \"role\": \"UNIT\", \"objectType\": \"3:11:1:225:6:3:1:201\", " +
            "\"templateName\": \"Infantry BN (USA, IBCT)\", \"fidelity\": \"PROXY\" }"), Variants()));
        var byFid = CompositionTable.Parse(Json(VRow("R-CAT", "catalogue", "\"F-X-H\"", T(TBdeBox),
            "{ \"function\": \"HQ\", \"count\": 1, \"role\": \"UNIT\", \"objectType\": \"" + T(THq) + "\", " +
            "\"templateName\": \"t\", \"fidelity\": \"AUTHORED\" }"), Variants()));
        var gType = CompositionVariants.Check(byType.ForVariant("catalogue"), CompositionVariants.Select(byType, "catalogue"),
                                              authoredRows, fv, vendorRoot, home);
        var gFid = CompositionVariants.Check(byFid.ForVariant("catalogue"), CompositionVariants.Select(byFid, "catalogue"),
                                             authoredRows, fv, vendorRoot, home);
        Check(gType.Refused && gType.Why.Contains("A-INF-BN-IBCT") && gFid.Refused && gFid.Why.Contains("fidelity AUTHORED"),
              "BYPASS CONTROL: a CATALOGUE-variant row naming an authored type, or an entry of fidelity AUTHORED, still needs " +
              "the derived set - refused on the shipped fixture", $"{gType.Why} | {gFid.Why}");
        var badDecl = CompositionTable.Parse(Json(VRow("R-A", "authored", "\"F-X-H\"", T(TBdeBox), Sub("HQ", 1, "UNIT", THq)),
                                                  Variants(UnitPositionPolicy.AggregateModelSet)));
        var gDecl = CompositionVariants.Check(badDecl.ForVariant("authored"), CompositionVariants.Select(badDecl, "authored"),
                                              authoredRows, fv, vendorRoot, home);
        Check(gDecl.Refused && gDecl.RequiredName == CompositionVariants.DerivedModelSet,
              "BYPASS CONTROL: an 'authored' variant that DECLARES the shipped model set is not obeyed - it still needs " +
              CompositionVariants.DerivedModelSet, gDecl.Refusal ?? "(accepted)");

        // --- the hazard the guard exists for, on the INSTALLED vendor set ---
        var hazards = new[] { "C-USA-DIV-UCI-A", "C-USA-BDE-UCI-A", "C-USA-BDE-UCA-A" }
            .Select(id => CompositionResolver.ExpandRow(ta.ById(id), ta, cat)).ToList();
        Check(hazards.All(h => h.Refused && (h.Refusal.Contains("UNKNOWN TYPE") || h.Refusal.Contains("says UNIT"))),
              "THE HAZARD, on the SHIPPED catalogue: every authored row is REFUSED - its types land EMPTY generic containers " +
              "or the base abstract (C2 record sec 7) - which is why the guard refuses the start",
              string.Join(" | ", hazards.Select(h => h.Refusal)));

        // --- the type map's authoredRows: fidelity Authored, never lookup rows ---
        int rawRows;
        using (var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, "data", "unit-type-map-52-aggregate.json"))))
            rawRows = doc.RootElement.GetProperty("rows").GetArrayLength();
        Check(authoredRows.Count == 7 && authoredRows.All(a => a.Fidelity == TypeFidelity.Authored)
              && string.Join(",", authoredRows.Select(a => a.Id)) ==
                 "A-INF-BN-IBCT,A-FA-BN-IBCT,A-BEB-IBCT,A-BSB,A-DIV-HQ,A-FA-BN-ABCT,A-BEB-ABCT"
              && map.Rows.Count == rawRows && map.Rows.All(r => r.Fidelity != TypeFidelity.Authored),
              "the type map's 7 authoredRows are read with fidelity AUTHORED (TypeFidelity.Authored, a real value - not Failed); " +
              "the lookup rows are unchanged", $"{authoredRows.Count} authored ({string.Join(", ", authoredRows.Select(a => a.Fidelity).Distinct())}), {map.Rows.Count} of {rawRows} rows");
        var mixed = UnitTypeMap.Parse("{ \"nations\": { \"USA\": 225 }, \"rows\": [ { \"id\": \"L1\", \"functionId\": \"UCI\", " +
                                      "\"echelon\": \"H\", \"nationRole\": \"friendly\", \"nation\": \"USA\", \"objectType\": " +
                                      "\"3:11:1:225:6:3:1:201\", \"fidelity\": \"AUTHORED\" } ], \"authoredRows\": [ { \"id\": " +
                                      "\"A1\", \"fidelity\": \"PROXY\" }, { \"id\": \"A2\", \"fidelity\": \"AUTHORED\" } ] }");
        Check(mixed.Rows[0].Fidelity == TypeFidelity.Failed && mixed.AuthoredRows[0].Fidelity == TypeFidelity.Failed
              && mixed.AuthoredRows[1].Fidelity == TypeFidelity.Authored
              && map.FindByObjectType("3:11:1:225:6:3:1:201") == null
              && map.Lookup("UCI", 'H', "BDE", "friendly", "USA").Row.Id == "F-UCI-H",
              "AUTHORED stays a table defect among the LOOKUP rows (Failed, as typemap_check.py refuses it there); an " +
              "authoredRows entry of another fidelity is Failed; no authored type is ever looked up",
              $"{mixed.Rows[0].Fidelity} / {mixed.AuthoredRows[0].Fidelity} / {mixed.AuthoredRows[1].Fidelity}");
        var vendorLines = authoredRows.Select(a => CompositionVariants.AuthoredTypeLine(
            a, UnitTypeMap.ParseObjectType(a.ObjectType) is int[] t8 ? cat.Resolve(t8) : null, vendorRoot)).ToList();
        Check(vendorLines.All(l => !l.Ok && l.Text.StartsWith("TYPE MAP Authored: ", StringComparison.Ordinal)
                                   && l.Text.Contains("NOT its own UNIT")),
              "the TYPE MAP line prints the PARSED fidelity ('TYPE MAP Authored: ...'); on the SHIPPED catalogue each says " +
              "NOT its own UNIT", vendorLines[0].Text);

        // --- the start-up line ---
        string lineA = CompositionVariants.StartupLine(sa, ta, fd, derivedRoot, gOk, authoredRows.Count);
        string lineC = CompositionVariants.StartupLine(sc, tc, fv, vendorRoot, cShipped, authoredRows.Count);
        string lineN = CompositionVariants.StartupLine(sd, tc, fnone, vendorRoot, cNone, authoredRows.Count);
        Check(lineA.StartsWith("COMPOSITION VARIANT authored (Vrf:CompositionVariant; RL-20260927-04)", StringComparison.Ordinal)
              && lineA.Contains("loads C:\\C2SIM\\vrf-sms\\C2SIM_AggregateTacticalLevel.sms") && lineA.Contains("IN USE")
              && lineC.StartsWith("COMPOSITION VARIANT catalogue", StringComparison.Ordinal)
              && lineC.Contains("loads $(DATA_DIR)\\simulationModelSets\\AggregateTacticalLevel.sms")
              && lineC.Contains("needs no derived SMS") && lineC.Contains("NOT in use") && lineC.Contains("RL-20260927-04")
              && lineN.Contains("Vrf:Scenario is not set") && lineN.Contains("defaultVariant"),
              "the start-up line names the variant, the SMS the fixture loads (or why it is unknown) and the ruling id", lineA);

        // --- the service: ONE narrowing, before the refusal decision and before any row is resolved ---
        string svc = SafeRead(Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs"));
        string pre = Between(svc, "private bool ContainerStartupPreflight()", "private void PopulateInPlace(");
        int narrow = pre.IndexOf("_composition = _composition.ForVariant(variant.Name);", StringComparison.Ordinal);
        int refuse = pre.IndexOf("REFUSING TO START: {Problems}", StringComparison.Ordinal);
        int rowsAt = pre.IndexOf("foreach (var row in _composition.Rows)", StringComparison.Ordinal);
        Check(CountOf(svc, "_composition = _composition.ForVariant(") == 1 && narrow > 0 && narrow < refuse && refuse < rowsAt
              && pre.Contains("if (variant.Refused) problems.Add(variant.Refusal);")
              && pre.Contains("if (variantGuard.Refused) problems.Add(variantGuard.Refusal);")
              && CountOf(svc, "CompositionTable.Load(") == 1,
              "the service narrows the table ONCE, in the start-up preflight, before the refusal decision and before any row " +
              "resolves; an unknown variant and a failed guard are start refusals (PopulateInPlace resolves the narrowed table)",
              FormattableString.Invariant($"narrow {narrow}, refuse {refuse}, rows {rowsAt}"));

        // --- the DERIVED set: only when the authored variant is selected ---
        if (!authoredSelected)
        {
            Skip("the derived-set checks (the authored types land their own UNITs, the authored rings, the init rule unchanged " +
                 "on the derived chain, the guard on the LOADED derived catalogue)",
                 "not applicable: variant catalogue - run --populate-selftest --variant authored");
            return;
        }
        if (derived == null)
        {
            Check(false, "variant authored: the derived catalogue is available for the derived-set checks",
                  $"absent at {derivedSms} - tools/sms/Deploy-C2SimAggregateSms.ps1");
            return;
        }
        var ownLines = authoredRows.Select(a => CompositionVariants.AuthoredTypeLine(
            a, UnitTypeMap.ParseObjectType(a.ObjectType) is int[] t8 ? derived.Resolve(t8) : null, derivedSms)).ToList();
        Check(ownLines.All(l => l.Ok),
              "on the DERIVED catalogue each of the 7 authored types lands its OWN warfare-model UNIT (typemap_check.py's " +
              "authored gate, in the app's resolver)", string.Join(" | ", ownLines.Where(l => !l.Ok).Select(l => l.Text)));
        var rings = new Dictionary<string, (int N, double Spacing, double Radius, double Reach)>
        {
            ["C-USA-DIV-UCI-A"] = (1, 600.0, 0.0, 300.0), ["C-USA-BDE-UCI-A"] = (8, 180.0, 235.0, 835.0),
            ["C-USA-BDE-UCA-A"] = (29, 300.0, 1387.0, 1987.0),
        };
        foreach (var kv in rings)
        {
            var p = CompositionResolver.ExpandRow(ta.ById(kv.Key), ta, derived);
            var lay = PopulatePlanner.Plan("48_IBCT/28ID__AUTHORED", 54.0357, 23.2956, p.Leaves, 0.0);
            Check(!p.Refused && !lay.Refused && lay.Members.Count == kv.Value.N && Math.Abs(lay.SpacingMeters - kv.Value.Spacing) < 0.5
                  && Math.Abs(lay.RadiusMeters - kv.Value.Radius) < 1.0 && Math.Abs(lay.ReachMeters - kv.Value.Reach) < 1.0,
                  $"{kv.Key} on the derived catalogue: {kv.Value.N} member(s) on ONE ring, spacing {kv.Value.Spacing:F0} m, radius " +
                  $"{kv.Value.Radius:F0} m, reach {kv.Value.Reach:F0} m (composition_check.py --tree, FLAT)",
                  p.Refusal ?? FormattableString.Invariant($"{lay.Members.Count}, {lay.SpacingMeters:F1}, {lay.RadiusMeters:F1}, {lay.ReachMeters:F1} {lay.Refusal}"));
        }
        var init = InitParser.Parse(File.ReadAllText(Path.Combine(repo, "data", "IRONSTORM_CUTA_Initialization.xml")));
        var nations = new NationRoles("USA", "RUS");
        int same = 0, units = 0;
        foreach (var u in init.Units.Where(u => !string.IsNullOrEmpty(u.Latitude) && !string.IsNullOrEmpty(u.Longitude)))
        {
            units++;
            var plan = UnitTranslator.Plan(u with { ElevationAgl = "1000.0" }, TypeMapping.FidelityTable, map, nations);
            var cv = ContainerTypeRule.Choose(cat, plan.Type.Country, u.SymbolId, u.EchelonCode);
            var cd = ContainerTypeRule.Choose(derived, plan.Type.Country, u.SymbolId, u.EchelonCode);
            if (cv.TypeText == cd.TypeText && cv.TemplateName == cd.TemplateName && cv.ExactBranch == cd.ExactBranch) same++;
        }
        Check(units == 36 && same == 36,
              "the init rule on the DERIVED catalogue gives all 36 Iron Storm units the SAME container as the vendor set (the " +
              "derived set only ADDS)", $"{same} of {units}");
        var gLoaded = CompositionVariants.Check(ta, sa, authoredRows, fd, derived.Resolver.RootSms, home);
        Check(!gLoaded.Refused,
              "THE GUARD on the LOADED derived catalogue (its own RootSms) and the derived fixture -> accepted end to end",
              gLoaded.Refusal ?? derived.Describe);
    }

    // ---------------------------------------------------------------------------------------------- helpers ----
    private static string ArgAfter(string flag)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < a.Length; i++)
            if (string.Equals(a[i], flag, StringComparison.OrdinalIgnoreCase)) return a[i + 1];
        return null;
    }

    private static void Skip(string label, string reason)
    {
        _skip++;
        Console.WriteLine($"  [SKIP] SKIPPED: {label}  ({reason})");
    }

    private sealed class SyntheticCatalogue : ICatalogue
    {
        private readonly Dictionary<string, CatalogueEntry> _byType = new(StringComparer.Ordinal);
        public void Add(int[] t8, string name, CatalogueRole role, double? footprint, params SubordinateSpec[] subs)
            => _byType[string.Join(":", t8)] = new CatalogueEntry(name, name, role, t8, subs, footprint);
        public CatalogueEntry Resolve(int[] type8) => type8 != null && _byType.TryGetValue(string.Join(":", type8), out var e) ? e : null;
        public string Describe => $"synthetic, {_byType.Count} entries";
    }

    private static string SafeRead(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

    private static string Between(string s, string from, string to)
    {
        int a = s.IndexOf(from, StringComparison.Ordinal);
        if (a < 0) return "";
        int b = s.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        return b < 0 ? s.Substring(a) : s.Substring(a, b - a);
    }

    private static int CountOf(string s, string needle)
    {
        int n = 0;
        for (int i = s.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = s.IndexOf(needle, i + 1, StringComparison.Ordinal)) n++;
        return n;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "COA-STP1_Order.xml"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static void Check(bool ok, string label, string detail = "")
    {
        if (ok) _pass++; else _fail++;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}{(string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")")}");
    }

    private static int Finish()
    {
        // A SKIP is never a pass: it is counted apart and named (not applicable to the selected variant).
        string skipped = _skip == 0 ? "" : $" ({_skip} SKIPPED as not applicable to the selected variant - grep [SKIP])";
        Console.WriteLine(_fail == 0
            ? $"populate-selftest: ALL {_pass} CHECKS PASSED{skipped}"
            : $"populate-selftest: {_fail} FAILED, {_pass} passed{skipped}");
        return _fail == 0 ? 0 : 1;
    }
}
