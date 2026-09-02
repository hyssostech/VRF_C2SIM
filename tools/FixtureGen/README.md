# FixtureGen - authored Tank Platoon .scnx fixtures (region-vs-structure test)

Generates two structurally-identical VR-Forces 5.0.2 scenario fixtures that differ
ONLY in world location, to answer the open question from
`docs/experiments/MOJAVE_FIXTURE_2026-07-21.md`: does a STRUCTURALLY-COMPLETE,
AUTHORED, disaggregated Tank Platoon (USA) move at Mojave, or only at Sweden?
(R9 showed remotely-created units get 0 member offset routes at Mojave vs 45 at
Sweden. This isolates region/terrain from authored-vs-remote structure.)

Outputs (written into the MAK scenarios dir so `LaunchVrf -Scenario <name>` finds them):
```
C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Sweden.scnx
C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Mojave.scnx
```
Each = TropicTortoise base (geocentric `MAK Earth Space (online).mtf` + `C2simEx.sms`
+ the 3 mandatory global env objects) with grafted:
- one `AR Plt 1` Tank Platoon (USA) aggregate, class 3 `(11 1 225 3 2 0 0)`,
  Disaggregated, carrying the `vrf-aggregate-move-along` PSR (the R9 offset-route path);
- four `M1A2` members, class 1 `(1 1 225 1 1 3 0)`, parented to the aggregate;
- one `FixtureRoute` control-measure (class 1 `(17 0 0 2 0 0 0)`), 300 m eastward,
  anchor 150 m above terrain (waypoints clamp DOWN - the success case);
- an auto-run `move-along` plan (`plan-name` == aggregate uuid, empty `(triggers )`,
  the move Task directly in the top-level `(Block ...)`).

## How it works (all conventions verified against working shipped scenarios)
- `.scnx` is a ZIP of MAK S-expressions; parts are edited as text and re-zipped.
- The aggregate + 4 members are CLONED from `testFindTankPlatoonPositions.scnx`
  (isolated, structurally complete); the route from `MaklandCoordinatedAttack.scnx`.
- Every object's world position (`kinematics-state`/`parent-kinematics-state`/
  `local-kinematics-state`, all ECEF on a geocentric terrain) is overwritten with the
  target-site ECEF; every `orientation-tait-bryan` (DIS Euler in ECEF) with the
  East-level attitude for the site. Fresh deterministic uuids (uuid5) + oids +
  matching `.omp` entries; the demo scripted-task/script-controller state is stripped;
  the aggregate's member handle-map is remapped to the new member uuids.

## Run
```
python build_fixture.py       # extracts sources into ./_work, writes the two .scnx
python validate_fixture.py    # offline gate: paren balance, ASCII, plan/omp linkage
python coordcheck.py          # back-convert positions to lat/lon; confirm the site
```

## Exercise-clock frame settings (added 2026-09-02)

`--frame-mode` / `--frame-time` write the `(frame-mode "...")` / `(frame-time <s>)` lines of
the `.scn` (VRFUsersGuide 5.0.2 sec 12.2.1 p.351-352, Table 17 p.354; modes also in
`include\vrfcgf\cgf.h:1192-1203`). Legal modes: `variable-frame`, `fixed-frame`,
`fixed-frame-run-to-complete`; a fixed-frame mode needs a non-zero frame time or sim time
never advances (Table 17 p.354).

**Both default to "leave the base scenario's lines alone."** That identity default is what
keeps the fixtures above byte-for-byte reproducible - proven by regenerating all three and
diffing the 33 staged parts by SHA-256 (33/33 identical) after the option was added.

`--out-dir` redirects the `.scnx` away from `C:\MAK\...\userData\scenarios` (the default), so
a fixture can be built without writing under `C:\MAK`.

`--frame-variant SRC:OUT` emits `OUT.scnx` = `SRC.scnx` with ONLY the frame settings moved:
every non-`.scn` part is copied byte-for-byte and the `.scn` gets its part-name references
retargeted plus the two frame lines rewritten. Used to build `TropicTortoise_FFRTC` for
`docs/experiments/PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md`:
```
python build_fixture.py --frame-variant TropicTortoise:TropicTortoise_FFRTC \
       --frame-mode fixed-frame-run-to-complete --frame-time 0.045455
```
The built archive is committed at `frame_variants/TropicTortoise_FFRTC.scnx` so the live
executor only has to copy it into the MAK scenarios directory. Note the `.scnx` SHA-256 is
NOT reproducible (zip entries carry file mtimes); verify the `.scn` inside it instead.
`ecef.py` / `orient.py` are the standalone derivations of the coordinate and
orientation math (validated: ECEF reproduces the handoff Mojave number to 0.000 m;
orientation round-trips stored attitudes to clean integer headings).

Offline-validated with the sibling tool `tools/ScnxDiff/scnx_diff.py dump`.
The `_work/` extract + staging dir is disposable (git-ignored).
