using System.Collections.Concurrent;

namespace VrfC2SimApp;

/// <summary>What a task's points MEAN (V4b). Not where they came from - that is
/// <see cref="GeometrySource"/> - but what shape they describe.</summary>
public enum GeometryKind
{
    /// <summary>No points at all. Not a reading: R2 executes the task at the performing unit's
    /// own position (<see cref="TaskDispatchPolicy.ForZeroGeometry"/>).</summary>
    None,
    /// <summary>ONE place. One point, or several points that sit on top of each other (COA-STP1's
    /// T2 and T3 export the same coordinate twice).</summary>
    Point,
    /// <summary>A path to drive, in order. The reading every verb got before V4b.</summary>
    Route,
    /// <summary>A ring: the task is about the AREA it encloses, so the move goes to its centroid
    /// and the ring itself becomes a VR-Forces control area.</summary>
    ObjectiveArea,
}

/// <summary>
/// V4b - WHAT THE EMBEDDED Location MEANS, PER VERB.
///
/// THE PROBLEM, from the schema itself. `LocationType` (C2SIM_SMX_LOX_CWIX2024.cs:3610-3625) is a
/// choice of `GeodeticCoordinate` or `RelativeLocation`, and `GeodeticCoordinateType` (:3634) is
/// latitude, longitude and two optional altitudes. There is no shape type, no closure flag and no
/// radius anywhere in the binding. So a polygon, an axis of advance and a single control point
/// reach this interface as the SAME XML - one list of points - and until V4b every one of them was
/// driven as a route, whatever the verb said.
///
/// THE RULE (docs/experiments/DESIGN_V4B_EMBEDDED_LOCATION_2026-09-14.md sec 3), verb-typed AND
/// shape-typed, because neither alone is enough:
///
///   n == 0                                          -> None          (R2 executes in place)
///   every point within PointCoincidenceMeters       -> Point
///   first == last, >= 3 distinct vertices           -> ObjectiveArea (ANY verb)
///   area verb, >= 3 distinct vertices, ClosureRatio  &lt;= RingClosureRatio -> ObjectiveArea
///   otherwise                                       -> Route
///
/// WHY THE SHAPE TEST IS NOT OPTIONAL - the measurement that forced it. COA-STP1's T32 is a SEIZE
/// (an area verb) carrying FOUR points, and those four points are an axis of advance 23.6 km long:
/// legs of 7,093 + 11,791 + 4,743 m, with the last point 23,579 m from the first, i.e. a closure
/// ratio of 0.998. A verb-only rule ("an area verb with 3+ points means an area") would collapse
/// that axis to a centroid and the battalion would stop driving it. Conversely the init's twelve
/// multi-vertex tactical areas all CLOSE their ring explicitly (first vertex repeated as last, 0 m,
/// ratio 0.000, on perimeters of 11.8-46.5 km), so the closed-ring test is calibrated on the real
/// authored data rather than guessed. The two 3-point tasks (T13 BREACH, T36 CLRLND) sit at 0.43
/// and are task-mission SYMBOLS - they match init TaskGraphics __FRIEN_16 and __FRIEN_13 vertex for
/// vertex - so they are driven as routes and NAMED as symbols in the log (see
/// <see cref="Reading.Note"/>), not silently turned into areas.
///
/// PURE. No bridge, no clock, no logging: it returns the points the dispatch should use plus the
/// lines the caller should log, exactly like <see cref="TaskGeometryResolver"/>, so the whole rule
/// is decidable offline (`--rulings-selftest`, section V4b) and printable per order
/// (`--parse-order`, the V4b shape census).
/// </summary>
public static class TaskGeometryInterpretation
{
    /// <summary>How close two authored points have to be to count as the SAME place - both for
    /// "all of these points are one point" and for "this ring is closed". 10 m is far below the
    /// scale of any authored feature in the data (the smallest task figure in COA-STP1 spans 360 m)
    /// and far above the 1e-6 deg (about 0.1 m) precision the order is written with. COA-STP1's T2
    /// and T3 export the identical coordinate twice - 0 m apart - which is what this collapses.</summary>
    public const double PointCoincidenceMeters = 10.0;

    /// <summary>The ring test for an UNCLOSED but returning figure: (distance from the last point
    /// back to the first) / (total path length). Zero for an explicitly closed ring, at most 1 by
    /// the triangle inequality, and about 1 for a straight run of legs. Measured on the data:
    /// 0.000 on the init's 12 closed areas, 0.43 on the two 3-point task symbols, 0.998 on the nine
    /// COA-STP1 axes of advance. 0.75 sits in the empty middle of that distribution.</summary>
    public const double RingClosureRatio = 0.75;

    /// <summary>A figure whose whole extent is smaller than this, under a verb that is not an area
    /// verb, is almost certainly a task-mission SYMBOL rather than a path: FM 3-90 B-8 (breach:
    /// "the area located between the arms of the graphic shows the general location for the
    /// breach") and B-17 (clear: "the bar connecting the arrows designates the desired limit of
    /// advance"). It is still driven as a route - nothing here invents a vendor task - but the
    /// caller is told, so V5/V6 bind it to the vendor parameter it really is instead of
    /// re-deriving this from the XML later.</summary>
    public const double SymbolSpanMeters = 1000.0;

    /// <summary>The prefix of the line every embedded-Location reading logs. Held here so the
    /// self-test and a log scan agree on it verbatim.</summary>
    public const string EmbeddedReadAs = "embedded Location read as";

    /// <summary>What the dispatch should do with a task's geometry.</summary>
    /// <param name="Kind">The shape the points describe.</param>
    /// <param name="Points">The points to drive: unchanged for a Route, the one place for a Point,
    /// and the ring's CENTROID (one point) for an ObjectiveArea.</param>
    /// <param name="AreaVertices">The ring, with a repeated closing vertex dropped - empty unless
    /// <paramref name="Kind"/> is ObjectiveArea. This is what the control area is created from.</param>
    /// <param name="CreateObjectiveArea">True only when the ring came from the EMBEDDED Location:
    /// a MapGraphicID names a graphic the init already created under that same uuid, and a second
    /// object would duplicate it.</param>
    /// <param name="Log">The one line that says how the points were read (never null).</param>
    /// <param name="Note">A second line when the shape and the verb disagree, or when the figure
    /// looks like a task symbol; null when there is nothing to say.</param>
    public sealed record Reading(GeometryKind Kind,
                                 List<(double Lat, double Lon, double? Elev)> Points,
                                 IReadOnlyList<(double Lat, double Lon, double? Elev)> AreaVertices,
                                 bool CreateObjectiveArea, string Log, string Note);

    /// <summary>
    /// Read a task's resolved geometry. <paramref name="source"/> decides whether the reading
    /// APPLIES at all: V4b interprets the EMBEDDED Location, because that is the one path where
    /// C2SIM gives a bare point list with no type on it. Geometry that came from a MapGraphicID was
    /// already typed by the graphic it names (an area was collapsed to its centroid by
    /// <see cref="TaskGeometryResolver"/>, a line contributed its vertices), so it passes through
    /// untouched - and nothing is created for it.
    /// </summary>
    public static Reading Interpret(string actionCode,
                                    IReadOnlyList<(double Lat, double Lon, double? Elev)> points,
                                    GeometrySource source)
    {
        var pts = points ?? Array.Empty<(double, double, double?)>();
        string verb = (actionCode ?? "").Trim().ToUpperInvariant();
        if (verb.Length == 0) verb = "(none)";

        // NO GEOMETRY AT ALL is not a reading of anything, and it must not be described as one.
        // (Caught reviewing this change: the MapGraphic passthrough below used to swallow this case
        // and log "geometry from MapGraphicID ..." for the nine COA-STP1 tasks that carry NEITHER -
        // a sentence about a uuid that is not there.)
        if (source == GeometrySource.None || pts.Count == 0)
            return new Reading(GeometryKind.None, new List<(double, double, double?)>(),
                               Array.Empty<(double, double, double?)>(), false,
                               $"no task geometry to read (0 points, verb {verb}) - R2 executes it at the " +
                               "performing unit's own position",
                               null);

        if (source != GeometrySource.EmbeddedLocation)
        {
            var kind = pts.Count == 1 ? GeometryKind.Point : GeometryKind.Route;
            return new Reading(kind, new List<(double, double, double?)>(pts),
                               Array.Empty<(double, double, double?)>(), false,
                               $"geometry from MapGraphicID used as the graphic authored it " +
                               $"({pts.Count} point(s), verb {verb}) - the graphic's own kind decided it, so no " +
                               "embedded-Location interpretation applies (V4b)",
                               null);
        }

        var shape = Classify(verb, pts);

        if (shape == GeometryKind.Point)
        {
            var one = new List<(double Lat, double Lon, double? Elev)> { pts[0] };
            string extra = pts.Count > 1
                ? $" - {pts.Count} points within {PointCoincidenceMeters:F0} m of each other, so this is ONE place " +
                  "and the duplicates are dropped"
                : " - one place to go to";
            return new Reading(shape, one, Array.Empty<(double, double, double?)>(), false,
                               $"{EmbeddedReadAs} {shape} ({pts.Count} points, verb {verb}){extra}", null);
        }

        if (shape == GeometryKind.ObjectiveArea)
        {
            var ring = RingVertices(pts);
            var centroid = TaskGeometryResolver.Centroid(ring);
            string how = IsExplicitlyClosed(pts)
                ? "a CLOSED ring (first vertex repeated as last)"
                : $"an area verb on a returning figure (closure ratio {ClosureRatio(pts):F2} <= {RingClosureRatio:F2})";
            return new Reading(shape, new List<(double Lat, double Lon, double? Elev)> { centroid },
                               ring, true,
                               $"{EmbeddedReadAs} {shape} ({pts.Count} points, verb {verb}) - {how}; the move goes " +
                               $"to its centroid {centroid.Lat:F6},{centroid.Lon:F6} and the {ring.Count} ring " +
                               "vertices are created as a VR-Forces control area under the task's own uuid",
                               null);
        }

        // Route: the points are driven in order, exactly as before V4b. Say so, and say when the
        // figure does not look like a path.
        string note = null;
        double span = Spread(pts);
        if (pts.Count >= 3 && span <= SymbolSpanMeters && !IsAreaVerb(verb))
            note = $"the {pts.Count} points span only {span:F0} m - that is a task-mission SYMBOL (FM 3-90 B-8 " +
                   "breach arms / B-17 clear limit-of-advance bar), not a path. It is driven as a route because " +
                   "no vendor tactical task is wired yet (V5/V6); the symbol's anchor points are what those items " +
                   "will bind";
        else if (pts.Count >= 3 && IsAreaVerb(verb) && RingVertices(pts).Count < 3)
            note = $"verb {verb} is an AREA verb and its {pts.Count} points close, but they are only " +
                   $"{RingVertices(pts).Count} distinct corners - two corners are a line, not a polygon, so it is " +
                   "driven as a route";
        else if (pts.Count >= 3 && IsAreaVerb(verb))
            note = $"verb {verb} is an AREA verb but its {pts.Count} points do not close (closure ratio " +
                   $"{ClosureRatio(pts):F2} > {RingClosureRatio:F2}, span {span:F0} m) - the export gave an axis, " +
                   "not an objective (STP-801: the first task graphic, linearised), so it is driven as a route";
        return new Reading(shape, new List<(double, double, double?)>(pts),
                           Array.Empty<(double, double, double?)>(), false,
                           $"{EmbeddedReadAs} {shape} ({pts.Count} points, verb {verb})", note);
    }

    /// <summary>The shape rule itself, with nothing else attached (sec 3 of the design note).</summary>
    public static GeometryKind Classify(string actionCode,
                                        IReadOnlyList<(double Lat, double Lon, double? Elev)> points)
    {
        var pts = points ?? Array.Empty<(double, double, double?)>();
        if (pts.Count == 0) return GeometryKind.None;
        if (Spread(pts) <= PointCoincidenceMeters) return GeometryKind.Point;
        // A ring needs three DISTINCT corners: a "closed" three-point list is one leg out and back,
        // which is a line, and createControlArea cannot make a polygon of two vertices.
        if (RingVertices(pts).Count >= 3)
        {
            if (IsExplicitlyClosed(pts)) return GeometryKind.ObjectiveArea;
            if (IsAreaVerb(actionCode) && ClosureRatio(pts) <= RingClosureRatio) return GeometryKind.ObjectiveArea;
        }
        return GeometryKind.Route;
    }

    /// <summary>The verbs whose OBJECT is an area, taken from the ONE verb table the dispatch
    /// already uses (<see cref="VerbMapping"/>): SECURE, OCCUPY, SEIZE, RETAIN, BLOCK, DEFEND,
    /// GUARD. Deliberately not a second table - a verb must not be able to mean one thing to the
    /// classifier and another to the dispatch.</summary>
    public static bool IsAreaVerb(string actionCode)
        => VerbMapping.Classify(actionCode).Intent == TaskIntent.HoldObjective;

    /// <summary>First vertex repeated as the last one - how the init's 12 multi-vertex tactical
    /// areas are authored (measured: 0 m, all 12).</summary>
    public static bool IsExplicitlyClosed(IReadOnlyList<(double Lat, double Lon, double? Elev)> pts)
        => pts != null && pts.Count >= 4
           && TaskGeometryResolver.DistMeters(pts[0], pts[^1]) <= PointCoincidenceMeters;

    /// <summary>The ring's corners: the points with a repeated closing vertex dropped, so the
    /// centroid does not weight the first corner twice.</summary>
    public static IReadOnlyList<(double Lat, double Lon, double? Elev)> RingVertices(
        IReadOnlyList<(double Lat, double Lon, double? Elev)> pts)
    {
        if (pts == null || pts.Count == 0) return Array.Empty<(double, double, double?)>();
        bool closed = pts.Count >= 2 && TaskGeometryResolver.DistMeters(pts[0], pts[^1]) <= PointCoincidenceMeters;
        return closed ? pts.Take(pts.Count - 1).ToList() : pts;
    }

    /// <summary>(distance from the last point back to the first) / (total path length). 1.0 for a
    /// straight run; 0 for a closed ring. Returns 1.0 for a degenerate (zero-length) path so a
    /// nonsense figure can never be read as an area.</summary>
    public static double ClosureRatio(IReadOnlyList<(double Lat, double Lon, double? Elev)> pts)
    {
        if (pts == null || pts.Count < 2) return 1.0;
        double path = 0.0;
        for (int i = 1; i < pts.Count; i++) path += TaskGeometryResolver.DistMeters(pts[i - 1], pts[i]);
        if (path <= 0.0) return 1.0;
        return TaskGeometryResolver.DistMeters(pts[0], pts[^1]) / path;
    }

    /// <summary>The figure's extent: the greatest distance from the first point to any other.</summary>
    public static double Spread(IReadOnlyList<(double Lat, double Lon, double? Elev)> pts)
    {
        double max = 0.0;
        if (pts == null) return max;
        foreach (var p in pts) max = Math.Max(max, TaskGeometryResolver.DistMeters(pts[0], p));
        return max;
    }

    /// <summary>The VR-Forces name of the control area an ObjectiveArea task creates - the same
    /// convention the route object already uses ("&lt;TaskName&gt; ROUTE").</summary>
    public static string ObjectiveAreaName(string taskName) => (taskName ?? "").Trim() + " OBJECTIVE";

    /// <summary>The once-per-task key. Same dictionary and same shape as the init's duplicate
    /// guard ("area:&lt;uuid|name&gt;"), with its own prefix so a task and an init graphic can
    /// never collide.</summary>
    public static string ObjectiveAreaKey(string taskUuid, string taskName)
        => "taskarea:" + (string.IsNullOrEmpty(taskUuid) ? ObjectiveAreaName(taskName) : taskUuid);

    /// <summary>
    /// THE ONCE-PER-TASK DECISION, as a function so it is checkable offline. `ExecuteTaskOnTick` is
    /// re-entered for the SAME task by the TerrainProfile reply (the default ground path), and an
    /// order can be delivered twice; both would otherwise create the objective area again.
    /// </summary>
    public static bool ShouldCreateObjectiveArea(ConcurrentDictionary<string, byte> created,
                                                 string taskUuid, string taskName)
        => created != null && created.TryAdd(ObjectiveAreaKey(taskUuid, taskName), 0);
}
