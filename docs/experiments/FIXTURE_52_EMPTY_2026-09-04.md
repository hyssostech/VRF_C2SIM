# R1: the EMPTY 5.2-native fixture (2026-09-04)

Offline build only. Nothing launched; nothing written under `C:\MAK` (donor opened
READ-ONLY, mtime still the 2026-01-05 install date). Implements R1 of
`docs/experiments/RESEARCH_52_FIXTURE_FORMAT_2026-09-04.md` sec 5.

## Build / deploy
```
python tools/FixtureGen/build_fixture.py --profile 5.2 --empty \
       --frame-mode fixed-frame-run-to-complete --frame-time 0.033333 \
       --scenario-name "R9 Mojave empty fixture (5.2, FFRTC)"
# default --out-dir = tools/FixtureGen/frame_variants/ (writes nothing under C:\MAK).
# SANCTIONED DEPLOY, run by a LIVE executor: the same flags plus
#   --out-dir "C:\MAK\vrforces5.2d\userData\scenarios"
#   then LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52
```
`--profile` is explicit; nothing sniffs a version. The 5.0.2 paths are untouched - all
3 authored fixtures still regenerate byte-identical (33/33 parts by SHA-256).

## What was built
| artefact (under `tools/FixtureGen/`) | sha256 | bytes |
| --- | --- | --- |
| `frame_variants/R9_Mojave_Empty_52.scnx` (FFRTC 0.033333) | `ed65c3513f26981311b150e68ce1f8d51f15d9fb48b434e2f864d6438ab83a68` | 6286 |
| `frame_variants/R9_Mojave_Empty_52_VF.scnx` (variable-frame 0.1) | `d6f7ffdf84982c78b2c6086f48af027f28402501c3b377a14ff2f6414ab7ff1f` | 6345 |

- The 5.2 writer stamps a fixed zip date, so a rebuild reproduces the sha256.
- Donor `...5.2d\userData\scenarios\Sample\VR-TheWorld_Online\GroundMovement.scnx` - a
  5.2-NATIVE-saved sample (RESEARCH sec 5), chosen because its `.pln` (36 B), `.osrx`,
  `.sgr`, `.ovl` and `.spt` are ALREADY empty stubs. 7 of 11 members copied byte-for-
  byte, after asserting none names a stripped object.
- `.oob`: 43 simulation objects removed; 2 global singletons kept - `GlblTerrDmg 1`
  `(105 105 105 105 105 105 105)` and `GlobalEnv 1` `(21 0 0 1 0 0 1)`; prefix,
  separator and suffix glue preserved byte-for-byte. `.omp`: 45 -> 2 map-entries.
- `.gui_settings`: `DtObjectSettings` emptied, boost `class_id`s taken VERBATIM from a
  shipped zero-object file (`Sample\DroneAttack.gui_settings`) where
  `SystemScriptsAvailable` is id 2 and `Overlays` id 3 - NOT 4/5 as in a donor carrying
  object settings; the donor's `Overlays` block is spliced in, renumbered.
- `.scn` keys set: `Terrain-Database` = `Gui-Terrain-Database` = `$(SHARED_DATA_DIR)\
  TerrainData\TerrainConfiguration\MAK Earth (online).mtf` (exact name listed 2026-09-04
  in `C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\`);
  `Simulation-Model-Set-Files` = `$(DATA_DIR)\simulationModelSets\EntityLevel.sms`;
  `frame-mode`/`frame-time` (UG52 Table 20 p.354); `scenario-name`; the extent (below).
  The four 5.2-only keys (`gui-runtime-scheme*`, `remote-attachment-scheme*`) come
  through from the donor.
- PLAYBOX/AOI - the only AOI key either file carries is `.scn` `scenario-data
  (ScenarioExtentInformation "x,y,z,r")`, an ECEF centre + radius, decoded and confirmed
  against the donor's own value (46.78N 7.57E, its Swiss play area). Re-centred on the
  R9 box (lat 34.5605-34.6696, lon -116.7127..-116.3867, h 1041 m) ->
  `-2.34914e+06,-4.70144e+06,3.60339e+06,16134.8` = 34.6150 / -116.5496 / 1041 m,
  r 16134.8 m. The `.pln` carries no AOI (empty stub); the two globals hold only
  sentinel positions (`6378137 1 1`, `1 1 1`), so the `.oob` needed no re-centring.

## Validation - `validate_fixture.py` runs two gates, exit non-zero on failure
- 5.0.2 regression, output unchanged: `python validate_fixture.py` -> Sweden OK,
  Mojave OK, `ALL FIXTURES: OK`.
- `--empty-52 frame_variants/R9_Mojave_Empty_52.scnx` -> all 22 checks OK; the VF twin
  passes with `--frame-mode variable-frame --frame-time 0.1`. Checks: both object-type
  syntaxes parse; member set == donor's; paren/ASCII per member; **0 simulation objects**
  + both globals present; terrain (x2) and SMS; the 3 frame checks; `.scn` part refs
  resolve; `.omp` set == `.oob` set; `.pln` has no Plan; no member references a stripped
  object; extent centre in the R9 AOI, radius covering the box.
- Negative controls (`--negative-controls <scratch dir>`, then `--expect-fail`): the
  missing-`frame-time` copy FAILs on `frame-time (absent)`; the stray-object copy FAILs
  on `simulation objects in .oob 1 (expect 0) - A1 (11 1 206 3 2 0 0)`. Gate: OK.
- Instrument bugs caught while building: (a) the `.omp` regex ate BOTH surrounding
  newlines, so back-to-back entries matched only every other one (23 of 45) - an
  independent `(map-entry` count is now asserted on both sides of the strip; (b) matching
  globals on object-type KIND alone is wrong (`Weather.scnx` carries weather-REGION
  objects `(21 0 0 2 0 0 1)`) - match is on the first SIX fields, the 7th differing.

## Live-only questions PREREG_52_FIXTURE_LOAD must answer (RESEARCH sec 6)
1. Does this globals-only `.oob` load headless to serviceable back-end readiness?
2. Does `MAK Earth (online)` page in at 34.6N/-116.55W inside the readiness budget, and
   what does `blockOnAsynchronousOperations 1` cost in wall time there?
3. Do C# creation and MoveAlongRoute behave the same on a `.scn` we authored?
4. Does 5.2 honour FFRTC + `frame-time 0.033333` on an empty scenario (Y-9); is the
   FFRTC/VF pair separable in wall time?
5. Is the authored zero-object `.gui_settings` accepted, and does the extent key alone
   aim the view at the AOI? (unsettled offline)
6. Does 5.2 accept a `.scn` WITHOUT the 4 new scheme keys? NOT exercised (donor has them).
