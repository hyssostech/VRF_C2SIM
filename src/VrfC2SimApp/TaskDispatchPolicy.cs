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

/// <summary>What the task's AffectedEntity turns out to be (R3).</summary>
public enum TargetResolution
{
    /// <summary>A DISTINCT object this interface created: the one case that can be named as a
    /// VR-Forces target (DtFireAtTargetTask / DtBreachTask / DtFollowEntityTask).</summary>
    DistinctEntity,
    /// <summary>The performing unit itself. R3: this is not an error and not "no target" - the
    /// task's GEOMETRY is the objective, and the objective is what the task is about.</summary>
    SelfIsObjective,
    /// <summary>Named, but not an object we created (an out-of-scope OPFOR entity).</summary>
    Unresolved,
    /// <summary>The order names no AffectedEntity at all.</summary>
    NoTarget,
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

    /// <summary>
    /// What the task's AffectedEntity is (R3, user ruling 2026-09-14: "the target IS the
    /// objective"). SELF IS ITS OWN ANSWER, not a degenerate "no target": STP sets AffectedEntity
    /// to the performing unit on all 42 COA-STP1 tasks (C2SimXmlBuilder.cs:427-429), so reading
    /// that as an error made every ATTACK-family task in the order a degraded one.
    /// </summary>
    public static TargetResolution ForTarget(bool hasAffectedEntity, bool resolved, bool isSelf)
    {
        if (!hasAffectedEntity) return TargetResolution.NoTarget;
        if (!resolved) return TargetResolution.Unresolved;
        return isSelf ? TargetResolution.SelfIsObjective : TargetResolution.DistinctEntity;
    }

    /// <summary>
    /// R3: every resolution except a distinct entity routes the task to ITS OWN GEOMETRY - the
    /// objective - and NONE of them refuses the task. VR-Forces' own tactical tasks agree:
    /// company_seize, co_clear, company_breach, plt_attack_by_fire and unit-attack-to-objective
    /// all take the objective GRAPHIC as their parameter, never a named enemy entity.
    /// </summary>
    public static bool FallsBackToGeometry(TargetResolution r) => r != TargetResolution.DistinctEntity;

    /// <summary>R3: no verb is refused for self-targeting - or for any other target resolution.
    /// A task the interface cannot aim at a named entity is still a task about its objective.</summary>
    public static bool RefusesForTarget(TargetResolution r) => false;

    /// <summary>
    /// M1 (cold-start review of 5c67d41). HOW LONG A SUCCESSOR WAITS FOR ITS PREDECESSOR, given
    /// that the predecessor's completion is its ARMED END TIME (R4).
    ///
    /// The defect this replaces was an ordering proof, not a race. The gate expired at
    /// dispatch + Vrf:TaskPredecessorTimeoutSeconds while the timed completion fires at
    /// dispatch + Duration x Vrf:DurationScale (strictly later: MarkDispatched calls
    /// NotifyDispatched BEFORE TimedCompletionPolicy.Register, and the first walk after
    /// registration anchors only). With the shipped 600 s default against COA-STP1's 4,800 s and
    /// 7,200 s Durations, all 31 gated tasks were SKIPPED with TASKABRT - 11 dispatches out of 42.
    /// Even appsettings.Demo.json's 7,200 s lost the ten PT2H chains by a second or two.
    ///
    /// THE RULE: the window is the longer of what the operator configured and the predecessor's
    /// own end time plus a margin. The configured value keeps its meaning - it is the floor, and
    /// the only thing bounding a predecessor with NO Duration at all - while a predecessor that
    /// carries one can never be outlived by the gate waiting for it.
    /// </summary>
    /// <param name="configuredSeconds">Vrf:TaskPredecessorTimeoutSeconds, the floor.</param>
    /// <param name="predecessorEndSeconds">The predecessor's Duration AFTER Vrf:DurationScale, in
    /// seconds; 0 or non-finite when it has no armed end time (then the floor stands alone).</param>
    /// <summary>
    /// R4: apply Vrf:DurationScale to an authored order time (milliseconds in, milliseconds out),
    /// so the Duration that ENDS a task and the StartTime that HOLDS one back can never be
    /// compressed differently. PURE, and it assumes a VALIDATED scale - the service rejects a
    /// non-finite or non-positive Vrf:DurationScale once at start-up (m8) rather than letting one
    /// mean "no end time" on one half of the order's clock and "dispatch now" on the other.
    /// A non-positive time is not a time: 0 in, 0 out.
    /// </summary>
    public static long ScaleOrderMs(long ms, double scale)
        => ms <= 0 ? 0L
         : !double.IsFinite(scale) || scale <= 0.0 ? ms
         : (long)Math.Round(ms * scale);

    /// <param name="marginSeconds">Vrf:TaskPredecessorEndMarginSeconds - the slack that covers the
    /// timed walk's cadence and the ordering above. Negative is treated as 0.</param>
    public static double PredecessorTimeoutSeconds(double configuredSeconds, double predecessorEndSeconds,
                                                   double marginSeconds)
    {
        double floor = double.IsFinite(configuredSeconds) ? Math.Max(1.0, configuredSeconds) : 1.0;
        if (!double.IsFinite(predecessorEndSeconds) || predecessorEndSeconds <= 0.0) return floor;
        double margin = double.IsFinite(marginSeconds) ? Math.Max(0.0, marginSeconds) : 0.0;
        return Math.Max(floor, predecessorEndSeconds + margin);
    }
}
