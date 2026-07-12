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
