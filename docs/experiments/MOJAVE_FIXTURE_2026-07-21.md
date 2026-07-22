# Mojave region-vs-structure fixture - build log (2026-07-21)

Supervisor-mode build of the authored Tank Platoon fixture called for by
docs/HANDOFF_2026-07-21_SESSION_JUMP.md "NEXT ACTION". This doc is the living
record; it is updated AS the build progresses, not at the end.

## Question the fixture settles
Our C2SIM-created units freeze at Mojave (R9: 0 member offset routes vs 45 at
Sweden). Does a STRUCTURALLY-COMPLETE, AUTHORED disaggregated Tank Platoon move at
Mojave? R9 never controlled for authored-vs-remote structure. Run the SAME authored
file at Sweden (positive control - expect motion) and Mojave (expect freeze if the
cause is region/terrain; motion if the cause was remote-created structure).

## State verified by the supervisor (own hands, 2026-07-21)
- PORT repo HEAD = 4c12011, tree clean bar untracked .code-workspace.
- appNo "*** NEXT FREE:" marker = 3553 (OPUS_EXECUTION_PLAN.md:1250, sole
  value-bearing marker; 7 other hits are instructions/pointers).
- Processes: only rtiAssistant/rtiexec/rtiForwarder alive; no leftover VRF federate.
- R9 evidence re-read (docs/experiments/R9_region_swap_2026-07-13.txt): line 33
  "Mojave window = 0; Sweden control = 45" offset routes; lines 34-35 the empty-route
  log for 114.MechCoy + 1222.MechPlt at Mojave. Telemetry: Sweden 1222.MechPlt walks
  east (lon 16.499 -> 16.519), every Mojave unit bit-frozen from t=23. Confirmed.

## FORMAT FINDINGS (verified against the files)
- A .scnx is a ZIP of members. .scn/.oob/.xtr/.orb/.pln/.omp = MAK S-expressions;
  .osrx/.spt/.sgr/.ovl/.gui_settings = boost XML. (Confirmed by extraction + the
  in-repo tools/ScnxDiff which already parses this.)
- .oob object grammar: (local-vrf-object (marking-text "...") (object-type <cls>
  (<7 DIS nums>)) ... (state-repository (force ...) (aggregate-state Aggregated|
  Disaggregated) (parent-name "VRF_UUID:<guid>"|"VRF_UUID:<n> Force"|"") ...)).
  class 1 = entity, class 3 = aggregate. Composition = parent-name; subordinate
  order = document order (first = leader). object-type of Tank Platoon (USA) =
  class 3 enum (11 1 225 3 2 0 0); M1A2 member = class 1 enum (1 1 225 1 1 3 0)
  (authoritative source: model set EntityLevel/vrfSim/"Tank Platoon (USA).entity",
  which lists 4 subordinates 1:1:1:225:1:1:3:0 handles PL/PSG/PLWM/PSGWM).
- Model set: C2simEx.sms includes EntityLevel.sms; Tank Platoon (USA) resolves
  under both. TropicTortoise.scnx already pins C2simEx.sms + MAK Earth Space
  (online).mtf and carries the mandatory globals with zero combat units -> it is
  the fixture BASE.
- tools/ScnxDiff dumps but does NOT write, and does NOT yet parse .pln. Use it as
  the offline validation gate on the authored fixture (confirm 1 aggregate +
  4 M1A2 members, Disaggregated, correct types/tree).

## Coordinates (proven-valid, from R9 telemetry t=3)
Reuse R9's golden 1222.MechPlt start so region is the only variable:
- Sweden: 58.702956, 16.499229, alt ~51 m (unit MOVED here in R9).
- Mojave: 34.612956, -116.600487, alt ~1041 m (unit FROZE here in R9).
ECEF (WGS84) to be computed by the supervisor for the .oob position field + a
3-point eastward route at on-terrain altitude (above-terrain = clean structure
test; a below-terrain variant is the confound control per the handoff).

## Controls required (from the handoff NEXT ACTION - load-bearing)
1. Sweden GATE first: if the authored Tank Platoon freezes at Sweden, the fixture is
   wrong - fix before Mojave. (R9's 45 offset routes were mechanized aggregates
   generally, not a Tank Platoon specifically.)
2. Moves-both is NOT sufficient to prove remote-structure-deficiency: the fixture
   uses above-terrain waypoints while C2SIM units had order-derived (maybe
   below-terrain) ones - so ALSO run a below-terrain-waypoint variant.
3. Freezes-both could be a malformed file or a benign non-start: verify
   plan-name == aggregate UUID, the task is in the top-level Block, and the
   state-repository is CLEAN (empty suspended-task-list / task-status-list, no baked
   formation-clamp) BEFORE concluding the move is broken.
4. Convoy 1 (the Hawaii auto-run precedent) uses convoy-to-task, NOT the
   disaggregated move-along/buildOffsetRoute path - so it is only weak precedent;
   the Sweden gate is what makes the fixture trustworthy.

## GRAMMAR + CONVENTIONS (verified by supervisor from working files)
- Clone source = developer_toolkit_examples/luaTerrainReasoningQuery/
  testFindTankPlatoonPositions.scnx :: "AR Plt 1" (uuid 6af0c793): Tank Platoon
  (USA) class 3 (11 1 225 3 2 0 0), Disaggregated, 4 M1A2 members (class 1
  (1 1 225 1 1 3 0)), ForceFriendly, ISOLATED scenario. Its aggregate carries the
  disaggregated-movement subsystem + working-formation + vrf-aggregate-move-along
  PSR (the R9 offset-route path) AND the aggregated-movement subsystem. ONE demo
  scripted task in the aggregate task-status-list -> strip to (task-status-list ).
  Members' task/suspended lists already empty.
- Position: ECEF geocentric XYZ in (kinematics-state (position)) +
  (parent-kinematics-state (position)) + (local-kinematics-state (position)). On a
  GEOCENTRIC terrain (MAK Earth online / MAK Earth Space online) all THREE hold the
  SAME ECEF (verified in BehaviorGroundAttackByFire, on MAK Earth online). On a
  LOCAL flat terrain (Ground-db.mtf, testFindTankPlatoon's terrain) local-kinematics
  is a small topocentric offset. Since the fixture overwrites all three with the
  target ECEF, the clone-source terrain is irrelevant.
- Orientation: (orientation-tait-bryan psi theta phi) = DIS Euler in ECEF. VERIFIED
  by round-trip: BGABF platoon (-1.722977 0.930465 -2.902643) = local heading 170
  deg level; Makland route (-2.093990 -1.396050 -3.141590) = heading 0 (N) level.
  East-level attitude: SWEDEN (1.858762 0 -2.595356); MOJAVE (-0.464266 0 -2.174906)
  (scratchpad/orient.py, round-trips to hdg=90,0,0).
- Move task: task-type "move-along" (NOT "move-along-route" - 0 hits in 68 scnx);
  binds (route "VRF_UUID:..."); traversal-direction 0; start-at-closest-point True.
- Route object: class 1 (17 0 0 2 0 0 0), ForceNeutral, parent-name USE-DEFAULT,
  extended-data (DtDoNotAddToOrbat "1"); (geometry (body-vertices (vertex X Y Z)...))
  are body-frame (NED: X=North Y=East Z=Down) offsets about the ECEF anchor rotated
  by the route's orientation-tait-bryan. Fixture route: anchor = leader ECEF at
  terrain+150 m; orientation = NED heading 0; vertices (0 0 0)(0 150 0)(0 300 0) =
  300 m eastward at anchor height -> ABOVE terrain -> per-vertex clamp is a clamp-DOWN
  (the SUCCESS case; below-terrain clamp-up is the R9 failure).
- Auto-run plan (.pln): outer ( (Plan-File (version "2.0")) (Plan ...) ). A plan
  auto-runs at start iff (triggers ) is EMPTY and the Block has no RegisterTrigger/
  wait-on-trigger - the move Task sits directly in (Block ...). plan-name ==
  owning object's VRF_UUID (verified: Convoy 1 plan-name == Convoy 1 oob uuid).
- .omp: 1:1 (map-entry (address 1 3001) (uuid ...)) per object - MUST add one per
  new object. oid "1:3001:N", N unique; TropicTortoise uses 1..3 -> new = 4..9.
  .orb inert (empty (orbat )); no edit needed.
- BASE = TropicTortoise parts (geocentric MAK Earth Space online + C2simEx.sms +
  mandatory members + 3 global env objects); graft 1 aggregate + 4 members + 1 route
  into .oob, 6 entries into .omp, author .pln, keep .scn terrain/model set.
- Coordinates (ECEF, WGS84; converter validated to 0.000 m vs handoff Mojave):
  SWEDEN leader 58.702956,16.499229 h51; MOJAVE leader 34.612956,-116.600487 h1041
  (R9 golden 1222.MechPlt starts - moved at Sweden, froze at Mojave).

## BUILT + OFFLINE-VALIDATED (2026-07-21)
Generator tools/FixtureGen/build_fixture.py produced two structurally-identical
fixtures differing ONLY in location:
  C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Sweden.scnx
  C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Mojave.scnx
Each = TropicTortoise base + AR Plt 1 (Tank Platoon USA, Disaggregated) with 4 M1A2
members + FixtureRoute + an auto-run move-along plan (plan-name == aggregate uuid).

Offline gates (all PASS, both fixtures):
- scnx_diff dump: 9 objects (1 aggregate + 8 entity); tree = AR Plt 1
  [AGG:Disaggregated] class 3 (11 1 225 3 2 0 0) over M1A2 1..4 (class 1
  (1 1 225 1 1 3 0)); terrain MAK Earth Space (online).mtf; model set C2simEx.sms;
  aggregate carries vrf-aggregate-move-along PSR (the R9 offset-route path).
- Paren-balanced + ASCII-clean on every S-expr member (PowerShell byte scan).
- plan-name == aggregate uuid; task-type "move-along"; (triggers ) empty (auto-run);
  route ref present in .oob; 4 members parent-name == aggregate; agg task-status +
  suspended lists empty; demo script removed; .omp = 9 map-entries, 1:1 with .oob.
- Coordinate back-check: all 3 kinematics triples/object identical ECEF (geocentric
  convention); AR Plt 1 lands at Sweden 58.70296,16.49923 / Mojave 34.61296,
  -116.60049 (d < 1e-4 deg); route anchor +150 m above terrain.

Adversarial review caught + fixed (visible per the loop):
- Extraction-by-uuid-substring hijacked by cross-references (aggregate contains
  member uuids; members contain aggregate uuid) -> switched to own-header indexing.
- Baked script-controller run-state referenced the luaTerrainReasoningQuery script
  (absent from C2simEx.sms) -> emptied (script-state)/(script-information) to the
  BGABF clean form; asserted the demo script string is gone.
- STALE member uuids in the aggregate's PL/PSG/PLWM/PSGWM handle-map (a false-freeze
  trap: aggregate would reference non-existent entities) -> added old->new member
  uuid remap + a post-assembly assert that NO source uuid survives.

Residual (unverifiable offline; the live run + Sweden gate resolve them):
- VR-Forces loader acceptance of the authored S-expr.
- Whether the untriggered move-along auto-runs on RunSim for a disaggregated unit
  (Convoy 1 proved the pattern with convoy-to-task; move-along on a disaggregated
  platoon is the fixture's open question). If it does NOT auto-run, that is the
  handoff's "freezes-both = benign non-start" branch, diagnosable via plan binding.
- Cosmetic: aggregate role handles (PL etc.) map to member LABELS out of doc order;
  irrelevant to the offset-route test (members identified by uuid in telemetry).

## Build status
- [x] State + format recon; grammar recon; conventions resolved (all verified).
- [x] Author + offline-validate Sweden & Mojave fixtures (generator in repo).
- [x] Pre-registration written (2026-07-22): predictions, branch table, falsifiers,
      appNo budget in docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md.
- [ ] GET GO-AHEAD, then live run via the validated pipeline (Sweden gate first).

## PLANNED LIVE RUN (pending go-ahead; NOT yet executed)
Marker 3553. Per fixture: ledger fresh appNos -> LaunchVrf -Scenario
TankPltFixture_Sweden (gate) then _Mojave -> WatchVrf -> RunSim -> analyze -> StopVrf.
Observable: do the 4 disaggregated members get offset routes and move?
  moves-Sweden / freezes-Mojave => REGION/terrain (empty offset-route is location-specific).
  moves-both => authored structure works; then run the below-terrain-waypoint variant.
  freezes-both => diagnose auto-start binding + state-repo cleanliness before concluding.
