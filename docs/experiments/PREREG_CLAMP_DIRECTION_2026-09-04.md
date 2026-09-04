# PREREG - does VR-Forces' create-time ground clamp RAISE a below-terrain birth, or only DROP?

Date 2026-09-04. Tier HEAVY (it adjudicates a cause claim that is compiled into our defaults
and has shaped weeks of work). Registered BEFORE the run. User challenge: "my suspicion is
that the solution is to specify an altitude related to the terrain, not sea level - but I may
be wrong, and the docs might have something different that is required in VRF".

## 1. The claim under test (OURS, asserted in code as fact, never tested)
src/VrfC2SimApp/VrfSettings.cs:256-257: "VRF's createEntity ground clamp (default on) can only
DROP the birth onto the local surface - a clamp cannot RAISE a below-terrain birth, which is
why fixed-MSL births bury units at high elevation."
src/VrfC2SimApp/VrfC2SimService.cs:583 repeats it. On that claim rests
CreateAltitudeSafeMslMeters = 10000 (birth every ground unit at 10000 m MSL so the clamp can
fall onto terrain), and a good deal of altitude machinery downstream.

## 2. What the DOCUMENTATION actually says (read 2026-09-04, all local 5.2d)
D1. vrfmsgs/ifCreateVrfObject.h:210-214 - "If True (the default) the object will be created and
    placed on the NEAREST POLYGON. Otherwise, it will be created and then placed at the
    altitude specified in the position." `clampToGround()` / `setClampToGround(bool)`.
    NEAREST is directionless. It does not say "dropped".
D2. vrfcontrol/vrfRemoteController.h:1275 and :1291 - BOTH createEntity overloads take
    `bool groundClamp = true` as a DEFAULT ARGUMENT. Our VrfFacade::CreateEntity
    (VrfFacade.cpp:697) passes only through uniqueName, so WE ALREADY SEND groundClamp=true
    on every create and never set it anywhere (0 hits for clampToGround in src/ and tools/).
D3. help Content/SharedTopics/3Dnovaentities/GroundClamping.htm - "VR-Forces keeps an entity
    anchored to the terrain surface REGARDLESS OF THE ALTITUDE DATA contained in its state
    update." (This page is the DISPLAY-side clamp - cosmetic - and is NOT the warrant for D1;
    recorded so the two are not conflated, which our own record has done before.)
D4. help .../ObjectCreation/vrf_setAltitudeInDialogBox.htm - creating an object offers "Above
    Sea Level" ("If the terrain is higher than the altitude entered, the entity will be below
    the surface of the ground") vs "Above Terrain" ("The distance above the terrain at this
    location"). So ABOVE-TERRAIN IS A FIRST-CLASS REFERENCE FRAME IN THE PRODUCT.
D5. help .../ObjectCreation/vrf_setRouteVertexAltitude.htm - route vertices likewise: "Set
    Altitude Above Sea Level. This could result in some vertices being below ground." vs "Set
    Altitude Above Terrain. Sets the altitude of each vertex the amount above the terrain."
D6. help DataRequests/EntityLevel/vrf_sets_setAltitude.htm - the Altitude SET REQUEST: "Select
    MSL (mean sea level) or AGL (above ground level) to specify the base for the altitude
    value." An AGL frame exists in the request layer, not only in the GUI.

## 3. Arms (one variable: the requested create altitude. Same live sim 3908, same AOI,
same type, same terrain, minutes apart)
CONTROL (already run, PREREG_52_FIXTURE_LOAD P3): create at 10000 m MSL -> reflected
  34.615000/-116.550000 at alt 1149.8 m, stable over 12 samples. Terrain there is ~1150 m.
TEST: create at 50 m MSL - roughly 1100 m BELOW the local surface - everything else identical.

## 4. Predictions (registered before running)
P1 (THE ONE THAT MATTERS): if the clamp places on the NEAREST polygon as D1 says, the test
   entity reflects at ~1149.8 m, the SAME surface as the control. That FALSIFIES our
   "cannot RAISE" claim, and CreateAltitudeSafeMslMeters is unnecessary machinery.
   FALSIFIER: the entity reflects at or near 50 m (or NaN, or never reflects) -> our claim is
   CORRECT as written, the safe-high create is load-bearing, and it should be documented as a
   VERIFIED behaviour instead of an asserted one.
P2: whatever the outcome, the reflected altitude is STABLE across samples (a moving value
   would mean something else is acting and the run is not clean).
NOTE ON SCOPE, stated before the result: this tests the CREATE clamp for an ENTITY. It does
NOT test route/waypoint vertex altitude (D5's frame), and it does NOT re-open the freeze
question - birth altitude is already FALSIFIED as the freeze discriminator (CORRECTIONS_LOG
"Birth altitude"). A pass here removes a workaround; it does not explain any freeze.

## 5. Procedure
appNos ledgered BEFORE the run: 3911 CreateOne, 3912 WatchVrf. Sim 3908 stays up so the
control and the test share one sim, one terrain page-in and one federation.

## 6. Result (2026-09-04, live sim 3908, appNos 3911/3912;
runs/launch52/watch_3912_clampdir_*.trace - BOTH entities observed in ONE 30 s capture)
  CONTROL 1:3908:6 requested 10000 m MSL -> reflected 34.615000/-116.550000 alt **1149.8 m**
  TEST    1:3908:7 requested    50 m MSL -> reflected 34.615000/-116.550000 alt **-0.0 m**
Both stable across all 6 samples (P2 held). Lat/lon identical, so the only difference is the
requested altitude - the arms are clean.

**P1 IS FALSIFIED. The create-time clamp does NOT raise a below-terrain birth.** Our
"cannot RAISE" claim in VrfSettings.cs:256-257 SURVIVES this test and is now VERIFIED rather
than asserted. CreateAltitudeSafeMslMeters is load-bearing for the create path and must NOT be
removed on the strength of the header's "nearest polygon" wording.

**UNEXPLAINED, recorded as a falsifier and NOT swept:** the test entity did not stay at its
requested 50 m either - it sits at -0.0 m, ~1150 m below the surface AND ~50 m below what was
asked for. Neither the header ("placed at the altitude specified in the position" when not
clamped) nor "nearest polygon" predicts -0.0. Candidate readings, none tested: the clamp
searched DOWNWARD only and, finding no polygon, fell through to the ellipsoid; or a
below-terrain create is snapped to 0 by some other rule. THE DOC WORDING IS THEREFORE
MISLEADING AT BEST - "nearest" is not what the product does. Do not build on -0.0 until it is
explained; it is enough for today that below-terrain creates do not land on the surface.

## 7. THE ACTUAL ANSWER TO "how does VRF want altitude specified" (docs, 2026-09-04)
The two readings in our record were each right about a DIFFERENT object, and each wrong to
generalise. Split them and both the mystery and the workaround dissolve:

**(a) ENTITY / AGGREGATE altitude - AGL IS DIRECTLY SUPPORTED. No query needed.**
`vrfcontrol/vrfRemoteController.h:1372-1374`:
   `virtual void setAltitude(const DtUUID& uuid, double altitude,
                             bool aboveGroundLevel = false, ...)`
That third parameter IS the GUI's MSL/AGL selector (help DataRequests/EntityLevel/
vrf_sets_setAltitude.htm: "Select MSL ... or AGL ... to specify the base for the altitude
value"), carried by vrftasks/setAltitudeRequest.h::aboveGroundLevel(). So "put this unit
2 m above whatever the ground is here" is ONE CALL with no terrain knowledge whatsoever.
*** WE ALREADY DO THIS: VrfFacade.cpp:739 calls setAltitude(uuid, alt, TRUE) - AGL. The
capability has been wired the whole time. What defeats it is that the app SKIPS the
post-create SetAltitude whenever the unit was born above terrain (the safe-high create),
so on the normal path the AGL call never fires. The workaround is not needed to reach the
surface; the AGL call reaches it directly. ***

**(b) ROUTE / WAYPOINT VERTEX altitude - NO AGL FRAME EXISTS IN THE API.**
`createRoute` (:1023, :1033) and `createWaypoint` (:991, :1001) take ONLY a geocentric
`DtVector` / `DtList` of vertices. There is no altitude-frame argument and no AGL field on
any waypoint, route or vertex type in vrfmsgs/, vrfobj/ or vrfutil/ (verified by absence).
The GUI's "Set Altitude Above Terrain" for vertices (help .../vrf_setRouteVertexAltitude.htm)
is a FRONT-END convenience that resolves to absolute values before they are sent.
=> For vertices a remote controller genuinely must obtain terrain height itself, which is
exactly what DtIfRequestTerrainProfileInformation is for. THE EARLIER SESSION'S "ask the
simulator" READING IS CORRECT - FOR ROUTES - and our TerrainProfile mode is the right shape.
It is NOT, and never was, needed to place an ENTITY on the ground.

**(c) Adjacent, unused, worth knowing:** vrfutil/scenario.h:617
`DtSimulationObjectGroupAltitudesAreAboveTerrain = 0x00000020` - and it is part of the DEFAULT
import flag set at :633 - so scenario/object-group IMPORT already interprets altitudes as
above-terrain. vrfutil/createObjectParser.h:37 `usingAGL` and the MSDL importer's
`locationAGL` say the same for those paths. An authored-fixture route (our .scnx) is therefore
in a different frame from a remotely-created one; nobody has checked which frame FixtureGen
writes. NOT INVESTIGATED - flagged, not claimed.

Adversarial review: strongest competing account of the CONTROL/TEST split was "the clamp did
raise, and 1149.8 vs -0.0 reflects something else entirely (observer, dead reckoning, wrong
entity)". Rejected: both entities were read by ONE observer in ONE capture at the same
timestamps, at identical lat/lon, differing only in requested altitude, and the control's
1149.8 m matches the independently-logged terrain load at that AOI. VERIFIED here: the clamp's
direction, the AGL parameter's existence and our passing TRUE to it, and the ABSENCE of any
vertex-level AGL frame. ASSUMED, NOT VERIFIED: that calling setAltitude(uuid, h, AGL=true) on
an already-buried entity lifts it to terrain+h - the obvious confirming test, one call, not yet
run; and that "terrain here is ~1150 m" (taken from the control's clamped value, not from an
independent elevation source).
