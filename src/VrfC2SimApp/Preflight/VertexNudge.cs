namespace VrfC2SimApp.Preflight;

/// <summary>The nudge search's two numbers (RL-20260927-01).</summary>
public sealed record VertexNudgeOptions
{
    /// <summary>Vrf:PreflightVertexNudgeMaxMeters (300). Beyond it a moved vertex is no longer the
    /// place STP named, so the vertex is KEPT and reported for STP authoring instead. 0 = never move
    /// a vertex: the check still runs and still reports.</summary>
    public double MaxMeters { get; init; } = 300.0;

    /// <summary>Ring spacing, and the arc spacing along a ring: 25 m, the vendor's own formation slot
    /// spacing - the same granularity the lateral route shift searches at.</summary>
    public double StepMeters { get; init; } = 25.0;
}

/// <summary>What one point is, for the nudge: known or not, and which rule it breaks.</summary>
public sealed record NudgeVerdict(bool Known, bool Water, bool Building, bool Slope, string Note)
{
    public bool Bad => Water || Building || Slope;
    public bool Clear => Known && !Bad;
    /// <summary>Water was found but a road bridge carries the point (OsmQuery.BridgeExempt).</summary>
    public bool OnBridge { get; init; }
    public static NudgeVerdict Of(OsmPointCheck c)
        => new(c.Known, c.Water, c.Building, false, c.Describe()) { OnBridge = c.OnBridge };
}

/// <summary>
/// The outcome for ONE authored vertex. <see cref="Problem"/> means the vertex itself is KNOWN to lie
/// in water or on a building; <see cref="Moved"/> that clear ground was found and the vertex now sits
/// there; <see cref="Unresolved"/> that none was found within the maximum and the vertex is KEPT;
/// <see cref="Unverified"/> that the tiles under it could not be read, so nothing was decided.
/// </summary>
public sealed record VertexNudge
{
    /// <summary>Index in the ROUTE (0 is the unit's live position and is never checked).</summary>
    public int RouteIndex { get; init; }
    public (double Lat, double Lon) From { get; init; }
    public (double Lat, double Lon) To { get; init; }
    public bool Problem { get; init; }
    public bool Moved { get; init; }
    public bool Unverified { get; init; }
    /// <summary>The authored vertex is carried by a road bridge (water under it does not count): kept.</summary>
    public bool OnBridge { get; init; }
    public bool Unresolved => Problem && !Moved;
    /// <summary>What is wrong at the authored vertex (or why it is unverified).</summary>
    public string Why { get; init; } = "";
    public double DistanceM { get; init; }
    public double BearingDeg { get; init; }
    public string Compass { get; init; } = "";
    public double SearchedMeters { get; init; }
    public int Tried { get; init; }
    public int RefusedWater { get; init; }
    public int RefusedBuilding { get; init; }
    public int RefusedSlope { get; init; }
    public int RefusedUnknown { get; init; }
    /// <summary>What the chosen ground was checked against, for the report.</summary>
    public string ClearOf { get; init; } = "";
}

/// <summary>
/// THE VERTEX NUDGE (RL-20260927-01), PURE: rings of increasing radius round a bad vertex, the first
/// KNOWN-clear point wins. The terrain enters only through the two delegates, which is what lets the
/// self-test drive it with synthetic verdicts and the service drive it with the tiles.
///
/// WHY A NUDGE AT ALL. Every movement task the vendor has plans to or drives at a vertex; none of them
/// makes a vertex in a lake safe. Move To checks "Is destination in nav area?", falls back to the feature
/// planner and ABORTS on failure (ground-vehicle-move-to.lua :1401-1404, :1423) - its only endpoint
/// adjustment is to a road shoulder (:423-460), never out of water; Move Along Route drives straight at
/// the vertex and stops at the edge (FINDING_GROUND_MOVEMENT_PRACTICE sec 5.2). Change (e) of cut A did
/// this by hand; this does it for every vertex and says so.
///
/// THE ORDER ON A RING, and why: candidates are taken cheapest-first by the path they would make -
/// prev -&gt; c -&gt; next for a passage point (so the vertex stays ON ITS LANE, near the straight line
/// between its neighbours, rather than wherever the geometry happens to be clear), and prev -&gt; c for
/// the last vertex (the near side of the obstacle: the unit stops short of the lake rather than beyond
/// it). Ties break on bearing from north, clockwise - deterministic, so a re-run moves the same vertex
/// to the same place.
///
/// UNKNOWN IS NEVER CLEAR, in both directions: a vertex whose tiles cannot be read is NOT moved (it is
/// not known to be bad), and a candidate whose tiles cannot be read is NOT taken (it is not known to be
/// clear).
/// </summary>
public static class VertexNudgeSearch
{
    /// <summary>The points of one ring: radius r, arc spacing at most <paramref name="stepM"/> (and at
    /// least 8 points), bearings from north clockwise.</summary>
    public static List<(double Lat, double Lon, double BearingDeg)> Ring((double Lat, double Lon) c,
                                                                         double radiusM, double stepM)
    {
        int n = Math.Max(8, (int)Math.Ceiling(2.0 * Math.PI * radiusM / Math.Max(1.0, stepM)));
        var outp = new List<(double, double, double)>(n);
        double k = RouteShift.MetresPerDegree;
        double cos = Math.Cos(c.Lat * Math.PI / 180.0);
        for (int i = 0; i < n; i++)
        {
            double brg = 360.0 * i / n;
            double th = brg * Math.PI / 180.0;
            outp.Add((c.Lat + radiusM * Math.Cos(th) / k, c.Lon + radiusM * Math.Sin(th) / (k * cos), brg));
        }
        return outp;
    }

    /// <summary>
    /// Nudge one vertex. <paramref name="atVertex"/> judges the AUTHORED vertex (water, buildings - the
    /// trigger); <paramref name="candidate"/> judges a ring point (water, buildings and, on the entity
    /// profile, flagged slope - the target). Returns what happened; never throws on bad ground.
    /// </summary>
    public static VertexNudge Nudge(int routeIndex, (double Lat, double Lon) v,
                                    (double Lat, double Lon)? prev, (double Lat, double Lon)? next,
                                    VertexNudgeOptions opt,
                                    Func<(double Lat, double Lon), NudgeVerdict> atVertex,
                                    Func<(double Lat, double Lon), NudgeVerdict> candidate,
                                    string clearOf)
    {
        var here = atVertex(v);
        if (!here.Water && !here.Building)
            return new VertexNudge
            {
                RouteIndex = routeIndex, From = v, To = v,
                Unverified = !here.Known,
                OnBridge = here.OnBridge,
                Why = here.Known && !here.OnBridge ? "clear" : here.Note,
            };

        int tried = 0, rw = 0, rb = 0, rs = 0, ru = 0;
        double searched = 0.0;
        double step = Math.Max(1.0, opt.StepMeters);
        for (double r = step; opt.MaxMeters > 0 && r <= opt.MaxMeters + 1e-9; r += step)
        {
            searched = r;
            var ring = Ring(v, r, step)
                .Select((p, i) => (P: (p.Lat, p.Lon), p.BearingDeg, i, Cost: Cost(prev, (p.Lat, p.Lon), next)))
                .OrderBy(c => Math.Round(c.Cost, 3))
                .ThenBy(c => c.i)
                .ToList();
            foreach (var c in ring)
            {
                tried++;
                var cv = candidate(c.P);
                if (cv.Clear)
                    return new VertexNudge
                    {
                        RouteIndex = routeIndex, From = v, To = c.P, Problem = true, Moved = true,
                        Why = here.Note,
                        DistanceM = TileMath.DistanceMeters(v.Lat, v.Lon, c.P.Lat, c.P.Lon),
                        BearingDeg = c.BearingDeg,
                        Compass = CompassWord(c.BearingDeg),
                        SearchedMeters = r, Tried = tried,
                        RefusedWater = rw, RefusedBuilding = rb, RefusedSlope = rs, RefusedUnknown = ru,
                        ClearOf = clearOf,
                    };
                if (cv.Water) rw++;
                else if (cv.Building) rb++;
                else if (cv.Slope) rs++;
                else if (!cv.Known) ru++;
            }
        }
        return new VertexNudge
        {
            RouteIndex = routeIndex, From = v, To = v, Problem = true, Moved = false,
            Why = here.Note, SearchedMeters = Math.Max(0.0, opt.MaxMeters), Tried = tried,
            RefusedWater = rw, RefusedBuilding = rb, RefusedSlope = rs, RefusedUnknown = ru,
            ClearOf = clearOf,
        };
    }

    /// <summary>The path a candidate would make: prev -&gt; c -&gt; next for a passage point, prev -&gt; c for
    /// the last vertex, 0 when there is no neighbour at all.</summary>
    public static double Cost((double Lat, double Lon)? prev, (double Lat, double Lon) c,
                              (double Lat, double Lon)? next)
    {
        double cost = 0.0;
        if (prev.HasValue) cost += TileMath.DistanceMeters(prev.Value.Lat, prev.Value.Lon, c.Lat, c.Lon);
        if (next.HasValue) cost += TileMath.DistanceMeters(c.Lat, c.Lon, next.Value.Lat, next.Value.Lon);
        return cost;
    }

    public static string CompassWord(double bearingDeg)
    {
        string[] words = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
        int i = (int)Math.Floor(((bearingDeg % 360.0 + 360.0) % 360.0 + 22.5) / 45.0) % 8;
        return words[i];
    }

    /// <summary>The route with every MOVED vertex replaced, everything else point for point.</summary>
    public static List<(double Lat, double Lon)> Apply(IReadOnlyList<(double Lat, double Lon)> route,
                                                       IReadOnlyList<VertexNudge> nudges)
    {
        var outp = route?.ToList() ?? new List<(double Lat, double Lon)>();
        if (nudges != null)
            foreach (var n in nudges)
                if (n.Moved && n.RouteIndex > 0 && n.RouteIndex < outp.Count) outp[n.RouteIndex] = n.To;
        return outp;
    }
}
