using System.Collections.Concurrent;

namespace VrfC2SimApp;

/// <summary>
/// STP-822 part 2 / STP-823: WHAT THE INTERFACE CAN KNOW ABOUT NAVIGATION AREAS, and what it
/// cannot.
///
/// *** THIS IS NOT A CRASH GUARD - THE CAUSE STATEMENT IT WAS BUILT ON IS WITHDRAWN. ***
/// It was built while V6d's reading stood (V6_LIVE_JOIN_GATE secs 9.3-9.6): on a fixture with no
/// navigation area a ground move-along STOPPED the back end, its console ending at
/// `Is current point in nav area?` -> FALSE -> `Plan off feature path` -> `Plan path` ->
/// `Checking status of job for M1A2 10`. **V6e (run 20260915T135636Z, sec 11) FALSIFIED that**:
/// the same order on the same MojaveAO20 area with that condition answering TRUE stopped the back
/// end at the identical point - `Calc off road nav path part` -> one poll -> silence - and the
/// stopped state is a RUNAWAY ALLOCATION (~2.2 GB/min at under one core), independent of nav data
/// and of console level. Nav data only decided WHICH path job was started.
///
/// WHAT SURVIVES, and all this gate now claims: without a navigation area a ground move is
/// planned by the FEATURE planner on one straight part, silently (G7B_G8_RESULTS sec 1.5). That
/// is a fidelity precondition an operator may choose to refuse on - which is why the setting
/// ships OFF and the default is the user's.
///
/// THE EVIDENCE THAT EXISTS, IN BAND, WITH NO NATIVE CHANGE. The interface already subscribes to
/// the VR-Forces OBJECT CONSOLE - `DtVrfRemoteController::addObjectConsoleMessageCallback`
/// (vrfRemoteController.h:1970) -> `VrfFacade::OnObjectConsoleMessage` ->
/// `VrfBridge.ObjectConsoleMessage` -> `VrfC2SimService.OnVrfObjectConsoleMessage` - and opens
/// each created object's console at Vrf:ObjectConsoleNotifyLevel (members at
/// Vrf:ObjectConsoleMemberNotifyLevel). Two rows on that channel bear on navigation:
///   POSITIVE  "New Primary nav area: | &lt;area&gt;" - the simulator's own acquisition of a
///             navigation area for that object. Printed at object-console level &gt;= 3. It is the
///             same row the RUNNER's stage-7d READY gate polls out of vrfc2simapp.log
///             (scripts/RunnerLib.ps1 Get-NavAreaRows), so this reads our own log's source
///             rather than a new channel.
///   NEGATIVE  "Is current point in nav area?" / "fail in action Is current point in nav area?" -
///             the behaviour-tree condition itself, at level 4. It arrives DURING execution, so
///             it can never gate the dispatch that produced it; it is recorded as corroboration
///             for the NEXT one and named in the refusal text.
///
/// THE EVIDENCE THAT DOES NOT EXIST. There is NO nav-area query a remote controller can issue.
/// `vrfcontrol/vrfRemoteController.h` has no navigation accessor at all (its only "nav" matches
/// are IFF/ATC navaid parameters); `navigationAreasManager.h` lives in `vrfGuiCore` - the
/// front-end GUI library, which this process does not link and which is not a remote-control
/// API; `vrfNavigation/navArea.h` and the rest of that module are the back-end/generator side.
/// The terrain-profile reply carries elevations, not navigability. So the console rows above are
/// the only in-band evidence, and this class is honest about their limit.
///
/// *** THE LIMITATION, STATED PLAINLY ***
///   1. A "New Primary nav area" row proves an area was acquired BY THAT OBJECT. The runner's own
///      note records that any object's row is what its gate accepts, and it has never been
///      established that EVERY platform prints one. So the refusal below fires only when NO
///      object has printed one at all - which is exactly the V6d case (no nav data anywhere) and
///      is deliberately conservative against refusing a task on a fixture that does have nav
///      data. It does NOT prove the taskee's own start point is inside an area; the report says
///      which of the two was seen.
///   2. It can see nothing at all below object-console level 3. With the consoles lower (the
///      shipped default is OFF, -1), the gate cannot distinguish "no nav area" from "not
///      watching", so it WARNS ONCE and dispatches rather than refusing on ignorance.
/// </summary>
public sealed class NavAreaEvidence
{
    /// <summary>The object-console level at which "New Primary nav area" is printed
    /// (scripts/RunnerLib.ps1: "The rows only exist at object-console level &gt;= 3, which is why
    /// the runner refuses -PreOrderGate with the console below that").</summary>
    public const int MinLevelForAreaRow = 3;

    private const string AreaRowMarker = "New Primary nav area";
    private const string NotInAreaMarker = "Is current point in nav area?";

    // uuid -> wall seconds of that object's most recent area row / most recent failed condition.
    private readonly ConcurrentDictionary<string, double> _areaRowWall = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, double> _notInAreaWall = new(StringComparer.Ordinal);
    // uuid -> the highest console level this interface ASKED the sim to open for that object.
    private readonly ConcurrentDictionary<string, int> _consoleLevel = new(StringComparer.Ordinal);
    private double _anyAreaRowWall = double.NaN;
    private string _lastArea = "";

    /// <summary>Record that this interface requested a console level for an object. "Could we have
    /// seen the row?" is answered from these, not from the settings, because the settings say what
    /// was CONFIGURED and this says what was actually asked for, per object.</summary>
    public void ConsoleOpened(string uuid, int level)
    {
        if (string.IsNullOrEmpty(uuid)) return;
        _consoleLevel.AddOrUpdate(uuid, level, (_, old) => Math.Max(old, level));
    }

    /// <summary>Admit one decoded object-console row. Cheap ordinal Contains first: this is on the
    /// tick thread and sees every console row of the run (162,787 of them on A4).</summary>
    public void Observe(string uuid, string decodedText, double wallSeconds)
    {
        if (string.IsNullOrEmpty(decodedText)) return;
        if (decodedText.Contains(AreaRowMarker, StringComparison.Ordinal))
        {
            if (!string.IsNullOrEmpty(uuid)) _areaRowWall[uuid] = wallSeconds;
            _anyAreaRowWall = wallSeconds;
            int bar = decodedText.LastIndexOf('|');
            _lastArea = bar >= 0 && bar + 1 < decodedText.Length
                      ? decodedText[(bar + 1)..].Trim() : decodedText.Trim();
        }
        else if (decodedText.Contains(NotInAreaMarker, StringComparison.Ordinal)
                 && !string.IsNullOrEmpty(uuid))
            _notInAreaWall[uuid] = wallSeconds;
    }

    /// <summary>The verdict for ONE dispatch.</summary>
    /// <param name="TaskeeEvidence">One of the taskee's own uuids printed an area row in window.</param>
    /// <param name="AnyEvidence">SOME object printed one in window (what the gate acts on).</param>
    /// <param name="CanSee">At least one of the taskee's uuids has a console open at level &gt;= 3,
    /// so the absence of a row is informative rather than an artefact of not watching.</param>
    /// <param name="NotInAreaSeen">One of the taskee's uuids has FAILED the nav-area condition
    /// since its last area row - direct, if late, corroboration.</param>
    /// <param name="Area">The last area name seen, for the log.</param>
    public readonly record struct Verdict(bool TaskeeEvidence, bool AnyEvidence, bool CanSee,
                                          bool NotInAreaSeen, string Area);

    /// <summary>
    /// Decide for a taskee, from the uuids that represent it in the simulation (its own object
    /// plus its members - an aggregate's members are what actually drive).
    /// </summary>
    public Verdict Decide(IEnumerable<string> uuids, double wallNow, double evidenceSeconds)
    {
        bool taskee = false, canSee = false, notInArea = false;
        double window = Math.Max(0.0, evidenceSeconds);
        foreach (var u in uuids ?? Array.Empty<string>())
        {
            if (string.IsNullOrEmpty(u)) continue;
            if (_consoleLevel.TryGetValue(u, out var lvl) && lvl >= MinLevelForAreaRow) canSee = true;
            bool fresh = _areaRowWall.TryGetValue(u, out var aw) && (wallNow - aw) <= window;
            if (fresh) taskee = true;
            if (_notInAreaWall.TryGetValue(u, out var nw) && (!_areaRowWall.TryGetValue(u, out var aw2) || nw > aw2))
                notInArea = true;
        }
        bool any = taskee
                   || (!double.IsNaN(_anyAreaRowWall) && (wallNow - _anyAreaRowWall) <= window);
        return new Verdict(taskee, any, canSee, notInArea, _lastArea);
    }

    /// <summary>
    /// THE GATE, as a pure function so the policy is testable without a bridge:
    /// refuse only when the interface COULD have seen an area row and NONE arrived.
    /// </summary>
    public static bool ShouldRefuse(Verdict v) => v.CanSee && !v.AnyEvidence;

    /// <summary>
    /// The one line an operator reads when the gate blocks a task, and the ObservationReport's
    /// Marking. Names the unit, the window, and whether the taskee's OWN console said it was not
    /// in an area - so the honest distinction of note 1 above is in the artefact, not only here.
    /// </summary>
    public static string RefusalMarking(string unitName, double evidenceSeconds, Verdict v)
        => $"NAV-AREA GATE (Vrf:RequireNavAreaForGroundTasks): no navigation area evidence for " +
           $"{unitName} - no VR-Forces object has printed a \"New Primary nav area\" row in the last " +
           $"{evidenceSeconds:F0} s, with this unit's object console open at level >= " +
           $"{MinLevelForAreaRow}." +
           (v.NotInAreaSeen ? " Its own console has already FAILED \"Is current point in nav area?\"." : "") +
           " Without a navigation area this move would be planned by the FEATURE planner on one " +
           "straight part (STP-823 - note that ticket's \"it stops the back end\" cause statement " +
           "was WITHDRAWN by V6e), so the task is refused instead of issued.";

    /// <summary>The WARNING wording for the case the gate cannot judge: consoles too low to ever
    /// print the row. Said once per run - it is a configuration fact, not a per-task event.</summary>
    public static string BlindWarning(int configuredLevel)
        => "Vrf:RequireNavAreaForGroundTasks is ON but the object console is at level " +
           $"{configuredLevel} - the \"New Primary nav area\" row is only printed at level >= " +
           $"{MinLevelForAreaRow}, so this interface CANNOT tell a fixture with no navigation area " +
           "from one it is not watching. Ground tasks are DISPATCHED unchecked. Set " +
           $"Vrf:ObjectConsoleNotifyLevel >= {MinLevelForAreaRow} (and Vrf:ObjectConsoleMemberNotifyLevel " +
           "for an aggregate's members) to arm the gate.";
}
