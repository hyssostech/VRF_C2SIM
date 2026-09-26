# tools/navdata - generating, gating and registering navigation areas

- `nav_gate.py` - the connectivity gate. Run it on every generation log before anything uses the area.
- `make_nav_terrain.py` - registers a `.navRuntimeConfig` on a COPY of the terrain (`out/`, git-ignored).
- Tests: `pwsh -NoProfile -File tests\NavGate.Tests.ps1`; `python make_nav_terrain.py --selftest`.

## Generating an area (vrfNavGenerator.exe, headless)

Vendor references: the tool's own usage text (`vrfNavGenerator.exe --help`, captured 2026-09-25 to
`C:\C2SIM\vrf-nav\work\log\vrfNavGenerator-help.txt`), plus UG52 ch. 66 (66.2 p1276, 66.2.1 p1277, 66.3 p1283,
66.5 p1287). No PDF documents the command line. Own record: docs/experiments/PREREG_NAVCONTROL_WEST20_2026-09-15.md
:462-470 (the invocation) and PREREG_ASSEMBLY_LAYOUT_2026-09-07.md sec 3g (gen1-gen4).

    cwd  C:\MAK\vrforces5.2d\bin64
    env  PATH = C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin;<PATH>
         MAK_VRFDIR=C:\MAK\vrforces5.2d  MAK_VRLDIR=C:\MAK\vrlink5.10
         MAKLMGRD_LICENSE_FILE = the User-scope value (the Machine scope may name an expired file)
    vrfNavGenerator.exe --terrain "<.mtf>" --config "<cfg>\<AREA>.navGenConfig" --outputPath "<navDataDir>\<AREA>"
         --verbose --navDataDir "<navDataDir>" --userDataDir "C:\C2SIM\vrf-nav\userdata"
         --logFileName "<log>\gen-<AREA>.log"   > stdout file (never a pipe)

Rules, each from a recorded failure:
- `--verbose` is a bare SWITCH. `--verbose 1` is refused (WEST20 :282-289). Log headers print "--verbose 1" anyway.
- The tool EMPTIES `--outputPath` first. Keep the config and the log outside it (ASSEMBLY_LAYOUT gen1).
- Always pass `--navDataDir`. The default is $(SHARED_DATA_DIR)/TerrainData/navData, which is under C:\MAK (gen1).
- PRE-CREATE the `--navDataDir` folder (or pass `--runtimeConfigPath <file>`). On 2026-09-25 the folder did not exist
  when the tool wrote the .navRuntimeConfig. The log said "DtReaderWriterFile::putSelf() : Could not open file ...
  for writing", and the run finished with NO runtime config. Leading explanation, not yet re-tested: the write does
  not create parent folders.
- Pass `--userDataDir C:\C2SIM\vrf-nav\userdata` (owner decision 2026-09-25). At the end of a run the tool MOVES every
  per-sector .ClientInput intermediate to <userData>\NavDataDebug. The default <userData> is ..\userData, i.e.
  C:\MAK\vrforces5.2d\userData. On 2026-09-25 that was 1,600 files / 10.8 GB for a 20 x 20 km area. During the run
  the intermediates sit in --outputPath, so expect ~10 GB there mid-run.
- Do NOT pass `--appDataDir` (WEST20 :47-49; owner keeps that). Known consequence: the tool writes its streamed-terrain
  tile cache to C:\MAK\vrforces5.2d\appData\cache\vrfsim (144 MB on 2026-09-25).
- MAX_PATH: keep the path prefixes short. gen3 failed at 338 characters. The output tree under C:\C2SIM\vrf-nav is
  short enough without a junction. Long scratch folders are not.
- Never run the generator during a live run; it is CPU-heavy (WEST20 :129-134).
- A regeneration is a NEW artefact. On MAK Earth (online) the streamed terrain input changes over time (WEST20 PART 2),
  so the old area's numbers do not carry over. Gate it again.

## Gating

    python tools\navdata\nav_gate.py <gen.log> --area-dir "<navDataDir>\<AREA>" --area-hash [--json]

Exit 0 = every sector's ratio (Average Neighbor Node Count / (Average Node Count - 1)) >= 0.9. Exit 1 = at least one
sector fails; the failing sectors are listed. Exit 2 = nothing parsed. Record the byte size, the manifest sha256 and
the nav-tag histogram alongside the result.

## Registering

    python tools\navdata\make_nav_terrain.py --runtime-config "<navDataDir>\<AREA>.navRuntimeConfig" --out "<copy .mtf>"

The copy names the runtime config by absolute path, so the data must stay where it was generated.
