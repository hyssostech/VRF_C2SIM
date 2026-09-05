namespace VrfC2SimApp;

/// <summary>
/// Decides how a created object is placed vertically. PURE - no bridge, no sim, so it is
/// testable offline (<c>--placement-selftest</c>). Written 2026-09-05 to replace the retired
/// "birth every ground unit at 10000 m MSL so the create clamp drops it" workaround and the
/// SIDC-character ground test that gated it. Extended the same day with the TERRAIN-ANCHORED
/// create altitude - MAK's own documented pattern, see the fourth bullet below.
///
/// DOCUMENTED BASIS (every branch cites its source):
///  - C2SIM states altitude as AltitudeAGL ("distance vertically above ground level") or
///    AltitudeMSL ("distance vertically above mean sea level"); BOTH are OPTIONAL elements of
///    GeodeticCoordinate (C2SIM_SMX_LOX_CWIX2024.xsd :155, :163, :2716-2717). Every init in
///    data/ carries neither, so "no altitude given" is the normal case, not an edge case.
///  - VR-Forces places a created object on the terrain by default: createEntity/createAggregate
///    default groundClamp=true (vrfRemoteController.h:1275, :1291); the create message: "If True
///    (the default) the object will be created and placed on the nearest polygon"
///    (ifCreateVrfObject.h:210-212).
///  - THE PLACEMENT RULE IS THE SIMULATOR'S, NOT OURS: "By default, ground, lifeform, rotary-wing,
///    and fixed-wing entities are placed on the ground. Surface and subsurface entities are created
///    at sea level. Ground-based entities are placed at the highest possible terrain intersection
///    at the location." (UG52 14.3.3; help SimObjectsSection/ObjectCreation/
///    vrf_newEntityPlacement.htm). Each created member platform goes through place(location,
///    heading, clampToGround=true) (vrfMovingObjectStateRepository.h:251-253; localObjectManager.h
///    :1013-1025).
///  - CREATE AT THE TERRAIN, DO NOT CREATE AT 0 AND HOPE. MAK's own shipped sample hands the
///    create a point that is ALREADY at the surface: commandLineRemoteController.cxx:710-772
///    creates a Tank_Plt aggregate and its three M1A2 members at one geocentric point
///    (-5506764,-2240896,2301927, "Points are from Ala Moana terrain"), which decodes to
///    1.0 m above the ellipsoid on that near-sea-level terrain, and it never calls setAltitude
///    (docs/VRF_ALTITUDE_FRAMES.md sec 1a, read from C:\MAK\vrforces5.2d\examples 2026-09-05).
///    <see cref="Decide"/> therefore takes a terrain height - obtained by the caller from
///    DtIfRequestTerrainProfileInformation (ifRequestTerrainProfileInformation.h:45-51), the same
///    query the route path already uses - and puts a LAND create at terrain + a small clearance,
///    the shape of MAK's own 1.0 m. When the terrain height is UNKNOWN (no reply, timeout,
///    request not sent) the create altitude is exactly what it was before this change:
///    C2SIM's MSL if given, else 0. The clamp is then the only thing placing the object, which
///    is where PREREG_CLAMP_DIRECTION sec 6 saw a below-terrain create reflect -0.0.
///  - The AGL set is belt-and-braces, and its scope is narrower than the boilerplate suggests:
///    setAltitude(uuid, altitude, bool aboveGroundLevel) (vrfRemoteController.h:1372-1374;
///    VrfFacade.cpp:739 passes TRUE) is carried by DtSetAltitudeRequest, whose header says it "is
///    ignored if the vehicle is not an air-going vehicle" (vrftasks/setAltitudeRequest.h:24-25) -
///    yet a ground M1A2 was observed lifted -0.0 -> 1149.8 m by exactly this call
///    (PREREG_CLAMP_DIRECTION sec 8a; uncontrolled). Header and observation disagree; the set is
///    KEPT because it costs nothing if the header is right and rescues a buried object if the
///    observation is. The confirming run decides. For a UNIT the "applies to the entire aggregate"
///    note (:1369-1371) is generic boilerplate on ~17 setters, and NEITHER unit set-controller
///    registers an altitude callback (vrfmodel/disaggregatedSetController.h:51-71;
///    pseudoAggregatedSetController.h:39-62) - so on a unit this set is documented to do nothing.
///    The documented unit-level lever is setLocation: the unit's formation controller turns it
///    into a snap-into-formation, which issues a DtSetLocationRequest per subordinate, and
///    "Ground vehicles will be clamped to the terrain surface" (vrftasks/setLocationRequest.h
///    :27,31-32). Not used here yet; it is the plan-B named in the confirming-run prereg.
///  - Domain is the DIS domain of the type we CREATE (SISO-REF-010.xml:3116-3119: 1 Land, 2 Air,
///    3 Surface, 4 Subsurface) - the simulator's classification, not APP6 symbology.
///  - For air units with no C2SIM altitude there is NO documented default anywhere; the value is
///    an explicit, named, logged knob (VrfSettings.AirDefaultAltitudeAglMeters).
/// </summary>
public static class PlacementPolicy
{
    public const int DomainLand = 1, DomainAir = 2, DomainSurface = 3, DomainSubsurface = 4;

    /// <param name="createAltMeters">altitude to put in the create position (absolute, MAK-convention
    /// MSL = height above the WGS-84 ellipsoid - docs/VRF_ALTITUDE_FRAMES.md "UNITS")</param>
    /// <param name="setAglMeters">post-create setAltitude value in metres ABOVE GROUND, or null for no set</param>
    /// <param name="why">one-line, log-ready reason naming the C2SIM element (or its absence) that decided it</param>
    /// <param name="createAltFromTerrain">true when the create altitude was anchored to the
    /// terrain-profile answer; false when it came from the fallback (C2SIM MSL, or 0)</param>
    public readonly record struct Decision(double CreateAltMeters, double? SetAglMeters, string Why,
                                           bool CreateAltFromTerrain);

    /// <param name="domain">DIS domain of the type being created (SISO-REF-010.xml:3116-3119)</param>
    /// <param name="c2simAgl">C2SIM GeodeticCoordinate/AltitudeAGL, or null when the init omits it</param>
    /// <param name="c2simMsl">C2SIM GeodeticCoordinate/AltitudeMSL, or null when the init omits it</param>
    /// <param name="airDefaultAglMeters">VrfSettings.AirDefaultAltitudeAglMeters (ARBITRARY - no documented default)</param>
    /// <param name="terrainHeightMeters">the back end's own terrain height under the create point
    /// (DtIfRequestTerrainProfileInformation reply), or null when it is not known - no reply,
    /// timeout, request not sent, or a sample that failed the caller's frame check</param>
    /// <param name="createClearanceMeters">VrfSettings.CreateClearanceMeters, added to the terrain
    /// height for a LAND create (MAK's sample point sits 1.0 m above the ellipsoid on sea-level
    /// terrain - commandLineRemoteController.cxx:710-772 via VRF_ALTITUDE_FRAMES sec 1a)</param>
    public static Decision Decide(int domain, double? c2simAgl, double? c2simMsl,
                                  double airDefaultAglMeters, double? terrainHeightMeters,
                                  double createClearanceMeters)
    {
        // ---------------- the post-create AGL set (the terrain query never changes it) ----------
        // It is stated in the frame C2SIM stated it, and it is BELT-AND-BRACES: the create above is
        // what is meant to place the object (UG52 14.3.3), this only insures against the create
        // landing wrong. Documented "ignored if the vehicle is not an air-going vehicle"
        // (vrftasks/setAltitudeRequest.h:24-25) yet observed to lift a ground M1A2 from -0.0 to
        // 1149.8 m (PREREG_CLAMP_DIRECTION sec 8a, one uncontrolled run). Kept for that reason.
        double? setAgl;
        string setWhy;
        if (c2simAgl is double agl)
        {
            setAgl = agl;
            setWhy = "C2SIM AltitudeAGL -> setAltitude(aboveGroundLevel=TRUE)";
        }
        else if (domain == DomainLand && !c2simMsl.HasValue)
        {
            setAgl = 0.0;
            setWhy = "C2SIM gave no altitude -> on the ground: setAltitude(0, aboveGroundLevel=TRUE)";
        }
        else if (domain == DomainAir && !c2simMsl.HasValue)
        {
            setAgl = airDefaultAglMeters;
            setWhy = "C2SIM gave no altitude for an AIR unit -> AirDefaultAltitudeAglMeters " +
                     "(ARBITRARY - no documented default; configurable)";
        }
        else if (c2simMsl.HasValue)
        {
            setAgl = null;
            setWhy = "C2SIM AltitudeMSL is an absolute - no AGL set";
        }
        else
        {
            // Surface / subsurface / unknown domain: the simulator's own water handling -
            // "Subsurface entities will be constrained between the water surface and bottom"
            // (ifCreateVrfObject.h:212). Never invent an altitude here.
            setAgl = null;
            setWhy = "no AGL set; sim-side surface/depth handling (ifCreateVrfObject.h:212)";
        }

        // ---------------- the create altitude ----------------
        // 1. C2SIM's own MSL, when given, WINS over the terrain query: it is an absolute the
        //    sender authored, and the query only stands in for an altitude nobody stated.
        if (c2simMsl is double msl)
            return new(msl, setAgl,
                       "create alt = C2SIM AltitudeMSL (authored absolute; wins over the terrain query); " + setWhy,
                       false);

        // 2. Terrain known -> create AT the surface, MAK's documented pattern
        //    (commandLineRemoteController.cxx:710-772; UG52 14.3.3). LAND gets the small
        //    clearance; AIR gets the AGL that is in play, so the object is born where it belongs
        //    instead of ~terrain metres underground. SURFACE/SUBSURFACE are left at 0: the vendor
        //    rule for them is "created at sea level" (UG52 14.3.3), not "at the terrain".
        if (terrainHeightMeters is double terrain)
        {
            if (domain == DomainLand)
                return new(terrain + createClearanceMeters, setAgl,
                           string.Format(System.Globalization.CultureInfo.InvariantCulture,
                               "create alt = terrain {0:F1} m + CreateClearanceMeters {1} m (created AT the "
                               + "surface - UG52 14.3.3 + MAK's own sample); ", terrain, createClearanceMeters) + setWhy,
                           true);
            if (domain == DomainAir)
            {
                double above = c2simAgl ?? airDefaultAglMeters;
                return new(terrain + above, setAgl,
                           string.Format(System.Globalization.CultureInfo.InvariantCulture,
                               "create alt = terrain {0:F1} m + {1} m AGL (air; the AGL set repeats it); ",
                               terrain, above) + setWhy,
                           true);
            }
        }

        // 3. FALLBACK - exactly the pre-terrain-query values: C2SIM MSL if given (handled above),
        //    else 0. Reached two ways: no terrain height at all (the real fallback), or a domain
        //    the terrain does not govern - surface/subsurface are "created at sea level" by the
        //    vendor rule (UG52 14.3.3), and an unrecognised domain gets no invented altitude.
        return new(0.0, setAgl,
                   (terrainHeightMeters is null
                        ? "create alt = 0 (FALLBACK - no terrain height for this point; the create clamp "
                          + "is then the only thing placing it, ifCreateVrfObject.h:210-212); "
                        : "create alt = 0 (sea level - the terrain does not govern this domain, UG52 14.3.3); ") + setWhy,
                   false);
    }
}
