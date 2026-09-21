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
    public static TaskeeReadiness Classify(bool plannedAtInit, bool createRequested,
                                           bool nameBound, bool locationReadable)
    {
        if (!plannedAtInit) return TaskeeReadiness.Unknown;
        if (nameBound) return locationReadable ? TaskeeReadiness.Ready : TaskeeReadiness.BoundNotReadable;
        return createRequested ? TaskeeReadiness.RequestedNotBound : TaskeeReadiness.PlannedNotRequested;
    }

    /// <summary>Can waiting change this state? The three states between "planned" and "ready".</summary>
    public static bool IsTransient(TaskeeReadiness state)
        => state == TaskeeReadiness.PlannedNotRequested
        || state == TaskeeReadiness.RequestedNotBound
        || state == TaskeeReadiness.BoundNotReadable;

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

    /// <summary>The TASKABRT reason when the bound expires. The state is IN it, by rule.</summary>
    public static string TimeoutAbortReason(string taskName, string unitName, TaskeeReadiness state,
                                            double wallSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "ABANDONED after waiting {0:F1} s for the back end: task '{1}' could not be "
             + "dispatched because unit {2} {3} [{4}]. {5} bounds this wait.",
               wallSeconds, taskName, unitName, Describe(state), StateName(state), TimeoutSettingKey);

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
    /// The window the READY TO TASK OBSERVATION uses when the feature is turned off. The
    /// observation holds nothing up - it only reports what the initialization achieved - so it
    /// still runs at Vrf:DispatchReadinessTimeoutSeconds = 0, on this default window, rather than
    /// never printing a line for the operator the runbook tells to wait for one.
    /// </summary>
    public const double ObservationWindowSeconds = 60.0;

    /// <summary>How long the init barrier waits before reporting what it HAS.</summary>
    public static double BarrierSeconds(double timeoutSeconds)
        => timeoutSeconds > 0.0 ? timeoutSeconds : ObservationWindowSeconds;

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
