using System.Collections.Concurrent;

namespace VrfC2SimApp;

/// <summary>
/// Tracks, per unit, the ONE task VR-Forces is currently executing for it (VRF runs a
/// single task at a time; dispatching a new task REPLACES the in-flight one). Written at
/// dispatch, consumed at completion, so a VRF task-complete callback - which carries only
/// the unit marking + a task-type string, NOT the task uuid - is attributed to the task
/// actually in flight, not to whatever task was dispatched LAST (the completion-
/// misattribution defect, NEXT_SESSION_GUIDANCE.md sec 2.4 DEFECT A: with the old
/// last-write map, a completion almost always named a LATER task's uuid in the TASKCMPLT
/// report and released the wrong successor's gate).
/// Pure (no bridge / MAK) so it is unit-testable offline (--sequencer-selftest).
/// </summary>
public sealed class InFlightTracker
{
    /// <summary>One dispatched-and-not-yet-completed task. DestLat/DestLon (degrees) = the move's
    /// last vertex when the task is a move, for the arrival-evidence completion (ArrivalPolicy);
    /// null for tasks without a destination (patrol, engage).
    /// RouteLengthMeters / StartLat / StartLon (STP-837) describe the JOURNEY, not just its end:
    /// the great-circle length of the route as dispatched, and the position the taskee was
    /// dispatched from. ArrivalPolicy needs both - the length sets the arrival radius and the
    /// traversal bar, and the start is what says whether the last vertex is far enough from it
    /// for arriving there to mean anything at all. NaN/null = "not a move, or not recorded",
    /// which degrades to the pre-STP-837 tolerances rather than to a stricter unasked-for rule.</summary>
    public readonly record struct InFlight(string TaskUuid, string TaskName, string ExpectedKind,
                                           DateTime DispatchedUtc, double? DestLat = null, double? DestLon = null,
                                           string TaskeeUuid = "",
                                           double RouteLengthMeters = double.NaN,
                                           double? StartLat = null, double? StartLon = null);

    private readonly ConcurrentDictionary<string, InFlight> _byUnitName = new();

    /// <summary>A snapshot of every in-flight (unit name, record) for the arrival monitor.</summary>
    public IReadOnlyList<KeyValuePair<string, InFlight>> Snapshot() => _byUnitName.ToArray();

    /// <summary>
    /// Record that a task was dispatched to a unit. Returns the record it SUPERSEDED (the
    /// previously in-flight task, which VRF abandons on retask - its completion will never
    /// arrive and its successors' gates must NOT be released) or null if the unit was idle.
    /// </summary>
    public InFlight? RecordDispatch(string unitName, InFlight rec)
    {
        InFlight? superseded = null;
        _byUnitName.AddOrUpdate(unitName, rec, (_, old) => { superseded = old; return rec; });
        return superseded;
    }

    /// <summary>Attribute a completion: pops the unit's in-flight record. False if idle.</summary>
    public bool TryComplete(string unitName, out InFlight completed)
        => _byUnitName.TryRemove(unitName, out completed);

    /// <summary>Whether the unit has a task in flight (the whenIdle timeout policy).</summary>
    public bool IsBusy(string unitName) => _byUnitName.ContainsKey(unitName);

    /// <summary>Peek at the unit's in-flight record without popping it.</summary>
    public bool TryGetCurrent(string unitName, out InFlight current)
    {
        current = default;
        return !string.IsNullOrEmpty(unitName) && _byUnitName.TryGetValue(unitName, out current);
    }

    /// <summary>
    /// Pop the unit's in-flight record ONLY IF it is still the named task (m9 of the cold-start
    /// review of 5c67d41). The R4 timed completion needs this: a task that ended because its time
    /// was up must release the unit, but by the time the walk runs the unit may already have been
    /// re-tasked, and popping THAT record would make a live task invisible to the arrival monitor,
    /// the progress watchdog and the whenIdle policy. The compare-and-remove is atomic, so a
    /// concurrent vendor completion and this call cannot both succeed.
    /// </summary>
    public bool TryCompleteIfCurrent(string unitName, string taskUuid, out InFlight completed)
    {
        completed = default;
        if (string.IsNullOrEmpty(unitName) || string.IsNullOrEmpty(taskUuid)) return false;
        if (!_byUnitName.TryGetValue(unitName, out var current)) return false;
        if (!string.Equals(current.TaskUuid, taskUuid, StringComparison.Ordinal)) return false;
        if (!_byUnitName.TryRemove(new KeyValuePair<string, InFlight>(unitName, current))) return false;
        completed = current;
        return true;
    }

    /// <summary>
    /// Loose sanity check of a VRF completion's task-type string (e.g. "move-along")
    /// against the kind we dispatched. Deliberately tolerant - the exact VRF type strings
    /// are not knowable offline, so unknown/empty strings never flag. Attribution does NOT
    /// depend on this; it only powers an anomaly log line.
    /// </summary>
    public static bool KindLooksRight(string expectedKind, string vrfTaskType)
    {
        if (string.IsNullOrEmpty(expectedKind) || string.IsNullOrEmpty(vrfTaskType)) return true;
        string token = expectedKind.Split('-', ' ')[0];
        return vrfTaskType.Contains(token, StringComparison.OrdinalIgnoreCase);
    }
}
