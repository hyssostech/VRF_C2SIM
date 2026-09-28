namespace VrfC2SimApp.Preflight;

/// <summary>
/// The vendor simulation model set a scenario runs on (UG52 13.7: fixed per scenario -
/// EntityLevel.sms or AggregateTacticalLevel.sms). Configured as Vrf:ModelSet (RL-20260927-01); the
/// aggregate-profile lane reads the same key.
/// </summary>
public enum ModelSet { EntityLevel, AggregateTacticalLevel }

/// <summary>
/// WHICH FEATURES STOP A MOVER, AND HOW THE PRE-FLIGHT FLAGS A LEG, PER MODEL SET (RL-20260927-01,
/// docs/PLAN_MOVEMENT_2026-09-27.md M2). Each field is read off the vendor's own files for the terrain
/// that model set runs on; nothing here is tuned.
///
/// ENTITY LEVEL (MAK Earth (online).earth). The ground vehicle's limits are slope and soil (the 0.92
/// ratio, unchanged) and the terrain's VRFSIM features: osm.features.water.xml loads every filtered
/// osm-water feature as a "Lake" (MAK_WATERWAY, featureconfig.txt:413-414, an obstacle to the ground
/// avoider, :405), and the same features sit on top of the soil composite (deep-water, acceleration-
/// factor 0.000, for values 80/82/200). That earth file includes NO river-line layer
/// (osm.features.rivers.xml is included by no shipped .earth), so river LINES are not water here - a
/// wide river is an osm-water polygon and is counted as one. A leg is flagged by the slope ratio OR by
/// OSM water within +/-25 m of the line (the vendor's formation slot spacing; a single platform's
/// avoider swerves inside it).
///
/// AGGREGATE LEVEL (MAK Earth Aggregate (online).earth + VRFSIM.Aggregate.feature.model.xml). The
/// unit's CENTRE POINT is what moves (UG52 27.1.4) and terrain enters only through
/// tank-aggregated-movement.sysdef :112-131: MAK_WATERWAY (Lake, River lines at MAK_WIDTH 5 m,
/// Ocean, Coast) and ALPINE are speed-factor 0 - the unit STOPS; FOREST/URBAN/MOUNTAIN are 0.25;
/// HILLS/CULTIVATED/DESERT 0.65; roads 1.0. So the slope ratio is OFF, a leg is flagged only when
/// impassable water is ON the centreline, and forest/swamp/municipal land use is REPORTED as expected
/// slow, never flagged. NOT READ (named, not hidden): Ocean (osm-oceans z11), Coast (Shallow-10
/// z10), Alpine/Mountain/Hills (z12 sets) - inland AOs so far; a coastal or alpine AO needs them.
///
/// BUILDINGS ARE A LEG FLAG ON NEITHER PROFILE (the code's behaviour, unchanged) and a VERTEX check on both.
/// Entity level: the planner routes round them (Move To per vertex, M1). Aggregate level: the old reason
/// here - "buildings cannot stop an aggregate (27.1.4)" - is REFUTED. MAK Earth Aggregate (online) gives the
/// sim engine the OSM footprints AND simplified 3-D building models (buildings.worldwide.osm.online.xml
/// :11-12, :55); a container's members are placed on the highest surface, roofs included (UG52 14.3.3 p386),
/// and the aggregated movement actuator's max-slope check refuses a member at a footprint edge - 11 of
/// 48 IBCT's Mech COs stopped inside one building of the -2 hamlet in G1-2 with "Terrain too steep"
/// (docs/experiments/FINDING_AGGREGATE_MOVEMENT_OBSTACLES_2026-09-28.md secs 1 and 4(a)). The remedy on
/// record is the TASK, not this rule: a container's route is driven by the vendor's planning task, which
/// plans round MAK_OBSTACLE (buildings and water) - M3, RL-20260928-03 (Vrf:AggregateMovePlanner). A
/// building leg rule for a literally-driven leg is the finding's sec 4(a) proposal and is NOT built here.
/// </summary>
public sealed record ModelSetRules
{
    /// <summary>+/-25 m: the entity-level water corridor round a leg - the vendor's follower slot
    /// spacing ({-50,-25,0,+25,+50}, READ_4-27 sec 2.2), i.e. the lane one vehicle's avoider works in.</summary>
    public const double EntityWaterCorridorMeters = 25.0;

    /// <summary>RESTRICTED_L2 speed-factor (tank-aggregated-movement.sysdef:124-127).</summary>
    public const double AggregateSlowSpeedFactor = 0.25;

    public ModelSet ModelSet { get; init; }
    public bool UseSlope { get; init; }
    public double WaterCorridorMeters { get; init; }
    public bool RiverLinesAreWater { get; init; }
    public bool ReportSlowTerrain { get; init; }

    public static readonly ModelSetRules Entity = new()
    {
        ModelSet = ModelSet.EntityLevel,
        UseSlope = true,
        WaterCorridorMeters = EntityWaterCorridorMeters,
        RiverLinesAreWater = false,
        ReportSlowTerrain = false,
    };

    public static readonly ModelSetRules Aggregate = new()
    {
        ModelSet = ModelSet.AggregateTacticalLevel,
        UseSlope = false,
        WaterCorridorMeters = 0.0,
        RiverLinesAreWater = true,
        ReportSlowTerrain = true,
    };

    public static ModelSetRules For(ModelSet m) => m == ModelSet.AggregateTacticalLevel ? Aggregate : Entity;

    /// <summary>
    /// Vrf:ModelSet -> the enum, through THE SAME PARSER the task judges use
    /// (UnitPositionPolicy.TryParseModelSet, D1): case-insensitive, a trailing ".sms" accepted, absent or
    /// empty = EntityLevel. Two readers of one key with one parser cannot disagree about which model set
    /// a run is on. Anything else is NOT recognised: the caller falls back to EntityLevel and says so
    /// loudly - a wrong model set must not pass silently, and a typo must not stop the interface.
    /// </summary>
    public static bool TryParse(string value, out ModelSet modelSet)
    {
        bool ok = UnitPositionPolicy.TryParseModelSet(value, out bool aggregate);
        modelSet = ok && aggregate ? ModelSet.AggregateTacticalLevel : ModelSet.EntityLevel;
        return ok;
    }

    /// <summary>One line for the start-up log: what flags a leg under this model set.</summary>
    public string Describe() => ModelSet == ModelSet.AggregateTacticalLevel
        ? "AggregateTacticalLevel: slope ratio OFF; a leg is FLAGGED only when OSM water (Lake areas, River lines at " +
          "MAK_WIDTH 5 m) lies ON the centreline - MAK_WATERWAY is speed-factor 0, the unit stops; forest/swamp/" +
          "municipal land use is REPORTED as expected slow (0.25), never flagged; Ocean/Coast/Alpine/Mountain/Hills " +
          "sets are NOT read"
        : FormattableString.Invariant(
              $"EntityLevel: a leg is FLAGGED by the slope ratio OR by OSM water (osm-water Lake areas) within +/-{WaterCorridorMeters:F0} m of the line; river LINES are not water here (MAK Earth (online).earth loads no River layer)");

    /// <summary>
    /// THE LEG RULE, pure: does the pre-dispatch stage act on this leg, and why. Buildings never flag a
    /// leg. A leg with NO VERDICT on slope can still be flagged for water - the two are independent.
    /// </summary>
    public (bool Flagged, bool Slope, bool Water, string Reason) FlagLeg(LegMetrics leg)
    {
        if (leg == null) return (false, false, false, "");
        bool slope = UseSlope && leg.Flagged;
        bool water = leg.Osm?.Water == true;
        var why = new List<string>();
        if (slope)
            why.Add(FormattableString.Invariant($"slope ratio {leg.Ratio:F3} at {leg.WorstSM / 1000.0:F2} km"));
        if (water)
            why.Add(ModelSet == ModelSet.AggregateTacticalLevel
                ? FormattableString.Invariant($"impassable OSM water ON the centreline at {leg.Osm.WaterFirstSM / 1000.0:F2} km ({leg.Osm.WaterKind}, OSM {leg.Osm.WaterId})")
                : FormattableString.Invariant($"OSM water within {WaterCorridorMeters:F0} m of the line at {leg.Osm.WaterFirstSM / 1000.0:F2} km ({leg.Osm.WaterKind}, OSM {leg.Osm.WaterId})"));
        return (slope || water, slope, water, string.Join("; ", why));
    }
}
