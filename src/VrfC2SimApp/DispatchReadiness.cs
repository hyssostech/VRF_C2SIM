using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// WHERE A TASKEE IS, THE MOMENT ITS TASK IS READY TO DISPATCH.
///
/// The five states are the ones this interface can actually OBSERVE. "Requested, not yet
/// confirmed" and "created, name not yet bound" are ONE value on purpose: ObjectCreated is the
/// only evidence the interface ever has that an object exists, and it binds the name in the same
/// callback (VrfC2SimService.OnVrfObjectCreated -> NameRegistry.Bind), so nothing can tell the two
/// apart. The one case that is genuinely "created but not bound" - an AMBIGUOUS name, or a rebind
/// the registry REFUSED - is a naming defect, not a transient: it is already an ERROR at the
/// instant it happens and it never heals, so it presents here as <see cref="RequestedNotBound"/>
/// and costs the readiness timeout before the abort it would have got anyway. Named, not hidden.
/// </summary>
public enum TaskeeReadiness
{
    /// <summary>The C2SIM initialization never planned this unit. NOT transient - refuse at once.</summary>
    Unknown = 0,

    /// <summary>The init planned it, but no create has been requested for it yet - in D5b the
    /// init's creation was deferred to a terrain-profile reply that had not landed
    /// ("creation deferred to the reply (timeout 10 s -> fallback altitudes)").</summary>
    PlannedNotRequested = 1,

    /// <summary>A create was requested and no VR-Forces object has bound to that name yet.</summary>
    RequestedNotBound = 2,

    /// <summary>Bound to a VR-Forces object whose location cannot be read yet: the ObjectCreated
    /// control message precedes HLA discovery by an unbounded interval.</summary>
    BoundNotReadable = 3,

    /// <summary>Bound AND readable. Dispatch, with nothing logged and nothing waited for.</summary>
    Ready = 4,

    /// <summary>
    /// B1 (cold-start review of 945e054). Bound and readable, AND YET NOT TASKABLE: this unit's
    /// order-time materialization is PARKED behind the initialization barrier, so the object that
    /// is bound and readable is the EMPTY SHELL the init created, and the members the order needs
    /// have not been created. Dispatching here drives an empty shell - and worse, the parked
    /// materialization will later DELETE that very object (MaterializeUnit case 3) out from under
    /// the task. Transient by construction: the barrier always settles or expires.
    /// </summary>
    MaterializationParked = 5,

    // RETIRED 2026-09-25: NotOnTheGround = 6 ("BOUND-BUT-NOT-ON-THE-GROUND"), added 2026-09-21 by the
    // buried-units lane (8aeb127). It held a task whose taskee the app had measured off the terrain
    // until a read-back confirmed the placement correction; in run 20260921T143243Z the read-back
    // never landed and two legitimate tasks were ended (RL-20260921-06; the removal is in the
    // completion unit's scope approved as RL-20260925-01). Its description also named the correction
    // as "setAltitude 0 m AGL" - wrong, the code sends a setLocation (docs/CORRECTIONS_LOG.md F-2).
    // The value 6 is not reused.
}

/// <summary>
/// DEFER, DO NOT ABORT (D5b, run 20260921T072530Z_wayb).
///
/// An order pushed 0.31 s after the initialization reached `ExecuteTaskOnTick` before the init's
/// own creates had been issued. `VrfC2SimService.cs:3117` DROPPED T_R5_TK1 because
/// 1.BdeHQ~PXY "was not created" - and the unit was created two log lines later and went on to
/// send 15 position reports. `:3313` REFUSED T_R5_PL1 because a live location could not be read.
/// Both are TRANSIENT states of a healthy system reported as permanent failures of it; the Way A
/// control differs in nothing but the order of two messages.
///
/// THE RULE. A transient state HOLDS the task - bounded by Vrf:DispatchReadinessTimeoutSeconds,
/// re-evaluated on the events that can change it and on the tick - and aborts only when the bound
/// expires, with the state it was still in named. A NON-transient state (the taskee is not in the
/// initialization at all; the back end has been declared LOST) is refused promptly, because
/// turning a data error or a dead simulator into a silent 60 s wait is a worse failure than the
/// one being fixed.
///
/// PURE. Every decision and every sentence is here, so `--dispatch-readiness-selftest` drives the
/// REAL rule rather than a re-implementation of it, and the sentences themselves are assertable.
/// </summary>
public static class DispatchReadiness
{
    /// <summary>The prefix an operator greps for. One line when a hold starts, one when it ends.</summary>
    public const string HoldPrefix = "WAITING FOR THE BACK END";

    /// <summary>The line an operator waits for before pushing an order (DEMO_RUNBOOK sec 4).</summary>
    public const string ReadyToTaskPrefix = "READY TO TASK";

    /// <summary>The setting that bounds every hold. Named in the log lines so an operator can find it.</summary>
    public const string TimeoutSettingKey = "Vrf:DispatchReadinessTimeoutSeconds";

    /// <summary>
    /// The state, from the four observations the service can make. <paramref name="locationReadable"/>
    /// is consulted only when <paramref name="nameBound"/> is true (it cannot be read without a uuid).
    /// "Not planned by the init" wins over everything: an object bound under a name we never planned
    /// is not this taskee.
    /// </summary>
    /// <remarks>
    /// TRAP, named rather than removed (delta review of 27960f6): this 4-argument form forwards
    /// materializationParked = false, so a caller that forgets the fifth observation silently gets
    /// the PRE-B1 semantics. It is kept only because the selftest asserts the two forms agree; the
    /// service always calls the 5-argument one, through ClassifyTaskee.
    /// </remarks>
    public static TaskeeReadiness Classify(bool plannedAtInit, bool createRequested,
                                           bool nameBound, bool locationReadable)
        => Classify(plannedAtInit, createRequested, nameBound, locationReadable, false);

    /// <summary>
    /// B1: the same rule plus the STRUCTURAL INVARIANT. A unit whose order-time materialization is
    /// PARKED behind the initialization barrier is NEVER <see cref="TaskeeReadiness.Ready"/>,
    /// however bound and however readable its init-created shell is - dispatching onto it drives an
    /// empty shell, and the parked work would then delete that object underneath the task. The
    /// parked flag is consulted LAST, so a unit that is not even bound still reports the more
    /// proximate truth about itself; both states hold the task, so the ordering costs nothing.
    /// </summary>
    /// <remarks>2026-09-25: the six-argument form that added GROUND CONTACT (notOnTheGround) is
    /// retired with its state - ground contact no longer holds a task (RL-20260921-06).</remarks>
    public static TaskeeReadiness Classify(bool plannedAtInit, bool createRequested,
                                           bool nameBound, bool locationReadable,
                                           bool materializationParked)
    {
        if (!plannedAtInit) return TaskeeReadiness.Unknown;
        if (!nameBound)
            return createRequested ? TaskeeReadiness.RequestedNotBound : TaskeeReadiness.PlannedNotRequested;
        if (!locationReadable) return TaskeeReadiness.BoundNotReadable;
        return materializationParked ? TaskeeReadiness.MaterializationParked : TaskeeReadiness.Ready;
    }

    /// <summary>Can waiting change this state? Every state between "planned" and "ready".</summary>
    public static bool IsTransient(TaskeeReadiness state)
        => state == TaskeeReadiness.PlannedNotRequested
        || state == TaskeeReadiness.RequestedNotBound
        || state == TaskeeReadiness.BoundNotReadable
        || state == TaskeeReadiness.MaterializationParked;

    /// <summary>A state that must be refused at once, however generous the bound.</summary>
    public static bool MustRefusePromptly(TaskeeReadiness state) => state == TaskeeReadiness.Unknown;

    /// <summary>
    /// THE DECISION. Hold only a transient state, only when the feature is configured on, and only
    /// while the back end is not declared LOST - a stopped simulator produces
    /// <see cref="TaskeeReadiness.BoundNotReadable"/> forever and STP-822 already knows it is gone.
    /// timeoutSeconds &lt;= 0 is today's behaviour: nothing is held.
    /// </summary>
    public static bool ShouldHold(TaskeeReadiness state, double timeoutSeconds, bool backendLost)
        => IsTransient(state) && timeoutSeconds > 0.0 && !backendLost;

    /// <summary>The short token that goes in a TASKABRT, so the state is machine-greppable.</summary>
    public static string StateName(TaskeeReadiness state) => state switch
    {
        TaskeeReadiness.Unknown             => "NOT-IN-THE-INITIALIZATION",
        TaskeeReadiness.PlannedNotRequested => "PLANNED-BUT-NOT-REQUESTED",
        TaskeeReadiness.RequestedNotBound   => "REQUESTED-BUT-NOT-BOUND",
        TaskeeReadiness.BoundNotReadable    => "BOUND-BUT-NOT-READABLE",
        TaskeeReadiness.MaterializationParked => "MATERIALIZATION-PARKED",
        TaskeeReadiness.Ready               => "READY",
        _                                   => "UNSPECIFIED",
    };

    /// <summary>What the state MEANS, in the words an operator needs. One clause, no full stop.</summary>
    public static string Describe(TaskeeReadiness state) => state switch
    {
        TaskeeReadiness.Unknown =>
            "is not in the C2SIM initialization at all, so there is nothing to wait for",
        TaskeeReadiness.PlannedNotRequested =>
            "is planned by the initialization but its create has not been issued yet (the init's "
          + "terrain-profile query decides its altitude and has not replied)",
        TaskeeReadiness.RequestedNotBound =>
            "has been requested from VR-Forces and no created object has bound to its name yet "
          + "(either the create has not round-tripped, or an object came back under a name that "
          + "could not be attributed - look for a NAME REBIND REFUSED or NAME COLLISION line)",
        TaskeeReadiness.BoundNotReadable =>
            "is bound to a VR-Forces object whose live location cannot be read yet (the "
          + "ObjectCreated control message precedes HLA discovery by an unbounded interval)",
        TaskeeReadiness.MaterializationParked =>
            "is bound and readable, but the object that answers is the EMPTY SHELL the "
          + "initialization created: this unit's order-time materialization is PARKED behind the "
          + "initialization barrier, so its members do not exist yet and the parked work will "
          + "delete and re-create this very object when the barrier settles (B1)",
        TaskeeReadiness.Ready =>
            "is bound and its live location reads",
        _ => "is in an unspecified state",
    };

    /// <summary>ONE loud line when a hold starts.</summary>
    public static string HoldLine(string taskName, string unitName, TaskeeReadiness state,
                                  double timeoutSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "{0}: task '{1}' held - unit {2} {3} [{4}]. Holding up to {5:F0} s of WALL clock "
             + "({6}) for it to become taskable instead of dropping the task; on expiry the task "
             + "is abandoned and TASKABRT names this state.",
               HoldPrefix, taskName, unitName, Describe(state), StateName(state),
               timeoutSeconds, TimeoutSettingKey);

    /// <summary>ONE line when the hold ends well. Both clocks, each labelled with what it is.</summary>
    public static string ReleasedLine(string taskName, string unitName, TaskeeReadiness startState,
                                      double wallSeconds, double taskClockSeconds, string taskClockMode)
        => string.Format(CultureInfo.InvariantCulture,
               "{0}: task '{1}' RELEASED - unit {2} is taskable. Held {3:F2} s of WALL clock and "
             + "{4:F2} s of TASK CLOCK (Vrf:TaskClock={5}); it was [{6}] when the hold started. "
             + "The task dispatches now, and its C2SIM Duration runs from this dispatch.",
               HoldPrefix, taskName, unitName, wallSeconds, taskClockSeconds, taskClockMode,
               StateName(startState));

    /// <summary>
    /// The TASKABRT reason when the bound expires. The state is IN it, by rule - and so is the one
    /// thing the state CANNOT tell the operator (S2, cold-start review of 945e054): STP-822 cannot
    /// see a back end that dies WITHOUT RESIGNING (STP-853), so a silent simulator leaves
    /// _backendLost false and this branch, not the liveness branch, is what fires. Saying "the unit
    /// is still not bound" and stopping there would send the reader to the data when the simulator
    /// may simply be gone.
    /// </summary>
    public static string TimeoutAbortReason(string taskName, string unitName, TaskeeReadiness state,
                                            double wallSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "ABANDONED after waiting {0:F1} s for the back end: task '{1}' could not be "
             + "dispatched because unit {2} {3} [{4}]. {5} bounds this wait. CHECK THAT THE BACK "
             + "END IS STILL RUNNING before reading this as a data problem: STP-853 - a back end "
             + "that dies WITHOUT RESIGNING is not detected by the STP-822 liveness rule, so this "
             + "timeout, not a 'back end lost' line, is what a silent simulator produces here.",
               wallSeconds, taskName, unitName, Describe(state), StateName(state), TimeoutSettingKey);

    /// <summary>
    /// B1, the second defence, and the one that is structural. If a parked materialization is ever
    /// reached for a unit a task has ALREADY been dispatched onto, the shell is NOT deleted: the
    /// task keeps what it has (an empty shell, driving), and this says so as loudly as it can. The
    /// alternative - deleting the object a live task is steering - is the failure the review found.
    /// </summary>
    public static string RefusedDeleteUnderDispatchedTask(string unitName, string taskName)
        => string.Format(CultureInfo.InvariantCulture,
               "MATERIALIZE {0}: REFUSING TO MATERIALIZE - task '{1}' has ALREADY BEEN DISPATCHED "
             + "onto this unit, and materializing it now would DELETE the very VR-Forces object "
             + "that task is steering (B1). The unit is LEFT AS IT IS: it keeps driving the object "
             + "it was given, which under CreationPolicy=AtOrder is the EMPTY SHELL the "
             + "initialization created, so the move is real but the unit has no members. This "
             + "state should be unreachable - the barrier is capped below the composition backstop "
             + "and a parked unit is never classified READY - so if you are reading this line, one "
             + "of those two defences did not hold: capture the log and the init, and check "
             + "whether any initialization object failed to bind (the {2} line above).",
               unitName, taskName, ReadyToTaskPrefix);

    /// <summary>The TASKABRT reason when STP-822 has declared the back end LOST during a hold.</summary>
    public static string BackendLostAbortReason(string taskName, string unitName, TaskeeReadiness state,
                                                double wallSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "ABANDONED after waiting {0:F1} s: task '{1}' could not be dispatched because unit "
             + "{2} {3} [{4}], and the BACK END HAS BEEN DECLARED LOST (STP-822) - waiting the "
             + "full {5} would not change that.",
               wallSeconds, taskName, unitName, Describe(state), StateName(state), TimeoutSettingKey);

    // ================= THE INITIALIZATION'S OWN READINESS =================
    // The same question asked of the WHOLE init rather than one taskee: are the objects the policy
    // creates at init time bound, and has one location read succeeded? That is the event the
    // operator waits for before pushing an order, and it is the event that releases a held
    // order-time materialization (the overlap, brief item 2).

    /// <summary>
    /// The window the READY TO TASK OBSERVATION ASKS FOR when the feature is turned off, BEFORE the
    /// cap in <see cref="BarrierSeconds"/> applies. The observation holds nothing up - it only
    /// reports what the initialization achieved - so it still runs at
    /// Vrf:DispatchReadinessTimeoutSeconds = 0, rather than never printing a line for the operator
    /// the runbook tells to wait for one. In practice the cap always binds first (20 s at the
    /// shipped settings), so this value is the wish and not the answer; ask BarrierSeconds.
    /// </summary>
    public const double ObservationWindowSeconds = 60.0;

    /// <summary>
    /// D2 (delta review of 27960f6): THE SLACK THE BARRIER MUST LEAVE, DERIVED FROM THE TWO
    /// SETTINGS THAT SPEND IT rather than picked.
    ///
    /// What an awaiting task needs is not that the DRAIN happens before the composition backstop -
    /// it is that the drained materialization's GATE is COMPLETE before it. Case 3 completes that
    /// gate only after two bounded waits, both of which the drain starts:
    ///   (i)  the re-create goes through StartPlacementTerrainQuery ("ORDER MATERIALIZATION"), so
    ///        the create is deferred to the terrain reply - up to Vrf:TerrainProfileTimeoutSeconds
    ///        (10 s) when that reply never comes;
    ///   (ii) the re-created object is released on REFLECTION by ReleaseReflected, whose deadline is
    ///        Vrf:CompositionTimeoutSeconds (15 s) after its ObjectCreated.
    /// Worst case delete -> gate is therefore terrain + composition = 25 s at the shipped values,
    /// against the 5 s this used to allow. The margin is now exactly that sum.
    /// </summary>
    public static double BarrierBackstopMarginSeconds(double compositionTimeoutSeconds,
                                                      double terrainProfileTimeoutSeconds)
        => terrainProfileTimeoutSeconds + compositionTimeoutSeconds;

    /// <summary>
    /// How long the init barrier waits before reporting what it HAS - CAPPED so that the work it
    /// releases has FINISHED before RunTaskAsync's composition backstop
    /// (Vrf:CompositionTimeoutSeconds + 30) gives up on it.
    ///
    /// B1, the cold-start review of 945e054. Uncapped, the 60 s barrier outlived the 45 s backstop:
    /// the composition await expired, logged "dispatching anyway" and FELL THROUGH; the init shell
    /// was bound and readable, so the task drove the EMPTY SHELL; and 15 s later the barrier
    /// expired, drained, and MaterializeUnit case 3 DELETED that object out from under the
    /// dispatched task.
    ///
    /// THIS CAP IS WHAT CLOSES B1, and it closes it by an inequality rather than by a race. A
    /// task's composition await starts at T_gate, which is at or after the order, which is strictly
    /// after the init armed the barrier (the park only exists if it was), and expires at
    /// T_gate + composition + 30. The drain runs at T_init + cap and its gate completes by
    /// T_init + cap + terrain + composition. With cap = composition + 30 - (terrain + composition)
    /// = 30 - terrain, that is T_init + 30 + composition, which is at or before
    /// T_gate + composition + 30 for every T_gate >= T_init. Note what cancels: the composition
    /// term appears on both sides, because the backstop's own "+ CompositionTimeoutSeconds" is
    /// already the budget for the reflection half of the round trip. **20 s at the shipped
    /// Vrf:TerrainProfileTimeoutSeconds = 10.**
    ///
    /// The floor of 1 s keeps a hostile configuration (a huge terrain timeout) from producing a
    /// zero or negative barrier, which would disable the overlap fix silently; a configuration that
    /// hits the floor has a terrain timeout near 30 s and its own problems.
    /// </summary>
    public static double BarrierSeconds(double timeoutSeconds, double compositionTimeoutSeconds,
                                        double terrainProfileTimeoutSeconds)
    {
        double want = timeoutSeconds > 0.0 ? timeoutSeconds : ObservationWindowSeconds;
        double backstop = compositionTimeoutSeconds + 30.0
                        - BarrierBackstopMarginSeconds(compositionTimeoutSeconds, terrainProfileTimeoutSeconds);
        return Math.Max(1.0, Math.Min(want, backstop));
    }

    /// <summary>
    /// THE LINE THE DEMO RUNBOOK TELLS THE OPERATOR TO WAIT FOR. Printed once, when every name the
    /// init asked VR-Forces to create is bound AND at least one live location has been read.
    /// <paramref name="shellsAtOrder"/> &gt; 0 is the honest CreationPolicy=AtOrder variant: those
    /// objects exist and are bound, but their MEMBERS are created when an order first names them.
    /// </summary>
    public static string ReadyToTaskLine(int bound, int planned, int shellsAtOrder, double wallSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} - {1} of {2} init unit(s) bound and at least one live location read, after "
             + "{3:F1} s. It is safe to push the order.{4}",
               ReadyToTaskPrefix, bound, planned, wallSeconds,
               shellsAtOrder > 0
                   ? string.Format(CultureInfo.InvariantCulture,
                       " CreationPolicy=AtOrder: {0} of them are EMPTY SHELLS - they display, "
                     + "reflect a position and can be tasked, and their members are created when "
                     + "an order first names them. What is ready is the shells; what is created "
                     + "later is their contents.", shellsAtOrder)
                   : " Every object this initialization creates exists now.");

    /// <summary>
    /// The honest variant when the bound expires first. NOT a wedge: held work is released anyway,
    /// so a create that never round-trips costs the timeout and not the run.
    /// </summary>
    public static string NotReadyToTaskLine(int bound, int planned, string missingSample, double wallSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} - NOT REACHED within {1:F0} s: only {2} of {3} init unit(s) are bound with a "
             + "readable location. Missing: [{4}]. The wait is over ({5} bounds it) and held work "
             + "is released - an order pushed now may still be dropped for a unit that never "
             + "arrived.",
               ReadyToTaskPrefix, wallSeconds, bound, planned,
               string.IsNullOrEmpty(missingSample) ? "(none named)" : missingSample,
               TimeoutSettingKey);

    /// <summary>
    /// Said once, at order receipt, when the order beat the initialization's own creations. The
    /// brief's "an order that arrives before it gets a line saying so".
    /// </summary>
    public static string OrderBeforeReadyLine(int bound, int planned)
        => string.Format(CultureInfo.InvariantCulture,
               "ORDER BEFORE {0}: this order arrived while the initialization's own creations are "
             + "still outstanding ({1} of {2} unit(s) bound so far). Nothing is dropped for it - "
             + "each task is HELD until its taskee is taskable ({3}) - but the operator did not "
             + "wait for the {0} line, and a scripted push should gate on that line.",
               ReadyToTaskPrefix, bound, planned, TimeoutSettingKey);

    /// <summary>
    /// The line an order-time materialization prints when it is HELD because the init's own
    /// creations are still outstanding (the overlap). The task waits on the composition gate this
    /// hold pre-registers, exactly as the shell-not-reflected deferral does.
    /// </summary>
    public static string MaterializeHeldLine(string unitName, string why, int outstanding, int planned)
        => string.Format(CultureInfo.InvariantCulture,
               "MATERIALIZE {0} ({1}): HELD - the initialization's own creation of {2} of its {3} "
             + "object(s) is still outstanding, and deleting/re-creating this unit's shell in that "
             + "window overlaps the init's creates on the back end. It runs in one pass when the "
             + "init settles (or when {4} expires); the task waits on the composition gate.",
               unitName, why, outstanding, planned, TimeoutSettingKey);
}
