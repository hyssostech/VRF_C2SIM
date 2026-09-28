using System.Globalization;
using VrfC2SimApp.Preflight;

namespace VrfC2SimApp;

/// <summary>
/// M3 - WHICH VENDOR TASK MOVES A POPULATED AGGREGATE CONTAINER ALONG A ROUTE (Vrf:AggregateMovePlanner).
///
/// RULED 2026-09-28, RL-20260928-03 (owner, "AUTO it is"): a tasked container moves by the vendor's PLANNING tasks,
/// per STP vertex; the default is <see cref="Auto"/>; Group, PerMemberOffRoad and Literal stay selectable for
/// registered comparisons; the pre-flight is report + fallback.
///
/// WHY (docs/experiments/FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md secs 2, 4, 5): PA_Move_Along_Route - what
/// C1 gave a container - is the aggregate LITERAL executor: every member on STP's straight centreline, no planning,
/// no avoidance (UG52 35.5.7 -> 30.24, 23.3). In G1-2 11 of 48 IBCT's Mech COs stopped on it inside one building of
/// the -2 hamlet ("Terrain too steep"; PREREG_IRONSTORM_AGG_G1-2_2026-09-28.md Result N1). The aggregate model set
/// HAS planning moves: navigate-to-location (base\scripts), whose obstacleQuery MAK_OBSTACLE covers the buildings and
/// water the sim loads (featureconfig.txt :405), and Move (Group) (AggregateTacticalLevel\scripts
/// group_movement_simplified) over the per-member Move to Location (Plan Along Roads) (UG52 35.5.11 p735).
/// </summary>
public enum AggregateMovePlanner
{
    /// <summary>THE DEFAULT (RL-20260928-03). Per STP vertex, per MEMBER, navigate-to-location with obstacleQuery
    /// MAK_OBSTACLE and a 10 m buffer always; pathQuery MAK_ROAD when the sim's road network lies within
    /// Vrf:RoadProximityMeters of the leg's line, else NONE - the manual's guidance per leg (UG52 35.5.11 p735: plan
    /// along roads "if there is a road network"; 30.5 p587: roads are looked for "over a fairly wide area", and "If
    /// there are no roads close to the route ... do not select this option").</summary>
    Auto,
    /// <summary>Move (Group) on the CONTAINER per vertex, useRoads = true (the vendor's default,
    /// group_movement_simplified.xml :70-74): each member is sent Move_To_Location_Plan_Path to its own offset
    /// destination (group_movement_simplified.lua :220-224), which plans with MAK_OBSTACLE and MAK_ROAD.</summary>
    Group,
    /// <summary>Move (Group) with useRoads = false: each member gets a plain move-to-location (group_movement_simplified
    /// .lua :225-227) - a DIRECT move, NOT a planner. A labelled control only: refused at start-up unless
    /// Vrf:AllowLiteralMove=true.</summary>
    GroupOffRoad,
    /// <summary>Per vertex, per member, navigate-to-location with pathQuery NONE everywhere (featureconfig.txt :88
    /// "Matches no features"), obstacleQuery MAK_OBSTACLE, buffer 10 m - the off-road frame variant.</summary>
    PerMemberOffRoad,
    /// <summary>Today's PA_Move_Along_Route on the route object (C1) - the rollback and the G1-2 baseline, issued
    /// byte-for-byte as before.</summary>
    Literal,
}

/// <summary>M3's road decision for one leg under <see cref="AggregateMovePlanner.Auto"/>.</summary>
public enum RoadProximity
{
    /// <summary>The planner does not decide roads per leg (Group, GroupOffRoad, PerMemberOffRoad, Literal).</summary>
    NotApplicable,
    /// <summary>A sim road lies within Vrf:RoadProximityMeters of the leg's line: pathQuery MAK_ROAD.</summary>
    Near,
    /// <summary>No sim road within Vrf:RoadProximityMeters and every tile of that band was read: pathQuery NONE.</summary>
    Far,
    /// <summary>No sim road found within the band on the tiles that could be read, but some could not: pathQuery NONE,
    /// and the line says UNKNOWN - never "no roads".</summary>
    Unknown,
}

/// <summary>One leg's road decision, with the evidence it was made on.</summary>
public readonly record struct RoadDecision(RoadProximity Proximity, string PathQuery, double DistanceM, string RoadId,
                                           string RoadKind, double ProximityM, double ReportBandM, int UnknownTiles)
{
    public bool UseRoads => string.Equals(PathQuery, AggregateMovePolicy.RoadPathQuery, StringComparison.Ordinal);
}

/// <summary>What one vertex issues: the script, the variables, and whether it goes to the container or to each member.</summary>
public sealed record PlannedVertexTask(string ScriptId, IReadOnlyList<ContainerTaskVar> Vars, bool PerMember, bool UseRoads,
                                       string PathQuery);

/// <summary>
/// M3, pure (no bridge, no clock, no logger): the setting, what each planner issues, the vendor's completion rule, the
/// AUTO road decision and every log-line shape. `--planned-move-selftest` checks it offline.
/// </summary>
public static class AggregateMovePolicy
{
    public const string RulingId = "RL-20260928-03";
    public const string FindingRef = "FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28";
    public const AggregateMovePlanner Default = AggregateMovePlanner.Auto;

    /// <summary>group_movement_simplified.xml :5 &lt;myScriptId&gt; ("Move (Group)", directory Echelon Group :20). The
    /// completion carries the id lower-cased (UG52 36.2.1 p753; ContainerScripts) - it is lower case already.</summary>
    public const string GroupScript = "group_movement_simplified";
    /// <summary>SMS\base\scripts\navigate-to-location.xml :5 &lt;myScriptId&gt; ("Navigate to Location").</summary>
    public const string NavigateScript = "navigate-to-location";
    /// <summary>featureconfig.txt :405: MAK_BUILDING, MAK_VEGETATION, MAK_INFRASTRUCTURE, MAK_WATERWAY and dynamic
    /// obstacles - what navigate-to-location plans round.</summary>
    public const string ObstacleQuery = "MAK_OBSTACLE";
    /// <summary>navigate-to-location.lua :27, :59-63 - its default path query: the road network.</summary>
    public const string RoadPathQuery = "MAK_ROAD";
    /// <summary>featureconfig.txt :88 "NONE: FALSE -- Matches no features" - plan on no path network.</summary>
    public const string NoPathQuery = "NONE";
    /// <summary>The buffer every per-member plan keeps from an obstacle feature (navigate-to-location.lua :14, :33-43;
    /// .xml range 0..1000) - the pre-flight's own 10 m clearance (Vrf:PreflightBuildingClearanceMeters).</summary>
    public const double BufferMeters = 10.0;
    /// <summary>Vrf:RoadProximityMeters default - UG52 30.5 p587, roads are looked for "over a fairly wide area".</summary>
    public const double DefaultRoadProximityMeters = 500.0;

    // ---------------------------------------------------------------------------------------------- the setting ----
    /// <summary>Vrf:AggregateMovePlanner -> the enum: a NAME, case-insensitive; blank = the default (Auto,
    /// RL-20260928-03). Anything else (a number included) is NOT recognised - the caller refuses to start.</summary>
    public static bool TryParse(string raw, out AggregateMovePlanner planner)
    {
        planner = Default;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        foreach (var name in Enum.GetNames(typeof(AggregateMovePlanner)))
            if (string.Equals(name, raw.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                planner = Enum.Parse<AggregateMovePlanner>(name);
                return true;
            }
        return false;
    }

    /// <summary>The start-up refusal for this setting on the aggregate model set, or null. An unknown value, and
    /// GroupOffRoad without Vrf:AllowLiteralMove (it is a direct move, not a planner - a control only).</summary>
    public static string StartRefusal(string raw, bool allowLiteralMove)
    {
        if (!TryParse(raw, out var p))
            return $"Vrf:AggregateMovePlanner '{raw}' is not one of {string.Join(", ", Enum.GetNames(typeof(AggregateMovePlanner)))} " +
                   $"(blank = {Default}, {RulingId})";
        if (p == AggregateMovePlanner.GroupOffRoad && !allowLiteralMove)
            return "Vrf:AggregateMovePlanner=GroupOffRoad sends every member a DIRECT move-to-location (Move (Group) with " +
                   "useRoads off, group_movement_simplified.lua :225-227) - it plans nothing and is a labelled CONTROL, not " +
                   "a planner; it runs only with Vrf:AllowLiteralMove=true";
        return null;
    }

    /// <summary>Every planner but Literal replaces PA_Move_Along_Route on a container's route.</summary>
    public static bool IsPlanned(AggregateMovePlanner p) => p != AggregateMovePlanner.Literal;

    /// <summary>The per-member planners: navigate-to-location to each member, completion aggregated here.</summary>
    public static bool IsPerMember(AggregateMovePlanner p) => p is AggregateMovePlanner.Auto or AggregateMovePlanner.PerMemberOffRoad;

    /// <summary>Move (Group) on the container: the vendor aggregates its members' completions itself.</summary>
    public static bool IsGroup(AggregateMovePlanner p) => p is AggregateMovePlanner.Group or AggregateMovePlanner.GroupOffRoad;

    /// <summary>The ONE vendor task type whose completion is a vertex of the container's chain.</summary>
    public static string CompletionTaskType(AggregateMovePlanner p) => IsGroup(p) ? GroupScript : NavigateScript;

    /// <summary>The kind the in-flight record carries (its first token matches the completion type,
    /// InFlightTracker.KindLooksRight).</summary>
    public static string DispatchKind(AggregateMovePlanner p) => IsGroup(p) ? GroupScript : NavigateScript;

    // ------------------------------------------------------------------------------------------ what is issued ----
    /// <summary>
    /// THE TASK FOR ONE VERTEX. The destination is the STP vertex with altitude 0: the vendor's "location without
    /// altitude" is a geocentric vector (UG52 36.2 p756) that the GUI saves at ellipsoid altitude 0.00 m (32 of 32
    /// Road to Kaunas destinations, lane M3 2026-09-28), and the script's Lua table is built from the SCRIPT'S
    /// metadata (vrfLua/luaScriptInterfaceImpl.h :130-131; UG52 37.3.3 Table 40 p799: Location with or without
    /// altitude -> Location3D) - so the facade's "location" (a DtRwVector, VrfFacade.cpp :184-185) is the vendor's
    /// own value class for it (RoadToKaunasPhaseTwo.pln :597-599; .oob :109485-109487).
    ///   Group / GroupOffRoad: RunScriptedTask(container, "group_movement_simplified", [destination, useRoads]) -
    ///     destinationPoint is left unset (the script uses it only when valid, .lua :246-250).
    ///   Auto / PerMemberOffRoad: RunScriptedTask(member, "navigate-to-location", [destination, obstacleQuery
    ///     MAK_OBSTACLE, pathQuery MAK_ROAD|NONE, buffer 10, displayRoute false, query ""]) to EVERY member - the six
    ///     variables the vendor's own saved navigate-to-location task carries (.oob :109481-109500). query="" is sent
    ///     on purpose: the script lets the old "query" name OVERRIDE obstacleQuery whenever it is not "" (.lua :48-51),
    ///     and an absent value is nil, not "".
    /// </summary>
    public static PlannedVertexTask ForVertex(AggregateMovePlanner p, double lat, double lon, RoadDecision road)
    {
        if (IsGroup(p))
        {
            bool useRoads = p == AggregateMovePlanner.Group;
            return new PlannedVertexTask(GroupScript, new[]
            {
                new ContainerTaskVar("destination", ContainerTaskVarKind.Location, Lat: lat, Lon: lon, Alt: 0.0),
                new ContainerTaskVar("useRoads", ContainerTaskVarKind.Flag, Flag: useRoads),
            }, PerMember: false, UseRoads: useRoads, PathQuery: useRoads ? RoadPathQuery : "");
        }
        if (IsPerMember(p))
        {
            string path = p == AggregateMovePlanner.Auto && road.Proximity == RoadProximity.Near ? RoadPathQuery : NoPathQuery;
            return new PlannedVertexTask(NavigateScript, NavigateVars(lat, lon, path), PerMember: true,
                                         UseRoads: path == RoadPathQuery, PathQuery: path);
        }
        throw new InvalidOperationException($"Vrf:AggregateMovePlanner={p} issues no planned vertex (Literal is PA_Move_Along_Route)");
    }

    public static IReadOnlyList<ContainerTaskVar> NavigateVars(double lat, double lon, string pathQuery) => new[]
    {
        new ContainerTaskVar("destination", ContainerTaskVarKind.Location, Lat: lat, Lon: lon, Alt: 0.0),
        new ContainerTaskVar("obstacleQuery", ContainerTaskVarKind.Text, ObstacleQuery),
        new ContainerTaskVar("pathQuery", ContainerTaskVarKind.Text, pathQuery ?? NoPathQuery),
        new ContainerTaskVar("buffer", ContainerTaskVarKind.Number, Number: BufferMeters),
        new ContainerTaskVar("displayRoute", ContainerTaskVarKind.Flag, Flag: false),
        new ContainerTaskVar("query", ContainerTaskVarKind.Text, ""),
    };

    // ------------------------------------------------------------------------------------- the AUTO road decision ----
    /// <summary>
    /// THE PER-LEG ROAD DECISION (Auto only; RL-20260928-03). <paramref name="inBand"/> is the sim's road layer
    /// (OsmSet.Highways) searched within the proximity of the leg's line, <paramref name="wide"/> the same within twice
    /// it (only to print a distance for a FAR leg). NEAR = a road within the proximity (-> MAK_ROAD); else UNKNOWN when
    /// any tile of the proximity band was unreadable, FAR when every one was read (both -> NONE). Unknown ground is
    /// never "no roads".
    /// </summary>
    public static RoadDecision DecideRoads(AggregateMovePlanner p, OsmRoadProximity inBand, OsmRoadProximity wide,
                                           double proximityM)
    {
        double prox = double.IsFinite(proximityM) && proximityM > 0.0 ? proximityM : DefaultRoadProximityMeters;
        if (p != AggregateMovePlanner.Auto)
            return new RoadDecision(RoadProximity.NotApplicable,
                                    p == AggregateMovePlanner.PerMemberOffRoad ? NoPathQuery : "", double.NaN, "", "", prox,
                                    2.0 * prox, 0);
        if (inBand != null && inBand.Found && inBand.MinM <= prox)
            return new RoadDecision(RoadProximity.Near, RoadPathQuery, inBand.MinM, inBand.RoadId, inBand.RoadKind, prox,
                                    2.0 * prox, inBand.UnknownTiles);
        int unknown = inBand?.UnknownTiles ?? 1;
        if (inBand == null || unknown > 0)
            return new RoadDecision(RoadProximity.Unknown, NoPathQuery, double.NaN, "", "", prox, 2.0 * prox, unknown);
        bool farFound = wide != null && wide.Found;
        return new RoadDecision(RoadProximity.Far, NoPathQuery, farFound ? wide.MinM : double.PositiveInfinity,
                                farFound ? wide.RoadId : "", farFound ? wide.RoadKind : "", prox, 2.0 * prox, 0);
    }

    // ------------------------------------------------------------------------------ the vendor's completion rule ----
    /// <summary>
    /// MOVE (GROUP)'S COMPLETION, AS THE VENDOR DEFINES IT - and the rule the per-member planners are aggregated by
    /// ("completion aggregated from the members like Move (Group) does"). group_movement_simplified.lua :196-205 gives
    /// the role PseudoSub isCritical = true and no successPolicy / letAllFinish, so the behaviour engine's defaults
    /// apply (makLua\behaviorEngine\roleNode.lua :61-67: successPolicy "one", letAllFinish true):
    ///   - the role node ends only when EVERY member's task has ended (:169-246; letAllFinish);
    ///   - it SUCCEEDS when AT LEAST ONE member's task succeeded (:217-223, :270-271);
    ///   - it FAILS when none did (:265-266) - or when there is no member at all (:113-134, critical);
    ///   - "critical" then makes that role failure the command's failure (:274-280; behaviorEngine.lua :35-38).
    /// A member's task result is its task's own (taskNode.lua :84-97); a destroyed member counts as failed
    /// (roleNode.lua :182-190). NOTE: "one member's plan failure fails the command" (the brief; FINDING sec 2) is NOT
    /// this rule - the implementation fails only when NO member succeeded.
    /// Returns null while any member is still running, else true/false.
    /// </summary>
    public static bool? GroupOutcome(IReadOnlyCollection<bool?> memberResults)
    {
        if (memberResults == null || memberResults.Count == 0) return false;          // critical role, no subordinate
        if (memberResults.Any(r => r == null)) return null;                            // letAllFinish: still running
        return memberResults.Any(r => r == true);                                      // successPolicy "one"
    }

    /// <summary>The TASKABRT reason for a planned vertex the vendor rule failed.</summary>
    public static string FailureReason(string container, int vertex, int vertexCount, AggregateMovePlanner p, int members,
                                       int failed)
        => IsGroup(p)
            ? $"PLANNED MOVE {container} vertex {vertex} of {vertexCount}: {GroupScript} FAILED - VR-Forces ended Move (Group) " +
              "with success=false, which its behaviour engine does only when NO member's move succeeded (role PseudoSub " +
              "critical, successPolicy one - roleNode.lua :246-280); the chain ends and no further vertex is issued"
            : FormattableString.Invariant(
                $"PLANNED MOVE {container} vertex {vertex} of {vertexCount}: {NavigateScript} FAILED for {failed} of {members} ") +
              "member(s) and succeeded for none - by the vendor's Move (Group) rule (a critical role fails only when no member " +
              "succeeded, roleNode.lua :246-280) the vertex FAILED; the chain ends and no further vertex is issued";

    // ------------------------------------------------------------------------------------------- the log lines ----
    /// <summary>
    /// ONE LINE PER VERTEX EVENT, one shape (the G1-3 registration greps it):
    ///   "PLANNED MOVE &lt;container&gt; vertex k of N: &lt;planner&gt; issued (useRoads &lt;bool&gt;) -&gt; &lt;outcome&gt;"
    /// &lt;planner&gt; is the script id; for a per-member planner useRoads is pathQuery MAK_ROAD. Outcomes:
    /// OUTSTANDING (at issue), COMPLETED (an intermediate vertex), LAST VERTEX COMPLETED, FAILED.
    /// </summary>
    public static string VertexLine(string container, int vertex, int vertexCount, string scriptId, bool useRoads,
                                     string outcome, string detail = "")
        => $"PLANNED MOVE {container} vertex {vertex.ToString(CultureInfo.InvariantCulture)} of " +
           $"{vertexCount.ToString(CultureInfo.InvariantCulture)}: {scriptId} issued (useRoads {(useRoads ? "true" : "false")}) " +
           $"-> {outcome}{(string.IsNullOrEmpty(detail) ? "" : " " + detail)}";

    /// <summary>
    /// THE AUTO DECISION LINE, per leg (the coordinator's shape):
    ///   "PLANNED MOVE &lt;c&gt; vertex k of N: roads NEAR (&lt;d&gt; m) -&gt; MAK_ROAD; obstacleQuery MAK_OBSTACLE buffer 10 m"
    ///   "PLANNED MOVE &lt;c&gt; vertex k of N: roads FAR (&lt;d&gt; m) -&gt; NONE; obstacleQuery MAK_OBSTACLE buffer 10 m"
    /// plus UNKNOWN (a tile of the proximity band unreadable) -&gt; NONE. A FAR leg with no road even within twice the
    /// proximity prints "&gt; &lt;2P&gt; m" for the distance.
    /// </summary>
    public static string RoadLine(string container, int vertex, int vertexCount, RoadDecision r)
    {
        string head = $"PLANNED MOVE {container} vertex {vertex.ToString(CultureInfo.InvariantCulture)} of " +
                      $"{vertexCount.ToString(CultureInfo.InvariantCulture)}: ";
        string tail = FormattableString.Invariant($"; obstacleQuery {ObstacleQuery} buffer {BufferMeters:0} m");
        return r.Proximity switch
        {
            RoadProximity.Near => head + FormattableString.Invariant($"roads NEAR ({r.DistanceM:F0} m) -> {RoadPathQuery}") + tail +
                                  $" [nearest OSM {r.RoadId} {r.RoadKind}; within Vrf:RoadProximityMeters={r.ProximityM.ToString("F0", CultureInfo.InvariantCulture)}]",
            RoadProximity.Far => head + (double.IsFinite(r.DistanceM)
                                     ? FormattableString.Invariant($"roads FAR ({r.DistanceM:F0} m) -> {NoPathQuery}")
                                     : FormattableString.Invariant($"roads FAR (> {r.ReportBandM:F0} m) -> {NoPathQuery}")) + tail +
                                 (double.IsFinite(r.DistanceM) ? $" [nearest OSM {r.RoadId} {r.RoadKind}]" : "") +
                                 FormattableString.Invariant($" [none within Vrf:RoadProximityMeters={r.ProximityM:F0}]"),
            RoadProximity.Unknown => head + FormattableString.Invariant(
                                         $"roads UNKNOWN ({r.UnknownTiles} osm-highways tile(s) within {r.ProximityM:F0} m NOT readable, no road within {r.ProximityM:F0} m on the rest) -> {NoPathQuery}") + tail,
            _ => head + $"roads not decided per leg (pathQuery {(string.IsNullOrEmpty(r.PathQuery) ? "n/a" : r.PathQuery)})" + tail,
        };
    }

    /// <summary>The start-up line, said once and in every state (absence is not evidence of a mode).</summary>
    public static string StartupLine(AggregateMovePlanner p, bool containerMode, bool allowLiteralMove, double proximityM,
                                     int highwayTiles, int highwayEmpty, string cacheDir)
    {
        if (!containerMode)
            return $"AGGREGATE MOVE PLANNER not in use (Vrf:ModelSet is not AggregateTacticalLevel): Vrf:AggregateMovePlanner={p} " +
                   "applies only to populated Aggregate Containers (M3, " + RulingId + ").";
        string what = p switch
        {
            AggregateMovePlanner.Auto =>
                "per STP vertex, per MEMBER, navigate-to-location with obstacleQuery MAK_OBSTACLE and buffer 10 m always, " +
                FormattableString.Invariant($"pathQuery MAK_ROAD where the sim's road layer (osm-highways) lies within {proximityM:F0} m ") +
                "(Vrf:RoadProximityMeters) of the leg's line, else NONE - decided and logged per leg",
            AggregateMovePlanner.Group =>
                "per STP vertex, Move (Group) (group_movement_simplified) on the CONTAINER with useRoads=true - each member " +
                "plans its own offset destination with Move to Location (Plan Along Roads)",
            AggregateMovePlanner.GroupOffRoad =>
                "per STP vertex, Move (Group) with useRoads=false - each member drives a DIRECT move-to-location; a labelled " +
                "CONTROL (Vrf:AllowLiteralMove=" + (allowLiteralMove ? "true" : "false") + "), not a planner",
            AggregateMovePlanner.PerMemberOffRoad =>
                "per STP vertex, per MEMBER, navigate-to-location with pathQuery NONE, obstacleQuery MAK_OBSTACLE, buffer 10 m",
            _ => "PA_Move_Along_Route on the route object, as C1 built it - the LITERAL executor, the rollback and the G1-2 baseline",
        };
        string roads = p == AggregateMovePlanner.Auto
            ? FormattableString.Invariant($" Road layer cache: {cacheDir}\\osm-highways ({highwayTiles} tile file(s), {highwayEmpty} of them 0 bytes); ") +
              "the dispatch reads it and never fetches; the route-shift worker fetches missing tiles online first."
            : "";
        string completion = IsPlanned(p)
            ? " The next vertex is issued only when the previous one COMPLETES (the vendor's own rule for Move (Group); the same " +
              "rule aggregates the members of a per-member planner: done when every member's move ended, a success when at " +
              "least one succeeded); arrival evidence, D-6 and the watchdog judge the LAST vertex; the pre-flight REPORTS a " +
              "planned route and does not detour it (" + FindingRef + " sec 5 item 4)."
            : " The lateral route shift still detours a flagged leg of this literally-driven route.";
        return $"AGGREGATE MOVE PLANNER {p} (Vrf:AggregateMovePlanner; {RulingId}, the default is Auto): a CONTAINER's route " +
               $"is driven {what}.{roads}{completion} Point moves (PA_Move_To_Location_Direct), patrols (PA_Patrol_Route) and holds " +
               "are unchanged.";
    }
}

/// <summary>
/// M3: THE PER-MEMBER STEP OF A PLANNED CONTAINER MOVE (Auto, PerMemberOffRoad). One context per container, bound to
/// the container's vertex-chain generation; one OPEN STEP per issued vertex, holding each member's result. The
/// member's own "navigate-to-location" completions arrive through OnVrfTaskCompleted (the member branch); when the
/// LAST member of the step reports, the step closes with the vendor's Move (Group) rule
/// (<see cref="AggregateMovePolicy.GroupOutcome"/>) and the caller hands that one aggregated completion to the
/// container's chain. Thread-safe (one lock); the service calls it from the tick thread only.
/// </summary>
public sealed class PlannedMoveTracker
{
    public readonly record struct Member(string Name, string Uuid);

    public readonly record struct Context(string Container, string ContainerUuid, long Generation,
                                          AggregateMovePlanner Planner, IReadOnlyList<Member> Members,
                                          IReadOnlyList<RoadDecision> Roads, int VertexCount);

    /// <summary>What one member completion did. Consumed = it was a member step of an open vertex of this
    /// container; StepDone = it was the last one and <see cref="Success"/> is the vertex's outcome.</summary>
    public readonly record struct MemberVerdict(bool Consumed, bool StepDone, bool Success, int Vertex, int Members,
                                                int Succeeded, int Failed, int Pending, string Why);

    private sealed class Ctx
    {
        public Context C;
        public int OpenVertex;                                        // 0 = no step open
        public Dictionary<string, bool?> Results = new(StringComparer.Ordinal);
    }

    private readonly object _lock = new();
    private readonly Dictionary<string, Ctx> _byContainer = new(StringComparer.Ordinal);

    /// <summary>Start (or replace) the container's context.</summary>
    public void Begin(Context c)
    {
        if (string.IsNullOrEmpty(c.Container)) return;
        lock (_lock) _byContainer[c.Container] = new Ctx { C = c };
    }

    public bool TryGet(string container, out Context c)
    {
        c = default;
        if (string.IsNullOrEmpty(container)) return false;
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container, out var x)) return false;
            c = x.C;
            return true;
        }
    }

    /// <summary>Does this container's context belong to that chain generation?</summary>
    public bool Owns(string container, long generation)
        => TryGet(container, out var c) && c.Generation == generation;

    /// <summary>The road decision for vertex k (1-based), or NotApplicable.</summary>
    public RoadDecision RoadFor(string container, int vertex)
    {
        if (!TryGet(container, out var c) || c.Roads == null || vertex < 1 || vertex > c.Roads.Count)
            return new RoadDecision(RoadProximity.NotApplicable, "", double.NaN, "", "", 0, 0, 0);
        return c.Roads[vertex - 1];
    }

    /// <summary>Open vertex k's step for a per-member planner: every member pending.</summary>
    public bool BeginVertex(string container, long generation, int vertex)
    {
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container ?? "", out var x) || x.C.Generation != generation) return false;
            x.OpenVertex = vertex;
            x.Results = x.C.Members.ToDictionary(m => m.Name, _ => (bool?)null, StringComparer.Ordinal);
            return true;
        }
    }

    /// <summary>A member's own completion. Only a "navigate-to-location" completion of a member of an OPEN step counts;
    /// anything else (a subtask, a late report after the step closed, another container's member) is not consumed.</summary>
    public MemberVerdict OnMemberCompleted(string container, string member, string taskType, bool success)
    {
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container ?? "", out var x))
                return new MemberVerdict(false, false, false, 0, 0, 0, 0, 0, "no planned move for this container");
            if (!AggregateMovePolicy.IsPerMember(x.C.Planner))
                return new MemberVerdict(false, false, false, x.OpenVertex, 0, 0, 0, 0,
                                         $"{x.C.Planner} aggregates its members in the vendor's Move (Group)");
            if (x.OpenVertex == 0)
                return new MemberVerdict(false, false, false, 0, x.C.Members.Count, 0, 0, 0, "no vertex step is open");
            if (!string.Equals(taskType, AggregateMovePolicy.NavigateScript, StringComparison.OrdinalIgnoreCase))
                return new MemberVerdict(false, false, false, x.OpenVertex, x.C.Members.Count, 0, 0, 0,
                                         $"'{taskType}' is not the step's {AggregateMovePolicy.NavigateScript}");
            if (!x.Results.TryGetValue(member ?? "", out var prior))
                return new MemberVerdict(false, false, false, x.OpenVertex, x.C.Members.Count, 0, 0, 0,
                                         "not a member this step tasked");
            if (prior != null)
                return new MemberVerdict(true, false, false, x.OpenVertex, x.C.Members.Count, 0, 0, 0,
                                         "a second completion from the same member in this step - swallowed");
            x.Results[member] = success;
            int ok = x.Results.Values.Count(v => v == true), bad = x.Results.Values.Count(v => v == false),
                pending = x.Results.Values.Count(v => v == null);
            var outcome = AggregateMovePolicy.GroupOutcome(x.Results.Values.ToList());
            if (outcome == null)
                return new MemberVerdict(true, false, false, x.OpenVertex, x.Results.Count, ok, bad, pending, "");
            int vertex = x.OpenVertex;
            x.OpenVertex = 0;                                           // the step is closed; late reports are not consumed
            return new MemberVerdict(true, true, outcome.Value, vertex, x.Results.Count, ok, bad, 0, "");
        }
    }

    public void End(string container)
    {
        if (string.IsNullOrEmpty(container)) return;
        lock (_lock) _byContainer.Remove(container);
    }
}
