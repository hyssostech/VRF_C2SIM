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

    /// <summary>
    /// The geometry this task will be dispatched on. PRECEDENCE: MapGraphicID(s) that resolve to
    /// objects created at init, else the task's embedded Location, else nothing. PURE - it reads
    /// the task and a map of graphics and returns points plus the lines the caller should log; it
    /// decides nothing about what the points MEAN (no verb-typed interpretation - V4b).
    /// </summary>
    /// <param name="graphics">C2SIM uuid -> the graphic created at init under that uuid.</param>
    public static Resolution Resolve(OrderTask task, IReadOnlyDictionary<string, TaskGraphic> graphics)
    {
        var log = new List<string>();
        var warn = new List<string>();
        var points = new List<(double Lat, double Lon, double? Elev)>();
        var ids = task?.MapGraphicUuids ?? Array.Empty<string>();
        var unmatched = new List<string>();

        foreach (var id in ids)
        {
            if (graphics != null && graphics.TryGetValue(id, out var g) && g.Points is { Count: > 0 })
            {
                if (string.Equals(g.Kind, TaskGraphic.KindArea, StringComparison.OrdinalIgnoreCase))
                {
                    var c = Centroid(g.Points);
                    points.Add(c);
                    log.Add($"geometry from MapGraphicID {id} -> {g.Name} (area, {g.Points.Count} vertices, " +
                            $"centroid {c.Lat:F6},{c.Lon:F6})");
                }
                else
                {
                    points.AddRange(g.Points);
                    log.Add($"geometry from MapGraphicID {id} -> {g.Name} ({g.Kind}, {g.Points.Count} vertices)");
                }
            }
            else unmatched.Add(id);
        }

        if (points.Count > 0)
        {
            foreach (var id in unmatched)
                warn.Add($"MapGraphicID {id} matched NO graphic in the initialization - ignored (the other " +
                         "graphic(s) on this task resolved, so the task still has geometry)");
            return new Resolution(points, GeometrySource.MapGraphic, log, warn);
        }

        // Nothing resolved: the embedded Location. This is NOT a workaround - it is valid C2SIM
        // and the user ruled it stays supported alongside MapGraphicID - but it is also how the
        // STP-801 export gap shows up, so the line names it either way.
        foreach (var id in unmatched)
            warn.Add($"MapGraphicID {id} matched NO graphic in the initialization - the interface has no " +
                     "geometry under that uuid (an init/order mismatch, or a graphic type the init parser " +
                     "does not collect)");
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

    /// <summary>The centroid of a graphic's vertices - the plain arithmetic mean, which is what an
    /// "go to this area" task needs and what the areas on disk (1-19 vertices, all within a few km)
    /// make meaningful. No spherical correction: at COA-STP1's extent the difference is centimetres
    /// against areas kilometres across.</summary>
    private static (double Lat, double Lon, double? Elev) Centroid(
        IReadOnlyList<(double Lat, double Lon, double? Elev)> pts)
    {
        double lat = 0, lon = 0;
        foreach (var p in pts) { lat += p.Lat; lon += p.Lon; }
        return (lat / pts.Count, lon / pts.Count, null);
    }
}
