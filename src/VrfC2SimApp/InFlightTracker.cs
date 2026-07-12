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
    /// <summary>One dispatched-and-not-yet-completed task.</summary>
    public readonly record struct InFlight(string TaskUuid, string TaskName, string ExpectedKind,
                                           DateTime DispatchedUtc);

    private readonly ConcurrentDictionary<string, InFlight> _byUnitName = new();

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
