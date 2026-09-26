using System.Globalization;
using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// AN OBJECT PLACED ON THE FALLBACK IS RE-MEASURED AND CORRECTED, AND A TASKEE THE APP HAS MEASURED
/// OFF THE TERRAIN IS NEVER TASKED SILENTLY - the measurement is LOGGED at dispatch.
///
/// *** 2026-09-25 (RL-20260921-06; the completion unit's scope, approved as RL-20260925-01): THE
/// PLACEMENT STAYS, THE DISPATCH GATE AND THE FALSE TALLY GO. *** From 8aeb127 until then a taskee
/// measured off the terrain was HELD as BOUND-BUT-NOT-ON-THE-GROUND until a read-back confirmed its
/// correction, and refused with a TASKABRT if none did. MEASUREMENT (run 20260921T143243Z, n = 1):
/// thirty-two objects were measured off the terrain and sent a setLocation, an independent trace
/// shows them at the terrain height afterwards, the read-back never landed (why is undiagnosed),
/// the summary said "0 RE-CLAMPED AND VERIFIED ... 32 NEVER MEASURED", and the gate ended two
/// tasks. So now: nothing holds, refuses or abandons a task on a ground-contact verdict (the
/// dispatch-time measurement is only logged, and no setLocation is sent at dispatch); an object
/// that was corrected and whose read-back never landed is counted as exactly that
/// (<see cref="Outcome.CorrectedNotVerified"/>), not as never measured. Where the text below says
/// a task is held or refused, it describes the design before that date.
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
///   (2) NEVER TASK WHAT YOU HAVE MEASURED AS WRONG WITHOUT SAYING SO. The app already holds the
///       measurement in its hand at dispatch (TerrainVertexAuthoring.cs:68-73). Reporting it is a
///       duty, not a theory. (Until 2026-09-25 this read "NEVER TASK WHAT YOU HAVE MEASURED AS
///       WRONG" and the gate held the task; see the banner above.)
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

    /// <summary>The setting that bounds the whole re-clamp, named in the lines so it can be found.</summary>
    public const string BoundSettingKey = "Vrf:PlacementReclampSeconds";

    /// <summary>The setting that sets N - the gap above which an object is MEASURED off the terrain
    /// (a correction for an enrolled idle object, a logged line at dispatch; since 2026-09-25 it
    /// holds no task).</summary>
    public const string ToleranceSettingKey = "Vrf:PlacementReclampToleranceMeters";

    /// <summary>
    /// WHAT THE APP KNOWS ABOUT ONE OBJECT'S GROUND CONTACT. The default is
    /// <see cref="Unknown"/>. Since 2026-09-25 no value holds a task; the contact decides whether
    /// the init sweep corrects an object and what the dispatch-time line says.
    /// </summary>
    public enum Contact
    {
        /// <summary>No usable terrain answer for this object yet - the state every object is in on a
        /// run where the terrain query is never answered.</summary>
        Unknown = 0,
        /// <summary>Measured: live altitude within tolerance of the terrain under it.</summary>
        OnGround = 1,
        /// <summary>Measured: live altitude further than the tolerance from the terrain under it.</summary>
        OffGround = 2,
    }

    /// <summary>What a sweep decided to DO about one object this pass.</summary>
    public enum Action
    {
        /// <summary>Nothing to do (no answer yet, or already on the ground).</summary>
        None = 0,
        /// <summary>Issue the setLocation correction at the object's own live lat/lon and wait for
        /// the read-back (see <see cref="CorrectionLocation"/> for why it is not a setAltitude).</summary>
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
        /// <summary>MEASURED off the terrain at the last read: a correction was issued and the
        /// read-back did NOT confirm it, or (concluded at the bound) the object was measured off
        /// the terrain while a task was in flight on it, so no correction was sent.</summary>
        StillOffGround = 3,
        /// <summary>The bound expired with no usable terrain answer for this object - nothing was
        /// measured, so nothing is claimed and nothing is held.</summary>
        NeverMeasured = 4,
        /// <summary>2026-09-25. The object was measured off the terrain and a correction WAS
        /// issued, and the bound expired with no read-back of it. Before this outcome existed these
        /// objects were counted NEVER MEASURED - the "32 NEVER MEASURED" beside thirty-two
        /// correction lines of run 20260921T143243Z (RL-20260921-06). Nothing is claimed about
        /// whether the correction worked.</summary>
        CorrectedNotVerified = 5,
    }

    /// <summary>
    /// How an object STILL PENDING at the end of a window is concluded (the service's
    /// ConcludePlacementReclamp). A correction with no read-back is <see cref="Outcome.CorrectedNotVerified"/>;
    /// a measurement off the terrain with no correction (the unit had a task in flight) is
    /// <see cref="Outcome.StillOffGround"/>; only an object with NO usable measurement at all is
    /// <see cref="Outcome.NeverMeasured"/>. A settled outcome is returned unchanged.
    /// </summary>
    public static Outcome ConcludeAtBound(Outcome current, int correctionsIssued, Contact lastContact)
        => current != Outcome.Pending ? current
         : correctionsIssued > 0 ? Outcome.CorrectedNotVerified
         : lastContact == Contact.OffGround ? Outcome.StillOffGround
         : lastContact == Contact.OnGround ? Outcome.AlreadyOnGround
         : Outcome.NeverMeasured;

    /// <summary>The five counts the summary reports; they sum to the enrolled count.</summary>
    public readonly record struct Counts(int OnGround, int Reclamped, int CorrectedNotVerified,
                                         int StillOff, int NeverMeasured)
    {
        public int Total => OnGround + Reclamped + CorrectedNotVerified + StillOff + NeverMeasured;
    }

    /// <summary>A census of concluded outcomes. A still-Pending entry (none should reach here: the
    /// service concludes them first) is counted as never measured, never silently dropped.</summary>
    public static Counts Tally(IEnumerable<Outcome> outcomes)
    {
        int a = 0, r = 0, c = 0, s = 0, n = 0;
        foreach (var o in outcomes)
            switch (o)
            {
                case Outcome.AlreadyOnGround: a++; break;
                case Outcome.Reclamped: r++; break;
                case Outcome.CorrectedNotVerified: c++; break;
                case Outcome.StillOffGround: s++; break;
                default: n++; break;
            }
        return new Counts(a, r, c, s, n);
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
    /// How many corrections one object gets. ONE. A second identical request adds no information,
    /// and a loop of them would be the "false green" shape this project already has a memory note
    /// about. The bound is on the REQUEST, not on the MEASUREMENT: an object that has spent its
    /// correction is still re-measured whenever a new task asks about it (see the service's
    /// re-arm), because a terrain page that arrives late can make the verdict wrong.
    /// </summary>
    public const int MaxCorrections = 1;

    /// <summary>
    /// *** THE CORRECTION IS A setLocation, NOT A setAltitude. THE VENDOR HEADERS DECIDE THIS, AND
    /// THEY DISAGREE WITH EACH OTHER ABOUT WHICH CALL MOVES A GROUND VEHICLE. *** Both read by
    /// named path 2026-09-21 from C:\MAK\vrforces5.2d\include:
    ///
    ///   vrftasks/setAltitudeRequest.h:23-25 - "DtSetAltitudeRequest is used to set the altitude
    ///   for an entity. It is IGNORED IF THE VEHICLE IS NOT AN AIR-GOING VEHICLE." (and the
    ///   altitude "is the height above the terrain in local coordinates").
    ///
    ///   vrftasks/setLocationRequest.h:26-32 - "DtSetLocationRequest is used to force a location
    ///   for (sometimes called teleporting) an entity ... Z IS IGNORED FOR NON-AIR VEHICLES ...
    ///   parameters: location - The new location for the entity, in geocentric (meters).
    ///   GROUND VEHICLES WILL BE CLAMPED TO THE TERRAIN SURFACE."
    ///
    /// So for the exact class this policy enrols - DIS domain 1, LAND - the vendor documents
    /// setAltitude as a NO-OP and setLocation as the thing that clamps it to the surface. A first
    /// version of this class used setAltitude; that was choosing the one documented not to work.
    /// (docs/VRF_ALTITUDE_FRAMES.md sec 1b's single prior observation of a ground M1A2 apparently
    /// lifted by an AGL set is ONE UNCONTROLLED RUN whose "VERIFIED END TO END" was WITHDRAWN by
    /// audit - it is not evidence against a header, and it is not what this builds on.)
    ///
    /// NO NATIVE CHANGE: SetLocation is already exposed, VrfBridge.cpp:429 ->
    /// VrfFacade.cpp:986-987 `controller->setLocation(DtUUID(uuid), toGeocentric(pos))`.
    ///
    /// WHAT THE CALL WANTS AND IN WHICH FRAME. A geodetic lat/lon in DEGREES plus an altitude in
    /// METRES, which VrfFacade::toGeocentric (VrfFacade.cpp:133-138) turns into the geocentric
    /// vector the request carries. The altitude is MAK-convention MSL - height above the WGS-84
    /// ellipsoid (docs/VRF_ALTITUDE_FRAMES.md "UNITS") - the same frame the create position uses.
    /// Per the header that Z IS IGNORED for a ground vehicle, so its value cannot be the thing that
    /// places the object; <see cref="CorrectionAltitude"/> nevertheless sends the altitude the
    /// CREATE would have used had the terrain answered, so the request is right in both readings
    /// and nothing rests on a number the vendor says it discards.
    ///
    /// THE LAT/LON IS THE OBJECT'S OWN. Not a new position - this is a correction, not a move. The
    /// enrolled entry carries the create point; the sweep uses the object's own live position. Sending anything else would teleport a unit to fix its altitude.
    /// </summary>
    public static Geodetic CorrectionLocation(double latDeg, double lonDeg, double terrainMeters,
                                              double createClearanceMeters)
        => new Geodetic { LatDeg = latDeg, LonDeg = lonDeg,
                          AltMeters = CorrectionAltitude(terrainMeters, createClearanceMeters) };

    /// <summary>The altitude that goes in the correction: terrain + Vrf:CreateClearanceMeters, i.e.
    /// exactly what PlacementPolicy.Decide's terrain arm would have produced for this LAND object
    /// had the init query been answered (PlacementPolicy.cs:137-144). Documented as IGNORED for a
    /// ground vehicle (setLocationRequest.h:26-32); sent correct anyway.</summary>
    public static double CorrectionAltitude(double terrainMeters, double createClearanceMeters)
        => terrainMeters + createClearanceMeters;

    /// <summary>
    /// *** THE HARD GUARD: THIS REQUEST MAY CHANGE AN ALTITUDE. IT MAY NEVER MOVE A UNIT IN PLAN.
    /// *** (DL-1, delta review of b3f9c38.) setLocationRequest.h:26 calls the request "force a
    /// location for (sometimes called TELEPORTING) an entity" - so the moment the correction became
    /// a setLocation, sending anything but the unit's own CURRENT lat/lon stopped being a
    /// correction and became a teleport. A first version of the init sweep sent the enrolled CREATE
    /// point: harmless for the stationary object it was written against, and a yank back to its
    /// birth coordinate for a unit that had begun driving.
    ///
    /// THE BOUND IS 1 METRE, and it is chosen to be unreachable by anything except a defect. The
    /// caller reads the live position and builds the correction from THAT SAME READ, so the only
    /// horizontal difference possible is numerical: 1e-7 degrees is about 1.1 cm of latitude, five
    /// orders below this bound. It is also far below any real displacement - the slowest movement
    /// this project has ever recorded is 0.28 m/s (the Iron Storm T10 crawl), so 1 m is ~3.5 s of
    /// the slowest motion on record. A correction that fails this test is not "slightly off": it is
    /// code about to move a unit somewhere it did not drive, and it is refused.
    /// </summary>
    public const double MaxCorrectionHorizontalMeters = 1.0;

    /// <summary>
    /// TRUE when the correction would displace the unit horizontally - i.e. when it must NOT be
    /// sent. Pure, so <c>--placement-reclamp-selftest</c> asserts the invariant rather than a
    /// comment claiming it.
    /// </summary>
    public static bool WouldMoveHorizontally(double liveLatDeg, double liveLonDeg,
                                             double fixLatDeg, double fixLonDeg,
                                             double maxMeters = MaxCorrectionHorizontalMeters)
        => TerrainVertexAuthoring.DistMeters(liveLatDeg, liveLonDeg, fixLatDeg, fixLonDeg) > maxMeters;

    /// <summary>
    /// The ERROR when the guard above fires. It should be unreachable - the caller builds the fix
    /// from the live read - so if it is ever printed, something between the read and the send is
    /// wrong and the unit is left where it is rather than moved.
    /// </summary>
    public static string RefusedToMoveLine(string name, double liveLatDeg, double liveLonDeg,
                                           double fixLatDeg, double fixLonDeg)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} {1}: CORRECTION REFUSED - it would have moved the unit {2:F1} m horizontally, "
             + "from its live {3:F6},{4:F6} to {5:F6},{6:F6}. setLocation is a TELEPORT "
             + "(setLocationRequest.h:26) and this path may only change an altitude, never a "
             + "position. Nothing was sent; the unit keeps its place and its altitude verdict "
             + "stands. THIS LINE SHOULD BE UNREACHABLE - the correction is built from the same "
             + "live read it is checked against, so seeing it means that invariant broke.",
               Prefix, name, TerrainVertexAuthoring.DistMeters(liveLatDeg, liveLonDeg, fixLatDeg, fixLonDeg),
               liveLatDeg, liveLonDeg, fixLatDeg, fixLonDeg);

    /// <summary>
    /// The line when a correction is withheld because the unit IS UNDER A TASK. Also DL-1: a
    /// teleport into a running move is worse than a wrong altitude - the vendor's movement
    /// controller is mid-plan and the C2 side has been told the task started.
    /// </summary>
    public static string SkippedTaskInFlightLine(string name, string taskName, Measurement m)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} {1}: measured off the terrain (live {2:F1} m vs terrain {3:F1} m, gap {4:F0} m) "
             + "but NO CORRECTION IS ISSUED - task '{5}' is in flight on this unit, and setLocation "
             + "is a teleport (setLocationRequest.h:26) that would yank a moving unit out of its "
             + "own route. The measurement stands and is re-taken when a later task asks about this "
             + "unit; a unit is corrected before it is tasked, never during.",
               Prefix, name, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, taskName);

    /// <summary>
    /// DL-1: the unit moved between the terrain query and its reply, so the answer is about ground
    /// it no longer stands on. Not an error - just a measurement that does not apply. Reuses the
    /// repo's existing calibrated notion of "this sample answers this point",
    /// TerrainVertexAuthoring.DefaultMaxHorizontalMismatchMeters (50 m), rather than inventing a
    /// second number for the same idea.
    /// </summary>
    public static bool DriftedSinceQuery(double queriedLatDeg, double queriedLonDeg,
                                         double liveLatDeg, double liveLonDeg)
        => TerrainVertexAuthoring.DistMeters(queriedLatDeg, queriedLonDeg, liveLatDeg, liveLonDeg)
           > TerrainVertexAuthoring.DefaultMaxHorizontalMismatchMeters;

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
               "{0}: {1} of {2} object(s) - LAND PLATFORMS ONLY - were created at the FALLBACK "
             + "altitude because the init's terrain-profile query was not answered, so nothing has "
             + "placed them on the terrain: the create clamp needs a polygon "
             + "(ifCreateVrfObject.h:210-212) and a terrain page that is not loaded has none. The "
             + "terrain is re-asked every {3:F0} s for up to {4:F0} s ({5}) once each object is "
             + "bound; anything further than {6:F0} m ({7}) from the terrain under it is corrected "
             + "with setLocation at its own lat/lon - \"Ground vehicles will be clamped to the "
             + "terrain surface\", setLocationRequest.h:26-32 - and CONFIRMED BY READING THE "
             + "ALTITUDE BACK. AGGREGATES ARE NOT ENROLLED: a unit's published Z is a derived "
             + "bounding-box quantity whose rule is NOT DOCUMENTED, so it is not a ground-contact "
             + "measurement (VRF_ALTITUDE_FRAMES sec 1a); an aggregate taskee is measured at "
             + "dispatch on its MEMBERS' centroid instead. Creates were NOT delayed for this: MAK's "
             + "own sample says creating is what pages a streaming terrain in "
             + "(simpleCGF/main.cxx:120-133).",
               Prefix, objects, planned, retrySeconds, boundSeconds, BoundSettingKey,
               toleranceMeters, ToleranceSettingKey);
    /// <summary>One line per object when a correction is ISSUED. It is not a claim that it worked.</summary>
    public static string CorrectionLine(string name, Measurement m, double toleranceMeters,
                                        double latDeg, double lonDeg, double sentAltMeters)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} {1}: MEASURED OFF THE TERRAIN - live {2:F1} m vs terrain {3:F1} m under its "
             + "own point (gap {4:F0} m, tolerance {5:F0} m); both are MAK-convention MSL (WGS-84 "
             + "ellipsoid, docs/VRF_ALTITUDE_FRAMES.md 'UNITS'). Issuing setLocation at its OWN "
             + "lat/lon {6:F6},{7:F8} with altitude {8:F1} m (terrain + Vrf:CreateClearanceMeters). "
             + "setLocation AND NOT setAltitude, by the vendor's own headers: "
             + "setAltitudeRequest.h:23-25 says the altitude request \"is ignored if the vehicle is "
             + "not an air-going vehicle\", while setLocationRequest.h:26-32 says \"Z is ignored for "
             + "non-air vehicles\" and \"Ground vehicles will be clamped to the terrain surface\" - "
             + "so for a LAND object the clamp is what places it and the altitude sent is expected "
             + "to be discarded. THIS IS NOT YET A FIX: the next sweep reads the altitude back and "
             + "only a read-back within tolerance is recorded as a correction.",
               Prefix, name, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, toleranceMeters,
               latDeg, lonDeg, sentAltMeters);

    /// <summary>
    /// BL-2 (cold-start review of 3151fec). One line when a unit that had been judged OFF the
    /// terrain is later MEASURED ON it - a late terrain page, or the back end re-placing the object
    /// on its own. Without this the first bad verdict would stand for the life of the process and
    /// every later task on that unit would be held to its bound and abandoned, with nothing in the
    /// app ever looking again.
    /// </summary>
    public static string ClearedLine(string name, Measurement m)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} {1}: CLEARED - re-measured ON the terrain, live {2:F1} m vs terrain {3:F1} m "
             + "(gap {4:F0} m). The earlier off-the-terrain verdict is withdrawn and this unit is "
             + "taskable again; a verdict is a measurement and it expires when a newer measurement "
             + "disagrees with it.",
               Prefix, name, m.LiveAltMeters, m.TerrainMeters, m.GapMeters);

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
             + "{3:F1} m (gap {4:F0} m). The correction issued was the one the vendor documents for "
             + "this class - setLocation, \"Ground vehicles will be clamped to the terrain surface\" "
             + "(vrftasks/setLocationRequest.h:26-32) - so a read-back that still disagrees is NOT "
             + "the known setAltitude no-op and needs explaining rather than excusing. Candidates, "
             + "none adjudicated here: the clamp needs a terrain page this point still lacks; the "
             + "read is of a reflected state that has not caught up; or the request was rejected. "
             + "NO FURTHER REQUEST IS ISSUED for this object - one correction, then the "
             + "measurement speaks. The verdict is RE-MEASURED whenever a new task asks about this "
             + "unit, so a late terrain page clears it; it does not hold or refuse any task.",
               Prefix, name, m.LiveAltMeters, m.TerrainMeters, m.GapMeters);

    /// <summary>
    /// SF-2 (cold-start review). ONE short line per ground dispatch that the gate PASSES, so a
    /// healthy run produces the distribution of measured gaps instead of only ever proving the
    /// refusal path. Without it the 50-100 m band - between this policy's refusal bar and the
    /// vertex-0 NOTE threshold - has no evidence in any log.
    /// </summary>
    public static string GatePassedLine(string unitName, Measurement m, double toleranceMeters)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} gate: {1} is ON the terrain - live {2:F1} m vs terrain {3:F1} m under vertex 0 "
             + "(gap {4:F1} m, tolerance {5:F0} m). Dispatching.",
               Prefix, unitName, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, toleranceMeters);

    /// <summary>THE LINE A PREREG SCORES. One per window, when the re-clamp finishes or expires.
    /// FIVE counts that sum to the enrolled count (2026-09-25: the third is new, and the old
    /// "the third is held and abandoned" clause is gone - nothing is held on a verdict any more).</summary>
    public static string SummaryLine(Counts c, double wallSeconds)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} summary: {1} object(s) measured ON the terrain, {2} RE-CLAMPED AND VERIFIED, "
             + "{3} CORRECTED, READ-BACK NOT RECEIVED, {4} STILL OFF the terrain, {5} NEVER MEASURED "
             + "(no terrain answer within {6}), after {7:F1} s ({8} enrolled). Only a read-back is "
             + "evidence that a correction worked; none of these holds or refuses a task.",
               Prefix, c.OnGround, c.Reclamped, c.CorrectedNotVerified, c.StillOff, c.NeverMeasured,
               BoundSettingKey, wallSeconds, c.Total);

    /// <summary>
    /// The line the DISPATCH-TIME measurement prints for a taskee measured OFF the terrain
    /// (2026-09-25: it replaces the "NOT DISPATCHED YET" hold line and the REFUSED abort reason).
    /// The task is dispatched; the line is the record.
    /// </summary>
    public static string DispatchGateOffTerrainLine(string taskName, string unitName, Measurement m,
                                                    double toleranceMeters)
        => string.Format(CultureInfo.InvariantCulture,
               "{0} gate: {1} measured OFF the terrain at dispatch - live {2:F1} m vs terrain {3:F1} m "
             + "under vertex 0 (gap {4:F0} m, tolerance {5:F0} m / {6}) - dispatching task '{7}'. The "
             + "measurement is recorded, no correction is sent at dispatch, and the task is not "
             + "held: an unconfirmed read-back ended legitimate tasks in run 20260921T143243Z.",
               Prefix, unitName, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, toleranceMeters,
               ToleranceSettingKey, taskName);
}
