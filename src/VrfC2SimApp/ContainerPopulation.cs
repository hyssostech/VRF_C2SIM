using System.Globalization;

namespace VrfC2SimApp;

/// <summary>A scripted-task variable, bridge-free (so the population logic loads without VrfBridge.dll). The service
/// converts each to the bridge's ScriptVar; --populate-selftest proves the conversion binds the vendor's types.
/// Text and Number (M3, RL-20260928-03) are the bridge's existing String and Real kinds (ScriptVar.Text / ScriptVar.Number,
/// --scripted-task-selftest): navigate-to-location's obstacleQuery / pathQuery / query are strings and its buffer a
/// number (base\scripts\navigate-to-location.xml; the vendor's own saved task carries DtRwString and DtRwReal for them,
/// RoadToKaunasPhaseTwo.oob :109481-109500).</summary>
public enum ContainerTaskVarKind { Object, Flag, Location, Text, Number }

public readonly record struct ContainerTaskVar(string Name, ContainerTaskVarKind Kind, string Text = "",
                                               bool Flag = false, double Lat = 0.0, double Lon = 0.0, double Alt = 0.0,
                                               double Number = 0.0)
{
    public override string ToString() => Kind switch
    {
        ContainerTaskVarKind.Object => $"{Name}={Text}",
        ContainerTaskVarKind.Flag => $"{Name}={(Flag ? "true" : "false")}",
        ContainerTaskVarKind.Text => $"{Name}=\"{Text}\"",
        ContainerTaskVarKind.Number => FormattableString.Invariant($"{Name}={Number:0.###}"),
        _ => FormattableString.Invariant($"{Name}=({Lat:F6},{Lon:F6})"),
    };
}

/// <summary>
/// THE CONTAINER'S OWN TASKS (C1; RL-20260927-03). An Aggregate Container has no movement system of its own
/// (PseudoAggregate.ope :40 "(systems )"); its script-enable-controller enables the PA_* scripted tasks that task its
/// subordinates (PseudoAggregate.ope:115-117). The script ids are the vendor XML's myScriptId fields, verbatim:
///   PA_Move_Along_Route.xml:5         &lt;myScriptId&gt;PA_Move_Along_Route&lt;/myScriptId&gt;
///   PA_Move_To_Location_Direct.xml:5  &lt;myScriptId&gt;PA_Move_To_Location_Direct&lt;/myScriptId&gt;
///   PA_Patrol_Route.xml:5             &lt;myScriptId&gt;PA_Patrol_Route&lt;/myScriptId&gt;
/// (all under C:\MAK\vrforces5.2d\data\simulationModelSets\AggregateLevelBase\scripts). The case is the XML's. The
/// vendor sends the same ids in BOTH cases at run time - "PA_Move_Along_Route" from group-attack-to-objective.lua:533
/// and group-navigate-route-to-location.lua:81, "pa_move_along_route" from PA_Move_Along_Route.lua:47/:66 and the
/// Road to Kaunas plan - because "Internally, VR-Forces converts all script IDs to lower case" (UG52 36.2.1 p753); a
/// vendor plan stores task-type "pa_attack_by_fire" beside script-id "PA_Attack_By_Fire"
/// (AggregateLevelSimulationOverview.pln:1767-1770), so the task type the completion carries is the lower-case id.
/// VARIABLES, name and type from the same XML files: route = simulationobject (PA_Move_Along_Route.xml:32-33),
/// reverseDirection / startAtClosestVertex / retrograde = checkbox (:68-69, :86-87, :50-51); location = location
/// (PA_Move_To_Location_Direct.xml:32-33). VALUES: the vendor plan's (RoadToKaunas.pln:4796-4812 -
/// reverseDirection False, startAtClosestVertex False, no retrograde), and retrograde false for the direct move.
/// </summary>
public static class ContainerScripts
{
    public const string MoveAlongRoute = "PA_Move_Along_Route";
    public const string MoveToLocationDirect = "PA_Move_To_Location_Direct";
    public const string PatrolRoute = "PA_Patrol_Route";

    /// <summary>The vendor script files these ids come from (the self-test re-reads their myScriptId).</summary>
    public static readonly IReadOnlyList<string> VendorXmlFiles = new[]
    {
        "PA_Move_Along_Route.xml", "PA_Move_To_Location_Direct.xml", "PA_Patrol_Route.xml",
    };

    public static IReadOnlyList<ContainerTaskVar> AlongRoute(string routeUuid) => new[]
    {
        new ContainerTaskVar("route", ContainerTaskVarKind.Object, routeUuid ?? ""),
        new ContainerTaskVar("reverseDirection", ContainerTaskVarKind.Flag, Flag: false),
        new ContainerTaskVar("startAtClosestVertex", ContainerTaskVarKind.Flag, Flag: false),
    };

    public static IReadOnlyList<ContainerTaskVar> Patrol(string routeUuid) => new[]
    {
        new ContainerTaskVar("route", ContainerTaskVarKind.Object, routeUuid ?? ""),
    };

    public static IReadOnlyList<ContainerTaskVar> ToLocation(double lat, double lon, double alt) => new[]
    {
        new ContainerTaskVar("location", ContainerTaskVarKind.Location, Lat: lat, Lon: lon, Alt: alt),
        new ContainerTaskVar("retrograde", ContainerTaskVarKind.Flag, Flag: false),
    };

    /// <summary>The variables a ROUTE script takes, by script id (the route-created callback's side).</summary>
    public static IReadOnlyList<ContainerTaskVar> ForRoute(string scriptId, string routeUuid)
        => string.Equals(scriptId, PatrolRoute, StringComparison.Ordinal) ? Patrol(routeUuid) : AlongRoute(routeUuid);

    /// <summary>
    /// WHICH SCRIPT, from the dispatch form the entity path already decides (VertexChainPolicy.FormFor; design sec 6:
    /// "one point gets PA_Move_To_Location_Direct for MoveToLocation (:4984); a patrol PA_Patrol_Route (:6004)", the
    /// route PA_Move_Along_Route where MoveAlongRoute is issued (:6025)). A container is an aggregate, so FormFor
    /// never answers MoveToPerVertex for it.
    /// </summary>
    public static string ForForm(GroundMoveForm form, bool patrol)
        => form == GroundMoveForm.SinglePointMoveTo ? MoveToLocationDirect : patrol ? PatrolRoute : MoveAlongRoute;
}

/// <summary>What the population state machine calls on VR-Forces. The service implements it over VrfBridge on the
/// tick thread; --populate-selftest implements it with a recording fake. DeleteObject is here ONLY so a fake can
/// prove it is never called: populating in place never deletes (RL-20260927-03; UG52 18.5 p442 "If you delete a unit,
/// all of its members are deleted").</summary>
public interface IContainerBridge
{
    void AddToOrganization(string childUuid, string superiorUuid);
    /// <summary>How many subordinates the aggregate PUBLISHES (sub-aggregates + entities); -1 = no reading.</summary>
    int PublishedSubordinateCount(string aggregateUuid);
    void RunScriptedTask(string uuid, string scriptId, IReadOnlyList<ContainerTaskVar> vars);
    void DeleteObject(string uuid);
}

public enum PopulateStage
{
    /// <summary>Planned; the member creates have not been issued yet (the slot check runs off the tick thread).</summary>
    Planned,
    /// <summary>The member creates are issued; waiting for their ObjectCreated.</summary>
    Creating,
    /// <summary>Every member (or the arrived subset) is attached by AddToOrganization; waiting for the container to
    /// PUBLISH them.</summary>
    Attached,
    /// <summary>The container publishes its members: its tasks may be issued.</summary>
    Published,
    /// <summary>No composition, or it could not be planned: the container stays EMPTY and a move on it is refused.</summary>
    Refused,
    /// <summary>The gate's bound passed before the container published its members: a move on it is refused.</summary>
    TimedOut,
}

/// <summary>One line the service logs for a population event (the populator decides, the service writes).</summary>
public readonly record struct PopulateEvent(bool Warning, string Text);

/// <summary>The dispatch verdict for a MOVE on a container.</summary>
public readonly record struct ContainerMoveVerdict(bool Ready, int Members, string Reason);

/// <summary>
/// THE IN-PLACE POPULATION, ONE STATE MACHINE PER TASKED CONTAINER (C1; RL-20260927-03: "the only units that need to
/// be hydrated are the ones that are actually task[ed]" - populate in place, never delete and re-create).
///   Begin      -> Planned: the members and their ring slots are fixed, the gate is registered.
///   Issued     -> Creating: the service created every member AGGREGATED (UG52 Table 68 p1470: "4 Aggregated unit (a
///                 unit that does not have subordinate simulation objects)").
///   created    -> when EVERY member has its ObjectCreated: AddToOrganization(member, container) for each, in planned
///                 order (UG52 18.1 p438: "You cannot create units that are subordinates of an existing unit. Once you
///                 create a unit, you can subordinate it to another unit.") -> Attached. At the ATTACH deadline the
///                 members that did arrive are attached, the missing ones named (never a silent partial).
///   Sweep      -> Published once the container PUBLISHES at least the attached count: the attach lands in the
///                 NEXT-FRAME subordinate manager (vrfobjcore/vrfObjectStateRepository.inl:148-156) and the vendor
///                 script snapshots its subordinates ONCE, in init() (PA_Move_Along_Route.lua:37) - so the task waits
///                 for the container's published subordinate list (vl/aggregateStateRepository.h:86-92), read through
///                 VrfBridge.PublishedSubordinateCount. Past the gate's bound -> TimedOut.
///   MoveVerdict-> a MOVE is issued only on Published; everything else is REFUSED with the reason (TASKABRT) - a
///                 memberless container's move ends at once (PA_Move_Along_Route.lua:152-155) and must never read as
///                 a success.
/// Thread-safe (one lock): Begin runs on the order thread, the rest on the tick thread.
/// </summary>
public sealed class ContainerPopulator
{
    private sealed class Pop
    {
        public string Name = "", Uuid = "";
        public PopulateStage Stage;
        public List<PopulateMember> Members = new();
        public List<(string Name, string Uuid)> Existing = new();      // precedence (1): the STP TO's children
        public List<string> RequiredChildren = new();                  // precedence (1): each must be Published
        public Dictionary<string, string> CreatedUuid = new(StringComparer.Ordinal);
        public List<string> AttachedNames = new();
        public int Expected;
        public int LastPublished = -1;
        public DateTime StartedUtc, AttachDeadline, Deadline, IssuedUtc, AttachedUtc, EndedUtc;
        public string Reason = "";
        public string Source = "";
        public long Generation;
        public TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly object _lock = new();
    private readonly Dictionary<string, Pop> _byContainer = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _containerOfMember = new(StringComparer.Ordinal);
    private long _generation;

    /// <summary>Is any population still waiting on the back end (a tick phase guard - costs nothing otherwise)?</summary>
    public bool HasActive
    {
        get
        {
            lock (_lock)
                return _byContainer.Values.Any(p => p.Stage is PopulateStage.Planned or PopulateStage.Creating
                                                            or PopulateStage.Attached);
        }
    }

    public bool IsMember(string name, out string container)
    {
        container = null;
        if (string.IsNullOrEmpty(name)) return false;
        lock (_lock) return _containerOfMember.TryGetValue(name, out container);
    }

    public bool Knows(string container)
    {
        if (string.IsNullOrEmpty(container)) return false;
        lock (_lock) return _byContainer.ContainsKey(container);
    }

    public PopulateStage StageOf(string container)
    {
        lock (_lock) return _byContainer.TryGetValue(container ?? "", out var p) ? p.Stage : PopulateStage.Refused;
    }

    /// <summary>The members' names and uuids known so far (the console channel opens them).</summary>
    public IReadOnlyList<(string Name, string Uuid)> MembersOf(string container)
    {
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container ?? "", out var p)) return Array.Empty<(string, string)>();
            var outp = p.Members.Select(m => (m.Name, p.CreatedUuid.TryGetValue(m.Name, out var u) ? u : "")).ToList();
            outp.AddRange(p.Existing);
            return outp;
        }
    }

    /// <summary>
    /// M3 (RL-20260928-03): the members this container's population ATTACHED, in attach order, with the uuid their
    /// ObjectCreated bound; FromTo = an existing STP TO sub-container (precedence 1), not a created warfare-model unit.
    /// Empty until the population has attached. The per-member planners task exactly these.
    /// </summary>
    public IReadOnlyList<(string Name, string Uuid, bool FromTo)> AttachedMembersOf(string container)
    {
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container ?? "", out var p)) return Array.Empty<(string, string, bool)>();
            var outp = new List<(string, string, bool)>(p.AttachedNames.Count);
            foreach (var name in p.AttachedNames)
            {
                var existing = p.Existing.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.Ordinal));
                if (existing.Name != null) outp.Add((name, existing.Uuid ?? "", true));
                else outp.Add((name, p.CreatedUuid.TryGetValue(name, out var u) ? u ?? "" : "", false));
            }
            return outp;
        }
    }

    /// <summary>The gate RunTaskAsync awaits (completed on Published, Refused or TimedOut - never left hanging).</summary>
    public Task GateOf(string container)
    {
        lock (_lock) return _byContainer.TryGetValue(container ?? "", out var p) ? p.Gate.Task : Task.CompletedTask;
    }

    /// <summary>Start a table/catalogue population (Planned). Returns the generation the service carries to Issued,
    /// so a late slot-check worker cannot issue creates for a population that has already ended.</summary>
    public long Begin(string container, string containerUuid, IReadOnlyList<PopulateMember> members, string source,
                      DateTime now, double timeoutSeconds, double publicationWindowSeconds)
    {
        lock (_lock)
        {
            var p = new Pop
            {
                Name = container, Uuid = containerUuid ?? "", Stage = PopulateStage.Planned,
                Members = members?.ToList() ?? new List<PopulateMember>(), Source = source ?? "",
                StartedUtc = now, Generation = ++_generation,
            };
            SetDeadlines(p, now, timeoutSeconds, publicationWindowSeconds);
            _byContainer[container] = p;
            foreach (var m in p.Members) _containerOfMember[m.Name] = container;
            return p.Generation;
        }
    }

    /// <summary>Precedence (1): the container's declared STP TO subordinates exist as containers. Registered here
    /// (any thread); <see cref="AttachExisting"/> attaches them on the tick thread. Each child is populated on its own
    /// by the same rules; the parent's move needs every child <see cref="PopulateStage.Published"/>.</summary>
    public long BeginExisting(string container, string containerUuid, IReadOnlyList<(string Name, string Uuid)> children,
                              DateTime now, double timeoutSeconds)
    {
        lock (_lock)
        {
            var p = new Pop
            {
                Name = container, Uuid = containerUuid ?? "", Stage = PopulateStage.Planned, Source = "the STP TO",
                StartedUtc = now, Generation = ++_generation,
                Existing = children?.ToList() ?? new List<(string, string)>(),
            };
            p.RequiredChildren = p.Existing.Select(c => c.Name).ToList();
            SetDeadlines(p, now, timeoutSeconds, 0.0);
            _byContainer[container] = p;
            if (p.Existing.Count == 0)
                End(p, PopulateStage.Refused, now, "POPULATE REFUSED: none of its declared STP TO subordinates was created");
            return p.Generation;
        }
    }

    /// <summary>Tick thread: attach the STP TO's existing subordinates AGAIN, in the declared order - addToOrganization
    /// detaches from any prior superior first (vrfRemoteController.h:1334-1339), so this restores the declared order
    /// and covers an init composition that timed out - then wait for the container to publish them.</summary>
    public IReadOnlyList<PopulateEvent> AttachExisting(string container, long generation, DateTime now,
                                                       IContainerBridge bridge)
    {
        var ev = new List<PopulateEvent>();
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container ?? "", out var p) || p.Generation != generation
                || p.Stage != PopulateStage.Planned || p.Existing.Count == 0) return ev;
            foreach (var c in p.Existing) bridge.AddToOrganization(c.Uuid, p.Uuid);
            p.AttachedNames = p.Existing.Select(c => c.Name).ToList();
            p.Expected = p.Existing.Count;
            p.AttachedUtc = now;
            p.Stage = PopulateStage.Attached;
            ev.Add(new PopulateEvent(false,
                $"POPULATE {container}: its {p.Expected} declared STP TO subordinate(s) [{string.Join(", ", p.AttachedNames)}] " +
                "are ATTACHED in the declared order (existing containers - never re-created, RL-20260927-03); each is " +
                "populated by the same rules, and this container waits for all of them and for its own publication. " +
                "NESTED: its move sends pa_move_along_route to each sub-container (PA_Move_Along_Route.lua:65-70), whose " +
                "final retrograde step no vendor plan exercises (design sec 4.4)."));
        }
        return ev;
    }

    /// <summary>No composition (or no plan): the container stays EMPTY; the gate completes at once so no task waits on
    /// it, and a MOVE on it is refused with this reason.</summary>
    public void Refuse(string container, string containerUuid, string reason, DateTime now)
    {
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container, out var p))
            {
                p = new Pop { Name = container, Uuid = containerUuid ?? "", StartedUtc = now, Generation = ++_generation };
                _byContainer[container] = p;
            }
            End(p, PopulateStage.Refused, now, reason);
        }
    }

    /// <summary>The service issued the member creates (tick thread). False = the population ended meanwhile (its
    /// gate timed out), and the caller must NOT create anything.</summary>
    public bool MarkIssued(string container, long generation, DateTime now)
    {
        lock (_lock)
        {
            if (!_byContainer.TryGetValue(container, out var p) || p.Generation != generation
                || p.Stage != PopulateStage.Planned) return false;
            p.Stage = PopulateStage.Creating;
            p.IssuedUtc = now;
            return true;
        }
    }

    /// <summary>A member's ObjectCreated (tick thread). When the LAST member arrives every member is attached, in
    /// planned order, and the population waits for publication.</summary>
    public IReadOnlyList<PopulateEvent> OnMemberCreated(string memberName, string memberUuid, DateTime now,
                                                        IContainerBridge bridge)
    {
        var ev = new List<PopulateEvent>();
        lock (_lock)
        {
            if (!_containerOfMember.TryGetValue(memberName ?? "", out var container)
                || !_byContainer.TryGetValue(container, out var p)) return ev;
            p.CreatedUuid[memberName] = memberUuid ?? "";
            if (p.Stage != PopulateStage.Creating)
            {
                ev.Add(new PopulateEvent(true,
                    $"POPULATE {container}: member {memberName} ({memberUuid}) was created AFTER the population ended " +
                    $"({p.Stage}: {p.Reason}) - it is NOT attached and takes no part in any task."));
                return ev;
            }
            if (p.CreatedUuid.Count < p.Members.Count) return ev;
            Attach(p, now, bridge, ev, partial: false);
        }
        return ev;
    }

    /// <summary>The tick phase (tick thread): the attach deadline, the publication read and the gate's bound.</summary>
    public IReadOnlyList<PopulateEvent> Sweep(DateTime now, IContainerBridge bridge)
    {
        var ev = new List<PopulateEvent>();
        lock (_lock)
        {
            foreach (var p in _byContainer.Values.ToList())
            {
                switch (p.Stage)
                {
                    case PopulateStage.Planned:
                        if (now >= p.AttachDeadline)
                            TimeOut(p, now, ev, "its member creates were never issued (the slot check did not finish)");
                        break;
                    case PopulateStage.Creating:
                        if (now < p.AttachDeadline) break;
                        if (p.CreatedUuid.Count == 0)
                            TimeOut(p, now, ev, $"NONE of its {p.Members.Count} member(s) was created");
                        else
                            Attach(p, now, bridge, ev, partial: true);
                        break;
                    case PopulateStage.Attached:
                        int published = bridge.PublishedSubordinateCount(p.Uuid);
                        p.LastPublished = published;
                        if (published >= p.Expected && p.Expected > 0)
                        {
                            End(p, PopulateStage.Published, now, "");
                            ev.Add(new PopulateEvent(false,
                                Inv($"POPULATE {p.Name}: the container PUBLISHES {published} subordinate(s) (expected {p.Expected}) ") +
                                Inv($"{(now - p.StartedUtc).TotalSeconds:F1} s after the population began, ") +
                                Inv($"{(now - p.AttachedUtc).TotalSeconds:F1} s after the attach - READY FOR TASKING ") +
                                "(RL-20260927-03)."));
                        }
                        else if (now >= p.Deadline)
                            TimeOut(p, now, ev, published < 0
                                ? $"its published subordinate list could NOT BE READ (expected {p.Expected})"
                                : $"the container published {published} of {p.Expected} attached subordinate(s)");
                        break;
                }
            }
        }
        return ev;
    }

    /// <summary>May a MOVE be issued to this container? Only when it has PUBLISHED its members (and, for a container
    /// populated through its STP TO, when every such subordinate has too).</summary>
    public ContainerMoveVerdict MoveVerdict(string container, DateTime now)
    {
        lock (_lock) return VerdictLocked(container, now, 0);
    }

    private ContainerMoveVerdict VerdictLocked(string container, DateTime now, int depth)
    {
        if (!_byContainer.TryGetValue(container ?? "", out var p))
            return new ContainerMoveVerdict(false, 0,
                $"container {container} was never populated - no order referenced it as a performer, so it has NO " +
                "members (RL-20260927-03)");
        switch (p.Stage)
        {
            case PopulateStage.Published:
                if (depth < CompositionResolver.MaxDepth)
                    foreach (var child in p.RequiredChildren)
                    {
                        var cv = VerdictLocked(child, now, depth + 1);
                        if (!cv.Ready)
                            return new ContainerMoveVerdict(false, 0,
                                $"its STP TO subordinate {child} is not populated: {cv.Reason}");
                    }
                return new ContainerMoveVerdict(true, Math.Max(p.Expected, p.AttachedNames.Count), "");
            case PopulateStage.Refused:
            case PopulateStage.TimedOut:
                return new ContainerMoveVerdict(false, 0, p.Reason);
            default:
                return new ContainerMoveVerdict(false, 0,
                    Inv($"its population is still {p.Stage} {(now - p.StartedUtc).TotalSeconds:F0} s after it began - it ") +
                    "has published no members yet");
        }
    }

    /// <summary>Issue a scripted move ONLY to a container that may take one (the single place the move is sent, so
    /// a memberless container can never be tasked). Returns the verdict.</summary>
    public ContainerMoveVerdict TryIssueScriptedMove(string container, string containerUuid, string scriptId,
                                                     IReadOnlyList<ContainerTaskVar> vars, DateTime now,
                                                     IContainerBridge bridge)
    {
        var v = MoveVerdict(container, now);
        if (v.Ready) bridge.RunScriptedTask(containerUuid, scriptId, vars);
        return v;
    }

    /// <summary>
    /// M3 (RL-20260928-03): a PER-MEMBER planned vertex - the same scripted task to each member, by the member's uuid, and
    /// ONLY while the container may take a move (the same single gate as <see cref="TryIssueScriptedMove"/>). Returns the
    /// verdict; nothing is issued when it is not Ready.
    /// </summary>
    public ContainerMoveVerdict TryIssueMemberMoves(string container, IReadOnlyList<string> memberUuids, string scriptId,
                                                    IReadOnlyList<ContainerTaskVar> vars, DateTime now, IContainerBridge bridge)
    {
        var v = MoveVerdict(container, now);
        if (!v.Ready) return v;
        foreach (string memberUuid in memberUuids ?? Array.Empty<string>())
            bridge.RunScriptedTask(memberUuid, scriptId, vars);
        return v;
    }

    private static void SetDeadlines(Pop p, DateTime now, double timeoutSeconds, double publicationWindowSeconds)
    {
        double t = Math.Max(1.0, double.IsFinite(timeoutSeconds) ? timeoutSeconds : 1.0);
        double w = Math.Clamp(double.IsFinite(publicationWindowSeconds) ? publicationWindowSeconds : 0.0, 0.0, t * 0.5);
        p.Deadline = now.AddSeconds(t);
        p.AttachDeadline = now.AddSeconds(t - w);
    }

    private void Attach(Pop p, DateTime now, IContainerBridge bridge, List<PopulateEvent> ev, bool partial)
    {
        var missing = new List<string>();
        foreach (var m in p.Members)                     // PLANNED order: the first listed (the HQ) is attached first
        {
            if (p.CreatedUuid.TryGetValue(m.Name, out var uuid) && !string.IsNullOrEmpty(uuid))
            {
                bridge.AddToOrganization(uuid, p.Uuid);
                p.AttachedNames.Add(m.Name);
            }
            else missing.Add(m.Name);
        }
        p.Expected = p.AttachedNames.Count;
        p.AttachedUtc = now;
        p.Stage = PopulateStage.Attached;
        if (partial)
            ev.Add(new PopulateEvent(true,
                Inv($"POPULATE {p.Name}: only {p.AttachedNames.Count} of {p.Members.Count} member(s) were created ") +
                Inv($"{(now - p.StartedUtc).TotalSeconds:F0} s after the population began - the {p.AttachedNames.Count} ") +
                $"that exist are ATTACHED, the rest are NOT part of this unit: [{string.Join(", ", missing)}]. Waiting " +
                "for the container to publish them."));
        else
            ev.Add(new PopulateEvent(false,
                $"POPULATE {p.Name}: {p.Members.Count} of {p.Members.Count} member(s) created; AddToOrganization " +
                $"issued for all of them in planned order ({p.Members.FirstOrDefault()?.Name} first) - waiting for the " +
                Inv($"container to PUBLISH them (gate ends {(p.Deadline - now).TotalSeconds:F0} s from now).")));
    }

    private static string Inv(FormattableString f) => FormattableString.Invariant(f);

    private static void TimeOut(Pop p, DateTime now, List<PopulateEvent> ev, string why)
    {
        End(p, PopulateStage.TimedOut, now, FormattableString.Invariant(
            $"POPULATE TIMED OUT after {(now - p.StartedUtc).TotalSeconds:F0} s: {why}"));
        ev.Add(new PopulateEvent(true,
            $"POPULATE {p.Name}: {p.Reason} - its MOVE tasks are REFUSED (TASKABRT), never dispatched to an empty " +
            "container (RL-20260927-03)."));
    }

    private static void End(Pop p, PopulateStage stage, DateTime now, string reason)
    {
        p.Stage = stage;
        p.Reason = reason ?? "";
        p.EndedUtc = now;
        p.Gate.TrySetResult();
    }
}

/// <summary>The start-up arithmetic and wording of C1 (pure, so --populate-selftest can pin it).</summary>
public static class ContainerStartup
{
    /// <summary>Slack on top of the stage bounds, WALL seconds.</summary>
    public const double PopulateSlackSeconds = 15.0;

    /// <summary>Vrf:ContainerPopulateTimeoutSeconds in force: the configured value when positive, else DERIVED from
    /// the bounds of the stages a population goes through - the slot check's worker (the route shift's own bound),
    /// the member placement query, the creates' round trip and the publication window (one CompositionTimeoutSeconds
    /// each) - plus <see cref="PopulateSlackSeconds"/>. 30 + 10 + 2 x 15 + 15 = 85 s at the shipped values.</summary>
    public static double PopulateTimeoutSeconds(double configured, double routeShiftTimeoutSeconds,
                                                double terrainProfileTimeoutSeconds, double compositionTimeoutSeconds)
    {
        if (double.IsFinite(configured) && configured > 0.0) return configured;
        static double Pos(double v) => double.IsFinite(v) && v > 0.0 ? v : 0.0;
        return Pos(routeShiftTimeoutSeconds) + Pos(terrainProfileTimeoutSeconds) + 2.0 * Math.Max(1.0, Pos(compositionTimeoutSeconds))
               + PopulateSlackSeconds;
    }

    /// <summary>The one line an ENTITY-level run says about C1 (said in both states - absence is not evidence of
    /// off - the house style of VertexChainPolicy.StartupLine and UnitPositionPolicy.StartupLine).</summary>
    public static string OffLine(string modelSetRaw)
        => "AGGREGATE CONTAINERS off (Vrf:ModelSet=" + (string.IsNullOrWhiteSpace(modelSetRaw) ? "(not set)" : modelSetRaw.Trim()) +
           " -> EntityLevel): units are created and tasked exactly as before C1 - shells + platforms under " +
           "CreationPolicy=AtOrder, every aggregate Disaggregated. Populated Aggregate Containers (RL-20260927-02, " +
           "RL-20260927-03, RL-20260927-04) apply ONLY on Vrf:ModelSet=AggregateTacticalLevel.";
}

/// <summary>
/// D-6 (RL-20260927-04: "withhold a short/vacuous container completion"): the vendor's completion of a container's
/// scripted MOVE is only its script's word that every snapshot subordinate finished (PA_Move_Along_Route.lua:152-155) -
/// an EMPTY snapshot finishes on the first tick. When it arrives with the container's own centroid (D1: the position
/// the vendor publishes for it, the unweighted mean of its subordinates) farther than the vacuous radius
/// (Vrf:VertexArrivalRadiusMeters, the vertex chain's bar - the same arrival scale, no new number) from the route's
/// final vertex, TASKCMPLT is WITHHELD: the task stays in flight, arrival evidence and the start time + Duration rule
/// decide (RL-20260921-09), and a unit that never gets there is the progress watchdog's (RL-20260913-03). The
/// distance test is VertexChainPolicy.IsVacuous: an unreadable centroid (NaN) is never "short" - nothing is claimed
/// that was not measured.
/// </summary>
public static class ContainerCompletionPolicy
{
    public enum Verdict { HandOn, WithholdShort }

    public static Verdict Decide(bool success, bool hasDestination, double distanceMeters, double radiusMeters)
        => success && hasDestination && VertexChainPolicy.IsVacuous(distanceMeters, radiusMeters)
            ? Verdict.WithholdShort : Verdict.HandOn;

    /// <summary>The WARN line's core sentence - the G1 prereg greps "completed short:".</summary>
    public static string ShortText(double distanceMeters)
        => FormattableString.Invariant($"completed short: {distanceMeters:F0} m from the route end");
}
