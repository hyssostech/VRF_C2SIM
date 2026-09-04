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
"cannot RAISE" claim in VrfSettings.cs SURVIVES this test and is now VERIFIED rather than
asserted.

*** DESIGN VERDICT WITHDRAWN 2026-09-04. This paragraph continued: "CreateAltitudeSafeMslMeters
is load-bearing for the create path and must NOT be removed." THAT DOES NOT FOLLOW FROM THIS
MEASUREMENT and it is withdrawn. The measurement establishes which way the clamp travels. It
says nothing about whether an MSL birth is the right way to place a unit - and it is not
(sec 8a: an AGL set lifts a unit onto the ground directly). The error was pre-committed: sec 4's
falsifier was written as a BINARY - clamp raises => workaround unnecessary, clamp does not raise
=> "the safe-high create is load-bearing" - which silently excluded the actual answer, that one
should not birth at an arbitrary MSL at all. A false dichotomy in a falsifier clause hands a
design conclusion the authority of a measurement.
LESSON, and it is the same shape as the 2026-07-16 rot this prereg was investigating: a true
narrow finding written down FUSED to a design conclusion, after which the fusion is what gets
read back. KEEP MEASUREMENT AND DESIGN IMPLICATION IN SEPARATE SENTENCES, and never put a
design verdict in a falsifier. ***

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

## 8. AGL CONFIRMATION ARM (registered 2026-09-04 BEFORE running, at user direction:
"Confirm the AGL approach. If there are issues (which I very much doubt), this has indeed been
cured by the query, so that is a verified plan b")
The one line sec 7 marked ASSUMED, NOT VERIFIED is now tested. New tool tools/SetAlt (additive,
clone of CreateOne's join/act/resign) calls VrfBridge.SetAltitude -> VrfFacade.cpp:739 ->
setAltitude(uuid, metres, aboveGroundLevel=TRUE). SUBJECT: the STILL-BURIED test entity from
sec 6, uuid a2035220-e0f2-034d-a95a-c75ea8d82d31 (entityId 1:3908:7), currently reflecting
-0.0 m under ~1150 m of terrain. It is the ideal subject precisely because it is buried: an
AGL set that works must LIFT it, which is the direction the create clamp cannot do.
REQUEST: 2.0 m above terrain. appNos 3913 (SetAlt), 3914 (WatchVrf).
P3 (HIGH): the entity's reflected MSL altitude moves from -0.0 to ~1151.8 m (the control's
   1149.8 m surface + 2 m), stable across samples. That CONFIRMS AGL placement end to end and
   proves an entity needs no terrain query.
   FALSIFIER: it stays at -0.0, or moves to 2.0 (i.e. the flag was ignored and 2 m was taken as
   MSL), or goes anywhere else. ANY of those means the AGL path does not work as documented -
   in which case PLAN B IS ALREADY VERIFIED AND SHIPPING: GroundWaypointAltitudeMode=
   TerrainProfile queries the back end for terrain height and authors from it (design doc sec 7,
   Rows 2c/2cR, two consecutive live runs, zero warnings). Nothing depends on P3 succeeding;
   this arm decides only whether a SIMPLER path is available, not whether we have one.
NOTE: SetAltitude has NO reply message, so SetAlt cannot self-confirm. The scoring is done by
an INDEPENDENT observer (WatchVrf), not by the tool that issued the change - deliberately,
because a tool reporting its own success is the recurring false-green shape here.

### 8a. Result (2026-09-04; tools/SetAlt built against Release-5.2, appNos 3913/3914;
runs/launch52/watch_3914_agl_*.trace)
**AGL PLACEMENT WORKS.** Same live sim, one observer, one capture:
  CONTROL 7113902b (never touched)      1149.8 m  ->  1149.8 m
  SUBJECT a2035220 (AGL set to 2 m)      -0.0 m   ->  **1149.8 m**
The subject was BURIED ~1150 m below the surface and one `setAltitude(uuid, 2.0,
aboveGroundLevel=TRUE)` put it ON the surface - the exact direction the create clamp CANNOT go
(sec 6). Stable across all 5 samples. No terrain height was known, computed or queried by the
caller. The AGL capability is now EXERCISED, not inferred from a header: before today no tool
in this repo had ever called SetAltitude (0 hits across tools/).
P3 MISSED IN DETAIL, CONFIRMED IN SUBSTANCE - stated plainly rather than rounded off: P3
predicted ~1151.8 m (surface + 2 m) and the result is 1149.8 m, the surface exactly, matching
the untouched control. Sec 4 listed "goes anywhere else" as a falsifier, so as literally
written the prediction failed. READING (consistent, NOT separately tested): the subject is an
M1A2 and VR-Forces holds ground vehicles on the surface continuously, so a 2 m offset on a tank
collapses to zero. UNTESTED: whether AGL preserves a NON-ZERO offset for something that can
leave the ground - one air-entity run would settle it. For the purpose at hand (put a ground
unit on the ground) the observed behaviour is exactly what is wanted.
CONSEQUENCE: the MSL birth is retiring (see the withdrawal in sec 6). The FALLBACK the user
named is untouched and remains proven: route vertices have no AGL frame at all (sec 7b), so
GroundWaypointAltitudeMode=TerrainProfile stays the answer there.
STILL UNEXPLAINED from sec 6, and not swept: why the below-terrain CREATE landed at -0.0 rather
than at its requested 50.

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
