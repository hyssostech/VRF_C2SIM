# PREREG - IRON STORM ON THE AGGREGATE PROFILE, RUN G1-5: G1-4 re-run on the W1 build - nudged member slots clear OSM water by 10 m (the planner's obstacle buffer), H5 gated on every tasked member, the scorer's river rule read on the road and bridge ways (one unscored pre-warm + one scored run)

STATUS: STOPPED AT THE W GATE 2026-10-04 - FAIL (P-FALS(e)) as scored, every other limb passed; whether it binds is the
owner's question (the Result below; G1-4 precedent RL-20261004-01). Was: REGISTERED 2026-10-04, LAUNCH PENDING - the seat's
go-live under RL-20260928-01. COLD-REVIEWED 2026-10-04 by Fable
(RL-20260928-04; scratch 7207877b...\fable_g15\REVIEW_G1-5_PREREG_fable_2026-10-04.txt): GO WITH FIXES - findings 1-7 and ITEM 5
applied in the commit after a08bad8 (the composite reading and the one to watch, the four boots, the bridge rule's acceptance
region and two controls, H5's closed-vertex rule in W, step S, the recorded water distance, the moved-member records). Registered by lane G1-5 (session 7207877b; the registration commit's own time is
the stamp) on branch prereg/g1-5 from main d455092, BEFORE any order push, holder action or launch. PREPARATION ONLY: no
C2SIM push, no rtiexec or holder start, no VR-Forces or runner launch, no appNumber claimed, no edit of
docs/OPUS_EXECUTION_PLAN.md, no C:\MAK write, no build. Marks: [V] = checked by this lane while writing this file (read-only
hashes, source reads, the deployed dll and the scorer on real run directories, all offline); [A] = taken from the record or
another lane's output, not re-checked here. This file is G1-4's registration (PREREG_IRONSTORM_AGG_G1-4_2026-09-28.md, "G1-4"
below - itself G1-3's with one variable) with ONE intended variable - W1 as deployed - and it says only what differs: every
section of G1-4 not restated here applies unchanged, with its src line numbers moved by the map of sec 2. Its Result (N1-N3,
COLD REVIEW AND RULING) is this registration's starting point. Scratch <L5> = H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\
7207877b-f104-4515-91f7-c662e3fce85c\scratchpad\laneG1-5 (every g1_5_* script and output named below).

## Registration

PREREG ID: IRONSTORM_AGG_G1-2026-10-04-5
DATE (UTC): 2026-10-04, about 20:30Z, before any launch (the registration commit's timestamp is authoritative)
BINARY / COMMIT: the DEPLOYED main-checkout build 1.0.0+git.52f50e0.Release-5.2 (not DIRTY), rebuilt IN PLACE by the W1
deploy (OPS agent of the seat) from main 52f50e0 at 19:16:03-19:16:27Z - managed code only, the ten managed consumers; the
bridge was not rebuilt, so the pin 03226dd0 stands (RUNBOOK sec 9, "W1 DEPLOY 2026-10-04 19:16Z", :2420 [V: read]). 52f50e0 =
the W1 merge b0c3bb6 (fix/slot-water-clearance e4badc8 + 03a350d + 377ce98) + docs-only commits; `git diff --stat 52f50e0
d455092 -- src scripts tools data config` is EMPTY [V], and `git diff --stat b0bad53 52f50e0 -- src scripts tools data config
tests` = OsmSelfTest.cs, Preflight/PreflightService.cs, VrfC2SimService.cs, VrfSettings.cs, appsettings.json,
appsettings.Demo.json - W1 only; scripts/ unchanged since G1-4's build [V]. Read-only hashes at 2026-10-04T19:38Z [V]:
VrfC2SimApp.exe 9253e7f33c8b44f6b1f4d224a3cef584261df430f7a668ce3039067cd82b1af3, VrfC2SimApp.dll
deb658ebaf4e0810f76a1e849e2dfd6c6bc38fa4731e16b9ed875b074c097579 (both = the deploy line), ProductVersion
1.0.0+git.52f50e0.Release-5.2, exe written 19:16:08.86Z; deployed appsettings.json
e9c742e9dab2956edf7a9233aa7b79fc6adc5ce1265563f664309ba3b6458e2c = src at d455092, appsettings.Demo.json
09bf49bc8c4abd49a45e4630b72e50dca179fd4d62b034b37b290fc77f9c22e1 = src; VrfBridge.dll 03226dd0... in the app, WatchVrf,
CreateOne and RtiProbe trees; RtiProbe.exe 9f960c39... / RtiProbe.dll 517510d8... (lane R4's tree of 2026-09-28 18:16Z, not
rebuilt since). Deployed settings [V]: Vrf:PreflightSlotWaterClearanceMeters 10 (appsettings.json :98, appsettings.Demo.json
:83), PreflightBuildingClearanceMeters 10, PreflightVertexNudgeMaxMeters 300, PreflightRouteShift true, AggregateMovePlanner
Auto, RoadProximityMeters 500, AllowLiteralMove false, PreflightOffline false, VertexArrivalRadiusMeters 100. Fixture
IronStorm_Centre_52_Aggregate.scnx 804e2c39..., order 7a986137..., init 2000e856..., type map cc41f833..., composition
9684e945... - all G1-4's [V]. The preflight-cache 520 files, manifest 27c2117e... (cache_manifest.ps1), the road layer 41
tiles, manifest 213bae20... - G1-4's [V]; its osm and osm-water sets are byte-identical to the scorer's tile copy (225 + 225
files, 0 differing; <L5>\g1_5_tilecmp_out.txt) [V]. Offline suites on the deployed exe [A: the deploy line]: 28 switches 0
FAIL, --osm-selftest 161 PASS (169 on the deployed cache); RunnerTurnaround 779/0/1. NOT built by this lane. THIS FILE:
RunnerTurnaround in the lane's worktree (no build outputs there): 750 passed, 7 failed, 13 skipped; 13d's prereg lint PASSES
with this file among its 18 subjects; the 7 failures (runner -DryRun and 8v3 checks that need the bin\ trees) are the SAME 7,
line for line, with this file removed (750 / 7 / 13, 17 subjects) [V: <L5>\g1_5_suite.txt, g1_5_suite_control_without_file.txt].
TIER AND GATE: HEAVY / PREREG
RUN KIND: movement

VENDOR CITATION: G1-4's list, unchanged (and through it G1-3's). For THE ONE VARIABLE and the new scorer rule:
- navigate-to-location.lua :14 (C:\MAK\vrforces5.2d\data\simulationModelSets\base\scripts, read-only): "taskParameters.buffer
  Type: Real Unit: meters - Distance that the path will stay away from the non-traversable feature." [V: read]; :36-45 - a
  buffer <= 0 becomes 0.0 [V]. Our planner passes buffer 10 (RL-20260928-03), so a member born within 10 m of a water feature
  starts inside the distance its own path must keep - W1 moves such slots out of it.
- UG52 27.1.4 p531 (VR-Forces_5.2_Users_Guide.txt :17094-17101): "Rivers and water bodies may stop a unit completely, although
  bridges allow them to cross" and "the center point of the unit is used to determine what type of terrain the unit is on" [V].
- UG52 23.2 p501 "Planning a Path Using Roads" (:16161-16178): the planner "uses the VR-Forces feature path planner to find a
  route on a network of road features ... any feature defined to be a MAK_ROAD"; it "finds the point on the road network
  closest to the start" and searches along the roads [V]. This is why the scorer's new rule reads a crossing on the road and
  bridge WAYS: a member planned with pathQuery MAK_ROAD (every cut-A leg decided NEAR, G1-4 Result) drives the road network,
  and a bridge is a road way.
- The source of W1 (src at d455092, = the deployed 52f50e0): PreflightService.cs CheckSlot :487-517 (the band: a slot within
  the clearance is bad ground; a candidate must lie more than the clearance from every OSM water feature; :503 a road bridge
  exempts nothing for a slot), DescribeSlotCheck :519-539, EffectiveSlotWaterClearance :544-545, SlotClearanceClause :548-556;
  VrfC2SimService.cs :3165-3171 (the start-up line), CheckPopulateSlots :3356-3377 (svc.CheckSlot :3370), the L-SLOT line
  :3399-3400 [V: read].

OWN-RECORD CITATION:
- Rulings: G1-4's list, unchanged (RL-20260927-01..-06, RL-20260928-01..-04, RL-20260921-09, RL-20260913-03, RL-20260914-01,
  RL-20260925-01, RL-20260926-01, RL-20260920-01), and THIS REGISTRATION'S MANDATE: RL-20261004-01 ("As recommended": G1-4's W
  FAIL binds as scored; G1-5 gates H5 on every tasked member on a build whose nudged member slots clear OSM water by >= the
  planner's 10 m buffer; the scorer's bridge rule reads the fixes against the road/bridge ways, fail-first on G1-4's pre-warm)
  and RL-20261004-02 ("Keep both": a slot IN water or WITHIN the clearance is moved; no bridge exemption for slots) [V:
  docs/RULINGS.md :102-118].
- docs/PLAN_MOVEMENT_2026-09-27.md CLOSED :14-15 (G1-4 STOPPED at its W gate as scored; G1-5 gates H5 on every member after a
  >= 10 m water clearance) [V]; rows M3b (:81), G1-4 (:83); step table "W1 -> G1-5" (:99) [V].
- docs/HANDOFF_SEAT_2026-09-28.md sec 2 (:30-34: holder 56380 gone, RTI trio down, the marker and the branch (b') numbers) and
  sec 3 items 3-4 (:73-90: the cold review, RL-20261004-01, W1, RL-20261004-02, the slot predictions to make) [V].
- G1-4 in full, its Result N1 (INF3WPN1 born 1.7 m from lake 16373225, 0.0 m; readings (e) and (vi)), N2 (T02 crossed river
  8011072 consistent with bridge 218414262 at every fix; the 16.24 m is the chord), N3 and the COLD REVIEW AND RULING [V].
- RUNBOOK sec 9 "W1 DEPLOY 2026-10-04" (:2420) and sec 11o (:4187-4240) [V].
- Fable's cold review of W1 (scratch 7207877b...\fable_w1\REVIEW_W1_fable_2026-10-04.txt) and its harness output
  (harness_out.log): GO WITH FIXES; G1-5's slot predictions [V: read; re-derived independently in sec 1(y)].
- THE CONTROL: IRONSTORM_AGG_G1-2026-09-28-4 (G1-4; pre-warm 20260928T225800Z_run, STOPPED at its W gate).

## 0. Purpose, in plain words

G1-5 re-runs G1-4 - the ruled cut-A order on the AGGREGATE model set, 36 empty containers at init, the three performers
populated in place at order receipt (28ID 1 member, 1-112 IN 5, 48 IBCT 17), each container's route driven per STP vertex by
the vendor's navigate-to-location per member (Auto, RL-20260928-03), the M3b names - with ONE change: W1 as deployed. In G1-4
48_IBCT.INF3WPN1 was born 1.7 m from OSM lake 16373225 (the slot nudge stopped at the polygon's edge) and never moved, so H5
failed and T14's vertex 1 never closed (G1-4 Result N1). W1 gives a populated container's member slots a water clearance of
Vrf:PreflightSlotWaterClearanceMeters = 10 m, the planner's own obstacle buffer: a slot IN OSM water or WITHIN 10 m of it is
moved to the nearest ground MORE than 10 m from every OSM water feature (RL-20261004-01, -02). On cut A that moves the SAME
four 48 IBCT slots G1-4 moved, each to a new landing 11-16 m from the water (sec 1(y)). The scorer changes too, as ruled: the
river-crossing falsifier judges a crossing on the road and bridge ways (sec 1(z)), so G1-4's chord corner-cut 16 m from a
wooden bridge (N2) no longer fires, while a crossing with no bridge on a plausible path still does.

THE RUN'S HIGH PREDICTIONS (sec 4): the member slots exactly as re-derived (SL); EVERY tasked member moves at every vertex it
is sent to, INF3WPN1 included - gated on every tasked member in the W gate AND the scored run (H5, RL-20261004-01); no river
crossed off a bridge by the ways rule (P-FALS (e)); and G1-4's H1-H4. MEDIUM: T14 and T10 arrive in the scored run (H6) -
under W1 T14's vertex 1 is no longer held open by a member that never ends, if H5 holds. THE FALSIFIER OF THE WINDOW stays
G1-4's V (a back-end crash VOIDS it).

## 1. Decisions taken from the record

(a)-(x) - G1-4's, unchanged (and through them G1-3's (a)-(t)): the order and its five tasks, the settings (nothing passed for
the planner, for M3b or for W1), the pre-warm as the W gate, population at order receipt, M2 report + fallback, what is issued
(navigate-to-location per member, obstacleQuery MAK_OBSTACLE, buffer 10, pathQuery MAK_ROAD on every cut-A leg), the ring and
its slots, the timings, the names (M3b, sec 1(u) there), EXECUTOR REFUSED (v), the crash-void (w). Their src line numbers move
by sec 2's map. Differences:
- (b) W1 adds ONE setting, Vrf:PreflightSlotWaterClearanceMeters = 10, SHIPPED in appsettings.json :98 and
  appsettings.Demo.json :83 (and VrfSettings.cs :1184's default) - nothing is passed on the command line, which stays G1-4's
  byte for byte [V].
- (i) THE RING is G1-4's: the same anchors (53.992385,23.211255) / (54.042688,23.308235) / (54.019389,23.313902), the same
  spacing 180 m, the same radii (0 / 153.117 / 489.797 m = 180 / (2 sin(pi/N)), DeStacker.cs :480-483) - the anchors were
  identical in G1-2's two runs, G1-3's and G1-4's pre-warm [V: grep of the four logs]. Only the four slots of sec 1(y) land
  elsewhere; the uuids and names are G1-4's.
- (n) THE CONTROL is G1-4's pre-warm (20260928T225800Z_run): the same order, fixture, init, composition, settings, sequence and
  harness geometry; the build differs by W1 only (sec Registration).
- (x) THE SCORER: <L5>\g1_5_score.py = lane G1-4's g1_4_score.py + the changes of (y)-(aa), derived by the asserted edit script
  <L5>\g1_5_edit_score.py (sec 4 MEASURES).

(y) THE ONE VARIABLE - W1'S MEMBER SLOTS, RE-DERIVED OFFLINE ON THE DEPLOYED BUILD [V: <L5>\g1_5_slots.ps1 ->
g1_5_slots_out.txt]. The script loads the DEPLOYED VrfC2SimApp.dll (deb658eb...; no compile) into pwsh 7.6 / .NET 10, rebuilds
G1-4's 23 PLANNED ring slots from G1-4's own POPULATE lines (anchor, N, spacing; the exact radius as above; the bearing
360 k / N, ContainerComposition.cs :775, :788; DeStacker.EqualBearingOffset :494-500; 111320 m per degree, :719, :761, :790)
and runs PreflightService.CheckSlot with the deployed settings over the deployed preflight-cache, Offline. ITS CONTROL - the
rule before W1 (clearance 0, G1-4's build) - lands every one of the 23 within 0.07 m of G1-4's logged point and prints all 23
of G1-4's verdicts VERBATIM ("CONTROL ... PASS"); 0 tiles fetched; the cache unchanged (520 files before and after). An
INDEPENDENT reader (the scorer's Python OSM decoder, <L5>\g1_5_slot_xcheck_out.txt) puts the four new landings outside every
water polygon, 15.8 / 14.4 / 11.4 / 13.6 m from the nearest edge, and reproduces G1-4 Result N1's 2.3 / 5.5 / 7.5 / 1.7 m for
the old ones [V]. PREDICTED:

| member (48_IBCT) | planned slot | G1-4 (clearance 0) | G1-5 (W1): the L-SLOT verdict | landing (+/- 1 m) | from water (C# / Python) | moved vs G1-4 |
|---|---|---|---|---|---|---|
| INF1RIF2 (slot 4) | IN lake 197345448 | 125 m north-west, 2.1 m from it | SLOT MOVED 150 m north-west | (54.022263,23.318918) | 15.5 / 15.8 m | 41.0 m |
| INF1RIF3 (slot 5) | IN lake 197345448 | 125 m south-west, 5.4 m | SLOT MOVED 125 m south-west | (54.019001,23.320008) | 14.3 / 14.4 m | 24.5 m |
| INF1WPN1 (slot 6) | IN lake 197345448 | 25 m south, 7.4 m | SLOT MOVED 25 m south-west | (54.018026,23.320835) | 11.3 / 11.4 m (the thinnest RAW distance; a secondary watch) | 19.1 m |
| INF3WPN1 (slot 16) | IN lake 16373225 | 75 m south-east, 1.7 m | SLOT MOVED 75 m south | (54.021976,23.308668) | 13.7 / 13.6 m (THE ONE TO WATCH: 3.7 m over the buffer with the lake between it and the road network, sec 4 COMPETITORS) | 48.6 m |

Each of the four lines reads, in full: `POPULATE 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE slot <k> of 17: <member>
(Mech CO (USA, M2), C-USA-BDE-UCI/<INFn>/<suffix>) at (<lat>,<lon>), bearing <b> deg from the container point - SLOT MOVED <d>
m <compass> - the planned slot lies IN OSM water (natural=water, OSM <id>); the new point is the nearest ground clear of OSM
water and of OSM buildings within 10 m - water clearance 10 m (Vrf:PreflightSlotWaterClearanceMeters, RL-20261004-01).`
(VrfC2SimService.cs :3399-3400 + PreflightService.cs :509 + :555; the verdict text is the deployed dll's own output [V]). The
other 19 slots print "- clear." at their planned points (<L5>\g1_5_expected.txt lists all 23 points); 0 UNVERIFIED, 0 KEPT ON
BAD GROUND, 0 newly moved by the band. THE START-UP LINE, once, at the container start-up (:3165-3171; the deployed dll's
DescribeSlotCheck on the deployed settings [V]): `MEMBER SLOT CHECK (populated containers): a planned member slot IN OSM water
or within 10 m of it (water clearance 10 m (Vrf:PreflightSlotWaterClearanceMeters, RL-20261004-01); at least the planner's 10 m
obstacle buffer, RL-20260928-03) or within 10 m of an OSM building is moved to the nearest ground more than 10 m from every OSM
water feature (a road bridge exempts nothing for a slot) and clear of buildings, on 25 m rings within 300 m
(Vrf:PreflightVertexNudgeMaxMeters); none found = KEPT ON BAD GROUND and logged; unreadable ground = UNVERIFIED, kept. Authored
route vertices keep the VERTEX CHECK's own rule.` with no "NOT RUN THIS RUN" suffix (PreflightRouteShift is true).
AGAINST FABLE'S W1 HARNESS: the same four slots, the same distances and compass words, 0 newly moved, 0 UNVERIFIED, 0 KEPT, 19
at their points - AGREE. Its water distances (15.5 / 13.6 / 10.9 / 13.6 m) differ from these by 0.0-0.7 m: it rebuilt the
planned slots from the logged 0.1-degree bearings and the logged F0 radius (its own "rebuild-vs-log" 0.7-0.9 m), where this
lane's rebuild is 0.01-0.07 m off the log. The registered values are this lane's; +/- 1 m covers both.
WHAT IS GATED (Fable's review of this registration, finding 6): SL2 gates each landing within 1 m of the predicted point and
the verdict text; the WATER DISTANCES in the table (C# and Python) are RECORDED, not gated - they follow from the landing.

(z) THE SCORER'S BRIDGE RULE (RL-20261004-01) - designed here, FAIL-FIRST ON REAL DATA [V]. A crossing is found as before (the
chord between consecutive fixes of a member cut against every OSM river line, g1_4_score.py WaterIndex.crossings), and both
rules accept it when the chord's crossing point lies within BRIDGE_TOL_M = 15 m of a drivable road bridge (G1-4's rule). THE
WAYS RULE (the default, --bridge-rule ways) ALSO accepts it when (i) BOTH BANK FIXES (the fixes either side of the chord) lie ON
THE ROAD NETWORK - within ROAD_TOL_M = 15 m of a drivable highway way of the same osm tiles (highway present, not in
NoDriveHighway, not area=yes; bridges included) - AND (ii) a drivable road bridge OVER THAT SAME RIVER (a segment of it cuts
the river id's line, or a vertex of it lies within 5 m of the line) lies ON A PLAUSIBLE PATH between them: the shortest path
from bank fix A to a point P of the bridge way and on to bank fix B, min |AP| + |PB|, is at most DETOUR_MAX = 1.5 x the chord
|AB|. Otherwise the crossing is OFF A BRIDGE and, with G1-4's clause that the member then drove on 50 m or more, P-FALS (e) /
P11 / P14 fail. Under the ways rule every CROSSING line prints both measures. (The alternative the ruling named - interpolating along the
road - is not used: it needs the planned path, which is not printed, G1-4 Result N2 (b).) THE FOUR REQUIRED OUTPUTS:
- (a) THE CONTROL, G1-4's pre-warm with the OLD rule (`g1_5_score.py <run> --wgate --bridge-rule chord`): FIRES - "crossed
  river 8011072 at 54.007560,23.236494 16 m from any drivable road bridge and drove on 2990 m [chord rule: chord crossing point
  16.3 m ...]"; W GATE: FAIL (H5, P-FALS(e), SL0, SL2) [<L5>\g1_5_bridge_a_chord_g14prewarm.txt]. (SL0 / SL2 fail there as they
  must: G1-4's build printed no start-up line and no clearance clause, and its landings are the old ones.)
- (b) THE NEW RULE, the same run: DOES NOT FIRE - "ON A BRIDGE by the ways rule: banks 0.3 m (OSM 218414261) / 0.3 m (OSM
  365845592) from a drivable road; bridge 218414262 over river 8011072: path via it 147.7 m = 1.02 x the 144.9 m chord, the
  chord's nearest approach 13.6 m"; "P-FALS (e) ... (bridge rule: ways): PASS"; W GATE: FAIL (H5, SL0, SL2) - H5 still fails on
  INF3WPN1, as it must on G1-4's build [<L5>\g1_5_bridge_b_ways_g14prewarm.txt].
- (c) DIRTY CONTROLS THE NEW RULE MUST STILL CATCH, on the real geometry of river 8011072 (<L5>\g1_5_bridge_controls.py ->
  g1_5_bridge_c_controls.txt; the scorer's own crossing / drive-on / judge code): (c1) a synthetic member crossing square-on
  at (54.023496,23.225134), 1,928 m from the nearest drivable road bridge, banks 60 m out, driving on 300 m - FIRES under both
  rules; (c4) the same at the river point closest to a bridge beyond 200 m, (54.009084,23.235016), 206 m (path via the bridge
  3.56 x the chord) - FIRES under both; (c2) BOTH BANKS ON THE ROAD NETWORK (4.9 m from OSM 1006531274, 1.2 m from track
  178824546) at (54.004053,23.246005), 713 m from a bridge - FIRES under the ways rule (5.64 x): the bridge clause, not the bank
  clause, does the work; (c3) G1-4's REAL fixes with bridge 218414262 removed from the index - FIRES under both ("ALL AS
  REQUIRED").
- (d) A CROSSING-DETECTOR CONTROL (it exercises WaterIndex.crossings, not judge(): there is no crossing to judge) - G1-2's T02
  STOP AT THAT RIVER (scored run 20260928T142731Z_run; `g1_5_score.py <run> --member-names c1c --bridge-rule ways`):
  28ID__FRIENDLY_INFANTRY_DI.HQ1 "river crossings 0 (off a road bridge and driving on 0)", "P-FALS (e) ... (bridge rule:
  ways): PASS" - no crossing, no fire [V: <L5>\g1_5_bridge_d_ways_g12scored.txt]. Its last fix 2.5 m from the straight-line
  river point is G1-2 Result :761's figure [A]; the scorer's O2/P14 line in the same output prints the same 2.5 m.
The bank fix past the river in (b) is 0.3 m from unclassified OSM 365845592; G1-4 Result N2 (c) gave its distance to the named
track 178824546 (9.4 m), not to the nearest drivable way - both are on the network (<L5>\g1_5_waytags_out.txt) [V].
THE ACCEPTANCE REGION (Fable's review, finding 3). min |AP| + |PB| <= 1.5 |AB| holds for every P inside an ellipse with foci A
and B and semi-minor axis sqrt(1.5^2 - 1) / 2 = 0.56 x the chord: on G1-4's 145 m chord a bridge over the river up to about
81 m to the side of the chord is ACCEPTED. So a FORD up to about 80 m from such a bridge, with both bank fixes on roads, reads
ON A BRIDGE - the rule's BLIND ZONE, which scales with the fix spacing (about 11 m on a 20 m chord). WHY 1.5: a member that
leaves the road at a right angle to the chord's direction, crosses on the bridge and turns back (the G1-4 corner: road ->
bridge -> track) drives at most sqrt 2 = 1.41 x the chord when the bridge sits on the chord's perpendicular bisector at half
its length; 1.5 admits that bend with margin and nothing much wider. TWO CONTROLS ON THE REAL 145 m CHORD (28ID.HQ1's fixes
t=129.4 (54.006989,23.235646) -> t=131.4 (54.007971,23.237104), a synthetic river across it, a 57 m bridge displaced ALONG the
river) [V: --selftest, <L5>\g1_5_score_selftest.txt]: displaced 60 m - "ON ... path via it 188.2 m = 1.30 x the 144.9 m chord"
- ACCEPTED, recorded as the blind zone; displaced 100 m - "OFF ... 247.0 m = 1.70 x" - FIRES. RESIDUAL FALSE FIRE: bridges_over
is per OSM way id, so a river split across several way ids with the bridge over a different id than the one the chord cuts
finds no bridge "over that river" and FIRES (the chord acceptance still applies within 15 m) - a scorer false positive, read
by hand if it occurs.

(aa) H5 ON EVERY TASKED MEMBER (RL-20261004-01). In the W gate AND in the scored run H5 judges EVERY tasked member of T10, T14
and T02 (5 + 17 + 1 = 23) at every vertex it is sent to that was issued 30 trace-s or more before the last fix. W-MOVE keeps
G1-4's clause - every dispatched T10 member displaced > 50 m, T14 / T02 members RECORDED - and THAT CLAUSE DOES NOT NARROW H5:
the two limbs measure different things (> 50 m toward a vertex; > 1.0 m at all), and a T14 or T02 member that does not move
fails H5 in the pre-warm and stops E. G1-4's N3 cannot recur: this paragraph, sec 3 W, H5's row and MEASURES say the same.
WHICH VERTICES (Fable's review, finding 4; members_move, G1-4's code unchanged): in the W GATE H5 judges vertex 1 whenever it
was issued and a vertex k > 1 only once vertex k has CLOSED (a later vertex still open at the window's end is not judged there);
in the SCORED run it judges every issued vertex. Both modes skip a vertex issued < 30 trace-s before the last fix.

## Conditions

CONSOLE LEVEL: 4

PRE-ORDER GATE: none (G1-4's); the order is pushed right after the runner's stage-7 oracle gate.

DurationScale: 0.25 (--duration-scale 0.25)

ARMED ENDS VS STALL WINDOW: G1-4's, unchanged - the watchdog on the SIMULATION clock, window 360 SIM s, 50 m, on each
container's one position; T01 / T13 300 (no destination, never watched); T02 / T14 300 and T10 450 (destination tasks: an
unarrived one goes OVERDUE and stays watched). An EXECUTOR REFUSED vertex ends its task at once and clears its stall state.

DEVIATION FROM RECORD: the template's "PRE-ORDER GATE: --pre-order-gate nav-area" ("gate PushOrder on the first "New Primary nav area" row; warm the area first") is not used - the aggregate model set loads no nav data (G1's to G1-4's, unchanged).

DEVIATION FROM RECORD: stall detection is switched on with --env Vrf__StallDetection=true, not by loading the demo profile the owner named ("ON in the demo profile (Recommended)", RL-20260925-01 Q3) (G1-3's and G1-4's, unchanged).

DEVIATION FROM RECORD: the successor-gate floor is 600 s - appsettings.json's shipped value - not the 7200 s the wrapper exports ("export Vrf__TaskPredecessorTimeoutSeconds=7200", scripts/RunScenario.sh:357) (G1-3's and G1-4's, unchanged).

DEVIATION FROM RECORD: the window closes EARLY under -StopWhenComplete, where E1 registered "--no-stop-when-complete --run-secs 2700"; the 2700 s cap is kept (G1-3's and G1-4's, unchanged).

DEVIATION FROM RECORD: the pre-warm is a whole runner launch that also pushes the order and runs 120 s, where DEMO_RUNBOOK sec 0.4 says "let it reach the initialization, then stop it"; there is no init-only runner mode (G1-3's and G1-4's, unchanged; it is the W gate).

DEVIATION FROM RECORD: the design's G1 outline ("Order: cut A reduced to T13 -> T14 on T14's ORIGINAL line") is replaced by the ruled cut-A order at the seat's direction (G1-3's and G1-4's, unchanged).

EFFECT OF -StopWhenComplete ON THE WINDOW: G1-4's, unchanged.

## 2. What the code emits - log-line shapes (src at d455092 = the deployed build's 52f50e0 source)

G1-4's sec 2 applies shape for shape (the M3b lines, the vendor's relayed lines, the runner's crash-void lines). ITS LINE
NUMBERS MOVE by W1's hunks [V: `git diff -U0 b0bad53 d455092 -- src/VrfC2SimApp/VrfC2SimService.cs`]: VrfC2SimService.cs -
unchanged below :3163; +9 for G1-4's :3163-3339; the slot check G1-4's :3340-3376 rewritten (now :3349-3377); -5 for G1-4's
:3377-7454; -4 above :7454. AggregateMovePlanner.cs, VertexChain.cs, ContainerComposition.cs, VrfNames.cs and scripts/ are
unchanged [V: not in the diff stat].
CHANGED / NEW by W1:
- L-SLOT-START (INFO, :3165-3171): the MEMBER SLOT CHECK start-up line of sec 1(y), once, after the AGGREGATE CONTAINERS ON
  line and before the AGGREGATE MOVE PLANNER line. NEW (G1-4: absent).
- L-SLOT (INFO, :3399-3400): one line per member slot, `POPULATE <container> slot <k> of <N>: <member> (<template>, <path>) at
  (<lat>,<lon>), bearing <b> deg from the container point - <verdict>.`; the verdict is "clear", "SLOT MOVED ... - water
  clearance 10 m (Vrf:PreflightSlotWaterClearanceMeters, RL-20261004-01)", "KEPT ON BAD GROUND ...", "UNVERIFIED ..." or a
  bridge text (PreflightService.cs :508-516). CHANGED: a moved slot's verdict ends with the clearance clause; the 4 landings of
  sec 1(y).
- The member creates, L-ID-CREATED, L-MEMBER, the consoles and every PLANNED MOVE line: G1-4's shapes, the four moved members
  created at their new points (the altitude by the TERRAIN QUERY under the new point, as in G1-4 L754).

## 3. Sequence and exact command lines - the GO-LIVE (nothing below has been run)

G1-4's sequence and command lines, with this lane's scripts (<L5>, NOT RUN against anything live): g1_5_golive_checks.ps1
(phases prertiexec | preholder | prelaunch; the W1 hashes and settings; the rtiexec and holder pids as parameters),
g1_5_c3_push.sh, g1_5_runner.sh (prewarm-dryrun | prewarm | scored-dryrun | scored - G1-4's command line byte for byte, the log
names aside), g1_5_claim_holder.ps1 (5281 -> 5285; tested on a scratch COPY of the plan, not on the real file [V]),
g1_5_rti_federates.py <rtiexec pid>, g1_5_score.py; lane E2's cache_manifest.ps1 and lane G1's simcache_listing.ps1 stay in
G1-3's scratch u3. Python = /c/Users/PauloBarthelmess/AppData/Local/Programs/Python/Python312/python.exe. THE SEAT RUNS R, D',
S, A, C3, W, E AND F under RL-20260928-01.

THE HOLDER - BRANCH (b'), THE ONLY BRANCH [V at 2026-10-04T19:39Z, g1_5_golive_prertiexec_registration.txt: no rtiexec,
rtiForwarder, rtiAssistant, RtiProbe, vrfSim, VrfC2SimApp or MSBuild process; the machine booted 2026-09-30T21:47:46Z]. Holder
56380 (appNo 5255) ended with the reboot of 2026-09-28 23:16:45Z [A: HANDOFF_SEAT :30-31]. StartFederationHolder52.ps1 REFUSES
with exit 2 when no rtiexec runs ("rtiexec is not running - start it first: pwsh -NoProfile -File scripts\StartRtiExec52.ps1",
:24-25, :162-165, :183) [V: read], and the holder step comes BEFORE the runner, so the runner's Stage 2r (StartRtiExec52,
RUNBOOK :902-904) cannot be the one that starts it for the holder. Hence step R:
R.  rtiexec, ENSURE-UP, once (no appNumber): golive_checks -Phase prertiexec -MarkerWant 5281 (0 FAILED but the REST limb
    until step S brings the server up - sec 8 item 3); `"C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File
    scripts\StartRtiExec52.ps1 -DryRun`, then without -DryRun - EXPECT "RTIEXEC READY ... tcp=...:4001" and exit 0 (3 = NOT
    LISTENING = STOP); record the rtiexec pid and its log runs\launch52\rtiexec_*-<pid>.log (StartRtiExec52.ps1 :1-48: it
    NEVER kills or restarts one, the rtiexec starts its own rtiForwarder). From here the rtiexec, its forwarder and any
    assistant are NEVER touched; the runner's Stage 2r then finds it up and starts nothing.
D'. THE HOLDER: golive_checks -Phase preholder -RtiexecPid <pid> -MarkerWant 5281 (0 FAILED but REST); `pwsh -NoProfile -File
    <L5>\g1_5_claim_holder.ps1 -Path <main checkout>\docs\OPUS_EXECUTION_PLAN.md` (marker 5281 -> 5285, the entry above it);
    `scripts/StartFederationHolder52.ps1 -AppNumbers 5281,5282,5283,5284 -SettleSecs 28800` (-WhatIf first) - EXPECT "HOLDER
    JOINED: pid ... appNo ...", exit 0; exit 1 (none joined) or 2 (a precondition) = STOP, no blind relaunch (RUNBOOK 9c).
    THEN THE FEDERATE LIST, read-only: `<python> <L5>/g1_5_rti_federates.py <rtiexec pid>` - EXPECT exactly one joined federate,
    the holder's RtiProbe; any other = STOP before W.
S.  THE PRIVATE C2SIM SERVER, BEFORE A (Fable's review, finding 5): the container c2sim-server-vrf EXISTS, "Exited (255) 5 days
    ago", image c2sim-server:4.8.4.9-rev1 [V: `docker ps -a` at 2026-10-04T20:5xZ], so RUNBOOK :1716's `docker run ...` does NOT
    apply (it would fail on the existing name): `docker start c2sim-server-vrf`, wait about 30 s (RUNBOOK :1717), then EXPECT
    REST http://127.0.0.1:18080/C2SIMServer -> HTTP 200 (golive_checks' REST limb). No 200 = STOP before A. The operator's
    8080 / 61613 server is never touched (RL-20260928-01).
A.  golive_checks -Phase prelaunch -RtiexecPid <pid> -HolderPid <holder pid> -MarkerWant 5285 - EXPECT 0 checks FAILED (the
    REST limb included: the private server is up since step S). A2 - G1-4's, on the W1 build: the deployed exe's offline suites [A:
    the W1 deploy line] or re-run by the seat from PowerShell with the 5.2 PATH prefix; --osm-selftest 161 PASS (169 with the
    deployed preflight-cache). A FAIL = STOP before W.
C.  C3 - `sh <L5>/g1_5_c3_push.sh` (G1-4's two lines, the private server 18080 / 61614, RL-20260928-01). EXPECT both exit 0,
    "QUERYINIT : 40 Units", one `ORDER (69670 chars)` echo carrying 5 tasks. Any failure = STOP.
W.  THE PRE-WARM (unscored) = THE W GATE: golive_checks -Phase prelaunch ... (0 FAILED); simcache_listing.ps1 -Out
    <L5>/simcache_before_prewarm.txt; `sh <L5>/g1_5_runner.sh prewarm-dryrun` (EXPECT the holder PERSISTENT, block 5285-5295,
    "marker would advance to: 5296", G1-4's D2 lines, 120 s CAP); then `sh <L5>/g1_5_runner.sh prewarm` ONCE. GATE for E - ANY
    miss = STOP before E: StopVrf52 exit 0 or 6 (8 = VOID); the runner exit 0 and NO `BACK END CRASHED` line; the post-W
    inventory shows only the rtiexec and its forwarder, the persistent holder and the pre-warm's Stage 2h holder; the cache
    manifest unchanged; AND
        <python> <L5>/g1_5_score.py <prewarm run dir> --wgate > <L5>/W_gate_<run>.txt
    prints "W GATE: PASS" (exit 0): G1-4's limbs (population + identity branch A, M1-M17, H1-H3, W-MOVE with its T10-only
    member clause, W-DONE, V-CRASH) AND THE NEW ONES - SL0-SL4 (the member slots, sec 1(y)); H5 ON EVERY TASKED MEMBER of T10,
    T14 and T02 (sec 1(aa) - W-MOVE's T10-only clause does not narrow it); P-FALS (e) under the WAYS rule (sec 1(z)). The WATCH
    line (INF3WPN1, INF1WPN1) is RECORDED. Then simcache_listing.ps1 -Out <L5>/simcache_after_prewarm.txt.
E.  THE RUN: golive_checks -Phase prelaunch ... -MarkerWant 5296; `sh <L5>/g1_5_runner.sh scored-dryrun` (EXPECT block
    5296-5306, marker -> 5307, 2700 s CAP with -StopWhenComplete); then `sh <L5>/g1_5_runner.sh scored` ONCE. The command,
    verbatim G1-4's (sec 3 E there), the log name aside:
        scripts/RunScenario.sh --scenario IronStorm_Centre_52_Aggregate --init data/IRONSTORM_CUTA_Initialization.xml
          --order data/IRONSTORM_CUTA_Order.xml --client-id "Not Set" --model-set auto --duration-scale 0.25
          --object-console 4 --member-console 4 --stop-when-complete --run-secs 2700 --env Vrf__StallDetection=true
          --env Vrf__StallClock=sim --env Vrf__TaskPredecessorTimeoutSeconds=600 --sample-threads --no-gui
          --log runs/launch52/RunScenario-ironstorm-agg-g1-5-<stamp>.log
    GATE on teardown: StopVrf52 exit 0 or 6 (P21); 8 = VOID (V); 3, 5 or 7 = STOP.
F.  Post-run, in the FOREGROUND: the inventory; golive_checks -Phase prelaunch ... -MarkerWant 5307; simcache listing;
        <python> <L5>/g1_5_score.py <scored run dir> > <L5>/score_<run>.txt
W and E start only with at least 60 min of the holder's 28800 s hold left (golive_checks -MinHoldLeftMin 60). QUIET PERIOD:
G1-4's (no Stop-Process / taskkill of any kind, no build, suite, subagent or second runner from W to F's inventory). Never
touched: the rtiexec of step R, its rtiForwarder, any rtiAssistant, any RtiProbe holder.

## 4. Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

MEASURES: G1-4's, per TASK UUID from the run's vrfc2simapp.log in line order, positions from watchvrf-trace.csv POS rows.
THE SCORER <L5>\g1_5_score.py (g1_4_score.py + these changes, all [V]): --bridge-rule ways (default) | chord (G1-4's, the
control); WaterIndex reads the drivable highway ways of the same osm tiles and judges each crossing (sec 1(z)); SL0-SL4 read the
L-SLOT-START and L-SLOT lines against the embedded prediction table of sec 1(y) (SLOT_TOL_M 1.0), gated in the W gate and
printed in the full score; H5 is labelled and gated on EVERY tasked member in both modes (it already was in G1-4's wgate;
members_move is G1-4's: > 1.0 m per member per vertex, a vertex issued < 30 trace-s before the last fix NOT EVALUATED; in the
W gate a vertex k > 1 is judged only once it has CLOSED, in the scored run whenever issued - sec 1(aa)); the WATCH line and the
MOVED MEMBER records (both RECORDED, never gating; ITEM 5 of Fable's review): per member whose L-SLOT line says SLOT MOVED, its
FIRST-LEG BEARING (birth fix -> the first fix >= 50 m from it), the MINIMUM DISTANCE OF ITS FIRST 300 m OF FIXES TO OSM WATER
(negative = inside a polygon) and the relayed vendor "Movement constrained by features" COUNT (the member's own console, family
CON-CONSTRAINED). Reference values [V: <L5>\g1_5_bridge_b_ways_g14prewarm.txt, g1_5_bridge_d_ways_g12scored.txt,
g1_5_records_g12prewarm.txt]: G1-4 pre-warm - INF3WPN1 bearing NONE (never 50 m from its birth fix), 1.7 m, constrained 1;
INF1RIF2 294 deg, 2.3 m, 0; INF1RIF3 166 deg, 5.5 m, 0; INF1WPN1 189 deg, 7.5 m, 0. G1-2 scored (Literal) - INF3WPN1 136 deg,
1.7 m, 0; INF1RIF2 NONE, -1.6 m (inside), constrained 1 (G1-2's far-shore stop); INF1RIF3 277 deg, 5.5 m; INF1WPN1 288 deg, 7.5 m.
Its --selftest: 71 checks PASS, 0 FAIL [V: <L5>\g1_5_score_selftest.txt] - G1-4's 53 unchanged (the
uuid oracle, the chain checker, the CLEAN control, S7, the must-not controls and every dirty control; the clean synthetic run
now carries the W1 slot lines and passes the new limbs too) plus 18 NEW: CLEAN - every SL limb passes; both bridge rules pass
the synthetic crossing ON a bridge; FIVE SLOT DIRTY CONTROLS each caught and the W gate FAILS - slot-old (G1-4's lines: SL0,
SL2), slot-far (INF3WPN1 5.6 m off: SL2), slot-extra (a fifth SLOT MOVED: SL1), slot-unverified (SL4), slot-nostartup (SL0);
SEVEN BRIDGE CASES on a synthetic G1-4-shaped corner (the chord cuts the river 16.1 m from the bridge): the oracle, the chord
rule FIRES, the ways rule does NOT, and the ways rule FIRES with no bridge over that river, with the bridge 250 m upstream
(5.11 x), with the banks off the network, and does not fire on a chord through the bridge; TWO CONTROLS ON G1-4's REAL 145 m
CHORD (Fable finding 3): the bridge displaced 60 m along the river is ACCEPTED (1.30 x - the blind zone, recorded), 100 m
FIRES (1.70 x); TWO RECORD CHECKS (ITEM 5): the clean run prints 4 MOVED MEMBER records and its W gate still PASSES, and a
relayed "Movement constrained by features" for INF3WPN1 is counted 1 in its record while the W gate still PASSES (recorded,
never gating). THE REAL-DATA FAIL-FIRST: sec 1(z)
(a)-(d). THE G1-4 REPRODUCTION: the copied scorer, before any edit, reproduced G1-4's W gate file line for line (only the run
path in the header differs) [V: <L5>\_baseline_wgate_g14.txt].

THE NEW AND CHANGED ROWS (G1-5's one variable and the ruled scorer). G1-4's H1-H4, H6, R-REF and V, and G1-3's P0-P23, P-FALS
and O1-O12 apply as registered there, their line numbers per sec 2, with the changes named after this table.

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| SL | THE MEMBER SLOTS AS RE-DERIVED (sec 1(y)): SL0 the MEMBER SLOT CHECK start-up line once, verbatim; SL1 23 L-SLOT lines with EXACTLY 4 "SLOT MOVED" - 48_IBCT.INF1RIF2, INF1RIF3, INF1WPN1, INF3WPN1; SL2 each as the table (150 m north-west / 125 m south-west / 25 m south-west / 75 m south, the water id, the landing within 1 m) and ending "- water clearance 10 m (Vrf:PreflightSlotWaterClearanceMeters, RL-20261004-01)"; SL3 the other 19 "clear" at their planned points (within 1 m); SL4 0 UNVERIFIED, 0 KEPT ON BAD GROUND. In the pre-warm and the scored run. | HIGH | Any limb = STOP: the deployed W1 code did not do on the live cache what it does offline on the same cache - recorded with the lines, NOT a movement verdict. | |
| H5 | EVERY TASKED MEMBER MOVES at every vertex it is sent to (displacement > 1.0 m on the trace), ALL 23 - T10's 5, T14's 17, T02's 1 - in the W GATE AND the SCORED run (RL-20261004-01); W-MOVE's T10-only > 50 m clause does not narrow it. THE ROW G1-5 EXISTS TO TEST, AND THE ONE TO WATCH: 48_IBCT.INF3WPN1, born 13.7 m from lake 16373225 instead of 1.7 m, moves at T14 vertex 1 - its EFFECTIVE margin is the thinnest on cut A: 3.7 m over the planner's 10 m buffer, measured against OSM, with the lake still BETWEEN it and the road network (the segment from its new landing to the nearest drivable road, OSM 595161645, 306 m, cuts lake 16373225 - sec 4 COMPETITORS). A SECONDARY WATCH: 48_IBCT.INF1WPN1, 11.3 m from lake 197345448 (the thinnest raw distance, 1.3 m over the buffer, but no lake between it and its nearest road; its G1-4 slot, 7.5 m, moved 773.8 m). | HIGH | A member that does not move at a vertex issued 30 trace-s or more before the last fix = STOP (a later vertex is NOT EVALUATED); in the pre-warm it stops E. A member whose plan FAILED (M10 / M12) also reads 0 m: that miss is M10 / M12's. INF3WPN1 at 0 m again from 13.7 m = the "born at the shoreline" reading of N1 does not account for G1-4's stop - recorded with its console (G1-4 N1 (c)'s line-for-line comparison) and the sim's lines; no cause is claimed. | |
| P-FALS(e) | NO RIVER CROSSED OFF A BRIDGE BY THE WAYS RULE and no fix deeper than 25 m inside OSM water (G1-3's P-FALS (e) / P11 / P14 water limb with sec 1(z)'s rule): T02's 28ID.HQ1 is expected to cross river 8011072 again on its MAK_ROAD path (its slot and leg are G1-4's) and to read ON A BRIDGE (218414262) by the ways rule. | HIGH | A crossing OFF A BRIDGE by the ways rule after which the member drove on 50 m or more, or a fix > 25 m inside water = STOP. A crossing that only the chord rule calls off a bridge is RECORDED (both measures print). | |
| WATCH | INF3WPN1 and INF1WPN1: max displacement and the first instant within 50 m of each T14 vertex (scorer WATCH line). | RECORDED | - | |
| MM | THE MOVED-MEMBER RECORDS (ITEM 5 of Fable's review; no variable change): per SLOT-MOVED member (the four) the first-leg bearing, the minimum distance of its first 300 m of fixes to OSM water, and the relayed vendor "Movement constrained by features" count (scorer MOVED MEMBER lines; reference values in MEASURES). | RECORDED, never gating | - | |

CHANGES TO G1-4's ROWS: H5 - replaced by the row above (it is G1-4's H5 made explicit about the gate; nothing in its measure
changes). H6 - unchanged, MEDIUM, scored run only; under W1 T14's vertex 1 is expected to CLOSE in the pre-warm too, because the
member that held it open in G1-4 is predicted to move (H5) - so T14's W-DONE is expected "closed" or "NOT REACHED", not
"reached but NOT closed" (S9 = FAIL, as G1-4). H1-H4, R-REF, V - unchanged (W1 touches no name, uuid, route reference, planner
parameter or crash path). P0 (ii) - the build is 52f50e0's (sec Registration); (i) holder branch (b') with step R (sec 3).
P3 - the 23 members under G1-4's uuids and M3b names, four of them created at the sec 1(y) landings. P-FALS - per the row above.
Everything else as registered in G1-4 and G1-3.

WHAT W1 COULD CHANGE BEYOND THE SLOTS (expectations, not new rows): (1) THE FOUR MOVED MEMBERS START 19-49 m from G1-4's points,
so their planned paths to T14 vertex 1 differ from G1-4's; their H5 / H3 / R-REF measures are the same measures. (2) T14's
container centroid moves by the four members' shifts / 17 (a few metres) - inside every 50 m tolerance. (3) T10's and T02's
slots are unchanged (0 moved), so their legs, roads and paths are G1-4's; their arrivals (H6) are expected as there (G1-4: T10
1.7 m, T02 0.5 m) and stay MEDIUM. (4) T14 can now complete vertex 1 and be issued vertex 2 - a leg G1-4 never reached on M3b
(G1-3's vertex 2 "Cound not create route", G1-4 P7c carried: expected ZERO). (5) Nothing in W1 touches the init's 36 creates:
O8, the FALLBACK altitudes, is expected again.

THE CARRIED UNEXPLAINED ITEMS, each with its expectation here: G1-4's list (the crash - expected not to recur, V MEDIUM;
vertex 2's "Cound not create route" - expected 0; "Pathr" - gone; O8; first-to-return order) unchanged, plus G1-4's own:
- N1 - WHY INF3WPN1 STOPPED (reading (e) a terrain hold at its birth point [A], weakened by G1-2 (sec 7); competitors (vi) the
  planner's 10 m buffer, (v) a planned first leg into the lake, and the composite of sec 4 COMPETITORS): G1-5 does not answer
  it (sec 7). Expected: it moves (H5).
- N1's CONTRAST - why 1.7 m from one lake stopped a unit while 2.3-7.5 m from another did not: G1-5 removes the contrast (every
  moved slot now 11-16 m out), so it cannot answer it either.
- N2 - the bridge crossing: the ways rule is the scorer's answer to the chord; the crossing itself stays inferred from fixes
  and the model (the planned path is not printed), as N2 (c) says.

COMPETITORS FOR THE ROW G1-5 EXISTS TO TEST (Fable reviewed them cold, fable_g15). A PASS of H5 with INF3WPN1 moving is
CONSISTENT with every reading below and so is NOT a test between them: (e) the sim's water (the aggregate actuator's IMPASSABLE
MAK_WATERWAY, speed-factor 0 at the unit's centre point, UG52 27.1.4 p531) covered a point 1.7 m outside the OSM polygon and does
not cover one 13.7 m out - WEAKENED by G1-2 (sec 7); (vi) the planner's buffer 10 m (navigate-to-location.lua :14) constrained a
unit born inside it and does not constrain one born outside it; (v) the planned first leg led into the lake. THE COMPOSITE
READING (Fable's review, finding 1): a start INSIDE the 10 m planner buffer (featureconfig.txt :405 - MAK_OBSTACLE includes
MAK_WATERWAY [A: Fable]) WITH THE LAKE BETWEEN THE START AND THE ROAD NETWORK. MEASUREMENT [V: fable_g15\nearest_road.py, re-run
by this lane, <L5>\g1_5_nearest_road_rerun.txt]: the straight segment from INF3WPN1's G1-4 birth point to the nearest drivable
road (OSM 595161645, 292.7 m) CUTS lake 16373225; the three siblings' segments (2.3 / 5.5 / 7.5 m from lake 197345448, all of
which moved) are clear of water; INF3RIF3 (113.6 m from the lake, moved) also cuts it. READING [A]: only INF3WPN1 had BOTH
conditions - inside the buffer AND the lake between it and the roads - so the composite fits every G1-4 member and both G1-2
runs (no planner there). W1 removes only the first condition for INF3WPN1: its new landing is 13.7 m out, 3.7 m over the buffer
measured against OSM, and its segment to the same road (306.0 m) STILL cuts the lake - the thinnest effective margin, hence THE
one to watch. A FAIL - INF3WPN1 at 0 m from 13.7 m - would weaken (vi) and (v) as stated, and is predicted by the composite only
if the sim's own water sits more than 3.7 m outside the OSM shoreline there (unmeasured), or by a cause not tied to water.
H5 stays HIGH. THE OBSERVATION THAT WOULD DISCRIMINATE (not part of G1-5; sec 7).

NAMED STOP-WITH-REPORT BRANCHES: G1-4's S1-S11, and S12 THE SLOTS DIFFER (an SL limb missed): recorded with the L-SLOT lines and
the start-up line, the deployed dll's offline output (g1_5_slots_out.txt) beside them; STOP. S13 A MOVED MEMBER STILL STOPS (H5
missed by INF3WPN1, INF1WPN1 or another of the four): recorded with its console line for line against a sibling (G1-4 N1 (c)'s
method), its fixes and the sim's relayed lines; STOP.
STOP RULES: G1-4's - a missed HIGH row is a STOP (record it, no patch, no re-run under this registration); P0 / P20 VOID; P-FALS
or P11 / P14's HIGH limb = STOP and ask; the executor never intervenes in a window; a crash (V) VOIDS the window. A VOID or
STOPPED run is re-registered as IRONSTORM_AGG_G1-<date>-6 with new numbers. Buffer 10 is NOT changed under this registration
or its successor without a ruling: it is in RL-20260928-03's operative text.

ONE VARIABLE: W1 as deployed (Vrf:PreflightSlotWaterClearanceMeters 10, the band, no bridge exemption for slots -
RL-20261004-01, -02), on G1-4's order, fixture, init, composition, settings, command line, sequence and harness geometry; the
CONTROL is G1-4's pre-warm (20260928T225800Z_run). The scorer's bridge rule changes how a crossing is JUDGED, not what the run
does; the differences not intended are sec 4a.

## 4a. Differences named, not intended

- THE FEDERATION IS NEW: a fresh rtiexec (step R) and a fresh federation CREATED by the new holder, where G1-4 joined a
  federation held since G1-3 on a long-lived rtiexec (47980, up since 2026-09-26). The STP-825 create refusal (RUNBOOK sec 9c:
  about 42% of creates on the long-lived instance) is unmeasured on a fresh one; the holder's four numbers absorb up to three
  refusals.
- THE MACHINE HAS BOOTED FOUR TIMES since G1-4 (Fable's review, finding 2) [V: System event log 1074 / 6006 / 6005, read
  2026-10-04]: restarts initiated 2026-09-28 23:16:25Z and 2026-09-30 00:22:48Z by the user (StartMenuExperienceHost), and
  2026-09-30 01:44:07Z and 21:45:05Z by Windows Update (MoUsoCoreWorker.exe); LastBootUpTime 2026-09-30T21:47:46Z. KB5121794 was
  installed 2026-09-30 (Get-HotFix) [V]. The deployed trees, the cache and the fixture hash as registered [V]. THE SIM CACHE
  BASELINE is 21,807 files (G1-4's pre-warm added 2 - the one URI entry, G1-4 Result RECORDED [A]); it is listed before and
  after W as in G1-4.
- THE SCORED RUN HAS NO G1-4 CONTROL: G1-4 stopped at its W gate. Its scored rows' control remains G1-2's scored run
  (20260928T142731Z, on 699552c, the Literal path); every scored comparison names that difference.
- THE SCORER: the bridge rule (sec 1(z)) and the SL limbs are new; every other limb is G1-4's code, reproduced on G1-4's
  pre-warm before the edit.

## 5. Application numbers

The Appendix B marker reads `*** NEXT FREE: 5281 ***` (docs/OPUS_EXECUTION_PLAN.md :3628 in the main checkout's working tree,
read 2026-10-04T19:38Z [V]). NOTHING is claimed by this registration. The runner's block layout M..M+10 (M back end, M+1 front
end - BURNED under --no-gui, M+2 WatchVrf pre-check, M+3 WatchVrf trace, M+4 VrfC2SimApp, M+5 RtiProbe 2c, M+6 CreateOne -
BURNED unless the oracle gate fails, M+7..M+10 Stage 2h holder attempts; marker -> M+11) is G1-4's: scripts/ is unchanged since
b0bad53 [V]. Step R's rtiexec takes NO appNumber (StartRtiExec52.ps1 has none [V: read]). From the marker M read at the go-live
(5281 unless another run moved it; every number shifts with it and the difference is recorded), BRANCH (b'):
- THE NEW HOLDER 5281-5284, HAND-CLAIMED by g1_5_claim_holder.ps1 -From 5281 BEFORE it joins (marker -> 5285); numbers its
  StartFederationHolder52 run does not reach are BURNED.
- PRE-WARM 5285-5295 (marker -> 5296).
- SCORED 5296-5306 (marker -> 5307).
A launch that aborts burns its whole block. Never reuse a number. The claims are carried back to this branch with the Result.

## 6. Harvest (after the runs, read-only) and where results go

G1-4's sec 6, with g1_5_score.py (<run> --wgate for the pre-warm, then in full; --bridge-rule chord as well, RECORDED, so both
rules' readings of every crossing are on file; `--expected` for the 23 slot points and the 59 identity lines). Vendor sim logs
and C:\MAK\logs: COUNT and NAME / MTIME only. Results go to: the Result block below (measurement and implication in separate
sentences); PLAN_MOVEMENT_2026-09-27.md rows G1-4 / W1 -> G1-5 and the step table; RUNBOOK sec 11o and sec 9 (W1 seen live or
not); HANDOFF_SEAT sec 2; Appendix B annotated from the manifests. ASCII + CRLF.

## 7. What this run does NOT claim

- G1-4's list, unchanged (the map display; the authored variant; nested containers; the STP TO precedence; a patrol or a point
  move; combat; the entity-level profile; the full order; the NONE branch of Auto; Move (Group); the timing; the cause of
  G1-3's refusal; the counter's range; buffer 10 against 0; the Label).
- N1's MECHANISM: G1-5 CANNOT DISCRIMINATE N1's readings (e) the sim's terrain mobility at the birth point and (vi) the planner's
  10 m obstacle buffer - both predict that a member born 13.7 m from the water moves (sec 4 COMPETITORS) - nor (v) from (vi),
  nor either from a misalignment of the sim's water with OSM; so an INF3WPN1 that moves says W1's clearance removed the stop on
  cut A once (n = 1), not which mechanism made it. WHAT WOULD DISCRIMINATE (each
  needs its own registration; buffer 10 also needs a ruling, RL-20260928-03): (1) a member born between the OSM shoreline and
  10 m with buffer 0 (or obstacleQuery none) against the same slot with buffer 10 - (vi) predicts the first moves, (e) predicts
  neither does; (2) a sim-side terrain or feature query at G1-4's birth point (54.022048, 23.309402) through the vendor's API
  (docs first, CLAUDE.md sec 1) - (e) predicts an impassable (water) class there, (vi) predicts nothing about it; (3) a member
  born 2-10 m from water where the sim's water layer is known to match OSM.
- THE RECORD ALREADY HOLDS ONE OBSERVATION ON (e), unused by G1-4's Result and its cold review (sec 8 item 9). MEASUREMENT
  [V: <L5>\g1_5_inf3wpn1_runs_out.txt (Fable's disp.py with absolute paths) and g1_5_bridge_d_ways_g12scored.txt; G1-2's
  vrfc2simapp.log L644; its traces]: on the LITERAL path (build 699552c, PA_Move_Along_Route on STP's line, no
  navigate-to-location and so no obstacle buffer) the same member (uuid b6c4756d-e5c1-5d13-a1c9-508222bee540, Mech CO (USA,
  M2)) was born at the same point, (54.022048,23.309402) at 135.0 m, in BOTH G1-2 runs, and moved 1,662.5 m (scored; first fix
  more than 1 m away at t=74.1; within 50 m of T14's vertex at t=175.6, on to the -2 hamlet's building stop (54.030655,23.326944;
  one 'Terrain too steep')) and 1,660.2 m (pre-warm, t=74.4); G1-2's Result P10a / N1 [A] count it among the movers. G1-3 and
  G1-4 (the same point) read 0.0 m - G1-3's because the executor refused every route, so it says nothing here. READING [A]: (e) as written - the sim's impassable water under the unit's centre held it from
  its first tick - predicts that G1-2's INF3WPN1 would not have left that point either, under the same aggregated movement
  actuator; it did. That weakens (e) and leaves the readings specific to navigate-to-location: (vi) the 10 m buffer, and G1-4
  N1's (v), a planned first leg into the lake, and the composite of sec 4 COMPETITORS (inside the buffer with the lake between
  the start and the roads), which fits every run. The strongest competitor to that weakening: the sim's streamed water features
  differed between the runs (G1-2 14:27Z, G1-4 22:58Z the same day; unread, as N1 (e) says). Fable's cold review of this
  registration verified the measurement (fable_g15, ITEM 5); G1-4's Result now carries it as an ADDENDUM under N1 (e) - not
  re-scored. Not a G1-5 claim.
- THAT 10 m IS ENOUGH IN GENERAL: the clearance is measured from OSM polygons; the sim reads its own streamed features, whose
  alignment with OSM is unmeasured - INF3WPN1's 3.7 m over the buffer (13.7 m, with the lake between it and the roads) is the
  thinnest EFFECTIVE margin on cut A, INF1WPN1's 11.3 m the thinnest raw one; other orders, slots and lakes are not covered.
- THE RIVER CROSSING ITSELF: the ways rule judges fixes against ways; the member's planned path is not printed, so "on the
  bridge" stays an inference (G1-4 N2). A ford up to about 0.56 x the chord to the side of a bridge over the same river (about
  80 m on G1-4's 145 m chord), with both banks on roads, reads ON A BRIDGE under the ways rule - the blind zone of sec 1(z); a
  river split across OSM way ids can make the rule FIRE falsely (sec 1(z)).

## 8. Where the record was silent or disagreed - open points for the seat

1. THE RTIEXEC FOR THE HOLDER STEP. HANDOFF_SEAT sec 2 (:31) and PLAN_MOVEMENT :99 say "Stage 2r starts a fresh rtiexec" - true
   of the runner, but the holder step comes first and StartFederationHolder52.ps1 refuses without a running rtiexec (exit 2,
   :24-25, :162-165) [V]. This registration adds step R (StartRtiExec52.ps1, ensure-up, no appNumber; DEMO_RUNBOOK sec 2 Way B
   step 1 is the same order [V: :255-258]). The record is silent on who starts the trio after a reboot on a scored go-live; the
   seat confirms step R or names another.
2. FOUR BOOTS SINCE G1-4, three of them in no record (HANDOFF_SEAT names only 2026-09-28 23:16:45Z): two user restarts and two
   Windows Update restarts, KB5121794 installed 2026-09-30 (sec 4a) [V]. Nothing deployed changed (sec Registration hashes) [V];
   the OS update is an unintended difference, recorded; the seat's to note.
3. THE PRIVATE C2SIM SERVER IS DOWN: REST http://127.0.0.1:18080/C2SIMServer refused the connection at 2026-10-04T19:39Z [V:
   g1_5_golive_prertiexec_registration.txt]. The container EXISTS, Exited (255) [V: docker ps -a], so RUNBOOK sec 1's
   `docker run` (:1716) does not apply: step S (sec 3) is `docker start c2sim-server-vrf` then REST 200, before A
   (RL-20260928-01 authorises that server). RUNBOOK :1716 could say "docker start if it exists" - the seat's. The operator's
   8080 / 61613 server stays out of bounds.
4. THE BRIDGE RULE'S CONSTANTS are this lane's design, in no ruling: ROAD_TOL_M 15 (BRIDGE_TOL_M's width), DETOUR_MAX 1.5, the 5 m
   "over the river" tolerance. On G1-4 the margins are wide (banks 0.3 / 0.3 m; 1.02 x), and the nearest dirty control is 3.56 x
   (c4). The ways rule only RELAXES G1-4's (it keeps the 15 m chord acceptance first). DETOUR_MAX 1.5 accepts a bridge inside an
   ellipse of semi-minor 0.56 x the chord (about 81 m on G1-4's chord - the blind zone; the 60 m / 100 m controls, sec 1(z));
   1.5 is set by the right-angle bend (1.41 x). Residual: a false FIRE on a river split across OSM way ids. The seat approves or
   alters them before the go-live; changing one after the run is a re-score, not this registration.
5. FABLE'S FIGURES vs THIS LANE'S: 10.9 against 11.3 m for INF1WPN1, 13.6 against 14.3 m for INF1RIF3 (the others within 0.1 m)
   - explained by the planned-slot rebuild (sec 1(y)); the registered landings are this lane's, the +/- 1 m tolerance the
   handoff named. No disagreement on which slots move, where to (compass, ring) or the counts.
6. THE SL LIMBS ARE GATED HIGH in the W gate. An SL miss with every movement limb passing is a deterministic-code mismatch, not a
   movement verdict; the seat may prefer it RECORDED - registered HIGH because W1 is the one variable and its output is fully
   predicted.
7. THE HOLDER CREATES THE FEDERATION on a fresh rtiexec: if all four numbers are refused (exit 1) the go-live STOPS at D'; new
   numbers then come from the marker (5285), every later number shifts, and the shift is recorded (sec 5). The record has no
   refusal rate for a fresh instance.
8. G1-4 RESULT N2 (c) vs THE WAYS RULE'S BANK READING: N2 put the post-river fix "9.4 m from track 178824546"; the nearest
   drivable way to it is unclassified OSM 365845592 at 0.3 m [V]. Not a contradiction (N2 measured to the way it named); recorded
   so the Result does not re-derive it.

9. G1-2's INF3WPN1 MOVED FROM G1-4's BIRTH POINT ON THE LITERAL PATH (sec 7; measurement [V], reading [A]): an observation in
   the record that bears on G1-4 N1's readings and that N1 and its cold review did not weigh. It changes no row of this
   registration (W1 moves the slot either way, and H5 is the test). Fable verified it (fable_g15, ITEM 5); the G1-4 Result
   carries it as an ADDENDUM under N1 (e), with the composite reading (sec 4 COMPETITORS), not re-scored.

SEAT DECISIONS 2026-10-04, before any go-live step (the seat, Opus, under RL-20260928-01 and RL-20260928-04):
- (1) Step R confirmed as registered (StartRtiExec52 ensure-up before the holder; DEMO_RUNBOOK sec 2 Way B is the same order).
- (2) The four boots and KB5121794 stand as recorded unintended differences; nothing deployed changed.
- (3) Step S confirmed (`docker start c2sim-server-vrf`, REST 200); the RUNBOOK :1716 wording is a doc follow-up after the Result.
- (4) The bridge-rule constants (ROAD_TOL_M 15, DETOUR_MAX 1.5, the 5 m over-the-river tolerance) are APPROVED as registered,
  blind zone and all; none is changed after the run.
- (6) The SL limbs stay HIGH in the W gate, as registered.
- (7) As written: a holder refused on all four numbers STOPS the go-live at D'.

## Result (written after the harvest, never from a live read)

RESULT 2026-10-04: STOPPED AT THE W GATE - NO SCORED RUN. The pre-warm (run 20261004T202533Z_run, launched 20:25:32Z, runner
exit 0 at 20:34:19Z) scored "W GATE: FAIL (branch A; P-FALS(e)) - STOP before E" (g1_5_score.py --wgate, scorer sha256
c60b0b6c..., output sha256 c2031c74...; scratch laneG1-5\W_gate_20261004T202533Z_run.txt; re-run by this lane from the same
scorer on the same run directory: identical line for line, the header's run path aside). E was not launched: P0-P23 and H6
are NOT SCORED and the scored block 5296-5306 was never claimed. Every other W limb passed: SL0-SL4 (the four slots moved as
predicted), H1-H5 (H5 on all 23 tasked members; 48_IBCT.INF3WPN1 moved 1,701.0 m from 13.6 m off lake 16373225), W-MOVE,
W-DONE, V-CRASH. The one miss is P-FALS (e) under the ways rule: all 17 T14 members, on 6 distinct tracks, crossed the
Wiersnianka where road 300614718 crosses OSM river way 1467512812, tagged tunnel=culvert, and drove on (N1). By the stop rules
(sec 4) nothing is patched or re-run under this registration; whether the FAIL binds as scored is the owner's question (the
G1-4 precedent: RL-20261004-01); the successor is IRONSTORM_AGG_G1-2026-10-04-6 or later, with new numbers. Marks: [V] =
checked by this harvest lane after the run (read-only: the run directory, the runner log and manifest, the rtiexec log, the
trace, the reports capture, the thread samples, the scorer re-run, the OSM tile copy the scorer reads, the vendor RiverL
filter, featureconfig and mech sysdef, the seat's go-live artefacts); [A] = the seat's record or an inference. The vendor sim
log and C:\MAK\logs: counts, names, sizes and mtimes only. This lane's scripts and outputs: scratch 7207877b...\harvestG1-5
(h_*.py, h_*_out.txt, h_wgate_repro.txt, h_wgate_chord.txt, h_full_ways.txt).

THE SEQUENCE AS RUN (by the seat under RL-20260928-01) [V: scratch laneG1-5 artefacts and the runner log
runs/launch52/RunScenario-ironstorm-agg-g1-5-prewarm-20261004T202532Z.log, unless marked]:
- R, 20:21:36Z: prertiexec 1 check FAILED - REST 18080 refused, expected until step S (sec 8 item 3; R_prertiexec.txt).
  StartRtiExec52 dry run, then live: rtiexec pid 65540 started 20:22:01Z and started its own rtiForwarder 58844 (20:22:04Z);
  "RTIEXEC READY rtiexec=65540 forwarder=58844 tcp=127.0.0.1:4001" (R_rtiexec.txt). Its log is runs\launch52\
  rtiexec_20261004T202200Z5.0.1-20261004-162202-Legatus-281993-65540.log (the script's own "[WARN] rtiexec log NOT created"
  is a naming test, N4 (d)).
- D', 20:22:23Z: preholder 1 FAILED (REST, as at R; Dp_preholder.txt). g1_5_claim_holder.ps1 claimed 5281-5284, marker 5281
  -> 5285 (Dp_claim.txt; commit 3f04f34). StartFederationHolder52 attempt 1 on 5281 at 20:22:51Z: "HOLDER JOINED: pid 67984
  appNo 5281" at 20:22:59Z, 28800 s (Dp_holder.txt); rtiexec log L2026, handle 2, the log's first join. The federate list
  (g1_5_rti_federates.py) printed 0 join lines - a parser defect (N4 (a)); the seat counted one join by hand [A], and the log
  holds exactly one join before the pre-warm's Stage 2h holder (L5343; that holder started 20:25:36Z) [V].
- S: `docker start c2sim-server-vrf` [A: the seat; no artefact]; REST 200 by 20:23:41Z [V: A_prelaunch.txt].
- A, 20:23:41Z: prelaunch 0 FAILED - the registered hashes and settings, 41 / 0 road tiles, cache 520 / 27c2117e, rtiexec
  65540 and forwarder 58844, holder 67984 with 479 min left, marker 5285, REST 200 (A_prelaunch.txt). A2 (the offline suites
  on the deployed exe) [A: the W1 deploy line; no A2 artefact in the lane's scratch].
- C3, 20:23:52-20:24:25Z: PushInit exit 0 ("QUERYINIT : 40 Units"; the server read RUNNING before its reset), PushOrder exit
  0, one "ORDER (69670 chars)" echo at 20:23:55.081 (C3_stdout.txt, g1_5_c3_pushinit.txt, g1_5_c3_pushorder.txt).
- W: prelaunch 20:25:12.9Z 0 FAILED (W_prelaunch.txt; 478 min left); the sim cache listed 20:25:12Z; the dry run 20:25:13-16Z
  exit 0; the pre-warm launched 20:25:32Z (g1_5_runner.stamps.txt): holder 67984 recognised PERSISTENT (runner log :88), block
  5285-5295 claimed and marker 5285 -> 5296 before any join (:130), Stage 2r found 65540 up and started nothing (:146-147),
  Stage 2h holder pid 68076 on 5292 joined in 3 s (:160-161; rtiexec log L5343, handle 3), Stage 2c RtiProbe on 5290 exit 0
  (:171), the back end pid 63444 on 5285 started 20:25:47.505Z (:185-186), the app pid 62220 on 5289, the oracle gate passed
  on 36 real-coordinate POS lines (:250), the order on the bus 20:29:06.253Z (:258), the window ran 121.3 s to its 120 s cap -
  4 of 5 tasks terminal, T14 (1075b583...) open (:270-271) - the app exit 0, a clean resign (:279), StopVrf EXIT 6 (:288-289;
  N4 (c)). Runner exit 0, end 20:34:19Z; 0 "BACK END CRASHED" (app log count). Scored by the seat at 20:34:36Z (the W gate
  file's mtime). Post-W inventory 20:35:03Z (postW_inventory.txt): rtiexec 65540, forwarder 58844, holder 67984 (468 min
  left) and the pre-warm's Stage 2h holder 68076; 1 check FAILED, "exactly one RtiProbe: 2 (want 1)", which is that
  registered second holder (sec 3 W names it; N4 (b)); REST 200; marker 5296. The sim cache listed 20:35:05Z.
- THE QUIET PERIOD held: no other runner log in runs\launch52 between W's prelaunch and the post-W inventory (the suite's
  dry-run logs sit at 20:18-20:19Z and 20:36-20:37Z) [V: listing].
- THE REGISTRATION FREEZE held [V: git commit stamps]: the last registration commit a992def (the seat's decisions, sec 8) at
  20:21:24Z, step R at 20:21:36Z, the holder claim 3f04f34 at 20:22:46Z, the pre-warm launched 20:25:32Z. The commit stamps
  govern the DATE line's "about 20:30Z".
THE CLOCKS [V]: trace t0 = WatchVrf join 20:28:25.609Z; T10 dispatched t=74.0 (SIM 345.6), T14 t=79.2 (SIM 380.2), T02 t=80.2
(SIM 381.2); every member's last fix t=190.9 (20:31:36.5Z; the window ended 20:31:37.7Z, manifest observationEndUtc); the
trace's last fix of any object t=221.7. The sim ran about 6.5-6.8 x wall (L64823: 64.1 WALL s = 414.7 SIM s; L77855: 78.0
WALL s = 527.3 SIM s). Reports (reports-captured.log): TASKSTRT T01 and T13 20:29:09.0Z, T10 20:29:39.6Z, T14 20:29:44.8Z,
T02 20:29:45.9Z; TASKCMPLT T01 and T13 20:29:44.4Z, T10 20:30:44.0Z, T02 20:31:03.9Z; T14 none. Peak back-end working set
4,133 MB at 20:31:39Z and 3.43 cores at 20:30:04Z (thread-samples.csv); WS-runaway alerts 992.7 MB/min at 20:26:58Z and
1,628.4 MB/min at 20:29:08.6Z (load-time, as before; manifest backendWsRunaway true).

WHAT HELD [V: the W gate output and this lane's re-run]: W0-W7 (1 / 5 / 17 of N attached and published); IDENTITY branch A,
I1-I17 - 59 of 59 created under the requested uuid and bound by it, the M3b short names as markings (RL-20260928-02);
SL0 the MEMBER SLOT CHECK start-up line once, exact (L32); SL1 23 slot lines with exactly 4 SLOT MOVED - 48_IBCT.INF1RIF2,
INF1RIF3, INF1WPN1, INF3WPN1; SL2 4 of 4 as predicted (distance, compass, water id, landing within 1 m, the clearance clause;
INF3WPN1 at (54.021976,23.308667), L726); SL3 19 clear at their planned points; SL4 0 UNVERIFIED, 0 KEPT ON BAD GROUND;
M1-M10 and M12-M17 on all three movers (road layer 16 / 10 / 14 readable, 0 not; every leg NEAR -> MAK_ROAD on the offline
table's ways 384833185, 372319246 / 367165904 / 1262103420, 783013873 / 312326007); M10 0 FAILED; H1 0 relayed "route does
not exist" and R-VEND 0 (vendor log count); H2 0 EXECUTOR REFUSED and 0 pre-M3b equivalent; H3 50 of 50 echoes exact (T10
5 x 3, T14 17 x 2, T02 1); R-REF 57 of 57 WHOLE ("<member> Path part <n>"); H4 by the identity limbs; H5 PASS - 33
member-vertex pairs judged in the W gate (the W gate judges a vertex k > 1 only once it has closed), 0 not moved, INF3WPN1
included; the same scorer in full mode on this run (RECORDED - the pre-warm is unscored) judges 50 pairs, T14 vertex 2
included, 0 not moved; W-MOVE PASS (T10 5 of 5 > 50 m; T14 17 of 17 and T02 1 of 1 RECORDED); W-DONE PASS (T14 vertex 1
closed - "the LAST of 17: 17 succeeded, 0 failed", L70619 - and T10's three and T02's one; nothing reached but not closed;
the scorer prints "NOT REACHED []"; T14 vertex 2 is NOT REACHED by this lane's reading of its O1 line, N3); V-CRASH no signal for pid 63444 (C:\MAK\logs holds its .log, 14,814,344 B, last written
20:31:39Z, and no .callstack.log or .dmp [V: listing]); the fixes-inside-water limb (0 fixes deeper than 25 m; "longest run
0" for every member). P-FALS (e)'s T02 limb held as predicted: 28ID.HQ1 crossed river 8011072 at 54.007540,23.236524, 13.3 m
from a drivable road bridge (218414262) - ON A BRIDGE by the 15 m chord acceptance the ways rule keeps first - and drove on
3,035 m to 0.5 m from its destination.
THE WATCH (RECORDED): INF3WPN1 1,701.0 m, within 50 m of vertex 1 at t=142.1, vertex 2 NONE; INF1WPN1 1,483.2 m, t=109.4,
NONE.
THE MOVED-MEMBER RECORDS (RECORDED; G1-4's pre-warm reference in brackets): INF1RIF2 first-leg bearing 310 deg, first-300-m
water minimum 15.8 m, 0 constrained [294 deg, 2.3 m, 0]; INF1RIF3 174 deg, 14.5 m, 0 [166, 5.5, 0]; INF1WPN1 294 deg, 11.3 m,
0 [189, 7.5, 0]; INF3WPN1 89 deg, 9.2 m (lake 16373225, t=88.9), 0 [NONE, 1.7 m, 1].
THE MOVES [V: the app log, the trace]: T10 - vertices closed 3 / 4 / 20 m from them (scorer R12), ARRIVAL EVIDENCE 64.1 WALL
s after dispatch (L64823), final fix 1.4 m. T02 - its one vertex closed 19 m from it after 5,354 m (L80173); OVERDUE, then
ARRIVED 78.0 WALL s = 527.3 SIM s after dispatch (L77855-77857); final fix 0.5 m. T14 - vertex 1 closed with the unit 6 m
from it after 1,273 m (L70621), all 17 members within 50 m of it by t=148.2; vertex 2 issued (L70659-70661: NEAR 0 m ->
MAK_ROAD on 312326007, to (54.040348,23.324206)) - the first M3b leg past a vertex 1 (G1-4's vertex 1 never closed); 0
"Cound not create route" (G1-3's vertex-2 text; P7c carried, held).

WHAT FAILED [V]: P-FALS (e) / P11 / P14 under the ways rule - every T14 member "crossed river 197345450" (14 members) or
"1467512812" (INF2HQ1, INF3HQ1, CAV1) at about 54.01986,23.32848, 4,860-4,866 m from any drivable road bridge, and drove on
1,194-2,018 m; each line reads "OFF A BRIDGE by the ways rule: banks 0.2-2.9 m (south) / 0.5-1.9 m (north) from a drivable
road (<= 15 m: ON the network); NO drivable road bridge over river <id> in the index" (W gate output lines 118-151; the
P-FALS line 163). The chord rule (RECORDED, `--bridge-rule chord`, h_wgate_chord.txt) reads the same 17 crossings OFF A
BRIDGE, T02's ON, and prints the same verdict.

N1 - THE CULVERT CROSSING:
(a) THE GROUND [V: harvestG1-5\h_culvert_geom_out.txt, h_culvert_geom2_out.txt - lane G1's tile copy, the tiles the scorer
    reads]: the Wiersnianka is three OSM ways here. 197345450 (waterway=river) comes from the west and ends at
    54.019860,23.328481 (C below); 1467512812 (waterway=river, tunnel=culvert, layer=-1) is a 2-vertex way 9.5 m long, from C
    to 54.019807,23.328593; 1467512811 (waterway=river, no tunnel tag) continues from there. Road 300614718 (highway=
    unclassified, surface=asphalt) runs north-south 3.7 m east of C and CROSSES the culvert way at 54.019835,23.328533 - 4.5 m
    from C, inside the culvert's 9.5 m; it crosses neither 197345450 nor 1467512811. Track 360178110 joins 300614718 at
    54.019362,23.328470, 55 m south of C. No feature within 300 m in either tile set carries a bridge, ford or other tunnel
    tag; the nearest drivable road bridge is 4.86 km away (the scorer). The osm-water set (the sim's Lake layer) holds nothing
    within 185 m of C (lake 197345448 at 185.5 m).
(b) THE FIXES [V: the trace]: the 17 crossings are 6 distinct chords. The 12 Mech COs were within 2 m of one another from
    t=148.2, 5 m from vertex 1 (each member is sent to the vertex point itself - H3's echo), to t=160.4, and again from t=162.4
    to the window's end, so their 12 crossings are one chord. Per chord - south bank fix (distance to road 300614718 or track
    360178110; distance south of the river line) / north bank fix (to the road; north) / chord length / the chord's crossing
    point to the road centreline and to the culvert x road point:
    - HQ1: t=154.3 track 0.5 m, 101 m / t=156.3 road 0.7 m, 19 m / 128.4 m / 3.7 m, 4.4 m.
    - INF1HQ1: t=154.3 track 0.2 m, 98 m / t=156.3 road 1.0 m, 57 m / 170.1 m / 10.2 m, 11.5 m.
    - INF2HQ1, INF3HQ1: t=154.3 track 0.5 m, 93-97 m / t=156.3 road 0.5-0.6 m, 18-19 m / 121-123 m / 2.9 m, 3.5 m.
    - CAV1: t=156.3 road 0.8 m and 1.3 m from the river line - the one fix AT the crossing, on the road / t=158.4 road 1.9 m,
      111 m / 123.7 m / 1.2 m, 1.5 m.
    - the 12 Mech COs: t=162.4 road 2.9 m, track 5.0 m (at their junction), 46 m / t=164.5 road 0.7 m, 15 m / 67.5-67.7 m /
      4.6 m, 5.3 m.
    Every crossing point lies 1.2-10.2 m from the road's centreline and 1.5-11.5 m from where the road crosses the culvert
    way; no fix lies off the road within 10 m of the river line. THE OFFSETS ARE THE CHORDS' STRAIGHTNESS [V: Fable's cold
    review, scratch fable_g15r\rederive_out.txt]: chord by chord, the road's own vertex at the culvert lies off the straight
    chord line by 2.2 / 4.1 / 10.3 / 4.6 / 3.4 / 3.4 m (CAV1 / HQ1 / INF1HQ1 / the Mech COs / INF2HQ1 / INF3HQ1), against
    crossing-point offsets of 1.2 / 3.7 / 10.2 / 4.5 / 2.8 / 2.9 m; a chord that follows the road at both ends cuts its bend
    by that much. THE SPEED DIP [V: same source; m per trace-second, the chord before / the crossing chord / the chord after]:
    HQ1 103 / 64 / 71; INF1HQ1 103 / 85 / 62; INF2HQ1 104 / 61 / 74; INF3HQ1 105 / 62 / 74; CAV1 85 / 59 / 69; the 12 Mech COs
    46 / 32 / 58 - every chord at or next to the crossing runs at about 0.6 x its neighbours, on all six tracks. The dip does
    not move any fix off the road. It is UNEXPLAINED; candidates, no cause claimed: the track-to-road junction 55 m south of C
    (a turn of about 30 deg); landuse=residential 25 m from C (URBAN is MAK_MECH_RESTRICTED_L2, speed-factor 0.25); the river
    or culvert in the sim's mobility layer. The last bears on competitor (iii) below: a road entry that outranks every other
    entry predicts no dip at C. Every member then drove on north along 300614718 (1,194-2,018
    m); 0 "Movement constrained by features" and 0 "Terrain too steep" (app log 0 / vendor log count 0 [V:
    h_counts_out.txt]); 0 stationary runs (the scorer); all were still moving when the window closed (N3).
(c) THE READING MOST CONSISTENT WITH THE EVIDENCE [V for the ground and the fixes; A for the path]: the members drove the road
    over the culvert. The leg was planned on the road network (vertex 2 NEAR -> MAK_ROAD); both bank fixes of every chord are
    on the network; the one fix at the river is on the road; and the road crosses the river only through the culvert. The
    planned path is not printed (DEBUG_DETAIL false), so the crossing is inferred from the fixes and the plan's road query,
    not observed.
(d) THE SIM'S WATER AT C [V: the files read; A: that the running terrain used this file]: the vendor's aggregate River filter
    (C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\VRFSIM.Aggregate.feature.model.xml :323-354, "RiverL")
    keeps every way, not a node or a polygon, with waterway river / canal / tidal_channel, and tests no tunnel or culvert tag
    (as src OsmFeatures.cs :454-456 records). If the aggregate terrain loads that file (PREREG G1 :46 [A]), 197345450 and the
    culvert way are both River features at C - MAK_WATERWAY (featureconfig.txt :413), IMPASSABLE for a mech unit (:262;
    mech-aggregated-movement.sysdef :116-120, speed-factor 0, priority 200). The same sysdef gives MAK_MECH_UNRESTRICTED_TERRAIN
    = MAK_ROAD (featureconfig.txt :259) speed-factor 1 at priority 100 (:111-115). Which entry governs where a road and a river
    line overlap is not in the docs read here: UG52 27.1.4 p531 defers to "Configuring Aggregate-Level Movement Restrictions"
    in the Adding Content manual, which is NOT INSTALLED - C:\MAK\vrforces5.2d\doc\AddingContent.pdf is a one-page placeholder
    ("Documentation Has Not Been Installed": the vendor documentation installer was not run), docs/vendor/mak-5.2 holds no
    Adding Content text, and the installed txt set has 0 hits for "culvert" [V: Fable's cold review, fable_g15r].
THE COMPETITORS: (i) A FORD BESIDE THE CULVERT - the members left the road for a few metres to cross the open channel: no fix
supports it (the fix at the river is 0.8 m from the road), the planner was routing on the road network, and an impassable
river line would have held them as G1-2's T02 member was held at river 8011072 off the road; but the fixes are 67-170 m apart
across the river and the path is not printed, so the trace alone cannot exclude it. (ii) THE SIM'S WATER LAYER HAD NO WATER
THERE - not supported by the vendor filter (no culvert exclusion), but the streamed features the sim used are unread and could
differ from the tile copy. (iii) THE ROAD'S MOBILITY ENTRY OUTRANKED THE RIVER'S at the overlap - consistent with every fix's
position, unverified (the priority rule is in the uninstalled manual), and in tension with the speed dip at C if the road
outranks every entry (N1 (b)). (ii) and (iii) bear on WHY a member could pass C, not on WHERE it crossed; all three
are consistent with a crossing on, or within a few metres of, road 300614718. WHAT WOULD FALSIFY THE ON-THE-ROAD READING: a
fix in the channel more than about 5 m off the road's centreline; a "Movement constrained" line or a stop at either bank; the
planned path (a DEBUG_DETAIL run) leaving the road at C; a sim-side feature query showing the road broken at C. None is in
the evidence.
(e) WHAT THE REGISTRATION NAMED: sec 1(z) named two residual errors of the ways rule - a blind zone (a ford near a bridge over
    the same river reads ON) and a false FIRE when the bridge spans another way id of the same river. This is a third case it
    did not name: a road over a river with no bridge tag (here tunnel=culvert on the river way). The rule fired as written.
IMPLICATION (a scorer design matter for the successor - the seat's, not this lane's): the ways rule has no culvert clause.
A clause accepting a crossing where a drivable road way crosses the river way within the chord tolerance, both banks on the
network, would accept this crossing. Whether it should be narrow (a river way tagged tunnel=culvert) or wide (any drivable
road crossing a river line, which (iii) would make the sim allow) turns on the unread priority rule.

N2 - 48_IBCT.INF3WPN1, THE ROW G1-5 EXISTED TO TEST (n = 1):
MEASUREMENT [V: the trace, app log L726, harvestG1-5\h_inf3wpn1_out.txt, h_g14pt_out.txt]: born at (54.021976,23.308667) as
predicted (SL2), 13.6 m outside lake 16373225 on the scorer's tiles and 48.6 m from its G1-4 birth point. It stayed there from
its first fix (t=42.1) to T14's dispatch (t=79.2), was 17.6 m out at t=82.8 and left on bearing 89 deg (the first fix 50 m
out, t=86.9). From t=82.8 to t=99.2 its 9 fixes ran east, then north-east and north round the lake's east shore, 9.2-10.2 m
from the OSM shoreline (minimum 9.2 m at t=88.9), passing 7.9 m from G1-4's birth point (54.022048,23.309402); the chords
between those fixes (5-50 m long) come within 4.7 m of the shoreline where they cut its curve. It reached the road network
north-east of the lake at t=101.2-105.3 (track 595161645 at 10.9 m, then unclassified 1309922882 / 1262103420 at 1.6 / 0.2
m), came within 50 m of vertex 1 at t=142.1 and moved 1,701.0 m in all; 0 "Movement constrained by features" (G1-4: 1). The
straight segment from its landing to the nearest drivable road (595161645, 306 m) cuts the lake (sec 4 COMPETITORS); the
member went round it.
READING [A]: (1) the pass is what every registered reading predicts from 13.6 m - (e), (vi), (v) and the composite (sec 7) -
so it does not discriminate them; the lake between the start and the roads (the composite's second condition) was present
again and did not stop it, as the composite predicts once its first condition (a start inside the 10 m buffer) is removed.
(2) A track that holds 9.2-10.2 m from the OSM shoreline for about 200 m, away from any road, fits a path planned round the
water with the 10 m buffer (navigate-to-location.lua :14) against a water feature lying within about a metre of the OSM
shoreline on that stretch [A: the planned path is not printed; the fixes are 2 s apart]. If the planner's water edge there
matches OSM to a metre, G1-4's birth point (1.7 m outside OSM, 7.9 m from this track) lay about 8 m inside the planner's
buffer - the start that (vi) and the composite name - and (e) would need the actuator's water to reach at least 1.7 m beyond
an edge the planner appears to place at the OSM line; that weakens (e) further only if the planner and the actuator read the
same feature [A]. (3) It does not separate (vi) from (v) (a first leg into the lake from G1-4's start): G1-5 never started
inside the buffer. The discriminating observations remain sec 7's (1)-(3).

N3 - T14 VERTEX 2 NOT REACHED IN THE 120 s WINDOW [V: the trace, harvestG1-5\h_tail_out.txt; runner log :270]: vertex 2 was
issued when the last member closed vertex 1 (t=148.2); at the last fix (t=190.9) the container was 874.0 m from it, the four
HQ members 297-315 m, CAV1 428 m and the 12 Mech COs 1,103.6 m, all still moving (the Mech COs 91 m and HQ1 about 150 m in
the last 2 s). The task was open at the cap ("still open: 1075b583..."), so -StopWhenComplete did not fire. The scorer
classifies W-DONE per task: it prints "NOT REACHED []" (T14 counts as closed on its vertex 1) and shows vertex 2 only as "O1
vertex 2 of 2: NO member within 50 m"; calling vertex 2 NOT REACHED is this lane's reading of that O1 line, not the scorer's
print. By W-DONE's rule a NOT REACHED vertex is recorded, not a stop. RECORDED, no reading: the Mech COs' last fix (54.030578,23.327180) is 17.6 m from the
point where G1-2's INF3WPN1 stopped at a building of the -2 hamlet (sec 7); they were moving west there, on the line the HQs
and CAV1 had driven about 18 s earlier.

N4 - THE HARNESS [V]:
(a) THE FEDERATE PARSER: g1_5_rti_federates.py (lane G1-4's rti_federates.py, log chosen by pid) matches only
    `^Federate Federate (.+?) \("(.+?)" (\d+)\).* has (joined|resigned from) federation "`. The long-lived rtiexec 47980 wrote
    each join doubled and interleaved (its L4051: "Federate Federate remoteControl 34096 ("remoteControl" 2)remoteControl 34096
    ... has joined federation " has joined federation ..."); the fresh rtiexec 65540 writes it once (L2026: "Federate
    remoteControl 67984 ("remoteControl" 2) has joined federation "MAK-ONE-2025"."), so the parser found 0 join lines in a log
    that held the holder's join (Dp_federates.txt), and D''s "exactly one joined federate" was not machine-checked. By grep it
    held. The whole log: 7 joins (67984; 68076; 34468, Stage 2c; Federate5, the back end; 67260, the WatchVrf pre-check;
    26380, the trace; 62220, the app) and 5 resigns (34468, 67260, 62220, 26380 normal; the back end LostConnectionResign,
    forced by the rtiexec after the kill, L25356-25393); 67984 and 68076 still joined at the harvest read. The log now also
    holds 68076's NormalResign (L25502, stamped 20:40:40Z - the end of its 900 s hold); 67984 remains joined.
(b) THE POST-W INVENTORY CHECK: golive_checks -Phase prelaunch asserts "exactly one RtiProbe (the persistent holder)"; after
    the pre-warm the registered inventory (sec 3 W) includes the pre-warm's Stage 2h holder (68076, a 900 s hold from about
    20:25:39Z), so the check's FAIL is that expected holder - the check is wrong for the post-W phase, not the inventory.
(c) STOPVRF EXIT 6, again: taskkill without /F refused for 120 s; the run's own back end force-stopped by pid and start time;
    its windows "NVOGLDC invisible" and "Default IME" only, no crash modal (stopvrf.stdout.log). The registered gate accepts 0
    or 6. Seen on G1-4 and earlier runs [A: lane D1].
(d) StartRtiExec52's "[WARN] rtiexec log NOT created at <path>": the rtiexec appends its own suffix
    ("5.0.1-20261004-162202-Legatus-281993-65540") to the -l name, so the script's exact-path test misses a log that exists
    (the preholder check finds it by glob; the holder and the runner read it). No effect on the run.
THE CARRIED ITEMS [V]: THE CRASH did not recur (V MEDIUM held; 1 crash in 3 runs on this path). O8 DID NOT RECUR, against
sec 4's expectation (5): 36 of 36 init create altitudes came from the TERRAIN QUERY, 0 from the FALLBACK (L377, 20:29:02.985Z;
G1-4: 0 of 36) - recorded, no cause claimed (the federation, the rtiexec and the machine's boot are new, sec 4a). O6 - 18 "No
creator found" lines (as G1-4). P19 - 79 cache HITs, 0 HTTP FETCH (L99963). THE SIM CACHE - unchanged: 21,807 files,
495,599,180 B before and after (simcache_before / after_prewarm.txt; G1-4's pre-warm added 2). The vendor log
(runs\20261004T202533Z_run\vendor\vendor-vrfSim.log, 14,814,344 B, the twin of C:\MAK\logs\...-162550-...-63444.log): "Path
part" 57, "48_IBCT.INF3WPN1" 20, "Movement constrained" 0, "route does not exist" 0, every failure text 0 (counts).

THE NUMBERS [V: Dp_claim.txt, Dp_holder.txt; runner log :92-105, :130, :301; run-manifest.json appNumbers and ledger; the
rtiexec log's joins]:
- THE HOLDER 5281-5284, claimed by g1_5_claim_holder.ps1 before the holder joined (marker 5281 -> 5285, commit 3f04f34): 5281
  CONSUMED by RtiProbe pid 67984 (attempt 1, joined 20:22:59Z, holding 28800 s - to about 2026-10-05T04:22:51Z; never
  touched); 5282-5284 BURNED (not reached - one attempt per number until one joins).
- THE PRE-WARM 5285-5295 (marker 5285 -> 5296 by the runner, :130; manifest ledger 5285 -> 5296; commit bd21b35): 5285 (back
  end, pid 63444, force-stopped by identity, StopVrf exit 6, no crash), 5287 (WatchVrf pre-check; pid 67260 by the rtiexec
  log's join order), 5288 (WatchVrf trace, pid 26380), 5289 (VrfC2SimApp, pid 62220, exit 0), 5290 (Stage 2c RtiProbe, exit
  0; pid 34468 by the join order), 5292 (Stage 2h holder attempt 1, pid 68076, joined in 3 s, left inside its 900 s hold)
  CONSUMED; 5286 (--no-gui), 5291 (the oracle gate passed), 5293-5295 BURNED.
- MARKER 5296. THE SCORED BLOCK 5296-5306 was never claimed. A successor takes new numbers from the marker as it reads at its
  go-live (5296 unless another run moved it). While holder 67984 holds (to about 04:22:51Z on 2026-10-05) a successor's runner
  would find it PERSISTENT, as G1-4's found 56380 [A]; after that, a new holder claim (branch (b')).

WHAT IT MEANS (implication, not measurement): W1 did on the live cache what it does offline - the four slots landed as
predicted - and every tasked member moved, INF3WPN1 included, so G1-4's shore stop did not recur on cut A, once (n = 1); that
says nothing about which mechanism made G1-4's stop (sec 7; N2). For the first time on M3b, T14 closed its vertex 1 (17 of 17)
and drove its second leg. The W gate's FAIL rests on P-FALS (e) alone: a scorer rule with no culvert clause reading a road
over a culvert as an off-bridge river crossing (N1; the path [A]); the members' movement shows no stop, no constraint line and
no fix off the road at the river. Nothing in the run is read as W1's, the M3 planner's or M3b's.

NEXT, in order (the seat's and the owner's): (1) Fable's cold review of this Result (RL-20260928-04). (2) RULE - the owner:
whether the W gate's FAIL binds as scored, the precedent being RL-20261004-01 for G1-4 (P-FALS (e) there was a chord corner at
a bridge; here a road over a culvert). (3) DESIGN - the seat's, its first step the owner's: (3a) INSTALL the VR-Forces 5.2d
documentation (the vendor's documentation installer; it writes under C:\MAK, so an owner action), then read "Configuring
Aggregate-Level Movement Restrictions" in Adding Content - it settles how the sim resolves a road over a river line; (3b) a
culvert clause for the ways rule (N1 IMPLICATION), its narrow or wide form, FAIL-FIRST on THIS pre-warm
(20261004T202533Z_run) - the clause must turn the 17 crossings ON and keep sec 1(z)'s dirty controls (c1)-(c4) firing;
(3c) a cheap discriminator for N1 (b)'s speed dip, from this run's trace: T10's and T02's speeds through other road junctions
and hamlets (not run by this lane). (4) PREREG G1-6 on the same build and order with that scorer, new
numbers from the marker (5296 or later). (5) Harness: g1_5_rti_federates.py to accept the single join line (and keep the
doubled form), with a control on each log; the post-W inventory check to allow the run's own Stage 2h holder; the
StartRtiExec52 log-name test (N4). (6) Doc updates (sec 6): Appendix B annotated from the manifest (the seat's; this lane does
not edit OPUS_EXECUTION_PLAN.md), PLAN_MOVEMENT rows G1-4 / W1 -> G1-5 and the step table, RUNBOOK sec 11o and sec 9 (W1 seen
live: SL0-SL4 held), RUNBOOK :1716's "docker start if it exists" (sec 8 item 3), HANDOFF_SEAT sec 2.

COLD REVIEW (2026-10-04): Fable, cold, on the evidence only (RL-20260928-04; scratch 7207877b...\fable_g15r\rederive.py,
rederive_out.txt): ACCEPT WITH FIXES - every verdict, limb and number re-checked; its fixes are applied above (N1 (b) the speed
dip and the offsets as the road's bend; N1 (d) Adding Content not installed, NEXT (3a); N3 the scorer's print against this
lane's reading; N4 (a) 68076's resign; the registration freeze). Its point (C): the FAIL does not depend on any reading - the
frozen rule fired on fixes nobody disputes. A culvert clause is a NEW clause; applied to G1-5 it would still be a re-score, so
it belongs to a successor registered before its run.

ADVERSARIAL REVIEW: (N1) the strongest competitor to "they crossed on the road over the culvert" is a ford a few metres
beside it; against it stand the one fix at the river (0.8 m from the road), both banks of every chord on the network, the
plan's road query, and the off-road stop of G1-2's member at a river line; for it, only the 67-170 m fix spacing. Unexplained
and standing: why an impassable River line at C (the vendor filter keeps culverts) did not hold units on the road - the
priority rule (iii) is a candidate mechanism, its manual is not installed, and the speed dip at C sits uneasily with a
road that outranks every entry, so no cause is claimed. (N2) the strongest competitor to "the
pass leaves (e)-(vi) undiscriminated" would be a reading that predicts a stop from 13.6 m - none was registered; the
shore-following reading of (2) rests on 2-s fixes and an unprinted path, and on the planner and the actuator reading the
same water, so it is [A] and is not used to retire (e). (Harness) the federate count rests on grep, not the script.
Verified: the run directory (app log lines, trace, reports, manifest, thread samples, stop logs), the runner log, the rtiexec
log's join and resign lines, the scorer re-run (W gate identical; chord rule; full mode), the OSM geometry on the scorer's
tiles, the vendor RiverL filter, featureconfig and mech sysdef lines, UG52 27.1.4, the seat's go-live artefacts, C:\MAK\logs
and the vendor log by name / size / mtime / count, the sim-cache listings. Assumed: S (docker start) and A2 as the seat
records; that the aggregate terrain loads the RiverL file read here; the priority semantics; the planned paths; the pid of
the pre-check and of Stage 2c (by join order).
