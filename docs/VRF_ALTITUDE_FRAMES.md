# VR-Forces altitude frames - THE canonical answer. Cite this file; do not re-derive.

Written 2026-09-04 after a user challenge ("It is ridiculous that I am at this stage still
having to redirect something that have been solved"). Every claim below carries a HEADER or
DOC citation, or is marked VERIFIED BY RUN with the run id, or is marked OPEN. Nothing here
rests on inference from observed behaviour alone - that is precisely what rotted.

## 0. THE SOURCE FRAME - where the whole problem started (2026-09-05, read from the schema)
C2SIM does not have "an altitude". `GeodeticCoordinate` carries TWO OPTIONAL elements
(C2SIM_SMX_LOX_CWIX2024.xsd :2716-2717): `AltitudeAGL` - "distance vertically above ground level"
(:155) - and `AltitudeMSL` - "distance vertically above mean sea level" (:163). **Every init in
data/ carries NEITHER** (checked 2026-09-05), so "no altitude given" is the normal case.
The oracle (frozen C++) read whichever element it met into ONE field, used it as an ABSOLUTE
create altitude (C2SIMinterface.cpp:1384), invented "1000.0" when absent on the belief that
"1000.0 triggers VRForces Gound Clamping" (:685, :1378-1379 - never a VR-Forces behaviour; it
was simply above the ground at sea-level Bogaland), and then sent ElevationAgl+1 through
setAltitude(..., TRUE) (:721-724) - about 1001 m ABOVE GROUND for a ground unit. The port
inherited all of it (InitParser.Elevation collapsed AGL/MSL into a string named ElevationAgl;
UnitTranslator.cs:63/:159), added a 10000 m "safe-high birth" on top, and skipped the one call
that places things. Three weeks of altitude machinery compensated for a frame that was wrong at
the point of ingestion.
**WHAT THE CODE DOES NOW (4b4d0f9, PlacementPolicy.cs, --placement-selftest):** the typed
AltitudeAgl/AltitudeMsl are preserved from the init; the create position is the AUTHORED
lat/lon (altitude = C2SIM MSL if given, else 0 - irrelevant for land under the default clamp);
the altitude the object should HAVE is stated in the frame C2SIM stated it - AGL through
setAltitude(agl, aboveGroundLevel=TRUE); land with nothing given -> AGL 0 (on the ground); air
with nothing given -> a NAMED knob logged as ARBITRARY; surface/subsurface -> the sim's own water
handling. "Ground" = the DIS domain of the created type (SISO-REF-010.xml:3116-3119), not an
APP6 symbology character. The 10000 m birth, the +1 and the SIDC test are gone; Fixed100 keeps
the oracle's behaviour byte-for-byte as the parity escape hatch. LIVE CONFIRMATION STILL OWED.

## UNITS - what "m MSL" means in this project
Our readback is `DtGeodeticCoord::alt()` with no geoid datum set (`matrix/geoidDatum.h` default
`DtNoGeoid`; nothing in the tree sets one), i.e. height above the WGS-84 ELLIPSOID. That is NOT a
mislabel, it is MAK's documented convention: "for coordinates in DIS PDUs, it is assumed the
simulation model of mean sea level is coincident with the WGS84 ellipsoid. Altitude MSL shall use
the ellipsoid as their zero-reference surface" and "MAK ONE applications assume 0 MSL and make use
of geoid-based values as-is" (help AddingContent/Terrain/EarthFiles/vrf_egmModel.htm:199, :201).
So "m MSL" here = ellipsoidal height, by the vendor's definition; the geoid separation (~-32 m at
the Mojave AOI) is absent from every figure by design. (2026-09-04's audit called this "WRONG
(labels)"; the vendor page above re-scopes that to "MAK-convention MSL, state it once".) The
"-0.0" of the below-terrain create is still the ellipsoid surface - see sec 2, still OPEN.

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
  time. Until 2026-09-05 the normal path never reached it - the app SKIPPED the deferred
  SetAltitude whenever it used the 10000 m create. As of 4b4d0f9 the normal path IS this call:
  PlacementPolicy decides the AGL value (C2SIM's, or 0 for a land unit with none) and the
  ObjectCreated handler sends it (VrfC2SimService.cs, `_pendingAltitude`).
- **EXERCISED ONCE 2026-09-04, UNCONTROLLED** (PREREG_CLAMP_DIRECTION sec 8a; `tools/SetAlt`,
  appNos 3913/3914, scored by an INDEPENDENT observer, not by the tool that issued the change):
  an entity sitting BURIED at -0.0 m under ~1150 m of terrain read 1149.8 m - ON THE SURFACE -
  after one AGL call, while an untouched control held 1149.8 m in the same capture. Before that
  run no tool in this repo had ever called SetAltitude: documented, wired since July, never
  exercised. The wire path IS verified (SetAlt -> VrfBridge.cpp:353 -> VrfFacade.cpp:739 ->
  vrfRemoteController.h:1372) and the AGL semantics are vendor-stated (help
  DataRequests/EntityLevel/vrf_sets_setAltitude.htm, a SIMULATION request).
  *** "VERIFIED END TO END" WITHDRAWN 2026-09-04 by adversarial audit - the experiment does not
  exclude its alternatives: 29 minutes passed unobserved between the two captures; the control
  was ALREADY on the surface so a global re-clamp or terrain re-page would leave it unchanged
  and look identical; and no capture of the SetAlt run itself was retained. To VERIFY: two
  buried entities, set ONE, capture the tool output, and sample within ~1 s. ***
  Also withdrawn: "2 m AGL on a TANK settles at the surface BECAUSE VR-Forces holds ground
  vehicles on the surface continuously" - NO VENDOR SOURCE SAYS THAT. The Glossary defines
  ground clamping as a 3D VISUALIZATION behaviour, and the sim-side note in CORRECTIONS_LOG
  covers MOVING vehicles, which this stationary one was not. An untested competing mechanism
  fits equally: the entity was placed at terrain+2 m and FELL 2 m under ground contact long
  before it was observed. Whether AGL preserves a non-zero offset is UNTESTED either way.

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

**THE MECHANISM IS THE WRONG FRAME.** The approach is sec 1: set the altitude ABOVE GROUND LEVEL
and the simulator finds the ground. Birthing a unit at 10000 m so gravity-by-clamp drops it onto
the surface is compensation for having picked an MSL frame when an above-terrain frame was
available and already wired. THE FRAME ARGUMENT IS A DESIGN ARGUMENT AND RESTS ON THE HEADERS AND
THE VENDOR DOCS (sec 1), NOT ON THE ONE-ENTITY PROBE - keep them apart, per Q3 below. What the
probe showed is narrow: one buried stationary M1A2 read on-surface after one AGL call, once,
uncontrolled. It does not establish that retiring the create path is SAFE for every unit: the
AGL create path has never run through the app, air/sea units are out of scope, and aggregates
are untested. Retirement therefore needs its own prereg and confirming run (HANDOFF NEXT 6b) -
that is not hedging, it is the difference between the direction and the change.
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
