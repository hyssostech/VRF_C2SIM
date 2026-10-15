namespace VrfC2SimApp;

/// <summary>
/// THE BOUNDED NAVIGATE GATE (D36 mitigation candidate; STP-884). NOT ON MAIN until a live arm passes (supervisor,
/// 2026-10-15). PURE - every decision and every sentence is here, so `--navigate-gate-selftest` drives the real rule.
///
/// WHY. docs/experiments/D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md: re-tasking 48 IBCT's 17 members with a second
/// navigate-to-location ~1.5-1.8 min (wall) after the first, while 13 of them were still moving, stopped the VR-Forces
/// 5.2d back end within 0.3 s, 3 runs of 3 (19:45Z, arms F and H; the ROE excluded by H). The same re-task with 7
/// members outstanding on a dry start (arm ii), and 8.25 min after the first move with 10 members stuck (arm i), did
/// NOT stop it. The cause is not closed (n=1 per new arm; the number of members outstanding and the wet start are not
/// separated), so this is a CANDIDATE: it removes the one shape that stopped the back end - a navigate dispatched onto
/// a container whose members' previous navigates are still out - by waiting for them to end, with a bound.
/// STP-884 is the same family on a PLATFORM: an ATTACK's move half completes, its successors are released, and the
/// deferred Fire At is re-recorded in flight under the same task - a successor dispatched then supersedes the engage.
///
/// THE RULE. A task about to be dispatched that WILL MOVE its unit (<see cref="TaskMoves"/>) is HELD while the unit has
/// vendor work in flight (<see cref="Classify"/>): a container with member navigates of an open per-member step still
/// pending, or a platform whose in-flight record is an ENGAGE. The hold ends when the work ends (CLEARED), when the
/// bound expires (BOUND - the task is then DISPATCHED anyway, with a warning: arm (i) re-tasked a container 8.25 min
/// after its first move, 10 members never having ended, and the back end ran on), or when the back end is declared
/// LOST (the task is then abandoned with one TASKABRT, as the readiness hold ends). A task that does not move (hold in place,
/// passage of lines held in place, no geometry) is never held: it issues no movement.
///
/// THE BOUND. <see cref="DefaultMaxWaitSeconds"/> = 480 s WALL, from D36: the only re-task observed SAFE with members
/// still out was 495 s wall after the first move (arm i); the ones that stopped came 94-111 s after it (F, H). Nothing
/// between is measured, so the bound sits at the measured safe point, not at an interpolation. WALL, not sim: the
/// vendor's planning jobs are asynchronous work, and the arms ran at ~6x - the sim seconds say nothing about them.
/// 0 turns the gate OFF (today's behaviour exactly; the fail-first arm of the selftest).
/// </summary>
public static class NavigateGate
{
    public const string SettingKey = "Vrf:NavigateGateMaxWaitSeconds";
    public const double DefaultMaxWaitSeconds = 480.0;
    public const string Prefix = "NAVIGATE GATE";
    public const string RecordRef = "D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15";

    /// <summary>The in-flight kind the platform engage is recorded under (VrfC2SimService IssueEngage /
    /// PendingEngage "fire").</summary>
    public const string EngageKind = "fire";

    /// <summary>The vendor work a unit can have in flight that a new movement must not land on.</summary>
    public enum Work
    {
        None = 0,
        /// <summary>A container whose open per-member step still has members whose navigate has not ended.</summary>
        MemberMovesOutstanding = 1,
        /// <summary>A platform whose in-flight record is the deferred engage of an ATTACK (STP-884).</summary>
        EngageInFlight = 2,
    }

    /// <summary>How a hold ended.</summary>
    public enum Release { Hold = 0, Cleared = 1, Bound = 2, BackendLost = 3 }

    /// <summary>Does dispatching this task MOVE its unit? False for the in-place intents and for a task with no
    /// geometry (R2 executes it in place); true for everything else, the unmapped verbs' bare move included.</summary>
    public static bool TaskMoves(TaskIntent intent, bool hasGeometry)
        => hasGeometry && intent != TaskIntent.HoldInPlace && intent != TaskIntent.PassageOfLines;

    /// <summary>The vendor work in flight on the unit. pendingMembers = members of the container's OPEN per-member step
    /// whose navigate has not ended (0 when no step is open, or not a container); inFlightKind = the unit's in-flight
    /// record's kind (null/empty = idle); inFlightTaskUuid = that record's C2SIM task; newTaskUuid = the task about to
    /// dispatch (a task never waits on itself).</summary>
    public static Work Classify(int pendingMembers, string inFlightKind, string inFlightTaskUuid, string newTaskUuid)
    {
        if (pendingMembers > 0) return Work.MemberMovesOutstanding;
        if (string.Equals(inFlightKind, EngageKind, StringComparison.Ordinal)
            && !string.Equals(inFlightTaskUuid ?? "", newTaskUuid ?? "", StringComparison.Ordinal))
            return Work.EngageInFlight;
        return Work.None;
    }

    /// <summary>Hold this dispatch? Only a MOVING task, onto a unit with work in flight, with the gate on, and with
    /// the back end not declared lost (a lost back end is refused by the ordinary path, never waited on).</summary>
    public static bool ShouldHold(Work work, bool taskMoves, double maxWaitSeconds, bool backendLost)
        => maxWaitSeconds > 0 && taskMoves && work != Work.None && !backendLost;

    /// <summary>One step of the hold, evaluated once a second: the work now, the wall seconds held, the bound.</summary>
    public static Release Step(Work workNow, double heldWallSeconds, double maxWaitSeconds, bool backendLost)
    {
        if (backendLost) return Release.BackendLost;
        if (workNow == Work.None) return Release.Cleared;
        if (heldWallSeconds >= maxWaitSeconds) return Release.Bound;
        return Release.Hold;
    }

    /// <summary>Does this release DISPATCH the task? Cleared and Bound do (the bound is not an abort - arm (i));
    /// BackendLost abandons it (TASKABRT; successors fail fast).</summary>
    public static bool Dispatches(Release r) => r is Release.Cleared or Release.Bound;

    private static string What(Work w, int pending) => w switch
    {
        Work.MemberMovesOutstanding => $"{pending} member(s) of its previous move still have their navigate-to-location out",
        Work.EngageInFlight => "the deferred engage (Fire At) of its previous ATTACK is still in flight (STP-884)",
        _ => "nothing in flight",
    };

    private static string N0(double v) => v.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
    private static string N1(double v) => v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

    public static string HoldLine(string taskName, string unitName, Work w, int pending, double maxWaitSeconds)
        => $"{Prefix}: task '{taskName}' on {unitName} is HELD - {What(w, pending)}. A new movement is not dispatched onto " +
           $"vendor work in flight ({RecordRef}: that shape stopped the 5.2d back end 3 of 3); it goes when the work ends, " +
           $"or at the bound {N0(maxWaitSeconds)} s ({SettingKey}).";

    public static string ClearedLine(string taskName, string unitName, double heldWallSeconds)
        => $"{Prefix}: task '{taskName}' on {unitName} RELEASED after {N1(heldWallSeconds)} WALL s - the work in flight " +
           "ended; dispatching.";

    public static string BoundLine(string taskName, string unitName, Work w, int pending, double heldWallSeconds)
        => $"{Prefix}: task '{taskName}' on {unitName} reached the bound after {N0(heldWallSeconds)} WALL s with " +
           $"{What(w, pending)} - DISPATCHING ANYWAY (a late re-task ran on in {RecordRef} arm (i)); watch the back end.";

    public static string BackendLostLine(string taskName, string unitName, double heldWallSeconds)
        => $"{Prefix}: task '{taskName}' on {unitName} - the back end was declared LOST after {N0(heldWallSeconds)} WALL s " +
           "of the hold; the task is ABANDONED (TASKABRT) and its successors fail fast.";

    public static string StartupLine(double maxWaitSeconds)
        => maxWaitSeconds > 0
            ? $"{Prefix} ON ({SettingKey}={N0(maxWaitSeconds)} s WALL): a task that MOVES its unit is held while the " +
              "unit's previous move (container members) or deferred engage (platform, STP-884) is still in flight - " +
              $"released when it ends, dispatched anyway at the bound. CANDIDATE mitigation ({RecordRef}); not on main."
            : $"{Prefix} OFF ({SettingKey}=0): movements are dispatched onto vendor work in flight, as before.";
}
