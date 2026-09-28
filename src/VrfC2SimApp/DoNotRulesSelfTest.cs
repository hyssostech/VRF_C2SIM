using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace VrfC2SimApp;

/// <summary>
/// THE "DO NOT" RULES, PINNED BY SOURCE GUARDS (AUDIT_RULINGS_IN_CODE_2026-09-28 sec 4 fix 2; sec 3 item 2 - rules
/// "kept by absence": nothing failed if a lane put the call back). A section of `--rulings-selftest`. It reads the
/// CHECKOUT's sources (like --populate-selftest's source checks), with comments and string/here-doc contents blanked by
/// <see cref="CodeText"/>, so a rule's own explanation in a comment or a log message never trips it. One check per rule:
///
///   (d1) no 10000 m birth - CITED, not duplicated: PlacementSelfTest (--placement-selftest) pins it; checked present.
///   (d2) C8 (DESIGN_ORBAT_TO_VRF, 2026-09-02): a VR-Forces object is addressed by its real uuid, never a
///        name-as-DtUUID - every DtUUID the facade builds, and every id argument the app hands the bridge.
///   (d3) every CreateEntity / CreateAggregate passes a uuid (C1d, RL-20260928-02) - CITED: --populate-selftest p15 (e).
///   (d4) RTI-UNTOUCHED (RUNBOOK sec 0): no Stop-Process / taskkill SWEEP in scripts/ or tools/ that could reach
///        rtiexec, rtiForwarder, rtiAssistant or an RtiProbe holder - the kill statements are exactly the three known,
///        by pid, gated to the VR-Forces back end; none by name anywhere.
///   (d5) RL-20260914-04 as it stands: Vrf:SubordinateFanOut is OFF and nothing turns it on.
///   (d6)-(d9) the audit's own fix-2 list: Y-12 no Autonomous Actions send, Y-13 no road / navigation preference send,
///        C10 no sendVrfObjectCreateMsg + initialFormation, C6 + G3/G4 ReorganizeAggregate only in the opt-in
///        Vrf:AggregateFormation=auto handler.
///   (d10) M3, RL-20260928-03: NO CONTAINER MOVE ISSUES PA_Move_Along_Route unless Vrf:AggregateMovePlanner=Literal - the
///        planned arm (every other planner) returns before the container route arm's ContainerScripts.ForForm, and the
///        script id lives nowhere else in the app. (Y-13 is untouched: a container's per-leg road use is the planner's
///        pathQuery / useRoads task variable under RL-20260928-03, not an entity road or navigation preference.)
///   Each scanner is run on a DIRTY control first (a synthetic violation it must flag, and a comment/string mention it
///   must not).
/// </summary>
public static class DoNotRulesSelfTest
{
    public static int Run()
    {
        int failures = 0;
        string repo = FindRepoRoot();
        Check(ref failures, repo != null, "(d0) the repository is found above the executable (data/COA-STP1_Order.xml)");
        if (repo == null) return failures;
        Cited(ref failures, repo);
        NameAsUuid(ref failures, repo);
        RtiUntouched(ref failures, repo);
        FanOut(ref failures, repo);
        AuditFix2(ref failures, repo);
        PlannedContainerMove(ref failures, repo);
        return failures;
    }

    // ------------------------------------------------------------------------ (d1) + (d3) ----
    private static void Cited(ref int failures, string repo)
    {
        string placement = Read(repo, "src", "VrfC2SimApp", "PlacementSelfTest.cs");
        Check(ref failures, placement.Contains("Check(\"no 10000 m birth anywhere\",")
                            && placement.Contains("\"no 10000 in ANY domain/altitude/terrain combination (5x4x3x3 = 180 rows)\""),
              "(d1) no 10000 m birth (Y-17; CLAUDE.md sec 3) - CITED, not duplicated: --placement-selftest still carries the " +
              "negative control and its 180-row sweep (PlacementSelfTest.cs)");
        string container = Read(repo, "src", "VrfC2SimApp", "ContainerSelfTest.cs");
        Check(ref failures, container.Contains("\"EVERY _bridge.CreateEntity / CreateAggregate call site in the app passes a uuid - the plan's StartingUuid, as the \""),
              "(d3) every CreateEntity / CreateAggregate passes a uuid (C1d, RL-20260928-02) - CITED, not duplicated: " +
              "--populate-selftest p15 (e) still carries the call-site guard (ContainerSelfTest.cs)");
    }

    // ------------------------------------------------------------------------------ (d2) ----
    // The bridge methods whose arguments ADDRESS an object, and which argument positions do (0-based).
    private static readonly (string Method, int[] Ids)[] IdCalls =
    {
        ("DeleteObject", new[] { 0 }), ("SetAltitude", new[] { 0 }), ("SetLocation", new[] { 0 }),
        ("SetTarget", new[] { 0, 1 }), ("AddToOrganization", new[] { 0, 1 }), ("SetObjectNotifyLevel", new[] { 0 }),
        ("SetRulesOfEngagement", new[] { 0 }), ("MoveToLocation", new[] { 0 }), ("MoveAlongRoute", new[] { 0, 1 }),
        ("PlanAndMoveTo", new[] { 0, 1 }), ("GetAggregateMembers", new[] { 0 }), ("PublishedSubordinateCount", new[] { 0 }),
        ("SetAggregateFormation", new[] { 0 }), ("ReorganizeAggregate", new[] { 0 }), ("RequestAvailableFormations", new[] { 0 }),
        ("MoveIntoFormation", new[] { 0 }), ("Breach", new[] { 0, 1 }), ("PatrolRoute", new[] { 0, 1 }),
        ("FollowEntity", new[] { 0, 1 }), ("FireAtTarget", new[] { 0, 1 }), ("RunScriptedTask", new[] { 0 }),
        ("SendScriptedSet", new[] { 0 }), ("TryGetEntityGeodetic", new[] { 0 }), ("TryGetEntityKinematics", new[] { 0 }),
    };
    private static readonly Regex NameLike = new(@"name|marking", RegexOptions.IgnoreCase);

    /// <summary>Every id argument of a bridge call in C# code (comments and strings already blanked) that reads like
    /// a NAME, as "method: argument".</summary>
    private static List<string> NameIdArguments(string code)
    {
        var bad = new List<string>();
        foreach (var (method, ids) in IdCalls)
            foreach (Match m in Regex.Matches(code, @"\b_?bridge\." + method + @"\("))
            {
                var args = TopLevelArgs(code, m.Index + m.Length);
                foreach (int i in ids)
                    if (i < args.Count && NameLike.IsMatch(args[i])) bad.Add($"{method}: {args[i].Trim()}");
            }
        return bad;
    }

    /// <summary>Every DtUUID(...) construction in C++ code (comments and strings blanked) whose argument is not a
    /// uuid-named identifier, or reads like a name.</summary>
    private static List<string> NonUuidDtUuids(string code, out int total)
    {
        var bad = new List<string>();
        total = 0;
        foreach (Match m in Regex.Matches(code, @"\bDtUUID\("))
        {
            var args = TopLevelArgs(code, m.Index + m.Length);
            if (args.Count == 0) continue;
            total++;
            string a = args[0].Trim();
            if (!Regex.IsMatch(a, "uuid", RegexOptions.IgnoreCase) || NameLike.IsMatch(a) || a.Contains("DtString")) bad.Add("DtUUID(" + a + ")");
        }
        return bad;
    }

    private static void NameAsUuid(ref int failures, string repo)
    {
        // DIRTY CONTROLS FIRST: the 2026-09-02 shape (a route NAME handed over as the route's id), in both layers.
        string dirtyCs = CodeText.Blank("_bridge.MoveAlongRoute(vrfUuid, routeName);\n" +
                                        "// _bridge.MoveAlongRoute(vrfUuid, routeName) in a comment\n" +
                                        "_log.LogInformation(\"_bridge.MoveAlongRoute(vrfUuid, routeName)\");\n", CodeText.Lang.CSharp);
        string dirtyCpp = CodeText.Blank("p_->controller->moveAlongRoute(DtUUID(uuid), DtUUID(routeName.c_str()), DtSimSendToAll);\n" +
                                         "// DtUUID(markingText) in a comment\n", CodeText.Lang.CSharp);
        var dirtyBad = NameIdArguments(dirtyCs);
        var dirtyDt = NonUuidDtUuids(dirtyCpp, out _);
        Check(ref failures, dirtyBad.Count == 1 && dirtyBad[0] == "MoveAlongRoute: routeName" && dirtyDt.Count == 1
                            && dirtyDt[0] == "DtUUID(routeName.c_str())",
              "(d2) DIRTY CONTROL: a route NAME passed as an id is flagged in the app (_bridge.MoveAlongRoute(vrfUuid, " +
              "routeName)) and in the facade (DtUUID(routeName.c_str())); the same text in a comment or a string is not",
              string.Join(" | ", dirtyBad.Concat(dirtyDt)));

        int dtTotal = 0;
        var facadeBad = new List<string>();
        foreach (string f in Directory.GetFiles(Path.Combine(repo, "src", "VrfFacade")).Where(p => p.EndsWith(".cpp") || p.EndsWith(".cxx") || p.EndsWith(".h"))
                                      .Concat(Directory.GetFiles(Path.Combine(repo, "src", "VrfBridge"), "*.cpp")))
        {
            var bad = NonUuidDtUuids(CodeText.Blank(File.ReadAllText(f), CodeText.Lang.CSharp), out int n);
            dtTotal += n;
            facadeBad.AddRange(bad.Select(b => Path.GetFileName(f) + ": " + b));
        }
        Check(ref failures, dtTotal >= 30 && facadeBad.Count == 0,
              $"(d2) C8 - NO NAME AS A DtUUID: all {dtTotal} DtUUID(...) constructions in src/VrfFacade and src/VrfBridge take a " +
              "uuid-named argument (uuid, routeUuid, targetUuid, ...), none a name or a marking",
              string.Join(" | ", facadeBad));

        var appBad = new List<string>();
        int files = 0;
        foreach (string f in Directory.GetFiles(Path.Combine(repo, "src", "VrfC2SimApp"), "*.cs", SearchOption.AllDirectories))
        {
            if (f.EndsWith("SelfTest.cs", StringComparison.Ordinal) || f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
            files++;
            appBad.AddRange(NameIdArguments(CodeText.Blank(File.ReadAllText(f), CodeText.Lang.CSharp)).Select(b => Path.GetFileName(f) + ": " + b));
        }
        Check(ref failures, files > 20 && appBad.Count == 0,
              $"(d2) C8 - NO NAME AS AN ID in the app: no id argument of a bridge call (24 addressing methods, {files} source " +
              "files, the self-tests excluded) reads like a name or a marking", string.Join(" | ", appBad));
        string svc = Read(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");
        Check(ref failures, svc.Contains("_bridge.MoveAlongRoute(pending.TaskeeVrfUuid, e.Uuid);")
                            && svc.Contains("_bridge.PatrolRoute(pending.TaskeeVrfUuid, e.Uuid);")
                            && svc.Contains("_bridge.PlanAndMoveTo(pending.TaskeeVrfUuid, e.Uuid);"),
              "(d2) C8 - the route / waypoint task is issued with the CREATED object's own uuid (e.Uuid from ObjectCreated), " +
              "the 2026-09-02 fix (PREREG_ROUTE_UUID_FIX_2026-09-02)");
    }

    // ------------------------------------------------------------------------------ (d4) ----
    // The PowerShell aliases (kill, spps) and bash kill count - but not "kill -0", which sends no signal (bash's own
    // liveness probe, RunScenario.sh's sampler wait).
    private static readonly Regex KillToken = new(
        @"\bStop-Process\b|\btaskkill\b|(?<![\w$.-])(?:kill|spps)(?=\s+(?:-(?!0\b)|\$|\d))|\.Kill\s*\(|\bTerminateProcess\s*\(|" +
        @"\bos\.kill\s*\(|\.terminate\s*\(\s*\)|\bpkill\b|\bkillall\b", RegexOptions.IgnoreCase);
    // Kill BY NAME, anywhere - comments and strings INCLUDED (the strictest reading; nothing in the tree says it today).
    private static readonly Regex ByName = new(
        @"(?:Stop-Process|(?<![\w$.-])kill|(?<![\w$.-])spps)\s+(?:[^\r\n|;]*\s)?-(?:Name|ProcessName)\b|\|\s*Stop-Process\b|" +
        @"\btaskkill\b[^\r\n]*/IM\b|\bpkill\b|\bkillall\b", RegexOptions.IgnoreCase);
    private static readonly string[] Protected = { "rtiexec", "rtiForwarder", "rtiAssistant", "RtiProbe" };

    // THE INVENTORY - every executable kill statement in scripts/ and tools/, reviewed. A new one fails (d4a) until it
    // is reviewed and added here (and to RUNBOOK sec 11m).
    private static readonly (string File, string Statement)[] KnownKills =
    {
        ("scripts/LaunchVrf52.ps1", "try { Stop-Process -Id $ProcessId -Force -ErrorAction Stop }"),
        ("scripts/StopVrf52.ps1", "$out = & taskkill /PID $p.Id 2>&1"),
        ("scripts/StopVrf52.ps1", "Stop-Process -Id $p.Id -Force -ErrorAction Continue"),
    };

    /// <summary>Every executable kill statement in one file's CODE (comments, strings, here-strings blanked).</summary>
    private static List<(int Line, string Statement)> KillStatements(string text, CodeText.Lang lang)
    {
        var outp = new List<(int, string)>();
        string code = CodeText.Blank(text, lang);
        string[] codeLines = code.Split('\n');
        string[] rawLines = text.Split('\n');
        for (int i = 0; i < codeLines.Length; i++)
            if (KillToken.IsMatch(codeLines[i]))
                outp.Add((i + 1, Regex.Replace(rawLines[i].Trim(), @"\s+", " ")));
        return outp;
    }

    private static void RtiUntouched(ref int failures, string repo)
    {
        // DIRTY CONTROLS FIRST: three sweeps that WOULD reach the RTI, and three mentions that are not code.
        const string dirty = "Get-Process rtiexec | Stop-Process -Force\n" +
                             "& taskkill /F /IM rtiForwarder.exe\n" +
                             "kill -Name RtiProbe\n" +
                             "# Stop-Process -Name rtiexec  (a comment)\n" +
                             "Say-Warn 'never run taskkill /IM rtiexec.exe'\n" +
                             "$msg = @'\nStop-Process -Id 1\n'@\n";
        var dirtyKills = KillStatements(dirty, CodeText.Lang.PowerShell);
        var dirtyByName = dirty.Split('\n').Where(l => ByName.IsMatch(l)).ToList();
        var bashKills = KillStatements("kill -0 \"$SAMPLER_BG_PID\" 2>/dev/null || break\nkill -9 $pid\n" +
                                       "cat <<'USAGE'\n  Stop-Process on the pid\nUSAGE\necho \"do NOT run taskkill sweeps\"\n",
                                       CodeText.Lang.Bash);
        Check(ref failures, dirtyKills.Count == 3 && dirtyKills.Select(k => k.Line).SequenceEqual(new[] { 1, 2, 3 })
                            && dirtyByName.Count == 5 && bashKills.Count == 1 && bashKills[0].Line == 2,
              "(d4) DIRTY CONTROL: a pipeline Stop-Process, a taskkill /IM and a 'kill -Name' are found as kill statements (a " +
              "comment, a message and a here-string are not); the by-name rule sees all five by-name forms, comment and " +
              "message included; in bash 'kill -9' is found and 'kill -0' (no signal), a here-doc and an echo are not",
              $"statements at {string.Join(",", dirtyKills.Select(k => k.Line))}; by-name {dirtyByName.Count}; bash at " +
              string.Join(",", bashKills.Select(k => k.Line)));

        var found = new List<(string File, string Statement)>();
        var byName = new List<string>();
        int scanned = 0;
        foreach (string root in new[] { "scripts", "tools" })
            foreach (string f in Directory.GetFiles(Path.Combine(repo, root), "*.*", SearchOption.AllDirectories))
            {
                var lang = CodeText.LangOf(f);
                if (lang == null) continue;
                string rel = Path.GetRelativePath(repo, f).Replace('\\', '/');
                if (Regex.IsMatch(rel, @"/(bin|obj|out|preflight_cache|__pycache__|node_modules)/")) continue;
                scanned++;
                string text = File.ReadAllText(f);
                found.AddRange(KillStatements(text, lang.Value).Select(k => (rel, k.Statement)));
                byName.AddRange(text.Split('\n').Select((l, i) => (l, i)).Where(x => ByName.IsMatch(x.l))
                                    .Select(x => $"{rel}:{x.i + 1}: {x.l.Trim()}"));
            }
        string Show(IEnumerable<(string File, string Statement)> xs) => string.Join(" | ", xs.Select(x => x.File + ": " + x.Statement));
        var unknown = found.Where(k => !KnownKills.Contains(k)).ToList();
        var missing = KnownKills.Where(k => !found.Contains(k)).ToList();
        Check(ref failures, scanned > 50 && unknown.Count == 0 && missing.Count == 0 && found.Count == KnownKills.Length,
              $"(d4) RTI-UNTOUCHED: the executable kill statements in scripts/ and tools/ ({scanned} files) are EXACTLY the " +
              "three reviewed ones - LaunchVrf52's own crashed back end, StopVrf52's taskkill without /F and its identity-gated " +
              "force; a new one fails here until it is reviewed",
              $"unknown [{Show(unknown)}] missing [{Show(missing)}]");
        Check(ref failures, found.All(k => Regex.IsMatch(k.Statement, @"Stop-Process -Id |taskkill /PID "))
                            && found.All(k => !Protected.Any(p => k.Statement.Contains(p, StringComparison.OrdinalIgnoreCase)))
                            && byName.Count == 0,
              "(d4) ... every one is BY PID, none names rtiexec / rtiForwarder / rtiAssistant / RtiProbe, and NO text in " +
              "scripts/ or tools/ - comments included - carries a kill-by-name form (-Name, a piped Stop-Process, /IM, pkill)",
              string.Join(" | ", byName));

        string launch = Read(repo, "scripts", "LaunchVrf52.ps1");
        string stop = Read(repo, "scripts", "StopVrf52.ps1");
        string runner = Read(repo, "scripts", "RunC2SimScenario.ps1");
        string lib = Read(repo, "scripts", "RunnerLib.ps1");
        int close = launch.IndexOf("function Close-CrashedBackend {", StringComparison.Ordinal);
        int nameCheck = launch.IndexOf("if ($p.Name -ne $ExpectedName) {", close < 0 ? 0 : close, StringComparison.Ordinal);
        int launchKill = launch.IndexOf("try { Stop-Process -Id $ProcessId -Force -ErrorAction Stop }", StringComparison.Ordinal);
        Check(ref failures, close > 0 && launch.Contains("param([int]$ProcessId, [string]$ExpectedName = 'vrfSimHLA1516e',")
                            && nameCheck > close && launchKill > nameCheck,
              "(d4) LaunchVrf52's one Stop-Process sits in Close-CrashedBackend, takes a PID and re-checks the image is " +
              "vrfSimHLA1516e first (the pid-recycling guard)");
        int beLoop = stop.IndexOf("foreach ($p in @(Get-Procs $procBackend)) {\r\n    try {\r\n        $out = & taskkill /PID $p.Id 2>&1",
                                  StringComparison.Ordinal);
        int beLoopLf = stop.IndexOf("foreach ($p in @(Get-Procs $procBackend)) {\n    try {\n        $out = & taskkill /PID $p.Id 2>&1",
                                    StringComparison.Ordinal);
        int identity = stop.IndexOf("Test-OwnBackendIdentity -ExpectedPid $ForceOwnBackendPid", StringComparison.Ordinal);
        int force = stop.IndexOf("Stop-Process -Id $p.Id -Force -ErrorAction Continue", StringComparison.Ordinal);
        Check(ref failures, stop.Contains("$procBackend  = 'vrfSimHLA1516e'") && (beLoop > 0 || beLoopLf > 0)
                            && identity > 0 && force > identity
                            && lib.Contains("$protected = @('rtiexec', 'rtiForwarder', 'rtiAssistant', 'RtiProbe', 'vrfGui', 'vrfLauncher')"),
              "(d4) StopVrf52's taskkill loops over the back end ($procBackend = vrfSimHLA1516e) and its force is behind " +
              "Test-OwnBackendIdentity, whose protected list names rtiexec, rtiForwarder, rtiAssistant, RtiProbe, vrfGui and " +
              "vrfLauncher (RTT 10f exercises it)");
        Check(ref failures, stop.Contains("$rtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')")
                            && runner.Contains("$RtiNames     = @('rtiAssistant','rtiexec','rtiForwarder')"),
              "(d4) the RTI trio is named where it is INVENTORIED, never stopped: StopVrf52 $rtiNames and the runner's $RtiNames");
    }

    // ------------------------------------------------------------------------------ (d5) ----
    private static void FanOut(ref int failures, string repo)
    {
        var compiled = new VrfSettings();
        string app = Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
        string demo = Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");
        var shipped = new ConfigurationBuilder().AddJsonFile(app, optional: false)
                          .AddJsonFile(demo, optional: false).Build().GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        bool exported = new[] { "RunScenario.sh", "RunC2SimScenario.ps1", "StartInterface52.ps1", "RunnerLib.ps1" }
            .Any(s => Read(repo, "scripts", s).Contains("Vrf__SubordinateFanOut", StringComparison.OrdinalIgnoreCase));
        Check(ref failures, !compiled.SubordinateFanOut && !shipped.SubordinateFanOut && !exported,
              "(d5) RL-20260914-04 AS IT STANDS (R6: test the vendor filter, then fan battalions out to companies - not built): " +
              "Vrf:SubordinateFanOut is OFF in VrfSettings.cs and both shipped json files, and no script exports it. The audit " +
              "(2026-09-28 sec 5) reads it as probably overtaken by RL-20260926-01 and RL-20260927-06 - retiring it is the owner's " +
              "call; this pins the current value only");
    }

    // ------------------------------------------------------------------------- (d6)-(d9) ----
    private static readonly Regex Autonomy = new(@"AutonomousAction|AIEnabled|setAutonomous|autonomous", RegexOptions.IgnoreCase);
    private static readonly Regex RoadPref = new(@"RoadPreference|road-preference|Prefer Roads|NavigationPreference|setNavigationPreference|preferRoads",
                                                 RegexOptions.IgnoreCase);
    private static readonly Regex ObjectCreateMsg = new(@"sendVrfObjectCreateMsg|VrfObjectCreateMsg|initialFormation", RegexOptions.IgnoreCase);

    private static void AuditFix2(ref int failures, string repo)
    {
        // DIRTY CONTROL FIRST: each pattern flags a code line and ignores the same words in a comment and a string.
        string dirty = CodeText.Blank("_bridge.SetAutonomousActionsEnabled(uuid, false);\n" +
                                      "_bridge.SetNavigationPreference(uuid, \"Prefer Roads\");\n" +
                                      "c->sendVrfObjectCreateMsg(msg, initialFormation);\n" +
                                      "// AutonomousActions, RoadPreference, sendVrfObjectCreateMsg in a comment\n" +
                                      "_log.LogInformation(\"AutonomousActions RoadPreference sendVrfObjectCreateMsg\");\n", CodeText.Lang.CSharp);
        var dl = dirty.Split('\n');
        Check(ref failures, dl.Count(l => Autonomy.IsMatch(l)) == 1 && dl.Count(l => RoadPref.IsMatch(l)) == 1
                            && dl.Count(l => ObjectCreateMsg.IsMatch(l)) == 1,
              "(d6-d8) DIRTY CONTROL: an autonomy send, a navigation-preference send and a sendVrfObjectCreateMsg are each " +
              "flagged in code, and not in a comment or a string");

        var hits = new List<(string File, string Rule, int Line)>();
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(Path.Combine(repo, "src", "VrfC2SimApp"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("SelfTest.cs", StringComparison.Ordinal)
                        && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)));
        files.AddRange(Directory.GetFiles(Path.Combine(repo, "src", "VrfFacade")).Where(p => p.EndsWith(".cpp") || p.EndsWith(".cxx") || p.EndsWith(".h")));
        files.AddRange(Directory.GetFiles(Path.Combine(repo, "src", "VrfBridge"), "*.cpp"));
        foreach (string f in files)
        {
            string[] lines = CodeText.Blank(File.ReadAllText(f), CodeText.Lang.CSharp).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (Autonomy.IsMatch(lines[i])) hits.Add((Path.GetFileName(f), "Y-12", i + 1));
                if (RoadPref.IsMatch(lines[i])) hits.Add((Path.GetFileName(f), "Y-13", i + 1));
                if (ObjectCreateMsg.IsMatch(lines[i])) hits.Add((Path.GetFileName(f), "C10", i + 1));
            }
        }
        string H(string rule) => string.Join(", ", hits.Where(h => h.Rule == rule).Select(h => $"{h.File}:{h.Line}"));
        Check(ref failures, files.Count > 20 && hits.All(h => h.Rule != "Y-12"),
              $"(d6) Y-12 - AUTONOMOUS ACTIONS LEFT ON: no code in the app, the facade or the bridge ({files.Count} files) sends " +
              "an Autonomous Actions / AI-enabled setting (UG52 23.6: disabling it also disables path planning)", H("Y-12"));
        Check(ref failures, hits.All(h => h.Rule != "Y-13"),
              "(d7) Y-13 - THE SMS ROAD DEFAULTS: no code sends a road or navigation preference (none unless a C2SIM order " +
              "carries road-use intent, which no verb maps yet)", H("Y-13"));
        Check(ref failures, hits.All(h => h.Rule != "C10"),
              "(d8) C10 - NO DIRECT CREATE: no code builds sendVrfObjectCreateMsg + initialFormation (DESIGN_ORBAT C10); every " +
              "create goes through createEntity / createAggregate", H("C10"));

        string svc = Read(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");
        string svcCode = CodeText.Blank(svc, CodeText.Lang.CSharp);
        int calls = Regex.Matches(svcCode, @"\b_bridge\.ReorganizeAggregate\(").Count;
        int others = files.Where(f => !f.EndsWith("VrfC2SimService.cs", StringComparison.Ordinal) && f.EndsWith(".cs", StringComparison.Ordinal))
                          .Sum(f => Regex.Matches(CodeText.Blank(File.ReadAllText(f), CodeText.Lang.CSharp), @"\.ReorganizeAggregate\(").Count);
        int handler = svc.IndexOf("private void OnVrfAvailableFormations(", StringComparison.Ordinal);
        int guard = svc.IndexOf("if (!_vrf.AggregateFormation.Equals(\"auto\", StringComparison.OrdinalIgnoreCase)) return;", handler < 0 ? 0 : handler,
                                StringComparison.Ordinal);
        int call = svc.IndexOf("_bridge.ReorganizeAggregate(e.Uuid);", StringComparison.Ordinal);
        int next = svc.IndexOf("private void OnVrfTerrainProfile(", StringComparison.Ordinal);
        Check(ref failures, calls == 1 && others == 0 && handler > 0 && guard > handler && call > guard && next > call,
              "(d9) C6 + G3/G4 - NO POST-ATTACH REORGANIZE: ReorganizeAggregate is called ONCE in the app, inside " +
              "OnVrfAvailableFormations and after its 'Vrf:AggregateFormation != auto -> return' guard (the opt-in repair; C4 " +
              "keeps it OFF)", $"calls {calls}, elsewhere {others}, handler {handler}, guard {guard}, call {call}");
    }

    // ----------------------------------------------------------------------------- (d10) ----
    private static readonly Regex PlannedGate = new(
        @"if\s*\(\s*moveForm\s*==\s*GroundMoveForm\.RouteTask\s*&&\s*unit\.IsContainer\s*&&\s*!patrol\s*&&\s*AggregateMovePolicy\.IsPlanned\(\s*_movePlanner\s*\)\s*\)");
    private const string RouteArmForForm = "ContainerScripts.ForForm(moveForm, patrol)";

    /// <summary>Does the planned-move gate come before the container route arm's ForForm, with a return in between?</summary>
    private static bool PlannedGateGuardsRouteArm(string code)
    {
        var gate = PlannedGate.Match(code);
        int arm = code.IndexOf(RouteArmForForm, StringComparison.Ordinal);
        return gate.Success && arm > gate.Index
               && code.Substring(gate.Index, arm - gate.Index).Contains("StartContainerPlannedMove(", StringComparison.Ordinal)
               && code.Substring(gate.Index, arm - gate.Index).Contains("return;", StringComparison.Ordinal);
    }

    private static void PlannedContainerMove(ref int failures, string repo)
    {
        // DIRTY CONTROLS FIRST: C1's shape before M3 (the route arm reached with no planner gate), a gate that does not
        // return, and the clean shape.
        string dirtyNoGate = CodeText.Blank(
            "if (moveForm == GroundMoveForm.MoveToPerVertex) { StartVertexChain(task, unit, vrfUuid, routeGeo, attackTargetVrf); return; }\n" +
            "string containerScript = unit.IsContainer ? ContainerScripts.ForForm(moveForm, patrol) : null;\n", CodeText.Lang.CSharp);
        string dirtyNoReturn = CodeText.Blank(
            "if (moveForm == GroundMoveForm.RouteTask && unit.IsContainer && !patrol && AggregateMovePolicy.IsPlanned(_movePlanner))\n" +
            "    StartContainerPlannedMove(task, unit, vrfUuid, routeGeo, containerMembers);\n" +
            "string containerScript = unit.IsContainer ? ContainerScripts.ForForm(moveForm, patrol) : null;\n", CodeText.Lang.CSharp);
        string clean = CodeText.Blank(
            "if (moveForm == GroundMoveForm.RouteTask && unit.IsContainer && !patrol && AggregateMovePolicy.IsPlanned(_movePlanner))\n" +
            "{\n    StartContainerPlannedMove(task, unit, vrfUuid, routeGeo, containerMembers);\n    return;\n}\n" +
            "// ContainerScripts.ForForm(moveForm, patrol) in a comment\n" +
            "string containerScript = unit.IsContainer ? ContainerScripts.ForForm(moveForm, patrol) : null;\n", CodeText.Lang.CSharp);
        Check(ref failures, !PlannedGateGuardsRouteArm(dirtyNoGate) && !PlannedGateGuardsRouteArm(dirtyNoReturn)
                            && PlannedGateGuardsRouteArm(clean),
              "(d10) DIRTY CONTROLS: a container route arm reached with no planner gate, and a gate that does not return, are " +
              "flagged; the gated shape (StartContainerPlannedMove; return; before ForForm) is not");

        string svcCode = CodeText.Blank(Read(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs"), CodeText.Lang.CSharp);
        int gates = PlannedGate.Matches(svcCode).Count;
        int forForms = Regex.Matches(svcCode, @"\bContainerScripts\.ForForm\(").Count;
        Check(ref failures, gates == 1 && PlannedGateGuardsRouteArm(svcCode) && forForms == 2
                            && svcCode.Contains("ContainerScripts.ForForm(GroundMoveForm.SinglePointMoveTo, patrol: false)", StringComparison.Ordinal),
              "(d10) M3, RL-20260928-03 - NO CONTAINER MOVE ISSUES PA_Move_Along_Route UNLESS Vrf:AggregateMovePlanner=Literal: " +
              "ExecuteTaskOnTick's planned arm (moveForm RouteTask, a container, not a patrol, IsPlanned) calls " +
              "StartContainerPlannedMove and RETURNS before the route arm's ContainerScripts.ForForm(moveForm, patrol) - the one " +
              "place PA_Move_Along_Route is chosen (ForForm's other caller is the single-point PA_Move_To_Location_Direct)",
              $"gates {gates}, ForForm calls {forForms}");

        // No scripted-task issue in the app takes the literal script id directly - it arrives only through ForForm.
        var direct = new List<string>();
        foreach (string f in Directory.GetFiles(Path.Combine(repo, "src", "VrfC2SimApp"), "*.cs", SearchOption.AllDirectories))
        {
            if (f.EndsWith("SelfTest.cs", StringComparison.Ordinal) || f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
            string code = CodeText.Blank(File.ReadAllText(f), CodeText.Lang.CSharp);
            foreach (Match m in Regex.Matches(code, @"\b(RunScriptedTask|TryIssueScriptedMove|TryIssueMemberMoves)\("))
                foreach (var a in TopLevelArgs(code, m.Index + m.Length))
                    if (a.Contains("MoveAlongRoute", StringComparison.Ordinal)) direct.Add(Path.GetFileName(f) + ": " + m.Value + a.Trim());
        }
        Check(ref failures, direct.Count == 0,
              "(d10) no RunScriptedTask / TryIssueScriptedMove / TryIssueMemberMoves call in the app names the move-along script " +
              "directly - a container's route script is whatever ForForm answered behind the planner gate", string.Join(" | ", direct));

        var planned = Enum.GetValues<AggregateMovePlanner>().Where(AggregateMovePolicy.IsPlanned).ToList();
        Check(ref failures, planned.Count == Enum.GetValues<AggregateMovePlanner>().Length - 1
                            && !AggregateMovePolicy.IsPlanned(AggregateMovePlanner.Literal),
              "(d10) IsPlanned is true for EVERY Vrf:AggregateMovePlanner value but Literal (exhaustive), so only Literal reaches " +
              "the route arm", string.Join(",", planned));
    }

    // --------------------------------------------------------------------------- helpers ----
    /// <summary>The top-level comma-separated arguments of a call whose "(" ends just before <paramref name="start"/>.</summary>
    private static List<string> TopLevelArgs(string s, int start)
    {
        var args = new List<string>();
        int depth = 0;
        var cur = new StringBuilder();
        for (int j = start; j < s.Length; j++)
        {
            char ch = s[j];
            if (ch == '(' || ch == '[' || ch == '{') { depth++; cur.Append(ch); }
            else if (ch == ')' || ch == ']' || ch == '}')
            {
                if (depth == 0) { if (cur.ToString().Trim().Length > 0 || args.Count > 0) args.Add(cur.ToString()); return args; }
                depth--; cur.Append(ch);
            }
            else if (ch == ',' && depth == 0) { args.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        return args;
    }

    private static string Read(string repo, params string[] parts)
    {
        string p = Path.Combine(new[] { repo }.Concat(parts).ToArray());
        return File.Exists(p) ? File.ReadAllText(p) : "";
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

    private static void Check(ref int failures, bool ok, string label, string detail = "")
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}" + (ok || string.IsNullOrEmpty(detail) ? "" : " -- " + detail));
        if (!ok) failures++;
    }
}

/// <summary>
/// SOURCE TEXT WITH ITS NON-CODE BLANKED: comments and the contents of strings, PowerShell here-strings and bash
/// here-documents become spaces, line breaks are kept - so a line number in the result is the line number in the file,
/// and a rule's own explanation (a comment, a log message, a usage text) can never trip a source guard. A small lexer
/// per language, deliberately conservative: it knows quotes, escapes, line and block comments, here-strings and
/// here-docs, and nothing else (a kill command built inside a string is caught by (d4)'s by-name rule, which reads the
/// raw text).
/// </summary>
public static class CodeText
{
    public enum Lang { PowerShell, Bash, CSharp, Python }

    public static Lang? LangOf(string path)
    {
        string e = Path.GetExtension(path).ToLowerInvariant();
        return e switch
        {
            ".ps1" or ".psm1" => Lang.PowerShell,
            ".sh" => Lang.Bash,
            ".cs" or ".cpp" or ".cxx" or ".h" => Lang.CSharp,
            ".py" => Lang.Python,
            _ => null,
        };
    }

    public static string Blank(string text, Lang lang)
    {
        var o = new StringBuilder(text.Length);
        int i = 0, n = text.Length;
        char Peek(int k) => i + k < n ? text[i + k] : '\0';
        void Keep() { o.Append(text[i]); i++; }
        void Drop() { char c = text[i]; o.Append(c == '\n' || c == '\r' ? c : ' '); i++; }
        bool AtLineStart() { int k = i - 1; return k < 0 || text[k] == '\n'; }
        bool RestOfLineBlank(int from) { for (int k = from; k < n && text[k] != '\n'; k++) if (!char.IsWhiteSpace(text[k])) return false; return true; }

        while (i < n)
        {
            char c = text[i];
            switch (lang)
            {
                case Lang.PowerShell:
                    if (c == '<' && Peek(1) == '#') { while (i < n && !(text[i] == '#' && Peek(1) == '>')) Drop(); if (i < n) { Drop(); Drop(); } continue; }
                    if (c == '#') { while (i < n && text[i] != '\n') Drop(); continue; }
                    if (c == '@' && (Peek(1) == '\'' || Peek(1) == '"') && RestOfLineBlank(i + 2))
                    {
                        char q = Peek(1);
                        Keep(); Drop();
                        while (i < n && text[i] != '\n') Drop();
                        while (i < n && !(AtLineStart() && text[i] == q && Peek(1) == '@')) Drop();
                        if (i < n) { Drop(); Keep(); }
                        continue;
                    }
                    if (c == '`') { Keep(); if (i < n) Keep(); continue; }
                    if (c == '\'') { Keep(); while (i < n) { if (text[i] == '\'' && Peek(1) == '\'') { Drop(); Drop(); continue; } if (text[i] == '\'') break; Drop(); } if (i < n) Keep(); continue; }
                    if (c == '"') { Keep(); while (i < n) { if (text[i] == '`') { Drop(); if (i < n) Drop(); continue; } if (text[i] == '"' && Peek(1) == '"') { Drop(); Drop(); continue; } if (text[i] == '"') break; Drop(); } if (i < n) Keep(); continue; }
                    Keep(); continue;

                case Lang.Bash:
                    if (c == '#' && (i == 0 || char.IsWhiteSpace(text[i - 1]) || text[i - 1] == ';' || text[i - 1] == '('))
                    { while (i < n && text[i] != '\n') Drop(); continue; }
                    if (c == '<' && Peek(1) == '<' && Peek(2) != '<')
                    {
                        var m = Regex.Match(text.Substring(i), @"^<<(-?)\s*(['""]?)([A-Za-z_][A-Za-z0-9_]*)\2");
                        if (m.Success)
                        {
                            string tag = m.Groups[3].Value;
                            bool dash = m.Groups[1].Value == "-";
                            for (int k = 0; k < m.Length; k++) Keep();
                            while (i < n && text[i] != '\n') Keep();            // the rest of the opening line is code
                            if (i < n) Keep();
                            while (i < n)
                            {
                                int eol = text.IndexOf('\n', i);
                                string line = (eol < 0 ? text.Substring(i) : text.Substring(i, eol - i)).TrimEnd('\r');
                                bool end = (dash ? line.TrimStart('\t') : line) == tag;
                                int stop = eol < 0 ? n : eol + 1;
                                while (i < stop) { if (end) Keep(); else Drop(); }
                                if (end) break;
                            }
                            continue;
                        }
                    }
                    if (c == '\\') { Keep(); if (i < n) Keep(); continue; }
                    if (c == '\'') { Keep(); while (i < n && text[i] != '\'') Drop(); if (i < n) Keep(); continue; }
                    if (c == '"') { Keep(); while (i < n && text[i] != '"') { if (text[i] == '\\') { Drop(); if (i < n) Drop(); continue; } Drop(); } if (i < n) Keep(); continue; }
                    Keep(); continue;

                case Lang.CSharp:
                    if (c == '/' && Peek(1) == '/') { while (i < n && text[i] != '\n') Drop(); continue; }
                    if (c == '/' && Peek(1) == '*') { while (i < n && !(text[i] == '*' && Peek(1) == '/')) Drop(); if (i < n) { Drop(); Drop(); } continue; }
                    if (c == '@' && Peek(1) == '"') { Keep(); Keep(); while (i < n) { if (text[i] == '"' && Peek(1) == '"') { Drop(); Drop(); continue; } if (text[i] == '"') break; Drop(); } if (i < n) Keep(); continue; }
                    if (c == '"' || c == '\'') { char q = c; Keep(); while (i < n && text[i] != q && text[i] != '\n') { if (text[i] == '\\') { Drop(); if (i < n) Drop(); continue; } Drop(); } if (i < n && text[i] == q) Keep(); continue; }
                    Keep(); continue;

                default: // Python
                    if (c == '#') { while (i < n && text[i] != '\n') Drop(); continue; }
                    if ((c == '"' || c == '\'') && Peek(1) == c && Peek(2) == c)
                    { char q = c; Keep(); Keep(); Keep(); while (i < n && !(text[i] == q && Peek(1) == q && Peek(2) == q)) { if (text[i] == '\\') { Drop(); if (i < n) Drop(); continue; } Drop(); } if (i < n) { Keep(); Keep(); Keep(); } continue; }
                    if (c == '"' || c == '\'') { char q = c; Keep(); while (i < n && text[i] != q && text[i] != '\n') { if (text[i] == '\\') { Drop(); if (i < n) Drop(); continue; } Drop(); } if (i < n && text[i] == q) Keep(); continue; }
                    Keep(); continue;
            }
        }
        return o.ToString();
    }
}
