namespace VrfC2SimApp;

/// <summary>
/// Decides how a created object is placed vertically. PURE - no bridge, no sim, so it is
/// testable offline (<c>--placement-selftest</c>). Written 2026-09-05 to replace the retired
/// "birth every ground unit at 10000 m MSL so the create clamp drops it" workaround and the
/// SIDC-character ground test that gated it.
///
/// DOCUMENTED BASIS (every branch cites its source):
///  - C2SIM states altitude as AltitudeAGL ("distance vertically above ground level") or
///    AltitudeMSL ("distance vertically above mean sea level"); BOTH are OPTIONAL elements of
///    GeodeticCoordinate (C2SIM_SMX_LOX_CWIX2024.xsd :155, :163, :2716-2717). Every init in
///    data/ carries neither, so "no altitude given" is the normal case, not an edge case.
///  - VR-Forces places a created object on the terrain by default: createEntity/createAggregate
///    default groundClamp=true (vrfRemoteController.h:1275, :1291); the create message: "If True
///    (the default) the object will be created and placed on the nearest polygon"
///    (ifCreateVrfObject.h:210-212). The create altitude is therefore irrelevant for land objects
///    under the default; it is honoured only when the clamp is off, which this interface never does.
///  - Altitude ABOVE GROUND is a first-class API frame: setAltitude(uuid, altitude,
///    bool aboveGroundLevel) (vrfRemoteController.h:1372-1374); VrfFacade.cpp:739 passes TRUE.
///    "if this command is specified for an aggregate leader, the change will apply to the entire
///    aggregate" (:1369-1371) - so one call places a UNIT's platforms too.
///  - Domain is the DIS domain of the type we CREATE (SISO-REF-010.xml:3116-3119: 1 Land, 2 Air,
///    3 Surface, 4 Subsurface) - the simulator's classification, not APP6 symbology.
///  - For air units with no C2SIM altitude there is NO documented default anywhere; the value is
///    an explicit, named, logged knob (VrfSettings.AirDefaultAltitudeAglMeters).
/// </summary>
public static class PlacementPolicy
{
    public const int DomainLand = 1, DomainAir = 2, DomainSurface = 3, DomainSubsurface = 4;

    /// <param name="createAltMeters">altitude to put in the create position (absolute; 0 unless C2SIM gave MSL)</param>
    /// <param name="setAglMeters">post-create setAltitude value in metres ABOVE GROUND, or null for no set</param>
    /// <param name="why">one-line, log-ready reason naming the C2SIM element (or its absence) that decided it</param>
    public readonly record struct Decision(double CreateAltMeters, double? SetAglMeters, string Why);

    public static Decision Decide(int domain, double? c2simAgl, double? c2simMsl, double airDefaultAglMeters)
    {
        // The create position carries C2SIM's MSL if it gave one; otherwise 0. Under the default
        // clamp a land object's create altitude does not decide where it ends up.
        double createAlt = c2simMsl ?? 0.0;

        if (c2simAgl is double agl)
            return new(createAlt, agl, "C2SIM AltitudeAGL -> setAltitude(aboveGroundLevel=TRUE)");

        switch (domain)
        {
            case DomainLand:
                return c2simMsl.HasValue
                    ? new(createAlt, null, "C2SIM AltitudeMSL honoured at create; the default create clamp (ifCreateVrfObject.h:210) may override it")
                    : new(createAlt, 0.0, "C2SIM gave no altitude -> on the ground: setAltitude(0, aboveGroundLevel=TRUE)");
            case DomainAir:
                return c2simMsl.HasValue
                    ? new(createAlt, null, "C2SIM AltitudeMSL at create")
                    : new(createAlt, airDefaultAglMeters, "C2SIM gave no altitude for an AIR unit -> AirDefaultAltitudeAglMeters (ARBITRARY - no documented default; configurable)");
            default:
                // Surface / subsurface / other: the simulator's own water handling - "Subsurface
                // entities will be constrained between the water surface and bottom"
                // (ifCreateVrfObject.h:212). No AGL set unless C2SIM asked for one (handled above).
                return new(createAlt, null, "no AGL set; sim-side surface/depth handling (ifCreateVrfObject.h:212)");
        }
    }
}
