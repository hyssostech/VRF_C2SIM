namespace VrfC2SimApp;

/// <summary>
/// Offline check of InitParser against a real C2SIM init file. Pure managed (no bridge,
/// no MAK, no VR-Forces): `VrfC2SimApp --parse-init &lt;file&gt; [clientId]`.
/// Prints a summary so the parse can be eyeballed against the golden trace (which
/// created 49 units + 4 areas from the STP init).
/// </summary>
public static class InitParseCheck
{
    /// <summary>SF-A: the spacing the SKIPPED-group line illustrates with. It is the RULED
    /// company number (Vrf:DeStackSpacingMeters' 700 m, C14 2026-09-07), which is the value an
    /// operator turning Vrf:DeStackEchelonFallbackMeters on would reach for first - the two keys
    /// are about the same echelons. It is an ILLUSTRATION, not a default: the fallback ships at 0
    /// and this diagnostic changes nothing.</summary>
    public const double DeStackFallbackIllustrationMeters = 700.0;

    public static int Run(string path, string clientId = "STP")
    {
        if (!File.Exists(path)) { Console.WriteLine($"file not found: {path}"); return 1; }
        var data = InitParser.Parse(File.ReadAllText(path));

        Console.WriteLine($"=== InitParser check: {Path.GetFileName(path)} ===");
        Console.WriteLine($"SystemName: {data.SystemName}");
        Console.WriteLine($"Units: {data.Units.Count}");
        Console.WriteLine($"  with location: {data.Units.Count(u => u.Latitude.Length > 0)}");
        Console.WriteLine($"  hostility: {Group(data.Units.Select(u => u.HostilityCode))}");
        Console.WriteLine($"  systemName: {Group(data.Units.Select(u => u.SystemName))}");

        // Units the interface WOULD create: our clientId + hostility + coords present.
        int wouldCreate = data.Units.Count(u =>
            u.Uuid.Length > 0 && u.SystemName == clientId &&
            u.HostilityCode.Length > 0 && u.Latitude.Length > 0 && u.Longitude.Length > 0);
        Console.WriteLine($"  would create (clientId={clientId}): {wouldCreate}  (golden trace: 49)");

        Console.WriteLine($"Areas: {data.Areas.Count}  (golden trace: 4)");
        foreach (var a in data.Areas)
            Console.WriteLine($"  area '{a.Name}' pts={a.Points.Count}");

        // ---- V3: the LINE and POINT graphics, and what would be created from them -------------
        // The interface used to parse these away entirely (only S.TacticalAreaType was collected),
        // so this section is the first place the objective/phase-line/control-point geometry STP
        // ships is visible without opening the XML. COA-STP1: 35 areas + 41 lines + 317 points.
        Console.WriteLine($"Lines: {data.Lines.Count}  " +
                          $"(Route={data.Lines.Count(l => l.Kind == "Route")}, " +
                          $"Boundary={data.Lines.Count(l => l.Kind == "Boundary")}; " +
                          $"with uuid={data.Lines.Count(l => l.Uuid.Length > 0)})");
        Console.WriteLine($"  vertex histogram: {Histogram(data.Lines.Select(l => l.Points.Count))}");
        int creatableLines = data.Lines.Count(l => l.Points.Count >= 2);
        Console.WriteLine($"  creatable as VRF routes (>=2 vertices): {creatableLines}" +
                          $"  (skipped, <2 vertices: {data.Lines.Count - creatableLines})");
        foreach (var l in data.Lines.Take(5))
            Console.WriteLine($"    line '{l.Name}' [{l.Kind}] pts={l.Points.Count} uuid={l.Uuid}");

        Console.WriteLine($"Points: {data.Points.Count}  (with uuid={data.Points.Count(p => p.Uuid.Length > 0)}, " +
                          $"with position={data.Points.Count(p => p.HasPosition)})");
        Console.WriteLine($"  location-count histogram: {Histogram(data.Points.Select(p => p.Points.Count))}");
        foreach (var p in data.Points.Take(5))
            Console.WriteLine($"    point '{p.Name}' uuid={p.Uuid} " +
                              (p.HasPosition ? $"{p.Position.Lat},{p.Position.Lon}" : "(NO POSITION)"));

        // The GRAPHICS CREATION PLAN - what DispatchInit would queue, per flag. Printed next to
        // the unit placement plan below so one command shows everything an init would create.
        Console.WriteLine("Graphics creation plan (what DispatchInit would queue):");
        Console.WriteLine($"  areas  -> CreateControlArea x{data.Areas.Count}  (always; uuid = the C2SIM uuid)");
        Console.WriteLine($"  lines  -> CreateRoute       x{creatableLines}   (only when Vrf:CreateInitLines=true; default false)");
        Console.WriteLine($"  points -> CreateWaypoint    x{data.Points.Count(p => p.HasPosition)}   (only when Vrf:CreateInitPoints=true; default false)");
        // Uniqueness matters: createWaypoint/createRoute document "the name must be unique (if
        // specified)" (vrfRemoteController.h:987, :1019). Say so here rather than find out live.
        var allGraphicNames = data.Areas.Select(a => a.Name)
            .Concat(data.Lines.Select(l => l.Name))
            .Concat(data.Points.Select(p => p.Name)).ToList();
        var nameDups = allGraphicNames.GroupBy(n => n).Where(g => g.Count() > 1).ToList();
        var unitNames = data.Units.Select(u => u.Name).ToHashSet();
        int nameVsUnit = allGraphicNames.Count(unitNames.Contains);
        Console.WriteLine($"  graphic names: {allGraphicNames.Count} total, {allGraphicNames.Distinct().Count()} distinct, " +
                          $"{nameDups.Count} duplicated, {nameVsUnit} colliding with a UNIT name" +
                          (nameDups.Count > 0 ? "  <-- createWaypoint/createRoute want unique names" : ""));

        Console.WriteLine("First 6 creatable units (name | host | sidc | dis | lat,lon):");
        foreach (var u in data.Units.Where(u =>
                     u.SystemName == clientId && u.Latitude.Length > 0).Take(6))
            Console.WriteLine($"  {u.Name,-14} {u.HostilityCode,-6} {u.SymbolId,-15} {u.DisEntityType,-18} {u.Latitude},{u.Longitude}");

        // What the app would CREATE, grouped by the PLANNED VR-Forces DIS type (via
        // UnitTranslator, i.e. the SIDC dispatch - NOT the init's DisEntityType). This is
        // the E1 view: per-type formation names key on the CREATED aggregate type
        // (NEXT_SESSION_GUIDANCE sec 4 E1), so experiments need units per created type.
        var plans = data.Units
            .Where(u => u.Uuid.Length > 0 && u.SystemName == clientId &&
                        u.HostilityCode.Length > 0 && u.Latitude.Length > 0 && u.Longitude.Length > 0)
            .Select(u => (Unit: u, Plan: UnitTranslator.Plan(
                string.IsNullOrEmpty(u.ElevationAgl) ? u with { ElevationAgl = "1000.0" } : u)))
            .ToList();
        Console.WriteLine("Planned creations by created type (up to 3 examples each - name uuid lat,lon):");
        foreach (var g in plans.GroupBy(p => (p.Plan.IsAggregate, Type: TypeStr(p.Plan.Type)))
                               .OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"  {(g.Key.IsAggregate ? "AGG" : "ENT")} {g.Key.Type} x{g.Count()}");
            foreach (var p in g.Take(3))
                Console.WriteLine($"      {p.Unit.Name,-16} {p.Unit.Uuid} {p.Unit.Latitude},{p.Unit.Longitude}");
        }

        // R8 view (docs/UNIT_MOVEMENT_RESEARCH.md sec 4): stacked-coordinate groups among
        // the creatable units - identical spawn coordinates are the COA-STP1 pathology
        // that blocks aggregate marching. Same grouping key as the runtime DeStacker.
        //
        // SCOPE (cold-start review, 2026-09-20): this must use the SAME CompositionPlan
        // classification ProcessInitializationLocked/DeStacker.Apply use, not a raw coordinate
        // grouping. Before this fix the diagnostic reported every coordinate-sharing unit
        // (including a company's own declared platoons, whose "shared" coordinate is only the
        // InitParser superior cascade - InitParser.cs:144-153) as something DeStackCreates
        // would move, while the real de-stack (SF2) excludes composed children entirely. That
        // let this tool disagree with the app it describes (R9 lean: this used to print "4
        // units affected" while the runtime moves 0). Goes through ComputeStackedGroups below,
        // the SAME function --destack-selftest cross-checks against DeStacker.Apply, so the two
        // cannot drift apart again. Assumes Vrf:ComposeHierarchy=true, the default
        // (appsettings.json) and what every runner run uses.
        var planList = plans.Select(p => p.Plan).ToList();
        var hierarchy = plans.Select(p => (p.Unit.Uuid, SuperiorUuid: (p.Unit.SuperiorUuid ?? "").Trim())).ToList();
        var (groupCount, unitsAffected, composedChildIndices) = ComputeStackedGroups(planList, hierarchy);
        // Recomputed here only for the per-group example listing below (Console output, not a
        // count) - the counts themselves come from ComputeStackedGroups, not from this.
        var comp = CompositionPlan.Classify(planList, hierarchy);
        var stacks = plans
            .Where((p, i) => comp.IsIndependent(i))
            .GroupBy(p => DeStacker.CoordKey(p.Plan.Pos.LatDeg, p.Plan.Pos.LonDeg))
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Count())
            .ToList();
        Console.WriteLine($"Stacked-coordinate groups (2+ INDEPENDENT creatable units at identical lat/lon; " +
                          $"composed children excluded per CompositionPlan, ComposeHierarchy=true assumed): " +
                          $"{groupCount}" +
                          (groupCount > 0 ? $"  ({unitsAffected} units affected; Vrf:DeStackCreates would spread them)" : ""));
        if (composedChildIndices.Count > 0)
            Console.WriteLine($"  {composedChildIndices.Count} composed child(ren), not counted in the " +
                              "INDEPENDENT groups above (they take no 700 m ring slot): " +
                              string.Join(", ", composedChildIndices.OrderBy(i => i).Take(12)
                                                    .Select(i => plans[i].Unit.Name)) +
                              (composedChildIndices.Count > 12 ? ", ..." : ""));
        // THE SECOND LANE (user ruling 2026-09-21): composed SIBLINGS that share a coordinate are
        // spread around their parent at their OWN echelon's spacing. Reported here with the same
        // classifier and the same spacing table the runtime uses, so this diagnostic keeps telling
        // the truth about what Vrf:DeStackCreates would do to the file.
        var echelons = plans.Select(p => EchelonSpacing.KeyOf(p.Unit.EchelonCode, p.Unit.SymbolId)).ToList();
        var siblingPlans = planList.ToList();
        var sibling = DeStacker.ApplyComposedSiblings(siblingPlans, comp.ComposedGroups, echelons,
                                                      k => EchelonSpacing.SpacingFor(k, 0.0), 0.0,
                                                      out var siblingSkipped);
        Console.WriteLine($"Composed-sibling groups (2+ children of one parent at identical lat/lon; " +
                          $"Vrf:DeStackComposedSiblings would spread them around the parent at their " +
                          $"own echelon's spacing): {sibling.Count}" +
                          (siblingSkipped.Count > 0
                              ? $"  ({siblingSkipped.Count} group(s) have no echelon the table covers " +
                                "and would NOT be spread)"
                              : ""));
        // SF-D4 (cold-start review of 1d0fb69, 2026-09-21): PRINT BOTH NUMBERS, NAMED. This line
        // used to read "350 m rings", which is the SPACING - the minimum separation the ruling
        // asks for - and NOT the radius any child is moved by. Since N4 the two are different
        // numbers (r = spacing / (2 sin(pi/N)): 175.0 m at N=2, 202.1 m at N=3, 350.0 m only at
        // N=6), so an operator reading "350 m rings" and then measuring 202 m on the map has been
        // told the wrong thing by the diagnostic that exists to tell him what will happen. Both
        // are printed, each labelled with what it is, and the displacement is the one the runtime
        // actually applied (g.Moved), not a third derivation of it.
        foreach (var g in sibling.Take(5))
            Console.WriteLine("  " + DeStacker.DescribeSiblingGroup(g));
        // SF-A (cold-start review of 1d0fb69, 2026-09-21): THE SKIPPED GROUPS, SIZED, AND WHAT
        // THE ONE SETTING WOULD DO TO THEM. Until now this diagnostic said only how MANY groups
        // were skipped, so "no shipped fixture has more than 3 composed siblings in one group"
        // could stand in the code as a remark for a week while R9 full carried larger ones -
        // skipped, invisible, and one un-shipped key (Vrf:DeStackEchelonFallbackMeters, default
        // 0) away from being spread. An operator considering that key needs the radius it would
        // produce BEFORE the run, not after.
        foreach (var s in siblingSkipped.Take(5))
            Console.WriteLine($"  SKIPPED: {s.Count} child(ren) of {s.ParentName} - {s.Reason}. " +
                              $"With Vrf:DeStackEchelonFallbackMeters={DeStackFallbackIllustrationMeters:F0} " +
                              $"they would take a ring of radius " +
                              $"{DeStacker.CentroidPreservingRadius(s.Count, DeStackFallbackIllustrationMeters):F1} m.");
        foreach (var g in stacks.Take(5))
            Console.WriteLine($"  {g.Count()} units at {g.Key.Lat},{g.Key.Lon}: " +
                              string.Join(", ", g.Take(4).Select(p => p.Unit.Name)) + (g.Count() > 4 ? ", ..." : ""));

        return 0;
    }

    /// <summary>
    /// THE STACK COUNT AND AFFECTED-UNIT COUNT, computed ONCE so `--parse-init` and
    /// `--destack-selftest` can never print two different answers for the same file again (the
    /// defect this replaces: this diagnostic used to group by raw coordinate and ignore
    /// CompositionPlan, so it called a company's own composed platoons "affected" when the real
    /// de-stack - SF2, 2026-09-20 - holds them with their parent and moves nothing). Uses the
    /// SAME CompositionPlan.Classify and DeStacker.CoordKey the runtime de-stack uses; a
    /// composed child neither anchors nor joins a group, matching DeStacker.Apply exactly.
    /// </summary>
    public static (int Groups, int UnitsAffected, IReadOnlySet<int> ComposedChildIndices) ComputeStackedGroups(
        IReadOnlyList<CreationPlan> plans, IReadOnlyList<(string Uuid, string SuperiorUuid)> hierarchy)
    {
        var comp = CompositionPlan.Classify(plans, hierarchy);
        var stacks = Enumerable.Range(0, plans.Count)
            .Where(i => comp.IsIndependent(i))
            .GroupBy(i => DeStacker.CoordKey(plans[i].Pos.LatDeg, plans[i].Pos.LonDeg))
            .Where(g => g.Count() > 1)
            .ToList();
        return (stacks.Count, stacks.Sum(g => g.Count()), comp.ComposedChildIndices);
    }

    private static string TypeStr(VrfC2Sim.EntityTypeSpec t)
        => $"{t.Kind}.{t.Domain}.{t.Country}.{t.Category}.{t.Subcategory}.{t.Specific}.{t.Extra}";

    private static string Histogram(IEnumerable<int> counts) =>
        string.Join(", ", counts.GroupBy(c => c).OrderBy(g => g.Key).Select(g => $"{g.Key}pt x{g.Count()}"));

    private static string Group(IEnumerable<string> vals) =>
        string.Join(", ", vals.GroupBy(v => v.Length == 0 ? "(none)" : v)
                               .OrderByDescending(g => g.Count())
                               .Select(g => $"{g.Key}={g.Count()}"));
}
