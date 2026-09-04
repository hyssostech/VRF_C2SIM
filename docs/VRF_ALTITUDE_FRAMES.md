# VR-Forces altitude frames - THE canonical answer. Cite this file; do not re-derive.

Written 2026-09-04 after a user challenge ("It is ridiculous that I am at this stage still
having to redirect something that have been solved"). Every claim below carries a HEADER or
DOC citation, or is marked VERIFIED BY RUN with the run id, or is marked OPEN. Nothing here
rests on inference from observed behaviour alone - that is precisely what rotted.

## THE ONE-PARAGRAPH ANSWER
VR-Forces has TWO altitude frames, MSL and above-terrain, and it exposes them
**unevenly**: an ENTITY's altitude can be set directly in AGL through the remote API, but a
ROUTE VERTEX cannot - vertices are geocentric-absolute only, so a remote controller must ask
the simulator for terrain height and author absolute values. Most of the confusion in this
project came from answering one of those questions with the other one's answer.

## 1. ENTITY / AGGREGATE altitude - AGL IS DIRECTLY SUPPORTED
`vrfcontrol/vrfRemoteController.h:1372-1374`
```
virtual void setAltitude(const DtUUID& uuid, double altitude,
                         bool aboveGroundLevel = false,
                         const DtSimulationAddress& addr = DtSimSendToAll) const;
```
- `aboveGroundLevel` IS the GUI's MSL/AGL selector: help
  `Content/DataRequests/EntityLevel/vrf_sets_setAltitude.htm` - "Select MSL (mean sea level)
  or AGL (above ground level) to specify the base for the altitude value." Carried on the wire
  by `vrftasks/setAltitudeRequest.h::aboveGroundLevel()`.
- So "place this unit N m above the ground, wherever the ground is" is ONE CALL and needs NO
  terrain knowledge, NO terrain query and NO safe-high birth.
- WE ALREADY PASS IT: `src/VrfFacade/VrfFacade.cpp:739` calls
  `setAltitude(DtUUID(uuid), altitudeMeters, TRUE)`. The capability has been wired the whole
  time. On the normal path it never fires, because the app SKIPS the deferred SetAltitude
  whenever it used the safe-high create (`VrfC2SimService.cs`, "SKIP the deferred SetAltitude").

## 2. CREATE-time clamp - REAL, but ONE-DIRECTIONAL
`vrfmsgs/ifCreateVrfObject.h:210-214` - "If True (the default) the object will be created and
placed on the nearest polygon." `vrfcontrol/vrfRemoteController.h:1275,:1291` - both
`createEntity` overloads default `bool groundClamp = true`, and our `VrfFacade::CreateEntity`
(`VrfFacade.cpp:697`) passes only through `uniqueName`, so we always send TRUE.
- VERIFIED BY RUN (PREREG_CLAMP_DIRECTION_2026-09-04, sim 3908, appNos 3911/3912, ONE observer,
  ONE capture, identical lat/lon, only the requested altitude differing):
    create at 10000 m MSL -> reflected 1149.8 m (the surface).  Clamp DROPS. Works.
    create at    50 m MSL -> reflected   -0.0 m.                Clamp does NOT RAISE.
- So `CreateAltitudeSafeMslMeters = 10000` IS load-bearing for the create path. Do NOT delete
  it on the strength of the header's word "nearest" - the product does not do "nearest".
- **OPEN / UNEXPLAINED**: the below-terrain create landed at -0.0, not at its requested 50 and
  not on the surface. No doc predicts that. Do not build on -0.0 until someone explains it.

## 3. ROUTE / WAYPOINT VERTEX altitude - NO AGL FRAME EXISTS
`createRoute` (`vrfRemoteController.h:1023,:1033`) and `createWaypoint` (`:991,:1001`) take
ONLY geocentric vertices - no altitude-frame argument. VERIFIED BY ABSENCE: no `agl` /
`aboveGroundLevel` / `aboveTerrain` field on any waypoint, route or vertex type in `vrfmsgs/`,
`vrfobj/` or `vrfutil/`. The GUI's "Set Altitude Above Terrain" for vertices (help
`.../ObjectCreation/vrf_setRouteVertexAltitude.htm`) is a FRONT-END convenience that resolves
to absolute values before sending.
=> For vertices the controller genuinely must obtain terrain height itself. That is what
`DtIfRequestTerrainProfileInformation` is for, and `GroundWaypointAltitudeMode=TerrainProfile`
is the right shape. This is the reading an earlier session reached - CORRECT, FOR ROUTES ONLY.
The Users Guide is explicit that "Set Altitude Above Sea Level ... could result in some
vertices being below ground": absolute vertex altitude is the author's responsibility.

## 4. SCENARIO IMPORT is a THIRD frame - not investigated
`vrfutil/scenario.h:617` `DtSimulationObjectGroupAltitudesAreAboveTerrain = 0x00000020`, and
:633 includes it in the DEFAULT import flag set. `vrfutil/createObjectParser.h:37 usingAGL`
and the MSDL importer's `locationAGL` say the same for those paths. So an AUTHORED .scnx may
carry above-terrain altitudes while a REMOTELY-created object carries MSL. NOBODY HAS CHECKED
WHICH FRAME FixtureGen WRITES. OPEN - flagged, not claimed.

## 5. WHAT IS FALSIFIED - do not re-derive it
**BIRTH ALTITUDE IS NOT THE FREEZE DISCRIMINATOR.** `docs/CORRECTIONS_LOG.md` "Birth altitude":
the 10000 m fix was ALREADY ACTIVE in the three 2026-07-19 scored runs and units froze anyway;
three taskees at the SAME birth altitude split one-mover / two-frozen. The clamp cures BURIAL,
not freezing; the two were never shown to be the same thing. Also falsified as freeze causes:
WAYPOINT ALTITUDE (the below-terrain fixture variant moved -
`docs/HANDOFF_2026-07-22_PLAN_ASSIGNMENT.md:27`) and REGION.
A statement of the form "born buried, therefore never moves" is ROT. It has re-entered this
project at least twice after being falsified.

## 6. WHY IT KEPT COMING BACK (the actual source, so it can be closed)
1. **The architecture was built from probe inference, never from the header.** Nobody read
   `setAltitude`'s `aboveGroundLevel` parameter before designing a safe-high-birth workaround
   for a problem the parameter already solves. The docs-first rule exists because of this.
2. **The falsification was recorded in ONE place and the claim lived in SIX.** CORRECTIONS_LOG
   held the refutation; `VrfSettings.cs`, `VrfC2SimService.cs`, `CreateOne/Program.cs`,
   `VRF_GROUNDWORK_PLAN.md` and `VRF_GROUND_TRUTH.md` each repeated the refuted link with no
   back-pointer. A reader of any of those five never learns it is dead. On 2026-09-04 a session
   read the CreateOne comment and put the refuted claim into a fresh prereg.
3. **One setting name spans two frames.** `GroundWaypointAltitudeMode` governs BOTH route-vertex
   altitude AND the entity CREATE position (`VrfSettings.cs:221`). Two questions with different
   documented answers behind one knob, so correcting one never forces correcting the other.
4. **`VRF_GROUNDWORK_PLAN.md` called it "the one fully-closed class"** - the strongest possible
   instruction not to look again, attached to a claim that was already dead.

## 7. TRIPWIRES (fire before writing, not after)
- Writing "buried" or "underground" near "freeze"/"never moves"? STOP - falsified, sec 5.
- About to add terrain-height machinery to place an ENTITY? STOP - sec 1, one AGL call.
- About to put an AGL/above-terrain altitude on a ROUTE VERTEX? STOP - sec 3, no such frame;
  query terrain and send absolute.
- About to state ANY altitude behaviour? Cite a header line or a help page, or mark it OPEN.
