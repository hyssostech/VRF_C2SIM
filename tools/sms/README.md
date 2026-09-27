# tools/sms - the C2SIM derived simulation model sets, built at deploy time

`Deploy-C2SimSms.ps1` BUILDS the four derived SMSs this project's `_AG` / `_C2K` fixtures
name by absolute path, under `C:\C2SIM\vrf-sms`, from the VR-Forces 5.2d files INSTALLED on
the machine. It exists because the SMS trees were never in the repo (DEMO_READINESS row 21
said they should be) and were lost with the 2026-09 machine rebuild.

**No vendor file is in this repo, and none may be added** - not even as a test fixture. This
repo is public; vrfSim.opd, the Lua/XML scripts, the .ope and the .sysdef are MAK's. The repo
carries only the recipe: which vendor file, which line, what it must read before the change,
what it reads after.

## Run (pwsh 7, pinned)
```
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimSms.ps1 -WhatIf
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimSms.ps1
```
Parameters: `-VrfRoot` (default `C:\MAK\vrforces5.2d`, read only), `-Dest` (default
`C:\C2SIM\vrf-sms`), `-WhatIf` (assert and hash everything, write nothing). It refuses a
`-Dest` under `C:\MAK` or under `-VrfRoot` (also through a junction), a relative `-Dest` and a
UNC/device path. It is deterministic (no timestamps), idempotent (an identical file is left
alone), deletes nothing, and FAILS (exit 3) if a derived tree holds a file it did not write -
any extra file there would also override the vendor.

Exit codes: 0 ok; 2 bad argument or vendor file missing; 3 a vendor line did not read as
recorded (nothing is written), a read-back hash disagreed, or a stray file; 5 unexpected.

## What it builds
Layout = the vendor's own (`MAKTest.sms` beside `MAKTest\`): `<Dest>\NAME.sms` plus the
model-set directory `<Dest>\NAME\`.

| NAME | overrides (under `<Dest>\NAME\`) | record |
|---|---|---|
| `C2SIM_EntityLevel_AbstractGraphs` | `scripts\ground-vehicle-move-to.lua` + `.xml` | G7b; fixtures `_AG`, `_NavAO_AG`, `_NavAO20_AG_jst` |
| `C2SIM_EntityLevel_AbstractGraphs_Slope2` | the same script + `vrfSim\systems\movement\ground-tracked.sysdef` | N2b; `_NavAO_AG_S2`, `_NavAO20_AG_S2` |
| `C2SIM_EntityLevel_Corridor2000` | `vrfSim\platforms\Ground_Vehicle.ope` | N1; `_NavAO_C2K` |
| `C2SIM_EntityLevel_Corridor2000_Slope2` | the same .ope + the same .sysdef | N2; `_NavAO_C2K_S2` |

Every tree also holds `vrfSim.opd` byte-identical to `EntityLevel\vrfSim.opd` (checked by
sha256). The changes, each asserted against the original line first:

| file | line | vendor original | derived |
|---|---|---|---|
| `ground-vehicle-move-to.lua` | 488 | `local params = {useAbstractGraphs = false, ...}` | `true`, plus one inserted line `printInfo("C2SIM override ground-vehicle-move-to.lua: useAbstractGraphs=true")` |
| `Ground_Vehicle.ope` | 22 | `(DtRwReal propagation-box-extent 200.000000)` | `2000.000000`, plus `(DtRwBoolean enable-path-plan-timing True)` |
| `ground-tracked.sysdef` | 133 | `(slope-avoidance-factor 1.000000)` | `2.000000` |
| `ground-vehicle-move-to.xml` | - | - | unchanged copy (carries `<myScriptId>`) |

Each `.sms` = our 5-line `;;` header + the vendor `MAKTest.sms` below its header comment, with
ONE line changed: `(model-set-directory "MAKTest")` -> `"NAME"`. That body carries
`(include "..\data\simulationModelSets\EntityLevel.sms")` (MAKTest.sms:87; the same form UG52
68.3.3 p1313 prints), `(enumerations-file "")` and `(read-only False)`; the script asserts all
four lines before using it.

Record: `docs/experiments/G7B_G8_RESULTS_2026-09-14.md` :28-35 (the derived SMS, the :488
diff, 32 proof lines in run C), :241-242 (the proof string), :509-518 (shape copied from
MAKTest.sms; CR6 residual risk), :590-595; `docs/experiments/PREREG_N1_N2_CORRIDOR_SLOPE_2026-09-14.md`
:195-218 (the four trees, opd identity, the .ope/.sysdef diffs); `tools/FixtureGen/README.md`
(the `--sms` default). Vendor, VR-Forces 5.2 Users Guide (`docs/vendor/mak-5.2`, git-ignored; re-fetch per
`docs/vendor/README.txt`; pages read 2026-09-25): 68.3.1 p1310
(an including SMS overrides the included one's files), 68.3.3 p1312 (priority), 68.3.4 p1313
("the scripts in the highest priority SMS supersede those in the lower priority SMSs"), 68.3.5
p1314 ("every SMS must have vrfSim.opd"), Table 15 p271 (absolute paths).

## The run-time proof
- AbstractGraphs trees: the `printInfo` line prints once per mesh query in a member console
  (G7b run C: 32 lines for 32 queries; 0 on the vendor script).
- Corridor2000 trees: `enable-path-plan-timing` prints path-planning time (PREREG_N1_N2 sec 3.1).

## Validate (offline)
```
python tools/FixtureGen/validate_fixture.py --empty-52 tools/FixtureGen/frame_variants/R9_Mojave_Empty_52_AG.scnx --sms C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms
```
must report the include of EntityLevel.sms, `overrides script id ground-vehicle-move-to`,
`useAbstractGraphs true, true` (the regex also matches the proof line's own text - the
vendor script reads `false`) and the proof line; `ALL FIXTURES: OK`. For a `_NavAO*` fixture
add `--terrain` with the terrain copy the fixture names. DIRTY CONTROL: the same command with
`--sms C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel.sms` reports
`useAbstractGraphs false`, `run-time proof line (none ...)` and FAILS.

## Gaps (what the record did not keep - laneR_restore_plan sec 4)
- **G1**: the original `.sms` text, the exact `printInfo` line (text and position) and the
  `.xml` were never saved. Chosen here: `.sms` from MAKTest.sms as above (the record says its
  shape was copied from there); the proof line directly after :488, same indentation, text =
  the recorded proof string verbatim, so it prints once per `findPathToLocation` job; the
  `.xml` is the vendor's unchanged (the record names no change to it).
- **G2**: no SMS hash was recorded, so no rebuilt tree can be proved byte-identical to the lost
  one. This build is deterministic; its manifest is the hash record from 2026-09-25 on.
- Run-time proof of the rebuilt trees (a member console showing the proof line) is still owed:
  no live run was part of the rebuild.

Manifest of the 2026-09-25 build (vendor 5.2d inputs: MAKTest.sms 28866ef6...,
ground-vehicle-move-to.lua f38ba14c..., Ground_Vehicle.ope 20be8e14..., ground-tracked.sysdef
6bef1c51..., vrfSim.opd aeb77abf...):
```
a2baa41b68e619aefeec381fb2436448d3e37aa04cab64599bbf57d3f3515f7f  C2SIM_EntityLevel_AbstractGraphs.sms
1a54ea2f8bffc7170ac1f98419f3aa4de1a16011893d7ca14eb6c55cd7da89b4  C2SIM_EntityLevel_AbstractGraphs_Slope2.sms
de0a6e3dd4159674ba7ad709ef9c60d5df071fb2383bd20e18292a1c47cacc90  C2SIM_EntityLevel_Corridor2000.sms
a93b5c68c8428e94682cfdb15d39f1ef8c9363cdc05d1bc4355b32f19bc6ee5f  C2SIM_EntityLevel_Corridor2000_Slope2.sms
00698ff701cb10d24e6679bb6d5a1c0c4417684fa65e1a52ed74187995291d50  */scripts/ground-vehicle-move-to.lua
38998dc1b85f1d5ca66a6d32992d7bb38a1b00929c2ba5a17f03101e855c4a30  */scripts/ground-vehicle-move-to.xml (= vendor)
ce5166579f7e08283994f690c0c56820cfdab5f3afef1d9c2747b201b5a289e8  */vrfSim/platforms/Ground_Vehicle.ope
2df34a50ff0d476e76b84db6d710cd10b3ad402d12288901f04631289b589484  */vrfSim/systems/movement/ground-tracked.sysdef
aeb77abfe687fbce1aeb52d7d620f70c894f4647f8bba8cf8cedccc310eaa1b0  */vrfSim.opd (= vendor)
```

## The AGGREGATE derived set: `Deploy-C2SimAggregateSms.ps1` (package C2, 2026-09-27)

Builds `<Dest>\C2SIM_AggregateTacticalLevel.sms` + `<Dest>\C2SIM_AggregateTacticalLevel\` - the seven AUTHORED US
Army unit types of RL-20260927-04 (Infantry BN (USA, IBCT), FA BN (USA, IBCT), Brigade Engineer BN (USA, IBCT),
Brigade Support BN (USA), Division HQ (USA), FA BN (USA, ABCT), Brigade Engineer BN (USA, ABCT)). The set INCLUDES the
shipped `AggregateTacticalLevel.sms` unchanged and only ADDS: `vrfSim.opd` (= the vendor's), one `vrfSim\*.entity`
per type (an SMS's object types ARE those files, UG52 68.8 p1326) and one `gui\visuals\Unit\*.leaf` + `*.magx` per
type (its symbol). Record, provenance and open items: `docs/experiments/AGGREGATE_AUTHORED_UNITS_2026-09-27.md`.

The chain, every step checked:
- `aggregate_authored_design.json` - the DECISIONS (donor, assemblies, set values, system deletes/grafts, symbol),
  each with its reason and doctrine paragraph.
- `python tools/aggregate/authored_units.py --write` - turns them into `C2SIM_AggregateTacticalLevel.recipe.json`:
  literal edits against the installed vendor files (a single-line edit names the exact line; a block edit its first
  line, line count and sha256) plus the sha256 of every input and of every OUTPUT. `--check` = the committed recipe
  equals the regeneration (the gate after any design or vendor change); `--report` = the rolled values and every
  value KEPT from a donor; `--selftest` = the roll-up port against the vendor's own rolled units + DIRTY controls;
  `--emit DIR` = the Python-side build into a scratch directory (refuses C:\MAK).
- `Deploy-C2SimAggregateSms.ps1` - asserts every edit on the installed files, builds in memory, gates each output
  (ASCII, XML parse / .sms parentheses, sha256 == recipe), then writes; a failed assertion writes nothing.

```
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimAggregateSms.ps1 -WhatIf
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimAggregateSms.ps1
& "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -File tools\sms\Deploy-C2SimAggregateSms.ps1 -SelfTest
```
Parameters and guards as `Deploy-C2SimSms.ps1` (`-VrfRoot`, `-Dest`, plus `-Recipe`); exit codes in the script
header (0 ok; 2 bad argument or missing file; 3 vendor changed / output hash / not ASCII / no parse / read-back /
stray file). `-SelfTest` builds CLEAN into a temp directory (22 files + the .sms, idempotent rerun) and runs six DIRTY
controls on a mirrored vendor tree - an edited donor line, a line inside an edited block, the first line of a deleted
block, a line inside a grafted source block, an asserted .sms line, a donor line no edit touches - each must exit 3
having written nothing. NO `smsChecksum*.cksm`: those are the upgrade tool's checksums of the STANDARD sets (UG52
68.5.2 p1319); MAKTest ships none.

THE PAIRING: the authored types exist ONLY in this set. On the shipped `AggregateTacticalLevel.sms` they land empty
generic containers or the base abstract, so the `authored` composition variant runs only with the fixture
`IronStorm_Centre_52_Aggregate_C2SIM` (tools/FixtureGen). Do not "Roll Up" these types in the Simulation Object
Editor: it would overwrite the recipe's set values (UG52 72.5 p1428) - rerun this script instead.

Manifest of the 2026-09-27 build (vendor 5.2d inputs: 22, sha256 each in the recipe's `vendorInputs`):
```
22e99245ee29f27d312d2065ef5199643e60efdcd4b6c4fd0d109d8da4b5a7f1  C2SIM_AggregateTacticalLevel.sms
6384a5a9662a5cdfb4f56d60bf347834e9fa3d14c298f992de7b3830d5608d16  */vrfSim.opd (= vendor)
60e60943e6df057be80d3ac6db46ba720abd28082bd6a1c619eafcbbd20ff143  */vrfSim/Infantry BN (USA, IBCT).entity
e8669ce3dcc88624cc776e683942b103878a5af6c62f64ff97c19ea8667db3fe  */vrfSim/FA BN (USA, IBCT).entity
ee8d53f4b12d1e7d967036d984c5fb10a17ca8a35e1c2123fa4f70ec79629165  */vrfSim/Brigade Engineer BN (USA, IBCT).entity
56a646fd259737921f402bbb37e96d820f7c6a9534ffe66986d53b686284a6d8  */vrfSim/Brigade Support BN (USA).entity
d8cc0074ad2098f011661eea0d61bbfed032701773c14278402508e905b23e67  */vrfSim/Division HQ (USA).entity
91ee9579223829a8555b2622761e8b881853321da86ecc50aa60d53a963e5bd0  */vrfSim/FA BN (USA, ABCT).entity
0cf7fa388ea206cae2e8dd80173a4d35f3810dba5859abe3664d3a3b72dc7c0d  */vrfSim/Brigade Engineer BN (USA, ABCT).entity
3a3cf3ab3ab67396d0ad9b7843e0ff45389a20dd3e626bf4066ee236edca845c  */gui/visuals/Unit/Infantry BN (USA, IBCT).leaf
cf3811217151871a46aba0ae518f7b80e6bfe92354add5e7db4badd0bac4635e  */gui/visuals/Unit/Infantry BN (USA, IBCT).magx
9b0244eb2083e3854bb270540e33358f2008d11eb83a99126714405a1d8efa7a  */gui/visuals/Unit/FA BN (USA, IBCT).leaf
913b4d9bcf149adecfed607149da64f3762ea0680f30cbf7eb9089730f2e5276  */gui/visuals/Unit/FA BN (USA, IBCT).magx
e9a9c3152611b19afb1393426dc17ed8157bfad2d7707e837f1feebe65d6a30d  */gui/visuals/Unit/Brigade Engineer BN (USA, IBCT).leaf
0e710bcb2a71cdf3360023080318be3ce1e23b7f6ea48278b9a477889918f60b  */gui/visuals/Unit/Brigade Engineer BN (USA, IBCT).magx
f9ac0dfbce0cab9b1a79e5c138882339a75f7e632257d132638ccd3a61e81541  */gui/visuals/Unit/Brigade Support BN (USA).leaf
b09e0955162d8f7170286d95c76f49fc78d0852a5f32beb5ce023a790ad4fb4f  */gui/visuals/Unit/Brigade Support BN (USA).magx
20f64e6d9bfaf3f616e4402bf7cdcd65f2b15e7bac2b79d588d7a9ce1052bb57  */gui/visuals/Unit/Division HQ (USA).leaf
a4650f5829152faf332cc723ec7a873e3218b47619ecc37fe104727f193f803e  */gui/visuals/Unit/Division HQ (USA).magx
7293305e99f322b60e63548d26fd3d1a05884e92768b3e3b2e84f7ec0a2f6d47  */gui/visuals/Unit/FA BN (USA, ABCT).leaf
4d577b0cc60c7b4d8ff3c1131f6dfc767f98ab804fe36f89cbb4ddbcdc036e41  */gui/visuals/Unit/FA BN (USA, ABCT).magx
54d52aea6e521ac1d696349ccee120af742f88d3a6f8314764b3472446e53833  */gui/visuals/Unit/Brigade Engineer BN (USA, ABCT).leaf
b4fb636484d3c14d0145d15f9e7ed66482b26fc1b16425bf693bcaac7c1151a2  */gui/visuals/Unit/Brigade Engineer BN (USA, ABCT).magx
```
