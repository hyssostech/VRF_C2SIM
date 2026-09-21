using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// AN OBJECT PLACED ON THE FALLBACK IS RE-MEASURED AND CORRECTED, AND A TASKEE THE APP HAS MEASURED
/// OFF THE TERRAIN IS NEVER TASKED SILENTLY.
///
/// WHAT HAPPENED (run 20260921T114910Z_run, Iron Storm cut A, MAK Earth streaming a cold Suwalki).
/// The init's ONE terrain-profile query for all 36 create points went unanswered inside
/// Vrf:TerrainProfileTimeoutSeconds (`app:225` sent, `app:309` timed out), so every object took
/// PlacementPolicy's FALLBACK arm - create altitude 0, absolute (PlacementPolicy.cs:160-165) - and
/// `app:383` said so: "PLACEMENT summary: 0 of 36 create altitude(s) came from the TERRAIN QUERY,
/// 36 from the FALLBACK". Neither of the two things that place an object could place it, because
/// BOTH resolve against the same terrain the back end had not paged in:
///   - the create clamp, "the object will be created and placed on the nearest polygon"
///     (vrfmsgs/ifCreateVrfObject.h:210-212) - there was no polygon;
///   - the post-create setAltitude(uuid, 0, aboveGroundLevel=TRUE)
///     (vrfcontrol/vrfRemoteController.h:1372-1374, VrfFacade.cpp:739), sent from the ObjectCreated
///     callback (VrfC2SimService.cs:4946-4949) and therefore inside the same cold window; "above
///     the ground" against a ground the back end reads as 0 (terrainDatabase.h:398-399 returns
///     terrainHeight 0.0 when it finds no intersection) is MSL 0.
/// At dispatch the app MEASURED the consequence itself and tasked the units anyway: `app:1135`
/// "taskee altitude not terrain-clamped: live -0.0 m vs terrain 145.4 m under vertex 0 (gap 145 m)
/// - authoring from terrain anyway", `app:1939` the same at 155.8 m.
///
/// *** WHAT THIS CLASS DOES NOT CLAIM. *** docs/VRF_ALTITUDE_FRAMES.md sec 5 and sec 7:
/// "BIRTH ALTITUDE IS NOT THE FREEZE DISCRIMINATOR ... A statement of the form 'born buried,
/// therefore never moves' is ROT. It has re-entered this project at least twice after being
/// falsified." This class makes no causal claim about movement. Its two justifications are
/// independent of it:
///   (1) THE PLACEMENT CONTRACT. "By default, ground, lifeform, rotary-wing, and fixed-wing
///       entities are placed on the ground ... at the highest possible terrain intersection at the
///       location" (UG52 14.3.3; help vrf_newEntityPlacement.htm). An object at MSL 0 under 150 m
///       of terrain is not placed as the vendor documents, whatever that costs it.
///   (2) NEVER TASK WHAT YOU HAVE MEASURED AS WRONG. The app already holds the measurement in its
///       hand at dispatch (TerrainVertexAuthoring.cs:68-73). Acting on it is a reporting duty, not
///       a theory.
///
/// WHY THE CORRECTION IS ISSUED AFTER THE CREATES AND NOT INSTEAD OF THEM. MAK's own sample says
/// creating objects is what makes a paging terrain page: "creating [entities] before the sim starts
/// will insure that when using a paging terrain, the necessary pages will immediately get paged in"
/// (simpleCGF/main.cxx:120-133, read into docs/VRF_ALTITUDE_FRAMES.md sec 1a). In the run above the
/// first terrain answer - to an INIT-path query on the SAME code path, same frame check
/// (`Init (ORDER MATERIALIZATION)`, `app:756`; reply `app:784`) - arrived ~32 s after the init query
/// gave up and AFTER all 36 creates had bound. A retry that refuses to create until the terrain
/// answers may therefore be waiting on something only creating can produce, and it would also drive
/// DispatchReadiness.BarrierSeconds (= 30 - Vrf:TerrainProfileTimeoutSeconds) to its floor and
/// re-open the D5b/B1 overlap. So: create on schedule, then re-ask, then correct, then VERIFY BY
/// READING THE ALTITUDE BACK.
///
/// THE READ-BACK IS NOT OPTIONAL. docs/VRF_ALTITUDE_FRAMES.md sec 1b: the single prior exercise of
/// this call had its "VERIFIED END TO END" WITHDRAWN by adversarial audit, and the stated way to
/// verify is "two buried entities, set ONE, capture the tool output, and sample within ~1 s". A
/// correction this class ISSUED is never recorded as a correction that WORKED.
///
/// PURE. No bridge, no clock, no logging - every decision and every sentence is here, so
/// <c>--placement-reclamp-selftest</c> drives the REAL rule rather than a re-implementation of it.
/// </summary>
public static class PlacementReclampPolicy
{
    /// <summary>The prefix an operator greps for.</summary>
    public const string Prefix = "PLACEMENT RE-CLAMP";

    /// <summary>The readiness token the dispatch gate names. The brief's own wording.</summary>
    public const string NotOnGroundToken = "BOUND-BUT-NOT-ON-THE-GROUND";

    /// <summary>The setting that bounds the whole re-clamp, named in the lines so it can be found.</summary>
    public const string BoundSettingKey = "Vrf:PlacementReclampSeconds";

    /// <summary>The setting that sets N - the gap at which a taskee is not tasked.</summary>
    public const string ToleranceSettingKey = "Vrf:PlacementReclampToleranceMeters";

    /// <summary>
    /// WHAT THE APP KNOWS ABOUT ONE OBJECT'S GROUND CONTACT. The default is
    /// <see cref="Unknown"/> and it is PERMISSIVE by construction: an object nobody has measured is
    /// tasked exactly as it is today. Only a MEASUREMENT can hold a task.
    /// </summary>
    public enum Contact
    {
        /// <summary>No usable terrain answer for this object yet - the state every object is in on a
        /// run where the terrain query is never answered. Never holds anything.</summary>
        Unknown = 0,
        /// <summary>Measured: live altitude within tolerance of the terrain under it.</summary>
        OnGround = 1,
        /// <summary>Measured: live altitude further than the tolerance from the terrain under it.
        /// This is the only value that holds a task.</summary>
        OffGround = 2,
    }

    /// <summary>What a sweep decided to DO about one object this pass.</summary>
    public enum Action
    {
        /// <summary>Nothing to do (no answer yet, or already on the ground).</summary>
        None = 0,
        /// <summary>Issue setAltitude(uuid, 0, aboveGroundLevel=TRUE) and wait for the read-back.</summary>
        Correct = 1,
        /// <summary>A correction was already issued and the read-back still disagrees: stop
        /// correcting and let the verdict stand, loudly.</summary>
        GiveUp = 2,
    }

    /// <summary>How one object's re-clamp ENDED. One of these is reported per object, once.</summary>
    public enum Outcome
    {
        /// <summary>Still running.</summary>
        Pending = 0,
        /// <summary>Measured on the ground without needing a correction (the terrain simply answered
        /// later, and the create clamp had already done its job once the page arrived).</summary>
        AlreadyOnGround = 1,
        /// <summary>A correction was issued AND the read-back confirmed it.</summary>
        Reclamped = 2,
        /// <summary>A correction was issued and the read-back did NOT confirm it.</summary>
        StillOffGround = 3,
        /// <summary>The bound expired with no usable terrain answer for this object - nothing was
        /// measured, so nothing is claimed and nothing is held.</summary>
        NeverMeasured = 4,
    }

    /// <summary>The verdict on one measurement. <paramref name="GapMeters"/> is 0 when unknown.</summary>
    public readonly record struct Measurement(Contact Contact, double GapMeters,
                                              double LiveAltMeters, double TerrainMeters);

    /// <summary>
    /// THE MEASUREMENT. Both inputs are absolute and in the SAME frame - MAK-convention MSL, i.e.
    /// height above the WGS-84 ellipsoid (docs/VRF_ALTITUDE_FRAMES.md "UNITS": our readback is
    /// DtGeodeticCoord::alt() with no geoid datum, and MAK defines MSL as the ellipsoid,
    /// vrf_egmModel.htm:199,:201). The terrain height comes from the back end's own
    /// DtIfRequestTerrainProfileInformation reply, which is the same number the route path authors
    /// vertices from - so a gap here is the app disagreeing with itself, not with a model.
    /// </summary>
    public static Measurement Measure(double liveAltMeters, double? terrainMeters, double toleranceMeters)
    {
        if (terrainMeters is not double terrain)
            return new Measurement(Contact.Unknown, 0.0, liveAltMeters, 0.0);
        double gap = Math.Abs(liveAltMeters - terrain);
        double tol = Math.Max(0.0, toleranceMeters);
        return new Measurement(gap > tol ? Contact.OffGround : Contact.OnGround, gap, liveAltMeters, terrain);
    }

    /// <summary>
    /// WHAT TO DO ABOUT A MEASUREMENT. <paramref name="correctionsIssued"/> is how many times this
    /// object has already been corrected; <see cref="MaxCorrections"/> bounds it, so a back end that
    /// ignores the set (setAltitudeRequest.h:24-25 says it "is ignored if the vehicle is not an
    /// air-going vehicle") produces ONE loud give-up rather than a set every sweep for the bound.
    /// </summary>
    public static Action Decide(Contact contact, int correctionsIssued)
        => contact != Contact.OffGround ? Action.None
         : correctionsIssued < MaxCorrections ? Action.Correct
         : Action.GiveUp;

    /// <summary>
    /// How many corrections one object gets. ONE. The call is documented to be ignored for a
    /// non-air vehicle and was observed once, uncontrolled, to work on a ground M1A2
    /// (docs/VRF_ALTITUDE_FRAMES.md sec 1b) - so a second identical set adds no information and a
    /// loop of them would be the "false green" shape this project already has a memory note about.
    /// </summary>
    public const int MaxCorrections = 1;

    /// <summary>The altitude the correction asks for: 0 m ABOVE GROUND LEVEL. Not a number anyone
    /// picked - it is the same value PlacementPolicy already computes for a land object whose C2SIM
    /// init gives no altitude ("C2SIM gave no altitude -> on the ground: setAltitude(0,
    /// aboveGroundLevel=TRUE)", PlacementPolicy.cs:99-103), delivered through the one call the
    /// bridge already exposes (VrfBridge.cpp:426 -> VrfFacade.cpp:739, aboveGroundLevel=TRUE).</summary>
    public const double CorrectionAglMeters = 0.0;

    /// <summary>Has the whole re-clamp run out of time? Wall seconds since the arm.</summary>
    public static bool Expired(double elapsedSeconds, double boundSeconds)
        => elapsedSeconds >= Math.Max(0.0, boundSeconds);

    /// <summary>May another terrain query be issued now? One in flight at a time, and no faster than
    /// the retry interval - the query is a back-end round trip and this sweep runs every tick.</summary>
    public static bool MayQuery(bool queryInFlight, double secondsSinceLastQuery, double retrySeconds)
        => !queryInFlight && secondsSinceLastQuery >= Math.Max(0.1, retrySeconds);

    /// <summary>
    /// The state one object ends in. Kept separate from <see cref="Decide"/> so the summary can
    /// never disagree with what was done.
    /// </summary>
    public static Outcome Conclude(Contact contact, int correctionsIssued)
        => contact switch
        {
            Contact.OnGround => correctionsIssued > 0 ? Outcome.Reclamped : Outcome.AlreadyOnGround,
            Contact.OffGround => correctionsIssued > 0 ? Outcome.StillOffGround : Outcome.Pending,
            _ => Outcome.NeverMeasured,
        };

    // ===================== THE SENTENCES =====================

    /// <summary>ONE line when the re-clamp arms. It says what is wrong and what will be done, so a
    /// run that ends here is still readable.</summary>
    public static string ArmedLine(int objects, int planned, double boundSeconds, double retrySeconds,
                                   double toleranceMeters)
        => string.Format(CultureInfo.InvariantCulture,
               "{0}: {1} of {2} object(s) were created at the FALLBACK altitude because the init's "
             + "terrain-profile query was not answered, so nothing has placed them on the terrain - "
             + "neither the create clamp (ifCreateVrfObject.h:210-212) nor the post-create AGL set "
             + "(vrfRemoteController.h:1372-1374) can resolve against a terrain page that is not "
             + "loaded. The terrain is re-asked every {3:F0} s for up to {4:F0} s ({5}) once each "
             + "object is bound; anything further than {6:F0} m ({7}) from the terrain under it is "
             + "corrected with setAltitude(0, aboveGroundLevel=TRUE) and CONFIRMED BY READING THE "
             + "ALTITUDE BACK. Creates were NOT delayed for this: MAK's own sample says creating is "
             + "what pages a streaming terrain in (simpleCGF/main.cxx:120-133).",
               Prefix, objects, planned, retrySeconds, boundSeconds, BoundSettingKey,
               toleranceMeters, ToleranceSettingKey);
    /// <summary>One line per object when a correction is ISSUED. It is not a claim that it worked.</summary>
    public static string CorrectionLine(string name, Measurement m, double toleranceMeters)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} {1}: MEASURED OFF THE TERRAIN - live {2:F1} m vs terrain {3:F1} m under its "
             + "create point (gap {4:F0} m, tolerance {5:F0} m); both are MAK-convention MSL "
             + "(WGS-84 ellipsoid, docs/VRF_ALTITUDE_FRAMES.md 'UNITS'). Issuing setAltitude(0 m "
             + "ABOVE GROUND LEVEL). THIS IS NOT YET A FIX: the next sweep reads the altitude back "
             + "and only a read-back within tolerance is recorded as a correction.",
               Prefix, name, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, toleranceMeters);

    /// <summary>One line per object when the read-back CONFIRMS the correction.</summary>
    public static string VerifiedLine(string name, Measurement before, Measurement after)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} {1}: RE-CLAMPED AND VERIFIED - live altitude read back at {2:F1} m against "
             + "terrain {3:F1} m (gap {4:F0} m, was {5:F0} m). The object is on the terrain and the "
             + "read is the evidence, not the set.",
               Prefix, name, after.LiveAltMeters, after.TerrainMeters, after.GapMeters, before.GapMeters);

    /// <summary>One line per object when the read-back does NOT confirm it. Loud, and it names the
    /// documented reason the set may legitimately have done nothing.</summary>
    public static string GaveUpLine(string name, Measurement m)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} {1}: STILL OFF THE TERRAIN AFTER A CORRECTION - live {2:F1} m vs terrain "
             + "{3:F1} m (gap {4:F0} m). setAltitude is documented as \"ignored if the vehicle is "
             + "not an air-going vehicle\" (vrftasks/setAltitudeRequest.h:24-25), so this may be the "
             + "vendor behaving as written rather than a failure. No further set is issued. A task "
             + "on this unit is HELD as [{5}] and abandoned rather than driven from under the "
             + "ground; the documented unit-level lever that has NOT been tried is setLocation, "
             + "whose formation controller clamps ground vehicles to the surface "
             + "(vrftasks/setLocationRequest.h:27,31-32).",
               Prefix, name, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, NotOnGroundToken);

    /// <summary>THE LINE A PREREG SCORES. One per run, when the re-clamp finishes or expires.</summary>
    public static string SummaryLine(int alreadyOnGround, int reclamped, int stillOff, int neverMeasured,
                                     double wallSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} summary: {1} object(s) measured ON the terrain, {2} RE-CLAMPED AND VERIFIED, "
             + "{3} STILL OFF the terrain, {4} NEVER MEASURED (no terrain answer within {5}), after "
             + "{6:F1} s. Only the first two are safe to task; the third is held and abandoned; the "
             + "fourth is tasked exactly as it is today, because nothing was measured and nothing is "
             + "claimed.",
               Prefix, alreadyOnGround, reclamped, stillOff, neverMeasured, BoundSettingKey, wallSeconds);

    /// <summary>
    /// The line the DISPATCH gate prints when it refuses to task on the vertex-0 measurement. This
    /// is the measurement the app already made and then ignored (`app:1135`, `app:1939`).
    /// </summary>
    public static string DispatchGateHeldLine(string taskName, string unitName, double liveAltMeters,
                                              double terrainMeters, double gapMeters, double toleranceMeters)
        => string.Format(CultureInfo.InvariantCulture,
               "{0}: task '{1}' is NOT DISPATCHED YET - the route's own terrain reply puts {2} at "
             + "live {3:F1} m against terrain {4:F1} m under vertex 0 (gap {5:F0} m, tolerance "
             + "{6:F0} m / {7}). Until 2026-09-21 this was logged and the task was dispatched anyway "
             + "(\"authoring from terrain anyway\"), which is how run 20260921T114910Z tasked two "
             + "platforms measured 145 m and 156 m off the terrain. The correction is issued now and "
             + "the task is HELD as [{8}] until the altitude READS BACK on the terrain, bounded by "
             + "{9}; on expiry the task is abandoned with that state named.",
               Prefix, taskName, unitName, liveAltMeters, terrainMeters, gapMeters, toleranceMeters,
               ToleranceSettingKey, NotOnGroundToken, DispatchReadiness.TimeoutSettingKey);

    /// <summary>
    /// The TASKABRT reason when a task comes back to the gate a SECOND time and the taskee is STILL
    /// off the terrain. Bounded by construction: the gate defers each task at most once, so a
    /// re-clamp that does not take costs one extra round trip and not a loop.
    /// </summary>
    public static string DispatchGateRefusalReason(string taskName, string unitName, double liveAltMeters,
                                                   double terrainMeters, double gapMeters,
                                                   double toleranceMeters)
        => string.Format(CultureInfo.InvariantCulture,
               "REFUSED [{0}]: task '{1}' is not dispatched because unit {2} is STILL measured off "
             + "the terrain after a correction and a re-measure - live {3:F1} m against terrain "
             + "{4:F1} m under vertex 0 (gap {5:F0} m, tolerance {6:F0} m / {7}). The interface "
             + "will not task a unit it has itself measured off the ground the vendor documents it "
             + "should be placed on (UG52 14.3.3). This is a PLACEMENT failure, not a task failure: "
             + "check the {8} lines above and whether the terrain under this AO had streamed when "
             + "the initialization's creates went out.",
               NotOnGroundToken, taskName, unitName, liveAltMeters, terrainMeters, gapMeters,
               toleranceMeters, ToleranceSettingKey, Prefix);
}
