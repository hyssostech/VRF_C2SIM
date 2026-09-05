# PREREG - does the placement rewrite (4b4d0f9) put ground platforms AND unit members on terrain?

Date 2026-09-05. Tier HEAVY (it changes creation for every unit and adjudicates two
documentation-vs-observation conflicts). Written BEFORE any launch. NOT YET RUN.

## 0. Citations this experiment rests on (own record + vendor) - the gate CLAUDE.md sec 1 demands
OWN RECORD: docs/VRF_ALTITUDE_FRAMES.md sec 0 (source frame), sec 1a (platforms clamp, units
organize), PREREG_CLAMP_DIRECTION_2026-09-04 secs 6 and 8a (the two observations in tension),
src/VrfC2SimApp/PlacementPolicy.cs (the rule under test), commit 4b4d0f9.
VENDOR: UG52 14.3.3 / help vrf_newEntityPlacement.htm ("ground ... entities are placed on the
ground ... at the highest possible terrain intersection"); vrfmsgs/ifCreateVrfObject.h:210-214
(clampToGround default true, "nearest polygon"); vrfMovingObjectStateRepository.h:251-253
(place() with clampToGround); vrfcontrol/vrfRemoteController.h:1275/:1291 (createEntity
groundClamp=true), :1295-1306 (createAggregate, no clamp param), :1372-1374 (setAltitude AGL);
vrftasks/setAltitudeRequest.h:24-25 ("ignored if the vehicle is not an air-going vehicle");
vrftasks/setLocationRequest.h:27,31-32 (unit/platform setLocation clamps ground vehicles);
vrfmodel/disaggregatedActuator.h:20-26 (unit position derived from subordinates);
vrfmodel/disaggregatedSetController.h:51-71 + pseudoAggregatedSetController.h:39-62 (no
altitude callback on a unit). C2SIM: C2SIM_SMX_LOX_CWIX2024.xsd :155,:163,:2716-2717.

## 1. Frame
5.2 stack, rtiexec posture (PREREG_52_RTIEXEC), fixture R9_Mojave_Empty_52.scnx (loads headless,
PREREG_52_FIXTURE_LOAD), C2SIM server c2sim-server-vrf (18080/61614). Init = the R9 lean init:
3 taskees incl. at least one ENTITY (1.BdeHQ -> M1A2) and two UNITS (1222.MechPlt -> Tank
Platoon (USA), 114.MechCoy -> Tank Company (USA)), all with NO altitude element. Terrain at the
AOI ~1150 m (SRTM 1150.0; clamped control 1149.8). ONE variable vs the last 5.0.2 golden: the
placement code (authored lat/lon + AGL set) instead of the 10000 m birth. No order is pushed -
this prereg is about WHERE THINGS ARE BORN, not whether they move.

## 2. What the code will do (from PlacementPolicy, offline-tested)
Every land object (domain 1): create at authored lat/lon with create altitude 0 (~1150 m BELOW
the surface at this AOI), then setAltitude(0, aboveGroundLevel=TRUE) on the created uuid (for a
unit: the aggregate's uuid).

## 3. Predictions - each names the doc it comes from and the observation that would kill it
P1 (HIGH, UG52 14.3.3 + place() clampToGround): every created ENTITY platform reflects at the
   terrain surface (1140-1160 m at these coordinates), never at ~0 and never at 10000.
   COMPETING (PREREG_CLAMP_DIRECTION sec 6): the create at 0 lands at -0.0 like the 50 m create
   did, and only the AGL set lifts it. DISCRIMINATOR: WatchVrf sampling from BEFORE the deferred
   SetAltitude fires (the ObjectCreated handler) - if the first samples read ~0 and later ones
   ~1150, the clamp did not place and the set did. Either way P1 is MET if the STEADY state is on
   terrain; the discriminator tells us WHICH mechanism to trust and is recorded separately.
P2 (HIGH, 14.3.3 applied per member; disaggregatedActuator.h): every MEMBER platform of each
   unit reflects at the terrain surface. The unit's own published Z is recorded but NOT scored
   (its derivation is undocumented).
   FALSIFIER: any member at ~0 or NaN after 60 s. That is a STOP: the create clamp does not place
   below-terrain members and the AGL set on the aggregate did nothing (as its set-controllers
   document) -> plan B = setLocation(unit, authored lat/lon) (setLocationRequest.h:31-32), NOT a
   return to the 10000 m birth.
P3 (MEDIUM, setAltitudeRequest.h:24-25 vs sec 8a): the AGL set on a GROUND PLATFORM either
   (a) has no visible effect because the platform was already placed by the clamp, or (b) is what
   lifts it. Scored only via the P1 discriminator; either outcome is consistent with the docs
   read as a whole. What would be NEW information: (c) the set moves an already-placed platform
   off the surface - would falsify "harmless belt-and-braces".
P4 (record only): the 6 PLACEMENT log lines read "created at authored lat/lon (create alt 0 m);
   post-create SetAltitude: 0 m ABOVE GROUND LEVEL - C2SIM gave no altitude -> on the ground".
   No line mentions 10000, "safe MSL", or SKIPPED.
SUCCESS = P1 AND P2. P2's falsifier is the one that changes the code (plan B).

## 4. Procedure
1. Ledger appNos (sim, app, WatchVrf) BEFORE launch. 2. LaunchVrf52 -Scenario R9_Mojave_Empty_52
-NoGui. 3. WatchVrf --diag started BEFORE the init is pushed, 5 s cadence, 120 s. 4. Push the R9
lean init only. 5. Read: PLACEMENT log lines; per-uuid POS altitudes for entity, for each unit
member, and for each aggregate; note the sample index at which each land object first reads
>1000 m. 6. Fill sec 5; teardown (StopVrf52; RTI infra untouched).

## 5. Result
(filled after the run)
