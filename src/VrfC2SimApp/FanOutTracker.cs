namespace VrfC2SimApp;

/// <summary>
/// R10 subordinate fan-out bookkeeping (docs/UNIT_MOVEMENT_RESEARCH.md sec 4c). When an
/// aggregate's move is fanned out to its member entities, the VRF completion callbacks
/// arrive PER MEMBER (marking = the member entity's name, e.g. "M1A2 33") and carry no
/// task uuid. This tracker maps member completions back to the owning unit's task and
/// says when the unit-level TASKCMPLT is due.
///
/// Robustness (Step 2): the unit completion is synthesized when a QUORUM of members
/// complete (FanOutCompletionFraction; 1.0 = ALL, today's behavior) OR when a straggler
/// TIMEOUT fires (TrySynthesizeByTimeout). Either trigger sets the fan-out's Synthesized
/// flag exactly once; the record then LIVES to SWALLOW the remaining (late-straggler)
/// member completions - so a member that finishes after synthesis does NOT fall through
/// to the caller's unit-level path and emit a spurious empty-uuid TASKCMPLT. The record
/// is removed when the last pending member drains or when a superseding task Registers.
///
/// PURE (no bridge/logging - offline-testable via --fanout-selftest); thread-safe (the
/// tick thread registers + completes members; the straggler timer callback synthesizes -
/// all state is serialized under _lock).
/// </summary>
public sealed class FanOutTracker
{
    private sealed class FanOut
    {
        public required string UnitName;
        public required string TaskUuid;
        public required HashSet<string> Pending;
        public int Total;
        public double Fraction;      // completion quorum fraction, (0,1]; 1.0 = all must finish
        public bool Synthesized;     // a quorum/timeout synthesis already fired for this fan-out
    }

    private readonly object _lock = new();
    private readonly Dictionary<string, FanOut> _byUnit = new();       // unit -> active fan-out
    private readonly Dictionary<string, string> _unitByMember = new(); // member name -> unit

    /// <summary>
    /// Start tracking a fan-out for <paramref name="unitName"/>. REPLACES any active
    /// fan-out for the unit (a retask supersedes - VRF replaces the members' tasks too,
    /// including a Synthesized-but-not-yet-drained record). <paramref name="completionFraction"/>
    /// is the quorum fraction, clamped to (0,1] (<=0 or >1 -> 1.0 = all members). Returns the
    /// number of members registered (0 = nothing registered; caller should not have fanned out).
    /// </summary>
    public int Register(string unitName, string taskUuid, IEnumerable<string> memberNames,
                        double completionFraction = 1.0)
    {
        lock (_lock)
        {
            CancelLocked(unitName);
            var pending = new HashSet<string>(
                memberNames.Where(n => !string.IsNullOrEmpty(n)), StringComparer.Ordinal);
            if (pending.Count == 0) return 0;
            double frac = (completionFraction > 0.0 && completionFraction <= 1.0) ? completionFraction : 1.0;
            var f = new FanOut
            {
                UnitName = unitName,
                TaskUuid = taskUuid,
                Pending = pending,
                Total = pending.Count,
                Fraction = frac,
                Synthesized = false,
            };
            _byUnit[unitName] = f;
            foreach (var m in pending) _unitByMember[m] = unitName;
            return pending.Count;
        }
    }

    /// <summary>
    /// Back-compat overload (a caller that does not need the swallow signal). Forwards to the
    /// six-out form and discards <c>alreadySynthesized</c>. Used by the existing offline checks.
    /// </summary>
    public bool TryCompleteMember(string memberName, out string unitName, out string taskUuid,
                                  out int remaining, out bool allDone)
        => TryCompleteMember(memberName, out unitName, out taskUuid, out remaining, out allDone, out _);

    /// <summary>
    /// If <paramref name="memberName"/> belongs to an active fan-out, mark it complete.
    /// Returns true with the owning unit + task. Outcome (mutually exclusive):
    ///  - <paramref name="alreadySynthesized"/> true: this member completed AFTER a prior
    ///    quorum/timeout synthesis - it is SWALLOWED; the caller must NOT report a unit
    ///    completion (allDone is false). When the last such straggler drains, the record is removed.
    ///  - <paramref name="allDone"/> true: this completion MET the quorum (and no synthesis had
    ///    fired yet) - the caller synthesizes the unit-level TASKCMPLT now. If members remain,
    ///    the record LIVES in a Synthesized state to swallow them; otherwise it is removed.
    ///  - both false: quorum not yet met - <paramref name="remaining"/> carries the count (the
    ///    caller's "N remaining" progress log).
    /// A name that is not a pending fan-out member returns false (normal unit-level completions
    /// flow through the caller's existing path).
    /// </summary>
    public bool TryCompleteMember(string memberName, out string unitName, out string taskUuid,
                                  out int remaining, out bool allDone, out bool alreadySynthesized)
    {
        unitName = ""; taskUuid = ""; remaining = 0; allDone = false; alreadySynthesized = false;
        if (string.IsNullOrEmpty(memberName)) return false;
        lock (_lock)
        {
            if (!_unitByMember.TryGetValue(memberName, out var unit)) return false;
            if (!_byUnit.TryGetValue(unit, out var f)) { _unitByMember.Remove(memberName); return false; }
            if (!f.Pending.Remove(memberName)) return false;
            // Clean the member->unit mapping on EVERY completion path (progress, quorum, and
            // late-straggler swallow) so it cannot leak or mis-route a later same-named member.
            _unitByMember.Remove(memberName);
            unitName = f.UnitName;
            taskUuid = f.TaskUuid;
            remaining = f.Pending.Count;

            if (f.Synthesized)
            {
                // Late straggler after a prior quorum/timeout synthesis: swallow it. The caller
                // logs at Debug and returns without reporting a unit completion.
                alreadySynthesized = true;
                if (f.Pending.Count == 0) _byUnit.Remove(unit);   // last one drained - drop the record
                return true;
            }

            int completed = f.Total - f.Pending.Count;
            bool quorumMet = completed >= (int)Math.Ceiling(f.Total * f.Fraction);
            if (quorumMet)
            {
                f.Synthesized = true;
                allDone = true;                                   // caller synthesizes the unit TASKCMPLT
                if (f.Pending.Count == 0) _byUnit.Remove(unit);   // nothing left to swallow - drop it
                // else KEEP the (now Synthesized) record so the remaining members are swallowed.
                return true;
            }

            // Quorum not yet met - progress only.
            return true;
        }
    }

    /// <summary>
    /// Straggler-timeout synthesis. If the unit has a NON-Synthesized fan-out whose stored
    /// TaskUuid equals <paramref name="expectedTaskUuid"/>, mark it Synthesized and return true
    /// with the completed/total counts (for the warning). Returns false if the fan-out is
    /// ABSENT (all members already completed, or cancelled), ALREADY Synthesized (a prior
    /// quorum/timeout already fired - idempotent), or its TaskUuid DIFFERS from the expected one.
    ///
    /// The uuid guard is LOAD-BEARING, not belt-and-suspenders: supersession cancels the old
    /// record and Registers a NEW one under the same unit name. Without the guard the OLD task's
    /// timer, firing later, would find the NEW task's record and synthesize ITS completion
    /// prematurely. The Synthesized flag makes timer-vs-quorum idempotent; the uuid makes
    /// timer-vs-supersession safe.
    /// </summary>
    public bool TrySynthesizeByTimeout(string unitName, string expectedTaskUuid,
                                       out int completed, out int total)
    {
        completed = 0; total = 0;
        lock (_lock)
        {
            if (!_byUnit.TryGetValue(unitName, out var f)) return false;              // no fan-out (drained/cancelled)
            if (f.Synthesized) return false;                                          // already synthesized (idempotent)
            if (!string.Equals(f.TaskUuid, expectedTaskUuid, StringComparison.Ordinal)) return false; // superseded
            f.Synthesized = true;
            total = f.Total;
            completed = f.Total - f.Pending.Count;
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
        foreach (var m in f.Pending) _unitByMember.Remove(m);   // clears remaining members (incl. a Synthesized record's)
        return true;
    }
}
