namespace VrfC2SimApp;

/// <summary>
/// WHICH GROUND-MOVE FORM A DISPATCH ISSUES (RL-20260927-01). Decided once, at the committed dispatch
/// point of VrfC2SimService.ExecuteTaskOnTick, by <see cref="VertexChainPolicy.FormFor"/>.
/// </summary>
public enum GroundMoveForm
{
    /// <summary>The route is ONE point: one MoveToLocation, exactly as before this change.</summary>
    SinglePointMoveTo,
    /// <summary>A LONE ground PLATFORM (not an aggregate) with a route of two or more points: one
    /// MoveToLocation per route vertex, the next issued only when the previous one COMPLETES
    /// (<see cref="VertexChainTracker"/>). No route object is created.</summary>
    MoveToPerVertex,
    /// <summary>CreateRoute + the deferred MoveAlongRoute (or PatrolRoute): every UNIT, every patrol,
    /// every non-ground mover, and every mover at all when Vrf:PlatformMoveToPerVertex is false.</summary>
    RouteTask,
}

/// <summary>
/// RL-20260927-01 - A LONE GROUND PLATFORM DRIVES ITS ROUTE AS ONE MOVE TO PER VERTEX.
///
/// WHY (docs/experiments/FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27.md secs 0-2 and 5.2). VR-Forces
/// gives a ground vehicle two route-shaped tasks and they are not the same product:
///   - Move Along Route is the LITERAL EXECUTOR: "a sequence of movements directly to each vertex of the
///     route. There is no path planning done as it moves toward the next vertex" (UG52 23.3 p505). It is
///     what the planner itself uses to drive a segment it has ALREADY planned, and it has no recovery.
///   - Move To is the DESTINATION task: it plans on roads, on the navigation mesh and around feature
///     obstacles, follows its own plan with Move Along Route subtasks, and recovers from a blockage by
///     backing up, skirting and replanning once (UG52 23.1 p500, 23.2 p500, 23.2.2 p502). "Movement to
///     a point is now performed by a Move To task" (MG 2.4 p18); moveToTask.h :181-184 - a move-to on a
///     ground vehicle starts ground-vehicle-move-to as a subtask, "which does more intelligent path
///     planning".
/// A UNIT's Move Along Route already plans per vertex for every member ("Each subordinate computes an
/// offset route and then traverses it by planning a path to each vertex in sequence", UG52 30.22 p598;
/// 30.24 p600), so units are NOT changed. A LONE PLATFORM given CreateRoute + MoveAlongRoute ran the
/// literal executor on STP's straight lines, and every stop on record is exactly that case (FINDING sec 3).
/// The vendor's own replacement for move-along is this shape: move_along_route_and_continue.lua walks the
/// route's vertices and starts a move-to-location subtask for each (EntityLevel scripts, :31-58).
///
/// WHAT IS KEPT: STP's vertices and their order - including any waypoints the lateral route shift
/// inserted (they are vertices like any other). The planner chooses only the path BETWEEN two of them.
/// Everything that judges the task is unchanged and keys on the LAST vertex: arrival evidence and the
/// time rules (RL-20260921-09), the progress watchdog (report-only, RL-20260913-03; TASKABRT,
/// RL-20260914-01) and a platform's parked Fire At (P0.3). An intermediate vertex's completion never
/// reaches any of them.
///
/// Pure: no bridge, no clock, no logger - so `--rulings-selftest` checks it offline.
/// </summary>
public static class VertexChainPolicy
{
    /// <summary>The ruling this behaviour implements; every log line about it names it.</summary>
    public const string RulingId = "RL-20260927-01";

    /// <summary>The kind MarkDispatched records for a chained move (the in-flight record's ExpectedKind).
    /// Its first token is "move", so InFlightTracker.KindLooksRight accepts the last vertex's "move-to"
    /// completion without an attribution-anomaly line.</summary>
    public const string DispatchKind = "move-to-per-vertex";

    /// <summary>
    /// THE VACUOUS-COMPLETION BAR, metres (Vrf:VertexArrivalRadiusMeters). A vertex's Move To that reports
    /// SUCCESS while the unit is farther than this from the vertex is VACUOUS - the R11 trap
    /// (docs/UNIT_MOVEMENT_RESEARCH.md :394-412: a planned move "completed" with both units still on their
    /// spawn points). WHY 100: a genuine arrival ends inside the 5.2 near-distance of 15 m (the D7/Y-13 row:
    /// near-distance 25 -> 15 m on every movement controller), and the Move To's only endpoint adjustment is
    /// onto a road shoulder, max(turning-radius, road width) away (ground-vehicle-move-to.lua :423-460) -
    /// metres. 100 m is several times that, so reflection lag and a shoulder offset never trip it, and it is
    /// a fifth of the 500 m Vrf:ArrivalRadiusMeters ceiling, so a completion it flags is one arrival evidence
    /// has to confirm on its own rather than a near miss. It is the same scale the interface already uses
    /// for "moved at all" (Vrf:ArrivalMinTravelMeters 100, Vrf:DropOriginVertexMeters 100) - no new number.
    /// 0 or less switches the test off: every completion is taken as an arrival.
    /// </summary>
    public const double DefaultVertexArrivalRadiusMeters = 100.0;

    /// <summary>
    /// THE ONE DECISION. Order of the tests = order of precedence:
    ///   one point               -> SinglePointMoveTo (unchanged);
    ///   switched on AND not an aggregate AND ground AND not a patrol -> MoveToPerVertex;
    ///   anything else           -> RouteTask (CreateRoute + MoveAlongRoute / PatrolRoute, unchanged).
    /// </summary>
    /// <param name="isAggregate">CreatedUnit.IsAggregate - a unit (UG52 30.22/30.24 plans per vertex
    /// already) never takes the chain.</param>
    /// <param name="isGround">DIS domain 1 of the created type. Move To's planner is the GROUND planner.</param>
    /// <param name="patrol">A Reconnoiter task PATROLS the route (PatrolRoute) - not a chain.</param>
    /// <param name="routePoints">The FINAL route, live start included, after the origin-vertex drop and
    /// after the route-shift and terrain-profile re-entries.</param>
    /// <param name="perVertexEnabled">Vrf:PlatformMoveToPerVertex.</param>
    public static GroundMoveForm FormFor(bool isAggregate, bool isGround, bool patrol, int routePoints,
                                         bool perVertexEnabled)
    {
        if (routePoints <= 1) return GroundMoveForm.SinglePointMoveTo;
        if (perVertexEnabled && !isAggregate && isGround && !patrol) return GroundMoveForm.MoveToPerVertex;
        return GroundMoveForm.RouteTask;
    }

    /// <summary>
    /// Is a VR-Forces completion's task type one a chained Move To can have produced? DtMoveToTask::type()
    /// "Returns \"move-to\"" (vrftasks/moveToTask.h :60); a report that carries no type is "can't tell"
    /// and is accepted, as InFlightTracker.KindLooksRight does. STRICTER than KindLooksRight on purpose: a
    /// late "move-along" completion of a route task the chain replaced must never advance the chain.
    /// </summary>
    public static bool IsChainMoveToType(string vrfTaskType)
        => string.IsNullOrEmpty(vrfTaskType) || vrfTaskType.Contains("move-to", StringComparison.OrdinalIgnoreCase);

    /// <summary>A completion with the unit farther than the bar from its vertex. An unreadable distance
    /// (NaN) is never vacuous - the chain cannot claim what it did not measure - and a bar of 0 or less
    /// switches the test off.</summary>
    public static bool IsVacuous(double distanceMeters, double radiusMeters)
        => double.IsFinite(radiusMeters) && radiusMeters > 0.0
           && double.IsFinite(distanceMeters) && distanceMeters > radiusMeters;

    /// <summary>The start-up line, said once and in BOTH states (absence is not evidence of off).</summary>
    public static string StartupLine(bool enabled, double radiusMeters)
    {
        string bar = double.IsFinite(radiusMeters) && radiusMeters > 0.0
            ? FormattableString.Invariant($"{radiusMeters:F0} m (Vrf:VertexArrivalRadiusMeters)")
            : "OFF (Vrf:VertexArrivalRadiusMeters <= 0: every completion is taken as an arrival)";
        return enabled
            ? $"MOVE TO PER VERTEX ON (Vrf:PlatformMoveToPerVertex, {RulingId}; FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27 " +
              "sec 5.2): a LONE ground PLATFORM with a route of two or more points is driven as one Move To per route " +
              "vertex - MoveToLocation to vertex 1, the next only when that one COMPLETES - so VR-Forces plans and " +
              "recovers every leg (UG52 23.1-23.2); no route object is created for it. UNITS are unchanged (their unit " +
              "Move Along Route plans per vertex already, UG52 30.22/30.24), and so are patrols, non-ground movers and " +
              "one-point moves. Arrival evidence, the time rules, the progress watchdog and a parked Fire At key on the " +
              $"LAST vertex only. A vertex completion farther than {bar} from its vertex is logged VACUOUS (R11). Turn " +
              "it off with Vrf:PlatformMoveToPerVertex=false (env Vrf__PlatformMoveToPerVertex=false): every ground " +
              "mover then gets CreateRoute + MoveAlongRoute as before."
            : $"MOVE TO PER VERTEX off (Vrf:PlatformMoveToPerVertex=false; the shipped default is ON, {RulingId}): " +
              "every ground mover with a route of two or more points gets CreateRoute + MoveAlongRoute, the pre-" +
              "2026-09-27 behaviour - a lone platform drives STP's lines with the literal executor, which plans " +
              "nothing and does not recover (UG52 23.3).";
    }
}

/// <summary>
/// THE VERTEX CHAIN, per unit (RL-20260927-01; see <see cref="VertexChainPolicy"/> for the why).
///
/// ONE STATE MACHINE PER UNIT NAME, because VR-Forces runs ONE task at a time (a Move To REPLACES
/// whatever the entity runs) and completions are attributed by the unit's marking:
///   Start          -> vertex 1 OUTSTANDING (the caller issues its MoveToLocation at once, same tick);
///   OnCompletion   -> vertex k done: if it is not the last, the chain moves to k+1 NOT outstanding -
///                     the caller re-enters the tick thread and calls TryBeginIssue, which is the ONLY
///                     way k+1 becomes outstanding. So a Move To is never issued before the previous one
///                     completed, and a second completion arriving in between is a Stray, not a step;
///   last vertex    -> the chain ENDS and the caller hands the completion to the task's own completion
///                     rules (or, when it is vacuous, withholds it - see <see cref="Outcome.FinalVacuous"/>).
/// A chain is ENDED (Clear/ClearIfOtherTask) by every terminal end of its VR-Forces task: a new task for the
/// same unit, a Fire At replacing the move, a vendor FAILURE, a dispatch that threw. A BACK-END LOSS FREEZES
/// every chain instead (<see cref="FreezeAll"/>): the task is aborted and "NOTHING IS RE-TASKED" on recovery
/// (STP-822), so a frozen chain never issues another Move To - but no command REPLACED its outstanding Move
/// To, so if the back end was only hung that Move To can still complete, and an INTERMEDIATE vertex's late
/// completion must not then pass for the task's (<see cref="Outcome.Retired"/>). NOT ended by the progress
/// watchdog's STALL report: that TASKABRT is report-only (RL-20260913-03, "report-only ... no automatic
/// re-tasking yet" as the question was put; the service's ReportStall says the task "stays in flight, no
/// VR-Forces command is issued and nothing is re-tasked") - so a unit that frees itself and reaches its
/// vertex carries on along its route, exactly as Move Along Route would have.
///
/// NO DOUBLE START: a second Start for the SAME task on a unit whose chain for that task is still running
/// returns <see cref="StartOutcome.AlreadyRunning"/> and changes nothing. A Start for a DIFFERENT task - or
/// for the same task over a FROZEN chain (a re-pushed order after a back-end recovery) - replaces the old
/// chain (the new Move To replaces the old one in VR-Forces).
///
/// Thread-safe (one lock); the service calls it from the tick thread only.
/// </summary>
public sealed class VertexChainTracker
{
    /// <summary>A geodetic point, degrees and metres. The bridge's Geodetic is deliberately NOT used: this
    /// class must load without VrfBridge.dll so `--rulings-selftest` runs without the MAK bin dirs.</summary>
    public readonly record struct Point(double Lat, double Lon, double Alt = 0.0);

    public enum StartOutcome
    {
        /// <summary>A new chain; its vertex 1 is OUTSTANDING and must be issued now.</summary>
        Started,
        /// <summary>A new chain that REPLACED another task's chain on the same unit.</summary>
        Replaced,
        /// <summary>This task's chain is already running on this unit: NOTHING is started or issued.</summary>
        AlreadyRunning,
        /// <summary>No unit name or no vertex to drive to: nothing is started.</summary>
        Refused,
    }

    public enum Outcome
    {
        /// <summary>The unit has no chain: the service's completion path runs exactly as before.</summary>
        NotChained,
        /// <summary>An INTERMEDIATE vertex completed: re-enter the tick thread and issue <see cref="Decision.Next"/>.
        /// NOTHING downstream is told - no completion, no watchdog reset, no engage.</summary>
        Advance,
        /// <summary>The LAST vertex completed: the chain has ended and the completion goes to the task's own
        /// completion rules (arrival evidence + start time + Duration, the parked Fire At) unchanged.</summary>
        FinalVertex,
        /// <summary>The LAST vertex reported success with the unit farther than the vacuous bar from it and
        /// no arrival report yet: the chain has ended, and the completion is NOT taken as the task's arrival.
        /// The task stays in flight; arrival evidence and the time rules decide the C2SIM outcome and a unit
        /// that never gets there is the progress watchdog's (RL-20260921-05: "The notion that geting stuck
        /// midway is a complete is completelly illogical").</summary>
        FinalVacuous,
        /// <summary>VR-Forces reported the Move To FAILED (success=false): the chain has ended and the
        /// completion takes the existing failure path (TASKABRT, follow-ons abandoned, no engage).</summary>
        Failed,
        /// <summary>A completion that is not this chain's outstanding Move To - none is outstanding (the next
        /// one is still being issued), or its type is not a move-to. SWALLOWED; the chain is unchanged.</summary>
        Stray,
        /// <summary>A FROZEN chain's (back-end loss) outstanding Move To completed on an INTERMEDIATE vertex, or
        /// a completion arrived with none outstanding: the chain has ended, the completion is SWALLOWED - it is
        /// not the task's arrival - and no further Move To is issued (nothing is re-tasked after a loss). A frozen
        /// chain's LAST vertex is <see cref="FinalVertex"/> and its failure <see cref="Failed"/>, as for any
        /// chain: the task is still the unit's in-flight task.</summary>
        Retired,
    }

    /// <summary>A copy of one chain's state, for decisions and log lines. VertexNumber is 1-based: the
    /// vertex whose Move To is outstanding, or is about to be issued. Frozen = a back-end loss froze it.
    /// CompletionTaskType = "" for a lone platform's Move To chain (RL-20260927-01), else the ONE vendor task type
    /// a planned container chain accepts (M3, RL-20260928-03 - "group_movement_simplified" or
    /// "navigate-to-location").</summary>
    public readonly record struct Snapshot(string UnitName, string VrfUuid, string TaskUuid, string TaskeeUuid,
                                           string TaskName, long Generation, int VertexNumber, int VertexCount,
                                           bool Outstanding, int VacuousCount, bool Frozen,
                                           string CompletionTaskType = "")
    {
        /// <summary>A PLANNED container chain (M3), not a lone platform's Move To chain.</summary>
        public bool Planned => !string.IsNullOrEmpty(CompletionTaskType);
    }

    public readonly record struct StartResult(StartOutcome Outcome, Point First, Snapshot Chain, Snapshot Replaced);

    /// <summary>
    /// What one completion means. CompletedVertex is 1-based (0 when nothing was completed). Distance is
    /// the unit's distance to the COMPLETED vertex and Displacement how far it moved since
    /// DisplacementFrom (0 = the dispatch position, k = vertex k's completion); both NaN when the unit's
    /// position could not be read. Next / NextVertex are set on Advance only.
    /// </summary>
    public readonly record struct Decision(Outcome Outcome, Snapshot Chain, int CompletedVertex,
                                           double DistanceMeters, double DisplacementMeters, int DisplacementFrom,
                                           bool Vacuous, Point Next, int NextVertex);

    private sealed class Chain
    {
        public string UnitName = "", VrfUuid = "", TaskUuid = "", TaskeeUuid = "", TaskName = "";
        public long Generation;
        public List<Point> Destinations = new();
        public int Index;                 // 0-based: the outstanding / next vertex
        public bool Outstanding;          // its Move To has been ISSUED and has not completed
        public Point LastFix;             // where the unit was at LastFixVertex (0 = at dispatch)
        public int LastFixVertex;
        public int VacuousCount;
        public bool Frozen;               // a back-end loss froze it: it issues nothing ever again
        public string CompletionTaskType = "";   // "" = the M1 move-to rule; else the one type accepted (M3)
        public bool HandLastVertexOn;     // M3: the last vertex always goes to the service's own rules (D-6)
    }

    private readonly object _lock = new();
    private readonly Dictionary<string, Chain> _chains = new(StringComparer.Ordinal);
    private long _generation;

    public VertexChainTracker(double vertexArrivalRadiusMeters)
        => VertexArrivalRadiusMeters = vertexArrivalRadiusMeters;

    /// <summary>Vrf:VertexArrivalRadiusMeters as this tracker applies it (0 or less = no vacuous test).</summary>
    public double VertexArrivalRadiusMeters { get; }

    /// <summary>How many chains are running.</summary>
    public int Count { get { lock (_lock) return _chains.Count; } }

    /// <summary>
    /// Start a chain for one dispatched task. <paramref name="origin"/> is the route's point 0 (the unit's
    /// live position at dispatch); <paramref name="destinations"/> are points 1..n - a two-point route is
    /// exactly ONE Move To. On Started/Replaced, vertex 1 is OUTSTANDING and <see cref="StartResult.First"/>
    /// is where the caller's MoveToLocation must go, now; if that call throws, the caller must Clear.
    /// M3 (RL-20260928-03), a PLANNED CONTAINER's chain: <paramref name="completionTaskType"/> is the ONE vendor task
    /// type whose completion is a vertex (exact, case-insensitive; an empty type is NOT accepted - a container runs only
    /// the planner's task, and nothing else may advance it), and <paramref name="handLastVertexOn"/> sends the LAST
    /// vertex to the service's own completion rules even when it is vacuous - for a container those rules are D-6
    /// (RL-20260927-04), which withholds a short completion itself, with its own line. Both default to the lone
    /// platform's behaviour (RL-20260927-01), unchanged.
    /// </summary>
    public StartResult Start(string unitName, string vrfUuid, string taskUuid, string taskeeUuid, string taskName,
                             Point origin, IReadOnlyList<Point> destinations, string completionTaskType = null,
                             bool handLastVertexOn = false)
    {
        if (string.IsNullOrEmpty(unitName) || destinations == null || destinations.Count == 0)
            return new StartResult(StartOutcome.Refused, default, default, default);
        lock (_lock)
        {
            Snapshot replaced = default;
            bool didReplace = false;
            if (_chains.TryGetValue(unitName, out var existing))
            {
                if (!existing.Frozen && string.Equals(existing.TaskUuid, taskUuid ?? "", StringComparison.Ordinal))
                    return new StartResult(StartOutcome.AlreadyRunning, default, Snap(existing), default);
                replaced = Snap(existing);
                didReplace = true;
            }
            var c = new Chain
            {
                UnitName = unitName,
                VrfUuid = vrfUuid ?? "",
                TaskUuid = taskUuid ?? "",
                TaskeeUuid = taskeeUuid ?? "",
                TaskName = taskName ?? "",
                Generation = ++_generation,
                Destinations = new List<Point>(destinations),
                Index = 0,
                Outstanding = true,
                LastFix = origin,
                LastFixVertex = 0,
                CompletionTaskType = completionTaskType ?? "",
                HandLastVertexOn = handLastVertexOn,
            };
            _chains[unitName] = c;
            return new StartResult(didReplace ? StartOutcome.Replaced : StartOutcome.Started,
                                   c.Destinations[0], Snap(c), replaced);
        }
    }

    /// <summary>
    /// A VR-Forces completion for <paramref name="unitName"/> (already resolved from the marking).
    /// <paramref name="fix"/> is the unit's live position NOW (null = unreadable).
    /// <paramref name="arrivalAlreadyReported"/> = the task was already reported complete from arrival
    /// evidence: a vacuous LAST vertex is then no longer withheld - the service's own swallow takes it.
    /// </summary>
    public Decision OnCompletion(string unitName, string vrfTaskType, bool success, Point? fix,
                                 bool arrivalAlreadyReported)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(unitName) || !_chains.TryGetValue(unitName, out var c))
                return new Decision(Outcome.NotChained, default, 0, double.NaN, double.NaN, 0, false, default, 0);
            // Not this chain's Move To: none is outstanding (vertex k+1 is still being issued), or the
            // report is of another task type. Swallowed - the chain is untouched. A FROZEN chain with nothing
            // outstanding will never issue again, so it ends here instead.
            if (!c.Outstanding)
            {
                if (!c.Frozen)
                    return new Decision(Outcome.Stray, Snap(c), 0, double.NaN, double.NaN, 0, false, default, 0);
                var snapIdle = Snap(c);
                _chains.Remove(unitName);
                return new Decision(Outcome.Retired, snapIdle, 0, double.NaN, double.NaN, 0, false, default, 0);
            }
            if (!Accepts(c, vrfTaskType))
                return new Decision(Outcome.Stray, Snap(c), 0, double.NaN, double.NaN, 0, false, default, 0);

            int k = c.Index + 1;
            int n = c.Destinations.Count;
            var target = c.Destinations[c.Index];
            double dist = fix is Point f ? Meters(f, target) : double.NaN;
            double disp = fix is Point g ? Meters(c.LastFix, g) : double.NaN;
            int dispFrom = c.LastFixVertex;

            if (!success)
            {
                var snapFailed = Snap(c);
                _chains.Remove(unitName);
                return new Decision(Outcome.Failed, snapFailed, k, dist, disp, dispFrom, false, default, 0);
            }

            bool vacuous = VertexChainPolicy.IsVacuous(dist, VertexArrivalRadiusMeters);
            if (vacuous) c.VacuousCount++;
            if (fix is Point h) { c.LastFix = h; c.LastFixVertex = k; }

            if (c.Index == n - 1)
            {
                var snapLast = Snap(c);
                _chains.Remove(unitName);
                return new Decision(vacuous && !arrivalAlreadyReported && !c.HandLastVertexOn
                                        ? Outcome.FinalVacuous : Outcome.FinalVertex,
                                    snapLast, k, dist, disp, dispFrom, vacuous, default, 0);
            }

            if (c.Frozen)
            {
                // A back-end loss froze it: this intermediate vertex is not the task's arrival, and nothing
                // is re-tasked after a loss - the chain ends here and issues no next vertex.
                var snapRetired = Snap(c);
                _chains.Remove(unitName);
                return new Decision(Outcome.Retired, snapRetired, k, dist, disp, dispFrom, vacuous, default, 0);
            }

            c.Index++;
            c.Outstanding = false;
            return new Decision(Outcome.Advance, Snap(c), k, dist, disp, dispFrom, vacuous,
                                c.Destinations[c.Index], c.Index + 1);
        }
    }

    /// <summary>
    /// The ONLY way an Advance becomes an issued Move To: true exactly once, for the chain generation and
    /// vertex the Advance named, and only if nothing ended or replaced the chain in between. On true that
    /// vertex is now OUTSTANDING and <paramref name="vertex"/> is where MoveToLocation must go.
    /// </summary>
    public bool TryBeginIssue(string unitName, long generation, int vertexNumber, out Point vertex, out Snapshot chain)
    {
        vertex = default;
        chain = default;
        if (string.IsNullOrEmpty(unitName)) return false;
        lock (_lock)
        {
            if (!_chains.TryGetValue(unitName, out var c) || c.Generation != generation || c.Outstanding
                || c.Frozen || c.Index + 1 != vertexNumber) return false;
            c.Outstanding = true;
            vertex = c.Destinations[c.Index];
            chain = Snap(c);
            return true;
        }
    }

    /// <summary>The unit's running chain, if any.</summary>
    public bool TryGet(string unitName, out Snapshot chain)
    {
        chain = default;
        if (string.IsNullOrEmpty(unitName)) return false;
        lock (_lock)
        {
            if (!_chains.TryGetValue(unitName, out var c)) return false;
            chain = Snap(c);
            return true;
        }
    }

    /// <summary>End the unit's chain, whatever task it belongs to. True if there was one.</summary>
    public bool Clear(string unitName, out Snapshot removed)
    {
        removed = default;
        if (string.IsNullOrEmpty(unitName)) return false;
        lock (_lock)
        {
            if (!_chains.TryGetValue(unitName, out var c)) return false;
            removed = Snap(c);
            _chains.Remove(unitName);
            return true;
        }
    }

    /// <summary>End the unit's chain only if it belongs to ANOTHER task than <paramref name="taskUuid"/> -
    /// what a new dispatch for the unit does (VR-Forces runs one task at a time).</summary>
    public bool ClearIfOtherTask(string unitName, string taskUuid, out Snapshot removed)
    {
        removed = default;
        if (string.IsNullOrEmpty(unitName)) return false;
        lock (_lock)
        {
            if (!_chains.TryGetValue(unitName, out var c)
                || string.Equals(c.TaskUuid, taskUuid ?? "", StringComparison.Ordinal)) return false;
            removed = Snap(c);
            _chains.Remove(unitName);
            return true;
        }
    }

    /// <summary>
    /// A BACK-END LOSS (STP-822): every task has just been aborted and nothing is re-tasked on recovery, so
    /// every chain is FROZEN - it never issues another Move To (TryBeginIssue refuses it). It is not removed:
    /// no command replaced its outstanding Move To, and if the back end was only hung that Move To can still
    /// complete - an intermediate vertex then ends the chain as <see cref="Outcome.Retired"/> (swallowed),
    /// the last vertex as <see cref="Outcome.FinalVertex"/>. Returns how many chains were frozen.
    /// </summary>
    public int FreezeAll()
    {
        lock (_lock)
        {
            int n = 0;
            foreach (var c in _chains.Values)
                if (!c.Frozen) { c.Frozen = true; n++; }
            return n;
        }
    }

    private static Snapshot Snap(Chain c)
        => new(c.UnitName, c.VrfUuid, c.TaskUuid, c.TaskeeUuid, c.TaskName, c.Generation, c.Index + 1,
               c.Destinations.Count, c.Outstanding, c.VacuousCount, c.Frozen, c.CompletionTaskType);

    /// <summary>Is this completion's type a vertex of this chain? The lone platform's rule
    /// (<see cref="VertexChainPolicy.IsChainMoveToType"/>) unless the chain names its one type (M3).</summary>
    private static bool Accepts(Chain c, string vrfTaskType)
        => string.IsNullOrEmpty(c.CompletionTaskType)
            ? VertexChainPolicy.IsChainMoveToType(vrfTaskType)
            : string.Equals(vrfTaskType, c.CompletionTaskType, StringComparison.OrdinalIgnoreCase);

    private static double Meters(Point a, Point b) => RouteExtentPolicy.GreatCircleMeters(a.Lat, a.Lon, b.Lat, b.Lon);
}
