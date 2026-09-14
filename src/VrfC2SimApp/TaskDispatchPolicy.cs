namespace VrfC2SimApp;

/// <summary>What a dispatch does with a task that carries NO geometry at all.</summary>
public enum ZeroGeometryAction
{
    /// <summary>Fire at the resolved target where the unit stands (no move to make).</summary>
    EngageInPlace,
    /// <summary>Breach the resolved obstacle where the unit stands.</summary>
    BreachInPlace,
    /// <summary>R2: execute the task AT THE PERFORMING UNIT'S OWN POSITION - the unit holds
    /// where it is, no vendor move is issued, and the task ends at its end time.</summary>
    ExecuteInPlace,
    /// <summary>Refuse: there is no unit to task at all.</summary>
    Refuse,
}

/// <summary>
/// THE DISPATCH DECISIONS THE 2026-09-14 RULINGS CHANGED, as pure functions so they are decidable
/// offline (`--rulings-selftest`) instead of only inside a live run.
///
/// R2 - "a task without geometry uses the geometry of the performing (who) unit". Nine of
/// COA-STP1's 42 tasks carry no Location (the air-defence chain T9-T12 among them). The interface
/// used to log "NO LOCATION GIVEN - CAN'T EXECUTE TASK", abandon the task and, through
/// NotifyAbandoned, take its whole STREND chain down with it - which is exactly how run G6 lost
/// T9, T10, T11 and T12. Doctrine says those tasks ("Provide...", "Maintain...", "Continue...")
/// are performed WHERE THE UNIT IS; the user ruled the same. So the task is dispatched in place,
/// the derivation is reported, and the chain continues. The only refusal left is the one that was
/// never about geometry: there is no performing unit to task.
/// </summary>
public static class TaskDispatchPolicy
{
    /// <summary>The exact sentence the in-place dispatch reports to C2SIM (a NameObservation, the
    /// same observation channel R-SURFACE-PROXY uses). The wording is the ruling's.</summary>
    public const string ZeroGeometryObservation =
        "no geometry in the order: executing at the performing unit's position";

    /// <summary>
    /// What to do with a task that carries no geometry (R2). The order of the tests is the order
    /// of certainty: no performer is the only thing that makes the task impossible; a resolved
    /// target gives the task something concrete to do where the unit stands; otherwise the
    /// performing unit's own position IS the geometry.
    /// </summary>
    /// <param name="performerResolved">The taskee resolved to a VR-Forces object we can read.
    /// False only on the paths that already refuse before geometry is even looked at (the unit
    /// was never created, its name is not bound to an object, its position cannot be read).</param>
    public static ZeroGeometryAction ForZeroGeometry(bool performerResolved, bool hasAttackTarget,
                                                     bool hasBreachTarget)
    {
        if (!performerResolved) return ZeroGeometryAction.Refuse;
        if (hasAttackTarget) return ZeroGeometryAction.EngageInPlace;
        if (hasBreachTarget) return ZeroGeometryAction.BreachInPlace;
        return ZeroGeometryAction.ExecuteInPlace;
    }

    /// <summary>Does this action mean the task will never run - i.e. must its successors be told
    /// to stop waiting (TaskSequencer.NotifyAbandoned) and STP told TASKABRT?</summary>
    public static bool Refuses(ZeroGeometryAction action) => action == ZeroGeometryAction.Refuse;
}
