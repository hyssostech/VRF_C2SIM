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
- **VERIFIED END TO END 2026-09-04** (PREREG_CLAMP_DIRECTION sec 8a; `tools/SetAlt`, appNos
  3913/3914, scored by an INDEPENDENT observer, not by the tool that issued the change): an
  entity sitting BURIED at -0.0 m under ~1150 m of terrain was lifted ONTO THE SURFACE
  (1149.8 m) by one AGL call, while an untouched control held 1149.8 m in the same capture.
  That is the direction the create clamp cannot go. Before that run no tool in this repo had
  ever called SetAltitude - the capability was documented, wired, and never exercised.
  Recorded honestly: 2 m AGL on a TANK settles at the surface, not 2 m above it, because
  VR-Forces holds ground vehicles on the surface continuously. Whether AGL preserves a
  non-zero offset for an entity that can leave the ground is UNTESTED.

## 2. CREATE-time clamp - REAL, but ONE-DIRECTIONAL
`vrfmsgs/ifCreateVrfObject.h:210-214` - "If True (the default) the object will be created and
placed on the nearest polygon." `vrfcontrol/vrfRemoteController.h:1275,:1291` - both
`createEntity` overloads default `bool groundClamp = true`, and our `VrfFacade::CreateEntity`
(`VrfFacade.cpp:697`) passes only through `uniqueName`, so we always send TRUE.
- VERIFIED BY RUN (PREREG_CLAMP_DIRECTION_2026-09-04, sim 3908, appNos 3911/3912, ONE observer,
  ONE capture, identical lat/lon, only the requested altitude differing):
    create at 10000 m MSL -> reflected 1149.8 m (the surface).  Clamp DROPS. Works.
    create at    50 m MSL -> reflected   -0.0 m.                Clamp does NOT RAISE.
- WHAT THIS DOES AND DOES NOT SHOW. It shows the clamp's DIRECTION. It does NOT show that
  birthing at 10000 m MSL is the right way to place a unit - that question is settled by sec 1
  (set AGL and the ground is found for you), not by this measurement.
  *** A first version of this line read "CreateAltitudeSafeMslMeters IS load-bearing ... do NOT
  delete it". THAT IS A DESIGN VERDICT SMUGGLED INTO A MEASUREMENT and it is withdrawn. The
  measurement is only about which way the clamp travels. The safe-high birth exists to
  compensate for having chosen an MSL frame in the first place; choose AGL and the clamp's
  direction stops mattering. See sec 6 Q3 for how this got written. ***
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

## 6. TWO SEPARATE QUESTIONS - keep them apart (they were conflated on 2026-09-04)

**FIRST, WHAT IS NOT BROKEN.** The buried-birth PROBLEM was found and cured: ground units are
no longer born underground. That outcome stands and is not retracted anywhere in this file.
What was falsified is only the claim that burial explained the FREEZES (sec 5).

**BUT THE MECHANISM IS THE WRONG FRAME, AND SAYING OTHERWISE IS THE ROT.** The approach is
sec 1: set the altitude ABOVE GROUND LEVEL and the simulator finds the ground. Birthing a unit
at 10000 m MSL so gravity-by-clamp drops it onto the surface is compensation for having picked
an MSL frame when an above-terrain frame was available and already wired. It is to be RETIRED,
not defended. (Sequencing: the AGL path must be exercised once first - no tool had ever called
SetAltitude - and route vertices are unaffected either way, sec 3.)
*** A first version of this section said `CreateAltitudeSafeMslMeters` "WORKS and STAYS" and
called AGL merely "tidier". WITHDRAWN - see Q3. ***

**Q1 - why is the implementation clumsier than it needed to be?**
MOJAVE_ROOTCAUSE part 13c (2026-07-16) correctly read the header and found
`setAltitude`'s `aboveGroundLevel`, already passed TRUE by VrfFacade. The finding was used ONLY
to exonerate a suspect, and the AGL call was then skipped in favour of the safe-high birth. So
a docs finding that KILLS a hypothesis should also be checked for what it ENABLES. That is a
real lesson about design quality. IT IS NOT THE CAUSE OF ANY REGRESSION, and reading it as one
(as the first version of this section did) mistakes an elegance question for a defect.

**Q2 - why did a September session re-assert a July-falsified claim? (the actual regression)**
Because the refutation was written in ONE place and the refuted claim was left standing in SIX.
CORRECTIONS_LOG held the falsification; `VrfSettings.cs`, `VrfC2SimService.cs`,
`CreateOne/Program.cs`, `VRF_GROUNDWORK_PLAN.md`, `VRF_GROUND_TRUTH.md` and `START_HERE.md`
each repeated "born buried therefore never moves" with NO back-pointer, so a reader of any of
them never learned it was dead. On 2026-09-04 a session opened CreateOne's header comment to
choose a create altitude, read the dead claim there, and put it into a fresh prereg. No
reasoning about part 13c was involved - just a stale comment at the point of use.
Two aggravating factors, both of the same kind:
  - `VRF_GROUNDWORK_PLAN.md` called it "the one fully-closed class" - the strongest possible
    instruction not to look again, attached to a claim that was already dead.
  - `START_HERE.md`, the ENTRY doc, still led with "BREAKTHROUGH ... ENTITY-FREEZE ROOT CAUSE
    IS FOUND ... the frozen entity class is CURED" for seven weeks after it was falsified.
THE FIX FOR Q2: a refutation must be written at every site that repeats the claim, not only in
the corrections log. Done.

**Q3 - why did it come back AGAIN, hours later, in this very file? (2026-09-04, same day)**
Because I re-created it myself, and this is the generator worth remembering. Writing
PREREG_CLAMP_DIRECTION, I framed sec 4's falsifier as a BINARY: either the clamp raises, so the
workaround is unnecessary, or it does not, and then "the safe-high create is load-bearing".
That excluded the real answer - do not birth at an arbitrary MSL at all - before any data
existed. The run then falsified P1, so the pre-written design verdict was promoted to a RESULT
("must NOT be removed"), and from there into VrfSettings.cs and into this file as
"WORKS and STAYS". `git grep` confirms the phrase existed nowhere in the repo before that
commit: it was manufactured that afternoon, and it carried the authority of a live measurement.
SAME SHAPE AS Q1 AND Q2: a true narrow finding recorded FUSED to a design conclusion, after
which the fusion is what the next reader inherits - even when the next reader is the same
session an hour later.
RULE, now the standing one for this project: **keep the measurement and its design implication
in separate sentences, and never put a design verdict inside a falsifier clause.** A falsifier
says what the world will look like; it does not say what should then be built.

## 7. TRIPWIRES (fire before writing, not after)
- Writing "buried" or "underground" near "freeze"/"never moves"? STOP - falsified, sec 5.
- About to add terrain-height machinery to place an ENTITY? STOP - sec 1, one AGL call.
- About to put an AGL/above-terrain altitude on a ROUTE VERTEX? STOP - sec 3, no such frame;
  query terrain and send absolute.
- About to state ANY altitude behaviour? Cite a header line or a help page, or mark it OPEN.
