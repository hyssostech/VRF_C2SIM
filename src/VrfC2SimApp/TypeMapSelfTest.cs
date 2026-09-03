namespace VrfC2SimApp;

/// <summary>
/// Offline check of the fidelity mapping (`--typemap-selftest`). Four parts, per
/// docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 7.5 items 1-2:
///
///   A. TABLE - the type map loads and is internally consistent. WHICH map follows the installed
///      catalog: data/unit-type-map.json for the 5.0.2 C2simEx chain, data/unit-type-map-52.json
///      for the 5.2d EntityLevel chain (docs/VRF_5.2_MIGRATION_DIFF.md sec F / Y-8); the run
///      header names both so a pass can never be misread as covering the other catalog.
///   B. RESOLVER - every row's objectType lands the template the row NAMES, under the vendor's
///      best-match rule, against the SMS chain installed at VRF_HOME (default C:\MAK\vrforces5.0.2).
///      The resolver itself is re-validated 6/6 against docs/VRF_GROUND_TRUTH.md sec 0.1.5 FIRST,
///      so a broken resolver cannot silently pass the rows. SKIPS LOUDLY (exit 0, banner) when the
///      MAK install is absent - it never silently passes.
///   C. COMPOSITION - the transitive subordinate expansion of every landed template contains no
///      Ground_Aggregate and no zero-subordinate abstract (the sec 3.5/3.6 traps). A template's own
///      subordinate list is subject to the SAME best-match rule, so "it is a real composed unit"
///      must be verified transitively.
///   D. LOOKUP - key order (a)-(e), the JC-1 coverage backstop, the DISCountry override, and the
///      JC-2 PRC refuse-to-start, as pure unit checks with no MAK dependency.
///
/// Needs the MAK bin dirs on PATH only because the app links the bridge for its value types
/// (same as --translator-selftest); it does NOT start VR-Forces and writes nothing under C:\MAK.
/// </summary>
public static class TypeMapSelfTest
{
    private static int _fail;
    private static int _pass;

    public static int Run()
    {
        Console.WriteLine("=== unit-type-map fidelity self-test ===");

        // Which catalog is installed decides which table is under test (see the class comment).
        string home = ObjectTypeResolver.DefaultVrfHome;
        bool haveCatalog = Directory.Exists(ObjectTypeResolver.ModelSetsDir(home));
        ObjectTypeResolver res = haveCatalog ? ObjectTypeResolver.LoadChain(home) : null;
        string mapFile = res != null && res.RootSms == "EntityLevel" ? "data/unit-type-map-52.json" : "data/unit-type-map.json";
        Console.WriteLine($"table under test: {mapFile} (catalog root {(res?.RootSms ?? "none - no MAK install at " + home)})");

        // ---- A. the table -------------------------------------------------
        string path = UnitTypeMap.ResolvePath(mapFile);
        if (path == null)
        {
            Console.WriteLine("[FAIL] " + mapFile + " not found (cwd=" +
                              Directory.GetCurrentDirectory() + ", app=" + AppContext.BaseDirectory + ")");
            return 1;
        }
        UnitTypeMap map;
        try { map = UnitTypeMap.Load(path); }
        catch (Exception ex) { Console.WriteLine("[FAIL] load " + path + ": " + ex.Message); return 1; }
        Console.WriteLine($"table: {map.Rows.Count} rows from {path}");

        CheckTable(map);

        // ---- D. lookup semantics (no MAK needed) --------------------------
        CheckLookup(map);

        // ---- B + C. the installed catalog ---------------------------------
        if (res == null)
        {
            Console.WriteLine();
            Console.WriteLine("*** SKIPPED the RESOLVER and COMPOSITION checks (parts B and C): no VR-Forces");
            Console.WriteLine("*** model sets at " + ObjectTypeResolver.ModelSetsDir(home) + ".");
            Console.WriteLine("*** Set VRF_HOME to the install root to run them. Every objectType in the table");
            Console.WriteLine("*** is UNVERIFIED against the catalog in this run - this is NOT a pass.");
        }
        else
        {
            Console.WriteLine($"catalog: {res.Templates.Count} simObjects from " +
                              string.Join(" -> ", res.ModelSetDirs.Select(d => Path.GetFileName(Path.GetDirectoryName(d)))) +
                              $" under {home}");
            // Which catalog this run actually proved against (5.0.2 = C2simEx root, 8-field types;
            // 5.2d = EntityLevel root, 7-field types normalised - docs/VRF_5.2_MIGRATION_DIFF.md sec F).
            Console.WriteLine($"catalog: root {res.RootSms}.sms; {res.SevenFieldTypes} seven-field type strings normalised" +
                              (res.SevenFieldTypes == 0 ? " (8-field 5.0.2 form)" : " (7-field 5.2 form)"));
            CheckResolverAgainstGroundTruth(res);
            CheckRows(map, res);
        }

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? $"SELF-TEST PASSED ({_pass} checks)"
            : $"SELF-TEST FAILED ({_fail} of {_pass + _fail} checks)");
        return _fail == 0 ? 0 : 1;
    }

    // ---- A ----------------------------------------------------------------

    private static void CheckTable(UnitTypeMap map)
    {
        Console.WriteLine();
        Console.WriteLine("-- A. table consistency --");

        Report("row ids unique", map.Rows.Select(r => r.Id).Distinct().Count() == map.Rows.Count,
               "duplicate ids: " + string.Join(", ", map.Rows.GroupBy(r => r.Id)
                   .Where(g => g.Count() > 1).Select(g => g.Key)));

        Report("nations block has USA/RUS/PRC",
               map.Nations.ContainsKey("USA") && map.Nations.ContainsKey("RUS") && map.Nations.ContainsKey("PRC"),
               "got: " + string.Join(", ", map.Nations.Keys));

        foreach (var r in map.Rows)
        {
            if (r.Fidelity == TypeFidelity.AuthoredPending)
            {
                Report($"{r.Id}: AUTHORED_PENDING carries no objectType",
                       r.ObjectType.Length == 0, "objectType=" + r.ObjectType);
                Report($"{r.Id}: AUTHORED_PENDING states the gap", r.ProxyNote.Length > 0, "empty proxyNote");
                continue;
            }
            Report($"{r.Id}: fidelity is a known value", r.Fidelity != TypeFidelity.Failed,
                   "unparsable fidelity");
            var f = r.Fields;
            Report($"{r.Id}: objectType is 8 fields", f != null, "objectType='" + r.ObjectType + "'");
            if (f == null) continue;
            Report($"{r.Id}: superType {f[0]} agrees with isAggregate={r.IsAggregate}",
                   (f[0] == 3) == r.IsAggregate, "superType 3 = unit, 1 = individual");
            Report($"{r.Id}: names a template", r.TemplateName.Length > 0, "empty templateName");
            if (r.Fidelity == TypeFidelity.Proxy)
                Report($"{r.Id}: PROXY states the substitution", r.ProxyNote.Length > 0, "empty proxyNote");
            // Never an intentional generic: Ground_Aggregate's own type and the two types the
            // survey (sec 2.3) proves fall through to it must not appear as a target.
            Report($"{r.Id}: not an intentional Ground_Aggregate",
                   r.ObjectType is not ("3:11:1:0:0:0:0:0" or "3:11:1:225:1:1:3:0" or "3:11:1:225:5:20:0:0"
                                        or "3:11:1:225:2:1:1:0"),
                   "objectType=" + r.ObjectType);
        }

        // Both nation roles must have a catch-all, or key (e) cannot fire.
        foreach (var (role, nation) in new[] { ("friendly", "USA"), ("hostile", "RUS"), ("hostile", "PRC") })
            Report($"catch-all row exists for {role}/{nation}",
                   map.Rows.Any(r => r.NationRole == role && r.Nation == nation
                                     && r.FunctionId.Length == 0 && r.Echelon == '*'), "");
    }

    // ---- B ----------------------------------------------------------------

    // docs/VRF_GROUND_TRUTH.md sec 0.1.5 records these six resolutions as VERIFIED (on 5.0.2, against
    // the C2simEx chain). If the resolver cannot reproduce them, nothing it says about the rows is
    // worth anything - so this runs first. On the 5.2d EntityLevel chain the Mobile Irregular row is
    // meaningless (the template lived only in C2simEx) and the other five are the 5.0.2 truth
    // carried over: 5.2 ground truth is NOT established until the creation-line gate on a live 5.2
    // back end (docs/VRF_5.2_MIGRATION_DIFF.md sec F) - the banner says so.
    private static void CheckResolverAgainstGroundTruth(ObjectTypeResolver res)
    {
        bool c2simEx = res.RootSms == "C2simEx";
        Console.WriteLine();
        Console.WriteLine("-- B0. resolver vs VRF_GROUND_TRUTH sec 0.1.5 (6 VERIFIED resolutions) --");
        if (!c2simEx)
            Console.WriteLine("       root " + res.RootSms + ": 5.0.2 truth replayed on the 5.2 catalog (C2simEx-only row skipped); " +
                              "5.2 truth itself is UNVERIFIED until the live creation-line gate.");
        var truth = new (string Query, string Template)[]
        {
            ("3:11:1:225:5:2:0:0",  "Tank Company (USA)"),
            ("3:11:1:225:1:1:3:0",  "Ground_Aggregate"),
            ("3:11:1:225:5:20:0:0", "Ground_Aggregate"),
            ("3:11:1:225:2:1:1:0",  "Ground_Aggregate"),
            ("3:11:1:0:13:34:0:1",  "Mobile Irregular"),
            ("3:11:1:225:3:2:0:0",  "Tank Platoon (USA)"),
        };
        foreach (var (q, expected) in truth)
        {
            if (!c2simEx && expected == "Mobile Irregular") continue;
            var landed = res.Resolve(q);
            Report($"{q} -> {expected}", landed != null && landed.Name == expected,
                   "got " + (landed?.Name ?? "(no match)"));
        }
    }

    private static void CheckRows(UnitTypeMap map, ObjectTypeResolver res)
    {
        Console.WriteLine();
        Console.WriteLine("-- B. every row lands its named template --");
        foreach (var r in map.Rows)
        {
            var f = r.Fields;
            if (f == null) continue;                       // AUTHORED_PENDING: nothing to resolve
            var winners = res.ResolveAll(f);
            // A vendor DUPLICATE (two files, identical objectType/matchType/subordinates - e.g.
            // "Fire Support Team (USA)" and "Fire_Support_Team") is a tie the vendor created, not a
            // table defect: accept the named template anywhere in the tied set.
            bool ok = winners.Any(w => w.Name == r.TemplateName);
            Report($"{r.Id}: {r.ObjectType} -> {r.TemplateName}", ok,
                   "landed " + (winners.Count == 0 ? "(no match)"
                                : string.Join(" | ", winners.Select(w => w.Name))));
            if (ok && winners.Count > 1)
                Console.WriteLine($"       note: {winners.Count}-way vendor tie ({string.Join(" | ", winners.Select(w => w.Name))})");
        }

        Console.WriteLine();
        Console.WriteLine("-- C. no landed template expands to a generic or an empty abstract --");
        foreach (var r in map.Rows)
        {
            var f = r.Fields;
            if (f == null || !r.IsAggregate) continue;     // entities have no subordinate list
            var traps = new List<string>();
            foreach (var (depth, query, landed) in res.Expand(f))
            {
                if (depth == 0) continue;
                string q = string.Join(':', query);
                if (landed == null) traps.Add($"d{depth} {q} -> NO MATCH");
                else if (landed.Name == "Ground_Aggregate") traps.Add($"d{depth} {q} -> Ground_Aggregate");
                else if (landed.IsUnit && landed.Subordinates.Count == 0)
                    traps.Add($"d{depth} {q} -> {landed.Name} (zero-subordinate abstract)");
            }
            Report($"{r.Id}: {r.TemplateName} composition is real", traps.Count == 0,
                   string.Join("; ", traps.Distinct()));
        }
    }

    // ---- D ----------------------------------------------------------------

    private static void CheckLookup(UnitTypeMap map)
    {
        Console.WriteLine();
        Console.WriteLine("-- D. lookup key order, backstop, overrides, refuse-to-start --");
        var nations = new NationRoles("USA", "RUS");

        // key extraction
        Report("functionId of SFGPUCIZ---D--- is UCIZ",
               UnitTypeMap.FunctionIdOf("SFGPUCIZ---D---") == "UCIZ", UnitTypeMap.FunctionIdOf("SFGPUCIZ---D---"));
        Report("functionId of a blank field is (none)",
               UnitTypeMap.FunctionIdOf("SFGP-------E---") == "(none)", UnitTypeMap.FunctionIdOf("SFGP-------E---"));
        Report("echelon char of SFGPUCIZ--EH--- is H",
               UnitTypeMap.EchelonCharOf("SFGPUCIZ--EH---") == 'H', UnitTypeMap.EchelonCharOf("SFGPUCIZ--EH---").ToString());

        // (b) functionId + SIDC echelon
        var m = map.Lookup("UCA", 'E', "COY", "friendly", "USA");
        Report("(b) UCA/E/friendly-USA -> Tank Company (USA)",
               m.KeyUsed.StartsWith("b:") && m.Row.TemplateName == "Tank Company (USA)",
               $"{m.KeyUsed} {m.Row.TemplateName}");

        // (c) EchelonCode cross-check: a SIDC echelon character with no row, EchelonCode that has one
        m = map.Lookup("UCA", 'Z', "COY", "friendly", "USA");
        Report("(c) UCA/echelon 'Z' falls to EchelonCode COY",
               m.KeyUsed.StartsWith("c:") && m.Row.Id == "F-UCA-E", $"{m.KeyUsed} {m.Row.Id}");

        // (d) echelon-only fallback for an unknown function ID
        m = map.Lookup("ZZZZ", 'D', "PLT", "friendly", "USA");
        Report("(d) unknown functionId at echelon D -> the echelon-only row",
               m.KeyUsed.StartsWith("d:") && m.Row.Id == "F-GEN-D", $"{m.KeyUsed} {m.Row.Id}");

        // (e) catch-all, and it is NOT an aggregate generic
        m = map.Lookup("ZZZZ", 'Q', "NKN", "hostile", "RUS");
        Report("(e) unknown functionId AND echelon -> the catch-all row",
               m.KeyUsed.StartsWith("e:") && m.Row.Id == "H-GEN-ANY", $"{m.KeyUsed} {m.Row.Id}");
        Report("(e) the catch-all is a single platform, not a generic aggregate",
               !m.Row.IsAggregate, "isAggregate=" + m.Row.IsAggregate);

        // nation role selection
        Report("hostile role selects OpposingNation",
               UnitTypeMap.NationFor(nations, hostile: true) == "RUS", UnitTypeMap.NationFor(nations, true));
        m = map.Lookup("UCA", 'E', "COY", "hostile", "RUS");
        Report("hostile UCA/E/RUS -> Tank Company (RUS)",
               m.Row.TemplateName == "Tank Company (RUS)", m.Row.TemplateName);

        // key (a): the init's own SISOEntityType, and its coverage backstop (JC-1)
        Report("init DIS 11.1.225.3.4.0.0 -> 3:11:1:225:3:4:0:0",
               UnitTypeMap.VrfObjectTypeFromInitDis("11.1.225.3.4.0.0") == "3:11:1:225:3:4:0:0",
               UnitTypeMap.VrfObjectTypeFromInitDis("11.1.225.3.4.0.0"));
        Report("an all-zero SISOEntityType is no type at all (COA-STP1)",
               UnitTypeMap.VrfObjectTypeFromInitDis("0.0.0.0.0.0.0") == null, "");
        Report("(a) the R9 mech-platoon type IS covered by the table",
               map.FindByObjectType("3:11:1:225:3:4:0:0")?.TemplateName == "Mechanized Platoon (USA) IFV (Deprecated)",
               map.FindByObjectType("3:11:1:225:3:4:0:0")?.TemplateName ?? "(none)");
        Report("(a) backstop: the R9 Country-153 type is NOT covered",
               map.FindByObjectType("3:11:1:153:5:4:0:0") == null, "a Country-153 row appeared");

        // The R9 lean init, end to end through Plan(): the init's own type must WIN over the
        // SIDC-derived armor default (survey sec 4.2 - the port throws that type away today).
        var r9 = Unit("SFGPUCIZ---D---", "11.1.225.3.4.0.0", "PLT", hostile: false);
        var plan = UnitTranslator.Plan(r9, TypeMapping.FidelityTable, map, nations);
        Report("(a) R9 1222.MechPlt: init type wins -> Mechanized Platoon (USA) IFV",
               plan.TemplateName == "Mechanized Platoon (USA) IFV (Deprecated)"
               && plan.Type.Category == 3 && plan.Type.Subcategory == 4 && plan.IsAggregate,
               $"{plan.TemplateName} {plan.Type.Category}/{plan.Type.Subcategory}");

        // ... and a type the table does not cover falls back to the SIDC row instead of creating
        // an empty Country-153 abstract.
        var nl = Unit("SFGPUCIZ--EH---", "11.1.153.5.4.0.0", "BDE", hostile: false);   // R9 1.BdeHQ, verbatim
        plan = UnitTranslator.Plan(nl, TypeMapping.FidelityTable, map, nations);
        Report("(a) R9 1.BdeHQ: uncovered Country-153 type -> SIDC row (M577A2_Command_Post)",
               plan.TemplateName == "M577A2_Command_Post" && !plan.IsAggregate,
               plan.TemplateName + " agg=" + plan.IsAggregate);
        Report("(a) the backstop says so in the note",
               plan.MapNote.Contains("backstop", StringComparison.OrdinalIgnoreCase), plan.MapNote);

        // sec 7.3: a per-unit DISCountry overrides the configured nation.
        var ru = Unit("SHGPUCA----E---", "11.1.222.5.2.0.0", "COY", hostile: true);
        plan = UnitTranslator.Plan(ru, TypeMapping.FidelityTable, map, new NationRoles("USA", "PRC"));
        Report("sec 7.3: init DISCountry 222 overrides OpposingNation=PRC",
               plan.TemplateName == "Tank Company (RUS)", plan.TemplateName);

        // COA-STP1: all-zero SISOEntityType, so the SIDC rows do the work.
        var coa = Unit("SHGPUCFHE--E---", "0.0.0.0.0.0.0", "COY", hostile: true);
        plan = UnitTranslator.Plan(coa, TypeMapping.FidelityTable, map, nations);
        Report("COA-STP1 hostile UCFHE/E -> the US M109 battery PROXY (no RUS artillery exists)",
               plan.Fidelity == TypeFidelity.Proxy && plan.TemplateName == "Field Artillery Battery (USA) M109",
               $"{plan.Fidelity} {plan.TemplateName}");
        Report("that PROXY carries a substitution string for R-SURFACE-PROXY",
               plan.Substitution.Contains("WRONG NATION"), plan.Substitution);

        var exact = Unit("SFGPUCA----E---", "0.0.0.0.0.0.0", "COY", hostile: false);
        plan = UnitTranslator.Plan(exact, TypeMapping.FidelityTable, map, nations);
        Report("an EXACT row surfaces NO substitution",
               plan.Fidelity == TypeFidelity.Exact && plan.Substitution.Length == 0,
               $"{plan.Fidelity} '{plan.Substitution}'");

        // An AUTHORED_PENDING row must fail loudly, never emit a type.
        var prc = Unit("SHGPUCA----E---", "0.0.0.0.0.0.0", "COY", hostile: true);
        plan = UnitTranslator.Plan(prc, TypeMapping.FidelityTable, map, new NationRoles("USA", "PRC"));
        Report("PRC UCA/E is AUTHORED_PENDING and emits no type",
               plan.Fidelity == TypeFidelity.AuthoredPending && plan.Type.Category == 0,
               $"{plan.Fidelity} cat={plan.Type.Category}");

        // JC-2: PRC refuses to start; RUS and USA do not.
        Report("JC-2: OpposingNation=PRC refuses to start",
               map.CheckOpposingNationSupported("PRC") is { Length: > 0 }, "no error returned");
        Report("JC-2: the refusal names the missing content",
               (map.CheckOpposingNationSupported("PRC") ?? "").Contains("AUTHORED_PENDING")
               && (map.CheckOpposingNationSupported("PRC") ?? "").Contains("Country-45"),
               map.CheckOpposingNationSupported("PRC"));
        Report("JC-2: OpposingNation=RUS starts", map.CheckOpposingNationSupported("RUS") == null,
               map.CheckOpposingNationSupported("RUS"));
        Report("an unknown OpposingNation refuses to start",
               map.CheckOpposingNationSupported("ZZZ") is { Length: > 0 }, "no error returned");
        // The same hole exists on the friendly side; the check is role-parameterized.
        Report("FriendlyNation=USA starts",
               map.CheckNationSupported("friendly", "USA") == null,
               map.CheckNationSupported("friendly", "USA"));
        Report("FriendlyNation=PRC refuses to start (no friendly PRC rows)",
               map.CheckNationSupported("friendly", "PRC") is { Length: > 0 }, "no error returned");
        Report("the friendly refusal names the FriendlyNation setting",
               (map.CheckNationSupported("friendly", "PRC") ?? "").Contains("Vrf:FriendlyNation"),
               map.CheckNationSupported("friendly", "PRC"));

        // The two legacy modes must be untouched by all of the above.
        var legacy = Unit("SFGPUCIZ---D---", "11.1.225.3.4.0.0", "PLT", hostile: false);
        var real = UnitTranslator.Plan(legacy, TypeMapping.RealTemplates, map, nations);
        Report("RealTemplates is unchanged (still Tank Platoon (USA) 11.1.225.3.2.0.0)",
               real.Type.Category == 3 && real.Type.Subcategory == 2
               && real.Fidelity == TypeFidelity.Unspecified,
               $"{real.Type.Category}/{real.Type.Subcategory} {real.Fidelity}");
        var golden = UnitTranslator.Plan(legacy, TypeMapping.GoldenParity, map, nations);
        Report("GoldenParity is unchanged (still 11.1.225.1.1.3.0)",
               golden.Type.Category == 1 && golden.Type.Subcategory == 1 && golden.Type.Specific == 3,
               $"{golden.Type.Category}/{golden.Type.Subcategory}/{golden.Type.Specific}");
    }

    // ---- helpers ----------------------------------------------------------

    private static InitUnit Unit(string sidc, string disType, string echelonCode, bool hostile)
        => new()
        {
            Name = "u",
            Uuid = "uuid",
            SystemName = "STP",
            HostilityCode = hostile ? "HO" : "FR",
            Latitude = "50.0",
            Longitude = "7.0",
            ElevationAgl = "1000.0",
            SymbolId = sidc,
            DisEntityType = disType,
            EchelonCode = echelonCode,
            DirectionPhi = "",
        };

    private static void Report(string label, bool ok, string detail)
    {
        if (ok) _pass++; else _fail++;
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {label}{(ok ? "" : "  <- " + detail)}");
    }
}
