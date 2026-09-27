namespace VrfC2SimApp;

/// <summary>
/// WHERE THE TWO TASK-LEVEL JUDGES LOOK FOR A UNIT - the arrival evidence (C15) and the progress
/// watchdog (C16) - and what a MEMBERLESS aggregate means. Decided by
/// <see cref="UnitPositionPolicy.SourceFor"/>; read by VrfC2SimService.TryReadUnitPositions, the ONE
/// reader both judges (and the dispatch baseline and the engage fallback's stays-put test) share.
/// </summary>
public enum UnitPositionSource
{
    /// <summary>A PLATFORM: its own reflected position, one sample of one (unchanged).</summary>
    Platform,
    /// <summary>An aggregate that publishes members: every member's position (unchanged).</summary>
    Members,
    /// <summary>D1 (RL-20260927-01): a memberless aggregate on the AGGREGATE model set - one simulated
    /// object per unit, with no members by construction (UG52 27.1; "the center point of the unit is
    /// used", 27.1.4) - is ONE position: its own reflected centre point, one sample of one.</summary>
    AggregateLeaf,
    /// <summary>A memberless aggregate on the ENTITY model set: its members have not reflected yet (they
    /// arrive asynchronously after the create) or it is an empty shell. NOTHING to judge this tick - it is
    /// skipped, exactly as before D1.</summary>
    NotYetJudgeable,
    /// <summary>No VR-Forces object is bound to the unit's name yet (platform or aggregate alike): nothing to
    /// judge, and nothing to say about members - the R1 poll names an unbound unit once already.</summary>
    Unbound,
}

/// <summary>
/// D1 (RL-20260927-01; docs/PLAN_MOVEMENT_2026-09-27.md row D1). Before this, the arrival evidence and the
/// progress watchdog read an aggregate's MEMBER positions only and skipped an aggregate that published none.
/// On the AggregateTacticalLevel model set every simulated unit is exactly that - one object, no members -
/// so a leaf that stopped at a lake was silent at task level and its arrival was never scored. A memberless
/// aggregate on that model set now counts as ONE position: the unit's own reflected position, the same read
/// the R1 position reports have always made (VrfBridge.TryGetEntityGeodetic, which resolves an aggregate's
/// state repository as well as an entity's).
///
/// HOW THE TWO MEMBERLESS CASES ARE TOLD APART: BY THE MODEL SET, NOT BY TIME. A memberless aggregate on
/// the ENTITY model set is a pseudo-aggregate whose member platforms have not reflected yet (they arrive
/// asynchronously after the create) or an empty shell; on the AGGREGATE model set a unit never has members.
/// A per-object guess ("no members for N seconds, so it must be a leaf") would misjudge exactly the shell and
/// the slow reflection it has to leave alone, and the vendor's own marker (the published aggregate state) is
/// not exposed by the facade. The model set is fixed per SCENARIO (UG52 13.7), so one configuration key
/// states it for the whole run: Vrf:ModelSet.
///
/// SAFE DEFAULT: an absent, empty or unrecognised Vrf:ModelSet is EntityLevel - the behaviour before D1, bit
/// for bit - and an unrecognised value is said once at start-up. The key reaches main with the aggregate-
/// profile lane (appsettings, runner -ModelSet exporting Vrf__ModelSet) and the pre-flight lane
/// (VrfSettings.ModelSet); the service reads it straight from the configuration, so it is honoured the moment
/// either lands, and the accepted spellings match the pre-flight lane's ModelSetRules.TryParse (case-
/// insensitive, a trailing ".sms" allowed).
///
/// Pure: no bridge, no clock - `--rulings-selftest` checks it offline.
/// </summary>
public static class UnitPositionPolicy
{
    /// <summary>The model set whose units are memberless by construction.</summary>
    public const string AggregateModelSet = "AggregateTacticalLevel";

    /// <summary>The model set every scenario in the record ran on, and the safe default.</summary>
    public const string EntityModelSet = "EntityLevel";

    /// <summary>
    /// How long after dispatch an ENTITY-level aggregate may stay memberless before the service says so
    /// (once per unit-task). A diagnostic, never a judgement: the unit is still skipped. 60 WALL seconds is
    /// twice the arrival check's own 30 s post-dispatch floor (Vrf:ArrivalMinSecondsSinceDispatch), and a
    /// task only dispatches after its unit is bound and readable (D5b) and a composed unit's children have
    /// reflected (ReleaseReflected), so members still missing then are not the create-time transient.
    /// </summary>
    public const double MemberlessWarnSeconds = 60.0;

    /// <summary>
    /// Vrf:ModelSet -> is it the aggregate model set? TRUE only for "AggregateTacticalLevel" (any case, an
    /// optional ".sms", surrounding blanks). Returns whether the value was RECOGNISED: absent/empty and
    /// "EntityLevel" are recognised as the entity model set; anything else is NOT recognised and falls back
    /// to the entity model set (the caller says so once).
    /// </summary>
    public static bool TryParseModelSet(string value, out bool aggregateModelSet)
    {
        aggregateModelSet = false;
        string v = (value ?? "").Trim();
        if (v.EndsWith(".sms", StringComparison.OrdinalIgnoreCase)) v = v.Substring(0, v.Length - 4).Trim();
        if (v.Length == 0 || string.Equals(v, EntityModelSet, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(v, AggregateModelSet, StringComparison.OrdinalIgnoreCase))
        {
            aggregateModelSet = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// THE ONE DECISION. A platform is its own position; an aggregate with members is its members (both
    /// unchanged); a MEMBERLESS aggregate is ONE position on the aggregate model set (D1) and nothing to judge
    /// on the entity model set (unchanged: members not reflected yet, or a shell).
    /// </summary>
    public static UnitPositionSource SourceFor(bool isAggregate, int memberCount, bool aggregateModelSet)
    {
        if (!isAggregate) return UnitPositionSource.Platform;
        if (memberCount > 0) return UnitPositionSource.Members;
        return aggregateModelSet ? UnitPositionSource.AggregateLeaf : UnitPositionSource.NotYetJudgeable;
    }

    /// <summary>Is a memberless ENTITY-level aggregate old enough to be worth one line? (See
    /// <see cref="MemberlessWarnSeconds"/>.)</summary>
    public static bool ShouldWarnMemberless(UnitPositionSource source, double secondsSinceDispatch)
        => source == UnitPositionSource.NotYetJudgeable && secondsSinceDispatch >= MemberlessWarnSeconds;

    /// <summary>The start-up line: the model set as this code read it and what a memberless aggregate means.</summary>
    public static string StartupLine(string rawValue, bool recognised, bool aggregateModelSet)
    {
        string shown = string.IsNullOrWhiteSpace(rawValue) ? "(not set)" : "'" + rawValue.Trim() + "'";
        string head = $"MODEL SET for task judging: Vrf:ModelSet={shown} -> " +
                      (aggregateModelSet ? AggregateModelSet : EntityModelSet) + " (D1, RL-20260927-01). ";
        string rule = aggregateModelSet
            ? "A MEMBERLESS aggregate - an aggregate-level unit, one simulated object with no members (UG52 27.1) - " +
              "counts as ONE position, its own reflected centre point: arrival evidence scores it 1 of 1 and the " +
              "progress watchdog samples it, so a unit that stops short is no longer silent at task level. An " +
              "aggregate that publishes members is judged on its members, as before."
            : "A memberless aggregate is NOT judged (its members have not reflected yet, or it is an empty shell) - " +
              "the behaviour before D1. On the AGGREGATE model set this would leave every unit silent at task level: " +
              "set Vrf:ModelSet=AggregateTacticalLevel (the runner's -ModelSet exports it).";
        string bad = recognised ? "" :
            " WARNING: the value is not EntityLevel or AggregateTacticalLevel, so the safe default EntityLevel is used.";
        return head + rule + bad;
    }
}
