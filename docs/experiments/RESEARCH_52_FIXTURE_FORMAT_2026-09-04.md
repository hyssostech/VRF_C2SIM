# RESEARCH: the 5.2d scenario fixture - container, terrain, SMS, headless path (2026-09-04)

Read-only. No process launched, nothing written under C:\MAK, no code changed. Sources:
UG52 = C:\MAK\vrforces5.2d\doc\VRFUsersGuide.pdf; AC52 = doc\AddingContent.pdf; DISK =
inspected with python zipfile / text read. V = verified on disk or quoted; I = inferred.

## 1. The .scnx container - 5.0.2 vs 5.2d

V UG52 12.1 p350: ".scn lists the locations of the other scenario files, the terrain
database, and the simulation model set"; .oob objects + state; .omp object->sim-engine map
(12.4 p357); .pln plans (12.3 p356, "we recommend that you do not try to create plan files
or edit plan files by hand"); .xtr force hostility + spawn templates; .spt scenario Lua;
.ovl unpublished tactical graphics; .sgr selection groups; .osrx observer views;
.gui_settings GUI settings. "By default, scenarios are saved in a compressed zip archive
with the extension .scnx." .orb (Orbat) is referenced by the .scn but is NOT in the UG52
list; every shipped .orb, both versions, is the 10-byte literal "(orbat )".

V DISK: the member set is IDENTICAL 5.0.2 vs 5.2d - same 11 extensions (TropicTortoise.scnx
vs Sample\Raid, Traffic, FirstExperience\firstexperience). .ovl (291 B), .sgr (402 B) and
.spt (313 B) are byte-identical boost_serialization v14 stubs in both, and the .pln
boilerplate is exactly the template build_fixture.py :466-509 authors.
V .scn: `(version 3.200000)` in BOTH. Key-set diff (5.0.2 Raid.scn vs 5.2d Raid.scn):
- ADDED in 5.2: gui-runtime-scheme, gui-runtime-scheme-data, remote-attachment-scheme,
  remote-attachment-scheme-data (UG52 Table 20 p354; DIFF E3+E4). Optional - firstexperience
  carries them empty, 5.2d default-scenario.tmpl omits them.
- Component-Attachment is NOT a version marker: UG52 Table 20 p353 says it "is not saved in
  the scenario file. To force a specific value, you must edit the scenario file" (V: 5.2d
  samples split 17 with / 22 without).
- Unchanged, i.e. everything FixtureGen touches: Terrain-Database, Gui-Terrain-Database, the
  9 part-name references, Simulation-Model-Set-Files, scenario-name, frame-mode, frame-time.

V THE ONE REAL BREAK - .oob object-type syntax. 5.0.2 writes `(object-type  1 (17 0 0 2 0 0
0))` (class prefix + parenthesised 7-tuple); 5.2-saved scenarios write the flat
`(object-type 17 0 0 2 0 0 0)` - same shape as the 7-field SMS change (DIFF sec F/H). 5.2d
READS both: of 62 shipped 5.2d .scnx, 39 still carry the old form (Raid, ArrivalsHall, all
luaTerrainReasoningQuery examples), 23 the new (firstexperience, Traffic, GroundMovement,
WainwrightMechanizedAttack, all AggregateTacticalLevel). That is MG C3 "pre-5.2 scenario
loads" observed on disk.
I FixtureGen transfer: the container approach (unzip, edit parts as text, re-zip) transfers
unchanged, and `build_frame_variant()` :130-181 works on a 5.2 .scnx as written. The GRAFT
path does NOT: `own_class()` :271 and the member filter :388 match only the old form, both
clone sources (testFindTankPlatoonPositions, MaklandCoordinatedAttack) are old-form or absent
from 5.2d, and validate_fixture.py :67,:95 has the same coupling.

## 2. Terrain for the R9 Mojave AOI on 5.2d

V R9 box (data\R9_Mojave_Initialization.xml, 58 points; _UnitMove_Order.xml, 6):
lat 34.5605..34.6696, lon -116.7127..-116.3867.
V Shipped .mtf covering it (SharedData\19\latest\TerrainData\TerrainConfiguration): every
whole-earth online configuration - MAK Earth (online), MAK Earth Base (online), MAK Earth
Air and Space (online), MAK Earth Aggregate (online), MAK Earth (online) - Simple For High
Fliers - plus NTC (online).mtf (AC52 Table 1 p30: "A high-resolution area of the National
Training Center at Fort Irwin in California"). "MAK Earth Space (online).mtf" is absent
(DIFF C1 re-confirmed). NO offline .mtf covers the AOI - the local-data terrains are Ala
Moana, ArrivalsHall, Brooklyn, DestructibleTerrain, Grid, Ground_DB II, MAK Proving Ground,
PacificGroveTrail, Sadr City, TrumanCarrier, VR-Village and Test-*.
V NTC (online).earth is itself a worldwide TMS base (vr-theworld.com layer 149) + one local
berm GeoTIFF + OSM/biome/building includes, with no .surfChar.map - so it is not an offline
option and buys nothing at 34.6N (its insets sit north of the R9 box). Y-7's ruling
(MAK Earth (online)) stands; NTC is not a better pick.
V AC52 p28: terrains named "(online)" "connect to VR-TheWorld or another online server and
therefore require an internet connection." Cache dir: OSGEARTH_CACHE_PATH /
VRFSIM_OSGEARTH_CACHE_PATH, the sim engine honouring the VRFSIM_ one (AC52 8.1.1 p239).
V AC52 8.1.2 p239-240: generating the cache is GUI-ONLY (View > Terrain Editor Panel > Tools
> Generate Cache, lat/lon box + LOD range) and "Feature data is not cached" - Y-7 option (2)
confirmed as stated.

V OFFLINE-AUTHORED (Y-7 3/4, COLD_START_MAP 5.a): N34W117.dem (27,264,000 B) and N34W117.tif
exist under TerrainData\Terrain\California\{Elevation,Imagery}, and a scan of every
.earth/.mtf/.xml under SharedData\19 finds ZERO references to either - a .mtf must be
authored. AC52 p28 workflow: (1) create an earth file, (2) add terrain patches, (3) save as
.mtf, (4) open it; steps 2-4 are documented only as GUI actions (AC52 ch.2, ch.4 "Managing
Terrains Using the MAK ONE Application GUI") and no CLI terrain builder is documented.
I: both artefacts are text - NTC (online).mtf is a single-patch boost XML whose only variable
parts are <myName>/<myFilename> - so a headless clone (that .mtf repointed at an authored
"Mojave Offline.earth" carrying GDALElevation + GDALImage on the two N34W117 files) is
PLAUSIBLE but UNVERIFIED; shipped .mtf boost versions differ (NTC=14, MAK Earth=20).

## 3. The SMS

V UG52 Table 20 p354: Simulation-Model-Set-Files "Specifies a list of SMS files to load. The
path is relative to the default data directory (./data) or a user-specified data directory."
Every 5.2d entity-level sample uses
`(Simulation-Model-Set-Files "$(DATA_DIR)\simulationModelSets\EntityLevel.sms")`.
V DISK data\simulationModelSets: AggregateLevel, AggregateLevelBase, AggregateTacticalLevel,
EntityLevel, MAKTest, base - no C2simEx (DIFF C2 / Y-8). vrfSim also takes
`--simulationModelSet sms` (UG52 Table 11 p183, "must be declared separately for GUI and sim
engine, even in combined mode"); a .bsn entry can override it with `sms-filename` (Table 15).

## 4. An EMPTY fixture, headlessly

V Frame keys UNCHANGED (UG52 Table 20 p354, verbatim): frame-mode is one of variable-frame /
fixed-frame / fixed-frame-run-to-complete; frame-time "the length of a frame, in seconds ...
If frame-mode is set to fixed-frame or fixed-frame-run-to-complete, you must set the frame
time to a non-zero value. A value of zero ... prevents simulation time from advancing."
Identical to the 5.0.2 text build_fixture.py :74-116 cites; modes at UG52 3.4.3 p122.
FixtureGen's --frame-mode / --frame-time need no change.
V There IS a documented no-scenario start: vrfSim `(--terrainDatabase | -T) terrain_database`,
"Mutually exclusive with the -L option" (UG52 Table 11 p184; example 4.5.1 p145), and UG52
3.2.5 p118 says a sim engine started that way "can be used to create new simulation objects
and tactical graphics going forward in the simulation". With -T there is no .scn, hence NO
frame-mode/frame-time lever (--clockMode is unrelated - it picks the Windows timestamp clock
source, UG52 p179).
V Batch mode is neither a fixture builder nor a runner for us: "Batch mode is read-only. You
cannot save a scenario, create simulation objects or objects, pause the scenario, or
otherwise change it, while a batch is running" (UG52 7.10 p269).
V No CLI or Lua "save scenario" path was found. The only proven headless authoring channel is
the one this repo already owns: edit zip members as text and re-zip (build_fixture.py,
byte-for-byte reproducible on 5.0.2). And a globals-only .oob is a real, loadable shape:
5.0.2 TropicTortoise.oob holds only GlblTerrDmg / GlobalEnv / Blocking Terrain Page-In Area,
and 5.2d firstexperience.oob holds GlblTerrDmg (105 105 ...) + GlobalEnv (21 0 0 1 0 0 1)
beside its 3 real objects. 5.2d factory vrfSim.mtl ships automaticallyCreateGlobalEnvironment
0, blockOnAsynchronousOperations 0, maxAsynchronousTerrainThreads 7,
loadAllNavigationDataOnTerrainLoad 0, defaultTerrainDatabasePath "".

## 5. Recommended path, ranked (levers only - no code proposed)

R1 EMPTY .scnx on MAK Earth (online) + EntityLevel.sms, built by FixtureGen from a
5.2-NATIVE-saved donor (GroundMovement.scnx, Weather.scnx or BehaviorGroundAttackByFire.scnx
- all three are MAK Earth (online) + EntityLevel + new-format .oob). Levers: the existing
--frame-variant member-copy machinery, plus "strip the .oob to its global objects, empty the
.omp and .pln, retarget part names / scenario-name". Cheapest artefact that keeps the frame
lever, the terrain ruling and the AOI. Risks: a globals-only load on 5.2 is untested (I);
stale ScenarioExtentInformation aims the GUI at the donor AOI (cosmetic); the graft/validator
regexes must learn the flat object-type form before any UNIT is authored into a 5.2 .oob.
R2 NO fixture: launch with -T "<...>\MAK Earth (online).mtf" --simulationModelSet EntityLevel
and no -L. LaunchVrf52.ps1 already drops -L when -Scenario is '' (:193); the missing levers
are -T and --simulationModelSet. Zero artefacts, but LOSES frame-mode/frame-time (no .scn),
taking the FFRTC speed-up and the Y-9 fixed-frame knob with them. Prototype zero, not the
Phase-2/3 baseline.
R3 Status quo (runner default Sample\FirstExperience\firstexperience on Ala Moana, :496):
keeps running but abandons the Mojave AOI and every 5.0.2 comparison. Fallback only.
R4 Offline terrain (Y-7 3/4) from N34W117 - only if R1's online fetch is the blocker;
UNVERIFIED, see sec 2.
Terrain risk shared by R1-R3 (Y-9, DIFF C5): an (online) terrain makes the first frames pay a
vr-theworld tile fetch, and blockOnAsynchronousOperations applies ONLY in a fixed-frame mode
- so that knob exists in R1 and does not exist in R2.

## 6. Questions only a live run can answer

- Does a globals-only (or empty) .oob load on 5.2d without a GUI, and does the back end reach
  serviceable readiness on it?
- Does vrfSim -T (no -L) tick, accept remote object creation and publish - R2 a product path
  or only a probe?
- Does MAK Earth (online) page in at 34.6N / -116.6W inside the runner's readiness budget,
  and what does blockOnAsynchronousOperations 1 cost there in wall time?
- Do the C# creation and MoveAlongRoute stages behave the same on a fixture whose .scn we
  authored rather than 5.2 saving it (the .scn is the only member we would author)?
- Does 5.2 accept a .scn WITHOUT the four new gui-runtime-scheme / remote-attachment-scheme
  keys (default-scenario.tmpl omits them; every 5.2-saved .scn has them)?
