namespace VrfC2SimApp;

/// <summary>
/// THE REAL STP EXPORT, THROUGH THE OFFLINE INTAKE PATH: `VrfC2SimApp --stpexport-selftest`.
/// Pure managed - parse, register, resolve, classify. No bridge call, no MAK runtime, no network,
/// no federation, no C2SIM server.
///
/// WHY A FIXTURE AND NOT A SYNTHETIC CASE. Every number this test asserts was FALSIFIED by the
/// first real STP export the project received (STP-IRON-STORM-SYNTHETIC, characterised 2026-09-20)
/// on a build whose whole offline suite was green:
///   - 0 of 40 units were created, because the export's SystemEntityList/SystemName is the literal
///     string "Not Set" and appsettings ships Vrf:ClientId="STP". One log line, no counts, no
///     override to type, nothing on the C2SIM bus.
///   - 0 of the order's 35 MapGraphicID references resolved, because `_graphicsByC2SimUuid` was
///     filled only from the INITIALIZATION while 34 of the 35 name a graphic carried IN THE ORDER
///     and the 35th names nothing at all; every task fell back to its embedded Location and the
///     order produced ~35 warning lines saying so, one per id.
///   - 5 of 23 tasks carried verbs (ExecutePlanPhase, CRESRV) that were not in the verb table, so
///     they ran as bare movement behind a coverage-gap warning.
/// The counts below are COUNTED FROM THE TWO FILES in data/, which are the producer's bytes
/// unchanged (155,020 and 111,493 bytes, CRLF, 100% ASCII). A re-export that changes them should
/// fail this test loudly rather than quietly changing what the interface does.
/// </summary>
public static class StpExportSelfTest
{
    private const string InitFixture = "STP-IRON-STORM-SYNTHETIC_Initialization.xml";
    private const string OrderFixture = "STP-IRON-STORM-SYNTHETIC_Order.xml";

    // ---- counted from the two files, 2026-09-20 --------------------------------------------
    private const int Units = 40;                 // SystemEntityList/ActorReference, and Unit elements
    private const string ExportSystemName = "Not Set";
    private const int InitGraphics = 24;          // 19 Line + 5 TacticalArea, 0 TaskGraphic
    private const int OrderGraphics = 33;         // 9 Line + 10 Point + 3 TacticalArea + 11 TaskGraphic
    private const int Tasks = 23;
    private const int MapGraphicRefs = 35;        // MapGraphicID elements over all 23 tasks
    private const int DanglingRefs = 1;           // T22's 7843ad57-... exists nowhere in either file
    private const int TasksWithRefs = 16;         // tasks carrying at least one MapGraphicID
    private const int TasksFromGraphic = 15;      // ... of which one (T22) has only the dangling id
    private const int TasksWithBoth = 16;         // MapGraphicID AND an embedded Location on one task

    public static int Run()
    {
        int failures = 0;
        string initPath = FindData(InitFixture), orderPath = FindData(OrderFixture);
        if (initPath == null || orderPath == null)
        {
            Console.Error.WriteLine($"stpexport-selftest: fixtures not found (looked for data/{InitFixture} " +
                                    $"and data/{OrderFixture} upwards from {AppContext.BaseDirectory} and " +
                                    $"{Directory.GetCurrentDirectory()}).");
            return 2;
        }
        Console.WriteLine("=== THE REAL STP EXPORT THROUGH THE OFFLINE INTAKE PATH ===");
        Console.WriteLine($"init  : {initPath}");
        Console.WriteLine($"order : {orderPath}");

        var init = InitParser.Parse(File.ReadAllText(initPath));
        var order = OrderParser.Parse(File.ReadAllText(orderPath));

        failures += CheckIntake(init);
        failures += CheckClientId(init);
        failures += CheckGraphics(init, order);
        failures += CheckVerbs(order);
        failures += CheckDurations(order);

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "stpexport-selftest: ALL CHECKS PASSED"
                                        : $"stpexport-selftest: {failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- 1. what arrived

    private static int CheckIntake(InitData init)
    {
        int f = 0;
        Console.WriteLine();
        Console.WriteLine("--- 1. what the export contains ---");
        Check(ref f, init.Units.Count == Units,
              $"{Units} units parsed (got {init.Units.Count})");
        Check(ref f, init.SystemName == ExportSystemName,
              $"SystemEntityList/SystemName is the literal \"{ExportSystemName}\" (got \"{init.SystemName}\") - " +
              "an unset export field, not a chosen value");
        int placeable = init.Units.Count(u => u.Latitude.Length > 0 && u.Longitude.Length > 0);
        Console.WriteLine($"  [--] {placeable} of {init.Units.Count} unit(s) have a position after the superior " +
                          $"cascade; {init.Units.Count - placeable} inherit from a parent that has none");
        Console.WriteLine($"  [--] graphics parsed from the init: {init.Areas.Count} area(s), {init.Lines.Count} " +
                          $"line(s), {init.Points.Count} point(s), {init.TaskGraphics.Count} task symbol(s)");
        return f;
    }

    // ---------------------------------------------------------------- 2. the ClientId filter

    /// <summary>
    /// THE BLOCKER, AND THE DIAGNOSTIC THAT NOW NAMES ITS FIX. The filter is NOT loosened - an
    /// interface that tasked every producer's units would create somebody else's ORBAT, and C13
    /// (user ruling 2026-09-06) says the init is context and only this client's units are
    /// simulated. What changed is that the failure is actionable and reaches the bus.
    /// </summary>
    private static int CheckClientId(InitData init)
    {
        int f = 0;
        Console.WriteLine();
        Console.WriteLine("--- 2. the Vrf:ClientId filter (the blocker, and its diagnostic) ---");

        int matchedStp = init.Units.Count(u => u.SystemName == "STP");
        int matchedReal = init.Units.Count(u => u.SystemName == ExportSystemName);
        Check(ref f, matchedStp == 0,
              $"with the SHIPPED Vrf:ClientId=\"STP\", {matchedStp} of {init.Units.Count} units match - " +
              "nothing is created and nothing is taskable");
        Check(ref f, matchedReal == Units,
              $"with Vrf__ClientId=\"{ExportSystemName}\", {matchedReal} of {init.Units.Count} units match " +
              "(the whole ORBAT becomes creatable)");

        var counts = ClientIdPolicy.SystemNameCounts(init.Units);
        Check(ref f, counts.Count == 1 && counts[0].Name == ExportSystemName && counts[0].Count == Units,
              $"the diagnostic counts the SystemNames it saw: [{ClientIdPolicy.DescribeSystemNames(counts)}]");
        Check(ref f, ClientIdPolicy.SuggestedOverride(counts) == $"Vrf__ClientId=\"{ExportSystemName}\"",
              $"and proposes the EXACT override to type: {ClientIdPolicy.SuggestedOverride(counts)}");

        string msg = ClientIdPolicy.MismatchMessage("file", "STP", init.Units.Count, counts);
        Console.WriteLine("  [--] the message the ERROR line and the ObservationReport both carry:");
        Console.WriteLine("       " + msg);
        // Every element the brief asked the failure to name, asserted one at a time so a future
        // edit that drops one is a named failure rather than a silently shorter sentence.
        Check(ref f, msg.Contains(ClientIdPolicy.Marker, StringComparison.Ordinal),
              "the message carries the CLIENTID MISMATCH marker a harvest can count");
        Check(ref f, msg.Contains("\"STP\"", StringComparison.Ordinal),
              "it names the ClientId that was in force");
        Check(ref f, msg.Contains($"\"{ExportSystemName}\" x{Units}", StringComparison.Ordinal),
              $"it names every distinct SystemName WITH ITS COUNT (\"{ExportSystemName}\" x{Units})");
        Check(ref f, msg.Contains($"Vrf__ClientId=\"{ExportSystemName}\"", StringComparison.Ordinal),
              "it spells the override exactly as it must be typed");
        Check(ref f, msg.Contains("0 of 40", StringComparison.Ordinal),
              "it says how many of how many matched");
        Check(ref f, msg.Contains("producer", StringComparison.Ordinal),
              "and it names the OTHER fix - the producer stamping its real SystemName - so the " +
              "override does not read as the only answer");

        // A blank SystemName has no override that could work, and the message must not offer one.
        var blank = new List<InitUnit> { new() { Uuid = "u1", SystemName = "" },
                                         new() { Uuid = "u2", SystemName = "   " } };
        var blankCounts = ClientIdPolicy.SystemNameCounts(blank);
        string blankMsg = ClientIdPolicy.MismatchMessage("broadcast", "STP", 2, blankCounts);
        Check(ref f, ClientIdPolicy.SuggestedOverride(blankCounts) == ""
                     && blankMsg.Contains("NO OVERRIDE CAN FIX THIS", StringComparison.Ordinal)
                     && !blankMsg.Contains($"Vrf__ClientId=\"{ClientIdPolicy.BlankSystemName}\"", StringComparison.Ordinal),
              "an init whose SystemNames are ALL BLANK gets 'no override can fix this' and is sent to the " +
              "producer - never an unusable Vrf__ClientId=\"(blank/absent)\"");

        // A MIXED init proposes the most common producer, and still lists the others.
        var mixed = new List<InitUnit> { new() { SystemName = "STP" }, new() { SystemName = "C2SIM" },
                                         new() { SystemName = "C2SIM" }, new() { SystemName = "" } };
        var mixedCounts = ClientIdPolicy.SystemNameCounts(mixed);
        // The ties break ORDINALLY after the count, so the line is the same on every run of the
        // same init - "(blank/absent)" sorts before "STP" because '(' is 0x28.
        Check(ref f, ClientIdPolicy.SuggestedOverride(mixedCounts) == "Vrf__ClientId=\"C2SIM\""
                     && ClientIdPolicy.DescribeSystemNames(mixedCounts)
                        == $"\"C2SIM\" x2, \"{ClientIdPolicy.BlankSystemName}\" x1, \"STP\" x1",
              "a MIXED init proposes the producer that owns most of the tree and still lists the rest, " +
              $"blanks included, in a stable order: [{ClientIdPolicy.DescribeSystemNames(mixedCounts)}]");
        return f;
    }

    // ---------------------------------------------------------------- 3. graphics resolution

    /// <summary>
    /// THE STRUCTURAL FIX: an order's own graphics are registered before its tasks are translated,
    /// and a TaskGraphic is no longer skipped. Exercised through the SAME map and the SAME resolver
    /// the service uses, in the SAME order (init first, then order, order never overwriting init) -
    /// a test that rebuilt the map its own way would prove nothing about the dispatch.
    /// </summary>
    private static int CheckGraphics(InitData init, OrderData order)
    {
        int f = 0;
        Console.WriteLine();
        Console.WriteLine("--- 3. MapGraphicID resolution (was 0 of 35) ---");

        Check(ref f, order.Graphics.Count == OrderGraphics,
              $"{OrderGraphics} tactical graphic(s) parsed FROM THE ORDER (got {order.Graphics.Count}) - " +
              "OrderBody/Entity/PhysicalEntity/MapGraphic/TacticalGraphic, which the schema allows and " +
              "this producer uses exclusively");
        int initGraphicCount = init.Areas.Count + init.Lines.Count + init.Points.Count + init.TaskGraphics.Count;
        Check(ref f, initGraphicCount == InitGraphics,
              $"{InitGraphics} graphic(s) parsed from the init (got {initGraphicCount})");
        Console.WriteLine("  [--] order graphics by element: " +
                          string.Join(", ", order.Graphics.GroupBy(g => g.Element)
                                                 .OrderByDescending(g => g.Count())
                                                 .Select(g => $"{g.Key} x{g.Count()}")));

        // Build the map exactly as VrfC2SimService does: init first, then the order, and the init
        // wins a uuid collision.
        var map = new Dictionary<string, TaskGraphic>(StringComparer.Ordinal);
        foreach (var a in init.Areas)
            if (a.Uuid.Length > 0)
                map[a.Uuid] = new TaskGraphic(a.Uuid, a.Name, TaskGraphic.KindArea, Pts(a.Points));
        foreach (var l in init.Lines)
            if (l.Uuid.Length > 0 && l.Points.Count > 0)
                map[l.Uuid] = new TaskGraphic(l.Uuid, l.Name, TaskGraphic.KindLine, Pts(l.Points));
        foreach (var p in init.Points)
            if (p.Uuid.Length > 0 && p.HasPosition)
                map[p.Uuid] = new TaskGraphic(p.Uuid, p.Name, TaskGraphic.KindPoint,
                                              new List<(double, double, double?)>
                                              { (p.Position.Lat, p.Position.Lon, (double?)p.Position.Elev) });
        foreach (var t in init.TaskGraphics)
            if (t.Uuid.Length > 0 && t.Points.Count > 0)
                map[t.Uuid] = new TaskGraphic(t.Uuid, t.Name,
                                              t.Points.Count >= 2 ? TaskGraphic.KindLine : TaskGraphic.KindPoint,
                                              Pts(t.Points));
        int collisions = 0;
        foreach (var g in order.Graphics)
        {
            if (map.ContainsKey(g.Uuid)) { collisions++; continue; }
            map[g.Uuid] = new TaskGraphic(g.Uuid, g.Name, g.Kind, Pts(g.Points));
        }
        Check(ref f, collisions == 0,
              $"no uuid collision between the init's graphics and the order's (got {collisions}) - the " +
              "two sets are disjoint on this export, so nothing is being shadowed");

        // Resolve every task and count what happened.
        int refs = 0, resolvedRefs = 0, dangling = 0, fromGraphic = 0, fromEmbedded = 0, none = 0;
        int tasksWithRefs = 0, danglingWarns = 0, disagreementWarns = 0, bothPresent = 0;
        var rows = new List<string>();
        foreach (var task in order.Tasks)
        {
            var res = TaskGeometryResolver.Resolve(task, map);
            int ids = task.MapGraphicUuids.Count;
            refs += ids;
            if (ids > 0) tasksWithRefs++;
            if (ids > 0 && task.Points.Count > 0) bothPresent++;
            int unmatched = task.MapGraphicUuids.Count(id => !map.ContainsKey(id));
            dangling += unmatched;
            resolvedRefs += ids - unmatched;
            // The two warning KINDS are counted apart: one is "an id named nothing", the other is
            // m7's consistency check - "the graphic you named and the coordinates you embedded are
            // more than a kilometre apart". Lumping them would hide the second, which is a finding
            // about the EXPORT that only became visible once the references resolved at all.
            foreach (var w in res.Warnings)
                if (w.Contains("DANGLING", StringComparison.Ordinal)) danglingWarns++;
                else if (w.Contains("apart", StringComparison.Ordinal)) disagreementWarns++;
            switch (res.Source)
            {
                case GeometrySource.MapGraphic: fromGraphic++; break;
                case GeometrySource.EmbeddedLocation: fromEmbedded++; break;
                default: none++; break;
            }
            rows.Add($"    {task.ActionCode,-16} mg={ids} loc={task.Points.Count,-2} -> " +
                     $"{res.Source,-16} {res.Points.Count,2} vertex(es)" +
                     (unmatched > 0 ? $"  [{unmatched} DANGLING]" : ""));
        }

        Check(ref f, order.Tasks.Count == Tasks, $"{Tasks} tasks parsed (got {order.Tasks.Count})");
        Check(ref f, refs == MapGraphicRefs,
              $"{MapGraphicRefs} MapGraphicID reference(s) over all tasks (got {refs})");
        Check(ref f, tasksWithRefs == TasksWithRefs,
              $"{TasksWithRefs} task(s) carry at least one (got {tasksWithRefs})");
        Check(ref f, resolvedRefs == MapGraphicRefs - DanglingRefs,
              $"*** {resolvedRefs} of {MapGraphicRefs} references RESOLVE (was 0) ***");
        Check(ref f, dangling == DanglingRefs,
              $"exactly {DanglingRefs} reference is genuinely DANGLING (got {dangling}) - T22's " +
              "7843ad57-e8d9-fc50-8211-3b3a1dc357c9 appears in neither file as a graphic");
        Check(ref f, danglingWarns == 1,
              $"and it produces ONE warning naming it, on ONE task (got {danglingWarns}) - not one line " +
              "per unresolved id, which on this order used to be 35");
        Check(ref f, fromGraphic == TasksFromGraphic,
              $"{fromGraphic} task(s) now take their geometry from a MapGraphic (was 0; the 16th, T22, " +
              "has only the dangling id and falls back to its embedded Location)");
        // A FINDING ABOUT THE EXPORT that could not be seen while nothing resolved: on 7 tasks the
        // graphic the order NAMES and the coordinates it EMBEDS are more than a kilometre apart.
        // The MapGraphic wins (R1) and the separation is reported; it is the producer's to explain.
        Check(ref f, disagreementWarns == 7,
              $"{disagreementWarns} task(s) name a graphic whose geometry is MORE THAN 1 km from the " +
              "embedded Location on the same task - the order disagreeing with itself, invisible until " +
              "the references resolved, reported rather than guessed at (m7)");
        Check(ref f, fromGraphic + fromEmbedded + none == Tasks,
              $"every task has a source: {fromGraphic} MapGraphic + {fromEmbedded} EmbeddedLocation + " +
              $"{none} None = {Tasks}");

        // THE PRECEDENCE RULE, on the first message where the case actually occurs. The schema
        // imposes none ("WHERE is represented by hasLocation and/or hasMapGraphicID",
        // C2SIM_SMX_LOX_CWIX2024.xsd:3787/:3808, both elements 0..unbounded at :1391-1392); the
        // convention is the user's R1 note of 2026-09-14, "precedence MapGraphicID > embedded,
        // consistency check when both".
        Check(ref f, bothPresent == TasksWithBoth,
              $"{bothPresent} task(s) carry BOTH a MapGraphicID and an embedded Location - the case the " +
              "precedence rule exists for, and the first export on disk that has it");
        {
            var both = order.Tasks.First(t => t.MapGraphicUuids.Count > 0 && t.Points.Count > 0
                                              && t.MapGraphicUuids.All(map.ContainsKey));
            var res = TaskGeometryResolver.Resolve(both, map);
            Check(ref f, res.Source == GeometrySource.MapGraphic,
                  "when both are present the MapGraphic wins (R1, user ruling 2026-09-14)");
            Check(ref f, res.Log.Any(l => l.Contains("embedded Location point(s) ignored", StringComparison.Ordinal)),
                  "and the embedded half is NOT dropped in silence - the count and the separation between " +
                  "the two answers are logged (m7's consistency check)");
            var stripped = both with { MapGraphicUuids = Array.Empty<string>(), MapGraphicUuid = "" };
            var fallback = TaskGeometryResolver.Resolve(stripped, map);
            Check(ref f, fallback.Source == GeometrySource.EmbeddedLocation && fallback.Points.Count > 0,
                  "and with the reference removed the SAME task falls back to its embedded Location - the " +
                  "fallback is precedence, not a workaround being removed");
        }

        Console.WriteLine("  [--] per-task geometry source (verb, #MapGraphicID, #Location -> source, vertices):");
        foreach (var r in rows) Console.WriteLine(r);
        return f;
    }

    // ---------------------------------------------------------------- 4. the verbs

    private static int CheckVerbs(OrderData order)
    {
        int f = 0;
        Console.WriteLine();
        Console.WriteLine("--- 4. the verbs this export uses ---");
        var byCode = order.Tasks.GroupBy(t => t.ActionCode)
                          .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        Console.WriteLine("  [--] " + string.Join(", ", byCode.OrderByDescending(kv => kv.Value)
                                                              .Select(kv => $"{kv.Key} x{kv.Value}")));
        Check(ref f, byCode.TryGetValue("ExecutePlanPhase", out int epp) && epp == 3,
              $"the export carries ExecutePlanPhase x3 (got {(byCode.TryGetValue("ExecutePlanPhase", out int e) ? e : 0)})");
        Check(ref f, byCode.TryGetValue("CRESRV", out int cr) && cr == 2,
              $"and CRESRV x2 (got {(byCode.TryGetValue("CRESRV", out int c) ? c : 0)})");

        var unrecognised = order.Tasks.Where(t => !VerbMapping.Classify(t.ActionCode).Recognized)
                                .Select(t => t.ActionCode).Distinct(StringComparer.Ordinal).ToList();
        Check(ref f, unrecognised.Count == 0,
              $"EVERY verb in this export is now in the table (was 2 unrecognised over 5 tasks; got " +
              $"[{string.Join(", ", unrecognised)}])");

        int inPlace = order.Tasks.Count(t => VerbMapping.Classify(t.ActionCode).Intent == TaskIntent.HoldInPlace);
        Check(ref f, inPlace == 3,
              $"the 3 ExecutePlanPhase tasks classify as HoldInPlace (got {inPlace}): no vendor task is " +
              "issued, the unit is reported where it stands, and the geometry it carries is named as not " +
              "driven - a phase marker is not a move");
        return f;
    }

    // ---------------------------------------------------------------- 5. the second export defect

    /// <summary>
    /// A DEFECT THIS TEST FOUND, not one it was written for, and the reason it is here rather than
    /// in a report: the characterisation recorded "all 23 carry a Duration", which is true of the
    /// ELEMENT and false of the VALUE. Every one of them is the canonical ISO-8601 short form -
    /// PT20M, PT30M, PT50M - and C2SIM 1.1 does not allow it: IsoTimeDurationBaseType is an
    /// xs:string restricted to the pattern
    ///   [P]{1}[0-9]{2}[Y]{1}[0-9]{2}[M]{1}[0-9]{2}[D]{1}T{1}[0-9]{2}[H]{1}[0-9]{2}[M]{1}[0-9]{2}[S]{1}
    /// (C2SIM_SMX_LOX_CWIX2024.xsd:17-24). Every field, two digits, all present. COA-STP1 obeys it
    /// (P00Y00M00DT01H20M00S x32, P00Y00M00DT02H00M00S x10); this export does not.
    ///
    /// CONSEQUENCE, and it is not small: R4 closes hold-type tasks at dispatch + Duration, and
    /// almost this entire order is hold-type work. With no decodable Duration NOTHING has an end
    /// time, no STREND successor is released by a completion, and the whole 9-chain structure runs
    /// on the flat predecessor floor instead of on the order's own clock.
    ///
    /// THE DECODER IS NOT LOOSENED TO ACCEPT IT. Accepting PT20M here would make this interface
    /// the only thing in the federation that could read the order, and would hide a one-field
    /// producer fix behind a silent success - the same shape as the ClientId filter in section 2.
    /// </summary>
    private static int CheckDurations(OrderData order)
    {
        int f = 0;
        Console.WriteLine();
        Console.WriteLine("--- 5. Duration: the export's SECOND schema violation (found by this test) ---");
        int decodable = order.Tasks.Count(t => t.DurationMs > 0);
        Check(ref f, decodable == 0,
              $"{decodable} of {order.Tasks.Count} tasks carry a Duration this interface can decode - the " +
              "export writes PT20M/PT30M/PT50M, and C2SIM 1.1 requires P##Y##M##DT##H##M##S (xsd:17-24)");
        Check(ref f, order.Warnings.Count(w => w.Contains("VIOLATES THE C2SIM 1.1 SCHEMA", StringComparison.Ordinal))
                     == order.Tasks.Count,
              $"the parser says so ONCE PER TASK ({order.Tasks.Count} warning(s)), names the required pattern " +
              "and points the fix at the producer");
        // The reference order proves the decoder is not simply broken.
        string coa = FindData("COA-STP1_Order.xml");
        if (coa != null)
        {
            var refOrder = OrderParser.Parse(File.ReadAllText(coa));
            Check(ref f, refOrder.Tasks.Count > 0 && refOrder.Tasks.All(t => t.DurationMs > 0)
                         && !refOrder.Warnings.Any(w => w.Contains("VIOLATES", StringComparison.Ordinal)),
                  $"and the schema-conforming reference order still decodes every one of its " +
                  $"{refOrder.Tasks.Count} Durations - the decoder is right, the export is wrong");
        }
        // What it costs the new HoldInPlace verb, said out loud rather than discovered live.
        int markerNoEnd = order.Tasks.Count(t => VerbMapping.Classify(t.ActionCode).Intent == TaskIntent.HoldInPlace
                                                 && t.DurationMs <= 0);
        Console.WriteLine($"  [--] CONSEQUENCE: {markerNoEnd} ExecutePlanPhase task(s) issue no VR-Forces task " +
                          "AND have no decodable Duration, so nothing could ever end them - they are refused " +
                          "as MALFORMED (Q4). Fix the export's Duration form and they run. Every other task " +
                          "in this order is equally without an end time; they merely hide it behind a move.");
        return f;
    }

    // ---------------------------------------------------------------- helpers

    private static List<(double Lat, double Lon, double? Elev)> Pts(
        IEnumerable<(double Lat, double Lon, double Elev)> src)
        => src.Select(p => (p.Lat, p.Lon, (double?)p.Elev)).ToList();

    private static string FindData(string name)
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            for (int i = 0; dir != null && i < 10; i++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "data", name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
