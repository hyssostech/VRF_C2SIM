namespace VrfC2SimApp;

/// <summary>
/// R10 subordinate fan-out bookkeeping (docs/UNIT_MOVEMENT_RESEARCH.md sec 4c). When an
/// aggregate's move is fanned out to its member entities, the VRF completion callbacks
/// arrive PER MEMBER (marking = the member entity's name, e.g. "M1A2 33") and carry no
/// task uuid. This tracker maps member completions back to the owning unit's task and
/// says when ALL members have completed - the moment the unit-level TASKCMPLT is due.
/// PURE (no bridge/logging - offline-testable via --fanout-selftest); thread-safe (the
/// tick thread registers, the completion callback completes - same thread today, but
/// the lock keeps that an implementation detail).
/// </summary>
public sealed class FanOutTracker
{
    private sealed class FanOut
    {
        public required string UnitName;
        public required string TaskUuid;
        public required HashSet<string> Pending;
        public int Total;
    }

    private readonly object _lock = new();
    private readonly Dictionary<string, FanOut> _byUnit = new();       // unit -> active fan-out
    private readonly Dictionary<string, string> _unitByMember = new(); // member name -> unit

    /// <summary>
    /// Start tracking a fan-out for <paramref name="unitName"/>. REPLACES any active
    /// fan-out for the unit (a retask supersedes - VRF replaces the members' tasks too).
    /// Returns the number of members registered (0 = nothing registered; caller should
    /// not have fanned out).
    /// </summary>
    public int Register(string unitName, string taskUuid, IEnumerable<string> memberNames)
    {
        lock (_lock)
        {
            CancelLocked(unitName);
            var pending = new HashSet<string>(
                memberNames.Where(n => !string.IsNullOrEmpty(n)), StringComparer.Ordinal);
            if (pending.Count == 0) return 0;
            var f = new FanOut { UnitName = unitName, TaskUuid = taskUuid, Pending = pending, Total = pending.Count };
            _byUnit[unitName] = f;
            foreach (var m in pending) _unitByMember[m] = unitName;
            return pending.Count;
        }
    }

    /// <summary>
    /// If <paramref name="memberName"/> belongs to an active fan-out, mark it complete.
    /// Returns true with the owning unit + task; <paramref name="allDone"/> flags the
    /// LAST member (the fan-out is then removed - the caller synthesizes the unit-level
    /// completion). A name that is not a pending fan-out member returns false (normal
    /// unit-level completions flow through the caller's existing path).
    /// </summary>
    public bool TryCompleteMember(string memberName, out string unitName, out string taskUuid,
                                  out int remaining, out bool allDone)
    {
        unitName = ""; taskUuid = ""; remaining = 0; allDone = false;
        if (string.IsNullOrEmpty(memberName)) return false;
        lock (_lock)
        {
            if (!_unitByMember.TryGetValue(memberName, out var unit)) return false;
            if (!_byUnit.TryGetValue(unit, out var f)) { _unitByMember.Remove(memberName); return false; }
            if (!f.Pending.Remove(memberName)) return false;
            _unitByMember.Remove(memberName);
            unitName = f.UnitName;
            taskUuid = f.TaskUuid;
            remaining = f.Pending.Count;
            allDone = f.Pending.Count == 0;
            if (allDone) _byUnit.Remove(unit);
            return true;
        }
    }

    /// <summary>Drop the unit's active fan-out (supersession/cleanup). True if one existed.</summary>
    public bool Cancel(string unitName)
    {
        lock (_lock) { return CancelLocked(unitName); }
    }

    private bool CancelLocked(string unitName)
    {
        if (!_byUnit.Remove(unitName, out var f)) return false;
        foreach (var m in f.Pending) _unitByMember.Remove(m);
        return true;
    }
}
