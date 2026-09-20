namespace VrfC2SimApp;

/// <summary>
/// Layer-2 intent a C2SIM verb maps to. This is the semantic category the executor
/// dispatches on; the concrete VR-Forces task composition per intent lives in Layer 2
/// (VrfFacade + ExecuteTaskOnTick). See docs/SEMANTIC_MAPPING.md sec 3 for the grounded
/// verb -> intent table and PORT.md sec 10 for why the bare movement projector is not the
/// target design.
/// </summary>
public enum TaskIntent
{
    /// <summary>Bare movement projector: CreateRoute + MoveAlongRoute. Today's path for
    /// MOVE, and the fallback for every intent not yet wired in Layer 2.</summary>
    Move,

    /// <summary>DtBreachTask against the affected entity (obstacle).</summary>
    Breach,

    /// <summary>Move-to-contact + DtFireAtTargetTask / DtTargetEntityTask on the affected
    /// entity. Offensive fires verbs.</summary>
    Attack,

    /// <summary>Move-to + DtHoldUntilTask (hold-in-place on an objective, optional scan).</summary>
    HoldObjective,

    /// <summary>DtPatrolRouteTask + spot reporting (screen / scout).</summary>
    Reconnoiter,

    /// <summary>DtFollowEntityTask / convoy - escort another entity.</summary>
    Escort,

    /// <summary>Composite move + engage sweep. NOT DtClearTask (which is a task-cancel).</summary>
    Clear,

    /// <summary>DtMoveIntoFormationTask - the proper aggregate-in-formation move. Orthogonal
    /// to the verb; the real fix for the stuck-aggregate finding (PORT.md sec 10).</summary>
    MoveInFormation,

    /// <summary>
    /// THE VERB ITSELF NAMES NO MOVEMENT, so no move is manufactured from it. The task is
    /// executed at the performing unit's own position - R2's <c>ExecuteInPlace</c>, reached
    /// through the VERB instead of through absent geometry - no vendor task is issued, the
    /// dispatch is TASKSTRT plus R4's end time, and any geometry the task happens to carry is
    /// NAMED in the log as not driven rather than quietly turned into a route.
    ///
    /// WHY THIS EXISTS RATHER THAN A MOVE ROW. The C2SIM schema carries no semantics on a
    /// TaskActionCode at all (0 of 1,629 enumeration facets in
    /// C2SIM_SMX_LOX_CWIX2024.xsd carry an annotation), so a verb's meaning comes from the
    /// project's own record. Where that record says a code is a MARKER, mapping it to Move
    /// would invent a movement the order never asked for - the "fake move" the vocabulary
    /// work exists to stop. Bare Move is the fallback for a verb we have NOT ruled on; this
    /// is the answer for one we HAVE.
    /// </summary>
    HoldInPlace,
}

/// <summary>
/// The Layer-1 classification of one C2SIM task's verb: the intent it maps to, a
/// human-readable description of the intended Layer-2 composition (for logging + docs),
/// whether that composition is actually wired in Layer 2 yet, and whether the verb was
/// even found in the mapping table. When <see cref="Implemented"/> is false the executor
/// falls back to bare movement and logs the gap; when <see cref="Recognized"/> is false
/// the verb was unknown (also bare movement, but surfaced distinctly as a coverage gap so
/// a new C2SIM verb never degrades silently).
/// </summary>
public sealed record VerbPlan(string ActionCode, TaskIntent Intent, string Composition,
                              bool Implemented, bool Recognized);

/// <summary>
/// Layer 1 of the two-layer semantic map (docs/SEMANTIC_MAPPING.md): classifies a C2SIM
/// <c>TaskActionCode</c> into a <see cref="TaskIntent"/> + intended VR-Forces composition.
/// PURE (no bridge / MAK dependency) so it is reviewable and testable offline
/// (VrfC2SimApp --verb-selftest).
///
/// The table is grounded on the ACTUAL verbs in the real orders (COA-STP1_Order,
/// VRF-Approved-5June24_Order) - see SEMANTIC_MAPPING.md sec 2a. An unlisted verb
/// classifies as <see cref="TaskIntent.Move"/> (the safe bare-movement fallback).
/// </summary>
public static class VerbMapping
{
    /// <summary>Whether an intent's Layer-2 composition is wired today. Only Move is done;
    /// Breach/Attack/... land in later units (SEMANTIC_MAPPING.md sec 5). Kept here (not on
    /// the row) so flipping a verb on is one edit when its facade task is added.</summary>
    private static bool IsImplemented(TaskIntent intent) => intent switch
    {
        TaskIntent.Move => true,
        TaskIntent.Attack => true,        // unit 3: DtFireAtTargetTask on the affected entity
        TaskIntent.Breach => true,        // unit 2: DtBreachTask on the affected obstacle
        TaskIntent.Reconnoiter => true,   // DtPatrolRouteTask along the route (SCREEN/SCOUT)
        TaskIntent.Escort => true,        // DtFollowEntityTask on the escorted entity (ESCRT)
        TaskIntent.HoldInPlace => true,   // R2's in-place dispatch, reached by the verb (no vendor task)
        // HoldObjective (DtHoldUntilTask + scan) and Clear (composite) stay bare-move fallbacks;
        // MoveInFormation is config-driven (aggregate moves), not verb-classified.
        _ => false,
    };

    // Verb -> (intent, composition). Keys are UPPERCASE C2SIM TaskActionCode values.
    // Composition strings mirror SEMANTIC_MAPPING.md sec 3.
    private static readonly IReadOnlyDictionary<string, (TaskIntent Intent, string Composition)> Map =
        new Dictionary<string, (TaskIntent, string)>(StringComparer.Ordinal)
        {
            ["MOVE"]   = (TaskIntent.Move,            "CreateRoute + MoveAlongRoute"),
            ["BREACH"] = (TaskIntent.Breach,          "approach move + DtBreachTask(affected)"),
            ["ATTACK"] = (TaskIntent.Attack,          "move-to-contact + fireAtTarget(affected)"),
            ["DESTRY"] = (TaskIntent.Attack,          "move-to-contact + fireAtTarget(affected)"),
            ["FIX"]    = (TaskIntent.Attack,          "move-to-contact + fireAtTarget(affected)"),
            ["DISRPT"] = (TaskIntent.Attack,          "move-to-contact + fireAtTarget(affected)"),
            ["PENTRT"] = (TaskIntent.Attack,          "move-to-contact + fireAtTarget(affected)"),
            ["SECURE"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["OCCUPY"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["SEIZE"]  = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["RETAIN"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["BLOCK"]  = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["DEFEND"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["GUARD"]  = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["SCREEN"] = (TaskIntent.Reconnoiter,     "DtPatrolRouteTask + spot reporting"),
            ["SCOUT"]  = (TaskIntent.Reconnoiter,     "DtPatrolRouteTask + spot reporting"),
            ["ESCRT"]  = (TaskIntent.Escort,          "DtFollowEntityTask / convoy"),
            ["CLRLND"] = (TaskIntent.Clear,           "composite move + engage sweep"),

            // ---- THE TWO VERBS THE IRON STORM EXPORT ADDED (2026-09-20) ------------------------
            // Both are valid TaskActionCodeType members (C2SIM_SMX_LOX_CWIX2024.xsd:3913 CRESRV,
            // :3957 ExecutePlanPhase) and both were UNRECOGNISED here, so five of Iron Storm's 23
            // tasks ran as bare movement with a coverage-gap warning. The schema annotates NO
            // enumeration member, so the reading below is the project's own record, cited per row.
            //
            // ExecutePlanPhase IS A PLAN-PHASE MARKER, NOT A MOVE. The one sentence the schema
            // spends on it is on the TRIGGER that consumes it:
            //   "A trigger for the execution of a plan phase that is based on the start time of a
            //    task. Typically this task will be defined in an order and will have a
            //    TaskActionCode of ExecutePlanPhase."  (OnOrderTriggerType, xsd:4388-4396)
            // i.e. the task exists so that OTHER tasks can be gated on ITS start time; the phase
            // change is the payload, and the code says nothing about going anywhere. The project's
            // own survey classifies it exactly so - "Not ours: ... ExecutePlanPhase (phase marker)"
            // (docs/STP_TASK_VOCABULARY_2026-09-03.md:39) and "not ours - air / CSS / stability /
            // marker" (docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md:222-223).
            // So: HoldInPlace. STP's own passage-of-lines code is CNFPSL
            // (STP_TASK_VOCABULARY_2026-09-03.md:36-37); an export that means "move" should send
            // that, and a marker is not this interface's to re-interpret as one.
            ["EXECUTEPLANPHASE"] = (TaskIntent.HoldInPlace,
                "phase marker (xsd:4388-4396 OnOrderTrigger): no vendor task; the task is executed " +
                "at the unit's own position and ends at its C2SIM Duration"),

            // CRESRV = "constitute reserve" (docs/STP_TASK_VOCABULARY_2026-09-03.md:37). It is a
            // TERRAIN/POSTURE verb in the hold family: the unit occupies its reserve position and
            // stays uncommitted. There is NO vendor task for it - "Not representable in any VRF
            // task without authoring: ... CRESRV" (STP_TASK_VOCABULARY_2026-09-03.md:81-83, and
            // TASK_VOCABULARY_ASSESSMENT_2026-09-14.md:692-694) - which is exactly what
            // HoldObjective already records: Implemented=false, so the dispatch keeps the
            // documented move-to fallback and the Layer-2 gap stays LOUD instead of the verb
            // reading as unknown. Recognising it is the whole change: an unrecognised verb says
            // "nobody has looked at this", and somebody now has.
            // STILL OPEN (not built here): ASSESSMENT:692-694 also asks that the gap warning
            // become a reported ObservationReport rather than a log line - that is the L2 lane.
            ["CRESRV"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
        };

    /// <summary>Classify a C2SIM TaskActionCode. Null/empty/unlisted -> bare Move fallback.</summary>
    public static VerbPlan Classify(string actionCode)
    {
        string key = (actionCode ?? "").Trim().ToUpperInvariant();
        if (Map.TryGetValue(key, out var row))
            return new VerbPlan(key, row.Intent, row.Composition, IsImplemented(row.Intent), Recognized: true);
        // Unlisted verb: fall back to bare movement (SEMANTIC_MAPPING.md sec 6), but flag it
        // as unrecognized so the executor surfaces the coverage gap instead of degrading silently.
        return new VerbPlan(key, TaskIntent.Move, "CreateRoute + MoveAlongRoute (fallback)",
                            Implemented: true, Recognized: false);
    }
}
