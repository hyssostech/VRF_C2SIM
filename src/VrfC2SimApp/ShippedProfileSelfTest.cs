using Microsoft.Extensions.Configuration;

namespace VrfC2SimApp;

/// <summary>
/// THE OVERLAY-ONLY SETTLEMENTS, PINNED WHERE THEY SHIP (AUDIT_RULINGS_IN_CODE_2026-09-28 sec 4 fix 1; sec 3 item 1).
/// A section of `--rulings-selftest`: the values the ledger settled are read through the REAL configuration stack - the
/// two shipped json files layered as the Host layers them (the RulingsSelfTest C14 / VertexChainSelfTest v5 pattern) -
/// plus scripts/RunScenario.sh's exports and data/unit-type-map-52-aggregate.json. It PINS WHAT SHIPS. Where the base
/// default disagrees with a ruling it pins the base as it is and names the disagreement in its message: choosing the
/// base default is the owner's call (the audit's sec 5), not this test's, and VrfSettings.cs is not changed here.
///
///   (s1) RL-20260902-01 TypeMappingMode    (s2) RL-20260906-02 CreationPolicy   (s3) RL-20260925-01 Q3 StallDetection
///   (s4) RL-20260913-03 the stall window, on the SIM clock in the demo since 2026-10-05
///   (s5) the ModelSet: EntityLevel in code and the base, AggregateTacticalLevel in the Demo overlay since 2026-10-05 - and
///        the D2b guard, fed the overlay's own value, ACCEPTS STP's raw Iron Storm export (RL-20260927-06; RL-20261004-05)
///   (s6)-(s7) RL-20260927-02 the hostile side RUS, in the settings and in the aggregate map (nation 260)
///   (s8) Y-2 the config-file identity      (s9) C1/C2 ComposeHierarchy          (s10) C4 AggregateFormation OFF
///   (s11) DIRTY CONTROLS: each demo pin, re-read with its one key flipped in memory, must FAIL.
///   (s12) RL-20260928-03 the planned container move: Vrf:AggregateMovePlanner=Auto everywhere it ships (+ its dirty control).
///   (s13) M3b (the G1-3 Result) the EXECUTOR REFUSED bar: Vrf:VertexArrivalRadiusMeters 100 everywhere it ships (+ its dirty
///         control) - 0 would let a planned vertex that moved nothing advance again.
///   (s14) RL-20261005-02 the demo terrain extent: OFF in the base, the Iron Storm centre + 2 km in the Demo overlay (+ its
///         dirty control).
///   (s15) THE DEMO OVERLAY IS THE AGGREGATE IRON STORM PROFILE, COMPLETE (2026-10-05): the keys that must travel with
///         Vrf:ModelSet=AggregateTacticalLevel (type map, scenario, composition) agree with it, and the runner values of the
///         rehearsed runs (StallClock sim, TaskPredecessorTimeoutSeconds 600) are what it carries; DurationScale is pinned
///         AS IT IS (the owner's open decision). The pins of s4/s5/s15 FAIL on the overlay as it was before 2026-10-05.
/// </summary>
public static class ShippedProfileSelfTest
{
    public static int Run()
    {
        int failures = 0;
        string repo = FindRepoRoot();
        string appSettings = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
        string demoSettings = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");
        string wrapper = repo == null ? null : Path.Combine(repo, "scripts", "RunScenario.sh");
        string aggMap = repo == null ? null : Path.Combine(repo, "data", "unit-type-map-52-aggregate.json");
        bool present = repo != null && File.Exists(appSettings) && File.Exists(demoSettings) && File.Exists(wrapper) && File.Exists(aggMap);
        Check(ref failures, present, $"(s0) the shipped files are on disk: both appsettings, RunScenario.sh, the aggregate map ({repo})");
        if (!present) return failures;

        var compiled = new VrfSettings();
        IConfiguration baseCfg = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false).Build();
        IConfiguration demoCfg = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false)
                                                           .AddJsonFile(demoSettings, optional: false).Build();
        var shipped = baseCfg.GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        var demo = demoCfg.GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        string rsh = File.ReadAllText(wrapper);
        bool Exported(string kv) => rsh.Contains("\nexport " + kv + "\r\n", StringComparison.Ordinal)
                                    || rsh.Contains("\nexport " + kv + "\n", StringComparison.Ordinal);

        // (s1) RL-20260902-01 - best fidelity: every unit as its correct VR-Forces type (the fidelity table).
        Check(ref failures, demo.TypeMappingMode == "FidelityTable" && Exported("Vrf__TypeMappingMode=FidelityTable"),
              "(s1) RL-20260902-01: the Demo overlay and RunScenario.sh's export carry TypeMappingMode=FidelityTable");
        Check(ref failures, shipped.TypeMappingMode == "RealTemplates" && compiled.TypeMappingMode == "RealTemplates",
              "(s1) WHAT THE BASE SHIPS, pinned as it is - it DISAGREES with RL-20260902-01: appsettings.json and VrfSettings.cs " +
              "default to RealTemplates (the 5.0.2 parity dispatch - every company a Tank Company (USA), hostile included), so a " +
              "bare exe or a direct RunC2SimScenario.ps1 run does not get the ruling. Flipping the base is the owner's call " +
              "(audit 2026-09-28 sec 5); this test does not choose",
              $"base {shipped.TypeMappingMode}, compiled {compiled.TypeMappingMode}");

        // (s2) RL-20260906-02 (C13) - simulate only the taskees; the init is ORBAT context.
        Check(ref failures, demo.MaterializeAtOrder && Exported("Vrf__CreationPolicy=AtOrder"),
              "(s2) RL-20260906-02: the Demo overlay and RunScenario.sh's export carry CreationPolicy=AtOrder");
        Check(ref failures, baseCfg["Vrf:CreationPolicy"] == null && !shipped.MaterializeAtOrder && compiled.CreationPolicy == "AtInit",
              "(s2) WHAT THE BASE SHIPS, pinned as it is - it DISAGREES with RL-20260906-02: appsettings.json has no " +
              "CreationPolicy key and VrfSettings.cs defaults to AtInit (the whole ORBAT created in full); the owner's call",
              $"base key '{baseCfg["Vrf:CreationPolicy"]}', compiled {compiled.CreationPolicy}");

        // (s3) RL-20260925-01 Q3 - stall detection ON in the demo profile, and only there.
        Check(ref failures, demo.StallDetection && !shipped.StallDetection && !compiled.StallDetection,
              "(s3) RL-20260925-01 Q3: StallDetection is ON in the Demo overlay and OFF in appsettings.json and VrfSettings.cs " +
              "- ON IN THE DEMO PROFILE ONLY, as ruled");

        // (s4) RL-20260913-03 - the stall window. What ships is the 2026-09-13 calibration, not the approved figure. Since
        // 2026-10-05 the Demo overlay runs it on the SIM clock, as every rehearsed Iron Storm run did (G1-6: "the watchdog on
        // the SIMULATION clock, window 360 SIM s, 50 m"; the bounded rehearsal's --env Vrf__StallClock=sim). The base and the
        // code stay on wall.
        double demoWindow = ResolvedWindow(demo);
        bool demoSimValid;
        bool demoPrefersSim = StallPolicy.ParseClockPreference(demo.StallClock, out demoSimValid);
        Check(ref failures, demo.StallWindowSeconds == 0 && demoSimValid && demoPrefersSim
                            && demoWindow == StallPolicy.CalibratedWindowSimSeconds && demoWindow == 360.0
                            && StallPolicy.ResolveWindowSeconds(0, usingSimClock: false) == 240.0 && demo.StallMoveMeters == 50.0
                            && compiled.StallClock == "wall" && baseCfg["Vrf:StallClock"] == null,
              "(s4) THE STALL WINDOW AS IT SHIPS, pinned as it is - it DIFFERS from RL-20260913-03's approved 120 SIMULATED s: the " +
              "demo profile resolves to 360 SIM s (Vrf:StallClock=sim since 2026-10-05, the rehearsed runs' clock; " +
              "StallWindowSeconds 0 = the 2026-09-13 calibration; 240 s on the wall clock, which the code and appsettings.json " +
              "keep), 50 m. No ledger line records the owner accepting the window figure (audit 2026-09-28 sec 5); this test " +
              "does not choose",
              $"window {demoWindow} s, clock '{demo.StallClock}', {demo.StallMoveMeters} m, compiled clock '{compiled.StallClock}', " +
              $"base key '{baseCfg["Vrf:StallClock"]}'");

        // (s5) The model set: EntityLevel by default in code and the base (the runner chooses per order, RL-20260927-06); the
        // Demo overlay - the hand-started Way B path (RL-20261004-05), which chooses nothing - is AggregateTacticalLevel since
        // 2026-10-05, because the demo is Iron Storm and above BN is aggregate-only (RL-20260927-06, RL-20260928-01).
        Check(ref failures, compiled.ModelSet == "EntityLevel" && shipped.ModelSet == "EntityLevel" && demo.ModelSet == "AggregateTacticalLevel",
              "(s5) Vrf:ModelSet is EntityLevel in VrfSettings.cs and appsettings.json (the runner chooses per order, " +
              "RL-20260927-06) and AggregateTacticalLevel in the Demo overlay (the Iron Storm demo, RL-20261004-05)",
              $"compiled {compiled.ModelSet}, base {shipped.ModelSet}, demo {demo.ModelSet}");
        // ... and THE CONSEQUENCE, through the app's own code path: the RAW key as VrfC2SimService reads it
        // (config "Vrf:ModelSet" -> UnitPositionPolicy.TryParseModelSet), the D2b guard (ModelSetGuard.Decide) over STP's RAW
        // Iron Storm export - the order Way B pushes. Under the base it is REFUSED (the control: the guard bites); under the
        // Demo overlay it is ALLOWED. On the overlay as it was before 2026-10-05 (EntityLevel) this check FAILS.
        string isInit = Path.Combine(repo, "data", "STP-IRON-STORM-SYNTHETIC_Initialization.xml");
        string isOrder = Path.Combine(repo, "data", "STP-IRON-STORM-SYNTHETIC_Order.xml");
        if (File.Exists(isInit) && File.Exists(isOrder))
        {
            var echelons = ModelSetGuard.ReadInit(File.ReadAllText(isInit));
            var performers = OrderParser.Parse(File.ReadAllText(isOrder)).Tasks.Select(t => t.TaskeeUuid).ToList();
            ModelSetVerdict Guard(IConfiguration cfg)
            {
                string raw = cfg.GetSection("Vrf")["ModelSet"];
                UnitPositionPolicy.TryParseModelSet(raw, out bool agg);
                return ModelSetGuard.Decide(agg, raw, performers, echelons.Units, echelons.Other);
            }
            var onBase = Guard(baseCfg);
            var onDemo = Guard(demoCfg);
            Check(ref failures, echelons.Parsed && performers.Count == 23 && onBase.Kind == ModelSetCheckKind.Refused
                                && onBase.Highest == "DIV",
                  "(s5) CONTROL: STP's raw Iron Storm export (23 tasks, highest TASKED echelon DIV) is REFUSED by the D2b guard " +
                  "under appsettings.json alone (EntityLevel) - the guard bites on this order",
                  $"parsed {echelons.Parsed}, tasks {performers.Count}, kind {onBase.Kind}, highest {onBase.Highest}");
            Check(ref failures, onDemo.Kind == ModelSetCheckKind.AllowedAggregate && onDemo.AbortReason == ""
                                && onDemo.Line.StartsWith("MODEL SET RULE (D2b): allowed", StringComparison.Ordinal),
                  "(s5) the SAME order under appsettings.json + appsettings.Demo.json (StartInterface52.ps1's profile, no hand-set " +
                  "env) is ALLOWED by the D2b guard - the Iron Storm order Way B pushes is no longer refused at receipt",
                  $"kind {onDemo.Kind}: {onDemo.Line}");
        }
        else Check(ref failures, false, $"(s5) STP's raw Iron Storm export is on disk ({isInit}, {isOrder})");

        // (s6) RL-20260927-02 (1) - the hostile side is RUS.
        Check(ref failures, compiled.OpposingNation == "RUS" && shipped.OpposingNation == "RUS" && demo.OpposingNation == "RUS"
                            && compiled.FriendlyNation == "USA",
              "(s6) RL-20260927-02 (\"1. RUS\"): Vrf:OpposingNation is RUS in VrfSettings.cs, appsettings.json and the Demo " +
              "overlay (friendly USA)");

        // (s7) ... and in the aggregate map, where RUS is DIS 260 and has warfare-model units.
        var map = UnitTypeMap.Load(aggMap);
        string note = File.ReadAllText(aggMap);
        int noteAt = note.IndexOf("HOSTILE NATION", StringComparison.Ordinal);
        string hostileNote = noteAt < 0 ? "" : note.Substring(noteAt, Math.Max(0, note.IndexOf('\n', noteAt) - noteAt));
        Check(ref failures, map.Nations.TryGetValue("RUS", out int rus) && rus == 260 && map.Nations["USA"] == 225
                            && map.Nations["BLR"] == 246 && map.CheckOpposingNationSupported("RUS") == null,
              "(s7) RL-20260927-02 in data/unit-type-map-52-aggregate.json: RUS is DIS 260 (not the entity map's 222) and has " +
              "usable hostile UNIT rows; USA 225, BLR 246");
        Check(ref failures, hostileNote.Contains("RL-20260927-02") && hostileNote.Contains("RUS")
                            && !hostileNote.Contains("not decided here") && !hostileNote.Contains("OWNER DECISION ("),
              "(s7) the map's hostile-nation note names the ruling (RL-20260927-02, RUS) - it no longer says the nation is " +
              "\"not decided here\"", hostileNote);

        // (s8) Y-2 - join through the MAK-ONE-2025 connection config; the C# FOM module list emptied.
        Check(ref failures, demo.ConfigFileIdentity && demo.Federation == "" && demo.FedFileName == "",
              "(s8) Y-2: the Demo overlay joins the config-file way (ConfigFileIdentity, no federation name, no FED file)");
        Check(ref failures, !shipped.ConfigFileIdentity && !compiled.ConfigFileIdentity && shipped.Federation == "CWIX-2024"
                            && shipped.FomModules.Count == 3,
              "(s8) WHAT THE BASE SHIPS, pinned as it is - it DIFFERS from Y-2: appsettings.json still names CWIX-2024 and three " +
              "FOM modules with ConfigFileIdentity false; only the runner (-VrfProfile 5.2) and the Demo overlay switch it",
              $"base federation '{shipped.Federation}', modules {shipped.FomModules.Count}");

        // (s9) C1/C2 (DESIGN_ORBAT_TO_VRF_2026-09-06) - compose per the vendor sample, ON by default.
        Check(ref failures, compiled.ComposeHierarchy && shipped.ComposeHierarchy && demo.ComposeHierarchy,
              "(s9) C1/C2: ComposeHierarchy is ON in VrfSettings.cs, appsettings.json and the Demo overlay");

        // (s10) C4 - AggregateFormation stays OFF (no create-time formation repair).
        Check(ref failures, compiled.AggregateFormation == "" && string.IsNullOrEmpty(shipped.AggregateFormation)
                            && string.IsNullOrEmpty(demo.AggregateFormation),
              "(s10) C4: AggregateFormation is empty (OFF) in VrfSettings.cs, appsettings.json and the Demo overlay");

        // (s11) DIRTY CONTROLS: every demo pin, re-read over the same two files with ITS key flipped in memory, must fail.
        var pins = new (string Key, string Flip, Func<VrfSettings, bool> Holds)[]
        {
            ("TypeMappingMode", "RealTemplates", v => v.TypeMappingMode == "FidelityTable"),
            ("CreationPolicy", "AtInit", v => v.MaterializeAtOrder),
            ("StallDetection", "false", v => v.StallDetection),
            ("StallClock", "wall", v => ResolvedWindow(v) == 360.0),
            ("ModelSet", "EntityLevel", v => v.ModelSet == "AggregateTacticalLevel"),
            ("TypeMapFile", "data/unit-type-map-52.json", v => v.TypeMapFile == AggregateTypeMap),
            ("Scenario", "", v => v.Scenario == DemoScenario),
            ("CompositionVariant", "authored", v => v.CompositionVariant == "catalogue"),
            ("TaskPredecessorTimeoutSeconds", "7200", v => v.TaskPredecessorTimeoutSeconds == 600),
            ("OpposingNation", "PRC", v => v.OpposingNation == "RUS"),
            ("ConfigFileIdentity", "false", v => v.ConfigFileIdentity),
            ("ComposeHierarchy", "false", v => v.ComposeHierarchy),
            ("AggregateFormation", "auto", v => string.IsNullOrEmpty(v.AggregateFormation)),
        };
        var holdsClean = pins.Where(p => p.Holds(demo)).Select(p => p.Key).ToList();
        var caught = new List<string>();
        foreach (var p in pins)
        {
            var flipped = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false)
                              .AddJsonFile(demoSettings, optional: false)
                              .AddInMemoryCollection(new Dictionary<string, string> { ["Vrf:" + p.Key] = p.Flip })
                              .Build().GetSection("Vrf").Get<VrfSettings>();
            if (flipped != null && !p.Holds(flipped)) caught.Add(p.Key);
        }
        Check(ref failures, holdsClean.Count == pins.Length && caught.Count == pins.Length,
              $"(s11) DIRTY CONTROLS: all {pins.Length} demo pins hold on the shipped files, and each FAILS once its own key is " +
              "flipped over them (TypeMappingMode, CreationPolicy, StallDetection, StallClock, ModelSet, TypeMapFile, Scenario, " +
              "CompositionVariant, TaskPredecessorTimeoutSeconds, OpposingNation, ConfigFileIdentity, ComposeHierarchy, " +
              "AggregateFormation) - the pins read the keys",
              $"clean {holdsClean.Count}, caught {string.Join(",", caught)}");

        // (s12) RL-20260928-03 ("AUTO it is") - a tasked container's route is driven by the vendor's planning tasks per
        // vertex, Auto by default; GroupOffRoad needs AllowLiteralMove; the road rule's proximity is 500 m. It ships in ALL
        // THREE places, so this one agrees with its ruling in the base as well as the overlay.
        Check(ref failures, compiled.AggregateMovePlanner == "Auto" && baseCfg["Vrf:AggregateMovePlanner"] == "Auto"
                            && shipped.AggregateMovePlanner == "Auto" && demo.AggregateMovePlanner == "Auto"
                            && !compiled.AllowLiteralMove && !shipped.AllowLiteralMove && !demo.AllowLiteralMove
                            && compiled.RoadProximityMeters == 500.0 && demo.RoadProximityMeters == 500.0
                            && !rsh.Contains("Vrf__AggregateMovePlanner", StringComparison.Ordinal),
              "(s12) RL-20260928-03: Vrf:AggregateMovePlanner=Auto in VrfSettings.cs, as an explicit key in appsettings.json and " +
              "through the Demo overlay; AllowLiteralMove false and RoadProximityMeters 500 in all three; RunScenario.sh exports " +
              "no other planner",
              $"compiled {compiled.AggregateMovePlanner}, base key '{baseCfg["Vrf:AggregateMovePlanner"]}', demo {demo.AggregateMovePlanner}");
        var literal = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false).AddJsonFile(demoSettings, optional: false)
                          .AddInMemoryCollection(new Dictionary<string, string> { ["Vrf:AggregateMovePlanner"] = "Literal" })
                          .Build().GetSection("Vrf").Get<VrfSettings>();
        Check(ref failures, literal != null && literal.AggregateMovePlanner != "Auto",
              "(s12) DIRTY CONTROL: Vrf:AggregateMovePlanner=Literal layered over the two files makes the pin FAIL - it reads the key");

        // (s13) M3b (2026-09-28, the G1-3 Result) - a PLANNED container's INTERMEDIATE vertex that "succeeds" without moving
        // the container is the EXECUTOR REFUSING (VertexChainPolicy.IsExecutorRefusal): the vertex FAILS, TASKABRT. It is
        // measured against Vrf:VertexArrivalRadiusMeters, which a 0 would switch off - so the bar is pinned where it ships.
        Check(ref failures, compiled.VertexArrivalRadiusMeters == 100.0 && baseCfg["Vrf:VertexArrivalRadiusMeters"] == "100"
                            && shipped.VertexArrivalRadiusMeters == 100.0 && demo.VertexArrivalRadiusMeters == 100.0
                            && !rsh.Contains("Vrf__VertexArrivalRadiusMeters", StringComparison.Ordinal)
                            && VertexChainPolicy.IsExecutorRefusal(1451, 0, demo.VertexArrivalRadiusMeters),
              "(s13) M3b: the EXECUTOR REFUSED bar ships ON - Vrf:VertexArrivalRadiusMeters 100 in VrfSettings.cs, as an explicit key " +
              "in appsettings.json and through the Demo overlay (RunScenario.sh exports none), so G1-3's vertex 1 (1451 m from it, " +
              "moved 0 m, L9979) FAILS as refused instead of advancing",
              $"compiled {compiled.VertexArrivalRadiusMeters}, base key '{baseCfg["Vrf:VertexArrivalRadiusMeters"]}', demo {demo.VertexArrivalRadiusMeters}");
        var barOff = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false).AddJsonFile(demoSettings, optional: false)
                         .AddInMemoryCollection(new Dictionary<string, string> { ["Vrf:VertexArrivalRadiusMeters"] = "0" })
                         .Build().GetSection("Vrf").Get<VrfSettings>();
        Check(ref failures, barOff != null && barOff.VertexArrivalRadiusMeters == 0.0
                            && !VertexChainPolicy.IsExecutorRefusal(1451, 0, barOff.VertexArrivalRadiusMeters),
              "(s13) DIRTY CONTROL: Vrf:VertexArrivalRadiusMeters=0 layered over the two files switches the refusal OFF - the pin " +
              "reads the key");

        // (s14) RL-20261005-02 - the demo is bounded by a terrain extent: OFF in the base (empty), the Iron Storm centre area
        // + 2 km in the Demo overlay - the very value --routeextent-selftest sec 11 judges cut A / FULL / the raw export with.
        Check(ref failures, compiled.DemoExtent == "" && shipped.DemoExtent == "" && baseCfg["Vrf:DemoExtent"] == ""
                            && RouteExtentPolicy.TryParseDemoExtent(shipped.DemoExtent, shipped.DemoExtentMarginKm, out var baseBound, out _)
                            && baseBound == null && !rsh.Contains("Vrf__DemoExtent", StringComparison.Ordinal),
              "(s14) RL-20261005-02: Vrf:DemoExtent is OFF (empty) in VrfSettings.cs and as an explicit key in appsettings.json, and " +
              "RunScenario.sh exports none - a runner opts in with --env Vrf__DemoExtent=...",
              $"compiled '{compiled.DemoExtent}', base '{shipped.DemoExtent}'");
        Check(ref failures, demo.DemoExtent == RouteExtentSelfTest.DemoOverlayExtent
                            && demo.DemoExtentMarginKm == RouteExtentSelfTest.DemoOverlayMarginKm
                            && RouteExtentPolicy.TryParseDemoExtent(demo.DemoExtent, demo.DemoExtentMarginKm, out var demoBound, out _)
                            && demoBound != null,
              "(s14) RL-20261005-02: the Demo overlay bounds the demo by the IRONSTORM-CENTRE area + 2 km " +
              $"({RouteExtentSelfTest.DemoOverlayExtent}), the value --routeextent-selftest sec 11 pins its refusal lists on",
              $"demo '{demo.DemoExtent}' + {demo.DemoExtentMarginKm} km");
        var extentOff = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false).AddJsonFile(demoSettings, optional: false)
                            .AddInMemoryCollection(new Dictionary<string, string> { ["Vrf:DemoExtent"] = "" })
                            .Build().GetSection("Vrf").Get<VrfSettings>();
        Check(ref failures, extentOff != null && extentOff.DemoExtent != RouteExtentSelfTest.DemoOverlayExtent,
              "(s14) DIRTY CONTROL: Vrf:DemoExtent='' layered over the two files makes the overlay pin FAIL - it reads the key");

        // (s15) THE DEMO OVERLAY IS THE AGGREGATE IRON STORM PROFILE, COMPLETE (2026-10-05). The model set is fixed per
        // scenario (UG52 13.7), so Vrf:ModelSet=AggregateTacticalLevel is only half a profile: the app does NOT derive the type
        // map (the runner does, Get-ModelSetTypeMapDefault) and reads the scenario for the composition guard. Each must agree,
        // in the overlay itself, with no hand-set env var - the 881d52e runbook's workaround this replaces.
        string mapPath = UnitTypeMap.ResolvePath(demo.TypeMapFile);
        string mapKey = "";
        if (mapPath != null)
        {
            try
            {
                using var mapDoc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(mapPath));
                if (mapDoc.RootElement.TryGetProperty("modelSetKey", out var k)) mapKey = k.GetString() ?? "";
            }
            catch (Exception ex) { mapKey = "unreadable: " + ex.Message; }
        }
        Check(ref failures, demo.TypeMappingMode == "FidelityTable" && demo.TypeMapFile == AggregateTypeMap && mapPath != null
                            && mapKey == "AggregateTacticalLevel" && shipped.TypeMapFile == "data/unit-type-map.json",
              "(s15) the Demo overlay's type map is the AGGREGATE fidelity table (data/unit-type-map-52-aggregate.json, " +
              "modelSetKey AggregateTacticalLevel - what the runner exports as Vrf__TypeMapFile for -ModelSet AggregateTacticalLevel); " +
              "appsettings.json keeps its own",
              $"demo '{demo.TypeMapFile}' -> {mapPath ?? "(not found)"}, modelSetKey '{mapKey}', base '{shipped.TypeMapFile}'");
        var fixture = CompositionVariants.ReadFixtureSms(
            Path.Combine(repo, "tools", "FixtureGen", "frame_variants", (demo.Scenario ?? "") + ".scnx"), "");
        Check(ref failures, demo.Scenario == DemoScenario && fixture.Known
                            && CompositionVariants.SmsModelSetName(fixture.Sms) == "AggregateTacticalLevel"
                            && compiled.Scenario == "" && shipped.Scenario == "",
              "(s15) the Demo overlay names the Iron Storm aggregate fixture (Vrf:Scenario=IronStorm_Centre_52_Aggregate), and the " +
              "repo copy of that fixture loads AggregateTacticalLevel.sms - the pairing the runner's Stage 0 checks; the code and " +
              "appsettings.json leave Scenario empty",
              $"demo '{demo.Scenario}', fixture {fixture.ScnxPath}: {fixture.Sms} ({fixture.Via})");
        Check(ref failures, demo.CompositionFile == shipped.CompositionFile && demo.CompositionFile == "data/unit-composition-52-aggregate.json"
                            && demo.CompositionVariant == CompositionVariants.Catalogue && demo.CatalogueSms == "",
              "(s15) the composition the aggregate runs used: data/unit-composition-52-aggregate.json, variant catalogue (the " +
              "runner's export), no CatalogueSms (the fixture loads the installed SMS, so the runner exported none)",
              $"file '{demo.CompositionFile}', variant '{demo.CompositionVariant}', sms '{demo.CatalogueSms}'");
        Check(ref failures, demo.TaskPredecessorTimeoutSeconds == 600 && shipped.TaskPredecessorTimeoutSeconds == 600
                            && Exported("Vrf__TaskPredecessorTimeoutSeconds=7200"),
              "(s15) the successor-gate floor is 600 s in the Demo overlay - the rehearsed runs' value (--env " +
              "Vrf__TaskPredecessorTimeoutSeconds=600 over RunScenario.sh's export of 7200, which this pins as it is)",
              $"demo {demo.TaskPredecessorTimeoutSeconds}, base {shipped.TaskPredecessorTimeoutSeconds}");
        string demoText = File.ReadAllText(demoSettings);
        Check(ref failures, demo.DurationScale == 1.0 && demoText.Contains("THE DEMO PACE IS THE OWNER'S OPEN DECISION", StringComparison.Ordinal),
              "(s15) DurationScale PINNED AS IT IS (1.0) - the demo pace is the owner's open decision and the overlay says so; " +
              "this test does not choose (the rehearsals ran 0.25 by the runner's --duration-scale)",
              $"demo {demo.DurationScale}");
        return failures;
    }

    /// <summary>The aggregate fidelity table the Demo overlay names (and the runner derives for AggregateTacticalLevel).</summary>
    private const string AggregateTypeMap = "data/unit-type-map-52-aggregate.json";

    /// <summary>The fixture the demo's VR-Forces loads (DEMO_RUNBOOK sec 2.1) and the overlay's Vrf:Scenario.</summary>
    private const string DemoScenario = "IronStorm_Centre_52_Aggregate";

    /// <summary>The stall window the watchdog would use under these settings (StallPolicy's own resolution).</summary>
    private static double ResolvedWindow(VrfSettings v)
        => StallPolicy.ResolveWindowSeconds(v.StallWindowSeconds, StallPolicy.ParseClockPreference(v.StallClock, out _));

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

    private static void Check(ref int failures, bool ok, string label, string detail = "")
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}" + (ok || string.IsNullOrEmpty(detail) ? "" : " -- " + detail));
        if (!ok) failures++;
    }
}
