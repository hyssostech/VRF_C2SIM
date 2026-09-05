# PREREG - does the placement rewrite (4b4d0f9) put ground platforms AND unit members on terrain?

Date 2026-09-05. Tier HEAVY (it changes creation for every unit and adjudicates two
documentation-vs-observation conflicts). Written BEFORE any launch. NOT YET RUN.

*** AMENDED 2026-09-05 (AMENDMENT A1, below sec 4). The arm changed BEFORE the run: land
objects are now CREATED AT terrain + 1 m from a terrain-profile query, with create-at-0 +
AGL set kept only as a logged fallback. THE LIVE PREDICTIONS AND PROCEDURE ARE A1.3 / A1.4
/ A1.6. Secs 2, 3 and 4 below are the pre-amendment text and are SUPERSEDED - they are kept
so the change of arm is legible, not because they are still what will be run. A1.1 also
corrects three FACTS in sec 1 (three objects, not six; how the entity/aggregate split is
decided; and the "1140-1160 m" band, which is wrong for two of the three objects). ***

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

## 2. What the code will do (from PlacementPolicy, offline-tested) - SUPERSEDED by A1.2
Every land object (domain 1): create at authored lat/lon with create altitude 0 (~1150 m BELOW
the surface at this AOI), then setAltitude(0, aboveGroundLevel=TRUE) on the created uuid (for a
unit: the aggregate's uuid).

## 3. Predictions - SUPERSEDED by A1.3 (kept as the pre-amendment record)
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

## 4. Procedure - SUPERSEDED by A1.6 (kept as the pre-amendment record)
1. Ledger appNos (sim, app, WatchVrf) BEFORE launch. 2. LaunchVrf52 -Scenario R9_Mojave_Empty_52
-NoGui. 3. WatchVrf --diag started BEFORE the init is pushed, 5 s cadence, 120 s. 4. Push the R9
lean init only. 5. Read: PLACEMENT log lines; per-uuid POS altitudes for entity, for each unit
member, and for each aggregate; note the sample index at which each land object first reads
>1000 m. 6. Fill sec 5; teardown (StopVrf52; RTI infra untouched).

# AMENDMENT A1 - 2026-09-05 - the create moves to TERRAIN + 1 m

Written BEFORE any launch, like the rest of this file. Nothing above is deleted: secs 2, 3
and 4 are the arm as it stood before this amendment and are marked SUPERSEDED where they
stand. This amendment states the NEW arm, its predictions, the competing hypotheses, the
instrument gaps found while checking the observer, and the exact run procedure.

## A1.0 Added citations - the NEW arm only (sec 0 stands unchanged)

VENDOR (re-read 2026-09-05 from C:\MAK\vrforces5.2d; quotes are verbatim):
- `vrfmsgs/ifRequestTerrainProfileInformation.h` - `DtIfRequestTerrainProfileInformation :
  DtSimInterfaceContent` (:17); `setRequestId(int)`; `setSendPartialInformation(bool)`, whose
  comment says "the user data of each information response is the index of the terrain profile
  request satisfied with the response"; `setPoints(const std::vector<DtVector>&)`. The reply is
  `vrfmsgs/ifIntersectionInformationResponse.h` - ":20 Note that all point information returned
  is in geocentric", `responseId()` :132, `complete()` :149.
- `vrfobjcore/terrainProfileRequestManager.h:24-25` (requests are handled in a thread),
  :109-117 (each per-point result carries `terrainHeight`).
- `vrfmsgs/ifCreateVrfObject.h:210-212`: "If True (the default) the object will be created and
  placed on the nearest polygon. Otherwise, it will be created and then placed at the altitude
  specified in the position. Subsurface entities will be constrained between the water surface
  and bottom."
- help `SimObjectsSection/ObjectCreation/vrf_newEntityPlacement.htm` (UG52 14.3.3): "By default,
  ground, lifeform, rotary-wing, and fixed-wing entities are placed on the ground. Surface and
  subsurface entities are created at sea level. Ground-based entities are placed at the highest
  possible terrain intersection at the location."
- `vrftasks/setAltitudeRequest.h:23-25`: "DtSetAltitudeRequest is used to set the altitude for
  an entity. It is ignored if the vehicle is not an air-going vehicle."
- `vrftasks/setLocationRequest.h:27,31-32`: "Z is ignored for non-air vehicles." /
  "Ground vehicles will be clamped to the terrain surface."
- MAK's own shipped sample `examples/remoteControl/commandLineRemoteController.cxx:710-772`:
  "Points are from Ala Moana terrain"; the aggregate and its members are created at ONE
  geocentric point that decodes to ~1.0 m above the ellipsoid at that near-sea-level terrain,
  and `setAltitude` is never called (0 hits for setAltitude / clampToGround under examples/).
  THE NEW ARM IS THIS PATTERN - with the terrain height asked for rather than hard-coded.

OWN RECORD:
- `docs/DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01.md` secs 1.1-1.4 (the request/reply/
  correlation contract) and sec 7 ROW 2c / ROW 2cR / ROW 3: the SAME message is LIVE-PROVEN on
  this pipeline - three requests, three complete three-sample replies, zero `warn:` lines,
  character-for-character identical on three consecutive runs. The create path reuses a proven
  request, not a new one.
- `src/VrfC2SimApp/TerrainVertexAuthoring.cs:24` (50 m horizontal gate - the frame check) and
  :30 (1 cm echo guard): the sample-validity rules the create path must reuse, or an echoed
  request point is authored as if it were a terrain height.
- `docs/VRF_ALTITUDE_FRAMES.md` sec 2: the create clamp DROPS and does not RAISE (verified),
  and the -0.0 landing of a below-terrain create is still OPEN and unexplained. The new arm
  stops creating below terrain at all, so it does not depend on that open question.

## A1.1 CORRECTIONS TO SEC 1 - facts read from the inputs on 2026-09-05, not predictions

(a) THREE objects are created, not six. `data/R9_Mojave_Lean_Initialization.xml` declares six
`<Unit>` elements but only three carry a `<Location>`: 1.BdeHQ (34.608416, -116.712685),
114.MechCoy (34.647629, -116.693388), 1222.MechPlt (34.612956, -116.600487). 1141/1142/1143
.MechPlt carry no coordinates and are skipped BEFORE placement with "Unit {Name} missing
lat/lon - skipping (parent fallback TODO)" (`VrfC2SimService.cs:539-545`). Expect THREE
PLACEMENT lines and THREE such WARNs.

(b) The entity/aggregate split sec 1 asserts is CONFIRMED, and the deciding mechanism is the
SIDC echelon character under the deployed `TypeMappingMode` "RealTemplates" (`VrfSettings.cs:77`;
`src/VrfC2SimApp/appsettings.json`) - NOT the fidelity table, which is opt-in:
1.BdeHQ SIDC `SFGPUCIZ--EH---`, echelon 'H' -> `UnitTranslator.cs:89` -> `Tank()` :196-198 ->
M1A2_Abrams_MBT, an ENTITY, DIS domain 1; 1222.MechPlt echelon 'D' -> :86 -> `ArmorPlatoon()`
:213-218 -> Tank Platoon (USA) 11:1:225:3:2:0:0, an AGGREGATE; 114.MechCoy echelon 'E' -> :87 ->
`ArmorCompany()` :220-221 -> Tank Company (USA) 11:1:225:5:2:0:0, an AGGREGATE. So P1 is scored
on ONE platform and P2 on the members of TWO units.

(c) *** THE "1140-1160 m at the R9 AOI" BAND IS WRONG AND WOULD FALSE-FAIL TWO OF THE THREE. ***
The three create points sit on terrain that differs by about 90 m. Measured, not estimated: Row 3
(run 20260902T113613Z, PREREG_TERRAIN_ROW3_DEFAULT sec 6) logged terrain + 10 m clearance per
task, and that run's own WatchVrf trace reads each clamped object's resting altitude -
    1.BdeHQ      terrain 1131.4 m  (T_R5_TK1 alts [1141.4, ...]; trace first sample 1131.4)
    114.MechCoy  terrain 1116.7 m  (T_R5_CO1 alts [1126.7, ...]; trace first sample 1116.7;
                                    its members 1116.1 - 1116.9)
    1222.MechPlt terrain 1040.6 m  (T_R5_PL1 alts [1050.6, ...]; trace first sample 1040.6;
                                    its members 1040.8 - 1041.0)
Task-to-taskee from `data/R9_Mojave_UnitMove_Order.xml` :33/:36, :61/:64, :89/:92; the trace
readings are `runs/20260902T113613Z_run/watchvrf-trace.csv` scored with the labels in that run's
`bin64-vrfSim.log`. The 1149.8 m figure sec 1 quotes is the CLAMP-DIRECTION probe point
(34.615, -116.550), which is none of these three.
CONSEQUENCE FOR SCORING: the pass band is PER OBJECT - terrain_i +/- 5 m - with terrain_i taken
from THIS run's own terrain-profile reply; the three values above are the pre-registered
expectation. They are 5.0.2-terrain values, so a systematic offset on ALL THREE is a terrain-
database difference between the R9 Mojave scenario and R9_Mojave_Empty_52.scnx and is recorded
as such; a PER-OBJECT disagreement is a placement failure.

## A1.2 (replaces sec 2) What the code will do - the AMENDED arm

Per created land object (DIS domain 1):
1. QUERY - `DtIfRequestTerrainProfileInformation` for the authored lat/lon, correlated on
   requestId, samples validated by the TerrainVertexAuthoring rules (50 m horizontal gate,
   1 cm echo guard).
2. CREATE at (authored lat/lon, terrain + 1.0 m) - the MAK-sample pattern (A1.0). groundClamp
   is never passed, so `createEntity`'s header default TRUE applies and the clamp only ever has
   to DROP 1 m, which is the direction it is verified to travel (ALTITUDE_FRAMES sec 2).
3. FALLBACK, only when the query does not answer within its timeout or returns no usable
   sample: create at altitude 0 and register the deferred `setAltitude(0, aboveGroundLevel=
   TRUE)` - the pre-amendment arm (sec 2). Which branch was taken is LOGGED per object.
The AGL set is no longer the primary mechanism for land objects; it is the fallback's mechanism.

## A1.3 (replaces sec 3) Predictions - each names its document and its falsifier

P1 - THE ENTITY IS BORN ON THE SURFACE. Confidence HIGH.
  DOC: UG52 14.3.3 / vrf_newEntityPlacement.htm ("ground ... entities are placed on the ground
  ... at the highest possible terrain intersection"); ifCreateVrfObject.h:210-212 (the default
  clamp places on the nearest polygon); commandLineRemoteController.cxx:710-772 (MAK hands the
  create a point AT the terrain and never sets altitude afterwards).
  CLAIM: 1.BdeHQ (M1A2, ENTITY) reflects within 5 m of 1131.4 m FROM ITS FIRST WatchVrf SAMPLE -
  `placement_check.py` reports mech=CREATE for it, not mech=SET.
  FALSIFIER: a first sample with |alt| <= 1 m, or within 50 m of 10000 m, or outside
  1131.4 +/- 5 m while a LATER sample is inside it.

P2 - EVERY MEMBER OF EVERY UNIT LIKEWISE. Confidence HIGH.
  DOC: 14.3.3 applied per member - each created platform goes through `place(location, heading,
  clampToGround=true)` (vrfMovingObjectStateRepository.h:251-253); the unit's own location is
  DERIVED from its subordinates each tick (vrfmodel/disaggregatedActuator.h:20-26, :48-51), so
  ground contact lives on the members.
  CLAIM: every member platform of 114.MechCoy reflects within 5 m of 1116.7 m and every member
  of 1222.MechPlt within 5 m of 1040.6 m, from its first sample. Row 3's trace gives the
  reference spread: 1116.1 - 1116.9 and 1040.8 - 1041.0.
  RECORDED, NOT SCORED: each AGGREGATE's own published Z. Its derivation is undocumented
  (ALTITUDE_FRAMES sec 1a), so it is evidence, not a criterion.
  FALSIFIER: any member with |alt| <= 1 m, within 50 m of 10000 m, or outside its unit's band
  60 s after creation.
  THAT FALSIFIER IS A STOP. Plan B if it fires: `setLocation(unit, authored lat/lon)` - the
  unit's formation controller turns it into a per-subordinate DtSetLocationRequest and "Ground
  vehicles will be clamped to the terrain surface" (setLocationRequest.h:31-32). NEVER the
  10000 m birth (ALTITUDE_FRAMES sec 0 retired it).

P3 - THE PLACEMENT LOG SAYS "TERRAIN QUERY", NOT "FALLBACK", FOR ALL THREE. Confidence MEDIUM.
  DOC: DESIGN_TERRAIN_PROFILE_VERTICES sec 7 ROW 2c / ROW 2cR / ROW 3 - the same request
  answered completely, three for three, on three consecutive runs on this pipeline; sec 1.3 (the
  back end's manager answers any sender on the session).
  CLAIM: each of the three PLACEMENT lines names the terrain query as the source of its create
  altitude and carries the terrain height it used.
  OPEN: the exact token is Lane A's to fix. The scorer greps case-insensitively for "terrain
  query" and for "fallback"; if Lane A's wording differs, quote the real line here before the
  run rather than after it.
  FALSIFIER: a "FALLBACK" line for any object.
  IF IT FIRES, THE RUN IS STILL VALID and the scoring switches for THAT OBJECT ONLY: it then ran
  the pre-amendment arm (create at 0, AGL set), so P1/P2 for it become "steady state within its
  band, by whichever mechanism", and the CLAMP-VS-SET DISCRIMINATOR applies -
    - first sample already in band (mech=CREATE): the CLAMP placed it, which is what 14.3.3
      says and what sec 1a's conflict (i) doubted;
    - first sample at ~0 followed by an in-band sample (mech=SET): the SET lifted a GROUND
      vehicle, which setAltitudeRequest.h:24-25 says is ignored for a non-air-going vehicle.
      THAT IS A NEW FINDING. It is recorded as a measurement; what should be built on it is a
      separate decision and is not part of this prediction.
  The discriminator needs a sample to land BETWEEN the create and the deferred SetAltitude.
  A1.5 GAP-1 says the instrument may not resolve that, and in which direction it fails.

P4 - NO OBJECT AT A FORBIDDEN ALTITUDE, AT ANY SAMPLE. Confidence HIGH.
  DOC: ALTITUDE_FRAMES sec 0 (the 10000 m birth, the oracle's +1 and the SIDC 'G' test are
  retired) and sec 2 (the -0.0 landing is OPEN and unexplained, so it must not appear in a run
  anything is built on).
  CLAIM: no scored uuid has ANY sample with |alt| <= 1 m or within 50 m of 10000 m.
  FALSIFIER: any such sample - `placement_check.py`'s ZERO and HIGH violations.

SUCCESS = P1 AND P2 AND P4. P3 selects WHICH scoring applies to an object; it is not itself a
pass or fail of the design.

## A1.4 Competing hypotheses - the strongest alternative to each, and what kills it

FOR P1
  H1a "The create altitude never mattered: the default clamp would have placed the object from
  ANY altitude, so terrain + 1 is decoration." KILLED BY THE RECORD, not by this run -
  PREREG_CLAMP_DIRECTION sec 6 observed a create at 50 m under ~1150 m terrain reflect -0.0, so
  the clamp does not raise. REVIVED IF: the fallback arm fires for some object and that object
  still lands in band from a create at 0. That would reopen ALTITUDE_FRAMES sec 2.
  H1b "It reads on-terrain because the query echoed the request point back instead of a terrain
  intersection." KILLED BY the 1 cm echo guard (TerrainVertexAuthoring.cs:30) AND by arithmetic:
  an echo puts the create at (its own request altitude + 1), so the logged terrain would equal
  create-altitude minus 1. CHECK: compare the terrain value in each PLACEMENT line with
  1131.4 / 1116.7 / 1040.6.
  H1c "Any of the three values would pass any of the three bands, so the test is vacuous."
  KILLED BY the bands: 1131.4, 1116.7 and 1040.6 are 15 m and 76 m apart against +/-5 m, so a
  mixed-up correlation FAILS rather than passes. This is why the band is per object.

FOR P2
  H2a "The members are placed by the aggregate's create, so an aggregate in band implies its
  members are." KILLED BY reading the members: disaggregatedActuator.h:20-26 derives the unit's
  position FROM the members, so the implication runs the other way. A run with the aggregate in
  band and any member out of band falsifies H2a directly - which is why P2 scores members and
  only records the aggregate.
  H2b "Member altitudes are formation output, not placement output: R1's set+reorganize
  (VrfC2SimService.OnVrfObjectCreated) snaps members into formation after creation." NOT KILLED,
  and it does not need to be for P2: formation snapping issues per-subordinate
  DtSetLocationRequests, and those clamp ground vehicles (setLocationRequest.h:31-32), so it
  predicts on-terrain too. It becomes decisive only if members read on-terrain while the
  aggregate's own PLACEMENT line shows a below-terrain create - so record that line.
  H2c "Nothing was created and the observer is reading the fixture's own objects." KILLED BY the
  fixture (R9_Mojave_Empty_52.scnx is empty) and by the census: scored uuids must be 0 before
  the init push, then 1 entity + 2 aggregates + their members.

FOR P3
  H3a "The reply arrives but carries no usable sample, because the terrain page under that
  lat/lon is not resident at create time." LIVE ALTERNATIVE and the most likely cause of a
  FALLBACK line. Developer's Guide contract C1 (DESIGN doc sec 1.5): a non-blocking terrain
  query on unpaged terrain "will return immediately and DataAvailable will be set to false"
  with "no terrain intersections". DISCRIMINATOR: fallbacks cluster at the START of the create
  sequence and later objects succeed, and the vendor log shows the page-in. That is a TIMING
  finding about the create path, not a refutation of the query.
  H3b "The query works but the app correlates a reply to the wrong object." KILLED BY the 50 m
  horizontal gate (TerrainVertexAuthoring.cs:24): the three create points are 1.7 - 5.6 km
  apart, so a mis-correlated sample is rejected and shows as a FALLBACK, never as a
  wrong-terrain success.

FOR P4
  H4a "A forbidden value is a dead-reckoning artefact of a moving object, not a placement."
  NOT APPLICABLE BY CONSTRUCTION: no order is pushed, so nothing is tasked to move, and a
  forbidden value on a stationary object cannot be a DR artefact. (Row 3's trace shows DR
  excursions to 2400 - 3400 m on MOVING members. That is exactly why this prereg pushes no
  order.)

## A1.5 Gaps in the instrument, found 2026-09-05 BEFORE the run

GAP-1 - P3's discriminator is ONE-SIDED and may not resolve. WatchVrf's cadence argument is a
POSITIVE INTEGER of seconds (`WatchRunner.cs:164-166`, ToolArgs.TryPositiveInt): 1 s is the
floor and there is no sub-second option. Its first sample is at start + 3 s ("small settle so
discovery gets going", :311) and every cadence seconds after. The create and the deferred
SetAltitude are both on the app's tick thread - SetAltitude is issued from OnVrfObjectCreated
(`VrfC2SimService.cs:1354-1355`) as soon as the create is acknowledged - so if that round trip
is under about a second, NO sample can fall between them. CONSEQUENCE: mech=SET is conclusive
(something lifted the object after the first reading); mech=CREATE is NOT conclusive on the
FALLBACK arm. On the primary arm it does not matter: there is no set to be confused with.
The same limit is WIDER for MEMBER platforms: a member's first sample is its first sample after
HLA discovery, not at creation, so "first sample" for P2 is an upper bound on how early the
member was on terrain. P2 is therefore a statement about where members ARE, and P1's
create-vs-set reading is carried by the ENTITY, which is created directly by this app.

GAP-2 - uuid-to-name correlation on 5.2 is unproven. `placement_check.py` labels uuids from the
vendor log's "Locally Simulated: <name> (VRF_UUID:...)" lines, which is what makes MEMBERS
nameable at all. That marker appears 65 times in a 5.0.2 run log
(`runs/20260902T113613Z_run/bin64-vrfSim.log`, where it correctly labelled all 44 scorable
objects) and ZERO times in the only 5.2 vendor log in the tree
(`runs/launch52/vrfSim_3908_20260904T145308Z.log`) - but that log is a fixture-load-only
capture with no remote creates, so its silence is not evidence either way. OPEN: whether a 5.2
harvested log with creates in it carries the marker. If it does not, P2 degrades from
per-member-by-name to per-member-by-band and members must be attributed to units by proximity;
the app's own "VRF created {Name} -> {Uuid}" line (`VrfC2SimService.cs:1350`) names only the
three created objects and is at Debug level, so it needs `Logging__LogLevel__Default=Debug`.

GAP-3 - no per-object terrain is logged today. The PLACEMENT line as it stands
(`VrfC2SimService.cs:659-662`) prints the create altitude and the AGL set but NO terrain
height, because there was no query. Lane A's amended line MUST print the terrain height it used
and which branch produced it, or P1's band cannot be checked against this run's own reply and
H1b cannot be tested at all.

GAP-4 - the empty-trace false green. `placement_check.py` returns FAIL, not PASS, when a trace
has no scorable POS object: a zero-object observation is the shape this project keeps mistaking
for success (memory: lessons - false greens).

## A1.6 (replaces sec 4) Procedure - exact commands, in order

PowerShell 7, from the repo root. `<A_SIM>`, `<A_WATCH>`, `<A_APP>`, `<A_PROBE>` are the
ledgered application numbers; `$Repo` is this checkout.

STEP 0 - LEDGER FIRST, BEFORE ANY JOIN. Read the single line of the form
`*** NEXT FREE: <n> ***` in `docs/OPUS_EXECUTION_PLAN.md` Appendix B (it read 3918 when this
amendment was written), claim FOUR consecutive numbers - `<A_SIM>` back end, `<A_WATCH>`
observer, `<A_APP>` interface, `<A_PROBE>` spare for an RtiProbe retry - and rewrite the marker
to n+4. Never reuse a number (RUNBOOK sec 0; CLAUDE.md sec 5). No GUI is launched, so no
frontend number is claimed.

STEP 1 - shell environment, once per shell (RUNBOOK 0.5.13; the 5.2 runtime path trap, DIFF
sec H - MAK DLLs bind by NAME on PATH and the Machine PATH lists 5.0.2 first):
```
$Vrf='C:\MAK\vrforces5.2d'; $Vrl='C:\MAK\vrlink5.10'; $Rti='C:\MAK\makRti5.0.1'
$Bin64 = Join-Path $Vrf 'bin64'
$env:PATH = "$Bin64;$Vrl\bin64;$Rti\bin;$env:PATH"
$env:MAK_RTIDIR            = $Rti
$env:RTI_RID_FILE          = "$Repo\config\rid-501-rtiexec-min.mtl"
$env:RTI_ASSISTANT_DISABLE = '1'
$env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
```
Every federate in the run must share that rid, or it does not share a connection.

STEP 2 - rtiexec (ENSURE-UP; never killed, persists across runs - RUNBOOK 0.5.2):
```
pwsh -File $Repo\scripts\StartRtiExec52.ps1
```
exit 0 = READY (already up, or started and listening on TCP 4001). exit 3 = not listening ->
STOP; do not launch a sim into a federation with no rendezvous.

STEP 3 - back end, headless, on the empty R9 fixture:
```
pwsh -File $Repo\scripts\LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52 -NoGui -BackendAppNumber <A_SIM>
```
`-FrontendAppNumber` is not needed with `-NoGui`. `--logFileName` is NOT passed (1-in-3 startup
crash, PREREG_52_CRASH_BISECT sec 5); the vendor's own log is harvested to
`$Repo\runs\launch52\vrfSim_<A_SIM>_<stamp>.log`. exit 3 = startup crash -> STOP.
SECRETS: that harvested copy carries the whole process environment in cleartext - never attach
it to a ticket, mail or issue.

STEP 4 - OBSERVER, STARTED BEFORE THE INIT PUSH, 1 s cadence:
```
Push-Location $Bin64
& $Repo\tools\WatchVrf\bin\Release-5.2\net10.0\win-x64\WatchVrf.exe <A_WATCH> 300 1 --diag --report-backends `
    *> $Repo\runs\launch52\watch_<A_WATCH>_placement.trace
Pop-Location
```
Positionals are `applicationNumber durationSecs sampleSecs` (`WatchRunner.cs:149-166`); 1 s is
the cadence floor (A1.5 GAP-1). Run it in its own window or job and CONFIRM it has printed a
`# t=` line with `backends=1` BEFORE step 5 - a trace that begins after the creates cannot
answer P1, and that is the whole experiment.

STEP 5 - INIT PUSH (what the runner does at Stage 6, `RunC2SimScenario.ps1:2383-2386`; the
endpoints are the PRIVATE server c2sim-server-vrf, `:305-306`):
```
& $Repo\tools\PushInit\bin\Release\net10.0\PushInit.exe `
    $Repo\data\R9_Mojave_Lean_Initialization.xml `
    http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
```
PushInit is managed-only and shared with 5.0.2 - there is no Release-5.2 build of it. exit 2 =
usage error and the server was NOT touched; exit 1 = the push was rejected. NO ORDER IS PUSHED
in this prereg: the question is where things are BORN.

STEP 6 - the interface, which is what actually creates the objects (Stage 6b,
`RunC2SimScenario.ps1:2418-2426`; the init must already be shared - RUNBOOK sec 3, init first,
the app late-joins):
```
$env:Vrf__ApplicationNumber   = '<A_APP>'
$env:C2SIM__RestUrl           = 'http://127.0.0.1:18080/C2SIMServer'
$env:C2SIM__StompUrl          = 'http://127.0.0.1:61614/topic/C2SIM'
$env:Vrf__ConfigFileIdentity  = 'true'
$env:Vrf__Federation          = ''
$env:Vrf__FedFileName         = ''
$env:Vrf__ConnectionConfigFile= "$Vrf\appData\settings\connections\MAK-ONE-2025-Config.xml"
$env:Vrf__TypeMapFile         = "$Repo\data\unit-type-map-52.json"
$AppDir = "$Repo\src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64"
Push-Location $Bin64
& $AppDir\VrfC2SimApp.exe --contentRoot=$AppDir *> $Repo\runs\launch52\app_<A_APP>_placement.log
Pop-Location
```
cwd MUST be VR-Forces bin64 so Legion finds `vrfLegion.lua` (RUNBOOK sec 7 item 3). Leave the
app running at least 90 s after the last PLACEMENT line so P2's "after 60 s" is observable, then
stop it (Ctrl+C, or tools/StopIface).

STEP 7 - teardown. Let the observer run out its window, then:
```
pwsh -File $Repo\scripts\StopVrf52.ps1
```
rtiexec / rtiForwarder / rtiAssistant are NEVER touched (RUNBOOK 0.5.2).

STEP 8 - SCORE, offline:
```
python $Repo\tools\analysis\placement_check.py $Repo\runs\launch52\watch_<A_WATCH>_placement.trace `
    --vendor-log $Repo\runs\launch52\vrfSim_<A_SIM>_<stamp>.log `
    --expect-name 1.BdeHQ=1131.4 --expect-name 114.MechCoy=1116.7 --expect-name 1222.MechPlt=1040.6 `
    --tol 5 --band 1000:1200 --show-skipped
```
It prints, per uuid, the FIRST sample altitude and its time, the first sample at or above
1000 m, the steady-state altitude, the min/max, the band applied, and mech=CREATE|SET|NEITHER -
plus PASS/FAIL per object and overall. exit 0 = every scored object passed; 1 = at least one
failed OR nothing was scorable; 2 = usage. Members carry the vendor's own markings ("M1A2 n",
"AR Plt n", "M3 1"), so once the labels are known add `--expect-name` for a member family;
anything unlabelled falls back to `--band`.
THEN READ THE APP LOG for what the trace cannot show: the three PLACEMENT lines (P3), the three
"missing lat/lon - skipping" WARNs (expected, A1.1a), the terrain height each query returned
(compare against the --expect values - that is the H1b check), and any "FALLBACK".

STEP 9 - fill sec 5: per-object first / steady / mech, the three PLACEMENT lines verbatim, the
terrain values the query returned, a verdict per prediction, and any hypothesis in A1.4 that the
run leaves standing.

# AMENDMENT A2 - 2026-09-05 - cold-start review NO-GO fixes (BEFORE any launch)

A cold-start adversarial review returned NO-GO on A1 for two errors, both verified against the
primary sources by the supervisor before this amendment. A2 supersedes the A1 items it names.

## A2.1 (supersedes A1.1a and H2c) - SIX objects, not three; ZERO skip-WARNs
VERIFIED: 1141/1142/1143.MechPlt carry no `<Location>` but name superior `139aa71b` = 114.MechCoy,
which HAS one, so InitParser's superior cascade (`InitParser.cs:120-132`) fills their lat/lon -
they are NOT skipped. The Row-3 run on this same lean init created 1143.MechPlt
(`runs/20260902T113613Z_run/vrfc2simapp.log`). So: **6 PLACEMENT lines, 0 "missing lat/lon" WARNs.**
Split under RealTemplates (SIDC[11]): 1.BdeHQ 'H' -> M1A2 ENTITY (domain 1); 114.MechCoy 'E',
1141/1142/1143/1222.MechPlt 'D'/'E' -> aggregates. **1 ENTITY + 5 AGGREGATES.** 1141/1142/1143 all
inherit 114.MechCoy's point, so FOUR aggregates are STACKED there (DeStackCreates default false,
`VrfSettings.cs:130`) - expected, recorded, not a fault. A1.4 H2c's census is corrected to
"1 entity + 5 aggregates, members of each"; a run showing 3 objects would now be the FAILURE.

## A2.2 (supersedes A1.2/A1.3 confound, closes A1.5 GAP-1) - this run ISOLATES the create
The AGL set was computed and registered on BOTH arms (`PlacementPolicy` sets it independent of
terrain; `FinalizePlacement` registered it either way), so an object reading on-terrain could not
be attributed to the create vs the set - and sec 8a proved the set alone lifts a buried ground
M1A2. NEW KNOB `Vrf:PlacementAglSet` (default true = production safety; VrfSettings.cs) suppresses
the placement-path set. **THIS RUN SETS IT FALSE.** Then, for every land object, where it ends up
is the CREATE ALONE - no set to rescue it. Consequences:
- P1 (ENTITY) and P2 (MEMBERS) both become clean create-path tests. P1 is no longer confounded.
- P3's clamp-vs-set discriminator is retired for this run: there is no set. A first sample already
  on terrain = the create placed it (what UG52 14.3.3 predicts and PREREG_CLAMP_DIRECTION sec 6
  doubted for a deep create); |alt|<=1 m or -0.0 at any sample = the create did NOT place it, and
  because there is no set, that is a clean STOP -> the code needs the set (default true) after all,
  OR the terrain-anchored create must be proven to land on the surface. Either is a real finding.
- SUCCESS = P1 AND P2 AND P4, all now measuring the create.

## A2.3 (supersedes STEP 6 / STEP 8) - env + scoring corrections
STEP 6 additionally exports, before launching the app:
  `$env:Vrf__PlacementAglSet='false'`  (isolate the create - A2.2)
  `$env:Logging__LogLevel__Default='Debug'`  (so the app's "VRF created {Name} -> {Uuid}" line at
     VrfC2SimService.cs is emitted - it is the ONLY name->uuid channel proven present on 5.2;
     GAP-2: the vendor log's "Locally Simulated:" marker has 0 occurrences in the one 5.2 log we
     have, so member-by-name may be unavailable and members then fall to per-band scoring).
STEP 8 scoring:
  - `--expect-name` for all SIX created objects: 1.BdeHQ=1131.4, 114.MechCoy=1116.7,
    1141.MechPlt / 1142.MechPlt / 1143.MechPlt = 1116.7 (they inherit 114's point), 1222.MechPlt
    =1040.6. Members inherit their unit's band.
  - The FALLBACK check greps ONLY `PLACEMENT: ... from the FALLBACK` lines - "fallback" also
    appears in the request-sent INFO line and the summary on every run (`placement_check.py` /
    the app log), so an unscoped grep false-positives.
  - RE-SCORE RULE for a systematic terrain offset: the reference bands are 5.0.2-terrain values;
    R9_Mojave_Empty_52.scnx uses "MAK Earth (online)" terrain. If ALL objects miss their band by
    the SAME sign and magnitude (within ~2 m of each other), that is a terrain-DB difference, not
    a placement failure: re-score each object against `this run's own` PLACEMENT terrain value
    +/- CreateClearanceMeters, and record the offset. A PER-OBJECT disagreement is a failure.

## A2.4 code guards added with A2 (cold-start review, non-blocking recommendations, done)
- `Vrf:PlacementAglSet` (above).
- ECHO/NO-DATA guard in `ResolvePlacementTerrain`: a terrain height within EchoToleranceMeters of
  the request point's own altitude (a no-data 0.0 at a 0-altitude create point) is REJECTED, so it
  cannot be logged as a real sea-level TERRAIN QUERY answer; that object falls back.
- try/catch around `RequestTerrainProfile` -> `FinalizePlacement(null)`: an exception can no longer
  silently drop every create (it was swallowed by TickLoop).
All three: build 0 errors, 9/9 offline self-tests green.

## 5. Result - 2026-09-05, RUN COMPLETE. SUCCESS (P1 AND P2 AND P4 all held).
appNos 3918 sim / 3919 WatchVrf / 3920 app (3921 spare, unused -> burned). Artifacts:
runs/launch52/placement_sim_3918_*.txt, watch_3919_placement_*.trace, app_3920_placement_*.log,
vrfSim_3918_placement_live.log. PlacementAglSet=false (create ISOLATED), Debug logging on.

HEADLINE: 44 of 44 scored objects on terrain, `mech=CREATE`, 0 failed
(placement_check.py, exit 0). The AGL set was SUPPRESSED, so this is the CREATE ALONE.
- 6 objects created, 0 skip-WARNs (A2.1 confirmed live): 1.BdeHQ PLATFORM + 114/1141/1142/1143/
  1222.MechPlt UNITS; PushInit reported "6 Units, SystemName=[STP]".
- PLACEMENT: 6 of 6 create altitudes from the TERRAIN QUERY, 0 FALLBACK. The request points were
  sent at altitude 0 (~1150+ m below the surface) and the query STILL returned real terrain
  heights - so **the OPEN question (does a below-terrain request point defeat the query?) is
  answered NO** (finding 9 / A1.0 Q2 closed). Terrain returned: 1.BdeHQ 1154.7, 1222.MechPlt
  1117.2, 114.MechCoy/1141/1142/1143 1260.1 (the three platoons inherit 114's point).
- P1 (ENTITY, create alone): 1.BdeHQ reflected 1154.7 m = its terrain height exactly, from its
  first sample, mech=CREATE. HELD.
- P2 (MEMBERS, create alone): every member of every unit on terrain, first sample, mech=CREATE -
  M1A2 1/2/3/4 at 1114.9-1120.1 (1222's ~1117 m terrain); the 114+3-platoon members (M1A2 6-30,
  HMMWV, M3, M577A2, AR Plt) at 1243-1328 m (~1260 m terrain + formation spread). HELD.
- P4 (no forbidden altitude): no scored object at |alt|<=1, -0.0, NaN or 10000 at any of 82
  samples. HELD. (The one skipped object is GlobalEnv, the fixture's cast-corrupted control.)
- GAP-2 CLOSED: "Locally Simulated:" appears 46x in this 5.2 vendor log (0x in the fixture-only
  log), so member-by-name labelling works on 5.2; all 44 labelled.
- The 5.0.2 reference bands (1131.4/1116.7/1040.6) did NOT apply - the 5.2 "MAK Earth (online)"
  terrain differs and NOT uniformly (per-object, not a flat offset). Scored per A2.3 against THIS
  run's own terrain replies +/-20 m; every object passed its own band.

WHAT THIS SETTLES: create-at-terrain-height places ground platforms AND unit members on the
surface with NO altitude set, NO 10000 m birth, NO reliance on the clamp raising anything. The
whole altitude saga is closed on the create path. The belt-and-braces AGL set stays default-ON
in production (it did no harm and is cheap insurance); this run proved it is not NEEDED.
STILL NOT DONE (out of this prereg's scope): a run that pushes an ORDER and confirms movement
(PREREG_R9_52); the aggregate's own published Z was recorded (units read 1117-1276 m, member-
derived) not scored, its rule still undocumented.
