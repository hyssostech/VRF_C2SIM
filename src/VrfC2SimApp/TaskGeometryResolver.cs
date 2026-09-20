namespace VrfC2SimApp;

/// <summary>Where a task's geometry came from (R1). Both paths are valid C2SIM.</summary>
public enum GeometrySource
{
    /// <summary>One or more MapGraphicID(s) resolved to objects created at init.</summary>
    MapGraphic,
    /// <summary>The task's embedded Location elements - valid C2SIM, and what STP exports
    /// today (user ruling: the embedded path stays supported, it is not a workaround).</summary>
    EmbeddedLocation,
    /// <summary>Neither: the task carries no geometry at all (R2 executes it in place).</summary>
    None,
}

/// <summary>
/// ONE graphic named in the init, addressable by the C2SIM uuid it was authored under, and holding
/// its AUTHORED points.
///
/// M5 (cold-start review of 5c67d41) - WHAT THIS MAP IS NOT. It is not a record of what VR-Forces
/// created: nothing here touches a VR-Forces object, and a row is written whether or not the
/// graphic is created (Vrf:CreateInitLines / Vrf:CreateInitPoints). A MapGraphicID resolves to
/// AUTHORED GEOMETRY - the very points the create would carry - so making the resolution depend on
/// a creation flag would refuse a task its own coordinates for a reason that has nothing to do with
/// the task. The uuid match IS object identity where objects exist (createControlArea and the route
/// / waypoint calls all take the C2SIM uuid as startingUUID, VrfFacade.cpp:725-737,
/// vrfRemoteController.h:991-1039), so the two readings agree wherever both apply.
///
/// The claim that used to stand here - that lines and points "join it unchanged when
/// Vrf:CreateInitLines/Points lands" - was wrong twice: nothing wrote those rows, and the resolver
/// never needed the flags. Measured on data/COA-STP1_Initialization.xml: 35 TacticalArea, 41 Line,
/// 317 Point, 16 TaskGraphic - so an areas-only map covered 35 of 409 graphics, and a MapGraphicID
/// naming a phase line, an axis of advance (the graphic the doctrine table defines MOVE and PENTRT
/// against), a boundary or an attack-by-fire position matched nothing.
/// </summary>
public sealed record TaskGraphic(string Uuid, string Name, string Kind,
                                 IReadOnlyList<(double Lat, double Lon, double? Elev)> Points)
{
    /// <summary>
    /// SF5 (cold-start review of 9d67f97): IS THIS THE SAME GRAPHIC, OR A DIFFERENT ONE UNDER THE
    /// SAME UUID? The order-graphic registration used to compare only the uuid, so pushing THE SAME
    /// ORDER TWICE - which the demo posture does - emitted one "Two different graphics under one
    /// uuid is a data defect in the export" WARNING per graphic (33 of them on the Iron Storm
    /// export), every one of them false. The two cases are genuinely different and now read
    /// differently: an identical re-publication is a no-op worth one counted line, a CONFLICTING
    /// one is the data defect the sentence describes.
    ///
    /// Name and Kind are compared ORDINALLY (both sides are already trimmed by their parsers); the
    /// vertices must match in ORDER and exactly, because a reordered or nudged vertex list IS a
    /// different geometry and the whole point of the check is that a task's route must not depend
    /// on which message arrived first. Elevation is part of the comparison: a graphic that gains
    /// elevations is not the graphic that had none.
    /// </summary>
    public bool SameContentAs(TaskGraphic other)
    {
        if (other is null) return false;
        if (!string.Equals(Name ?? "", other.Name ?? "", StringComparison.Ordinal)) return false;
        if (!string.Equals(Kind ?? "", other.Kind ?? "", StringComparison.Ordinal)) return false;
        var a = Points ?? Array.Empty<(double, double, double?)>();
        var b = other.Points ?? Array.Empty<(double, double, double?)>();
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].Lat != b[i].Lat || a[i].Lon != b[i].Lon) return false;
            if (a[i].Elev is double ea)
            {
                if (b[i].Elev is not double eb || ea != eb) return false;
            }
            else if (b[i].Elev is not null) return false;
        }
        return true;
    }

    /// <summary>
    /// SF5: WHAT HAPPENS WHEN A UUID IS PUBLISHED TWICE. One function, so the order path and the
    /// init path cannot disagree about it and the three cases are asserted rather than read out of
    /// a log.
    ///
    /// THE LIFETIME this encodes, in full:
    ///   * The graphic registry is NEVER cleared. It spans every order and every initialization of
    ///     a run, like _taskByUuid.
    ///   * An ORDER never replaces a published uuid: <see cref="Registration.Conflicting"/> keeps
    ///     the existing graphic, because a task already in flight may be driving it and taking the
    ///     newer one would make a route depend on message order.
    ///   * An INITIALIZATION does replace it - the init is the shared world every order is written
    ///     against - and since SF5 it says so on the same <see cref="Registration.Conflicting"/>.
    ///   * <see cref="Registration.Identical"/> is silent on BOTH paths: re-pushing an order (the
    ///     demo posture) and a duplicate init delivery (late-join QUERYINIT plus a broadcast) are
    ///     normal, not data defects.
    /// </summary>
    public enum Registration
    {
        /// <summary>Nothing was published under this uuid yet.</summary>
        New,
        /// <summary>The same graphic again, vertex for vertex. A no-op, counted, never warned.</summary>
        Identical,
        /// <summary>A DIFFERENT graphic under a published uuid - a data defect in the export.</summary>
        Conflicting,
    }

    /// <summary>Classify an incoming registration against whatever is already published.</summary>
    public static Registration Classify(TaskGraphic existing, TaskGraphic incoming)
        => existing is null ? Registration.New
           : existing.SameContentAs(incoming) ? Registration.Identical
           : Registration.Conflicting;

    /// <summary>Area graphics (the 35 tactical areas) reduce to ONE point, their centroid: an
    /// area is a place to go to, not a path to follow. Everything else contributes its vertices
    /// in order (a line is a route, a point is a point).</summary>
    public const string KindArea = "area";

    /// <summary>A C2SIM Line: a phase line, an axis of advance, a boundary, a breach lane. Its
    /// vertices are contributed IN ORDER - the line is a path, and the vendor object it is created
    /// as is a route (createRoute, vrfRemoteController.h:1023-1039).</summary>
    public const string KindLine = "line";

    /// <summary>A C2SIM Point: a control point, a decision point, a target reference point. ONE
    /// vertex, its authored position.</summary>
    public const string KindPoint = "point";
}

/// <summary>
/// R1 TRANSITION - WHERE A TASK'S GEOMETRY COMES FROM.
///
/// The C2SIM way to say "this task is about THAT graphic" is MapGraphicID: the init creates the
/// graphic, the order references it by uuid, and the interface uses the object it already made.
/// STP does not emit one today - its builder writes MapGraphicID only when
/// IncludeMapGraphicIdInTasks is set (C2SimBridgeAgentParams.cs:128, C2SimXmlBuilder.cs ~383/460),
/// which was off for both exports on disk, so 0 of COA-STP1's 42 tasks carry one and each task
/// instead carries the first tactical graphic LINEARISED into embedded Location elements. That
/// export gap is STP-801, on the STP side.
///
/// USER RULING: the embedded Location is VALID C2SIM and stays supported next to MapGraphicID -
/// it is a fallback in the sense of PRECEDENCE, not a workaround to be removed. So: resolve the
/// MapGraphicID(s) when they are there, use the embedded Location when they are not, and SAY
/// WHICH PATH WAS USED either way. No name heuristics (the withdrawn V4 matched task names
/// against area names); no verb-typed interpretation of the points (that is V4b, separate).
///
/// ================== THE RULE WHEN A TASK CARRIES BOTH, STATED AND SOURCED ==================
/// (Re-stated 2026-09-20 because the real STP export is the first message on disk where the case
/// actually occurs: 18 of Iron Storm's 23 tasks carry a MapGraphicID AND an embedded Location.)
///
/// 1. THE SCHEMA IMPOSES NO PRECEDENCE, and says so deliberately. `Location` and `MapGraphicID`
///    are both `minOccurs="0" maxOccurs="unbounded"` on the same ActionGroup sequence
///    (C2SIM_SMX_LOX_CWIX2024.xsd:1384-1396), and the TaskType/TaskGroup annotation reads
///    "WHERE is represented by hasLocation AND/OR hasMapGraphicID reference" (xsd:3787, :3808).
///    Both may be present, either may repeat, and nothing in the XSD - no xs:key, no xs:keyref,
///    no annotation - prefers one. Any tie-break is an APPLICATION convention and has to be
///    written down somewhere; this is where.
/// 2. THE CONVENTION IS MapGraphicID > EMBEDDED LOCATION, and it is the user's own
///    (docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md:1073-1079, R1 note of
///    2026-09-14: "precedence MapGraphicID > embedded, consistency check when both").
/// 3. WHY THAT WAY ROUND, in the data's own terms: the reference names a graphic that carries its
///    own KIND (area / line / point / task symbol), while an embedded Location list is a bare
///    sequence of coordinates with no shape attached - STP builds it by LINEARISING the first
///    tactical graphic, dropping the kind and everything after the first graphic
///    (docs/STP_TASK_VOCABULARY_2026-09-03.md:53-58). The reference is therefore strictly more
///    information about the same objective, and the fallback is strictly lossy. Measured on Iron
///    Storm: T04 names a task symbol, an axis of advance AND objective LANCASTER, and its embedded
///    Location carries 3 points - one graphic's worth.
/// 4. THE DROPPED HALF IS NEVER SILENT. The Location count and the separation between the two
///    answers are logged on every such task, and a separation past
///    <see cref="EmbeddedDisagreementMeters"/> is a WARNING: a few metres is one objective said
///    twice, kilometres is an order that disagrees with itself (m7).
/// 5. AN ID THAT DOES NOT RESOLVE IS ONE WARNING PER TASK naming every such id - not one line per
///    id. On the export as it arrived that difference is 1 line against 35.
/// </summary>
public static class TaskGeometryResolver
{
    /// <summary>The geometry a task will be dispatched on, and how it was derived.
    /// <paramref name="Log"/> is what happened; <paramref name="Warnings"/> is the subset the
    /// operator has to SEE (M5: a MapGraphicID that matched nothing is a data or scope gap between
    /// the order and the init, and it silently changes what the unit does - at Information it sat
    /// in a run log nobody reads until the unit had already held still for an hour).</summary>
    public sealed record Resolution(List<(double Lat, double Lon, double? Elev)> Points,
                                    GeometrySource Source, IReadOnlyList<string> Log,
                                    IReadOnlyList<string> Warnings);

    /// <summary>The line the embedded path always logs, so a log scan can count the tasks that
    /// are still running on the STP-801 export gap.</summary>
    public const string Stp801 = "STP-801";

    /// <summary>m7: how far the embedded Location may sit from the resolved MapGraphic geometry
    /// before the difference is a WARNING rather than a note. 1 km is well inside the scale of
    /// COA-STP1's areas (kilometres across, vertex-mean centroids 149-1,130 m from the true area
    /// centroid) and well outside authoring noise.</summary>
    public const double EmbeddedDisagreementMeters = 1000.0;

    /// <summary>Great-circle-ish metres between two authored points. Equirectangular, which is
    /// what a separation CHECK needs - it is compared against a 1 km threshold, not reported as a
    /// survey distance. PUBLIC since V4b: TaskGeometryInterpretation measures the same authored
    /// points with the same metric, and two copies of a distance function is how two answers start
    /// disagreeing.</summary>
    public static double DistMeters((double Lat, double Lon, double? Elev) a,
                                    (double Lat, double Lon, double? Elev) b)
    {
        const double MetersPerDegLat = 111320.0;
        double dLat = (a.Lat - b.Lat) * MetersPerDegLat;
        double dLon = (a.Lon - b.Lon) * MetersPerDegLat * Math.Cos(a.Lat * Math.PI / 180.0);
        return Math.Sqrt(dLat * dLat + dLon * dLon);
    }

    /// <summary>
    /// The geometry this task will be dispatched on. PRECEDENCE: MapGraphicID(s) that resolve to
    /// objects created at init, else the task's embedded Location, else nothing. PURE - it reads
    /// the task and a map of graphics and returns points plus the lines the caller should log; it
    /// decides nothing about what the points MEAN (no verb-typed interpretation - V4b).
    /// </summary>
    /// <param name="graphics">C2SIM uuid -> the graphic published under that uuid, by the
    /// INITIALIZATION or by the ORDER ITSELF (2026-09-20 - the schema allows either, xsd:2960-2977,
    /// and the real STP export uses the second exclusively).</param>
    /// <param name="taskeePos">The performing unit's own position (its AUTHORED init coordinate -
    /// the same one Vrf:DropOriginVertexMeters is measured against, and the one STP writes as a
    /// task's leading vertex). Used ONLY by the SF9 assembly rule, to recognise a graphic whose
    /// first vertex IS the unit's start. Null = no origin is known and none is dropped.</param>
    public static Resolution Resolve(OrderTask task, IReadOnlyDictionary<string, TaskGraphic> graphics,
                                     (double Lat, double Lon)? taskeePos = null)
    {
        var log = new List<string>();
        var warn = new List<string>();
        var ids = task?.MapGraphicUuids ?? Array.Empty<string>();
        var unmatched = new List<string>();
        var resolved = new List<TaskGraphic>();

        foreach (var id in ids)
        {
            if (graphics != null && graphics.TryGetValue(id, out var g) && g.Points is { Count: > 0 })
                resolved.Add(g);
            else unmatched.Add(id);
        }

        var points = AssembleRoute(resolved, taskeePos, log, warn);

        if (points.Count > 0)
        {
            if (unmatched.Count > 0)
                warn.Add($"{unmatched.Count} MapGraphicID(s) matched NO registered graphic and were ignored " +
                         $"[{string.Join(", ", unmatched)}] - the other graphic(s) on this task resolved, so " +
                         "the task still has geometry, but it is missing whatever those ids named");
            // m7 (cold-start review of 5c67d41): the embedded Location is DISCARDED here, and the
            // user's R1 note asked for "precedence MapGraphicID > embedded, consistency check when
            // both". Say what was dropped and how far apart the two answers were: a few metres is
            // the same objective expressed twice, kilometres is an order that disagrees with its
            // own initialization, and the log is the only place that difference can surface.
            var dropped = task?.Points;
            if (dropped is { Count: > 0 })
            {
                // m7's QUESTION is "do the order's two answers disagree about WHERE THIS TASK IS",
                // and since SF9 that has to be asked of the DESTINATIONS. It used to compare the
                // two FIRST points, which was a fair comparison while the resolved route began
                // wherever the first named graphic began. It is not one now: the SF9 assembly drops
                // the vertices that are the taskee's own position and puts the objective LAST,
                // while STP's embedded Location is the first tactical graphic linearised - usually
                // starting AT the unit. First-against-first therefore compares a start with an
                // objective and fired on 13 of this export's tasks instead of the 7 that really
                // disagree. Last-against-last compares the two answers to the same question.
                double sepStart = DistMeters(points[0], dropped[0]);
                double sep = DistMeters(points[^1], dropped[^1]);
                log.Add($"{dropped.Count} embedded Location point(s) ignored (MapGraphicID wins) - the two " +
                        $"answers' DESTINATIONS are {sep:F0} m apart (their first points {sepStart:F0} m)");
                if (sep > EmbeddedDisagreementMeters)
                    warn.Add($"the MapGraphicID geometry and the embedded Location on this task END {sep:F0} m " +
                             $"apart (more than {EmbeddedDisagreementMeters:F0} m): the order and the " +
                             "initialization disagree about where this task is, and the MapGraphicID was used");
            }
            return new Resolution(points, GeometrySource.MapGraphic, log, warn);
        }

        // Nothing resolved: the embedded Location. This is NOT a workaround - it is valid C2SIM
        // and the user ruled it stays supported alongside MapGraphicID - but it is also how the
        // STP-801 export gap shows up, so the line names it either way.
        if (unmatched.Count > 0)
            warn.Add($"{unmatched.Count} MapGraphicID(s) matched NO registered graphic " +
                     $"[{string.Join(", ", unmatched)}] - nothing in the initialization OR in this order " +
                     "publishes a graphic under that uuid, so the id is DANGLING and the geometry it named " +
                     "is lost");
        points.AddRange(task?.Points ?? new List<(double, double, double?)>());
        if (points.Count == 0)
        {
            if (unmatched.Count > 0)
                warn.Add($"this task's ONLY geometry was {unmatched.Count} unmatched MapGraphicID(s) and it " +
                         "carries no embedded Location - it will be executed IN PLACE (R2), which is NOT what " +
                         "the order asked for");
            return new Resolution(points, GeometrySource.None, log, warn);
        }
        log.Add(unmatched.Count == 0
            ? $"geometry from embedded Location (no MapGraphicID - {Stp801})"
            : $"geometry from embedded Location ({unmatched.Count} MapGraphicID(s) matched nothing - {Stp801})");
        return new Resolution(points, GeometrySource.EmbeddedLocation, log, warn);
    }

    /// <summary>How near two authored vertices must be to count as THE SAME PLACE - for dropping a
    /// graphic's leading vertex that is the taskee's own start, for recognising a destination the
    /// route already ends at, and for the doubles-back guard. 100 m is Vrf:DropOriginVertexMeters'
    /// shipped default, measured against the same question (PREREG_ASSEMBLY_LAYOUT_2026-09-07 sec
    /// 3f: "a task's leading route point that coincides with the unit's AUTHORED initialization
    /// position ... is dropped").</summary>
    public const double OriginCoincidenceMeters = 100.0;

    /// <summary>How far the start of the next LINE graphic may sit from the end of the route so far
    /// and still be read as a CONTINUATION of one path. Beyond it the graphics are not one path and
    /// the far one is not chained - it is reported instead. 5 km: an order's control measures for
    /// ONE task are drawn on one objective (Iron Storm's own lines join within 3 km where they join
    /// at all), while the disagreements this rule exists to stop are 17-105 km
    /// (stp_export_intake_review.md sec 1c).</summary>
    public const double ChainGapMeters = 5000.0;

    /// <summary>
    /// SF9 - HOW SEVERAL MapGraphicIDs ON ONE TASK BECOME ONE ROUTE.
    ///
    /// ============================ THE DEFECT THIS REPLACES ============================
    /// Every resolving graphic's vertices were appended in MapGraphicID DOCUMENT ORDER, whatever
    /// kind of graphic it was. On the real STP export that produces routes that DOUBLE BACK THROUGH
    /// THE TASKEE'S OWN START: Iron Storm T12 drove
    ///   54.280,23.320 -> 54.402,24.043 -> 54.390,23.969 -> 54.280,23.320 -> 54.371,23.994
    ///   -> 54.402,24.043
    /// - out 48.8 km, back to where it started, out again: 147.2 km for a ~50 km advance, on one of
    /// only two tasks the export dispatches at all. The same shape on T01/T03/T04/T08/T13/T15/T16/
    /// T23. The cause is that the task names a mission task SYMBOL, an AXIS of advance whose first
    /// vertex IS the unit's position, AND an objective AREA, and all three were spliced end to end.
    ///
    /// ============================ WHAT THE SCHEMA ACTUALLY SAYS ============================
    /// `MapGraphicID` is `minOccurs="0" maxOccurs="unbounded"` on the ActionGroup sequence
    /// (C2SIM_SMX_LOX_CWIX2024.xsd:1384-1396) and the TaskType/TaskGroup annotation reads "WHERE is
    /// represented by hasLocation and/or hasMapGraphicID reference" (xsd:3787, :3808). So a task MAY
    /// name several graphics, and what they jointly describe is WHERE THE TASK IS. The schema
    /// carries no xs:key, no xs:keyref, no ordering annotation and nothing that makes the list a
    /// waypoint sequence. CONCATENATION IN DOCUMENT ORDER WAS AN APPLICATION INVENTION WITH NO
    /// SOURCE - which is precisely what the record forbids ("You must have pointers to sources for
    /// every decision", PREREG_ASSEMBLY_LAYOUT 3g, where a per-vertex task split was withdrawn on
    /// exactly this ground).
    ///
    /// ============================ WHAT THE KINDS MEAN (already settled) ============================
    /// The graphic's own KIND is the information the embedded-Location fallback throws away, and
    /// this file already states what each kind is:
    ///   AREA  - "an area is a place to go to, NOT A PATH TO FOLLOW" (TaskGraphic.KindArea); it
    ///           already reduces to one reference point, its centroid.
    ///   LINE  - "a phase line, an axis of advance, a boundary, a breach lane. Its vertices are
    ///           contributed IN ORDER - the line IS a path" (TaskGraphic.KindLine).
    ///   POINT - "ONE vertex, its authored position" (TaskGraphic.KindPoint).
    /// A mission task SYMBOL with two or more anchors is classified as a line by OrderParser /
    /// InitParser, and is read as a path, because V4b settled that reading for the same coordinates
    /// when STP linearises them (docs/experiments/DESIGN_V4B_EMBEDDED_LOCATION_2026-09-14.md
    /// :141-151). That ruling is NOT reopened here.
    ///
    /// ============================ THE RULE ============================
    ///  1. LINES SUPPLY THE PATH. Points and areas supply the DESTINATION - never an intermediate
    ///     waypoint, because that is what forces a route back through somewhere it has been.
    ///  2. ANY vertex of a line within OriginCoincidenceMeters of the taskee's own position is
    ///     DROPPED - leading OR interior. The unit is already there. On this export one axis
    ///     carries the taskee's position as its THIRD of four vertices, which is by itself a 45 km
    ///     out-and-back. (The same reading Vrf:DropOriginVertexMeters applies to a route's leading
    ///     vertex; here it applies per graphic, to every vertex, before they are joined.)
    ///  3. LINES ARE CHAINED BY CONTINUITY, NEVER BY DOCUMENT ORDER. The first line is the one
    ///     whose nearer end is closest to the taskee; it is reversed if its far end is the nearer.
    ///     Each next line is the unused one whose nearer end is closest to the current route END,
    ///     reversed on the same test.
    ///  4. A line whose nearer end is more than ChainGapMeters from the route end is NOT one path
    ///     with it. It is dropped from the route and named in ONE warning - the interface does not
    ///     invent a leg to reach it.
    ///  5. A line that adds no ground (every vertex already within OriginCoincidenceMeters of the
    ///     route) is dropped silently: two graphics drawing one advance are one advance.
    ///  6. DESTINATIONS are appended after the path, in document order, skipping any that the route
    ///     already ends at. Several UNRELATED destinations and no line means the task claims to be
    ///     in two places: the FIRST is used and ONE warning names them all.
    ///  7. BACKSTOP: if the assembled route still returns within OriginCoincidenceMeters of the
    ///     taskee after leaving it, it is TRUNCATED at the return and warned. A route the interface
    ///     cannot explain is not one it drives.
    /// Everything the rule did is REPORTED PER GRAPHIC (which id supplied path or destination, and
    /// what was dropped), because a route assembled by a rule nobody can see is the same defect in
    /// a different place.
    ///
    /// PURE. A single graphic on a task takes exactly the path it took before (one line = its own
    /// vertices with at most a coincident origin dropped; one area = its centroid; one point = its
    /// vertex), so every order in data/ other than the Iron Storm export - all of which carry ZERO
    /// MapGraphicIDs - is bit-for-bit unaffected.
    /// </summary>
    private static List<(double Lat, double Lon, double? Elev)> AssembleRoute(
        IReadOnlyList<TaskGraphic> resolved, (double Lat, double Lon)? taskeePos,
        List<string> log, List<string> warn)
    {
        var route = new List<(double Lat, double Lon, double? Elev)>();
        if (resolved == null || resolved.Count == 0) return route;

        // 1. Partition by kind, and reduce each to what it contributes.
        var lines = new List<(TaskGraphic G, List<(double Lat, double Lon, double? Elev)> Pts)>();
        var destinations = new List<(TaskGraphic G, (double Lat, double Lon, double? Elev) Pt, string What)>();
        foreach (var g in resolved)
        {
            if (string.Equals(g.Kind, TaskGraphic.KindArea, StringComparison.OrdinalIgnoreCase))
            {
                var c = Centroid(g.Points);
                destinations.Add((g, c, $"area, {g.Points.Count} vertices, centroid {c.Lat:F6},{c.Lon:F6}"));
            }
            else if (g.Points.Count == 1
                     || string.Equals(g.Kind, TaskGraphic.KindPoint, StringComparison.OrdinalIgnoreCase))
            {
                destinations.Add((g, g.Points[0], $"{g.Kind}, 1 vertex"));
            }
            else
            {
                var pts = new List<(double Lat, double Lon, double? Elev)>(g.Points);
                // 2. Drop EVERY vertex that IS the taskee's own start - leading or interior.
                //
                //    MEASURED, and the reason this clause is not just about the leading vertex:
                //    Iron Storm's axis GroundAttackAxi_116_ABCT_SLOT2 (T12) is authored
                //      (54.40219,24.04281) (54.38968,23.96870) (54.28,23.32) (54.37061,23.99426)
                //    and 54.28,23.32 IS 116 ABCT's own position - the THIRD of four vertices. So
                //    the 147 km zig-zag is not only graphics spliced end to end; ONE graphic sends
                //    the unit 45 km out, back to where it started, and out again. A vertex that is
                //    the unit's own position is not a place to drive to, wherever it sits in the
                //    list - which is the reading Vrf:DropOriginVertexMeters already applies to a
                //    route's leading vertex (PREREG_ASSEMBLY_LAYOUT_2026-09-07 sec 3f, where the
                //    same coordinate appears because "the order's FIRST route vertex is the STP
                //    assembly point itself"). SF9 extends it to interior vertices, on the same
                //    grounds and with a line per task saying it happened.
                int dropped = 0;
                if (taskeePos is { } tp)
                {
                    var origin = (tp.Lat, tp.Lon, (double?)null);
                    for (int i = pts.Count - 1; i >= 0 && pts.Count > 1; i--)
                        if (DistMeters(pts[i], origin) <= OriginCoincidenceMeters) { pts.RemoveAt(i); dropped++; }
                }
                if (dropped > 0)
                    log.Add($"MapGraphicID {g.Uuid} -> {g.Name} ({g.Kind}): {dropped} vertex(es) dropped - " +
                            "they ARE the taskee's own position, so driving to them is a return to where the " +
                            "unit already is (SF9; the reading Vrf:DropOriginVertexMeters applies to a " +
                            "route's leading vertex, extended to interior ones)");
                lines.Add((g, pts));
            }
        }

        // 3/4/5. Chain the lines by continuity from the taskee outwards.
        var unused = lines.ToList();
        var unchained = new List<TaskGraphic>();
        var anchor = taskeePos is { } t0 ? (t0.Lat, t0.Lon, (double?)null) : (double.NaN, double.NaN, (double?)null);
        bool haveAnchor = taskeePos.HasValue;
        while (unused.Count > 0)
        {
            var from = route.Count > 0 ? route[^1] : anchor;
            int best = 0; bool bestReverse = false; double bestD = double.MaxValue;
            for (int i = 0; i < unused.Count; i++)
            {
                var p = unused[i].Pts;
                double dHead = haveAnchor || route.Count > 0 ? DistMeters(from, p[0]) : 0.0;
                double dTail = haveAnchor || route.Count > 0 ? DistMeters(from, p[^1]) : double.MaxValue;
                double d = Math.Min(dHead, dTail);
                if (d < bestD) { bestD = d; best = i; bestReverse = dTail < dHead; }
            }
            var chosen = unused[best];
            unused.RemoveAt(best);
            var take = chosen.Pts;
            if (bestReverse) { take = take.ToList(); take.Reverse(); }

            if (route.Count > 0 && bestD > ChainGapMeters)
            {
                unchained.Add(chosen.G);
                continue;
            }
            int added = 0;
            foreach (var p in take)
            {
                // 5. A vertex the route already covers adds no ground.
                if (route.Count > 0 && DistMeters(route[^1], p) <= OriginCoincidenceMeters) continue;
                route.Add(p);
                added++;
            }
            log.Add($"path from MapGraphicID {chosen.G.Uuid} -> {chosen.G.Name} ({chosen.G.Kind}, " +
                    $"{chosen.Pts.Count} vertices{(bestReverse ? ", REVERSED for continuity" : "")}): " +
                    $"{added} vertex(es) joined" +
                    (route.Count > added ? $" {bestD:F0} m after the previous graphic's end" : ""));
        }
        if (unchained.Count > 0)
            warn.Add($"{unchained.Count} MapGraphicID(s) on this task are NOT continuous with the route its " +
                     $"other graphic(s) describe (their nearest end is more than {ChainGapMeters:F0} m from " +
                     $"it) and were NOT chained into it: " +
                     string.Join("; ", unchained.Select(g => $"{g.Uuid} '{g.Name}' ({g.Kind})")) +
                     " - the interface does not invent a leg to reach a graphic the order did not join " +
                     "up, and appending them in document order is what made routes double back (SF9)");

        // 6. ONE destination, last, never a waypoint. "The task is at THIS area / THIS point" does
        //    not become "drive to each of them in turn" - that is the shape that doubles back.
        var extraDest = new List<TaskGraphic>();
        bool destTaken = false;
        foreach (var d in destinations)
        {
            if (route.Count > 0 && DistMeters(route[^1], d.Pt) <= OriginCoincidenceMeters)
            {
                destTaken = true;   // the path already ends at this objective
                log.Add($"destination from MapGraphicID {d.G.Uuid} -> {d.G.Name} ({d.What}): the route already " +
                        "ends there, not repeated (SF9)");
                continue;
            }
            if (destTaken) { extraDest.Add(d.G); continue; }
            route.Add(d.Pt);
            destTaken = true;
            log.Add($"destination from MapGraphicID {d.G.Uuid} -> {d.G.Name} ({d.What}): appended as the route's " +
                    "DESTINATION, never as an intermediate waypoint (SF9)");
        }
        if (extraDest.Count > 0)
            warn.Add($"this task names {extraDest.Count + 1} destination graphic(s) (areas/points). A task is in " +
                     "ONE place: the first is used as the route's destination and the rest are IGNORED - " +
                     string.Join("; ", extraDest.Select(g => $"{g.Uuid} '{g.Name}' ({g.Kind})")) +
                     " (SF9; driving to each in turn is a route the order never described)");

        // 7. Backstop: a route must not come back through the unit's own start.
        if (taskeePos is { } tp2)
        {
            var origin = (tp2.Lat, tp2.Lon, (double?)null);
            bool left = false;
            for (int i = 0; i < route.Count; i++)
            {
                double d = DistMeters(route[i], origin);
                if (!left) { if (d > OriginCoincidenceMeters) left = true; continue; }
                if (d <= OriginCoincidenceMeters)
                {
                    warn.Add($"the route assembled from this task's MapGraphicID(s) RETURNS to the taskee's own " +
                             $"start at vertex {i + 1} of {route.Count} after leaving it - it is TRUNCATED there. " +
                             "A route that doubles back through its origin is not something the order asked for " +
                             "and is not something this interface drives (SF9 backstop).");
                    route.RemoveRange(i, route.Count - i);
                    break;
                }
            }
        }
        return route;
    }

    /// <summary>The centroid of a graphic's vertices - the plain arithmetic mean, which is what an
    /// "go to this area" task needs and what the areas on disk (1-19 vertices, all within a few km)
    /// make meaningful. No spherical correction: at COA-STP1's extent the difference is centimetres
    /// against areas kilometres across.
    ///
    /// PUBLIC since V4b, and for one reason: an order that NAMES its objective (MapGraphicID) and an
    /// order that EMBEDS it must arrive at the same point, so both paths call this one function.
    /// It carries the same recorded debt for both - C12 (pass-2 review, TASK_VOCABULARY_ASSESSMENT
    /// sec 7.1b): the vertex arithmetic mean is NOT the polygon centroid, and on the init's 12
    /// multi-vertex areas it sits 149-1,130 m from the true centroid against a 500 m arrival radius.
    /// V4b does not fix C12; it makes sure the fix will only have to land once.</summary>
    public static (double Lat, double Lon, double? Elev) Centroid(
        IReadOnlyList<(double Lat, double Lon, double? Elev)> pts)
    {
        double lat = 0, lon = 0;
        foreach (var p in pts) { lat += p.Lat; lon += p.Lon; }
        return (lat / pts.Count, lon / pts.Count, null);
    }
}
