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

    /// <summary>RL-20260926-01: advance to the breach location + one observation that the breach
    /// is not simulated (STP-865). No DtBreachTask (aggregate-level only).</summary>
    Breach,

    /// <summary>RL-20260926-01: advance to the objective; a UNIT gets rules of engagement fire at
    /// will, a PLATFORM with a distinct target gets DtFireAtTargetTask after the move
    /// (TaskDispatchPolicy.ForEngage).</summary>
    Attack,

    /// <summary>Move-to + DtHoldUntilTask (hold-in-place on an objective, optional scan).</summary>
    HoldObjective,

    /// <summary>DtPatrolRouteTask + spot reporting (screen / scout).</summary>
    Reconnoiter,

    /// <summary>DtFollowEntityTask / convoy - escort another entity.</summary>
    Escort,

    /// <summary>Composite move + engage sweep. NOT DtClearTask (which is a task-cancel).</summary>
    Clear,

    /// <summary>CONSTR (construct an obstacle; STP #429, RL-20261014-03): move to the obstacle and hold there, rules of
    /// engagement as ordered, plus ONE observation that the construction is not simulated - the BREACH pattern
    /// (STP-865) on the other side of an obstacle. No vendor task: nothing emplaces an obstacle from an order.</summary>
    Construct,

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

    /// <summary>
    /// FOLSPT (follow and support) / FOLASS (follow and assume). Since STP-846 STP exports these codes and names the
    /// SUPPORTED unit as the task's AffectedEntity. STP-871 (TaskDispatchPolicy.ForFollow): a PLATFORM with a distinct
    /// supported unit TRAILS it with DtFollowEntityTask under the order's rules of engagement; an Aggregate Container
    /// (no follow controller, C1) and a task with no distinct unit advance along the task's graphic to its end and
    /// HOLD there - the ordinary route move, no engagement task, completion by the time rules (RL-20260921-09).
    /// Added to the RL-20260926-01 unit at the coordinator's direction - NOT an owner ruling.
    /// </summary>
    FollowAndSupport,

    /// <summary>
    /// CNFPSL (conduct forward passage of lines; xsd:3899; STP's passage-of-lines code,
    /// docs/STP_TASK_VOCABULARY_2026-09-03.md:36). HELD IN PLACE FOR NOW, by the owner's answer of
    /// 2026-09-26 (RL-20260926-01 A6: "Hold for 1st run plus jira item for coa renderer"): Iron Storm
    /// cut A's T01/T13 list their four graphics (start point, passage point, lane, release point) out
    /// of doctrinal order, so driving them as listed is a 30-40 km zig-zag, and T01 is the division HQ
    /// that CONTROLS the passage. Route by graphic role is not built (STP-866). The dispatch is the
    /// ExecutePlanPhase one: no vendor task, no move, the order's ROE, completion by time. The repo
    /// holds no doctrine record for passage of lines (FM 3-90 was not cited from the record).
    /// </summary>
    PassageOfLines,
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
        TaskIntent.Attack => true,        // RL-20260926-01: advance + fire at will (unit) / Fire At (platform, distinct target)
        TaskIntent.Breach => true,        // RL-20260926-01: advance + "breach not simulated" observation (STP-865)
        TaskIntent.Reconnoiter => true,   // DtPatrolRouteTask along the route (SCREEN/SCOUT)
        TaskIntent.Escort => true,        // DtFollowEntityTask on the escorted entity (ESCRT)
        TaskIntent.HoldInPlace => true,   // R2's in-place dispatch, reached by the verb (no vendor task)
        TaskIntent.FollowAndSupport => true, // FOLSPT/FOLASS: the route move itself, then hold (no engagement)
        TaskIntent.PassageOfLines => true,   // CNFPSL: held in place for now (no vendor task; STP-866)
        TaskIntent.Construct => true,        // CONSTR: the move itself, then hold + 'construction not simulated' (RL-20261014-03)
        // HoldObjective (DtHoldUntilTask + scan) and Clear (composite) stay bare-move fallbacks;
        // MoveInFormation is config-driven (aggregate moves), not verb-classified.
        _ => false,
    };

    private const string AttackComposition =
        "advance to the objective + ROE fire at will (unit); Fire At only for a platform with a distinct target";
    private const string PassageComposition =
        "held in place for now (no vendor task, ends at its Duration); route by graphic role not implemented - STP-866";
    private const string FollowComposition =
        "advance along the task's graphic to its end and hold there; no engagement; ROE as ordered";
    private const string ConstructComposition =
        "advance to the obstacle location + hold + 'construction not simulated' observation; no vendor task";
    private const string InPlaceComposition =
        "held where the unit is (no vendor task, ends at its C2SIM Duration); the task's point is not driven";
    private const string BreachComposition =
        "advance to the breach location + 'breach not simulated' observation (STP-865); no DtBreachTask";

    // Verb -> (intent, composition). Keys are UPPERCASE C2SIM TaskActionCode values.
    // Composition strings mirror SEMANTIC_MAPPING.md sec 3.
    private static readonly IReadOnlyDictionary<string, (TaskIntent Intent, string Composition)> Map =
        new Dictionary<string, (TaskIntent, string)>(StringComparer.Ordinal)
        {
            ["MOVE"]   = (TaskIntent.Move,            "CreateRoute + MoveAlongRoute"),
            // RL-20260926-01 (TaskDispatchPolicy.ForEngage decides; these strings only describe it).
            ["BREACH"] = (TaskIntent.Breach,          BreachComposition),
            ["ATTACK"] = (TaskIntent.Attack,          AttackComposition),
            // STP's main / supporting attack (docs/STP_TASK_VOCABULARY_2026-09-03.md:31, :63;
            // TaskActionCodeType members, C2SIM_SMX_LOX_CWIX2024.xsd:3863, :3865).
            ["ATTMN"]  = (TaskIntent.Attack,          AttackComposition),
            ["ATTSPT"] = (TaskIntent.Attack,          AttackComposition),
            ["DESTRY"] = (TaskIntent.Attack,          AttackComposition),
            ["FIX"]    = (TaskIntent.Attack,          AttackComposition),
            ["DISRPT"] = (TaskIntent.Attack,          AttackComposition),
            ["PENTRT"] = (TaskIntent.Attack,          AttackComposition),
            // ENGAGE (xsd:3951): STP's friendly 'position sniper' (STP-924; ruling D2 = ENGAGE, relayed by the STP-1097
            // session 2026-10-14). The unit ATTACK path: advance to the point (about 0 m for an emplaced unit), ROE fire
            // at will, no Fire At to a unit (RL-20260926-01).
            ["ENGAGE"] = (TaskIntent.Attack,          AttackComposition),
            // DEFEAT (D26, RL-20261014-05; STP COA 1 'AttackInZoneToDefeat'): the doctrinal sibling of DESTRY - the ATTACK path.
            ["DEFEAT"] = (TaskIntent.Attack,          AttackComposition),
            // STP #417 / #429 (RL-20261014-03, the owner's 'As proposed', 2026-10-14). STP's meaning per row from the
            // STP-1097 session's read of C2SimTask.cs / AS_Table_compact.txt. None is kinetic.
            //   EVACT  - collect casualties / civilians / prisoners: move to the collection point and hold there.
            //   CONSTR - engineer constructs an obstacle: move to it, hold, 'construction not simulated'.
            //   HoldInPlace (ConvoyHalted), HIDE (occupy hide positions), UNCONW (food distribution, insurgent recruiting
            //   and services) - presence where the unit is: held in place, no move manufactured from the point.
            ["EVACT"]  = (TaskIntent.HoldObjective,   "move-to + hold (collection point; RL-20261014-03)"),
            ["CONSTR"] = (TaskIntent.Construct,       ConstructComposition),
            ["HOLDINPLACE"] = (TaskIntent.HoldInPlace, InPlaceComposition),
            ["HIDE"]   = (TaskIntent.HoldInPlace,     InPlaceComposition),
            ["UNCONW"] = (TaskIntent.HoldInPlace,     InPlaceComposition),
            ["SECURE"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["OCCUPY"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            // SETUP (xsd 'Set up: To prepare or establish a FACILITY, ORGANISATION or FEATURE'): STP's 'establish a command
            // post' (STP-1129; RL-20261014-04). Like OCCUPY: go to the CP point and hold; no geometry = held in place (R2).
            ["SETUP"]  = (TaskIntent.HoldObjective,   "move-to + hold (command post; RL-20261014-04)"),
            // D26 (RL-20261014-05): CSS presence at a point, like EVACT / SETUP - move to the point and hold; no geometry = held in
            // place (R2). KNOWN LIMIT (REINF): not tied to the unit it reinforces - the task's own geometry only.
            ["REFUEL"] = (TaskIntent.HoldObjective,   "move-to + hold (refuel point; RL-20261014-05)"),
            ["RESUPL"] = (TaskIntent.HoldObjective,   "move-to + hold (supply point; RL-20261014-05)"),
            ["REINF"]  = (TaskIntent.HoldObjective,   "move-to + hold (reinforcement point; not tied to the reinforced unit; RL-20261014-05)"),
            ["SEIZE"]  = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["RETAIN"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["BLOCK"]  = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["DEFEND"] = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["GUARD"]  = (TaskIntent.HoldObjective,   "move-to + DtHoldUntilTask + scan"),
            ["SCREEN"] = (TaskIntent.Reconnoiter,     "DtPatrolRouteTask + spot reporting"),
            ["SCOUT"]  = (TaskIntent.Reconnoiter,     "DtPatrolRouteTask + spot reporting"),
            ["ESCRT"]  = (TaskIntent.Escort,          "DtFollowEntityTask / convoy"),
            // Follow and support / follow and assume (C2SIM_SMX_LOX_CWIX2024.xsd:3963-3964;
            // docs/STP_TASK_VOCABULARY_2026-09-03.md:36). Not ESCRT: the supported unit is not in
            // STP's order, so nothing is followed - the unit drives the graphic and holds at its end.
            ["FOLSPT"] = (TaskIntent.FollowAndSupport, FollowComposition),
            ["FOLASS"] = (TaskIntent.FollowAndSupport, FollowComposition),
            // Conduct forward passage of lines (xsd:3899). Held in place for now (RL-20260926-01 A6).
            ["CNFPSL"] = (TaskIntent.PassageOfLines, PassageComposition),
            // CNRPSL (rearward passage of lines): STP exports it since STP PR #184 (STP-866, 2026-10-12) when the
            // task name says rearward. The same RL-20260926-01 A6 hold as CNFPSL; unlisted it fell back to a bare move.
            ["CNRPSL"] = (TaskIntent.PassageOfLines, PassageComposition),
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
