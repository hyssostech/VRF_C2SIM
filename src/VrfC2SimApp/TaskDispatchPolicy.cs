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
    /// B7 (pass-2 review): WHY A GATE GAVE UP, in words that name the actual failure. The service
    /// used one sentence for both timeouts - "did not complete within Ns of its dispatch" - so
    /// every A1 failure was reported against a dispatch that had never happened, quoting a window
    /// that was not the one that expired. Live gate 5 is specified as a LOG CHECK, which makes the
    /// wording part of the contract, so it is a pure function and the self-test locks both forms.
    /// </summary>
    /// <param name="dispatchTimeoutSeconds">The PHASE 1 window - see
    /// <see cref="PredecessorDispatchTimeoutSeconds"/>.</param>
    /// <param name="completionTimeoutSeconds">The PHASE 2 window - see
    /// <see cref="PredecessorTimeoutSeconds"/>.</param>
    public static string GateFailureReason(GateResult gate, double dispatchTimeoutSeconds,
                                           double completionTimeoutSeconds)
        => gate switch
        {
            GateResult.PredecessorAbandoned => "was skipped/abandoned upstream",
            GateResult.PredecessorNeverDispatched =>
                $"never dispatched within {dispatchTimeoutSeconds:F0}s of order receipt",
            GateResult.PredecessorTimeout =>
                $"did not complete within {completionTimeoutSeconds:F0}s of its dispatch",
            _ => "is ready (this is not a failure)",
        };

    /// <summary>The exact sentence a MALFORMED zero-geometry task is refused with (Q4). Locked by
    /// `--rulings-selftest` because it is what STP will be told, and because a refusal has to say
    /// which two elements the order left out or the operator cannot fix the order.</summary>
    public const string MalformedZeroGeometryRefusal =
        "MALFORMED: the order gives this task NEITHER a Duration NOR any geometry (no MapGraphicID " +
        "and no Location), so nothing in the order could ever end it and nothing in the simulation " +
        "could ever evidence it";

    /// <summary>
    /// Q4 (USER RULING 2026-09-14, REPLACING the supervisor default): A TASK WITH NO DURATION AND
    /// NO GEOMETRY IS MALFORMED, AND IS REFUSED - the interface does not invent a hold for it.
    ///
    /// R2 says a task without geometry is performed WHERE THE UNIT IS, and R4 says a task ends at
    /// its Duration. A task with neither has no place to be performed that the order chose and no
    /// time at which it is over: no vendor task is issued, so there is no arrival and no vendor
    /// completion either, and nothing would ever end it. The supervisor default invented
    /// Vrf:DefaultHoldSeconds (60 s) so the chain would proceed; the user ruled the other way,
    /// and the reasoning is the project's own: a number that is not in the order is a decision
    /// this interface is not entitled to make, and a chain built on one is worse than a chain that
    /// stops with a named cause. It is now an ERROR, a TASKABRT through the single emit point, and
    /// a NotifyAbandoned so the successors fail fast like every other refusal.
    ///
    /// SCOPE. Only the ExecuteInPlace kind - the one that issues no vendor task at all. A
    /// zero-geometry ATTACK or BREACH has a resolved TARGET, so it has something to do and the
    /// vendor reports when it is done; a MOVE has geometry and therefore arrival evidence. None
    /// of COA-STP1's 42 tasks is malformed by this test: all 42 carry a Duration.
    /// </summary>
    public static bool IsMalformedZeroGeometryTask(ZeroGeometryAction action, long durationMs)
        => action == ZeroGeometryAction.ExecuteInPlace && durationMs <= 0L;

    /// <summary>
    /// Q1 (USER RULING 2026-09-14): a superseded task is reported TASKABRT at the supersede point
    /// AND ITS SUCCESSORS ARE ABANDONED IMMEDIATELY. B1 of the pass-2 review: every other TASKABRT
    /// path in the service pairs the report with TaskSequencer.NotifyAbandoned, and this one did
    /// not - so a task the interface had just told STP was NOT being performed still held its
    /// successors at the gate for the whole derived window (up to 7,260 s) before they were
    /// skipped. The report stream and the gate must say the same thing.
    ///
    /// The other reading is still selectable: Vrf:SupersededTaskCode=TASKCMPLT means "the end time
    /// is the ORDER'S statement about the task", the timer stays armed and the successors keep
    /// waiting for the completion that will duly arrive - so THAT branch must NOT abandon.
    /// </summary>
    public static bool SupersedeAbandonsSuccessors(string supersededCode)
        => !string.Equals((supersededCode ?? "").Trim(), "TASKCMPLT", StringComparison.OrdinalIgnoreCase);

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
         : !IsUsableDurationScale(scale) ? ms
         : (long)Math.Round(ms * scale);

    /// <summary>
    /// m8: is Vrf:DurationScale a scale at all? It must be FINITE and GREATER THAN ZERO. Zero,
    /// negative, NaN and the infinities are configuration errors, not instructions: at scale 0 the
    /// Duration collapsed to "no end time armed" while the start delay collapsed to "dispatch
    /// now", so the whole order went out at once and none of it ever completed. The service
    /// rejects a bad value ONCE at start-up and runs at 1.0 - the order as written.
    /// </summary>
    public static bool IsUsableDurationScale(double scale) => double.IsFinite(scale) && scale > 0.0;

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

    /// <summary>
    /// The predecessor's own ARMED END TIME in seconds on the task clock - what
    /// <see cref="PredecessorTimeoutSeconds"/> derives the completion window from. ARMED is the
    /// operative word: with Vrf:TimedCompletion off nothing is armed and this is 0. One line, but
    /// it lives here rather than inline in the service so the offline chain walk
    /// (`--rulings-selftest`) and the live gate cannot drift apart: A1 survived a green suite
    /// precisely because the suite tested the parts and the service assembled them.
    /// </summary>
    /// <param name="timedCompletionOn">Vrf:TimedCompletion (B4 of the pass-2 review). With it OFF
    /// no end time is ever ARMED, so the predecessor's Duration says nothing about when it will
    /// complete and deriving a window from it only makes the eventual skip EIGHT TIMES SLOWER and
    /// quieter than the operator configured. Turning R4 off must not silently change the gate.</param>
    /// <param name="predecessorIsInThisOrder">The startAfterTaskUuid names a task the order
    /// actually carries. A DANGLING reference has no Duration to derive anything from.</param>
    /// <param name="predecessorDurationMs">That task's authored C2SIM Duration, in ms.</param>
    /// <param name="durationScale">Vrf:DurationScale, already validated by the service (m8).</param>
    public static double PredecessorEndSeconds(bool timedCompletionOn, bool predecessorIsInThisOrder,
                                               long predecessorDurationMs, double durationScale)
        => timedCompletionOn && predecessorIsInThisOrder
         ? ScaleOrderMs(predecessorDurationMs, durationScale) / 1000.0 : 0.0;

    /// <summary>The absolute backstop on phase 1 when nothing else bounds it - one day. Long
    /// enough that no authored chain reaches it (COA-STP1's deepest is 26,400 s), short enough
    /// that a wedged interface does not hold a gate open for the life of the process.</summary>
    public const double DefaultChainBackstopSeconds = 86400.0;

    /// <summary>
    /// A1 (cold-start review of `0c96f50`, pass 2). HOW LONG THE GATE WAITS FOR ITS PREDECESSOR
    /// TO **DISPATCH** - which is not the same question as how long it then waits for it to
    /// COMPLETE, and was wrongly answered with the same number.
    ///
    /// THE DEFECT. `HandleOrder` starts EVERY task's orchestration in one loop, so all 42 of
    /// COA-STP1's gates begin waiting at ORDER RECEIPT. Phase 1's window was the completion
    /// window - derived from the predecessor's own Duration - so it covered the predecessor's
    /// DURATION but knew nothing about its LEAD TIME (its start delay plus its own gate wait).
    /// A d2 task therefore demanded that its predecessor dispatch within 4,860 s while that
    /// predecessor was itself waiting out a 7,200 s root: measured, 21 of the 42 tasks dispatched
    /// and 21 were SKIPPED with TASKABRT, at the defaults AND at appsettings.Demo.json's 7,200 -
    /// and at a compressed DurationScale the outcome was not even deterministic.
    ///
    /// THE RULE. When the predecessor NAMES A TASK IN THIS ORDER there is no reason for phase 1
    /// to time out at all: every path in the service that can end a task without dispatching it
    /// calls TaskSequencer.NotifyAbandoned, so a successor already fails FAST on any real dead
    /// end (no PerformingEntity, unknown taskee, no performing unit, no route points, a refused
    /// dispatch, an exception). The phase-1 timer is only a backstop against a predecessor that
    /// will never speak for itself - and the ONE case of that is a DANGLING startAfterTaskUuid,
    /// which keeps the configured window exactly as before. Everything else gets the absolute
    /// backstop (Vrf:TaskChainBackstopSeconds), so a wedged chain still ends rather than waiting
    /// for the life of the process.
    ///
    /// Phase 2 is unchanged: once the predecessor HAS dispatched, the window is the derived
    /// completion window of <see cref="PredecessorTimeoutSeconds"/>, measured from its dispatch.
    /// </summary>
    /// <param name="predecessorIsInThisOrder">The startAfterTaskUuid names a task the order
    /// carries - so something will eventually dispatch it, complete it or abandon it.</param>
    /// <param name="completionWindowSeconds">The phase-2 window, from
    /// <see cref="PredecessorTimeoutSeconds"/>. It is the FLOOR here: a phase-1 window shorter
    /// than the phase-2 one would make no sense.</param>
    /// <param name="backstopSeconds">Vrf:TaskChainBackstopSeconds. Non-finite or non-positive
    /// falls back to <see cref="DefaultChainBackstopSeconds"/> - a misconfigured backstop must
    /// not become "skip the chain immediately".</param>
    public static double PredecessorDispatchTimeoutSeconds(bool predecessorIsInThisOrder,
                                                           double completionWindowSeconds,
                                                           double backstopSeconds)
    {
        double floor = double.IsFinite(completionWindowSeconds) ? Math.Max(1.0, completionWindowSeconds) : 1.0;
        if (!predecessorIsInThisOrder) return floor;
        double backstop = double.IsFinite(backstopSeconds) && backstopSeconds > 0.0
                        ? backstopSeconds : DefaultChainBackstopSeconds;
        return Math.Max(floor, backstop);
    }
}
